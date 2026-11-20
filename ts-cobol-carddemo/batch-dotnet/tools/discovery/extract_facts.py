#!/usr/bin/env python3
"""Phase 1 (Legacy Discovery) extractor for the batch-dotnet workbench.

Deterministically reads the estate under app/ (read-only) and writes the
discovery artefacts with line-level evidence:

  Artefacts/Discovery/Workloads/<JOB>-Jcl-Facts.md
  Artefacts/Discovery/Workloads/Batch-Schedule-Graph.json
  Artefacts/Discovery/Programs/<PGM>-Program-Facts.md
  Artefacts/Discovery/Rules/<PGM>-BRE.md

Business rules are declared below as (id, statement, program, regex); the
regex is resolved to a line number at run time and the run fails when a rule
cannot be evidenced, so no rule can exist without a citation.
"""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
APP = ROOT / "app"
OUT = ROOT / "batch-dotnet" / "Artefacts" / "Discovery"

DB2_APP = APP / "app-transaction-type-db2"
DB2_PROGRAMS = {"COBTUPDT", "CBTRNDYN"}
DB2_JOBS = {"MNTTRDB2", "MNTTRDYN"}


def cbl_path(pgm: str) -> Path:
    return (DB2_APP if pgm in DB2_PROGRAMS else APP) / "cbl" / f"{pgm}.cbl"


def jcl_path(job: str) -> Path:
    return (DB2_APP if job in DB2_JOBS else APP) / "jcl" / f"{job}.jcl"


WORKLOADS = {
    "READACCT": ["CBACT01C"],
    "READCARD": ["CBACT02C"],
    "INTCALC": ["CBACT04C"],
    "POSTTRAN": ["CBTRN02C"],
    "TRANREPT": ["CBTRN03C"],
    "MNTTRDB2": ["COBTUPDT"],
    "MNTTRDYN": ["CBTRNDYN"],
}

