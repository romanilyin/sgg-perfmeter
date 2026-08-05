using System;
using System.Diagnostics;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal static class PerfMeterSelfObservability
	{
		internal const int WindowSizeFrames = 120;
		internal const long CollectorCpuBudgetNanoseconds = 500000L;
		internal const long CustomMetricProvidersCpuBudgetNanoseconds = 500000L;
		internal const long CpuCoreProviderCpuBudgetNanoseconds = 1000000L;
		internal const long OverlayCpuBudgetNanoseconds = 2000000L;
		internal const long RenderIntegrationCpuBudgetNanoseconds = 500000L;
		internal const long CollectorAllocationBudgetBytes = 0L;
		internal const long CustomMetricProvidersAllocationBudgetBytes = 4096L;
		internal const long CpuCoreProviderAllocationBudgetBytes = 0L;
		internal const long OverlayAllocationBudgetBytes = 131072L;
		internal const long RenderIntegrationAllocationBudgetBytes = 0L;

		private static ComponentAccumulator _collector;
		private static ComponentAccumulator _customMetricProviders;
		private static ComponentAccumulator _cpuCoreProvider;
		private static ComponentAccumulator _overlay;
		private static ComponentAccumulator _urpRenderIntegration;
		private static ComponentAccumulator _hdrpRenderIntegration;
		private static bool _enabled;

		internal static void Start(PerfMeterRenderPipelineKind renderPipeline)
		{
			_enabled = true;
			_collector.Reset(true);
			_customMetricProviders.Reset(true);
			_cpuCoreProvider.Reset(true);
			_overlay.Reset(true);
			_urpRenderIntegration.Reset(renderPipeline == PerfMeterRenderPipelineKind.Universal);
			_hdrpRenderIntegration.Reset(renderPipeline == PerfMeterRenderPipelineKind.HighDefinition);
		}

		internal static void EnsureStarted(PerfMeterRenderPipelineKind renderPipeline)
		{
			if (!_enabled)
			{
				Start(renderPipeline);
			}
		}

		internal static void Stop()
		{
			_enabled = false;
			_collector = default;
			_customMetricProviders = default;
			_cpuCoreProvider = default;
			_overlay = default;
			_urpRenderIntegration = default;
			_hdrpRenderIntegration = default;
		}

		internal static MeasurementScope Measure(PerfMeterSelfOverheadComponent component)
		{
			return new MeasurementScope(component, _enabled && IsSupported(component));
		}

		internal static void ResetComponent(PerfMeterSelfOverheadComponent component)
		{
			ref ComponentAccumulator accumulator = ref GetAccumulator(component);
			accumulator.Reset(accumulator.Supported);
		}

		internal static PerfMeterSelfOverheadSnapshot GetSnapshot()
		{
			return GetSnapshot(Time.frameCount);
		}

		internal static PerfMeterSelfOverheadSnapshot GetSnapshotForTesting(int frame)
		{
			return GetSnapshot(frame);
		}

		private static PerfMeterSelfOverheadSnapshot GetSnapshot(int frame)
		{
			if (!_enabled)
			{
				return PerfMeterSelfOverheadSnapshot.NotInitialized;
			}

			PerfMeterSelfOverheadComponentSnapshot collector = _collector.GetSnapshot(PerfMeterSelfOverheadComponent.Collector, frame, CollectorCpuBudgetNanoseconds, CollectorAllocationBudgetBytes);
			PerfMeterSelfOverheadComponentSnapshot customMetricProviders = _customMetricProviders.GetSnapshot(PerfMeterSelfOverheadComponent.CustomMetricProviders, frame, CustomMetricProvidersCpuBudgetNanoseconds, CustomMetricProvidersAllocationBudgetBytes);
			PerfMeterSelfOverheadComponentSnapshot cpuCoreProvider = _cpuCoreProvider.GetSnapshot(PerfMeterSelfOverheadComponent.CpuCoreProvider, frame, CpuCoreProviderCpuBudgetNanoseconds, CpuCoreProviderAllocationBudgetBytes);
			PerfMeterSelfOverheadComponentSnapshot overlay = _overlay.GetSnapshot(PerfMeterSelfOverheadComponent.Overlay, frame, OverlayCpuBudgetNanoseconds, OverlayAllocationBudgetBytes);
			PerfMeterSelfOverheadComponentSnapshot urp = _urpRenderIntegration.GetSnapshot(PerfMeterSelfOverheadComponent.UrpRenderIntegration, frame, RenderIntegrationCpuBudgetNanoseconds, RenderIntegrationAllocationBudgetBytes);
			PerfMeterSelfOverheadComponentSnapshot hdrp = _hdrpRenderIntegration.GetSnapshot(PerfMeterSelfOverheadComponent.HdrpRenderIntegration, frame, RenderIntegrationCpuBudgetNanoseconds, RenderIntegrationAllocationBudgetBytes);
			PerfMeterSelfOverheadState state = collector.State == PerfMeterSelfOverheadComponentState.Ready
				? PerfMeterSelfOverheadState.Ready
				: PerfMeterSelfOverheadState.Collecting;

			return new PerfMeterSelfOverheadSnapshot(
				state,
				PerfMeterAvailability.Unavailable,
				collector,
				customMetricProviders,
				cpuCoreProvider,
				overlay,
				urp,
				hdrp);
		}

		internal static void RecordSampleForTesting(PerfMeterSelfOverheadComponent component, int frame, long elapsedNanoseconds, long allocatedBytes)
		{
			RecordSample(component, frame, elapsedNanoseconds, allocatedBytes);
		}

		internal static long StopwatchTicksToNanoseconds(long ticks)
		{
			if (ticks <= 0L)
			{
				return 0L;
			}

			double nanoseconds = ticks * (1000000000d / Stopwatch.Frequency);
			return nanoseconds >= long.MaxValue ? long.MaxValue : (long)Math.Round(nanoseconds);
		}

		private static bool IsSupported(PerfMeterSelfOverheadComponent component)
		{
			return GetAccumulator(component).Supported;
		}

		private static void RecordSample(PerfMeterSelfOverheadComponent component, int frame, long elapsedNanoseconds, long allocatedBytes)
		{
			if (!_enabled)
			{
				return;
			}

			ref ComponentAccumulator accumulator = ref GetAccumulator(component);
			if (accumulator.Supported)
			{
				accumulator.Record(frame, Math.Max(0L, elapsedNanoseconds), Math.Max(0L, allocatedBytes));
			}
		}

		private static ref ComponentAccumulator GetAccumulator(PerfMeterSelfOverheadComponent component)
		{
			switch (component)
			{
				case PerfMeterSelfOverheadComponent.Collector:
					return ref _collector;
				case PerfMeterSelfOverheadComponent.CustomMetricProviders:
					return ref _customMetricProviders;
				case PerfMeterSelfOverheadComponent.CpuCoreProvider:
					return ref _cpuCoreProvider;
				case PerfMeterSelfOverheadComponent.Overlay:
					return ref _overlay;
				case PerfMeterSelfOverheadComponent.UrpRenderIntegration:
					return ref _urpRenderIntegration;
				case PerfMeterSelfOverheadComponent.HdrpRenderIntegration:
					return ref _hdrpRenderIntegration;
				default:
					throw new ArgumentOutOfRangeException(nameof(component), component, null);
			}
		}

		internal readonly struct MeasurementScope : IDisposable
		{
			private readonly PerfMeterSelfOverheadComponent _component;
			private readonly long _startTimestamp;
			private readonly long _startAllocatedBytes;
			private readonly bool _active;

			internal MeasurementScope(PerfMeterSelfOverheadComponent component, bool active)
			{
				_component = component;
				_active = active;
				_startTimestamp = active ? Stopwatch.GetTimestamp() : 0L;
				_startAllocatedBytes = active ? GC.GetAllocatedBytesForCurrentThread() : 0L;
			}

			public void Dispose()
			{
				if (!_active)
				{
					return;
				}

				long elapsedNanoseconds = StopwatchTicksToNanoseconds(Stopwatch.GetTimestamp() - _startTimestamp);
				long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - _startAllocatedBytes;
				RecordSample(_component, Time.frameCount, elapsedNanoseconds, allocatedBytes);
			}
		}

		private struct ComponentAccumulator
		{
			private Window _current;
			private Window _latest;

			internal bool Supported { get; private set; }

			internal void Reset(bool supported)
			{
				Supported = supported;
				_current.Clear();
				_latest.Clear();
			}

			internal void Record(int frame, long elapsedNanoseconds, long allocatedBytes)
			{
				if (!_current.Initialized)
				{
					_current.Start(frame);
				}
				else
				{
					Advance(frame);
				}

				_current.Add(elapsedNanoseconds, allocatedBytes);
			}

			internal PerfMeterSelfOverheadComponentSnapshot GetSnapshot(PerfMeterSelfOverheadComponent component, int frame, long cpuBudgetNanoseconds, long allocationBudgetBytes)
			{
				if (!Supported)
				{
					return new PerfMeterSelfOverheadComponentSnapshot(
						component,
						PerfMeterSelfOverheadComponentState.Unsupported,
						0,
						0,
						0d,
						0d,
						0L,
						0d,
						NanosecondsToMilliseconds(cpuBudgetNanoseconds),
						allocationBudgetBytes,
						PerfMeterSelfOverheadBudgetState.NotEvaluated,
						PerfMeterSelfOverheadBudgetState.NotEvaluated);
				}

				Advance(frame);
				Window window = _latest.InvocationCount > 0 ? _latest : _current;
				if (window.InvocationCount <= 0)
				{
					return new PerfMeterSelfOverheadComponentSnapshot(
						component,
						PerfMeterSelfOverheadComponentState.NotMeasured,
						0,
						0,
						0d,
						0d,
						0L,
						0d,
						NanosecondsToMilliseconds(cpuBudgetNanoseconds),
						allocationBudgetBytes,
						PerfMeterSelfOverheadBudgetState.NotEvaluated,
						PerfMeterSelfOverheadBudgetState.NotEvaluated);
				}

				bool ready = _latest.InvocationCount > 0;
				int frameCount = ready
					? window.FrameCount
					: Math.Min(WindowSizeFrames, (int)Math.Min(int.MaxValue, (long)unchecked((uint)(frame - window.StartFrame)) + 1L));
				double averageNanoseconds = (double)window.ElapsedNanoseconds / window.InvocationCount;
				double averageAllocatedBytes = (double)window.AllocatedBytes / window.InvocationCount;
				return new PerfMeterSelfOverheadComponentSnapshot(
					component,
					ready ? PerfMeterSelfOverheadComponentState.Ready : PerfMeterSelfOverheadComponentState.Collecting,
					frameCount,
					window.InvocationCount,
					NanosecondsToMilliseconds(averageNanoseconds),
					NanosecondsToMilliseconds(window.MaxElapsedNanoseconds),
					window.AllocatedBytes,
					averageAllocatedBytes,
					NanosecondsToMilliseconds(cpuBudgetNanoseconds),
					allocationBudgetBytes,
					averageNanoseconds <= cpuBudgetNanoseconds ? PerfMeterSelfOverheadBudgetState.WithinBudget : PerfMeterSelfOverheadBudgetState.Exceeded,
					averageAllocatedBytes <= allocationBudgetBytes ? PerfMeterSelfOverheadBudgetState.WithinBudget : PerfMeterSelfOverheadBudgetState.Exceeded);
			}

			private void Advance(int frame)
			{
				if (!_current.Initialized)
				{
					return;
				}

				uint elapsedFrames = unchecked((uint)(frame - _current.StartFrame));
				if (elapsedFrames < WindowSizeFrames)
				{
					return;
				}

				if (_current.InvocationCount > 0)
				{
					_current.FrameCount = WindowSizeFrames;
					_latest = _current;
				}

				uint elapsedWindows = elapsedFrames / WindowSizeFrames;
				int frameOffset = unchecked((int)(elapsedWindows * (uint)WindowSizeFrames));
				_current.Start(unchecked(_current.StartFrame + frameOffset));
			}
		}

		private struct Window
		{
			internal bool Initialized;
			internal int StartFrame;
			internal int FrameCount;
			internal int InvocationCount;
			internal long ElapsedNanoseconds;
			internal long MaxElapsedNanoseconds;
			internal long AllocatedBytes;

			internal void Start(int frame)
			{
				Initialized = true;
				StartFrame = frame;
				FrameCount = 0;
				InvocationCount = 0;
				ElapsedNanoseconds = 0L;
				MaxElapsedNanoseconds = 0L;
				AllocatedBytes = 0L;
			}

			internal void Clear()
			{
				Initialized = false;
				StartFrame = 0;
				FrameCount = 0;
				InvocationCount = 0;
				ElapsedNanoseconds = 0L;
				MaxElapsedNanoseconds = 0L;
				AllocatedBytes = 0L;
			}

			internal void Add(long elapsedNanoseconds, long allocatedBytes)
			{
				InvocationCount++;
				ElapsedNanoseconds = SaturatingAdd(ElapsedNanoseconds, elapsedNanoseconds);
				MaxElapsedNanoseconds = Math.Max(MaxElapsedNanoseconds, elapsedNanoseconds);
				AllocatedBytes = SaturatingAdd(AllocatedBytes, allocatedBytes);
			}
		}

		private static double NanosecondsToMilliseconds(double nanoseconds)
		{
			return Math.Max(0d, nanoseconds) / 1000000d;
		}

		private static long SaturatingAdd(long left, long right)
		{
			return right > long.MaxValue - left ? long.MaxValue : left + right;
		}
	}
}
