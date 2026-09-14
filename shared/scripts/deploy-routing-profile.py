#!/usr/bin/env python3
"""Deploy customer-owned KQL tables, functions, and update policies."""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import shutil
import subprocess
import sys
import urllib.error
import urllib.request
from typing import Any


IDENTIFIER_PATTERN = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")
KUSTO_TYPES = {
    "bool",
    "datetime",
    "decimal",
    "dynamic",
    "guid",
    "int",
    "long",
    "real",
    "string",
    "timespan",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Deploy a RawTelemetry routing profile to a Fabric Eventhouse"
    )
    parser.add_argument("--profile", required=True, help="Path to routes.json")
    parser.add_argument("--query-uri", help="KQL query service URI")
    parser.add_argument("--database", help="KQL database name")
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Validate and print commands without connecting to Fabric",
    )
    return parser.parse_args()


def require_identifier(value: Any, field: str) -> str:
    if not isinstance(value, str) or not IDENTIFIER_PATTERN.fullmatch(value):
        raise ValueError(f"{field} must be a valid Kusto identifier")
    return value


def optional_bool(value: Any, default: bool, field: str) -> bool:
    if value is None:
        return default
    if not isinstance(value, bool):
        raise ValueError(f"{field} must be a boolean")
    return value


def load_profile(profile_path: pathlib.Path) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    if not profile_path.is_file():
        raise ValueError(f"Routing profile not found: {profile_path}")

    profile = json.loads(profile_path.read_text(encoding="utf-8"))
    if profile.get("version") != 1:
        raise ValueError("Routing profile version must be 1")

    source_table = require_identifier(profile.get("sourceTable"), "sourceTable")
    if source_table != "RawTelemetry":
        raise ValueError("sourceTable must be RawTelemetry")

    routes = profile.get("routes")
    if not isinstance(routes, list) or not routes:
        raise ValueError("Routing profile must contain at least one route")

    names: set[str] = set()
    targets: set[str] = set()
    normalized_routes: list[dict[str, Any]] = []

    for index, route in enumerate(routes):
        if not isinstance(route, dict):
            raise ValueError(f"routes[{index}] must be an object")

        name = require_identifier(route.get("name"), f"routes[{index}].name")
        target = require_identifier(
            route.get("targetTable"), f"routes[{index}].targetTable"
        )
        if target == source_table:
            raise ValueError(f"Route {name} cannot target its source table")
        if name in names:
            raise ValueError(f"Duplicate route name: {name}")
        if target in targets:
            raise ValueError(f"Duplicate target table: {target}")
        names.add(name)
        targets.add(target)

        columns = route.get("columns")
        if not isinstance(columns, list) or not columns:
            raise ValueError(f"Route {name} must define at least one column")

        normalized_columns: list[dict[str, str]] = []
        column_names: set[str] = set()
        for column_index, column in enumerate(columns):
            if not isinstance(column, dict):
                raise ValueError(
                    f"Route {name} column {column_index} must be an object"
                )
            column_name = require_identifier(
                column.get("name"), f"Route {name} column name"
            )
            column_type = str(column.get("type", "")).lower()
            if column_type not in KUSTO_TYPES:
                raise ValueError(
                    f"Route {name} column {column_name} has unsupported type "
                    f"{column_type!r}"
                )
            if column_name in column_names:
                raise ValueError(f"Route {name} has duplicate column {column_name}")
            column_names.add(column_name)
            normalized_columns.append({"name": column_name, "type": column_type})

        transform_value = route.get("transformFile")
        if not isinstance(transform_value, str) or not transform_value.strip():
            raise ValueError(f"Route {name} must define transformFile")
        transform_path = (profile_path.parent / transform_value).resolve()
        if not transform_path.is_relative_to(profile_path.parent.resolve()):
            raise ValueError(f"Route {name} transformFile escapes the profile directory")
        if not transform_path.is_file():
            raise ValueError(f"Route {name} transform not found: {transform_path}")

        query = transform_path.read_text(encoding="utf-8").strip()
        query_lines = [
            line.strip()
            for line in query.splitlines()
            if line.strip() and not line.strip().startswith("//")
        ]
        if not query_lines or not query_lines[0].startswith("RawTelemetry"):
            raise ValueError(f"Route {name} transform must start from RawTelemetry")
        if any(line.startswith(".") for line in query_lines):
            raise ValueError(f"Route {name} transform cannot contain management commands")

        normalized_routes.append(
            {
                "name": name,
                "targetTable": target,
                "columns": normalized_columns,
                "query": query,
                "enabled": optional_bool(
                    route.get("enabled"), True, f"Route {name} enabled"
                ),
                "transactional": optional_bool(
                    route.get("transactional"), False, f"Route {name} transactional"
                ),
            }
        )

    return profile, normalized_routes


