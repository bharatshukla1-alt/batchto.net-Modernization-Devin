# JCL Facts: READACCT

`json
{
  "job_name": "READACCT",
  "app_name": "",
  "inventory_summary": {
    "total_steps": 2,
    "total_dd_statements_used": 10,
    "unique_datasets": 5,
    "vsam_used": true,
    "vsam_dataset_count": 1,
    "tape_dataset_count": 0,
    "temporary_dataset_count": 0,
    "gdg_plus1_used": false,
    "unique_gdg_plus1_count": 0,
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
    "utility_step_count": 1,
    "unique_utility_program_count": 0,
    "runtime_program_count": 0,
    "application_program_count": 1,
    "system_program_count": 1,
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
    "system_components": 1,
    "repository_completeness_percentage": 50,
    "jcl_complexity_score": 15,
    "jcl_complexity_rating": "LOW"
  },
  "repository_validation": {
    "repository_validation_performed": true,
    "total_components_referenced": 2,
    "components_found": 1,
    "components_not_found": 0,
    "external_components": 0,
    "system_components": 1,
    "repository_completeness_percentage": 50
  },
  "complexity_assessment": {
    "jcl_complexity_score": 15,
    "complexity_rating": "LOW",
    "complexity_drivers": [
      "2 EXEC steps",
      "1 system component",
      "5 unique datasets"
    ],
    "complexity_breakdown": {
      "structural_complexity": 4,
      "dataset_complexity": 7,
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
  "gdg_plus1_inventory": [],
  "interface_inventory": [],
  "niche_program_calls": [],
  "entry_programs": [
    {
      "program_name": "CBACT01C",
      "step_name": "STEP05",
      "invocation_type": "PGM",
      "component_classification": "APPLICATION_PROGRAM",
      "entry_program": true,
      "source_found": true,
      "reference_location": "STEP05 EXEC PGM=CBACT01C",
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
      "step_name": "PREDEL",
      "program": "IEFBR14",
      "proc": "",
      "conditions": "",
      "entry_program": false,
      "niche_technology": {
        "niche_detected": false,
        "technology_type": "",
        "detection_source": ""
      },
      "component_references": [
        {
          "component_name": "IEFBR14",
          "component_type": "PROGRAM",
          "component_classification": "SYSTEM_COMPONENT",
          "reference_type": "EXEC PGM",
          "reference_location": "PREDEL EXEC PGM=IEFBR14",
          "source_found": false,
          "component_status": "SYSTEM"
        }
      ],
      "external_interfaces": [],
      "datasets": [
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "DD01",
          "dataset_name_original": "AWS.M2.CARDDEMO.ACCTDATA.PSCOMP",
          "dataset_name": "AWS.M2.CARDDEMO.ACCTDATA.PSCOMP",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "",
          "role_io": "",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "(MOD,DELETE,DELETE)",
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
          "ddname": "DD02",
          "dataset_name_original": "AWS.M2.CARDDEMO.ACCTDATA.ARRYPS",
          "dataset_name": "AWS.M2.CARDDEMO.ACCTDATA.ARRYPS",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "",
          "role_io": "",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "(MOD,DELETE,DELETE)",
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
          "ddname": "DD03",
          "dataset_name_original": "AWS.M2.CARDDEMO.ACCTDATA.VBPS",
          "dataset_name": "AWS.M2.CARDDEMO.ACCTDATA.VBPS",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "",
          "role_io": "",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "(MOD,DELETE,DELETE)",
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
        "utility_used": true,
        "utility_name": "IEFBR14",
        "utility_category": "DATASET_MANAGEMENT",
        "utility_subcategory": "DELETE",
        "sysin_content": ""
      }
    },
    {
      "step_name": "STEP05",
      "program": "CBACT01C",
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
          "component_name": "CBACT01C",
          "component_type": "PROGRAM",
          "component_classification": "APPLICATION_PROGRAM",
          "reference_type": "EXEC PGM",
          "reference_location": "STEP05 EXEC PGM=CBACT01C",
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
          "ddname": "OUTFILE",
          "dataset_name_original": "AWS.M2.CARDDEMO.ACCTDATA.PSCOMP",
          "dataset_name": "AWS.M2.CARDDEMO.ACCTDATA.PSCOMP",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "SEQ",
          "role_io": "OUTPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "(NEW,CATLG,DELETE)",
          "dcb": {
            "recfm": "FB",
            "lrecl": "107",
            "blksize": "0"
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "(CYL,(1,2),RLSE)",
          "unit": "SYSAD",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "ARRYFILE",
          "dataset_name_original": "AWS.M2.CARDDEMO.ACCTDATA.ARRYPS",
          "dataset_name": "AWS.M2.CARDDEMO.ACCTDATA.ARRYPS",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "SEQ",
          "role_io": "OUTPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "(NEW,CATLG,DELETE)",
          "dcb": {
            "recfm": "FB",
            "lrecl": "110",
            "blksize": "0"
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "(CYL,(1,2),RLSE)",
          "unit": "SYSAD",
          "volume": ""
        },
        {
          "dd_used": true,
          "dd_used_reason": "DSN present",
          "ddname": "VBRCFILE",
          "dataset_name_original": "AWS.M2.CARDDEMO.ACCTDATA.VBPS",
          "dataset_name": "AWS.M2.CARDDEMO.ACCTDATA.VBPS",
          "gdg_plus1": false,
          "gdg_name": "",
          "symbolic_resolution": {},
          "type": "SEQ",
          "role_io": "OUTPUT",
          "temp_flag": false,
          "rpt_ctl": "",
          "disposition": "(NEW,CATLG,DELETE)",
          "dcb": {
            "recfm": "VB",
            "lrecl": "84",
            "blksize": "0"
          },
          "sms": {
            "storclas": "",
            "mgmtclas": "",
            "dataclas": ""
          },
          "space": "(CYL,(1,2),RLSE)",
          "unit": "SYSAD",
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
