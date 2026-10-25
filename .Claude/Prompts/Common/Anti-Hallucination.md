# Anti-Hallucination & Evidence Anchoring Rule

1. NEVER invent, infer, or hallucinate business logic, field definitions, program calls, or file structures that cannot be directly proven from the source code, copybooks, or JCL provided.
2. Every business rule, data structure, or control flow statement MUST cite direct evidence:
   - `source_file`: relative repository path (e.g., `cbl/CBACT04C.cbl`)
   - `paragraph_or_section`: exact paragraph name (e.g., `1050-UPDATE-ACCOUNT`)
   - `line_range_or_snippet`: exact code line or snippet
3. If information is missing or ambiguous, you MUST record a Gap (`GAP-XXX`) with severity and required SME decision instead of making assumptions.
4. No target-specific assumptions (e.g., assuming PySpark, Python, or .NET during extraction). Extract semantic facts only.
5. All file paths must be relative to the repository workspace root. NEVER hardcode absolute local paths or user home directories.