def function_name(route_name: str) -> str:
    return f"fn_route_{route_name}"


def build_commands(routes: list[dict[str, Any]]) -> list[str]:
    commands: list[str] = []
    for route in routes:
        target = route["targetTable"]
        columns = ",\n  ".join(
            f"{column['name']}:{column['type']}" for column in route["columns"]
        )
        fn_name = function_name(route["name"])
        commands.append(f".create-merge table {target} (\n  {columns}\n)")
        commands.append(
            ".create-or-alter function "
            f"with (folder='routing', docstring='Managed RawTelemetry route: "
            f"{route['name']}') {fn_name}() {{\n{route['query']}\n}}"
        )
        policy = [
            {
                "IsEnabled": route["enabled"],
                "Source": "RawTelemetry",
                "Query": f"{fn_name}()",
                "IsTransactional": route["transactional"],
                "PropagateIngestionProperties": True,
            }
        ]
        policy_json = json.dumps(policy, separators=(",", ":"))
        commands.append(
            f".alter table {target} policy update\n```\n{policy_json}\n```"
        )
    return commands


def resolve_azure_cli() -> str:
    for candidate in ("az", "az.cmd", "az.bat", "az.exe"):
        resolved = shutil.which(candidate)
        if resolved:
            return resolved
    raise RuntimeError("Azure CLI ('az') was not found on PATH")


def get_access_token() -> str:
    completed = subprocess.run(
        [
            resolve_azure_cli(),
            "account",
            "get-access-token",
            "--resource",
            "https://kusto.kusto.windows.net",
            "--query",
            "accessToken",
            "-o",
            "tsv",
        ],
        check=True,
        capture_output=True,
        text=True,
    )
    token = completed.stdout.strip()
    if not token:
        raise RuntimeError("Azure CLI returned an empty Kusto access token")
    return token


def execute_command(query_uri: str, database: str, token: str, command: str) -> None:
    endpoint = f"{query_uri.rstrip('/')}/v1/rest/mgmt"
    body = json.dumps({"db": database, "csl": command}).encode("utf-8")
    request = urllib.request.Request(
        endpoint,
        data=body,
        method="POST",
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
            "Accept": "application/json",
        },
    )
    try:
        with urllib.request.urlopen(request) as response:
            response.read()
    except urllib.error.HTTPError as exc:
        details = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(
            f"Kusto command failed with HTTP {exc.code}: {details}\n"
            f"Failing command:\n{command}"
        ) from exc


def main() -> int:
    args = parse_args()
    profile_path = pathlib.Path(args.profile).resolve()
    _, routes = load_profile(profile_path)
    commands = build_commands(routes)

    if args.dry_run:
        print(f"Validated {len(routes)} route(s) from {profile_path}")
        for command in commands:
            print(f"\n{command}")
        return 0

    if not args.query_uri or not args.database:
        raise ValueError("--query-uri and --database are required unless --dry-run is used")

    token = get_access_token()
    for command in commands:
        execute_command(args.query_uri, args.database, token, command)

    print(f"Deployed {len(routes)} route(s) from {profile_path}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, RuntimeError, subprocess.CalledProcessError) as exc:
        print(f"Routing profile deployment failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
