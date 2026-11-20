# MNTTRDB2 - JCL facts

Source: `app/app-transaction-type-db2/jcl/MNTTRDB2.jcl` (read-only estate).

| Step | Line | Program/Proc | PARM | DD | DSN | DISP | DCB |
|---|---|---|---|---|---|---|---|
| STEP1 | L21 | PGM=IKJEFT01 (DSN RUN PROGRAM(COBTUPDT) L30) |  | STEPLIB (L22) | OEM.DB2.DAZ1.SDSNEXIT | SHR |  |
| | | | | DBRMLIB (L25) | AWS.M2.CARDDEMO.DBRMLIB | SHR |  |
| | | | | SYSTSPRT (L26) | SYSOUT |  |  |
| | | | | INPFILE (L27) | INPFILE | SHR |  |
| | | | | SYSTSIN (L28) | instream |  |  |
