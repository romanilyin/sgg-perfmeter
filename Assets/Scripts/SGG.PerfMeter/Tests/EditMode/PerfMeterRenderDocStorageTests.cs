using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterRenderDocStorageTests
	{
		private string _projectRoot;
		private TestClock _clock;
		private TestFreeSpace _freeSpace;
		private IncrementingNonceProvider _nonces;

		[SetUp]
		public void SetUp()
		{
			_projectRoot = Path.Combine(Path.GetTempPath(), "sgg-perfmeter-renderdoc-" + Guid.NewGuid().ToString("N"));
			_clock = new TestClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
			_freeSpace = new TestFreeSpace(long.MaxValue);
			_nonces = new IncrementingNonceProvider(0x1000u);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_projectRoot))
			{
				Directory.Delete(_projectRoot, true);
			}
		}

		[Test]
		public void PreflightCreatesUniqueOwnedRootAndUsesSafeNativeDefaults()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			PerfMeterRenderDocPreflightProvider provider = new PerfMeterRenderDocPreflightProvider(storage);

			SggRdResult result = provider.Prepare(
				new PerfMeterCaptureOptions("storage-happy", PerfMeterCaptureTool.RenderDoc),
				out PerfMeterRenderDocPreflight preflight);

			Assert.That(result, Is.EqualTo(SggRdResult.Ok));
			Assert.That(preflight.RequestNonce, Is.Not.Zero);
			Assert.That(preflight.RootPath, Does.StartWith(storage.SourceRoot));
			Assert.That(preflight.CapturePathTemplate, Is.EqualTo(Path.Combine(preflight.RootPath, "capture")));
			Assert.That(File.Exists(Path.Combine(preflight.RootPath, PerfMeterRenderDocStoragePolicy.MarkerFileName)), Is.True);
			Assert.That(preflight.ArtifactOptions.StorageMode, Is.EqualTo(PerfMeterExternalArtifactStorageMode.MetadataOnly));
			Assert.That(preflight.ArtifactOptions.SharePolicy, Is.EqualTo(PerfMeterExternalArtifactSharePolicy.DoNotShare));
			Assert.That(
				preflight.ArtifactOptions.PrivacyFlags,
				Is.EqualTo(PerfMeterExternalArtifactPrivacyFlags.ContainsGpuCaptureData |
					PerfMeterExternalArtifactPrivacyFlags.Sensitive |
					PerfMeterExternalArtifactPrivacyFlags.RequiresReview));

			Assert.That(storage.TryInspectOwnedRoot(preflight.RootPath, out PerfMeterRenderDocStorageMarker marker, out _, out string inspectError), Is.EqualTo(SggRdResult.Ok), inspectError);
			Assert.That(marker.RequestNonce, Is.EqualTo(preflight.RequestNonce));
			Assert.That(marker.SessionId, Is.Not.EqualTo("storage-happy"));
			Assert.That(marker.SessionId, Does.Not.Contain("storage-happy"));
			Assert.That(marker.SessionId, Has.Length.EqualTo(32));
			Assert.That(marker.Generation, Is.Zero);
			Assert.That(marker.State, Is.EqualTo(PerfMeterRenderDocStorageState.Preflight));
			Assert.That(storage.TryGetUsage(out PerfMeterRenderDocStorageUsage usage, out string usageError), Is.EqualTo(SggRdResult.Ok), usageError);
			Assert.That(usage.Source.ReservedBytes, Is.EqualTo(PerfMeterRenderDocStoragePolicy.SourceReservationBytes));

			Assert.That(preflight.SetTerminal(out string terminalError), Is.EqualTo(SggRdResult.Ok), terminalError);
			Assert.That(preflight.ReleaseReservation(out string releaseError), Is.EqualTo(SggRdResult.Ok), releaseError);
			Assert.That(storage.TryDeleteOwnedRoot(preflight.RootPath, out string deleteError), Is.EqualTo(SggRdResult.Ok), deleteError);
		}

		[Test]
		public void StorageBackedPreparationFailureAbortsRootAndPreservesCleanupPending()
		{
			TestRetryDelay delay = new TestRetryDelay();
			int attempts = 0;
			bool failDeletes = true;
			PerfMeterRenderDocStorage storage = CreateStorage(
				path =>
				{
					attempts++;
					if (failDeletes)
					{
						throw new IOException("post-reservation failure");
					}

					Directory.Delete(path, true);
				},
				delay);
			PerfMeterRenderDocPreflightProvider provider = new PerfMeterRenderDocPreflightProvider(
				storage,
				delegate(string captureId)
				{
					throw new InvalidOperationException("synthetic preparation failure");
				});

			SggRdResult result = provider.Prepare(
				new PerfMeterCaptureOptions("post-reservation", PerfMeterCaptureTool.RenderDoc),
				out PerfMeterRenderDocPreflight preflight);

			Assert.That(result, Is.EqualTo(SggRdResult.InternalError));
			Assert.That(preflight.Reservation, Is.Null);
			Assert.That(attempts, Is.EqualTo(3));
			Assert.That(delay.Delays, Is.EqualTo(new[] { 25, 50 }));
			string rootPath = Path.Combine(storage.SourceRoot, "0000000000001000");
			Assert.That(storage.TryInspectOwnedRoot(rootPath, out PerfMeterRenderDocStorageMarker marker, out _, out string inspectError), Is.EqualTo(SggRdResult.Ok), inspectError);
			Assert.That(marker.State, Is.EqualTo(PerfMeterRenderDocStorageState.CleanupPending));

			failDeletes = false;
			Assert.That(storage.TryCleanup((session, generation) => false, out _, out string cleanupError), Is.EqualTo(SggRdResult.Ok), cleanupError);
			Assert.That(Directory.Exists(rootPath), Is.False);
			Assert.That(attempts, Is.EqualTo(4));
		}

		[Test]
		public void FixedStoragePolicyValuesRemainBaselined()
		{
			Assert.That(PerfMeterRenderDocStoragePolicy.MaxPayloadBytes, Is.EqualTo(536870912L));
			Assert.That(PerfMeterRenderDocStoragePolicy.SourcePoolBytes, Is.EqualTo(2147483648L));
			Assert.That(PerfMeterRenderDocStoragePolicy.CopyEmbedPoolBytes, Is.EqualTo(2147483648L));
			Assert.That(PerfMeterRenderDocStoragePolicy.MaxTerminalItems, Is.EqualTo(16));
			Assert.That(PerfMeterRenderDocStoragePolicy.RetentionDays, Is.EqualTo(7));
			Assert.That(PerfMeterRenderDocStoragePolicy.StaleNonterminalHours, Is.EqualTo(24));
			Assert.That(PerfMeterRenderDocStoragePolicy.FreeSpaceFloorBytes, Is.EqualTo(1073741824L));
			Assert.That(PerfMeterRenderDocStoragePolicy.MetadataReserveBytes, Is.EqualTo(1048576L));
			Assert.That(PerfMeterRenderDocStoragePolicy.IoAttempts, Is.EqualTo(3));
			Assert.That(PerfMeterRenderDocStoragePolicy.FirstRetryDelayMilliseconds, Is.EqualTo(25));
			Assert.That(PerfMeterRenderDocStoragePolicy.SecondRetryDelayMilliseconds, Is.EqualTo(50));
		}

		[Test]
		public void TraversalOutsideAndUnownedRootsAreRejectedWithoutDeletion()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			Directory.CreateDirectory(storage.SourceRoot);
			string outsideRoot = Path.Combine(_projectRoot, "outside");
			string unownedRoot = Path.Combine(storage.SourceRoot, "unowned");
			Directory.CreateDirectory(outsideRoot);
			Directory.CreateDirectory(unownedRoot);

			Assert.That(
				storage.TryInspectOwnedRoot(Path.Combine(storage.SourceRoot, "..", "outside"), out _, out _, out _),
				Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(storage.TryInspectOwnedRoot(outsideRoot, out _, out _, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(storage.TryDeleteOwnedRoot(outsideRoot, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(storage.TryDeleteOwnedRoot(unownedRoot, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(Directory.Exists(outsideRoot), Is.True);
			Assert.That(Directory.Exists(unownedRoot), Is.True);
		}

		[Test]
		public void MissingCorruptAndMismatchedMarkersFailClosed()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			PerfMeterRenderDocStorageReservation missing = ReserveSource(storage, "missing-marker");
			File.Delete(missing.MarkerPath);
			Assert.That(storage.TryInspectOwnedRoot(missing.RootPath, out _, out _, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			missing.Release(out _);
			Directory.Delete(missing.RootPath, true);

			PerfMeterRenderDocStorageReservation corrupt = ReserveSource(storage, "corrupt-marker");
			File.WriteAllText(corrupt.MarkerPath, "not a marker");
			Assert.That(storage.TryInspectOwnedRoot(corrupt.RootPath, out _, out _, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			corrupt.Release(out _);
			Directory.Delete(corrupt.RootPath, true);

			PerfMeterRenderDocStorageReservation mismatch = ReserveSource(storage, "mismatch-marker");
			string markerText = File.ReadAllText(mismatch.MarkerPath);
			string replacement = (mismatch.RequestNonce + 1u).ToString("x16");
			markerText = markerText.Replace(
				"request_nonce=" + mismatch.RequestNonce.ToString("x16"),
				"request_nonce=" + replacement);
			File.WriteAllText(mismatch.MarkerPath, markerText);
			Assert.That(storage.TryInspectOwnedRoot(mismatch.RootPath, out _, out _, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(storage.TryDeleteOwnedRoot(mismatch.RootPath, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			mismatch.Release(out _);
			Directory.Delete(mismatch.RootPath, true);
		}

		[Test]
		public void UnknownOwnedRootContentRequiresManualReviewAndIsNeverDeleted()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			PerfMeterRenderDocStorageReservation reservation = ReserveSource(storage, "unknown-content");
			Assert.That(reservation.SetState(PerfMeterRenderDocStorageState.Terminal, out string terminalError), Is.EqualTo(SggRdResult.Ok), terminalError);
			string unknownPath = Path.Combine(reservation.RootPath, "user-notes.txt");
			File.WriteAllText(unknownPath, "unknown");

			Assert.That(storage.TryInspectOwnedRoot(reservation.RootPath, out _, out _, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(storage.TryDeleteOwnedRoot(reservation.RootPath, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(storage.TryCleanup((session, generation) => false, out _, out string cleanupError), Is.EqualTo(SggRdResult.Ok), cleanupError);
			Assert.That(File.Exists(unknownPath), Is.True);
			Assert.That(Directory.Exists(reservation.RootPath), Is.True);
		}

		[Test]
		public void MarkerStateCannotMoveBackward()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			PerfMeterRenderDocStorageReservation reservation = ReserveSource(storage, "state-order");
			Assert.That(reservation.SetState(PerfMeterRenderDocStorageState.AwaitingArtifact, out string awaitingError), Is.EqualTo(SggRdResult.Ok), awaitingError);

			Assert.That(reservation.SetState(PerfMeterRenderDocStorageState.Preflight, out string backwardError), Is.EqualTo(SggRdResult.InvalidArgument), backwardError);
			Assert.That(storage.TryInspectOwnedRoot(reservation.RootPath, out PerfMeterRenderDocStorageMarker marker, out _, out string inspectError), Is.EqualTo(SggRdResult.Ok), inspectError);
			Assert.That(marker.State, Is.EqualTo(PerfMeterRenderDocStorageState.AwaitingArtifact));
			Assert.That(reservation.Abort(out string abortError), Is.EqualTo(SggRdResult.Ok), abortError);
		}

		[Test]
		public void CleanupPendingTombstoneIsRediscoveredAfterRestart()
		{
			PerfMeterRenderDocStorage first = CreateStorage();
			PerfMeterRenderDocStorageReservation reservation = ReserveSource(first, "cleanup-reload");
			Assert.That(reservation.SetState(PerfMeterRenderDocStorageState.CleanupPending, out string pendingError), Is.EqualTo(SggRdResult.Ok), pendingError);
			string tombstone = reservation.RootPath + ".cleanup";
			Directory.Move(reservation.RootPath, tombstone);

			PerfMeterRenderDocStorage restarted = CreateStorage();
			Assert.That(restarted.TryCleanup((session, generation) => false, out _, out string cleanupError), Is.EqualTo(SggRdResult.Ok), cleanupError);
			Assert.That(Directory.Exists(tombstone), Is.False);
		}

		[Test]
		public void CleanupTombstoneBlocksNonceReuseAcrossStorageInstances()
		{
			PerfMeterRenderDocStorage first = CreateStorage();
			PerfMeterRenderDocStorageReservation abandoned = ReserveSource(first, "cleanup-collision");
			Assert.That(abandoned.SetState(PerfMeterRenderDocStorageState.Terminal, out string terminalError), Is.EqualTo(SggRdResult.Ok), terminalError);
			string tombstone = abandoned.RootPath + ".cleanup";
			Directory.Move(abandoned.RootPath, tombstone);

			PerfMeterRenderDocStorage second = new PerfMeterRenderDocStorage(
				_projectRoot,
				_freeSpace,
				_clock,
				new IncrementingNonceProvider(0x1000u),
				new TestRetryDelay());
			PerfMeterRenderDocStorageReservation replacement = ReserveSource(second, "replacement");

			Assert.That(replacement.RequestNonce, Is.EqualTo(0x1001u));
			Assert.That(Directory.Exists(tombstone), Is.True);
			CompleteAndDelete(second, replacement);
			Directory.Move(tombstone, abandoned.RootPath);
			Assert.That(first.TryDeleteOwnedRoot(abandoned.RootPath, out string cleanupError), Is.EqualTo(SggRdResult.Ok), cleanupError);
		}

		[Test]
		public void ReparseAliasIsRejectedWhenThePlatformSupportsSymbolicLinks()
		{
			MethodInfo createSymbolicLink = typeof(Directory).GetMethod(
				"CreateSymbolicLink",
				BindingFlags.Public | BindingFlags.Static,
				null,
				new[] { typeof(string), typeof(string) },
				null);
			if (createSymbolicLink == null && Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				Assert.Ignore("Directory symbolic links are unavailable in this Unity profile.");
			}

			PerfMeterRenderDocStorage storage = CreateStorage();
			Directory.CreateDirectory(storage.SourceRoot);
			string outsideRoot = Path.Combine(_projectRoot, "reparse-target");
			string aliasRoot = Path.Combine(storage.SourceRoot, "alias");
			Directory.CreateDirectory(outsideRoot);
			if (!TryCreateDirectorySymbolicLink(createSymbolicLink, aliasRoot, outsideRoot))
			{
				Assert.Ignore("The test host does not permit symbolic links.");
			}

			Assert.That(storage.TryInspectOwnedRoot(aliasRoot, out _, out _, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(storage.TryDeleteOwnedRoot(aliasRoot, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(Directory.Exists(outsideRoot), Is.True);
		}

		private static bool TryCreateDirectorySymbolicLink(MethodInfo createSymbolicLink, string aliasRoot, string targetRoot)
		{
			try
			{
				if (createSymbolicLink != null)
				{
					createSymbolicLink.Invoke(null, new object[] { aliasRoot, targetRoot });
					return Directory.Exists(aliasRoot);
				}

				if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				{
					return false;
				}

				using (Process process = Process.Start(new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = "/c mklink /D \"" + aliasRoot + "\" \"" + targetRoot + "\"",
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				}))
				{
					process.WaitForExit();
					return process.ExitCode == 0 && Directory.Exists(aliasRoot);
				}
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is PlatformNotSupportedException || exception is TargetInvocationException)
			{
				return false;
			}
		}

		[Test]
		public void SourceAndCopyEmbedPoolsAccountIndependently()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			PerfMeterRenderDocStorageReservation source = ReserveSource(storage, "source");
			PerfMeterRenderDocStorageReservation copy = ReserveCopy(storage, "copy", PerfMeterRenderDocStoragePolicy.MaxPayloadBytes / 2L);
			File.WriteAllBytes(Path.Combine(source.RootPath, "capture.rdc"), new byte[] { 1, 2, 3 });
			File.WriteAllBytes(Path.Combine(copy.RootPath, "capture.rdc"), new byte[] { 4, 5 });

			Assert.That(storage.TryGetUsage(out PerfMeterRenderDocStorageUsage usage, out string error), Is.EqualTo(SggRdResult.Ok), error);
			Assert.That(usage.Source.OwnedBytes, Is.GreaterThan(0L));
			Assert.That(usage.CopyEmbed.OwnedBytes, Is.GreaterThan(0L));
			Assert.That(usage.Source.Pool, Is.EqualTo(PerfMeterRenderDocStoragePool.Source));
			Assert.That(usage.CopyEmbed.Pool, Is.EqualTo(PerfMeterRenderDocStoragePool.CopyEmbed));
			Assert.That(usage.Source.ReservedBytes, Is.EqualTo(PerfMeterRenderDocStoragePolicy.SourceReservationBytes));
			Assert.That(usage.CopyEmbed.ReservedBytes, Is.EqualTo(PerfMeterRenderDocStoragePolicy.MaxPayloadBytes / 2L + PerfMeterRenderDocStoragePolicy.MetadataReserveBytes));

			CompleteAndDelete(storage, source);
			CompleteAndDelete(storage, copy);
		}

		[Test]
		public void PayloadValidationRequiresOwnedRdcAndAcceptsAStableNonemptyFile()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			PerfMeterRenderDocStorageReservation reservation = ReserveCopy(storage, "payload", 4L);
			string payloadPath = Path.Combine(reservation.RootPath, "capture.rdc");
			File.WriteAllBytes(payloadPath, new byte[] { 1, 2, 3, 4 });

			Assert.That(storage.TryValidatePayload(reservation, payloadPath, out long payloadBytes, out string payloadError), Is.EqualTo(SggRdResult.Ok), payloadError);
			Assert.That(payloadBytes, Is.EqualTo(4L));
			Assert.That(storage.TryValidatePayload(reservation, Path.Combine(_projectRoot, "outside.rdc"), out _, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			File.WriteAllBytes(payloadPath, Array.Empty<byte>());
			Assert.That(storage.TryValidatePayload(reservation, payloadPath, out _, out _), Is.EqualTo(SggRdResult.CaptureFailed));
			CompleteAndDelete(storage, reservation);
		}

		[Test]
		public void ExactCopyLimitIsAcceptedAndOneByteOverflowIsRejected()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			PerfMeterRenderDocStorageReservation exact = ReserveCopy(storage, "exact", PerfMeterRenderDocStoragePolicy.MaxPayloadBytes);
			Assert.That(exact.Release(out string releaseError), Is.EqualTo(SggRdResult.Ok), releaseError);
			Assert.That(exact.SetState(PerfMeterRenderDocStorageState.Terminal, out _), Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(
				storage.TryReserveCopyOrEmbed(
					new PerfMeterRenderDocStorageRequest("overflow", 0u),
					PerfMeterExternalArtifactStorageMode.Copy,
					PerfMeterRenderDocStoragePolicy.MaxPayloadBytes + 1L,
					out _,
					out string error),
				Is.EqualTo(SggRdResult.CaptureFailed),
				error);
			Directory.Delete(exact.RootPath, true);
		}

		[Test]
		public void FreeSpaceFloorAcceptsExactReservationAndRejectsOneByteBelow()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			_freeSpace.AvailableBytes = PerfMeterRenderDocStoragePolicy.FreeSpaceFloorBytes + PerfMeterRenderDocStoragePolicy.SourceReservationBytes - 1L;
			Assert.That(
				storage.TryReserveSource(new PerfMeterRenderDocStorageRequest("floor-fail", 0u), out _, out string belowError),
				Is.EqualTo(SggRdResult.CaptureFailed),
				belowError);

			_freeSpace.AvailableBytes++;
			PerfMeterRenderDocStorageReservation exact = ReserveSource(storage, "floor-exact");
			CompleteAndDelete(storage, exact);
		}

		[Test]
		public void IndependentReservationsFailDeterministicallyAtAggregateQuota()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			List<PerfMeterRenderDocStorageReservation> reservations = new List<PerfMeterRenderDocStorageReservation>();
			for (int index = 0; index < 3; index++)
			{
				reservations.Add(ReserveSource(storage, "quota-" + index));
			}

			Assert.That(
				storage.TryReserveSource(new PerfMeterRenderDocStorageRequest("quota-overflow", 0u), out _, out string error),
				Is.EqualTo(SggRdResult.CaptureFailed),
				error);

			foreach (PerfMeterRenderDocStorageReservation reservation in reservations)
			{
				CompleteAndDelete(storage, reservation);
			}
		}

		[Test]
		public void RetentionRemovesExpiredAndOldestTerminalItemsButPreservesActiveRoots()
		{
			PerfMeterRenderDocStorage storage = CreateStorage();
			DateTimeOffset now = _clock.UtcNow;
			_clock.UtcNow = now - TimeSpan.FromDays(8);
			PerfMeterRenderDocStorageReservation expired = ReserveSource(storage, "expired");
			Assert.That(expired.SetState(PerfMeterRenderDocStorageState.Terminal, out _), Is.EqualTo(SggRdResult.Ok));
			_clock.UtcNow = now;
			Assert.That(storage.TryCleanup((session, generation) => false, out _, out string expiredError), Is.EqualTo(SggRdResult.Ok), expiredError);
			Assert.That(Directory.Exists(expired.RootPath), Is.False);

			List<PerfMeterRenderDocStorageReservation> terminal = new List<PerfMeterRenderDocStorageReservation>();
			for (int index = 0; index < PerfMeterRenderDocStoragePolicy.MaxTerminalItems + 1; index++)
			{
				PerfMeterRenderDocStorageReservation reservation = ReserveSource(storage, "terminal-" + index);
				Assert.That(reservation.SetState(PerfMeterRenderDocStorageState.Terminal, out _), Is.EqualTo(SggRdResult.Ok));
				terminal.Add(reservation);
			}

			Assert.That(storage.TryCleanup((session, generation) => false, out PerfMeterRenderDocStorageUsage usage, out string retentionError), Is.EqualTo(SggRdResult.Ok), retentionError);
			Assert.That(usage.Source.TerminalItemCount, Is.EqualTo(PerfMeterRenderDocStoragePolicy.MaxTerminalItems));
			Assert.That(Directory.Exists(terminal[0].RootPath), Is.False);

			_clock.UtcNow = now - TimeSpan.FromHours(25);
			PerfMeterRenderDocStorageReservation stale = ReserveSource(storage, "stale");
			Assert.That(stale.Release(out _), Is.EqualTo(SggRdResult.Ok));
			_clock.UtcNow = now;
			Assert.That(storage.TryCleanup((session, generation) => false, out _, out string staleError), Is.EqualTo(SggRdResult.Ok), staleError);
			Assert.That(Directory.Exists(stale.RootPath), Is.False);

			_clock.UtcNow = now - TimeSpan.FromHours(25);
			PerfMeterRenderDocStorageReservation active = ReserveSource(storage, "active");
			_clock.UtcNow = now;
			Assert.That(storage.TryCleanup((session, generation) => false, out _, out string activeError), Is.EqualTo(SggRdResult.Ok), activeError);
			Assert.That(Directory.Exists(active.RootPath), Is.True);
			CompleteAndDelete(storage, active);
			foreach (PerfMeterRenderDocStorageReservation reservation in terminal)
			{
				if (Directory.Exists(reservation.RootPath))
				{
					storage.TryDeleteOwnedRoot(reservation.RootPath, out _);
				}
			}
		}

		[Test]
		public void CleanupRetriesUseTwentyFiveAndFiftyMillisecondsAndPreservePendingOwnership()
		{
			TestRetryDelay delay = new TestRetryDelay();
			int attempts = 0;
			bool failDeletes = true;
			PerfMeterRenderDocStorage storage = CreateStorage(
				path =>
				{
					attempts++;
					if (failDeletes)
					{
						throw new IOException("transient test failure");
					}

					Directory.Delete(path, true);
				},
				delay);
			PerfMeterRenderDocStorageReservation reservation = ReserveSource(storage, "retry");
			Assert.That(reservation.SetState(PerfMeterRenderDocStorageState.Terminal, out _), Is.EqualTo(SggRdResult.Ok));

			Assert.That(storage.TryDeleteOwnedRoot(reservation.RootPath, out _), Is.EqualTo(SggRdResult.InternalError));
			Assert.That(attempts, Is.EqualTo(3));
			Assert.That(delay.Delays, Is.EqualTo(new[] { 25, 50 }));
			Assert.That(storage.TryInspectOwnedRoot(reservation.RootPath, out PerfMeterRenderDocStorageMarker marker, out _, out _), Is.EqualTo(SggRdResult.Ok));
			Assert.That(marker.State, Is.EqualTo(PerfMeterRenderDocStorageState.CleanupPending));

			failDeletes = false;
			Assert.That(storage.TryCleanup((session, generation) => false, out _, out string cleanupError), Is.EqualTo(SggRdResult.Ok), cleanupError);
			Assert.That(Directory.Exists(reservation.RootPath), Is.False);
			Assert.That(attempts, Is.EqualTo(4));
		}

		private PerfMeterRenderDocStorage CreateStorage(Action<string> deleteDirectory = null, TestRetryDelay retryDelay = null)
		{
			return new PerfMeterRenderDocStorage(
				_projectRoot,
				_freeSpace,
				_clock,
				_nonces,
				retryDelay,
				deleteDirectory);
		}

		private PerfMeterRenderDocStorageReservation ReserveSource(PerfMeterRenderDocStorage storage, string sessionId)
		{
			SggRdResult result = storage.TryReserveSource(
				new PerfMeterRenderDocStorageRequest(sessionId, 0u),
				out PerfMeterRenderDocStorageReservation reservation,
				out string error);
			Assert.That(result, Is.EqualTo(SggRdResult.Ok), error);
			return reservation;
		}

		private PerfMeterRenderDocStorageReservation ReserveCopy(PerfMeterRenderDocStorage storage, string sessionId, long payloadBytes)
		{
			SggRdResult result = storage.TryReserveCopyOrEmbed(
				new PerfMeterRenderDocStorageRequest(sessionId, 0u),
				PerfMeterExternalArtifactStorageMode.Copy,
				payloadBytes,
				out PerfMeterRenderDocStorageReservation reservation,
				out string error);
			Assert.That(result, Is.EqualTo(SggRdResult.Ok), error);
			return reservation;
		}

		private static void CompleteAndDelete(
			PerfMeterRenderDocStorage storage,
			PerfMeterRenderDocStorageReservation reservation)
		{
			if (reservation.IsReleased && !Directory.Exists(reservation.RootPath))
			{
				return;
			}

			if (!reservation.IsReleased)
			{
				Assert.That(reservation.SetState(PerfMeterRenderDocStorageState.Terminal, out string terminalError), Is.EqualTo(SggRdResult.Ok), terminalError);
			}

			if (Directory.Exists(reservation.RootPath))
			{
				Assert.That(storage.TryDeleteOwnedRoot(reservation.RootPath, out string deleteError), Is.EqualTo(SggRdResult.Ok), deleteError);
			}
		}

		private sealed class TestFreeSpace : IPerfMeterRenderDocFreeSpaceProvider
		{
			internal TestFreeSpace(long availableBytes)
			{
				AvailableBytes = availableBytes;
			}

			internal long AvailableBytes;

			public long GetAvailableBytes(string path)
			{
				return AvailableBytes;
			}
		}

		private sealed class TestClock : IPerfMeterRenderDocClock
		{
			internal TestClock(DateTimeOffset utcNow)
			{
				UtcNow = utcNow;
			}

			internal DateTimeOffset UtcNowValue;

			public DateTimeOffset UtcNow
			{
				get { return UtcNowValue; }
				set { UtcNowValue = value; }
			}
		}

		private sealed class IncrementingNonceProvider : IPerfMeterRenderDocNonceProvider
		{
			private ulong _next;

			internal IncrementingNonceProvider(ulong first)
			{
				_next = first;
			}

			public ulong NextNonce()
			{
				return _next++;
			}
		}

		private sealed class TestRetryDelay : IPerfMeterRenderDocRetryDelay
		{
			internal readonly List<int> Delays = new List<int>();

			public void Delay(TimeSpan delay)
			{
				Delays.Add((int)delay.TotalMilliseconds);
			}
		}
	}
}
