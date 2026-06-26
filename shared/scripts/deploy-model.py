#!/usr/bin/env python3
"""Deploy ISA-95 core and extension KQL scripts using a connection contract."""

from __future__ import annotations

import argparse
import json
import pathlib
import shutil
import subprocess
import sys
import uuid
import urllib.error
import urllib.request
from typing import Iterable

import yaml

from hierarchy_mapper import map_hierarchy_config_to_nodes
from sql_connection import SqlConnectionValidationError, build_pyodbc_connection_string, parse_sql_target
from sql_seed_commands import build_upsert_node_commands

try:
    import pyodbc
except Exception:  # noqa: BLE001
    pyodbc = None

REQUIRED_CONNECTION_FIELDS = (
    "tenantId",
    "subscriptionId",
    "resourceGroup",
    "region",
    "workspaceId",
    "eventhouseId",
    "kqlDatabase",
    "generatedAt",
    "schemaVersion",
)

REQUIRED_HIERARCHY_KEYS = ("enterprise", "sites", "areas", "workCenters", "workUnits")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Deploy ISA-95 model assets")
    parser.add_argument("--connection", required=True)
    parser.add_argument("--core-dir", required=True)
    parser.add_argument("--extensions-dir", required=True)
    parser.add_argument("--hierarchy-config", required=True)
    parser.add_argument("--fail-on-warning", action="store_true")
    parser.add_argument(
        "--log-kql",
        action="store_true",
        help="Print each generated KQL command before execution",
    )
    return parser.parse_args()


def read_connection(path: pathlib.Path) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    missing = [k for k in REQUIRED_CONNECTION_FIELDS if k not in data or not data[k]]
    if missing:
        raise ValueError(f"connection.json missing required fields: {', '.join(missing)}")
    return data


def iter_kql_files(folder: pathlib.Path) -> Iterable[pathlib.Path]:
    if not folder.exists():
        return []
    return sorted(folder.glob("*.kql"))


def get_access_token(resource: str) -> str:
    base_args = [
        "account",
        "get-access-token",
        "--resource",
        resource,
        "--query",
        "accessToken",
        "-o",
        "tsv",
    ]

    az_path = shutil.which("az") or shutil.which("az.cmd") or shutil.which("az.exe")
    if az_path:
        completed = subprocess.run([az_path, *base_args], check=False, capture_output=True, text=True)
        if completed.returncode == 0 and completed.stdout.strip():
            return completed.stdout.strip()

    # Fallback for Windows shells where az is available via cmd but not direct PATH lookup.
    fallback_cmd = ["cmd", "/c", "az", *base_args]
    completed = subprocess.run(fallback_cmd, check=False, capture_output=True, text=True)
    if completed.returncode != 0 or not completed.stdout.strip():
        raise RuntimeError(f"Failed to get Azure token for {resource}: {completed.stderr.strip()}")
    return completed.stdout.strip()


def http_get_json(url: str, bearer_token: str) -> dict:
    request = urllib.request.Request(
        url,
        headers={"Authorization": f"Bearer {bearer_token}"},
        method="GET",
    )
    try:
        with urllib.request.urlopen(request) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"GET {url} failed ({exc.code}): {body}") from exc


def http_post_json(url: str, bearer_token: str, payload: dict) -> dict:
    body = json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(
        url,
        headers={
            "Authorization": f"Bearer {bearer_token}",
            "Content-Type": "application/json; charset=utf-8",
        },
        data=body,
        method="POST",
    )
    try:
        with urllib.request.urlopen(request) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        error_body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"POST {url} failed ({exc.code}): {error_body}") from exc


def resolve_query_service_uri(connection: dict) -> str:
    fabric_token = get_access_token("https://api.fabric.microsoft.com")
    workspace_id = connection["workspaceId"]
    databases_url = f"https://api.fabric.microsoft.com/v1/workspaces/{workspace_id}/kqlDatabases"
    databases = http_get_json(databases_url, fabric_token).get("value", [])
    target_name = connection["kqlDatabase"]

    for database in databases:
        if database.get("displayName") == target_name:
            properties = database.get("properties", {})
            query_uri = properties.get("queryServiceUri")
            if query_uri:
                return query_uri.rstrip("/")

    raise RuntimeError(f"Unable to resolve queryServiceUri for KQL database '{target_name}'")


def split_kql_commands(script_text: str) -> list[str]:
    commands: list[str] = []
    current: list[str] = []

    for line in script_text.splitlines():
        stripped = line.strip()
        if not current and (not stripped or stripped.startswith("//")):
            continue

        starts_new_command = line.lstrip().startswith(".") and len(current) > 0
        if starts_new_command:
            command = "\n".join(current).strip()
            if command:
                commands.append(command)
            current = [line]
        else:
            current.append(line)

    trailing = "\n".join(current).strip()
    if trailing:
        commands.append(trailing)
    return commands


