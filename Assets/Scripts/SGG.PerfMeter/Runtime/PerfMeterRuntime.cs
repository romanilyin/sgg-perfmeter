using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterRuntime : MonoBehaviour
	{
		private const string GameObjectName = "SGG PerfMeter Runtime";
		private static PerfMeterRuntime _instance;

		private readonly PerfMeterCollector _collector = new PerfMeterCollector();
		private readonly PerfMeterFrameStatsSampler _frameStatsSampler = new PerfMeterFrameStatsSampler();
		private readonly PerfMeterOverdrawController _overdrawController = new PerfMeterOverdrawController();
		private PerfMeterStatusSnapshot _status;
		private PerfMeterMetricsSnapshot _latestMetrics;
		private PerfMeterOverlay _overlay;
		private string _lastCollectorWarning = string.Empty;
		private PerfMeterOverlayCorner _overlayCorner = PerfMeterOverlayCorner.TopRight;
		private PerfMeterOverlayMode _overlayMode = PerfMeterOverlayMode.Full;
		private PerfMeterTargetFps _targetFps = PerfMeterTargetFps.Fps60;
		private bool _overlayRequestedVisible = true;

		internal static PerfMeterRuntime Instance => _instance;
		internal PerfMeterStatusSnapshot Status => _status;
		internal PerfMeterMetricsSnapshot LatestMetrics => _latestMetrics;
		internal bool IsOverlayVisible => _overlay != null && _overlay.IsVisible;
		internal PerfMeterOverlayCorner OverlayCorner => _overlayCorner;
		internal PerfMeterOverlayMode OverlayMode => _overlayMode;
		internal PerfMeterTargetFps TargetFps => _targetFps;
		internal static bool IsOverdrawMeasurementActive => _instance != null && _instance._overdrawController.IsMeasuring;
		internal static PerfMeterOverdrawMeasurementState OverdrawState => _instance != null ? _instance._overdrawController.State : PerfMeterOverdrawMeasurementState.Off;

		internal static void EnsureRunning()
		{
			if (_instance != null)
			{
				_instance._collector.Start();
				_instance.EnsureOverlayState();
				return;
			}

			GameObject gameObject = new GameObject(GameObjectName);
			gameObject.hideFlags = HideFlags.DontSave;
			_instance = gameObject.AddComponent<PerfMeterRuntime>();
			if (Application.isPlaying)
			{
				DontDestroyOnLoad(gameObject);
			}
			_instance.SetRunningPlaceholders();
			_instance.EnsureOverlayState();
		}

		internal static void StopRunning()
		{
			if (_instance == null)
			{
				return;
			}

			PerfMeterRuntime runtime = _instance;
			runtime._collector.Stop();
			runtime._frameStatsSampler.Reset();
			runtime._overdrawController.Reset();
			runtime._status = CreateStoppedStatus();
			runtime._latestMetrics = PerfMeterMetricsSnapshot.Stopped;
			_instance = null;

			if (Application.isPlaying)
			{
				Destroy(runtime.gameObject);
			}
			else
			{
				DestroyImmediate(runtime.gameObject);
			}
		}

		internal static PerfMeterStatusSnapshot CreateStoppedStatus()
		{
			return CreateStatus(PerfMeterRuntimeState.Stopped, -1, string.Empty, string.Empty);
		}

		private void Awake()
		{
			if (_instance != null && _instance != this)
			{
				DestroyDuplicate();
				return;
			}

			_instance = this;
			SetRunningPlaceholders();
		}

		private void OnEnable()
		{
			if (_instance == this)
			{
				_collector.Start();
				SetRunningPlaceholders();
				EnsureOverlayState();
			}
		}

		private void Update()
		{
			int frame = Time.frameCount;
			double frameBudgetMs = GetFrameBudgetMs(_targetFps);
			_latestMetrics = _collector.Collect(frame, frameBudgetMs, out PerfMeterFrameTimingAvailability frameTimingAvailability, out string warning);
			_frameStatsSampler.AddSample(_latestMetrics.CpuFrameTimeMs, _latestMetrics.GpuFrameTimeAvailable);
			_latestMetrics = WithRuntimeStats(_latestMetrics, _frameStatsSampler.GetSnapshot());
			_lastCollectorWarning = warning;
			warning = CombineWarnings(warning, _overdrawController.Warning);
			_status = CreateStatus(
				PerfMeterRuntimeState.Running,
				frame,
				frameTimingAvailability,
				warning,
				_collector.LastError,
				_latestMetrics.Bottleneck,
				_collector.AvailableCounters,
				_collector.UnavailableCounters,
				IsOverlayVisible,
				_overdrawController.State,
				_overdrawController.Progress,
				_overdrawController.Ratio,
				_overlayCorner,
				_overlayMode,
				_targetFps);
		}

		internal void RequestOverdrawMeasurement(int frameCount)
		{
			_overdrawController.RequestMeasurement(frameCount);
			_latestMetrics = WithOverdrawState(_latestMetrics);
			RefreshStatusOverlayState();
		}

		internal void CancelOverdrawMeasurement()
		{
			_overdrawController.CancelMeasurement();
			_latestMetrics = WithOverdrawState(_latestMetrics);
			RefreshStatusOverlayState();
		}

		internal static bool TryBeginOverdrawRenderGraphFrame(int unityFrame, int screenPixelCount, out GraphicsBuffer counterBuffer)
		{
			counterBuffer = null;

			if (_instance == null)
			{
				return false;
			}

			bool started = _instance._overdrawController.TryBeginRenderGraphFrame(unityFrame, screenPixelCount, out counterBuffer);
			_instance._latestMetrics = _instance.WithOverdrawState(_instance._latestMetrics);
			_instance.RefreshStatusOverlayState();
			return started;
		}

		internal static void CompleteOverdrawCounterReadback(AsyncGPUReadbackRequest request)
		{
			if (_instance == null)
			{
				return;
			}

			_instance._overdrawController.CompleteCounterReadback(request);
			_instance._latestMetrics = _instance.WithOverdrawState(_instance._latestMetrics);
			_instance.RefreshStatusOverlayState();
		}

		internal static void FailOverdrawMeasurement(string error)
		{
			if (_instance == null)
			{
				return;
			}

			_instance._overdrawController.FailMeasurement(error);
			_instance._latestMetrics = _instance.WithOverdrawState(_instance._latestMetrics);
			_instance.RefreshStatusOverlayState();
		}

		internal void SetOverlayVisible(bool visible)
		{
			_overlayRequestedVisible = visible;
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetVisible(visible);
			}

			RefreshStatusOverlayState();
		}

		internal void SetOverlayCorner(PerfMeterOverlayCorner corner)
		{
			_overlayCorner = corner;
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetCorner(corner);
			}

			RefreshStatusOverlayState();
		}

		internal void SetOverlayMode(PerfMeterOverlayMode mode)
		{
			_overlayMode = mode;
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetMode(mode);
			}

			RefreshStatusOverlayState();
		}

		internal void SetTargetFps(PerfMeterTargetFps targetFps)
		{
			_targetFps = NormalizeTargetFps(targetFps);
			_latestMetrics = WithTargetFrameBudget(_latestMetrics);
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetTargetFps(_targetFps);
			}

			RefreshStatusOverlayState();
		}

		internal static double GetFrameBudgetMs(PerfMeterTargetFps targetFps)
		{
			return 1000d / (int)NormalizeTargetFps(targetFps);
		}

		private void OnDisable()
		{
			if (_instance == this)
			{
				_collector.Stop();
				_frameStatsSampler.Reset();
				_overdrawController.Reset();
				_status = CreateStoppedStatus();
				_latestMetrics = PerfMeterMetricsSnapshot.Stopped;
			}
		}

		private void OnDestroy()
		{
			if (_instance == this)
			{
				_instance = null;
			}
		}

		private void SetRunningPlaceholders()
		{
			int frame = Time.frameCount;
			_frameStatsSampler.Reset();
			_status = CreateStatus(
				PerfMeterRuntimeState.Running,
				frame,
				PerfMeterFrameTimingAvailability.NotCollected,
				string.Empty,
				_collector.LastError,
				PerfMeterBottleneck.Unknown,
				_collector.AvailableCounters,
				_collector.UnavailableCounters,
				IsOverlayVisible,
				_overdrawController.State,
				_overdrawController.Progress,
				_overdrawController.Ratio,
				_overlayCorner,
				_overlayMode,
				_targetFps);

			_latestMetrics = new PerfMeterMetricsSnapshot(
				PerfMeterRuntimeState.Running,
				PerfMeterAvailability.Available,
				frame,
				PerfMeterBottleneck.Unknown,
				GetFrameBudgetMs(_targetFps),
				false,
				0d,
				0d,
				0d,
				0d,
				0d,
				0,
				0,
				0,
				0,
				0,
				0,
				0L,
				0L,
				0L,
				0L,
				0d,
				_overdrawController.State,
				_overdrawController.Progress);
		}

		private static PerfMeterStatusSnapshot CreateStatus(PerfMeterRuntimeState state, int collectionFrame, string warning, string lastError)
		{
			return CreateStatus(
				state,
				collectionFrame,
				PerfMeterFrameTimingAvailability.NotCollected,
				warning,
				lastError,
				PerfMeterBottleneck.Unknown,
				PerfMeterCounterAvailability.None,
				PerfMeterCounterAvailability.None,
				false,
				PerfMeterOverdrawMeasurementState.Off,
				0f,
				0d,
				PerfMeterOverlayCorner.TopRight,
				PerfMeterOverlayMode.Full,
				PerfMeterTargetFps.Fps60);
		}

		private static PerfMeterStatusSnapshot CreateStatus(
			PerfMeterRuntimeState state,
			int collectionFrame,
			PerfMeterFrameTimingAvailability frameTimingAvailability,
			string warning,
			string lastError,
			PerfMeterBottleneck bottleneck,
			PerfMeterCounterAvailability availableCounters,
			PerfMeterCounterAvailability unavailableCounters,
			bool overlayVisible = false,
			PerfMeterOverdrawMeasurementState overdrawState = PerfMeterOverdrawMeasurementState.Off,
			float overdrawProgress = 0f,
			double overdrawRatio = 0d,
			PerfMeterOverlayCorner overlayCorner = PerfMeterOverlayCorner.TopRight,
			PerfMeterOverlayMode overlayMode = PerfMeterOverlayMode.Full,
			PerfMeterTargetFps targetFps = PerfMeterTargetFps.Fps60)
		{
			return new PerfMeterStatusSnapshot(
				state,
				PerfMeterAvailability.Available,
				frameTimingAvailability,
				SystemInfo.graphicsDeviceType,
				SystemInfo.graphicsDeviceName,
				warning,
				collectionFrame,
				lastError,
				bottleneck,
				availableCounters,
				unavailableCounters,
				overlayVisible,
				overdrawState,
				overdrawProgress,
				overdrawRatio,
				overlayCorner,
				overlayMode,
				NormalizeTargetFps(targetFps));
		}

		private PerfMeterMetricsSnapshot WithOverdrawState(PerfMeterMetricsSnapshot metrics)
		{
			return WithRuntimeStats(metrics, new PerfMeterFrameStatsSnapshot(
				metrics.FrameSampleCount,
				metrics.GpuValidSampleCount,
				metrics.AverageFps,
				metrics.OnePercentLowFps,
				metrics.PointOnePercentLowFps,
				metrics.FrameSpikeCount,
				metrics.SevereFrameSpikeCount));
		}

		private PerfMeterMetricsSnapshot WithRuntimeStats(PerfMeterMetricsSnapshot metrics, PerfMeterFrameStatsSnapshot frameStats)
		{
			return new PerfMeterMetricsSnapshot(
				metrics.State,
				metrics.Availability,
				metrics.CollectionFrame,
				metrics.Bottleneck,
				metrics.FrameBudgetMs,
				metrics.GpuFrameTimeAvailable,
				metrics.CpuFrameTimeMs,
				metrics.CpuMainThreadFrameTimeMs,
				metrics.CpuRenderThreadFrameTimeMs,
				metrics.CpuMainThreadPresentWaitTimeMs,
				metrics.GpuFrameTimeMs,
				metrics.DrawCalls,
				metrics.SetPassCalls,
				metrics.Batches,
				metrics.Vertices,
				metrics.BrgDrawCalls,
				metrics.BrgInstances,
				metrics.IndexBufferUploadInFrameBytes,
				metrics.SystemUsedMemoryBytes,
				metrics.GcReservedMemoryBytes,
				metrics.GpuMemoryBytes,
				_overdrawController.Ratio,
				_overdrawController.State,
				_overdrawController.Progress,
				metrics.SrpBatcherInstances,
				frameStats.SampleCount,
				frameStats.GpuValidSampleCount,
				frameStats.AverageFps,
				frameStats.OnePercentLowFps,
				frameStats.PointOnePercentLowFps,
				frameStats.FrameSpikeCount,
				frameStats.SevereFrameSpikeCount);
		}

		private PerfMeterMetricsSnapshot WithTargetFrameBudget(PerfMeterMetricsSnapshot metrics)
		{
			return new PerfMeterMetricsSnapshot(
				metrics.State,
				metrics.Availability,
				metrics.CollectionFrame,
				metrics.Bottleneck,
				GetFrameBudgetMs(_targetFps),
				metrics.GpuFrameTimeAvailable,
				metrics.CpuFrameTimeMs,
				metrics.CpuMainThreadFrameTimeMs,
				metrics.CpuRenderThreadFrameTimeMs,
				metrics.CpuMainThreadPresentWaitTimeMs,
				metrics.GpuFrameTimeMs,
				metrics.DrawCalls,
				metrics.SetPassCalls,
				metrics.Batches,
				metrics.Vertices,
				metrics.BrgDrawCalls,
				metrics.BrgInstances,
				metrics.IndexBufferUploadInFrameBytes,
				metrics.SystemUsedMemoryBytes,
				metrics.GcReservedMemoryBytes,
				metrics.GpuMemoryBytes,
				metrics.OverdrawRatio,
				metrics.OverdrawState,
				metrics.OverdrawProgress,
				metrics.SrpBatcherInstances,
				metrics.FrameSampleCount,
				metrics.GpuValidSampleCount,
				metrics.AverageFps,
				metrics.OnePercentLowFps,
				metrics.PointOnePercentLowFps,
				metrics.FrameSpikeCount,
				metrics.SevereFrameSpikeCount);
		}

		private static string CombineWarnings(string first, string second)
		{
			if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second) && first.Contains(second))
			{
				return first;
			}

			if (string.IsNullOrEmpty(first))
			{
				return second ?? string.Empty;
			}

			if (string.IsNullOrEmpty(second))
			{
				return first;
			}

			return first + " " + second;
		}

		private void EnsureOverlayState()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (_overlay == null)
			{
				GameObject overlayObject = new GameObject("SGG PerfMeter Overlay");
				overlayObject.hideFlags = HideFlags.DontSave;
				overlayObject.transform.SetParent(transform, worldPositionStays: false);
				_overlay = overlayObject.AddComponent<PerfMeterOverlay>();
			}

			_overlay.SetCorner(_overlayCorner);
			_overlay.SetMode(_overlayMode);
			_overlay.SetTargetFps(_targetFps);
			_overlay.SetVisible(_overlayRequestedVisible);
			RefreshStatusOverlayState();
		}

		private void RefreshStatusOverlayState()
		{
			_status = CreateStatus(
				_status.State,
				_status.CollectionFrame,
				_status.FrameTimingAvailability,
				CombineWarnings(_lastCollectorWarning, _overdrawController.Warning),
				_status.LastError,
				_status.Bottleneck,
				_status.AvailableCounters,
				_status.UnavailableCounters,
				IsOverlayVisible,
				_overdrawController.State,
				_overdrawController.Progress,
				_overdrawController.Ratio,
				_overlayCorner,
				_overlayMode,
				_targetFps);
		}

		private static PerfMeterTargetFps NormalizeTargetFps(PerfMeterTargetFps targetFps)
		{
			switch (targetFps)
			{
				case PerfMeterTargetFps.Fps15:
				case PerfMeterTargetFps.Fps30:
				case PerfMeterTargetFps.Fps60:
				case PerfMeterTargetFps.Fps90:
				case PerfMeterTargetFps.Fps120:
				case PerfMeterTargetFps.Fps144:
				case PerfMeterTargetFps.Fps240:
					return targetFps;
				default:
					return PerfMeterTargetFps.Fps60;
			}
		}

		private void DestroyDuplicate()
		{
			if (Application.isPlaying)
			{
				Destroy(gameObject);
			}
			else
			{
				DestroyImmediate(gameObject);
			}
		}
	}
}
