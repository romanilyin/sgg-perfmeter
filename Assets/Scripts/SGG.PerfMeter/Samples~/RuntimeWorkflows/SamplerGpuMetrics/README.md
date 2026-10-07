# Exact Render Graph GPU samplers (URP)

Import **Runtime Workflows**. In a URP 17.4+ renderer, add one `PerfMeterSamplerGpuMetricsFeature` and assign `PerfMeterSamplerWorkload.compute` to its Compute Shader field. Start PerfMeter and show custom metrics. The feature records an offscreen compute workload and an offscreen raster clear; it does not modify the camera image. This opt-in sample adds workload and recording overhead. Remove it for normal profiling.

`sample.exact.compute` and `sample.exact.raster` use the existing custom metric registry and therefore the existing retained-session JSON `custom_metrics` export. No renderer installation count proves these timings exist. A renderer feature may be installed while its passes are not scheduled, culled, or unsupported. A missing compute shader leaves the compute metric explicitly unavailable. No sampled zero is replaced with a fake positive timing; a tiny raster clear may legitimately measure zero.

## Attach to your own pass

Keep the **same sampler instance** used by `AddComputePass` / `AddRasterRenderPass`, with a unique native marker name and stable metric ID:

```csharp
// Once, in the producer's initialization path. Recording/lifetime are caller-owned.
sampler = new ProfilingSampler("My unique workload marker");
metric = new PerfMeterProfilingSamplerMetricProvider("game.gpu.workload", "Workload", sampler);
sampler.enableRecording = true; // Only the coordinated recording owner may do this.
metric.SetEnabled(true);
PerformanceMeter.RegisterCustomMetricProvider(metric);

// In the producer's scheduling path, not a generic installed-feature probe:
metric.ReportProducerState(PerfMeterGpuPassAvailability.Scheduled);
using (var builder = renderGraph.AddComputePass<PassData>("Workload", out var data, sampler))
{
    data.Metric = metric;
    // Declare real resources and keep your workload's normal culling policy.
    builder.SetRenderFunc(static (PassData data, ComputeGraphContext context) =>
    {
        // Emit the actual dispatch/draw here.
        data.Metric.ReportPassExecution(); // CPU render-function evidence, NOT GPU completion.
    });
}

// In explicit producer gates, report NotScheduled or Unavailable instead of inventing zero.
// metric.ReportProducerState(PerfMeterGpuPassAvailability.NotScheduled);

// On teardown, this example is the ONLY owner of recording and the sampler.
PerformanceMeter.UnregisterCustomMetricProvider(metric);
metric.SetEnabled(false);
metric.Dispose();
sampler.enableRecording = false;
(sampler as System.IDisposable)?.Dispose(); // Public on SRP 17.6; optional on older SRP.
```

If another owner shares the sampler, that owner must coordinate recording and native-resource lifetime. The provider **never** toggles `enableRecording` or disposes a sampler, including on disable/re-enable/disposal. Its `SetEnabled` only declares/resets an observation epoch; it cannot read back the setter-only recording flag. Notify it of actual recording interruptions. Do not stop/dispose a shared sampler while other owners need it. Distinct sampler objects with the same marker name can still share native recorders in SRP 17.6; exact object identity is not a guarantee of isolated native timing. Do not use this adapter to discover samplers by name.

## Availability and freshness contract

- Reports aggregate across cameras: any scheduled/executed camera wins over skipped/unavailable cameras in the **same CPU frame**, in either order. New-frame negative evidence overrides old positive evidence. The GPU value/count is the sampler-wide sum, not a per-camera average; use distinct sampler markers/providers for camera-specific metrics.
- Report availability from the actual scheduling/gating path and call `ReportPassExecution` inside the render function when it emits the workload. Scheduling alone does not prove callback execution. Neither callback execution nor `gpuSampleCount == 0` proves GPU completion or pass absence.
- `TryCollect` always returns a snapshot, even when unavailable, so the expected series remains present in collection/export. `Available=false`'s zero placeholder is not a measurement. A nonnegative finite value with **positive sample count**, including value zero, can become available.
- GPU results can arrive later than the CPU callback. The default bound is eight CPU frames for producer, callback, and changed-observation evidence; choose a larger `maxObservationAgeFrames` only when justified by the workload. There is no blocking GPU wait.
- Enabling captures a baseline, clears prior producer/observation evidence, and does not accept that baseline. A later count/value transition with recent scheduling and render-function evidence is required, at least one CPU frame after the first callback. Resuming callbacks after an idle gap beyond the bound takes another baseline rather than reviving a retained result. No-sample observations are not held as previous positive values. An exception/non-finite/negative reading fails closed.
- Sampler APIs expose **no GPU source frame, result sequence, or camera identity**. `LastObservationFrame` is when a qualified change was **observed on the CPU**, not the GPU frame being measured. Available snapshots retain `DelayedSamplerObservation` warning. No count/value transition can prove source-frame provenance: a late pre-epoch native result may be indistinguishable from a current one. This provider does not claim strict GPU epoch isolation.
- Unchanged readings cannot prove new GPU results. They remain usable only within the configured window, then become `SamplerObservationStale`, even if identical real measurements might still be produced. This conservative false-unavailable case includes repeatedly sampled zero; do not “fix” it by refreshing on collection or scheduling alone.
- Feature deactivation with no producer callbacks becomes unavailable after the evidence bound. If you need immediate unavailable state, call `SetEnabled(false)` in the producer's own deactivation path; re-enable only after its coordinated recording owner restores recording.
- All API calls/reads are main-thread-only. Collection polls once per CPU frame; producer reports after a collection can affect evidence but cannot force another native read in that frame. Aggregate camera decisions before collection when possible.

This adapter neither enables global GPU profiling nor guarantees platform/Player GPU recording support. Test the actual backend and build: URP installation, annotations/RenderDoc scopes, and passing synthetic tests are not evidence that arbitrary GPU timings are available. Unsupported/release-build sampling can remain `GpuSamplesPending` without falsely declaring a pass absent.
