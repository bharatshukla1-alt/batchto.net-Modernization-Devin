# Program Facts: CBACT02C

`json
{
  "app_name": "",
  "program_information": {
    "program_name": "CBACT02C",
    "language": "COBOL",
    "program_type": "APPLICATION",
    "execution_model": "BATCH",
    "entry_program": true,
    "author": "AWS",
    "source_file": "CBACT02C.cbl"
  },
  "inventory_summary": {
    "copybook_count": 1,
    "file_count": 1,
    "table_count": 0,
    "program_call_count": 1,
    "restartable_programs": 0,
    "checkpoint_enabled_programs": 0,
    "unresolved_reference_count": 0,
    "db2_used": false,
    "imsdb_used": false,
    "idms_used": false,
    "external_interface_used": false,
    "external_interface_count": 0,
    "upstream_interface_count": 0,
    "downstream_interface_count": 0,
    "niche_technology_used": false,
    "niche_dependency_count": 0,
    "pointer_used": false,
    "pointer_count": 0,
    "business_rule_count": 0,
    "cyclomatic_complexity": 13,
    "complexity_rating": "MEDIUM",
    "business_context_classified": false
  },
  "repository_validation": {
    "repository_validation_performed": true,
    "total_components_referenced": 3,
    "components_found": 2,
    "components_not_found": 0,
    "external_components": 0,
    "system_components": 1,
    "programs_found": 0,
    "programs_not_found": 0,
    "unresolved_reference_count": 0,
    "copybooks_found": 1,
    "copybooks_not_found": 0,
    "copybook_repository_completeness": 100,
    "repository_completeness_percentage": 66.67
  },
  "business_context": {
    "business_capability": "",
    "business_process": "",
    "business_function": "",
    "classification_confidence": "NONE",
    "classification_evidence": []
  },
  "interface_inventory": [],
  "niche_technology_used": false,
  "niche_dependencies": [],
  "pointer_analysis": {
    "pointer_used": false,
    "pointer_count": 0,
    "pointer_risk_level": "",
    "pointer_operations": [],
    "pointer_definitions": []
  },
  "copybook_dependencies": [
    {
      "copybook_name": "CVACT02Y",
      "copybook_type": "COPY",
      "usage_section": "WORKING_STORAGE",
      "repository_status": "FOUND",
      "reference_location": "COPY CVACT02Y"
    }
  ],
  "file_dependencies": [
    {
      "logical_file_name": "CARDFILE-FILE",
      "runtime_dd_name": "CARDFILE",
      "file_type": "",
      "access_mode": "INPUT",
      "file_relationship": "INBOUND",
      "organization": "INDEXED",
      "multiple_record_layouts": false,
      "variable_length": false,
      "data_movement_type": "SOURCE",
      "crud_operations": [
        "READ"
      ]
    }
  ],
  "database_dependencies": {
    "db2": {
      "db2_used": false,
      "tables": [],
      "views": [],
      "aliases": [],
      "synonyms": [],
      "stored_procedures": [],
      "user_defined_functions": [],
      "packages": [],
      "plans": [],
      "schemas": [],
      "dynamic_sql_used": false,
      "dynamic_sql_count": 0,
      "sql_operation_counts": {
        "select_count": 0,
        "insert_count": 0,
        "update_count": 0,
        "delete_count": 0,
        "merge_count": 0,
        "cursor_count": 0
      }
    },
    "imsdb": {
      "imsdb_used": false,
      "dbds": [],
      "psbs": [],
      "pcbs": [],
      "segments": []
    },
    "idms": {
      "idms_used": false,
      "idms_records": [],
      "idms_areas": [],
      "idms_sets": [],
      "idms_schemas": []
    }
  },
  "transaction_dependencies": {
    "cics_used": false,
    "ims_dc_used": false,
    "transactions": [],
    "mapsets": [],
    "maps_sent": [],
    "maps_received": [],
    "ts_queues": [],
    "td_queues": [],
    "cics_files": [],
    "cics_programs": [],
    "channels": [],
    "containers": [],
    "container_operations": [],
    "commarea_analysis": {
      "commarea_used": false,
      "commarea_reference": "",
      "commarea_length": 0
    },
    "cics_navigation": [],
    "cics_commands": []
  },
  "program_calls": [
    {
      "called_program": "CEE3ABD",
      "call_type": "CALL",
      "static_dynamic": "STATIC",
      "reference_location": "CALL 'CEE3ABD' USING ABCODE, TIMING"
    }
  ],
  "business_rules": [],
  "error_handling": [
    {
      "error_type": "FILE STATUS",
      "error_handling_present": true,
      "handling_action": "DISPLAY; MOVE ERROR CODE; ABEND",
      "reference_location": "CARDFILE-STATUS checks with PERFORM 9999-ABEND-PROGRAM"
    },
    {
      "error_type": "ABEND",
      "error_handling_present": true,
      "handling_action": "ABEND",
      "reference_location": "9999-ABEND-PROGRAM CALL 'CEE3ABD' USING ABCODE, TIMING"
    }
  ],
  "checkpoint_restart": {
    "restartable": false,
    "checkpoint_used": false,
    "restart_logic_present": false,
    "checkpoint_type": "",
    "restart_evidence": []
  },
  "complexity_assessment": {
    "cyclomatic_complexity": 13,
    "complexity_rating": "MEDIUM",
    "complexity_drivers": [
      "IF count: 11",
      "PERFORM count: 11",
      "CALL count: 1",
      "Copybooks: 1",
      "REDEFINES: 1"
    ],
    "complexity_breakdown": {
      "structural_complexity": 23,
      "data_complexity": 3,
      "integration_complexity": 1,
      "niche_technology_complexity": 0,
      "operational_complexity": 5
    }
  },
  "component_not_found": [],
  "unresolved_references": [],
  "modernization_inventory": {
    "db2_used": false,
    "imsdb_used": false,
    "idms_used": false,
    "cics_used": false,
    "commarea_used": false,
    "tsq_used": false,
    "tdq_used": false,
    "channels_used": false,
    "containers_used": false,
    "mq_used": false,
    "external_interface_used": false,
    "upstream_interfaces": 0,
    "downstream_interfaces": 0,
    "niche_technology_used": false,
    "niche_dependency_count": 0,
    "pointer_used": false,
    "pointer_count": 0,
    "pointer_arithmetic_present": false,
    "bll_cell_addressing_present": false,
    "dynamic_calls": false,
    "assembler_dependency": false,
    "restartable": false,
    "checkpoint_used": false,
    "unsupported_constructs": []
  }
}
`
