using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerformanceMeterApiTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
		}

		[Test]
		public void QueryBeforeStartReturnsStoppedStatus()
		{
			PerfMeterStatusSnapshot status = default;
			Assert.DoesNotThrow(() => status = PerformanceMeter.GetStatus());

			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
			Assert.That(status.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(status.FrameTimingAvailability, Is.EqualTo(PerfMeterFrameTimingAvailability.NotCollected));
			Assert.That(status.CollectionFrame, Is.EqualTo(-1));
			Assert.That(status.GraphicsDeviceName, Is.Not.Null);
			Assert.That(status.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(status.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot tryStatus), Is.True);
			Assert.That(tryStatus.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[Test]
		public void EnsureRunningCreatesRunningStatus()
		{
			Assert.DoesNotThrow(PerformanceMeter.EnsureRunning);

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(status.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(status.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(status.Warning, Is.Not.Null);
			Assert.That(status.Bottleneck, Is.EqualTo(PerfMeterBottleneck.Unknown));
		}

		[Test]
		public void StopReturnsStoppedStatus()
		{
			PerformanceMeter.EnsureRunning();
			PerformanceMeter.Stop();

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
			Assert.That(status.CollectionFrame, Is.EqualTo(-1));
		}

		[Test]
		public void MetricsQueryIsSafe()
		{
			PerfMeterMetricsSnapshot stoppedMetrics = default;
			Assert.DoesNotThrow(() => stoppedMetrics = PerformanceMeter.GetLatestMetrics());
			Assert.That(stoppedMetrics.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
			Assert.That(stoppedMetrics.FrameSampleCount, Is.EqualTo(0));
			Assert.That(stoppedMetrics.AverageFps, Is.EqualTo(0d));
			Assert.That(stoppedMetrics.OnePercentLowFps, Is.EqualTo(0d));
			Assert.That(stoppedMetrics.PointOnePercentLowFps, Is.EqualTo(0d));

			Assert.That(PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot tryMetrics), Is.True);
			Assert.That(tryMetrics.Availability, Is.EqualTo(PerfMeterAvailability.Available));

			PerformanceMeter.EnsureRunning();
			PerfMeterMetricsSnapshot runningMetrics = PerformanceMeter.GetLatestMetrics();
			Assert.That(runningMetrics.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(runningMetrics.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(runningMetrics.FrameBudgetMs, Is.EqualTo(1000d / 60d).Within(0.001d));
			Assert.That(runningMetrics.SrpBatcherInstances, Is.GreaterThanOrEqualTo(0));
			Assert.That(runningMetrics.GpuMemoryBytes, Is.GreaterThanOrEqualTo(0L));
		}

		[Test]
		public void OverlayApiIsSafeInEditMode()
		{
			Assert.That(PerformanceMeter.IsOverlayVisible, Is.False);
			Assert.That(PerformanceMeter.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.TopRight));
			Assert.That(PerformanceMeter.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.DoesNotThrow(() => PerformanceMeter.SetOverlayVisible(true));

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(status.OverlayVisible, Is.False);
			Assert.That(status.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.TopRight));
			Assert.That(PerformanceMeter.IsOverlayVisible, Is.False);

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.BottomRight));
			Assert.That(PerformanceMeter.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomRight));
			Assert.That(PerformanceMeter.GetStatus().OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomRight));

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.Graphs));
			Assert.That(PerformanceMeter.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));
			Assert.That(PerformanceMeter.GetStatus().OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));

			Assert.DoesNotThrow(() => PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.GetStatus().TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.GetLatestMetrics().FrameBudgetMs, Is.EqualTo(1000d / 120d).Within(0.001d));

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverlayVisible(false));
			Assert.That(PerformanceMeter.GetStatus().OverlayVisible, Is.False);
		}

		[Test]
		public void OverdrawApiIsOptInAndSafeInEditMode()
		{
			PerfMeterStatusSnapshot stoppedStatus = PerformanceMeter.GetStatus();
			Assert.That(stoppedStatus.OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Off));
			Assert.That(stoppedStatus.OverdrawProgress, Is.EqualTo(0f));

			Assert.DoesNotThrow(() => PerformanceMeter.RequestOverdrawMeasurement(2));
			PerfMeterStatusSnapshot measuringStatus = PerformanceMeter.GetStatus();
			PerfMeterMetricsSnapshot measuringMetrics = PerformanceMeter.GetLatestMetrics();
			bool measurementAccepted = measuringStatus.OverdrawState == PerfMeterOverdrawMeasurementState.Measuring ||
				measuringStatus.OverdrawState == PerfMeterOverdrawMeasurementState.Unsupported;
			Assert.That(measurementAccepted, Is.True);
			Assert.That(measuringMetrics.OverdrawState, Is.EqualTo(measuringStatus.OverdrawState));
			Assert.That(measuringMetrics.OverdrawRatio, Is.EqualTo(0d));

			Assert.DoesNotThrow(PerformanceMeter.CancelOverdrawMeasurement);
			Assert.That(PerformanceMeter.GetStatus().OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Canceled));
		}

		[Test]
		public void UnsupportedOverdrawDoesNotScheduleRenderGraphFrame()
		{
			PerfMeterOverdrawController controller = new PerfMeterOverdrawController();
			controller.RequestMeasurement(2, "Unsupported test backend.");

			Assert.That(controller.State, Is.EqualTo(PerfMeterOverdrawMeasurementState.Unsupported));
			Assert.That(controller.Progress, Is.EqualTo(0f));
			Assert.That(controller.Ratio, Is.EqualTo(0d));
			Assert.That(controller.Warning, Does.Contain("Unsupported test backend"));
			Assert.That(controller.TryBeginRenderGraphFrame(1, 100, out UnityEngine.GraphicsBuffer counterBuffer, out int measurementId), Is.False);
			Assert.That(counterBuffer, Is.Null);
			Assert.That(measurementId, Is.EqualTo(-1));
		}

		[Test]
		public void StaleOverdrawReadbackDoesNotMutateNewMeasurementSession()
		{
			PerfMeterOverdrawController controller = new PerfMeterOverdrawController();
			controller.RequestMeasurement(2, string.Empty);
			int staleMeasurementId = controller.CurrentMeasurementId;

			controller.RequestMeasurement(2, string.Empty);
			Assert.That(controller.CurrentMeasurementId, Is.GreaterThan(staleMeasurementId));

			controller.CompleteCounterReadback(staleMeasurementId, default);
			Assert.That(controller.State, Is.EqualTo(PerfMeterOverdrawMeasurementState.Measuring));
			Assert.That(controller.RecordedFrameCount, Is.EqualTo(0));
			Assert.That(controller.Progress, Is.EqualTo(0f));
		}
	}
}
