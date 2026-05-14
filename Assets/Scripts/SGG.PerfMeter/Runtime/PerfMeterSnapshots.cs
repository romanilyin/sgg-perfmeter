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
			PerfMeterOverlayCorner overlayCorner = PerfMeterOverlayCorner.TopRight,
			PerfMeterOverlayMode overlayMode = PerfMeterOverlayMode.Full,
			PerfMeterTargetFps targetFps = PerfMeterTargetFps.Fps60)
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
			OverdrawState = overdrawState;
			OverdrawProgress = Mathf.Clamp01(overdrawProgress);
			OverdrawRatio = overdrawRatio;
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
		public PerfMeterOverdrawMeasurementState OverdrawState { get; }
		public float OverdrawProgress { get; }
		public double OverdrawRatio { get; }
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
}
