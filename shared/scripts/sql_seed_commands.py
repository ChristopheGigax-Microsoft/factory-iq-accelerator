#!/usr/bin/env python3
"""Generate idempotent SQL commands for baseline seed operations."""

from __future__ import annotations

from typing import Iterable


def build_upsert_node_commands(nodes: Iterable[dict]) -> list[tuple[str, tuple]]:
    commands: list[tuple[str, tuple]] = []
    sql = """
    MERGE dbo.isa95_baseline_node AS target
    USING (VALUES (?, ?, ?, ?)) AS source (node_id, node_type, parent_node_id, display_name)
      ON target.node_id = source.node_id
    WHEN MATCHED THEN
      UPDATE SET
        node_type = source.node_type,
        parent_node_id = source.parent_node_id,
        display_name = source.display_name,
        version = target.version + 1,
        updated_at = SYSUTCDATETIME(),
        updated_by = 'seed-runner'
    WHEN NOT MATCHED THEN
      INSERT (node_id, node_type, parent_node_id, display_name, status, created_by, updated_by)
      VALUES (source.node_id, source.node_type, source.parent_node_id, source.display_name, 'Active', 'seed-runner', 'seed-runner');
    """
    for node in nodes:
        commands.append(
            (
                sql,
                (
                    node["nodeId"],
                    node["nodeType"],
                    node.get("parentNodeId"),
                    node["displayName"],
                ),
            )
        )
    return commands
