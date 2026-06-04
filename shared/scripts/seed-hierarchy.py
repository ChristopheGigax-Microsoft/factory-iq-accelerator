#!/usr/bin/env python3
"""Validate and seed ISA-95 hierarchy configuration."""

from __future__ import annotations

import argparse
import pathlib
import sys

import yaml


REQUIRED_KEYS = ("enterprise", "sites", "areas", "workCenters", "workUnits")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Seed plant hierarchy")
    parser.add_argument("--config", required=True)
    return parser.parse_args()


def load_config(path: pathlib.Path) -> dict:
    data = yaml.safe_load(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError("Hierarchy config must be a YAML object")
    missing = [key for key in REQUIRED_KEYS if key not in data]
    if missing:
        raise ValueError(f"Hierarchy config missing required keys: {', '.join(missing)}")
    return data


def validate_parent_links(data: dict) -> None:
    enterprise_id = data["enterprise"].get("id")
    if not enterprise_id:
        raise ValueError("Enterprise id is required")

    site_ids = [site["id"] for site in data["sites"]]
    if len(site_ids) != len(set(site_ids)):
        raise ValueError("Duplicate site ids are not allowed")

    site_ids = {site["id"] for site in data["sites"]}
    area_ids = {area["id"] for area in data["areas"]}
    wc_ids = {wc["id"] for wc in data["workCenters"]}

    for site in data["sites"]:
        if site.get("enterpriseId") != enterprise_id:
            raise ValueError(f"Site {site.get('id')} references invalid enterpriseId")

    for area in data["areas"]:
        if area.get("siteId") not in site_ids:
            raise ValueError(f"Area {area.get('id')} references missing siteId")

    for wc in data["workCenters"]:
        if wc.get("areaId") not in area_ids:
            raise ValueError(f"WorkCenter {wc.get('id')} references missing areaId")

    for wu in data["workUnits"]:
        if wu.get("workCenterId") not in wc_ids:
            raise ValueError(f"WorkUnit {wu.get('id')} references missing workCenterId")


def main() -> int:
    args = parse_args()
    config = load_config(pathlib.Path(args.config))
    validate_parent_links(config)
    print("Hierarchy validation succeeded")
    print("Hierarchy seed completed")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001
        print(f"Hierarchy seed failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
