using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace SGG.PerfMeter
{
	internal static class PerfMeterRenderDocFinalizationPolicy
	{
		internal const int PollMilliseconds = 100;
		internal const int FirstCandidateMilliseconds = 30000;
		internal const int QuietMilliseconds = 500;
		internal const int StabilizationMilliseconds = 60000;
		internal const int StableSampleCount = 4;
		internal const int StableSampleMilliseconds = 250;
		internal const int TotalMilliseconds = 180000;
	}

	internal interface IPerfMeterRenderDocMonotonicClock
	{
		long Timestamp { get; }
		long Frequency { get; }
		void Delay(TimeSpan delay);
	}

	internal readonly struct PerfMeterRenderDocFileSample
	{
		internal PerfMeterRenderDocFileSample(byte[] identity, long sizeBytes, long lastWriteTicks)
		{
			Identity = identity ?? Array.Empty<byte>();
			SizeBytes = sizeBytes;
			LastWriteTicks = lastWriteTicks;
		}

		internal byte[] Identity { get; }
		internal long SizeBytes { get; }
		internal long LastWriteTicks { get; }
	}

	internal interface IPerfMeterRenderDocFileBinding : IDisposable
	{
		SggRdResult TrySample(out PerfMeterRenderDocFileSample sample, out string error);
		SggRdResult TryComputeSha256(long maximumBytes, Func<bool> shouldStop, out string sha256, out string error);
		SggRdResult TryCopyTo(string destinationPath, long maximumBytes, Func<bool> shouldStop, out string error);
	}

	internal interface IPerfMeterRenderDocFileBindingFactory
	{
		SggRdResult TryOpen(string path, out IPerfMeterRenderDocFileBinding binding, out string error);
	}

	internal readonly struct PerfMeterRenderDocFinalizationResult
	{
		internal PerfMeterRenderDocFinalizationResult(
			SggRdResult result,
			PerfMeterExternalArtifactSnapshot artifact,
			string retainedPayloadPath,
			string warning)
		{
			Result = result;
			Artifact = artifact;
			RetainedPayloadPath = retainedPayloadPath ?? string.Empty;
			Warning = warning ?? string.Empty;
		}

		internal SggRdResult Result { get; }
		internal PerfMeterExternalArtifactSnapshot Artifact { get; }
		internal string RetainedPayloadPath { get; }
		internal string Warning { get; }
		internal bool Succeeded => Result == SggRdResult.Ok;
	}

	internal interface IPerfMeterRenderDocArtifactFinalizer
	{
		PerfMeterRenderDocFinalizationResult Run(
			IPerfMeterRenderDocBridge bridge,
			SggRdCaptureTokenV1 token,
			PerfMeterRenderDocPreflight preflight,
			Func<bool> isCancellationRequested = null);
	}

	// Run is intentionally synchronous: callers must schedule it on a worker.
	internal sealed class PerfMeterRenderDocArtifactFinalizer : IPerfMeterRenderDocArtifactFinalizer
	{
		private readonly PerfMeterRenderDocStorage _storage;
		private readonly IPerfMeterRenderDocFileBindingFactory _fileBindings;
		private readonly IPerfMeterRenderDocMonotonicClock _clock;

		internal PerfMeterRenderDocArtifactFinalizer(PerfMeterRenderDocStorage storage)
			: this(storage, new WindowsFileBindingFactory(), new StopwatchClock())
		{
		}

		internal PerfMeterRenderDocArtifactFinalizer(
			PerfMeterRenderDocStorage storage,
			IPerfMeterRenderDocFileBindingFactory fileBindings,
			IPerfMeterRenderDocMonotonicClock clock)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
			_fileBindings = fileBindings ?? throw new ArgumentNullException(nameof(fileBindings));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			if (_clock.Frequency <= 0L)
			{
				throw new ArgumentException("A positive monotonic frequency is required.", nameof(clock));
			}
		}

		public PerfMeterRenderDocFinalizationResult Run(
			IPerfMeterRenderDocBridge bridge,
			SggRdCaptureTokenV1 token,
			PerfMeterRenderDocPreflight preflight,
			Func<bool> isCancellationRequested = null)
		{
			long operationStart = _clock.Timestamp;
			PerfMeterRenderDocStorageReservation sourceReservation = preflight.Reservation;
			if (bridge == null || sourceReservation == null || sourceReservation.IsReleased ||
				token.StructSize < PerfMeterRenderDocAbiV1.CaptureTokenSizeAsUInt ||
				token.RequestNonce == 0u || token.RequestNonce != preflight.RequestNonce ||
				token.StartUnixNanoseconds == 0u)
			{
				FinishSource(sourceReservation);
				return Failed(preflight, SggRdResult.InvalidArgument, "renderdoc_finalization_input_invalid", 0L, string.Empty, string.Empty);
			}

			PerfMeterExternalArtifactStorageMode storageMode = preflight.ArtifactOptions.StorageMode;
			if (storageMode == PerfMeterExternalArtifactStorageMode.Embed)
			{
				FinishSource(sourceReservation);
				return Failed(preflight, SggRdResult.InvalidArgument, "renderdoc_embed_path_not_enabled", 0L, string.Empty, string.Empty);
			}

			if (sourceReservation.SetState(PerfMeterRenderDocStorageState.AwaitingArtifact, out string stateError) != SggRdResult.Ok)
			{
				return Failed(preflight, SggRdResult.InternalError, stateError, 0L, string.Empty, string.Empty);
			}

			SggRdResult observeResult = TryObserveCandidate(
				bridge,
				token,
				operationStart,
				isCancellationRequested,
				out string sourcePath,
				out long candidateTimestamp,
				out string error);
			if (observeResult != SggRdResult.Ok)
			{
				FinishSource(sourceReservation);
				return Failed(preflight, observeResult, error, 0L, string.Empty, string.Empty);
			}

			SggRdResult payloadResult = _storage.TryValidatePayloadPath(sourceReservation, sourcePath, out error);
			if (payloadResult != SggRdResult.Ok)
			{
				FinishSource(sourceReservation);
				return Failed(preflight, payloadResult, error, 0L, string.Empty, string.Empty);
			}

			if (sourceReservation.SetState(PerfMeterRenderDocStorageState.Finalizing, out stateError) != SggRdResult.Ok)
			{
				FinishSource(sourceReservation);
				return Failed(preflight, SggRdResult.InternalError, stateError, 0L, string.Empty, string.Empty);
			}

			SggRdResult bindingResult = TryOpenStableBinding(
				sourcePath,
				candidateTimestamp,
				operationStart,
				isCancellationRequested,
				out IPerfMeterRenderDocFileBinding sourceBinding,
				out PerfMeterRenderDocFileSample stableSample,
				out error);
			if (bindingResult != SggRdResult.Ok)
			{
				FinishSource(sourceReservation);
				return Failed(preflight, bindingResult, error, 0L, string.Empty, string.Empty);
			}

			using (sourceBinding)
			{
				long payloadBytes = stableSample.SizeBytes;
				if (IsCanceled(isCancellationRequested))
				{
					FinishSource(sourceReservation);
					return Failed(preflight, SggRdResult.CaptureFailed, "renderdoc_finalization_canceled", payloadBytes, string.Empty, string.Empty);
				}

				Func<bool> shouldStop = () => IsCanceled(isCancellationRequested) || IsTotalExpired(operationStart);
				SggRdResult hashResult = sourceBinding.TryComputeSha256(
					PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
					shouldStop,
					out string sourceHash,
					out error);
				if (hashResult != SggRdResult.Ok || IsTotalExpired(operationStart))
				{
					FinishSource(sourceReservation);
					return Failed(preflight, hashResult == SggRdResult.Ok ? SggRdResult.CaptureFailed : hashResult,
						IsTotalExpired(operationStart) ? "renderdoc_finalization_deadline" : error,
						payloadBytes, sourceHash, string.Empty);
				}

				SggRdResult verifyResult = TryVerifyBoundPath(sourcePath, stableSample, sourceHash, shouldStop, out error);
				if (verifyResult != SggRdResult.Ok)
				{
					FinishSource(sourceReservation);
					return Failed(preflight, verifyResult, error, payloadBytes, sourceHash, string.Empty);
				}

				string identityHash = HashIdentity(stableSample.Identity, preflight.RequestNonce);
				string retainedPath = string.Empty;
				string postCopyHash = string.Empty;
				PerfMeterRenderDocStorageReservation copyReservation = null;
				if (storageMode == PerfMeterExternalArtifactStorageMode.Copy)
				{
					SggRdResult copyReserveResult = _storage.TryReserveCopyOrEmbed(
						sourceReservation.Request,
						PerfMeterExternalArtifactStorageMode.Copy,
						payloadBytes,
						out copyReservation,
						out error);
					if (copyReserveResult != SggRdResult.Ok)
					{
						FinishSource(sourceReservation);
						return Failed(preflight, copyReserveResult, error, payloadBytes, sourceHash, identityHash);
					}

					retainedPath = Path.Combine(copyReservation.RootPath, "capture.rdc");
					SggRdResult copyResult = sourceBinding.TryCopyTo(
						retainedPath,
						PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
						shouldStop,
						out error);
					if (copyResult == SggRdResult.Ok)
					{
						copyResult = TryVerifyCopy(retainedPath, sourceHash, payloadBytes, shouldStop, out postCopyHash, out error);
					}
					if (copyResult == SggRdResult.Ok)
					{
						copyResult = TryVerifyBoundPath(sourcePath, stableSample, sourceHash, shouldStop, out error);
					}

					if (copyResult != SggRdResult.Ok || IsTotalExpired(operationStart))
					{
						SggRdResult abortResult = copyReservation.Abort(out string abortError);
						if (abortResult != SggRdResult.Ok)
						{
							error = CombineErrors(error, abortError);
							copyResult = SggRdResult.InternalError;
						}
						FinishSource(sourceReservation);
						return Failed(preflight, copyResult == SggRdResult.Ok ? SggRdResult.CaptureFailed : copyResult,
							IsTotalExpired(operationStart) ? "renderdoc_finalization_deadline" : error,
							payloadBytes, sourceHash, identityHash);
					}

					SggRdResult copyTerminalResult = copyReservation.SetState(PerfMeterRenderDocStorageState.Terminal, out string copyTerminalError);
					if (copyTerminalResult != SggRdResult.Ok)
					{
						SggRdResult abortResult = copyReservation.Abort(out string abortError);
						return Failed(
							preflight,
							abortResult == SggRdResult.Ok ? copyTerminalResult : SggRdResult.InternalError,
							CombineErrors(copyTerminalError, abortError),
							payloadBytes,
							sourceHash,
							identityHash);
					}
				}

				if (!TryFinishSource(sourceReservation, out string sourceTerminalError))
				{
					if (copyReservation != null)
					{
						SggRdResult copyCleanupResult = _storage.TryDeleteOwnedRoot(
							copyReservation.RootPath,
							out string copyCleanupError);
						if (copyCleanupResult != SggRdResult.Ok)
						{
							sourceTerminalError = CombineErrors(sourceTerminalError, copyCleanupError);
						}
					}
					return Failed(preflight, SggRdResult.InternalError, sourceTerminalError, payloadBytes, sourceHash, identityHash);
				}
				bool canceledAtCompletion = IsCanceled(isCancellationRequested);
				bool expiredAtCompletion = IsTotalExpired(operationStart);
				if (canceledAtCompletion || expiredAtCompletion)
				{
					string completionError = canceledAtCompletion
						? "renderdoc_finalization_canceled"
						: "renderdoc_finalization_deadline";
					SggRdResult completionResult = SggRdResult.CaptureFailed;
					if (!string.IsNullOrEmpty(retainedPath))
					{
						SggRdResult deleteResult = _storage.TryDeleteOwnedRoot(Path.GetDirectoryName(retainedPath), out string deleteError);
						if (deleteResult != SggRdResult.Ok)
						{
							completionResult = SggRdResult.InternalError;
							completionError = CombineErrors(completionError, deleteError);
						}
					}
					return Failed(
						preflight,
						completionResult,
						completionError,
						payloadBytes,
						sourceHash,
						identityHash);
				}
				return new PerfMeterRenderDocFinalizationResult(
					SggRdResult.Ok,
					CreateSnapshot(preflight, storageMode, payloadBytes, sourceHash, postCopyHash, identityHash, string.Empty, true),
					retainedPath,
					string.Empty);
			}
		}

		private SggRdResult TryObserveCandidate(
			IPerfMeterRenderDocBridge bridge,
			SggRdCaptureTokenV1 token,
			long operationStart,
			Func<bool> isCancellationRequested,
			out string sourcePath,
			out long candidateTimestamp,
			out string error)
		{
			sourcePath = string.Empty;
			candidateTimestamp = 0L;
			error = string.Empty;
			SggRdArtifactV1 selectedArtifact = default;
			long firstCandidate = 0L;
			bool hasCandidate = false;
			while (true)
			{
				if (IsCanceled(isCancellationRequested))
				{
					error = "renderdoc_finalization_canceled";
					return SggRdResult.CaptureFailed;
				}

				if (IsTotalExpired(operationStart))
				{
					error = "renderdoc_finalization_deadline";
					return SggRdResult.CaptureFailed;
				}

				long beforeCall = _clock.Timestamp;
				if (!hasCandidate && ElapsedMilliseconds(operationStart, beforeCall) >= PerfMeterRenderDocFinalizationPolicy.FirstCandidateMilliseconds)
				{
					error = "renderdoc_artifact_observation_deadline";
					return SggRdResult.CaptureNotObserved;
				}

				SggRdResult result = bridge.TryGetNewArtifact(token, out SggRdArtifactV1 artifact, out string observedPath);
				long now = _clock.Timestamp;
				if (IsTotalExpired(operationStart))
				{
					error = "renderdoc_finalization_deadline";
					return SggRdResult.CaptureFailed;
				}
				if (!hasCandidate && ElapsedMilliseconds(operationStart, now) >= PerfMeterRenderDocFinalizationPolicy.FirstCandidateMilliseconds)
				{
					error = "renderdoc_artifact_observation_deadline";
					return SggRdResult.CaptureNotObserved;
				}
				if (result == SggRdResult.Ok)
				{
					if (!IsValidArtifact(token, artifact))
					{
						error = "renderdoc_artifact_evidence_invalid";
						return SggRdResult.CaptureFailed;
					}
					if (!hasCandidate)
					{
						hasCandidate = true;
						firstCandidate = now;
						candidateTimestamp = now;
						selectedArtifact = artifact;
						sourcePath = observedPath ?? string.Empty;
					}
					else if (artifact.Index != selectedArtifact.Index ||
						artifact.RenderDocTimestampSeconds != selectedArtifact.RenderDocTimestampSeconds ||
						!string.Equals(observedPath, sourcePath, StringComparison.Ordinal))
					{
						error = "renderdoc_artifact_candidate_changed";
						return SggRdResult.CaptureFailed;
					}

					if (hasCandidate && ElapsedMilliseconds(firstCandidate, now) >= PerfMeterRenderDocFinalizationPolicy.QuietMilliseconds)
					{
						return SggRdResult.Ok;
					}
				}
				else if (result != SggRdResult.CaptureNotObserved || hasCandidate)
				{
					error = result == SggRdResult.CaptureFailed
						? "renderdoc_artifact_ambiguous"
						: "renderdoc_artifact_observation_failed";
					return result == SggRdResult.CaptureNotObserved ? SggRdResult.CaptureFailed : result;
				}

				_clock.Delay(TimeSpan.FromMilliseconds(PerfMeterRenderDocFinalizationPolicy.PollMilliseconds));
			}
		}

		private SggRdResult TryOpenStableBinding(
			string sourcePath,
			long candidateTimestamp,
			long operationStart,
			Func<bool> isCancellationRequested,
			out IPerfMeterRenderDocFileBinding binding,
			out PerfMeterRenderDocFileSample stableSample,
			out string error)
		{
			binding = null;
			stableSample = default;
			error = string.Empty;
			while (ElapsedMilliseconds(candidateTimestamp, _clock.Timestamp) < PerfMeterRenderDocFinalizationPolicy.StabilizationMilliseconds)
			{
				if (IsCanceled(isCancellationRequested) || IsTotalExpired(operationStart))
				{
					error = IsCanceled(isCancellationRequested) ? "renderdoc_finalization_canceled" : "renderdoc_finalization_deadline";
					return SggRdResult.CaptureFailed;
				}

				SggRdResult openResult = _fileBindings.TryOpen(sourcePath, out binding, out error);
				if (openResult != SggRdResult.Ok)
				{
					binding?.Dispose();
					binding = null;
					_clock.Delay(TimeSpan.FromMilliseconds(PerfMeterRenderDocFinalizationPolicy.StableSampleMilliseconds));
					continue;
				}

				int unchangedSamples = 0;
				PerfMeterRenderDocFileSample previous = default;
				while (ElapsedMilliseconds(candidateTimestamp, _clock.Timestamp) < PerfMeterRenderDocFinalizationPolicy.StabilizationMilliseconds)
				{
					if (IsCanceled(isCancellationRequested) || IsTotalExpired(operationStart))
					{
						error = IsCanceled(isCancellationRequested) ? "renderdoc_finalization_canceled" : "renderdoc_finalization_deadline";
						binding.Dispose();
						binding = null;
						return SggRdResult.CaptureFailed;
					}

					SggRdResult sampleResult = binding.TrySample(out PerfMeterRenderDocFileSample current, out error);
					if (sampleResult != SggRdResult.Ok)
					{
						break;
					}

					if (current.SizeBytes > PerfMeterRenderDocStoragePolicy.MaxPayloadBytes)
					{
						error = "renderdoc_storage_payload_limit_exceeded";
						binding.Dispose();
						binding = null;
						return SggRdResult.CaptureFailed;
					}

					if (current.SizeBytes > 0L && (unchangedSamples == 0 || SamplesEqual(previous, current)))
					{
						unchangedSamples++;
					}
					else
					{
						unchangedSamples = current.SizeBytes > 0L ? 1 : 0;
					}

					previous = current;
					if (unchangedSamples >= PerfMeterRenderDocFinalizationPolicy.StableSampleCount)
					{
						stableSample = current;
						return SggRdResult.Ok;
					}

					_clock.Delay(TimeSpan.FromMilliseconds(PerfMeterRenderDocFinalizationPolicy.StableSampleMilliseconds));
				}

				binding.Dispose();
				binding = null;
			}

			error = "renderdoc_artifact_stabilization_deadline";
			return SggRdResult.CaptureFailed;
		}

		private SggRdResult TryVerifyBoundPath(
			string path,
			PerfMeterRenderDocFileSample expected,
			string expectedHash,
			Func<bool> shouldStop,
			out string error)
		{
			error = string.Empty;
			SggRdResult result = _fileBindings.TryOpen(path, out IPerfMeterRenderDocFileBinding verification, out error);
			if (result != SggRdResult.Ok)
			{
				return result;
			}

			using (verification)
			{
				result = verification.TrySample(out PerfMeterRenderDocFileSample sample, out error);
				if (result != SggRdResult.Ok || !SamplesEqual(expected, sample))
				{
					error = "renderdoc_artifact_identity_changed";
					return SggRdResult.CaptureFailed;
				}

				result = verification.TryComputeSha256(
					PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
					shouldStop,
					out string hash,
					out error);
				if (result != SggRdResult.Ok || !string.Equals(hash, expectedHash, StringComparison.Ordinal))
				{
					error = "renderdoc_artifact_hash_changed";
					return SggRdResult.CaptureFailed;
				}

				result = verification.TrySample(out PerfMeterRenderDocFileSample afterHash, out error);
				if (result != SggRdResult.Ok || !SamplesEqual(expected, afterHash))
				{
					error = "renderdoc_artifact_identity_changed";
					return SggRdResult.CaptureFailed;
				}
				if (IsCanceled(shouldStop))
				{
					error = "renderdoc_artifact_verification_stopped";
					return SggRdResult.CaptureFailed;
				}
			}

			return SggRdResult.Ok;
		}

		private SggRdResult TryVerifyCopy(
			string path,
			string expectedHash,
			long expectedBytes,
			Func<bool> shouldStop,
			out string hash,
			out string error)
		{
			hash = string.Empty;
			SggRdResult result = _fileBindings.TryOpen(path, out IPerfMeterRenderDocFileBinding binding, out error);
			if (result != SggRdResult.Ok)
			{
				return result;
			}

			using (binding)
			{
				result = binding.TrySample(out PerfMeterRenderDocFileSample sample, out error);
				if (result != SggRdResult.Ok || sample.SizeBytes != expectedBytes)
				{
					error = "renderdoc_copy_size_mismatch";
					return SggRdResult.CaptureFailed;
				}

				PerfMeterRenderDocFileSample beforeHash = sample;
				result = binding.TryComputeSha256(
					PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
					shouldStop,
					out hash,
					out error);
				if (result != SggRdResult.Ok || !string.Equals(hash, expectedHash, StringComparison.Ordinal))
				{
					error = "renderdoc_copy_hash_mismatch";
					return SggRdResult.CaptureFailed;
				}

				result = binding.TrySample(out PerfMeterRenderDocFileSample afterHash, out error);
				if (result != SggRdResult.Ok || !SamplesEqual(beforeHash, afterHash))
				{
					error = "renderdoc_copy_identity_changed";
					return SggRdResult.CaptureFailed;
				}
				if (IsCanceled(shouldStop))
				{
					error = "renderdoc_copy_verification_stopped";
					return SggRdResult.CaptureFailed;
				}
			}

			return SggRdResult.Ok;
		}

		private PerfMeterRenderDocFinalizationResult Failed(
			PerfMeterRenderDocPreflight preflight,
			SggRdResult result,
			string warning,
			long sizeBytes,
			string sourceHash,
			string identityHash)
		{
			if (!TryFinishSource(preflight.Reservation, out string cleanupError))
			{
				warning = string.IsNullOrEmpty(warning)
					? cleanupError
					: warning + "; " + cleanupError;
				result = SggRdResult.InternalError;
			}

			return new PerfMeterRenderDocFinalizationResult(
				result,
				CreateSnapshot(preflight, preflight.ArtifactOptions.StorageMode, sizeBytes, sourceHash, string.Empty, identityHash, warning, false),
				string.Empty,
				warning);
		}

		private static PerfMeterExternalArtifactSnapshot CreateSnapshot(
			PerfMeterRenderDocPreflight preflight,
			PerfMeterExternalArtifactStorageMode storageMode,
			long sizeBytes,
			string sourceHash,
			string postCopyHash,
			string identityHash,
			string warning,
			bool finalized)
		{
			PerfMeterExternalArtifactOptions baseline = preflight.ArtifactOptions;
			return new PerfMeterExternalArtifactOptions(
				artifactId: baseline.ArtifactId,
				artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
				toolId: "renderdoc",
				toolVersion: baseline.ToolVersion,
				requestId: baseline.RequestId,
				hostNamespace: baseline.HostNamespace,
				associationState: finalized ? PerfMeterExternalArtifactAssociationState.BridgeAuthenticated : PerfMeterExternalArtifactAssociationState.Unverified,
				finalizationState: finalized ? PerfMeterExternalArtifactFinalizationState.Finalized : PerfMeterExternalArtifactFinalizationState.Failed,
				authorityState: finalized ? PerfMeterExternalArtifactAuthorityState.Observed : PerfMeterExternalArtifactAuthorityState.Unknown,
				containsGpuCaptureData: storageMode == PerfMeterExternalArtifactStorageMode.MetadataOnly
					? PerfMeterExternalArtifactContentState.Unknown
					: finalized
						? PerfMeterExternalArtifactContentState.Present
						: PerfMeterExternalArtifactContentState.Absent,
				privacyFlags: PerfMeterExternalArtifactPrivacyFlags.ContainsGpuCaptureData |
					PerfMeterExternalArtifactPrivacyFlags.Sensitive |
					PerfMeterExternalArtifactPrivacyFlags.RequiresReview,
				storageMode: storageMode,
				quotaBytes: PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
				sharePolicy: storageMode == PerfMeterExternalArtifactStorageMode.MetadataOnly
					? PerfMeterExternalArtifactSharePolicy.DoNotShare
					: PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare,
				sizeBytes: sizeBytes,
				observedSourceSha256: sourceHash,
				postCopySha256: postCopyHash,
				warning: warning)
				.WithSourceFileIdentitySha256(identityHash)
				.ToSnapshot();
		}

		private static void FinishSource(PerfMeterRenderDocStorageReservation reservation)
		{
			TryFinishSource(reservation, out _);
		}

		private static bool TryFinishSource(PerfMeterRenderDocStorageReservation reservation, out string error)
		{
			error = string.Empty;
			if (reservation == null || reservation.IsReleased)
			{
				return true;
			}

			SggRdResult terminalResult = reservation.SetState(PerfMeterRenderDocStorageState.Terminal, out error);
			if (terminalResult == SggRdResult.Ok)
			{
				return true;
			}

			string terminalError = error;
			SggRdResult abortResult = reservation.Abort(out string abortError);
			error = string.IsNullOrEmpty(abortError)
				? terminalError
				: terminalError + "; " + abortError;
			return abortResult == SggRdResult.Ok;
		}

		private static bool IsValidArtifact(SggRdCaptureTokenV1 token, SggRdArtifactV1 artifact)
		{
			if (token.StructSize < PerfMeterRenderDocAbiV1.CaptureTokenSizeAsUInt ||
				token.StartUnixNanoseconds == 0u ||
				artifact.StructSize < PerfMeterRenderDocAbiV1.ArtifactSizeAsUInt ||
				artifact.Index < token.CountBefore ||
				artifact.ObservedUnixNanoseconds < token.StartUnixNanoseconds)
			{
				return false;
			}

			ulong startSeconds = token.StartUnixNanoseconds / 1000000000u;
			ulong earliest = startSeconds > 5u ? startSeconds - 5u : 0u;
			ulong latest = startSeconds > ulong.MaxValue - 30u ? ulong.MaxValue : startSeconds + 30u;
			return artifact.RenderDocTimestampSeconds >= earliest && artifact.RenderDocTimestampSeconds <= latest;
		}

		private bool IsTotalExpired(long operationStart)
		{
			return ElapsedMilliseconds(operationStart, _clock.Timestamp) >= PerfMeterRenderDocFinalizationPolicy.TotalMilliseconds;
		}

		private long ElapsedMilliseconds(long start, long end)
		{
			if (end <= start)
			{
				return 0L;
			}

			long ticks = end - start;
			return ticks > long.MaxValue / 1000L
				? long.MaxValue
				: ticks * 1000L / _clock.Frequency;
		}

		private static bool SamplesEqual(PerfMeterRenderDocFileSample left, PerfMeterRenderDocFileSample right)
		{
			return left.SizeBytes == right.SizeBytes &&
				left.LastWriteTicks == right.LastWriteTicks &&
				FixedBytesEqual(left.Identity, right.Identity);
		}

		private static bool FixedBytesEqual(byte[] left, byte[] right)
		{
			if (left == null || right == null || left.Length == 0 || left.Length != right.Length)
			{
				return false;
			}

			int difference = 0;
			for (int index = 0; index < left.Length; index++)
			{
				difference |= left[index] ^ right[index];
			}

			return difference == 0;
		}

		private static string HashIdentity(byte[] identity, ulong nonce)
		{
			byte[] input = new byte[(identity?.Length ?? 0) + sizeof(ulong)];
			if (identity != null)
			{
				Buffer.BlockCopy(identity, 0, input, 0, identity.Length);
			}

			for (int index = 0; index < sizeof(ulong); index++)
			{
				input[input.Length - sizeof(ulong) + index] = (byte)(nonce >> (index * 8));
			}

			using (SHA256 sha256 = SHA256.Create())
			{
				return ToHex(sha256.ComputeHash(input));
			}
		}

		private static bool IsCanceled(Func<bool> predicate)
		{
			if (predicate == null)
			{
				return false;
			}

			try
			{
				return predicate();
			}
			catch (Exception)
			{
				return true;
			}
		}

		private static string CombineErrors(string first, string second)
		{
			if (string.IsNullOrEmpty(first))
			{
				return second ?? string.Empty;
			}
			return string.IsNullOrEmpty(second) ? first : first + "; " + second;
		}

		private static string ToHex(byte[] bytes)
		{
			char[] characters = new char[bytes.Length * 2];
			const string alphabet = "0123456789abcdef";
			for (int index = 0; index < bytes.Length; index++)
			{
				characters[index * 2] = alphabet[bytes[index] >> 4];
				characters[index * 2 + 1] = alphabet[bytes[index] & 0xf];
			}

			return new string(characters);
		}

		private sealed class StopwatchClock : IPerfMeterRenderDocMonotonicClock
		{
			public long Timestamp => Stopwatch.GetTimestamp();
			public long Frequency => Stopwatch.Frequency;
			public void Delay(TimeSpan delay) => Thread.Sleep(delay);
		}

		private sealed class WindowsFileBindingFactory : IPerfMeterRenderDocFileBindingFactory
		{
			public SggRdResult TryOpen(string path, out IPerfMeterRenderDocFileBinding binding, out string error)
			{
				binding = null;
				error = string.Empty;
				if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				{
					error = "renderdoc_file_identity_unsupported";
					return SggRdResult.UnsupportedPlatform;
				}

				try
				{
					binding = new WindowsFileBinding(path);
					return SggRdResult.Ok;
				}
				catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
				{
					error = "renderdoc_file_open_failed";
					return SggRdResult.CaptureNotObserved;
				}
			}
		}

		private sealed class WindowsFileBinding : IPerfMeterRenderDocFileBinding
		{
			private readonly FileStream _stream;

			internal WindowsFileBinding(string path)
			{
				_stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 81920, FileOptions.SequentialScan);
			}

			public SggRdResult TrySample(out PerfMeterRenderDocFileSample sample, out string error)
			{
				sample = default;
				error = string.Empty;
				if (!GetFileInformationByHandle(_stream.SafeFileHandle.DangerousGetHandle(), out ByHandleFileInformation information))
				{
					error = "renderdoc_file_identity_failed";
					return SggRdResult.InternalError;
				}

				byte[] identity = new byte[12];
				WriteUInt32(identity, 0, information.VolumeSerialNumber);
				WriteUInt32(identity, 4, information.FileIndexHigh);
				WriteUInt32(identity, 8, information.FileIndexLow);
				long size = ((long)information.FileSizeHigh << 32) | information.FileSizeLow;
				long writeTicks = ((long)information.LastWriteTimeHigh << 32) | information.LastWriteTimeLow;
				sample = new PerfMeterRenderDocFileSample(identity, size, writeTicks);
				return SggRdResult.Ok;
			}

			public SggRdResult TryComputeSha256(
				long maximumBytes,
				Func<bool> shouldStop,
				out string sha256,
				out string error)
			{
				sha256 = string.Empty;
				error = string.Empty;
				try
				{
					_stream.Position = 0L;
					using (SHA256 algorithm = SHA256.Create())
					{
						byte[] buffer = new byte[81920];
						long total = 0L;
						int read;
						while ((read = _stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							bool stopped = IsCanceled(shouldStop);
							if (stopped || read > maximumBytes - total)
							{
								error = stopped ? "renderdoc_file_hash_stopped" : "renderdoc_storage_payload_limit_exceeded";
								return SggRdResult.CaptureFailed;
							}

							algorithm.TransformBlock(buffer, 0, read, null, 0);
							total += read;
						}

						algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
						if (IsCanceled(shouldStop))
						{
							error = "renderdoc_file_hash_stopped";
							return SggRdResult.CaptureFailed;
						}
						sha256 = ToHex(algorithm.Hash);
					}
					return SggRdResult.Ok;
				}
				catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
				{
					error = "renderdoc_file_hash_failed";
					return SggRdResult.InternalError;
				}
			}

			public SggRdResult TryCopyTo(
				string destinationPath,
				long maximumBytes,
				Func<bool> shouldStop,
				out string error)
			{
				error = string.Empty;
				try
				{
					_stream.Position = 0L;
					using (FileStream destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
					{
						byte[] buffer = new byte[81920];
						long total = 0L;
						int read;
						while ((read = _stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							bool stopped = IsCanceled(shouldStop);
							if (stopped || read > maximumBytes - total)
							{
								error = stopped ? "renderdoc_copy_stopped" : "renderdoc_storage_payload_limit_exceeded";
								return SggRdResult.CaptureFailed;
							}
							destination.Write(buffer, 0, read);
							total += read;
						}
						destination.Flush(true);
						if (IsCanceled(shouldStop))
						{
							error = "renderdoc_copy_stopped";
							return SggRdResult.CaptureFailed;
						}
					}
					return SggRdResult.Ok;
				}
				catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
				{
					error = "renderdoc_copy_failed";
					return SggRdResult.InternalError;
				}
			}

			public void Dispose() => _stream.Dispose();

			private static void WriteUInt32(byte[] bytes, int offset, uint value)
			{
				bytes[offset] = (byte)value;
				bytes[offset + 1] = (byte)(value >> 8);
				bytes[offset + 2] = (byte)(value >> 16);
				bytes[offset + 3] = (byte)(value >> 24);
			}

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool GetFileInformationByHandle(IntPtr file, out ByHandleFileInformation information);

			[StructLayout(LayoutKind.Sequential)]
			private struct ByHandleFileInformation
			{
				internal uint FileAttributes;
				internal uint CreationTimeLow;
				internal uint CreationTimeHigh;
				internal uint LastAccessTimeLow;
				internal uint LastAccessTimeHigh;
				internal uint LastWriteTimeLow;
				internal uint LastWriteTimeHigh;
				internal uint VolumeSerialNumber;
				internal uint FileSizeHigh;
				internal uint FileSizeLow;
				internal uint NumberOfLinks;
				internal uint FileIndexHigh;
				internal uint FileIndexLow;
			}
		}
	}
}
