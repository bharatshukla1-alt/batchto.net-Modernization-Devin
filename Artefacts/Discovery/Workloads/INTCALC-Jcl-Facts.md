# JCL Facts: INTCALC

`json
{
  "job_name": "INTCALC",
  "app_name": "",
  "inventory_summary": {
    "total_steps": 1,
    "total_dd_statements_used": 9,
    "unique_datasets": 7,
    "vsam_used": true,
    "vsam_dataset_count": 5,
    "tape_dataset_count": 0,
    "temporary_dataset_count": 0,
    "gdg_plus1_used": true,
    "unique_gdg_plus1_count": 1,
    "business_context_classified": false,
    "db2_used": false,
    "ims_used": false,
    "cics_used": false,
    "external_interface_used": false,
    "external_interface_count": 0,
    "upstream_interface_count": 0,
    "downstream_interface_count": 0,
    "restartable_steps": 0,
    "entry_program_count": 1,
    "utility_step_count": 0,
    "unique_utility_program_count": 0,
    "runtime_program_count": 0,
    "application_program_count": 1,
    "system_program_count": 0,
    "proc_used": false,
    "symbolics_used": false,
    "unresolved_reference_count": 0,
    "niche_program_count": 0,
    "niche_technology_used": false,
    "niche_technology_types": [],
    "niche_technology_detection_count": 0,
    "niche_technology_risk_indicator": "",
    "missing_component_detected": false,
    "missing_component_count": 0,
    "components_found": 1,
    "components_not_found": 0,
    "external_components": 0,
    "system_components": 0,
    "repository_completeness_percentage": 100,
    "jcl_complexity_score": 25,
    "jcl_complexity_rating": "LOW"
  },
  "repository_validation": {
    "repository_validation_performed": true,
    "total_components_referenced": 1,
    "components_found": 1,
    "components_not_found": 0,
    "external_components": 0,
    "system_components": 0,
    "repository_completeness_percentage": 100
  },
  "complexity_assessment": {
    "jcl_complexity_score": 25,
    "complexity_rating": "LOW",
    "complexity_drivers": [
      "1 EXEC step",
      "5 VSAM datasets",
      "1 GDG(+1) dataset"
    ],
    "complexity_breakdown": {
      "structural_complexity": 2,
      "dataset_complexity": 19,
      "processing_complexity": 4,
      "integration_complexity": 0,
      "operational_complexity": 0
    }
  },
  "business_context": {
    "business_capability": "",
    "business_process": "",
    "business_function": "",
    "classification_confidence": "NONE",
    "classification_evidence": []
  },
  "gdg_plus1_inventory": [
    {
      "gdg_name": "AWS.M2.CARDDEMO.SYSTRAN"
    }
  ],
  "interface_inventory": [],
  "niche_program_calls": [],
  "entry_programs": [
    {
      "program_name": "CBACT04C",
      "step_name": "STEP15",
      "invocation_type": "PGM",
      "component_classification": "APPLICATION_PROGRAM",
      "entry_program": true,
      "source_found": true,
      "reference_location": "STEP15 EXEC PGM=CBACT04C",
      "component_status": "FOUND"
    }
  ],
  "component_not_found": [],
  "unresolved_references": [],
  "symbolics": {
    "set_variables": [],
    "referenced_variables": []
  },
  "steps": [
    {
      "step_name": "STEP15",
      "program": "CBACT04C",
      "proc": "",
      "conditions": "",
      "entry_program": true,
      "niche_technology": {
        "niche_detected": false,
        "technology_type": "",
        "detection_source": ""
      },
      "component_references": [
        {
          "component_name": "CBACT04C",
          "component_type": "PROGRAM",
          "component_classification": "APPLICATION_PROGRAM",
          "reference_type": "EXEC PGM",
          "reference_location": "STEP15 EXEC PGM=CBACT04C",
          "source_found": true,
          "component_status": "FOUND"
        }
      ],
      "external_interfaces": [],
      "datasets": [
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "STEPLIB",
          "dataset_name_original": "AWS.M2.CARDDEMO.LOADLIB",
          "dataset_name": "AWS.M2.CARDDEMO.LOADLIB",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "",
          "role_io": "INPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "SHR",
          "dcb": {
            "recfm": "",
            "lrecl": "",
            "blksize": ""
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "",
          "unit": "",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "SYSOUT present",
          "ddname": "SYSPRINT",
          "dataset_name_original": "",
          "dataset_name": "",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "SYSOUT",
          "role_io": "OUTPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "",
          "dcb": {
            "recfm": "",
            "lrecl": "",
            "blksize": ""
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "",
          "unit": "",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "SYSOUT present",
          "ddname": "SYSOUT",
          "dataset_name_original": "",
          "dataset_name": "",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "SYSOUT",
          "role_io": "OUTPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "",
          "dcb": {
            "recfm": "",
            "lrecl": "",
            "blksize": ""
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "",
          "unit": "",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "TCATBALF",
          "dataset_name_original": "AWS.M2.CARDDEMO.TCATBALF.VSAM.KSDS",
          "dataset_name": "AWS.M2.CARDDEMO.TCATBALF.VSAM.KSDS",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "VSAM",
          "role_io": "INPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "SHR",
          "dcb": {
            "recfm": "",
            "lrecl": "",
            "blksize": ""
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "",
          "unit": "",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "XREFFILE",
          "dataset_name_original": "AWS.M2.CARDDEMO.CARDXREF.VSAM.KSDS",
          "dataset_name": "AWS.M2.CARDDEMO.CARDXREF.VSAM.KSDS",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "VSAM",
          "role_io": "INPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "SHR",
          "dcb": {
            "recfm": "",
            "lrecl": "",
            "blksize": ""
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "",
          "unit": "",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "XREFFIL1",
          "dataset_name_original": "AWS.M2.CARDDEMO.CARDXREF.VSAM.AIX.PATH",
          "dataset_name": "AWS.M2.CARDDEMO.CARDXREF.VSAM.AIX.PATH",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "VSAM",
          "role_io": "INPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "SHR",
          "dcb": {
            "recfm": "",
            "lrecl": "",
            "blksize": ""
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "",
          "unit": "",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "ACCTFILE",
          "dataset_name_original": "AWS.M2.CARDDEMO.ACCTDATA.VSAM.KSDS",
          "dataset_name": "AWS.M2.CARDDEMO.ACCTDATA.VSAM.KSDS",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "VSAM",
          "role_io": "INPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "SHR",
          "dcb": {
            "recfm": "",
            "lrecl": "",
            "blksize": ""
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "",
          "unit": "",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "DISCGRP",
          "dataset_name_original": "AWS.M2.CARDDEMO.DISCGRP.VSAM.KSDS",
          "dataset_name": "AWS.M2.CARDDEMO.DISCGRP.VSAM.KSDS",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "VSAM",
          "role_io": "INPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "SHR",
          "dcb": {
            "recfm": "",
            "lrecl": "",
            "blksize": ""
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "",
          "unit": "",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "TRANSACT",
          "dataset_name_original": "AWS.M2.CARDDEMO.SYSTRAN(+1)",
          "dataset_name": "AWS.M2.CARDDEMO.SYSTRAN(+1)",
          "gdg_plus1": true,
          "gdg_name": "AWS.M2.CARDDEMO.SYSTRAN",
          "symbolic_resolution": {},
          "type": "GDG",
          "role_io": "OUTPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "(NEW,CATLG,DELETE)",
          "dcb": {
            "recfm": "F",
            "lrecl": "350",
            "blksize": "0"
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "(CYL,(1,1),RLSE)",
          "unit": "SYSDA",
          "volume": ""
        }
      ],
      "db2": {
        "db2_used": false,
        "program": "",
        "plan": "",
        "package": "",
        "subsystem": "",
        "libraries": [],
        "utility_type": "",
        "sql_statements": ""
      },
      "ims": {
        "ims_used": false,
        "program": "",
        "psb": "",
        "region_type": "",
        "dbd_names": [],
        "pcb_names": [],
        "libraries": {
          "psblib": "",
          "dbdlib": "",
          "acblib": "",
          "reslib": ""
        },
        "datasets": [],
        "control_statements": []
      },
      "cics": {
        "cics_used": false,
        "cics_resources": [],
        "cics_evidence": []
      },
      "restart_checkpoint": {
        "restartable": false,
        "checkpoint_used": false,
        "checkpoint_reference": "",
        "restart_parameter": "",
        "restart_evidence": []
      },
      "utility": {
        "utility_used": false,
        "utility_name": "",
        "utility_category": "",
        "utility_subcategory": "",
        "sysin_content": ""
      }
    }
  ]
}
`
