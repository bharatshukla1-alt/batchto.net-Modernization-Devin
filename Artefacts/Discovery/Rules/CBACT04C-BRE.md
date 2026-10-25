# Business Rules: CBACT04C

`json
{
  "business_summary": {
    "program_name": "CBACT04C",
    "program_description": "Batch interest calculator program for the CardDemo application. Reads transaction category balance records sequentially, looks up applicable interest rates from a disclosure group file, computes monthly interest per transaction category per account, writes interest charge transaction records, and updates account current balances with total accrued interest while resetting cycle credit and debit amounts.",
    "business_objective": "Calculate and post monthly interest charges to card accounts based on transaction category balances and account-group-specific interest rates from a disclosure group reference file.",
    "business_outcome": "Account current balances are updated with computed interest charges; interest transactions are written to the transaction file; cycle credit and debit amounts are reset to zero.",
    "confidence_level": "HIGH",
    "evidence": [
      "Program header comment: 'Function : This is a interest calculator program.' (CBACT04C.cbl line 5)",
      "COMPUTE WS-MONTHLY-INT = ( TRAN-CAT-BAL * DIS-INT-RATE) / 1200 (CBACT04C.cbl paragraph 1300-COMPUTE-INTEREST)",
      "ADD WS-TOTAL-INT TO ACCT-CURR-BAL (CBACT04C.cbl paragraph 1050-UPDATE-ACCOUNT)",
      "MOVE 0 TO ACCT-CURR-CYC-CREDIT / MOVE 0 TO ACCT-CURR-CYC-DEBIT (CBACT04C.cbl paragraph 1050-UPDATE-ACCOUNT)"
    ]
  },
  "business_process_summary": [
    {
      "step_number": 1,
      "process_name": "Open Files",
      "description": "Open TCATBAL-FILE (input), XREF-FILE (input), DISCGRP-FILE (input), ACCOUNT-FILE (I-O), and TRANSACT-FILE (output) for processing.",
      "source_reference": "CBACT04C.cbl PROCEDURE DIVISION - paragraphs 0000-TCATBALF-OPEN through 0400-TRANFILE-OPEN"
    },
    {
      "step_number": 2,
      "process_name": "Read Transaction Category Balance Record",
      "description": "Sequentially read next TRAN-CAT-BAL-RECORD from TCATBAL-FILE. Set END-OF-FILE='Y' when end of file is reached.",
      "source_reference": "CBACT04C.cbl paragraph 1000-TCATBALF-GET-NEXT"
    },
    {
      "step_number": 3,
      "process_name": "Detect Account Group Boundary",
      "description": "Compare TRANCAT-ACCT-ID to WS-LAST-ACCT-NUM. If different, a new account is encountered: trigger account update for the prior account (unless first record), reset WS-TOTAL-INT to zero, and set the new account as current.",
      "source_reference": "CBACT04C.cbl PROCEDURE DIVISION main loop - IF TRANCAT-ACCT-ID NOT= WS-LAST-ACCT-NUM"
    },
    {
      "step_number": 4,
      "process_name": "Read Account Data",
      "description": "Read ACCOUNT-RECORD from ACCOUNT-FILE using TRANCAT-ACCT-ID as key to obtain account attributes including ACCT-GROUP-ID and ACCT-CURR-BAL.",
      "source_reference": "CBACT04C.cbl paragraph 1100-GET-ACCT-DATA"
    },
    {
      "step_number": 5,
      "process_name": "Read Card Cross-Reference Data",
      "description": "Read CARD-XREF-RECORD from XREF-FILE using TRANCAT-ACCT-ID as alternate key to obtain XREF-CARD-NUM for transaction record population.",
      "source_reference": "CBACT04C.cbl paragraph 1110-GET-XREF-DATA"
    },
    {
      "step_number": 6,
      "process_name": "Lookup Interest Rate from Disclosure Group",
      "description": "Read DIS-GROUP-RECORD from DISCGRP-FILE using ACCT-GROUP-ID + TRANCAT-TYPE-CD + TRANCAT-CD as composite key. If not found, fall back to 'DEFAULT' account group code.",
      "source_reference": "CBACT04C.cbl paragraphs 1200-GET-INTEREST-RATE and 1200-A-GET-DEFAULT-INT-RATE"
    },
    {
      "step_number": 7,
      "process_name": "Compute Monthly Interest",
      "description": "If DIS-INT-RATE is non-zero, compute monthly interest as (TRAN-CAT-BAL * DIS-INT-RATE) / 1200 and accumulate into WS-TOTAL-INT.",
      "source_reference": "CBACT04C.cbl paragraph 1300-COMPUTE-INTEREST"
    },
    {
      "step_number": 8,
      "process_name": "Write Interest Transaction Record",
      "description": "Generate and write an interest transaction record to TRANSACT-FILE with transaction type '01', category '05', source 'System', description 'Int. for a/c {ACCT-ID}', and amount equal to WS-MONTHLY-INT.",
      "source_reference": "CBACT04C.cbl paragraph 1300-B-WRITE-TX"
    },
    {
      "step_number": 9,
      "process_name": "Update Account Balance",
      "description": "When account group boundary is crossed or end-of-file is reached, add WS-TOTAL-INT to ACCT-CURR-BAL, reset ACCT-CURR-CYC-CREDIT and ACCT-CURR-CYC-DEBIT to zero, and rewrite the account record.",
      "source_reference": "CBACT04C.cbl paragraph 1050-UPDATE-ACCOUNT"
    },
    {
      "step_number": 10,
      "process_name": "Close Files",
      "description": "Close all five files after processing is complete.",
      "source_reference": "CBACT04C.cbl paragraphs 9000-TCATBALF-CLOSE through 9400-TRANFILE-CLOSE"
    }
  ],
  "business_entities": [
    {
      "entity_name": "Transaction Category Balance",
      "entity_type": "Input Record",
      "evidence": "FD-TRAN-CAT-BAL-RECORD / TRAN-CAT-BAL-RECORD (CVTRA01Y.cpy) - keyed by TRANCAT-ACCT-ID + TRANCAT-TYPE-CD + TRANCAT-CD; carries TRAN-CAT-BAL"
    },
    {
      "entity_name": "Account",
      "entity_type": "Master Record (Read/Update)",
      "evidence": "ACCOUNT-RECORD (CVACT01Y.cpy) - contains ACCT-ID, ACCT-CURR-BAL, ACCT-GROUP-ID, ACCT-CURR-CYC-CREDIT, ACCT-CURR-CYC-DEBIT"
    },
    {
      "entity_name": "Disclosure Group",
      "entity_type": "Reference Record",
      "evidence": "DIS-GROUP-RECORD (CVTRA02Y.cpy) - keyed by DIS-ACCT-GROUP-ID + DIS-TRAN-TYPE-CD + DIS-TRAN-CAT-CD; carries DIS-INT-RATE"
    },
    {
      "entity_name": "Card Cross-Reference",
      "entity_type": "Reference Record",
      "evidence": "CARD-XREF-RECORD (CVACT03Y.cpy) - links XREF-ACCT-ID to XREF-CARD-NUM and XREF-CUST-ID"
    },
    {
      "entity_name": "Transaction",
      "entity_type": "Output Record",
      "evidence": "TRAN-RECORD (CVTRA05Y.cpy) - written to TRANSACT-FILE for each interest charge generated"
    }
  ],
  "business_data_elements": [
    {
      "element_name": "TRANCAT-ACCT-ID",
      "description": "Account identifier on the transaction category balance record; used to group records by account and key account/xref lookups.",
      "source_reference": "CVTRA01Y.cpy - 10 TRANCAT-ACCT-ID PIC 9(11)"
    },
    {
      "element_name": "TRANCAT-TYPE-CD",
      "description": "Transaction type code; part of composite key for disclosure group interest rate lookup.",
      "source_reference": "CVTRA01Y.cpy - 10 TRANCAT-TYPE-CD PIC X(02)"
    },
    {
      "element_name": "TRANCAT-CD",
      "description": "Transaction category code; part of composite key for disclosure group interest rate lookup.",
      "source_reference": "CVTRA01Y.cpy - 10 TRANCAT-CD PIC 9(04)"
    },
    {
      "element_name": "TRAN-CAT-BAL",
      "description": "Balance amount for the transaction category; used as the principal in monthly interest computation.",
      "source_reference": "CVTRA01Y.cpy - 05 TRAN-CAT-BAL PIC S9(09)V99"
    },
    {
      "element_name": "ACCT-GROUP-ID",
      "description": "Account group identifier from account record; used as the primary key component for disclosure group interest rate lookup.",
      "source_reference": "CVACT01Y.cpy - 05 ACCT-GROUP-ID PIC X(10)"
    },
    {
      "element_name": "ACCT-CURR-BAL",
      "description": "Current balance of the account; updated by adding total computed interest at account group boundary.",
      "source_reference": "CVACT01Y.cpy - 05 ACCT-CURR-BAL PIC S9(10)V99"
    },
    {
      "element_name": "ACCT-CURR-CYC-CREDIT",
      "description": "Current cycle credit amount on the account; reset to zero after interest posting.",
      "source_reference": "CVACT01Y.cpy - 05 ACCT-CURR-CYC-CREDIT PIC S9(10)V99"
    },
    {
      "element_name": "ACCT-CURR-CYC-DEBIT",
      "description": "Current cycle debit amount on the account; reset to zero after interest posting.",
      "source_reference": "CVACT01Y.cpy - 05 ACCT-CURR-CYC-DEBIT PIC S9(10)V99"
    },
    {
      "element_name": "DIS-INT-RATE",
      "description": "Annual interest rate from disclosure group record; applied to transaction category balance to derive monthly interest.",
      "source_reference": "CVTRA02Y.cpy - 05 DIS-INT-RATE PIC S9(04)V99"
    },
    {
      "element_name": "WS-MONTHLY-INT",
      "description": "Computed monthly interest for a single transaction category; intermediate calculation result.",
      "source_reference": "CBACT04C.cbl WS-MISC-VARS - 05 WS-MONTHLY-INT PIC S9(09)V99"
    },
    {
      "element_name": "WS-TOTAL-INT",
      "description": "Accumulated total interest across all transaction categories for the current account; added to ACCT-CURR-BAL at account boundary.",
      "source_reference": "CBACT04C.cbl WS-MISC-VARS - 05 WS-TOTAL-INT PIC S9(09)V99"
    },
    {
      "element_name": "XREF-CARD-NUM",
      "description": "Card number from cross-reference record; populated onto interest transaction record (TRAN-CARD-NUM).",
      "source_reference": "CVACT03Y.cpy - 05 XREF-CARD-NUM PIC X(16)"
    },
    {
      "element_name": "TRAN-TYPE-CD",
      "description": "Transaction type code on generated interest transaction; hardcoded to '01' for interest transactions.",
      "source_reference": "CBACT04C.cbl paragraph 1300-B-WRITE-TX - MOVE '01' TO TRAN-TYPE-CD"
    },
    {
      "element_name": "TRAN-CAT-CD",
      "description": "Transaction category code on generated interest transaction; hardcoded to '05' for interest transactions.",
      "source_reference": "CBACT04C.cbl paragraph 1300-B-WRITE-TX - MOVE '05' TO TRAN-CAT-CD"
    },
    {
      "element_name": "PARM-DATE",
      "description": "Run date passed as external parameter; used as prefix of generated transaction ID.",
      "source_reference": "CBACT04C.cbl LINKAGE SECTION - 05 PARM-DATE PIC X(10)"
    }
  ],
  "business_rules": [
    {
      "rule_id": "BR001",
      "rule_name": "Interest Rate Lookup by Account Group and Transaction Category",
      "rule_category": "Lookup Rule",
      "business_description": "The applicable interest rate for a transaction category balance is determined by reading the disclosure group file using a composite key of account group identifier, transaction type code, and transaction category code.",
      "condition": "For each TRAN-CAT-BAL-RECORD, compose lookup key: ACCT-GROUP-ID + TRANCAT-TYPE-CD + TRANCAT-CD",
      "action": "Read DIS-GROUP-RECORD from DISCGRP-FILE using composite key (FD-DIS-ACCT-GROUP-ID + FD-DIS-TRAN-TYPE-CD + FD-DIS-TRAN-CAT-CD)",
      "outcome": "DIS-INT-RATE is populated from the disclosure group record for use in interest calculation",
      "acceptance_criteria": {
        "given": "A transaction category balance record exists with a known ACCT-GROUP-ID, TRANCAT-TYPE-CD, and TRANCAT-CD",
        "when": "The program reads the disclosure group file with those three values as the composite key",
        "then": "DIS-INT-RATE is retrieved from the matching disclosure group record"
      },
      "decision_table": [
        {
          "condition": "DISCGRP-STATUS = '00' (record found)",
          "outcome": "DIS-INT-RATE from matched disclosure group record is used"
        },
        {
          "condition": "DISCGRP-STATUS = '23' (record not found)",
          "outcome": "Fall back to DEFAULT group lookup (BR002)"
        }
      ],
      "source_reference": "CBACT04C.cbl paragraph 1200-GET-INTEREST-RATE",
      "source_snippet": "MOVE ACCT-GROUP-ID TO FD-DIS-ACCT-GROUP-ID\n           MOVE TRANCAT-CD TO FD-DIS-TRAN-CAT-CD\n           MOVE TRANCAT-TYPE-CD TO FD-DIS-TRAN-TYPE-CD\n           PERFORM 1200-GET-INTEREST-RATE\n...\n1200-GET-INTEREST-RATE.\n           READ DISCGRP-FILE INTO DIS-GROUP-RECORD\n                INVALID KEY\n                   DISPLAY 'DISCLOSURE GROUP RECORD MISSING'\n                   DISPLAY 'TRY WITH DEFAULT GROUP CODE'\n           END-READ.",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR002",
      "rule_name": "Default Interest Rate Fallback When Account Group Not Found",
      "rule_category": "Exception Rule",
      "business_description": "If the disclosure group record is not found for the account's specific group, the program retries the lookup using a hardcoded 'DEFAULT' account group identifier to obtain a default interest rate.",
      "condition": "DISCGRP-STATUS = '23' after initial disclosure group lookup",
      "action": "MOVE 'DEFAULT' TO FD-DIS-ACCT-GROUP-ID and PERFORM 1200-A-GET-DEFAULT-INT-RATE to re-read disclosure group file",
      "outcome": "DIS-INT-RATE is populated from the DEFAULT disclosure group record",
      "acceptance_criteria": {
        "given": "A transaction category balance record has an ACCT-GROUP-ID for which no disclosure group record exists",
        "when": "The initial disclosure group read returns file status '23' (not found)",
        "then": "The program substitutes 'DEFAULT' as the account group ID and reads the disclosure group file again to obtain a default interest rate"
      },
      "decision_table": [
        {
          "condition": "DISCGRP-STATUS = '23' after primary lookup",
          "outcome": "MOVE 'DEFAULT' TO FD-DIS-ACCT-GROUP-ID; re-read DISCGRP-FILE"
        },
        {
          "condition": "DISCGRP-STATUS = '00' on default lookup",
          "outcome": "DIS-INT-RATE from DEFAULT disclosure group record is used"
        }
      ],
      "source_reference": "CBACT04C.cbl paragraphs 1200-GET-INTEREST-RATE and 1200-A-GET-DEFAULT-INT-RATE",
      "source_snippet": "IF  DISCGRP-STATUS  = '23'\n               MOVE 'DEFAULT' TO FD-DIS-ACCT-GROUP-ID\n               PERFORM 1200-A-GET-DEFAULT-INT-RATE\n           END-IF\n...\n1200-A-GET-DEFAULT-INT-RATE.\n           READ DISCGRP-FILE INTO DIS-GROUP-RECORD",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR003",
      "rule_name": "Skip Interest Calculation When Interest Rate is Zero",
      "rule_category": "Eligibility Rule",
      "business_description": "Interest computation and fee calculation are only performed for transaction categories that carry a non-zero interest rate. Categories with a zero rate are bypassed.",
      "condition": "DIS-INT-RATE NOT = 0",
      "action": "PERFORM 1300-COMPUTE-INTEREST and PERFORM 1400-COMPUTE-FEES",
      "outcome": "Interest and fees are computed only for transaction categories with an applicable non-zero rate; zero-rate categories produce no interest transaction",
      "acceptance_criteria": {
        "given": "A disclosure group record has been successfully retrieved for the current transaction category",
        "when": "DIS-INT-RATE equals zero",
        "then": "No interest calculation is performed and no interest transaction is written for that category"
      },
      "decision_table": [
        {
          "condition": "DIS-INT-RATE NOT = 0",
          "outcome": "Compute interest and fees; write interest transaction"
        },
        {
          "condition": "DIS-INT-RATE = 0",
          "outcome": "Skip interest and fee computation; no transaction written"
        }
      ],
      "source_reference": "CBACT04C.cbl PROCEDURE DIVISION main loop",
      "source_snippet": "IF DIS-INT-RATE NOT = 0\n                       PERFORM 1300-COMPUTE-INTEREST\n                       PERFORM 1400-COMPUTE-FEES\n                     END-IF",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR004",
      "rule_name": "Monthly Interest Calculation Formula",
      "rule_category": "Calculation Rule",
      "business_description": "Monthly interest for a transaction category balance is calculated by multiplying the category balance by the annual interest rate and dividing by 1200 (12 months x 100 to convert percentage rate to decimal).",
      "condition": "DIS-INT-RATE NOT = 0 (prerequisite from BR003)",
      "action": "COMPUTE WS-MONTHLY-INT = (TRAN-CAT-BAL * DIS-INT-RATE) / 1200",
      "outcome": "WS-MONTHLY-INT holds the monthly interest amount for the current transaction category balance",
      "acceptance_criteria": {
        "given": "A transaction category balance TRAN-CAT-BAL and an annual interest rate DIS-INT-RATE are available",
        "when": "Interest is computed for the category",
        "then": "WS-MONTHLY-INT = (TRAN-CAT-BAL * DIS-INT-RATE) / 1200"
      },
      "decision_table": [],
      "source_reference": "CBACT04C.cbl paragraph 1300-COMPUTE-INTEREST",
      "source_snippet": "1300-COMPUTE-INTEREST.\n\n           COMPUTE WS-MONTHLY-INT\n            = ( TRAN-CAT-BAL * DIS-INT-RATE) / 1200\n\n           ADD WS-MONTHLY-INT  TO WS-TOTAL-INT\n           PERFORM 1300-B-WRITE-TX.",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR005",
      "rule_name": "Accumulate Total Interest Per Account",
      "rule_category": "Aggregation Rule",
      "business_description": "Monthly interest amounts computed for each transaction category are accumulated into a running total (WS-TOTAL-INT) for the account. This total is used to update the account balance at the end of account processing.",
      "condition": "Each time WS-MONTHLY-INT is computed for a non-zero interest rate category",
      "action": "ADD WS-MONTHLY-INT TO WS-TOTAL-INT",
      "outcome": "WS-TOTAL-INT accumulates the sum of all category interest amounts for the current account",
      "acceptance_criteria": {
        "given": "Monthly interest WS-MONTHLY-INT has been computed for a transaction category",
        "when": "The interest amount is accumulated",
        "then": "WS-TOTAL-INT is incremented by WS-MONTHLY-INT; WS-TOTAL-INT is reset to zero when a new account is encountered"
      },
      "decision_table": [],
      "source_reference": "CBACT04C.cbl paragraph 1300-COMPUTE-INTEREST and main loop",
      "source_snippet": "ADD WS-MONTHLY-INT  TO WS-TOTAL-INT\n...\nMOVE 0 TO WS-TOTAL-INT   (on account group boundary)",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR006",
      "rule_name": "Interest Transaction Record Generation",
      "rule_category": "Reporting Rule",
      "business_description": "For each transaction category that produces a non-zero interest amount, a system-generated interest transaction record is written to the transaction output file with fixed transaction type '01' and category '05', identifying the account and linked card number.",
      "condition": "WS-MONTHLY-INT has been computed and is to be recorded",
      "action": "Populate TRAN-RECORD: TRAN-TYPE-CD='01', TRAN-CAT-CD='05', TRAN-SOURCE='System', TRAN-DESC='Int. for a/c {ACCT-ID}', TRAN-AMT=WS-MONTHLY-INT, TRAN-CARD-NUM=XREF-CARD-NUM, TRAN-ID=PARM-DATE||WS-TRANID-SUFFIX, then WRITE FD-TRANFILE-REC",
      "outcome": "An interest charge transaction record is written to TRANSACT-FILE for each applicable transaction category",
      "acceptance_criteria": {
        "given": "Monthly interest has been computed for a transaction category balance",
        "when": "The interest transaction record is generated",
        "then": "A transaction record with type '01', category '05', source 'System', amount equal to WS-MONTHLY-INT, and card number from XREF-CARD-NUM is written to the transaction file"
      },
      "decision_table": [],
      "source_reference": "CBACT04C.cbl paragraph 1300-B-WRITE-TX",
      "source_snippet": "MOVE '01'                 TO TRAN-TYPE-CD\n           MOVE '05'                 TO TRAN-CAT-CD\n           MOVE 'System'             TO TRAN-SOURCE\n           STRING 'Int. for a/c ' ,\n                  ACCT-ID\n                  DELIMITED BY SIZE\n            INTO TRAN-DESC\n           END-STRING\n           MOVE WS-MONTHLY-INT       TO TRAN-AMT\n           MOVE XREF-CARD-NUM        TO TRAN-CARD-NUM\n           WRITE FD-TRANFILE-REC FROM TRAN-RECORD",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR007",
      "rule_name": "Transaction ID Generation Using Run Date and Sequence Suffix",
      "rule_category": "Calculation Rule",
      "business_description": "Each interest transaction is assigned a unique transaction ID formed by concatenating the run date (passed as external parameter PARM-DATE) with an incrementing numeric suffix counter (WS-TRANID-SUFFIX).",
      "condition": "Each time an interest transaction record is to be written",
      "action": "ADD 1 TO WS-TRANID-SUFFIX; STRING PARM-DATE, WS-TRANID-SUFFIX DELIMITED BY SIZE INTO TRAN-ID",
      "outcome": "TRAN-ID is set to a unique value combining the run date and an incrementing sequence number",
      "acceptance_criteria": {
        "given": "An interest transaction is being generated",
        "when": "The transaction ID is constructed",
        "then": "TRAN-ID = PARM-DATE concatenated with the incremented WS-TRANID-SUFFIX value"
      },
      "decision_table": [],
      "source_reference": "CBACT04C.cbl paragraph 1300-B-WRITE-TX",
      "source_snippet": "ADD 1 TO WS-TRANID-SUFFIX\n\n           STRING PARM-DATE,\n                  WS-TRANID-SUFFIX\n             DELIMITED BY SIZE\n             INTO TRAN-ID\n           END-STRING.",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR008",
      "rule_name": "Account Balance Update with Total Interest and Cycle Reset",
      "rule_category": "Calculation Rule",
      "business_description": "When all transaction category records for an account have been processed (detected at account ID group boundary or end-of-file), the total accumulated interest (WS-TOTAL-INT) is added to the account's current balance, and both cycle credit and cycle debit amounts are reset to zero. The account record is then rewritten.",
      "condition": "TRANCAT-ACCT-ID changes (NOT= WS-LAST-ACCT-NUM) or END-OF-FILE = 'Y'",
      "action": "ADD WS-TOTAL-INT TO ACCT-CURR-BAL; MOVE 0 TO ACCT-CURR-CYC-CREDIT; MOVE 0 TO ACCT-CURR-CYC-DEBIT; REWRITE FD-ACCTFILE-REC FROM ACCOUNT-RECORD",
      "outcome": "Account current balance is increased by total interest accrued; cycle credit and debit counters are cleared to zero; updated account record is persisted",
      "acceptance_criteria": {
        "given": "All transaction category balance records for an account have been processed and WS-TOTAL-INT has been accumulated",
        "when": "Account group boundary is reached or end-of-file occurs",
        "then": "ACCT-CURR-BAL = ACCT-CURR-BAL + WS-TOTAL-INT; ACCT-CURR-CYC-CREDIT = 0; ACCT-CURR-CYC-DEBIT = 0; account record is rewritten"
      },
      "decision_table": [
        {
          "condition": "TRANCAT-ACCT-ID NOT= WS-LAST-ACCT-NUM AND WS-FIRST-TIME NOT = 'Y'",
          "outcome": "PERFORM 1050-UPDATE-ACCOUNT for prior account; reset WS-TOTAL-INT to 0; set new current account"
        },
        {
          "condition": "END-OF-FILE = 'Y'",
          "outcome": "PERFORM 1050-UPDATE-ACCOUNT for last account processed"
        },
        {
          "condition": "WS-FIRST-TIME = 'Y' (very first account boundary)",
          "outcome": "Skip account update; set WS-FIRST-TIME = 'N'"
        }
      ],
      "source_reference": "CBACT04C.cbl paragraph 1050-UPDATE-ACCOUNT and main processing loop",
      "source_snippet": "1050-UPDATE-ACCOUNT.\n* Update the balances in account record to reflect posted trans.\n           ADD WS-TOTAL-INT  TO ACCT-CURR-BAL\n           MOVE 0 TO ACCT-CURR-CYC-CREDIT\n           MOVE 0 TO ACCT-CURR-CYC-DEBIT\n\n           REWRITE FD-ACCTFILE-REC FROM  ACCOUNT-RECORD",
      "confidence_level": "HIGH"
    },
    {
      "rule_id": "BR009",
      "rule_name": "First Account Boundary Suppression",
      "rule_category": "Decision Rule",
      "business_description": "On the very first account group boundary encountered in the run, the account update step is suppressed because no prior account has been loaded. Subsequent boundaries trigger account update processing.",
      "condition": "TRANCAT-ACCT-ID NOT= WS-LAST-ACCT-NUM",
      "action": "IF WS-FIRST-TIME NOT = 'Y' THEN PERFORM 1050-UPDATE-ACCOUNT ELSE MOVE 'N' TO WS-FIRST-TIME",
      "outcome": "Account update is skipped for the very first account boundary; WS-FIRST-TIME flag is set to 'N' for all subsequent boundaries",
      "acceptance_criteria": {
        "given": "The program encounters a new account ID in the transaction category balance file",
        "when": "WS-FIRST-TIME = 'Y' (first account boundary in the run)",
        "then": "Account update is not performed; WS-FIRST-TIME is set to 'N'"
      },
      "decision_table": [
        {
          "condition": "TRANCAT-ACCT-ID NOT= WS-LAST-ACCT-NUM AND WS-FIRST-TIME = 'Y'",
          "outcome": "Skip 1050-UPDATE-ACCOUNT; MOVE 'N' TO WS-FIRST-TIME"
        },
        {
          "condition": "TRANCAT-ACCT-ID NOT= WS-LAST-ACCT-NUM AND WS-FIRST-TIME = 'N'",
          "outcome": "PERFORM 1050-UPDATE-ACCOUNT for previous account"
        }
      ],
      "source_reference": "CBACT04C.cbl PROCEDURE DIVISION main processing loop",
      "source_snippet": "IF TRANCAT-ACCT-ID NOT= WS-LAST-ACCT-NUM\n                       IF WS-FIRST-TIME NOT = 'Y'\n                          PERFORM 1050-UPDATE-ACCOUNT\n                       ELSE\n                          MOVE 'N' TO WS-FIRST-TIME\n                       END-IF\n                       MOVE 0 TO WS-TOTAL-INT\n                       MOVE TRANCAT-ACCT-ID TO WS-LAST-ACCT-NUM",
      "confidence_level": "HIGH"
    }
  ],
  "business_capability_mapping": [
    {
      "business_capability": "Interest Calculation",
      "business_process": "Monthly Interest Computation and Posting",
      "business_function": "Compute monthly interest per transaction category balance; post interest charges to account; generate interest transaction records",
      "evidence": "Program header: 'Function : This is a interest calculator program.' (CBACT04C.cbl line 5); COMPUTE WS-MONTHLY-INT = (TRAN-CAT-BAL * DIS-INT-RATE) / 1200 (paragraph 1300-COMPUTE-INTEREST)"
    }
  ],
  "processing_inputs": [
    {
      "input_name": "TCATBAL-FILE",
      "input_type": "Indexed Sequential File (INPUT)",
      "purpose": "Source of transaction category balance records; drives the main processing loop; provides TRANCAT-ACCT-ID, TRANCAT-TYPE-CD, TRANCAT-CD, and TRAN-CAT-BAL",
      "evidence": "SELECT TCATBAL-FILE ASSIGN TO TCATBALF ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL (CBACT04C.cbl FILE-CONTROL)"
    },
    {
      "input_name": "XREF-FILE",
      "input_type": "Indexed File (INPUT, Random Access)",
      "purpose": "Card cross-reference lookup; provides XREF-CARD-NUM for populating interest transaction records",
      "evidence": "SELECT XREF-FILE ASSIGN TO XREFFILE ORGANIZATION IS INDEXED ACCESS MODE IS RANDOM (CBACT04C.cbl FILE-CONTROL)"
    },
    {
      "input_name": "DISCGRP-FILE",
      "input_type": "Indexed File (INPUT, Random Access)",
      "purpose": "Disclosure group reference file; provides DIS-INT-RATE keyed by account group, transaction type, and transaction category",
      "evidence": "SELECT DISCGRP-FILE ASSIGN TO DISCGRP ORGANIZATION IS INDEXED ACCESS MODE IS RANDOM (CBACT04C.cbl FILE-CONTROL)"
    },
    {
      "input_name": "ACCOUNT-FILE",
      "input_type": "Indexed File (I-O, Random Access)",
      "purpose": "Account master file; read to obtain ACCT-GROUP-ID and current balances; updated with computed interest and cycle reset",
      "evidence": "SELECT ACCOUNT-FILE ASSIGN TO ACCTFILE ORGANIZATION IS INDEXED ACCESS MODE IS RANDOM (CBACT04C.cbl FILE-CONTROL); OPEN I-O ACCOUNT-FILE (paragraph 0300-ACCTFILE-OPEN)"
    },
    {
      "input_name": "EXTERNAL-PARMS (PARM-DATE)",
      "input_type": "JCL Linkage Parameter",
      "purpose": "Run date passed as external parameter; used as prefix in generated transaction IDs",
      "evidence": "LINKAGE SECTION: 05 PARM-DATE PIC X(10); PROCEDURE DIVISION USING EXTERNAL-PARMS (CBACT04C.cbl)"
    }
  ],
  "processing_outputs": [
    {
      "output_name": "TRANSACT-FILE",
      "output_type": "Sequential File (OUTPUT)",
      "purpose": "Transaction output file; receives system-generated interest charge transaction records (type '01', category '05') for each applicable transaction category balance",
      "evidence": "SELECT TRANSACT-FILE ASSIGN TO TRANSACT ORGANIZATION IS SEQUENTIAL; OPEN OUTPUT TRANSACT-FILE; WRITE FD-TRANFILE-REC FROM TRAN-RECORD (CBACT04C.cbl paragraph 1300-B-WRITE-TX)"
    },
    {
      "output_name": "ACCOUNT-FILE (Updated)",
      "output_type": "Indexed File (Rewritten)",
      "purpose": "Account master records updated with total interest added to ACCT-CURR-BAL and cycle credit/debit reset to zero",
      "evidence": "REWRITE FD-ACCTFILE-REC FROM ACCOUNT-RECORD (CBACT04C.cbl paragraph 1050-UPDATE-ACCOUNT)"
    }
  ],
  "traceability_summary": {
    "total_business_rules": 9,
    "rules_with_source_evidence": 9,
    "rules_without_source_evidence": 0
  }
}
`
