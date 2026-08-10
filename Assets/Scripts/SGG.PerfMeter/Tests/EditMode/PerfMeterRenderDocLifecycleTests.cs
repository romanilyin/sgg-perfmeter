using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterRenderDocLifecycleTests
	{
		private string _projectRoot;
		private PerfMeterRenderDocStorage _storage;
		private ManualWorkerScheduler _worker;
		private FakeBridge _bridge;
		private FakeFinalizer _finalizer;
		private PerfMeterRenderDocCaptureBackend _backend;
		private PerfMeterCaptureCoordinator _coordinator;

		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterNativeCaptureBackendRegistry.ResetForTests();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
			_projectRoot = Path.Combine(Path.GetTempPath(), "sgg-perfmeter-renderdoc-lifecycle-" + Guid.NewGuid().ToString("N"));
			_storage = new PerfMeterRenderDocStorage(
				_projectRoot,
				new TestFreeSpace(),
				new TestClock(),
				new IncrementingNonceProvider(),
				new NoRetryDelay());
			_worker = new ManualWorkerScheduler();
			_bridge = new FakeBridge();
			_finalizer = new FakeFinalizer();
			_backend = new PerfMeterRenderDocCaptureBackend(
				_bridge,
				new PerfMeterRenderDocPreflightProvider(_storage),
				new SupportedPlatformProvider(),
				_worker,
				_finalizer);
			PerfMeterNativeCaptureBackendRegistry.Register(_backend);
			_coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new UnusedGenericBackend()),
				new TestScope());
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterNativeCaptureBackendRegistry.ResetForTests();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
			if (Directory.Exists(_projectRoot))
			{
				Directory.Delete(_projectRoot, true);
			}
		}

		[Test]
		public void WorkerPreflightAndFinalizationRetainGenerationAndResources()
		{
			PerfMeterCaptureOptions options = NativeOptions("worker-success");

			Assert.That(_coordinator.Request(options), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(_coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.PreRoll));
			Assert.That(_coordinator.Status.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.Preflight));
			Assert.That(_coordinator.HasPendingCompletion, Is.True);
			Assert.That(_bridge.BeginCount, Is.Zero);
			Assert.That(Directory.Exists(_storage.SourceRoot), Is.False);

			_worker.CompleteNext();
			Assert.That(Directory.Exists(_storage.SourceRoot), Is.True);
			_coordinator.Tick();

			Assert.That(_coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Capturing));
			Assert.That(_bridge.BeginCount, Is.EqualTo(1));
			Assert.That(_backend.Snapshot.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.BeginExecuted));
			Assert.That(_storage.TryInspectOwnedRoot(
				_bridge.RootPath,
				out PerfMeterRenderDocStorageMarker marker,
				out _,
				out string markerError), Is.EqualTo(SggRdResult.Ok), markerError);
			Assert.That(marker.Generation, Is.EqualTo((ulong)_coordinator.Generation));
			Assert.That(marker.State, Is.EqualTo(PerfMeterRenderDocStorageState.Capturing));

			_coordinator.Tick();
			Assert.That(_coordinator.EndOfFramePending, Is.True);
			Assert.That(_coordinator.TickAtEndOfFrame(_coordinator.Generation), Is.True);
			Assert.That(_coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Completed));
			Assert.That(_coordinator.HasActiveResources, Is.True);
			Assert.That(_backend.Snapshot.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.AwaitingArtifact));

			_worker.CompleteNext();
			_coordinator.Tick();

			Assert.That(_coordinator.HasActiveResources, Is.False);
			Assert.That(_backend.Snapshot.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.Completed));
			Assert.That(_coordinator.TryConsumeExternalArtifact(out PerfMeterCaptureExternalArtifactCompletion completion), Is.True);
			Assert.That(completion.CaptureId, Is.EqualTo(options.CaptureId));
			Assert.That(completion.Generation, Is.EqualTo(_coordinator.Generation));
			Assert.That(completion.Artifact.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Finalized));
			Assert.That(_coordinator.TryConsumeExternalArtifact(out _), Is.False);
		}

		[Test]
		public void CancelDuringPreflightNeverBeginsAndDeletesOwnedRootOnWorker()
		{
			Assert.That(_coordinator.Request(NativeOptions("cancel-preflight")), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(_coordinator.Cancel("cancel-preflight"), Is.False);
			Assert.That(_bridge.BeginCount, Is.Zero);

			_worker.CompleteNext();
			_coordinator.Tick();
			Assert.That(_worker.PendingCount, Is.EqualTo(1), "Owned cleanup must be scheduled separately from preflight completion.");
			_worker.CompleteNext();
			_coordinator.Tick();

			Assert.That(_coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Canceled));
			Assert.That(_coordinator.HasActiveResources, Is.False);
			Assert.That(_bridge.BeginCount, Is.Zero);
			Assert.That(Directory.Exists(_storage.SourceRoot) && Directory.GetDirectories(_storage.SourceRoot).Length > 0, Is.False);
		}

		[Test]
		public void CancelDuringFinalizationSuppressesStaleArtifactCompletion()
		{
			Assert.That(_coordinator.Request(NativeOptions("cancel-finalization")), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			_worker.CompleteNext();
			_coordinator.Tick();
			_coordinator.Tick();
			Assert.That(_coordinator.TickAtEndOfFrame(_coordinator.Generation), Is.True);
			Assert.That(_coordinator.HasPendingCompletion, Is.True);

			Assert.That(_coordinator.Cancel("cancel-finalization"), Is.False);
			_worker.CompleteNext();
			_coordinator.Tick();

			Assert.That(_coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Canceled));
			Assert.That(_coordinator.HasActiveResources, Is.False);
			Assert.That(_coordinator.TryConsumeExternalArtifact(out _), Is.False);
		}

		[Test]
		public void RuntimeDestructionDuringWorkerPreflightPreservesCleanupWithoutBeginningCapture()
		{
			const string captureId = "destroy-worker-preflight";
			Assert.That(PerfMeterRuntime.EnsureRunning(), Is.True);
			PerfMeterRuntime.Instance.SetCaptureBackendV2ForTests(_backend);
			Assert.That(
				PerformanceMeter.RequestCapture(
					NativeOptions(captureId),
					new PerfMeterCaptureBundleOptions(includeScreenshot: false)),
				Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus().IsHeld, Is.True);

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			typeof(PerfMeterRuntime).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runtime, null);
			typeof(PerfMeterRuntime).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runtime, null);
			UnityEngine.Object.DestroyImmediate(runtime.gameObject);

			Assert.That(PerfMeterRuntime.Instance, Is.Null);
			Assert.That(PerformanceMeter.GetCaptureBundleStatus(captureId).State, Is.EqualTo(PerfMeterCaptureBundleState.Recording));
			Assert.That(_bridge.BeginCount, Is.Zero);

			_worker.CompleteNext();
			Assert.That(PerfMeterRuntime.EnsureRunning(), Is.False, "Completed preflight must first schedule marker-owned cleanup.");
			Assert.That(_worker.PendingCount, Is.EqualTo(1));
			_worker.CompleteNext();

			Assert.That(PerfMeterRuntime.EnsureRunning(), Is.True);
			Assert.That(PerformanceMeter.GetCaptureBundleStatus(captureId).State, Is.EqualTo(PerfMeterCaptureBundleState.Canceled));
			Assert.That(_bridge.BeginCount, Is.Zero);
			Assert.That(Directory.Exists(_storage.SourceRoot) && Directory.GetDirectories(_storage.SourceRoot).Length > 0, Is.False);
		}

		[Test]
		public void BeginFailureRetainsResourcesUntilWorkerCleanupCompletes()
		{
			_bridge.BeginResult = SggRdResult.CaptureFailed;
			Assert.That(_coordinator.Request(NativeOptions("begin-failure-cleanup")), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			_worker.CompleteNext();

			_coordinator.Tick();

			Assert.That(_coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(_coordinator.HasActiveResources, Is.True);
			Assert.That(_bridge.BeginCount, Is.EqualTo(1));
			Assert.That(_worker.PendingCount, Is.EqualTo(1));

			_worker.CompleteNext();
			_coordinator.Tick();

			Assert.That(_coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(_coordinator.HasActiveResources, Is.False);
			Assert.That(_coordinator.TryConsumeExternalArtifact(out _), Is.False);
			Assert.That(Directory.Exists(_storage.SourceRoot) && Directory.GetDirectories(_storage.SourceRoot).Length > 0, Is.False);
		}

		[Test]
		public void FinalizerExceptionRetainsResourcesUntilWorkerCleanupCompletes()
		{
			_finalizer.ThrowOnRun = true;
			Assert.That(_coordinator.Request(NativeOptions("finalizer-exception-cleanup")), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			_worker.CompleteNext();
			_coordinator.Tick();
			_coordinator.Tick();
			Assert.That(_coordinator.TickAtEndOfFrame(_coordinator.Generation), Is.True);
			_worker.CompleteNext();

			_coordinator.Tick();

			Assert.That(_coordinator.HasActiveResources, Is.True);
			Assert.That(_worker.PendingCount, Is.EqualTo(1));
			Assert.That(_coordinator.TryConsumeExternalArtifact(out _), Is.False);

			_worker.CompleteNext();
			_coordinator.Tick();

			Assert.That(_coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(_coordinator.HasActiveResources, Is.False);
			Assert.That(Directory.Exists(_storage.SourceRoot) && Directory.GetDirectories(_storage.SourceRoot).Length > 0, Is.False);
		}

		private static PerfMeterCaptureOptions NativeOptions(string captureId)
		{
			return new PerfMeterCaptureOptions(
				captureId,
				PerfMeterCaptureTool.RenderDoc,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativeRequired);
		}

		private sealed class ManualWorkerScheduler : IPerfMeterRenderDocWorkerScheduler
		{
			private readonly Queue<ICompletableOperation> _pending = new Queue<ICompletableOperation>();

			internal int PendingCount => _pending.Count;

			public IPerfMeterRenderDocWorkerOperation<T> Start<T>(Func<T> operation)
			{
				ManualOperation<T> pending = new ManualOperation<T>(operation);
				_pending.Enqueue(pending);
				return pending;
			}

			internal void CompleteNext()
			{
				Assert.That(_pending.Count, Is.GreaterThan(0));
				_pending.Dequeue().Complete();
			}

			private interface ICompletableOperation
			{
				void Complete();
			}

			private sealed class ManualOperation<T> : IPerfMeterRenderDocWorkerOperation<T>, ICompletableOperation
			{
				private readonly Func<T> _operation;
				private T _result;
				private Exception _exception;

				internal ManualOperation(Func<T> operation)
				{
					_operation = operation;
				}

				public bool IsCompleted { get; private set; }

				public void Complete()
				{
					try
					{
						_result = _operation();
					}
					catch (Exception exception)
					{
						_exception = exception;
					}
					IsCompleted = true;
				}

				public T GetResult()
				{
					if (!IsCompleted)
					{
						throw new InvalidOperationException("Worker operation is not complete.");
					}
					if (_exception != null)
					{
						throw _exception;
					}
					return _result;
				}
			}
		}

		private sealed class FakeFinalizer : IPerfMeterRenderDocArtifactFinalizer
		{
			internal bool ThrowOnRun { get; set; }

			public PerfMeterRenderDocFinalizationResult Run(
				IPerfMeterRenderDocBridge bridge,
				SggRdCaptureTokenV1 token,
				PerfMeterRenderDocPreflight preflight,
				Func<bool> isCancellationRequested = null)
			{
				if (ThrowOnRun)
				{
					throw new IOException("synthetic finalizer failure");
				}
				if (isCancellationRequested != null && isCancellationRequested())
				{
					preflight.Abort(out _);
					return new PerfMeterRenderDocFinalizationResult(
						SggRdResult.CaptureFailed,
						FailedArtifact(preflight),
						string.Empty,
						"canceled");
				}

				preflight.SetTerminal(out _);
				return new PerfMeterRenderDocFinalizationResult(
					SggRdResult.Ok,
					new PerfMeterExternalArtifactOptions(
						artifactId: preflight.ArtifactOptions.ArtifactId,
						artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
						requestId: preflight.ArtifactOptions.RequestId,
						associationState: PerfMeterExternalArtifactAssociationState.BridgeAuthenticated,
						finalizationState: PerfMeterExternalArtifactFinalizationState.Finalized,
						authorityState: PerfMeterExternalArtifactAuthorityState.Observed,
						containsGpuCaptureData: PerfMeterExternalArtifactContentState.Unknown,
						privacyFlags: preflight.ArtifactOptions.PrivacyFlags,
						storageMode: PerfMeterExternalArtifactStorageMode.MetadataOnly,
						quotaBytes: PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
						sharePolicy: PerfMeterExternalArtifactSharePolicy.DoNotShare,
						sizeBytes: 4L,
						observedSourceSha256: new string('a', 64))
						.WithSourceFileIdentitySha256(new string('b', 64))
						.ToSnapshot(),
					preflight.CapturePathTemplate + ".rdc",
					string.Empty);
			}

			private static PerfMeterExternalArtifactSnapshot FailedArtifact(PerfMeterRenderDocPreflight preflight)
			{
				return new PerfMeterExternalArtifactOptions(
					artifactId: preflight.ArtifactOptions.ArtifactId,
					artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
					requestId: preflight.ArtifactOptions.RequestId,
					finalizationState: PerfMeterExternalArtifactFinalizationState.Failed,
					warning: "canceled").ToSnapshot();
			}
		}

		private sealed class FakeBridge : IPerfMeterRenderDocBridge
		{
			internal int BeginCount { get; private set; }
			internal string RootPath { get; private set; } = string.Empty;
			internal SggRdResult BeginResult { get; set; } = SggRdResult.Ok;

			public SggRdResult GetCapabilities(out SggRdCapabilitiesV1 capabilities)
			{
				capabilities = new SggRdCapabilitiesV1
				{
					StructSize = PerfMeterRenderDocAbiV1.CapabilitiesSizeAsUInt,
					BridgeAbiMajor = PerfMeterRenderDocAbiV1.AbiMajor,
					BridgeAbiMinor = PerfMeterRenderDocAbiV1.AbiMinor,
					PlatformSupported = 1u,
					ModuleLoaded = 1u,
					ExportAvailable = 1u,
					ApiNegotiated = 1u,
					TargetControlConnected = 1u,
					ApiMajor = 1u,
					ApiMinor = 7u,
					SupportsDiscard = 1u,
					SupportsComments = 1u,
					SupportsTitle = 1u,
					CaptureCount = 0u
				};
				return SggRdResult.Ok;
			}

			public SggRdResult BeginCapture(ulong requestNonce, string capturePathTemplate, string title, out SggRdCaptureTokenV1 token)
			{
				BeginCount++;
				RootPath = Path.GetDirectoryName(capturePathTemplate);
				token = new SggRdCaptureTokenV1
				{
					StructSize = PerfMeterRenderDocAbiV1.CaptureTokenSizeAsUInt,
					RequestNonce = requestNonce,
					CountBefore = 0u,
					StartUnixNanoseconds = 1u
				};
				return BeginResult;
			}

			public SggRdResult EndCapture(SggRdCaptureTokenV1 token) => SggRdResult.Ok;
			public SggRdResult DiscardCapture(SggRdCaptureTokenV1 token) => SggRdResult.Ok;
			public SggRdResult TryGetNewArtifact(SggRdCaptureTokenV1 token, out SggRdArtifactV1 artifact, out string observedPath) { artifact = default; observedPath = string.Empty; return SggRdResult.CaptureNotObserved; }
			public SggRdResult SetCaptureComments(SggRdCaptureTokenV1 token, string observedPath, string comments) => SggRdResult.Ok;
		}

		private sealed class SupportedPlatformProvider : IPerfMeterRenderDocPlatformProvider
		{
			public PerfMeterRenderDocPlatformInfo GetPlatformInfo()
			{
				return new PerfMeterRenderDocPlatformInfo(RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D11, true, true);
			}
		}

		private sealed class TestScope : IPerfMeterCaptureScope
		{
			private string _captureId = string.Empty;
			public bool TryBegin(string captureId) { if (!string.IsNullOrEmpty(_captureId)) return false; _captureId = captureId; return true; }
			public bool TryEnd(string captureId) { if (_captureId != captureId) return false; _captureId = string.Empty; return true; }
		}

		private sealed class UnusedGenericBackend : IPerfMeterCaptureBackend
		{
			public PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool) => new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Unavailable, "unused");
			public bool TryBegin(PerfMeterCaptureTool tool, out string error) { error = "unused"; return false; }
			public bool TryEnd(out string error) { error = string.Empty; return true; }
		}

		private sealed class TestFreeSpace : IPerfMeterRenderDocFreeSpaceProvider
		{
			public long GetAvailableBytes(string path) => long.MaxValue;
		}

		private sealed class TestClock : IPerfMeterRenderDocClock
		{
			public DateTimeOffset UtcNow => new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
		}

		private sealed class IncrementingNonceProvider : IPerfMeterRenderDocNonceProvider
		{
			private ulong _next = 0x3000u;
			public ulong NextNonce() => _next++;
		}

		private sealed class NoRetryDelay : IPerfMeterRenderDocRetryDelay
		{
			public void Delay(TimeSpan delay) { }
		}
	}
}
