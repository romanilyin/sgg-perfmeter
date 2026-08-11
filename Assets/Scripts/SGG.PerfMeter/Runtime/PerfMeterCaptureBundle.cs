using System;

namespace SGG.PerfMeter
{
	public enum PerfMeterCaptureBundleState
	{
		None = 0,
		Recording = 1,
		PendingScreenshot = 2,
		Ready = 3,
		Exported = 4,
		Canceled = 5,
		Unavailable = 6,
		Error = 7
	}

	public enum PerfMeterCaptureScreenshotState
	{
		NotRequested = 0,
		Pending = 1,
		Available = 2,
		Unavailable = 3,
		Error = 4
	}

	public enum PerfMeterCaptureExternalArtifactState
	{
		Unavailable = 0,
		FileObserved = 1,
		Authoritative = 2
	}

	public enum PerfMeterCaptureBundleExportStatus
	{
		Exported = 0,
		NotFound = 1,
		NotReady = 2,
		Conflict = 3,
		PathRejected = 4,
		QuotaExceeded = 5,
		AuthorityRequired = 6,
		IoError = 7,
		Canceled = 8
	}

	public enum PerfMeterCaptureBundleExportPhase
	{
		None = 0,
		Snapshotting = 1,
		Queued = 2,
		Serializing = 3,
		CopyingExternalArtifact = 4,
		HashingExternalArtifact = 5,
		Committing = 6,
		Retaining = 7,
		Completed = 8,
		Canceled = 9,
		Failed = 10
	}

	public enum PerfMeterCaptureBundleExportRequestResult
	{
		Started = 0,
		AlreadyActive = 1,
		NotFound = 2,
		NotReady = 3,
		Conflict = 4,
		InvalidRequest = 5,
		Unavailable = 6,
		Failed = 7
	}

	public readonly struct PerfMeterCaptureBundleOptions
	{
		public PerfMeterCaptureBundleOptions(bool includeScreenshot = false)
		{
			IncludeScreenshot = includeScreenshot;
		}

		public bool IncludeScreenshot { get; }
	}

	public readonly struct PerfMeterCaptureBundleStatusSnapshot
	{
		public PerfMeterCaptureBundleStatusSnapshot(
			PerfMeterAvailability availability,
			PerfMeterCaptureBundleState state,
			string bundleId,
			string captureId,
			PerfMeterCaptureState captureState,
			PerfMeterCaptureTool requestedTool,
			int baselineSampleCount,
			int captureSampleCount,
			int droppedCaptureSampleCount,
			int alertEventCount,
			bool alertEventsTruncated,
			PerfMeterCaptureScreenshotState screenshotState,
			PerfMeterCaptureExternalArtifactState externalArtifactState,
			string committedRelativePath,
			string warning,
			PerfMeterMemorySnapshotState memorySnapshotState = PerfMeterMemorySnapshotState.NotRequested)
			: this(
				availability,
				state,
				bundleId,
				captureId,
				captureState,
				requestedTool,
				baselineSampleCount,
				captureSampleCount,
				droppedCaptureSampleCount,
				alertEventCount,
				alertEventsTruncated,
				screenshotState,
				externalArtifactState,
				committedRelativePath,
				warning,
				memorySnapshotState,
				PerfMeterExternalArtifactSnapshot.Empty)
		{
		}

		public PerfMeterCaptureBundleStatusSnapshot(
			PerfMeterAvailability availability,
			PerfMeterCaptureBundleState state,
			string bundleId,
			string captureId,
			PerfMeterCaptureState captureState,
			PerfMeterCaptureTool requestedTool,
			int baselineSampleCount,
			int captureSampleCount,
			int droppedCaptureSampleCount,
			int alertEventCount,
			bool alertEventsTruncated,
			PerfMeterCaptureScreenshotState screenshotState,
			PerfMeterCaptureExternalArtifactState externalArtifactState,
			string committedRelativePath,
			string warning,
			PerfMeterMemorySnapshotState memorySnapshotState,
			PerfMeterExternalArtifactSnapshot externalArtifact)
		{
			Availability = availability;
			State = state;
			BundleId = bundleId ?? string.Empty;
			CaptureId = captureId ?? string.Empty;
			CaptureState = captureState;
			RequestedTool = requestedTool;
			BaselineSampleCount = Math.Max(0, baselineSampleCount);
			CaptureSampleCount = Math.Max(0, captureSampleCount);
			DroppedCaptureSampleCount = Math.Max(0, droppedCaptureSampleCount);
			AlertEventCount = Math.Max(0, alertEventCount);
			AlertEventsTruncated = alertEventsTruncated;
			ScreenshotState = screenshotState;
			ExternalArtifactState = externalArtifactState;
			CommittedRelativePath = committedRelativePath ?? string.Empty;
			Warning = warning ?? string.Empty;
			MemorySnapshotState = memorySnapshotState;
			ExternalArtifact = NormalizeExternalArtifact(externalArtifact);
		}

		public static PerfMeterCaptureBundleStatusSnapshot None => new PerfMeterCaptureBundleStatusSnapshot(
			PerfMeterAvailability.Unknown,
			PerfMeterCaptureBundleState.None,
			string.Empty,
			string.Empty,
			PerfMeterCaptureState.Idle,
			PerfMeterCaptureTool.Unknown,
			0,
			0,
			0,
			0,
			false,
			PerfMeterCaptureScreenshotState.NotRequested,
			PerfMeterCaptureExternalArtifactState.Unavailable,
			string.Empty,
			string.Empty);

		public bool IsTerminal => State == PerfMeterCaptureBundleState.Ready || State == PerfMeterCaptureBundleState.Exported || State == PerfMeterCaptureBundleState.Canceled || State == PerfMeterCaptureBundleState.Unavailable || State == PerfMeterCaptureBundleState.Error;
		public bool IsExportReady => IsTerminal && State != PerfMeterCaptureBundleState.Exported;
		public PerfMeterAvailability Availability { get; }
		public PerfMeterCaptureBundleState State { get; }
		public string BundleId { get; }
		public string CaptureId { get; }
		public PerfMeterCaptureState CaptureState { get; }
		public PerfMeterCaptureTool RequestedTool { get; }
		public int BaselineSampleCount { get; }
		public int CaptureSampleCount { get; }
		public int DroppedCaptureSampleCount { get; }
		public int AlertEventCount { get; }
		public bool AlertEventsTruncated { get; }
		public PerfMeterCaptureScreenshotState ScreenshotState { get; }
		public PerfMeterCaptureExternalArtifactState ExternalArtifactState { get; }
		public string CommittedRelativePath { get; }
		public string Warning { get; }
		public PerfMeterMemorySnapshotState MemorySnapshotState { get; }
		public PerfMeterExternalArtifactSnapshot ExternalArtifact { get; }

		private static PerfMeterExternalArtifactSnapshot NormalizeExternalArtifact(PerfMeterExternalArtifactSnapshot value)
		{
			return string.IsNullOrEmpty(value.ArtifactId) &&
				string.IsNullOrEmpty(value.RequestId) &&
				value.ArtifactKind == PerfMeterExternalArtifactKind.Unknown &&
				value.AssociationState == PerfMeterExternalArtifactAssociationState.None &&
				string.IsNullOrEmpty(value.SourceFileIdentitySha256)
				? PerfMeterExternalArtifactSnapshot.Empty
				: value;
		}
	}

