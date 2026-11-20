# READCARD - JCL facts

Source: `app/jcl/READCARD.jcl` (read-only estate).

| Step | Line | Program/Proc | PARM | DD | DSN | DISP | DCB |
|---|---|---|---|---|---|---|---|
| STEP05 | L22 | PGM=CBACT02C |  | STEPLIB (L23) | AWS.M2.CARDDEMO.LOADLIB | SHR |  |
| | | | | CARDFILE (L25) | AWS.M2.CARDDEMO.CARDDATA.VSAM.KSDS | SHR |  |
| | | | | SYSOUT (L27) | SYSOUT |  |  |
| | | | | SYSPRINT (L28) | SYSOUT |  |  |