RULES = {
    "COBTUPDT": [
        ("BR-TUP-001", "OPEN FILE OK / NOT OK is displayed and the read loop runs either way (0001-OPEN-FILES falls through).", r"DISPLAY 'OPEN FILE OK'"),
        ("BR-TUP-002", "Record type A inserts, U updates, D deletes, '*' is a comment; any other type is an error.", r"EVALUATE INPUT-REC-TYPE"),
        ("BR-TUP-003", "INSERT/UPDATE bind the 2-character type code and the full 50-character description (trailing spaces stored).", r":INPUT-REC-DESC"),
        ("BR-TUP-004", "SQLCODE +100 on UPDATE/DELETE would report 'No records found.', but Open COBOL ESQL returns 0 when no row matches, so a missing row reports success (fixture t05).", r"WHEN SQLCODE = \+100"),
        ("BR-TUP-005", "An error DISPLAYs WS-RETURN-MSG (never cleared), sets RETURN-CODE 4 and processing continues with the next record.", r"MOVE 4 TO RETURN-CODE"),
        ("BR-TUP-006", "Negative SQLCODE message: 'Error accessing: TRANSACTION_TYPE table. SQLCODE:' followed by SQLCODE edited ----9.", r"' TRANSACTION_TYPE table\. SQLCODE:'"),
    ],
    "CBTRNDYN": [
        ("BR-DYN-001", "DYNSQL lines starting with '--' are skipped; columns 1-72 of the other lines are concatenated, each followed by one blank, into an 800-byte statement.", r"IF DYNSQL-REC\(1:2\) NOT = '--'"),
        ("BR-DYN-002", "The statement is PREPAREd and EXECUTEd; the EXECUTE SQLCODE is displayed (statement errors surface at EXECUTE under ocesql).", r"EXECUTE DYNSTMT"),
        ("BR-DYN-003", "A failed PREPARE or EXECUTE sets RETURN-CODE 8 and processing continues.", r"MOVE 8 TO RETURN-CODE"),
        ("BR-DYN-004", "The program named in PARMS is CALLed dynamically (CALL identifier); COBTUPDT's STOP RUN ends the run unit, so 'CBTRNDYN: END' is not reached.", r"CALL WS-PGM-NAME"),
    ],
    "CBACT04C": [
        ("BR-INT-001", "Interest is computed per TCATBALF record as (TRAN-CAT-BAL * DIS-INT-RATE) / 1200 with no ROUNDED phrase: the result is truncated to 2 decimals.", r"=\s*\(\s*TRAN-CAT-BAL\s*\*\s*DIS-INT-RATE\)\s*/\s*1200"),
        ("BR-INT-002", "Interest is only computed when the disclosure-group rate is non-zero.", r"IF\s+DIS-INT-RATE\s+NOT\s*=\s*0"),
        ("BR-INT-003", "When the account's disclosure group is missing (status 23) the DEFAULT group is read.", r"MOVE 'DEFAULT'\s+TO FD-DIS-ACCT-GROUP-ID"),
        ("BR-INT-004", "The accumulated interest is added to ACCT-CURR-BAL when the account changes.", r"ADD WS-TOTAL-INT\s+TO ACCT-CURR-BAL"),
        ("BR-INT-005", "Cycle credit and cycle debit are reset to zero on account update.", r"MOVE 0\s+TO ACCT-CURR-CYC-CREDIT"),
        ("BR-INT-006", "The account update is triggered only by an account break (first record suppressed); the last account at end-of-file is never updated (legacy quirk).", r"IF\s+WS-FIRST-TIME NOT = 'Y'"),
        ("BR-INT-007", "Interest transaction id = PARM date (10) + 6-digit running suffix.", r"STRING PARM-DATE"),
        ("BR-INT-008", "Interest transactions carry type 01, category 05, source 'System'.", r"MOVE 'System'\s+TO TRAN-SOURCE"),
        ("BR-INT-009", "Description is 'Int. for a/c ' followed by the account id.", r"'Int\. for a/c '"),
        ("BR-INT-010", "Origin and processing timestamps are the DB2-format current timestamp (frozen clock in the harness).", r"MOVE DB2-FORMAT-TS\s+TO TRAN-ORIG-TS"),
        ("BR-INT-011", "The card number on the interest transaction comes from the XREF alternate index keyed by account id.", r"KEY IS FD-XREF-ACCT-ID"),
        ("BR-INT-012", "1400-COMPUTE-FEES is a placeholder with no logic (fees not implemented).", r"1400-COMPUTE-FEES\."),
    ],
    "CBTRN02C": [
        ("BR-PST-001", "A daily transaction whose card number is not in XREF is rejected with reason 100 'INVALID CARD NUMBER FOUND'.", r"MOVE 100 TO WS-VALIDATION-FAIL-REASON"),
        ("BR-PST-002", "A transaction whose account is missing is rejected with reason 101 'ACCOUNT RECORD NOT FOUND'.", r"MOVE 101 TO WS-VALIDATION-FAIL-REASON"),
        ("BR-PST-003", "Credit limit check uses cycle amounts: limit >= cycle credit - cycle debit + amount, else reason 102 'OVERLIMIT TRANSACTION'.", r"COMPUTE WS-TEMP-BAL = ACCT-CURR-CYC-CREDIT"),
        ("BR-PST-004", "Expiry check: account expiration date >= origin date (first 10 chars), else reason 103; 103 overwrites 102 when both fail.", r"IF ACCT-EXPIRAION-DATE >= DALYTRAN-ORIG-TS \(1:10\)"),
        ("BR-PST-005", "Rejected transactions are written to DALYREJS as the 350-byte record plus an 80-byte trailer (reason 9(4) + description X(76)).", r"MOVE WS-VALIDATION-TRAILER TO VALIDATION-TRAILER"),
        ("BR-PST-006", "Missing (account, type, category) balance records are created with the transaction amount.", r"MOVE 'Y' TO WS-CREATE-TRANCAT-REC"),
        ("BR-PST-007", "Existing category balances are incremented by the transaction amount.", r"ADD DALYTRAN-AMT TO TRAN-CAT-BAL"),
        ("BR-PST-008", "The amount is added to ACCT-CURR-BAL; non-negative amounts to cycle credit, negative amounts to cycle debit.", r"IF DALYTRAN-AMT >= 0"),
        ("BR-PST-009", "Posted transactions get PROC-TS = current DB2-format timestamp.", r"MOVE DB2-FORMAT-TS TO TRAN-PROC-TS"),
        ("BR-PST-010", "TRANSACT is opened OUTPUT: the posted-transaction file is replaced by this run's postings.", r"OPEN OUTPUT TRANSACT-FILE"),
        ("BR-PST-011", "Return code 4 when at least one transaction was rejected.", r"MOVE 4 TO RETURN-CODE"),
    ],
    "CBTRN03C": [
        ("BR-RPT-001", "Report period is read from DATEPARM (start date, filler, end date).", r"READ DATE-PARMS-FILE INTO WS-DATEPARM-RECORD"),
        ("BR-RPT-002", "A transaction outside the period triggers NEXT SENTENCE, which leaves the whole PERFORM loop (no totals written) - legacy quirk.", r"NEXT SENTENCE"),
        ("BR-RPT-003", "A change of card number writes the account total of the previous card (not before the first).", r"PERFORM 1120-WRITE-ACCOUNT-TOTALS"),
        ("BR-RPT-004", "Every line counter multiple of the page size (20) writes page totals and new headers.", r"FUNCTION MOD\(WS-LINE-COUNTER, WS-PAGE-SIZE\) = 0"),
        ("BR-RPT-005", "Page totals roll into the grand total and reset; account totals reset but do not reset the page total.", r"ADD WS-PAGE-TOTAL TO WS-GRAND-TOTAL"),
        ("BR-RPT-006", "At end-of-file the last TRAN-AMT is added again to page and account totals before page and grand totals are written; the final account total is never written - legacy quirk.", r"DISPLAY 'TRAN-AMT ' TRAN-AMT"),
        ("BR-RPT-007", "XREF, TRANTYPE and TRANCATG lookups that fail abend the program (U999).", r"DISPLAY 'INVALID CARD NUMBER : '"),
        ("BR-RPT-008", "The detail line carries id, account, type code/desc, category code/desc, source and the edited amount (-ZZZ,ZZZ,ZZZ.ZZ).", r"MOVE TRAN-AMT TO TRAN-REPORT-AMT"),
    ],
    "CBACT01C": [
        ("BR-RAC-001", "Each account is displayed field by field on SYSOUT.", r"DISPLAY 'ACCT-ID\s*:'"),
        ("BR-RAC-002", "OUTFILE carries the account with CURR-CYC-DEBIT packed (COMP-3); 2525.00 is moved only when the debit is zero.", r"MOVE 2525\.00 TO OUT-ACCT-CURR-CYC-DEBIT"),
        ("BR-RAC-003", "The reissue date is reformatted by the assembler routine COBDATFT (type 2 in, type 2 out: YYYY-MM-DD -> YYYYMMDD).", r"CALL 'COBDATFT'"),
        ("BR-RAC-004", "ARRYFILE: occurrence 1 and 2 copy the balance with debits 1005.00 / 1525.00; occurrence 3 is -1025.00 / -2500.00.", r"MOVE -1025\.00 TO ARR-ACCT-CURR-BAL\(3\)"),
        ("BR-RAC-005", "VBRCFILE receives a 12-byte and a 39-byte variable record per account.", r"MOVE 39 TO WS-RECD-LEN"),
    ],
    "CBACT02C": [
        ("BR-RCD-001", "Every card record is read sequentially by card number and displayed on SYSOUT.", r"DISPLAY CARD-RECORD"),
    ],
}