def check_kusto_response(response_payload: dict) -> None:
    for table in response_payload.get("Tables", []):
        if table.get("TableName") != "Table_2":
            continue
        columns = table.get("Columns", [])
        rows = table.get("Rows", [])
        col_map = {col.get("ColumnName"): idx for idx, col in enumerate(columns)}
        severity_idx = col_map.get("SeverityName")
        status_idx = col_map.get("StatusCode")
        desc_idx = col_map.get("StatusDescription")
        if severity_idx is None:
            continue
        for row in rows:
            severity_name = str(row[severity_idx])
            status_code = row[status_idx] if status_idx is not None else 0
            if severity_name in {"Error", "Fatal"} or (isinstance(status_code, int) and status_code != 0):
                description = row[desc_idx] if desc_idx is not None else "Unknown failure"
                raise RuntimeError(f"KQL command failed: {description}")


def kql_string(value: str) -> str:
    return json.dumps(value)


def build_append_command(table_name: str, columns: list[str], rows: list[list[str]]) -> str:
    column_signature = ", ".join(f"{column}:string" for column in columns)
    if rows:
        values: list[str] = []
        for row in rows:
            values.extend(kql_string(cell) for cell in row)
        datatable_rows = "[\n  " + ",\n  ".join(values) + "\n]"
    else:
        datatable_rows = "[]"
    return (
        f".append {table_name} <|\n"
        f"datatable ({column_signature})\n"
        f"{datatable_rows}"
    )


def generate_hierarchy_seed_commands(config_path: pathlib.Path) -> list[str]:
    config = yaml.safe_load(config_path.read_text(encoding="utf-8"))
    if not isinstance(config, dict):
        raise ValueError("Hierarchy config must be a YAML object")

    missing = [key for key in REQUIRED_HIERARCHY_KEYS if key not in config]
    if missing:
        raise ValueError(f"Hierarchy config missing required keys: {', '.join(missing)}")

    enterprise = config["enterprise"]
    enterprise_rows = [[enterprise["id"], enterprise["name"]]]

    site_rows = [[site["id"], site["enterpriseId"], site["name"]] for site in config["sites"]]
    area_rows = [[area["id"], area["siteId"], area["name"]] for area in config["areas"]]
    workcenter_rows = [
        [work_center["id"], work_center["areaId"], work_center["name"]]
        for work_center in config["workCenters"]
    ]
    workunit_rows = [
        [work_unit["id"], work_unit["workCenterId"], work_unit["name"]]
        for work_unit in config["workUnits"]
    ]

    table_definitions = [
        ("Enterprise", ["EnterpriseId", "Name"], enterprise_rows),
        ("Site", ["SiteId", "EnterpriseId", "Name"], site_rows),
        ("Area", ["AreaId", "SiteId", "Name"], area_rows),
        ("WorkCenter", ["WorkCenterId", "AreaId", "Name"], workcenter_rows),
        ("WorkUnit", ["WorkUnitId", "WorkCenterId", "Name"], workunit_rows),
    ]

    commands: list[str] = []
    for table_name, columns, rows in table_definitions:
        commands.append(f".clear table {table_name} data")
        commands.append(build_append_command(table_name, columns, rows))
    return commands


def is_data_load_command(command: str) -> bool:
    normalized = command.lower()
    write_markers = (
        ".ingest",
        ".set",
        ".append",
        ".set-or-append",
        ".set-or-replace",
        ".insert",
    )
    return any(marker in normalized for marker in write_markers)


def run_kql_file(
    file_path: pathlib.Path,
    connection: dict,
    query_service_uri: str,
    kusto_token: str,
    log_kql: bool,
) -> bool:
    commands = split_kql_commands(file_path.read_text(encoding="utf-8"))
    if not commands:
        print(f"[skip] {file_path.name} has no executable KQL commands")
        return False

    endpoint = f"{query_service_uri}/v1/rest/mgmt"
    contains_data_load = False
    for index, command in enumerate(commands, start=1):
        print(f"[apply] {file_path.name}#{index} -> {connection['kqlDatabase']}")
        if log_kql:
            print(f"[kql-begin] {file_path.name}#{index}")
            print(command)
            print(f"[kql-end] {file_path.name}#{index}")

        if is_data_load_command(command):
            contains_data_load = True

        response_payload = http_post_json(
            endpoint,
            kusto_token,
            {"db": connection["kqlDatabase"], "csl": command},
        )
        check_kusto_response(response_payload)
    return contains_data_load


def run_hierarchy_seed(config_path: pathlib.Path) -> None:
    cmd = [
        sys.executable,
        str(pathlib.Path(__file__).with_name("seed-hierarchy.py")),
        "--config",
        str(config_path),
    ]
    completed = subprocess.run(cmd, check=False)
    if completed.returncode != 0:
        raise RuntimeError("Hierarchy seeding failed")


def run_hierarchy_kql_seed(
    config_path: pathlib.Path,
    connection: dict,
    query_service_uri: str,
    kusto_token: str,
    log_kql: bool,
) -> bool:
    commands = generate_hierarchy_seed_commands(config_path)
    endpoint = f"{query_service_uri}/v1/rest/mgmt"

    for index, command in enumerate(commands, start=1):
        print(f"[apply] hierarchy-seed#{index} -> {connection['kqlDatabase']}")
        if log_kql:
            print(f"[kql-begin] hierarchy-seed#{index}")
            print(command)
            print(f"[kql-end] hierarchy-seed#{index}")

        response_payload = http_post_json(
            endpoint,
            kusto_token,
            {"db": connection["kqlDatabase"], "csl": command},
        )
        check_kusto_response(response_payload)
    return True


