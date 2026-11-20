-- Chunk restart checkpoints, committed in the same transaction as the KSDS rows they describe.
CREATE TABLE IF NOT EXISTS batch_checkpoint (
    checkpoint_key VARCHAR(64) PRIMARY KEY,
    payload        TEXT        NOT NULL
);