	public readonly struct PerfMeterCaptureBundleExportStatusSnapshot
	{
		public PerfMeterCaptureBundleExportStatusSnapshot(
			string exportId,
			string captureId,
			string bundleId,
			PerfMeterCaptureBundleExportPhase phase,
			float progress,
			long bytesProcessed,
			long totalBytes,
			string committedRelativePath,
			PerfMeterCaptureBundleExportStatus legacyStatus,
			bool success,
			bool cancellationRequested,
			bool isTerminal,
			bool canRetry,
			string error,
			string warning,
			string startedUtc,
			string completedUtc,
			PerfMeterExternalArtifactSnapshot externalArtifact = default)
		{
			ExportId = exportId ?? string.Empty;
			CaptureId = captureId ?? string.Empty;
			BundleId = bundleId ?? string.Empty;
			Phase = phase;
			Progress = float.IsNaN(progress) || float.IsInfinity(progress) ? 0f : Math.Max(0f, Math.Min(1f, progress));
			BytesProcessed = Math.Max(0L, bytesProcessed);
			TotalBytes = Math.Max(0L, totalBytes);
			CommittedRelativePath = committedRelativePath ?? string.Empty;
			LegacyStatus = legacyStatus;
			Success = success;
			CancellationRequested = cancellationRequested;
			IsTerminal = isTerminal;
			CanRetry = canRetry;
			Error = error ?? string.Empty;
			Warning = warning ?? string.Empty;
			StartedUtc = startedUtc ?? string.Empty;
			CompletedUtc = completedUtc ?? string.Empty;
			ExternalArtifact = NormalizeExternalArtifact(externalArtifact);
		}

		public static PerfMeterCaptureBundleExportStatusSnapshot None => new PerfMeterCaptureBundleExportStatusSnapshot(
			string.Empty,
			string.Empty,
			string.Empty,
			PerfMeterCaptureBundleExportPhase.None,
			0f,
			0L,
			0L,
			string.Empty,
			PerfMeterCaptureBundleExportStatus.NotFound,
			false,
			false,
			true,
			false,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty);

		public bool IsActive => !IsTerminal;
		public bool IsCanceled => Phase == PerfMeterCaptureBundleExportPhase.Canceled || LegacyStatus == PerfMeterCaptureBundleExportStatus.Canceled;
		public string RelativePath => CommittedRelativePath;
		public PerfMeterCaptureBundleExportStatus Status => LegacyStatus;
		public string ExportId { get; }
		public string CaptureId { get; }
		public string BundleId { get; }
		public PerfMeterCaptureBundleExportPhase Phase { get; }
		public float Progress { get; }
		public long BytesProcessed { get; }
		public long TotalBytes { get; }
		public string CommittedRelativePath { get; }
		public PerfMeterCaptureBundleExportStatus LegacyStatus { get; }
		public bool Success { get; }
		public bool CancellationRequested { get; }
		public bool IsTerminal { get; }
		public bool CanRetry { get; }
		public string Error { get; }
		public string Warning { get; }
		public string StartedUtc { get; }
		public string CompletedUtc { get; }
		public PerfMeterExternalArtifactSnapshot ExternalArtifact { get; }

		private static PerfMeterExternalArtifactSnapshot NormalizeExternalArtifact(PerfMeterExternalArtifactSnapshot value)
		{
			return string.IsNullOrEmpty(value.ArtifactId) &&
				string.IsNullOrEmpty(value.RequestId) &&
				value.ArtifactKind == PerfMeterExternalArtifactKind.Unknown &&
				value.AssociationState == PerfMeterExternalArtifactAssociationState.None &&
				string.IsNullOrEmpty(value.SourceFileIdentitySha256)
				? PerfMeterExternalArtifactSnapshot.Empty
				: value;
		}
	}

	public readonly struct PerfMeterCaptureCapabilitiesSnapshot
	{
		public PerfMeterCaptureCapabilitiesSnapshot(
			bool renderDocSupported,
			bool pixSupported,
			bool screenshotSupported,
			int maxCaptureFrames,
			int maxRollFrames,
			long maxBundleBytes,
			long maxScreenshotBytes,
			long totalQuotaBytes,
			int maxCommittedBundles,
			int retentionDays,
			string bundleRoot,
			long maxMemorySnapshotBytes = 0L)
		{
			RenderDocSupported = renderDocSupported;
			PixSupported = pixSupported;
			ScreenshotSupported = screenshotSupported;
			MaxCaptureFrames = maxCaptureFrames;
			MaxRollFrames = maxRollFrames;
			MaxBundleBytes = maxBundleBytes;
			MaxScreenshotBytes = maxScreenshotBytes;
			TotalQuotaBytes = totalQuotaBytes;
			MaxCommittedBundles = maxCommittedBundles;
			RetentionDays = retentionDays;
			BundleRoot = bundleRoot ?? string.Empty;
			MaxMemorySnapshotBytes = Math.Max(0L, maxMemorySnapshotBytes);
		}

		public bool RenderDocSupported { get; }
		public bool PixSupported { get; }
		public bool ScreenshotSupported { get; }
		public int MaxCaptureFrames { get; }
		public int MaxRollFrames { get; }
		public long MaxBundleBytes { get; }
		public long MaxScreenshotBytes { get; }
		public long TotalQuotaBytes { get; }
		public int MaxCommittedBundles { get; }
		public int RetentionDays { get; }
		public string BundleRoot { get; }
		public long MaxMemorySnapshotBytes { get; }
	}

	public readonly struct PerfMeterCaptureBundleExportResult
	{
		public PerfMeterCaptureBundleExportResult(
			bool success,
			PerfMeterCaptureBundleExportStatus status,
			string relativePath,
			string error,
			PerfMeterCaptureBundleStatusSnapshot bundle)
			: this(success, status, relativePath, error, bundle, bundle.ExternalArtifact)
		{
		}

		public PerfMeterCaptureBundleExportResult(bool success, PerfMeterCaptureBundleExportStatus status, string relativePath, string error, PerfMeterCaptureBundleStatusSnapshot bundle, PerfMeterExternalArtifactSnapshot externalArtifact)
		{
			Success = success;
			Status = status;
			RelativePath = relativePath ?? string.Empty;
			Error = error ?? string.Empty;
			Bundle = bundle;
			ExternalArtifact = string.IsNullOrEmpty(externalArtifact.ArtifactId) && string.IsNullOrEmpty(externalArtifact.RequestId) && externalArtifact.ArtifactKind == PerfMeterExternalArtifactKind.Unknown && string.IsNullOrEmpty(externalArtifact.SourceFileIdentitySha256)
				? bundle.ExternalArtifact
				: externalArtifact;
		}

		public bool Success { get; }
		public PerfMeterCaptureBundleExportStatus Status { get; }
		public string RelativePath { get; }
		public string Error { get; }
		public PerfMeterCaptureBundleStatusSnapshot Bundle { get; }
		public PerfMeterExternalArtifactSnapshot ExternalArtifact { get; }
	}

	internal sealed class PerfMeterCaptureBundleCoordinator
	{
		internal const int MaxCaptureSamples = 600;
		private BundleRecord _record;

		internal PerfMeterCaptureBundleStatusSnapshot GetStatus(string captureId = null)
		{
			if (_record == null || (!string.IsNullOrEmpty(captureId) && !string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal)))
			{
				return PerfMeterCaptureBundleStatusSnapshot.None;
			}

			return _record.CreateStatus();
		}

