using System;
using System.Security.Cryptography;
using System.Text;

namespace SGG.PerfMeter
{
	internal readonly struct PerfMeterRenderDocPreflight
	{
		internal PerfMeterRenderDocPreflight(ulong requestNonce, string capturePathTemplate, string title)
			: this(
				requestNonce,
				capturePathTemplate,
				title,
				new PerfMeterExternalArtifactOptions(
					artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
					containsGpuCaptureData: PerfMeterExternalArtifactContentState.Unknown,
					privacyFlags: PerfMeterExternalArtifactPrivacyFlags.ContainsGpuCaptureData |
						PerfMeterExternalArtifactPrivacyFlags.Sensitive |
						PerfMeterExternalArtifactPrivacyFlags.RequiresReview,
					storageMode: PerfMeterExternalArtifactStorageMode.MetadataOnly,
					quotaBytes: PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
					sharePolicy: PerfMeterExternalArtifactSharePolicy.DoNotShare),
				null)
		{
		}

		internal PerfMeterRenderDocPreflight(
			ulong requestNonce,
			string capturePathTemplate,
			string title,
			PerfMeterExternalArtifactOptions artifactOptions,
			PerfMeterRenderDocStorageReservation reservation)
		{
			RequestNonce = requestNonce;
			CapturePathTemplate = capturePathTemplate ?? string.Empty;
			Title = title ?? string.Empty;
			ArtifactOptions = artifactOptions;
			Reservation = reservation;
		}

		internal ulong RequestNonce { get; }
		internal string CapturePathTemplate { get; }
		internal string Title { get; }
		internal PerfMeterExternalArtifactOptions ArtifactOptions { get; }
		internal PerfMeterRenderDocStorageReservation Reservation { get; }
		internal string RootPath => Reservation == null ? string.Empty : Reservation.RootPath;

		internal SggRdResult SetTerminal(out string error)
		{
			if (Reservation == null)
			{
				error = string.Empty;
				return SggRdResult.Ok;
			}

			return Reservation.SetState(PerfMeterRenderDocStorageState.Terminal, out error);
		}

		internal SggRdResult ReleaseReservation(out string error)
		{
			if (Reservation == null)
			{
				error = string.Empty;
				return SggRdResult.Ok;
			}

			return Reservation.Release(out error);
		}

		internal SggRdResult Abort(out string error)
		{
			if (Reservation == null)
			{
				error = string.Empty;
				return SggRdResult.Ok;
			}

			return Reservation.Abort(out error);
		}
	}

	internal interface IPerfMeterRenderDocPreflightProvider
	{
		SggRdResult Prepare(PerfMeterCaptureOptions options, out PerfMeterRenderDocPreflight preflight);
	}

	internal interface IPerfMeterRenderDocPreflightProviderV2 : IPerfMeterRenderDocPreflightProvider
	{
		SggRdResult Prepare(
			PerfMeterCaptureOptions options,
			int generation,
			out PerfMeterRenderDocPreflight preflight);
	}

	internal interface IPerfMeterRenderDocCleanupProvider
	{
		SggRdResult RetryPendingCleanup(string rootPath, out string error);
	}

	internal sealed class PerfMeterRenderDocPreflightProvider : IPerfMeterRenderDocPreflightProviderV2, IPerfMeterRenderDocCleanupProvider
	{
		private const string StorageFailureWarning =
			"RenderDoc native preflight remains fail-closed until PM-RDOC-003C/003D worker/lifecycle wiring is enabled.";
		private readonly PerfMeterRenderDocStorage _storage;
		private readonly Func<string, string> _titleFactory;

		internal PerfMeterRenderDocPreflightProvider()
		{
			// The default production seam must not touch the filesystem before the
			// asynchronous worker/lifecycle wiring owns the preflight lifetime.
			_storage = null;
			_titleFactory = null;
		}

		internal PerfMeterRenderDocPreflightProvider(PerfMeterRenderDocStorage storage)
			: this(storage, null)
		{
		}

		internal PerfMeterRenderDocPreflightProvider(
			PerfMeterRenderDocStorage storage,
			Func<string, string> titleFactory)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
			_titleFactory = titleFactory ?? CreateBoundedTitle;
		}

		internal PerfMeterRenderDocStorage Storage => _storage;

		public SggRdResult Prepare(PerfMeterCaptureOptions options, out PerfMeterRenderDocPreflight preflight)
		{
			return Prepare(options, 0, out preflight);
		}

