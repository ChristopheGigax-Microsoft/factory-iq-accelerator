CREATE UNIQUE INDEX IF NOT EXISTS IX_baseline_node_unique
    ON dbo.isa95_baseline_node(node_type, node_id);

ALTER TABLE dbo.isa95_operational_record
    ADD CONSTRAINT CK_operational_effective_range
    CHECK (effective_to IS NULL OR effective_to > effective_from);

CREATE INDEX IF NOT EXISTS IX_baseline_parent
    ON dbo.isa95_baseline_node(parent_node_id);
