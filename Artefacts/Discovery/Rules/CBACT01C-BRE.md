# Business Rules: CBACT01C

`json
{
  "business_summary": {
    "program_name": "CBACT01C",
    "program_description": "Batch COBOL program belonging to the CardDemo application. Reads an indexed account file (KSDS VSAM) sequentially and writes account data to three output files: a flat sequential output file, an array-structured sequential file, and a variable-length record file.",
    "business_objective": "Extract all account records from the account master file and produce multiple output representations of account data for downstream consumption.",
    "business_outcome": "All account records from ACCTFILE are read and written to OUTFILE (flat), ARRYFILE (array), and VBRCFILE (variable-length) outputs. The account reissue date is reformatted during extraction.",
    "confidence_level": "MEDIUM",
    "evidence": [
      "Program header comment: 'FUNCTION: READ THE ACCOUNT FILE AND WRITE INTO FILES.' (CBACT01C.cbl line 000500)",
      "Program header comment: 'Application: CardDemo' (CBACT01C.cbl line 000300)",
      "Copybook CVACT01Y comment: 'Data-structure for account entity (RECLN 300)'",
      "DISPLAY 'START OF EXECUTION OF PROGRAM CBACT01C' (CBACT01C.cbl PROCEDURE DIVISION)",
      "inventory metadata: execution_model=BATCH, entry_program=true, file_count=4"
    ]
  },
  "business_process_summary": [
    {
      "step_number": 1,
      "process_name": "Open Files",
      "description": "Open ACCTFILE for INPUT and OUTFILE, ARRYFILE, VBRCFILE for OUTPUT. Abend on any open failure.",
      "source_reference": "CBACT01C.cbl paragraphs 0000-ACCTFILE-OPEN, 2000-OUTFILE-OPEN, 3000-ARRFILE-OPEN, 4000-VBRFILE-OPEN"
    },
    {
      "step_number": 2,
      "process_name": "Sequential Account Read Loop",
      "description": "Repeatedly read the next account record from the indexed ACCTFILE until end-of-file is reached.",
      "source_reference": "CBACT01C.cbl PROCEDURE DIVISION: PERFORM UNTIL END-OF-FILE = 'Y'"
    },
    {
      "step_number": 3,
      "process_name": "Display Account Record",
      "description": "Display all account fields to system output for each record successfully read.",
      "source_reference": "CBACT01C.cbl paragraph 1100-DISPLAY-ACCT-RECORD"
    },
    {
      "step_number": 4,
      "process_name": "Populate and Write Flat Account Output Record",
      "description": "Map account fields to the flat output record structure, apply reissue date format conversion and default debit logic, then write to OUTFILE.",
      "source_reference": "CBACT01C.cbl paragraphs 1300-POPUL-ACCT-RECORD, 1350-WRITE-ACCT-RECORD"
    },
    {
      "step_number": 5,
      "process_name": "Populate and Write Array Account Record",
      "description": "Map account ID and balance data into a 5-occurrence array structure and write to ARRYFILE.",
      "source_reference": "CBACT01C.cbl paragraphs 1400-POPUL-ARRAY-RECORD, 1450-WRITE-ARRY-RECORD"
    },
    {
      "step_number": 6,
      "process_name": "Populate and Write Variable-Length Records",
      "description": "Write two variable-length records to VBRCFILE: a 12-byte record containing account ID and active status, and a 39-byte record containing account ID, current balance, credit limit, and reissue year.",
      "source_reference": "CBACT01C.cbl paragraphs 1500-POPUL-VBRC-RECORD, 1550-WRITE-VB1-RECORD, 1575-WRITE-VB2-RECORD"
    },
    {
      "step_number": 7,
      "process_name": "Close Account File",
      "description": "Close ACCTFILE on completion of all record processing. Abend on close failure.",
      "source_reference": "CBACT01C.cbl paragraph 9000-ACCTFILE-CLOSE"
    }
  ],
  "business_entities": [
    {
      "entity_name": "Account",
      "entity_type": "Master Record",
      "evidence": "CVACT01Y copybook: '01 ACCOUNT-RECORD' with fields ACCT-ID, ACCT-ACTIVE-STATUS, ACCT-CURR-BAL, ACCT-CREDIT-LIMIT, ACCT-CASH-CREDIT-LIMIT, ACCT-OPEN-DATE, ACCT-EXPIRAION-DATE, ACCT-REISSUE-DATE, ACCT-CURR-CYC-CREDIT, ACCT-CURR-CYC-DEBIT, ACCT-ADDR-ZIP, ACCT-GROUP-ID"
    }
  ],
  "business_data_elements": [
    {
      "element_name": "ACCT-ID",
      "description": "Account identifier. 11-digit numeric key of the account record.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-ID PIC 9(11)"
    },
    {
      "element_name": "ACCT-ACTIVE-STATUS",
      "description": "Active status flag for the account. 1-character alphanumeric.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-ACTIVE-STATUS PIC X(01)"
    },
    {
      "element_name": "ACCT-CURR-BAL",
      "description": "Current balance of the account. Signed 10-digit numeric with 2 decimal places.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-CURR-BAL PIC S9(10)V99"
    },
    {
      "element_name": "ACCT-CREDIT-LIMIT",
      "description": "Credit limit assigned to the account. Signed 10-digit numeric with 2 decimal places.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-CREDIT-LIMIT PIC S9(10)V99"
    },
    {
      "element_name": "ACCT-CASH-CREDIT-LIMIT",
      "description": "Cash credit limit assigned to the account. Signed 10-digit numeric with 2 decimal places.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-CASH-CREDIT-LIMIT PIC S9(10)V99"
    },
    {
      "element_name": "ACCT-OPEN-DATE",
      "description": "Date the account was opened. 10-character alphanumeric.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-OPEN-DATE PIC X(10)"
    },
    {
      "element_name": "ACCT-EXPIRAION-DATE",
      "description": "Expiration date of the account. 10-character alphanumeric.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-EXPIRAION-DATE PIC X(10)"
    },
    {
      "element_name": "ACCT-REISSUE-DATE",
      "description": "Reissue date of the account. 10-character alphanumeric. Subject to date format conversion during extraction.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-REISSUE-DATE PIC X(10)"
    },
    {
      "element_name": "ACCT-CURR-CYC-CREDIT",
      "description": "Current cycle credit amount for the account. Signed 10-digit numeric with 2 decimal places.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-CURR-CYC-CREDIT PIC S9(10)V99"
    },
    {
      "element_name": "ACCT-CURR-CYC-DEBIT",
      "description": "Current cycle debit amount for the account. Signed 10-digit numeric with 2 decimal places. Defaults to 2525.00 when zero.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-CURR-CYC-DEBIT PIC S9(10)V99"
    },
    {
      "element_name": "ACCT-GROUP-ID",
      "description": "Group identifier associated with the account. 10-character alphanumeric.",
      "source_reference": "CVACT01Y.cpy: 05 ACCT-GROUP-ID PIC X(10)"
    }
  ],
  "business_rules": [
    {
      "rule_id": "BR001",
      "rule_name": "Account File Sequential Read Until EOF",
      "rule_category": "Decision Rule",
      "business_description": "The program reads all account records from the indexed account file sequentially, continuing until the end-of-file condition is detected. Each record read successfully triggers downstream processing.",
      "condition": "END-OF-FILE = 'N'",
      "action": "Perform 1000-ACCTFILE-GET-NEXT to read next account record and process it",
      "outcome": "All account records are processed in sequence; loop terminates when END-OF-FILE = 'Y'",
      "acceptance_criteria": {
        "given": "The account file (ACCTFILE) contains one or more account records",
        "when": "The program enters the main processing loop",
        "then": "Each account record is read and processed in sequence until end-of-file is reached"
      },
      "decision_table": [
        {
          "condition": "END-OF-FILE = 'N'",
          "outcome": "Read next account record and process"
        },
        {
          "condition": "END-OF-FILE = 'Y'",
          "outcome": "Exit processing loop"
        }
      ],
      "source_reference": "CBACT01C.cbl PROCEDURE DIVISION main loop",
      "source_snippet": "PERFORM UNTIL END-OF-FILE = 'Y'\n    IF  END-OF-FILE = 'N'\n        PERFORM 1000-ACCTFILE-GET-NEXT\n        IF  END-OF-FILE = 'N'\n            DISPLAY ACCOUNT-RECORD\n        END-IF\n    END-IF\nEND-PERFORM.",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR002",
      "rule_name": "Account Read Status Routing",
      "rule_category": "Exception Rule",
      "business_description": "After each account record read, the file status is evaluated. A status of '00' indicates a successful read; status '10' signals end-of-file and terminates processing; any other status is treated as a read error and causes an abnormal program termination.",
      "condition": "ACCTFILE-STATUS value after READ",
      "action": "Route to success, EOF termination, or abend based on file status value",
      "outcome": "Normal continuation on '00'; END-OF-FILE set to 'Y' on '10'; program abends on any other status",
      "acceptance_criteria": {
        "given": "A READ operation has been performed against ACCTFILE-FILE",
        "when": "The file status code is evaluated",
        "then": "Status '00' continues processing, status '10' sets END-OF-FILE='Y' to terminate, any other status displays an error and abends the program"
      },
      "decision_table": [
        {
          "condition": "ACCTFILE-STATUS = '00'",
          "outcome": "Set APPL-RESULT = 0 (APPL-AOK); continue record processing"
        },
        {
          "condition": "ACCTFILE-STATUS = '10'",
          "outcome": "Set APPL-RESULT = 16 (APPL-EOF); set END-OF-FILE = 'Y'"
        },
        {
          "condition": "ACCTFILE-STATUS not '00' and not '10'",
          "outcome": "Set APPL-RESULT = 12; display error, perform 9999-ABEND-PROGRAM"
        }
      ],
      "source_reference": "CBACT01C.cbl paragraph 1000-ACCTFILE-GET-NEXT",
      "source_snippet": "IF  ACCTFILE-STATUS = '00'\n    MOVE 0 TO APPL-RESULT\n    ...\nELSE\n    IF  ACCTFILE-STATUS = '10'\n        MOVE 16 TO APPL-RESULT\n    ELSE\n        MOVE 12 TO APPL-RESULT\n    END-IF\nEND-IF\nIF  APPL-AOK\n    CONTINUE\nELSE\n    IF  APPL-EOF\n        MOVE 'Y' TO END-OF-FILE\n    ELSE\n        DISPLAY 'ERROR READING ACCOUNT FILE'\n        MOVE ACCTFILE-STATUS TO IO-STATUS\n        PERFORM 9910-DISPLAY-IO-STATUS\n        PERFORM 9999-ABEND-PROGRAM\n    END-IF\nEND-IF",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR003",
      "rule_name": "Default Current Cycle Debit Value",
      "rule_category": "Calculation Rule",
      "business_description": "When the account's current cycle debit amount is zero, the output record is assigned a default current cycle debit value of 2525.00. This ensures the output file never contains a zero debit amount for the current cycle field.",
      "condition": "ACCT-CURR-CYC-DEBIT EQUAL TO ZERO",
      "action": "MOVE 2525.00 TO OUT-ACCT-CURR-CYC-DEBIT",
      "outcome": "Output current cycle debit field is set to 2525.00 when source value is zero",
      "acceptance_criteria": {
        "given": "An account record has been read from ACCTFILE",
        "when": "The ACCT-CURR-CYC-DEBIT field is equal to zero",
        "then": "The output field OUT-ACCT-CURR-CYC-DEBIT is set to 2525.00"
      },
      "decision_table": [
        {
          "condition": "ACCT-CURR-CYC-DEBIT = ZERO",
          "outcome": "Set OUT-ACCT-CURR-CYC-DEBIT = 2525.00"
        },
        {
          "condition": "ACCT-CURR-CYC-DEBIT NOT EQUAL TO ZERO",
          "outcome": "OUT-ACCT-CURR-CYC-DEBIT retains source value (no explicit MOVE; field not populated from source in this paragraph)"
        }
      ],
      "source_reference": "CBACT01C.cbl paragraph 1300-POPUL-ACCT-RECORD",
      "source_snippet": "IF  ACCT-CURR-CYC-DEBIT EQUAL TO ZERO\n    MOVE 2525.00         TO   OUT-ACCT-CURR-CYC-DEBIT\nEND-IF.",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR004",
      "rule_name": "Account Reissue Date Format Conversion",
      "rule_category": "Calculation Rule",
      "business_description": "The account reissue date, stored in YYYY-MM-DD format, is converted to YYYYMMDD format via a call to the COBDATFT assembler routine before being written to the output file. Input type '2' (YYYY-MM-DD) and output type '2' (YYYYMMDD) are specified.",
      "condition": "For every account record processed",
      "action": "Move ACCT-REISSUE-DATE to CODATECN-INP-DATE with TYPE='2' and OUTTYPE='2'; call COBDATFT; move CODATECN-0UT-DATE to OUT-ACCT-REISSUE-DATE",
      "outcome": "OUT-ACCT-REISSUE-DATE contains the reissue date in YYYYMMDD format",
      "acceptance_criteria": {
        "given": "An account record with a reissue date in YYYY-MM-DD format has been read",
        "when": "The flat output record is being populated",
        "then": "The reissue date is reformatted to YYYYMMDD via COBDATFT and stored in OUT-ACCT-REISSUE-DATE"
      },
      "decision_table": [],
      "source_reference": "CBACT01C.cbl paragraph 1300-POPUL-ACCT-RECORD",
      "source_snippet": "MOVE   ACCT-REISSUE-DATE       TO   CODATECN-INP-DATE\n                                    WS-REISSUE-DATE.\nMOVE   '2'                     TO   CODATECN-TYPE.\nMOVE   '2'                     TO   CODATECN-OUTTYPE.\n*CALL ASSEMBLER PROGRAM FOR DATE FORMATTING\nCALL 'COBDATFT'       USING CODATECN-REC.\nMOVE   CODATECN-0UT-DATE       TO   OUT-ACCT-REISSUE-DATE.",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR005",
      "rule_name": "Variable-Length Record Type 1 - Account Status Record (12 bytes)",
      "rule_category": "Routing Rule",
      "business_description": "A 12-byte variable-length record containing Account ID (11 digits) and Account Active Status (1 character) is written to the variable-length output file (VBRCFILE) for each account processed.",
      "condition": "For every account record successfully read",
      "action": "MOVE 12 TO WS-RECD-LEN; MOVE VBRC-REC1 TO VBR-REC(1:12); WRITE VBR-REC",
      "outcome": "A 12-byte variable-length record with account ID and active status is appended to VBRCFILE",
      "acceptance_criteria": {
        "given": "An account record has been read from ACCTFILE",
        "when": "Variable-length record type 1 is being written",
        "then": "A 12-byte record containing ACCT-ID and ACCT-ACTIVE-STATUS is written to VBRCFILE"
      },
      "decision_table": [],
      "source_reference": "CBACT01C.cbl paragraphs 1500-POPUL-VBRC-RECORD and 1550-WRITE-VB1-RECORD",
      "source_snippet": "MOVE   ACCT-ID            TO VB1-ACCT-ID\n                             VB2-ACCT-ID.\nMOVE   ACCT-ACTIVE-STATUS TO VB1-ACCT-ACTIVE-STATUS.\n...\n1550-WRITE-VB1-RECORD.\n    MOVE 12 TO WS-RECD-LEN.\n    MOVE VBRC-REC1 TO VBR-REC(1:WS-RECD-LEN).\n    WRITE VBR-REC.",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR006",
      "rule_name": "Variable-Length Record Type 2 - Account Balance/Credit Record (39 bytes)",
      "rule_category": "Routing Rule",
      "business_description": "A 39-byte variable-length record containing Account ID, Current Balance, Credit Limit, and Reissue Year (4 characters) is written to the variable-length output file (VBRCFILE) for each account processed.",
      "condition": "For every account record successfully read",
      "action": "MOVE 39 TO WS-RECD-LEN; MOVE VBRC-REC2 TO VBR-REC(1:39); WRITE VBR-REC",
      "outcome": "A 39-byte variable-length record with account ID, current balance, credit limit, and reissue year is appended to VBRCFILE",
      "acceptance_criteria": {
        "given": "An account record has been read from ACCTFILE",
        "when": "Variable-length record type 2 is being written",
        "then": "A 39-byte record containing VB2-ACCT-ID, VB2-ACCT-CURR-BAL, VB2-ACCT-CREDIT-LIMIT, and VB2-ACCT-REISSUE-YYYY is written to VBRCFILE"
      },
      "decision_table": [],
      "source_reference": "CBACT01C.cbl paragraphs 1500-POPUL-VBRC-RECORD and 1575-WRITE-VB2-RECORD",
      "source_snippet": "MOVE   ACCT-CURR-BAL           TO  VB2-ACCT-CURR-BAL.\nMOVE   ACCT-CREDIT-LIMIT       TO  VB2-ACCT-CREDIT-LIMIT.\nMOVE   WS-ACCT-REISSUE-YYYY    TO  VB2-ACCT-REISSUE-YYYY.\n...\n1575-WRITE-VB2-RECORD.\n    MOVE 39 TO WS-RECD-LEN.\n    MOVE VBRC-REC2 TO VBR-REC(1:WS-RECD-LEN).\n    WRITE VBR-REC.",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR007",
      "rule_name": "Array Record Hardcoded Balance/Debit Values",
      "rule_category": "Calculation Rule",
      "business_description": "When populating the array output record, hardcoded debit and balance values are assigned to array occurrences 1 through 3. Occurrence 1 uses the live account current balance and debit of 1005.00. Occurrence 2 uses the live account current balance and debit of 1525.00. Occurrence 3 uses hardcoded negative values: balance -1025.00 and debit -2500.00. The business meaning of these hardcoded values is not described in source comments.",
      "condition": "For every account record successfully read",
      "action": "Move ACCT-CURR-BAL to ARR-ACCT-CURR-BAL(1); MOVE 1005.00 to ARR-ACCT-CURR-CYC-DEBIT(1); MOVE ACCT-CURR-BAL to ARR-ACCT-CURR-BAL(2); MOVE 1525.00 to ARR-ACCT-CURR-CYC-DEBIT(2); MOVE -1025.00 to ARR-ACCT-CURR-BAL(3); MOVE -2500.00 to ARR-ACCT-CURR-CYC-DEBIT(3)",
      "outcome": "ARRYFILE record written with 3 populated array occurrences using a mix of live account balance and hardcoded debit/balance values",
      "acceptance_criteria": {
        "given": "An account record has been read from ACCTFILE",
        "when": "The array output record is being populated",
        "then": "Array occurrence 1 contains ACCT-CURR-BAL and 1005.00 debit; occurrence 2 contains ACCT-CURR-BAL and 1525.00 debit; occurrence 3 contains -1025.00 balance and -2500.00 debit"
      },
      "decision_table": [],
      "source_reference": "CBACT01C.cbl paragraph 1400-POPUL-ARRAY-RECORD",
      "source_snippet": "MOVE   ACCT-ID         TO   ARR-ACCT-ID.\nMOVE   ACCT-CURR-BAL   TO   ARR-ACCT-CURR-BAL(1).\nMOVE   1005.00         TO   ARR-ACCT-CURR-CYC-DEBIT(1).\nMOVE   ACCT-CURR-BAL   TO   ARR-ACCT-CURR-BAL(2).\nMOVE   1525.00         TO   ARR-ACCT-CURR-CYC-DEBIT(2).\nMOVE   -1025.00        TO   ARR-ACCT-CURR-BAL(3).\nMOVE   -2500.00        TO   ARR-ACCT-CURR-CYC-DEBIT(3).",
      "confidence_level": "LOW"
    }
  ],
  "business_capability_mapping": [
    {
      "business_capability": null,
      "business_process": null,
      "business_function": null,
      "evidence": "Metadata field business_context.classification_confidence = 'NONE'; no explicit business capability assignment found in source or metadata."
    }
  ],
  "processing_inputs": [
    {
      "input_name": "ACCTFILE",
      "input_type": "VSAM KSDS Indexed Sequential File",
      "purpose": "Account master file read sequentially to extract all account records for output processing.",
      "evidence": "CBACT01C.cbl FILE-CONTROL: SELECT ACCTFILE-FILE ASSIGN TO ACCTFILE ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL RECORD KEY IS FD-ACCT-ID. FD record length 300 (11 + 289)."
    }
  ],
  "processing_outputs": [
    {
      "output_name": "OUTFILE",
      "output_type": "Sequential File",
      "purpose": "Flat sequential output of account records with all key account fields including reformatted reissue date and default debit logic applied.",
      "evidence": "CBACT01C.cbl FILE-CONTROL: SELECT OUT-FILE ASSIGN TO OUTFILE ORGANIZATION IS SEQUENTIAL. FD OUT-FILE with fields: OUT-ACCT-ID, OUT-ACCT-ACTIVE-STATUS, OUT-ACCT-CURR-BAL, OUT-ACCT-CREDIT-LIMIT, OUT-ACCT-CASH-CREDIT-LIMIT, OUT-ACCT-OPEN-DATE, OUT-ACCT-EXPIRAION-DATE, OUT-ACCT-REISSUE-DATE, OUT-ACCT-CURR-CYC-CREDIT, OUT-ACCT-CURR-CYC-DEBIT, OUT-ACCT-GROUP-ID."
    },
    {
      "output_name": "ARRYFILE",
      "output_type": "Sequential File",
      "purpose": "Array-structured sequential output of account records with 5-occurrence balance/debit array per record.",
      "evidence": "CBACT01C.cbl FILE-CONTROL: SELECT ARRY-FILE ASSIGN TO ARRYFILE ORGANIZATION IS SEQUENTIAL. FD ARRY-FILE with ARR-ACCT-ID and ARR-ACCT-BAL OCCURS 5 TIMES containing ARR-ACCT-CURR-BAL and ARR-ACCT-CURR-CYC-DEBIT."
    },
    {
      "output_name": "VBRCFILE",
      "output_type": "Variable-Length Sequential File",
      "purpose": "Variable-length record output with two record types per account: a 12-byte status record and a 39-byte balance/credit record.",
      "evidence": "CBACT01C.cbl FILE-CONTROL: SELECT VBRC-FILE ASSIGN TO VBRCFILE ORGANIZATION IS SEQUENTIAL. FD VBRC-FILE RECORDING MODE IS V RECORD IS VARYING IN SIZE FROM 10 TO 80 DEPENDING ON WS-RECD-LEN."
    }
  ],
  "traceability_summary": {
    "total_business_rules": 7,
    "rules_with_source_evidence": 7,
    "rules_without_source_evidence": 0
  }
}
`
