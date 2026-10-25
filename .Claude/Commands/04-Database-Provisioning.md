---
description: Run Stage 3 Database & Schema Provisioning to generate relational schemas, migrations, and Dapper/EF Core stores for VSAM and batch operational tables.
---

## Step 1: Initialize Database Target Directory
Create the target folders:
- `dotnet/db/migrations/`
- `dotnet/src/CardDemo.Data/Stores/`

## Step 2: Invoke Schema-Store-Provisioner-Agent
Execute the `Schema-Store-Provisioner-Agent` taking the copybook facts and IR datasets as input:
- Account Master (`CVACT01Y.cpy` -> `ACCTDATA`)
- Card Cross-Reference (`CVACT03Y.cpy` -> `CARDXREF`)
- Category Balances (`CVTRA01Y.cpy` -> `TCATBALF`)
- Disclosure Group Rates (`CVTRA02Y.cpy` -> `DISCGRP`)
- Output Transaction File (`CVTRA05Y.cpy` -> `SYSTRAN`)

## Step 3: Generate SQL Migrations
Generate SQL scripts under `dotnet/db/migrations/`:
- `001_Create_CardDemo_VSAM_Tables.sql` (Tables, Primary Keys, Composite Indexes)
- `002_Create_Batch_Operational_Tables.sql` (`BATCH_STEP_CHECKPOINT` and `MMP_RUN_LEDGER`)
- `003_Sqlite_Local_Schema.sql` (Lightweight script for unit/contract tests)

## Step 4: Generate C# Keyed Store Implementations
Generate C# repository implementations under `dotnet/src/CardDemo.Data/Stores/`:
- `IKeyedStore<TKey, TRecord>.cs` interface
- `DapperKeyedStore.cs` (high-performance bulk-query store)
- `SqliteKeyedStore.cs` (for automated testing without external DB dependency)
- `MemoryKeyedStore.cs` (for sub-millisecond unit tests)

## Step 5: Verification
Ensure table structures preserve precision, scale, and composite key definitions from the copybooks.
