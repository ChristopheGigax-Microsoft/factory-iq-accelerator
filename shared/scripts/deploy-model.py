#!/usr/bin/env python3
"""Deploy ISA-95 core and extension KQL scripts using a connection contract."""

from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys
from typing import Iterable

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


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Deploy ISA-95 model assets")
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


def iter_kql_files(folder: pathlib.Path) -> Iterable[pathlib.Path]:
    if not folder.exists():
        return []
    return sorted(folder.glob("*.kql"))


def run_kql_file(file_path: pathlib.Path, connection: dict) -> None:
    # Placeholder command for model application; replace with SDK-backed execution.
    print(f"[apply] {file_path.name} -> {connection['kqlDatabase']}")


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


def main() -> int:
    args = parse_args()
    connection = read_connection(pathlib.Path(args.connection))

    print("[stage] apply core scripts")
    for kql_file in iter_kql_files(pathlib.Path(args.core_dir)):
        run_kql_file(kql_file, connection)

    # Extensions are intentionally applied after core scripts.
    print("[stage] apply extension scripts")
    for kql_file in iter_kql_files(pathlib.Path(args.extensions_dir)):
        run_kql_file(kql_file, connection)

    print("[stage] seed hierarchy")
    run_hierarchy_seed(pathlib.Path(args.hierarchy_config))
    print("Model deployment completed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