def query_table_count(
    table_name: str,
    connection: dict,
    query_service_uri: str,
    kusto_token: str,
) -> int:
    endpoint = f"{query_service_uri}/v2/rest/query"
    response_payload = http_post_json(
        endpoint,
        kusto_token,
        {"db": connection["kqlDatabase"], "csl": f"{table_name} | count"},
    )

    if not isinstance(response_payload, list):
        return 0

    for frame in response_payload:
        if frame.get("FrameType") == "DataTable" and frame.get("TableKind") == "PrimaryResult":
            rows = frame.get("Rows", [])
            if rows and rows[0]:
                return int(rows[0][0])
    return 0


def print_dimension_counts(connection: dict, query_service_uri: str, kusto_token: str) -> None:
    dimensions = ("Enterprise", "Site", "Area", "WorkCenter", "WorkUnit")
    print("[stage] dimension row counts")
    for table_name in dimensions:
        count = query_table_count(table_name, connection, query_service_uri, kusto_token)
        print(f"[count] {table_name}: {count}")


def run_sql_baseline_seed(connection: dict, hierarchy_config: pathlib.Path) -> bool:
    target = parse_sql_target(connection)
    if target is None:
        print("[stage] skip SQL baseline seed (sqlBaseline metadata not provided)")
        return False

    if pyodbc is None:
        raise RuntimeError("pyodbc is required when sqlBaseline metadata is present")

    hierarchy = yaml.safe_load(hierarchy_config.read_text(encoding="utf-8"))
    nodes = map_hierarchy_config_to_nodes(hierarchy)
    commands = build_upsert_node_commands(nodes)
    seed_run_id = str(uuid.uuid4())

    conn_str = build_pyodbc_connection_string(target)
    with pyodbc.connect(conn_str) as conn:  # type: ignore[union-attr]
        cursor = conn.cursor()
        cursor.execute(
            """
            INSERT INTO dbo.baseline_seed_run (seed_run_id, status, seed_source)
            VALUES (?, 'Running', ?)
            """,
            seed_run_id,
            str(hierarchy_config),
        )

        try:
            for statement, params in commands:
                cursor.execute(statement, params)
            cursor.execute(
                """
                UPDATE dbo.baseline_seed_run
                SET status='Succeeded', completed_at=SYSUTCDATETIME(), counts=?
                WHERE seed_run_id=?
                """,
                json.dumps({"baselineNodes": len(commands)}),
                seed_run_id,
            )
            conn.commit()
            print(f"[seed-run] Succeeded: {seed_run_id}")
            return True
        except Exception as exc:  # noqa: BLE001
            cursor.execute(
                """
                UPDATE dbo.baseline_seed_run
                SET status='Failed', completed_at=SYSUTCDATETIME(), error_message=?
                WHERE seed_run_id=?
                """,
                str(exc),
                seed_run_id,
            )
            conn.commit()
            raise


def main() -> int:
    args = parse_args()
    connection = read_connection(pathlib.Path(args.connection))
    query_service_uri = resolve_query_service_uri(connection)
    kusto_token = get_access_token("https://kusto.kusto.windows.net")
    detected_data_load = False

    print("[stage] apply core scripts")
    for kql_file in iter_kql_files(pathlib.Path(args.core_dir)):
        detected_data_load = run_kql_file(
            kql_file,
            connection,
            query_service_uri,
            kusto_token,
            args.log_kql,
        ) or detected_data_load

    # Extensions are intentionally applied after core scripts.
    print("[stage] apply extension scripts")
    for kql_file in iter_kql_files(pathlib.Path(args.extensions_dir)):
        detected_data_load = run_kql_file(
            kql_file,
            connection,
            query_service_uri,
            kusto_token,
            args.log_kql,
        ) or detected_data_load

    print("[stage] seed hierarchy")
    run_hierarchy_seed(pathlib.Path(args.hierarchy_config))

    sql_seeded = run_sql_baseline_seed(connection, pathlib.Path(args.hierarchy_config))
    if not sql_seeded:
        print("[stage] populate ISA-95 dimensions")
        detected_data_load = run_hierarchy_kql_seed(
            pathlib.Path(args.hierarchy_config),
            connection,
            query_service_uri,
            kusto_token,
            args.log_kql,
        ) or detected_data_load

    if not detected_data_load:
        warning = (
            "No KQL data-loading commands were detected in applied scripts. "
            "Tables can exist but remain empty until ingest/insert commands run."
        )
        print(f"[warning] {warning}")
        if args.fail_on_warning:
            raise RuntimeError(warning)

    if not sql_seeded:
        print_dimension_counts(connection, query_service_uri, kusto_token)

    print("Model deployment completed")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except SqlConnectionValidationError as exc:
        print(f"SQL connection metadata invalid: {exc}", file=sys.stderr)
        raise SystemExit(1)
