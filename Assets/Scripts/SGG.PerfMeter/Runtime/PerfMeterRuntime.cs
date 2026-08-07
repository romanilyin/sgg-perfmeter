using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterRuntime : MonoBehaviour
	{
		private const string GameObjectName = "SGG PerfMeter Runtime";
		private const string FocusPausedWarning = "Frame timing collection is paused while the application is unfocused or paused.";
		private const string FocusResumeWarmupWarning = "Frame timing collection is warming up after focus or pause resume.";
		private const int FocusResumeIgnoreFrames = 3;
		private const int AlertLifecycleWarmupSamples = 120;
		private static PerfMeterRuntime _instance;
		private static PerfMeterCaptureCoordinator _pendingCaptureCleanup;
		private static PerfMeterGraphicsStateCollectionCoordinator _pendingGraphicsStateCleanup;
		private static string _pendingAlertCaptureId = string.Empty;
		private static readonly PerfMeterCaptureBundleCoordinator CaptureBundles = new PerfMeterCaptureBundleCoordinator();

		private readonly PerfMeterCollector _collector = new PerfMeterCollector();
		private readonly PerfMeterFrameStatsSampler _frameStatsSampler = new PerfMeterFrameStatsSampler();
		private readonly PerfMeterCpuCoreSampler _cpuCoreSampler = new PerfMeterCpuCoreSampler();
		private readonly PerfMeterOverdrawController _overdrawController = new PerfMeterOverdrawController();
		private readonly PerfMeterSessionRecorder _sessionRecorder = new PerfMeterSessionRecorder();
		private PerfMeterCaptureCoordinator _captureCoordinator;
		private PerfMeterMemorySnapshotCoordinator _memorySnapshotCoordinator;
		private PerfMeterGraphicsStateCollectionCoordinator _graphicsStateCollectionCoordinator;
		private readonly PerfMeterMemorySnapshotTriggerEvaluator _memorySnapshotTriggerEvaluator = new PerfMeterMemorySnapshotTriggerEvaluator();
		private readonly WaitForEndOfFrame _graphicsStateEndOfFrame = new WaitForEndOfFrame();
		private PerfMeterAlertEngine _alertEngine = new PerfMeterAlertEngine();
		private PerfMeterStatusSnapshot _status;
		private PerfMeterMetricsSnapshot _latestMetrics;
		private PerfMeterCustomMetricSnapshot[] _latestCustomMetrics = System.Array.Empty<PerfMeterCustomMetricSnapshot>();
		private PerfMeterPlatformTelemetrySnapshot _latestPlatformTelemetry = PerfMeterPlatformTelemetrySnapshot.Unavailable();
		private PerfMeterOverlay _overlay;
		private string _lastCollectorWarning = string.Empty;
		private PerfMeterOverlayCorner _overlayCorner = PerfMeterOverlayCorner.TopRight;
		private PerfMeterOverlayMode _overlayMode = PerfMeterOverlayMode.Full;
		private PerfMeterOverlayTheme _overlayTheme = PerfMeterOverlayTheme.ClassicDark;
		private PerfMeterOverlayLayout _overlayLayout = PerfMeterOverlayLayout.MetricBars;
		private PerfMeterOverlayFontFamily _overlayFontFamily = PerfMeterOverlayFontFamily.Manrope;
		private PerfMeterOverlayPreset _overlayPreset = PerfMeterOverlayPreset.FullDiagnostics;
		private string _visualOverlayPresetId = PerfMeterOverlayPresetDefaults.FullDiagnosticsId;
		private PerfMeterOverlayModule _overlayModules = PerfMeterSettingsStore.GetPresetModules(PerfMeterOverlayPreset.FullDiagnostics);
		private PerfMeterTargetFps _targetFps = PerfMeterTargetFps.Fps60;
		private float _overlayScale = 1f;
		private float _overlayOpacity = 0.84f;
		private float _overlayFontSize = 12f;
		private float _overlayRefreshIntervalSeconds = 0.25f;
		private int _overlayGraphHistoryLength = 120;
		private int _overdrawDefaultFrameCount = 60;
		private int _overdrawMaxFrameCount = 600;
		private PerfMeterSettingsSnapshot _settings = PerfMeterSettingsStore.Defaults;
		private bool _overlayRequestedVisible = true;
		private bool _overdrawHeatmapVisible;
		private bool _applicationFocused = true;
		private bool _applicationPaused;
		private bool _cpuCoreSamplingActive;
		private bool _structuredLogsEnabled = true;
		private bool _captureCleanupPending;
		private int _focusResumeIgnoreFrames;
		private int _alertSampleCount;
		private string _alertCaptureId = string.Empty;
		private string _captureBundleId = string.Empty;
		private PerfMeterAlertClassification _lastAlertClassification = PerfMeterAlertClassification.Lifecycle;
		private bool _alertEngineInitialized;
		private PerfMeterMemorySnapshotTriggerOptions _memorySnapshotTriggers;
		private bool _memoryAutomaticAttemptBlocked;
		private string _graphicsStateTraceIdForCurrentFrame = string.Empty;

		internal static PerfMeterRuntime Instance => _instance;
		internal static PerfMeterCaptureStatusSnapshot PendingCaptureStatus => _pendingCaptureCleanup != null ? _pendingCaptureCleanup.Status : PerfMeterCaptureStatusSnapshot.NotRunning;
		internal static PerfMeterGraphicsStateCollectionStatusSnapshot PendingGraphicsStateCollectionStatus => _pendingGraphicsStateCleanup != null ? _pendingGraphicsStateCleanup.GetStatus() : PerfMeterGraphicsStateCollectionStatusSnapshot.Idle;
		internal static string PendingAlertCaptureId => _pendingAlertCaptureId;
		internal static PerfMeterCaptureBundleStatusSnapshot CaptureBundleStatus(string captureId = null) => CaptureBundles.GetStatus(captureId);
		internal static PerfMeterCaptureCapabilitiesSnapshot CaptureCapabilities => PerfMeterCaptureBundleExporter.GetCapabilities();
		internal PerfMeterStatusSnapshot Status => _status.WithSelfOverhead(PerfMeterSelfObservability.GetSnapshot());
		internal PerfMeterMetricsSnapshot LatestMetrics => _latestMetrics;
		internal PerfMeterPlatformTelemetrySnapshot LatestPlatformTelemetry => _latestPlatformTelemetry;
		internal PerfMeterProfilerMetricCatalogSnapshot ProfilerMetricCatalog => _collector.GetProfilerMetricCatalog();
		internal PerfMeterSelfOverheadSnapshot SelfOverhead => PerfMeterSelfObservability.GetSnapshot();
		internal PerfMeterCaptureStatusSnapshot CaptureStatus => _captureCoordinator != null ? _captureCoordinator.Status : PerfMeterCaptureStatusSnapshot.NotRunning;
		internal PerfMeterMemorySnapshotStatusSnapshot MemorySnapshotStatus => _memorySnapshotCoordinator != null ? _memorySnapshotCoordinator.GetStatus(Time.realtimeSinceStartupAsDouble) : PerfMeterMemorySnapshotStatusSnapshot.NotRunning;
		internal PerfMeterMemorySnapshotTriggerOptions MemorySnapshotTriggers => _memorySnapshotTriggers;
		internal PerfMeterGraphicsStateCollectionStatusSnapshot GraphicsStateCollectionStatus => _graphicsStateCollectionCoordinator != null ? _graphicsStateCollectionCoordinator.GetStatus() : PerfMeterGraphicsStateCollectionStatusSnapshot.Idle;
		internal PerfMeterGraphicsDiagnosticsSnapshot GraphicsDiagnostics => CreateGraphicsDiagnostics();
		internal bool IsOverlayVisible => IsRuntimeOverlaySupported && _overlay != null && _overlay.IsVisible;
		internal PerfMeterOverlayCorner OverlayCorner => _overlayCorner;
		internal PerfMeterOverlayMode OverlayMode => _overlayMode;
		internal PerfMeterOverlayTheme OverlayTheme => _overlayTheme;
		internal PerfMeterOverlayLayout OverlayLayout => _overlayLayout;
		internal PerfMeterOverlayFontFamily OverlayFontFamily => _overlayFontFamily;
		internal PerfMeterOverlayPreset OverlayPreset => _overlayPreset;
		internal string VisualOverlayPresetId => _visualOverlayPresetId;
		internal PerfMeterOverlayModule OverlayModules => _overlayModules;
		internal PerfMeterTargetFps TargetFps => _targetFps;
		internal bool EditorWarningLogsEnabled => _settings.EditorWarningsEnabled;
		internal bool StructuredLogsEnabled => _structuredLogsEnabled;
		internal string ActiveAlertCaptureId => _alertCaptureId;
		internal PerfMeterAlertClassification LastAlertClassification => _lastAlertClassification;
		internal PerfMeterCollectionMode CollectionMode => GetCollectionMode();
		internal bool IsSessionRecording => _sessionRecorder.IsRecording;
		internal static bool IsOverdrawMeasurementActive => _instance != null && _instance._overdrawController.IsMeasuring;
		internal static bool IsOverdrawHeatmapVisible => _instance != null && _instance._overdrawHeatmapVisible;
		internal static PerfMeterOverdrawMeasurementState OverdrawState => _instance != null ? _instance._overdrawController.State : PerfMeterOverdrawMeasurementState.Off;

		internal static PerfMeterCaptureBundleExportResult ExportCaptureBundle(string captureId, string path, string externalArtifactPath, bool requireAuthoritativeExternalArtifact)
		{
			if (!CaptureBundles.TryGetExportData(captureId, out PerfMeterCaptureBundleExportData data))
			{
				PerfMeterCaptureBundleStatusSnapshot status = CaptureBundles.GetStatus(captureId);
				PerfMeterCaptureBundleExportStatus exportStatus = status.State == PerfMeterCaptureBundleState.None
					? PerfMeterCaptureBundleExportStatus.NotFound
					: PerfMeterCaptureBundleExportStatus.NotReady;
				return new PerfMeterCaptureBundleExportResult(false, exportStatus, string.Empty, exportStatus == PerfMeterCaptureBundleExportStatus.NotFound ? "capture_not_found" : "capture_not_ready", status);
			}

			PerfMeterCaptureBundleExportResult result = PerfMeterCaptureBundleExporter.Export(data, path, externalArtifactPath, requireAuthoritativeExternalArtifact);
			if (result.Success)
			{
				CaptureBundles.MarkExported(captureId, result.RelativePath, result.Bundle.ExternalArtifactState);
				if (data.MemorySnapshotArtifact.IsAvailable)
				{
					bool deleted;
					if (_instance != null && _instance._memorySnapshotCoordinator != null)
					{
						deleted = _instance._memorySnapshotCoordinator.DiscardArtifact(data.MemorySnapshotArtifact.SourcePath);
					}
					else
					{
						deleted = new PerfMeterMemorySnapshotStorage(System.IO.Path.Combine(Application.dataPath, "..")).TryDelete(data.MemorySnapshotArtifact.SourcePath);
					}

					if (deleted)
					{
						CaptureBundles.ClearMemorySnapshotArtifact(captureId, data.MemorySnapshotArtifact.SourcePath);
					}
					else
					{
						return new PerfMeterCaptureBundleExportResult(true, result.Status, result.RelativePath, "memory_snapshot_cleanup_failed", result.Bundle);
					}
				}
			}

			return result;
		}

		internal static void ResetCaptureBundlesForTests()
		{
			CaptureBundles.ResetForTests();
		}

		internal bool RefreshProfilerMetricCatalog()
		{
			if (!CanMutateRuntime)
			{
				return false;
			}

			bool refreshed = _collector.RefreshProfilerMetricCatalog();
			RefreshStatusOverlayState();
			return refreshed;
		}

		internal static bool EnsureRunning()
		{
			if (!TryReleasePendingGraphicsStateCleanup())
			{
				return false;
			}

			if (!TryReleasePendingCaptureCleanup())
			{
				return false;
			}

			if (_instance != null)
			{
				if (!_instance.isActiveAndEnabled)
				{
					return false;
				}

				if (_instance._captureCleanupPending)
				{
					if (!_instance.TryResetCaptureCoordinator())
					{
						_instance.RecordPendingCaptureCleanup();
						return false;
					}

					_instance.ResetProfilerInstrumentationForRunningState();
					_instance.SetRunningPlaceholders();
				}

				PerfMeterSelfObservability.EnsureStarted(PerfMeterRenderPipelineDetector.GetActiveKind());
				_instance._collector.Start();
				_instance.EnsureOverlayState();
				return true;
			}

			GameObject gameObject = new GameObject(GameObjectName);
			gameObject.hideFlags = HideFlags.DontSave;
			_instance = gameObject.AddComponent<PerfMeterRuntime>();
			if (Application.isPlaying)
			{
				DontDestroyOnLoad(gameObject);
			}
			PerfMeterSelfObservability.EnsureStarted(PerfMeterRenderPipelineDetector.GetActiveKind());
			_instance.SetRunningPlaceholders();
			_instance.EnsureOverlayState();
			return _instance != null;
		}

		internal static void StopRunning()
		{
			if (_instance == null)
			{
				if (!TryReleasePendingCaptureCleanup())
				{
					PerfMeterSelfObservability.Stop();
					return;
				}

				PerfMeterProfilerInstrumentation.Reset();
				PerfMeterSelfObservability.Stop();
				return;
			}

			PerfMeterRuntime runtime = _instance;
			runtime._collector.Stop();
			runtime._frameStatsSampler.Reset();
			runtime._cpuCoreSampler.Reset();
			runtime._cpuCoreSamplingActive = false;
			runtime._overdrawController.Reset();
			runtime._sessionRecorder.Stop(Time.realtimeSinceStartupAsDouble);
			runtime.FinalizeGraphicsStateCollectionForShutdown();
			runtime.FinalizeMemorySnapshotForShutdown("Runtime stopped during memory snapshot capture.");
			runtime.FinalizeCaptureBundleForShutdown("Runtime stopped during capture.");
			bool captureReleased = runtime.TryResetCaptureCoordinator();
			runtime._alertEngine.Clear();
			runtime._overdrawHeatmapVisible = false;
			runtime.DestroyOverlay();
			runtime._status = CreateStoppedStatus();
			runtime._latestMetrics = PerfMeterMetricsSnapshot.Stopped;
			runtime._latestCustomMetrics = System.Array.Empty<PerfMeterCustomMetricSnapshot>();
			runtime._latestPlatformTelemetry = PerfMeterPlatformTelemetrySnapshot.Unavailable();
			PerfMeterProfilerInstrumentation.Reset();
			if (!captureReleased)
			{
				runtime.RecordPendingCaptureCleanup();
				PerfMeterSelfObservability.Stop();
				return;
			}

			PerfMeterSelfObservability.Stop();
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
			EnsureCaptureCoordinator();
			EnsureMemorySnapshotCoordinator();
			EnsureGraphicsStateCollectionCoordinator();
			SetRunningPlaceholders();
		}

		private void OnEnable()
		{
			if (_instance == this)
			{
				if (!TryReleasePendingGraphicsStateCleanup())
				{
					return;
				}

				_memorySnapshotTriggerEvaluator.Reset();
				_memoryAutomaticAttemptBlocked = false;
				EnsureCaptureCoordinator();
				EnsureMemorySnapshotCoordinator();
				EnsureGraphicsStateCollectionCoordinator();
				if (!TryResetCaptureCoordinator())
				{
					PerfMeterProfilerInstrumentation.Reset();
					RecordPendingCaptureCleanup();
					PerfMeterSelfObservability.Stop();
					return;
				}

				PerfMeterSelfObservability.Start(PerfMeterRenderPipelineDetector.GetActiveKind());
				ResetProfilerInstrumentationForRunningState();
				_collector.Start();
				ApplyAlertSettings();
				SetRunningPlaceholders();
				EnsureOverlayState();
			}
		}

		private void Update()
		{
			PerfMeterGraphicsStateCollectionStatusSnapshot graphicsStateStatus = _graphicsStateCollectionCoordinator != null
				? _graphicsStateCollectionCoordinator.GetStatus()
				: PerfMeterGraphicsStateCollectionStatusSnapshot.Idle;
			_graphicsStateTraceIdForCurrentFrame = graphicsStateStatus.IsActive ? graphicsStateStatus.CaptureId : string.Empty;
			if (_captureCleanupPending)
			{
				return;
			}

			if (TrySkipCollectionForFocusState(out string focusWarning))
			{
				PerfMeterProfilerInstrumentation.ResetFrameTimings();
				_lastCollectorWarning = focusWarning;
				RefreshRunningStatus(Time.frameCount, PerfMeterFrameTimingAvailability.NotCollected, focusWarning);
				ProcessMemorySnapshotCompletion();
				TickCaptureAndUpdateBundle();
				return;
			}

			int frame = Time.frameCount;
			double frameBudgetMs = GetFrameBudgetMs(_targetFps);
			PerfMeterMetricsSnapshot collectedMetrics = _collector.Collect(frame, frameBudgetMs, out PerfMeterFrameTimingAvailability frameTimingAvailability, out string warning, out bool frameTimingSampleIgnored);
			if (frameTimingSampleIgnored)
			{
				_lastCollectorWarning = warning;
				RefreshRunningStatus(frame, frameTimingAvailability, warning);
				ProcessMemorySnapshotCompletion();
				TickCaptureAndUpdateBundle();
				return;
			}

			_latestMetrics = collectedMetrics;
			_frameStatsSampler.AddSample(_latestMetrics.CpuFrameTimeMs, _latestMetrics.GpuFrameTimeAvailable);
			_latestMetrics = WithRuntimeStats(_latestMetrics, _frameStatsSampler.GetSnapshot());
			UpdateCpuCoreSampler(Time.unscaledTime);
			_latestCustomMetrics = PerfMeterCustomMetricRegistry.Collect();
			_latestPlatformTelemetry = PerfMeterPlatformTelemetryRegistry.Collect();
			PerfMeterProfilerInstrumentation.RecordThermalAvailability(_latestPlatformTelemetry.IsAvailable && _latestPlatformTelemetry.ThermalWarningLevelAvailable);
			PerfMeterCaptureStatusSnapshot captureStatus = _captureCoordinator != null ? _captureCoordinator.Status : PerfMeterCaptureStatusSnapshot.NotRunning;
			PerfMeterSessionSampleSnapshot frameSample = new PerfMeterSessionSampleSnapshot(frame, Time.realtimeSinceStartupAsDouble, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, _latestMetrics, _latestCustomMetrics, _latestPlatformTelemetry, _graphicsStateTraceIdForCurrentFrame);
			if (CaptureBundles.IsRecordingCaptureFrame(captureStatus))
			{
				bool needsContext = CaptureBundles.NeedsCaptureContext(captureStatus.CaptureId);
				CaptureBundles.RecordCaptureFrame(
					frameSample,
					needsContext ? PerfMeterDeviceInfoProvider.CreateSnapshot() : default,
					needsContext ? PerfMeterCameraSnapshotProvider.CreateSnapshot(PerfMeterCameraSource.Auto, null) : default,
					needsContext ? PerfMeterRenderGraphAnalytics.GetSnapshot() : default,
					needsContext ? PerfMeterRenderGraphAnalytics.GetRenderIntegrationSnapshot() : default,
					needsContext ? Status : default);
			}
			else
			{
				_sessionRecorder.Update(_latestMetrics, frame, Time.realtimeSinceStartupAsDouble, _latestCustomMetrics, _latestPlatformTelemetry, _graphicsStateTraceIdForCurrentFrame);
			}
			_lastAlertClassification = !string.IsNullOrEmpty(_alertCaptureId)
				? PerfMeterAlertClassification.Capture
				: _alertSampleCount < AlertLifecycleWarmupSamples
					? PerfMeterAlertClassification.Lifecycle
					: PerfMeterAlertClassification.SteadyState;
			_alertEngine.Evaluate(_latestMetrics, _latestPlatformTelemetry, Time.realtimeSinceStartupAsDouble, _lastAlertClassification, _alertCaptureId);
			_alertSampleCount++;
			_lastCollectorWarning = warning;
			RefreshRunningStatus(frame, frameTimingAvailability, warning);
			ProcessMemorySnapshotCompletion();
			EvaluateMemorySnapshotTriggers(frame);
			TickCaptureAndUpdateBundle();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (Application.isBatchMode || _applicationFocused == hasFocus)
			{
				return;
			}

			_applicationFocused = hasFocus;
			if (hasFocus)
			{
				_focusResumeIgnoreFrames = FocusResumeIgnoreFrames;
			}

			_sessionRecorder.SetApplicationFocusState(_applicationFocused, _applicationPaused, Time.frameCount, Time.realtimeSinceStartupAsDouble);
			RefreshStatusOverlayState();
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			if (_applicationPaused == pauseStatus)
			{
				return;
			}

			_applicationPaused = pauseStatus;
			if (!pauseStatus)
			{
				_focusResumeIgnoreFrames = FocusResumeIgnoreFrames;
			}

			_sessionRecorder.SetApplicationFocusState(_applicationFocused, _applicationPaused, Time.frameCount, Time.realtimeSinceStartupAsDouble);
			RefreshStatusOverlayState();
		}

		internal PerfMeterAlertSnapshot[] GetLatestAlerts()
		{
			return _alertEngine.GetLatestAlerts();
		}

		internal PerfMeterAlertHistorySnapshot GetAlertHistory()
		{
			return _alertEngine.History;
		}

		internal PerfMeterCaptureRequestResult RequestCapture(PerfMeterCaptureOptions options)
		{
			if (!CanMutateRuntime)
			{
				return PerfMeterCaptureRequestResult.Unavailable;
			}

			EnsureCaptureCoordinator();
			ProcessMemorySnapshotCompletion();
			if (!TryReleaseGraphicsStateCollectionCleanup() ||
				(_memorySnapshotCoordinator != null && _memorySnapshotCoordinator.GetStatus(Time.realtimeSinceStartupAsDouble).IsActive) ||
				(_graphicsStateCollectionCoordinator != null && _graphicsStateCollectionCoordinator.IsBusy))
			{
				return PerfMeterCaptureRequestResult.RejectedOverlap;
			}
			if (!_captureCoordinator.Status.IsActive && !string.IsNullOrEmpty(_alertCaptureId))
			{
				return PerfMeterCaptureRequestResult.RejectedOverlap;
			}

			PerfMeterCaptureRequestResult result = _captureCoordinator.Request(options);
			if (result == PerfMeterCaptureRequestResult.Started || result == PerfMeterCaptureRequestResult.Unavailable || result == PerfMeterCaptureRequestResult.Failed)
			{
				_alertEngine.BeginCaptureEventCollection();
			}

			return result;
		}

		internal PerfMeterMemorySnapshotRequestResult RequestMemorySnapshot(PerfMeterMemorySnapshotOptions options)
		{
			if (!CanMutateRuntime)
			{
				return PerfMeterMemorySnapshotRequestResult.Unavailable;
			}

			EnsureMemorySnapshotCoordinator();
			ProcessMemorySnapshotCompletion();
			if (!TryReleaseGraphicsStateCollectionCleanup() ||
				(_captureCoordinator != null && _captureCoordinator.HasActiveResources) ||
				(_graphicsStateCollectionCoordinator != null && _graphicsStateCollectionCoordinator.IsBusy) ||
				!string.IsNullOrEmpty(_alertCaptureId))
			{
				return PerfMeterMemorySnapshotRequestResult.RejectedOverlap;
			}

			if (!TryDiscardCaptureBundleMemoryArtifact(true))
			{
				return PerfMeterMemorySnapshotRequestResult.RejectedOverlap;
			}

			double now = Time.realtimeSinceStartupAsDouble;
			PerfMeterMemorySnapshotRequestResult result = _memorySnapshotCoordinator.Request(options, now);
			if (result == PerfMeterMemorySnapshotRequestResult.Started ||
				result == PerfMeterMemorySnapshotRequestResult.Unavailable ||
				result == PerfMeterMemorySnapshotRequestResult.InsufficientDiskSpace ||
				result == PerfMeterMemorySnapshotRequestResult.Failed)
			{
				PerfMeterMemorySnapshotStatusSnapshot status = _memorySnapshotCoordinator.GetStatus(now);
				CaptureBundles.StartMemorySnapshot(options, status, _settings, GetEffectiveSettingsSnapshot(_settings));
				_captureBundleId = CaptureBundles.GetStatus(options.CaptureId).BundleId;
				ProcessMemorySnapshotCompletion();
				if (!status.IsActive && CaptureBundles.GetStatus(options.CaptureId).State == PerfMeterCaptureBundleState.Recording)
				{
					FinalizeMemorySnapshot(status, default);
				}
			}

			return result;
		}

		internal PerfMeterGraphicsStateCollectionRequestResult RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions options)
		{
			if (!CanMutateRuntime)
			{
				return PerfMeterGraphicsStateCollectionRequestResult.Unavailable;
			}

			EnsureGraphicsStateCollectionCoordinator();
			ProcessMemorySnapshotCompletion();
			if (!_sessionRecorder.IsRecording)
			{
				return PerfMeterGraphicsStateCollectionRequestResult.InvalidRequest;
			}

			if ((_captureCoordinator != null && _captureCoordinator.HasActiveResources) ||
				(_memorySnapshotCoordinator != null && _memorySnapshotCoordinator.GetStatus(Time.realtimeSinceStartupAsDouble).IsActive) ||
				!string.IsNullOrEmpty(_alertCaptureId))
			{
				return PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap;
			}

			PerfMeterGraphicsStateCollectionRequestResult result = _graphicsStateCollectionCoordinator.RequestTrace(options, out int generation);
			if (result == PerfMeterGraphicsStateCollectionRequestResult.Started)
			{
				StartCoroutine(TickGraphicsStateTraceAtEndOfFrame(options.CaptureId, generation));
			}

			return result;
		}

		internal PerfMeterGraphicsStateCollectionRequestResult PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions options)
		{
			if (!CanMutateRuntime)
			{
				return PerfMeterGraphicsStateCollectionRequestResult.Unavailable;
			}

			EnsureGraphicsStateCollectionCoordinator();
			ProcessMemorySnapshotCompletion();
			if ((_captureCoordinator != null && _captureCoordinator.HasActiveResources) ||
				(_memorySnapshotCoordinator != null && _memorySnapshotCoordinator.GetStatus(Time.realtimeSinceStartupAsDouble).IsActive) ||
				!string.IsNullOrEmpty(_alertCaptureId))
			{
				return PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap;
			}

			return _graphicsStateCollectionCoordinator.Prewarm(options);
		}

		internal bool CancelGraphicsStateTrace(string captureId)
		{
			return CanMutateRuntime && _graphicsStateCollectionCoordinator != null && _graphicsStateCollectionCoordinator.CancelTrace(captureId);
		}

		internal bool ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions options)
		{
			if (!CanMutateRuntime)
			{
				return false;
			}

			_memorySnapshotTriggers = options;
			_memorySnapshotTriggerEvaluator.Reset();
			_memoryAutomaticAttemptBlocked = false;
			return true;
		}

		internal PerfMeterCaptureRequestResult RequestCapture(PerfMeterCaptureOptions options, PerfMeterCaptureBundleOptions bundleOptions)
		{
			PerfMeterCaptureRequestResult result = RequestCapture(options);
			if (result == PerfMeterCaptureRequestResult.AlreadyActive)
			{
				return result;
			}

			if (result == PerfMeterCaptureRequestResult.Started || result == PerfMeterCaptureRequestResult.Unavailable || result == PerfMeterCaptureRequestResult.Failed)
			{
				if (!TryDiscardCaptureBundleMemoryArtifact(false))
				{
					if (result == PerfMeterCaptureRequestResult.Started && !_captureCoordinator.Cancel(options.CaptureId))
					{
						return PerfMeterCaptureRequestResult.Failed;
					}

					return PerfMeterCaptureRequestResult.RejectedOverlap;
				}

				PerfMeterCaptureStatusSnapshot captureStatus = _captureCoordinator.Status;
				CaptureBundles.Start(options, bundleOptions, captureStatus, _settings, GetEffectiveSettingsSnapshot(_settings));
				_captureBundleId = CaptureBundles.GetStatus(options.CaptureId).BundleId;
				if (!captureStatus.IsActive)
				{
					FinalizeCaptureBundle(captureStatus);
				}
			}

			return result;
		}

		private PerfMeterSettingsSnapshot GetEffectiveSettingsSnapshot(PerfMeterSettingsSnapshot configuredSettings)
		{
			return PerfMeterSettingsStore.WithRuntimeState(
				configuredSettings,
				GetCollectionMode(),
				IsOverlayVisible,
				_overlayCorner,
				_overlayMode,
				_overlayTheme,
				_overlayLayout,
				_overlayFontFamily,
				_targetFps,
				_overlayPreset,
				_overlayModules,
				_overlayScale,
				_overlayOpacity,
				_overlayFontSize,
				_overlayRefreshIntervalSeconds,
				_overlayGraphHistoryLength,
				_visualOverlayPresetId);
		}

		internal bool CancelCapture(string captureId)
		{
			bool canceled = _captureCoordinator != null && _captureCoordinator.Cancel(captureId);
			if (_captureCoordinator != null)
			{
				PerfMeterCaptureStatusSnapshot captureStatus = _captureCoordinator.Status;
				CaptureBundles.UpdateCaptureStatus(captureStatus);
				if (!captureStatus.IsActive)
				{
					FinalizeCaptureBundle(captureStatus);
				}
			}

			return canceled;
		}

		internal void SetCaptureBackendForTests(IPerfMeterCaptureBackend backend)
		{
			if (!CanMutateRuntime)
			{
				throw new System.InvalidOperationException("The performance meter runtime is not accepting capture backend changes.");
			}

			if (_captureCoordinator != null && !_captureCoordinator.Reset())
			{
				throw new System.InvalidOperationException("The active capture backend could not be reset.");
			}

			_captureCoordinator = new PerfMeterCaptureCoordinator(backend, new RuntimeCaptureScope(this));
		}

		internal void TickCaptureForTests()
		{
			if (CanMutateRuntime)
			{
				_captureCoordinator?.Tick();
			}
		}

		internal bool BeginAlertCapture(string captureId)
		{
			if (!CanMutateRuntime)
			{
				return false;
			}

			if (_captureCoordinator != null && _captureCoordinator.Status.IsActive)
			{
				return false;
			}

			if (_memorySnapshotCoordinator != null && _memorySnapshotCoordinator.GetStatus(Time.realtimeSinceStartupAsDouble).IsActive)
			{
				return false;
			}

			if (!TryReleaseGraphicsStateCollectionCleanup() ||
				(_graphicsStateCollectionCoordinator != null && _graphicsStateCollectionCoordinator.IsBusy))
			{
				return false;
			}

			return BeginAlertCaptureCore(captureId);
		}

		internal bool EndAlertCapture(string captureId)
		{
			if (!CanMutateRuntime)
			{
				return false;
			}

			if (_captureCoordinator != null && _captureCoordinator.Status.IsActive)
			{
				return false;
			}

			return EndAlertCaptureCore(captureId);
		}

		private bool BeginAlertCaptureCore(string captureId)
		{
			using (PerfMeterProfilerInstrumentation.AlertCaptureMarker.Auto())
			{
				if (!string.IsNullOrEmpty(_alertCaptureId) && !string.Equals(_alertCaptureId, captureId, System.StringComparison.Ordinal))
				{
					return false;
				}

				_alertCaptureId = captureId;
				PerfMeterProfilerInstrumentation.RecordAlertScopeActive(true);
				return true;
			}
		}

		private bool EndAlertCaptureCore(string captureId)
		{
			using (PerfMeterProfilerInstrumentation.AlertCaptureMarker.Auto())
			{
				if (!string.Equals(_alertCaptureId, captureId, System.StringComparison.Ordinal))
				{
					return false;
				}

				_alertCaptureId = string.Empty;
				PerfMeterProfilerInstrumentation.RecordAlertScopeActive(false);
				return true;
			}
		}

		internal void ClearAlerts()
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_alertEngine.ResetHistory(Time.frameCount, Time.realtimeSinceStartupAsDouble, PerfMeterAlertHistoryResetReason.ExplicitClear);
			RefreshStatusOverlayState();
		}

		internal void ResetStats()
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_frameStatsSampler.Reset();
			_sessionRecorder.ResetStats(Time.frameCount, Time.realtimeSinceStartupAsDouble, _latestMetrics, _applicationFocused, _applicationPaused);
			_alertEngine.ResetHistory(Time.frameCount, Time.realtimeSinceStartupAsDouble, PerfMeterAlertHistoryResetReason.StatsReset);
			_latestMetrics = WithRuntimeStats(_latestMetrics, _frameStatsSampler.GetSnapshot());
			RefreshStatusOverlayState();
		}

		internal void SetCollectionMode(PerfMeterCollectionMode mode)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			switch (NormalizeCollectionMode(mode))
			{
				case PerfMeterCollectionMode.Background:
					_overlayRequestedVisible = false;
					EnsureOverlayState();
					ResetCpuCoreSamplerIfInactive();
					RefreshStatusOverlayState();
					break;
				case PerfMeterCollectionMode.Overlay:
					_overlayRequestedVisible = true;
					EnsureOverlayState();
					ResetCpuCoreSamplerIfInactive();
					RefreshStatusOverlayState();
					break;
				case PerfMeterCollectionMode.OverdrawDiagnostic:
					_overlayRequestedVisible = true;
					EnsureOverlayState();
					ResetCpuCoreSamplerIfInactive();
					RequestOverdrawMeasurement(_overdrawDefaultFrameCount);
					break;
			}
		}

		internal void StartSession(PerfMeterSessionOptions options)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			PerfMeterSettingsSnapshot configuredSettings = _settings;
			PerfMeterSettingsSnapshot effectiveSettings = GetEffectiveSettingsSnapshot(configuredSettings);
			PerfMeterSessionOptions normalizedOptions = options.MaxSamples > 0 ? options : PerfMeterSessionOptions.FromSettings(configuredSettings);
			_sessionRecorder.Start(
				normalizedOptions,
				PerfMeterDeviceInfoProvider.CreateSnapshot(),
				PerfMeterCameraSnapshotProvider.CreateSnapshot(PerfMeterCameraSource.Auto, null),
				configuredSettings,
				Time.frameCount,
				Time.realtimeSinceStartupAsDouble,
				_latestMetrics,
				_applicationFocused,
				_applicationPaused,
				effectiveSettings);
			RefreshStatusOverlayState();
		}

		internal void StopSession()
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			PerfMeterGraphicsStateCollectionStatusSnapshot graphicsStateStatus = GraphicsStateCollectionStatus;
			if (graphicsStateStatus.IsActive)
			{
				_graphicsStateCollectionCoordinator.CancelTrace(graphicsStateStatus.CaptureId);
			}

			_sessionRecorder.Stop(Time.realtimeSinceStartupAsDouble);
			RefreshStatusOverlayState();
		}

		internal PerfMeterSessionSummarySnapshot GetSessionSummary()
		{
			return _sessionRecorder.GetSummary();
		}

		internal PerfMeterSessionSampleSnapshot[] GetSessionSamples()
		{
			return _sessionRecorder.GetSamplesCopy();
		}

		internal PerfMeterCustomMetricSnapshot[] GetLatestCustomMetrics()
		{
			if (_latestCustomMetrics.Length == 0)
			{
				return PerfMeterCustomMetricRegistry.Collect();
			}

			PerfMeterCustomMetricSnapshot[] copy = new PerfMeterCustomMetricSnapshot[_latestCustomMetrics.Length];
			System.Array.Copy(_latestCustomMetrics, copy, _latestCustomMetrics.Length);
			return copy;
		}

		internal PerfMeterCustomMetricSnapshot[] PeekLatestCustomMetrics()
		{
			return _latestCustomMetrics.Length == 0 ? PerfMeterCustomMetricRegistry.Collect() : _latestCustomMetrics;
		}

		internal PerfMeterCpuCoreLoadSnapshot[] GetCpuCoreLoads()
		{
			return _cpuCoreSampler.GetLoadsCopy();
		}

		internal PerfMeterCpuCoreLoadSnapshot[] PeekCpuCoreLoads()
		{
			return _cpuCoreSampler.PeekLoads();
		}

		internal int CpuCoreLoadCount => _cpuCoreSampler.CoreCount;
		internal PerfMeterCpuCoreLoadAvailability CpuCoreLoadAvailability => _cpuCoreSampler.Availability;

		internal void RequestOverdrawMeasurement(int frameCount)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			int normalizedFrameCount = frameCount <= 0 ? _overdrawDefaultFrameCount : frameCount;
			_overdrawController.RequestMeasurement(Mathf.Clamp(normalizedFrameCount, 1, _overdrawMaxFrameCount));
			_latestMetrics = WithOverdrawState(_latestMetrics);
			RefreshStatusOverlayState();
		}

		internal void CancelOverdrawMeasurement()
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_overdrawController.CancelMeasurement();
			_latestMetrics = WithOverdrawState(_latestMetrics);
			RefreshStatusOverlayState();
		}

		internal void SetOverdrawHeatmapVisible(bool visible)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			if (visible && PerfMeterRenderPipelineDetector.GetActiveKind() == PerfMeterRenderPipelineKind.HighDefinition)
			{
				_overdrawHeatmapVisible = false;
				_overdrawController.MarkUnsupported(PerfMeterOverdrawController.GetUnsupportedReason());
				_latestMetrics = WithOverdrawState(_latestMetrics);
				RefreshStatusOverlayState();
				return;
			}

			_overdrawHeatmapVisible = visible;
			RefreshStatusOverlayState();
		}

		internal static bool TryBeginOverdrawRenderGraphFrame(int unityFrame, int screenPixelCount, out GraphicsBuffer counterBuffer, out int measurementId)
		{
			counterBuffer = null;
			measurementId = -1;

			if (_instance == null || !_instance.CanMutateRuntime)
			{
				return false;
			}

			bool started = _instance._overdrawController.TryBeginRenderGraphFrame(unityFrame, screenPixelCount, out counterBuffer, out measurementId);
			_instance._latestMetrics = _instance.WithOverdrawState(_instance._latestMetrics);
			_instance.RefreshStatusOverlayState();
			return started;
		}

		internal static void CompleteOverdrawCounterReadback(int measurementId, AsyncGPUReadbackRequest request)
		{
			if (_instance == null || !_instance.CanMutateRuntime)
			{
				return;
			}

			_instance._overdrawController.CompleteCounterReadback(measurementId, request);
			_instance._latestMetrics = _instance.WithOverdrawState(_instance._latestMetrics);
			_instance.RefreshStatusOverlayState();
		}

		internal static void FailOverdrawMeasurement(string error)
		{
			if (_instance == null || !_instance.CanMutateRuntime)
			{
				return;
			}

			_instance._overdrawController.FailMeasurement(error);
			_instance._latestMetrics = _instance.WithOverdrawState(_instance._latestMetrics);
			_instance.RefreshStatusOverlayState();
		}

		internal static void MarkOverdrawMeasurementUnsupported(string reason)
		{
			if (_instance == null || !_instance.CanMutateRuntime)
			{
				return;
			}

			_instance._overdrawController.MarkUnsupported(reason);
			_instance._latestMetrics = _instance.WithOverdrawState(_instance._latestMetrics);
			_instance.RefreshStatusOverlayState();
		}

		internal void SetOverlayVisible(bool visible)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_overlayRequestedVisible = visible;
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetVisible(visible);
			}

			ResetCpuCoreSamplerIfInactive();
			RefreshStatusOverlayState();
		}

		internal void SetOverlayCorner(PerfMeterOverlayCorner corner)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

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
			if (!CanMutateRuntime)
			{
				return;
			}

			_visualOverlayPresetId = string.Empty;
			_overlayLayout = PerfMeterSettingsStore.GetLayoutForMode(mode);
			_overlayMode = PerfMeterSettingsStore.GetLayoutMode(_overlayLayout, PerfMeterOverlayMode.Full);
			MarkOverlayPresetCustomIfLayoutChanged();
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetMode(_overlayMode);
				_overlay.SetLayout(_overlayLayout);
			}

			ResetCpuCoreSamplerIfInactive();
			RefreshStatusOverlayState();
		}

		internal void SetOverlayTheme(PerfMeterOverlayTheme theme)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_visualOverlayPresetId = string.Empty;
			_overlayTheme = PerfMeterSettingsStore.NormalizeOverlayTheme(theme);
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetTheme(_overlayTheme);
			}

			RefreshStatusOverlayState();
		}

		internal void SetOverlayLayout(PerfMeterOverlayLayout layout)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_visualOverlayPresetId = string.Empty;
			_overlayLayout = PerfMeterSettingsStore.NormalizeOverlayLayout(layout);
			_overlayMode = PerfMeterSettingsStore.GetLayoutMode(_overlayLayout, PerfMeterOverlayMode.Full);
			MarkOverlayPresetCustomIfLayoutChanged();
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetMode(_overlayMode);
				_overlay.SetLayout(_overlayLayout);
			}

			ResetCpuCoreSamplerIfInactive();
			RefreshStatusOverlayState();
		}

		internal void SetOverlayFontFamily(PerfMeterOverlayFontFamily fontFamily)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_visualOverlayPresetId = string.Empty;
			_overlayFontFamily = PerfMeterSettingsStore.NormalizeOverlayFontFamily(fontFamily);
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetFontFamily(_overlayFontFamily);
			}

			RefreshStatusOverlayState();
		}

		internal void SetOverlayPreset(PerfMeterOverlayPreset preset)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_overlayPreset = NormalizeOverlayPreset(preset);
			_visualOverlayPresetId = _overlayPreset.ToString();
			_overlayModules = PerfMeterSettingsStore.GetPresetModules(_overlayPreset);
			_overlayLayout = PerfMeterSettingsStore.GetPresetLayout(_overlayPreset);
			_overlayMode = PerfMeterSettingsStore.GetLayoutMode(_overlayLayout, PerfMeterOverlayMode.Full);

			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetMode(_overlayMode);
				_overlay.SetModules(_overlayModules);
				_overlay.SetLayout(_overlayLayout);
			}

			ResetCpuCoreSamplerIfInactive();
			RefreshStatusOverlayState();
		}

		internal void SetOverlayModules(PerfMeterOverlayModule modules)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_visualOverlayPresetId = string.Empty;
			PerfMeterOverlayModule normalizedModules = NormalizeOverlayModules(modules, _overlayPreset);
			if (_overlayPreset != PerfMeterOverlayPreset.Custom && normalizedModules != PerfMeterSettingsStore.GetPresetModules(_overlayPreset))
			{
				_overlayPreset = PerfMeterOverlayPreset.Custom;
				_visualOverlayPresetId = string.Empty;
			}

			_overlayModules = normalizedModules;
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetModules(_overlayModules);
			}

			ResetCpuCoreSamplerIfInactive();
			RefreshStatusOverlayState();
		}

		internal void SetOverlayModuleVisible(PerfMeterOverlayModule module, bool visible)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			PerfMeterOverlayModule normalizedModule = module & PerfMeterOverlayModule.All;
			if (normalizedModule == PerfMeterOverlayModule.None)
			{
				return;
			}

			_overlayPreset = PerfMeterOverlayPreset.Custom;
			_visualOverlayPresetId = string.Empty;
			_overlayModules = visible ? _overlayModules | normalizedModule : _overlayModules & ~normalizedModule;
			SetOverlayModules(_overlayModules);
		}

		internal void ApplyVisualOverlayPreset(string presetId, PerfMeterOverlayPresetJson preset)
		{
			if (!CanMutateRuntime || preset == null)
			{
				return;
			}

			PerfMeterOverlayPresetValidationResult validation = PerfMeterOverlayPresetUtility.Validate(preset);
			if (!validation.IsValid)
			{
				_lastCollectorWarning = PerfMeterOverlayPresetUtility.CombineWarnings(_lastCollectorWarning, validation.Warning);
				RefreshStatusOverlayState();
				return;
			}

			_visualOverlayPresetId = string.IsNullOrEmpty(presetId) ? preset.id : presetId;
			_overlayPreset = PerfMeterOverlayPreset.Custom;
			_overlayCorner = PerfMeterOverlayPresetUtility.GetCorner(preset);
			_overlayTheme = PerfMeterOverlayPresetUtility.GetTheme(preset);
			_overlayLayout = PerfMeterOverlayPresetUtility.GetLayout(preset);
			_overlayMode = PerfMeterSettingsStore.GetLayoutMode(_overlayLayout, PerfMeterOverlayMode.Full);
			_overlayFontFamily = PerfMeterOverlayPresetUtility.GetFontFamily(preset);
			_overlayModules = PerfMeterOverlayPresetUtility.GetEnabledModules(preset, out string widgetWarning);
			_overlayScale = PerfMeterOverlayPresetUtility.GetScale(preset, _overlayScale);
			_overlayOpacity = PerfMeterOverlayPresetUtility.GetOpacity(preset, _overlayOpacity);
			_lastCollectorWarning = PerfMeterOverlayPresetUtility.CombineWarnings(_lastCollectorWarning, PerfMeterOverlayPresetUtility.CombineWarnings(validation.Warning, widgetWarning));
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetCorner(_overlayCorner);
				_overlay.SetMode(_overlayMode);
				_overlay.SetTheme(_overlayTheme);
				_overlay.SetLayout(_overlayLayout);
				_overlay.SetFontFamily(_overlayFontFamily);
				_overlay.SetModules(_overlayModules);
				_overlay.SetTuning(_overlayScale, _overlayOpacity, _overlayFontSize, _overlayRefreshIntervalSeconds, _overlayGraphHistoryLength);
			}

			ResetCpuCoreSamplerIfInactive();
			RefreshStatusOverlayState();
		}

		internal void SetTargetFps(PerfMeterTargetFps targetFps)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_targetFps = NormalizeTargetFps(targetFps);
			RebuildAlertRules();
			_latestMetrics = WithTargetFrameBudget(_latestMetrics);
			EnsureOverlayState();

			if (_overlay != null)
			{
				_overlay.SetTargetFps(_targetFps);
			}

			RefreshStatusOverlayState();
		}

		internal void SetOverlayUpdateOptions(float refreshIntervalSeconds, int graphHistoryLength)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_overlayRefreshIntervalSeconds = Mathf.Clamp(refreshIntervalSeconds, PerfMeterSettingsStore.MinOverlayRefreshIntervalSeconds, PerfMeterSettingsStore.MaxOverlayRefreshIntervalSeconds);
			_overlayGraphHistoryLength = Mathf.Clamp(graphHistoryLength, PerfMeterSettingsStore.MinOverlayGraphHistoryLength, PerfMeterSettingsStore.MaxOverlayGraphHistoryLength);
			EnsureOverlayState();
			if (_overlay != null)
			{
				_overlay.SetTuning(_overlayScale, _overlayOpacity, _overlayFontSize, _overlayRefreshIntervalSeconds, _overlayGraphHistoryLength);
			}

			RefreshStatusOverlayState();
		}

		internal void SetOverlayTuning(PerfMeterSettingsSnapshot settings)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_settings = settings;
			_overlayScale = settings.OverlayScale;
			_overlayOpacity = settings.OverlayOpacity;
			_overlayFontSize = settings.OverlayFontSize;
			_overlayRefreshIntervalSeconds = settings.OverlayRefreshIntervalSeconds;
			_overlayGraphHistoryLength = settings.OverlayGraphHistoryLength;
			_overlayTheme = settings.OverlayTheme;
			_overlayLayout = settings.OverlayLayout;
			_overlayMode = PerfMeterSettingsStore.GetLayoutMode(_overlayLayout, settings.OverlayMode);
			_overlayFontFamily = settings.OverlayFontFamily;
			_visualOverlayPresetId = settings.ActiveOverlayPresetId;
			_overdrawDefaultFrameCount = settings.OverdrawDefaultFrameCount;
			_overdrawMaxFrameCount = settings.OverdrawMaxFrameCount;
			_alertEngine.ApplySettings(settings, _targetFps);

			if (_overlay != null)
			{
				_overlay.SetMode(_overlayMode);
				_overlay.SetTheme(_overlayTheme);
				_overlay.SetLayout(_overlayLayout);
				_overlay.SetFontFamily(_overlayFontFamily);
				_overlay.SetTuning(_overlayScale, _overlayOpacity, _overlayFontSize, _overlayRefreshIntervalSeconds, _overlayGraphHistoryLength);
			}

			ResetCpuCoreSamplerIfInactive();
			RefreshStatusOverlayState();
		}

		internal void SetEditorWarningLogsEnabled(bool enabled)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_settings = PerfMeterSettingsStore.WithEditorWarningsEnabled(_settings, enabled);
			_alertEngine.ApplySettings(_settings, _targetFps);
			RefreshStatusOverlayState();
		}

		internal void SetStructuredLogsEnabled(bool enabled)
		{
			if (!CanMutateRuntime)
			{
				return;
			}

			_structuredLogsEnabled = enabled;
			_alertEngine.SetStructuredLogsEnabled(enabled);
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
				_cpuCoreSampler.Reset();
				_cpuCoreSamplingActive = false;
				_overdrawController.Reset();
				_sessionRecorder.Stop(Time.realtimeSinceStartupAsDouble);
				FinalizeGraphicsStateCollectionForShutdown();
				FinalizeMemorySnapshotForShutdown("Runtime disabled during memory snapshot capture.");
				FinalizeCaptureBundleForShutdown("Runtime disabled during capture.");
				bool captureReleased = TryResetCaptureCoordinator();

				_alertEngine.Clear();
				_overdrawHeatmapVisible = false;
				_status = CreateStoppedStatus();
				_latestMetrics = PerfMeterMetricsSnapshot.Stopped;
				_latestPlatformTelemetry = PerfMeterPlatformTelemetrySnapshot.Unavailable();
				PerfMeterProfilerInstrumentation.Reset();
				if (!captureReleased)
				{
					RecordPendingCaptureCleanup();
				}

				PerfMeterSelfObservability.Stop();
			}
		}

		private void OnDestroy()
		{
			FinalizeCaptureBundleForShutdown("Runtime destroyed during capture.");
			bool captureReleased = TryResetCaptureCoordinator();
			if (captureReleased)
			{
				_pendingAlertCaptureId = string.Empty;
			}

			if (!captureReleased && _captureCoordinator != null)
			{
				_pendingCaptureCleanup = _captureCoordinator;
				_pendingAlertCaptureId = _captureCoordinator.ScopeActive ? _alertCaptureId : string.Empty;
				RecordPendingCaptureCleanup();
			}
			else if (object.ReferenceEquals(_pendingCaptureCleanup, _captureCoordinator))
			{
				_pendingCaptureCleanup = null;
				_pendingAlertCaptureId = string.Empty;
			}

			_cpuCoreSampler.Dispose();
			if (_instance == this)
			{
				_instance = null;
			}
		}

		private void SetRunningPlaceholders()
		{
			int frame = Time.frameCount;
			_frameStatsSampler.Reset();
			ApplyAlertSettings();
			_status = CreateStatus(
				PerfMeterRuntimeState.Running,
				frame,
				GetCollectionMode(),
				PerfMeterFrameTimingAvailability.NotCollected,
				string.Empty,
				_collector.LastError,
				PerfMeterBottleneck.Unknown,
				GetAvailableCounters(),
				GetUnavailableCounters(),
				IsOverlayVisible,
				_overdrawController.State,
				_overdrawController.Progress,
				_overdrawController.Ratio,
				_overdrawHeatmapVisible,
				_overlayCorner,
				_overlayMode,
				_overlayTheme,
				_overlayLayout,
				_overlayFontFamily,
				_targetFps,
				_overlayPreset,
				_overlayModules,
				_sessionRecorder.State,
				_sessionRecorder.IsRecording,
				_sessionRecorder.SampleCount,
				_sessionRecorder.DroppedSampleCount,
				_alertEngine.ActiveAlertCount,
				_alertEngine.FiredAlertCount,
				_alertEngine.LatestAlert.RuleId,
				_alertEngine.LatestAlert.Message,
				applicationFocused: _applicationFocused,
				applicationPaused: _applicationPaused,
				editorWarningsEnabled: _settings.EditorWarningsEnabled,
				visualOverlayPresetId: _visualOverlayPresetId,
				selfOverhead: PerfMeterSelfObservability.GetSnapshot());

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
				_overdrawController.Progress,
				srpBatcherInstances: 0,
				frameSampleCount: 0,
				gpuValidSampleCount: 0,
				averageFps: 0d,
				onePercentLowFps: 0d,
				pointOnePercentLowFps: 0d,
				frameSpikeCount: 0,
				severeFrameSpikeCount: 0,
				shaderGpuProgramCreationValue: 0L,
				graphicsPipelineCreationValue: 0L,
				profilerMetricCatalogRevision: _collector.ProfilerMetricCatalogRevision,
				shaderGpuProgramCreationCapability: _collector.GetProfilerMetricCapability(PerfMeterProfilerMetricSemantic.ShaderGpuProgramCreation),
				graphicsPipelineCreationCapability: _collector.GetProfilerMetricCapability(PerfMeterProfilerMetricSemantic.GraphicsPipelineCreation));
		}

		private static PerfMeterStatusSnapshot CreateStatus(PerfMeterRuntimeState state, int collectionFrame, string warning, string lastError)
		{
			return CreateStatus(
				state,
				collectionFrame,
				state == PerfMeterRuntimeState.Stopped ? PerfMeterCollectionMode.Stopped : PerfMeterCollectionMode.Overlay,
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
				false,
				PerfMeterOverlayCorner.TopRight,
				PerfMeterOverlayMode.Full,
				PerfMeterOverlayTheme.ClassicDark,
				PerfMeterOverlayLayout.MetricBars,
				PerfMeterOverlayFontFamily.Manrope,
				PerfMeterTargetFps.Fps60,
				PerfMeterOverlayPreset.FullDiagnostics,
				PerfMeterSettingsStore.GetPresetModules(PerfMeterOverlayPreset.FullDiagnostics),
				PerfMeterSessionState.Idle,
				false,
				0,
				0,
				0,
				0,
				string.Empty,
				string.Empty,
				true,
				false,
				selfOverhead: PerfMeterSelfOverheadSnapshot.NotInitialized);
		}

		private static PerfMeterStatusSnapshot CreateStatus(
			PerfMeterRuntimeState state,
			int collectionFrame,
			PerfMeterCollectionMode collectionMode,
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
			bool overdrawHeatmapVisible = false,
			PerfMeterOverlayCorner overlayCorner = PerfMeterOverlayCorner.TopRight,
			PerfMeterOverlayMode overlayMode = PerfMeterOverlayMode.Full,
			PerfMeterOverlayTheme overlayTheme = PerfMeterOverlayTheme.ClassicDark,
			PerfMeterOverlayLayout overlayLayout = PerfMeterOverlayLayout.MetricBars,
			PerfMeterOverlayFontFamily overlayFontFamily = PerfMeterOverlayFontFamily.Manrope,
			PerfMeterTargetFps targetFps = PerfMeterTargetFps.Fps60,
			PerfMeterOverlayPreset overlayPreset = PerfMeterOverlayPreset.FullDiagnostics,
			PerfMeterOverlayModule overlayModules = PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Timing | PerfMeterOverlayModule.Graphs | PerfMeterOverlayModule.Rendering | PerfMeterOverlayModule.SrpBatcher | PerfMeterOverlayModule.Brg | PerfMeterOverlayModule.Uploads | PerfMeterOverlayModule.Memory | PerfMeterOverlayModule.Gc | PerfMeterOverlayModule.GpuMemory | PerfMeterOverlayModule.Overdraw | PerfMeterOverlayModule.Heatmap | PerfMeterOverlayModule.Warnings | PerfMeterOverlayModule.CustomMetrics | PerfMeterOverlayModule.CpuCoreBars,
			PerfMeterSessionState sessionState = PerfMeterSessionState.Idle,
			bool sessionRecording = false,
			int sessionSampleCount = 0,
			int sessionDroppedSampleCount = 0,
			int activeAlertCount = 0,
			int firedAlertCount = 0,
			string latestAlertRuleId = "",
			string latestAlertMessage = "",
			bool applicationFocused = true,
			bool applicationPaused = false,
			bool editorWarningsEnabled = true,
			string visualOverlayPresetId = "",
			PerfMeterSelfOverheadSnapshot selfOverhead = default)
		{
			return new PerfMeterStatusSnapshot(
				state,
				PerfMeterAvailability.Available,
				collectionMode,
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
				overdrawHeatmapVisible,
				overlayCorner,
				overlayMode,
				NormalizeTargetFps(targetFps),
				NormalizeOverlayPreset(overlayPreset),
				NormalizeOverlayModules(overlayModules, NormalizeOverlayPreset(overlayPreset)),
				sessionState,
				sessionRecording,
				sessionSampleCount,
				sessionDroppedSampleCount,
				activeAlertCount,
				firedAlertCount,
				latestAlertRuleId,
				latestAlertMessage,
				applicationFocused,
				applicationPaused,
				overlayTheme: PerfMeterSettingsStore.NormalizeOverlayTheme(overlayTheme),
				overlayLayout: PerfMeterSettingsStore.NormalizeOverlayLayout(overlayLayout),
				overlayFontFamily: PerfMeterSettingsStore.NormalizeOverlayFontFamily(overlayFontFamily),
				editorWarningsEnabled: editorWarningsEnabled,
				visualOverlayPresetId: visualOverlayPresetId,
				selfOverhead: selfOverhead);
		}

		private PerfMeterMetricsSnapshot WithOverdrawState(PerfMeterMetricsSnapshot metrics)
		{
			PerfMeterProfilerInstrumentation.RecordOverdrawState(_overdrawController.State);
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
				frameStats.SevereFrameSpikeCount,
				metrics.ShaderGpuProgramCreationValue,
				metrics.GraphicsPipelineCreationValue,
				metrics.ProfilerMetricCatalogRevision,
				metrics.ShaderGpuProgramCreationCapability,
				metrics.GraphicsPipelineCreationCapability);
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
				metrics.SevereFrameSpikeCount,
				metrics.ShaderGpuProgramCreationValue,
				metrics.GraphicsPipelineCreationValue,
				metrics.ProfilerMetricCatalogRevision,
				metrics.ShaderGpuProgramCreationCapability,
				metrics.GraphicsPipelineCreationCapability);
		}

		private bool TrySkipCollectionForFocusState(out string warning)
		{
			if (_applicationPaused || !_applicationFocused)
			{
				warning = FocusPausedWarning;
				return true;
			}

			if (_focusResumeIgnoreFrames > 0)
			{
				using (PerfMeterProfilerInstrumentation.FrameTimingMarker.Auto())
				{
					FrameTimingManager.CaptureFrameTimings();
				}
				_focusResumeIgnoreFrames--;
				warning = FocusResumeWarmupWarning;
				return true;
			}

			warning = string.Empty;
			return false;
		}

		private void RefreshRunningStatus(int frame, PerfMeterFrameTimingAvailability frameTimingAvailability, string warning)
		{
			_status = CreateStatus(
				PerfMeterRuntimeState.Running,
				frame,
				GetCollectionMode(),
				frameTimingAvailability,
				CombineWarnings(CombineWarnings(warning, _overdrawController.Warning), GetCpuCoreWarning()),
				_collector.LastError,
				_latestMetrics.Bottleneck,
				GetAvailableCounters(),
				GetUnavailableCounters(),
				IsOverlayVisible,
				_overdrawController.State,
				_overdrawController.Progress,
				_overdrawController.Ratio,
				_overdrawHeatmapVisible,
				_overlayCorner,
				_overlayMode,
				_overlayTheme,
				_overlayLayout,
				_overlayFontFamily,
				_targetFps,
				_overlayPreset,
				_overlayModules,
				_sessionRecorder.State,
				_sessionRecorder.IsRecording,
				_sessionRecorder.SampleCount,
				_sessionRecorder.DroppedSampleCount,
				_alertEngine.ActiveAlertCount,
				_alertEngine.FiredAlertCount,
				_alertEngine.LatestAlert.RuleId,
				_alertEngine.LatestAlert.Message,
				_applicationFocused,
				_applicationPaused,
				_settings.EditorWarningsEnabled,
				_visualOverlayPresetId,
				PerfMeterSelfObservability.GetSnapshot());
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
			if (!Application.isPlaying || !CanMutateRuntime)
			{
				return;
			}

			if (!IsRuntimeOverlaySupported)
			{
				DestroyOverlay();
				RefreshStatusOverlayState();
				return;
			}

			if (!_overlayRequestedVisible)
			{
				DestroyOverlay();
				RefreshStatusOverlayState();
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
			_overlay.SetTheme(_overlayTheme);
			_overlay.SetLayout(_overlayLayout);
			_overlay.SetFontFamily(_overlayFontFamily);
			_overlay.SetModules(_overlayModules);
			_overlay.SetTargetFps(_targetFps);
			_overlay.SetTuning(_overlayScale, _overlayOpacity, _overlayFontSize, _overlayRefreshIntervalSeconds, _overlayGraphHistoryLength);
			_overlay.SetVisible(_overlayRequestedVisible);
			RefreshStatusOverlayState();
		}

		private void DestroyOverlay()
		{
			if (_overlay == null)
			{
				return;
			}

			GameObject overlayObject = _overlay.gameObject;
			_overlay.SetVisible(false);
			overlayObject.SetActive(false);
			_overlay = null;
			if (Application.isPlaying)
			{
				Destroy(overlayObject);
			}
			else
			{
				DestroyImmediate(overlayObject);
			}
		}

		private void RefreshStatusOverlayState()
		{
			_status = CreateStatus(
				_status.State,
				_status.CollectionFrame,
				GetCollectionMode(),
				_status.FrameTimingAvailability,
				CombineWarnings(CombineWarnings(_lastCollectorWarning, _overdrawController.Warning), GetCpuCoreWarning()),
				_collector.LastError,
				_status.Bottleneck,
				GetAvailableCounters(),
				GetUnavailableCounters(),
				IsOverlayVisible,
				_overdrawController.State,
				_overdrawController.Progress,
				_overdrawController.Ratio,
				_overdrawHeatmapVisible,
				_overlayCorner,
				_overlayMode,
				_overlayTheme,
				_overlayLayout,
				_overlayFontFamily,
				_targetFps,
				_overlayPreset,
				_overlayModules,
				_sessionRecorder.State,
				_sessionRecorder.IsRecording,
				_sessionRecorder.SampleCount,
				_sessionRecorder.DroppedSampleCount,
				_alertEngine.ActiveAlertCount,
				_alertEngine.FiredAlertCount,
				_alertEngine.LatestAlert.RuleId,
				_alertEngine.LatestAlert.Message,
				_applicationFocused,
				_applicationPaused,
				_settings.EditorWarningsEnabled,
				_visualOverlayPresetId,
				PerfMeterSelfObservability.GetSnapshot());
		}

		private PerfMeterCounterAvailability GetAvailableCounters()
		{
			PerfMeterCounterAvailability counters = _collector.AvailableCounters;
			if (ShouldSampleCpuCoreLoads() && (_cpuCoreSampler.Availability == PerfMeterCpuCoreLoadAvailability.Available || _cpuCoreSampler.Availability == PerfMeterCpuCoreLoadAvailability.WarmingUp))
			{
				counters |= PerfMeterCounterAvailability.CpuCoreLoad;
			}

			return counters;
		}

		private PerfMeterCounterAvailability GetUnavailableCounters()
		{
			PerfMeterCounterAvailability counters = _collector.UnavailableCounters;
			if (ShouldSampleCpuCoreLoads() && (_cpuCoreSampler.Availability == PerfMeterCpuCoreLoadAvailability.Unsupported || _cpuCoreSampler.Availability == PerfMeterCpuCoreLoadAvailability.Unavailable))
			{
				counters |= PerfMeterCounterAvailability.CpuCoreLoad;
			}

			return counters;
		}

		private string GetCpuCoreWarning()
		{
			return ShouldSampleCpuCoreLoads() ? _cpuCoreSampler.Warning : string.Empty;
		}

		private void UpdateCpuCoreSampler(float unscaledTime)
		{
			if (!ShouldSampleCpuCoreLoads())
			{
				ResetCpuCoreSamplerIfInactive();
				return;
			}

			if (!_cpuCoreSamplingActive)
			{
				_cpuCoreSampler.Reset();
				_cpuCoreSamplingActive = true;
			}

			_cpuCoreSampler.Update(unscaledTime);
		}

		private void ResetCpuCoreSamplerIfInactive()
		{
			if (ShouldSampleCpuCoreLoads())
			{
				return;
			}

			if (_cpuCoreSamplingActive || _cpuCoreSampler.CoreCount > 0)
			{
				_cpuCoreSampler.Reset();
				_cpuCoreSamplingActive = false;
			}
		}

		private void MarkOverlayPresetCustomIfLayoutChanged()
		{
			if (_overlayPreset != PerfMeterOverlayPreset.Custom && _overlayLayout != PerfMeterSettingsStore.GetPresetLayout(_overlayPreset))
			{
				_overlayPreset = PerfMeterOverlayPreset.Custom;
				_visualOverlayPresetId = string.Empty;
			}
		}

		private bool ShouldSampleCpuCoreLoads()
		{
			if (!_overlayRequestedVisible)
			{
				return false;
			}

			if ((_overlayModules & (PerfMeterOverlayModule.CpuCoreBars | PerfMeterOverlayModule.CpuCoreGraphs)) != 0)
			{
				return true;
			}

			return _overlayLayout == PerfMeterOverlayLayout.MetricBars && _overlayMode != PerfMeterOverlayMode.FpsOnly && (_overlayModules & PerfMeterOverlayModule.CpuCores) != 0;
		}

		private void ApplyAlertSettings()
		{
			_settings = PerfMeterSettingsStore.LoadFromResources();
			if (!_alertEngineInitialized)
			{
				ApplyAlertSettings(_settings);
				return;
			}

			_overdrawDefaultFrameCount = _settings.OverdrawDefaultFrameCount;
			_overdrawMaxFrameCount = _settings.OverdrawMaxFrameCount;
			_alertEngine.ApplySettings(_settings, _targetFps);
		}

		private void ApplyAlertSettings(PerfMeterSettingsSnapshot settings)
		{
			_settings = settings;
			_overdrawDefaultFrameCount = settings.OverdrawDefaultFrameCount;
			_overdrawMaxFrameCount = settings.OverdrawMaxFrameCount;
			_alertEngine = new PerfMeterAlertEngine(PerfMeterAlertEngine.CreateDefaultRules(_targetFps, settings));
			_alertEngine.ApplySettings(settings, _targetFps);
			_alertEngine.SetStructuredLogsEnabled(_structuredLogsEnabled);
			PerfMeterAlertHistoryResetReason resetReason = _alertEngineInitialized
				? PerfMeterAlertHistoryResetReason.RulesChanged
				: PerfMeterAlertHistoryResetReason.RuntimeStarted;
			_alertEngine.ResetHistory(Time.frameCount, Time.realtimeSinceStartupAsDouble, resetReason);
			_alertEngineInitialized = true;
		}

		private void RebuildAlertRules()
		{
			ApplyAlertSettings(_settings);
		}

		private PerfMeterCollectionMode GetCollectionMode()
		{
			if (!CanMutateRuntime)
			{
				return PerfMeterCollectionMode.Stopped;
			}

			if (_overdrawHeatmapVisible || IsOverdrawDiagnosticState(_overdrawController.State))
			{
				return PerfMeterCollectionMode.OverdrawDiagnostic;
			}

			return _overlayRequestedVisible ? PerfMeterCollectionMode.Overlay : PerfMeterCollectionMode.Background;
		}

		private static bool IsRuntimeOverlaySupported
		{
			get
			{
			#if UNITY_6000_4_OR_NEWER
				return true;
			#else
				return false;
			#endif
			}
		}

		private static bool IsOverdrawDiagnosticState(PerfMeterOverdrawMeasurementState state)
		{
			return state == PerfMeterOverdrawMeasurementState.Measuring || state == PerfMeterOverdrawMeasurementState.Completed || state == PerfMeterOverdrawMeasurementState.Error || state == PerfMeterOverdrawMeasurementState.Unsupported;
		}

		private static PerfMeterCollectionMode NormalizeCollectionMode(PerfMeterCollectionMode mode)
		{
			switch (mode)
			{
				case PerfMeterCollectionMode.Background:
				case PerfMeterCollectionMode.Overlay:
				case PerfMeterCollectionMode.OverdrawDiagnostic:
					return mode;
				default:
					return PerfMeterCollectionMode.Overlay;
			}
		}

		private static PerfMeterOverlayPreset NormalizeOverlayPreset(PerfMeterOverlayPreset preset)
		{
			switch (preset)
			{
				case PerfMeterOverlayPreset.Custom:
				case PerfMeterOverlayPreset.Minimal:
				case PerfMeterOverlayPreset.Timing:
				case PerfMeterOverlayPreset.Rendering:
				case PerfMeterOverlayPreset.Memory:
				case PerfMeterOverlayPreset.Overdraw:
				case PerfMeterOverlayPreset.FullDiagnostics:
				case PerfMeterOverlayPreset.AgentDebug:
					return preset;
				default:
					return PerfMeterOverlayPreset.FullDiagnostics;
			}
		}

		private static PerfMeterOverlayModule NormalizeOverlayModules(PerfMeterOverlayModule modules, PerfMeterOverlayPreset preset)
		{
			PerfMeterOverlayModule normalized = modules & PerfMeterOverlayModule.All;
			return normalized == PerfMeterOverlayModule.None ? PerfMeterSettingsStore.GetPresetModules(preset) : normalized;
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

		private bool CanMutateRuntime => isActiveAndEnabled && !_captureCleanupPending;

		private bool TryResetCaptureCoordinator()
		{
			if (_captureCoordinator == null || _captureCoordinator.Reset())
			{
				_captureCleanupPending = false;
				_alertCaptureId = string.Empty;
				return true;
			}

			_captureCleanupPending = true;
			_status = CreateStoppedStatus();
			return false;
		}

		private void RecordPendingCaptureCleanup()
		{
			if (_captureCoordinator == null)
			{
				return;
			}

			PerfMeterProfilerInstrumentation.RecordCaptureState(_captureCoordinator.Status.State);
			PerfMeterProfilerInstrumentation.RecordAlertScopeActive(_captureCoordinator.ScopeActive || !string.IsNullOrEmpty(_alertCaptureId));
		}

		private void ResetProfilerInstrumentationForRunningState()
		{
			PerfMeterProfilerInstrumentation.Reset();
			PerfMeterProfilerInstrumentation.RecordSessionState(_sessionRecorder.State);
			PerfMeterProfilerInstrumentation.RecordAlertScopeActive(false);
			PerfMeterProfilerInstrumentation.RecordOverdrawState(_overdrawController.State);
			PerfMeterProfilerInstrumentation.RecordCaptureState(_captureCoordinator != null ? _captureCoordinator.Status.State : PerfMeterCaptureState.Idle);
		}

		private static bool TryReleasePendingCaptureCleanup()
		{
			if (_pendingCaptureCleanup == null)
			{
				return true;
			}

			if (!_pendingCaptureCleanup.Reset())
			{
				PerfMeterProfilerInstrumentation.RecordCaptureState(_pendingCaptureCleanup.Status.State);
				PerfMeterProfilerInstrumentation.RecordAlertScopeActive(_pendingCaptureCleanup.ScopeActive);
				return false;
			}

			_pendingCaptureCleanup = null;
			_pendingAlertCaptureId = string.Empty;
			PerfMeterProfilerInstrumentation.Reset();
			return true;
		}

		internal static bool CancelPendingCapture(string captureId)
		{
			if (_pendingCaptureCleanup == null)
			{
				return false;
			}

			bool canceled = _pendingCaptureCleanup.Cancel(captureId);
			if (!canceled)
			{
				PerfMeterProfilerInstrumentation.RecordCaptureState(_pendingCaptureCleanup.Status.State);
				PerfMeterProfilerInstrumentation.RecordAlertScopeActive(_pendingCaptureCleanup.ScopeActive);
			}
			else
			{
				_pendingAlertCaptureId = string.Empty;
			}

			return canceled;
		}

		private void TickCaptureAndUpdateBundle()
		{
			if (_captureCoordinator == null)
			{
				return;
			}

			_captureCoordinator.Tick();
			PerfMeterCaptureStatusSnapshot captureStatus = _captureCoordinator.Status;
			CaptureBundles.UpdateCaptureStatus(captureStatus);
			if (!captureStatus.IsActive)
			{
				FinalizeCaptureBundle(captureStatus);
			}
		}

		private void FinalizeCaptureBundleForShutdown(string warning)
		{
			if (_captureCoordinator != null)
			{
				PerfMeterCaptureStatusSnapshot captureStatus = _captureCoordinator.Status;
				PerfMeterCaptureBundleStatusSnapshot bundleStatus = CaptureBundles.GetStatus(captureStatus.CaptureId);
				if (bundleStatus.State == PerfMeterCaptureBundleState.Recording)
				{
					if (captureStatus.IsActive)
					{
						_captureCoordinator.Cancel(captureStatus.CaptureId);
						captureStatus = _captureCoordinator.Status;
					}

					CaptureBundles.UpdateCaptureStatus(captureStatus);
					FinalizeCaptureBundle(captureStatus);
				}
			}

			CaptureBundles.CompletePendingScreenshotAsUnavailable(_captureBundleId, warning);
		}

		private void EvaluateMemorySnapshotTriggers(int frame)
		{
			if (_memoryAutomaticAttemptBlocked || !_memorySnapshotTriggerEvaluator.TryEvaluate(_latestMetrics, _memorySnapshotTriggers, out PerfMeterMemorySnapshotTrigger trigger))
			{
				return;
			}

			PerfMeterMemorySnapshotOptions options = new PerfMeterMemorySnapshotOptions(
				"memory-" + trigger.ToString().ToLowerInvariant() + "-" + frame,
				trigger,
				_memorySnapshotTriggers.CaptureFlags,
				_memorySnapshotTriggers.MinimumFreeDiskBytes,
				_memorySnapshotTriggers.CooldownSeconds);
			PerfMeterMemorySnapshotRequestResult result = RequestMemorySnapshot(options);
			if (result == PerfMeterMemorySnapshotRequestResult.Unavailable ||
				result == PerfMeterMemorySnapshotRequestResult.InsufficientDiskSpace ||
				result == PerfMeterMemorySnapshotRequestResult.InvalidRequest ||
				result == PerfMeterMemorySnapshotRequestResult.Failed ||
				(result == PerfMeterMemorySnapshotRequestResult.RejectedOverlap && _memorySnapshotCoordinator != null && _memorySnapshotCoordinator.CleanupBlocked))
			{
				_memoryAutomaticAttemptBlocked = true;
			}
		}

		private void ProcessMemorySnapshotCompletion()
		{
			if (_memorySnapshotCoordinator != null && _memorySnapshotCoordinator.TryConsumeCompletion(out PerfMeterMemorySnapshotStatusSnapshot status, out PerfMeterMemorySnapshotArtifact artifact))
			{
				FinalizeMemorySnapshot(status, artifact);
			}
		}

		private void FinalizeMemorySnapshot(PerfMeterMemorySnapshotStatusSnapshot status, PerfMeterMemorySnapshotArtifact artifact)
		{
			PerfMeterCaptureBundleStatusSnapshot bundleStatus = CaptureBundles.GetStatus(status.CaptureId);
			if (bundleStatus.State != PerfMeterCaptureBundleState.Recording || bundleStatus.RequestedTool != PerfMeterCaptureTool.MemoryProfiler)
			{
				if (artifact.IsAvailable)
				{
					_memorySnapshotCoordinator?.DiscardArtifact(artifact.SourcePath);
				}

				return;
			}

			CaptureBundles.ObserveMemorySnapshot(
				status,
				artifact,
				_sessionRecorder.GetSummary(),
				_sessionRecorder.GetSamplesCopy(),
				Status,
				PerfMeterDeviceInfoProvider.CreateSnapshot(),
				PerfMeterCameraSnapshotProvider.CreateSnapshot(PerfMeterCameraSource.Auto, null),
				PerfMeterRenderGraphAnalytics.GetSnapshot(),
				PerfMeterRenderGraphAnalytics.GetRenderIntegrationSnapshot());
		}

		private void FinalizeMemorySnapshotForShutdown(string warning)
		{
			if (_memorySnapshotCoordinator == null)
			{
				return;
			}

			_memorySnapshotCoordinator.Shutdown(Time.realtimeSinceStartupAsDouble, warning);
			ProcessMemorySnapshotCompletion();
		}

		private void FinalizeGraphicsStateCollectionForShutdown()
		{
			if (_graphicsStateCollectionCoordinator == null)
			{
				return;
			}

			_graphicsStateCollectionCoordinator.Shutdown();
			if (_graphicsStateCollectionCoordinator.HasPendingCleanup)
			{
				_pendingGraphicsStateCleanup = _graphicsStateCollectionCoordinator;
			}
		}

		private static bool TryReleasePendingGraphicsStateCleanup()
		{
			if (_pendingGraphicsStateCleanup == null)
			{
				return true;
			}

			if (!_pendingGraphicsStateCleanup.RetryPendingCleanup() || _pendingGraphicsStateCleanup.HasPendingCleanup)
			{
				return false;
			}

			_pendingGraphicsStateCleanup = null;
			return true;
		}

		private System.Collections.IEnumerator TickGraphicsStateTraceAtEndOfFrame(string captureId, int generation)
		{
			while (CanMutateRuntime && _graphicsStateCollectionCoordinator != null)
			{
				if (!_graphicsStateCollectionCoordinator.IsActiveTrace(captureId, generation))
				{
					yield break;
				}

				yield return Application.isBatchMode ? null : _graphicsStateEndOfFrame;
				if (_graphicsStateCollectionCoordinator.IsActiveTrace(captureId, generation))
				{
					_graphicsStateCollectionCoordinator.Tick(generation);
				}
			}
		}

		private bool TryReleaseGraphicsStateCollectionCleanup()
		{
			return _graphicsStateCollectionCoordinator == null ||
				!_graphicsStateCollectionCoordinator.HasPendingCleanup ||
				(_graphicsStateCollectionCoordinator.RetryPendingCleanup() && !_graphicsStateCollectionCoordinator.HasPendingCleanup);
		}

		private bool TryDiscardCaptureBundleMemoryArtifact(bool onlyIfCoordinatorDoesNotOwnArtifact)
		{
			if (!CaptureBundles.TryGetMemorySnapshotArtifact(out PerfMeterMemorySnapshotArtifact artifact))
			{
				return true;
			}

			if (onlyIfCoordinatorDoesNotOwnArtifact && _memorySnapshotCoordinator != null && _memorySnapshotCoordinator.HasArtifact(artifact.SourcePath))
			{
				return true;
			}

			bool deleted = _memorySnapshotCoordinator != null && _memorySnapshotCoordinator.HasArtifact(artifact.SourcePath)
				? _memorySnapshotCoordinator.DiscardArtifact(artifact.SourcePath)
				: new PerfMeterMemorySnapshotStorage(System.IO.Path.Combine(Application.dataPath, "..")).TryDelete(artifact.SourcePath);
			if (deleted)
			{
				CaptureBundles.ClearMemorySnapshotArtifact(artifact.Status.CaptureId, artifact.SourcePath);
			}

			return deleted;
		}

		private void FinalizeCaptureBundle(PerfMeterCaptureStatusSnapshot captureStatus)
		{
			PerfMeterCaptureBundleStatusSnapshot bundleStatus = CaptureBundles.GetStatus(captureStatus.CaptureId);
			if (bundleStatus.State != PerfMeterCaptureBundleState.Recording)
			{
				return;
			}

			PerfMeterAlertSnapshot[] alerts = _alertEngine.GetFiredCaptureEvents(captureStatus.CaptureId, out bool alertsTruncated);
			CaptureBundles.ObserveCapture(
				captureStatus,
				_sessionRecorder.GetSummary(),
				_sessionRecorder.GetSamplesCopy(),
				Status,
				PerfMeterDeviceInfoProvider.CreateSnapshot(),
				PerfMeterCameraSnapshotProvider.CreateSnapshot(PerfMeterCameraSource.Auto, null),
				PerfMeterRenderGraphAnalytics.GetSnapshot(),
				PerfMeterRenderGraphAnalytics.GetRenderIntegrationSnapshot(),
				alerts,
				alertsTruncated);
			ScheduleCaptureBundleScreenshot();
		}

		private void ScheduleCaptureBundleScreenshot()
		{
			if (!CaptureBundles.TryStartScreenshot(out string captureId, out string bundleId))
			{
				return;
			}

			if (Application.isBatchMode || !Application.isPlaying)
			{
				CaptureBundles.CompleteScreenshot(captureId, bundleId, null, "Runtime screenshot is unavailable outside non-batch Play Mode.", true);
				return;
			}

			StartCoroutine(PerfMeterCaptureScreenshot.Capture((bytes, error, unavailable) => CaptureBundles.CompleteScreenshot(captureId, bundleId, bytes, error, unavailable)));
		}

		private void EnsureCaptureCoordinator()
		{
			if (_captureCoordinator == null)
			{
				_captureCoordinator = new PerfMeterCaptureCoordinator(new PerfMeterExternalGpuProfilerBackend(), new RuntimeCaptureScope(this));
			}
		}

		private void EnsureMemorySnapshotCoordinator()
		{
			if (_memorySnapshotCoordinator == null)
			{
				_memorySnapshotCoordinator = new PerfMeterMemorySnapshotCoordinator(
					new PerfMeterMemorySnapshotStorage(System.IO.Path.Combine(Application.dataPath, "..")));
			}
		}

		private void EnsureGraphicsStateCollectionCoordinator()
		{
			if (_graphicsStateCollectionCoordinator == null)
			{
				_graphicsStateCollectionCoordinator = new PerfMeterGraphicsStateCollectionCoordinator(
					new PerfMeterGraphicsStateCollectionStorage(System.IO.Path.Combine(Application.dataPath, "..")));
			}
		}

		private PerfMeterGraphicsDiagnosticsSnapshot CreateGraphicsDiagnostics()
		{
			PerfMeterProfilerMetricCapabilitySnapshot shader = _latestMetrics.ShaderGpuProgramCreationCapability;
			PerfMeterProfilerMetricCapabilitySnapshot pipeline = _latestMetrics.GraphicsPipelineCreationCapability;
			bool available = shader.IsAvailable || pipeline.IsAvailable;
			PerfMeterGraphicsStateCollectionCapabilitiesSnapshot stateCollection = PerfMeterGraphicsStateCollectionBackendRegistry.GetCapabilities();
			return new PerfMeterGraphicsDiagnosticsSnapshot(
				available ? PerfMeterAvailability.Available : PerfMeterAvailability.Unavailable,
				_latestMetrics.CollectionFrame,
				_latestMetrics.ProfilerMetricCatalogRevision,
				SystemInfo.graphicsDeviceType,
				SystemInfo.graphicsDeviceName,
				SystemInfo.graphicsDeviceVendor,
				SystemInfo.graphicsDeviceVersion,
				stateCollection.Availability,
				stateCollection.SupportsParallelPsoCreation,
				_latestMetrics.ShaderGpuProgramCreationValue,
				_latestMetrics.GraphicsPipelineCreationValue,
				shader,
				pipeline,
				available ? string.Empty : "Shader and graphics-pipeline creation profiler markers are unavailable on this runtime.");
		}


		private sealed class RuntimeCaptureScope : IPerfMeterCaptureScope
		{
			private readonly PerfMeterRuntime _runtime;

			internal RuntimeCaptureScope(PerfMeterRuntime runtime)
			{
				_runtime = runtime;
			}

			public bool TryBegin(string captureId)
			{
				return string.IsNullOrEmpty(_runtime._alertCaptureId) && _runtime.BeginAlertCaptureCore(captureId);
			}

			public bool TryEnd(string captureId)
			{
				return _runtime.EndAlertCaptureCore(captureId);
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
