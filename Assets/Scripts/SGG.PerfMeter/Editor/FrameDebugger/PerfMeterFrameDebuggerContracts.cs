using System;

namespace SGG.PerfMeter.Editor.FrameDebugger
{
	public enum PerfMeterFrameDebuggerExportState { Idle, Capturing, Writing, Completed, Partial, Cancelled, Failed }

	public sealed class PerfMeterFrameDebuggerExportOptions
	{
		public const int MaxCapturedCharacters = 8 * 1024 * 1024;
		public const long MaxOutputBytes = 64L * 1024 * 1024;
		public const long MaxRetainedBytes = 256L * 1024 * 1024;
		public const int MaxRetainedExports = 8;
		public int MaxEvents = 2048;
		public int MaxFieldsPerEvent = 128;
		public int MaxPropertiesPerEvent = 64;
		public int MaxStringLength = 2048;
		public double EventTimeoutSeconds = 0.5d;
		public double TotalTimeoutSeconds = 60d;
		public bool IncludeProperties;

		internal PerfMeterFrameDebuggerExportOptions Freeze()
		{
			if (MaxEvents < 1 || MaxEvents > 8192 || MaxFieldsPerEvent < 16 || MaxFieldsPerEvent > 256 ||
				MaxPropertiesPerEvent < 0 || MaxPropertiesPerEvent > 128 || MaxStringLength < 64 || MaxStringLength > 4096 ||
				double.IsNaN(EventTimeoutSeconds) || double.IsInfinity(EventTimeoutSeconds) || EventTimeoutSeconds < 0.05d || EventTimeoutSeconds > 2d ||
				double.IsNaN(TotalTimeoutSeconds) || double.IsInfinity(TotalTimeoutSeconds) || TotalTimeoutSeconds < 1d || TotalTimeoutSeconds > 120d)
				throw new ArgumentOutOfRangeException(nameof(PerfMeterFrameDebuggerExportOptions));
			return (PerfMeterFrameDebuggerExportOptions)MemberwiseClone();
		}
	}

	public sealed class PerfMeterFrameDebuggerExportStatus
	{
		public PerfMeterFrameDebuggerExportState State { get; internal set; }
		public string ExportId { get; internal set; } = string.Empty;
		public int ProcessedEvents { get; internal set; }
		public int RequestedEvents { get; internal set; }
		public string Message { get; internal set; } = string.Empty;
		public string OutputDirectory { get; internal set; } = string.Empty;
		public bool IsBusy => State == PerfMeterFrameDebuggerExportState.Capturing || State == PerfMeterFrameDebuggerExportState.Writing;
		internal PerfMeterFrameDebuggerExportStatus Copy() => (PerfMeterFrameDebuggerExportStatus)MemberwiseClone();
	}

	internal readonly struct PerfMeterFrameDebuggerObservation
	{
		internal PerfMeterFrameDebuggerObservation(bool enabled, bool local, ulong windowId, int targetId, int count, int limit, int eventsHash)
		{
			Enabled = enabled; Local = local; WindowId = windowId; TargetId = targetId;
			EventCount = count; SelectedLimit = limit; EventsHash = eventsHash;
		}
		internal bool Enabled { get; }
		internal bool Local { get; }
		internal ulong WindowId { get; }
		internal int TargetId { get; }
		internal int EventCount { get; }
		internal int SelectedLimit { get; }
		internal int EventsHash { get; }
		internal bool SameCapture(PerfMeterFrameDebuggerObservation other) => Enabled && Local && other.Enabled && other.Local &&
			WindowId == other.WindowId && TargetId == other.TargetId && EventCount == other.EventCount && EventsHash == other.EventsHash;
	}

	internal interface IPerfMeterFrameDebuggerBackend
	{
		bool TryObserve(out PerfMeterFrameDebuggerObservation observation, out string reason);
		bool TrySelect(int limit, out string reason);
		bool TryRead(int index, PerfMeterFrameDebuggerExportOptions options, out PerfMeterFrameDebuggerEvent row, out string reason);
	}

	// Detached CLR values only. Value: null, string, bool, numeric primitive, string[] or double[]. Never a Unity object.
	internal sealed class PerfMeterFrameDebuggerField
	{
		public string Name = string.Empty;
		public string Kind = "string";
		public bool Available;
		public object Value;
		public string Reason = string.Empty;
		public bool Truncated;
	}

	internal sealed class PerfMeterFrameDebuggerEvent
	{
		public int Index;
		public int RawEventType;
		public string EventType = string.Empty;
		public string Name = string.Empty;
		public string Status = "available";
		public string Reason = string.Empty;
		public bool Truncated;
		public PerfMeterFrameDebuggerField[] Fields = Array.Empty<PerfMeterFrameDebuggerField>();
	}

	internal sealed class PerfMeterFrameDebuggerSnapshot
	{
		public string ExportId = string.Empty;
		public string StartedUtc = string.Empty;
		public string CompletedUtc = string.Empty;
		public string UnityVersion = string.Empty;
		public string RenderPipeline = string.Empty;
		public string GraphicsApi = string.Empty;
		public string SceneName = string.Empty;
		public string SessionId = string.Empty;
		public string DiagnosticIntervalEndedUtc = string.Empty;
		public string RestorationReason = string.Empty;
		public string Completeness = "partial";
		public string Warning = string.Empty;
		public int OriginalLimit;
		public int TotalEventCount;
		public int RequestedEventCount;
		public int ExportedEventCount;
		public int MissingEventCount;
		public int TruncatedEventCount;
		public bool SelectionRestored;
		public bool IncludeProperties;
		public PerfMeterFrameDebuggerEvent[] Events = Array.Empty<PerfMeterFrameDebuggerEvent>();
	}

	internal sealed class PerfMeterFrameDebuggerWriteResult
	{
		public string OutputDirectory = string.Empty;
		public string JsonSha256 = string.Empty;
		public string CsvSha256 = string.Empty;
		public long TotalBytes;
	}
}