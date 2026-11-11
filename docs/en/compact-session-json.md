# Compact session JSON reader contract

`PerformanceMeter.ExportSessionJson(path, PerfMeterSessionJsonFormat.Compact)` and `perfmeter.session.export {"format":"json_compact","path":"Temp/PerfMeter/session-compact.json"}` explicitly select session schema 3. `ExportSessionJson(path)` and MCP `format: "json"` retain schema 2. Capture bundle `sgg.perfmeter.capture-samples` schema 1 and CSV do not use this format.

Schema 3 preserves the schema 2 envelope, summary, settings, sample order, built-in sample fields, telemetry, self-overhead window, and timeline. Only the representation of sample `custom_metrics` changes, with two added root tables:

```json
{
  "schema_version": 3,
  "custom_metric_descriptors": [
    {"id":"cloud.gpu_ms","name":"Cloud","category":"GPU","unit":"ms"}
  ],
  "custom_metric_warnings": ["", "No delayed recorder sample"],
  "samples": [
    {"custom_metrics":[{"descriptor":0,"value":1.25,"available":true,"warning":0}]},
    {"custom_metrics":[{"descriptor":0,"value":0,"available":false,"warning":1}]}
  ]
}
```

This fragment omits unchanged envelope and sample fields. For each custom metric entry, read `custom_metric_descriptors[entry.descriptor]` and combine its `id`, `name`, `category`, and `unit` with the entry's `value`, `available`, and `custom_metric_warnings[entry.warning]`. Indexes are zero-based integers local to this export. Validate their ranges; reject an unsupported schema instead of treating references as schema 2 metric objects.

Descriptor identity is the complete ordinal `(id, name, category, unit)` tuple captured with the sample. A changed name, category, or unit for the same ID gets a distinct descriptor. Warning strings are interned independently with exact ordinal identity. Both tables use first occurrence in retained sample order and then metric order; entries are never sorted or merged by ID. The sample entry order and repeated IDs remain intact. Table indexes are not stable across different sessions or retained sample sets.

Values, availability, and warnings belong to each retained sample. An unavailable value is not converted to zero, a warning does not imply unavailable, and an omitted provider remains omitted. An empty metric collection is `[]`; an empty session has empty descriptor and warning tables. Metadata is read only from retained samples, without consulting current providers.

String nulls use the existing full-format convention of an empty JSON string. Finite values use invariant round-trip numbers; nonfinite values use the existing strings `"NaN"`, `"Infinity"`, and `"-Infinity"`. Readers should preserve those representations instead of interpreting unavailable or nonfinite entries as measured zero. Exporting the same retained inputs and package identity produces identical UTF-8 bytes independent of current culture.

The existing atomic writer and overwrite policy apply: the runtime API atomically replaces a destination, while MCP requires a new project-local path and returns `file_exists` for an existing file. A failed atomic replacement preserves the previous artifact, and temporary files are removed on the existing best-effort cleanup path.

Compact mode reduces repeated custom metadata and warning bytes. It still builds the complete JSON string and UTF-8 output buffer in memory; it is not a streaming exporter and does not guarantee a size reduction for short sessions or mostly unique metadata.
