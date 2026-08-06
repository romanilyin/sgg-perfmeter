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
			Assert.That(PerformanceMeter.GetStatus().SelfOverhead.Overlay.InvocationCount, Is.EqualTo(1));
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
		}

		[Serializable]
		private sealed class SelfOverheadComponentPayload
		{
			public string component;
			public int invocation_count;
			public string cpu_budget_state;
		}
	}
}
