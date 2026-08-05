using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterCollector
	{
		internal const double DefaultFrameBudgetMs = 1000d / 60d;
		internal const double MaxFrameTimingSampleMs = 60000d;

		private const string OpenGlGpuTimingWarning = "GPU frame timing can be unavailable or unreliable on OpenGL/OpenGLES. Prefer Vulkan/Metal/D3D for GPU-bound classification.";
		private const string MissingGpuTimingWarning = "GPU frame timing is unavailable. Enable Frame Timing Stats and verify platform GPU timer support.";
		private const string InvalidFrameTimingWarning = "Ignored invalid FrameTimingManager sample outside the 0-60000 ms sanity range.";
		private const string MissingCountersWarning = "Some ProfilerRecorder counters are unavailable on this Unity version, platform, or render path.";
		private const string CollectorStoppedWarning = "PerfMeter collector is not running.";

		private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
		private readonly PerfMeterProfilerMetricCatalog _profilerMetricCatalog = new PerfMeterProfilerMetricCatalog();
		private bool _isRunning;

		internal PerfMeterCounterAvailability AvailableCounters => _profilerMetricCatalog.AvailableCounters;
		internal PerfMeterCounterAvailability UnavailableCounters => _profilerMetricCatalog.UnavailableCounters;
		internal string LastError => _profilerMetricCatalog.LastError;

		internal void Start()
		{
			if (_isRunning)
			{
				return;
			}

			_profilerMetricCatalog.Start();
			_isRunning = true;
		}

		internal void Stop()
		{
			_profilerMetricCatalog.Stop();
			_isRunning = false;
		}

		internal bool RefreshProfilerMetricCatalog()
		{
			return _isRunning && _profilerMetricCatalog.Refresh();
		}

		internal PerfMeterProfilerMetricCatalogSnapshot GetProfilerMetricCatalog()
		{
			return _profilerMetricCatalog.GetSnapshot();
		}

		internal PerfMeterMetricsSnapshot Collect(int collectionFrame, double frameBudgetMs, out PerfMeterFrameTimingAvailability frameTimingAvailability, out string warning, out bool frameTimingSampleIgnored)
		{
			if (!_isRunning)
			{
				frameTimingAvailability = PerfMeterFrameTimingAvailability.NotCollected;
				warning = CollectorStoppedWarning;
				frameTimingSampleIgnored = false;
				return PerfMeterMetricsSnapshot.Stopped;
			}
			_profilerMetricCatalog.RefreshSampleStates();

			FrameTimingManager.CaptureFrameTimings();
			uint timingCount = FrameTimingManager.GetLatestTimings(1, _frameTimings);
			FrameTiming timing = timingCount > 0 ? _frameTimings[0] : default;
			bool hasValidCpuFrameTiming = timingCount > 0 && HasValidCpuFrameTiming(timing);
			frameTimingSampleIgnored = timingCount > 0 && !hasValidCpuFrameTiming;
			frameTimingAvailability = hasValidCpuFrameTiming ? PerfMeterFrameTimingAvailability.Available : PerfMeterFrameTimingAvailability.Unavailable;

			double cpuFrameTimeMs = hasValidCpuFrameTiming ? timing.cpuFrameTime : 0d;
			double cpuMainThreadFrameTimeMs = hasValidCpuFrameTiming ? timing.cpuMainThreadFrameTime : 0d;
			double cpuRenderThreadFrameTimeMs = hasValidCpuFrameTiming ? timing.cpuRenderThreadFrameTime : 0d;
			double cpuMainThreadPresentWaitTimeMs = hasValidCpuFrameTiming ? timing.cpuMainThreadPresentWaitTime : 0d;
			double gpuFrameTimeMs = hasValidCpuFrameTiming && IsValidFrameTimingSampleMs(timing.gpuFrameTime) ? timing.gpuFrameTime : 0d;
			bool gpuFrameTimeAvailable = gpuFrameTimeMs > 0d;
			bool invalidGpuFrameTiming = hasValidCpuFrameTiming && timing.gpuFrameTime > 0d && !gpuFrameTimeAvailable;

			PerfMeterBottleneck bottleneck = ClassifyBottleneck(
				frameTimingAvailability,
				frameBudgetMs,
				cpuFrameTimeMs,
				cpuMainThreadFrameTimeMs,
				cpuRenderThreadFrameTimeMs,
				cpuMainThreadPresentWaitTimeMs,
				gpuFrameTimeMs,
				gpuFrameTimeAvailable);

			warning = GetWarning(gpuFrameTimeAvailable, frameTimingSampleIgnored || invalidGpuFrameTiming);

			return new PerfMeterMetricsSnapshot(
				PerfMeterRuntimeState.Running,
				PerfMeterAvailability.Available,
				collectionFrame,
				bottleneck,
				frameBudgetMs,
				gpuFrameTimeAvailable,
				cpuFrameTimeMs,
				cpuMainThreadFrameTimeMs,
				cpuRenderThreadFrameTimeMs,
				cpuMainThreadPresentWaitTimeMs,
				gpuFrameTimeMs,
				ReadCounter(PerfMeterCounterAvailability.DrawCalls),
				ReadCounter(PerfMeterCounterAvailability.SetPassCalls),
				ReadCounter(PerfMeterCounterAvailability.Batches),
				ReadCounter(PerfMeterCounterAvailability.Vertices),
				ReadCounter(PerfMeterCounterAvailability.BrgDrawCalls),
				ReadCounter(PerfMeterCounterAvailability.BrgInstances),
				ReadLongCounter(PerfMeterCounterAvailability.IndexBufferUploadInFrameBytes),
				ReadLongCounter(PerfMeterCounterAvailability.SystemUsedMemory),
				ReadLongCounter(PerfMeterCounterAvailability.GcReservedMemory),
				ReadLongCounter(PerfMeterCounterAvailability.GpuMemory),
				0d,
				srpBatcherInstances: ReadCounter(PerfMeterCounterAvailability.SrpBatcherInstances));
		}

		private int ReadCounter(PerfMeterCounterAvailability counter)
		{
			long value = ReadLongCounter(counter);
			return value > int.MaxValue ? int.MaxValue : (int)value;
		}

		private long ReadLongCounter(PerfMeterCounterAvailability counter)
		{
			return _profilerMetricCatalog.ReadLongCounter(counter);
		}

		private string GetWarning(bool gpuFrameTimeAvailable, bool invalidFrameTiming)
		{
			if (UsesOpenGlGpuTiming())
			{
				return OpenGlGpuTimingWarning;
			}

			if (invalidFrameTiming)
			{
				return InvalidFrameTimingWarning;
			}

			if (!gpuFrameTimeAvailable)
			{
				return MissingGpuTimingWarning;
			}

			return UnavailableCounters != PerfMeterCounterAvailability.None ? MissingCountersWarning : string.Empty;
		}

		private static bool UsesOpenGlGpuTiming()
		{
			GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
			return deviceType == GraphicsDeviceType.OpenGLES3 || deviceType == GraphicsDeviceType.OpenGLCore;
		}

		internal static bool IsValidFrameTimingSampleMs(double value)
		{
			return value > 0d && value <= MaxFrameTimingSampleMs && !double.IsNaN(value) && !double.IsInfinity(value);
		}

		internal static bool IsValidFrameTimingComponentMs(double value)
		{
			return value >= 0d && value <= MaxFrameTimingSampleMs && !double.IsNaN(value) && !double.IsInfinity(value);
		}

		private static bool HasValidCpuFrameTiming(FrameTiming timing)
		{
			return IsValidFrameTimingSampleMs(timing.cpuFrameTime) &&
				IsValidFrameTimingComponentMs(timing.cpuMainThreadFrameTime) &&
				IsValidFrameTimingComponentMs(timing.cpuRenderThreadFrameTime) &&
				IsValidFrameTimingComponentMs(timing.cpuMainThreadPresentWaitTime);
		}

		internal static PerfMeterBottleneck ClassifyBottleneck(
			PerfMeterFrameTimingAvailability frameTimingAvailability,
			double frameBudgetMs,
			double cpuFrameTimeMs,
			double cpuMainThreadFrameTimeMs,
			double cpuRenderThreadFrameTimeMs,
			double cpuMainThreadPresentWaitTimeMs,
			double gpuFrameTimeMs,
			bool gpuFrameTimeAvailable)
		{
			if (frameTimingAvailability != PerfMeterFrameTimingAvailability.Available)
			{
				return PerfMeterBottleneck.Unknown;
			}

			double cpuMainWorkTimeMs = System.Math.Max(0d, cpuMainThreadFrameTimeMs - cpuMainThreadPresentWaitTimeMs);
			bool mainOverBudget = cpuMainWorkTimeMs > frameBudgetMs;
			bool renderOverBudget = cpuRenderThreadFrameTimeMs > frameBudgetMs;
			bool gpuOverBudget = gpuFrameTimeAvailable && gpuFrameTimeMs > frameBudgetMs;
			bool hasSignificantPresentWait = cpuMainThreadPresentWaitTimeMs > 1d ||
				(cpuFrameTimeMs > 0d && cpuMainThreadPresentWaitTimeMs / cpuFrameTimeMs >= 0.25d);
			bool workBelowBudget = cpuMainWorkTimeMs < frameBudgetMs * 0.85d &&
				cpuRenderThreadFrameTimeMs < frameBudgetMs * 0.85d &&
				gpuFrameTimeAvailable && gpuFrameTimeMs < frameBudgetMs * 0.85d;

			if (hasSignificantPresentWait && !gpuFrameTimeAvailable && !mainOverBudget && !renderOverBudget)
			{
				return PerfMeterBottleneck.Unknown;
			}

			if (hasSignificantPresentWait && workBelowBudget)
			{
				return PerfMeterBottleneck.PresentLimited;
			}

			if (!gpuOverBudget && !mainOverBudget && !renderOverBudget)
			{
				return PerfMeterBottleneck.Balanced;
			}

			double gpuOverBudgetMs = gpuOverBudget ? gpuFrameTimeMs - frameBudgetMs : double.NegativeInfinity;
			double mainOverBudgetMs = mainOverBudget ? cpuMainWorkTimeMs - frameBudgetMs : double.NegativeInfinity;
			double renderOverBudgetMs = renderOverBudget ? cpuRenderThreadFrameTimeMs - frameBudgetMs : double.NegativeInfinity;

			if (gpuOverBudgetMs >= mainOverBudgetMs && gpuOverBudgetMs >= renderOverBudgetMs)
			{
				return PerfMeterBottleneck.GpuBound;
			}

			if (mainOverBudgetMs >= renderOverBudgetMs)
			{
				return PerfMeterBottleneck.CpuMainThreadBound;
			}

			if (renderOverBudget)
			{
				return PerfMeterBottleneck.CpuRenderThreadBound;
			}

			return PerfMeterBottleneck.Balanced;
		}

	}
}
