CREATE TABLE IF NOT EXISTS dbo.baseline_seed_run (
    seed_run_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    started_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    completed_at DATETIME2 NULL,
    status NVARCHAR(16) NOT NULL,
    seed_source NVARCHAR(512) NOT NULL,
    counts NVARCHAR(MAX) NULL,
    error_message NVARCHAR(MAX) NULL
);

CREATE TABLE IF NOT EXISTS dbo.baseline_change_record (
    change_id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    entity_type NVARCHAR(32) NOT NULL,
    entity_id NVARCHAR(128) NOT NULL,
    action NVARCHAR(32) NOT NULL,
    actor NVARCHAR(256) NOT NULL,
    changed_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    change_summary NVARCHAR(MAX) NULL,
    seed_run_id UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_change_seed_run FOREIGN KEY (seed_run_id)
        REFERENCES dbo.baseline_seed_run(seed_run_id)
);
