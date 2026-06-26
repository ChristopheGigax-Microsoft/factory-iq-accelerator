CREATE TABLE IF NOT EXISTS dbo.isa95_baseline_node (
    node_id NVARCHAR(128) NOT NULL PRIMARY KEY,
    node_type NVARCHAR(32) NOT NULL,
    parent_node_id NVARCHAR(128) NULL,
    display_name NVARCHAR(256) NOT NULL,
    status NVARCHAR(16) NOT NULL DEFAULT 'Active',
    version INT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by NVARCHAR(256) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_by NVARCHAR(256) NOT NULL,
    CONSTRAINT FK_baseline_parent FOREIGN KEY (parent_node_id)
        REFERENCES dbo.isa95_baseline_node(node_id)
);

CREATE TABLE IF NOT EXISTS dbo.isa95_operational_record (
    record_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    node_id NVARCHAR(128) NOT NULL,
    record_type NVARCHAR(32) NOT NULL,
    payload NVARCHAR(MAX) NOT NULL,
    effective_from DATETIME2 NOT NULL,
    effective_to DATETIME2 NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by NVARCHAR(256) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_by NVARCHAR(256) NOT NULL,
    CONSTRAINT FK_operational_node FOREIGN KEY (node_id)
        REFERENCES dbo.isa95_baseline_node(node_id)
);
