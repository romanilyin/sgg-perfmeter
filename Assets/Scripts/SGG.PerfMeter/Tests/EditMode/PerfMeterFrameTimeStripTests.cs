using System;
using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterFrameTimeStripTests
	{
		[Test]
		public void HistoryAdvancesOncePerCollectedFrameAndPreservesGaps()
		{
			PerfMeterFrameTimeStripHistory history = new PerfMeterFrameTimeStripHistory(8);

			Assert.That(history.AddSample(100, 16d, true), Is.True);
			Assert.That(history.AddSample(100, 99d, true), Is.False);
			Assert.That(history.AddSample(101, 0d, false), Is.True);
			Assert.That(history.AddSample(102, 80d, true), Is.True);

			Assert.That(history.Count, Is.EqualTo(3));
			Assert.That(history.LastFrame, Is.EqualTo(102));
			Assert.That(history.TryGetSample(1, out int gapFrame, out double gapValue, out bool gapValid), Is.True);
			Assert.That(gapFrame, Is.EqualTo(101));
			Assert.That(gapValue, Is.Zero);
			Assert.That(gapValid, Is.False);
			Assert.That(history.TryGetSample(2, out int spikeFrame, out double spikeValue, out bool spikeValid), Is.True);
			Assert.That(spikeFrame, Is.EqualTo(102));
			Assert.That(spikeValue, Is.EqualTo(80d));
			Assert.That(spikeValid, Is.True);
		}

		[Test]
		public void BoundedHistoryRetainsNewestRawSamples()
		{
			PerfMeterFrameTimeStripHistory history = new PerfMeterFrameTimeStripHistory(4);
			for (int frame = 1; frame <= 6; frame++)
			{
				history.AddSample(frame, frame == 5 ? 90d : 10d + frame, true);
			}

			Assert.That(history.Count, Is.EqualTo(4));
			Assert.That(history.TryGetSample(0, out int firstFrame, out _, out _), Is.True);
			Assert.That(firstFrame, Is.EqualTo(3));
			Assert.That(history.GetPeak(), Is.EqualTo(90d));
		}

		[Test]
		public void PixelEnvelopePreservesSingleFrameSpikeWithoutAveraging()
		{
			PerfMeterFrameTimeStripHistory history = new PerfMeterFrameTimeStripHistory(8);
			double[] samples = { 10d, 11d, 12d, 100d, 13d, 14d, 15d, 16d };
			for (int sample = 0; sample < samples.Length; sample++)
			{
				history.AddSample(sample, samples[sample], true);
			}

			Assert.That(history.TryGetEnvelope(0, 2, out double firstMin, out double firstMax), Is.True);
			Assert.That(firstMin, Is.EqualTo(10d));
			Assert.That(firstMax, Is.EqualTo(100d));
			Assert.That(history.TryGetEnvelope(1, 2, out double secondMin, out double secondMax), Is.True);
			Assert.That(secondMin, Is.EqualTo(13d));
			Assert.That(secondMax, Is.EqualTo(16d));
		}

		[Test]
		public void WarmedRawHistoryUpdatesDoNotAllocate()
		{
			PerfMeterFrameTimeStripHistory history = new PerfMeterFrameTimeStripHistory(600);
			for (int frame = 0; frame < 600; frame++)
			{
				history.AddSample(frame, 16d, true);
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			long before = GC.GetAllocatedBytesForCurrentThread();
			for (int frame = 600; frame < 1600; frame++)
			{
				history.AddSample(frame, frame == 1200 ? 75d : 16d, true);
			}
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

			Assert.That(history.Count, Is.EqualTo(600));
			Assert.That(history.LastFrame, Is.EqualTo(1599));
			Assert.That(allocatedBytes, Is.Zero);
		}

		[Test]
		public void WarmedStripElementUpdatesDoNotAllocate()
		{
			PerfMeterOverlay.PerfMeterFrameTimeStripElement strip = new PerfMeterOverlay.PerfMeterFrameTimeStripElement(120);
			strip.AddSample(0, 16d, true);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long before = GC.GetAllocatedBytesForCurrentThread();
			for (int frame = 1; frame <= 1000; frame++)
			{
				strip.AddSample(frame, frame == 500 ? 75d : 16d, true);
			}
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

			Assert.That(strip.SampleCount, Is.EqualTo(120));
			Assert.That(strip.LastFrame, Is.EqualTo(1000));
			Assert.That(allocatedBytes, Is.Zero);
		}
	}
}
