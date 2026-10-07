using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace SGG.PerfMeter.Tests.URP.EditMode
{
	public sealed class PerfMeterProfilingSamplerMetricProviderTests
	{
		[Test]
		public void RequiresStableIdExactSamplerAndPositiveObservationWindow()
		{
			FakeReadings readings = new FakeReadings();
			Assert.Throws<ArgumentException>(() => new PerfMeterProfilingSamplerMetricProvider(" ", "Test", readings));
			Assert.Throws<ArgumentNullException>(() => new PerfMeterProfilingSamplerMetricProvider("gpu.test", "Test", (ProfilingSampler)null));
			Assert.Throws<ArgumentOutOfRangeException>(() => new PerfMeterProfilingSamplerMetricProvider("gpu.test", "Test", readings, 0));
		}

		[Test]
		public void AlwaysReportsUnavailableMetricUntilRecordingAndProducerEvidenceExist()
		{
			FakeReadings readings = new FakeReadings { Count = 1, ElapsedMs = 5d };
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			AssertReason(provider, "RecordingNotDeclared");
			provider.SetEnabled(true);
			AssertReason(provider, "ProducerEvidenceMissing");
			Assert.That(provider.Id, Is.EqualTo("gpu.test"));
			Assert.That(provider.LastObservationFrame, Is.EqualTo(-1));
		}

		[Test]
		public void ZeroSamplesDoNotInferMissingPassAndScheduledAloneDoesNotProveExecution()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			provider.SetEnabled(true);
			provider.ReportProducerState(PerfMeterGpuPassAvailability.Scheduled);
			AssertReason(provider, "PassCallbackNotObserved");
			provider.ReportPassExecution();
			AssertReason(provider, "GpuSamplesPending");
			Assert.That(provider.ObservedGpuSampleCount, Is.Zero);
		}

		[Test]
		public void DelayedSampledZeroIsAvailableAndHasOnlyCpuObservationProvenance()
		{
			FakeReadings readings = new FakeReadings { FrameCount = 100 };
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			provider.SetEnabled(true);
			provider.ReportPassExecution();
			AssertReason(provider, "GpuSamplesPending");
			readings.FrameCount = 102;
			readings.Count = 2;
			readings.ElapsedMs = 0d;
			provider.ReportProducerState(PerfMeterGpuPassAvailability.Scheduled);
			PerfMeterCustomMetricSnapshot metric = Collect(provider);
			Assert.That(metric.Available, Is.True);
			Assert.That(metric.Value, Is.Zero);
			Assert.That(metric.Unit, Is.EqualTo("ms"));
			Assert.That(metric.Warning, Does.StartWith("DelayedSamplerObservation:"));
			Assert.That(provider.LastObservationFrame, Is.EqualTo(102), "This is observation time, not GPU source frame 100.");
			Assert.That(provider.ObservedGpuSampleCount, Is.EqualTo(2));
		}

		[Test]
		public void CurrentFrameReadingCannotBeAcceptedAtTheFirstRenderFunctionCallback()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			provider.SetEnabled(true);
			readings.FrameCount++;
			readings.Count = 1;
			readings.ElapsedMs = 5d;
			provider.ReportPassExecution();
			AssertReason(provider, "SamplerObservationUnchanged");
			readings.FrameCount++;
			provider.ReportPassExecution();
			AssertReason(provider, "SamplerObservationUnchanged");
			readings.FrameCount++;
			readings.ElapsedMs = 6d;
			provider.ReportPassExecution();
			Assert.That(Collect(provider).Available, Is.True);
		}

		[Test]
		public void ZeroCountDoesNotCarryForwardPriorPositiveSampleAndLaterZeroValueCanRecover()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			MakeAvailable(provider, readings);
			readings.FrameCount++;
			readings.Count = 0;
			readings.ElapsedMs = 0d;
			provider.ReportPassExecution();
			AssertReason(provider, "GpuSamplesPending");
			readings.FrameCount++;
			readings.Count = 1;
			provider.ReportPassExecution();
			PerfMeterCustomMetricSnapshot metric = Collect(provider);
			Assert.That(metric.Available, Is.True);
			Assert.That(metric.Value, Is.Zero);
		}

		[Test]
		public void ExplicitNegativeProducerEvidenceOverridesOldPositiveGpuSamples()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			MakeAvailable(provider, readings);
			readings.FrameCount++;
			provider.ReportProducerState(PerfMeterGpuPassAvailability.NotScheduled);
			AssertReason(provider, "PassNotScheduled");
			Assert.That(provider.LastObservationFrame, Is.EqualTo(-1));
			readings.FrameCount++;
			provider.ReportProducerState(PerfMeterGpuPassAvailability.Unavailable);
			AssertReason(provider, "PassUnavailable");
			readings.FrameCount++;
			provider.ReportProducerState(PerfMeterGpuPassAvailability.Scheduled);
			AssertReason(provider, "PassCallbackNotObserved");
		}

		[Test]
		public void LateGpuResultCannotReviveStaleProducerEvidence()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings, 3);
			provider.SetEnabled(true);
			provider.ReportPassExecution();
			readings.FrameCount = 4;
			readings.Count = 1;
			readings.ElapsedMs = 7d;
			AssertReason(provider, "ProducerEvidenceStale");
		}

		[Test]
		public void ContinuedSchedulingCannotReviveStaleRenderFunctionEvidence()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings, 3);
			provider.SetEnabled(true);
			provider.ReportPassExecution();
			readings.FrameCount = 4;
			provider.ReportProducerState(PerfMeterGpuPassAvailability.Scheduled);
			readings.Count = 1;
			readings.ElapsedMs = 7d;
			AssertReason(provider, "PassCallbackEvidenceStale");
		}

		[Test]
		public void CallbackAfterIdleGapCannotReviveRetainedSampleWithoutLaterChange()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings, 3);
			MakeAvailable(provider, readings);
			readings.FrameCount = 10;
			provider.ReportPassExecution();
			AssertReason(provider, "SamplerObservationUnchanged");
			readings.FrameCount++;
			provider.ReportPassExecution();
			AssertReason(provider, "SamplerObservationUnchanged");
			readings.FrameCount++;
			readings.ElapsedMs = 2d;
			provider.ReportPassExecution();
			Assert.That(Collect(provider).Available, Is.True);
		}

		[Test]
		public void UnchangedResultsExpireEvenWhenProducerRunsEveryFrame()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings, 3);
			MakeAvailable(provider, readings);
			for (int frame = 2; frame <= 4; frame++)
			{
				readings.FrameCount = frame;
				provider.ReportPassExecution();
				Assert.That(Collect(provider).Available, Is.True);
			}
			readings.FrameCount = 5;
			provider.ReportPassExecution();
			AssertReason(provider, "SamplerObservationStale");
			readings.FrameCount = 6;
			readings.ElapsedMs = 2d;
			provider.ReportPassExecution();
			Assert.That(Collect(provider).Available, Is.True);
		}

		[Test]
		public void ReenableRequiresNewEvidenceAndDoesNotReuseUnchangedOldReading()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			MakeAvailable(provider, readings);
			provider.SetEnabled(false);
			AssertReason(provider, "RecordingNotDeclared");
			readings.FrameCount = 10;
			provider.SetEnabled(true);
			AssertReason(provider, "ProducerEvidenceMissing");
			provider.ReportPassExecution();
			readings.FrameCount++;
			provider.ReportPassExecution();
			AssertReason(provider, "SamplerObservationUnchanged");
			readings.FrameCount++;
			readings.ElapsedMs = 2d;
			provider.ReportPassExecution();
			Assert.That(Collect(provider).Available, Is.True);
		}

		[Test]
		public void RepeatedEnableDoesNotResetAnActiveObservationEpoch()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			MakeAvailable(provider, readings);
			int readCount = readings.ReadCount;
			provider.SetEnabled(true);
			Assert.That(Collect(provider).Available, Is.True);
			Assert.That(readings.ReadCount, Is.EqualTo(readCount));
		}

		[Test]
		public void ExistingRegistryRetainsUnavailableSeriesAndPreservesSampledZero()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			PerformanceMeter.RegisterCustomMetricProvider(provider);
			try
			{
				PerfMeterCustomMetricSnapshot unavailable = FindMetric(PerformanceMeter.GetCustomMetrics(), provider.Id);
				Assert.That(unavailable.Available, Is.False);
				provider.SetEnabled(true);
				provider.ReportPassExecution();
				readings.FrameCount++;
				readings.Count = 1;
				PerfMeterCustomMetricSnapshot zero = FindMetric(PerformanceMeter.GetCustomMetrics(), provider.Id);
				Assert.That(zero.Available, Is.True);
				Assert.That(zero.Value, Is.Zero);
				provider.Dispose();
				Assert.That(FindMetric(PerformanceMeter.GetCustomMetrics(), provider.Id).Available, Is.False);
			}
			finally
			{
				PerformanceMeter.UnregisterCustomMetricProvider(provider);
			}
		}

		[Test]
		public void SameFramePositiveCameraEvidenceWinsInEitherOrderAndValueIsNotDivided()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			provider.SetEnabled(true);
			provider.ReportProducerState(PerfMeterGpuPassAvailability.NotScheduled);
			provider.ReportPassExecution(); // First camera.
			provider.ReportPassExecution(); // Second camera, same sampler.
			provider.ReportProducerState(PerfMeterGpuPassAvailability.Unavailable); // Third camera.
			readings.FrameCount++;
			readings.Count = 2;
			readings.ElapsedMs = 6d;
			PerfMeterCustomMetricSnapshot metric = Collect(provider);
			Assert.That(metric.Available, Is.True);
			Assert.That(metric.Value, Is.EqualTo(6d), "Sampler-wide aggregate; no invented per-camera average.");
		}

		[TestCase(double.NaN, 1)]
		[TestCase(double.PositiveInfinity, 1)]
		[TestCase(double.NegativeInfinity, 1)]
		[TestCase(-1d, 1)]
		[TestCase(1d, -1)]
		public void NonfiniteOrNegativeReadingsAreUnavailable(double elapsedMs, int count)
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			provider.SetEnabled(true);
			provider.ReportPassExecution();
			readings.FrameCount++;
			readings.Count = count;
			readings.ElapsedMs = elapsedMs;
			AssertReason(provider, "InvalidGpuReading");
		}

		[Test]
		public void ReadFailureFailsClosedAndRecoveryRequiresAChangedObservation()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			MakeAvailable(provider, readings);
			readings.FrameCount++;
			readings.ThrowOnRead = true;
			provider.ReportPassExecution();
			AssertReason(provider, "SamplerReadFailed");
			readings.FrameCount++;
			readings.ThrowOnRead = false;
			provider.ReportPassExecution();
			AssertReason(provider, "SamplerObservationUnchanged");
			readings.FrameCount++;
			readings.ElapsedMs = 2d;
			provider.ReportPassExecution();
			Assert.That(Collect(provider).Available, Is.True);
		}

		[Test]
		public void DisposeIsIdempotentAndDoesNotMutateSharedReadingOwner()
		{
			FakeReadings readings = new FakeReadings();
			PerfMeterProfilingSamplerMetricProvider first = Create(readings);
			using PerfMeterProfilingSamplerMetricProvider second = Create(readings);
			first.SetEnabled(true);
			second.SetEnabled(true);
			first.ReportPassExecution();
			second.ReportPassExecution();
			first.SetEnabled(false);
			first.Dispose();
			first.Dispose();
			AssertReason(first, "ProviderDisposed");
			Assert.Throws<ObjectDisposedException>(() => first.SetEnabled(true));
			readings.FrameCount++;
			readings.Count = 1;
			readings.ElapsedMs = 2d;
			Assert.That(Collect(second).Available, Is.True, "One observer's lifetime must not stop another observer's source.");
		}

		[Test]
		public void ClockResetDropsAllPreviousProducerAndObservationEvidence()
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			MakeAvailable(provider, readings);
			readings.FrameCount = 0;
			AssertReason(provider, "ObservationClockReset");
			AssertReason(provider, "ProducerEvidenceMissing");
			Assert.That(provider.LastObservationFrame, Is.EqualTo(-1));
		}

		[TestCase(true)]
		[TestCase(false)]
		public void PollsOncePerCpuFrameAndWarmedProducerAndCollectionPathDoesNotAllocate(bool hasSamples)
		{
			FakeReadings readings = new FakeReadings();
			using PerfMeterProfilingSamplerMetricProvider provider = Create(readings);
			MakeAvailable(provider, readings);
			int reads = readings.ReadCount;
			Collect(provider);
			Collect(provider);
			Assert.That(readings.ReadCount, Is.EqualTo(reads));
			readings.Count = hasSamples ? 1 : 0;
			MeasureAllocations(provider, readings, out _); // JIT/interface/string warm-up outside measured assertion.
			long bytes = MeasureAllocations(provider, readings, out bool available);
			Assert.That(available, Is.EqualTo(hasSamples));
			Assert.That(bytes, Is.Zero);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static long MeasureAllocations(PerfMeterProfilingSamplerMetricProvider provider, FakeReadings readings, out bool available)
		{
			available = false;
			long before = GC.GetAllocatedBytesForCurrentThread();
			for (int iteration = 0; iteration < 1000; iteration++)
			{
				readings.FrameCount++;
				readings.ElapsedMs = 1d + (iteration & 1);
				provider.ReportPassExecution();
				provider.TryCollect(out PerfMeterCustomMetricSnapshot metric);
				available = metric.Available;
			}
			return GC.GetAllocatedBytesForCurrentThread() - before;
		}

		private static PerfMeterProfilingSamplerMetricProvider Create(FakeReadings readings, int maxAge = 8)
		{
			return new PerfMeterProfilingSamplerMetricProvider("gpu.test", "Test pass", readings, maxAge);
		}

		private static void MakeAvailable(PerfMeterProfilingSamplerMetricProvider provider, FakeReadings readings)
		{
			provider.SetEnabled(true);
			provider.ReportPassExecution();
			readings.FrameCount++;
			readings.Count = 1;
			readings.ElapsedMs = 1d;
			Assert.That(Collect(provider).Available, Is.True);
		}

		private static PerfMeterCustomMetricSnapshot Collect(PerfMeterProfilingSamplerMetricProvider provider)
		{
			Assert.That(provider.TryCollect(out PerfMeterCustomMetricSnapshot metric), Is.True);
			return metric;
		}

		private static void AssertReason(PerfMeterProfilingSamplerMetricProvider provider, string reason)
		{
			PerfMeterCustomMetricSnapshot metric = Collect(provider);
			Assert.That(metric.Available, Is.False);
			Assert.That(metric.Value, Is.Zero);
			Assert.That(metric.Warning, Does.StartWith(reason + ":"));
		}

		private static PerfMeterCustomMetricSnapshot FindMetric(PerfMeterCustomMetricSnapshot[] metrics, string id)
		{
			foreach (PerfMeterCustomMetricSnapshot metric in metrics)
			{
				if (metric.Id == id)
					return metric;
			}
			Assert.Fail("Expected custom metric was not retained by the existing registry: " + id);
			return default;
		}

		private sealed class FakeReadings : IPerfMeterProfilingSamplerReadings
		{
			public int FrameCount { get; set; }
			internal int Count;
			internal double ElapsedMs;
			internal bool ThrowOnRead;
			internal int ReadCount;

			public void Read(out int sampleCount, out double elapsedMs)
			{
				ReadCount++;
				if (ThrowOnRead)
					throw new InvalidOperationException("Synthetic sampler read failure.");
				sampleCount = Count;
				elapsedMs = ElapsedMs;
			}
		}
	}
}
