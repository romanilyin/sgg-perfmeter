using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterRenderDocArtifactFinalizerTests
	{
		private string _projectRoot;
		private PerfMeterRenderDocStorage _storage;
		private TestMonotonicClock _clock;
		private TestFileBindingFactory _files;
		private ulong _nextNonce;

		[SetUp]
		public void SetUp()
		{
			_projectRoot = Path.Combine(Path.GetTempPath(), "sgg-perfmeter-renderdoc-finalizer-" + Guid.NewGuid().ToString("N"));
			_clock = new TestMonotonicClock();
			_files = new TestFileBindingFactory(_clock);
			_nextNonce = 0x2000u;
			_storage = new PerfMeterRenderDocStorage(
				_projectRoot,
				new TestFreeSpace(),
				new TestUtcClock(),
				new TestNonceProvider(() => _nextNonce++),
				new NoRetryDelay());
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
		public void FixedFinalizationPolicyValuesRemainBaselined()
		{
			Assert.That(PerfMeterRenderDocFinalizationPolicy.PollMilliseconds, Is.EqualTo(100));
			Assert.That(PerfMeterRenderDocFinalizationPolicy.FirstCandidateMilliseconds, Is.EqualTo(30000));
			Assert.That(PerfMeterRenderDocFinalizationPolicy.QuietMilliseconds, Is.EqualTo(500));
			Assert.That(PerfMeterRenderDocFinalizationPolicy.StabilizationMilliseconds, Is.EqualTo(60000));
			Assert.That(PerfMeterRenderDocFinalizationPolicy.StableSampleCount, Is.EqualTo(4));
			Assert.That(PerfMeterRenderDocFinalizationPolicy.StableSampleMilliseconds, Is.EqualTo(250));
			Assert.That(PerfMeterRenderDocFinalizationPolicy.TotalMilliseconds, Is.EqualTo(180000));
		}

		[Test]
		public void MetadataOnlyFinalizationReenumeratesStabilizesAndPublishesSanitizedProvenance()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.MetadataOnly, new byte[] { 1, 2, 3, 4 });
			FakeBridge bridge = FakeBridge.Always(fixture.Path);

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(bridge, fixture.Token, fixture.Preflight);

			Assert.That(result.Succeeded, Is.True, result.Warning);
			Assert.That(bridge.ArtifactCalls, Is.GreaterThanOrEqualTo(6));
			Assert.That(result.RetainedPayloadPath, Is.Empty);
			Assert.That(result.Artifact.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Finalized));
			Assert.That(result.Artifact.AssociationState, Is.EqualTo(PerfMeterExternalArtifactAssociationState.BridgeAuthenticated));
			Assert.That(result.Artifact.AuthorityState, Is.EqualTo(PerfMeterExternalArtifactAuthorityState.Authenticated));
			Assert.That(result.Artifact.IsAuthoritative, Is.True);
			Assert.That(result.Artifact.StorageMode, Is.EqualTo(PerfMeterExternalArtifactStorageMode.MetadataOnly));
			Assert.That(result.Artifact.SharePolicy, Is.EqualTo(PerfMeterExternalArtifactSharePolicy.DoNotShare));
			Assert.That(result.Artifact.SizeBytes, Is.EqualTo(4L));
			Assert.That(result.Artifact.ObservedSourceSha256, Has.Length.EqualTo(64));
			Assert.That(result.Artifact.SourceFileIdentitySha256, Has.Length.EqualTo(64));
			Assert.That(result.Artifact.SourceFileIdentitySha256, Does.Not.Contain(fixture.Path));
			AssertTerminal(fixture.Preflight.RootPath);
		}

		[Test]
		public void FirstCandidateDeadlineExpiresAtExactBoundary()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.MetadataOnly, new byte[] { 1 });
			FakeBridge bridge = FakeBridge.Never();

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(bridge, fixture.Token, fixture.Preflight);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.CaptureNotObserved));
			Assert.That(result.Warning, Is.EqualTo("renderdoc_artifact_observation_deadline"));
			Assert.That(_clock.Timestamp, Is.EqualTo(30001L));
			AssertTerminal(fixture.Preflight.RootPath);
		}

		[Test]
		public void CandidateReturnedAfterObservationDeadlineIsRejected()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.MetadataOnly, new byte[] { 1 });
			FakeBridge bridge = FakeBridge.Always(fixture.Path);
			bridge.OnArtifactCall = () => _clock.Delay(TimeSpan.FromMilliseconds(PerfMeterRenderDocFinalizationPolicy.FirstCandidateMilliseconds));

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(bridge, fixture.Token, fixture.Preflight);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.CaptureNotObserved));
			Assert.That(result.Warning, Is.EqualTo("renderdoc_artifact_observation_deadline"));
			Assert.That(bridge.ArtifactCalls, Is.EqualTo(1));
			AssertTerminal(fixture.Preflight.RootPath);
		}

		[Test]
		public void DelayedSecondCandidateFailsDuringQuietWindow()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.MetadataOnly, new byte[] { 1 });
			FakeBridge bridge = new FakeBridge(fixture.Path,
				SggRdResult.Ok,
				SggRdResult.Ok,
				SggRdResult.CaptureFailed);

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(bridge, fixture.Token, fixture.Preflight);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.CaptureFailed));
			Assert.That(result.Warning, Is.EqualTo("renderdoc_artifact_ambiguous"));
			Assert.That(bridge.ArtifactCalls, Is.EqualTo(3));
			AssertTerminal(fixture.Preflight.RootPath);
		}

		[Test]
		public void ZeroAndGrowingSamplesMustReachFourUnchangedNonzeroSamples()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.MetadataOnly, Array.Empty<byte>());
			_files.SetBytes(fixture.Path, new byte[] { 4, 5, 6 });
			_files.EnqueueSamples(fixture.Path,
				Sample(0, 1),
				Sample(1, 2),
				Sample(2, 3),
				Sample(3, 4),
				Sample(3, 4),
				Sample(3, 4),
				Sample(3, 4));

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(FakeBridge.Always(fixture.Path), fixture.Token, fixture.Preflight);

			Assert.That(result.Succeeded, Is.True, result.Warning);
			Assert.That(result.Artifact.SizeBytes, Is.EqualTo(3L));
			Assert.That(_clock.Delays.FindAll(value => value == 250).Count, Is.GreaterThanOrEqualTo(6));
		}

		[Test]
		public void PathIdentityReplacementAfterHashFailsNonAuthoritatively()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.MetadataOnly, new byte[] { 8, 9 });
			_files.ReplaceIdentityOnOpen = 2;

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(FakeBridge.Always(fixture.Path), fixture.Token, fixture.Preflight);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.CaptureFailed));
			Assert.That(result.Warning, Is.EqualTo("renderdoc_artifact_identity_changed"));
			Assert.That(result.Artifact.IsAuthoritative, Is.False);
			Assert.That(result.Artifact.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Failed));
		}

		[Test]
		public void WindowsBindingReadsARegularFileThroughTheValidatedHandle()
		{
			if (!PerfMeterRenderDocWindowsFileSystem.IsSupported)
			{
				Assert.Ignore("Windows file-identity validation is unavailable on this host.");
			}

			Directory.CreateDirectory(_projectRoot);
			string path = Path.Combine(_projectRoot, "regular-capture.rdc");
			File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
			PerfMeterRenderDocWindowsFileSystem.FileBindingFactory factory =
				new PerfMeterRenderDocWindowsFileSystem.FileBindingFactory();

			Assert.That(
				factory.TryOpen(path, out IPerfMeterRenderDocFileBinding binding, out string openError),
				Is.EqualTo(SggRdResult.Ok),
				openError);
			using (binding)
			{
				Assert.That(binding.TrySample(out PerfMeterRenderDocFileSample sample, out string sampleError), Is.EqualTo(SggRdResult.Ok), sampleError);
				Assert.That(sample.SizeBytes, Is.EqualTo(3L));
				Assert.That(sample.Identity, Has.Length.EqualTo(12));
				Assert.That(
					binding.TryComputeSha256(3L, () => false, out string hash, out string hashError),
					Is.EqualTo(SggRdResult.Ok),
					hashError);
				Assert.That(hash, Has.Length.EqualTo(64));
			}
		}

		[Test]
		public void WindowsBindingRejectsAncestorJunctionTraversal()
		{
			if (!PerfMeterRenderDocWindowsFileSystem.IsSupported)
			{
				Assert.Ignore("Windows file-identity validation is unavailable on this host.");
			}

			string targetRoot = Path.Combine(_projectRoot, "junction-target");
			string aliasRoot = Path.Combine(_projectRoot, "junction-alias");
			Directory.CreateDirectory(targetRoot);
			string targetPath = Path.Combine(targetRoot, "capture.rdc");
			File.WriteAllBytes(targetPath, new byte[] { 1, 2, 3 });
			if (!TryCreateDirectoryJunction(aliasRoot, targetRoot))
			{
				Assert.Ignore("The test host does not permit directory junctions.");
			}

			try
			{
				PerfMeterRenderDocWindowsFileSystem.FileBindingFactory factory =
					new PerfMeterRenderDocWindowsFileSystem.FileBindingFactory();
				SggRdResult result = factory.TryOpen(
					Path.Combine(aliasRoot, "capture.rdc"),
					out IPerfMeterRenderDocFileBinding binding,
					out string error);

				binding?.Dispose();
				Assert.That(result, Is.EqualTo(SggRdResult.CaptureFailed));
				Assert.That(error, Is.EqualTo("renderdoc_file_path_reparse_or_changed"));
				Assert.That(File.Exists(targetPath), Is.True);
			}
			finally
			{
				Directory.Delete(aliasRoot, false);
			}
		}

		[Test]
		public void StabilizationDeadlineExpiresAtExactBoundaryForZeroByteFile()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.MetadataOnly, Array.Empty<byte>());

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(FakeBridge.Always(fixture.Path), fixture.Token, fixture.Preflight);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.CaptureFailed));
			Assert.That(result.Warning, Is.EqualTo("renderdoc_artifact_stabilization_deadline"));
			Assert.That(_clock.Timestamp, Is.EqualTo(60001L));
			AssertTerminal(fixture.Preflight.RootPath);
		}

		[Test]
		public void TotalDeadlineExpiresAtExactBoundaryAfterSourceHash()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.MetadataOnly, new byte[] { 1, 2 });
			_files.HashAdvanceMilliseconds = 178750;

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(FakeBridge.Always(fixture.Path), fixture.Token, fixture.Preflight);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.CaptureFailed));
			Assert.That(result.Warning, Is.EqualTo("renderdoc_finalization_deadline"));
			Assert.That(_clock.Timestamp, Is.EqualTo(180001L));
			AssertTerminal(fixture.Preflight.RootPath);
		}

		[Test]
		public void CopyUsesIndependentPoolAndRequiresReviewBeforeShare()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.Copy, new byte[] { 10, 11, 12 });

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(FakeBridge.Always(fixture.Path), fixture.Token, fixture.Preflight);

			Assert.That(result.Succeeded, Is.True, result.Warning);
			Assert.That(result.RetainedPayloadPath, Does.StartWith(_storage.CopyRoot));
			Assert.That(File.Exists(result.RetainedPayloadPath), Is.True);
			Assert.That(result.Artifact.StorageMode, Is.EqualTo(PerfMeterExternalArtifactStorageMode.Copy));
			Assert.That(result.Artifact.SharePolicy, Is.EqualTo(PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare));
			Assert.That(result.Artifact.PostCopySha256, Is.EqualTo(result.Artifact.ObservedSourceSha256));
			Assert.That(result.Artifact.IsAuthoritative, Is.True);
			Assert.That(result.Token.RequestNonce, Is.EqualTo(fixture.Token.RequestNonce));
			Assert.That(result.ObservedArtifact.StructSize, Is.EqualTo(PerfMeterRenderDocAbiV1.ArtifactSizeAsUInt));
			Assert.That(result.PayloadSource, Is.Not.Null);
			IDictionary reservations = (IDictionary)typeof(PerfMeterRenderDocStorage)
				.GetField("_reservations", BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(_storage);
			Assert.That(reservations.Contains(Path.GetDirectoryName(result.RetainedPayloadPath)), Is.False);
			Assert.That(result.PayloadSource.TryValidate(null, out string validationError), Is.True, validationError);
			byte[] changedBytes = { 12, 11, 10 };
			File.WriteAllBytes(result.RetainedPayloadPath, changedBytes);
			_files.SetBytes(result.RetainedPayloadPath, changedBytes);
			Assert.That(result.PayloadSource.TryValidate(null, out validationError), Is.False);
			Assert.That(validationError, Is.EqualTo("renderdoc_copy_descriptor_hash_changed"));
			Assert.That(_storage.TryGetUsage(out PerfMeterRenderDocStorageUsage usage, out string error), Is.EqualTo(SggRdResult.Ok), error);
			Assert.That(usage.Source.TerminalItemCount, Is.EqualTo(1));
			Assert.That(usage.CopyEmbed.TerminalItemCount, Is.EqualTo(1));
		}

		[Test]
		public void PostCopyHashMismatchFailsAndRemovesOwnedCopyStaging()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.Copy, new byte[] { 10, 11, 12 });
			_files.HashMismatchOnOpen = 3;

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(FakeBridge.Always(fixture.Path), fixture.Token, fixture.Preflight);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.CaptureFailed));
			Assert.That(result.Warning, Is.EqualTo("renderdoc_copy_hash_mismatch"));
			Assert.That(result.Artifact.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Failed));
			Assert.That(_storage.TryGetUsage(out PerfMeterRenderDocStorageUsage usage, out string error), Is.EqualTo(SggRdResult.Ok), error);
			Assert.That(usage.Source.TerminalItemCount, Is.EqualTo(1));
			Assert.That(usage.CopyEmbed.ItemCount, Is.Zero);
		}

		[Test]
		public void SourceTerminalFailureRemovesAlreadyTerminalCopy()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.Copy, new byte[] { 10, 11, 12 });
			_files.OnDispose = openIndex =>
			{
				if (openIndex == 4)
				{
					File.WriteAllText(fixture.Preflight.Reservation.MarkerPath, "corrupt");
				}
			};

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(
				FakeBridge.Always(fixture.Path),
				fixture.Token,
				fixture.Preflight);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.InternalError));
			Assert.That(result.Artifact.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Failed));
			Assert.That(Directory.Exists(_storage.CopyRoot) && Directory.GetDirectories(_storage.CopyRoot).Length > 0, Is.False);
		}

		[Test]
		public void EmbedRemainsFailClosedWithoutDedicatedNativeBundlePath()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.Embed, new byte[] { 1 });

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(FakeBridge.Always(fixture.Path), fixture.Token, fixture.Preflight);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.InvalidArgument));
			Assert.That(result.Warning, Is.EqualTo("renderdoc_embed_path_not_enabled"));
			Assert.That(result.Artifact.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Failed));
			AssertTerminal(fixture.Preflight.RootPath);
		}

		[Test]
		public void CancellationDuringStabilizationTerminatesOwnedSource()
		{
			CaptureFixture fixture = CreateFixture(PerfMeterExternalArtifactStorageMode.MetadataOnly, new byte[] { 1 });
			int checks = 0;

			PerfMeterRenderDocFinalizationResult result = CreateFinalizer().Run(
				FakeBridge.Always(fixture.Path),
				fixture.Token,
				fixture.Preflight,
				() => ++checks >= 10);

			Assert.That(result.Result, Is.EqualTo(SggRdResult.CaptureFailed));
			Assert.That(result.Warning, Is.EqualTo("renderdoc_finalization_canceled"));
			AssertTerminal(fixture.Preflight.RootPath);
		}

		private PerfMeterRenderDocArtifactFinalizer CreateFinalizer()
		{
			return new PerfMeterRenderDocArtifactFinalizer(_storage, _files, _clock);
		}

		private CaptureFixture CreateFixture(PerfMeterExternalArtifactStorageMode storageMode, byte[] bytes)
		{
			string captureId = "finalizer-" + storageMode.ToString().ToLowerInvariant();
			Assert.That(
				_storage.TryReserveSource(
					new PerfMeterRenderDocStorageRequest("opaque-session", 7u),
					out PerfMeterRenderDocStorageReservation reservation,
					out string error),
				Is.EqualTo(SggRdResult.Ok),
				error);
			string path = Path.Combine(reservation.RootPath, "capture.rdc");
			File.WriteAllBytes(path, bytes ?? Array.Empty<byte>());
			_files.Register(path, bytes ?? Array.Empty<byte>());
			PerfMeterExternalArtifactOptions options = new PerfMeterExternalArtifactOptions(
				artifactId: captureId + "-renderdoc",
				artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
				requestId: captureId,
				containsGpuCaptureData: PerfMeterExternalArtifactContentState.Unknown,
				privacyFlags: PerfMeterExternalArtifactPrivacyFlags.ContainsGpuCaptureData |
					PerfMeterExternalArtifactPrivacyFlags.Sensitive |
					PerfMeterExternalArtifactPrivacyFlags.RequiresReview,
				storageMode: storageMode,
				quotaBytes: PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
				sharePolicy: storageMode == PerfMeterExternalArtifactStorageMode.MetadataOnly
					? PerfMeterExternalArtifactSharePolicy.DoNotShare
					: PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare);
			PerfMeterRenderDocPreflight preflight = new PerfMeterRenderDocPreflight(
				reservation.RequestNonce,
				reservation.CapturePathTemplate,
				"test",
				options,
				reservation);
			SggRdCaptureTokenV1 token = new SggRdCaptureTokenV1
			{
				StructSize = PerfMeterRenderDocAbiV1.CaptureTokenSizeAsUInt,
				RequestNonce = reservation.RequestNonce,
				CountBefore = 0u,
				StartUnixNanoseconds = 1u
			};
			return new CaptureFixture(path, preflight, token);
		}

		private void AssertTerminal(string rootPath)
		{
			Assert.That(_storage.TryInspectOwnedRoot(rootPath, out PerfMeterRenderDocStorageMarker marker, out _, out string error), Is.EqualTo(SggRdResult.Ok), error);
			Assert.That(marker.State, Is.EqualTo(PerfMeterRenderDocStorageState.Terminal));
		}

		private static PerfMeterRenderDocFileSample Sample(long size, long write)
		{
			return new PerfMeterRenderDocFileSample(new byte[] { 1, 2, 3, 4 }, size, write);
		}

		private static bool TryCreateDirectoryJunction(string aliasRoot, string targetRoot)
		{
			try
			{
				using (Process process = Process.Start(new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = "/c mklink /J \"" + aliasRoot + "\" \"" + targetRoot + "\"",
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
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				return false;
			}
		}

		private readonly struct CaptureFixture
		{
			internal CaptureFixture(string path, PerfMeterRenderDocPreflight preflight, SggRdCaptureTokenV1 token)
			{
				Path = path;
				Preflight = preflight;
				Token = token;
			}

			internal string Path { get; }
			internal PerfMeterRenderDocPreflight Preflight { get; }
			internal SggRdCaptureTokenV1 Token { get; }
		}

		private sealed class FakeBridge : IPerfMeterRenderDocBridge
		{
			private readonly string _path;
			private readonly Queue<SggRdResult> _results;
			private SggRdResult _last;

			internal FakeBridge(string path, params SggRdResult[] results)
			{
				_path = path ?? string.Empty;
				_results = new Queue<SggRdResult>(results ?? Array.Empty<SggRdResult>());
				_last = _results.Count == 0 ? SggRdResult.Ok : SggRdResult.CaptureNotObserved;
			}

			internal static FakeBridge Always(string path) => new FakeBridge(path, SggRdResult.Ok);
			internal static FakeBridge Never() => new FakeBridge(string.Empty, SggRdResult.CaptureNotObserved);
			internal int ArtifactCalls { get; private set; }
			internal Action OnArtifactCall { get; set; }

			public SggRdResult TryGetNewArtifact(SggRdCaptureTokenV1 token, out SggRdArtifactV1 artifact, out string observedPath)
			{
				ArtifactCalls++;
				OnArtifactCall?.Invoke();
				if (_results.Count > 0)
				{
					_last = _results.Dequeue();
				}
				artifact = new SggRdArtifactV1
				{
					StructSize = PerfMeterRenderDocAbiV1.ArtifactSizeAsUInt,
					Index = token.CountBefore,
					RenderDocTimestampSeconds = 0u,
					ObservedUnixNanoseconds = 1u
				};
				observedPath = _path;
				return _last;
			}

			public SggRdResult GetCapabilities(out SggRdCapabilitiesV1 capabilities) { capabilities = default; return SggRdResult.Ok; }
			public SggRdResult BeginCapture(ulong requestNonce, string capturePathTemplate, string title, out SggRdCaptureTokenV1 token) { token = default; return SggRdResult.Ok; }
			public SggRdResult EndCapture(SggRdCaptureTokenV1 token) => SggRdResult.Ok;
			public SggRdResult DiscardCapture(SggRdCaptureTokenV1 token) => SggRdResult.Ok;
			public SggRdResult SetCaptureComments(SggRdCaptureTokenV1 token, string observedPath, string comments) => SggRdResult.Ok;
		}

		private sealed class TestMonotonicClock : IPerfMeterRenderDocMonotonicClock
		{
			internal readonly List<int> Delays = new List<int>();
			public long Timestamp { get; private set; } = 1L;
			public long Frequency => 1000L;
			public void Delay(TimeSpan delay)
			{
				int milliseconds = (int)delay.TotalMilliseconds;
				Delays.Add(milliseconds);
				Timestamp += milliseconds;
			}
		}

		private sealed class TestFileBindingFactory : IPerfMeterRenderDocFileBindingFactory
		{
			private readonly Dictionary<string, Record> _records = new Dictionary<string, Record>(StringComparer.Ordinal);
			private readonly TestMonotonicClock _clock;
			private int _openCount;

			internal TestFileBindingFactory(TestMonotonicClock clock)
			{
				_clock = clock;
			}

			internal int ReplaceIdentityOnOpen { get; set; }
			internal int HashMismatchOnOpen { get; set; }
			internal int HashAdvanceMilliseconds { get; set; }
			internal Action<int> OnDispose { get; set; }

			internal void Register(string path, byte[] bytes)
			{
				_records[path] = new Record(bytes, new byte[] { 1, 2, 3, 4 }, 1L);
			}

			internal void SetBytes(string path, byte[] bytes)
			{
				_records[path].Bytes = bytes;
			}

			internal void EnqueueSamples(string path, params PerfMeterRenderDocFileSample[] samples)
			{
				foreach (PerfMeterRenderDocFileSample sample in samples)
				{
					_records[path].Samples.Enqueue(sample);
				}
			}

			public SggRdResult TryOpen(string path, out IPerfMeterRenderDocFileBinding binding, out string error)
			{
				_openCount++;
				if (!_records.TryGetValue(path, out Record record))
				{
					binding = null;
					error = "missing";
					return SggRdResult.CaptureNotObserved;
				}
				if (ReplaceIdentityOnOpen == _openCount)
				{
					record.Identity = new byte[] { 9, 9, 9, 9 };
				}
				binding = new Binding(this, record, _openCount);
				error = string.Empty;
				return SggRdResult.Ok;
			}

			private sealed class Record
			{
				internal Record(byte[] bytes, byte[] identity, long write)
				{
					Bytes = bytes ?? Array.Empty<byte>();
					Identity = identity;
					Write = write;
				}
				internal byte[] Bytes;
				internal byte[] Identity;
				internal long Write;
				internal readonly Queue<PerfMeterRenderDocFileSample> Samples = new Queue<PerfMeterRenderDocFileSample>();
			}

			private sealed class Binding : IPerfMeterRenderDocFileBinding
			{
				private readonly TestFileBindingFactory _owner;
				private readonly Record _record;
				private readonly int _openIndex;
				internal Binding(TestFileBindingFactory owner, Record record, int openIndex) { _owner = owner; _record = record; _openIndex = openIndex; }
				public SggRdResult TrySample(out PerfMeterRenderDocFileSample sample, out string error)
				{
					if (_record.Samples.Count > 0)
					{
						sample = _record.Samples.Dequeue();
						_record.Identity = (byte[])sample.Identity.Clone();
						_record.Write = sample.LastWriteTicks;
					}
					else
					{
						sample = new PerfMeterRenderDocFileSample((byte[])_record.Identity.Clone(), _record.Bytes.LongLength, _record.Write);
					}
					error = string.Empty;
					return SggRdResult.Ok;
				}
				public SggRdResult TryComputeSha256(long maximumBytes, Func<bool> shouldStop, out string sha256, out string error)
				{
					if (_owner.HashAdvanceMilliseconds > 0)
					{
						_owner._clock.Delay(TimeSpan.FromMilliseconds(_owner.HashAdvanceMilliseconds));
						_owner.HashAdvanceMilliseconds = 0;
					}
					if (shouldStop != null && shouldStop())
					{
						sha256 = string.Empty;
						error = "stopped";
						return SggRdResult.CaptureFailed;
					}
					if (_record.Bytes.LongLength > maximumBytes)
					{
						sha256 = string.Empty;
						error = "limit";
						return SggRdResult.CaptureFailed;
					}
					using (SHA256 algorithm = SHA256.Create())
					{
						sha256 = BitConverter.ToString(algorithm.ComputeHash(_record.Bytes)).Replace("-", string.Empty).ToLowerInvariant();
					}
					if (_owner.HashMismatchOnOpen == _openIndex)
					{
						sha256 = new string('0', 64);
					}
					error = string.Empty;
					return SggRdResult.Ok;
				}
				public SggRdResult TryCopyTo(string destinationPath, long maximumBytes, Func<bool> shouldStop, out string error)
				{
					if ((shouldStop != null && shouldStop()) || _record.Bytes.LongLength > maximumBytes)
					{
						error = "canceled";
						return SggRdResult.CaptureFailed;
					}
					File.WriteAllBytes(destinationPath, _record.Bytes);
					_owner.Register(destinationPath, (byte[])_record.Bytes.Clone());
					error = string.Empty;
					return SggRdResult.Ok;
				}
				public void Dispose() => _owner.OnDispose?.Invoke(_openIndex);
			}
		}

		private sealed class TestFreeSpace : IPerfMeterRenderDocFreeSpaceProvider
		{
			public long GetAvailableBytes(string path) => long.MaxValue;
		}

		private sealed class TestUtcClock : IPerfMeterRenderDocClock
		{
			public DateTimeOffset UtcNow => new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
		}

		private sealed class TestNonceProvider : IPerfMeterRenderDocNonceProvider
		{
			private readonly Func<ulong> _next;
			internal TestNonceProvider(Func<ulong> next) { _next = next; }
			public ulong NextNonce() => _next();
		}

		private sealed class NoRetryDelay : IPerfMeterRenderDocRetryDelay
		{
			public void Delay(TimeSpan delay) { }
		}
	}
}
