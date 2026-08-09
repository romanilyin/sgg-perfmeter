using System;
using System.Text;

namespace SGG.PerfMeter
{
	public enum PerfMeterExternalArtifactKind
	{
		Unknown = 0,
		GpuCapture = 1,
		ProfilerTrace = 2,
		MemorySnapshot = 3,
		Other = 4
	}

	public enum PerfMeterExternalArtifactFinalizationState
	{
		Unavailable = 0,
		Observed = 1,
		Finalized = 2,
		Failed = 3
	}

	public enum PerfMeterExternalArtifactAuthorityState
	{
		Unknown = 0,
		Observed = 1,
		Authenticated = 2
	}

	public enum PerfMeterExternalArtifactAssociationState
	{
		None = 0,
		Unverified = 1,
		ToolAuthenticated = 2,
		BridgeAuthenticated = 3
	}

	public enum PerfMeterExternalArtifactContentState
	{
		Unknown = 0,
		Absent = 1,
		Present = 2
	}

	public enum PerfMeterExternalArtifactStorageMode
	{
		MetadataOnly = 0,
		Copy = 1,
		Embed = 2
	}

	public enum PerfMeterExternalArtifactSharePolicy
	{
		DoNotShare = 0,
		ProjectLocalOnly = 1,
		ReviewBeforeShare = 2
	}

	[Flags]
	public enum PerfMeterExternalArtifactPrivacyFlags
	{
		None = 0,
		ContainsGpuCaptureData = 1 << 0,
		ContainsProcessMemory = 1 << 1,
		Sensitive = 1 << 2,
		RequiresReview = 1 << 3,
		All = ContainsGpuCaptureData | ContainsProcessMemory | Sensitive | RequiresReview
	}

