using System;
using System.Diagnostics;
using System.IO;
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
			string warning,
			SggRdCaptureTokenV1 token = default,
			SggRdArtifactV1 observedArtifact = default,
			IPerfMeterNativeExternalArtifactPayloadSource payloadSource = null)
		{
			Result = result;
			Artifact = artifact;
			RetainedPayloadPath = retainedPayloadPath ?? string.Empty;
			Warning = warning ?? string.Empty;
			Token = token;
			ObservedArtifact = observedArtifact;
			PayloadSource = payloadSource;
		}

		internal SggRdResult Result { get; }
		internal PerfMeterExternalArtifactSnapshot Artifact { get; }
		internal string RetainedPayloadPath { get; }
		internal string Warning { get; }
		internal SggRdCaptureTokenV1 Token { get; }
		internal SggRdArtifactV1 ObservedArtifact { get; }
		internal IPerfMeterNativeExternalArtifactPayloadSource PayloadSource { get; }
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
			: this(storage, new PerfMeterRenderDocWindowsFileSystem.FileBindingFactory(), new StopwatchClock())
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
				out SggRdArtifactV1 observedArtifact,
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

			if (!TryCreateCaptureComments(preflight, token, observedArtifact, out string comments))
			{
				FinishSource(sourceReservation);
				return Failed(preflight, SggRdResult.InternalError, "renderdoc_capture_comments_invalid", 0L, string.Empty, string.Empty);
			}
			SggRdResult commentsResult = bridge.SetCaptureComments(token, sourcePath, comments);
			if (commentsResult != SggRdResult.Ok)
			{
				FinishSource(sourceReservation);
				return Failed(preflight, commentsResult, "renderdoc_capture_comments_failed", 0L, string.Empty, string.Empty);
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
					SggRdResult copyReleaseResult = copyReservation.Release(out string copyReleaseError);
					if (copyReleaseResult != SggRdResult.Ok)
					{
						SggRdResult copyCleanupResult = _storage.TryDeleteOwnedRoot(copyReservation.RootPath, out string copyCleanupError);
						return Failed(
							preflight,
							copyCleanupResult == SggRdResult.Ok ? copyReleaseResult : SggRdResult.InternalError,
							CombineErrors(copyReleaseError, copyCleanupError),
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
				PerfMeterExternalArtifactSnapshot snapshot = CreateSnapshot(
					preflight,
					storageMode,
					payloadBytes,
					sourceHash,
					postCopyHash,
					identityHash,
					string.Empty,
					true);
				IPerfMeterNativeExternalArtifactPayloadSource payloadSource = storageMode == PerfMeterExternalArtifactStorageMode.Copy
					? new RetainedCopyPayloadSource(
						_storage,
						_fileBindings,
						copyReservation,
						retainedPath,
						payloadBytes,
						postCopyHash)
					: storageMode == PerfMeterExternalArtifactStorageMode.Embed
						? new RetainedEmbedPayloadSource(
							_storage,
							_fileBindings,
							sourceReservation,
							sourcePath,
							payloadBytes,
							sourceHash)
						: null;
				return new PerfMeterRenderDocFinalizationResult(
					SggRdResult.Ok,
					snapshot,
					retainedPath,
					string.Empty,
					token,
					observedArtifact,
					payloadSource);
			}
		}

		private SggRdResult TryObserveCandidate(
			IPerfMeterRenderDocBridge bridge,
			SggRdCaptureTokenV1 token,
			long operationStart,
			Func<bool> isCancellationRequested,
			out string sourcePath,
			out long candidateTimestamp,
			out SggRdArtifactV1 observedArtifact,
			out string error)
		{
			sourcePath = string.Empty;
			candidateTimestamp = 0L;
			observedArtifact = default;
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
						observedArtifact = selectedArtifact;
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
					if (openResult != SggRdResult.CaptureNotObserved)
					{
						return openResult;
					}
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
			bool hasSourceEvidence = finalized &&
				sizeBytes > 0L &&
				!string.IsNullOrEmpty(sourceHash) &&
				!string.IsNullOrEmpty(identityHash);
			bool requiresPostCopyEvidence = storageMode == PerfMeterExternalArtifactStorageMode.Copy ||
				storageMode == PerfMeterExternalArtifactStorageMode.Embed;
			bool hasRequiredCopyEvidence = !requiresPostCopyEvidence ||
				(!string.IsNullOrEmpty(postCopyHash) && string.Equals(sourceHash, postCopyHash, StringComparison.Ordinal));
			bool authoritative = hasSourceEvidence && hasRequiredCopyEvidence;
			return new PerfMeterExternalArtifactOptions(
				artifactId: baseline.ArtifactId,
				artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
				toolId: "renderdoc",
				toolVersion: baseline.ToolVersion,
				requestId: baseline.RequestId,
				hostNamespace: baseline.HostNamespace,
				associationState: finalized ? PerfMeterExternalArtifactAssociationState.BridgeAuthenticated : PerfMeterExternalArtifactAssociationState.Unverified,
				finalizationState: finalized ? PerfMeterExternalArtifactFinalizationState.Finalized : PerfMeterExternalArtifactFinalizationState.Failed,
				authorityState: authoritative ? PerfMeterExternalArtifactAuthorityState.Authenticated : finalized ? PerfMeterExternalArtifactAuthorityState.Observed : PerfMeterExternalArtifactAuthorityState.Unknown,
				containsGpuCaptureData: finalized ? PerfMeterExternalArtifactContentState.Present : PerfMeterExternalArtifactContentState.Absent,
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
			if (terminalResult == SggRdResult.Ok && reservation.Release(out error) == SggRdResult.Ok)
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

		private static bool TryCreateCaptureComments(
			PerfMeterRenderDocPreflight preflight,
			SggRdCaptureTokenV1 token,
			SggRdArtifactV1 artifact,
			out string comments)
		{
			comments = "sgg.perfmeter.renderdoc\n" +
				"version=1\n" +
				"request_nonce=" + token.RequestNonce.ToString("x16", System.Globalization.CultureInfo.InvariantCulture) + "\n" +
				"generation=" + preflight.Reservation.Request.Generation.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" +
				"storage_mode=" + preflight.ArtifactOptions.StorageMode + "\n" +
				"capture_index=" + artifact.Index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" +
				"start_unix_nanoseconds=" + token.StartUnixNanoseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" +
				"renderdoc_timestamp_seconds=" + artifact.RenderDocTimestampSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" +
				"observed_unix_nanoseconds=" + artifact.ObservedUnixNanoseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n";
			return PerfMeterRenderDocUtf8.TryEncode(
				comments,
				PerfMeterRenderDocAbiV1.MaxCommentsBytes,
				false,
				out byte[] bytes) && bytes.Length > 0;
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

		private sealed class RetainedCopyPayloadSource : IPerfMeterNativeExternalArtifactPayloadSource
		{
			private readonly PerfMeterRenderDocStorage _storage;
			private readonly IPerfMeterRenderDocFileBindingFactory _fileBindings;
			private readonly PerfMeterRenderDocStorageRequest _request;
			private readonly ulong _requestNonce;
			private readonly string _rootPath;
			private readonly string _payloadPath;
			private readonly long _expectedBytes;
			private readonly string _expectedHash;

			internal RetainedCopyPayloadSource(
				PerfMeterRenderDocStorage storage,
				IPerfMeterRenderDocFileBindingFactory fileBindings,
				PerfMeterRenderDocStorageReservation reservation,
				string payloadPath,
				long expectedBytes,
				string expectedHash)
			{
				_storage = storage;
				_fileBindings = fileBindings;
				_request = reservation.Request;
				_requestNonce = reservation.RequestNonce;
				_rootPath = reservation.RootPath;
				_payloadPath = payloadPath ?? string.Empty;
				_expectedBytes = expectedBytes;
				_expectedHash = expectedHash ?? string.Empty;
			}

			public bool TryValidate(Func<bool> shouldStop, out string error)
			{
				error = string.Empty;
				if (IsCanceled(shouldStop))
				{
					error = "renderdoc_copy_descriptor_validation_canceled";
					return false;
				}
				SggRdResult inspectResult = _storage.TryInspectOwnedRoot(
					_rootPath,
					out PerfMeterRenderDocStorageMarker marker,
					out _,
					out error);
				if (inspectResult != SggRdResult.Ok ||
					marker.State != PerfMeterRenderDocStorageState.Terminal ||
					marker.RequestNonce != _requestNonce ||
					marker.Generation != _request.Generation ||
					!string.Equals(marker.SessionId, _request.SessionId, StringComparison.Ordinal) ||
					!string.Equals(_payloadPath, Path.Combine(_rootPath, "capture.rdc"), StringComparison.OrdinalIgnoreCase))
				{
					error = string.IsNullOrEmpty(error) ? "renderdoc_copy_descriptor_marker_invalid" : error;
					return false;
				}

				SggRdResult openResult = _fileBindings.TryOpen(
					_payloadPath,
					out IPerfMeterRenderDocFileBinding binding,
					out error);
				if (openResult != SggRdResult.Ok)
				{
					error = string.IsNullOrEmpty(error) ? "renderdoc_copy_descriptor_open_failed" : error;
					return false;
				}

				using (binding)
				{
					if (IsCanceled(shouldStop))
					{
						error = "renderdoc_copy_descriptor_validation_canceled";
						return false;
					}
					SggRdResult sampleResult = binding.TrySample(out PerfMeterRenderDocFileSample before, out error);
					if (sampleResult != SggRdResult.Ok || before.SizeBytes != _expectedBytes)
					{
						error = "renderdoc_copy_descriptor_size_changed";
						return false;
					}

					SggRdResult hashResult = binding.TryComputeSha256(
						PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
						shouldStop,
						out string hash,
						out error);
					if (hashResult != SggRdResult.Ok || !string.Equals(hash, _expectedHash, StringComparison.Ordinal))
					{
						error = "renderdoc_copy_descriptor_hash_changed";
						return false;
					}

					sampleResult = binding.TrySample(out PerfMeterRenderDocFileSample after, out error);
					if (sampleResult != SggRdResult.Ok || !SamplesEqual(before, after))
					{
						error = "renderdoc_copy_descriptor_identity_changed";
						return false;
					}
				}

				return true;
			}

			public bool TryStageEmbed(
				string stagingPath,
				long additionalStagingBytes,
				Func<bool> shouldStop,
				out PerfMeterNativeEmbeddedArtifact stagedArtifact,
				out string error)
			{
				stagedArtifact = default;
				error = "native_embed_payload_source_unavailable";
				return false;
			}

			public bool TryCompleteEmbed(bool committed, out string warning)
			{
				warning = string.Empty;
				return true;
			}
		}

		private sealed class RetainedEmbedPayloadSource : IPerfMeterNativeExternalArtifactPayloadSource
		{
			private readonly object _gate = new object();
			private readonly PerfMeterRenderDocStorage _storage;
			private readonly IPerfMeterRenderDocFileBindingFactory _fileBindings;
			private readonly PerfMeterRenderDocStorageRequest _request;
			private readonly ulong _requestNonce;
			private readonly string _rootPath;
			private readonly string _payloadPath;
			private readonly long _expectedBytes;
			private readonly string _expectedHash;
			private PerfMeterRenderDocStorageReservation _embedReservation;

			internal RetainedEmbedPayloadSource(
				PerfMeterRenderDocStorage storage,
				IPerfMeterRenderDocFileBindingFactory fileBindings,
				PerfMeterRenderDocStorageReservation reservation,
				string payloadPath,
				long expectedBytes,
				string expectedHash)
			{
				_storage = storage;
				_fileBindings = fileBindings;
				_request = reservation.Request;
				_requestNonce = reservation.RequestNonce;
				_rootPath = reservation.RootPath;
				_payloadPath = payloadPath ?? string.Empty;
				_expectedBytes = expectedBytes;
				_expectedHash = expectedHash ?? string.Empty;
			}

			public bool TryValidate(Func<bool> shouldStop, out string error)
			{
				lock (_gate)
				{
					return TryValidateSource(shouldStop, out error);
				}
			}

			public bool TryStageEmbed(
				string stagingPath,
				long additionalStagingBytes,
				Func<bool> shouldStop,
				out PerfMeterNativeEmbeddedArtifact stagedArtifact,
				out string error)
			{
				lock (_gate)
				{
					stagedArtifact = default;
					error = string.Empty;
					if (_embedReservation != null ||
						!IsSafeStagingPath(stagingPath) ||
						!TryValidateSource(shouldStop, out error))
					{
						error = string.IsNullOrEmpty(error) ? "renderdoc_embed_staging_invalid" : error;
						return false;
					}

					SggRdResult reserveResult = _storage.TryReserveEmbed(
						_request,
						_expectedBytes,
						additionalStagingBytes,
						out _embedReservation,
						out error);
					if (reserveResult != SggRdResult.Ok)
					{
						_embedReservation = null;
						return false;
					}

					bool succeeded = false;
					try
					{
						SggRdResult openResult = _fileBindings.TryOpen(
							_payloadPath,
							out IPerfMeterRenderDocFileBinding source,
							out error);
						if (openResult != SggRdResult.Ok)
						{
							return false;
						}

						string destinationPath = Path.Combine(stagingPath, "external", "renderdoc", "capture.rdc");
						Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
						using (source)
						{
							if (source.TrySample(out PerfMeterRenderDocFileSample before, out error) != SggRdResult.Ok ||
								before.SizeBytes != _expectedBytes ||
								source.TryComputeSha256(
									PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
									shouldStop,
									out string sourceHash,
									out error) != SggRdResult.Ok ||
								!string.Equals(sourceHash, _expectedHash, StringComparison.Ordinal) ||
								source.TryCopyTo(
									destinationPath,
									PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
									shouldStop,
									out error) != SggRdResult.Ok ||
								source.TrySample(out PerfMeterRenderDocFileSample after, out error) != SggRdResult.Ok ||
								!SamplesEqual(before, after))
							{
								error = string.IsNullOrEmpty(error) ? "renderdoc_embed_source_changed" : error;
								return false;
							}
						}

						SggRdResult destinationResult = _fileBindings.TryOpen(
							destinationPath,
							out IPerfMeterRenderDocFileBinding destination,
							out error);
						if (destinationResult != SggRdResult.Ok)
						{
							return false;
						}
						using (destination)
						{
							if (destination.TrySample(out PerfMeterRenderDocFileSample before, out error) != SggRdResult.Ok ||
								before.SizeBytes != _expectedBytes ||
								destination.TryComputeSha256(
									PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
									shouldStop,
									out string destinationHash,
									out error) != SggRdResult.Ok ||
								!string.Equals(destinationHash, _expectedHash, StringComparison.Ordinal) ||
								destination.TrySample(out PerfMeterRenderDocFileSample after, out error) != SggRdResult.Ok ||
								!SamplesEqual(before, after))
							{
								error = string.IsNullOrEmpty(error) ? "renderdoc_embed_destination_changed" : error;
								return false;
							}
						}

						byte[] markerBytes = PerfMeterRenderDocEmbeddedBundleStorage.CreateMarkerBytes(
							_request,
							_requestNonce,
							_storage.CurrentUtc,
							_expectedBytes,
							_expectedHash);
						string markerPath = Path.Combine(
							stagingPath,
							PerfMeterNativeExternalArtifactSourceDescriptor.EmbeddedMarkerRelativePath);
						using (FileStream marker = new FileStream(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
						{
							marker.Write(markerBytes, 0, markerBytes.Length);
							marker.Flush(true);
						}

						using (SHA256 sha256 = SHA256.Create())
						{
							stagedArtifact = new PerfMeterNativeEmbeddedArtifact(
								PerfMeterNativeExternalArtifactSourceDescriptor.EmbeddedPayloadRelativePath,
								_expectedBytes,
								_expectedHash,
								PerfMeterNativeExternalArtifactSourceDescriptor.EmbeddedMarkerRelativePath,
								markerBytes.LongLength,
								ToHex(sha256.ComputeHash(markerBytes)));
						}
						succeeded = true;
						return true;
					}
					finally
					{
						if (!succeeded)
						{
							AbortEmbedReservation(out _);
						}
					}
				}
			}

			public bool TryCompleteEmbed(bool committed, out string warning)
			{
				lock (_gate)
				{
					warning = string.Empty;
					bool cleaned = AbortEmbedReservation(out warning);
					if (!committed || !cleaned)
					{
						return cleaned;
					}

					SggRdResult retentionResult = _storage.TryCleanup(
						(sessionId, generation) => false,
						out _,
						out warning);
					return retentionResult == SggRdResult.Ok;
				}
			}

			private bool TryValidateSource(Func<bool> shouldStop, out string error)
			{
				error = string.Empty;
				if (IsCanceled(shouldStop))
				{
					error = "renderdoc_embed_validation_canceled";
					return false;
				}
				SggRdResult inspectResult = _storage.TryInspectOwnedRoot(
					_rootPath,
					out PerfMeterRenderDocStorageMarker marker,
					out _,
					out error);
				return inspectResult == SggRdResult.Ok &&
					marker.State == PerfMeterRenderDocStorageState.Terminal &&
					marker.RequestNonce == _requestNonce &&
					marker.Generation == _request.Generation &&
					string.Equals(marker.SessionId, _request.SessionId, StringComparison.Ordinal) &&
					string.Equals(_payloadPath, Path.Combine(_rootPath, "capture.rdc"), StringComparison.OrdinalIgnoreCase);
			}

			private bool IsSafeStagingPath(string stagingPath)
			{
				try
				{
					string bundleRoot = Path.GetFullPath(Path.Combine(
						_storage.ProjectRoot,
						PerfMeterCaptureBundleExporter.RelativeBundleRoot));
					string fullPath = Path.GetFullPath(stagingPath);
					return string.Equals(
						Path.GetDirectoryName(fullPath)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
						bundleRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
						StringComparison.OrdinalIgnoreCase) &&
						Path.GetFileName(fullPath).StartsWith(".sgg-perfmeter-staging-", StringComparison.OrdinalIgnoreCase) &&
						Directory.Exists(fullPath) &&
						(File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) == 0 &&
						File.Exists(Path.Combine(fullPath, ".sgg-perfmeter-bundle"));
				}
				catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
				{
					return false;
				}
			}

			private bool AbortEmbedReservation(out string warning)
			{
				warning = string.Empty;
				if (_embedReservation == null)
				{
					return true;
				}

				PerfMeterRenderDocStorageReservation reservation = _embedReservation;
				_embedReservation = null;
				SggRdResult result = reservation.Abort(out warning);
				return result == SggRdResult.Ok;
			}
		}

		private sealed class StopwatchClock : IPerfMeterRenderDocMonotonicClock
		{
			public long Timestamp => Stopwatch.GetTimestamp();
			public long Frequency => Stopwatch.Frequency;
			public void Delay(TimeSpan delay) => Thread.Sleep(delay);
		}

	}
}
