# Target Neutrality & Intermediate Representation (IR) Standard

1. **Target Agnostic Policy**: Analysis, facts extraction, canonical representation, and verification specifications must be strictly target-neutral.
2. **Forbidden Vocabulary in Extraction & Canonical Specifications**:
   - `Spark`, `PySpark`, `DataFrame`, `RDD`, `Cobrix`, `Parquet`, `HDFS`, `Delta Lake`
   - `Java`, `Spring Batch`, `JVM`
   - Specific target syntax constructs unless explicitly comparing target implementations.
3. **Canonical Construct Equivalents**:
   - Sequential Dataset / Flat File -> `SequentialFile` / `Stream`
   - Keyed VSAM (KSDS) -> `KeyedDataStore` with key fields
   - Relative VSAM (RRDS) -> `IndexedDataStore` with slot index
   - COBOL Records / Copybooks -> `RecordLayout` with physical encoding, offset, length, scale, precision
   - COMP-3 -> `PackedDecimal` (precision, scale, sign nibble convention)
   - COMP/COMP-4 -> `BinaryInteger` (bytes, signed/unsigned, TRUNC convention)
   - SORT / MERGE -> `OrderedTransformation` (sort keys, directions)
   - EVALUATE / IF -> `DecisionRule` / `BranchCondition`
   - Control Break -> `GroupBoundaryEvent` (key change detection)
4. **All target lowering decisions belong to `.NET Planning` & `.NET Generation` stages**, never in legacy analysis.
