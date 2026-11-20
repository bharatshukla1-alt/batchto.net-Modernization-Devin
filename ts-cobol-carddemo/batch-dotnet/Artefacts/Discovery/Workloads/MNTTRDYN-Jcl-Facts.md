# MNTTRDYN - JCL facts

Source: `app/app-transaction-type-db2/jcl/MNTTRDYN.jcl` (read-only estate).

| Step | Line | Program/Proc | PARM | DD | DSN | DISP | DCB |
|---|---|---|---|---|---|---|---|
| STEP1 | L23 | PGM=IKJEFT01 (DSN RUN PROGRAM(CBTRNDYN) L33) |  | STEPLIB (L24) | OEM.DB2.DAZ1.SDSNEXIT | SHR |  |
| | | | | DBRMLIB (L27) | AWS.M2.CARDDEMO.DBRMLIB | SHR |  |
| | | | | SYSTSPRT (L28) | SYSOUT |  |  |
| | | | | DYNSQL (L29) | DYNSQL | SHR |  |
| | | | | INPFILE (L30) | INPFILE | SHR |  |
| | | | | SYSTSIN (L31) | instream |  |  |
| | | | | SYSPRINT (L36) | SYSOUT |  |  |
| | | | | SYSOUT (L37) | SYSOUT |  |  |
| | | | | SYSUDUMP (L38) | SYSOUT |  |  |