GAPS = [
    ("GAP-001", "CBACT01C", "COBDATFT is HLASM (app/asm/COBDATFT.asm) and is not built into the GnuCOBOL loadlib, so READACCT cannot run on the harness; the .NET port follows the assembler source (VALIDIN2 path) and is proven by unit tests, not by golden files.", "Medium"),
    ("GAP-002", "CBACT01C", "OUT-ACCT-CURR-CYC-DEBIT is only moved when the debit is zero; otherwise the FD buffer keeps the previous record's value. The .NET port reproduces 'carry previous value' (initial value 0).", "Low"),
    ("GAP-003", "CBACT04C", "XREF alternate index (account id) with duplicates: VSAM returns the first card in AIX order; the port returns the lowest card number for the account.", "Low"),
    ("GAP-004", "ALL", "Plugin numeric convention prescribes MidpointRounding.AwayFromZero; the legacy source has no ROUNDED and golden fixtures prove truncation (i02-tie-half-cent). Legacy truncation is kept (ADR-003).", "High - resolved by ADR"),
    ("GAP-006", "COBTUPDT,CBTRNDYN", "SQLCODEs come from Open COBOL ESQL over PostgreSQL, not Db2: unique violation -403, any other error -400, no row found 0 (+100 never raised). Under PostgreSQL a failed statement aborts the whole unit of work (d03: COBTUPDT's later UPDATE also fails); Db2 would roll back only the failed statement. The port reproduces the golden (PostgreSQL) behaviour; a savepoint-per-statement policy is a migration decision (ADR-007).", "High - decision needed"),
    ("GAP-007", "CBTRNDYN", "After CALL COBTUPDT the run unit ends at COBTUPDT's STOP RUN with COBTUPDT's RETURN-CODE, so CBTRNDYN's RC 8 from a failed EXECUTE is lost (d03 ends RC 4). Kept as observed (ADR-007).", "Medium"),
    ("GAP-005", "TRANREPT", "SORT is DFSORT on z/OS; ordering of equal keys is not stated (no EQUALS). The harness and port use a stable sort.", "Low"),
]


