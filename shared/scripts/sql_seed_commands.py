#!/usr/bin/env python3
"""Generate idempotent SQL commands for baseline seed operations."""

from __future__ import annotations

from typing import Iterable


def build_upsert_node_commands(nodes: Iterable[dict]) -> list[tuple[str, tuple]]:
    commands: list[tuple[str, tuple]] = []
    sql = """
    MERGE dbo.Isa95BaselineNodes AS target
    USING (VALUES (?, ?, ?, ?, ?)) AS source (nodeId, nodeType, parentNodeId, displayName, user_id)
      ON target.nodeId = source.nodeId
    WHEN MATCHED THEN
      UPDATE SET
        nodeType = source.nodeType,
        parentNodeId = source.parentNodeId,
        displayName = source.displayName,
        user_id = source.user_id,
        version = target.version + 1,
        status = 'Active'
    WHEN NOT MATCHED THEN
      INSERT (id, nodeId, nodeType, parentNodeId, displayName, status, version, user_id)
      VALUES (NEWID(), source.nodeId, source.nodeType, source.parentNodeId, source.displayName, 'Active', 1, source.user_id);
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
                    "seed-runner",
                ),
            )
        )
    return commands
