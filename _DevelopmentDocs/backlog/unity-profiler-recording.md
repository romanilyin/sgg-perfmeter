# Standalone Unity Profiler Recording Plan

Статус: planned P2, docs-only design; реализация не начата.
Roadmap ID: `PM-PROF-001`.
Дата актуализации: 2026-08-30.

## Цель

Добавить в PerfMeter явное управление записью штатного Unity Profiler и сохранение уже собранного `.data` capture так, чтобы весь основной workflow работал без SGG Unity MCP Gateway. MCP, если он установлен, предоставляет только optional policy/audit adapter над тем же package-owned Editor service.

PerfMeter остается low-overhead diagnostics/capture-coordination package и не заменяет Unity Profiler UI, Profile Analyzer или Memory Profiler.

## Product Decision

Ownership разделяется по слоям:

| Слой | Ответственность |
| --- | --- |
| PerfMeter Runtime | Session ID, correlation markers, runtime metrics и immutable provenance inputs; без `UnityEditor` и MCP dependencies. |
| PerfMeter Editor | Capability detection, start/stop/save state machine, exact operation ownership, package-local artifacts и standalone UI. |
| PerfMeter MCP adapter | Optional descriptors/handlers, которые применяют MCP risk/approval contract и вызывают тот же PerfMeter Editor service. |
| Generic Gateway profiler commands | Независимая общепроектная возможность для проектов без PerfMeter; не является runtime dependency пакета. |

Допустимое направление зависимости:

```text
optional MCP adapter -> PerfMeter Editor service -> Editor backend
```

Запрещенное направление зависимости:

```text
PerfMeter Runtime/Editor -> Gateway process, Gateway policy or Gateway artifact store
```

Отсутствие Gateway package, process, config, token или policy не должно менять import, runtime collection, standalone recording UI/API, session export или capture behavior PerfMeter.

## Current State

PerfMeter уже предоставляет:

- bounded `perfmeter.session.start/stop/summary/export` для собственных samples;
- `ProfilerRecorder` metric capabilities и internal correlation markers;
- process-local owner/GPU/operation leases;
- RenderDoc/PIX external GPU capture coordination;
- optional Memory Profiler snapshots;
- package-local capture bundles и asynchronous export.

PerfMeter пока не предоставляет:

- управление штатной Unity Profiler recording state;
- package-owned start/stop ownership token;
- сохранение Unity Profiler `.data` с PerfMeter session provenance;
- standalone UI для такого start/stop/save;
- package-specific MCP adapter для того же workflow.

PerfMeter session recording, external GPU capture и Memory Profiler snapshot не считаются эквивалентом Unity Profiler recording.

## Supported First Slice

Первый slice ограничен текущим Unity Editor process:

- supported Unity floor остается `6000.4+`;
- local Editor и Play Mode recording поддерживаются через один Editor-only backend;
- optional `profile_editor` target явно входит в request и result;
- remote Player attach/selection, standalone Player control и arbitrary profiler connection management не входят в scope;
- package не открывает Unity Profiler window автоматически и не меняет enabled modules без отдельного будущего решения;
- opening Setup/status never starts or stops recording;
- `perfmeter.session.start` never starts Unity Profiler implicitly.

Version-fragile Unity Editor APIs изолируются за backend boundary. Missing type/member, unsupported signature или failed state verification возвращают typed unavailable/error result и не переходят к следующему fallback member после возможной mutation.

## Editor Contract

Editor-only API должен предоставлять typed immutable options/results/snapshots для следующих операций:

```text
GetCapabilities
GetStatus
TryStartRecording
TryStopRecording
RequestCaptureSave
GetCaptureSaveStatus
CancelCaptureSave
DiscardPendingCapture
```

Точные public names фиксируются implementation review, но один service остается source of truth для UI и optional MCP handlers.

Capabilities включают:

- Unity version и Editor-only availability;
- resolved recording-state and save API member provenance;
- supported local target modes;
- package-owned artifact limits;
- async save/cancel support;
- process-local lease semantics;
- explicit warnings for internal/version-fragile APIs.

Typed reasons включают минимум:

```text
NotSupportedOutsideEditor
ProfilerApiUnavailable
EditorBusy
AlreadyRecordingExternal
AlreadyRecordingOwned
NotRecording
WrongOwner
LostOwnership
LeaseConflict
CaptureSaveUnavailable
CaptureAssociationUnverified
CaptureWindowMismatch
CaptureTooLarge
InsufficientFreeSpace
PathRejected
PendingCaptureExpired
Canceled
UnityException
```

Unknown enum values fail closed and round-trip through serialized metadata without conversion to success.

## State And Ownership

Planned states:

```text
Idle
ObservedExternalRecording
Starting
RecordingOwned
Stopping
StoppedPendingSave
Saving
Completed
Canceled
Unavailable
LostOwnership
Error
```

Start sequence:

1. Verify Editor is not compiling, updating, entering/exiting Play Mode or quitting.
2. Resolve and snapshot exact API capabilities before mutation.
3. Read current Unity Profiler state.
4. If recording is already active without a matching PerfMeter operation, report `ObservedExternalRecording` and do not adopt it.
5. Acquire the existing `exclusive-profiling-operation` lease for one package-owned operation.
6. Create a bounded operation ID and ownership token.
7. Request recording once through the selected exact backend member.
8. Read state again and publish success only after the expected transition is observed.

Stop sequence:

1. Require the exact active package-owned operation/token.
2. Revalidate Editor lifecycle, lease, backend member and current profiler state.
3. Request stop once.
4. Verify the terminal state and freeze a bounded `StoppedPendingSave` candidate instead of immediately releasing operation ownership.
5. Retain that candidate and lease until exact save, explicit discard or bounded expiry so another package operation cannot silently replace the retained Profiler data.
6. Preserve typed uncertainty and the lease when delivery may have occurred but terminal state is unproven; never repeat stop automatically.

Save sequence:

1. Require the exact `StoppedPendingSave` operation ID and package-owned candidate.
2. Revalidate lease, current recording state and every available Profiler frame/data generation signal before invoking save.
3. If Unity does not expose enough evidence to prove that retained data still belongs to the stopped operation, publish `CaptureAssociationUnverified` and never claim an exact session/frame association. Confirmed replacement or a new recording returns `CaptureWindowMismatch`.
4. Invoke the selected save member once and transition to worker-side stabilization/publication.
5. Release the operation lease only after finalized save, explicit discard, terminal error with no possible owned work, or bounded expiry.

External recordings are read-only observations. PerfMeter must not stop, save-as-owned, rename or silently adopt a recording that it did not start.

Domain reload and Play Mode transitions require explicit reconciliation. Persisted `SessionState` may retain bounded operation metadata for diagnosis, but an in-memory ownership token does not become authoritative solely because IDs match after reload. Ambiguous recovery reports `LostOwnership` or external recording and performs no automatic stop/save.

## Lease Coordination

Unity Profiler recording claims the existing `PerfMeterProfilerLeaseResource.Operation` resource with key `exclusive-profiling-operation`.

The first slice does not require the GPU resource merely to record CPU/Editor data. A future mode that changes GPU profiling behavior must declare and acquire the additional resource explicitly.

Known Memory Profiler, GraphicsStateCollection, RenderDoc/PIX and future native profiling operations remain conflict-aware through the same coordinator. Process-local leases are not presented as cross-process authority.

## Standalone UI

`SGG/Perfmeter/Setup` receives a dedicated Unity Profiler Recording section, available without MCP.

Required presentation:

- current capability and recording ownership state;
- target mode and operation ID;
- `Start Recording`, `Stop Recording`, `Save Capture`, `Discard Pending Capture`, `Cancel Save` and reveal-folder actions where valid;
- clear distinction between package-owned and externally observed recording;
- capture save phase/progress, artifact identity, byte count and warnings;
- explicit privacy warning that `.data` may contain marker names, object/type context and project-sensitive profiling data.

Standalone authorization is an explicit user action. Start and save require a native Editor confirmation dialog describing performance/storage/privacy impact. Opening or refreshing the window remains read-only. A boolean passed through a public API is a caller intent field, not a security boundary.

