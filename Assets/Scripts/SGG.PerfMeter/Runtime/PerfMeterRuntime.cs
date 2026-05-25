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
		private static PerfMeterRuntime _instance;

		private readonly PerfMeterCollector _collector = new PerfMeterCollector();
		private readonly PerfMeterFrameStatsSampler _frameStatsSampler = new PerfMeterFrameStatsSampler();
		private readonly PerfMeterCpuCoreSampler _cpuCoreSampler = new PerfMeterCpuCoreSampler();
		private readonly PerfMeterOverdrawController _overdrawController = new PerfMeterOverdrawController();
		private readonly PerfMeterSessionRecorder _sessionRecorder = new PerfMeterSessionRecorder();
		private PerfMeterAlertEngine _alertEngine = new PerfMeterAlertEngine();
		private PerfMeterStatusSnapshot _status;
		private PerfMeterMetricsSnapshot _latestMetrics;
		private PerfMeterCustomMetricSnapshot[] _latestCustomMetrics = System.Array.Empty<PerfMeterCustomMetricSnapshot>();
		private PerfMeterOverlay _overlay;
		private string _lastCollectorWarning = string.Empty;
		private PerfMeterOverlayCorner _overlayCorner = PerfMeterOverlayCorner.TopRight;
		private PerfMeterOverlayMode _overlayMode = PerfMeterOverlayMode.Full;
		private PerfMeterOverlayTheme _overlayTheme = PerfMeterOverlayTheme.ClassicDark;
		private PerfMeterOverlayLayout _overlayLayout = PerfMeterOverlayLayout.MetricBars;
		private PerfMeterOverlayFontFamily _overlayFontFamily = PerfMeterOverlayFontFamily.Manrope;
		private PerfMeterOverlayPreset _overlayPreset = PerfMeterOverlayPreset.FullDiagnostics;
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
		private int _focusResumeIgnoreFrames;

		internal static PerfMeterRuntime Instance => _instance;
		internal PerfMeterStatusSnapshot Status => _status;
		internal PerfMeterMetricsSnapshot LatestMetrics => _latestMetrics;
		internal bool IsOverlayVisible => IsRuntimeOverlaySupported && _overlay != null && _overlay.IsVisible;
		internal PerfMeterOverlayCorner OverlayCorner => _overlayCorner;
		internal PerfMeterOverlayMode OverlayMode => _overlayMode;
		internal PerfMeterOverlayTheme OverlayTheme => _overlayTheme;
		internal PerfMeterOverlayLayout OverlayLayout => _overlayLayout;
		internal PerfMeterOverlayFontFamily OverlayFontFamily => _overlayFontFamily;
		internal PerfMeterOverlayPreset OverlayPreset => _overlayPreset;
		internal PerfMeterOverlayModule OverlayModules => _overlayModules;
		internal PerfMeterTargetFps TargetFps => _targetFps;
		internal bool EditorWarningLogsEnabled => _settings.EditorWarningsEnabled;
		internal PerfMeterCollectionMode CollectionMode => GetCollectionMode();
		internal bool IsSessionRecording => _sessionRecorder.IsRecording;
		internal static bool IsOverdrawMeasurementActive => _instance != null && _instance._overdrawController.IsMeasuring;
		internal static bool IsOverdrawHeatmapVisible => _instance != null && _instance._overdrawHeatmapVisible;
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
			runtime._cpuCoreSampler.Reset();
			runtime._cpuCoreSamplingActive = false;
			runtime._overdrawController.Reset();
			runtime._sessionRecorder.Stop(Time.realtimeSinceStartupAsDouble);
			runtime._alertEngine.Clear();
			runtime._overdrawHeatmapVisible = false;
			runtime._status = CreateStoppedStatus();
			runtime._latestMetrics = PerfMeterMetricsSnapshot.Stopped;
			runtime._latestCustomMetrics = System.Array.Empty<PerfMeterCustomMetricSnapshot>();
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
				ApplyAlertSettings();
				SetRunningPlaceholders();
				EnsureOverlayState();
			}
		}

		private void Update()
		{
			if (TrySkipCollectionForFocusState(out string focusWarning))
			{
				_lastCollectorWarning = focusWarning;
				RefreshRunningStatus(Time.frameCount, PerfMeterFrameTimingAvailability.NotCollected, focusWarning);
				return;
			}

			int frame = Time.frameCount;
			double frameBudgetMs = GetFrameBudgetMs(_targetFps);
			PerfMeterMetricsSnapshot collectedMetrics = _collector.Collect(frame, frameBudgetMs, out PerfMeterFrameTimingAvailability frameTimingAvailability, out string warning, out bool frameTimingSampleIgnored);
			if (frameTimingSampleIgnored)
			{
				_lastCollectorWarning = warning;
				RefreshRunningStatus(frame, frameTimingAvailability, warning);
				return;
			}

			_latestMetrics = collectedMetrics;
			_frameStatsSampler.AddSample(_latestMetrics.CpuFrameTimeMs, _latestMetrics.GpuFrameTimeAvailable);
			_latestMetrics = WithRuntimeStats(_latestMetrics, _frameStatsSampler.GetSnapshot());
			UpdateCpuCoreSampler(Time.unscaledTime);
			_latestCustomMetrics = PerfMeterCustomMetricRegistry.Collect();
			_sessionRecorder.Update(_latestMetrics, frame, Time.realtimeSinceStartupAsDouble, _latestCustomMetrics);
			_alertEngine.Evaluate(_latestMetrics, Time.realtimeSinceStartupAsDouble);
			_lastCollectorWarning = warning;
			RefreshRunningStatus(frame, frameTimingAvailability, warning);
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

		internal void ClearAlerts()
		{
			_alertEngine.Clear();
			RefreshStatusOverlayState();
		}

		internal void ResetStats()
		{
			_frameStatsSampler.Reset();
			_sessionRecorder.ResetStats(Time.frameCount, Time.realtimeSinceStartupAsDouble, _latestMetrics, _applicationFocused, _applicationPaused);
			_alertEngine.Clear();
			_latestMetrics = WithRuntimeStats(_latestMetrics, _frameStatsSampler.GetSnapshot());
			RefreshStatusOverlayState();
		}

		internal void SetCollectionMode(PerfMeterCollectionMode mode)
		{
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
			PerfMeterSettingsSnapshot settings = _settings;
			PerfMeterSessionOptions normalizedOptions = options.MaxSamples > 0 ? options : PerfMeterSessionOptions.FromSettings(settings);
			_sessionRecorder.Start(
				normalizedOptions,
				PerfMeterDeviceInfoProvider.CreateSnapshot(),
				PerfMeterCameraSnapshotProvider.CreateSnapshot(PerfMeterCameraSource.Auto, null),
				settings,
				Time.frameCount,
				Time.realtimeSinceStartupAsDouble,
				_latestMetrics,
				_applicationFocused,
				_applicationPaused);
			RefreshStatusOverlayState();
		}

		internal void StopSession()
		{
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
			int normalizedFrameCount = frameCount <= 0 ? _overdrawDefaultFrameCount : frameCount;
			_overdrawController.RequestMeasurement(Mathf.Clamp(normalizedFrameCount, 1, _overdrawMaxFrameCount));
			_latestMetrics = WithOverdrawState(_latestMetrics);
			RefreshStatusOverlayState();
		}

		internal void CancelOverdrawMeasurement()
		{
			_overdrawController.CancelMeasurement();
			_latestMetrics = WithOverdrawState(_latestMetrics);
			RefreshStatusOverlayState();
		}

		internal void SetOverdrawHeatmapVisible(bool visible)
		{
			_overdrawHeatmapVisible = visible;
			RefreshStatusOverlayState();
		}

		internal static bool TryBeginOverdrawRenderGraphFrame(int unityFrame, int screenPixelCount, out GraphicsBuffer counterBuffer, out int measurementId)
		{
			counterBuffer = null;
			measurementId = -1;

			if (_instance == null)
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
			if (_instance == null)
			{
				return;
			}

			_instance._overdrawController.CompleteCounterReadback(measurementId, request);
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

		internal static void MarkOverdrawMeasurementUnsupported(string reason)
		{
			if (_instance == null)
			{
				return;
			}

			_instance._overdrawController.MarkUnsupported(reason);
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

			ResetCpuCoreSamplerIfInactive();
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
			_overlayPreset = NormalizeOverlayPreset(preset);
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
			PerfMeterOverlayModule normalizedModules = NormalizeOverlayModules(modules, _overlayPreset);
			if (_overlayPreset != PerfMeterOverlayPreset.Custom && normalizedModules != PerfMeterSettingsStore.GetPresetModules(_overlayPreset))
			{
				_overlayPreset = PerfMeterOverlayPreset.Custom;
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
			PerfMeterOverlayModule normalizedModule = module & PerfMeterOverlayModule.All;
			if (normalizedModule == PerfMeterOverlayModule.None)
			{
				return;
			}

			_overlayPreset = PerfMeterOverlayPreset.Custom;
			_overlayModules = visible ? _overlayModules | normalizedModule : _overlayModules & ~normalizedModule;
			SetOverlayModules(_overlayModules);
		}

		internal void SetTargetFps(PerfMeterTargetFps targetFps)
		{
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

		internal void SetOverlayTuning(PerfMeterSettingsSnapshot settings)
		{
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
			_overdrawDefaultFrameCount = settings.OverdrawDefaultFrameCount;
			_overdrawMaxFrameCount = settings.OverdrawMaxFrameCount;
			ApplyAlertSettings(settings);

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
			_settings = PerfMeterSettingsStore.WithEditorWarningsEnabled(_settings, enabled);
			ApplyAlertSettings(_settings);
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
				_cpuCoreSampler.Reset();
				_cpuCoreSamplingActive = false;
				_overdrawController.Reset();
				_alertEngine.Clear();
				_overdrawHeatmapVisible = false;
				_status = CreateStoppedStatus();
				_latestMetrics = PerfMeterMetricsSnapshot.Stopped;
			}
		}

		private void OnDestroy()
		{
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
				applicationFocused: _applicationFocused,
				applicationPaused: _applicationPaused,
				editorWarningsEnabled: _settings.EditorWarningsEnabled);

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
				false);
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
			bool editorWarningsEnabled = true)
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
				editorWarningsEnabled: editorWarningsEnabled);
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

		private bool TrySkipCollectionForFocusState(out string warning)
		{
			if (_applicationPaused || !_applicationFocused)
			{
				warning = FocusPausedWarning;
				return true;
			}

			if (_focusResumeIgnoreFrames > 0)
			{
				FrameTimingManager.CaptureFrameTimings();
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
				_settings.EditorWarningsEnabled);
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

			if (!IsRuntimeOverlaySupported)
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
				_status.LastError,
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
				_settings.EditorWarningsEnabled);
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
			ApplyAlertSettings(_settings);
		}

		private void ApplyAlertSettings(PerfMeterSettingsSnapshot settings)
		{
			_settings = settings;
			_overdrawDefaultFrameCount = settings.OverdrawDefaultFrameCount;
			_overdrawMaxFrameCount = settings.OverdrawMaxFrameCount;
			_alertEngine = new PerfMeterAlertEngine(PerfMeterAlertEngine.CreateDefaultRules(_targetFps, settings));
			_alertEngine.ApplySettings(settings, _targetFps);
		}

		private void RebuildAlertRules()
		{
			ApplyAlertSettings(_settings);
		}

		private PerfMeterCollectionMode GetCollectionMode()
		{
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
