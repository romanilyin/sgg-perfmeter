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
		private static BoundWindow _sessionWindow;
		private static BoundWindow _captureWindow;
		private static UrpEvidence _liveUrpEvidence;
		private static PerfMeterRenderPipelineKind _renderPipeline;
		private static long _nextEpoch;
		private static long _runtimeEpoch;
		private static bool _enabled;
		private static Func<UrpConfigurationEvidence> _urpConfigurationProbe;

		internal static void Start(PerfMeterRenderPipelineKind renderPipeline)
		{
			_enabled = true;
			_renderPipeline = renderPipeline;
			_runtimeEpoch = NextEpoch();
			_liveUrpEvidence.Clear();
			_collector.Reset(true, _runtimeEpoch);
			_customMetricProviders.Reset(true, _runtimeEpoch);
			_cpuCoreProvider.Reset(true, _runtimeEpoch);
			_overlay.Reset(true, _runtimeEpoch);
			_urpRenderIntegration.Reset(renderPipeline == PerfMeterRenderPipelineKind.Universal, _runtimeEpoch);
			_hdrpRenderIntegration.Reset(renderPipeline == PerfMeterRenderPipelineKind.HighDefinition, _runtimeEpoch);
			_sessionWindow.Clear();
			_captureWindow.Clear();
			ProbeUrpConfiguration(Time.frameCount);
		}

		internal static void EnsureStarted(PerfMeterRenderPipelineKind renderPipeline)
		{
			if (!_enabled)
			{
				Start(renderPipeline);
			}
			else if (_renderPipeline != renderPipeline)
			{
				_sessionWindow.MarkPipelineChanged();
				_captureWindow.MarkPipelineChanged();
				_renderPipeline = renderPipeline;
				_runtimeEpoch = NextEpoch();
				_liveUrpEvidence.Clear();
				_urpRenderIntegration.Reset(renderPipeline == PerfMeterRenderPipelineKind.Universal, _runtimeEpoch);
				_hdrpRenderIntegration.Reset(renderPipeline == PerfMeterRenderPipelineKind.HighDefinition, _runtimeEpoch);
				ProbeUrpConfiguration(Time.frameCount);
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
			_sessionWindow = default;
			_captureWindow = default;
			_liveUrpEvidence = default;
			_renderPipeline = PerfMeterRenderPipelineKind.Unknown;
			_runtimeEpoch = 0L;
		}

		internal static MeasurementScope Measure(PerfMeterSelfOverheadComponent component)
		{
			return new MeasurementScope(component, _enabled && IsSupported(component));
		}

		internal static void ResetComponent(PerfMeterSelfOverheadComponent component)
		{
			ref ComponentAccumulator accumulator = ref GetAccumulator(component);
			accumulator.Reset(accumulator.Supported, NextEpoch());
		}

		internal static void ReportUrpFeatureState(
			PerfMeterUrpFeatureInstallationState installation,
			PerfMeterUrpFeatureEnabledState enabled,
			bool enqueued,
			int frame,
			string rendererName,
			string rendererType)
		{
			if (!_enabled)
			{
				return;
			}

			_liveUrpEvidence.Record(installation, enabled, enqueued, frame, rendererName, rendererType);
			_sessionWindow.RecordEvidence(installation, enabled, enqueued, frame, rendererName, rendererType);
			_captureWindow.RecordEvidence(installation, enabled, enqueued, frame, rendererName, rendererType);
		}

		internal static void RegisterUrpConfigurationProbe(Func<UrpConfigurationEvidence> probe)
		{
			_urpConfigurationProbe = probe;
			if (_enabled)
			{
				ProbeUrpConfiguration(Time.frameCount);
			}
		}

		internal static long BeginBoundWindow(PerfMeterSelfOverheadWindowKind kind, string identity, int startFrame)
		{
			EnsureStarted(PerfMeterRenderPipelineDetector.GetActiveKind());
			ref BoundWindow window = ref GetBoundWindow(kind);
			if (window.Active && string.Equals(window.Identity, identity, StringComparison.Ordinal))
			{
				return window.Epoch;
			}

			window.Begin(kind, identity, NextEpoch(), startFrame, _renderPipeline);
			ProbeUrpConfiguration(startFrame);
			return window.Epoch;
		}

		internal static PerfMeterSelfOverheadWindowSnapshot EndBoundWindow(PerfMeterSelfOverheadWindowKind kind, string identity, int endFrame)
		{
			ref BoundWindow window = ref GetBoundWindow(kind);
			return window.End(identity, endFrame, _enabled);
		}

		internal static PerfMeterSelfOverheadWindowSnapshot GetBoundWindowSnapshot(PerfMeterSelfOverheadWindowKind kind, string identity, int frame)
		{
			ref BoundWindow window = ref GetBoundWindow(kind);
			return window.GetSnapshot(identity, frame, _enabled);
		}

		internal static void BeginBoundWindowForTesting(PerfMeterSelfOverheadWindowKind kind, string identity, int startFrame, PerfMeterRenderPipelineKind renderPipeline, bool playModeActive = true, bool integrationTypeAvailable = true)
		{
			ref BoundWindow window = ref GetBoundWindow(kind);
			window.Begin(kind, identity, NextEpoch(), startFrame, renderPipeline, playModeActive, integrationTypeAvailable);
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

			PerfMeterSelfOverheadComponentSnapshot collector = _collector.GetSnapshot(PerfMeterSelfOverheadComponent.Collector, frame, CollectorCpuBudgetNanoseconds, CollectorAllocationBudgetBytes, PerfMeterSelfOverheadInactiveReason.None);
			PerfMeterSelfOverheadComponentSnapshot customMetricProviders = _customMetricProviders.GetSnapshot(PerfMeterSelfOverheadComponent.CustomMetricProviders, frame, CustomMetricProvidersCpuBudgetNanoseconds, CustomMetricProvidersAllocationBudgetBytes, PerfMeterSelfOverheadInactiveReason.None);
			PerfMeterSelfOverheadComponentSnapshot cpuCoreProvider = _cpuCoreProvider.GetSnapshot(PerfMeterSelfOverheadComponent.CpuCoreProvider, frame, CpuCoreProviderCpuBudgetNanoseconds, CpuCoreProviderAllocationBudgetBytes, PerfMeterSelfOverheadInactiveReason.None);
			PerfMeterSelfOverheadComponentSnapshot overlay = _overlay.GetSnapshot(PerfMeterSelfOverheadComponent.Overlay, frame, OverlayCpuBudgetNanoseconds, OverlayAllocationBudgetBytes, PerfMeterSelfOverheadInactiveReason.None);
			PerfMeterSelfOverheadComponentSnapshot urp = _urpRenderIntegration.GetSnapshot(PerfMeterSelfOverheadComponent.UrpRenderIntegration, frame, RenderIntegrationCpuBudgetNanoseconds, RenderIntegrationAllocationBudgetBytes, GetLiveUrpInactiveReason(frame));
			PerfMeterSelfOverheadComponentSnapshot hdrp = _hdrpRenderIntegration.GetSnapshot(PerfMeterSelfOverheadComponent.HdrpRenderIntegration, frame, RenderIntegrationCpuBudgetNanoseconds, RenderIntegrationAllocationBudgetBytes, PerfMeterSelfOverheadInactiveReason.None);
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
				if (component == PerfMeterSelfOverheadComponent.UrpRenderIntegration)
				{
					_sessionWindow.RecordMeasurement(frame, Math.Max(0L, elapsedNanoseconds), Math.Max(0L, allocatedBytes));
					_captureWindow.RecordMeasurement(frame, Math.Max(0L, elapsedNanoseconds), Math.Max(0L, allocatedBytes));
				}
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
			private long _epoch;

			internal bool Supported { get; private set; }

			internal void Reset(bool supported, long epoch)
			{
				Supported = supported;
				_epoch = epoch;
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

				_current.Add(frame, elapsedNanoseconds, allocatedBytes);
			}

			internal PerfMeterSelfOverheadComponentSnapshot GetSnapshot(PerfMeterSelfOverheadComponent component, int frame, long cpuBudgetNanoseconds, long allocationBudgetBytes, PerfMeterSelfOverheadInactiveReason inactiveReason)
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
						PerfMeterSelfOverheadBudgetState.NotEvaluated,
						_epoch,
						-1,
						-1,
						0,
						inactiveReason,
						PerfMeterAvailability.Unavailable);
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
						PerfMeterSelfOverheadBudgetState.NotEvaluated,
						_epoch,
						-1,
						-1,
						0,
						inactiveReason,
						PerfMeterAvailability.Unavailable);
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
					averageAllocatedBytes <= allocationBudgetBytes ? PerfMeterSelfOverheadBudgetState.WithinBudget : PerfMeterSelfOverheadBudgetState.Exceeded,
					_epoch,
					window.StartFrame,
					window.LastFrame,
					window.CallbackFrameCount,
					ready ? PerfMeterSelfOverheadInactiveReason.None : PerfMeterSelfOverheadInactiveReason.WindowIncomplete,
					PerfMeterAvailability.Unavailable);
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
			internal int LastFrame;
			internal int CallbackFrameCount;
			internal long ElapsedNanoseconds;
			internal long MaxElapsedNanoseconds;
			internal long AllocatedBytes;

			internal void Start(int frame)
			{
				Initialized = true;
				StartFrame = frame;
				FrameCount = 0;
				InvocationCount = 0;
				LastFrame = -1;
				CallbackFrameCount = 0;
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
				LastFrame = -1;
				CallbackFrameCount = 0;
				ElapsedNanoseconds = 0L;
				MaxElapsedNanoseconds = 0L;
				AllocatedBytes = 0L;
			}

			internal void Add(int frame, long elapsedNanoseconds, long allocatedBytes)
			{
				if (LastFrame != frame)
				{
					if (CallbackFrameCount < int.MaxValue)
					{
						CallbackFrameCount++;
					}
					LastFrame = frame;
				}
				if (InvocationCount < int.MaxValue)
				{
					InvocationCount++;
				}
				ElapsedNanoseconds = SaturatingAdd(ElapsedNanoseconds, elapsedNanoseconds);
				MaxElapsedNanoseconds = Math.Max(MaxElapsedNanoseconds, elapsedNanoseconds);
				AllocatedBytes = SaturatingAdd(AllocatedBytes, allocatedBytes);
			}
		}

		private static PerfMeterSelfOverheadInactiveReason GetLiveUrpInactiveReason(int frame)
		{
			if (!_enabled)
			{
				return PerfMeterSelfOverheadInactiveReason.RuntimeNotRunning;
			}

			if (!Application.isPlaying)
			{
				return PerfMeterSelfOverheadInactiveReason.PlayModeInactive;
			}

			if (_renderPipeline != PerfMeterRenderPipelineKind.Universal)
			{
				return PerfMeterSelfOverheadInactiveReason.PipelineNotUrp;
			}

			if (!PerfMeterRenderGraphAnalytics.IsRenderGraphFeatureAvailable())
			{
				return PerfMeterSelfOverheadInactiveReason.IntegrationTypeUnavailable;
			}

			if (!_liveUrpEvidence.Observed || unchecked((uint)(frame - _liveUrpEvidence.LastFrame)) > WindowSizeFrames)
			{
				return PerfMeterSelfOverheadInactiveReason.UnknownInactiveReason;
			}

			return DeriveInactiveReason(_liveUrpEvidence.Installation, _liveUrpEvidence.Enabled, _liveUrpEvidence.EnqueueCount);
		}

		private static void ProbeUrpConfiguration(int frame)
		{
			if (!_enabled || _renderPipeline != PerfMeterRenderPipelineKind.Universal || _urpConfigurationProbe == null)
			{
				return;
			}

			try
			{
				UrpConfigurationEvidence evidence = _urpConfigurationProbe();
				ReportUrpFeatureState(evidence.Installation, evidence.Enabled, false, frame, evidence.RendererName, evidence.RendererType);
			}
			catch (Exception)
			{
			}
		}

		private static PerfMeterSelfOverheadInactiveReason DeriveInactiveReason(
			PerfMeterUrpFeatureInstallationState installation,
			PerfMeterUrpFeatureEnabledState enabled,
			int enqueueCount)
		{
			if (installation == PerfMeterUrpFeatureInstallationState.NotInstalled)
			{
				return PerfMeterSelfOverheadInactiveReason.RendererFeatureNotInstalled;
			}

			if (enabled == PerfMeterUrpFeatureEnabledState.Disabled)
			{
				return PerfMeterSelfOverheadInactiveReason.RendererFeatureDisabled;
			}

			if (installation == PerfMeterUrpFeatureInstallationState.Installed && enabled == PerfMeterUrpFeatureEnabledState.Enabled)
			{
				return enqueueCount > 0
					? PerfMeterSelfOverheadInactiveReason.NoCameraCallbackObserved
					: PerfMeterSelfOverheadInactiveReason.PassNotEnqueued;
			}

			return PerfMeterSelfOverheadInactiveReason.UnknownInactiveReason;
		}

		private static ref BoundWindow GetBoundWindow(PerfMeterSelfOverheadWindowKind kind)
		{
			switch (kind)
			{
				case PerfMeterSelfOverheadWindowKind.Session:
					return ref _sessionWindow;
				case PerfMeterSelfOverheadWindowKind.Capture:
					return ref _captureWindow;
				default:
					throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
			}
		}

		private static long NextEpoch()
		{
			if (_nextEpoch < long.MaxValue)
			{
				_nextEpoch++;
			}

			return _nextEpoch;
		}

		private struct UrpEvidence
		{
			internal bool Observed;
			internal PerfMeterUrpFeatureInstallationState Installation;
			internal PerfMeterUrpFeatureEnabledState Enabled;
			internal int EnqueueCount;
			internal int FirstEnqueueFrame;
			internal int LastEnqueueFrame;
			internal int LastFrame;
			internal string RendererName;
			internal string RendererType;

			internal void Record(
				PerfMeterUrpFeatureInstallationState installation,
				PerfMeterUrpFeatureEnabledState enabled,
				bool enqueued,
				int frame,
				string rendererName,
				string rendererType)
			{
				Observed = true;
				Installation = installation;
				Enabled = enabled;
				LastFrame = frame;
				string normalizedRendererName = NormalizeEvidenceText(rendererName);
				string normalizedRendererType = NormalizeEvidenceText(rendererType);
				if (!string.IsNullOrEmpty(normalizedRendererName))
				{
					RendererName = normalizedRendererName;
				}
				if (!string.IsNullOrEmpty(normalizedRendererType))
				{
					RendererType = normalizedRendererType;
				}
				if (enqueued)
				{
					if (EnqueueCount == 0)
					{
						FirstEnqueueFrame = frame;
					}

					if (EnqueueCount < int.MaxValue)
					{
						EnqueueCount++;
					}
					LastEnqueueFrame = frame;
				}
			}

			internal void Clear()
			{
				Observed = false;
				Installation = PerfMeterUrpFeatureInstallationState.Unknown;
				Enabled = PerfMeterUrpFeatureEnabledState.Unknown;
				EnqueueCount = 0;
				FirstEnqueueFrame = -1;
				LastEnqueueFrame = -1;
				LastFrame = -1;
				RendererName = string.Empty;
				RendererType = string.Empty;
			}
		}

		internal readonly struct UrpConfigurationEvidence
		{
			internal UrpConfigurationEvidence(
				PerfMeterUrpFeatureInstallationState installation,
				PerfMeterUrpFeatureEnabledState enabled,
				string rendererName,
				string rendererType)
			{
				Installation = installation;
				Enabled = enabled;
				RendererName = rendererName ?? string.Empty;
				RendererType = rendererType ?? string.Empty;
			}

			internal PerfMeterUrpFeatureInstallationState Installation { get; }
			internal PerfMeterUrpFeatureEnabledState Enabled { get; }
			internal string RendererName { get; }
			internal string RendererType { get; }
		}

		private struct BoundWindow
		{
			private PerfMeterSelfOverheadWindowSnapshot _completed;
			private Window _measurement;
			private UrpEvidence _evidence;
			private PerfMeterRenderPipelineSnapshot _pipeline;
			private PerfMeterRenderPipelineAssetSource _pipelineSource;
			private ulong _pipelineAssetEntityId;
			private int _qualityLevel;
			private string _qualityLevelName;
			private bool _playModeActive;
			private bool _integrationTypeAvailable;
			private bool _pipelineChanged;

			internal bool Active { get; private set; }
			internal PerfMeterSelfOverheadWindowKind Kind { get; private set; }
			internal string Identity { get; private set; }
			internal long Epoch { get; private set; }
			internal int StartFrame { get; private set; }

			internal void Begin(PerfMeterSelfOverheadWindowKind kind, string identity, long epoch, int startFrame, PerfMeterRenderPipelineKind renderPipeline)
			{
				PerfMeterRenderPipelineSnapshot pipeline;
				PerfMeterRenderPipelineAssetSource pipelineSource;
				ulong pipelineAssetEntityId;
				try
				{
					pipeline = PerfMeterRenderPipelineDetector.CreateSnapshot(out pipelineSource, out pipelineAssetEntityId);
				}
				catch (Exception)
				{
					pipeline = new PerfMeterRenderPipelineSnapshot(renderPipeline, string.Empty, string.Empty, string.Empty);
					pipelineSource = PerfMeterRenderPipelineAssetSource.None;
					pipelineAssetEntityId = 0UL;
				}

				int qualityLevel = -1;
				string qualityLevelName = string.Empty;
				try
				{
					qualityLevel = QualitySettings.GetQualityLevel();
					string[] names = QualitySettings.names;
					qualityLevelName = qualityLevel >= 0 && qualityLevel < names.Length ? names[qualityLevel] : string.Empty;
				}
				catch (Exception)
				{
				}

				BeginCore(
					kind,
					identity,
					epoch,
					startFrame,
					pipeline,
					pipelineSource,
					pipelineAssetEntityId,
					qualityLevel,
					qualityLevelName,
					Application.isPlaying,
					PerfMeterRenderGraphAnalytics.IsRenderGraphFeatureAvailable());
			}

			internal void Begin(PerfMeterSelfOverheadWindowKind kind, string identity, long epoch, int startFrame, PerfMeterRenderPipelineKind renderPipeline, bool playModeActive, bool integrationTypeAvailable)
			{
				BeginCore(
					kind,
					identity,
					epoch,
					startFrame,
					new PerfMeterRenderPipelineSnapshot(renderPipeline, string.Empty, string.Empty, string.Empty),
					PerfMeterRenderPipelineAssetSource.None,
					0UL,
					0,
					string.Empty,
					playModeActive,
					integrationTypeAvailable);
			}

			internal void RecordEvidence(
				PerfMeterUrpFeatureInstallationState installation,
				PerfMeterUrpFeatureEnabledState enabled,
				bool enqueued,
				int frame,
				string rendererName,
				string rendererType)
			{
				if (Active && frame >= StartFrame)
				{
					_evidence.Record(installation, enabled, enqueued, frame, rendererName, rendererType);
				}
			}

			internal void RecordMeasurement(int frame, long elapsedNanoseconds, long allocatedBytes)
			{
				if (!Active || frame < StartFrame)
				{
					return;
				}

				if (!_measurement.Initialized)
				{
					_measurement.Start(frame);
				}

				_measurement.Add(frame, elapsedNanoseconds, allocatedBytes);
			}

			internal void MarkPipelineChanged()
			{
				_pipelineChanged |= Active;
			}

			internal PerfMeterSelfOverheadWindowSnapshot End(string identity, int endFrame, bool runtimeEnabled)
			{
				if (!Active && _completed.Kind != PerfMeterSelfOverheadWindowKind.None && string.Equals(_completed.Identity, identity, StringComparison.Ordinal))
				{
					return _completed;
				}

				if (!Active || !string.Equals(Identity, identity, StringComparison.Ordinal))
				{
					return CreateMismatch(Kind, identity, endFrame);
				}

				_completed = CreateSnapshot(Math.Max(StartFrame, endFrame), true, runtimeEnabled);
				Active = false;
				return _completed;
			}

			internal PerfMeterSelfOverheadWindowSnapshot GetSnapshot(string identity, int frame, bool runtimeEnabled)
			{
				if (Active)
				{
					return string.IsNullOrEmpty(identity) || string.Equals(Identity, identity, StringComparison.Ordinal)
						? CreateSnapshot(Math.Max(StartFrame, frame), false, runtimeEnabled)
						: CreateMismatch(Kind, identity, frame);
				}

				if (_completed.Kind != PerfMeterSelfOverheadWindowKind.None)
				{
					return string.IsNullOrEmpty(identity) || string.Equals(_completed.Identity, identity, StringComparison.Ordinal)
						? _completed
						: CreateMismatch(Kind, identity, frame);
				}

				return PerfMeterSelfOverheadWindowSnapshot.Unavailable;
			}

			internal void Clear()
			{
				this = default;
			}

			private void BeginCore(
				PerfMeterSelfOverheadWindowKind kind,
				string identity,
				long epoch,
				int startFrame,
				PerfMeterRenderPipelineSnapshot pipeline,
				PerfMeterRenderPipelineAssetSource pipelineSource,
				ulong pipelineAssetEntityId,
				int qualityLevel,
				string qualityLevelName,
				bool playModeActive,
				bool integrationTypeAvailable)
			{
				Active = true;
				Kind = kind;
				Identity = identity ?? string.Empty;
				Epoch = epoch;
				StartFrame = startFrame;
				_pipeline = pipeline;
				_pipelineSource = pipelineSource;
				_pipelineAssetEntityId = pipelineAssetEntityId;
				_qualityLevel = qualityLevel;
				_qualityLevelName = NormalizeEvidenceText(qualityLevelName);
				_playModeActive = playModeActive;
				_integrationTypeAvailable = integrationTypeAvailable;
				_pipelineChanged = false;
				_measurement.Clear();
				_evidence.Clear();
				_completed = PerfMeterSelfOverheadWindowSnapshot.Unavailable;
			}

			private PerfMeterSelfOverheadWindowSnapshot CreateSnapshot(int endFrame, bool complete, bool runtimeEnabled)
			{
				PerfMeterSelfOverheadInactiveReason reason = GetInactiveReason(runtimeEnabled);
				PerfMeterSelfOverheadComponentState state = PerfMeterSelfOverheadComponentState.NotMeasured;
				int windowFrameCount = 0;
				if (_measurement.InvocationCount > 0)
				{
					windowFrameCount = (int)Math.Min(int.MaxValue, Math.Max(1L, (long)endFrame - _measurement.StartFrame + 1L));
					state = windowFrameCount >= WindowSizeFrames
						? PerfMeterSelfOverheadComponentState.Ready
						: PerfMeterSelfOverheadComponentState.Collecting;
					reason = state == PerfMeterSelfOverheadComponentState.Ready
						? PerfMeterSelfOverheadInactiveReason.None
						: PerfMeterSelfOverheadInactiveReason.WindowIncomplete;
				}
				if (_pipelineChanged)
				{
					reason = PerfMeterSelfOverheadInactiveReason.UnknownInactiveReason;
				}

				double averageNanoseconds = _measurement.InvocationCount > 0 ? (double)_measurement.ElapsedNanoseconds / _measurement.InvocationCount : 0d;
				double averageAllocatedBytes = _measurement.InvocationCount > 0 ? (double)_measurement.AllocatedBytes / _measurement.InvocationCount : 0d;
				PerfMeterSelfOverheadComponentSnapshot component = new PerfMeterSelfOverheadComponentSnapshot(
					PerfMeterSelfOverheadComponent.UrpRenderIntegration,
					state,
					windowFrameCount,
					_measurement.InvocationCount,
					NanosecondsToMilliseconds(averageNanoseconds),
					NanosecondsToMilliseconds(_measurement.MaxElapsedNanoseconds),
					_measurement.AllocatedBytes,
					averageAllocatedBytes,
					NanosecondsToMilliseconds(RenderIntegrationCpuBudgetNanoseconds),
					RenderIntegrationAllocationBudgetBytes,
					_measurement.InvocationCount > 0 ? averageNanoseconds <= RenderIntegrationCpuBudgetNanoseconds ? PerfMeterSelfOverheadBudgetState.WithinBudget : PerfMeterSelfOverheadBudgetState.Exceeded : PerfMeterSelfOverheadBudgetState.NotEvaluated,
					_measurement.InvocationCount > 0 ? averageAllocatedBytes <= RenderIntegrationAllocationBudgetBytes ? PerfMeterSelfOverheadBudgetState.WithinBudget : PerfMeterSelfOverheadBudgetState.Exceeded : PerfMeterSelfOverheadBudgetState.NotEvaluated,
					Epoch,
					_measurement.InvocationCount > 0 ? _measurement.StartFrame : -1,
					_measurement.InvocationCount > 0 ? _measurement.LastFrame : -1,
					_measurement.CallbackFrameCount,
					reason,
					PerfMeterAvailability.Unavailable);
				bool contained = complete && !_pipelineChanged && _measurement.InvocationCount > 0 && _measurement.StartFrame >= StartFrame && _measurement.LastFrame <= endFrame;
				return new PerfMeterSelfOverheadWindowSnapshot(
					Kind,
					Identity,
					Epoch,
					StartFrame,
					endFrame,
					complete,
					contained,
					_pipeline,
					_pipelineSource,
					_pipelineAssetEntityId,
					_qualityLevel,
					_qualityLevelName,
					_evidence.Installation,
					_evidence.Enabled,
					_evidence.EnqueueCount,
					_evidence.EnqueueCount > 0 ? _evidence.FirstEnqueueFrame : -1,
					_evidence.EnqueueCount > 0 ? _evidence.LastEnqueueFrame : -1,
					_evidence.RendererName,
					_evidence.RendererType,
					component,
					_pipelineChanged
						? "The active render pipeline changed during this self-overhead window; attribution fails closed."
						: GetReasonWarning(reason));
			}

			private PerfMeterSelfOverheadInactiveReason GetInactiveReason(bool runtimeEnabled)
			{
				if (!runtimeEnabled)
				{
					return PerfMeterSelfOverheadInactiveReason.RuntimeNotRunning;
				}

				if (!_playModeActive)
				{
					return PerfMeterSelfOverheadInactiveReason.PlayModeInactive;
				}

				if (_pipeline.Kind != PerfMeterRenderPipelineKind.Universal)
				{
					return PerfMeterSelfOverheadInactiveReason.PipelineNotUrp;
				}

				if (!_integrationTypeAvailable)
				{
					return PerfMeterSelfOverheadInactiveReason.IntegrationTypeUnavailable;
				}

				return _evidence.Observed
					? DeriveInactiveReason(_evidence.Installation, _evidence.Enabled, _evidence.EnqueueCount)
					: PerfMeterSelfOverheadInactiveReason.UnknownInactiveReason;
			}
		}

		private static PerfMeterSelfOverheadWindowSnapshot CreateMismatch(PerfMeterSelfOverheadWindowKind kind, string identity, int frame)
		{
			PerfMeterSelfOverheadComponentSnapshot component = new PerfMeterSelfOverheadComponentSnapshot(
				PerfMeterSelfOverheadComponent.UrpRenderIntegration,
				PerfMeterSelfOverheadComponentState.NotMeasured,
				0,
				0,
				0d,
				0d,
				0L,
				0d,
				NanosecondsToMilliseconds(RenderIntegrationCpuBudgetNanoseconds),
				RenderIntegrationAllocationBudgetBytes,
				PerfMeterSelfOverheadBudgetState.NotEvaluated,
				PerfMeterSelfOverheadBudgetState.NotEvaluated,
				0L,
				-1,
				-1,
				0,
				PerfMeterSelfOverheadInactiveReason.CaptureWindowMismatch,
				PerfMeterAvailability.Unavailable);
			return new PerfMeterSelfOverheadWindowSnapshot(
				kind,
				identity,
				0L,
				frame,
				frame,
				false,
				false,
				default,
				PerfMeterRenderPipelineAssetSource.None,
				0UL,
				-1,
				string.Empty,
				PerfMeterUrpFeatureInstallationState.Unknown,
				PerfMeterUrpFeatureEnabledState.Unknown,
				0,
				-1,
				-1,
				string.Empty,
				string.Empty,
				component,
				GetReasonWarning(PerfMeterSelfOverheadInactiveReason.CaptureWindowMismatch));
		}

		private static string GetReasonWarning(PerfMeterSelfOverheadInactiveReason reason)
		{
			switch (reason)
			{
				case PerfMeterSelfOverheadInactiveReason.None:
					return string.Empty;
				case PerfMeterSelfOverheadInactiveReason.RuntimeNotRunning:
					return "PerfMeter runtime was not running for this self-overhead window.";
				case PerfMeterSelfOverheadInactiveReason.PlayModeInactive:
					return "Play Mode was inactive for this self-overhead window.";
				case PerfMeterSelfOverheadInactiveReason.PipelineNotUrp:
					return "The active render pipeline was not URP.";
				case PerfMeterSelfOverheadInactiveReason.IntegrationTypeUnavailable:
					return "The PerfMeter URP integration type was unavailable.";
				case PerfMeterSelfOverheadInactiveReason.RendererFeatureNotInstalled:
					return "Observed renderer configuration reported that the PerfMeter feature was not installed.";
				case PerfMeterSelfOverheadInactiveReason.RendererFeatureDisabled:
					return "The installed PerfMeter renderer feature was disabled.";
				case PerfMeterSelfOverheadInactiveReason.PassNotEnqueued:
					return "The installed PerfMeter renderer feature had no active pass enqueue condition.";
				case PerfMeterSelfOverheadInactiveReason.NoCameraCallbackObserved:
					return "A PerfMeter pass was enqueued but no RecordRenderGraph measurement callback was observed.";
				case PerfMeterSelfOverheadInactiveReason.WindowIncomplete:
					return "The package-owned callback window did not reach 120 frames.";
				case PerfMeterSelfOverheadInactiveReason.CaptureWindowMismatch:
					return "The requested identity did not match the active or completed self-overhead window.";
				default:
					return "URP integration inactivity could not be classified from bounded package evidence.";
			}
		}

		private static string NormalizeEvidenceText(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			const int maxLength = 128;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength);
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