Buttons remain disabled during incompatible Editor lifecycle states. Closing the window does not stop recording or cancel an accepted save operation.

## Artifact Contract

Package-owned artifacts use a fixed root independent of Gateway, provisionally:

```text
Library/SGGPerfMeter/ProfilerCaptures/
```

Each finalized item contains:

```text
<capture-id>.data
<capture-id>.json
```

Metadata schema is versioned and records:

- capture/operation/session IDs;
- package and Unity versions;
- requested and actual target mode;
- start/stop frame and UTC/monotonic timing evidence;
- resolved API member provenance;
- ownership and lease terminal state;
- PerfMeter session marker prefix and matched session/window IDs when available;
- `.data` relative path, byte count and SHA-256;
- finalized/canceled/error state;
- privacy/share classification and warnings.

The implementation must define and expose fixed item/pool/count/age/free-space limits before code is accepted. Arbitrary output paths, network upload and writes under `Assets/` or `Packages/` are prohibited.

Unity-owned save invocation remains on the main thread. Directory creation, bounded file stabilization, hashing, metadata serialization, retention and final atomic publication move to a worker after all required Unity state has been snapshotted. Worker code does not call Unity APIs.

Cancellation is cooperative. It can cancel queued or worker-side stabilization/publication, but it does not claim to interrupt a Unity save call already issued on the main thread. Uncertain save delivery remains non-replayable and retains staging/ownership evidence for bounded reconciliation.

Staging/final roots use ownership markers, canonical containment, reparse-point rejection and no-overwrite commit. Retention removes only recognized finalized package-owned items and never unknown files.

## Session Correlation

Starting Unity Profiler recording remains independent from starting a PerfMeter session. If both are active, metadata binds them without changing existing session schema semantics:

- current PerfMeter session ID;
- `SGG.PerfMeter.Session.<sessionId>.Begin/End` marker prefix;
- recording operation ID;
- observed overlap frame/time bounds;
- whether the session began before, during or after profiler recording;
- explicit unmatched/partial overlap state.

No frame correspondence is inferred from timestamps alone when exact marker/frame evidence is unavailable.

An additive future capture-bundle reference may point to the finalized package-owned profiler artifact by ID/hash. The large `.data` payload is not embedded into session JSON or returned inline by MCP.

## Optional MCP Adapter

The package may add extension commands after the standalone service and UI are accepted:

```text
perfmeter.profiler.recording.status
perfmeter.profiler.recording.start
perfmeter.profiler.recording.stop
perfmeter.profiler.capture.save.request
perfmeter.profiler.capture.save.status
perfmeter.profiler.capture.save.cancel
perfmeter.profiler.capture.discard
```

MCP requirements:

- status is read-only and never discovers capabilities by mutating Editor state;
- start/stop/save are `write`/`unsafe`, feature-gated and ask-gated by the host policy by default;
- start requires `confirm_recording:true` and save requires `confirm_capture_export:true` in addition to human policy approval;
- stop, save and discard bind the exact public operation ID; private in-process ownership tokens are never returned as authorization material;
- handlers call only the package-owned Editor service and contain no second `ProfilerDriver` implementation;
- results preserve package status/reason/ownership/artifact fields faithfully;
- timeout or response loss never authorizes automatic start/stop/save replay;
- no command accepts an arbitrary path or exposes raw `.data` inline;
- absence of Gateway leaves all standalone package behavior unchanged.

Generic Gateway `profiler.recording.*` commands may coexist for projects without PerfMeter. One workflow must not mix generic and package-owned ownership: status exposes external/owned state, and a package command refuses to adopt an active generic recording.

## Implementation Order

### A. Capability and backend prototype

- Audit exact Unity `6000.4` and current maintained Editor APIs for recording state, target and save.
- Record type/member/signature provenance and determine which calls mutate state.
- Build a fake backend and deterministic pure state-machine tests before production mutation.
- Keep the prototype production-disabled until ownership and failure behavior are reviewed.

### B. Editor service and ownership

