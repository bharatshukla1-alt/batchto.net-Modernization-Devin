# POSTTRAN - JCL facts

Source: `app/jcl/POSTTRAN.jcl` (read-only estate).

| Step | Line | Program/Proc | PARM | DD | DSN | DISP | DCB |
|---|---|---|---|---|---|---|---|
| STEP15 | L23 | PGM=CBTRN02C |  | STEPLIB (L24) | AWS.M2.CARDDEMO.LOADLIB | SHR |  |
| | | | | SYSPRINT (L26) | SYSOUT |  |  |
| | | | | SYSOUT (L27) | SYSOUT |  |  |
| | | | | TRANFILE (L28) | AWS.M2.CARDDEMO.TRANSACT.VSAM.KSDS | SHR |  |
| | | | | DALYTRAN (L30) | AWS.M2.CARDDEMO.DALYTRAN.PS | SHR |  |
| | | | | XREFFILE (L32) | AWS.M2.CARDDEMO.CARDXREF.VSAM.KSDS | SHR |  |
| | | | | DALYREJS (L34) | AWS.M2.CARDDEMO.DALYREJS(+1) | (NEW,CATLG,DELETE) | (RECFM=F,LRECL=430,BLKSIZE=0) |
| | | | | ACCTFILE (L39) | AWS.M2.CARDDEMO.ACCTDATA.VSAM.KSDS | SHR |  |
| | | | | TCATBALF (L41) | AWS.M2.CARDDEMO.TCATBALF.VSAM.KSDS | SHR |  |
