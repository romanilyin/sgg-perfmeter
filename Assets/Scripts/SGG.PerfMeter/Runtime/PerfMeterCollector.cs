using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterCollector
	{
		internal const double DefaultFrameBudgetMs = 1000d / 60d;

		private const string OpenGlGpuTimingWarning = "GPU frame timing can be unavailable or unreliable on OpenGL/OpenGLES. Prefer Vulkan/Metal/D3D for GPU-bound classification.";
		private const string MissingGpuTimingWarning = "GPU frame timing is unavailable. Enable Frame Timing Stats and verify platform GPU timer support.";
		private const string MissingCountersWarning = "Some ProfilerRecorder counters are unavailable on this Unity version, platform, or render path.";

		private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
		private readonly PerfMeterCounterAvailability[] _trackedCounters =
		{
			PerfMeterCounterAvailability.DrawCalls,
			PerfMeterCounterAvailability.SetPassCalls,
			PerfMeterCounterAvailability.Batches,
			PerfMeterCounterAvailability.Vertices,
			PerfMeterCounterAvailability.SrpBatcherInstances,
			PerfMeterCounterAvailability.BrgDrawCalls,
			PerfMeterCounterAvailability.BrgInstances,
			PerfMeterCounterAvailability.IndexBufferUploadInFrameBytes,
			PerfMeterCounterAvailability.SystemUsedMemory,
			PerfMeterCounterAvailability.GcReservedMemory,
			PerfMeterCounterAvailability.GpuMemory
		};
		private readonly RecorderSlot[] _recorders =
		{
			// Unity 6000 exposes draw/batch totals as component counters; matching logical counters are summed when read.
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.DrawCalls, "Standard Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.DrawCalls, "Standard Indirect Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.DrawCalls, "Standard Instanced Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.DrawCalls, "SRP Batcher Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.DrawCalls, "BRG Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.DrawCalls, "BRG Indirect Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.DrawCalls, "Null Geometry Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.DrawCalls, "Null Geometry Indirect Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.SetPassCalls, "SetPass Calls Count", "SetPass Calls"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.Batches, "Dynamic Batches Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.Batches, "Static Batches Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.Batches, "Instanced Batches Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.Vertices, "Vertices Count", "Vertices"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.SrpBatcherInstances, "SRP Batcher Instances Count", "SRP Batcher Instances"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.BrgDrawCalls, "BRG Draw Calls Count", "Hybrid Renderer (BRG) Draw Calls Count", "BatchRendererGroup Draw Calls Count", "GPU Resident Drawer Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.BrgDrawCalls, "BRG Indirect Draw Calls Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.BrgInstances, "BRG Instances Count", "Hybrid Renderer (BRG) Instances Count", "BatchRendererGroup Instances Count", "GPU Resident Drawer Instances Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.BrgInstances, "BRG Indirect Instances Count"),
			new RecorderSlot(ProfilerCategory.Render, PerfMeterCounterAvailability.IndexBufferUploadInFrameBytes, "Index Buffer Upload In Frame Bytes", "Index Buffer Upload Bytes", "Index Buffer Upload In Frame"),
			new RecorderSlot(ProfilerCategory.Memory, PerfMeterCounterAvailability.SystemUsedMemory, "System Used Memory"),
			new RecorderSlot(ProfilerCategory.Memory, PerfMeterCounterAvailability.GcReservedMemory, "GC Reserved Memory"),
			new RecorderSlot(ProfilerCategory.Memory, PerfMeterCounterAvailability.GpuMemory, "Gfx Used Memory", "GPU Used Memory", "Graphics Used Memory", "GPU Memory")
		};

		private PerfMeterCounterAvailability _availableCounters;
		private PerfMeterCounterAvailability _unavailableCounters;
		private string _lastError = string.Empty;
		private bool _isRunning;

		internal PerfMeterCounterAvailability AvailableCounters => _availableCounters;
		internal PerfMeterCounterAvailability UnavailableCounters => _unavailableCounters;
		internal string LastError => _lastError;

		internal void Start()
		{
			if (_isRunning)
			{
				return;
			}

			_availableCounters = PerfMeterCounterAvailability.None;
			_unavailableCounters = PerfMeterCounterAvailability.None;
			_lastError = string.Empty;

			for (int i = 0; i < _recorders.Length; i++)
			{
				_recorders[i].Start(ref _lastError);
			}
			RefreshCounterAvailability();

			_isRunning = true;
		}

		internal void Stop()
		{
			for (int i = 0; i < _recorders.Length; i++)
			{
				_recorders[i].Dispose();
			}

			_isRunning = false;
		}

		internal PerfMeterMetricsSnapshot Collect(int collectionFrame, double frameBudgetMs, out PerfMeterFrameTimingAvailability frameTimingAvailability, out string warning)
		{
			if (!_isRunning)
			{
				Start();
			}

			FrameTimingManager.CaptureFrameTimings();
			uint timingCount = FrameTimingManager.GetLatestTimings(1, _frameTimings);
			frameTimingAvailability = timingCount > 0 ? PerfMeterFrameTimingAvailability.Available : PerfMeterFrameTimingAvailability.Unavailable;

			FrameTiming timing = timingCount > 0 ? _frameTimings[0] : default;
			double cpuFrameTimeMs = timingCount > 0 ? timing.cpuFrameTime : 0d;
			double cpuMainThreadFrameTimeMs = timingCount > 0 ? timing.cpuMainThreadFrameTime : 0d;
			double cpuRenderThreadFrameTimeMs = timingCount > 0 ? timing.cpuRenderThreadFrameTime : 0d;
			double cpuMainThreadPresentWaitTimeMs = timingCount > 0 ? timing.cpuMainThreadPresentWaitTime : 0d;
			double gpuFrameTimeMs = timingCount > 0 ? timing.gpuFrameTime : 0d;
			bool gpuFrameTimeAvailable = gpuFrameTimeMs > 0d;

			PerfMeterBottleneck bottleneck = ClassifyBottleneck(
				frameTimingAvailability,
				frameBudgetMs,
				cpuFrameTimeMs,
				cpuMainThreadFrameTimeMs,
				cpuRenderThreadFrameTimeMs,
				cpuMainThreadPresentWaitTimeMs,
				gpuFrameTimeMs,
				gpuFrameTimeAvailable);

			warning = GetWarning(gpuFrameTimeAvailable);

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
			long value = 0L;
			for (int i = 0; i < _recorders.Length; i++)
			{
				if (_recorders[i].Counter == counter)
				{
					value += _recorders[i].LastValue;
				}
			}

			return value;
		}

		private void RefreshCounterAvailability()
		{
			_availableCounters = PerfMeterCounterAvailability.None;
			_unavailableCounters = PerfMeterCounterAvailability.None;

			for (int i = 0; i < _trackedCounters.Length; i++)
			{
				PerfMeterCounterAvailability counter = _trackedCounters[i];
				if (HasValidRecorder(counter))
				{
					_availableCounters |= counter;
				}
				else
				{
					_unavailableCounters |= counter;
				}
			}
		}

		private bool HasValidRecorder(PerfMeterCounterAvailability counter)
		{
			for (int i = 0; i < _recorders.Length; i++)
			{
				if (_recorders[i].Counter == counter && _recorders[i].IsValid)
				{
					return true;
				}
			}

			return false;
		}

		private string GetWarning(bool gpuFrameTimeAvailable)
		{
			if (UsesOpenGlGpuTiming())
			{
				return OpenGlGpuTimingWarning;
			}

			if (!gpuFrameTimeAvailable)
			{
				return MissingGpuTimingWarning;
			}

			return _unavailableCounters != PerfMeterCounterAvailability.None ? MissingCountersWarning : string.Empty;
		}

		private static bool UsesOpenGlGpuTiming()
		{
			GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
			return deviceType == GraphicsDeviceType.OpenGLES3 || deviceType == GraphicsDeviceType.OpenGLCore;
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

			bool mainOverBudget = cpuMainThreadFrameTimeMs > frameBudgetMs;
			bool renderOverBudget = cpuRenderThreadFrameTimeMs > frameBudgetMs;
			bool gpuOverBudget = gpuFrameTimeAvailable && gpuFrameTimeMs > frameBudgetMs;
			bool hasSignificantPresentWait = cpuMainThreadPresentWaitTimeMs > 1d ||
				(cpuFrameTimeMs > 0d && cpuMainThreadPresentWaitTimeMs / cpuFrameTimeMs >= 0.25d);
			double cpuMainWorkTimeMs = System.Math.Max(0d, cpuMainThreadFrameTimeMs - cpuMainThreadPresentWaitTimeMs);
			bool workBelowBudget = cpuMainWorkTimeMs < frameBudgetMs * 0.85d &&
				cpuRenderThreadFrameTimeMs < frameBudgetMs * 0.85d &&
				(!gpuFrameTimeAvailable || gpuFrameTimeMs < frameBudgetMs * 0.85d);

			if (hasSignificantPresentWait && workBelowBudget)
			{
				return PerfMeterBottleneck.PresentLimited;
			}

			if (gpuOverBudget)
			{
				return PerfMeterBottleneck.GpuBound;
			}

			if (mainOverBudget)
			{
				return PerfMeterBottleneck.CpuMainThreadBound;
			}

			if (renderOverBudget)
			{
				return PerfMeterBottleneck.CpuRenderThreadBound;
			}

			return PerfMeterBottleneck.Balanced;
		}

		private struct RecorderSlot
		{
			private readonly ProfilerCategory _category;
			private readonly string[] _names;
			private ProfilerRecorder _recorder;

			internal RecorderSlot(ProfilerCategory category, PerfMeterCounterAvailability counter, params string[] names)
			{
				_category = category;
				_names = names;
				Counter = counter;
				_recorder = default;
			}

			internal PerfMeterCounterAvailability Counter { get; }
			internal bool IsValid => _recorder.Valid;

			internal long LastValue => _recorder.Valid ? _recorder.LastValue : 0L;

			internal void Start(ref string lastError)
			{
				Dispose();
				string exceptionMessage = string.Empty;

				for (int i = 0; i < _names.Length; i++)
				{
					try
					{
						_recorder = ProfilerRecorder.StartNew(_category, _names[i], 1);
						if (_recorder.Valid)
						{
							return;
						}

						Dispose();
					}
					catch (System.Exception exception)
					{
						exceptionMessage = exception.Message;
						Dispose();
					}
				}

				if (!string.IsNullOrEmpty(exceptionMessage) && !lastError.Contains(exceptionMessage))
				{
					lastError = string.IsNullOrEmpty(lastError) ? exceptionMessage : lastError + " " + exceptionMessage;
				}
			}

			internal void Dispose()
			{
				if (_recorder.Valid)
				{
					_recorder.Dispose();
				}

				_recorder = default;
			}
		}
	}
}
