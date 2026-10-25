-- 002_Create_Batch_Operational_Tables.sql
CREATE TABLE IF NOT EXISTS BATCH_STEP_CHECKPOINT (
    JobId TEXT NOT NULL,
    StepName TEXT NOT NULL,
    Status TEXT NOT NULL,            -- 'Pending', 'Running', 'Completed', 'Failed'
    LastProcessedKey TEXT NULL,     -- e.g. '000000450000_01_0005'
    ProcessedCount BIGINT NOT NULL DEFAULT 0,
    LastCheckpointUtc TEXT NOT NULL,
    ErrorMessage TEXT NULL,
    PRIMARY KEY (JobId, StepName)
);

CREATE TABLE IF NOT EXISTS MMP_RUN_LEDGER (
    RunId TEXT PRIMARY KEY,
    Workload TEXT NOT NULL,
    BusinessDate TEXT NOT NULL,       -- 'YYYY-MM-DD'
    Status TEXT NOT NULL,            -- 'Running', 'Completed', 'Failed'
    StartedAt TEXT NOT NULL,
    CompletedAt TEXT NULL
);
