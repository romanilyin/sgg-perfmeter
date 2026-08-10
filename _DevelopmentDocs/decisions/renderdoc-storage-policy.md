# RenderDoc Storage And Finalization Policy

Status: `PM-RDOC-003A` policy accepted. Managed control, filesystem finalization,
bundle provenance, and real Unity `.rdc` validation remain implementation gates.
This decision does not enable native capture or make a support/release claim.

## Scope

This decision closes the numeric storage, retention, polling, cleanup, privacy,
and capture-stall gates deferred by the accepted
[`PM-RDOC-001` boundary](renderdoc-native-boundary.md). The initial values are
internal fixed limits, not public settings or MCP overrides. Future additive
configuration may only reduce these limits unless a new policy decision and
validation pass approves larger values.

The policy applies only to marker-owned Windows x64 Editor RenderDoc artifacts.
Unknown, external, user-owned, unmarked, or reparse-point paths are never
adopted, retained, copied, or deleted by this lifecycle.

## Fixed Limits

| Policy | Initial value |
| --- | ---: |
| Maximum source `.rdc` size | 512 MiB (`536870912` bytes) |
| Maximum Copy or Embed payload size | 512 MiB (`536870912` bytes) |
| Aggregate owned source pool | 2 GiB (`2147483648` bytes) |
| Aggregate owned Copy/Embed pool | 2 GiB (`2147483648` bytes), accounted separately from source and generic bundle bytes |
| Retained terminal items | 16 per source and Copy/Embed pool |
| Terminal item age | 7 days |
| Lost/nonterminal owned-root expiry | 24 hours after the owning session is no longer live |
| Free-space floor after a write | 1 GiB (`1073741824` bytes) |
| Storage metadata/staging reserve | 1 MiB (`1048576` bytes) per operation |
| Source reservation before begin | 512 MiB plus the 1 MiB reserve and 1 GiB floor |
| Transient filesystem attempts | 3 total attempts, with 25 ms and 50 ms delays before retries |
| Persistent cleanup sweeps | 3 worker sweeps after failed immediate cleanup; marker ownership remains persisted after exhaustion |
| First-candidate observation deadline | 30 seconds after successful end |
| Candidate quiet window | At least 500 ms with no capture-count or matching-candidate change |
| Artifact stabilization deadline | 60 seconds after first matching candidate |
| Stable-file sampling | 4 unchanged samples, 250 ms apart |
| Total finalization deadline | 180 seconds after successful end, including hashing and optional Copy/Embed |
| Warmed PerfMeter wrapper budget | 0.5 ms average per invocation and 0 B allocation |
| Real attached capture stall gate | At most 2000 ms added main/render stall for any begin/end capture boundary |

The 512 MiB artifact limit follows the released Memory Profiler cap and the
existing required synthetic-export gate. The 2 GiB, 16-item, 7-day, 24-hour,
1 GiB, and 3-attempt values follow the released capture-bundle, memory, and
graphics-state storage contracts. Native payload bytes do not inherit the
generic 64 MiB bundle/artifact limit.

## Roots And Accounting

The source root is:

```text
Temp/PerfMeter/RenderDoc/<nonce-hex>
```

The project-local Copy root is:

```text
Temp/PerfMeter/RenderDocCopies/<nonce-hex>
```

Embed writes the immutable payload inside the selected capture bundle at:

```text
external/renderdoc/capture.rdc
```

Each source or Copy root receives a versioned ownership record before payload
creation. The atomically replaced record stores only schema/version, request
nonce, owning session/generation, created UTC, state, and state UTC; raw capture
IDs and absolute paths are forbidden. States are `preflight`, `capturing`,
`awaiting_artifact`, `finalizing`, `terminal`, `cleanup_pending`, and
`lost_session`. UTC fields are retention evidence only; in-process deadlines
use a monotonic clock. A malformed, missing, future-version, or inconsistent
record makes the root unknown and therefore ineligible for automatic deletion.
Embed is owned by the existing bundle marker and records the same
nonce/generation and terminal state in RenderDoc provenance.