def lines(path: Path) -> list[str]:
    return path.read_text(encoding="latin-1").splitlines()


def cite(path: Path, pattern: str) -> int:
    rx = re.compile(pattern)
    for i, line in enumerate(lines(path), 1):
        body = line[6:72] if path.suffix == ".cbl" else line
        if len(line) > 6 and line[6:7] == "*" and path.suffix == ".cbl":
            continue
        if rx.search(" ".join(body.split())):
            return i
    raise SystemExit(f"no evidence for /{pattern}/ in {path}")


def rel(path: Path) -> str:
    return str(path.relative_to(ROOT))


def parse_jcl(job: str) -> dict:
    path = jcl_path(job)
    steps, cur, dd = [], None, None
    for n, raw in enumerate(lines(path), 1):
        if raw.startswith("//*"):
            continue
        m = re.match(r"//(\S+)\s+EXEC\s+(PGM|PROC)=([A-Z0-9#@$]+)(.*)", raw)
        if m:
            parm = re.search(r"PARM='([^']*)'", raw)
            cur = {"step": m.group(1), "kind": m.group(2), "target": m.group(3), "line": n,
                   "parm": parm.group(1) if parm else None, "dds": []}
            steps.append(cur)
            continue
        if cur is not None and (dsn := re.search(r"RUN\s+PROGRAM\(([A-Z0-9#@$]+)\)", raw)):
            cur["dsn_program"], cur["dsn_line"] = dsn.group(1), n
        m = re.match(r"//(\S+)\s+DD\s+(.*)", raw)
        if m and cur is not None:
            dd = {"dd": m.group(1), "line": n, "dsn": None, "disp": None, "dcb": None, "sysout": "SYSOUT=" in raw or "SYSOUT=" in m.group(2)}
            cur["dds"].append(dd)
        if dd is not None and raw.startswith("//"):
            for key in ("DSN", "DISP", "DCB"):
                mm = re.search(rf"{key}=(\([^)]*\)|[^,\s]+)", raw)
                if mm and dd.get(key.lower()) is None:
                    dd[key.lower()] = mm.group(1)
        if raw.startswith("/*") or (raw.startswith("//") and "EXEC" in raw):
            dd = None
    return {"job": job, "path": rel(path), "steps": steps}


