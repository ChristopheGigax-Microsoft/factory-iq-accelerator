#!/usr/bin/env python3
"""Shared baseline repository contracts and domain exceptions."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol


@dataclass(frozen=True)
class BaselineNode:
    node_id: str
    node_type: str
    parent_node_id: str | None
    display_name: str
    version: int


class BaselineRepositoryError(Exception):
    """Base repository error."""


class BaselineNotFoundError(BaselineRepositoryError):
    """Raised when a baseline entity cannot be found."""


class BaselineConflictError(BaselineRepositoryError):
    """Raised on optimistic concurrency conflicts."""


class BaselineValidationError(BaselineRepositoryError):
    """Raised when invalid hierarchy relationships are detected."""


class BaselineRepository(Protocol):
    def list_hierarchy(self, include_inactive: bool = False) -> list[BaselineNode]:
        ...

    def upsert_node(self, node: BaselineNode, expected_version: int | None = None) -> BaselineNode:
        ...
