using System;
using UnityEngine;

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
		IoError = 7
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
		{
			Availability = availability;
			State = state;
			BundleId = bundleId ?? string.Empty;
			CaptureId = captureId ?? string.Empty;
			CaptureState = captureState;
			RequestedTool = requestedTool;
			BaselineSampleCount = Mathf.Max(0, baselineSampleCount);
			CaptureSampleCount = Mathf.Max(0, captureSampleCount);
			DroppedCaptureSampleCount = Mathf.Max(0, droppedCaptureSampleCount);
			AlertEventCount = Mathf.Max(0, alertEventCount);
			AlertEventsTruncated = alertEventsTruncated;
			ScreenshotState = screenshotState;
			ExternalArtifactState = externalArtifactState;
			CommittedRelativePath = committedRelativePath ?? string.Empty;
			Warning = warning ?? string.Empty;
			MemorySnapshotState = memorySnapshotState;
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
		public PerfMeterCaptureBundleExportResult(bool success, PerfMeterCaptureBundleExportStatus status, string relativePath, string error, PerfMeterCaptureBundleStatusSnapshot bundle)
		{
			Success = success;
			Status = status;
			RelativePath = relativePath ?? string.Empty;
			Error = error ?? string.Empty;
			Bundle = bundle;
		}

		public bool Success { get; }
		public PerfMeterCaptureBundleExportStatus Status { get; }
		public string RelativePath { get; }
		public string Error { get; }
		public PerfMeterCaptureBundleStatusSnapshot Bundle { get; }
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

			_record.CaptureContext(device, camera, render, runtimeStatus);
			_record.Freeze(sessionSummary, baselineSamples, runtimeStatus, Array.Empty<PerfMeterAlertSnapshot>(), false);
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
			if (_record == null || _record.State != PerfMeterCaptureBundleState.Recording)
			{
				return;
			}

			_record.CaptureContext(device, camera, render, runtimeStatus);
			_record.AddCaptureSample(sample);
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
			if (_record == null || !string.Equals(_record.CaptureOptions.CaptureId, captureStatus.CaptureId, StringComparison.Ordinal))
			{
				return;
			}

			_record.CaptureStatus = captureStatus;
			if (captureStatus.State == PerfMeterCaptureState.PreRoll || captureStatus.State == PerfMeterCaptureState.Capturing || captureStatus.State == PerfMeterCaptureState.PostRoll)
			{
				return;
			}

			_record.CaptureContext(device, camera, render, runtimeStatus);
			_record.Freeze(sessionSummary, baselineSamples, runtimeStatus, alerts, alertsTruncated);
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

		internal void MarkExported(string captureId, string relativePath, PerfMeterCaptureExternalArtifactState externalArtifactState)
		{
			if (_record == null || !string.Equals(_record.CaptureOptions.CaptureId, captureId, StringComparison.Ordinal))
			{
				return;
			}

			_record.State = PerfMeterCaptureBundleState.Exported;
			_record.CommittedRelativePath = relativePath ?? string.Empty;
			_record.ExternalArtifactState = externalArtifactState;
		}

		internal void ResetForTests()
		{
			_record = null;
		}

		private sealed class BundleRecord
		{
			private readonly PerfMeterSessionSampleSnapshot[] _captureSamples = new PerfMeterSessionSampleSnapshot[MaxCaptureSamples];
			private int _captureSampleCount;
			private int _droppedCaptureSampleCount;
			private bool _contextCaptured;

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
				State = PerfMeterCaptureBundleState.Recording;
				ScreenshotState = bundleOptions.IncludeScreenshot ? PerfMeterCaptureScreenshotState.Pending : PerfMeterCaptureScreenshotState.NotRequested;
				StartedUtc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
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
			internal PerfMeterMemorySnapshotState MemorySnapshotState { get; set; } = PerfMeterMemorySnapshotState.NotRequested;
			internal PerfMeterMemorySnapshotArtifact MemorySnapshotArtifact { get; set; }
			internal bool ScreenshotStarted { get; set; }
			internal byte[] ScreenshotBytes { get; set; }
			internal string CommittedRelativePath { get; set; }
			internal string Warning { get; set; }
			internal string StartedUtc { get; }
			internal string CompletedUtc { get; private set; }
			internal PerfMeterDeviceSnapshot Device { get; private set; }
			internal PerfMeterCameraSnapshot Camera { get; private set; }
			internal PerfMeterRenderGraphSnapshot Render { get; private set; }
			internal PerfMeterStatusSnapshot RuntimeStatus { get; private set; }
			internal PerfMeterSessionSummarySnapshot SessionSummary { get; private set; }
			internal PerfMeterSessionSampleSnapshot[] BaselineSamples { get; private set; } = Array.Empty<PerfMeterSessionSampleSnapshot>();
			internal PerfMeterAlertSnapshot[] AlertEvents { get; private set; } = Array.Empty<PerfMeterAlertSnapshot>();
			internal bool AlertEventsTruncated { get; private set; }
			internal bool ContextCaptured => _contextCaptured;

			internal void CaptureContext(PerfMeterDeviceSnapshot device, PerfMeterCameraSnapshot camera, PerfMeterRenderGraphSnapshot render, PerfMeterStatusSnapshot runtimeStatus)
			{
				if (_contextCaptured)
				{
					return;
				}

				_contextCaptured = true;
				Device = device;
				Camera = camera;
				Render = render;
				RuntimeStatus = runtimeStatus;
			}

			internal void AddCaptureSample(PerfMeterSessionSampleSnapshot sample)
			{
				if (_captureSampleCount >= _captureSamples.Length)
				{
					_droppedCaptureSampleCount++;
					return;
				}

				_captureSamples[_captureSampleCount++] = CopySample(sample);
			}

			internal void Freeze(PerfMeterSessionSummarySnapshot sessionSummary, PerfMeterSessionSampleSnapshot[] baselineSamples, PerfMeterStatusSnapshot runtimeStatus, PerfMeterAlertSnapshot[] alerts, bool alertsTruncated)
			{
				if (!string.IsNullOrEmpty(CompletedUtc))
				{
					return;
				}

				CompletedUtc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
				SessionSummary = sessionSummary;
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
					MemorySnapshotState);
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
					CopySamples(BaselineSamples),
					captureSamples,
					ConfiguredSettings,
					EffectiveSettings,
					RuntimeStatus,
					Device,
					Camera,
					Render,
					(PerfMeterAlertSnapshot[])AlertEvents.Clone(),
					AlertEventsTruncated,
					screenshot,
					MemorySnapshotArtifact);
			}
		}

		private static PerfMeterSessionSampleSnapshot CopySample(PerfMeterSessionSampleSnapshot sample)
		{
			PerfMeterCustomMetricSnapshot[] customMetrics = sample.CustomMetrics == null ? Array.Empty<PerfMeterCustomMetricSnapshot>() : (PerfMeterCustomMetricSnapshot[])sample.CustomMetrics.Clone();
			return new PerfMeterSessionSampleSnapshot(sample.CollectionFrame, sample.CollectionTimeSeconds, sample.SceneName, sample.Metrics, customMetrics, sample.PlatformTelemetry, sample.GraphicsStateTraceId);
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

		private static string CombineWarnings(string first, string second)
		{
			if (string.IsNullOrEmpty(first))
			{
				return second ?? string.Empty;
			}

			return string.IsNullOrEmpty(second) ? first : first + " " + second;
		}

		private static PerfMeterCaptureStatusSnapshot MemoryCaptureStatus(PerfMeterMemorySnapshotStatusSnapshot status)
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
			PerfMeterMemorySnapshotArtifact memorySnapshotArtifact)
		{
			Status = status;
			CaptureOptions = captureOptions;
			BundleOptions = bundleOptions;
			StartedUtc = startedUtc ?? string.Empty;
			CompletedUtc = completedUtc ?? string.Empty;
			SessionSummary = sessionSummary;
			BaselineSamples = baselineSamples ?? Array.Empty<PerfMeterSessionSampleSnapshot>();
			CaptureSamples = captureSamples ?? Array.Empty<PerfMeterSessionSampleSnapshot>();
			ConfiguredSettings = configuredSettings;
			EffectiveSettings = effectiveSettings;
			RuntimeStatus = runtimeStatus;
			Device = device;
			Camera = camera;
			Render = render;
			AlertEvents = alertEvents ?? Array.Empty<PerfMeterAlertSnapshot>();
			AlertEventsTruncated = alertEventsTruncated;
			ScreenshotBytes = screenshotBytes;
			MemorySnapshotArtifact = memorySnapshotArtifact;
		}

		internal PerfMeterCaptureBundleStatusSnapshot Status { get; }
		internal PerfMeterCaptureOptions CaptureOptions { get; }
		internal PerfMeterCaptureBundleOptions BundleOptions { get; }
		internal string StartedUtc { get; }
		internal string CompletedUtc { get; }
		internal PerfMeterSessionSummarySnapshot SessionSummary { get; }
		internal PerfMeterSessionSampleSnapshot[] BaselineSamples { get; }
		internal PerfMeterSessionSampleSnapshot[] CaptureSamples { get; }
		internal PerfMeterSettingsSnapshot ConfiguredSettings { get; }
		internal PerfMeterSettingsSnapshot EffectiveSettings { get; }
		internal PerfMeterStatusSnapshot RuntimeStatus { get; }
		internal PerfMeterDeviceSnapshot Device { get; }
		internal PerfMeterCameraSnapshot Camera { get; }
		internal PerfMeterRenderGraphSnapshot Render { get; }
		internal PerfMeterAlertSnapshot[] AlertEvents { get; }
		internal bool AlertEventsTruncated { get; }
		internal byte[] ScreenshotBytes { get; }
		internal PerfMeterMemorySnapshotArtifact MemorySnapshotArtifact { get; }
	}
}