def parse_program(pgm: str) -> dict:
    path = cbl_path(pgm)
    src = lines(path)
    facts = {"program": pgm, "path": rel(path), "files": [], "copybooks": [], "paragraphs": [], "calls": [], "io": [], "displays": 0}
    for n, raw in enumerate(src, 1):
        if len(raw) > 6 and raw[6] == "*":
            continue
        body = raw[6:72]
        if m := re.search(r"SELECT\s+(\S+)\s+ASSIGN\s+TO\s+(\S+)", body):
            facts["files"].append({"name": m.group(1), "dd": m.group(2).rstrip("."), "line": n})
        if m := re.search(r"ORGANIZATION\s+IS\s+(\S+)", body):
            facts["files"][-1]["org"] = m.group(1).rstrip(".")
        if m := re.search(r"ACCESS\s+MODE\s+IS\s+(\S+)", body):
            facts["files"][-1]["access"] = m.group(1).rstrip(".")
        if m := re.search(r"RECORD\s+KEY\s+IS\s+(\S+)", body):
            facts["files"][-1]["key"] = m.group(1).rstrip(".")
        if m := re.search(r"ALTERNATE\s+RECORD\s+KEY\s+IS\s+(\S+)", body):
            facts["files"][-1]["altkey"] = m.group(1).rstrip(".")
        if m := re.search(r"\bCOPY\s+(\S+?)\.?\s*$", body.strip()):
            facts["copybooks"].append({"name": m.group(1).rstrip("."), "line": n})
        if re.match(r"^ {1,4}([0-9A-Z][0-9A-Z-]*)\.\s*$", body) and not body.strip().startswith(("EXIT", "GOBACK")):
            facts["paragraphs"].append({"name": body.strip().rstrip("."), "line": n})
        if m := re.search(r"CALL\s+'([^']+)'", body):
            facts["calls"].append({"target": m.group(1), "line": n})
        if m := re.search(r"\b(OPEN\s+(?:INPUT|OUTPUT|I-O|EXTEND)|READ|WRITE|REWRITE|DELETE|START)\s+([A-Z0-9-]+)", body):
            facts["io"].append({"verb": " ".join(m.group(1).split()), "target": m.group(2), "line": n})
        if "DISPLAY" in body:
            facts["displays"] += 1
    return facts


def exe(s: dict) -> str:
    dsn = f" (DSN RUN PROGRAM({s['dsn_program']}) L{s['dsn_line']})" if s.get("dsn_program") else ""
    return f"{s['kind']}={s['target']}{dsn}"


def write_jcl_facts(info: dict) -> None:
    out = [f"# {info['job']} - JCL facts", "", f"Source: `{info['path']}` (read-only estate).", "",
           "| Step | Line | Program/Proc | PARM | DD | DSN | DISP | DCB |", "|---|---|---|---|---|---|---|---|"]
    for s in info["steps"]:
        if not s["dds"]:
            out.append(f"| {s['step']} | L{s['line']} | {exe(s)} | {s['parm'] or ''} | | | | |")
        for i, d in enumerate(s["dds"]):
            head = f"| {s['step']} | L{s['line']} | {exe(s)} | {s['parm'] or ''} " if i == 0 else "| | | | "
            out.append(f"{head}| {d['dd']} (L{d['line']}) | {d['dsn'] or ('SYSOUT' if d['sysout'] else 'instream')} | {d['disp'] or ''} | {d['dcb'] or ''} |")
    (OUT / "Workloads" / f"{info['job']}-Jcl-Facts.md").write_text("\n".join(out) + "\n")


def write_program_facts(f: dict) -> None:
    out = [f"# {f['program']} - program facts", "", f"Source: `{f['path']}` (read-only estate).", "", "## Files", "",
           "| Logical file | DD | Organisation | Access | Key | Alt key | Line |", "|---|---|---|---|---|---|---|"]
    for x in f["files"]:
        out.append(f"| {x['name']} | {x['dd']} | {x.get('org', 'SEQUENTIAL')} | {x.get('access', 'SEQUENTIAL')} | {x.get('key', '')} | {x.get('altkey', '')} | L{x['line']} |")
    out += ["", "## Copybooks", ""] + [f"* `{c['name']}` (L{c['line']})" for c in f["copybooks"]]
    out += ["", "## External calls", ""] + ([f"* `{c['target']}` (L{c['line']})" for c in f["calls"]] or ["* none"])
    out += ["", "## Paragraphs", ""] + [f"* `{p['name']}` (L{p['line']})" for p in f["paragraphs"]]
    out += ["", "## I/O statements", "", "| Verb | Target | Line |", "|---|---|---|"] + [f"| {i['verb']} | {i['target']} | L{i['line']} |" for i in f["io"]]
    out += ["", f"DISPLAY statements: {f['displays']} (SYSOUT is part of the parity contract)."]
    (OUT / "Programs" / f"{f['program']}-Program-Facts.md").write_text("\n".join(out) + "\n")