		internal PerfMeterCaptureStatusSnapshot GetCaptureStatus(string captureId)
		{
			return _record != null && string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal)
				? _record.CaptureStatus
				: PerfMeterCaptureStatusSnapshot.NotRunning;
		}

		internal void Start(PerfMeterCaptureOptions captureOptions, PerfMeterCaptureBundleOptions bundleOptions, PerfMeterCaptureStatusSnapshot captureStatus)
		{
			Start(captureOptions, bundleOptions, captureStatus, default, default);
		}

		internal void Start(
			PerfMeterCaptureOptions captureOptions,
			PerfMeterCaptureBundleOptions bundleOptions,
			PerfMeterCaptureStatusSnapshot captureStatus,
			PerfMeterSettingsSnapshot configuredSettings,
			PerfMeterSettingsSnapshot effectiveSettings)
		{
			_record = new BundleRecord(captureOptions, bundleOptions, captureStatus, configuredSettings, effectiveSettings);
		}

		internal void StartMemorySnapshot(
			PerfMeterMemorySnapshotOptions options,
			PerfMeterMemorySnapshotStatusSnapshot memoryStatus,
			PerfMeterSettingsSnapshot configuredSettings,
			PerfMeterSettingsSnapshot effectiveSettings)
		{
			PerfMeterCaptureOptions captureOptions = new PerfMeterCaptureOptions(options.CaptureId, PerfMeterCaptureTool.MemoryProfiler);
			PerfMeterCaptureStatusSnapshot captureStatus = MemoryCaptureStatus(memoryStatus);
			_record = new BundleRecord(captureOptions, default, captureStatus, configuredSettings, effectiveSettings)
			{
				MemorySnapshotState = memoryStatus.State
			};
		}

		internal void ObserveMemorySnapshot(
			PerfMeterMemorySnapshotStatusSnapshot memoryStatus,
			PerfMeterMemorySnapshotArtifact artifact,
			PerfMeterSessionSummarySnapshot sessionSummary,
			PerfMeterSessionSampleSnapshot[] baselineSamples,
			PerfMeterStatusSnapshot runtimeStatus,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render)
		{
			ObserveMemorySnapshot(
				memoryStatus,
				artifact,
				sessionSummary,
				baselineSamples,
				PerfMeterSessionTimelineSnapshot.Empty,
				runtimeStatus,
				device,
				camera,
				render,
				PerfMeterRenderIntegrationSnapshot.NotObserved);
		}

		internal void ObserveMemorySnapshot(
			PerfMeterMemorySnapshotStatusSnapshot memoryStatus,
			PerfMeterMemorySnapshotArtifact artifact,
			PerfMeterSessionSummarySnapshot sessionSummary,
			PerfMeterSessionSampleSnapshot[] baselineSamples,
			PerfMeterSessionTimelineSnapshot sessionTimeline,
			PerfMeterStatusSnapshot runtimeStatus,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterRenderIntegrationSnapshot renderIntegration)
		{
			if (_record == null || _record.CaptureOptions.Tool != PerfMeterCaptureTool.MemoryProfiler || !string.Equals(_record.CaptureOptions.CaptureId, memoryStatus.CaptureId, StringComparison.Ordinal))
			{
				return;
			}

			_record.MemorySnapshotState = memoryStatus.State;
			_record.CaptureStatus = MemoryCaptureStatus(memoryStatus);
			if (memoryStatus.IsActive)
			{
				return;
			}

			_record.CaptureContext(device, camera, render, renderIntegration, runtimeStatus);
			_record.Freeze(sessionSummary, baselineSamples, sessionTimeline, runtimeStatus, Array.Empty<PerfMeterAlertSnapshot>(), false);
			_record.MemorySnapshotArtifact = artifact;
			_record.Warning = string.Empty;
			switch (memoryStatus.State)
			{
				case PerfMeterMemorySnapshotState.Completed:
					_record.State = artifact.IsAvailable ? PerfMeterCaptureBundleState.Ready : PerfMeterCaptureBundleState.Error;
					break;
				case PerfMeterMemorySnapshotState.Unavailable:
					_record.State = PerfMeterCaptureBundleState.Unavailable;
					break;
				default:
					_record.State = PerfMeterCaptureBundleState.Error;
					break;
			}
		}

		internal void UpdateCaptureStatus(PerfMeterCaptureStatusSnapshot captureStatus)
		{
			if (_record != null && string.Equals(_record.CaptureOptions.CaptureId, captureStatus.CaptureId, StringComparison.Ordinal))
			{
				_record.CaptureStatus = captureStatus;
			}
		}

		internal void SetSelfOverheadWindow(string captureId, PerfMeterSelfOverheadWindowSnapshot snapshot)
		{
			if (_record != null &&
				snapshot.Kind == PerfMeterSelfOverheadWindowKind.Capture &&
				snapshot.Epoch > 0L &&
				string.Equals(snapshot.Identity, captureId, StringComparison.Ordinal) &&
				string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal))
			{
				_record.SelfOverheadWindow = snapshot;
			}
		}

		internal void ObserveExternalArtifact(
			string captureId,
			string bundleId,
			PerfMeterExternalArtifactSnapshot snapshot)
		{
			if (_record == null ||
				_record.State == PerfMeterCaptureBundleState.Exported ||
				snapshot.IsAuthoritative ||
				!string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) ||
				!string.Equals(_record.BundleId, bundleId, StringComparison.Ordinal))
			{
				return;
			}

			_record.ExternalArtifact = snapshot;
			_record.ExternalArtifactState = GetExternalArtifactState(snapshot);
			_record.NativeArtifactSource = default;
		}

		internal bool ObserveNativeExternalArtifact(
			string captureId,
			string bundleId,
			PerfMeterExternalArtifactSnapshot snapshot,
			PerfMeterNativeExternalArtifactSourceDescriptor sourceDescriptor)
		{
			if (_record == null ||
				_record.State == PerfMeterCaptureBundleState.Exported ||
				_record.CaptureOptions.Tool != PerfMeterCaptureTool.RenderDoc ||
				_record.CaptureOptions.BackendMode == PerfMeterCaptureBackendMode.GenericUnity ||
				!string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) ||
				!string.Equals(_record.BundleId, bundleId, StringComparison.Ordinal) ||
				snapshot.StorageMode != _record.CaptureOptions.ExternalArtifactStorageMode ||
				!sourceDescriptor.IsStructurallyValid(captureId, snapshot))
			{
				return false;
			}

			_record.ExternalArtifact = snapshot;
			_record.ExternalArtifactState = snapshot.IsAuthoritative
				? PerfMeterCaptureExternalArtifactState.Authoritative
				: PerfMeterCaptureExternalArtifactState.FileObserved;
			_record.NativeArtifactSource = sourceDescriptor;
			return true;
		}

		private static PerfMeterCaptureExternalArtifactState GetExternalArtifactState(PerfMeterExternalArtifactSnapshot snapshot)
		{
			bool observedFile =
				(snapshot.FinalizationState == PerfMeterExternalArtifactFinalizationState.Observed ||
					snapshot.FinalizationState == PerfMeterExternalArtifactFinalizationState.Finalized) &&
				(snapshot.SizeBytes > 0L ||
					!string.IsNullOrEmpty(snapshot.ObservedSourceSha256) ||
					!string.IsNullOrEmpty(snapshot.SourceFileIdentitySha256));
			return observedFile
				? PerfMeterCaptureExternalArtifactState.FileObserved
				: PerfMeterCaptureExternalArtifactState.Unavailable;
		}

		internal bool IsRecordingCaptureFrame(PerfMeterCaptureStatusSnapshot captureStatus)
		{
			return _record != null && _record.State == PerfMeterCaptureBundleState.Recording && captureStatus.State == PerfMeterCaptureState.Capturing && string.Equals(_record.CaptureOptions.CaptureId, captureStatus.CaptureId, StringComparison.Ordinal);
		}

		internal bool NeedsCaptureContext(string captureId)
		{
			return _record != null && string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) && !_record.ContextCaptured;
		}

		internal void RecordCaptureFrame(
			PerfMeterSessionSampleSnapshot sample,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterStatusSnapshot runtimeStatus)
		{
			RecordCaptureFrame(sample, device, camera, render, PerfMeterRenderIntegrationSnapshot.NotObserved, runtimeStatus);
		}

		internal void RecordCaptureFrame(
			PerfMeterSessionSampleSnapshot sample,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterRenderIntegrationSnapshot renderIntegration,
			PerfMeterStatusSnapshot runtimeStatus)
		{
			if (_record == null || _record.State != PerfMeterCaptureBundleState.Recording)
			{
				return;
			}

			_record.CaptureContext(device, camera, render, renderIntegration, runtimeStatus);
			_record.AddCaptureSample(sample);
		}

		internal void RecordCaptureFrame(
			int collectionFrame,
			double collectionTimeSeconds,
			string sceneName,
			PerfMeterMetricsSnapshot metrics,
			PerfMeterCustomMetricCollection customMetrics,
			PerfMeterPlatformTelemetrySnapshot platformTelemetry,
			string graphicsStateTraceId,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterRenderIntegrationSnapshot renderIntegration,
			PerfMeterStatusSnapshot runtimeStatus)
		{
			RecordCaptureFrame(
				collectionFrame,
				collectionTimeSeconds,
				sceneName,
				metrics,
				customMetrics,
				platformTelemetry,
				graphicsStateTraceId,
				device,
				camera,
				render,
				renderIntegration,
				runtimeStatus,
				_record != null ? _record.CaptureStatus : PerfMeterCaptureStatusSnapshot.NotRunning,
				out _);
		}

		internal bool RecordCaptureFrame(
			int collectionFrame,
			double collectionTimeSeconds,
			string sceneName,
			PerfMeterMetricsSnapshot metrics,
			PerfMeterCustomMetricCollection customMetrics,
			PerfMeterPlatformTelemetrySnapshot platformTelemetry,
			string graphicsStateTraceId,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterRenderIntegrationSnapshot renderIntegration,
			PerfMeterStatusSnapshot runtimeStatus,
			PerfMeterCaptureStatusSnapshot captureStatus,
			out int captureSampleIndex)
		{
			captureSampleIndex = -1;
			if (_record == null || _record.State != PerfMeterCaptureBundleState.Recording)
			{
				return false;
			}

			_record.CaptureContext(device, camera, render, renderIntegration, runtimeStatus);
			return _record.AddCaptureSample(collectionFrame, collectionTimeSeconds, sceneName, metrics, customMetrics, platformTelemetry, graphicsStateTraceId, captureStatus, out captureSampleIndex);
		}

		internal void RecordMissingCaptureFrame(PerfMeterCaptureStatusSnapshot captureStatus, int frame, double timeSeconds, PerfMeterSessionTimelineReasonFlags reason)
		{
			if (_record == null || _record.State != PerfMeterCaptureBundleState.Recording || captureStatus.State != PerfMeterCaptureState.Capturing)
			{
				return;
			}

			_record.AddMissingCaptureFrame(captureStatus, frame, timeSeconds, reason);
		}

		internal void RecordCaptureBoundary(PerfMeterCaptureStatusSnapshot captureStatus, PerfMeterSessionTimelineCaptureBoundary boundary, int frame, double timeSeconds)
		{
			if (_record == null || _record.State != PerfMeterCaptureBundleState.Recording)
			{
				return;
			}

			_record.AddCaptureBoundary(captureStatus, boundary, frame, timeSeconds);
		}

		internal void ObserveCapture(
			PerfMeterCaptureStatusSnapshot captureStatus,
			PerfMeterSessionSummarySnapshot sessionSummary,
			PerfMeterSessionSampleSnapshot[] baselineSamples,
			PerfMeterStatusSnapshot runtimeStatus,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterAlertSnapshot[] alerts,
			bool alertsTruncated)
		{
			ObserveCapture(
				captureStatus,
				sessionSummary,
				baselineSamples,
				PerfMeterSessionTimelineSnapshot.Empty,
				runtimeStatus,
				device,
				camera,
				render,
				PerfMeterRenderIntegrationSnapshot.NotObserved,
				alerts,
				alertsTruncated);
		}

		internal void ObserveCapture(
			PerfMeterCaptureStatusSnapshot captureStatus,
			PerfMeterSessionSummarySnapshot sessionSummary,
			PerfMeterSessionSampleSnapshot[] baselineSamples,
			PerfMeterSessionTimelineSnapshot sessionTimeline,
			PerfMeterStatusSnapshot runtimeStatus,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterRenderIntegrationSnapshot renderIntegration,
			PerfMeterAlertSnapshot[] alerts,
			bool alertsTruncated)
		{
			if (_record == null || !string.Equals(_record.CaptureOptions.CaptureId, captureStatus.CaptureId, StringComparison.Ordinal))
			{
				return;
			}

			_record.CaptureStatus = captureStatus;
			if (captureStatus.State == PerfMeterCaptureState.PreRoll || captureStatus.State == PerfMeterCaptureState.Capturing || captureStatus.State == PerfMeterCaptureState.PostRoll)
			{
				return;
			}

			_record.CaptureContext(device, camera, render, renderIntegration, runtimeStatus);
			_record.Freeze(sessionSummary, baselineSamples, sessionTimeline, runtimeStatus, alerts, alertsTruncated);
			switch (captureStatus.State)
			{
				case PerfMeterCaptureState.Completed:
					if (_record.BundleOptions.IncludeScreenshot)
					{
						_record.State = PerfMeterCaptureBundleState.PendingScreenshot;
						_record.ScreenshotState = PerfMeterCaptureScreenshotState.Pending;
					}
					else
					{
						_record.State = PerfMeterCaptureBundleState.Ready;
					}
					break;
				case PerfMeterCaptureState.Canceled:
					_record.State = PerfMeterCaptureBundleState.Canceled;
					break;
				case PerfMeterCaptureState.Unavailable:
					_record.State = PerfMeterCaptureBundleState.Unavailable;
					break;
				default:
					_record.State = PerfMeterCaptureBundleState.Error;
					break;
			}

			if (captureStatus.State != PerfMeterCaptureState.Completed && _record.BundleOptions.IncludeScreenshot)
			{
				_record.ScreenshotState = captureStatus.State == PerfMeterCaptureState.Error
					? PerfMeterCaptureScreenshotState.Error
					: PerfMeterCaptureScreenshotState.Unavailable;
			}
		}

		internal void ObserveCapture(
			PerfMeterCaptureStatusSnapshot captureStatus,
			PerfMeterSessionSummarySnapshot sessionSummary,
			PerfMeterSessionSampleSnapshot[] baselineSamples,
			PerfMeterStatusSnapshot runtimeStatus,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterRenderIntegrationSnapshot renderIntegration,
			PerfMeterAlertSnapshot[] alerts,
			bool alertsTruncated)
		{
			ObserveCapture(
				captureStatus,
				sessionSummary,
				baselineSamples,
				PerfMeterSessionTimelineSnapshot.Empty,
				runtimeStatus,
				device,
				camera,
				render,
				renderIntegration,
				alerts,
				alertsTruncated);
		}

		internal void CancelActive(string warning)
		{
			if (_record == null || _record.State != PerfMeterCaptureBundleState.Recording)
			{
				return;
			}

			_record.State = PerfMeterCaptureBundleState.Canceled;
			_record.Warning = warning ?? string.Empty;
		}

		internal void CompletePendingScreenshotAsUnavailable(string bundleId, string warning)
		{
			if (_record != null && string.Equals(_record.BundleId, bundleId, StringComparison.Ordinal) && _record.State == PerfMeterCaptureBundleState.PendingScreenshot)
			{
				CompleteScreenshot(_record.CaptureOptions.CaptureId, bundleId, null, warning, true);
			}
		}

		internal bool TryStartScreenshot(out string captureId, out string bundleId)
		{
			captureId = string.Empty;
			bundleId = string.Empty;
			if (_record == null || _record.State != PerfMeterCaptureBundleState.PendingScreenshot || _record.ScreenshotStarted)
			{
				return false;
			}

			_record.ScreenshotStarted = true;
			captureId = _record.CaptureOptions.CaptureId;
			bundleId = _record.BundleId;
			return true;
		}

		internal void CompleteScreenshot(string captureId, string bundleId, byte[] pngBytes, string error, bool unavailable)
		{
			if (_record == null || !string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) || !string.Equals(_record.BundleId, bundleId, StringComparison.Ordinal) || _record.State != PerfMeterCaptureBundleState.PendingScreenshot)
			{
				return;
			}

			if (pngBytes != null && pngBytes.Length > 0 && pngBytes.LongLength <= PerfMeterCaptureBundleExporter.MaxScreenshotBytes)
			{
				_record.ScreenshotBytes = pngBytes;
				_record.ScreenshotState = PerfMeterCaptureScreenshotState.Available;
			}
			else
			{
				_record.ScreenshotState = unavailable ? PerfMeterCaptureScreenshotState.Unavailable : PerfMeterCaptureScreenshotState.Error;
				_record.Warning = string.IsNullOrEmpty(error) ? "Runtime screenshot was not produced." : error;
			}

			_record.State = PerfMeterCaptureBundleState.Ready;
		}

		internal bool TryGetExportData(string captureId, out PerfMeterCaptureBundleExportData data)
		{
			data = null;
			if (_record == null || !string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) || !_record.CreateStatus().IsExportReady)
			{
				return false;
			}

			data = _record.CreateExportData();
			return true;
		}

		internal bool TryBeginExport(string captureId, string bundleId, string exportId)
		{
			if (_record == null ||
				!string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) ||
				!string.Equals(_record.BundleId, bundleId, StringComparison.Ordinal) ||
				string.IsNullOrEmpty(exportId) ||
				!_record.CreateStatus().IsExportReady)
			{
				return false;
			}

			if (!string.IsNullOrEmpty(_record.ExportId) && !string.Equals(_record.ExportId, exportId, StringComparison.Ordinal))
			{
				return false;
			}

			_record.ExportId = exportId;
			return true;
		}

		internal void ClearExport(string captureId, string bundleId, string exportId)
		{
			if (_record != null &&
				string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) &&
				string.Equals(_record.BundleId, bundleId, StringComparison.Ordinal) &&
				string.Equals(_record.ExportId, exportId, StringComparison.Ordinal))
			{
				_record.ExportId = string.Empty;
			}
		}

		internal bool TryGetMemorySnapshotArtifact(out PerfMeterMemorySnapshotArtifact artifact)
		{
			artifact = _record != null ? _record.MemorySnapshotArtifact : default;
			return artifact.IsAvailable;
		}

		internal void ClearMemorySnapshotArtifact(string captureId, string path)
		{
			if (_record != null &&
				string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) &&
				string.Equals(_record.MemorySnapshotArtifact.SourcePath, path, StringComparison.Ordinal))
			{
				_record.MemorySnapshotArtifact = default;
			}
		}

		internal void AppendWarning(string captureId, string bundleId, string warning)
		{
			if (_record != null &&
				!string.IsNullOrEmpty(warning) &&
				string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) &&
				string.Equals(_record.BundleId, bundleId, StringComparison.Ordinal))
			{
				_record.Warning = CombineWarnings(_record.Warning, warning);
			}
		}

		internal void MarkExported(string captureId, string relativePath, PerfMeterCaptureExternalArtifactState externalArtifactState)
		{
			if (_record == null || !string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal))
			{
				return;
			}

			MarkExported(captureId, _record.BundleId, _record.ExportId, relativePath, externalArtifactState, _record.ExternalArtifact);
		}

		internal bool MarkExported(
			string captureId,
			string bundleId,
			string exportId,
			string relativePath,
			PerfMeterCaptureExternalArtifactState externalArtifactState,
			PerfMeterExternalArtifactSnapshot externalArtifact)
		{
			if (_record == null ||
				!string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal) ||
				!string.Equals(_record.BundleId, bundleId, StringComparison.Ordinal) ||
				(!string.IsNullOrEmpty(exportId) && !string.Equals(_record.ExportId, exportId, StringComparison.Ordinal)))
			{
				return false;
			}

			_record.State = PerfMeterCaptureBundleState.Exported;
			_record.CommittedRelativePath = relativePath ?? string.Empty;
			_record.ExternalArtifactState = externalArtifactState;
			_record.ExternalArtifact = externalArtifact;
			_record.ExportId = string.Empty;
			return true;
		}

		internal void ResetForTests()
		{
			_record = null;
		}

		private sealed class BundleRecord
		{
			private readonly PerfMeterSessionSampleSnapshot[] _captureSamples = new PerfMeterSessionSampleSnapshot[MaxCaptureSamples];
			private readonly PerfMeterSessionTimelineStore _timeline = new PerfMeterSessionTimelineStore();
			private int _captureSampleCount;
			private int _droppedCaptureSampleCount;
			private bool _contextCaptured;
			private bool _captureBeginBoundaryRecorded;
			private bool _captureEndBoundaryRecorded;

			internal BundleRecord(
				PerfMeterCaptureOptions captureOptions,
				PerfMeterCaptureBundleOptions bundleOptions,
				PerfMeterCaptureStatusSnapshot captureStatus,
				PerfMeterSettingsSnapshot configuredSettings,
				PerfMeterSettingsSnapshot effectiveSettings)
			{
				BundleId = Guid.NewGuid().ToString("N");
				CaptureOptions = captureOptions;
				BundleOptions = bundleOptions;
				CaptureStatus = captureStatus;
				ConfiguredSettings = configuredSettings;
				EffectiveSettings = effectiveSettings;
				RenderIntegration = PerfMeterRenderIntegrationSnapshot.NotObserved;
				SelfOverheadWindow = PerfMeterSelfOverheadWindowSnapshot.Unavailable;
				SessionSelfOverheadWindow = PerfMeterSelfOverheadWindowSnapshot.Unavailable;
				State = PerfMeterCaptureBundleState.Recording;
				ScreenshotState = bundleOptions.IncludeScreenshot ? PerfMeterCaptureScreenshotState.Pending : PerfMeterCaptureScreenshotState.NotRequested;
				StartedUtc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
				_timeline.Start(0, MaxCaptureSamples);
			}

			internal string BundleId { get; }
			internal PerfMeterCaptureOptions CaptureOptions { get; }
			internal PerfMeterCaptureBundleOptions BundleOptions { get; }
			internal PerfMeterCaptureStatusSnapshot CaptureStatus { get; set; }
			internal PerfMeterSettingsSnapshot ConfiguredSettings { get; }
			internal PerfMeterSettingsSnapshot EffectiveSettings { get; }
			internal PerfMeterCaptureBundleState State { get; set; }
			internal PerfMeterCaptureScreenshotState ScreenshotState { get; set; }
			internal PerfMeterCaptureExternalArtifactState ExternalArtifactState { get; set; }
			internal PerfMeterExternalArtifactSnapshot ExternalArtifact { get; set; } = PerfMeterExternalArtifactSnapshot.Empty;
			internal PerfMeterNativeExternalArtifactSourceDescriptor NativeArtifactSource { get; set; }
			internal PerfMeterMemorySnapshotState MemorySnapshotState { get; set; } = PerfMeterMemorySnapshotState.NotRequested;
			internal PerfMeterMemorySnapshotArtifact MemorySnapshotArtifact { get; set; }
			internal bool ScreenshotStarted { get; set; }
			internal byte[] ScreenshotBytes { get; set; }
			internal string CommittedRelativePath { get; set; }
			internal string Warning { get; set; }
			internal string ExportId { get; set; }
			internal string StartedUtc { get; }
			internal string CompletedUtc { get; private set; }
			internal PerfMeterDeviceSnapshot Device { get; private set; }
			internal PerfMeterCameraSnapshot Camera { get; private set; }
			internal PerfMeterRenderGraphSnapshot Render { get; private set; }
			internal PerfMeterRenderIntegrationSnapshot RenderIntegration { get; private set; }
			internal PerfMeterSelfOverheadWindowSnapshot SelfOverheadWindow { get; set; }
			internal PerfMeterSelfOverheadWindowSnapshot SessionSelfOverheadWindow { get; private set; }
			internal PerfMeterStatusSnapshot RuntimeStatus { get; private set; }
			internal PerfMeterSessionSummarySnapshot SessionSummary { get; private set; }
			internal PerfMeterSessionTimelineSnapshot SessionTimeline { get; private set; } = PerfMeterSessionTimelineSnapshot.Empty;
			internal PerfMeterSessionSampleSnapshot[] BaselineSamples { get; private set; } = Array.Empty<PerfMeterSessionSampleSnapshot>();
			internal PerfMeterAlertSnapshot[] AlertEvents { get; private set; } = Array.Empty<PerfMeterAlertSnapshot>();
			internal bool AlertEventsTruncated { get; private set; }
			internal bool ContextCaptured => _contextCaptured;

			internal void CaptureContext(PerfMeterDeviceSnapshot device, PerfMeterCameraSnapshot camera, PerfMeterRenderGraphSnapshot render, PerfMeterStatusSnapshot runtimeStatus)
			{
				CaptureContext(device, camera, render, PerfMeterRenderIntegrationSnapshot.NotObserved, runtimeStatus);
			}

			internal void CaptureContext(PerfMeterDeviceSnapshot device, PerfMeterCameraSnapshot camera, PerfMeterRenderGraphSnapshot render, PerfMeterRenderIntegrationSnapshot renderIntegration, PerfMeterStatusSnapshot runtimeStatus)
			{
				if (_contextCaptured)
				{
					return;
				}

				_contextCaptured = true;
				Device = device;
				Camera = camera;
				Render = render;
				RenderIntegration = renderIntegration;
				RuntimeStatus = runtimeStatus;
			}

			internal void AddCaptureSample(PerfMeterSessionSampleSnapshot sample)
			{
				if (_captureSampleCount >= _captureSamples.Length)
				{
					_droppedCaptureSampleCount++;
					return;
				}

				int sampleIndex = _captureSampleCount;
				_captureSamples[_captureSampleCount++] = CopySample(sample);
				_timeline.AddValidCapture(
					sample.CollectionFrame,
					sample.CollectionTimeSeconds,
					CaptureOptions.CaptureId,
					BundleId,
					CaptureStatus.CompletedCaptureFrames + 1,
					CaptureStatus.RequestedCaptureFrames,
					sampleIndex);
			}

			internal bool AddCaptureSample(
				int collectionFrame,
				double collectionTimeSeconds,
				string sceneName,
				PerfMeterMetricsSnapshot metrics,
				PerfMeterCustomMetricCollection customMetrics,
				PerfMeterPlatformTelemetrySnapshot platformTelemetry,
				string graphicsStateTraceId,
				PerfMeterCaptureStatusSnapshot captureStatus,
				out int captureSampleIndex)
			{
				captureSampleIndex = -1;
				if (_captureSampleCount >= _captureSamples.Length)
				{
					_droppedCaptureSampleCount++;
					return false;
				}

				PerfMeterCustomMetricSnapshot[] customMetricCopy = CopyCustomMetrics(customMetrics);
				captureSampleIndex = _captureSampleCount;
				_captureSamples[_captureSampleCount++] = new PerfMeterSessionSampleSnapshot(collectionFrame, collectionTimeSeconds, sceneName, metrics, customMetricCopy, platformTelemetry, graphicsStateTraceId);
				_timeline.AddValidCapture(
					collectionFrame,
					collectionTimeSeconds,
					captureStatus.CaptureId,
					BundleId,
					captureStatus.CompletedCaptureFrames + 1,
					captureStatus.RequestedCaptureFrames,
					captureSampleIndex);
				return true;
			}

			internal void AddMissingCaptureFrame(PerfMeterCaptureStatusSnapshot captureStatus, int frame, double timeSeconds, PerfMeterSessionTimelineReasonFlags reason)
			{
				_timeline.AddMissingCapture(
					frame,
					frame,
					timeSeconds,
					timeSeconds,
					captureStatus.CaptureId,
					BundleId,
					captureStatus.CompletedCaptureFrames + 1,
					captureStatus.RequestedCaptureFrames,
					reason);
			}

			internal void AddCaptureBoundary(PerfMeterCaptureStatusSnapshot captureStatus, PerfMeterSessionTimelineCaptureBoundary boundary, int frame, double timeSeconds)
			{
				if (boundary == PerfMeterSessionTimelineCaptureBoundary.Begin && _captureBeginBoundaryRecorded ||
					boundary == PerfMeterSessionTimelineCaptureBoundary.End && _captureEndBoundaryRecorded)
				{
					return;
				}

				_timeline.AddCaptureBoundary(
					frame,
					timeSeconds,
					captureStatus.CaptureId,
					BundleId,
					boundary,
					PerfMeterSessionTimelineUtility.GetCapturePhase(captureStatus.State),
					captureStatus.RequestedCaptureFrames,
					captureStatus.State == PerfMeterCaptureState.Unavailable || captureStatus.State == PerfMeterCaptureState.Error
						? PerfMeterSessionTimelineReasonFlags.CaptureFrameMissing
						: PerfMeterSessionTimelineReasonFlags.None);

				if (boundary == PerfMeterSessionTimelineCaptureBoundary.Begin)
				{
					_captureBeginBoundaryRecorded = true;
				}
				else if (boundary == PerfMeterSessionTimelineCaptureBoundary.End)
				{
					_captureEndBoundaryRecorded = true;
				}
			}

			internal void Freeze(PerfMeterSessionSummarySnapshot sessionSummary, PerfMeterSessionSampleSnapshot[] baselineSamples, PerfMeterSessionTimelineSnapshot sessionTimeline, PerfMeterStatusSnapshot runtimeStatus, PerfMeterAlertSnapshot[] alerts, bool alertsTruncated)
			{
				if (!string.IsNullOrEmpty(CompletedUtc))
				{
					return;
				}

				CompletedUtc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
				SessionSummary = sessionSummary;
				SessionSelfOverheadWindow = string.IsNullOrEmpty(sessionSummary.SessionId)
					? PerfMeterSelfOverheadWindowSnapshot.Unavailable
					: PerfMeterSelfObservability.GetBoundWindowSnapshot(PerfMeterSelfOverheadWindowKind.Session, sessionSummary.SessionId, runtimeStatus.CollectionFrame);
				SessionTimeline = sessionTimeline;
				BaselineSamples = CopySamples(baselineSamples);
				RuntimeStatus = runtimeStatus;
				AlertEvents = alerts ?? Array.Empty<PerfMeterAlertSnapshot>();
				AlertEventsTruncated = alertsTruncated;
			}

			internal PerfMeterCaptureBundleStatusSnapshot CreateStatus()
			{
				PerfMeterAvailability availability = State == PerfMeterCaptureBundleState.Unavailable || State == PerfMeterCaptureBundleState.Error
					? PerfMeterAvailability.Unavailable
					: PerfMeterAvailability.Available;
				return new PerfMeterCaptureBundleStatusSnapshot(
					availability,
					State,
					BundleId,
					CaptureOptions.CaptureId,
					CaptureStatus.State,
					CaptureOptions.Tool,
					BaselineSamples.Length,
					_captureSampleCount,
					_droppedCaptureSampleCount,
					AlertEvents.Length,
					AlertEventsTruncated,
					ScreenshotState,
					ExternalArtifactState,
					CommittedRelativePath,
					CombineWarnings(CaptureStatus.Warning, Warning),
					MemorySnapshotState,
					ExternalArtifact);
			}

			internal PerfMeterCaptureBundleExportData CreateExportData()
			{
				PerfMeterSessionSampleSnapshot[] captureSamples = new PerfMeterSessionSampleSnapshot[_captureSampleCount];
				for (int i = 0; i < _captureSampleCount; i++)
				{
					captureSamples[i] = CopySample(_captureSamples[i]);
				}

				byte[] screenshot = ScreenshotBytes == null ? null : (byte[])ScreenshotBytes.Clone();
				return new PerfMeterCaptureBundleExportData(
					CreateStatus(),
					CaptureOptions,
					BundleOptions,
					StartedUtc,
					CompletedUtc,
					SessionSummary,
					SessionTimeline,
					_timeline.GetSnapshotCopy(),
					CopySamples(BaselineSamples),
					captureSamples,
					ConfiguredSettings,
					EffectiveSettings,
					RuntimeStatus,
					Device,
					Camera,
					Render,
					RenderIntegration,
					(PerfMeterAlertSnapshot[])AlertEvents.Clone(),
					AlertEventsTruncated,
					screenshot,
					MemorySnapshotArtifact,
					NativeArtifactSource,
					SelfOverheadWindow,
					SessionSelfOverheadWindow);
			}
		}

		private static PerfMeterSessionSampleSnapshot CopySample(PerfMeterSessionSampleSnapshot sample)
		{
			PerfMeterCustomMetricSnapshot[] customMetrics = sample.CustomMetrics == null ? Array.Empty<PerfMeterCustomMetricSnapshot>() : (PerfMeterCustomMetricSnapshot[])sample.CustomMetrics.Clone();
			return new PerfMeterSessionSampleSnapshot(sample.CollectionFrame, sample.CollectionTimeSeconds, sample.SceneName, sample.Metrics, customMetrics, sample.PlatformTelemetry, sample.GraphicsStateTraceId);
		}

		private static PerfMeterCustomMetricSnapshot[] CopyCustomMetrics(PerfMeterCustomMetricCollection customMetrics)
		{
			if (customMetrics.Count == 0)
			{
				return Array.Empty<PerfMeterCustomMetricSnapshot>();
			}

			PerfMeterCustomMetricSnapshot[] copy = new PerfMeterCustomMetricSnapshot[customMetrics.Count];
			Array.Copy(customMetrics.Buffer, copy, customMetrics.Count);
			return copy;
		}

		private static PerfMeterSessionSampleSnapshot[] CopySamples(PerfMeterSessionSampleSnapshot[] samples)
		{
			if (samples == null || samples.Length == 0)
			{
				return Array.Empty<PerfMeterSessionSampleSnapshot>();
			}

			PerfMeterSessionSampleSnapshot[] copy = new PerfMeterSessionSampleSnapshot[samples.Length];
			for (int i = 0; i < samples.Length; i++)
			{
				copy[i] = CopySample(samples[i]);
			}

			return copy;
		}

		internal static string CombineWarnings(string first, string second)
		{
			if (string.IsNullOrEmpty(first))
			{
				return second ?? string.Empty;
			}

			if (string.IsNullOrEmpty(second) || first.IndexOf(second, StringComparison.Ordinal) >= 0)
			{
				return first;
			}

			return first + " " + second;
		}

		internal static PerfMeterCaptureStatusSnapshot MemoryCaptureStatus(PerfMeterMemorySnapshotStatusSnapshot status)
		{
			PerfMeterCaptureState state;
			switch (status.State)
			{
				case PerfMeterMemorySnapshotState.Capturing:
					state = PerfMeterCaptureState.Capturing;
					break;
				case PerfMeterMemorySnapshotState.Completed:
					state = PerfMeterCaptureState.Completed;
					break;
				case PerfMeterMemorySnapshotState.Unavailable:
					state = PerfMeterCaptureState.Unavailable;
					break;
				case PerfMeterMemorySnapshotState.Error:
					state = PerfMeterCaptureState.Error;
					break;
				default:
					state = PerfMeterCaptureState.Idle;
					break;
			}

			return new PerfMeterCaptureStatusSnapshot(
				status.Availability,
				state,
				status.CaptureId,
				PerfMeterCaptureTool.MemoryProfiler,
				0,
				1,
				0,
				0,
				state == PerfMeterCaptureState.Completed ? 1 : 0,
				0,
				status.Warning);
		}
	}

	internal sealed class PerfMeterCaptureBundleExportData
	{
		internal PerfMeterCaptureBundleExportData(
			PerfMeterCaptureBundleStatusSnapshot status,
			PerfMeterCaptureOptions captureOptions,
			PerfMeterCaptureBundleOptions bundleOptions,
			string startedUtc,
			string completedUtc,
			PerfMeterSessionSummarySnapshot sessionSummary,
			PerfMeterSessionSampleSnapshot[] baselineSamples,
			PerfMeterSessionSampleSnapshot[] captureSamples,
			PerfMeterSettingsSnapshot configuredSettings,
			PerfMeterSettingsSnapshot effectiveSettings,
			PerfMeterStatusSnapshot runtimeStatus,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterAlertSnapshot[] alertEvents,
			bool alertEventsTruncated,
			byte[] screenshotBytes,
			PerfMeterMemorySnapshotArtifact memorySnapshotArtifact,
			PerfMeterNativeExternalArtifactSourceDescriptor nativeArtifactSource = default,
			PerfMeterSelfOverheadWindowSnapshot selfOverheadWindow = default,
			PerfMeterSelfOverheadWindowSnapshot sessionSelfOverheadWindow = default)
			: this(
				status,
				captureOptions,
				bundleOptions,
				startedUtc,
				completedUtc,
				sessionSummary,
				PerfMeterSessionTimelineSnapshot.Empty,
				PerfMeterSessionTimelineSnapshot.Empty,
				baselineSamples,
				captureSamples,
				configuredSettings,
				effectiveSettings,
				runtimeStatus,
				device,
				camera,
				render,
				PerfMeterRenderIntegrationSnapshot.NotObserved,
				alertEvents,
				alertEventsTruncated,
				screenshotBytes,
				memorySnapshotArtifact,
				nativeArtifactSource,
				selfOverheadWindow,
				sessionSelfOverheadWindow)
		{
		}

		internal PerfMeterCaptureBundleExportData(
			PerfMeterCaptureBundleStatusSnapshot status,
			PerfMeterCaptureOptions captureOptions,
			PerfMeterCaptureBundleOptions bundleOptions,
			string startedUtc,
			string completedUtc,
			PerfMeterSessionSummarySnapshot sessionSummary,
			PerfMeterSessionTimelineSnapshot sessionTimeline,
			PerfMeterSessionTimelineSnapshot captureTimeline,
			PerfMeterSessionSampleSnapshot[] baselineSamples,
			PerfMeterSessionSampleSnapshot[] captureSamples,
			PerfMeterSettingsSnapshot configuredSettings,
			PerfMeterSettingsSnapshot effectiveSettings,
			PerfMeterStatusSnapshot runtimeStatus,
			PerfMeterDeviceSnapshot device,
			PerfMeterCameraSnapshot camera,
			PerfMeterRenderGraphSnapshot render,
			PerfMeterRenderIntegrationSnapshot renderIntegration,
			PerfMeterAlertSnapshot[] alertEvents,
			bool alertEventsTruncated,
			byte[] screenshotBytes,
			PerfMeterMemorySnapshotArtifact memorySnapshotArtifact,
			PerfMeterNativeExternalArtifactSourceDescriptor nativeArtifactSource = default,
			PerfMeterSelfOverheadWindowSnapshot selfOverheadWindow = default,
			PerfMeterSelfOverheadWindowSnapshot sessionSelfOverheadWindow = default)
		{
			Status = status;
			CaptureOptions = captureOptions;
			BundleOptions = bundleOptions;
			StartedUtc = startedUtc ?? string.Empty;
			CompletedUtc = completedUtc ?? string.Empty;
			SessionSummary = CopySummary(sessionSummary);
			SessionTimeline = CopyTimeline(sessionTimeline);
			CaptureTimeline = CopyTimeline(captureTimeline);
			BaselineSamples = baselineSamples ?? Array.Empty<PerfMeterSessionSampleSnapshot>();
			CaptureSamples = captureSamples ?? Array.Empty<PerfMeterSessionSampleSnapshot>();
			ConfiguredSettings = configuredSettings;
			EffectiveSettings = effectiveSettings;
			RuntimeStatus = runtimeStatus;
			Device = CopyDevice(device);
			Camera = camera;
			Render = render;
			RenderIntegration = renderIntegration;
			AlertEvents = alertEvents ?? Array.Empty<PerfMeterAlertSnapshot>();
			AlertEventsTruncated = alertEventsTruncated;
			ScreenshotBytes = screenshotBytes;
			MemorySnapshotArtifact = memorySnapshotArtifact;
			NativeArtifactSource = nativeArtifactSource;
			SelfOverheadWindow = selfOverheadWindow.SchemaVersion == 0
				? PerfMeterSelfOverheadWindowSnapshot.Unavailable
				: selfOverheadWindow;
			SessionSelfOverheadWindow = sessionSelfOverheadWindow.SchemaVersion == 0
				? PerfMeterSelfOverheadWindowSnapshot.Unavailable
				: sessionSelfOverheadWindow;
		}

		internal PerfMeterCaptureBundleStatusSnapshot Status { get; }
		internal PerfMeterCaptureOptions CaptureOptions { get; }
		internal PerfMeterCaptureBundleOptions BundleOptions { get; }
		internal string StartedUtc { get; }
		internal string CompletedUtc { get; }
		internal PerfMeterSessionSummarySnapshot SessionSummary { get; }
		internal PerfMeterSessionTimelineSnapshot SessionTimeline { get; }
		internal PerfMeterSessionTimelineSnapshot CaptureTimeline { get; }
		internal PerfMeterSessionSampleSnapshot[] BaselineSamples { get; }
		internal PerfMeterSessionSampleSnapshot[] CaptureSamples { get; }
		internal PerfMeterSettingsSnapshot ConfiguredSettings { get; }
		internal PerfMeterSettingsSnapshot EffectiveSettings { get; }
		internal PerfMeterStatusSnapshot RuntimeStatus { get; }
		internal PerfMeterDeviceSnapshot Device { get; }
		internal PerfMeterCameraSnapshot Camera { get; }
		internal PerfMeterRenderGraphSnapshot Render { get; }
		internal PerfMeterRenderIntegrationSnapshot RenderIntegration { get; }
		internal PerfMeterSelfOverheadWindowSnapshot SelfOverheadWindow { get; }
		internal PerfMeterSelfOverheadWindowSnapshot SessionSelfOverheadWindow { get; }
		internal PerfMeterAlertSnapshot[] AlertEvents { get; }
		internal bool AlertEventsTruncated { get; }
		internal byte[] ScreenshotBytes { get; }
		internal PerfMeterMemorySnapshotArtifact MemorySnapshotArtifact { get; }
		internal PerfMeterNativeExternalArtifactSourceDescriptor NativeArtifactSource { get; }

		private static PerfMeterSessionTimelineSnapshot CopyTimeline(PerfMeterSessionTimelineSnapshot timeline)
		{
			return new PerfMeterSessionTimelineSnapshot(
				timeline.Events ?? Array.Empty<PerfMeterSessionTimelineEventSnapshot>(),
				timeline.DroppedEventCount,
				timeline.IsComplete);
		}

		private static PerfMeterDeviceSnapshot CopyDevice(PerfMeterDeviceSnapshot device)
		{
			PerfMeterDisplaySnapshot[] displays = device.Displays == null || device.Displays.Length == 0
				? Array.Empty<PerfMeterDisplaySnapshot>()
				: (PerfMeterDisplaySnapshot[])device.Displays.Clone();
			return new PerfMeterDeviceSnapshot(
				device.UnityVersion,
				device.ApplicationPlatform,
				device.IsEditor,
				device.OperatingSystem,
				device.DeviceModel,
				device.DeviceType,
				device.ProcessorType,
				device.ProcessorCount,
				device.ProcessorFrequencyMhz,
				device.SystemMemorySizeMb,
				device.GraphicsDeviceType,
				device.GraphicsDeviceName,
				device.GraphicsDeviceVendor,
				device.GraphicsDeviceVersion,
				device.GraphicsMemorySizeMb,
				device.GraphicsShaderLevel,
				device.GraphicsMultiThreaded,
				device.MaxTextureSize,
				device.SupportsComputeShaders,
				device.SupportsAsyncGpuReadback,
				device.SupportsInstancing,
				device.SupportsGraphicsFence,
				device.ScreenWidth,
				device.ScreenHeight,
				device.CurrentResolutionWidth,
				device.CurrentResolutionHeight,
				device.CurrentRefreshRateNumerator,
				device.CurrentRefreshRateDenominator,
				device.CurrentRefreshRateHz,
				device.Dpi,
				device.FullScreen,
				device.FullScreenMode,
				device.MainWindowPositionAvailable,
				device.MainWindowPositionX,
				device.MainWindowPositionY,
				device.DisplayLayoutAvailable,
				device.DisplayLayoutWarning,
				displays,
				device.RenderPipeline,
				device.RenderPipelineAssetName,
				device.RenderPipelineAssetType,
				device.RenderPipelineRuntimeType);
		}

		private static PerfMeterSessionSummarySnapshot CopySummary(PerfMeterSessionSummarySnapshot summary)
		{
			return new PerfMeterSessionSummarySnapshot(
				summary.State,
				summary.Options,
				summary.SampleCount,
				summary.DroppedSampleCount,
				summary.FirstFrame,
				summary.LastFrame,
				summary.StartTimeSeconds,
				summary.StopTimeSeconds,
				summary.DurationSeconds,
				summary.AverageFrameTimeMs,
				summary.MinFrameTimeMs,
				summary.MaxFrameTimeMs,
				summary.AverageFps,
				summary.MinFps,
				summary.MaxFps,
				summary.GpuBoundSampleCount,
				summary.CpuMainThreadBoundSampleCount,
				summary.CpuRenderThreadBoundSampleCount,
				summary.PresentLimitedSampleCount,
				summary.FrameSpikeCount,
				summary.SevereFrameSpikeCount,
				summary.Warning,
				CopyDevice(summary.Device),
				summary.Camera,
				summary.ConfiguredSettings,
				summary.EffectiveSettings,
				summary.StartSceneName,
				summary.LastSceneName,
				summary.WholeRun,
				summary.CurrentScene,
				summary.FocusLossCount,
				summary.PauseCount,
				summary.FocusPausedDurationSeconds,
				summary.SessionId);
		}
	}
}
