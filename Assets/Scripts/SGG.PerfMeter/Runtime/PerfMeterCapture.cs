using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	public enum PerfMeterCaptureTool
	{
		Unknown = 0,
		RenderDoc = 1,
		Pix = 2,
		MemoryProfiler = 3
	}

	public enum PerfMeterCaptureBackendMode
	{
		GenericUnity = 0,
		NativePreferred = 1,
		NativeRequired = 2
	}

	public enum PerfMeterCaptureBackendKind
	{
		GenericUnity = 0,
		RenderDocNative = 1
	}

	public enum PerfMeterRenderDocCapturePhase
	{
		None = 0,
		Preflight = 1,
		BeginScheduled = 2,
		BeginExecuted = 3,
		EndScheduled = 4,
		EndExecuted = 5,
		AwaitingArtifact = 6,
		FinalizingArtifact = 7,
		Completed = 8,
		Failed = 9,
		LostSession = 10
	}

	public enum PerfMeterCaptureState
	{
		Idle = 0,
		PreRoll = 1,
		Capturing = 2,
		PostRoll = 3,
		Completed = 4,
		Canceled = 5,
		Unavailable = 6,
		Error = 7
	}

	public enum PerfMeterCaptureRequestResult
	{
		Started = 0,
		AlreadyActive = 1,
		RejectedOverlap = 2,
		Unavailable = 3,
		InvalidRequest = 4,
		Failed = 5
	}

	public readonly struct PerfMeterCaptureOptions
	{
		public PerfMeterCaptureOptions(
			string captureId,
			PerfMeterCaptureTool tool,
			int captureFrames = 1,
			int preRollFrames = 0,
			int postRollFrames = 0)
			: this(captureId, tool, captureFrames, preRollFrames, postRollFrames, PerfMeterCaptureBackendMode.GenericUnity)
		{
		}

		public PerfMeterCaptureOptions(
			string captureId,
			PerfMeterCaptureTool tool,
			int captureFrames,
			int preRollFrames,
			int postRollFrames,
			PerfMeterCaptureBackendMode backendMode)
			: this(
				captureId,
				tool,
				captureFrames,
				preRollFrames,
				postRollFrames,
				backendMode,
				PerfMeterExternalArtifactStorageMode.MetadataOnly)
		{
		}

		public PerfMeterCaptureOptions(
			string captureId,
			PerfMeterCaptureTool tool,
			int captureFrames,
			int preRollFrames,
			int postRollFrames,
			PerfMeterCaptureBackendMode backendMode,
			PerfMeterExternalArtifactStorageMode externalArtifactStorageMode)
		{
			CaptureId = captureId ?? string.Empty;
			Tool = tool;
			CaptureFrames = Mathf.Max(1, captureFrames);
			PreRollFrames = Mathf.Max(0, preRollFrames);
			PostRollFrames = Mathf.Max(0, postRollFrames);
			BackendMode = backendMode;
			ExternalArtifactStorageMode = externalArtifactStorageMode;
		}

		public string CaptureId { get; }
		public PerfMeterCaptureTool Tool { get; }
		public int CaptureFrames { get; }
		public int PreRollFrames { get; }
		public int PostRollFrames { get; }
		public PerfMeterCaptureBackendMode BackendMode { get; }
		public PerfMeterExternalArtifactStorageMode ExternalArtifactStorageMode { get; }

		internal bool IsValidBackendMode => Enum.IsDefined(typeof(PerfMeterCaptureBackendMode), BackendMode);
		internal bool IsValidExternalArtifactStorageMode => Enum.IsDefined(
			typeof(PerfMeterExternalArtifactStorageMode),
			ExternalArtifactStorageMode);
	}

	public readonly struct PerfMeterCaptureStatusSnapshot
	{
		public PerfMeterCaptureStatusSnapshot(
			PerfMeterAvailability availability,
			PerfMeterCaptureState state,
			string captureId,
			PerfMeterCaptureTool tool,
			int requestedPreRollFrames,
			int requestedCaptureFrames,
			int requestedPostRollFrames,
			int completedPreRollFrames,
			int completedCaptureFrames,
			int completedPostRollFrames,
			string warning)
			: this(
				availability,
				state,
				captureId,
				tool,
				requestedPreRollFrames,
				requestedCaptureFrames,
				requestedPostRollFrames,
				completedPreRollFrames,
				completedCaptureFrames,
				completedPostRollFrames,
				warning,
				PerfMeterCaptureBackendMode.GenericUnity,
				PerfMeterCaptureBackendKind.GenericUnity,
				PerfMeterRenderDocCapturePhase.None,
				-1,
				string.Empty)
		{
		}

		public PerfMeterCaptureStatusSnapshot(
			PerfMeterAvailability availability,
			PerfMeterCaptureState state,
			string captureId,
			PerfMeterCaptureTool tool,
			int requestedPreRollFrames,
			int requestedCaptureFrames,
			int requestedPostRollFrames,
			int completedPreRollFrames,
			int completedCaptureFrames,
			int completedPostRollFrames,
			string warning,
			PerfMeterCaptureBackendMode requestedBackendMode,
			PerfMeterCaptureBackendKind effectiveBackendKind,
			PerfMeterRenderDocCapturePhase nativePhase,
			int nativeResultCode,
			string fallbackReason)
		{
			Availability = availability;
			State = state;
			CaptureId = captureId ?? string.Empty;
			Tool = tool;
			RequestedPreRollFrames = Mathf.Max(0, requestedPreRollFrames);
			RequestedCaptureFrames = Mathf.Max(0, requestedCaptureFrames);
			RequestedPostRollFrames = Mathf.Max(0, requestedPostRollFrames);
			CompletedPreRollFrames = Mathf.Clamp(completedPreRollFrames, 0, RequestedPreRollFrames);
			CompletedCaptureFrames = Mathf.Clamp(completedCaptureFrames, 0, RequestedCaptureFrames);
			CompletedPostRollFrames = Mathf.Clamp(completedPostRollFrames, 0, RequestedPostRollFrames);
			Warning = warning ?? string.Empty;
			RequestedBackendMode = requestedBackendMode;
			EffectiveBackendKind = effectiveBackendKind;
			NativePhase = nativePhase;
			NativeResultCode = nativeResultCode;
			FallbackReason = fallbackReason ?? string.Empty;
		}

		public static PerfMeterCaptureStatusSnapshot NotRunning => new PerfMeterCaptureStatusSnapshot(
			PerfMeterAvailability.Unknown,
			PerfMeterCaptureState.Idle,
			string.Empty,
			PerfMeterCaptureTool.Unknown,
			0,
			0,
			0,
			0,
			0,
			0,
			"Capture coordinator is not running.",
			PerfMeterCaptureBackendMode.GenericUnity,
			PerfMeterCaptureBackendKind.GenericUnity,
			PerfMeterRenderDocCapturePhase.None,
			-1,
			string.Empty);

		public bool IsActive => State == PerfMeterCaptureState.PreRoll || State == PerfMeterCaptureState.Capturing || State == PerfMeterCaptureState.PostRoll;
		public PerfMeterAvailability Availability { get; }
		public PerfMeterCaptureState State { get; }
		public string CaptureId { get; }
		public PerfMeterCaptureTool Tool { get; }
		public int RequestedPreRollFrames { get; }
		public int RequestedCaptureFrames { get; }
		public int RequestedPostRollFrames { get; }
		public int CompletedPreRollFrames { get; }
		public int CompletedCaptureFrames { get; }
		public int CompletedPostRollFrames { get; }
		public string Warning { get; }
		public PerfMeterCaptureBackendMode RequestedBackendMode { get; }
		public PerfMeterCaptureBackendKind EffectiveBackendKind { get; }
		public PerfMeterRenderDocCapturePhase NativePhase { get; }
		public int NativeResultCode { get; }
		public string FallbackReason { get; }
	}

	internal readonly struct PerfMeterCaptureBackendCapability
	{
		internal PerfMeterCaptureBackendCapability(PerfMeterAvailability availability, string warning)
		{
			Availability = availability;
			Warning = warning ?? string.Empty;
		}

		internal PerfMeterAvailability Availability { get; }
		internal string Warning { get; }
	}

	internal interface IPerfMeterCaptureBackend
	{
		PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool);
		bool TryBegin(PerfMeterCaptureTool tool, out string error);
		bool TryEnd(out string error);
	}

	internal readonly struct PerfMeterCaptureBackendV2Snapshot
	{
		internal PerfMeterCaptureBackendV2Snapshot(
			PerfMeterAvailability availability,
			string warning,
			PerfMeterCaptureBackendKind effectiveBackendKind,
			PerfMeterRenderDocCapturePhase nativePhase,
			int nativeResultCode,
			string fallbackReason,
			bool requiresEndOfFrame,
			bool hasPendingCompletion,
			bool hasActiveResources)
		{
			Availability = availability;
			Warning = warning ?? string.Empty;
			EffectiveBackendKind = effectiveBackendKind;
			NativePhase = nativePhase;
			NativeResultCode = nativeResultCode;
			FallbackReason = fallbackReason ?? string.Empty;
			RequiresEndOfFrame = requiresEndOfFrame;
			HasPendingCompletion = hasPendingCompletion;
			HasActiveResources = hasActiveResources || hasPendingCompletion;
		}

		internal static PerfMeterCaptureBackendV2Snapshot Generic(PerfMeterAvailability availability, string warning)
		{
			return new PerfMeterCaptureBackendV2Snapshot(
				availability,
				warning,
				PerfMeterCaptureBackendKind.GenericUnity,
				PerfMeterRenderDocCapturePhase.None,
				-1,
				string.Empty,
				false,
				false,
				false);
		}

		internal PerfMeterAvailability Availability { get; }
		internal string Warning { get; }
		internal PerfMeterCaptureBackendKind EffectiveBackendKind { get; }
		internal PerfMeterRenderDocCapturePhase NativePhase { get; }
		internal int NativeResultCode { get; }
		internal string FallbackReason { get; }
		internal bool RequiresEndOfFrame { get; }
		internal bool HasPendingCompletion { get; }
		internal bool HasActiveResources { get; }
	}

	internal interface IPerfMeterCaptureBackendV2
	{
		PerfMeterCaptureBackendV2Snapshot GetCapability(PerfMeterCaptureOptions options);
		bool TryBegin(PerfMeterCaptureOptions options, out string error);
		bool ScheduleEnd(out string error);
		bool TryDiscard(out string error);
		void Tick();
		PerfMeterCaptureBackendV2Snapshot Snapshot { get; }
	}

	internal enum PerfMeterCaptureBackendBeginResult
	{
		Failed = 0,
		Pending = 1,
		Started = 2
	}

	internal enum PerfMeterNativeExternalArtifactSourceKind
	{
		None = 0,
		RenderDoc = 1
	}

	internal interface IPerfMeterNativeExternalArtifactPayloadSource
	{
		bool TryValidate(Func<bool> shouldStop, out string error);
		bool TryStageEmbed(
			string stagingPath,
			long additionalStagingBytes,
			Func<bool> shouldStop,
			out PerfMeterNativeEmbeddedArtifact stagedArtifact,
			out string error);
		bool TryCompleteEmbed(bool committed, out string warning);
	}

	internal readonly struct PerfMeterNativeEmbeddedArtifact
	{
		internal PerfMeterNativeEmbeddedArtifact(
			string payloadRelativePath,
			long payloadSizeBytes,
			string payloadSha256,
			string markerRelativePath,
			long markerSizeBytes,
			string markerSha256)
		{
			PayloadRelativePath = payloadRelativePath ?? string.Empty;
			PayloadSizeBytes = payloadSizeBytes;
			PayloadSha256 = payloadSha256 ?? string.Empty;
			MarkerRelativePath = markerRelativePath ?? string.Empty;
			MarkerSizeBytes = markerSizeBytes;
			MarkerSha256 = markerSha256 ?? string.Empty;
		}

		internal string PayloadRelativePath { get; }
		internal long PayloadSizeBytes { get; }
		internal string PayloadSha256 { get; }
		internal string MarkerRelativePath { get; }
		internal long MarkerSizeBytes { get; }
		internal string MarkerSha256 { get; }
	}

	internal readonly struct PerfMeterNativeExternalArtifactSourceDescriptor
	{
		internal const string EmbeddedPayloadRelativePath = "external/renderdoc/capture.rdc";
		internal const string EmbeddedMarkerRelativePath = ".sgg-perfmeter-renderdoc";
		internal const string EmbeddedMarkerHeader = "sgg.perfmeter.renderdoc-embed\n1\n";
		private const long NativePayloadQuotaBytes = 512L * 1024L * 1024L;
		private const PerfMeterExternalArtifactPrivacyFlags NativePrivacyFlags =
			PerfMeterExternalArtifactPrivacyFlags.ContainsGpuCaptureData |
			PerfMeterExternalArtifactPrivacyFlags.Sensitive |
			PerfMeterExternalArtifactPrivacyFlags.RequiresReview;

		internal PerfMeterNativeExternalArtifactSourceDescriptor(
			PerfMeterNativeExternalArtifactSourceKind kind,
			uint bridgeAbiMajor,
			uint bridgeAbiMinor,
			uint appApiMajor,
			uint appApiMinor,
			uint appApiPatch,
			string boundaryMode,
			string targetMode,
			ulong generation,
			ulong requestNonce,
			uint countBefore,
			ulong startUnixNanoseconds,
			uint captureIndex,
			ulong renderDocTimestampSeconds,
			ulong observedUnixNanoseconds,
			IPerfMeterNativeExternalArtifactPayloadSource payloadSource = null)
		{
			Kind = kind;
			BridgeAbiMajor = bridgeAbiMajor;
			BridgeAbiMinor = bridgeAbiMinor;
			AppApiMajor = appApiMajor;
			AppApiMinor = appApiMinor;
			AppApiPatch = appApiPatch;
			BoundaryMode = boundaryMode ?? string.Empty;
			TargetMode = targetMode ?? string.Empty;
			Generation = generation;
			RequestNonce = requestNonce;
			CountBefore = countBefore;
			StartUnixNanoseconds = startUnixNanoseconds;
			CaptureIndex = captureIndex;
			RenderDocTimestampSeconds = renderDocTimestampSeconds;
			ObservedUnixNanoseconds = observedUnixNanoseconds;
			PayloadSource = payloadSource;
		}

		internal bool IsAvailable => Kind == PerfMeterNativeExternalArtifactSourceKind.RenderDoc;
		internal PerfMeterNativeExternalArtifactSourceKind Kind { get; }
		internal uint BridgeAbiMajor { get; }
		internal uint BridgeAbiMinor { get; }
		internal uint AppApiMajor { get; }
		internal uint AppApiMinor { get; }
		internal uint AppApiPatch { get; }
		internal string BoundaryMode { get; }
		internal string TargetMode { get; }
		internal ulong Generation { get; }
		internal ulong RequestNonce { get; }
		internal uint CountBefore { get; }
		internal ulong StartUnixNanoseconds { get; }
		internal uint CaptureIndex { get; }
		internal ulong RenderDocTimestampSeconds { get; }
		internal ulong ObservedUnixNanoseconds { get; }
		private IPerfMeterNativeExternalArtifactPayloadSource PayloadSource { get; }

		internal bool IsStructurallyValid(
			string captureId,
			PerfMeterExternalArtifactSnapshot artifact)
		{
			if (!IsAvailable ||
				BridgeAbiMajor != 1u ||
				AppApiMajor != 1u ||
				AppApiMinor < 4u ||
				!string.Equals(BoundaryMode, "managed_end_of_frame", StringComparison.Ordinal) ||
				!string.Equals(TargetMode, "wildcard_device_window", StringComparison.Ordinal) ||
				RequestNonce == 0u ||
				CaptureIndex < CountBefore ||
				StartUnixNanoseconds == 0u ||
				ObservedUnixNanoseconds < StartUnixNanoseconds ||
				artifact.ArtifactKind != PerfMeterExternalArtifactKind.GpuCapture ||
				!string.Equals(artifact.ToolId, "renderdoc", StringComparison.Ordinal) ||
				!string.Equals(artifact.RequestId, captureId, StringComparison.Ordinal) ||
				artifact.AssociationState != PerfMeterExternalArtifactAssociationState.BridgeAuthenticated ||
				!artifact.IsFinalized ||
				artifact.ContainsGpuCaptureData != PerfMeterExternalArtifactContentState.Present ||
				artifact.SizeBytes <= 0L ||
				artifact.QuotaBytes != NativePayloadQuotaBytes ||
				artifact.SizeBytes > artifact.QuotaBytes ||
				artifact.PrivacyFlags != NativePrivacyFlags ||
				!IsSha256(artifact.ObservedSourceSha256) ||
				!IsSha256(artifact.SourceFileIdentitySha256))
			{
				return false;
			}

			ulong startSeconds = StartUnixNanoseconds / 1000000000u;
			ulong earliest = startSeconds > 5u ? startSeconds - 5u : 0u;
			ulong latest = startSeconds > ulong.MaxValue - 30u ? ulong.MaxValue : startSeconds + 30u;
			if (RenderDocTimestampSeconds < earliest || RenderDocTimestampSeconds > latest)
			{
				return false;
			}

			if (artifact.StorageMode == PerfMeterExternalArtifactStorageMode.MetadataOnly)
			{
				return artifact.IsAuthoritative &&
					PayloadSource == null &&
					string.IsNullOrEmpty(artifact.PostCopySha256) &&
					artifact.SharePolicy == PerfMeterExternalArtifactSharePolicy.DoNotShare;
			}

			if (artifact.StorageMode == PerfMeterExternalArtifactStorageMode.Copy)
			{
				return artifact.IsAuthoritative &&
					PayloadSource != null &&
					IsSha256(artifact.PostCopySha256) &&
					string.Equals(artifact.ObservedSourceSha256, artifact.PostCopySha256, StringComparison.Ordinal) &&
					artifact.SharePolicy == PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare;
			}

			return artifact.StorageMode == PerfMeterExternalArtifactStorageMode.Embed &&
				artifact.AuthorityState == PerfMeterExternalArtifactAuthorityState.Observed &&
				!artifact.IsAuthoritative &&
				PayloadSource != null &&
				string.IsNullOrEmpty(artifact.PostCopySha256) &&
				artifact.SharePolicy == PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare;
		}

		internal bool CanExportAuthoritatively(
			string captureId,
			PerfMeterExternalArtifactSnapshot artifact)
		{
			return IsStructurallyValid(captureId, artifact) &&
				(artifact.IsAuthoritative || artifact.StorageMode == PerfMeterExternalArtifactStorageMode.Embed);
		}

		internal bool TryValidatePayload(Func<bool> shouldStop, out string error)
		{
			if (PayloadSource == null)
			{
				error = string.Empty;
				return true;
			}

			try
			{
				return PayloadSource.TryValidate(shouldStop, out error);
			}
			catch (Exception exception)
			{
				error = "native_external_artifact_payload_validation_exception: " + exception.GetType().Name;
				return false;
			}
		}

		internal bool TryStageEmbed(
			string stagingPath,
			long additionalStagingBytes,
			Func<bool> shouldStop,
			out PerfMeterNativeEmbeddedArtifact stagedArtifact,
			out string error)
		{
			stagedArtifact = default;
			if (PayloadSource == null)
			{
				error = "native_embed_payload_source_unavailable";
				return false;
			}

			try
			{
				return PayloadSource.TryStageEmbed(stagingPath, additionalStagingBytes, shouldStop, out stagedArtifact, out error);
			}
			catch (Exception exception)
			{
				error = "native_embed_staging_exception: " + exception.GetType().Name;
				return false;
			}
		}

		internal bool TryCreateEmbeddedArtifactSnapshot(
			string captureId,
			PerfMeterExternalArtifactSnapshot artifact,
			PerfMeterNativeEmbeddedArtifact stagedArtifact,
			out PerfMeterExternalArtifactSnapshot embeddedArtifact)
		{
			embeddedArtifact = PerfMeterExternalArtifactSnapshot.Empty;
			if (!IsStructurallyValid(captureId, artifact) ||
				artifact.StorageMode != PerfMeterExternalArtifactStorageMode.Embed ||
				!string.Equals(stagedArtifact.PayloadRelativePath, EmbeddedPayloadRelativePath, StringComparison.Ordinal) ||
				stagedArtifact.PayloadSizeBytes != artifact.SizeBytes ||
				!IsSha256(stagedArtifact.PayloadSha256) ||
				!string.Equals(stagedArtifact.PayloadSha256, artifact.ObservedSourceSha256, StringComparison.Ordinal) ||
				!string.Equals(stagedArtifact.MarkerRelativePath, EmbeddedMarkerRelativePath, StringComparison.Ordinal) ||
				stagedArtifact.MarkerSizeBytes <= 0L ||
				stagedArtifact.MarkerSizeBytes > 64L * 1024L ||
				!IsSha256(stagedArtifact.MarkerSha256))
			{
				return false;
			}

			embeddedArtifact = new PerfMeterExternalArtifactOptions(
				artifact.ArtifactId,
				artifact.ArtifactKind,
				artifact.ToolId,
				artifact.ToolVersion,
				artifact.RequestId,
				artifact.HostNamespace,
				artifact.AssociationState,
				artifact.FinalizationState,
				PerfMeterExternalArtifactAuthorityState.Authenticated,
				artifact.ContainsGpuCaptureData,
				artifact.PrivacyFlags,
				artifact.StorageMode,
				artifact.QuotaBytes,
				artifact.SharePolicy,
				artifact.SizeBytes,
				artifact.ObservedSourceSha256,
				stagedArtifact.PayloadSha256,
				artifact.Warning)
				.WithSourceFileIdentitySha256(artifact.SourceFileIdentitySha256)
				.ToSnapshot();
			return true;
		}

		internal bool TryCompleteEmbed(bool committed, out string warning)
		{
			warning = string.Empty;
			if (PayloadSource == null)
			{
				return true;
			}

			try
			{
				return PayloadSource.TryCompleteEmbed(committed, out warning);
			}
			catch (Exception exception)
			{
				warning = "native_embed_completion_exception: " + exception.GetType().Name;
				return false;
			}
		}

		private static bool IsSha256(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Length != 64)
			{
				return false;
			}

			for (int index = 0; index < value.Length; index++)
			{
				char character = value[index];
				if (!((character >= '0' && character <= '9') ||
					(character >= 'a' && character <= 'f')))
				{
					return false;
				}
			}

			return true;
		}
	}

	internal readonly struct PerfMeterCaptureExternalArtifactCompletion
	{
		internal PerfMeterCaptureExternalArtifactCompletion(
			string captureId,
			int generation,
			PerfMeterExternalArtifactSnapshot artifact,
			string retainedPayloadPath)
			: this(captureId, generation, artifact, retainedPayloadPath, default)
		{
		}

		internal PerfMeterCaptureExternalArtifactCompletion(
			string captureId,
			int generation,
			PerfMeterExternalArtifactSnapshot artifact,
			string retainedPayloadPath,
			PerfMeterNativeExternalArtifactSourceDescriptor sourceDescriptor)
		{
			CaptureId = captureId ?? string.Empty;
			Generation = generation;
			Artifact = artifact;
			RetainedPayloadPath = retainedPayloadPath ?? string.Empty;
			SourceDescriptor = sourceDescriptor;
		}

		internal string CaptureId { get; }
		internal int Generation { get; }
		internal PerfMeterExternalArtifactSnapshot Artifact { get; }
		internal string RetainedPayloadPath { get; }
		internal PerfMeterNativeExternalArtifactSourceDescriptor SourceDescriptor { get; }
	}

	internal interface IPerfMeterCaptureBackendV3 : IPerfMeterCaptureBackendV2
	{
		PerfMeterCaptureBackendBeginResult TryBegin(
			PerfMeterCaptureOptions options,
			int generation,
			out string error);
		bool TryConsumeExternalArtifact(out PerfMeterCaptureExternalArtifactCompletion completion);
	}

	internal static class PerfMeterNativeCaptureBackendRegistry
	{
		private static IPerfMeterCaptureBackendV2 _backend;

		internal static void Register(IPerfMeterCaptureBackendV2 backend)
		{
			if (backend == null)
			{
				throw new ArgumentNullException(nameof(backend));
			}

			if (_backend != null && !ReferenceEquals(_backend, backend))
			{
				throw new InvalidOperationException("Only one native capture backend may be registered.");
			}

			_backend = backend;
		}

		internal static void Unregister(IPerfMeterCaptureBackendV2 backend)
		{
			if (ReferenceEquals(_backend, backend))
			{
				_backend = null;
			}
		}

		internal static bool TryGet(out IPerfMeterCaptureBackendV2 backend)
		{
			backend = _backend;
			return backend != null;
		}

		internal static void ResetForTests()
		{
			_backend = null;
		}
	}

	internal interface IPerfMeterCaptureScope
	{
		bool TryBegin(string captureId);
		bool TryEnd(string captureId);
	}

	internal sealed class PerfMeterCaptureCoordinator
	{
		private readonly IPerfMeterCaptureBackend _backend;
		private readonly IPerfMeterCaptureBackendV2 _backendV2;
		private readonly IPerfMeterCaptureBackendV3 _backendV3;
		private readonly IPerfMeterCaptureScope _scope;
		private PerfMeterCaptureOptions _options;
		private PerfMeterCaptureState _state;
		private PerfMeterAvailability _availability;
		private int _completedPreRollFrames;
		private int _completedCaptureFrames;
		private int _completedPostRollFrames;
		private string _warning = string.Empty;
		private bool _backendActive;
		private bool _scopeActive;
		private bool _endScheduled;
		private bool _endExecuted;
		private bool _cleanupAccepted;
		private bool _beginPending;
		private int _generation;

		internal PerfMeterCaptureCoordinator(IPerfMeterCaptureBackend backend, IPerfMeterCaptureScope scope)
		{
			_backend = backend ?? throw new ArgumentNullException(nameof(backend));
			_backendV2 = backend as IPerfMeterCaptureBackendV2;
			_backendV3 = backend as IPerfMeterCaptureBackendV3;
			_scope = scope ?? throw new ArgumentNullException(nameof(scope));
			SetState(PerfMeterCaptureState.Idle, PerfMeterAvailability.Unknown, string.Empty);
		}

		internal PerfMeterCaptureCoordinator(IPerfMeterCaptureBackendV2 backend, IPerfMeterCaptureScope scope)
		{
			_backendV2 = backend ?? throw new ArgumentNullException(nameof(backend));
			_backendV3 = backend as IPerfMeterCaptureBackendV3;
			_scope = scope ?? throw new ArgumentNullException(nameof(scope));
			SetState(PerfMeterCaptureState.Idle, PerfMeterAvailability.Unknown, string.Empty);
		}

		internal PerfMeterCaptureStatusSnapshot Status
		{
			get
			{
				PerfMeterCaptureBackendV2Snapshot backendSnapshot = _state == PerfMeterCaptureState.Idle
					? PerfMeterCaptureBackendV2Snapshot.Generic(_availability, _warning)
					: GetBackendSnapshot();
				PerfMeterRenderDocCapturePhase nativePhase = backendSnapshot.NativePhase;
				if (_endScheduled && !_endExecuted && backendSnapshot.EffectiveBackendKind == PerfMeterCaptureBackendKind.RenderDocNative)
				{
					nativePhase = PerfMeterRenderDocCapturePhase.EndScheduled;
				}
				else if (_endExecuted && backendSnapshot.EffectiveBackendKind == PerfMeterCaptureBackendKind.RenderDocNative &&
					backendSnapshot.HasPendingCompletion && nativePhase < PerfMeterRenderDocCapturePhase.AwaitingArtifact)
				{
					nativePhase = PerfMeterRenderDocCapturePhase.AwaitingArtifact;
				}

				return new PerfMeterCaptureStatusSnapshot(
					_availability,
					_state,
					_options.CaptureId,
					_options.Tool,
					_options.PreRollFrames,
					_options.CaptureFrames,
					_options.PostRollFrames,
					_completedPreRollFrames,
					_completedCaptureFrames,
					_completedPostRollFrames,
					_warning,
					_options.BackendMode,
					backendSnapshot.EffectiveBackendKind,
					nativePhase,
					backendSnapshot.NativeResultCode,
					backendSnapshot.FallbackReason);
			}
		}
		internal bool ScopeActive => _scopeActive;
		internal bool HasActiveResources => IsActiveState(_state) || _backendActive || _scopeActive;
		internal int Generation => _generation;
		internal bool RequiresEndOfFrame => _backendV2 != null && _backendActive && GetBackendSnapshot().RequiresEndOfFrame;
		internal bool EndOfFramePending => _backendV2 != null && _state == PerfMeterCaptureState.Capturing && _endScheduled && !_endExecuted && _backendActive && GetBackendSnapshot().RequiresEndOfFrame;
		internal bool HasPendingCompletion => _backendV2 != null && GetBackendSnapshot().HasPendingCompletion;

		internal PerfMeterCaptureRequestResult Request(PerfMeterCaptureOptions options)
		{
			using (PerfMeterProfilerInstrumentation.CaptureCoordinatorMarker.Auto())
			{
				if (string.IsNullOrEmpty(options.CaptureId) ||
					options.Tool == PerfMeterCaptureTool.Unknown ||
					!options.IsValidBackendMode ||
					!options.IsValidExternalArtifactStorageMode)
				{
					return PerfMeterCaptureRequestResult.InvalidRequest;
				}

				if (IsActiveState(_state))
				{
					return string.Equals(_options.CaptureId, options.CaptureId, StringComparison.Ordinal)
						? PerfMeterCaptureRequestResult.AlreadyActive
						: PerfMeterCaptureRequestResult.RejectedOverlap;
				}

				if (_backendActive || _scopeActive)
				{
					return PerfMeterCaptureRequestResult.RejectedOverlap;
				}

				SetRequest(options);
				PerfMeterCaptureBackendCapability capability;
				try
				{
					if (_backendV2 != null)
					{
						PerfMeterCaptureBackendV2Snapshot backendSnapshot = _backendV2.GetCapability(options);
						capability = new PerfMeterCaptureBackendCapability(backendSnapshot.Availability, backendSnapshot.Warning);
					}
					else
					{
						capability = _backend.GetCapability(options.Tool);
					}
				}
				catch (Exception exception)
				{
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, FormatException(exception));
					return PerfMeterCaptureRequestResult.Failed;
				}

				if (capability.Availability != PerfMeterAvailability.Available)
				{
					SetState(PerfMeterCaptureState.Unavailable, PerfMeterAvailability.Unavailable, capability.Warning);
					return PerfMeterCaptureRequestResult.Unavailable;
				}

				SetState(PerfMeterCaptureState.PreRoll, PerfMeterAvailability.Available, capability.Warning);
				if (_options.PreRollFrames == 0 && TryBeginCapture() == PerfMeterCaptureBackendBeginResult.Failed)
				{
					return PerfMeterCaptureRequestResult.Failed;
				}

				return PerfMeterCaptureRequestResult.Started;
			}
		}

		internal void Tick()
		{
			using (PerfMeterProfilerInstrumentation.CaptureCoordinatorMarker.Auto())
			{
				TickBackend();
				if (_beginPending)
				{
					PerfMeterCaptureBackendBeginResult beginResult = TryBeginCapture();
					if (beginResult != PerfMeterCaptureBackendBeginResult.Started)
					{
						CompleteBackendIfReady();
						return;
					}
					return;
				}
				CompleteBackendIfReady();
				if (_endScheduled && !_endExecuted)
				{
					return;
				}

				switch (_state)
				{
					case PerfMeterCaptureState.PreRoll:
						if (_beginPending)
						{
							break;
						}
						_completedPreRollFrames++;
						if (_completedPreRollFrames >= _options.PreRollFrames)
						{
							TryBeginCapture();
						}
						break;
					case PerfMeterCaptureState.Capturing:
						_completedCaptureFrames++;
						if (_completedCaptureFrames >= _options.CaptureFrames)
						{
							TryEndCapture();
						}
						break;
					case PerfMeterCaptureState.PostRoll:
						_completedPostRollFrames++;
						if (_completedPostRollFrames >= _options.PostRollFrames)
						{
							SetState(PerfMeterCaptureState.Completed, PerfMeterAvailability.Available, string.Empty);
						}
						break;
				}
			}
		}

		internal bool TickAtEndOfFrame(int generation)
		{
			using (PerfMeterProfilerInstrumentation.CaptureCoordinatorMarker.Auto())
			{
				if (generation != _generation || _state != PerfMeterCaptureState.Capturing || _backendV2 == null || !_backendActive || !_endScheduled || _endExecuted)
				{
					return false;
				}

				if (!EndOfFramePending)
				{
					return false;
				}

				if (!TryExecuteScheduledEnd(out string error))
				{
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, error);
					return false;
				}

				FinishCaptureEnd();
				return true;
			}
		}

		internal bool Cancel(string captureId)
		{
			using (PerfMeterProfilerInstrumentation.CaptureCoordinatorMarker.Auto())
			{
				if ((!IsActiveState(_state) && !_backendActive && !_scopeActive) || !string.Equals(_options.CaptureId, captureId, StringComparison.Ordinal))
				{
					return false;
				}

				AdvanceGeneration();
				_beginPending = false;
				_endScheduled = false;
				if (_cleanupAccepted)
				{
					if (!TryAdvanceAcceptedCleanup(out string pendingError))
					{
						SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, pendingError);
						return false;
					}

					if (!TryReleaseCaptureResources(out string scopeError))
					{
						SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, scopeError);
						return false;
					}
				}
				else
				{
					_endExecuted = false;
					if (!TryReleaseCaptureResources(out string error))
					{
						SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, error);
						return false;
					}
				}

				if (HasTerminalBackendFailure())
				{
					PerfMeterCaptureBackendV2Snapshot backendSnapshot = GetBackendSnapshot();
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, backendSnapshot.Warning);
					return false;
				}

				_endScheduled = false;
				_endExecuted = false;
				SetState(PerfMeterCaptureState.Canceled, PerfMeterAvailability.Available, string.Empty);
				return true;
			}
		}

		internal bool Reset()
		{
			AdvanceGeneration();
			_beginPending = false;
			_endScheduled = false;
			if (_cleanupAccepted)
			{
				if (!TryAdvanceAcceptedCleanup(out string pendingError))
				{
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, pendingError);
					return false;
				}

				if (!TryReleaseCaptureResources(out string scopeError))
				{
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, scopeError);
					return false;
				}
			}
			else
			{
				_endExecuted = false;
				if (!TryReleaseCaptureResources(out string error))
				{
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, error);
					return false;
				}
			}

			_endScheduled = false;
			_endExecuted = false;
			_options = default;
			_completedPreRollFrames = 0;
			_completedCaptureFrames = 0;
			_completedPostRollFrames = 0;
			SetState(PerfMeterCaptureState.Idle, PerfMeterAvailability.Unknown, string.Empty);
			return true;
		}

		private PerfMeterCaptureBackendBeginResult TryBeginCapture()
		{
			if (!_scopeActive)
			{
				_scopeActive = true;
				try
				{
					if (!_scope.TryBegin(_options.CaptureId))
					{
						_scopeActive = false;
						SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, "Another alert capture scope is active.");
						return PerfMeterCaptureBackendBeginResult.Failed;
					}
				}
				catch (Exception exception)
				{
					TryReleaseCaptureResources(out string cleanupError);
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, CombineErrors(FormatException(exception), cleanupError));
					return PerfMeterCaptureBackendBeginResult.Failed;
				}
			}

			try
			{
				PerfMeterCaptureBackendBeginResult beginResult;
				string error;
				if (_backendV3 != null)
				{
					beginResult = _backendV3.TryBegin(_options, _generation, out error);
					PerfMeterCaptureBackendV2Snapshot backendSnapshot = GetBackendSnapshot();
					_backendActive = beginResult != PerfMeterCaptureBackendBeginResult.Failed || backendSnapshot.HasActiveResources;
				}
				else if (_backendV2 != null)
				{
					bool started = _backendV2.TryBegin(_options, out error);
					beginResult = started
						? PerfMeterCaptureBackendBeginResult.Started
						: PerfMeterCaptureBackendBeginResult.Failed;
					PerfMeterCaptureBackendV2Snapshot backendSnapshot = GetBackendSnapshot();
					_backendActive = started || backendSnapshot.HasActiveResources;
				}
				else
				{
					bool started = _backend.TryBegin(_options.Tool, out error);
					beginResult = started
						? PerfMeterCaptureBackendBeginResult.Started
						: PerfMeterCaptureBackendBeginResult.Failed;
					_backendActive = started;
				}

				if (beginResult == PerfMeterCaptureBackendBeginResult.Pending)
				{
					_beginPending = true;
					return beginResult;
				}

				_beginPending = false;
				if (beginResult == PerfMeterCaptureBackendBeginResult.Failed)
				{
					TryReleaseCaptureResources(out string cleanupError);
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, CombineErrors(error, cleanupError));
					return beginResult;
				}
			}
			catch (Exception exception)
			{
				_backendActive = _backendV2 != null;
				TryReleaseCaptureResources(out string cleanupError);
				SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, CombineErrors(FormatException(exception), cleanupError));
				return PerfMeterCaptureBackendBeginResult.Failed;
			}

			SetState(PerfMeterCaptureState.Capturing, PerfMeterAvailability.Available, string.Empty);
			return PerfMeterCaptureBackendBeginResult.Started;
		}

		private void TryEndCapture()
		{
			if (_backendV2 != null)
			{
				if (_endScheduled)
				{
					return;
				}

				_endScheduled = true;
				if (RequiresEndOfFrame)
				{
					return;
				}

				if (!TryExecuteScheduledEnd(out string backendError))
				{
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, backendError);
					return;
				}

				FinishCaptureEnd();
				return;
			}

			if (!TryReleaseCaptureResources(out string error))
			{
				SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, error);
				return;
			}

			if (_options.PostRollFrames > 0)
			{
				SetState(PerfMeterCaptureState.PostRoll, PerfMeterAvailability.Available, string.Empty);
			}
			else
			{
				SetState(PerfMeterCaptureState.Completed, PerfMeterAvailability.Available, string.Empty);
			}
		}

		private bool TryReleaseCaptureResources(out string error)
		{
			string backendError = string.Empty;
			string scopeError = string.Empty;
			if (_backendActive)
			{
				try
				{
					bool released;
					if (_backendV2 != null)
					{
						released = _backendV2.TryDiscard(out backendError);
						PerfMeterCaptureBackendV2Snapshot backendSnapshot = GetBackendSnapshot();
						_backendActive = backendSnapshot.HasActiveResources;
						_cleanupAccepted = released && _backendActive;
						if (!_backendActive)
						{
							_endScheduled = false;
							_endExecuted = false;
						}
						else if (released)
						{
							_endScheduled = false;
							_endExecuted = true;
						}
					}
					else
					{
						released = _backend.TryEnd(out backendError);
						_backendActive = !released;
						_cleanupAccepted = false;
					}

				}
				catch (Exception exception)
				{
					backendError = FormatException(exception);
				}
			}

			if (_scopeActive)
			{
				try
				{
					if (_scope.TryEnd(_options.CaptureId))
					{
						_scopeActive = false;
					}
					else
					{
						scopeError = "Capture alert scope could not be released.";
					}
				}
				catch (Exception exception)
				{
					scopeError = FormatException(exception);
				}
			}

			error = CombineErrors(backendError, scopeError);
			return !_backendActive && !_scopeActive;
		}

		private bool TryAdvanceAcceptedCleanup(out string error)
		{
			TickBackend();
			CompleteBackendIfReady();
			if (!_backendActive)
			{
				error = string.Empty;
				_cleanupAccepted = false;
				return true;
			}

			PerfMeterCaptureBackendV2Snapshot backendSnapshot = GetBackendSnapshot();
			error = string.IsNullOrEmpty(backendSnapshot.Warning) ? _warning : backendSnapshot.Warning;
			return false;
		}

		private bool HasTerminalBackendFailure()
		{
			PerfMeterCaptureBackendV2Snapshot backendSnapshot = GetBackendSnapshot();
			PerfMeterRenderDocCapturePhase phase = backendSnapshot.NativePhase;
			return backendSnapshot.EffectiveBackendKind == PerfMeterCaptureBackendKind.RenderDocNative &&
				(phase == PerfMeterRenderDocCapturePhase.Failed ||
				 phase == PerfMeterRenderDocCapturePhase.LostSession);
		}

		private bool TryExecuteScheduledEnd(out string error)
		{
			error = string.Empty;
			if (_backendV2 == null || !_backendActive || _endExecuted)
			{
				return _backendV2 == null || !_backendActive;
			}

			_endExecuted = true;
			try
			{
				bool ended = _backendV2.ScheduleEnd(out error);
				PerfMeterCaptureBackendV2Snapshot backendSnapshot = GetBackendSnapshot();
				_backendActive = backendSnapshot.HasActiveResources;
				_cleanupAccepted = false;
				return ended;
			}
			catch (Exception exception)
			{
				error = FormatException(exception);
				_backendActive = true;
				return false;
			}
		}

		private void FinishCaptureEnd()
		{
			if (_scopeActive)
			{
				try
				{
					if (_scope.TryEnd(_options.CaptureId))
					{
						_scopeActive = false;
					}
					else
					{
						SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, "Capture alert scope could not be released.");
						return;
					}
				}
				catch (Exception exception)
				{
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, FormatException(exception));
					return;
				}
			}

			if (_options.PostRollFrames > 0)
			{
				SetState(PerfMeterCaptureState.PostRoll, PerfMeterAvailability.Available, string.Empty);
			}
			else
			{
				SetState(PerfMeterCaptureState.Completed, PerfMeterAvailability.Available, string.Empty);
			}
		}

		private void TickBackend()
		{
			if (_backendV2 == null || !_backendActive || (!_beginPending && !_endExecuted && !_cleanupAccepted))
			{
				return;
			}

			try
			{
				_backendV2.Tick();
			}
			catch (Exception exception)
			{
				SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, FormatException(exception));
			}
		}

		private void CompleteBackendIfReady()
		{
			if (_backendV2 == null || !_backendActive || !_endExecuted)
			{
				return;
			}

			PerfMeterCaptureBackendV2Snapshot backendSnapshot = GetBackendSnapshot();
			if (backendSnapshot.HasPendingCompletion || backendSnapshot.HasActiveResources)
			{
				return;
			}

			bool completedAcceptedCleanup = _cleanupAccepted;
			_backendActive = false;
			_cleanupAccepted = false;
			if (backendSnapshot.NativePhase == PerfMeterRenderDocCapturePhase.Failed ||
				backendSnapshot.NativePhase == PerfMeterRenderDocCapturePhase.LostSession)
			{
				SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, backendSnapshot.Warning);
			}
			else if (completedAcceptedCleanup && !_scopeActive)
			{
				SetState(PerfMeterCaptureState.Canceled, PerfMeterAvailability.Available, string.Empty);
			}
			else if (completedAcceptedCleanup)
			{
				SetState(
					PerfMeterCaptureState.Error,
					PerfMeterAvailability.Unavailable,
					string.IsNullOrEmpty(_warning) ? "Capture alert scope could not be released." : _warning);
			}
		}

		private PerfMeterCaptureBackendV2Snapshot GetBackendSnapshot()
		{
			if (_backendV2 == null)
			{
				return PerfMeterCaptureBackendV2Snapshot.Generic(_availability, _warning);
			}

			try
			{
				return _backendV2.Snapshot;
			}
			catch (Exception exception)
			{
				return new PerfMeterCaptureBackendV2Snapshot(
					PerfMeterAvailability.Unavailable,
					FormatException(exception),
					_options.BackendMode == PerfMeterCaptureBackendMode.GenericUnity
						? PerfMeterCaptureBackendKind.GenericUnity
						: PerfMeterCaptureBackendKind.RenderDocNative,
					PerfMeterRenderDocCapturePhase.Failed,
					11,
					string.Empty,
					false,
					false,
					_backendActive);
			}
		}

		internal bool TryConsumeExternalArtifact(out PerfMeterCaptureExternalArtifactCompletion completion)
		{
			completion = default;
			if (_backendV3 == null || !_backendV3.TryConsumeExternalArtifact(out PerfMeterCaptureExternalArtifactCompletion candidate))
			{
				return false;
			}

			if (candidate.Generation != _generation ||
				!string.Equals(candidate.CaptureId, _options.CaptureId, StringComparison.Ordinal))
			{
				return false;
			}

			completion = candidate;
			return true;
		}

		private void AdvanceGeneration()
		{
			unchecked
			{
				_generation++;
				if (_generation == 0)
				{
					_generation = 1;
				}
			}
		}

		private void SetRequest(PerfMeterCaptureOptions options)
		{
			AdvanceGeneration();
			_options = options;
			_completedPreRollFrames = 0;
			_completedCaptureFrames = 0;
			_completedPostRollFrames = 0;
			_warning = string.Empty;
			_backendActive = false;
			_scopeActive = false;
			_endScheduled = false;
			_endExecuted = false;
			_cleanupAccepted = false;
			_beginPending = false;
		}

		private void SetState(PerfMeterCaptureState state, PerfMeterAvailability availability, string warning)
		{
			_state = state;
			_availability = availability;
			_warning = warning ?? string.Empty;
			PerfMeterProfilerInstrumentation.RecordCaptureState(state);
		}

		private static bool IsActiveState(PerfMeterCaptureState state)
		{
			return state == PerfMeterCaptureState.PreRoll || state == PerfMeterCaptureState.Capturing || state == PerfMeterCaptureState.PostRoll;
		}

		private static string FormatException(Exception exception)
		{
			return exception.GetType().Name + ": " + exception.Message;
		}

		private static string CombineErrors(string first, string second)
		{
			if (string.IsNullOrEmpty(first))
			{
				return second ?? string.Empty;
			}

			return string.IsNullOrEmpty(second) ? first : first + " " + second;
		}
	}

	internal static class PerfMeterNativeCaptureResultCodes
	{
		internal const int Ok = 0;
		internal const int NotLoaded = 1;
		internal const int ExportMissing = 2;
		internal const int ApiNegotiationFailed = 3;
		internal const int UnsupportedPlatform = 9;
		internal const int InternalError = 11;
	}

	internal static class PerfMeterCaptureFallbackReasons
	{
		internal const string BackendUnavailable = "native_backend_unavailable";
		internal const string NotLoaded = "native_not_loaded";
		internal const string ExportMissing = "native_export_missing";
		internal const string ApiNegotiationFailed = "native_api_negotiation_failed";
		internal const string UnsupportedPlatform = "native_unsupported_platform";

		internal static string ForResultCode(int resultCode)
		{
			switch (resultCode)
			{
				case PerfMeterNativeCaptureResultCodes.NotLoaded: return NotLoaded;
				case PerfMeterNativeCaptureResultCodes.ExportMissing: return ExportMissing;
				case PerfMeterNativeCaptureResultCodes.ApiNegotiationFailed: return ApiNegotiationFailed;
				case PerfMeterNativeCaptureResultCodes.UnsupportedPlatform: return UnsupportedPlatform;
				default: return string.Empty;
			}
		}
	}

	internal sealed class PerfMeterCaptureBackendRouter : IPerfMeterCaptureBackend, IPerfMeterCaptureBackendV3
	{
		private readonly IPerfMeterCaptureBackend _genericBackend;
		private IPerfMeterCaptureBackendV2 _nativeBackend;
		private PerfMeterCaptureOptions _options;
		private PerfMeterCaptureBackendV2Snapshot _snapshot;
		private bool _nativeRequested;
		private bool _usingNative;

		internal PerfMeterCaptureBackendRouter(IPerfMeterCaptureBackend genericBackend)
		{
			_genericBackend = genericBackend ?? throw new ArgumentNullException(nameof(genericBackend));
			_snapshot = PerfMeterCaptureBackendV2Snapshot.Generic(PerfMeterAvailability.Unknown, string.Empty);
		}

		public PerfMeterCaptureBackendV2Snapshot Snapshot
		{
			get
			{
				if (_usingNative && _nativeBackend != null)
				{
					return ReadNativeSnapshot(_snapshot.FallbackReason);
				}

				return _snapshot;
			}
		}

		public PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool)
		{
			try
			{
				PerfMeterCaptureBackendCapability capability = _genericBackend.GetCapability(tool);
				_snapshot = PerfMeterCaptureBackendV2Snapshot.Generic(capability.Availability, capability.Warning);
				_nativeRequested = false;
				_usingNative = false;
				return capability;
			}
			catch (Exception exception)
			{
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					PerfMeterAvailability.Unavailable,
					FormatException(exception),
					PerfMeterCaptureBackendKind.GenericUnity,
					PerfMeterRenderDocCapturePhase.None,
					-1,
					string.Empty,
					false,
					false,
					false);
				return new PerfMeterCaptureBackendCapability(_snapshot.Availability, _snapshot.Warning);
			}
		}

		public bool TryBegin(PerfMeterCaptureTool tool, out string error)
		{
			return TryGenericBegin(tool, out error, string.Empty);
		}

		public bool TryEnd(out string error)
		{
			return TryGenericEnd(out error, string.Empty);
		}

		public PerfMeterCaptureBackendV2Snapshot GetCapability(PerfMeterCaptureOptions options)
		{
			_options = options;
			_nativeRequested = options.BackendMode != PerfMeterCaptureBackendMode.GenericUnity;
			_usingNative = false;
			_nativeBackend = null;

			if (!_nativeRequested)
			{
				GetCapability(options.Tool);
				return _snapshot;
			}

			if (!PerfMeterNativeCaptureBackendRegistry.TryGet(out _nativeBackend))
			{
				_snapshot = CreateNativeFailure(
					PerfMeterAvailability.Unavailable,
					"Native RenderDoc capture backend is not registered.",
					PerfMeterNativeCaptureResultCodes.UnsupportedPlatform,
					_options.BackendMode == PerfMeterCaptureBackendMode.NativePreferred
						? PerfMeterCaptureFallbackReasons.BackendUnavailable
						: string.Empty);
				return SelectFallbackOrNativeFailure(_snapshot);
			}

			PerfMeterCaptureBackendV2Snapshot nativeSnapshot;
			try
			{
				nativeSnapshot = _nativeBackend.GetCapability(options);
			}
			catch (Exception exception)
			{
				nativeSnapshot = CreateNativeFailure(
					PerfMeterAvailability.Unavailable,
					FormatException(exception),
					PerfMeterNativeCaptureResultCodes.InternalError,
					string.Empty);
			}

			_snapshot = NormalizeNativeSnapshot(nativeSnapshot, string.Empty);
			if (_snapshot.Availability == PerfMeterAvailability.Available)
			{
				_usingNative = true;
				return _snapshot;
			}

			return SelectFallbackOrNativeFailure(_snapshot);
		}

		public bool TryBegin(PerfMeterCaptureOptions options, out string error)
		{
			return TryBegin(options, 0, out error) == PerfMeterCaptureBackendBeginResult.Started;
		}

		public PerfMeterCaptureBackendBeginResult TryBegin(
			PerfMeterCaptureOptions options,
			int generation,
			out string error)
		{
			error = string.Empty;
			if (!_nativeRequested || !_usingNative)
			{
				return TryGenericBegin(options.Tool, out error, _snapshot.FallbackReason)
					? PerfMeterCaptureBackendBeginResult.Started
					: PerfMeterCaptureBackendBeginResult.Failed;
			}

			PerfMeterCaptureBackendBeginResult beginResult;
			PerfMeterCaptureBackendV2Snapshot nativeSnapshot;
			try
			{
				beginResult = _nativeBackend is IPerfMeterCaptureBackendV3 nativeV3
					? nativeV3.TryBegin(options, generation, out error)
					: _nativeBackend.TryBegin(options, out error)
						? PerfMeterCaptureBackendBeginResult.Started
						: PerfMeterCaptureBackendBeginResult.Failed;
				nativeSnapshot = ReadNativeSnapshot(_snapshot.FallbackReason);
			}
			catch (Exception exception)
			{
				beginResult = PerfMeterCaptureBackendBeginResult.Failed;
				error = FormatException(exception);
				nativeSnapshot = CreateNativeFailure(
					PerfMeterAvailability.Unavailable,
					error,
					PerfMeterNativeCaptureResultCodes.InternalError,
					string.Empty,
					true);
			}

			if (beginResult == PerfMeterCaptureBackendBeginResult.Started)
			{
				_snapshot = NormalizeNativeBeginSnapshot(nativeSnapshot);
				return beginResult;
			}

			if (beginResult == PerfMeterCaptureBackendBeginResult.Pending)
			{
				_snapshot = nativeSnapshot;
				return beginResult;
			}

			_snapshot = nativeSnapshot;
			if (IsAllowedPreBeginFallback(nativeSnapshot) && _options.BackendMode == PerfMeterCaptureBackendMode.NativePreferred)
			{
				string fallbackReason = PerfMeterCaptureFallbackReasons.ForResultCode(nativeSnapshot.NativeResultCode);
				_usingNative = false;
				return TryGenericBegin(options.Tool, out error, fallbackReason)
					? PerfMeterCaptureBackendBeginResult.Started
					: PerfMeterCaptureBackendBeginResult.Failed;
			}

			if (string.IsNullOrEmpty(error))
			{
				error = nativeSnapshot.Warning;
			}

			return PerfMeterCaptureBackendBeginResult.Failed;
		}

		public bool TryConsumeExternalArtifact(out PerfMeterCaptureExternalArtifactCompletion completion)
		{
			completion = default;
			return _nativeBackend is IPerfMeterCaptureBackendV3 nativeV3 &&
				nativeV3.TryConsumeExternalArtifact(out completion);
		}

		public bool ScheduleEnd(out string error)
		{
			if (!_usingNative || _nativeBackend == null)
			{
				return TryGenericEnd(out error, _snapshot.FallbackReason);
			}

			try
			{
				bool ended = _nativeBackend.ScheduleEnd(out error);
				PerfMeterCaptureBackendV2Snapshot nativeSnapshot = ReadNativeSnapshot(_snapshot.FallbackReason);
				_snapshot = ended ? NormalizeNativeEndSnapshot(nativeSnapshot) : nativeSnapshot;
				return ended;
			}
			catch (Exception exception)
			{
				error = FormatException(exception);
				_snapshot = CreateNativeFailure(
					PerfMeterAvailability.Unavailable,
					error,
					PerfMeterNativeCaptureResultCodes.InternalError,
					_snapshot.FallbackReason,
					true);
				return false;
			}
		}

		public bool TryDiscard(out string error)
		{
			if (!_usingNative || _nativeBackend == null)
			{
				return TryGenericEnd(out error, _snapshot.FallbackReason);
			}

			try
			{
				bool discarded = _nativeBackend.TryDiscard(out error);
				_snapshot = ReadNativeSnapshot(_snapshot.FallbackReason);
				return discarded;
			}
			catch (Exception exception)
			{
				error = FormatException(exception);
				_snapshot = CreateNativeFailure(
					PerfMeterAvailability.Unavailable,
					error,
					PerfMeterNativeCaptureResultCodes.InternalError,
					_snapshot.FallbackReason,
					true);
				return false;
			}
		}

		public void Tick()
		{
			if (!_usingNative || _nativeBackend == null)
			{
				return;
			}

			try
			{
				_nativeBackend.Tick();
				_snapshot = ReadNativeSnapshot(_snapshot.FallbackReason);
			}
			catch (Exception exception)
			{
				_snapshot = CreateNativeFailure(
					PerfMeterAvailability.Unavailable,
					FormatException(exception),
					PerfMeterNativeCaptureResultCodes.InternalError,
					_snapshot.FallbackReason,
					true);
			}
		}

		private PerfMeterCaptureBackendCapability GetGenericCapability(PerfMeterCaptureTool tool)
		{
			try
			{
				return _genericBackend.GetCapability(tool);
			}
			catch (Exception exception)
			{
				return new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Unavailable, FormatException(exception));
			}
		}

		private bool TryGenericBegin(PerfMeterCaptureTool tool, out string error, string fallbackReason)
		{
			try
			{
				bool started = _genericBackend.TryBegin(tool, out error);
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					started ? PerfMeterAvailability.Available : PerfMeterAvailability.Unavailable,
					started ? string.Empty : error,
					PerfMeterCaptureBackendKind.GenericUnity,
					_snapshot.NativePhase,
					_snapshot.NativeResultCode,
					fallbackReason,
					false,
					false,
					started);
				return started;
			}
			catch (Exception exception)
			{
				error = FormatException(exception);
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					PerfMeterAvailability.Unavailable,
					error,
					PerfMeterCaptureBackendKind.GenericUnity,
					_snapshot.NativePhase,
					_snapshot.NativeResultCode,
					fallbackReason,
					false,
					false,
					false);
				return false;
			}
		}

		private bool TryGenericEnd(out string error, string fallbackReason)
		{
			try
			{
				bool ended = _genericBackend.TryEnd(out error);
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					ended ? PerfMeterAvailability.Available : PerfMeterAvailability.Unavailable,
					ended ? string.Empty : error,
					PerfMeterCaptureBackendKind.GenericUnity,
					_snapshot.NativePhase,
					_snapshot.NativeResultCode,
					fallbackReason,
					false,
					false,
					!ended);
				return ended;
			}
			catch (Exception exception)
			{
				error = FormatException(exception);
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					PerfMeterAvailability.Unavailable,
					error,
					PerfMeterCaptureBackendKind.GenericUnity,
					_snapshot.NativePhase,
					_snapshot.NativeResultCode,
					fallbackReason,
					false,
					false,
					true);
				return false;
			}
		}

		private PerfMeterCaptureBackendV2Snapshot SelectFallbackOrNativeFailure(PerfMeterCaptureBackendV2Snapshot nativeSnapshot)
		{
			if (_options.BackendMode == PerfMeterCaptureBackendMode.NativePreferred && IsAllowedPreBeginFallback(nativeSnapshot))
			{
				PerfMeterCaptureBackendCapability genericCapability = GetGenericCapability(_options.Tool);
				string fallbackReason = string.IsNullOrEmpty(nativeSnapshot.FallbackReason)
					? PerfMeterCaptureFallbackReasons.ForResultCode(nativeSnapshot.NativeResultCode)
					: nativeSnapshot.FallbackReason;
				string warning = genericCapability.Warning;
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					genericCapability.Availability,
					warning,
					PerfMeterCaptureBackendKind.GenericUnity,
					nativeSnapshot.NativePhase,
					nativeSnapshot.NativeResultCode,
					fallbackReason,
					false,
					false,
					false);
				return _snapshot;
			}

			_usingNative = false;
			return nativeSnapshot;
		}

		private static bool IsAllowedPreBeginFallback(PerfMeterCaptureBackendV2Snapshot snapshot)
		{
			if (snapshot.HasActiveResources || snapshot.HasPendingCompletion)
			{
				return false;
			}

			if (PerfMeterCaptureFallbackReasons.ForResultCode(snapshot.NativeResultCode).Length == 0)
			{
				return false;
			}

			switch (snapshot.NativePhase)
			{
				case PerfMeterRenderDocCapturePhase.None:
				case PerfMeterRenderDocCapturePhase.Preflight:
				case PerfMeterRenderDocCapturePhase.Failed:
					return true;
				default:
					return false;
			}
		}

		private PerfMeterCaptureBackendV2Snapshot ReadNativeSnapshot(string fallbackReason)
		{
			try
			{
				return NormalizeNativeSnapshot(_nativeBackend.Snapshot, fallbackReason);
			}
			catch (Exception exception)
			{
				return CreateNativeFailure(
					PerfMeterAvailability.Unavailable,
					FormatException(exception),
					PerfMeterNativeCaptureResultCodes.InternalError,
					fallbackReason,
					true);
			}
		}

		private static PerfMeterCaptureBackendV2Snapshot NormalizeNativeSnapshot(PerfMeterCaptureBackendV2Snapshot snapshot, string fallbackReason)
		{
			return new PerfMeterCaptureBackendV2Snapshot(
				snapshot.Availability,
				snapshot.Warning,
				PerfMeterCaptureBackendKind.RenderDocNative,
				snapshot.NativePhase,
				snapshot.NativeResultCode,
				fallbackReason,
				snapshot.RequiresEndOfFrame,
				snapshot.HasPendingCompletion,
				snapshot.HasActiveResources);
		}

		private PerfMeterCaptureBackendV2Snapshot NormalizeNativeBeginSnapshot(PerfMeterCaptureBackendV2Snapshot snapshot)
		{
			PerfMeterRenderDocCapturePhase phase = snapshot.NativePhase < PerfMeterRenderDocCapturePhase.BeginExecuted
				? PerfMeterRenderDocCapturePhase.BeginExecuted
				: snapshot.NativePhase;
			return new PerfMeterCaptureBackendV2Snapshot(
				PerfMeterAvailability.Available,
				snapshot.Warning,
				PerfMeterCaptureBackendKind.RenderDocNative,
				phase,
				snapshot.NativeResultCode < 0 ? PerfMeterNativeCaptureResultCodes.Ok : snapshot.NativeResultCode,
				_snapshot.FallbackReason,
				snapshot.RequiresEndOfFrame,
				snapshot.HasPendingCompletion,
				true);
		}

		private PerfMeterCaptureBackendV2Snapshot NormalizeNativeEndSnapshot(PerfMeterCaptureBackendV2Snapshot snapshot)
		{
			PerfMeterRenderDocCapturePhase phase = snapshot.NativePhase < PerfMeterRenderDocCapturePhase.EndExecuted
				? PerfMeterRenderDocCapturePhase.EndExecuted
				: snapshot.NativePhase;
			return new PerfMeterCaptureBackendV2Snapshot(
				snapshot.Availability,
				snapshot.Warning,
				PerfMeterCaptureBackendKind.RenderDocNative,
				phase,
				snapshot.NativeResultCode,
				_snapshot.FallbackReason,
				snapshot.RequiresEndOfFrame,
				snapshot.HasPendingCompletion,
				snapshot.HasActiveResources);
		}

		private static PerfMeterCaptureBackendV2Snapshot CreateNativeFailure(
			PerfMeterAvailability availability,
			string warning,
			int resultCode,
			string fallbackReason,
			bool hasActiveResources = false)
		{
			return new PerfMeterCaptureBackendV2Snapshot(
				availability,
				warning,
				PerfMeterCaptureBackendKind.RenderDocNative,
				PerfMeterRenderDocCapturePhase.Failed,
				resultCode,
				fallbackReason,
				false,
				false,
				hasActiveResources);
		}

		private static string FormatException(Exception exception)
		{
			return exception.GetType().Name + ": " + exception.Message;
		}
	}

	internal sealed class PerfMeterExternalGpuProfilerBackend : IPerfMeterCaptureBackend
	{
		internal static PerfMeterCaptureBackendCapability EvaluateCapability(
			PerfMeterCaptureTool tool,
			RuntimePlatform platform,
			GraphicsDeviceType graphicsDeviceType,
			bool captureBuild,
			bool externalProfilerAttached)
		{
			if (tool != PerfMeterCaptureTool.RenderDoc && tool != PerfMeterCaptureTool.Pix)
			{
				return Unavailable("A supported external capture tool must be selected.");
			}

			if (!captureBuild)
			{
				return Unavailable("External GPU capture is limited to the Editor and Development builds.");
			}

			bool windows = platform == RuntimePlatform.WindowsEditor || platform == RuntimePlatform.WindowsPlayer;
			bool linux = platform == RuntimePlatform.LinuxEditor || platform == RuntimePlatform.LinuxPlayer;
			bool supported = tool == PerfMeterCaptureTool.Pix
				? windows && graphicsDeviceType == GraphicsDeviceType.Direct3D12
				: (windows || linux) && (graphicsDeviceType == GraphicsDeviceType.Direct3D11 || graphicsDeviceType == GraphicsDeviceType.Direct3D12 || graphicsDeviceType == GraphicsDeviceType.Vulkan);
			if (!supported)
			{
				return Unavailable(tool + " is not supported for " + platform + " with " + graphicsDeviceType + ".");
			}

			if (!externalProfilerAttached)
			{
				return Unavailable("The requested external GPU profiler is not attached.");
			}

			return new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Available, string.Empty);
		}

		public PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool)
		{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
			bool attached;
			try
			{
				attached = UnityEngine.Experimental.Rendering.ExternalGPUProfiler.IsAttached();
			}
			catch (Exception exception)
			{
				return Unavailable(exception.GetType().Name + ": " + exception.Message);
			}

			return EvaluateCapability(tool, Application.platform, SystemInfo.graphicsDeviceType, true, attached);
		#else
			return EvaluateCapability(tool, Application.platform, SystemInfo.graphicsDeviceType, false, false);
		#endif
		}

		public bool TryBegin(PerfMeterCaptureTool tool, out string error)
		{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
			try
			{
				UnityEngine.Experimental.Rendering.ExternalGPUProfiler.BeginGPUCapture();
				error = string.Empty;
				return true;
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		#else
			error = "External GPU capture is unavailable in non-development builds.";
			return false;
		#endif
		}

		public bool TryEnd(out string error)
		{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
			try
			{
				UnityEngine.Experimental.Rendering.ExternalGPUProfiler.EndGPUCapture();
				error = string.Empty;
				return true;
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		#else
			error = "External GPU capture is unavailable in non-development builds.";
			return false;
		#endif
		}

		private static PerfMeterCaptureBackendCapability Unavailable(string warning)
		{
			return new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Unavailable, warning);
		}
	}
}
