using System;
using System.Diagnostics;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterSelfObservabilityTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterSelfObservability.Stop();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterSelfObservability.Stop();
		}

		[Test]
		public void StoppedSnapshotIsNotInitializedAndGpuTimingIsUnavailable()
		{
			PerfMeterSelfOverheadSnapshot snapshot = PerformanceMeter.GetSelfOverhead();

			Assert.That(snapshot.State, Is.EqualTo(PerfMeterSelfOverheadState.NotInitialized));
			Assert.That(snapshot.CpuTimingAvailable, Is.False);
			Assert.That(snapshot.GpuTimingAvailability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(snapshot.HasBudgetViolation, Is.False);
			Assert.That(PerformanceMeter.GetStatus().SelfOverhead.State, Is.EqualTo(PerfMeterSelfOverheadState.NotInitialized));
			Assert.That(
				PerformanceMeter.GetSelfOverheadWindow((PerfMeterSelfOverheadWindowKind)999).Kind,
				Is.EqualTo(PerfMeterSelfOverheadWindowKind.None));
		}

		[Test]
		public void RenderIntegrationSupportMatchesActivePipeline()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfOverheadSnapshot urp = PerfMeterSelfObservability.GetSnapshot();

			Assert.That(urp.UrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.NotMeasured));
			Assert.That(urp.HdrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.Unsupported));

			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.HighDefinition);
			PerfMeterSelfOverheadSnapshot hdrp = PerfMeterSelfObservability.GetSnapshot();

			Assert.That(hdrp.UrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.Unsupported));
			Assert.That(hdrp.HdrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.NotMeasured));
		}

		[Test]
		public void CompletedWindowPublishesAveragesAndBudgetState()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			for (int frame = 0; frame < PerfMeterSelfObservability.WindowSizeFrames; frame++)
			{
				PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.Collector, frame, 100000L, 0L);
			}

			PerfMeterSelfObservability.RecordSampleForTesting(
				PerfMeterSelfOverheadComponent.Collector,
				PerfMeterSelfObservability.WindowSizeFrames,
				200000L,
				0L);
			PerfMeterSelfOverheadSnapshot snapshot = PerfMeterSelfObservability.GetSnapshotForTesting(PerfMeterSelfObservability.WindowSizeFrames);
			PerfMeterSelfOverheadComponentSnapshot collector = snapshot.Collector;

			Assert.That(snapshot.State, Is.EqualTo(PerfMeterSelfOverheadState.Ready));
			Assert.That(collector.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.Ready));
			Assert.That(collector.WindowFrameCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames));
			Assert.That(collector.InvocationCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames));
			Assert.That(collector.AverageCpuTimeMs, Is.EqualTo(0.1d).Within(0.000001d));
			Assert.That(collector.MaxCpuTimeMs, Is.EqualTo(0.1d).Within(0.000001d));
			Assert.That(collector.AllocatedBytes, Is.Zero);
			Assert.That(collector.CpuBudgetState, Is.EqualTo(PerfMeterSelfOverheadBudgetState.WithinBudget));
			Assert.That(collector.AllocationBudgetState, Is.EqualTo(PerfMeterSelfOverheadBudgetState.WithinBudget));
		}

		[Test]
		public void CurrentWindowReportsCpuAndAllocationBudgetViolations()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.RecordSampleForTesting(
				PerfMeterSelfOverheadComponent.Collector,
				0,
				PerfMeterSelfObservability.CollectorCpuBudgetNanoseconds + 1L,
				1L);

			PerfMeterSelfOverheadSnapshot snapshot = PerfMeterSelfObservability.GetSnapshotForTesting(0);

			Assert.That(snapshot.State, Is.EqualTo(PerfMeterSelfOverheadState.Collecting));
			Assert.That(snapshot.Collector.CpuBudgetState, Is.EqualTo(PerfMeterSelfOverheadBudgetState.Exceeded));
			Assert.That(snapshot.Collector.AllocationBudgetState, Is.EqualTo(PerfMeterSelfOverheadBudgetState.Exceeded));
			Assert.That(snapshot.HasBudgetViolation, Is.True);
		}

		[Test]
		public void SparseMeasurementsPublishFixedLengthCompletedWindow()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.Collector, 0, 100000L, 0L);

			PerfMeterSelfOverheadSnapshot elapsedSnapshot = PerfMeterSelfObservability.GetSnapshotForTesting(120);
			Assert.That(elapsedSnapshot.Collector.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.Ready));
			Assert.That(elapsedSnapshot.Collector.WindowFrameCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames));
			Assert.That(elapsedSnapshot.Collector.InvocationCount, Is.EqualTo(1));

			PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.Collector, 240, 200000L, 0L);
			PerfMeterSelfOverheadSnapshot sparseSnapshot = PerfMeterSelfObservability.GetSnapshotForTesting(240);
			Assert.That(sparseSnapshot.Collector.WindowFrameCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames));
			Assert.That(sparseSnapshot.Collector.InvocationCount, Is.EqualTo(1));
			Assert.That(sparseSnapshot.Collector.AverageCpuTimeMs, Is.EqualTo(0.1d).Within(0.000001d));
		}

		[Test]
		public void MultipleInvocationsPerFrameUsePerInvocationAverages()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			for (int frame = 0; frame < PerfMeterSelfObservability.WindowSizeFrames; frame++)
			{
				PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.Collector, frame, 100000L, 0L);
				PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.Collector, frame, 200000L, 0L);
			}

			PerfMeterSelfOverheadComponentSnapshot collector = PerfMeterSelfObservability
				.GetSnapshotForTesting(PerfMeterSelfObservability.WindowSizeFrames)
				.Collector;
			Assert.That(collector.WindowFrameCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames));
			Assert.That(collector.InvocationCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames * 2));
			Assert.That(collector.AverageCpuTimeMs, Is.EqualTo(0.15d).Within(0.000001d));
			Assert.That(collector.MaxCpuTimeMs, Is.EqualTo(0.2d).Within(0.000001d));
		}

		[Test]
		public void MeasurementScopeDoesNotAllocateOnCurrentThread()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			using (PerfMeterSelfObservability.Measure(PerfMeterSelfOverheadComponent.Collector))
			{
			}

			long before = GC.GetAllocatedBytesForCurrentThread();
			for (int i = 0; i < 1024; i++)
			{
				using (PerfMeterSelfObservability.Measure(PerfMeterSelfOverheadComponent.Collector))
				{
				}
			}
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

			Assert.That(allocatedBytes, Is.Zero);
		}

		[Test]
		public void ResetComponentClearsOnlySelectedMeasurements()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.Collector, 1, 100000L, 0L);
			PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.Overlay, 1, 100000L, 32L);

			PerfMeterSelfObservability.ResetComponent(PerfMeterSelfOverheadComponent.Overlay);
			PerfMeterSelfOverheadSnapshot snapshot = PerfMeterSelfObservability.GetSnapshotForTesting(1);

			Assert.That(snapshot.Collector.InvocationCount, Is.EqualTo(1));
			Assert.That(snapshot.Overlay.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.NotMeasured));
			Assert.That(snapshot.Overlay.InvocationCount, Is.Zero);
		}

		[Test]
		public void StopwatchFrequencyConvertsToOneSecond()
		{
			Assert.That(PerfMeterSelfObservability.StopwatchTicksToNanoseconds(Stopwatch.Frequency), Is.EqualTo(1000000000L));
		}

		[Test]
		public void RuntimeStatusMcpPublishesAdditiveSelfOverheadObject()
		{
			string stoppedJson = PerfMeterMcpCommands.RuntimeStatus();
			RuntimeStatusPayload stoppedPayload = JsonUtility.FromJson<RuntimeStatusPayload>(stoppedJson);
			Assert.That(stoppedPayload.self_overhead.state, Is.EqualTo("NotInitialized"));
			Assert.That(stoppedPayload.self_overhead.gpu_timing_availability, Is.EqualTo("Unavailable"));

			PerformanceMeter.EnsureRunning();
			PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.Overlay, Time.frameCount, 100000L, 0L);
			string runningJson = PerfMeterMcpCommands.RuntimeStatus();
			RuntimeStatusPayload runningPayload = JsonUtility.FromJson<RuntimeStatusPayload>(runningJson);
			Assert.That(runningPayload.self_overhead.state, Is.EqualTo("Collecting"));
			Assert.That(runningPayload.self_overhead.collector.component, Is.EqualTo("Collector"));
			Assert.That(runningPayload.self_overhead.collector.cpu_budget_state, Is.EqualTo("NotEvaluated"));
			Assert.That(runningPayload.self_overhead.overlay.invocation_count, Is.EqualTo(1));
			Assert.That(runningPayload.self_overhead.urp_render_integration.inactive_reason, Is.EqualTo("PlayModeInactive"));
			Assert.That(runningPayload.self_overhead.urp_render_integration.gpu_attribution_availability, Is.EqualTo("Unavailable"));
			Assert.That(PerformanceMeter.GetStatus().SelfOverhead.Overlay.InvocationCount, Is.EqualTo(1));
		}

		[Test]
		public void MissingRendererFeatureReturnsTypedCaptureBoundReason()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.BeginBoundWindowForTesting(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-missing",
				10,
				PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.ReportUrpFeatureState(
				PerfMeterUrpFeatureInstallationState.NotInstalled,
				PerfMeterUrpFeatureEnabledState.Unknown,
				false,
				10,
				"PC_Renderer",
				"UniversalRendererData");

			PerfMeterSelfOverheadWindowSnapshot window = PerfMeterSelfObservability.EndBoundWindow(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-missing",
				30);

			Assert.That(window.Kind, Is.EqualTo(PerfMeterSelfOverheadWindowKind.Capture));
			Assert.That(window.Identity, Is.EqualTo("capture-missing"));
			Assert.That(window.Epoch, Is.GreaterThan(0L));
			Assert.That(window.WindowStartFrame, Is.EqualTo(10));
			Assert.That(window.WindowEndFrame, Is.EqualTo(30));
			Assert.That(window.WindowComplete, Is.True);
			Assert.That(window.RenderPipeline.Kind, Is.EqualTo(PerfMeterRenderPipelineKind.Universal));
			Assert.That(window.RendererName, Is.EqualTo("PC_Renderer"));
			Assert.That(window.FeatureInstallation, Is.EqualTo(PerfMeterUrpFeatureInstallationState.NotInstalled));
			Assert.That(window.UrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.NotMeasured));
			Assert.That(window.UrpRenderIntegration.InactiveReason, Is.EqualTo(PerfMeterSelfOverheadInactiveReason.RendererFeatureNotInstalled));
			Assert.That(window.UrpRenderIntegration.GpuAttributionAvailability, Is.EqualTo(PerfMeterAvailability.Unavailable));
		}

		[Test]
		public void InstalledDormantFeatureReturnsPassNotEnqueued()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.BeginBoundWindowForTesting(
				PerfMeterSelfOverheadWindowKind.Session,
				"session-dormant",
				100,
				PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.ReportUrpFeatureState(
				PerfMeterUrpFeatureInstallationState.Installed,
				PerfMeterUrpFeatureEnabledState.Enabled,
				false,
				100,
				"ForwardRenderer",
				"UnityEngine.Rendering.Universal.UniversalRenderer");

			PerfMeterSelfOverheadWindowSnapshot window = PerfMeterSelfObservability.EndBoundWindow(
				PerfMeterSelfOverheadWindowKind.Session,
				"session-dormant",
				110);

			Assert.That(window.FeatureInstallation, Is.EqualTo(PerfMeterUrpFeatureInstallationState.Installed));
			Assert.That(window.FeatureEnabled, Is.EqualTo(PerfMeterUrpFeatureEnabledState.Enabled));
			Assert.That(window.EnqueueCount, Is.Zero);
			Assert.That(window.UrpRenderIntegration.InactiveReason, Is.EqualTo(PerfMeterSelfOverheadInactiveReason.PassNotEnqueued));
		}

		[TestCase(PerfMeterUrpFeatureEnabledState.Disabled, false, PerfMeterSelfOverheadInactiveReason.RendererFeatureDisabled)]
		[TestCase(PerfMeterUrpFeatureEnabledState.Enabled, true, PerfMeterSelfOverheadInactiveReason.NoCameraCallbackObserved)]
		public void InstalledInactiveFeatureUsesObservedTypedReason(
			PerfMeterUrpFeatureEnabledState enabled,
			bool enqueued,
			PerfMeterSelfOverheadInactiveReason expectedReason)
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.BeginBoundWindowForTesting(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-inactive",
				50,
				PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.ReportUrpFeatureState(
				PerfMeterUrpFeatureInstallationState.Installed,
				enabled,
				enqueued,
				50,
				"ForwardRenderer",
				"UnityEngine.Rendering.Universal.UniversalRenderer");

			PerfMeterSelfOverheadWindowSnapshot window = PerfMeterSelfObservability.EndBoundWindow(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-inactive",
				60);

			Assert.That(window.UrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.NotMeasured));
			Assert.That(window.UrpRenderIntegration.InactiveReason, Is.EqualTo(expectedReason));
		}

		[Test]
		public void ActiveMultiCameraWindowPublishesContainedCallbackAndInvocationBounds()
		{
			const int firstFrame = 200;
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.BeginBoundWindowForTesting(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-active",
				firstFrame,
				PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.ReportUrpFeatureState(
				PerfMeterUrpFeatureInstallationState.Installed,
				PerfMeterUrpFeatureEnabledState.Enabled,
				false,
				firstFrame,
				"ForwardRenderer",
				"UnityEngine.Rendering.Universal.UniversalRenderer");
			PerfMeterSelfOverheadWindowSnapshot notMeasured = PerfMeterSelfObservability.GetBoundWindowSnapshot(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-active",
				firstFrame);
			Assert.That(notMeasured.UrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.NotMeasured));
			Assert.That(notMeasured.UrpRenderIntegration.InactiveReason, Is.EqualTo(PerfMeterSelfOverheadInactiveReason.PassNotEnqueued));
			for (int frame = firstFrame; frame < firstFrame + PerfMeterSelfObservability.WindowSizeFrames; frame++)
			{
				PerfMeterSelfObservability.ReportUrpFeatureState(
					PerfMeterUrpFeatureInstallationState.Installed,
					PerfMeterUrpFeatureEnabledState.Enabled,
					true,
					frame,
					"ForwardRenderer",
					"UnityEngine.Rendering.Universal.UniversalRenderer");
				PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.UrpRenderIntegration, frame, 100000L, 0L);
				PerfMeterSelfObservability.ReportUrpFeatureState(
					PerfMeterUrpFeatureInstallationState.Installed,
					PerfMeterUrpFeatureEnabledState.Enabled,
					true,
					frame,
					"ForwardRenderer",
					"UnityEngine.Rendering.Universal.UniversalRenderer");
				PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.UrpRenderIntegration, frame, 200000L, 0L);
				if (frame == firstFrame)
				{
					PerfMeterSelfOverheadWindowSnapshot collecting = PerfMeterSelfObservability.GetBoundWindowSnapshot(
						PerfMeterSelfOverheadWindowKind.Capture,
						"capture-active",
						frame);
					Assert.That(collecting.UrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.Collecting));
					Assert.That(collecting.UrpRenderIntegration.InactiveReason, Is.EqualTo(PerfMeterSelfOverheadInactiveReason.WindowIncomplete));
				}
			}

			int lastFrame = firstFrame + PerfMeterSelfObservability.WindowSizeFrames - 1;
			PerfMeterSelfOverheadWindowSnapshot window = PerfMeterSelfObservability.EndBoundWindow(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-active",
				lastFrame);

			Assert.That(window.MeasurementContained, Is.True);
			Assert.That(window.EnqueueCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames * 2));
			Assert.That(window.FirstEnqueueFrame, Is.EqualTo(firstFrame));
			Assert.That(window.LastEnqueueFrame, Is.EqualTo(lastFrame));
			Assert.That(window.UrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.Ready));
			Assert.That(window.UrpRenderIntegration.InactiveReason, Is.EqualTo(PerfMeterSelfOverheadInactiveReason.None));
			Assert.That(window.UrpRenderIntegration.WindowFrameCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames));
			Assert.That(window.UrpRenderIntegration.CallbackFrameCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames));
			Assert.That(window.UrpRenderIntegration.InvocationCount, Is.EqualTo(PerfMeterSelfObservability.WindowSizeFrames * 2));
			Assert.That(window.UrpRenderIntegration.MeasurementFirstFrame, Is.EqualTo(firstFrame));
			Assert.That(window.UrpRenderIntegration.MeasurementLastFrame, Is.EqualTo(lastFrame));
			Assert.That(window.UrpRenderIntegration.AverageCpuTimeMs, Is.EqualTo(0.15d).Within(0.000001d));
		}

		[Test]
		public void LaterCaptureRejectsPriorCompletedWindowIdentity()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.BeginBoundWindowForTesting(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-first",
				1,
				PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.UrpRenderIntegration, 1, 100000L, 0L);
			PerfMeterSelfOverheadWindowSnapshot first = PerfMeterSelfObservability.EndBoundWindow(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-first",
				120);

			PerfMeterSelfObservability.BeginBoundWindowForTesting(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-second",
				200,
				PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfOverheadWindowSnapshot mismatch = PerfMeterSelfObservability.GetBoundWindowSnapshot(
				PerfMeterSelfOverheadWindowKind.Capture,
				"capture-first",
				210);

			Assert.That(first.Epoch, Is.GreaterThan(0L));
			Assert.That(mismatch.Epoch, Is.Zero);
			Assert.That(mismatch.MeasurementContained, Is.False);
			Assert.That(mismatch.UrpRenderIntegration.InvocationCount, Is.Zero);
			Assert.That(mismatch.UrpRenderIntegration.InactiveReason, Is.EqualTo(PerfMeterSelfOverheadInactiveReason.CaptureWindowMismatch));
		}

		[Test]
		public void PipelineChangeDuringBoundWindowFailsClosed()
		{
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.BeginBoundWindowForTesting(
				PerfMeterSelfOverheadWindowKind.Session,
				"session-pipeline-change",
				10,
				PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.RecordSampleForTesting(PerfMeterSelfOverheadComponent.UrpRenderIntegration, 10, 100000L, 0L);

			PerfMeterSelfObservability.EnsureStarted(PerfMeterRenderPipelineKind.HighDefinition);
			PerfMeterSelfOverheadWindowSnapshot window = PerfMeterSelfObservability.EndBoundWindow(
				PerfMeterSelfOverheadWindowKind.Session,
				"session-pipeline-change",
				20);

			Assert.That(window.MeasurementContained, Is.False);
			Assert.That(window.UrpRenderIntegration.InactiveReason, Is.EqualTo(PerfMeterSelfOverheadInactiveReason.UnknownInactiveReason));
			Assert.That(window.Warning, Does.Contain("render pipeline changed"));
		}

		[Test]
		public void SessionJsonSerializesBoundWindowAndAttributionBoundary()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			recorder.Start(PerfMeterSessionOptions.Default, default, default, PerfMeterSettingsStore.Defaults, 10, 1d, PerfMeterMetricsSnapshot.Stopped);
			string sessionId = recorder.GetSummary().SessionId;
			PerfMeterSelfObservability.Start(PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.BeginBoundWindowForTesting(
				PerfMeterSelfOverheadWindowKind.Session,
				sessionId,
				10,
				PerfMeterRenderPipelineKind.Universal);
			PerfMeterSelfObservability.ReportUrpFeatureState(
				PerfMeterUrpFeatureInstallationState.NotInstalled,
				PerfMeterUrpFeatureEnabledState.Unknown,
				false,
				10,
				"PC_Renderer",
				"UniversalRendererData");
			PerfMeterSelfObservability.EndBoundWindow(PerfMeterSelfOverheadWindowKind.Session, sessionId, 20);

			PerfMeterSelfOverheadWindowSnapshot window = PerfMeterSelfObservability.GetBoundWindowSnapshot(
				PerfMeterSelfOverheadWindowKind.Session,
				sessionId,
				20);
			string json = PerfMeterSessionExporter.BuildJson(
				recorder.GetSummary(),
				Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus(),
				PerfMeterSessionExporter.RuntimePackageIdentity,
				PerfMeterSessionTimelineSnapshot.Empty,
				window);

			Assert.That(json, Does.Contain("\"self_overhead_window\":{\"schema_version\":1"));
			Assert.That(json, Does.Contain("\"identity\":\"" + sessionId + "\""));
			Assert.That(json, Does.Contain("\"inactive_reason\":\"RendererFeatureNotInstalled\""));
			Assert.That(json, Does.Contain("\"gpu_attribution_availability\":\"Unavailable\""));
			Assert.That(json, Does.Contain("whole-frame CPU, GPU, hitch, and GC values are context only"));
		}

		[Test]
		public void RuntimeSessionLifecycleUsesExactPublicWindowIdentity()
		{
			PerformanceMeter.EnsureRunning();
			Assert.That(PerformanceMeter.TryStartSession(PerfMeterSessionOptions.Default).Succeeded, Is.True);
			string sessionId = PerformanceMeter.GetSessionSummary().SessionId;

			PerfMeterSelfOverheadWindowSnapshot active = PerformanceMeter.GetSelfOverheadWindow(PerfMeterSelfOverheadWindowKind.Session, sessionId);
			Assert.That(active.Identity, Is.EqualTo(sessionId));
			Assert.That(active.WindowComplete, Is.False);
			string activeMcp = PerfMeterMcpCommands.SessionSummary();
			Assert.That(activeMcp, Does.Contain("\"self_overhead_window\":{\"schema_version\":1"));
			Assert.That(activeMcp, Does.Contain("\"kind\":\"Session\",\"identity\":\"" + sessionId + "\""));

			Assert.That(PerformanceMeter.TryStopSession().Succeeded, Is.True);
			PerfMeterSelfOverheadWindowSnapshot completed = PerformanceMeter.GetSelfOverheadWindow(PerfMeterSelfOverheadWindowKind.Session, sessionId);
			Assert.That(completed.Identity, Is.EqualTo(sessionId));
			Assert.That(completed.WindowComplete, Is.True);
			Assert.That(completed.Epoch, Is.EqualTo(active.Epoch));
			Assert.That(PerfMeterMcpCommands.SessionSummary(), Does.Contain("\"window_complete\":true"));
		}

		[Serializable]
		private sealed class RuntimeStatusPayload
		{
			public SelfOverheadPayload self_overhead;
		}

		[Serializable]
		private sealed class SelfOverheadPayload
		{
			public string state;
			public string gpu_timing_availability;
			public SelfOverheadComponentPayload collector;
			public SelfOverheadComponentPayload overlay;
			public SelfOverheadComponentPayload urp_render_integration;
		}

		[Serializable]
		private sealed class SelfOverheadComponentPayload
		{
			public string component;
			public int invocation_count;
			public string cpu_budget_state;
			public string inactive_reason;
			public string gpu_attribution_availability;
		}
	}
}
