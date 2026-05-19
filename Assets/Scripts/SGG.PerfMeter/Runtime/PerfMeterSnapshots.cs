using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	public enum PerfMeterRuntimeState
	{
		Stopped = 0,
		Starting = 1,
		Running = 2,
		Error = 3
	}

	public enum PerfMeterCollectionMode
	{
		Stopped = 0,
		Background = 1,
		Overlay = 2,
		OverdrawDiagnostic = 3
	}

	public enum PerfMeterAvailability
	{
		Unknown = 0,
		Available = 1,
		Unavailable = 2
	}

	public enum PerfMeterFrameTimingAvailability
	{
		Unknown = 0,
		NotCollected = 1,
		Available = 2,
		Unavailable = 3
	}

	public enum PerfMeterBottleneck
	{
		Unknown = 0,
		Balanced = 1,
		GpuBound = 2,
		CpuMainThreadBound = 3,
		CpuRenderThreadBound = 4,
		PresentLimited = 5
	}

	public enum PerfMeterOverdrawMeasurementState
	{
		Off = 0,
		Measuring = 1,
		Completed = 2,
		Canceled = 3,
		Error = 4,
		Unsupported = 5
	}

	public enum PerfMeterOverlayCorner
	{
		TopLeft = 0,
		TopRight = 1,
		BottomLeft = 2,
		BottomRight = 3
	}

	public enum PerfMeterOverlayMode
	{
		FpsOnly = 0,
		TextCompact = 1,
		Graphs = 2,
		Full = 3
	}

	public enum PerfMeterOverlayPreset
	{
		Custom = 0,
		Minimal = 1,
		Timing = 2,
		Rendering = 3,
		Memory = 4,
		Overdraw = 5,
		FullDiagnostics = 6,
		AgentDebug = 7
	}

	[System.Flags]
	public enum PerfMeterOverlayModule
	{
		None = 0,
		Fps = 1 << 0,
		Timing = 1 << 1,
		Graphs = 1 << 2,
		Rendering = 1 << 3,
		SrpBatcher = 1 << 4,
		Brg = 1 << 5,
		Uploads = 1 << 6,
		Memory = 1 << 7,
		Gc = 1 << 8,
		GpuMemory = 1 << 9,
		Overdraw = 1 << 10,
		Heatmap = 1 << 11,
		Warnings = 1 << 12,
		All = Fps | Timing | Graphs | Rendering | SrpBatcher | Brg | Uploads | Memory | Gc | GpuMemory | Overdraw | Heatmap | Warnings
	}

	public enum PerfMeterTargetFps
	{
		Fps15 = 15,
		Fps30 = 30,
		Fps60 = 60,
		Fps90 = 90,
		Fps120 = 120,
		Fps144 = 144,
		Fps240 = 240
	}

	public enum PerfMeterMetric
	{
		CpuFrameTimeMs = 0,
		CpuMainThreadFrameTimeMs = 1,
		CpuRenderThreadFrameTimeMs = 2,
		GpuFrameTimeMs = 3,
		GpuFrameTimeAvailable = 4,
		AverageFps = 5,
		OnePercentLowFps = 6,
		OverdrawRatio = 7,
		SystemUsedMemoryBytes = 8,
		GcReservedMemoryBytes = 9,
		DrawCalls = 10,
		SetPassCalls = 11
	}

	public enum PerfMeterComparison
	{
		GreaterThan = 0,
		GreaterThanOrEqual = 1,
		LessThan = 2,
		LessThanOrEqual = 3,
		Equal = 4,
		NotEqual = 5
	}

	public interface IPerfMeterCustomMetricProvider
	{
		string Id { get; }
		bool TryCollect(out PerfMeterCustomMetricSnapshot metric);
	}

	public readonly struct PerfMeterCustomMetricSnapshot
	{
		public PerfMeterCustomMetricSnapshot(string id, string name, string category, string unit, double value, bool available = true, string warning = "")
		{
			Id = id ?? string.Empty;
			Name = string.IsNullOrEmpty(name) ? Id : name;
			Category = category ?? string.Empty;
			Unit = unit ?? string.Empty;
			Value = value;
			Available = available;
			Warning = warning ?? string.Empty;
		}

		public string Id { get; }
		public string Name { get; }
		public string Category { get; }
		public string Unit { get; }
		public double Value { get; }
		public bool Available { get; }
		public string Warning { get; }
	}

	[System.Flags]
	public enum PerfMeterAlertAction
	{
		None = 0,
		StructuredLog = 1 << 0,
		Callback = 1 << 1,
		EditorWarning = 1 << 2,
		Default = StructuredLog | Callback | EditorWarning
	}

	public readonly struct PerfMeterRule
	{
		public PerfMeterRule(string id, PerfMeterMetric metric, PerfMeterComparison comparison, double threshold, int consecutiveFrames = 1, float cooldownSeconds = 0f, PerfMeterAlertAction actions = PerfMeterAlertAction.Default)
		{
			Id = string.IsNullOrEmpty(id) ? metric.ToString() : id;
			Metric = metric;
			Comparison = comparison;
			Threshold = threshold;
			ConsecutiveFrames = Mathf.Max(1, consecutiveFrames);
			CooldownSeconds = Mathf.Max(0f, cooldownSeconds);
			Actions = actions;
		}

		public string Id { get; }
		public PerfMeterMetric Metric { get; }
		public PerfMeterComparison Comparison { get; }
		public double Threshold { get; }
		public int ConsecutiveFrames { get; }
		public float CooldownSeconds { get; }
		public PerfMeterAlertAction Actions { get; }
	}

	public readonly struct PerfMeterAlertSnapshot
	{
		public PerfMeterAlertSnapshot(string ruleId, PerfMeterMetric metric, PerfMeterComparison comparison, double threshold, double value, int collectionFrame, double timeSeconds, int consecutiveFrames, bool active, string message)
		{
			RuleId = ruleId ?? string.Empty;
			Metric = metric;
			Comparison = comparison;
			Threshold = threshold;
			Value = value;
			CollectionFrame = collectionFrame;
			TimeSeconds = timeSeconds;
			ConsecutiveFrames = Mathf.Max(0, consecutiveFrames);
			Active = active;
			Message = message ?? string.Empty;
		}

		public string RuleId { get; }
		public PerfMeterMetric Metric { get; }
		public PerfMeterComparison Comparison { get; }
		public double Threshold { get; }
		public double Value { get; }
		public int CollectionFrame { get; }
		public double TimeSeconds { get; }
		public int ConsecutiveFrames { get; }
		public bool Active { get; }
		public string Message { get; }
	}

	public enum PerfMeterSessionState
	{
		Idle = 0,
		Recording = 1,
		Stopped = 2
	}

	[System.Flags]
	public enum PerfMeterCounterAvailability
	{
		None = 0,
		DrawCalls = 1 << 0,
		SetPassCalls = 1 << 1,
		Batches = 1 << 2,
		Vertices = 1 << 3,
		BrgDrawCalls = 1 << 4,
		BrgInstances = 1 << 5,
		IndexBufferUploadInFrameBytes = 1 << 6,
		SystemUsedMemory = 1 << 7,
		GcReservedMemory = 1 << 8,
		SrpBatcherInstances = 1 << 9,
		GpuMemory = 1 << 10
	}

	public readonly struct PerfMeterStatusSnapshot
	{
		public PerfMeterStatusSnapshot(
			PerfMeterRuntimeState state,
			PerfMeterAvailability availability,
			PerfMeterCollectionMode collectionMode,
			PerfMeterFrameTimingAvailability frameTimingAvailability,
			GraphicsDeviceType graphicsDeviceType,
			string graphicsDeviceName,
			string warning,
			int collectionFrame,
			string lastError,
			PerfMeterBottleneck bottleneck = PerfMeterBottleneck.Unknown,
			PerfMeterCounterAvailability availableCounters = PerfMeterCounterAvailability.None,
			PerfMeterCounterAvailability unavailableCounters = PerfMeterCounterAvailability.None,
			bool overlayVisible = false,
			PerfMeterOverdrawMeasurementState overdrawState = PerfMeterOverdrawMeasurementState.Off,
			float overdrawProgress = 0f,
			double overdrawRatio = 0d,
			bool overdrawHeatmapVisible = false,
			PerfMeterOverlayCorner overlayCorner = PerfMeterOverlayCorner.TopRight,
			PerfMeterOverlayMode overlayMode = PerfMeterOverlayMode.Full,
			PerfMeterTargetFps targetFps = PerfMeterTargetFps.Fps60,
			PerfMeterOverlayPreset overlayPreset = PerfMeterOverlayPreset.FullDiagnostics,
			PerfMeterOverlayModule overlayModules = PerfMeterOverlayModule.All,
			PerfMeterSessionState sessionState = PerfMeterSessionState.Idle,
			bool sessionRecording = false,
			int sessionSampleCount = 0,
			int sessionDroppedSampleCount = 0,
			int activeAlertCount = 0,
			int firedAlertCount = 0,
			string latestAlertRuleId = "",
			string latestAlertMessage = "")
		{
			State = state;
			Availability = availability;
			CollectionMode = state == PerfMeterRuntimeState.Stopped ? PerfMeterCollectionMode.Stopped : collectionMode;
			FrameTimingAvailability = frameTimingAvailability;
			GraphicsDeviceType = graphicsDeviceType;
			GraphicsDeviceName = graphicsDeviceName ?? string.Empty;
			Warning = warning ?? string.Empty;
			CollectionFrame = collectionFrame;
			LastError = lastError ?? string.Empty;
			Bottleneck = bottleneck;
			AvailableCounters = availableCounters;
			UnavailableCounters = unavailableCounters;
			OverlayVisible = overlayVisible;
			OverlayCorner = overlayCorner;
			OverlayMode = overlayMode;
			TargetFps = targetFps;
			OverlayPreset = overlayPreset;
			OverlayModules = overlayModules == PerfMeterOverlayModule.None ? PerfMeterOverlayModule.All : overlayModules;
			OverdrawState = overdrawState;
			OverdrawProgress = Mathf.Clamp01(overdrawProgress);
			OverdrawRatio = overdrawRatio;
			OverdrawHeatmapVisible = overdrawHeatmapVisible;
			SessionState = sessionState;
			IsSessionRecording = sessionRecording;
			SessionSampleCount = Mathf.Max(0, sessionSampleCount);
			SessionDroppedSampleCount = Mathf.Max(0, sessionDroppedSampleCount);
			ActiveAlertCount = Mathf.Max(0, activeAlertCount);
			FiredAlertCount = Mathf.Max(0, firedAlertCount);
			LatestAlertRuleId = latestAlertRuleId ?? string.Empty;
			LatestAlertMessage = latestAlertMessage ?? string.Empty;
		}

		public PerfMeterRuntimeState State { get; }
		public PerfMeterAvailability Availability { get; }
		public PerfMeterCollectionMode CollectionMode { get; }
		public PerfMeterFrameTimingAvailability FrameTimingAvailability { get; }
		public GraphicsDeviceType GraphicsDeviceType { get; }
		public string GraphicsDeviceName { get; }
		public string Warning { get; }
		public int CollectionFrame { get; }
		public string LastError { get; }
		public PerfMeterBottleneck Bottleneck { get; }
		public PerfMeterCounterAvailability AvailableCounters { get; }
		public PerfMeterCounterAvailability UnavailableCounters { get; }
		public bool OverlayVisible { get; }
		public PerfMeterOverlayCorner OverlayCorner { get; }
		public PerfMeterOverlayMode OverlayMode { get; }
		public PerfMeterTargetFps TargetFps { get; }
		public PerfMeterOverlayPreset OverlayPreset { get; }
		public PerfMeterOverlayModule OverlayModules { get; }
		public PerfMeterOverdrawMeasurementState OverdrawState { get; }
		public float OverdrawProgress { get; }
		public double OverdrawRatio { get; }
		public bool OverdrawHeatmapVisible { get; }
		public PerfMeterSessionState SessionState { get; }
		public bool IsSessionRecording { get; }
		public int SessionSampleCount { get; }
		public int SessionDroppedSampleCount { get; }
		public int ActiveAlertCount { get; }
		public int FiredAlertCount { get; }
		public string LatestAlertRuleId { get; }
		public string LatestAlertMessage { get; }
	}

	public readonly struct PerfMeterSessionOptions
	{
		public const float DefaultWarmupSeconds = 0f;
		public const float DefaultSampleIntervalSeconds = 0.25f;
		public const int DefaultMaxSamples = 4096;
		public const int DefaultSceneLoadIgnoreFrames = 0;
		public const float DefaultSceneLoadIgnoreSeconds = 0f;

		public PerfMeterSessionOptions(int warmupFrames, float sampleIntervalSeconds, int maxSamples)
			: this(warmupFrames, DefaultWarmupSeconds, sampleIntervalSeconds, maxSamples, false, DefaultSceneLoadIgnoreFrames, DefaultSceneLoadIgnoreSeconds)
		{
		}

		public PerfMeterSessionOptions(int warmupFrames, float warmupSeconds, float sampleIntervalSeconds, int maxSamples, bool resetOnSceneLoad, int sceneLoadIgnoreFrames, float sceneLoadIgnoreSeconds)
		{
			WarmupFrames = Mathf.Max(0, warmupFrames);
			WarmupSeconds = Mathf.Max(0f, warmupSeconds);
			SampleIntervalSeconds = sampleIntervalSeconds > 0f ? sampleIntervalSeconds : DefaultSampleIntervalSeconds;
			MaxSamples = Mathf.Max(1, maxSamples);
			ResetOnSceneLoad = resetOnSceneLoad;
			SceneLoadIgnoreFrames = Mathf.Max(0, sceneLoadIgnoreFrames);
			SceneLoadIgnoreSeconds = Mathf.Max(0f, sceneLoadIgnoreSeconds);
		}

		public static PerfMeterSessionOptions Default => new PerfMeterSessionOptions(0, DefaultWarmupSeconds, DefaultSampleIntervalSeconds, DefaultMaxSamples, false, DefaultSceneLoadIgnoreFrames, DefaultSceneLoadIgnoreSeconds);

		public static PerfMeterSessionOptions FromSettings(PerfMeterSettingsSnapshot settings)
		{
			return new PerfMeterSessionOptions(
				settings.SessionWarmupFrames,
				settings.SessionWarmupSeconds,
				settings.SessionSampleIntervalSeconds,
				settings.SessionMaxSamples,
				settings.SessionResetOnSceneLoad,
				settings.SessionSceneLoadIgnoreFrames,
				settings.SessionSceneLoadIgnoreSeconds);
		}

		public int WarmupFrames { get; }
		public float WarmupSeconds { get; }
		public float SampleIntervalSeconds { get; }
		public int MaxSamples { get; }
		public bool ResetOnSceneLoad { get; }
		public int SceneLoadIgnoreFrames { get; }
		public float SceneLoadIgnoreSeconds { get; }
	}

	public readonly struct PerfMeterSessionSampleSnapshot
	{
		public PerfMeterSessionSampleSnapshot(int collectionFrame, double collectionTimeSeconds, string sceneName, PerfMeterMetricsSnapshot metrics)
			: this(collectionFrame, collectionTimeSeconds, sceneName, metrics, System.Array.Empty<PerfMeterCustomMetricSnapshot>())
		{
		}

		public PerfMeterSessionSampleSnapshot(int collectionFrame, double collectionTimeSeconds, string sceneName, PerfMeterMetricsSnapshot metrics, PerfMeterCustomMetricSnapshot[] customMetrics)
		{
			CollectionFrame = collectionFrame;
			CollectionTimeSeconds = collectionTimeSeconds;
			SceneName = sceneName ?? string.Empty;
			Metrics = metrics;
			CustomMetrics = customMetrics ?? System.Array.Empty<PerfMeterCustomMetricSnapshot>();
		}

		public int CollectionFrame { get; }
		public double CollectionTimeSeconds { get; }
		public string SceneName { get; }
		public PerfMeterMetricsSnapshot Metrics { get; }
		public PerfMeterCustomMetricSnapshot[] CustomMetrics { get; }
	}

	public readonly struct PerfMeterSessionWorstFrameSnapshot
	{
		public PerfMeterSessionWorstFrameSnapshot(int collectionFrame, double collectionTimeSeconds, string sceneName, double frameTimeMs, double fps, PerfMeterBottleneck bottleneck)
		{
			CollectionFrame = collectionFrame;
			CollectionTimeSeconds = collectionTimeSeconds;
			SceneName = sceneName ?? string.Empty;
			FrameTimeMs = frameTimeMs;
			Fps = fps;
			Bottleneck = bottleneck;
		}

		public static PerfMeterSessionWorstFrameSnapshot Empty => new PerfMeterSessionWorstFrameSnapshot(-1, 0d, string.Empty, 0d, 0d, PerfMeterBottleneck.Unknown);

		public bool IsAvailable => CollectionFrame >= 0;
		public int CollectionFrame { get; }
		public double CollectionTimeSeconds { get; }
		public string SceneName { get; }
		public double FrameTimeMs { get; }
		public double Fps { get; }
		public PerfMeterBottleneck Bottleneck { get; }
	}

	public readonly struct PerfMeterSessionScopeSummarySnapshot
	{
		public PerfMeterSessionScopeSummarySnapshot(
			string sceneName,
			int sampleCount,
			int firstFrame,
			int lastFrame,
			double startTimeSeconds,
			double lastSampleTimeSeconds,
			double durationSeconds,
			double averageFrameTimeMs,
			double minFrameTimeMs,
			double maxFrameTimeMs,
			double averageFps,
			double minFps,
			double maxFps,
			int gpuBoundSampleCount,
			int cpuMainThreadBoundSampleCount,
			int cpuRenderThreadBoundSampleCount,
			int presentLimitedSampleCount,
			int frameSpikeCount,
			int severeFrameSpikeCount,
			PerfMeterSessionWorstFrameSnapshot worstFrame)
		{
			SceneName = sceneName ?? string.Empty;
			SampleCount = Mathf.Max(0, sampleCount);
			FirstFrame = firstFrame;
			LastFrame = lastFrame;
			StartTimeSeconds = startTimeSeconds;
			LastSampleTimeSeconds = lastSampleTimeSeconds;
			DurationSeconds = durationSeconds;
			AverageFrameTimeMs = averageFrameTimeMs;
			MinFrameTimeMs = minFrameTimeMs;
			MaxFrameTimeMs = maxFrameTimeMs;
			AverageFps = averageFps;
			MinFps = minFps;
			MaxFps = maxFps;
			GpuBoundSampleCount = Mathf.Max(0, gpuBoundSampleCount);
			CpuMainThreadBoundSampleCount = Mathf.Max(0, cpuMainThreadBoundSampleCount);
			CpuRenderThreadBoundSampleCount = Mathf.Max(0, cpuRenderThreadBoundSampleCount);
			PresentLimitedSampleCount = Mathf.Max(0, presentLimitedSampleCount);
			FrameSpikeCount = Mathf.Max(0, frameSpikeCount);
			SevereFrameSpikeCount = Mathf.Max(0, severeFrameSpikeCount);
			WorstFrame = worstFrame;
		}

		public static PerfMeterSessionScopeSummarySnapshot Empty => new PerfMeterSessionScopeSummarySnapshot(
			string.Empty,
			0,
			-1,
			-1,
			0d,
			0d,
			0d,
			0d,
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
			PerfMeterSessionWorstFrameSnapshot.Empty);

		public string SceneName { get; }
		public int SampleCount { get; }
		public int FirstFrame { get; }
		public int LastFrame { get; }
		public double StartTimeSeconds { get; }
		public double LastSampleTimeSeconds { get; }
		public double DurationSeconds { get; }
		public double AverageFrameTimeMs { get; }
		public double MinFrameTimeMs { get; }
		public double MaxFrameTimeMs { get; }
		public double AverageFps { get; }
		public double MinFps { get; }
		public double MaxFps { get; }
		public int GpuBoundSampleCount { get; }
		public int CpuMainThreadBoundSampleCount { get; }
		public int CpuRenderThreadBoundSampleCount { get; }
		public int PresentLimitedSampleCount { get; }
		public int FrameSpikeCount { get; }
		public int SevereFrameSpikeCount { get; }
		public PerfMeterSessionWorstFrameSnapshot WorstFrame { get; }
	}

	public readonly struct PerfMeterSessionSummarySnapshot
	{
		public PerfMeterSessionSummarySnapshot(
			PerfMeterSessionState state,
			PerfMeterSessionOptions options,
			int sampleCount,
			int droppedSampleCount,
			int firstFrame,
			int lastFrame,
			double startTimeSeconds,
			double stopTimeSeconds,
			double durationSeconds,
			double averageFrameTimeMs,
			double minFrameTimeMs,
			double maxFrameTimeMs,
			double averageFps,
			double minFps,
			double maxFps,
			int gpuBoundSampleCount,
			int cpuMainThreadBoundSampleCount,
			int cpuRenderThreadBoundSampleCount,
			int presentLimitedSampleCount,
			int frameSpikeCount,
			int severeFrameSpikeCount,
			string warning,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterSettingsSnapshot settings,
			string startSceneName,
			string lastSceneName,
			PerfMeterSessionScopeSummarySnapshot wholeRun,
			PerfMeterSessionScopeSummarySnapshot currentScene)
		{
			State = state;
			Options = options;
			SampleCount = Mathf.Max(0, sampleCount);
			DroppedSampleCount = Mathf.Max(0, droppedSampleCount);
			FirstFrame = firstFrame;
			LastFrame = lastFrame;
			StartTimeSeconds = startTimeSeconds;
			StopTimeSeconds = stopTimeSeconds;
			DurationSeconds = durationSeconds;
			AverageFrameTimeMs = averageFrameTimeMs;
			MinFrameTimeMs = minFrameTimeMs;
			MaxFrameTimeMs = maxFrameTimeMs;
			AverageFps = averageFps;
			MinFps = minFps;
			MaxFps = maxFps;
			GpuBoundSampleCount = Mathf.Max(0, gpuBoundSampleCount);
			CpuMainThreadBoundSampleCount = Mathf.Max(0, cpuMainThreadBoundSampleCount);
			CpuRenderThreadBoundSampleCount = Mathf.Max(0, cpuRenderThreadBoundSampleCount);
			PresentLimitedSampleCount = Mathf.Max(0, presentLimitedSampleCount);
			FrameSpikeCount = Mathf.Max(0, frameSpikeCount);
			SevereFrameSpikeCount = Mathf.Max(0, severeFrameSpikeCount);
			Warning = warning ?? string.Empty;
			Device = device;
			Camera = camera;
			Settings = settings;
			StartSceneName = startSceneName ?? string.Empty;
			LastSceneName = lastSceneName ?? string.Empty;
			WholeRun = wholeRun;
			CurrentScene = currentScene;
		}

		public static PerfMeterSessionSummarySnapshot Empty => new PerfMeterSessionSummarySnapshot(
			PerfMeterSessionState.Idle,
			PerfMeterSessionOptions.Default,
			0,
			0,
			-1,
			-1,
			0d,
			0d,
			0d,
			0d,
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
			string.Empty,
			default,
			default,
			PerfMeterSettingsStore.Defaults,
			string.Empty,
			string.Empty,
			PerfMeterSessionScopeSummarySnapshot.Empty,
			PerfMeterSessionScopeSummarySnapshot.Empty);

		public PerfMeterSessionState State { get; }
		public PerfMeterSessionOptions Options { get; }
		public int SampleCount { get; }
		public int DroppedSampleCount { get; }
		public int FirstFrame { get; }
		public int LastFrame { get; }
		public double StartTimeSeconds { get; }
		public double StopTimeSeconds { get; }
		public double DurationSeconds { get; }
		public double AverageFrameTimeMs { get; }
		public double MinFrameTimeMs { get; }
		public double MaxFrameTimeMs { get; }
		public double AverageFps { get; }
		public double MinFps { get; }
		public double MaxFps { get; }
		public int GpuBoundSampleCount { get; }
		public int CpuMainThreadBoundSampleCount { get; }
		public int CpuRenderThreadBoundSampleCount { get; }
		public int PresentLimitedSampleCount { get; }
		public int FrameSpikeCount { get; }
		public int SevereFrameSpikeCount { get; }
		public string Warning { get; }
		public PerfMeterDeviceSnapshot Device { get; }
		public PerfMeterCameraSnapshot Camera { get; }
		public PerfMeterSettingsSnapshot Settings { get; }
		public string StartSceneName { get; }
		public string LastSceneName { get; }
		public PerfMeterSessionScopeSummarySnapshot WholeRun { get; }
		public PerfMeterSessionScopeSummarySnapshot CurrentScene { get; }
		public PerfMeterSessionWorstFrameSnapshot WorstFrame => WholeRun.WorstFrame;
		public PerfMeterSessionWorstFrameSnapshot CurrentSceneWorstFrame => CurrentScene.WorstFrame;
	}

	public readonly struct PerfMeterMetricsSnapshot
	{
		public static PerfMeterMetricsSnapshot Stopped => new PerfMeterMetricsSnapshot(
			PerfMeterRuntimeState.Stopped,
			PerfMeterAvailability.Available,
			-1,
			PerfMeterBottleneck.Unknown,
			1000d / 60d,
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
			PerfMeterOverdrawMeasurementState.Off,
			0f,
			0,
			0,
			0,
			0d,
			0d,
			0d,
			0,
			0);

		public PerfMeterMetricsSnapshot(
			PerfMeterRuntimeState state,
			PerfMeterAvailability availability,
			int collectionFrame,
			PerfMeterBottleneck bottleneck,
			double frameBudgetMs,
			bool gpuFrameTimeAvailable,
			double cpuFrameTimeMs,
			double cpuMainThreadFrameTimeMs,
			double cpuRenderThreadFrameTimeMs,
			double cpuMainThreadPresentWaitTimeMs,
			double gpuFrameTimeMs,
			int drawCalls,
			int setPassCalls,
			int batches,
			int vertices,
			int brgDrawCalls,
			int brgInstances,
			long indexBufferUploadInFrameBytes,
			long systemUsedMemoryBytes,
			long gcReservedMemoryBytes,
			long gpuMemoryBytes,
			double overdrawRatio,
			PerfMeterOverdrawMeasurementState overdrawState = PerfMeterOverdrawMeasurementState.Off,
			float overdrawProgress = 0f,
			int srpBatcherInstances = 0,
			int frameSampleCount = 0,
			int gpuValidSampleCount = 0,
			double averageFps = 0d,
			double onePercentLowFps = 0d,
			double pointOnePercentLowFps = 0d,
			int frameSpikeCount = 0,
			int severeFrameSpikeCount = 0)
		{
			State = state;
			Availability = availability;
			CollectionFrame = collectionFrame;
			Bottleneck = bottleneck;
			FrameBudgetMs = frameBudgetMs;
			GpuFrameTimeAvailable = gpuFrameTimeAvailable;
			CpuFrameTimeMs = cpuFrameTimeMs;
			CpuMainThreadFrameTimeMs = cpuMainThreadFrameTimeMs;
			CpuRenderThreadFrameTimeMs = cpuRenderThreadFrameTimeMs;
			CpuMainThreadPresentWaitTimeMs = cpuMainThreadPresentWaitTimeMs;
			GpuFrameTimeMs = gpuFrameTimeMs;
			DrawCalls = drawCalls;
			SetPassCalls = setPassCalls;
			Batches = batches;
			Vertices = vertices;
			BrgDrawCalls = brgDrawCalls;
			BrgInstances = brgInstances;
			IndexBufferUploadInFrameBytes = indexBufferUploadInFrameBytes;
			SystemUsedMemoryBytes = systemUsedMemoryBytes;
			GcReservedMemoryBytes = gcReservedMemoryBytes;
			GpuMemoryBytes = gpuMemoryBytes;
			OverdrawRatio = overdrawRatio;
			OverdrawState = overdrawState;
			OverdrawProgress = Mathf.Clamp01(overdrawProgress);
			SrpBatcherInstances = srpBatcherInstances;
			FrameSampleCount = frameSampleCount;
			GpuValidSampleCount = gpuValidSampleCount;
			AverageFps = averageFps;
			OnePercentLowFps = onePercentLowFps;
			PointOnePercentLowFps = pointOnePercentLowFps;
			FrameSpikeCount = frameSpikeCount;
			SevereFrameSpikeCount = severeFrameSpikeCount;
		}

		public PerfMeterRuntimeState State { get; }
		public PerfMeterAvailability Availability { get; }
		public int CollectionFrame { get; }
		public PerfMeterBottleneck Bottleneck { get; }
		public double FrameBudgetMs { get; }
		public bool GpuFrameTimeAvailable { get; }
		public double CpuFrameTimeMs { get; }
		public double CpuMainThreadFrameTimeMs { get; }
		public double CpuRenderThreadFrameTimeMs { get; }
		public double CpuMainThreadPresentWaitTimeMs { get; }
		public double GpuFrameTimeMs { get; }
		public int DrawCalls { get; }
		public int SetPassCalls { get; }
		public int Batches { get; }
		public int Vertices { get; }
		public int SrpBatcherInstances { get; }
		public int BrgDrawCalls { get; }
		public int BrgInstances { get; }
		public long IndexBufferUploadInFrameBytes { get; }
		public long SystemUsedMemoryBytes { get; }
		public long GcReservedMemoryBytes { get; }
		public long GpuMemoryBytes { get; }
		public double OverdrawRatio { get; }
		public PerfMeterOverdrawMeasurementState OverdrawState { get; }
		public float OverdrawProgress { get; }
		public int FrameSampleCount { get; }
		public int GpuValidSampleCount { get; }
		public double AverageFps { get; }
		public double OnePercentLowFps { get; }
		public double PointOnePercentLowFps { get; }
		public int FrameSpikeCount { get; }
		public int SevereFrameSpikeCount { get; }
	}

	public readonly struct PerfMeterDisplaySnapshot
	{
		public PerfMeterDisplaySnapshot(
			int index,
			string name,
			int width,
			int height,
			int workAreaX,
			int workAreaY,
			int workAreaWidth,
			int workAreaHeight,
			uint refreshRateNumerator,
			uint refreshRateDenominator,
			double refreshRateHz,
			bool isMainWindowDisplay,
			bool isFallback)
		{
			Index = index;
			Name = name ?? string.Empty;
			Width = width;
			Height = height;
			WorkAreaX = workAreaX;
			WorkAreaY = workAreaY;
			WorkAreaWidth = workAreaWidth;
			WorkAreaHeight = workAreaHeight;
			RefreshRateNumerator = refreshRateNumerator;
			RefreshRateDenominator = refreshRateDenominator;
			RefreshRateHz = refreshRateHz;
			IsMainWindowDisplay = isMainWindowDisplay;
			IsFallback = isFallback;
		}

		public int Index { get; }
		public string Name { get; }
		public int Width { get; }
		public int Height { get; }
		public int WorkAreaX { get; }
		public int WorkAreaY { get; }
		public int WorkAreaWidth { get; }
		public int WorkAreaHeight { get; }
		public uint RefreshRateNumerator { get; }
		public uint RefreshRateDenominator { get; }
		public double RefreshRateHz { get; }
		public bool IsMainWindowDisplay { get; }
		public bool IsFallback { get; }
	}

	public readonly struct PerfMeterDeviceSnapshot
	{
		public PerfMeterDeviceSnapshot(
			string unityVersion,
			RuntimePlatform applicationPlatform,
			bool isEditor,
			string operatingSystem,
			string deviceModel,
			DeviceType deviceType,
			string processorType,
			int processorCount,
			int processorFrequencyMhz,
			int systemMemorySizeMb,
			GraphicsDeviceType graphicsDeviceType,
			string graphicsDeviceName,
			string graphicsDeviceVendor,
			string graphicsDeviceVersion,
			int graphicsMemorySizeMb,
			int graphicsShaderLevel,
			bool graphicsMultiThreaded,
			int maxTextureSize,
			bool supportsComputeShaders,
			bool supportsAsyncGpuReadback,
			bool supportsInstancing,
			bool supportsGraphicsFence,
			int screenWidth,
			int screenHeight,
			int currentResolutionWidth,
			int currentResolutionHeight,
			uint currentRefreshRateNumerator,
			uint currentRefreshRateDenominator,
			double currentRefreshRateHz,
			float dpi,
			bool fullScreen,
			FullScreenMode fullScreenMode,
			bool mainWindowPositionAvailable,
			int mainWindowPositionX,
			int mainWindowPositionY,
			bool displayLayoutAvailable,
			string displayLayoutWarning,
			PerfMeterDisplaySnapshot[] displays)
		{
			UnityVersion = unityVersion ?? string.Empty;
			ApplicationPlatform = applicationPlatform;
			IsEditor = isEditor;
			OperatingSystem = operatingSystem ?? string.Empty;
			DeviceModel = deviceModel ?? string.Empty;
			DeviceType = deviceType;
			ProcessorType = processorType ?? string.Empty;
			ProcessorCount = processorCount;
			ProcessorFrequencyMhz = processorFrequencyMhz;
			SystemMemorySizeMb = systemMemorySizeMb;
			GraphicsDeviceType = graphicsDeviceType;
			GraphicsDeviceName = graphicsDeviceName ?? string.Empty;
			GraphicsDeviceVendor = graphicsDeviceVendor ?? string.Empty;
			GraphicsDeviceVersion = graphicsDeviceVersion ?? string.Empty;
			GraphicsMemorySizeMb = graphicsMemorySizeMb;
			GraphicsShaderLevel = graphicsShaderLevel;
			GraphicsMultiThreaded = graphicsMultiThreaded;
			MaxTextureSize = maxTextureSize;
			SupportsComputeShaders = supportsComputeShaders;
			SupportsAsyncGpuReadback = supportsAsyncGpuReadback;
			SupportsInstancing = supportsInstancing;
			SupportsGraphicsFence = supportsGraphicsFence;
			ScreenWidth = screenWidth;
			ScreenHeight = screenHeight;
			CurrentResolutionWidth = currentResolutionWidth;
			CurrentResolutionHeight = currentResolutionHeight;
			CurrentRefreshRateNumerator = currentRefreshRateNumerator;
			CurrentRefreshRateDenominator = currentRefreshRateDenominator;
			CurrentRefreshRateHz = currentRefreshRateHz;
			Dpi = dpi;
			FullScreen = fullScreen;
			FullScreenMode = fullScreenMode;
			MainWindowPositionAvailable = mainWindowPositionAvailable;
			MainWindowPositionX = mainWindowPositionX;
			MainWindowPositionY = mainWindowPositionY;
			DisplayLayoutAvailable = displayLayoutAvailable;
			DisplayLayoutWarning = displayLayoutWarning ?? string.Empty;
			Displays = displays ?? System.Array.Empty<PerfMeterDisplaySnapshot>();
		}

		public string UnityVersion { get; }
		public RuntimePlatform ApplicationPlatform { get; }
		public bool IsEditor { get; }
		public string OperatingSystem { get; }
		public string DeviceModel { get; }
		public DeviceType DeviceType { get; }
		public string ProcessorType { get; }
		public int ProcessorCount { get; }
		public int ProcessorFrequencyMhz { get; }
		public int SystemMemorySizeMb { get; }
		public GraphicsDeviceType GraphicsDeviceType { get; }
		public string GraphicsDeviceName { get; }
		public string GraphicsDeviceVendor { get; }
		public string GraphicsDeviceVersion { get; }
		public int GraphicsMemorySizeMb { get; }
		public int GraphicsShaderLevel { get; }
		public bool GraphicsMultiThreaded { get; }
		public int MaxTextureSize { get; }
		public bool SupportsComputeShaders { get; }
		public bool SupportsAsyncGpuReadback { get; }
		public bool SupportsInstancing { get; }
		public bool SupportsGraphicsFence { get; }
		public int ScreenWidth { get; }
		public int ScreenHeight { get; }
		public int CurrentResolutionWidth { get; }
		public int CurrentResolutionHeight { get; }
		public uint CurrentRefreshRateNumerator { get; }
		public uint CurrentRefreshRateDenominator { get; }
		public double CurrentRefreshRateHz { get; }
		public float Dpi { get; }
		public bool FullScreen { get; }
		public FullScreenMode FullScreenMode { get; }
		public bool MainWindowPositionAvailable { get; }
		public int MainWindowPositionX { get; }
		public int MainWindowPositionY { get; }
		public bool DisplayLayoutAvailable { get; }
		public string DisplayLayoutWarning { get; }
		public PerfMeterDisplaySnapshot[] Displays { get; }
	}
}