	public readonly struct PerfMeterExternalArtifactOptions
	{
		public const int MaxArtifactIdLength = 128;
		public const int MaxToolIdLength = 128;
		public const int MaxToolVersionLength = 128;
		public const int MaxRequestIdLength = 128;
		public const int MaxHostNamespaceLength = 128;
		public const int MaxWarningLength = 1024;
		public const long DefaultQuotaBytes = 64L * 1024L * 1024L;
		public const long MaxArtifactSizeBytes = 64L * 1024L * 1024L * 1024L;

		public PerfMeterExternalArtifactOptions(
			string artifactId = "",
			PerfMeterExternalArtifactKind artifactKind = PerfMeterExternalArtifactKind.Unknown,
			string toolId = "",
			string toolVersion = "",
			string requestId = "",
			string hostNamespace = "",
			PerfMeterExternalArtifactAssociationState associationState = PerfMeterExternalArtifactAssociationState.None,
			PerfMeterExternalArtifactFinalizationState finalizationState = PerfMeterExternalArtifactFinalizationState.Unavailable,
			PerfMeterExternalArtifactAuthorityState authorityState = PerfMeterExternalArtifactAuthorityState.Unknown,
			PerfMeterExternalArtifactContentState containsGpuCaptureData = PerfMeterExternalArtifactContentState.Unknown,
			PerfMeterExternalArtifactPrivacyFlags privacyFlags = PerfMeterExternalArtifactPrivacyFlags.None,
			PerfMeterExternalArtifactStorageMode storageMode = PerfMeterExternalArtifactStorageMode.MetadataOnly,
			long quotaBytes = DefaultQuotaBytes,
			PerfMeterExternalArtifactSharePolicy sharePolicy = PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare,
			long sizeBytes = 0L,
			string observedSourceSha256 = "",
			string postCopySha256 = "",
			string warning = "")
		{
			ArtifactId = PerfMeterExternalArtifactContract.NormalizeText(artifactId, MaxArtifactIdLength);
			ArtifactKind = PerfMeterExternalArtifactContract.NormalizeArtifactKind(artifactKind);
			ToolId = PerfMeterExternalArtifactContract.NormalizeText(toolId, MaxToolIdLength);
			ToolVersion = PerfMeterExternalArtifactContract.NormalizeText(toolVersion, MaxToolVersionLength);
			RequestId = PerfMeterExternalArtifactContract.NormalizeText(requestId, MaxRequestIdLength);
			HostNamespace = PerfMeterExternalArtifactContract.NormalizeText(hostNamespace, MaxHostNamespaceLength);
			AssociationState = PerfMeterExternalArtifactContract.NormalizeAssociationState(associationState);
			FinalizationState = PerfMeterExternalArtifactContract.NormalizeFinalizationState(finalizationState);
			AuthorityState = PerfMeterExternalArtifactContract.NormalizeAuthorityState(authorityState, AssociationState);
			ContainsGpuCaptureData = PerfMeterExternalArtifactContract.NormalizeContentState(containsGpuCaptureData);
			PrivacyFlags = privacyFlags & PerfMeterExternalArtifactPrivacyFlags.All;
			StorageMode = PerfMeterExternalArtifactContract.NormalizeStorageMode(storageMode);
			QuotaBytes = PerfMeterExternalArtifactContract.ClampNonNegative(quotaBytes, MaxArtifactSizeBytes);
			SharePolicy = PerfMeterExternalArtifactContract.NormalizeSharePolicy(sharePolicy);
			SizeBytes = PerfMeterExternalArtifactContract.ClampNonNegative(sizeBytes, MaxArtifactSizeBytes);
			ObservedSourceSha256 = PerfMeterExternalArtifactContract.NormalizeSha256(observedSourceSha256);
			PostCopySha256 = PerfMeterExternalArtifactContract.NormalizeSha256(postCopySha256);
			Warning = PerfMeterExternalArtifactContract.NormalizeText(warning, MaxWarningLength);
		}

		public static PerfMeterExternalArtifactOptions Default => new PerfMeterExternalArtifactOptions(
			quotaBytes: DefaultQuotaBytes,
			sharePolicy: PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare);

		public static PerfMeterExternalArtifactOptions LegacyObserved(
			string artifactId,
			string requestId,
			long sizeBytes,
			string observedSourceSha256 = "",
			string postCopySha256 = "")
		{
			string normalizedPostCopySha256 = PerfMeterExternalArtifactContract.NormalizeSha256(postCopySha256);
			return new PerfMeterExternalArtifactOptions(
				artifactId,
				PerfMeterExternalArtifactKind.GpuCapture,
				requestId: requestId,
				associationState: PerfMeterExternalArtifactAssociationState.Unverified,
				finalizationState: string.IsNullOrEmpty(normalizedPostCopySha256)
					? PerfMeterExternalArtifactFinalizationState.Observed
					: PerfMeterExternalArtifactFinalizationState.Finalized,
				authorityState: PerfMeterExternalArtifactAuthorityState.Observed,
				containsGpuCaptureData: PerfMeterExternalArtifactContentState.Unknown,
				privacyFlags: PerfMeterExternalArtifactPrivacyFlags.RequiresReview,
				storageMode: PerfMeterExternalArtifactStorageMode.Embed,
				quotaBytes: DefaultQuotaBytes,
				sharePolicy: PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare,
				sizeBytes: sizeBytes,
				observedSourceSha256: observedSourceSha256,
				postCopySha256: normalizedPostCopySha256);
		}

		public string ArtifactId { get; }
		public PerfMeterExternalArtifactKind ArtifactKind { get; }
		public string ToolId { get; }
		public string ToolVersion { get; }
		public string RequestId { get; }
		public string HostNamespace { get; }
		public PerfMeterExternalArtifactAssociationState AssociationState { get; }
		public PerfMeterExternalArtifactFinalizationState FinalizationState { get; }
		public PerfMeterExternalArtifactAuthorityState AuthorityState { get; }
		public PerfMeterExternalArtifactContentState ContainsGpuCaptureData { get; }
		public PerfMeterExternalArtifactPrivacyFlags PrivacyFlags { get; }
		public PerfMeterExternalArtifactStorageMode StorageMode { get; }
		public long QuotaBytes { get; }
		public PerfMeterExternalArtifactSharePolicy SharePolicy { get; }
		public long SizeBytes { get; }
		public string ObservedSourceSha256 { get; }
		public string PostCopySha256 { get; }
		public string Warning { get; }

		public PerfMeterExternalArtifactSnapshot ToSnapshot()
		{
			return new PerfMeterExternalArtifactSnapshot(this);
		}
	}

