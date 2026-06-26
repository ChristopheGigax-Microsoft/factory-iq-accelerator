#!/usr/bin/env python3
"""Map hierarchy YAML payloads to baseline entities."""

from __future__ import annotations

from typing import Any


def map_hierarchy_config_to_nodes(config: dict[str, Any]) -> list[dict[str, Any]]:
    nodes: list[dict[str, Any]] = []

    enterprise = config["enterprise"]
    nodes.append(
        {
            "nodeId": enterprise["id"],
            "nodeType": "Enterprise",
            "parentNodeId": None,
            "displayName": enterprise["name"],
        }
    )

    def append_items(items: list[dict[str, Any]], node_type: str, parent_key: str) -> None:
        for item in items:
            nodes.append(
                {
                    "nodeId": item["id"],
                    "nodeType": node_type,
                    "parentNodeId": item[parent_key],
                    "displayName": item["name"],
                }
            )

    append_items(config.get("sites", []), "Site", "enterpriseId")
    append_items(config.get("areas", []), "Area", "siteId")
    append_items(config.get("workCenters", []), "WorkCenter", "areaId")
    append_items(config.get("workUnits", []), "WorkUnit", "workCenterId")

    return nodes
