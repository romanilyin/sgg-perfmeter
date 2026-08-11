using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterCaptureCoordinatorTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterProfilerInstrumentation.Reset();
			PerfMeterNativeCaptureBackendRegistry.ResetForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterProfilerInstrumentation.Reset();
			PerfMeterNativeCaptureBackendRegistry.ResetForTests();
		}

		[Test]
		public void RenderDocBackendEnumsAndLegacyDefaultsHaveStableValues()
		{
			Assert.That((int)PerfMeterCaptureBackendMode.GenericUnity, Is.EqualTo(0));
			Assert.That((int)PerfMeterCaptureBackendMode.NativePreferred, Is.EqualTo(1));
			Assert.That((int)PerfMeterCaptureBackendMode.NativeRequired, Is.EqualTo(2));
			Assert.That((int)PerfMeterCaptureBackendKind.GenericUnity, Is.EqualTo(0));
			Assert.That((int)PerfMeterCaptureBackendKind.RenderDocNative, Is.EqualTo(1));
			Assert.That((int)PerfMeterRenderDocCapturePhase.None, Is.EqualTo(0));
			Assert.That((int)PerfMeterRenderDocCapturePhase.LostSession, Is.EqualTo(10));

			PerfMeterCaptureOptions legacy = new PerfMeterCaptureOptions("legacy", PerfMeterCaptureTool.RenderDoc);
			Assert.That(legacy.BackendMode, Is.EqualTo(PerfMeterCaptureBackendMode.GenericUnity));
			Assert.That(legacy.ExternalArtifactStorageMode, Is.EqualTo(PerfMeterExternalArtifactStorageMode.MetadataOnly));
			Assert.That(typeof(PerfMeterCaptureOptions).GetConstructor(new[]
			{
				typeof(string),
				typeof(PerfMeterCaptureTool),
				typeof(int),
				typeof(int),
				typeof(int)
			}), Is.Not.Null, "The released five-argument constructor must remain binary-compatible.");
			Assert.That(typeof(PerfMeterCaptureOptions).GetConstructor(new[]
			{
				typeof(string),
				typeof(PerfMeterCaptureTool),
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(PerfMeterCaptureBackendMode)
			}), Is.Not.Null, "The released six-argument constructor must remain binary-compatible.");
			PerfMeterCaptureOptions copied = new PerfMeterCaptureOptions(
				"copy",
				PerfMeterCaptureTool.RenderDoc,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativeRequired,
				PerfMeterExternalArtifactStorageMode.Copy);
			Assert.That(copied.ExternalArtifactStorageMode, Is.EqualTo(PerfMeterExternalArtifactStorageMode.Copy));
			PerfMeterCaptureStatusSnapshot oldStatus = new PerfMeterCaptureStatusSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterCaptureState.Capturing,
				"legacy",
				PerfMeterCaptureTool.RenderDoc,
				0,
				1,
				0,
				0,
				0,
				0,
				string.Empty);
			Assert.That(oldStatus.RequestedBackendMode, Is.EqualTo(PerfMeterCaptureBackendMode.GenericUnity));
			Assert.That(oldStatus.EffectiveBackendKind, Is.EqualTo(PerfMeterCaptureBackendKind.GenericUnity));
			Assert.That(oldStatus.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.None));
			Assert.That(oldStatus.NativeResultCode, Is.EqualTo(-1));
			Assert.That(oldStatus.FallbackReason, Is.Empty);
		}

		[Test]
		public void NativeRequiredUnavailableDoesNotInvokeGenericFallback()
		{
			FakeCaptureBackend generic = new FakeCaptureBackend();
			PerfMeterCaptureBackendRouter router = new PerfMeterCaptureBackendRouter(generic);
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator((IPerfMeterCaptureBackendV2)router, new FakeCaptureScope());

			PerfMeterCaptureRequestResult result = coordinator.Request(new PerfMeterCaptureOptions(
				"required",
				PerfMeterCaptureTool.RenderDoc,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativeRequired));

			Assert.That(result, Is.EqualTo(PerfMeterCaptureRequestResult.Unavailable));
			Assert.That(generic.BeginCount, Is.Zero);
			Assert.That(coordinator.Status.EffectiveBackendKind, Is.EqualTo(PerfMeterCaptureBackendKind.RenderDocNative));
			Assert.That(coordinator.Status.NativeResultCode, Is.EqualTo(PerfMeterNativeCaptureResultCodes.UnsupportedPlatform));
			Assert.That(coordinator.Status.FallbackReason, Is.Empty);
		}

		[TestCase(PerfMeterNativeCaptureResultCodes.NotLoaded, PerfMeterCaptureFallbackReasons.NotLoaded)]
		[TestCase(PerfMeterNativeCaptureResultCodes.ExportMissing, PerfMeterCaptureFallbackReasons.ExportMissing)]
		[TestCase(PerfMeterNativeCaptureResultCodes.ApiNegotiationFailed, PerfMeterCaptureFallbackReasons.ApiNegotiationFailed)]
		[TestCase(PerfMeterNativeCaptureResultCodes.UnsupportedPlatform, PerfMeterCaptureFallbackReasons.UnsupportedPlatform)]
		public void NativePreferredUnavailableAndBeginFailureUseOnlyAllowedFallback(int beginFailureCode, string expectedFallbackReason)
		{
			FakeCaptureBackend generic = new FakeCaptureBackend();
			PerfMeterCaptureBackendRouter absentRouter = new PerfMeterCaptureBackendRouter(generic);
			PerfMeterCaptureCoordinator absentCoordinator = new PerfMeterCaptureCoordinator((IPerfMeterCaptureBackendV2)absentRouter, new FakeCaptureScope());

			Assert.That(absentCoordinator.Request(new PerfMeterCaptureOptions(
				"preferred-absent",
				PerfMeterCaptureTool.RenderDoc,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativePreferred)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(generic.BeginCount, Is.EqualTo(1));
			Assert.That(absentCoordinator.Status.EffectiveBackendKind, Is.EqualTo(PerfMeterCaptureBackendKind.GenericUnity));
			Assert.That(absentCoordinator.Status.FallbackReason, Is.EqualTo(PerfMeterCaptureFallbackReasons.BackendUnavailable));

			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend
			{
				BeginSucceeds = false,
				BeginFailureCode = beginFailureCode,
				BeginFailurePhase = PerfMeterRenderDocCapturePhase.Failed
			};
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			FakeCaptureBackend beginFallbackGeneric = new FakeCaptureBackend();
			PerfMeterCaptureBackendRouter beginFallbackRouter = new PerfMeterCaptureBackendRouter(beginFallbackGeneric);
			PerfMeterCaptureCoordinator beginFallbackCoordinator = new PerfMeterCaptureCoordinator((IPerfMeterCaptureBackendV2)beginFallbackRouter, new FakeCaptureScope());

			Assert.That(beginFallbackCoordinator.Request(new PerfMeterCaptureOptions(
				"preferred-begin",
				PerfMeterCaptureTool.RenderDoc,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativePreferred)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(native.BeginCount, Is.EqualTo(1));
			Assert.That(beginFallbackGeneric.BeginCount, Is.EqualTo(1));
			Assert.That(beginFallbackCoordinator.Status.EffectiveBackendKind, Is.EqualTo(PerfMeterCaptureBackendKind.GenericUnity));
			Assert.That(beginFallbackCoordinator.Status.FallbackReason, Is.EqualTo(expectedFallbackReason));
			beginFallbackCoordinator.Tick();
			Assert.That(beginFallbackGeneric.EndCount, Is.EqualTo(1));
			Assert.That(native.ScheduleEndCount, Is.Zero);
			Assert.That(native.DiscardCount, Is.Zero);
		}

		[TestCase(PerfMeterNativeCaptureResultCodes.InternalError)]
		[TestCase(12)]
		public void NativePreferredDoesNotFallbackForDisallowedResult(int resultCode)
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend
			{
				BeginSucceeds = false,
				BeginFailureCode = resultCode,
				BeginFailurePhase = PerfMeterRenderDocCapturePhase.Failed
			};
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			FakeCaptureBackend generic = new FakeCaptureBackend();
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(generic),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions(
				"disallowed",
				PerfMeterCaptureTool.RenderDoc,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativePreferred)), Is.EqualTo(PerfMeterCaptureRequestResult.Failed));
			Assert.That(generic.BeginCount, Is.Zero);
			Assert.That(coordinator.Status.FallbackReason, Is.Empty);
		}

		[Test]
		public void NativePreferredNeverFallsBackAfterBeginUncertaintyOrSuccess()
		{
			FakeNativeCaptureBackend uncertain = new FakeNativeCaptureBackend
			{
				BeginSucceeds = false,
				BeginFailureCode = PerfMeterNativeCaptureResultCodes.NotLoaded,
				BeginFailurePhase = PerfMeterRenderDocCapturePhase.BeginExecuted
			};
			PerfMeterNativeCaptureBackendRegistry.Register(uncertain);
			FakeCaptureBackend generic = new FakeCaptureBackend();
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator((IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(generic), new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("uncertain", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativePreferred)), Is.EqualTo(PerfMeterCaptureRequestResult.Failed));
			Assert.That(generic.BeginCount, Is.Zero);

			PerfMeterNativeCaptureBackendRegistry.ResetForTests();
			FakeNativeCaptureBackend successful = new FakeNativeCaptureBackend();
			PerfMeterNativeCaptureBackendRegistry.Register(successful);
			generic = new FakeCaptureBackend();
			coordinator = new PerfMeterCaptureCoordinator((IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(generic), new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("successful", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativePreferred)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(generic.BeginCount, Is.Zero);
			Assert.That(coordinator.Status.EffectiveBackendKind, Is.EqualTo(PerfMeterCaptureBackendKind.RenderDocNative));
		}

		[TestCase(PerfMeterRenderDocCapturePhase.Failed)]
		[TestCase(PerfMeterRenderDocCapturePhase.Preflight)]
		public void NativePreferredDoesNotFallbackWhenBeginFailureRetainsResources(PerfMeterRenderDocCapturePhase phase)
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend
			{
				BeginSucceeds = false,
				BeginFailureCode = PerfMeterNativeCaptureResultCodes.NotLoaded,
				BeginFailurePhase = phase,
				BeginFailureHasActiveResources = true
			};
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			FakeCaptureBackend generic = new FakeCaptureBackend();
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(generic),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions(
				"uncertain-resources-" + phase,
				PerfMeterCaptureTool.RenderDoc,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativePreferred)), Is.EqualTo(PerfMeterCaptureRequestResult.Failed));
			Assert.That(generic.BeginCount, Is.Zero);
			Assert.That(native.DiscardCount, Is.EqualTo(1));
		}

		[Test]
		public void NativeEndRunsOnlyAtEndOfFrameAndOncePerGeneration()
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend { RequiresEndOfFrame = true };
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new FakeCaptureBackend()),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("eof", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			int generation = coordinator.Generation;
			Assert.That(coordinator.EndOfFramePending, Is.False);
			coordinator.Tick();
			Assert.That(native.ScheduleEndCount, Is.Zero);
			Assert.That(coordinator.EndOfFramePending, Is.True);
			Assert.That(coordinator.Status.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.EndScheduled));

			Assert.That(coordinator.TickAtEndOfFrame(generation), Is.True);
			Assert.That(native.ScheduleEndCount, Is.EqualTo(1));
			Assert.That(coordinator.TickAtEndOfFrame(generation), Is.False);
			Assert.That(native.ScheduleEndCount, Is.EqualTo(1));
			Assert.That(coordinator.EndOfFramePending, Is.False);
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Completed));

			coordinator.Reset();
			Assert.That(coordinator.TickAtEndOfFrame(generation), Is.False);
			Assert.That(native.ScheduleEndCount, Is.EqualTo(1));
		}

		[Test]
		public void NativeCancelUsesDiscardInsteadOfEnd()
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend { RequiresEndOfFrame = true };
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new FakeCaptureBackend()),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("cancel", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			coordinator.Tick();
			Assert.That(coordinator.Cancel("cancel"), Is.True);
			Assert.That(native.DiscardCount, Is.EqualTo(1));
			Assert.That(native.ScheduleEndCount, Is.Zero);
		}

		[Test]
		public void FailedCancelInvalidatesScheduledEndAndCanRetryDiscard()
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend { RequiresEndOfFrame = true, DiscardSucceeds = false };
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new FakeCaptureBackend()),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("discard-retry", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			int scheduledGeneration = coordinator.Generation;
			coordinator.Tick();
			Assert.That(coordinator.EndOfFramePending, Is.True);

			Assert.That(coordinator.Cancel("discard-retry"), Is.False);
			Assert.That(coordinator.HasActiveResources, Is.True);
			Assert.That(coordinator.EndOfFramePending, Is.False);
			Assert.That(coordinator.TickAtEndOfFrame(scheduledGeneration), Is.False);
			Assert.That(coordinator.TickAtEndOfFrame(coordinator.Generation), Is.False);
			Assert.That(native.ScheduleEndCount, Is.Zero);

			native.DiscardSucceeds = true;
			Assert.That(coordinator.Cancel("discard-retry"), Is.True);
			Assert.That(native.DiscardCount, Is.EqualTo(2));
			Assert.That(coordinator.HasActiveResources, Is.False);
			Assert.That(native.ScheduleEndCount, Is.Zero);
		}

		[Test]
		public void AcceptedDiscardCleanupKeepsTickingUntilResourcesClear()
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend
			{
				RequiresEndOfFrame = true,
				PendingAfterDiscard = true
			};
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new FakeCaptureBackend()),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("discard-pending", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(coordinator.Cancel("discard-pending"), Is.False);
			Assert.That(coordinator.HasPendingCompletion, Is.True);
			Assert.That(coordinator.HasActiveResources, Is.True);

			native.CompletePending = true;
			coordinator.Tick();

			Assert.That(native.TickCount, Is.EqualTo(1));
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Canceled));
			Assert.That(coordinator.HasPendingCompletion, Is.False);
			Assert.That(coordinator.HasActiveResources, Is.False);
		}

		[Test]
		public void AcceptedDiscardCleanupCannotHideOwnedScope()
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend
			{
				RequiresEndOfFrame = true,
				PendingAfterDiscard = true
			};
			FakeCaptureScope scope = new FakeCaptureScope { EndSucceeds = false };
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new FakeCaptureBackend()),
				scope);

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("discard-scope", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(coordinator.Cancel("discard-scope"), Is.False);

			native.CompletePending = true;
			coordinator.Tick();

			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(coordinator.ScopeActive, Is.True);
			Assert.That(coordinator.HasActiveResources, Is.True);
			Assert.That(native.DiscardCount, Is.EqualTo(1));

			scope.EndSucceeds = true;
			Assert.That(coordinator.Cancel("discard-scope"), Is.True);
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Canceled));
			Assert.That(coordinator.HasActiveResources, Is.False);
			Assert.That(native.DiscardCount, Is.EqualTo(1));
		}

		[Test]
		public void FailedAcceptedDiscardCleanupRemainsErrorAfterCancelRetry()
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend
			{
				RequiresEndOfFrame = true,
				PendingAfterDiscard = true
			};
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new FakeCaptureBackend()),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("discard-failed", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(coordinator.Cancel("discard-failed"), Is.False);

			native.CompletePending = true;
			native.FailPendingCompletion = true;
			Assert.That(coordinator.Cancel("discard-failed"), Is.False);

			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(coordinator.Status.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.Failed));
			Assert.That(coordinator.HasActiveResources, Is.False);
			Assert.That(native.DiscardCount, Is.EqualTo(1));
		}

		[Test]
		public void FailedDiscardWithoutResourcesRemainsError()
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend
			{
				DiscardSucceeds = false,
				DiscardFailureReleasesResources = true
			};
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new FakeCaptureBackend()),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("discard-terminal-error", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(coordinator.Cancel("discard-terminal-error"), Is.False);

			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(coordinator.Status.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.Failed));
			Assert.That(coordinator.HasActiveResources, Is.False);
			Assert.That(native.DiscardCount, Is.EqualTo(1));
		}

		[Test]
		public void NativePendingCompletionRetainsCoordinatorResourcesUntilTickCompletes()
		{
			FakeNativeCaptureBackend native = new FakeNativeCaptureBackend { RequiresEndOfFrame = true, PendingAfterEnd = true };
			PerfMeterNativeCaptureBackendRegistry.Register(native);
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new FakeCaptureBackend()),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("pending", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			coordinator.Tick();
			Assert.That(coordinator.TickAtEndOfFrame(coordinator.Generation), Is.True);
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Completed));
			Assert.That(coordinator.HasActiveResources, Is.True);
			Assert.That(coordinator.HasPendingCompletion, Is.True);

			native.CompletePending = true;
			coordinator.Tick();
			Assert.That(coordinator.HasActiveResources, Is.False);
			Assert.That(coordinator.HasPendingCompletion, Is.False);
		}

		[Test]
		public void CoordinatorRunsDeterministicPreCaptureAndPostRollFrames()
		{
			FakeCaptureBackend backend = new FakeCaptureBackend();
			FakeCaptureScope scope = new FakeCaptureScope();
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(backend, scope);

			PerfMeterCaptureRequestResult result = coordinator.Request(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc, 2, 2, 2));

			Assert.That(result, Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.PreRoll));
			coordinator.Tick();
			Assert.That(coordinator.Status.CompletedPreRollFrames, Is.EqualTo(1));
			Assert.That(backend.BeginCount, Is.Zero);

			coordinator.Tick();
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Capturing));
			Assert.That(coordinator.Status.CompletedPreRollFrames, Is.EqualTo(2));
			Assert.That(backend.BeginCount, Is.EqualTo(1));
			Assert.That(scope.ActiveCaptureId, Is.EqualTo("capture-1"));
			Assert.That(PerfMeterProfilerInstrumentation.CaptureState, Is.EqualTo((int)PerfMeterCaptureState.Capturing));

			coordinator.Tick();
			Assert.That(coordinator.Status.CompletedCaptureFrames, Is.EqualTo(1));
			coordinator.Tick();
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.PostRoll));
			Assert.That(coordinator.Status.CompletedCaptureFrames, Is.EqualTo(2));
			Assert.That(backend.EndCount, Is.EqualTo(1));
			Assert.That(scope.ActiveCaptureId, Is.Empty);

			coordinator.Tick();
			Assert.That(coordinator.Status.CompletedPostRollFrames, Is.EqualTo(1));
			coordinator.Tick();
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Completed));
			Assert.That(coordinator.Status.CompletedPostRollFrames, Is.EqualTo(2));
			Assert.That(coordinator.Status.IsActive, Is.False);
		}

		[Test]
		public void CoordinatorRejectsOverlapAndTreatsSameIdAsIdempotent()
		{
			FakeCaptureBackend backend = new FakeCaptureBackend();
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(backend, new FakeCaptureScope());
			PerfMeterCaptureOptions options = new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc, 1);

			Assert.That(coordinator.Request(options), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Capturing));
			Assert.That(coordinator.Request(options), Is.EqualTo(PerfMeterCaptureRequestResult.AlreadyActive));
			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("capture-2", PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.RejectedOverlap));
			Assert.That(backend.BeginCount, Is.EqualTo(1));
			Assert.That(coordinator.Cancel("capture-2"), Is.False);
			Assert.That(coordinator.Cancel("capture-1"), Is.True);
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Canceled));
			Assert.That(backend.EndCount, Is.EqualTo(1));
		}

		[Test]
		public void UnavailableBackendDoesNotStartCapture()
		{
			FakeCaptureBackend backend = new FakeCaptureBackend
			{
				Capability = new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Unavailable, "External tool is not attached.")
			};
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(backend, new FakeCaptureScope());

			PerfMeterCaptureRequestResult result = coordinator.Request(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc));

			Assert.That(result, Is.EqualTo(PerfMeterCaptureRequestResult.Unavailable));
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Unavailable));
			Assert.That(coordinator.Status.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(coordinator.Status.Warning, Does.Contain("not attached"));
			Assert.That(backend.BeginCount, Is.Zero);
		}

		[Test]
		public void BeginFailureReleasesAlertScopeAndReportsError()
		{
			FakeCaptureBackend backend = new FakeCaptureBackend { BeginSucceeds = false };
			FakeCaptureScope scope = new FakeCaptureScope();
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(backend, scope);

			PerfMeterCaptureRequestResult result = coordinator.Request(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc));

			Assert.That(result, Is.EqualTo(PerfMeterCaptureRequestResult.Failed));
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(coordinator.Status.Warning, Does.Contain("begin failed"));
			Assert.That(scope.ActiveCaptureId, Is.Empty);
			Assert.That(scope.EndCount, Is.EqualTo(1));
		}

		[Test]
		public void ScopeExceptionIsContainedAndPartiallyAcquiredScopeIsReleased()
		{
			FakeCaptureBackend backend = new FakeCaptureBackend();
			FakeCaptureScope scope = new FakeCaptureScope { ThrowOnBegin = true };
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(backend, scope);

			PerfMeterCaptureRequestResult result = default;
			Assert.DoesNotThrow(() => result = coordinator.Request(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc)));

			Assert.That(result, Is.EqualTo(PerfMeterCaptureRequestResult.Failed));
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(coordinator.Status.Warning, Does.Contain("scope begin failed"));
			Assert.That(scope.ActiveCaptureId, Is.Empty);
			Assert.That(scope.EndCount, Is.EqualTo(1));
			Assert.That(backend.BeginCount, Is.Zero);
		}

		[Test]
		public void EndFailureCanBeRetriedByCancel()
		{
			FakeCaptureBackend backend = new FakeCaptureBackend { EndSucceeds = false };
			FakeCaptureScope scope = new FakeCaptureScope();
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(backend, scope);
			coordinator.Request(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc));

			coordinator.Tick();
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(coordinator.Status.Warning, Does.Contain("end failed"));
			Assert.That(scope.ActiveCaptureId, Is.Empty);

			backend.EndSucceeds = true;
			Assert.That(coordinator.Cancel("capture-1"), Is.True);
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Canceled));
			Assert.That(backend.EndCount, Is.EqualTo(2));
		}

		[Test]
		public void ResetEndsActiveCaptureAndReturnsIdleState()
		{
			FakeCaptureBackend backend = new FakeCaptureBackend();
			FakeCaptureScope scope = new FakeCaptureScope();
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(backend, scope);
			coordinator.Request(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc, 3));

			Assert.That(coordinator.Reset(), Is.True);

			Assert.That(backend.EndCount, Is.EqualTo(1));
			Assert.That(scope.ActiveCaptureId, Is.Empty);
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Idle));
			Assert.That(coordinator.Status.CaptureId, Is.Empty);
		}

		[Test]
		public void ResetClearsNativeStatusMetadata()
		{
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(
				(IPerfMeterCaptureBackendV2)new PerfMeterCaptureBackendRouter(new FakeCaptureBackend()),
				new FakeCaptureScope());

			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("reset-status", PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.Unavailable));
			Assert.That(coordinator.Status.EffectiveBackendKind, Is.EqualTo(PerfMeterCaptureBackendKind.RenderDocNative));
			Assert.That(coordinator.Reset(), Is.True);

			PerfMeterCaptureStatusSnapshot status = coordinator.Status;
			Assert.That(status.State, Is.EqualTo(PerfMeterCaptureState.Idle));
			Assert.That(status.EffectiveBackendKind, Is.EqualTo(PerfMeterCaptureBackendKind.GenericUnity));
			Assert.That(status.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.None));
			Assert.That(status.NativeResultCode, Is.EqualTo(-1));
			Assert.That(status.FallbackReason, Is.Empty);
		}

		[Test]
		public void FailedResetPreservesBackendOwnershipForRetry()
		{
			FakeCaptureBackend backend = new FakeCaptureBackend { EndSucceeds = false };
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(backend, new FakeCaptureScope());
			coordinator.Request(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc, 3));

			Assert.That(coordinator.Reset(), Is.False);
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(coordinator.Status.CaptureId, Is.EqualTo("capture-1"));
			Assert.That(coordinator.HasActiveResources, Is.True);
			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("capture-2", PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.RejectedOverlap));

			backend.EndSucceeds = true;
			Assert.That(coordinator.Reset(), Is.True);
			Assert.That(backend.EndCount, Is.EqualTo(2));
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Idle));
			Assert.That(coordinator.HasActiveResources, Is.False);
		}

		[TestCase(PerfMeterCaptureTool.RenderDoc, RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D11, true)]
		[TestCase(PerfMeterCaptureTool.RenderDoc, RuntimePlatform.WindowsPlayer, GraphicsDeviceType.Direct3D12, true)]
		[TestCase(PerfMeterCaptureTool.RenderDoc, RuntimePlatform.LinuxEditor, GraphicsDeviceType.Vulkan, true)]
		[TestCase(PerfMeterCaptureTool.RenderDoc, RuntimePlatform.OSXEditor, GraphicsDeviceType.Metal, false)]
		[TestCase(PerfMeterCaptureTool.Pix, RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D12, true)]
		[TestCase(PerfMeterCaptureTool.Pix, RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D11, false)]
		[TestCase(PerfMeterCaptureTool.Pix, RuntimePlatform.LinuxEditor, GraphicsDeviceType.Vulkan, false)]
		public void ExternalGpuProfilerCapabilityUsesExplicitPlatformMatrix(PerfMeterCaptureTool tool, RuntimePlatform platform, GraphicsDeviceType graphicsDeviceType, bool expectedAvailable)
		{
			PerfMeterCaptureBackendCapability capability = PerfMeterExternalGpuProfilerBackend.EvaluateCapability(tool, platform, graphicsDeviceType, true, true);

			Assert.That(capability.Availability == PerfMeterAvailability.Available, Is.EqualTo(expectedAvailable));
		}

		[Test]
		public void ExternalGpuProfilerRequiresDevelopmentBuildAndAttachment()
		{
			PerfMeterCaptureBackendCapability release = PerfMeterExternalGpuProfilerBackend.EvaluateCapability(PerfMeterCaptureTool.RenderDoc, RuntimePlatform.WindowsPlayer, GraphicsDeviceType.Direct3D12, false, true);
			PerfMeterCaptureBackendCapability detached = PerfMeterExternalGpuProfilerBackend.EvaluateCapability(PerfMeterCaptureTool.RenderDoc, RuntimePlatform.WindowsPlayer, GraphicsDeviceType.Direct3D12, true, false);

			Assert.That(release.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(release.Warning, Does.Contain("Development builds"));
			Assert.That(detached.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(detached.Warning, Does.Contain("not attached"));
		}

		[Test]
		public void PublicApiReportsNotRunningAndEnforcesAlertScopeOwnership()
		{
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Idle));
			Assert.That(PerformanceMeter.GetCaptureStatus().Availability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(PerformanceMeter.RequestCapture(default), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));

			PerformanceMeter.EnsureRunning();
			FakeCaptureBackend backend = new FakeCaptureBackend();
			PerfMeterRuntime.Instance.SetCaptureBackendForTests(backend);
			Assert.That(PerformanceMeter.BeginAlertCapture("legacy"), Is.True);

			PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc));

			Assert.That(result, Is.EqualTo(PerfMeterCaptureRequestResult.RejectedOverlap));
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Idle));
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.EqualTo("legacy"));
			Assert.That(backend.BeginCount, Is.Zero);
			Assert.That(PerformanceMeter.EndAlertCapture("legacy"), Is.True);

			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc, 3)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(PerformanceMeter.BeginAlertCapture("capture-1"), Is.False);
			Assert.That(PerformanceMeter.BeginAlertCapture("other"), Is.False);
			Assert.That(PerformanceMeter.EndAlertCapture("capture-1"), Is.False);
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.EqualTo("capture-1"));
			Assert.That(PerformanceMeter.CancelCapture("capture-1"), Is.True);
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.Empty);
		}

		[Test]
		public void LegacyAlertScopeCannotInterfereDuringPreRollOrPostRoll()
		{
			PerformanceMeter.EnsureRunning();
			PerfMeterRuntime.Instance.SetCaptureBackendForTests(new FakeCaptureBackend());
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc, 1, 1, 1)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));

			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.PreRoll));
			Assert.That(PerformanceMeter.BeginAlertCapture("legacy"), Is.False);
			Assert.That(PerformanceMeter.EndAlertCapture("capture-1"), Is.False);
			PerfMeterRuntime.Instance.TickCaptureForTests();
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Capturing));

			PerfMeterRuntime.Instance.TickCaptureForTests();
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.PostRoll));
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.Empty);
			Assert.That(PerformanceMeter.BeginAlertCapture("legacy"), Is.False);
			Assert.That(PerformanceMeter.EndAlertCapture("capture-1"), Is.False);
			Assert.That(PerformanceMeter.CancelCapture("capture-1"), Is.True);
		}

		private sealed class FakeNativeCaptureBackend : IPerfMeterCaptureBackendV2
		{
			private PerfMeterCaptureBackendV2Snapshot _snapshot;

			internal PerfMeterAvailability CapabilityAvailability { get; set; } = PerfMeterAvailability.Available;
			internal string CapabilityWarning { get; set; } = string.Empty;
			internal bool BeginSucceeds { get; set; } = true;
			internal int BeginFailureCode { get; set; } = PerfMeterNativeCaptureResultCodes.NotLoaded;
			internal PerfMeterRenderDocCapturePhase BeginFailurePhase { get; set; } = PerfMeterRenderDocCapturePhase.Failed;
			internal bool BeginFailureHasActiveResources { get; set; }
			internal bool BeginFailureHasPendingCompletion { get; set; }
			internal bool RequiresEndOfFrame { get; set; }
			internal bool PendingAfterEnd { get; set; }
			internal bool PendingAfterDiscard { get; set; }
			internal bool CompletePending { get; set; }
			internal bool FailPendingCompletion { get; set; }
			internal bool EndSucceeds { get; set; } = true;
			internal bool ScheduleEndSucceeds
			{
				get => EndSucceeds;
				set => EndSucceeds = value;
			}
			internal bool DiscardSucceeds { get; set; } = true;
			internal bool DiscardFailureReleasesResources { get; set; }
			internal int BeginCount { get; private set; }
			internal int ScheduleEndCount { get; private set; }
			internal int DiscardCount { get; private set; }
			internal int TickCount { get; private set; }

			internal PerfMeterCaptureBackendV2Snapshot Snapshot => _snapshot;
			PerfMeterCaptureBackendV2Snapshot IPerfMeterCaptureBackendV2.Snapshot => Snapshot;

			public PerfMeterCaptureBackendV2Snapshot GetCapability(PerfMeterCaptureOptions options)
			{
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					CapabilityAvailability,
					CapabilityWarning,
					PerfMeterCaptureBackendKind.RenderDocNative,
					PerfMeterRenderDocCapturePhase.Preflight,
					PerfMeterNativeCaptureResultCodes.Ok,
					string.Empty,
					RequiresEndOfFrame,
					false,
					false);
				return _snapshot;
			}

			public bool TryBegin(PerfMeterCaptureOptions options, out string error)
			{
				BeginCount++;
				if (!BeginSucceeds)
				{
					error = "begin failed";
					_snapshot = new PerfMeterCaptureBackendV2Snapshot(
						PerfMeterAvailability.Unavailable,
						error,
						PerfMeterCaptureBackendKind.RenderDocNative,
						BeginFailurePhase,
						BeginFailureCode,
						string.Empty,
						RequiresEndOfFrame,
						BeginFailureHasPendingCompletion,
						BeginFailureHasActiveResources);
					return false;
				}

				error = string.Empty;
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					PerfMeterAvailability.Available,
					string.Empty,
					PerfMeterCaptureBackendKind.RenderDocNative,
					PerfMeterRenderDocCapturePhase.BeginExecuted,
					PerfMeterNativeCaptureResultCodes.Ok,
					string.Empty,
					RequiresEndOfFrame,
					false,
					true);
				return true;
			}

			public bool ScheduleEnd(out string error)
			{
				ScheduleEndCount++;
				if (!EndSucceeds)
				{
					error = "end failed";
					return false;
				}

				error = string.Empty;
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					PerfMeterAvailability.Available,
					string.Empty,
					PerfMeterCaptureBackendKind.RenderDocNative,
					PendingAfterEnd ? PerfMeterRenderDocCapturePhase.AwaitingArtifact : PerfMeterRenderDocCapturePhase.EndExecuted,
					PerfMeterNativeCaptureResultCodes.Ok,
					string.Empty,
					RequiresEndOfFrame,
					PendingAfterEnd,
					PendingAfterEnd);
				return true;
			}

			public bool TryDiscard(out string error)
			{
				DiscardCount++;
				if (!DiscardSucceeds)
				{
					error = "discard failed";
					if (DiscardFailureReleasesResources)
					{
						_snapshot = new PerfMeterCaptureBackendV2Snapshot(
							PerfMeterAvailability.Unavailable,
							error,
							PerfMeterCaptureBackendKind.RenderDocNative,
							PerfMeterRenderDocCapturePhase.Failed,
							PerfMeterNativeCaptureResultCodes.InternalError,
							_snapshot.FallbackReason,
							RequiresEndOfFrame,
							false,
							false);
					}
					return false;
				}

				error = string.Empty;
				_snapshot = new PerfMeterCaptureBackendV2Snapshot(
					PerfMeterAvailability.Available,
					string.Empty,
					PerfMeterCaptureBackendKind.RenderDocNative,
					PendingAfterDiscard && !CompletePending
						? PerfMeterRenderDocCapturePhase.FinalizingArtifact
						: PerfMeterRenderDocCapturePhase.Completed,
					_snapshot.NativeResultCode,
					_snapshot.FallbackReason,
					RequiresEndOfFrame,
					PendingAfterDiscard && !CompletePending,
					PendingAfterDiscard && !CompletePending);
				return true;
			}

			public void Tick()
			{
				TickCount++;
				if (CompletePending && _snapshot.HasPendingCompletion)
				{
					_snapshot = new PerfMeterCaptureBackendV2Snapshot(
						FailPendingCompletion ? PerfMeterAvailability.Unavailable : PerfMeterAvailability.Available,
						FailPendingCompletion ? "cleanup failed" : string.Empty,
						PerfMeterCaptureBackendKind.RenderDocNative,
						FailPendingCompletion ? PerfMeterRenderDocCapturePhase.Failed : PerfMeterRenderDocCapturePhase.Completed,
						FailPendingCompletion ? PerfMeterNativeCaptureResultCodes.InternalError : PerfMeterNativeCaptureResultCodes.Ok,
						_snapshot.FallbackReason,
						RequiresEndOfFrame,
						false,
						false);
				}
			}
		}

		private sealed class FakeCaptureBackend : IPerfMeterCaptureBackend
		{
			internal PerfMeterCaptureBackendCapability Capability { get; set; } = new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Available, string.Empty);
			internal bool BeginSucceeds { get; set; } = true;
			internal bool EndSucceeds { get; set; } = true;
			internal int BeginCount { get; private set; }
			internal int EndCount { get; private set; }

			public PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool)
			{
				return Capability;
			}

			public bool TryBegin(PerfMeterCaptureTool tool, out string error)
			{
				BeginCount++;
				error = BeginSucceeds ? string.Empty : "begin failed";
				return BeginSucceeds;
			}

			public bool TryEnd(out string error)
			{
				EndCount++;
				error = EndSucceeds ? string.Empty : "end failed";
				return EndSucceeds;
			}
		}

		private sealed class FakeCaptureScope : IPerfMeterCaptureScope
		{
			internal string ActiveCaptureId { get; private set; } = string.Empty;
			internal int EndCount { get; private set; }
			internal bool ThrowOnBegin { get; set; }
			internal bool EndSucceeds { get; set; } = true;

			public bool TryBegin(string captureId)
			{
				if (!string.IsNullOrEmpty(ActiveCaptureId))
				{
					return false;
				}

				ActiveCaptureId = captureId;
				if (ThrowOnBegin)
				{
					throw new System.InvalidOperationException("scope begin failed");
				}

				return true;
			}

			public bool TryEnd(string captureId)
			{
				EndCount++;
				if (!string.Equals(ActiveCaptureId, captureId, System.StringComparison.Ordinal))
				{
					return false;
				}
				if (!EndSucceeds)
				{
					return false;
				}

				ActiveCaptureId = string.Empty;
				return true;
			}
		}
	}
}
