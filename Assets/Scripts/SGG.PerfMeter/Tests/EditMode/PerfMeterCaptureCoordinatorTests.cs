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
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterProfilerInstrumentation.Reset();
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
		public void FailedResetPreservesBackendOwnershipForRetry()
		{
			FakeCaptureBackend backend = new FakeCaptureBackend { EndSucceeds = false };
			PerfMeterCaptureCoordinator coordinator = new PerfMeterCaptureCoordinator(backend, new FakeCaptureScope());
			coordinator.Request(new PerfMeterCaptureOptions("capture-1", PerfMeterCaptureTool.RenderDoc, 3));

			Assert.That(coordinator.Reset(), Is.False);
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(coordinator.Status.CaptureId, Is.EqualTo("capture-1"));
			Assert.That(coordinator.Request(new PerfMeterCaptureOptions("capture-2", PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.RejectedOverlap));

			backend.EndSucceeds = true;
			Assert.That(coordinator.Reset(), Is.True);
			Assert.That(backend.EndCount, Is.EqualTo(2));
			Assert.That(coordinator.Status.State, Is.EqualTo(PerfMeterCaptureState.Idle));
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

				ActiveCaptureId = string.Empty;
				return true;
			}
		}
	}
}
