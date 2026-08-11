using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterBottleneckClassifierTests
	{
		private const double BudgetMs = 1000d / 60d;

		[Test]
		public void MissingFrameTimingReturnsUnknown()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Unavailable,
				0d,
				0d,
				0d,
				0d,
				0d,
				false);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.Unknown));
		}

		[Test]
		public void SignificantPresentWaitWithWorkBelowBudgetReturnsPresentLimited()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Available,
				22d,
				20d,
				5d,
				8d,
				8d,
				true);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.PresentLimited));
		}

		[Test]
		public void SignificantPresentWaitWithoutGpuTimingReturnsUnknownWhenCpuWorkIsBelowBudget()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Available,
				26d,
				22d,
				5d,
				10d,
				0d,
				false);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.Unknown));
		}

		[Test]
		public void GpuOverBudgetReturnsGpuBoundWithoutPresentWaitRequirement()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Available,
				18d,
				8d,
				5d,
				0d,
				24d,
				true);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.GpuBound));
		}

		[Test]
		public void MainThreadOverBudgetReturnsCpuMainThreadBound()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Available,
				24d,
				24d,
				5d,
				0d,
				8d,
				true);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.CpuMainThreadBound));
		}

		[Test]
		public void MainThreadBoundUsesWorkTimeWithoutPresentWait()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Available,
				32d,
				30d,
				8d,
				10d,
				8d,
				true);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.CpuMainThreadBound));
		}

		[Test]
		public void RenderThreadOverBudgetReturnsCpuRenderThreadBound()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Available,
				24d,
				8d,
				24d,
				0d,
				8d,
				true);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.CpuRenderThreadBound));
		}

		[Test]
		public void MixedOverBudgetReturnsDominantOvershoot()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Available,
				23d,
				22d,
				30d,
				0d,
				20d,
				true);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.CpuRenderThreadBound));
		}

		[Test]
		public void MixedOverBudgetReturnsGpuWhenGpuOvershootDominates()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Available,
				22d,
				20d,
				18d,
				0d,
				32d,
				true);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.GpuBound));
		}

		[Test]
		public void WorkBelowBudgetWithoutPresentWaitReturnsBalanced()
		{
			PerfMeterBottleneck bottleneck = Classify(
				PerfMeterFrameTimingAvailability.Available,
				9d,
				8d,
				5d,
				0d,
				8d,
				true);

			Assert.That(bottleneck, Is.EqualTo(PerfMeterBottleneck.Balanced));
		}

		[Test]
		public void StableDiagnosticsRequireMinimumEvidenceAndPreserveRawWarning()
		{
			PerfMeterBottleneckStabilizer stabilizer = new PerfMeterBottleneckStabilizer(windowSize: 8, minimumEvidenceSamples: 5);
			for (int index = 0; index < 4; index++)
			{
				stabilizer.AddSample(index, index * 0.01d, PerfMeterBottleneck.GpuBound, true, PerfMeterDiagnosticFlags.None, "raw warning");
			}

			PerfMeterDiagnosticsSnapshot snapshot = stabilizer.GetSnapshot(0.04d);

			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(snapshot.StableBottleneck, Is.EqualTo(PerfMeterBottleneck.Unknown));
			Assert.That(snapshot.InstantaneousBottleneck, Is.EqualTo(PerfMeterBottleneck.GpuBound));
			Assert.That((snapshot.Flags & PerfMeterDiagnosticFlags.InsufficientEvidence) != 0, Is.True);
			Assert.That(snapshot.RawWarning, Is.EqualTo("raw warning"));
			Assert.That(snapshot.Coverage, Is.EqualTo(1f));
		}

		[Test]
		public void OscillatingEvidenceStaysUnknownAndReportsContradiction()
		{
			PerfMeterBottleneckStabilizer stabilizer = new PerfMeterBottleneckStabilizer(windowSize: 6, minimumEvidenceSamples: 6);
			for (int index = 0; index < 6; index++)
			{
				PerfMeterBottleneck bottleneck = index % 2 == 0 ? PerfMeterBottleneck.GpuBound : PerfMeterBottleneck.CpuMainThreadBound;
				stabilizer.AddSample(index, index * 0.01d, bottleneck, true, PerfMeterDiagnosticFlags.None, string.Empty);
			}

			PerfMeterDiagnosticsSnapshot snapshot = stabilizer.GetSnapshot(0.06d);

			Assert.That(snapshot.StableBottleneck, Is.EqualTo(PerfMeterBottleneck.Unknown));
			Assert.That(snapshot.HasContradictingEvidence, Is.True);
			Assert.That(snapshot.Confidence, Is.Zero);
		}

		[Test]
		public void StableDiagnosticsIgnoreSingleOutlierAndSwitchAfterSustainedEvidence()
		{
			PerfMeterBottleneckStabilizer stabilizer = new PerfMeterBottleneckStabilizer(windowSize: 8, minimumEvidenceSamples: 5);
			for (int index = 0; index < 5; index++)
			{
				stabilizer.AddSample(index, index * 0.01d, PerfMeterBottleneck.GpuBound, true, PerfMeterDiagnosticFlags.None, string.Empty);
			}
			stabilizer.AddSample(5, 0.05d, PerfMeterBottleneck.CpuRenderThreadBound, true, PerfMeterDiagnosticFlags.None, string.Empty);

			Assert.That(stabilizer.GetSnapshot(0.05d).StableBottleneck, Is.EqualTo(PerfMeterBottleneck.GpuBound));

			for (int index = 6; index < 13; index++)
			{
				stabilizer.AddSample(index, index * 0.01d, PerfMeterBottleneck.CpuRenderThreadBound, true, PerfMeterDiagnosticFlags.None, string.Empty);
			}

			PerfMeterDiagnosticsSnapshot snapshot = stabilizer.GetSnapshot(0.13d);
			Assert.That(snapshot.StableBottleneck, Is.EqualTo(PerfMeterBottleneck.CpuRenderThreadBound));
			Assert.That(snapshot.Confidence, Is.GreaterThanOrEqualTo(0.7f));
		}

		[Test]
		public void StableDiagnosticsReturnUnknownWhenEvidenceBecomesStale()
		{
			PerfMeterBottleneckStabilizer stabilizer = new PerfMeterBottleneckStabilizer(windowSize: 5, minimumEvidenceSamples: 5, staleAfterSeconds: 0.5d);
			for (int index = 0; index < 5; index++)
			{
				stabilizer.AddSample(index, index * 0.01d, PerfMeterBottleneck.GpuBound, true, PerfMeterDiagnosticFlags.None, string.Empty);
			}

			PerfMeterDiagnosticsSnapshot snapshot = stabilizer.GetSnapshot(1d);

			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(snapshot.Freshness, Is.EqualTo(PerfMeterDiagnosticEvidenceFreshness.Stale));
			Assert.That(snapshot.StableBottleneck, Is.EqualTo(PerfMeterBottleneck.Unknown));
			Assert.That((snapshot.Flags & PerfMeterDiagnosticFlags.StaleEvidence) != 0, Is.True);
			Assert.That(snapshot.SampleAgeSeconds, Is.EqualTo(0.96d).Within(0.0001d));
		}

		[Test]
		public void UnavailableEvidenceDoesNotIncreaseCoverage()
		{
			PerfMeterBottleneckStabilizer stabilizer = new PerfMeterBottleneckStabilizer(windowSize: 5, minimumEvidenceSamples: 3);
			for (int index = 0; index < 5; index++)
			{
				stabilizer.AddSample(index, index * 0.01d, PerfMeterBottleneck.Balanced, false, PerfMeterDiagnosticFlags.GpuTimingUnavailable, string.Empty);
			}

			PerfMeterDiagnosticsSnapshot snapshot = stabilizer.GetSnapshot(0.05d);

			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(snapshot.ValidEvidenceSampleCount, Is.Zero);
			Assert.That(snapshot.Coverage, Is.Zero);
			Assert.That((snapshot.Flags & PerfMeterDiagnosticFlags.GpuTimingUnavailable) != 0, Is.True);
		}

		private static PerfMeterBottleneck Classify(
			PerfMeterFrameTimingAvailability availability,
			double cpuFrameTimeMs,
			double cpuMainThreadFrameTimeMs,
			double cpuRenderThreadFrameTimeMs,
			double cpuMainThreadPresentWaitTimeMs,
			double gpuFrameTimeMs,
			bool gpuFrameTimeAvailable)
		{
			return PerfMeterCollector.ClassifyBottleneck(
				availability,
				BudgetMs,
				cpuFrameTimeMs,
				cpuMainThreadFrameTimeMs,
				cpuRenderThreadFrameTimeMs,
				cpuMainThreadPresentWaitTimeMs,
				gpuFrameTimeMs,
				gpuFrameTimeAvailable);
		}
	}
}
