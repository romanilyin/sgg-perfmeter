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
			PerfMeterOverlayModule overlayModules = PerfMeterOverlayModule.All)
		{
			State = state;
			Availability = availability;
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
		}

		public PerfMeterRuntimeState State { get; }
		public PerfMeterAvailability Availability { get; }
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