Source and Copy/Embed accounting are independent. Aggregate accounting uses
actual recursively measured owned bytes, including markers and native
provenance, while the 512 MiB per-file limit applies to the `.rdc` payload.
Embed bytes count against the native Copy/Embed pool but not the generic 64 MiB
component limit. Generic non-native bundle bytes continue to use their existing
quota. The current legacy caller-supplied exporter is therefore forbidden for
native descriptors: implementation requires a separate native export/accounting
path before Copy or Embed can be enabled. Retention may remove an entire
marker-owned bundle when its embedded native payload is the oldest item in the
Copy/Embed pool; it never removes a file from a committed bundle independently.

A source item is one marker-owned nonce root. A Copy item is one marker-owned
Copy nonce root. An Embed item is one committed marker-owned bundle containing
one native payload. Source uses its ownership-record terminal UTC; Copy uses its
own terminal UTC; Embed uses the terminal UTC persisted in RenderDoc provenance.
The shared Copy/Embed pool first removes items older than 7 days, then evicts the
oldest terminal item until both the 16-item and 2 GiB limits are satisfied.
Source applies the same age, then count, then byte ordering in its independent
pool. Equal timestamps are ordered by nonce bytes for deterministic tests.

Before native begin, retention removes expired terminal source roots and then
the oldest terminal roots until the source pool can accommodate a 512 MiB plus
1 MiB reservation. Free space must be at least the 1 GiB floor plus that full
reservation. Before Copy, free space must be at least the floor plus known
payload bytes plus the 1 MiB reserve. Before Embed, it must additionally include
all known non-native bundle bytes that will enter staging. Active operations are
never retention candidates. Failure to prove ownership, free sufficient quota,
or preserve the floor rejects preflight and is not a native-preferred fallback
reason.

## Observation And Finalization

Artifact polling runs on a worker, never the Unity main or render path. Polling
uses a 100 ms cadence until the first candidate or the 30-second observation
deadline. After the first candidate, candidate selection stays provisional for
at least 500 ms and until the capture count and complete matching-candidate set
remain unchanged. The bridge must re-enumerate after a provisional match; a
cached first candidate cannot bypass a delayed second-candidate ambiguity. The
current `PM-RDOC-002` first-candidate cache must be corrected and covered by a
delayed-second-candidate native test before managed native capture is enabled.

The accepted timestamp window is five seconds before the token start through
the 30-second observation deadline. Timestamp is corroborating evidence only;
nonce root, bridge index/count, exact candidate count, and stable Windows file
identity remain mandatory.

Opening or sharing failures may retry at the 250 ms stability cadence until the
60-second stabilization deadline. Once opened, the same Windows volume/file
identity must remain bound while four size/last-write samples remain unchanged
at 250 ms intervals. A zero-byte file is never stable. Source SHA-256 and any
Copy/Embed operation use the same identity, followed by a final identity, size,
and hash check. The complete operation must reach a terminal state within 180
seconds after end.

All in-process cadence, quiet-window, stabilization, and finalization intervals
use `Stopwatch.GetTimestamp`/QueryPerformanceCounter-derived monotonic elapsed
time. A deadline is expired when elapsed time is greater than or equal to its
limit; a sample exactly at the file-size or pool limit is accepted. Persisted
UTC timestamps never extend or resume an interrupted in-process deadline.

Replacement, deletion, growth after stability, identity change, ambiguous
candidates, source/copy size overflow, deadline expiry, quota failure, or a
post-copy hash mismatch produces a failed non-authoritative artifact. It never
falls back to generic capture after begin and never adopts a later file.

## Cleanup And Retry

End, discard, begin failure, cancellation, runtime stop, and domain reload all
restore the previous RenderDoc template before owned cleanup. Immediate
filesystem cleanup receives three attempts with 25 ms and 50 ms delays. A
remaining deletion failure writes or preserves a marker-owned pending-cleanup
record and keeps the profiling lease in cleanup/lost-session state.

