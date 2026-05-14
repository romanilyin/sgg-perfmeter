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
