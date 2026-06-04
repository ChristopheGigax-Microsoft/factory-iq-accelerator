# Extensions Ordering Convention

Customer extensions are loaded after core schema scripts.

## File Naming

Use numeric prefixes to control deterministic order:

- `30_*.kql` for extension tables/functions
- `40_*.kql` for extension update policies

## Rules

- Do not modify files under `shared/isa95-model/core/` for customer-specific behavior.
- Keep extension scripts idempotent (`.create-merge`, `.create-or-alter`).
- Use only entities required by your plant use case.
