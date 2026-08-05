using System;
using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterProfilerInstrumentationTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerformanceMeter.ClearCustomMetricProviders();
			PerfMeterProfilerInstrumentation.Reset();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerformanceMeter.ClearCustomMetricProviders();
			PerfMeterProfilerInstrumentation.Reset();
		}

		[Test]
		public void MarkerAndCounterNamesAreStableAndUnique()
		{
			string[] names =
			{
				PerfMeterProfilerInstrumentation.CollectMarkerName,
				PerfMeterProfilerInstrumentation.FrameTimingMarkerName,
				PerfMeterProfilerInstrumentation.CustomMetricsMarkerName,
				PerfMeterProfilerInstrumentation.CpuCoreMarkerName,
				PerfMeterProfilerInstrumentation.DeviceSnapshotMarkerName,
				PerfMeterProfilerInstrumentation.CameraSnapshotMarkerName,
				PerfMeterProfilerInstrumentation.BottleneckMarkerName,
				PerfMeterProfilerInstrumentation.SessionCaptureMarkerName,
				PerfMeterProfilerInstrumentation.AlertCaptureMarkerName,
				PerfMeterProfilerInstrumentation.ExportJsonMarkerName,
				PerfMeterProfilerInstrumentation.ExportCsvMarkerName,
				PerfMeterProfilerInstrumentation.ThermalSampleMarkerName,
				PerfMeterProfilerInstrumentation.CpuFrameTimeCounterName,
				PerfMeterProfilerInstrumentation.CpuMainThreadTimeCounterName,
				PerfMeterProfilerInstrumentation.CpuRenderThreadTimeCounterName,
				PerfMeterProfilerInstrumentation.CpuPresentWaitTimeCounterName,
				PerfMeterProfilerInstrumentation.CpuFrameTimingAvailableCounterName,
				PerfMeterProfilerInstrumentation.GpuFrameTimeCounterName,
				PerfMeterProfilerInstrumentation.GpuFrameTimingAvailableCounterName,
				PerfMeterProfilerInstrumentation.BottleneckCounterName,
				PerfMeterProfilerInstrumentation.CustomMetricCountCounterName,
				PerfMeterProfilerInstrumentation.SessionStateCounterName,
				PerfMeterProfilerInstrumentation.AlertScopeActiveCounterName,
				PerfMeterProfilerInstrumentation.OverdrawStateCounterName,
				PerfMeterProfilerInstrumentation.ThermalAvailableCounterName
			};

			CollectionAssert.AllItemsAreUnique(names);
			Assert.That(names, Has.All.StartsWith("SGG.PerfMeter."));
		}

		[TestCase(0d, 0L)]
		[TestCase(-1d, 0L)]
		[TestCase(0.000001d, 1L)]
		[TestCase(1.25d, 1250000L)]
		[TestCase(16.6667d, 16666700L)]
		public void MillisecondsToNanosecondsIsDeterministic(double milliseconds, long expected)
		{
			Assert.That(PerfMeterProfilerInstrumentation.MillisecondsToNanoseconds(milliseconds), Is.EqualTo(expected));
		}

		[Test]
		public void InvalidMillisecondsConvertToZero()
		{
			Assert.That(PerfMeterProfilerInstrumentation.MillisecondsToNanoseconds(double.NaN), Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.MillisecondsToNanoseconds(double.PositiveInfinity), Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.MillisecondsToNanoseconds(double.NegativeInfinity), Is.Zero);
		}

		[Test]
		public void FrameTimingAndStateCountersUseExistingSemanticCodes()
		{
			PerfMeterProfilerInstrumentation.RecordFrameTimings(true, 16.5d, 8d, 4d, 1d, true, 12.25d);
			PerfMeterProfilerInstrumentation.RecordBottleneck(PerfMeterBottleneck.GpuBound);
			PerfMeterProfilerInstrumentation.RecordCustomMetricCount(3);
			PerfMeterProfilerInstrumentation.RecordSessionState(PerfMeterSessionState.Recording);
			PerfMeterProfilerInstrumentation.RecordAlertScopeActive(true);
			PerfMeterProfilerInstrumentation.RecordOverdrawState(PerfMeterOverdrawMeasurementState.Measuring);
			PerfMeterProfilerInstrumentation.RecordThermalAvailability(true);

			Assert.That(PerfMeterProfilerInstrumentation.CpuFrameTimingAvailable, Is.EqualTo(1));
			Assert.That(PerfMeterProfilerInstrumentation.CpuFrameTimeNanoseconds, Is.EqualTo(16500000L));
			Assert.That(PerfMeterProfilerInstrumentation.CpuMainThreadTimeNanoseconds, Is.EqualTo(8000000L));
			Assert.That(PerfMeterProfilerInstrumentation.CpuRenderThreadTimeNanoseconds, Is.EqualTo(4000000L));
			Assert.That(PerfMeterProfilerInstrumentation.CpuPresentWaitTimeNanoseconds, Is.EqualTo(1000000L));
			Assert.That(PerfMeterProfilerInstrumentation.GpuFrameTimingAvailable, Is.EqualTo(1));
			Assert.That(PerfMeterProfilerInstrumentation.GpuFrameTimeNanoseconds, Is.EqualTo(12250000L));
			Assert.That(PerfMeterProfilerInstrumentation.Bottleneck, Is.EqualTo((int)PerfMeterBottleneck.GpuBound));
			Assert.That(PerfMeterProfilerInstrumentation.CustomMetricCount, Is.EqualTo(3));
			Assert.That(PerfMeterProfilerInstrumentation.SessionState, Is.EqualTo((int)PerfMeterSessionState.Recording));
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.EqualTo(1));
			Assert.That(PerfMeterProfilerInstrumentation.OverdrawState, Is.EqualTo((int)PerfMeterOverdrawMeasurementState.Measuring));
			Assert.That(PerfMeterProfilerInstrumentation.ThermalAvailable, Is.EqualTo(1));
		}

		[Test]
		public void ResetClearsFrameAndCaptureGauges()
		{
			PerfMeterProfilerInstrumentation.RecordFrameTimings(true, 10d, 5d, 2d, 1d, true, 8d);
			PerfMeterProfilerInstrumentation.RecordSessionState(PerfMeterSessionState.Stopped);
			PerfMeterProfilerInstrumentation.RecordAlertScopeActive(true);
			PerfMeterProfilerInstrumentation.RecordOverdrawState(PerfMeterOverdrawMeasurementState.Completed);
			PerfMeterProfilerInstrumentation.RecordThermalAvailability(true);

			PerfMeterProfilerInstrumentation.Reset();

			Assert.That(PerfMeterProfilerInstrumentation.CpuFrameTimeNanoseconds, Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.CpuFrameTimingAvailable, Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.GpuFrameTimeNanoseconds, Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.GpuFrameTimingAvailable, Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.Bottleneck, Is.EqualTo((int)PerfMeterBottleneck.Unknown));
			Assert.That(PerfMeterProfilerInstrumentation.CustomMetricCount, Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.SessionState, Is.EqualTo((int)PerfMeterSessionState.Idle));
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.OverdrawState, Is.EqualTo((int)PerfMeterOverdrawMeasurementState.Off));
			Assert.That(PerfMeterProfilerInstrumentation.ThermalAvailable, Is.Zero);
		}

		[Test]
		public void StopWithoutRuntimeAndProviderClearResetGauges()
		{
			PerfMeterProfilerInstrumentation.RecordFrameTimings(true, 10d, 5d, 2d, 1d, true, 8d);
			PerfMeterProfilerInstrumentation.RecordCustomMetricCount(4);
			PerfMeterProfilerInstrumentation.RecordAlertScopeActive(true);

			PerformanceMeter.ClearCustomMetricProviders();
			Assert.That(PerfMeterProfilerInstrumentation.CustomMetricCount, Is.Zero);

			PerfMeterProfilerInstrumentation.RecordCustomMetricCount(4);
			PerformanceMeter.Stop();

			Assert.That(PerfMeterProfilerInstrumentation.CpuFrameTimeNanoseconds, Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.CustomMetricCount, Is.Zero);
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.Zero);
		}
	}
}
