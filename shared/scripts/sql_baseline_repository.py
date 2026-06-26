#!/usr/bin/env python3
"""SQL-backed baseline repository implementation."""

from __future__ import annotations

from typing import Any

import pyodbc

from baseline_repository import (
    BaselineConflictError,
    BaselineNode,
    BaselineRepository,
    BaselineValidationError,
)
from sql_connection import SqlTarget, build_pyodbc_connection_string


class SqlBaselineRepository(BaselineRepository):
    def __init__(self, target: SqlTarget):
        self._target = target

    def _connect(self) -> pyodbc.Connection:
        return pyodbc.connect(build_pyodbc_connection_string(self._target))

    def list_hierarchy(self, include_inactive: bool = False) -> list[BaselineNode]:
        query = """
            SELECT node_id, node_type, parent_node_id, display_name, version
            FROM dbo.isa95_baseline_node
        """
        if not include_inactive:
            query += " WHERE status = 'Active'"
        query += " ORDER BY node_type, display_name"

        with self._connect() as connection:
            cursor = connection.cursor()
            rows = cursor.execute(query).fetchall()

        return [
            BaselineNode(
                node_id=str(row.node_id),
                node_type=str(row.node_type),
                parent_node_id=str(row.parent_node_id) if row.parent_node_id else None,
                display_name=str(row.display_name),
                version=int(row.version),
            )
            for row in rows
        ]

    def upsert_node(self, node: BaselineNode, expected_version: int | None = None) -> BaselineNode:
        with self._connect() as connection:
            cursor = connection.cursor()
            existing = cursor.execute(
                "SELECT version FROM dbo.isa95_baseline_node WHERE node_id = ?",
                node.node_id,
            ).fetchone()

            if existing:
                current_version = int(existing.version)
                if expected_version is not None and current_version != expected_version:
                    raise BaselineConflictError(
                        f"Version mismatch for {node.node_id}: expected {expected_version}, found {current_version}"
                    )
                next_version = current_version + 1
                cursor.execute(
                    """
                    UPDATE dbo.isa95_baseline_node
                    SET node_type=?, parent_node_id=?, display_name=?, version=?, updated_at=SYSUTCDATETIME(), updated_by=?
                    WHERE node_id=?
                    """,
                    node.node_type,
                    node.parent_node_id,
                    node.display_name,
                    next_version,
                    "fabric-app",
                    node.node_id,
                )
                connection.commit()
                return BaselineNode(
                    node_id=node.node_id,
                    node_type=node.node_type,
                    parent_node_id=node.parent_node_id,
                    display_name=node.display_name,
                    version=next_version,
                )

            if node.node_type != "Enterprise" and not node.parent_node_id:
                raise BaselineValidationError("Non-enterprise nodes require parent_node_id")

            cursor.execute(
                """
                INSERT INTO dbo.isa95_baseline_node
                (node_id, node_type, parent_node_id, display_name, status, created_by, updated_by)
                VALUES (?, ?, ?, ?, 'Active', ?, ?)
                """,
                node.node_id,
                node.node_type,
                node.parent_node_id,
                node.display_name,
                "fabric-app",
                "fabric-app",
            )
            connection.commit()
            return BaselineNode(
                node_id=node.node_id,
                node_type=node.node_type,
                parent_node_id=node.parent_node_id,
                display_name=node.display_name,
                version=1,
            )


def map_db_error(err: Exception) -> Exception:
    if isinstance(err, pyodbc.IntegrityError):
        return BaselineValidationError(str(err))
    if isinstance(err, pyodbc.ProgrammingError):
        return BaselineValidationError(str(err))
    return err