- Add typed contracts, operation IDs, exact transition verification and lease integration.
- Implement external-recording observation and no-adoption behavior.
- Add domain reload/Play Mode/quitting reconciliation with no automatic mutation.

### C. Async artifact publication

- Implement fixed-root staging, save request/status/cancel, stabilization, hash, metadata and retention.
- Reuse existing worker/snapshot/atomic-publication patterns without coupling to Gateway storage.
- Publish only nonempty, stable, hash-verified captures as completed.

### D. Standalone UI

- Add Setup section, explicit confirmations, progress/status and reveal-folder action.
- Validate the complete workflow in a clean consumer with no Gateway package or process.

### E. Optional MCP adapter

- Add package command descriptors/handlers over the accepted service.
- Validate policy/confirmation/no-replay and exact result fidelity separately.

### F. Public docs and release

- Document standalone UI/API first; MCP is an optional automation section.
- Add privacy/storage/troubleshooting and compatibility notes.
- Release only after the supported Unity matrix and clean consumer gates pass.

## Required Tests

EditMode/fake-backend coverage:

- capability available/unavailable and exact member provenance;
- idle start, owned repeat, external active refusal and transition verification failure;
- exact-owner stop, wrong owner, uncertain stop, lost lease and no replay;
- stopped-pending-save retention, exact save/discard/expiry release and replacement-window refusal;
- compile/update/Play Mode/quitting admission refusal;
- domain reload reconciliation without external recording mutation;
- package session overlap and unmatched correlation;
- fixed-root, traversal, reparse, existing target, staging cleanup and no-overwrite commit;
- file growth/stability, size/free-space bounds, hash mismatch and worker cancellation;
- cancellation before dispatch, after issued Unity save and during worker publication without false interruption claims;
- retention ignores unknown files and never follows reparse points;
- UI action availability and confirmation boundaries;
- MCP descriptor risk/schema/result fidelity when the optional adapter is present.

Live acceptance:

1. Clean Unity `6000.4` consumer with PerfMeter only: UI start, measured Play Mode interval, stop and save produce one nonempty `.data` plus matching hash-bound metadata.
2. The same standalone consumer runs normal PerfMeter runtime/session/export workflows without Gateway files, process, config or token.
3. An externally started Unity Profiler recording is observed but never stopped or adopted by PerfMeter.
4. One domain reload/Play Mode transition returns a typed reconciled or lost-ownership state without blind stop/save.
5. Current maintained Unity `6000.5+` line repeats capability/start/stop/save or is documented as typed unavailable; unsupported behavior is not claimed from compile alone.
6. Optional MCP consumer proves ask-gated one-dispatch start/stop/save, fixed-root artifact publication and zero replay after timeout/response loss.

Full package EditMode/PlayMode and clean Git UPM/npm consumer checks remain release gates. This planning iteration opens no Unity Editor and creates no runtime/test evidence.

## Non-Goals

- Replacing Unity Profiler or Profile Analyzer UI.
- Automatically starting recording with PerfMeter runtime/session/alert startup.
- Always-on or startup recording.
- Remote Player discovery/attach/control in the first slice.
- Enabling/disabling arbitrary Profiler modules.
- Memory Profiler snapshots, RenderDoc/PIX capture or Frame Debugger control through this API.
- Arbitrary output paths, uploads, telemetry streaming or credential handling.
- A hard dependency on Gateway, MCP SDK or Gateway artifact/policy formats.
- Removing or changing generic Gateway profiler commands.
- Claiming exact frame attribution when marker/frame evidence is absent.

## Exit Criteria

`PM-PROF-001` may be marked resolved only when:

- standalone service and UI work in a clean project without MCP;
- Runtime assemblies contain no `UnityEditor` or Gateway dependency;
- one source of truth serves UI and optional MCP handlers;
- external recording is never silently adopted or stopped;
- operation/lease/reload uncertainty remains typed and non-replayable;
- finalized artifacts are bounded, package-owned, stable and hash-verified;
- session/capture correlation is explicit and does not fabricate frame matches;
- supported Unity rows have real start/stop/save evidence;
- package tests, consumer installs, privacy docs and focused review pass.
