using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterApiTests
	{
		[SetUp]
		public void SetUp()
		{
			PerfMeter.Stop();
		}

		[TearDown]
		public void TearDown()
		{
			PerfMeter.Stop();
		}

		[Test]
		public void QueryBeforeStartReturnsStoppedStatus()
		{
			PerfMeterStatusSnapshot status = default;
			Assert.DoesNotThrow(() => status = PerfMeter.GetStatus());

			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
			Assert.That(status.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(status.FrameTimingAvailability, Is.EqualTo(PerfMeterFrameTimingAvailability.NotCollected));
			Assert.That(status.CollectionFrame, Is.EqualTo(-1));
			Assert.That(status.GraphicsDeviceName, Is.Not.Null);
			Assert.That(status.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(status.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(PerfMeter.TryGetStatus(out PerfMeterStatusSnapshot tryStatus), Is.True);
			Assert.That(tryStatus.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[Test]
		public void EnsureRunningCreatesRunningStatus()
		{
			Assert.DoesNotThrow(PerfMeter.EnsureRunning);

			PerfMeterStatusSnapshot status = PerfMeter.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(status.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(status.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(status.Warning, Is.Not.Null);
			Assert.That(status.Bottleneck, Is.EqualTo(PerfMeterBottleneck.Unknown));
		}

		[Test]
		public void StopReturnsStoppedStatus()
		{
			PerfMeter.EnsureRunning();
			PerfMeter.Stop();

			PerfMeterStatusSnapshot status = PerfMeter.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
			Assert.That(status.CollectionFrame, Is.EqualTo(-1));
		}

		[Test]
		public void MetricsQueryIsSafe()
		{
			PerfMeterMetricsSnapshot stoppedMetrics = default;
			Assert.DoesNotThrow(() => stoppedMetrics = PerfMeter.GetLatestMetrics());
			Assert.That(stoppedMetrics.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
			Assert.That(stoppedMetrics.FrameSampleCount, Is.EqualTo(0));
			Assert.That(stoppedMetrics.AverageFps, Is.EqualTo(0d));
			Assert.That(stoppedMetrics.OnePercentLowFps, Is.EqualTo(0d));
			Assert.That(stoppedMetrics.PointOnePercentLowFps, Is.EqualTo(0d));

			Assert.That(PerfMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot tryMetrics), Is.True);
			Assert.That(tryMetrics.Availability, Is.EqualTo(PerfMeterAvailability.Available));

			PerfMeter.EnsureRunning();
			PerfMeterMetricsSnapshot runningMetrics = PerfMeter.GetLatestMetrics();
			Assert.That(runningMetrics.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(runningMetrics.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(runningMetrics.FrameBudgetMs, Is.EqualTo(1000d / 60d).Within(0.001d));
			Assert.That(runningMetrics.SrpBatcherInstances, Is.GreaterThanOrEqualTo(0));
			Assert.That(runningMetrics.GpuMemoryBytes, Is.GreaterThanOrEqualTo(0L));
		}

		[Test]
		public void OverlayApiIsSafeInEditMode()
		{
			Assert.That(PerfMeter.IsOverlayVisible, Is.False);
			Assert.That(PerfMeter.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.TopRight));
			Assert.That(PerfMeter.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(PerfMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.DoesNotThrow(() => PerfMeter.SetOverlayVisible(true));

			PerfMeterStatusSnapshot status = PerfMeter.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(status.OverlayVisible, Is.False);
			Assert.That(status.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.TopRight));
			Assert.That(PerfMeter.IsOverlayVisible, Is.False);

			Assert.DoesNotThrow(() => PerfMeter.SetOverlayCorner(PerfMeterOverlayCorner.BottomRight));
			Assert.That(PerfMeter.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomRight));
			Assert.That(PerfMeter.GetStatus().OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomRight));

			Assert.DoesNotThrow(() => PerfMeter.SetOverlayMode(PerfMeterOverlayMode.Graphs));
			Assert.That(PerfMeter.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));
			Assert.That(PerfMeter.GetStatus().OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));

			Assert.DoesNotThrow(() => PerfMeter.SetTargetFps(PerfMeterTargetFps.Fps120));
			Assert.That(PerfMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerfMeter.GetStatus().TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerfMeter.GetLatestMetrics().FrameBudgetMs, Is.EqualTo(1000d / 120d).Within(0.001d));

			Assert.DoesNotThrow(() => PerfMeter.SetOverlayVisible(false));
			Assert.That(PerfMeter.GetStatus().OverlayVisible, Is.False);
		}

		[Test]
		public void OverdrawApiIsOptInAndSafeInEditMode()
		{
			PerfMeterStatusSnapshot stoppedStatus = PerfMeter.GetStatus();
			Assert.That(stoppedStatus.OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Off));
			Assert.That(stoppedStatus.OverdrawProgress, Is.EqualTo(0f));

			Assert.DoesNotThrow(() => PerfMeter.RequestOverdrawMeasurement(2));
			PerfMeterStatusSnapshot measuringStatus = PerfMeter.GetStatus();
			PerfMeterMetricsSnapshot measuringMetrics = PerfMeter.GetLatestMetrics();
			Assert.That(measuringStatus.OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Measuring));
			Assert.That(measuringMetrics.OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Measuring));
			Assert.That(measuringMetrics.OverdrawRatio, Is.EqualTo(0d));

			Assert.DoesNotThrow(PerfMeter.CancelOverdrawMeasurement);
			Assert.That(PerfMeter.GetStatus().OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Canceled));
		}
	}
}