Pending cleanup retries before the next native request and after reload. A
nonterminal root becomes stale only after 24 hours and after no live
session/generation can own it. Cleanup may delete only a root with a valid
matching marker beneath the canonical source or Copy root and with no reparse
point in its path. Unknown contents fail closed and remain for manual review.
An in-process failed immediate cleanup retains the capture lease while up to
three additional worker sweeps retry the exact persisted root. Exhaustion is
terminal for that in-process flight rather than an unbounded loop; the marker
remains eligible for the next-request or post-reload sweep.

Canceled or failed Embed writes exist only in the existing marker-owned bundle
staging directory and are removed by that staging cleanup contract. A failed
native payload is never committed into a successful bundle. Once committed, an
Embed payload is deleted only by retention of its complete marker-owned bundle.
The persisted state/state-UTC record is the sole source for terminal age and
24-hour lost-session sweeps after reload; directory timestamps alone never
grant cleanup authority.

## Privacy And Sharing

Native capture defaults to `MetadataOnly` plus `DoNotShare`, with
`ContainsGpuCaptureData`, `Sensitive`, and `RequiresReview` privacy flags.
`Copy` and `Embed` require an explicit per-request opt-in and publish
`ReviewBeforeShare`; neither mode uploads or opens network access. Absolute
source paths, raw Windows volume/file identifiers, and ownership-marker contents
stay internal. Versioned bundle provenance may contain only an opaque
`source_file_identity_sha256` derived from the exact identity and request nonce;
it never serializes raw volume or file IDs. Public API, MCP, and bundle summaries
expose only project-relative metadata.

`PerfMeterExternalArtifactOptions.Default` is not valid for native RenderDoc
because its share policy is `ReviewBeforeShare`. The native descriptor must
construct its policy explicitly and may publish authenticated authority only
after all additional RenderDoc gates in the boundary decision pass.

## Performance Gate

Fake/warmed tests measure PerfMeter-owned scheduling, validation, and interop
overhead separately from vendor work: average main/control or render-callback
overhead is at most 0.5 ms per invocation with 0 B managed allocation. Polling,
filesystem access, hashing, copying, and retention are worker-only.

Each real D3D11, D3D12, and Vulkan row records ten captures after one warmup and
reports average and maximum main/render stalls around begin and end. The
baseline is 120 immediately preceding uncaptured frames in the same scene,
graphics API, resolution, and tool attachment. Begin/end app-API call wall time
is an absolute metric and each call must complete within 2000 ms. Boundary-frame
stall is a separate metric:
`max(0, boundary_frame_duration - baseline_p95_frame_duration)`, calculated
independently for main and render timing. Every one of the ten measured captures
must pass both the absolute call-duration and added frame-stall gates. Missing
reliable main or render timing fails the row rather than assuming zero. Capture
frames remain perturbation samples and never enter normal baseline evidence.

## Validation Contract

Implementation tests baseline every numeric value and cover source and
Copy/Embed pools independently: exact-limit acceptance, one-byte overflow,
reservation/free-space rejection, count/age/quota retention ordering, active
item preservation, stale marker ownership, retry/pending cleanup, polling and
deadline boundaries, delayed second candidates, stable/growing/replaced files,
hash mismatch, cancellation, and default privacy/share behavior. Retained Copy
also covers generation/bundle-bound native descriptors, capability snapshot
binding, legacy caller-path rejection, terminal marker/size/identity/hash
revalidation, payload mutation, cancelable export hashing, additive provenance,
and exclusion of `.rdc` bytes from the generic 64 MiB bundle quota.

Related decisions: [`renderdoc-native-boundary.md`](renderdoc-native-boundary.md),
[`capture-bundles.md`](capture-bundles.md), and
[`roadmap.md`](../backlog/roadmap.md).
