#!/usr/bin/env python3
"""Deploy ISA-95 baseline data to SQL only."""

from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys

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
    "generatedAt",
    "schemaVersion",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Deploy ISA-95 model assets to SQL")
    parser.add_argument("--connection", required=True)
    parser.add_argument("--core-dir", required=True)
    parser.add_argument("--extensions-dir", required=True)
    parser.add_argument("--hierarchy-config", required=True)
    parser.add_argument("--fail-on-warning", action="store_true")
    return parser.parse_args()


def read_connection(path: pathlib.Path) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    missing = [k for k in REQUIRED_CONNECTION_FIELDS if k not in data or not data[k]]
    if missing:
        raise ValueError(f"connection.json missing required fields: {', '.join(missing)}")
    return data


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


def run_sql_baseline_seed(connection: dict, hierarchy_config: pathlib.Path) -> None:
    target = parse_sql_target(connection)
    if target is None:
        raise SqlConnectionValidationError(
            "connection.json missing required SQL metadata: sqlBaseline"
        )

    if pyodbc is None:
        raise RuntimeError("pyodbc is required when sqlBaseline metadata is present")

    hierarchy = yaml.safe_load(hierarchy_config.read_text(encoding="utf-8"))
    nodes = map_hierarchy_config_to_nodes(hierarchy)
    commands = build_upsert_node_commands(nodes)

    conn_str = build_pyodbc_connection_string(target)
    with pyodbc.connect(conn_str) as conn:  # type: ignore[union-attr]
        cursor = conn.cursor()
        for statement, params in commands:
            cursor.execute(statement, params)
        conn.commit()
        print(f"[seed] Upserted {len(commands)} nodes into Isa95BaselineNodes")


def main() -> int:
    args = parse_args()
    connection = read_connection(pathlib.Path(args.connection))

    print("[stage] validate hierarchy config")
    run_hierarchy_seed(pathlib.Path(args.hierarchy_config))

    print("[stage] seed SQL baseline")
    run_sql_baseline_seed(connection, pathlib.Path(args.hierarchy_config))

    print("Model deployment completed (SQL only)")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except SqlConnectionValidationError as exc:
        print(f"SQL connection metadata invalid: {exc}", file=sys.stderr)
        raise SystemExit(1)