	public readonly struct PerfMeterExternalArtifactSnapshot
	{
		public PerfMeterExternalArtifactSnapshot(PerfMeterExternalArtifactOptions options)
		{
			ArtifactId = options.ArtifactId ?? string.Empty;
			ArtifactKind = options.ArtifactKind;
			ToolId = options.ToolId ?? string.Empty;
			ToolVersion = options.ToolVersion ?? string.Empty;
			RequestId = options.RequestId ?? string.Empty;
			HostNamespace = options.HostNamespace ?? string.Empty;
			AssociationState = options.AssociationState;
			FinalizationState = options.FinalizationState;
			AuthorityState = options.AuthorityState == PerfMeterExternalArtifactAuthorityState.Authenticated &&
				IsAuthenticatedAssociation(options.AssociationState)
				? PerfMeterExternalArtifactAuthorityState.Authenticated
				: options.AuthorityState;
			ContainsGpuCaptureData = options.ContainsGpuCaptureData;
			PrivacyFlags = options.PrivacyFlags;
			StorageMode = options.StorageMode;
			QuotaBytes = options.QuotaBytes;
			SharePolicy = options.SharePolicy;
			SizeBytes = options.SizeBytes;
			ObservedSourceSha256 = options.ObservedSourceSha256 ?? string.Empty;
			PostCopySha256 = options.PostCopySha256 ?? string.Empty;
			Warning = options.Warning ?? string.Empty;
		}

		public static PerfMeterExternalArtifactSnapshot Empty => new PerfMeterExternalArtifactSnapshot(PerfMeterExternalArtifactOptions.Default);

		public static PerfMeterExternalArtifactSnapshot LegacyObserved(
			string artifactId,
			string requestId,
			long sizeBytes,
			string observedSourceSha256 = "",
			string postCopySha256 = "")
		{
			return PerfMeterExternalArtifactOptions.LegacyObserved(
				artifactId,
				requestId,
				sizeBytes,
				observedSourceSha256,
				postCopySha256).ToSnapshot();
		}

		public bool HasAuthenticatedAssociation => IsAuthenticatedAssociation(AssociationState);
		public bool IsAuthoritative => AuthorityState == PerfMeterExternalArtifactAuthorityState.Authenticated && HasAuthenticatedAssociation;
		public bool HasPostCopyHash => !string.IsNullOrEmpty(PostCopySha256);
		public bool IsFinalized => FinalizationState == PerfMeterExternalArtifactFinalizationState.Finalized;
		public string ArtifactId { get; }
		public PerfMeterExternalArtifactKind ArtifactKind { get; }
		public string ToolId { get; }
		public string ToolVersion { get; }
		public string RequestId { get; }
		public string HostNamespace { get; }
		public PerfMeterExternalArtifactAssociationState AssociationState { get; }
		public PerfMeterExternalArtifactFinalizationState FinalizationState { get; }
		public PerfMeterExternalArtifactAuthorityState AuthorityState { get; }
		public PerfMeterExternalArtifactContentState ContainsGpuCaptureData { get; }
		public PerfMeterExternalArtifactPrivacyFlags PrivacyFlags { get; }
		public PerfMeterExternalArtifactStorageMode StorageMode { get; }
		public long QuotaBytes { get; }
		public PerfMeterExternalArtifactSharePolicy SharePolicy { get; }
		public long SizeBytes { get; }
		public string ObservedSourceSha256 { get; }
		public string PostCopySha256 { get; }
		public string Warning { get; }

		private static bool IsAuthenticatedAssociation(PerfMeterExternalArtifactAssociationState associationState)
		{
			return associationState == PerfMeterExternalArtifactAssociationState.ToolAuthenticated ||
				associationState == PerfMeterExternalArtifactAssociationState.BridgeAuthenticated;
		}
	}

	internal static class PerfMeterExternalArtifactContract
	{
		private const int Sha256Length = 64;

		internal static string NormalizeText(string value, int maximumLength)
		{
			string normalized = (value ?? string.Empty).Trim();
			if (normalized.Length <= maximumLength)
			{
				return normalized;
			}

			return normalized.Substring(0, maximumLength);
		}

		internal static long ClampNonNegative(long value, long maximum)
		{
			return value < 0L ? 0L : value > maximum ? maximum : value;
		}

