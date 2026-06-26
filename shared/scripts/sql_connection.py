#!/usr/bin/env python3
"""SQL connection metadata parsing and validation helpers."""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class SqlTarget:
    server: str
    database: str
    driver: str
    authentication: str


class SqlConnectionValidationError(ValueError):
    """Raised when SQL metadata in connection.json is incomplete or invalid."""


def parse_sql_target(connection: dict) -> SqlTarget | None:
    sql = connection.get("sqlBaseline")
    if not sql:
        return None

    server = str(sql.get("server", "")).strip()
    database = str(sql.get("database", "")).strip()
    driver = str(sql.get("driver", "ODBC Driver 18 for SQL Server")).strip()
    authentication = str(sql.get("authentication", "ActiveDirectoryMsi")).strip()

    missing = []
    if not server:
        missing.append("sqlBaseline.server")
    if not database:
        missing.append("sqlBaseline.database")
    if missing:
        raise SqlConnectionValidationError(
            f"connection.json missing SQL fields: {', '.join(missing)}"
        )

    return SqlTarget(
        server=server,
        database=database,
        driver=driver,
        authentication=authentication,
    )


def build_pyodbc_connection_string(target: SqlTarget) -> str:
    return (
        f"Driver={{{target.driver}}};"
        f"Server=tcp:{target.server},1433;"
        f"Database={target.database};"
        f"Encrypt=yes;TrustServerCertificate=no;"
        f"Authentication={target.authentication};"
    )
