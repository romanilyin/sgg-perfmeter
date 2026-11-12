# Frame Debugger export

Export the current local Unity Frame Debugger capture from **SGG > Perfmeter > Frame Debugger Export**. This Editor tool writes an independent event snapshot; session JSON/CSV exports keep their existing formats.

## First supported adapter target

The adapter is enabled for Unity **6000.6.8f1**, Windows x64 Editor, **Direct3D 12**. The initial validation scene uses URP **17.6.0**. Other Editor versions or graphics APIs return an explicit unavailable result. Remote Player targets are outside this first version.

## Export a capture

1. Open **Window > Analysis > Frame Debugger**, choose the local Editor target, and enable Frame Debugger. Keep its window open with a valid selected event.
2. Open **SGG > Perfmeter > Frame Debugger Export**. Opening or refreshing this window does not enable Frame Debugger or start PerfMeter collection.
3. Choose **Maximum events** (default 2048, maximum 8192). Enable **Include shader properties** only when needed; it is off by default.
4. Click **Export current capture**. The service selects events while checking that the capture, target, window and selection still match. **Cancel export** requests cancellation.
5. Read the final status, then click **Show exported files**. Copy important results out of the temporary retention directory.

Changing the selected event or capture during export stops the walk. Your new selection is preserved. The original selection is restored only while the exporter still owns the same capture and last selected limit.

Recreating the native event list/hash also stops the walk with `Partial`, even when the image looks unchanged. A complete basic export was verified using an unchanged command buffer with clear and mesh draws in a URP project; broader URP Render Graph workloads remain unqualified.

## Files and limits

Output is saved under `Temp/PerfMeter/FrameDebuggerExports/<export-id>/`:

- `events.json`: schema `sgg.perfmeter.frame-debugger`, version 1; metadata, completeness, raw event types and typed fields with availability/reasons.
- `events.csv`: stable core columns for event, shader/pass, draw/geometry and render-target observations. Text is escaped for spreadsheet import; JSON retains raw text.
- `properties.csv`: optional long-form shader-property fields and native flags.
- `manifest.json`: file sizes and SHA-256 hashes, export identity and completeness.

Publication is atomic. Existing export IDs are never overwritten. The capture walk is bounded to 60 seconds by default; an event that does not become ready within its deadline is unavailable. A maximum-event, field or character limit produces an explicitly partial snapshot. Writing has a 30-second cancellation deadline and a **64 MiB total-output limit**; an output-limit failure requires fewer events/properties. Completed owned exports are retained up to **8 exports / 256 MiB**. Unknown files and unrelated directories are preserved; insufficient reclaimable space produces an error.

A **Partial** result is useful evidence with limited coverage. Check missing/truncated counts and field availability before drawing conclusions. RenderDoc/PIX capture files and capture bundles are separate workflows.

## Meaning and measurement

Event count, draw-call count, Render Graph pass count and GPU duration are different quantities. This snapshot does not measure per-event GPU time or reconstruct a previously observed worst frame. Scene/session context is observed correlation, not proof of exact source-frame identity.

Clear/resolve events suppress shader values that Unity may retain from the preceding event. Unknown event types remain explicit. Optional unsupported matrix/buffer property layouts are unavailable; texture pixels and buffer contents are never copied.

During the event walk, existing PerfMeter collection records explicit `EditorDiagnostic` gaps instead of adding diagnostic frames to normal session samples or alerts. The export does not start a collector. The interval begins with this export request; an earlier period with Frame Debugger enabled cannot be reconstructed.

Artifacts can contain object/event names, shader keywords and properties. Review them before sharing; export performs no automatic upload.

## Reader contract

Use the [JSON Schema](../../Assets/Scripts/SGG.PerfMeter/Editor/FrameDebugger/frame-debugger-v1.schema.json) and [minimal fixture](../../Assets/Scripts/SGG.PerfMeter/Tests/Fixtures/FrameDebugger/events-v1.json). Preserve unknown fields and raw event type values; reject an unsupported schema version explicitly. Unavailable fields have `value: null` with a reason, while measured zero remains numeric zero. Field `kind` identifies the source representation; imported names are plain data. Bound input bytes, arrays and strings before consuming external snapshots. This first version exports snapshots; an interactive importer and A/B analysis belong to later features.
