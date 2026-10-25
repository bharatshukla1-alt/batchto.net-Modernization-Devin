# Business Rules: CBACT02C

`json
{
  "business_summary": {
    "program_name": "CBACT02C",
    "program_description": "Batch COBOL program that reads and prints card data from a VSAM KSDS indexed file sequentially. Opens the card file, reads every card record from start to end-of-file, displays each record, and closes the file.",
    "business_objective": "Sequentially read all card records from the card data file and produce a printed output of each card record.",
    "business_outcome": "All card records in the card data file are displayed (printed) to output; processing terminates cleanly at end-of-file.",
    "confidence_level": "HIGH",
    "evidence": [
      "Program header comment: 'Function: Read and print card data file.' (CBACT02C.cbl line 4)",
      "PERFORM UNTIL END-OF-FILE = 'Y' loop with DISPLAY CARD-RECORD (CBACT02C.cbl lines 74-82)",
      "CVACT02Y copybook defines CARD-RECORD with CARD-NUM, CARD-ACCT-ID, CARD-CVV-CD, CARD-EMBOSSED-NAME, CARD-EXPIRAION-DATE, CARD-ACTIVE-STATUS"
    ]
  },
  "business_process_summary": [
    {
      "step_number": 1,
      "process_name": "Open Card File",
      "description": "Open the VSAM KSDS CARDFILE-FILE for sequential input. Abend the program if the file cannot be opened.",
      "source_reference": "CBACT02C.cbl - paragraph 0000-CARDFILE-OPEN"
    },
    {
      "step_number": 2,
      "process_name": "Read Card Records Sequentially",
      "description": "Repeatedly read the next card record from the file until end-of-file is reached.",
      "source_reference": "CBACT02C.cbl - PERFORM UNTIL END-OF-FILE = 'Y' / paragraph 1000-CARDFILE-GET-NEXT"
    },
    {
      "step_number": 3,
      "process_name": "Display Card Record",
      "description": "Display (print) each successfully read card record to output.",
      "source_reference": "CBACT02C.cbl lines 78-81 - DISPLAY CARD-RECORD"
    },
    {
      "step_number": 4,
      "process_name": "Detect End of File",
      "description": "When CARDFILE-STATUS = '10' (end-of-file), set END-OF-FILE flag to 'Y' to terminate the read loop.",
      "source_reference": "CBACT02C.cbl - paragraph 1000-CARDFILE-GET-NEXT lines 102-109"
    },
    {
      "step_number": 5,
      "process_name": "Close Card File",
      "description": "Close the CARDFILE-FILE after all records have been processed. Abend if close fails.",
      "source_reference": "CBACT02C.cbl - paragraph 9000-CARDFILE-CLOSE"
    }
  ],
  "business_entities": [
    {
      "entity_name": "Card Record",
      "entity_type": "File Record",
      "evidence": "CVACT02Y copybook defines 01 CARD-RECORD with fields: CARD-NUM, CARD-ACCT-ID, CARD-CVV-CD, CARD-EMBOSSED-NAME, CARD-EXPIRAION-DATE, CARD-ACTIVE-STATUS (RECLN 150)"
    },
    {
      "entity_name": "Card File",
      "entity_type": "VSAM KSDS File",
      "evidence": "SELECT CARDFILE-FILE ASSIGN TO CARDFILE ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL RECORD KEY IS FD-CARD-NUM (CBACT02C.cbl lines 30-34)"
    }
  ],
  "business_data_elements": [
    {
      "element_name": "CARD-NUM",
      "description": "16-character card number; primary key of the card file used to uniquely identify each card record.",
      "source_reference": "CVACT02Y.cpy - 05 CARD-NUM PIC X(16); also FD-CARD-NUM PIC X(16) in CBACT02C.cbl file section"
    },
    {
      "element_name": "CARD-ACCT-ID",
      "description": "11-digit account identifier associated with the card record.",
      "source_reference": "CVACT02Y.cpy - 05 CARD-ACCT-ID PIC 9(11)"
    },
    {
      "element_name": "CARD-CVV-CD",
      "description": "3-digit card CVV security code associated with the card.",
      "source_reference": "CVACT02Y.cpy - 05 CARD-CVV-CD PIC 9(03)"
    },
    {
      "element_name": "CARD-EMBOSSED-NAME",
      "description": "50-character name embossed on the card.",
      "source_reference": "CVACT02Y.cpy - 05 CARD-EMBOSSED-NAME PIC X(50)"
    },
    {
      "element_name": "CARD-EXPIRAION-DATE",
      "description": "10-character card expiration date.",
      "source_reference": "CVACT02Y.cpy - 05 CARD-EXPIRAION-DATE PIC X(10)"
    },
    {
      "element_name": "CARD-ACTIVE-STATUS",
      "description": "1-character flag indicating whether the card is active or inactive.",
      "source_reference": "CVACT02Y.cpy - 05 CARD-ACTIVE-STATUS PIC X(01)"
    },
    {
      "element_name": "CARDFILE-STATUS",
      "description": "2-character VSAM file status code returned after each file I/O operation; governs EOF detection and error handling.",
      "source_reference": "CBACT02C.cbl - 01 CARDFILE-STATUS / FILE STATUS IS CARDFILE-STATUS"
    },
    {
      "element_name": "END-OF-FILE",
      "description": "1-character flag ('Y'/'N') controlling the sequential read loop; set to 'Y' when EOF is detected.",
      "source_reference": "CBACT02C.cbl - 01 END-OF-FILE PIC X(01) VALUE 'N'"
    }
  ],
  "business_rules": [
    {
      "rule_id": "BR-001",
      "rule_name": "Sequential Card File Read Loop",
      "rule_category": "Routing Rule",
      "business_description": "All card records in the card file are processed sequentially from the beginning to end-of-file. Processing continues as long as the end-of-file flag is 'N'.",
      "condition": "END-OF-FILE = 'N'",
      "action": "Read next card record and display it",
      "outcome": "Every card record in the card file is read and displayed exactly once in sequential order.",
      "acceptance_criteria": {
        "given": "The card file is open and contains one or more card records",
        "when": "The sequential read loop executes",
        "then": "Every card record is read and displayed until end-of-file is reached"
      },
      "decision_table": [
        {
          "condition": "END-OF-FILE = 'N'",
          "outcome": "Perform 1000-CARDFILE-GET-NEXT; if still 'N', display CARD-RECORD"
        },
        {
          "condition": "END-OF-FILE = 'Y'",
          "outcome": "Exit PERFORM loop; proceed to close file"
        }
      ],
      "source_reference": "CBACT02C.cbl lines 74-82",
      "source_snippet": "PERFORM UNTIL END-OF-FILE = 'Y'\n    IF  END-OF-FILE = 'N'\n        PERFORM 1000-CARDFILE-GET-NEXT\n        IF  END-OF-FILE = 'N'\n            DISPLAY CARD-RECORD\n        END-IF\n    END-IF\nEND-PERFORM.",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR-002",
      "rule_name": "End-of-File Detection and Loop Termination",
      "rule_category": "Decision Rule",
      "business_description": "When VSAM file status '10' (end-of-file) is returned after a read, the end-of-file flag is set to 'Y', which terminates the card record processing loop.",
      "condition": "CARDFILE-STATUS = '10' after READ",
      "action": "MOVE 16 TO APPL-RESULT; then MOVE 'Y' TO END-OF-FILE",
      "outcome": "Card record processing loop terminates; no further card records are read.",
      "acceptance_criteria": {
        "given": "The program is reading card records sequentially",
        "when": "A READ returns CARDFILE-STATUS = '10' (end-of-file)",
        "then": "END-OF-FILE is set to 'Y' and the read loop terminates"
      },
      "decision_table": [
        {
          "condition": "CARDFILE-STATUS = '00'",
          "outcome": "APPL-RESULT = 0 (AOK); continue processing"
        },
        {
          "condition": "CARDFILE-STATUS = '10'",
          "outcome": "APPL-RESULT = 16 (EOF); set END-OF-FILE = 'Y'; terminate loop"
        },
        {
          "condition": "CARDFILE-STATUS = other",
          "outcome": "APPL-RESULT = 12; display error; abend program"
        }
      ],
      "source_reference": "CBACT02C.cbl - paragraph 1000-CARDFILE-GET-NEXT lines 100-116",
      "source_snippet": "READ CARDFILE-FILE INTO CARD-RECORD.\nIF  CARDFILE-STATUS = '00'\n    MOVE 0 TO APPL-RESULT\nELSE\n    IF  CARDFILE-STATUS = '10'\n        MOVE 16 TO APPL-RESULT\n    ELSE\n        MOVE 12 TO APPL-RESULT\n    END-IF\nEND-IF\nIF  APPL-AOK\n    CONTINUE\nELSE\n    IF  APPL-EOF\n        MOVE 'Y' TO END-OF-FILE\n    ELSE\n        DISPLAY 'ERROR READING CARDFILE'\n        MOVE CARDFILE-STATUS TO IO-STATUS\n        PERFORM 9910-DISPLAY-IO-STATUS\n        PERFORM 9999-ABEND-PROGRAM\n    END-IF\nEND-IF",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR-003",
      "rule_name": "Card Record Display on Successful Read",
      "rule_category": "Reporting Rule",
      "business_description": "A card record is displayed to output only when it has been successfully read (i.e., end-of-file has not been set). This ensures that no spurious output is produced when EOF is encountered.",
      "condition": "END-OF-FILE = 'N' after 1000-CARDFILE-GET-NEXT",
      "action": "DISPLAY CARD-RECORD",
      "outcome": "The complete card record (including card number, account ID, CVV, embossed name, expiration date, active status) is written to program output.",
      "acceptance_criteria": {
        "given": "A card record has been successfully read from the card file",
        "when": "END-OF-FILE remains 'N' after the read",
        "then": "The full CARD-RECORD is displayed to output"
      },
      "decision_table": [
        {
          "condition": "END-OF-FILE = 'N' after read",
          "outcome": "DISPLAY CARD-RECORD"
        },
        {
          "condition": "END-OF-FILE = 'Y' after read (EOF reached)",
          "outcome": "Do not display; exit loop"
        }
      ],
      "source_reference": "CBACT02C.cbl lines 78-81",
      "source_snippet": "PERFORM 1000-CARDFILE-GET-NEXT\nIF  END-OF-FILE = 'N'\n    DISPLAY CARD-RECORD\nEND-IF",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR-004",
      "rule_name": "Card File Open Failure Abend",
      "rule_category": "Exception Rule",
      "business_description": "If the card data file cannot be opened (CARDFILE-STATUS not '00'), processing cannot proceed and the program abends. This ensures no card data processing occurs when the source data file is unavailable.",
      "condition": "CARDFILE-STATUS != '00' after OPEN INPUT CARDFILE-FILE",
      "action": "MOVE 12 TO APPL-RESULT; display 'ERROR OPENING CARDFILE'; display IO-STATUS; CALL 'CEE3ABD' to abend",
      "outcome": "Program terminates abnormally; card file processing does not proceed with an unavailable or inaccessible card file.",
      "acceptance_criteria": {
        "given": "The program attempts to open the card file",
        "when": "CARDFILE-STATUS is not '00' after the OPEN",
        "then": "An error message is displayed and the program abends"
      },
      "decision_table": [
        {
          "condition": "CARDFILE-STATUS = '00' after OPEN",
          "outcome": "APPL-RESULT = 0 (AOK); continue to read loop"
        },
        {
          "condition": "CARDFILE-STATUS != '00' after OPEN",
          "outcome": "APPL-RESULT = 12; display error; abend program (CEE3ABD, ABCODE=999)"
        }
      ],
      "source_reference": "CBACT02C.cbl - paragraph 0000-CARDFILE-OPEN lines 118-133",
      "source_snippet": "MOVE 8 TO APPL-RESULT.\nOPEN INPUT CARDFILE-FILE\nIF  CARDFILE-STATUS = '00'\n    MOVE 0 TO APPL-RESULT\nELSE\n    MOVE 12 TO APPL-RESULT\nEND-IF\nIF  APPL-AOK\n    CONTINUE\nELSE\n    DISPLAY 'ERROR OPENING CARDFILE'\n    MOVE CARDFILE-STATUS TO IO-STATUS\n    PERFORM 9910-DISPLAY-IO-STATUS\n    PERFORM 9999-ABEND-PROGRAM\nEND-IF",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR-005",
      "rule_name": "Unexpected Card File Read Error Abend",
      "rule_category": "Exception Rule",
      "business_description": "If a card file read operation returns a status other than '00' (success) or '10' (end-of-file), this indicates an unexpected I/O error. The program displays an error diagnostic and abends to prevent processing partial or corrupt card data.",
      "condition": "CARDFILE-STATUS != '00' AND CARDFILE-STATUS != '10' after READ",
      "action": "MOVE 12 TO APPL-RESULT; display 'ERROR READING CARDFILE'; display IO-STATUS; CALL 'CEE3ABD' to abend",
      "outcome": "Program terminates abnormally with a diagnostic message; no further card records are read or displayed.",
      "acceptance_criteria": {
        "given": "The program is reading card records sequentially",
        "when": "A READ returns a status other than '00' or '10'",
        "then": "An error message including the file status code is displayed and the program abends"
      },
      "decision_table": [
        {
          "condition": "APPL-AOK (APPL-RESULT = 0)",
          "outcome": "Continue processing"
        },
        {
          "condition": "APPL-EOF (APPL-RESULT = 16)",
          "outcome": "Set END-OF-FILE = 'Y'; terminate loop"
        },
        {
          "condition": "APPL-RESULT = 12 (other error)",
          "outcome": "Display 'ERROR READING CARDFILE'; display IO-STATUS; abend (CEE3ABD, ABCODE=999)"
        }
      ],
      "source_reference": "CBACT02C.cbl - paragraph 1000-CARDFILE-GET-NEXT lines 104-115",
      "source_snippet": "IF  APPL-AOK\n    CONTINUE\nELSE\n    IF  APPL-EOF\n        MOVE 'Y' TO END-OF-FILE\n    ELSE\n        DISPLAY 'ERROR READING CARDFILE'\n        MOVE CARDFILE-STATUS TO IO-STATUS\n        PERFORM 9910-DISPLAY-IO-STATUS\n        PERFORM 9999-ABEND-PROGRAM\n    END-IF\nEND-IF",
      "confidence_level": "HIGH"
    }
  ],
  "business_capability_mapping": [
    {
      "business_capability": "Card Data Reporting",
      "business_process": "Sequential Card File Read and Print",
      "business_function": "Read and display all card records from card data file",
      "evidence": "Program header comment: 'Function: Read and print card data file.' (CBACT02C.cbl line 4); DISPLAY CARD-RECORD in main processing loop"
    }
  ],
  "processing_inputs": [
    {
      "input_name": "CARDFILE",
      "input_type": "VSAM KSDS Indexed File",
      "purpose": "Source file containing card records to be read and printed sequentially.",
      "evidence": "SELECT CARDFILE-FILE ASSIGN TO CARDFILE ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL RECORD KEY IS FD-CARD-NUM (CBACT02C.cbl lines 30-34); file_dependencies in CBACT02C.json"
    }
  ],
  "processing_outputs": [
    {
      "output_name": "CARD-RECORD Display",
      "output_type": "Program Output (SYSOUT)",
      "purpose": "Each card record is displayed to program output, constituting the printed report of all card data.",
      "evidence": "DISPLAY CARD-RECORD inside PERFORM UNTIL loop (CBACT02C.cbl lines 78-81)"
    }
  ],
  "traceability_summary": {
    "total_business_rules": 5,
    "rules_with_source_evidence": 5,
    "rules_without_source_evidence": 0
  }
}
`