def write_rules(pgm: str) -> list[dict]:
    path = cbl_path(pgm)
    resolved = []
    out = [f"# {pgm} - business rules (BRE)", "", f"Every rule cites `{rel(path)}`; the line is resolved by `tools/discovery/extract_facts.py`.", "",
           "| Rule | Statement | Evidence |", "|---|---|---|"]
    for rid, text, rx in RULES[pgm]:
        n = cite(path, rx)
        resolved.append({"id": rid, "program": pgm, "line": n, "statement": text})
        out.append(f"| {rid} | {text} | `{path.name}` L{n}: `{lines(path)[n - 1][6:72].strip()}` |")
    gaps = [g for g in GAPS if pgm in g[1].split(",") or g[1] == "ALL"]
    if gaps:
        out += ["", "## Gaps", ""] + [f"* **{g[0]}** ({g[3]}): {g[2]}" for g in gaps]
    (OUT / "Rules" / f"{pgm}-BRE.md").write_text("\n".join(out) + "\n")
    return resolved


def schedule_graph(jobs: dict) -> dict:
    ctm = APP / "scheduler" / "CardDemo.controlm"
    graph = {"source": rel(ctm), "jobs": {}, "edges": []}
    tree = ET.parse(ctm)
    producers: dict[str, str] = {}
    entries = []
    for job in tree.iter("JOB"):
        name = job.get("JOBNAME")
        ins = [c.get("NAME") for c in job.findall("INCOND")]
        outs = [c.get("NAME") for c in job.findall("OUTCOND") if c.get("SIGN") == "+"]
        entries.append((name, job.get("PARENT_FOLDER"), ins, outs))
        for o in outs:
            producers[o] = name
    for name, folder, ins, outs in entries:
        graph["jobs"][name] = {"folder": folder, "in": ins, "out": outs, "in_scope": name in jobs}
        for i in ins:
            if i in producers:
                graph["edges"].append({"from": producers[i], "to": name, "condition": i})
    for job, info in jobs.items():
        graph["jobs"].setdefault(job, {"folder": None, "in": [], "out": [], "in_scope": True})
        graph["jobs"][job]["steps"] = [{"step": s["step"], "exec": f"{exe(s)}", "line": s["line"]} for s in info["steps"]]
    graph["harness_chains"] = json.loads((ROOT / "tools" / "runjcl" / "chains.json").read_text())
    graph["harness_chains"] = {k: v["jobs"] for k, v in graph["harness_chains"].items()}
    return graph


def main() -> int:
    for sub in ("Workloads", "Programs", "Rules"):
        (OUT / sub).mkdir(parents=True, exist_ok=True)
    jobs, rules = {}, []
    for job, pgms in WORKLOADS.items():
        jobs[job] = parse_jcl(job)
        write_jcl_facts(jobs[job])
        for pgm in pgms:
            write_program_facts(parse_program(pgm))
            rules += write_rules(pgm)
    (OUT / "Workloads" / "Batch-Schedule-Graph.json").write_text(json.dumps(schedule_graph(jobs), indent=2) + "\n")
    (OUT / "rules.json").write_text(json.dumps(rules, indent=2) + "\n")
    (OUT / "Gaps.md").write_text("# Discovery gaps\n\n| Gap | Scope | Description | Severity |\n|---|---|---|---|\n" +
                                 "".join(f"| {g[0]} | {g[1]} | {g[2]} | {g[3]} |\n" for g in GAPS))
    print(f"discovery: {len(jobs)} workloads, {len(rules)} rules, {len(GAPS)} gaps -> {rel(OUT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