		internal static string NormalizeSha256(string value)
		{
			string normalized = (value ?? string.Empty).Trim();
			if (normalized.Length != Sha256Length)
			{
				return string.Empty;
			}

			StringBuilder builder = new StringBuilder(Sha256Length);
			for (int i = 0; i < normalized.Length; i++)
			{
				char character = normalized[i];
				if (!IsHex(character))
				{
					return string.Empty;
				}

				builder.Append(char.ToLowerInvariant(character));
			}

			return builder.ToString();
		}

		internal static PerfMeterExternalArtifactKind NormalizeArtifactKind(PerfMeterExternalArtifactKind value)
		{
			switch (value)
			{
				case PerfMeterExternalArtifactKind.Unknown:
				case PerfMeterExternalArtifactKind.GpuCapture:
				case PerfMeterExternalArtifactKind.ProfilerTrace:
				case PerfMeterExternalArtifactKind.MemorySnapshot:
				case PerfMeterExternalArtifactKind.Other:
					return value;
				default:
					return PerfMeterExternalArtifactKind.Unknown;
			}
		}

		internal static PerfMeterExternalArtifactAssociationState NormalizeAssociationState(PerfMeterExternalArtifactAssociationState value)
		{
			switch (value)
			{
				case PerfMeterExternalArtifactAssociationState.None:
				case PerfMeterExternalArtifactAssociationState.Unverified:
				case PerfMeterExternalArtifactAssociationState.ToolAuthenticated:
				case PerfMeterExternalArtifactAssociationState.BridgeAuthenticated:
					return value;
				default:
					return PerfMeterExternalArtifactAssociationState.None;
			}
		}

		internal static PerfMeterExternalArtifactFinalizationState NormalizeFinalizationState(PerfMeterExternalArtifactFinalizationState value)
		{
			switch (value)
			{
				case PerfMeterExternalArtifactFinalizationState.Unavailable:
				case PerfMeterExternalArtifactFinalizationState.Observed:
				case PerfMeterExternalArtifactFinalizationState.Finalized:
				case PerfMeterExternalArtifactFinalizationState.Failed:
					return value;
				default:
					return PerfMeterExternalArtifactFinalizationState.Unavailable;
			}
		}

		internal static PerfMeterExternalArtifactAuthorityState NormalizeAuthorityState(
			PerfMeterExternalArtifactAuthorityState value,
			PerfMeterExternalArtifactAssociationState associationState)
		{
			switch (value)
			{
				case PerfMeterExternalArtifactAuthorityState.Unknown:
				case PerfMeterExternalArtifactAuthorityState.Observed:
					return value;
				case PerfMeterExternalArtifactAuthorityState.Authenticated:
					return associationState == PerfMeterExternalArtifactAssociationState.ToolAuthenticated ||
						associationState == PerfMeterExternalArtifactAssociationState.BridgeAuthenticated
						? value
						: PerfMeterExternalArtifactAuthorityState.Unknown;
				default:
					return PerfMeterExternalArtifactAuthorityState.Unknown;
			}
		}

		internal static PerfMeterExternalArtifactContentState NormalizeContentState(PerfMeterExternalArtifactContentState value)
		{
			switch (value)
			{
				case PerfMeterExternalArtifactContentState.Unknown:
				case PerfMeterExternalArtifactContentState.Absent:
				case PerfMeterExternalArtifactContentState.Present:
					return value;
				default:
					return PerfMeterExternalArtifactContentState.Unknown;
			}
		}

		internal static PerfMeterExternalArtifactStorageMode NormalizeStorageMode(PerfMeterExternalArtifactStorageMode value)
		{
			switch (value)
			{
				case PerfMeterExternalArtifactStorageMode.MetadataOnly:
				case PerfMeterExternalArtifactStorageMode.Copy:
				case PerfMeterExternalArtifactStorageMode.Embed:
					return value;
				default:
					return PerfMeterExternalArtifactStorageMode.MetadataOnly;
			}
		}

		internal static PerfMeterExternalArtifactSharePolicy NormalizeSharePolicy(PerfMeterExternalArtifactSharePolicy value)
		{
			switch (value)
			{
				case PerfMeterExternalArtifactSharePolicy.DoNotShare:
				case PerfMeterExternalArtifactSharePolicy.ProjectLocalOnly:
				case PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare:
					return value;
				default:
					return PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare;
			}
		}

		private static bool IsHex(char value)
		{
			return value >= '0' && value <= '9' || value >= 'a' && value <= 'f' || value >= 'A' && value <= 'F';
		}
	}
}