		public SggRdResult Prepare(
			PerfMeterCaptureOptions options,
			int generation,
			out PerfMeterRenderDocPreflight preflight)
		{
			preflight = default;
			if (_storage == null)
			{
				return SggRdResult.InternalError;
			}

			PerfMeterRenderDocStorageReservation reservation = null;
			try
			{
				if (string.IsNullOrWhiteSpace(options.CaptureId))
				{
					return SggRdResult.InvalidArgument;
				}
				string opaqueSessionId = CreateOpaqueSessionId(options.CaptureId);
				PerfMeterRenderDocStorageRequest request = new PerfMeterRenderDocStorageRequest(
					opaqueSessionId,
					unchecked((ulong)Math.Max(0, generation)));
				SggRdResult result = _storage.TryReserveSource(request, out reservation, out string error);
				if (result != SggRdResult.Ok)
				{
					return result;
				}

				string capturePathTemplate = reservation.CapturePathTemplate;
				string title = _titleFactory(options.CaptureId);
				PerfMeterExternalArtifactOptions artifactOptions = CreateNativeArtifactOptions(
					options.CaptureId,
					options.ExternalArtifactStorageMode);
				preflight = new PerfMeterRenderDocPreflight(
					reservation.RequestNonce,
					capturePathTemplate,
					title,
					artifactOptions,
					reservation);
				return SggRdResult.Ok;
			}
			catch (Exception)
			{
				if (reservation != null)
				{
					reservation.Abort(out _);
					if (!reservation.IsReleased)
					{
						reservation.Release(out _);
					}
				}

				return SggRdResult.InternalError;
			}
		}

		public SggRdResult RetryPendingCleanup(string rootPath, out string error)
		{
			if (_storage == null)
			{
				error = "renderdoc_storage_cleanup_unavailable";
				return SggRdResult.InternalError;
			}

			return _storage.TryRetryPendingCleanup(rootPath, out error);
		}

		internal static string PolicyNotReadyMessage => StorageFailureWarning;

		private static string CreateOpaqueSessionId(string captureId)
		{
			byte[] randomBytes = new byte[16];
			string opaqueSessionId;
			using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
			{
				do
				{
					generator.GetBytes(randomBytes);
					StringBuilder hex = new StringBuilder(randomBytes.Length * 2);
					for (int index = 0; index < randomBytes.Length; index++)
					{
						hex.Append(randomBytes[index].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
					}

					opaqueSessionId = hex.ToString();
				}
				while (string.Equals(opaqueSessionId, captureId, StringComparison.Ordinal));
			}

			return opaqueSessionId;
		}

		private static PerfMeterExternalArtifactOptions CreateNativeArtifactOptions(
			string requestId,
			PerfMeterExternalArtifactStorageMode storageMode)
		{
			return new PerfMeterExternalArtifactOptions(
				artifactId: string.IsNullOrEmpty(requestId) ? "renderdoc" : requestId + "-renderdoc",
				artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
				requestId: requestId,
				associationState: PerfMeterExternalArtifactAssociationState.None,
				finalizationState: PerfMeterExternalArtifactFinalizationState.Unavailable,
				authorityState: PerfMeterExternalArtifactAuthorityState.Unknown,
				containsGpuCaptureData: PerfMeterExternalArtifactContentState.Unknown,
				privacyFlags: PerfMeterExternalArtifactPrivacyFlags.ContainsGpuCaptureData |
					PerfMeterExternalArtifactPrivacyFlags.Sensitive |
					PerfMeterExternalArtifactPrivacyFlags.RequiresReview,
				storageMode: storageMode,
				quotaBytes: PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
				sharePolicy: storageMode == PerfMeterExternalArtifactStorageMode.MetadataOnly
					? PerfMeterExternalArtifactSharePolicy.DoNotShare
					: PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare);
		}

		private static string CreateBoundedTitle(string captureId)
		{
			string candidate = string.IsNullOrEmpty(captureId)
				? "PerfMeter RenderDoc Capture"
				: "PerfMeter " + captureId;
			if (PerfMeterRenderDocUtf8.TryEncode(
				candidate,
				PerfMeterRenderDocAbiV1.MaxTitleBytes,
				false,
				out byte[] candidateBytes))
			{
				return candidate;
			}

			if (!PerfMeterRenderDocUtf8.TryEncode(candidate, int.MaxValue, false, out candidateBytes))
			{
				return "PerfMeter RenderDoc Capture";
			}

			byte[] digest;
			using (SHA256 sha256 = SHA256.Create())
			{
				digest = sha256.ComputeHash(candidateBytes);
			}

			StringBuilder hex = new StringBuilder(digest.Length * 2);
			for (int index = 0; index < digest.Length; index++)
			{
				hex.Append(digest[index].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
			}

			return "PerfMeter RenderDoc " + hex;
		}
	}
}
