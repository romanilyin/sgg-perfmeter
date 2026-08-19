using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal enum PerfMeterRenderDocStoragePool
	{
		Source = 0,
		CopyEmbed = 1
	}

	internal enum PerfMeterRenderDocStorageState
	{
		Preflight = 0,
		Capturing = 1,
		AwaitingArtifact = 2,
		Finalizing = 3,
		Terminal = 4,
		CleanupPending = 5,
		LostSession = 6
	}

	internal static class PerfMeterRenderDocStoragePolicy
	{
		internal const string RelativeSourceRoot = "Temp/PerfMeter/RenderDoc";
		internal const string RelativeCopyRoot = "Temp/PerfMeter/RenderDocCopies";
		internal const string MarkerFileName = ".sgg-perfmeter-renderdoc";
		internal const string MarkerSchema = "sgg.perfmeter.renderdoc.storage";
		internal const int MarkerVersion = 1;
		internal const long MaxPayloadBytes = 512L * 1024L * 1024L;
		internal const long SourcePoolBytes = 2L * 1024L * 1024L * 1024L;
		internal const long CopyEmbedPoolBytes = 2L * 1024L * 1024L * 1024L;
		internal const int MaxTerminalItems = 16;
		internal const int RetentionDays = 7;
		internal const int StaleNonterminalHours = 24;
		internal const long FreeSpaceFloorBytes = 1L * 1024L * 1024L * 1024L;
		internal const long MetadataReserveBytes = 1L * 1024L * 1024L;
		internal const int IoAttempts = 3;
		internal const int PersistentCleanupAttempts = 3;
		internal const int FirstRetryDelayMilliseconds = 25;
		internal const int SecondRetryDelayMilliseconds = 50;
		internal const int MaxSessionIdLength = 128;
		internal const int MaxMarkerBytes = 4096;

		internal const long SourceReservationBytes = MaxPayloadBytes + MetadataReserveBytes;

		internal static long GetPoolCapacity(PerfMeterRenderDocStoragePool pool)
		{
			return pool == PerfMeterRenderDocStoragePool.Source ? SourcePoolBytes : CopyEmbedPoolBytes;
		}

		internal static long GetReservationBytes(PerfMeterRenderDocStoragePool pool, long payloadBytes)
		{
			return pool == PerfMeterRenderDocStoragePool.Source
				? SourceReservationBytes
				: checked(payloadBytes + MetadataReserveBytes);
		}
	}

	internal interface IPerfMeterRenderDocFreeSpaceProvider
	{
		long GetAvailableBytes(string path);
	}

	internal interface IPerfMeterRenderDocClock
	{
		DateTimeOffset UtcNow { get; }
	}

	internal interface IPerfMeterRenderDocNonceProvider
	{
		ulong NextNonce();
	}

	internal interface IPerfMeterRenderDocRetryDelay
	{
		void Delay(TimeSpan delay);
	}

	internal readonly struct PerfMeterRenderDocStorageRequest
	{
		internal PerfMeterRenderDocStorageRequest(string sessionId, ulong generation)
		{
			SessionId = sessionId ?? string.Empty;
			Generation = generation;
		}

		internal string SessionId { get; }
		internal ulong Generation { get; }
	}

	internal readonly struct PerfMeterRenderDocStorageMarker
	{
		internal PerfMeterRenderDocStorageMarker(
			ulong requestNonce,
			string sessionId,
			ulong generation,
			DateTimeOffset createdUtc,
			PerfMeterRenderDocStorageState state,
			DateTimeOffset stateUtc)
		{
			RequestNonce = requestNonce;
			SessionId = sessionId ?? string.Empty;
			Generation = generation;
			CreatedUtc = createdUtc;
			State = state;
			StateUtc = stateUtc;
		}

		internal ulong RequestNonce { get; }
		internal string SessionId { get; }
		internal ulong Generation { get; }
		internal DateTimeOffset CreatedUtc { get; }
		internal PerfMeterRenderDocStorageState State { get; }
		internal DateTimeOffset StateUtc { get; }
	}

	internal readonly struct PerfMeterRenderDocStoragePoolUsage
	{
		internal PerfMeterRenderDocStoragePoolUsage(
			PerfMeterRenderDocStoragePool pool,
			long ownedBytes,
			long reservedBytes,
			int itemCount,
			int terminalItemCount)
		{
			Pool = pool;
			OwnedBytes = Math.Max(0L, ownedBytes);
			ReservedBytes = Math.Max(0L, reservedBytes);
			ItemCount = Math.Max(0, itemCount);
			TerminalItemCount = Math.Max(0, terminalItemCount);
			CapacityBytes = PerfMeterRenderDocStoragePolicy.GetPoolCapacity(pool);
		}

		internal static PerfMeterRenderDocStoragePoolUsage Empty(PerfMeterRenderDocStoragePool pool)
		{
			return new PerfMeterRenderDocStoragePoolUsage(pool, 0L, 0L, 0, 0);
		}

		internal PerfMeterRenderDocStoragePool Pool { get; }
		internal long OwnedBytes { get; }
		internal long ReservedBytes { get; }
		internal int ItemCount { get; }
		internal int TerminalItemCount { get; }
		internal long CapacityBytes { get; }
		internal long AccountedBytes => SaturatingAdd(OwnedBytes, ReservedBytes);
		internal long AvailableQuotaBytes => Math.Max(0L, CapacityBytes - AccountedBytes);

		private static long SaturatingAdd(long left, long right)
		{
			return right > long.MaxValue - left ? long.MaxValue : left + right;
		}
	}

	internal readonly struct PerfMeterRenderDocStorageUsage
	{
		internal PerfMeterRenderDocStorageUsage(
			PerfMeterRenderDocStoragePoolUsage source,
			PerfMeterRenderDocStoragePoolUsage copyEmbed)
		{
			Source = source;
			CopyEmbed = copyEmbed;
		}

		internal PerfMeterRenderDocStoragePoolUsage Source { get; }
		internal PerfMeterRenderDocStoragePoolUsage CopyEmbed { get; }
	}

	internal sealed class PerfMeterRenderDocStorageReservation
	{
		private readonly PerfMeterRenderDocStorage _owner;
		private bool _released;

		internal PerfMeterRenderDocStorageReservation(
			PerfMeterRenderDocStorage owner,
			PerfMeterRenderDocStorageRequest request,
			PerfMeterRenderDocStoragePool pool,
			PerfMeterExternalArtifactStorageMode storageMode,
			ulong requestNonce,
			string rootPath,
			long reservedBytes)
		{
			_owner = owner;
			Request = request;
			Pool = pool;
			StorageMode = storageMode;
			RequestNonce = requestNonce;
			RootPath = rootPath ?? string.Empty;
			MarkerPath = Path.Combine(RootPath, PerfMeterRenderDocStoragePolicy.MarkerFileName);
			CapturePathTemplate = Path.Combine(RootPath, "capture");
			ReservedBytes = Math.Max(0L, reservedBytes);
		}

		internal PerfMeterRenderDocStorageRequest Request { get; }
		internal PerfMeterRenderDocStoragePool Pool { get; }
		internal PerfMeterExternalArtifactStorageMode StorageMode { get; }
		internal ulong RequestNonce { get; }
		internal string RootPath { get; }
		internal string MarkerPath { get; }
		internal string CapturePathTemplate { get; }
		internal long ReservedBytes { get; }
		internal bool IsReleased => _released;

		internal SggRdResult SetState(PerfMeterRenderDocStorageState state, out string error)
		{
			return _owner.TrySetState(this, state, out error);
		}

		internal SggRdResult Release(out string error)
		{
			return _owner.TryRelease(this, out error);
		}

		internal SggRdResult Abort(out string error)
		{
			return _owner.TryAbort(this, out error);
		}

		internal void MarkReleased()
		{
			_released = true;
		}

		internal bool BelongsTo(PerfMeterRenderDocStorage owner)
		{
			return ReferenceEquals(_owner, owner);
		}
	}

	internal sealed class PerfMeterRenderDocStorage
	{
		private readonly object _gate = new object();
		private readonly string _projectRoot;
		private readonly string _sourceRoot;
		private readonly string _copyRoot;
		private readonly StringComparison _pathComparison;
		private readonly IPerfMeterRenderDocFreeSpaceProvider _freeSpaceProvider;
		private readonly IPerfMeterRenderDocClock _clock;
		private readonly IPerfMeterRenderDocNonceProvider _nonceProvider;
		private readonly IPerfMeterRenderDocRetryDelay _retryDelay;
		private readonly Action<string> _deleteDirectory;
		private readonly bool _useAtomicOwnedDelete;
		private readonly PerfMeterRenderDocEmbeddedBundleStorage _embeddedBundleStorage;
		private readonly Dictionary<string, PerfMeterRenderDocStorageReservation> _reservations =
			new Dictionary<string, PerfMeterRenderDocStorageReservation>(StringComparer.Ordinal);

		internal PerfMeterRenderDocStorage()
			: this(Path.GetFullPath(Path.Combine(Application.dataPath, "..")))
		{
		}

		internal PerfMeterRenderDocStorage(
			string projectRoot,
			IPerfMeterRenderDocFreeSpaceProvider freeSpaceProvider = null,
			IPerfMeterRenderDocClock clock = null,
			IPerfMeterRenderDocNonceProvider nonceProvider = null,
			IPerfMeterRenderDocRetryDelay retryDelay = null,
			Action<string> deleteDirectory = null)
		{
			if (string.IsNullOrWhiteSpace(projectRoot))
			{
				throw new ArgumentException("A project root is required.", nameof(projectRoot));
			}

			_projectRoot = NormalizeDirectory(Path.GetFullPath(projectRoot));
			_sourceRoot = NormalizeDirectory(Path.GetFullPath(Path.Combine(_projectRoot, PerfMeterRenderDocStoragePolicy.RelativeSourceRoot)));
			_copyRoot = NormalizeDirectory(Path.GetFullPath(Path.Combine(_projectRoot, PerfMeterRenderDocStoragePolicy.RelativeCopyRoot)));
			_pathComparison = Path.DirectorySeparatorChar == '\\'
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;
			_freeSpaceProvider = freeSpaceProvider ?? new DefaultFreeSpaceProvider();
			_clock = clock ?? new DefaultClock();
			_nonceProvider = nonceProvider ?? new CryptographicNonceProvider();
			_retryDelay = retryDelay ?? new ThreadSleepRetryDelay();
			_deleteDirectory = deleteDirectory ?? (path => Directory.Delete(path, true));
			_useAtomicOwnedDelete = deleteDirectory == null;
			_embeddedBundleStorage = new PerfMeterRenderDocEmbeddedBundleStorage(_projectRoot, UtcNow);
		}

		internal string ProjectRoot => _projectRoot;
		internal string SourceRoot => _sourceRoot;
		internal string CopyRoot => _copyRoot;
		internal DateTimeOffset CurrentUtc => UtcNow();

		internal SggRdResult TryReserveSource(
			PerfMeterRenderDocStorageRequest request,
			out PerfMeterRenderDocStorageReservation reservation,
			out string error)
		{
			return TryReserve(
				request,
				PerfMeterExternalArtifactStorageMode.MetadataOnly,
				PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
				0L,
				out reservation,
				out error);
		}

		internal SggRdResult TryReserveCopyOrEmbed(
			PerfMeterRenderDocStorageRequest request,
			PerfMeterExternalArtifactStorageMode storageMode,
			long payloadBytes,
			out PerfMeterRenderDocStorageReservation reservation,
			out string error)
		{
			return TryReserve(request, storageMode, payloadBytes, 0L, out reservation, out error);
		}

		internal SggRdResult TryReserveEmbed(
			PerfMeterRenderDocStorageRequest request,
			long payloadBytes,
			long additionalStagingBytes,
			out PerfMeterRenderDocStorageReservation reservation,
			out string error)
		{
			return TryReserve(
				request,
				PerfMeterExternalArtifactStorageMode.Embed,
				payloadBytes,
				additionalStagingBytes,
				out reservation,
				out error);
		}

		internal SggRdResult TryReserve(
			PerfMeterRenderDocStorageRequest request,
			PerfMeterExternalArtifactStorageMode storageMode,
			long payloadBytes,
			long additionalStagingBytes,
			out PerfMeterRenderDocStorageReservation reservation,
			out string error)
		{
			reservation = null;
			error = string.Empty;
			lock (_gate)
			{
				try
				{
					if (!IsValidRequest(request) || additionalStagingBytes < 0L)
					{
						error = "renderdoc_storage_request_invalid";
						return SggRdResult.InvalidArgument;
					}

					PerfMeterRenderDocStoragePool pool;
					long reservationBytes;
					if (storageMode == PerfMeterExternalArtifactStorageMode.MetadataOnly)
					{
						pool = PerfMeterRenderDocStoragePool.Source;
						payloadBytes = PerfMeterRenderDocStoragePolicy.MaxPayloadBytes;
						reservationBytes = PerfMeterRenderDocStoragePolicy.SourceReservationBytes;
					}
					else if (storageMode == PerfMeterExternalArtifactStorageMode.Copy ||
						storageMode == PerfMeterExternalArtifactStorageMode.Embed)
					{
						if (payloadBytes <= 0L)
						{
							error = "renderdoc_storage_payload_invalid";
							return SggRdResult.InvalidArgument;
						}

						if (payloadBytes > PerfMeterRenderDocStoragePolicy.MaxPayloadBytes)
						{
							error = "renderdoc_storage_payload_limit_exceeded";
							return SggRdResult.CaptureFailed;
						}

						pool = PerfMeterRenderDocStoragePool.CopyEmbed;
						reservationBytes = checked(payloadBytes + PerfMeterRenderDocStoragePolicy.MetadataReserveBytes);
					}
					else
					{
						error = "renderdoc_storage_mode_invalid";
						return SggRdResult.InvalidArgument;
					}

					string root = GetPoolRoot(pool);
					if (!TryEnsureStorageBase(root, out error))
					{
						return SggRdResult.InvalidArgument;
					}
					if (!PerfMeterRenderDocStorageLock.TryAcquire(root, out IDisposable poolLease, out error))
					{
						return SggRdResult.InternalError;
					}

					using (poolLease)
					{
						SggRdResult cleanupResult = CleanupPoolInternal(
							pool,
							GetLiveSessionPredicate(),
							reservationBytes,
							includeStale: true,
							out error);
						if (cleanupResult != SggRdResult.Ok)
						{
							return cleanupResult;
						}

						PerfMeterRenderDocStoragePoolUsage usage;
						SggRdResult usageResult = GetPoolUsageInternal(pool, out usage, out error);
						if (usageResult != SggRdResult.Ok)
						{
							return usageResult;
						}

						if (Exceeds(usage.AccountedBytes, reservationBytes, usage.CapacityBytes))
						{
							error = "renderdoc_storage_quota_exceeded";
							return SggRdResult.CaptureFailed;
						}

						long availableBytes;
						try
						{
							availableBytes = _freeSpaceProvider.GetAvailableBytes(root);
						}
						catch (Exception exception) when (IsIoException(exception))
						{
							error = "renderdoc_storage_free_space_unavailable";
							return SggRdResult.InternalError;
						}

						long requiredFreeBytes;
						try
						{
							requiredFreeBytes = checked(
								PerfMeterRenderDocStoragePolicy.FreeSpaceFloorBytes +
								reservationBytes +
								additionalStagingBytes);
						}
						catch (OverflowException)
						{
							error = "renderdoc_storage_free_space_floor";
							return SggRdResult.CaptureFailed;
						}
						if (availableBytes < requiredFreeBytes)
						{
							error = "renderdoc_storage_free_space_floor";
							return SggRdResult.CaptureFailed;
						}

						for (int attempt = 0; attempt < PerfMeterRenderDocStoragePolicy.IoAttempts; attempt++)
						{
							ulong nonce = _nonceProvider.NextNonce();
							if (nonce == 0u)
							{
								continue;
							}

							string nonceDirectory = nonce.ToString("x16", CultureInfo.InvariantCulture);
							string candidate = Path.Combine(root, nonceDirectory);
							if (!TryValidateOwnedRootPath(candidate, root, rejectTraversalInput: false, out error))
							{
								return SggRdResult.InvalidArgument;
							}
							if (!PerfMeterRenderDocStorageLock.TryAcquire(candidate, out IDisposable candidateLease, out error))
							{
								return SggRdResult.InternalError;
							}

							using (candidateLease)
							{
								string cleanupCandidate = candidate + ".cleanup";
								if (Directory.Exists(candidate) || File.Exists(candidate) ||
									Directory.Exists(cleanupCandidate) || File.Exists(cleanupCandidate))
								{
									continue;
								}

								Directory.CreateDirectory(candidate);
								if (!TryValidateOwnedRootPath(candidate, root, rejectTraversalInput: false, out error))
								{
									return SggRdResult.InvalidArgument;
								}

								DateTimeOffset now = UtcNow();
								PerfMeterRenderDocStorageMarker marker = new PerfMeterRenderDocStorageMarker(
									nonce,
									request.SessionId,
									request.Generation,
									now,
									PerfMeterRenderDocStorageState.Preflight,
									now);
								SggRdResult markerResult = TryWriteMarker(candidate, marker, out error);
								if (markerResult != SggRdResult.Ok)
								{
									TryRollbackEmptyCreatedRoot(candidate);
									return markerResult;
								}

								reservation = new PerfMeterRenderDocStorageReservation(
									this,
									request,
									pool,
									storageMode,
									nonce,
									candidate,
									reservationBytes);
								_reservations.Add(candidate, reservation);
								return SggRdResult.Ok;
							}
						}

						error = "renderdoc_storage_nonce_collision";
						return SggRdResult.InternalError;
					}
				}
				catch (Exception exception) when (IsIoException(exception))
				{
					error = "renderdoc_storage_io_error";
					return SggRdResult.InternalError;
				}
			}
		}

		internal SggRdResult TrySetState(
			PerfMeterRenderDocStorageReservation reservation,
			PerfMeterRenderDocStorageState state,
			out string error)
		{
			error = string.Empty;
			lock (_gate)
			{
				if (reservation == null || !reservation.BelongsTo(this) || reservation.IsReleased ||
					!IsValidState(state) || !_reservations.ContainsKey(reservation.RootPath))
				{
					error = "renderdoc_storage_reservation_invalid";
					return SggRdResult.InvalidArgument;
				}

				return TrySetStateInternal(reservation.RootPath, reservation.Request, reservation.RequestNonce, state, out error);
			}
		}

		internal SggRdResult TryRelease(
			PerfMeterRenderDocStorageReservation reservation,
			out string error)
		{
			error = string.Empty;
			lock (_gate)
			{
				if (reservation == null || !reservation.BelongsTo(this))
				{
					error = "renderdoc_storage_reservation_invalid";
					return SggRdResult.InvalidArgument;
				}

				if (reservation.IsReleased)
				{
					return SggRdResult.Ok;
				}

				_reservations.Remove(reservation.RootPath);
				reservation.MarkReleased();
				return SggRdResult.Ok;
			}
		}

		internal SggRdResult TryAbort(
			PerfMeterRenderDocStorageReservation reservation,
			out string error)
		{
			error = string.Empty;
			lock (_gate)
			{
				if (reservation == null || !reservation.BelongsTo(this) || reservation.IsReleased ||
					!_reservations.ContainsKey(reservation.RootPath))
				{
					error = "renderdoc_storage_reservation_invalid";
					return SggRdResult.InvalidArgument;
				}

				SggRdResult stateResult = TrySetStateInternal(
					reservation.RootPath,
					reservation.Request,
					reservation.RequestNonce,
					PerfMeterRenderDocStorageState.CleanupPending,
					out error);
				if (stateResult != SggRdResult.Ok)
				{
					return stateResult;
				}

				return TryDeleteOwnedRootInternal(
					reservation.RootPath,
					allowLostSession: false,
					allowStaleNonterminal: false,
					out error);
			}
		}

		internal SggRdResult TryMarkTerminal(
			PerfMeterRenderDocStorageReservation reservation,
			out string error)
		{
			return TrySetState(reservation, PerfMeterRenderDocStorageState.Terminal, out error);
		}

		internal SggRdResult TryGetUsage(
			out PerfMeterRenderDocStorageUsage usage,
			out string error)
		{
			lock (_gate)
			{
				SggRdResult sourceResult = GetPoolUsageInternal(PerfMeterRenderDocStoragePool.Source, out PerfMeterRenderDocStoragePoolUsage source, out error);
				if (sourceResult != SggRdResult.Ok)
				{
					usage = default;
					return sourceResult;
				}

				SggRdResult copyResult = GetPoolUsageInternal(PerfMeterRenderDocStoragePool.CopyEmbed, out PerfMeterRenderDocStoragePoolUsage copy, out error);
				if (copyResult != SggRdResult.Ok)
				{
					usage = default;
					return copyResult;
				}

				usage = new PerfMeterRenderDocStorageUsage(source, copy);
				return SggRdResult.Ok;
			}
		}

		internal SggRdResult TryInspectOwnedRoot(
			string rootPath,
			out PerfMeterRenderDocStorageMarker marker,
			out long ownedBytes,
			out string error)
		{
			lock (_gate)
			{
				return TryInspectOwnedRootInternal(rootPath, out marker, out ownedBytes, out error);
			}
		}

		internal SggRdResult TryValidatePayload(
			PerfMeterRenderDocStorageReservation reservation,
			string payloadPath,
			out long payloadBytes,
			out string error)
		{
			return TryValidatePayloadInternal(reservation, payloadPath, true, out payloadBytes, out error);
		}

		internal SggRdResult TryValidatePayloadPath(
			PerfMeterRenderDocStorageReservation reservation,
			string payloadPath,
			out string error)
		{
			return TryValidatePayloadInternal(reservation, payloadPath, false, out _, out error);
		}

		internal SggRdResult TryValidateRetainedSourcePayloadPath(
			string rootPath,
			string payloadPath,
			out string error)
		{
			error = string.Empty;
			lock (_gate)
			{
				if (!TryValidateOwnedRootPath(rootPath, _sourceRoot, rejectTraversalInput: true, out error) ||
					!TryValidatePayloadPath(payloadPath, rootPath, out error))
				{
					return SggRdResult.InvalidArgument;
				}
				if (!File.Exists(payloadPath))
				{
					error = "renderdoc_storage_payload_not_observed";
					return SggRdResult.CaptureNotObserved;
				}

				return SggRdResult.Ok;
			}
		}

		private SggRdResult TryValidatePayloadInternal(
			PerfMeterRenderDocStorageReservation reservation,
			string payloadPath,
			bool requireNonempty,
			out long payloadBytes,
			out string error)
		{
			payloadBytes = 0L;
			error = string.Empty;
			lock (_gate)
			{
				if (reservation == null || !reservation.BelongsTo(this) || reservation.IsReleased ||
					!_reservations.ContainsKey(reservation.RootPath))
				{
					error = "renderdoc_storage_reservation_invalid";
					return SggRdResult.InvalidArgument;
				}

				PerfMeterRenderDocStorageMarker marker;
				SggRdResult inspectResult = TryInspectOwnedRootInternal(reservation.RootPath, out marker, out _, out error);
				if (inspectResult != SggRdResult.Ok ||
					marker.RequestNonce != reservation.RequestNonce ||
					marker.Generation != reservation.Request.Generation ||
					!string.Equals(marker.SessionId, reservation.Request.SessionId, StringComparison.Ordinal))
				{
					error = string.IsNullOrEmpty(error) ? "renderdoc_storage_marker_mismatch" : error;
					return inspectResult == SggRdResult.Ok ? SggRdResult.InvalidArgument : inspectResult;
				}

				if (!TryValidatePayloadPath(payloadPath, reservation.RootPath, out string payloadError))
				{
					error = payloadError;
					return SggRdResult.InvalidArgument;
				}

				if (!File.Exists(payloadPath))
				{
					error = "renderdoc_storage_payload_not_observed";
					return SggRdResult.CaptureNotObserved;
				}

				payloadBytes = new FileInfo(payloadPath).Length;
				if (requireNonempty && payloadBytes <= 0L)
				{
					error = "renderdoc_storage_payload_empty";
					return SggRdResult.CaptureFailed;
				}

				if (payloadBytes > PerfMeterRenderDocStoragePolicy.MaxPayloadBytes)
				{
					error = "renderdoc_storage_payload_limit_exceeded";
					return SggRdResult.CaptureFailed;
				}

				return SggRdResult.Ok;
			}
		}

		internal SggRdResult TryDeleteOwnedRoot(string rootPath, out string error)
		{
			lock (_gate)
			{
				return TryDeleteOwnedRootInternal(rootPath, allowLostSession: false, allowStaleNonterminal: false, out error);
			}
		}

		internal SggRdResult TryRetryPendingCleanup(string rootPath, out string error)
		{
			error = string.Empty;
			lock (_gate)
			{
				if (!TryValidateOwnedRootPath(
						rootPath,
						GetRootForCandidate(rootPath),
						rejectTraversalInput: true,
						out error))
				{
					return SggRdResult.InvalidArgument;
				}

				string tombstonePath = (rootPath ?? string.Empty) + ".cleanup";
				if (Directory.Exists(tombstonePath) || File.Exists(tombstonePath))
				{
					return TryDeleteOwnedRootInternal(
						tombstonePath,
						allowLostSession: true,
						allowStaleNonterminal: false,
						out error);
				}

				if (Directory.Exists(rootPath) || File.Exists(rootPath))
				{
					return TryDeleteOwnedRootInternal(
						rootPath,
						allowLostSession: true,
						allowStaleNonterminal: false,
						out error);
				}

				return SggRdResult.Ok;
			}
		}

		internal SggRdResult TryCleanup(
			Func<string, ulong, bool> isSessionLive,
			out PerfMeterRenderDocStorageUsage usage,
			out string error)
		{
			lock (_gate)
			{
				SggRdResult sourceResult = CleanupPoolWithUsage(
					PerfMeterRenderDocStoragePool.Source,
					isSessionLive ?? GetLiveSessionPredicate(),
					out PerfMeterRenderDocStoragePoolUsage source,
					out error);
				if (sourceResult != SggRdResult.Ok)
				{
					usage = default;
					return sourceResult;
				}

				SggRdResult copyResult = CleanupPoolWithUsage(
					PerfMeterRenderDocStoragePool.CopyEmbed,
					isSessionLive ?? GetLiveSessionPredicate(),
					out PerfMeterRenderDocStoragePoolUsage copy,
					out error);
				if (copyResult != SggRdResult.Ok)
				{
					usage = default;
					return copyResult;
				}

				usage = new PerfMeterRenderDocStorageUsage(source, copy);
				return SggRdResult.Ok;
			}
		}

		private SggRdResult CleanupPoolWithUsage(
			PerfMeterRenderDocStoragePool pool,
			Func<string, ulong, bool> isSessionLive,
			out PerfMeterRenderDocStoragePoolUsage usage,
			out string error)
		{
			usage = PerfMeterRenderDocStoragePoolUsage.Empty(pool);
			error = string.Empty;
			string root = GetPoolRoot(pool);
			if (!Directory.Exists(root) && pool != PerfMeterRenderDocStoragePool.CopyEmbed)
			{
				return SggRdResult.Ok;
			}
			if (!PerfMeterRenderDocStorageLock.TryAcquire(root, out IDisposable poolLease, out error))
			{
				return SggRdResult.InternalError;
			}

			using (poolLease)
			{
				SggRdResult cleanupResult = CleanupPoolInternal(
					pool,
					isSessionLive,
					0L,
					includeStale: true,
					out error);
				return cleanupResult == SggRdResult.Ok
					? GetPoolUsageInternal(pool, out usage, out error)
					: cleanupResult;
			}
		}

		private SggRdResult CleanupPoolInternal(
			PerfMeterRenderDocStoragePool pool,
			Func<string, ulong, bool> isSessionLive,
			long requiredReservationBytes,
			bool includeStale,
			out string error)
		{
			error = string.Empty;
			string root = GetPoolRoot(pool);
			bool rootExists = Directory.Exists(root);
			if (!rootExists && pool != PerfMeterRenderDocStoragePool.CopyEmbed)
			{
				return SggRdResult.Ok;
			}

			if (rootExists && !IsSafePath(root))
			{
				error = "renderdoc_storage_root_reparse_or_invalid";
				return SggRdResult.InvalidArgument;
			}

			List<OwnedRoot> ownedRoots;
			SggRdResult scanResult = ScanOwnedRoots(pool, out ownedRoots, out error);
			if (scanResult != SggRdResult.Ok)
			{
				return scanResult;
			}
			List<PerfMeterRenderDocEmbeddedBundle> embeddedBundles = new List<PerfMeterRenderDocEmbeddedBundle>();
			if (pool == PerfMeterRenderDocStoragePool.CopyEmbed)
			{
				SggRdResult embeddedResult = _embeddedBundleStorage.TryScan(out embeddedBundles, out error);
				if (embeddedResult != SggRdResult.Ok)
				{
					return embeddedResult;
				}
			}

			DateTimeOffset now = UtcNow();
			for (int index = 0; index < ownedRoots.Count; index++)
			{
				OwnedRoot owned = ownedRoots[index];
				bool pending = owned.Marker.State == PerfMeterRenderDocStorageState.CleanupPending;
				bool stale = includeStale &&
					!IsInProcessActiveReservation(owned.RootPath) &&
					IsStaleNonterminal(owned.Marker, now) &&
					!IsSessionLiveSafely(isSessionLive, owned.Marker);
				bool expiredTerminal = owned.Marker.State == PerfMeterRenderDocStorageState.Terminal && IsOlderThan(owned.Marker.StateUtc, now, TimeSpan.FromDays(PerfMeterRenderDocStoragePolicy.RetentionDays));
				if (!pending && !stale && !expiredTerminal)
				{
					continue;
				}

				SggRdResult deleteResult = TryDeleteOwnedRootInternal(
					owned.RootPath,
					allowLostSession: stale || pending,
					allowStaleNonterminal: stale,
					out error);
				if (deleteResult != SggRdResult.Ok)
				{
					return deleteResult;
				}
			}
			for (int index = 0; index < embeddedBundles.Count; index++)
			{
				PerfMeterRenderDocEmbeddedBundle embedded = embeddedBundles[index];
				if (!IsOlderThan(
					embedded.StateUtc,
					now,
					TimeSpan.FromDays(PerfMeterRenderDocStoragePolicy.RetentionDays)))
				{
					continue;
				}

				SggRdResult deleteResult = _embeddedBundleStorage.TryDelete(embedded, out error);
				if (deleteResult != SggRdResult.Ok)
				{
					return deleteResult;
				}
			}

			SggRdResult refreshedResult = ScanOwnedRoots(pool, out ownedRoots, out error);
			if (refreshedResult != SggRdResult.Ok)
			{
				return refreshedResult;
			}
			if (pool == PerfMeterRenderDocStoragePool.CopyEmbed)
			{
				refreshedResult = _embeddedBundleStorage.TryScan(out embeddedBundles, out error);
				if (refreshedResult != SggRdResult.Ok)
				{
					return refreshedResult;
				}
			}

			List<RetentionItem> terminalItems = new List<RetentionItem>();
			for (int index = 0; index < ownedRoots.Count; index++)
			{
				if (ownedRoots[index].Marker.State == PerfMeterRenderDocStorageState.Terminal)
				{
					terminalItems.Add(new RetentionItem(ownedRoots[index]));
				}
			}
			for (int index = 0; index < embeddedBundles.Count; index++)
			{
				terminalItems.Add(new RetentionItem(embeddedBundles[index]));
			}

			terminalItems.Sort(CompareOldestRetentionItem);
			PerfMeterRenderDocStoragePoolUsage currentUsage;
			SggRdResult usageResult = GetPoolUsageInternal(pool, out currentUsage, out error);
			if (usageResult != SggRdResult.Ok)
			{
				return usageResult;
			}

			for (int index = 0; index < terminalItems.Count; index++)
			{
				bool overCount = currentUsage.TerminalItemCount > PerfMeterRenderDocStoragePolicy.MaxTerminalItems;
				bool overBytes = Exceeds(
					currentUsage.AccountedBytes,
					requiredReservationBytes,
					currentUsage.CapacityBytes);
				if (!overCount && !overBytes)
				{
					break;
				}

				RetentionItem item = terminalItems[index];
				SggRdResult deleteResult = item.IsEmbedded
					? _embeddedBundleStorage.TryDelete(item.EmbeddedBundle, out error)
					: TryDeleteOwnedRootInternal(
						item.OwnedRoot.RootPath,
						allowLostSession: false,
						allowStaleNonterminal: false,
						out error);
				if (deleteResult != SggRdResult.Ok)
				{
					return deleteResult;
				}

				usageResult = GetPoolUsageInternal(pool, out currentUsage, out error);
				if (usageResult != SggRdResult.Ok)
				{
					return usageResult;
				}
			}

			return SggRdResult.Ok;
		}

		private SggRdResult GetPoolUsageInternal(
			PerfMeterRenderDocStoragePool pool,
			out PerfMeterRenderDocStoragePoolUsage usage,
			out string error)
		{
			error = string.Empty;
			usage = PerfMeterRenderDocStoragePoolUsage.Empty(pool);
			string root = GetPoolRoot(pool);
			bool rootExists = Directory.Exists(root);
			if (!rootExists && pool != PerfMeterRenderDocStoragePool.CopyEmbed)
			{
				return SggRdResult.Ok;
			}

			if (rootExists && !IsSafePath(root))
			{
				error = "renderdoc_storage_root_reparse_or_invalid";
				return SggRdResult.InvalidArgument;
			}

			List<OwnedRoot> ownedRoots;
			SggRdResult scanResult = ScanOwnedRoots(pool, out ownedRoots, out error);
			if (scanResult != SggRdResult.Ok)
			{
				return scanResult;
			}

			long ownedBytes = 0L;
			long reservedBytes = 0L;
			int terminalItems = 0;
			int embeddedItemCount = 0;
			for (int index = 0; index < ownedRoots.Count; index++)
			{
				OwnedRoot owned = ownedRoots[index];
				ownedBytes = SaturatingAdd(ownedBytes, owned.OwnedBytes);
				if (owned.Marker.State == PerfMeterRenderDocStorageState.Terminal)
				{
					terminalItems++;
				}
				else if (IsReservationState(owned.Marker.State))
				{
					reservedBytes = SaturatingAdd(reservedBytes, GetPersistedReservationBytes(pool, owned));
				}
			}
			if (pool == PerfMeterRenderDocStoragePool.CopyEmbed)
			{
				SggRdResult embeddedResult = _embeddedBundleStorage.TryScan(
					out List<PerfMeterRenderDocEmbeddedBundle> embeddedBundles,
					out error);
				if (embeddedResult != SggRdResult.Ok)
				{
					return embeddedResult;
				}
				embeddedItemCount = embeddedBundles.Count;
				for (int index = 0; index < embeddedBundles.Count; index++)
				{
					ownedBytes = SaturatingAdd(ownedBytes, embeddedBundles[index].OwnedBytes);
					terminalItems++;
				}
			}

			usage = new PerfMeterRenderDocStoragePoolUsage(
				pool,
				ownedBytes,
				reservedBytes,
				ownedRoots.Count + embeddedItemCount,
				terminalItems);
			return SggRdResult.Ok;
		}

		private SggRdResult ScanOwnedRoots(
			PerfMeterRenderDocStoragePool pool,
			out List<OwnedRoot> ownedRoots,
			out string error)
		{
			ownedRoots = new List<OwnedRoot>();
			error = string.Empty;
			string root = GetPoolRoot(pool);
			if (!Directory.Exists(root))
			{
				return SggRdResult.Ok;
			}

			try
			{
				foreach (string child in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
				{
					PerfMeterRenderDocStorageMarker marker;
					long ownedBytes;
					SggRdResult result = TryInspectOwnedRootInternal(child, out marker, out ownedBytes, out string inspectError);
					if (result == SggRdResult.Ok)
					{
						ownedRoots.Add(new OwnedRoot(child, marker, ownedBytes));
					}
					else if (result == SggRdResult.InternalError)
					{
						error = inspectError;
						return result;
					}
					// Invalid, missing, mismatched, or reparse roots are unknown and are
					// deliberately left for manual review.
				}

				return SggRdResult.Ok;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_storage_scan_error";
				return SggRdResult.InternalError;
			}
		}

		private SggRdResult TryInspectOwnedRootInternal(
			string rootPath,
			out PerfMeterRenderDocStorageMarker marker,
			out long ownedBytes,
			out string error)
		{
			marker = default;
			ownedBytes = 0L;
			error = string.Empty;
			if (!TryValidateOwnedRootPath(rootPath, GetRootForCandidate(rootPath), rejectTraversalInput: true, out error))
			{
				return SggRdResult.InvalidArgument;
			}

			if (!Directory.Exists(rootPath) || !IsSafePath(rootPath))
			{
				error = "renderdoc_storage_root_not_owned";
				return SggRdResult.InvalidArgument;
			}

			string markerPath = Path.Combine(rootPath, PerfMeterRenderDocStoragePolicy.MarkerFileName);
			if (!File.Exists(markerPath) || !IsSafePath(markerPath))
			{
				error = "renderdoc_storage_marker_invalid";
				return SggRdResult.InvalidArgument;
			}

			SggRdResult markerResult = TryReadMarker(markerPath, out marker, out error);
			if (markerResult != SggRdResult.Ok)
			{
				return markerResult;
			}

			string expectedName = marker.RequestNonce.ToString("x16", CultureInfo.InvariantCulture);
			string actualName = Path.GetFileName(rootPath);
			bool cleanupTombstone = marker.State == PerfMeterRenderDocStorageState.CleanupPending &&
				string.Equals(actualName, expectedName + ".cleanup", StringComparison.Ordinal);
			if (!string.Equals(actualName, expectedName, StringComparison.Ordinal) && !cleanupTombstone)
			{
				error = "renderdoc_storage_marker_mismatch";
				return SggRdResult.InvalidArgument;
			}

			return TryMeasureOwnedRoot(rootPath, out ownedBytes, out error);
		}

		private SggRdResult TryDeleteOwnedRootInternal(
			string rootPath,
			bool allowLostSession,
			bool allowStaleNonterminal,
			out string error)
		{
			if (!PerfMeterRenderDocStorageLock.TryAcquire(rootPath, out IDisposable lease, out error))
			{
				return SggRdResult.InternalError;
			}

			using (lease)
			{
				return TryDeleteOwnedRootLocked(rootPath, allowLostSession, allowStaleNonterminal, out error);
			}
		}

		private SggRdResult TryDeleteOwnedRootLocked(
			string rootPath,
			bool allowLostSession,
			bool allowStaleNonterminal,
			out string error)
		{
			error = string.Empty;
			PerfMeterRenderDocStorageMarker marker;
			long ignoredBytes;
			SggRdResult inspectResult = TryInspectOwnedRootInternal(rootPath, out marker, out ignoredBytes, out error);
			if (inspectResult != SggRdResult.Ok)
			{
				return inspectResult;
			}

			if (marker.State != PerfMeterRenderDocStorageState.Terminal &&
				marker.State != PerfMeterRenderDocStorageState.CleanupPending &&
				(!allowLostSession || marker.State != PerfMeterRenderDocStorageState.LostSession) &&
				!allowStaleNonterminal)
			{
				error = "renderdoc_storage_active_root_preserved";
				return SggRdResult.InvalidArgument;
			}

			string deletePath = rootPath;
			if (_useAtomicOwnedDelete && !Path.GetFileName(rootPath).EndsWith(".cleanup", StringComparison.Ordinal))
			{
				SggRdResult pendingResult = TrySetStateLocked(
					rootPath,
					new PerfMeterRenderDocStorageRequest(marker.SessionId, marker.Generation),
					marker.RequestNonce,
					PerfMeterRenderDocStorageState.CleanupPending,
					out error);
				if (pendingResult != SggRdResult.Ok)
				{
					return pendingResult;
				}

				deletePath = rootPath + ".cleanup";
				try
				{
					if (Directory.Exists(deletePath) || File.Exists(deletePath))
					{
						error = "renderdoc_storage_cleanup_tombstone_conflict";
						return SggRdResult.InternalError;
					}

					Directory.Move(rootPath, deletePath);
				}
				catch (Exception exception) when (IsIoException(exception))
				{
					error = "renderdoc_storage_cleanup_claim_failed";
					return SggRdResult.InternalError;
				}

				SggRdResult claimedResult = TryInspectOwnedRootInternal(deletePath, out marker, out ignoredBytes, out error);
				if (claimedResult != SggRdResult.Ok || marker.State != PerfMeterRenderDocStorageState.CleanupPending)
				{
					error = string.IsNullOrEmpty(error) ? "renderdoc_storage_cleanup_claim_invalid" : error;
					return claimedResult == SggRdResult.Ok ? SggRdResult.InvalidArgument : claimedResult;
				}
			}

			SggRdResult deleteResult = TryDeleteDirectoryWithRetries(deletePath, marker, out error);
			if (deleteResult != SggRdResult.Ok)
			{
				if (!_useAtomicOwnedDelete)
				{
					SggRdResult pendingResult = TrySetStateInternal(
						rootPath,
						new PerfMeterRenderDocStorageRequest(marker.SessionId, marker.Generation),
						marker.RequestNonce,
						PerfMeterRenderDocStorageState.CleanupPending,
						out string pendingError);
					if (pendingResult != SggRdResult.Ok && !string.IsNullOrEmpty(pendingError))
					{
						error = "renderdoc_storage_cleanup_pending";
					}
				}

				return deleteResult;
			}

			if (_reservations.TryGetValue(rootPath, out PerfMeterRenderDocStorageReservation reservation))
			{
				_reservations.Remove(rootPath);
				reservation.MarkReleased();
			}

			return SggRdResult.Ok;
		}

		private SggRdResult TryDeleteDirectoryWithRetries(
			string rootPath,
			PerfMeterRenderDocStorageMarker marker,
			out string error)
		{
			error = string.Empty;
			for (int attempt = 0; attempt < PerfMeterRenderDocStoragePolicy.IoAttempts; attempt++)
			{
				try
				{
					if (!Directory.Exists(rootPath) && !File.Exists(rootPath))
					{
						return SggRdResult.Ok;
					}

					if (!IsSafePath(rootPath))
					{
						error = "renderdoc_storage_root_reparse_or_invalid";
						return SggRdResult.InvalidArgument;
					}

					if (_useAtomicOwnedDelete && PerfMeterRenderDocWindowsFileSystem.IsSupported)
					{
						byte[] markerBytes = StrictUtf8.GetBytes(SerializeMarker(marker));
						SggRdResult handleDeleteResult = PerfMeterRenderDocWindowsFileSystem.TryDeleteOwnedRoot(
							rootPath,
							markerBytes,
							out error);
						if (handleDeleteResult == SggRdResult.InvalidArgument ||
							handleDeleteResult == SggRdResult.UnsupportedPlatform)
						{
							return handleDeleteResult;
						}
						if (handleDeleteResult == SggRdResult.Ok)
						{
							return SggRdResult.Ok;
						}
						error = "renderdoc_storage_cleanup_pending";
					}
					else
					{
						_deleteDirectory(rootPath);
					}
					if (!Directory.Exists(rootPath) && !File.Exists(rootPath))
					{
						return SggRdResult.Ok;
					}
				}
				catch (Exception exception) when (IsIoException(exception))
				{
					error = "renderdoc_storage_cleanup_pending";
				}

				if (attempt + 1 < PerfMeterRenderDocStoragePolicy.IoAttempts)
				{
					_retryDelay.Delay(TimeSpan.FromMilliseconds(
						attempt == 0
							? PerfMeterRenderDocStoragePolicy.FirstRetryDelayMilliseconds
							: PerfMeterRenderDocStoragePolicy.SecondRetryDelayMilliseconds));
				}
			}

			error = "renderdoc_storage_cleanup_pending";
			return SggRdResult.InternalError;
		}

		private SggRdResult TrySetStateInternal(
			string rootPath,
			PerfMeterRenderDocStorageRequest request,
			ulong requestNonce,
			PerfMeterRenderDocStorageState state,
			out string error)
		{
			if (!PerfMeterRenderDocStorageLock.TryAcquire(rootPath, out IDisposable lease, out error))
			{
				return SggRdResult.InternalError;
			}

			using (lease)
			{
				return TrySetStateLocked(rootPath, request, requestNonce, state, out error);
			}
		}

		private SggRdResult TrySetStateLocked(
			string rootPath,
			PerfMeterRenderDocStorageRequest request,
			ulong requestNonce,
			PerfMeterRenderDocStorageState state,
			out string error)
		{
			error = string.Empty;
			PerfMeterRenderDocStorageMarker existing;
			SggRdResult inspectResult = TryInspectOwnedRootInternal(rootPath, out existing, out long ignoredBytes, out error);
			if (inspectResult != SggRdResult.Ok)
			{
				return inspectResult;
			}

			if (existing.RequestNonce != requestNonce ||
				existing.Generation != request.Generation ||
				!string.Equals(existing.SessionId, request.SessionId, StringComparison.Ordinal))
			{
				error = "renderdoc_storage_marker_mismatch";
				return SggRdResult.InvalidArgument;
			}

			if (!IsAllowedTransition(existing.State, state))
			{
				error = "renderdoc_storage_state_transition_invalid";
				return SggRdResult.InvalidArgument;
			}

			DateTimeOffset now = UtcNow();
			PerfMeterRenderDocStorageMarker replacement = new PerfMeterRenderDocStorageMarker(
				existing.RequestNonce,
				existing.SessionId,
				existing.Generation,
				existing.CreatedUtc,
				state,
				now);
			SggRdResult writeResult = TryWriteMarker(rootPath, replacement, out error);
			if (writeResult == SggRdResult.Ok &&
				(state == PerfMeterRenderDocStorageState.Terminal ||
				 state == PerfMeterRenderDocStorageState.CleanupPending ||
				 state == PerfMeterRenderDocStorageState.LostSession) &&
				_reservations.TryGetValue(rootPath, out PerfMeterRenderDocStorageReservation reservation))
			{
				_reservations.Remove(rootPath);
				reservation.MarkReleased();
			}

			return writeResult;
		}

		private SggRdResult TryWriteMarker(
			string rootPath,
			PerfMeterRenderDocStorageMarker marker,
			out string error)
		{
			SggRdResult result = SggRdResult.InternalError;
			error = string.Empty;
			for (int attempt = 0; attempt < PerfMeterRenderDocStoragePolicy.IoAttempts; attempt++)
			{
				result = TryWriteMarkerOnce(rootPath, marker, out error);
				if (result != SggRdResult.InternalError)
				{
					return result;
				}

				if (attempt + 1 < PerfMeterRenderDocStoragePolicy.IoAttempts)
				{
					_retryDelay.Delay(TimeSpan.FromMilliseconds(
						attempt == 0
							? PerfMeterRenderDocStoragePolicy.FirstRetryDelayMilliseconds
							: PerfMeterRenderDocStoragePolicy.SecondRetryDelayMilliseconds));
				}
			}

			return result;
		}

		private SggRdResult TryWriteMarkerOnce(
			string rootPath,
			PerfMeterRenderDocStorageMarker marker,
			out string error)
		{
			error = string.Empty;
			if (!TryValidateOwnedRootPath(rootPath, GetRootForCandidate(rootPath), rejectTraversalInput: false, out error))
			{
				return SggRdResult.InvalidArgument;
			}

			string markerPath = Path.Combine(rootPath, PerfMeterRenderDocStoragePolicy.MarkerFileName);
			string temporaryPath = markerPath + ".tmp-" + Guid.NewGuid().ToString("N");
			byte[] bytes;
			try
			{
				bytes = StrictUtf8.GetBytes(SerializeMarker(marker));
				using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
				{
					stream.Write(bytes, 0, bytes.Length);
					stream.Flush(true);
				}

				if (File.Exists(markerPath))
				{
					if (!IsSafePath(markerPath))
					{
						error = "renderdoc_storage_marker_invalid";
						return SggRdResult.InvalidArgument;
					}

					File.Replace(temporaryPath, markerPath, null);
				}
				else
				{
					File.Move(temporaryPath, markerPath);
				}

				return SggRdResult.Ok;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_storage_marker_write_failed";
				return SggRdResult.InternalError;
			}
			finally
			{
				try
				{
					if (File.Exists(temporaryPath) && IsSafePath(temporaryPath))
					{
						File.Delete(temporaryPath);
					}
				}
				catch (Exception)
				{
					// A leftover temporary marker is unknown content and is never
					// allowed to authorize deletion of the root.
				}
			}
		}

		private SggRdResult TryReadMarker(
			string markerPath,
			out PerfMeterRenderDocStorageMarker marker,
			out string error)
		{
			marker = default;
			error = string.Empty;
			try
			{
				FileAttributes attributes = File.GetAttributes(markerPath);
				if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
				{
					error = "renderdoc_storage_marker_invalid";
					return SggRdResult.InvalidArgument;
				}

				FileInfo info = new FileInfo(markerPath);
				if (info.Length <= 0L || info.Length > PerfMeterRenderDocStoragePolicy.MaxMarkerBytes)
				{
					error = "renderdoc_storage_marker_invalid";
					return SggRdResult.InvalidArgument;
				}

				byte[] bytes = File.ReadAllBytes(markerPath);
				string content = StrictUtf8.GetString(bytes);
				string[] lines = content.Split(new[] { '\n' }, StringSplitOptions.None);
				if (lines.Length != 9 || lines[8].Length != 0 ||
					lines[0] != PerfMeterRenderDocStoragePolicy.MarkerSchema ||
					lines[1] != PerfMeterRenderDocStoragePolicy.MarkerVersion.ToString(CultureInfo.InvariantCulture))
				{
					error = "renderdoc_storage_marker_invalid";
					return SggRdResult.InvalidArgument;
				}

				if (!TryGetMarkerValue(lines[2], "request_nonce", out string nonceText) ||
					!TryGetMarkerValue(lines[3], "owning_session", out string sessionText) ||
					!TryGetMarkerValue(lines[4], "generation", out string generationText) ||
					!TryGetMarkerValue(lines[5], "created_utc", out string createdText) ||
					!TryGetMarkerValue(lines[6], "state", out string stateText) ||
					!TryGetMarkerValue(lines[7], "state_utc", out string stateUtcText))
				{
					error = "renderdoc_storage_marker_invalid";
					return SggRdResult.InvalidArgument;
				}

				if (!TryParseNonce(nonceText, out ulong nonce) ||
					!TryDecodeSession(sessionText, out string sessionId) ||
					!ulong.TryParse(generationText, NumberStyles.None, CultureInfo.InvariantCulture, out ulong generation) ||
					!TryParseUtc(createdText, out DateTimeOffset createdUtc) ||
					!TryParseState(stateText, out PerfMeterRenderDocStorageState state) ||
					!TryParseUtc(stateUtcText, out DateTimeOffset stateUtc) ||
					createdUtc > stateUtc ||
					createdUtc > UtcNow() ||
					stateUtc > UtcNow())
				{
					error = "renderdoc_storage_marker_invalid";
					return SggRdResult.InvalidArgument;
				}

				marker = new PerfMeterRenderDocStorageMarker(nonce, sessionId, generation, createdUtc, state, stateUtc);
				return SggRdResult.Ok;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_storage_marker_invalid";
				return SggRdResult.InvalidArgument;
			}
		}

		private SggRdResult TryMeasureOwnedRoot(string rootPath, out long ownedBytes, out string error)
		{
			ownedBytes = 0L;
			error = string.Empty;
			try
			{
				int payloadCount = 0;
				foreach (string entry in Directory.EnumerateFileSystemEntries(rootPath, "*", SearchOption.TopDirectoryOnly))
				{
					FileAttributes attributes = File.GetAttributes(entry);
					if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0 || !IsSafePath(entry))
					{
						error = "renderdoc_storage_unknown_content";
						return SggRdResult.InvalidArgument;
					}

					string name = Path.GetFileName(entry);
					if (!string.Equals(name, PerfMeterRenderDocStoragePolicy.MarkerFileName, StringComparison.Ordinal))
					{
						payloadCount++;
						if (payloadCount > 1 || !string.Equals(Path.GetExtension(name), ".rdc", StringComparison.OrdinalIgnoreCase))
						{
							error = "renderdoc_storage_unknown_content";
							return SggRdResult.InvalidArgument;
						}
					}

					long length = new FileInfo(entry).Length;
					ownedBytes = SaturatingAdd(ownedBytes, length);
				}

				return SggRdResult.Ok;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_storage_measurement_failed";
				return SggRdResult.InternalError;
			}
		}

		private bool TryEnsureStorageBase(string root, out string error)
		{
			error = string.Empty;
			try
			{
				if (!IsSafePath(root) || File.Exists(root))
				{
					error = "renderdoc_storage_root_reparse_or_invalid";
					return false;
				}

				Directory.CreateDirectory(root);
				if (!Directory.Exists(root) || !IsSafePath(root))
				{
					error = "renderdoc_storage_root_reparse_or_invalid";
					return false;
				}

				return true;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_storage_root_reparse_or_invalid";
				return false;
			}
		}

		private bool TryValidateOwnedRootPath(
			string path,
			string expectedBase,
			bool rejectTraversalInput,
			out string error)
		{
			error = string.Empty;
			try
			{
				if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(expectedBase) ||
					(rejectTraversalInput && ContainsTraversal(path)) ||
					!Path.IsPathRooted(path) ||
					!Path.IsPathRooted(expectedBase))
				{
					error = "renderdoc_storage_path_rejected";
					return false;
				}

				string fullPath = NormalizeDirectory(Path.GetFullPath(path));
				string fullBase = NormalizeDirectory(Path.GetFullPath(expectedBase));
				if (!PathsEqual(Path.GetDirectoryName(fullPath), fullBase) ||
					!IsSafePath(fullBase) ||
					!IsSafePath(fullPath))
				{
					error = "renderdoc_storage_path_rejected";
					return false;
				}

				return true;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_storage_path_rejected";
				return false;
			}
		}

		private bool TryValidatePayloadPath(string path, string rootPath, out string error)
		{
			error = string.Empty;
			try
			{
				if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(rootPath) ||
					ContainsTraversal(path) || !Path.IsPathRooted(path))
				{
					error = "renderdoc_storage_payload_path_rejected";
					return false;
				}

				string fullPath = NormalizeDirectory(Path.GetFullPath(path));
				if (!PathsEqual(Path.GetDirectoryName(fullPath), rootPath) ||
					!string.Equals(Path.GetExtension(fullPath), ".rdc", StringComparison.OrdinalIgnoreCase) ||
					!IsSafePath(fullPath))
				{
					error = "renderdoc_storage_payload_path_rejected";
					return false;
				}

				if (!File.Exists(fullPath))
				{
					return !Directory.Exists(fullPath);
				}

				FileAttributes attributes = File.GetAttributes(fullPath);
				if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
				{
					error = "renderdoc_storage_payload_path_rejected";
					return false;
				}

				return true;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_storage_payload_path_rejected";
				return false;
			}
		}

		private bool IsSafePath(string path)
		{
			try
			{
				string current = NormalizeDirectory(Path.GetFullPath(path ?? string.Empty));
				bool isLeaf = true;
				while (!string.IsNullOrEmpty(current))
				{
					if (File.Exists(current) || Directory.Exists(current))
					{
						FileAttributes attributes = File.GetAttributes(current);
						if ((attributes & FileAttributes.ReparsePoint) != 0)
						{
							return false;
						}

						if ((attributes & FileAttributes.Directory) == 0 && isLeaf)
						{
							// A regular file is a safe leaf. Intermediate files are
							// rejected on the next walk step below.
						}
						else if ((attributes & FileAttributes.Directory) == 0 && !PathsEqual(current, _projectRoot))
						{
							return false;
						}
					}

					if (PathsEqual(current, _projectRoot))
					{
						return true;
					}

					string parent = Path.GetDirectoryName(current);
					if (string.IsNullOrEmpty(parent) || PathsEqual(parent, current))
					{
						return false;
					}

					current = parent;
					isLeaf = false;
				}
			}
			catch (Exception)
			{
				return false;
			}

			return false;
		}

		private string GetRootForCandidate(string path)
		{
			try
			{
				string fullPath = NormalizeDirectory(Path.GetFullPath(path ?? string.Empty));
				string parent = NormalizeDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);
				if (PathsEqual(parent, _sourceRoot))
				{
					return _sourceRoot;
				}

				if (PathsEqual(parent, _copyRoot))
				{
					return _copyRoot;
				}
			}
			catch (Exception)
			{
			}

			return string.Empty;
		}

		private string GetPoolRoot(PerfMeterRenderDocStoragePool pool)
		{
			return pool == PerfMeterRenderDocStoragePool.Source ? _sourceRoot : _copyRoot;
		}

		private Func<string, ulong, bool> GetLiveSessionPredicate()
		{
			return (sessionId, generation) =>
			{
				foreach (PerfMeterRenderDocStorageReservation reservation in _reservations.Values)
				{
					if (!reservation.IsReleased &&
						reservation.Request.Generation == generation &&
						string.Equals(reservation.Request.SessionId, sessionId, StringComparison.Ordinal))
					{
						return true;
					}
				}

				return false;
			};
		}

		private bool IsInProcessActiveReservation(string rootPath)
		{
			return _reservations.TryGetValue(rootPath, out PerfMeterRenderDocStorageReservation reservation) &&
				!reservation.IsReleased;
		}

		private DateTimeOffset UtcNow()
		{
			return _clock.UtcNow.ToUniversalTime();
		}

		private long GetPersistedReservationBytes(
			PerfMeterRenderDocStoragePool pool,
			OwnedRoot owned)
		{
			if (_reservations.TryGetValue(owned.RootPath, out PerfMeterRenderDocStorageReservation reservation) && !reservation.IsReleased)
			{
				return reservation.ReservedBytes;
			}

			if (owned.Marker.State == PerfMeterRenderDocStorageState.Preflight ||
				owned.Marker.State == PerfMeterRenderDocStorageState.Capturing ||
				owned.Marker.State == PerfMeterRenderDocStorageState.AwaitingArtifact ||
				owned.Marker.State == PerfMeterRenderDocStorageState.Finalizing)
			{
				return pool == PerfMeterRenderDocStoragePool.Source
					? PerfMeterRenderDocStoragePolicy.SourceReservationBytes
					: PerfMeterRenderDocStoragePolicy.MaxPayloadBytes + PerfMeterRenderDocStoragePolicy.MetadataReserveBytes;
			}

			return 0L;
		}

		private static bool IsValidRequest(PerfMeterRenderDocStorageRequest request)
		{
			if (string.IsNullOrEmpty(request.SessionId) || request.SessionId.Length > PerfMeterRenderDocStoragePolicy.MaxSessionIdLength)
			{
				return false;
			}

			for (int index = 0; index < request.SessionId.Length; index++)
			{
				if (char.IsControl(request.SessionId[index]))
				{
					return false;
				}
			}

			return true;
		}

		private static bool IsValidState(PerfMeterRenderDocStorageState state)
		{
			return state == PerfMeterRenderDocStorageState.Preflight ||
				state == PerfMeterRenderDocStorageState.Capturing ||
				state == PerfMeterRenderDocStorageState.AwaitingArtifact ||
				state == PerfMeterRenderDocStorageState.Finalizing ||
				state == PerfMeterRenderDocStorageState.Terminal ||
				state == PerfMeterRenderDocStorageState.CleanupPending ||
				state == PerfMeterRenderDocStorageState.LostSession;
		}

		private static bool IsAllowedTransition(
			PerfMeterRenderDocStorageState current,
			PerfMeterRenderDocStorageState next)
		{
			if (current == next)
			{
				return true;
			}

			switch (current)
			{
				case PerfMeterRenderDocStorageState.Preflight:
					return next == PerfMeterRenderDocStorageState.Capturing ||
						next == PerfMeterRenderDocStorageState.AwaitingArtifact ||
						next == PerfMeterRenderDocStorageState.Terminal ||
						next == PerfMeterRenderDocStorageState.CleanupPending ||
						next == PerfMeterRenderDocStorageState.LostSession;
				case PerfMeterRenderDocStorageState.Capturing:
					return next == PerfMeterRenderDocStorageState.AwaitingArtifact ||
						next == PerfMeterRenderDocStorageState.Terminal ||
						next == PerfMeterRenderDocStorageState.CleanupPending ||
						next == PerfMeterRenderDocStorageState.LostSession;
				case PerfMeterRenderDocStorageState.AwaitingArtifact:
					return next == PerfMeterRenderDocStorageState.Finalizing ||
						next == PerfMeterRenderDocStorageState.Terminal ||
						next == PerfMeterRenderDocStorageState.CleanupPending ||
						next == PerfMeterRenderDocStorageState.LostSession;
				case PerfMeterRenderDocStorageState.Finalizing:
					return next == PerfMeterRenderDocStorageState.Terminal ||
						next == PerfMeterRenderDocStorageState.CleanupPending ||
						next == PerfMeterRenderDocStorageState.LostSession;
				case PerfMeterRenderDocStorageState.Terminal:
					return next == PerfMeterRenderDocStorageState.CleanupPending;
				case PerfMeterRenderDocStorageState.LostSession:
					return next == PerfMeterRenderDocStorageState.Terminal ||
						next == PerfMeterRenderDocStorageState.CleanupPending;
				default:
					return false;
			}
		}

		private static void TryRollbackEmptyCreatedRoot(string rootPath)
		{
			try
			{
				bool empty = true;
				foreach (string ignored in Directory.EnumerateFileSystemEntries(rootPath))
				{
					empty = false;
					break;
				}

				if (Directory.Exists(rootPath) && empty)
				{
					Directory.Delete(rootPath, false);
				}
			}
			catch (Exception)
			{
				// A nonempty or contested root is unknown and remains for manual review.
			}
		}

		private static bool IsReservationState(PerfMeterRenderDocStorageState state)
		{
			return state == PerfMeterRenderDocStorageState.Preflight ||
				state == PerfMeterRenderDocStorageState.Capturing ||
				state == PerfMeterRenderDocStorageState.AwaitingArtifact ||
				state == PerfMeterRenderDocStorageState.Finalizing;
		}

		private static bool IsStaleNonterminal(PerfMeterRenderDocStorageMarker marker, DateTimeOffset now)
		{
			return marker.State != PerfMeterRenderDocStorageState.Terminal &&
				marker.State != PerfMeterRenderDocStorageState.CleanupPending &&
				IsOlderThan(marker.StateUtc, now, TimeSpan.FromHours(PerfMeterRenderDocStoragePolicy.StaleNonterminalHours));
		}

		private static bool IsOlderThan(DateTimeOffset timestamp, DateTimeOffset now, TimeSpan age)
		{
			return now >= timestamp && now - timestamp >= age;
		}

		private static bool IsSessionLiveSafely(
			Func<string, ulong, bool> isSessionLive,
			PerfMeterRenderDocStorageMarker marker)
		{
			if (isSessionLive == null)
			{
				return true;
			}

			try
			{
				return isSessionLive(marker.SessionId, marker.Generation);
			}
			catch (Exception)
			{
				return true;
			}
		}

		private int CompareOldestRetentionItem(RetentionItem left, RetentionItem right)
		{
			int timestamp = left.StateUtc.CompareTo(right.StateUtc);
			if (timestamp != 0)
			{
				return timestamp;
			}

			return CompareNonceBytes(left.RequestNonce, right.RequestNonce);
		}

		private static int CompareNonceBytes(ulong left, ulong right)
		{
			for (int shift = 56; shift >= 0; shift -= 8)
			{
				int comparison = ((left >> shift) & 0xffu).CompareTo((right >> shift) & 0xffu);
				if (comparison != 0)
				{
					return comparison;
				}
			}

			return 0;
		}

		private static bool Exceeds(long current, long additional, long capacity)
		{
			return additional > capacity || current > capacity - additional;
		}

		private static long SaturatingAdd(long left, long right)
		{
			return right > long.MaxValue - left ? long.MaxValue : left + right;
		}

		private static string SerializeMarker(PerfMeterRenderDocStorageMarker marker)
		{
			return PerfMeterRenderDocStoragePolicy.MarkerSchema + "\n" +
				PerfMeterRenderDocStoragePolicy.MarkerVersion.ToString(CultureInfo.InvariantCulture) + "\n" +
				"request_nonce=" + marker.RequestNonce.ToString("x16", CultureInfo.InvariantCulture) + "\n" +
				"owning_session=" + Convert.ToBase64String(StrictUtf8.GetBytes(marker.SessionId)) + "\n" +
				"generation=" + marker.Generation.ToString(CultureInfo.InvariantCulture) + "\n" +
				"created_utc=" + marker.CreatedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) + "\n" +
				"state=" + StateToText(marker.State) + "\n" +
				"state_utc=" + marker.StateUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) + "\n";
		}

		private static bool TryGetMarkerValue(string line, string key, out string value)
		{
			string prefix = key + "=";
			if (line == null || !line.StartsWith(prefix, StringComparison.Ordinal))
			{
				value = string.Empty;
				return false;
			}

			value = line.Substring(prefix.Length);
			return value.Length > 0;
		}

		private static bool TryParseNonce(string value, out ulong nonce)
		{
			nonce = 0u;
			return value != null && value.Length == 16 &&
				ulong.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out nonce) && nonce != 0u;
		}

		private static bool TryDecodeSession(string value, out string sessionId)
		{
			sessionId = string.Empty;
			try
			{
				byte[] bytes = Convert.FromBase64String(value ?? string.Empty);
				sessionId = StrictUtf8.GetString(bytes);
				return IsValidRequest(new PerfMeterRenderDocStorageRequest(sessionId, 0u));
			}
			catch (Exception)
			{
				sessionId = string.Empty;
				return false;
			}
		}

		private static bool TryParseUtc(string value, out DateTimeOffset utc)
		{
			if (DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out utc))
			{
				if (utc.Offset != TimeSpan.Zero)
				{
					utc = default;
					return false;
				}

				utc = utc.ToUniversalTime();
				return true;
			}

			utc = default;
			return false;
		}

		private static string StateToText(PerfMeterRenderDocStorageState state)
		{
			switch (state)
			{
				case PerfMeterRenderDocStorageState.Preflight:
					return "preflight";
				case PerfMeterRenderDocStorageState.Capturing:
					return "capturing";
				case PerfMeterRenderDocStorageState.AwaitingArtifact:
					return "awaiting_artifact";
				case PerfMeterRenderDocStorageState.Finalizing:
					return "finalizing";
				case PerfMeterRenderDocStorageState.Terminal:
					return "terminal";
				case PerfMeterRenderDocStorageState.CleanupPending:
					return "cleanup_pending";
				case PerfMeterRenderDocStorageState.LostSession:
					return "lost_session";
				default:
					return string.Empty;
			}
		}

		private static bool TryParseState(string value, out PerfMeterRenderDocStorageState state)
		{
			switch (value)
			{
				case "preflight":
					state = PerfMeterRenderDocStorageState.Preflight;
					return true;
				case "capturing":
					state = PerfMeterRenderDocStorageState.Capturing;
					return true;
				case "awaiting_artifact":
					state = PerfMeterRenderDocStorageState.AwaitingArtifact;
					return true;
				case "finalizing":
					state = PerfMeterRenderDocStorageState.Finalizing;
					return true;
				case "terminal":
					state = PerfMeterRenderDocStorageState.Terminal;
					return true;
				case "cleanup_pending":
					state = PerfMeterRenderDocStorageState.CleanupPending;
					return true;
				case "lost_session":
					state = PerfMeterRenderDocStorageState.LostSession;
					return true;
				default:
					state = default;
					return false;
			}
		}

		private static bool ContainsTraversal(string path)
		{
			string[] parts = (path ?? string.Empty).Replace('\\', '/').Split('/');
			for (int index = 0; index < parts.Length; index++)
			{
				if (parts[index] == "." || parts[index] == "..")
				{
					return true;
				}
			}

			return false;
		}

		private bool PathsEqual(string left, string right)
		{
			return string.Equals(
				NormalizeDirectory(left ?? string.Empty),
				NormalizeDirectory(right ?? string.Empty),
				_pathComparison);
		}

		private static string NormalizeDirectory(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			string normalized = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (normalized.Length == 0 && Path.IsPathRooted(path))
			{
				return Path.GetPathRoot(path) ?? path;
			}

			return normalized;
		}

		private static bool IsIoException(Exception exception)
		{
			return exception is IOException ||
				exception is UnauthorizedAccessException ||
				exception is ArgumentException ||
				exception is NotSupportedException ||
				exception is SecurityException;
		}

		private sealed class OwnedRoot
		{
			internal OwnedRoot(string rootPath, PerfMeterRenderDocStorageMarker marker, long ownedBytes)
			{
				RootPath = rootPath;
				Marker = marker;
				OwnedBytes = ownedBytes;
			}

			internal string RootPath { get; }
			internal PerfMeterRenderDocStorageMarker Marker { get; }
			internal long OwnedBytes { get; }
		}

		private sealed class RetentionItem
		{
			internal RetentionItem(OwnedRoot ownedRoot)
			{
				OwnedRoot = ownedRoot;
				EmbeddedBundle = default;
				StateUtc = ownedRoot.Marker.StateUtc;
				RequestNonce = ownedRoot.Marker.RequestNonce;
			}

			internal RetentionItem(PerfMeterRenderDocEmbeddedBundle embeddedBundle)
			{
				OwnedRoot = null;
				EmbeddedBundle = embeddedBundle;
				StateUtc = embeddedBundle.StateUtc;
				RequestNonce = embeddedBundle.RequestNonce;
			}

			internal bool IsEmbedded => OwnedRoot == null;
			internal OwnedRoot OwnedRoot { get; }
			internal PerfMeterRenderDocEmbeddedBundle EmbeddedBundle { get; }
			internal DateTimeOffset StateUtc { get; }
			internal ulong RequestNonce { get; }
		}

		private sealed class DefaultFreeSpaceProvider : IPerfMeterRenderDocFreeSpaceProvider
		{
			public long GetAvailableBytes(string path)
			{
				string driveRoot = Path.GetPathRoot(path);
				if (string.IsNullOrEmpty(driveRoot))
				{
					throw new IOException("Storage drive is unavailable.");
				}

				return new DriveInfo(driveRoot).AvailableFreeSpace;
			}
		}

		private sealed class DefaultClock : IPerfMeterRenderDocClock
		{
			public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
		}

		private sealed class CryptographicNonceProvider : IPerfMeterRenderDocNonceProvider
		{
			public ulong NextNonce()
			{
				byte[] bytes = new byte[sizeof(ulong)];
				using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
				{
					do
					{
						generator.GetBytes(bytes);
					}
					while (BitConverter.ToUInt64(bytes, 0) == 0u);
				}

				return BitConverter.ToUInt64(bytes, 0);
			}
		}

		private sealed class ThreadSleepRetryDelay : IPerfMeterRenderDocRetryDelay
		{
			public void Delay(TimeSpan delay)
			{
				Thread.Sleep(delay);
			}
		}

		private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
	}
}
