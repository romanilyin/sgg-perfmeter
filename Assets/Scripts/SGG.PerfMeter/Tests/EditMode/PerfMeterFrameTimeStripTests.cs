using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

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

		[TestCase(7.99d, 0.18f, 0.74f, 0.23f)]
		[TestCase(8d, 0.20f, 0.62f, 1f)]
		[TestCase(10d, 0.20f, 0.62f, 1f)]
		[TestCase(10.01d, 1f, 0.82f, 0.24f)]
		[TestCase(12d, 1f, 0.82f, 0.24f)]
		[TestCase(12.01d, 1f, 0.82f, 0.24f)]
		[TestCase(20d, 1f, 0.82f, 0.24f)]
		[TestCase(20.01d, 1f, 0.24f, 0.20f)]
		public void RawFrameTimeColorsUseFourBudgetBands(double frameTimeMs, float red, float green, float blue)
		{
			PerfMeterOverlay.PerfMeterFrameTimeStripElement strip = new PerfMeterOverlay.PerfMeterFrameTimeStripElement(16);
			strip.SetFrameBudgetMs(10d);

			Color color = strip.GetSeverityColorForTests(frameTimeMs);

			Assert.That(color.r, Is.EqualTo(red).Within(0.0001f));
			Assert.That(color.g, Is.EqualTo(green).Within(0.0001f));
			Assert.That(color.b, Is.EqualTo(blue).Within(0.0001f));
			Assert.That(color.a, Is.EqualTo(1f));
		}

		[Test]
		public void SmoothedGraphRawPeakBackdropDoesNotChangeScaleAndClipsToPlot()
		{
			PerfMeterOverlay.PerfMeterGraphElement graph = CreateGraph(8);
			graph.SetFrameBudgetMs(10d);
			graph.RecordRawSample(0, 10d, true);
			graph.AddSample(0, 10d, 0d, 0d, true);
			graph.RecordRawSample(1, 11d, true);
			graph.RecordRawSample(2, 75d, true);
			graph.RecordRawSample(3, 12d, true);
			graph.AddSample(3, 12d, 0d, 0d, true);

			PerfMeterOverlay.PerfMeterGraphElement baseline = CreateGraph(8);
			baseline.SetFrameBudgetMs(10d);
			baseline.AddSample(0, 10d, 0d, 0d, true);
			baseline.AddSample(3, 12d, 0d, 0d, true);

			Assert.That(graph.ScaleMs, Is.EqualTo(baseline.ScaleMs));
			Assert.That(graph.ScaleMs, Is.EqualTo(13d));
			Assert.That(graph.TryGetRawPeakBucket(1, 2, out int sample, out double smoothed, out double rawPeak), Is.True);
			Assert.That(sample, Is.EqualTo(1));
			Assert.That(smoothed, Is.EqualTo(12d));
			Assert.That(rawPeak, Is.EqualTo(75d));

			Rect plot = new Rect(0f, 0f, 100f, 50f);
			Assert.That(PerfMeterOverlay.PerfMeterGraphElement.ValueToY(plot, rawPeak, graph.ScaleMs), Is.EqualTo(plot.yMin));
			Assert.That(PerfMeterOverlay.PerfMeterGraphElement.ValueToY(plot, smoothed, graph.ScaleMs), Is.GreaterThan(plot.yMin));
		}

		[Test]
		public void SmoothedGraphPixelBucketsKeepMaximumValidRawPeak()
		{
			PerfMeterOverlay.PerfMeterGraphElement graph = CreateGraph(8);
			double[] rawSamples = { 11d, 12d, 13d, 100d, 14d, 15d, 16d, 40d };
			for (int frame = 0; frame < rawSamples.Length; frame++)
			{
				graph.RecordRawSample(frame, rawSamples[frame], true);
				graph.AddSample(frame, 10d, 0d, 0d, true);
			}

			Assert.That(graph.TryGetRawPeakBucket(0, 2, out int firstSample, out _, out double firstPeak), Is.True);
			Assert.That(firstSample, Is.EqualTo(3));
			Assert.That(firstPeak, Is.EqualTo(100d));
			Assert.That(graph.TryGetRawPeakBucket(1, 2, out int secondSample, out _, out double secondPeak), Is.True);
			Assert.That(secondSample, Is.EqualTo(7));
			Assert.That(secondPeak, Is.EqualTo(40d));
		}

		[Test]
		public void SmoothedGraphInvalidOrNonPeakRawSamplesDoNotCreateBackdrop()
		{
			PerfMeterOverlay.PerfMeterGraphElement graph = CreateGraph(8);
			graph.RecordRawSample(0, 80d, true);
			graph.RecordRawSample(1, 0d, false);
			graph.AddSample(1, 10d, 0d, 0d, true);
			graph.RecordRawSample(2, 80d, false);
			graph.AddSample(2, 10d, 0d, 0d, true);
			graph.RecordRawSample(3, 10d, true);
			graph.AddSample(3, 10d, 0d, 0d, true);
			graph.RecordRawSample(4, double.NaN, true);
			graph.AddSample(4, 10d, 0d, 0d, true);

			Assert.That(graph.TryGetRawPeakBucket(0, 1, out _, out _, out _), Is.False);
		}

		[Test]
		public void WarmedSmoothedGraphRawPeakAccumulationDoesNotAllocate()
		{
			PerfMeterOverlay.PerfMeterGraphElement graph = CreateGraph(120);
			graph.RecordRawSample(0, 16d, true);
			graph.AddSample(0, 16d, 0d, 0d, true);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long before = GC.GetAllocatedBytesForCurrentThread();
			for (int frame = 1; frame <= 1000; frame++)
			{
				graph.RecordRawSample(frame, frame == 500 ? 75d : 16d, true);
			}
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

			Assert.That(allocatedBytes, Is.Zero);
		}

		[Test]
		public void CustomSeriesUseStableIdsIndependentSignedScalesAndExplicitGaps()
		{
			PerfMeterOverlay.PerfMeterFrameTimeStripElement strip = new PerfMeterOverlay.PerfMeterFrameTimeStripElement(16);
			strip.ConfigureCustomMetricSeries(new[]
			{
				new PerfMeterCustomMetricGraphJson { metricId = "movement.horizontal", min = -10d, max = 10d, displayScale = 2d, color = "#FF3355", unit = "m/s" },
				new PerfMeterCustomMetricGraphJson { metricId = "movement.vertical", min = -1d, max = 1d, displayScale = 0.1d, color = "#33CCFF", unit = "ratio" }
			});

			Assert.That(strip.CustomSeriesCount, Is.EqualTo(2));
			Assert.That(strip.TryGetCustomSeriesConfiguration(0, out PerfMeterCustomMetricGraphConfiguration horizontal), Is.True);
			Assert.That(horizontal.MetricId, Is.EqualTo("movement.horizontal"));
			Assert.That(horizontal.Min, Is.EqualTo(-10d));
			Assert.That(horizontal.Max, Is.EqualTo(10d));
			Assert.That(horizontal.DisplayScale, Is.EqualTo(2d));
			Assert.That(horizontal.Unit, Is.EqualTo("m/s"));

			PerfMeterCustomMetricSnapshot[] firstFrame =
			{
				new PerfMeterCustomMetricSnapshot("movement.vertical", "Vertical", "movement", "raw", 7d),
				new PerfMeterCustomMetricSnapshot("movement.horizontal", "Horizontal", "movement", "raw", -3d)
			};
			strip.AddCustomMetricSamples(10, firstFrame, firstFrame.Length);
			strip.AddCustomMetricSamples(11, new[] { new PerfMeterCustomMetricSnapshot("movement.horizontal", "Horizontal", "movement", "raw", 0d, false) }, 1);

			Assert.That(strip.TryGetCustomSeriesSample(0, 0, out double horizontalValue, out bool horizontalValid), Is.True);
			Assert.That(horizontalValue, Is.EqualTo(-6d));
			Assert.That(horizontalValid, Is.True);
			Assert.That(strip.TryGetCustomSeriesSample(1, 0, out double verticalValue, out bool verticalValid), Is.True);
			Assert.That(verticalValue, Is.EqualTo(0.7d).Within(0.000001d));
			Assert.That(verticalValid, Is.True);
			Assert.That(strip.TryGetCustomSeriesSample(0, 1, out _, out bool unavailableValid), Is.True);
			Assert.That(unavailableValid, Is.False);
			Assert.That(strip.TryGetCustomSeriesSample(1, 1, out _, out bool missingValid), Is.True);
			Assert.That(missingValid, Is.False);
		}

		[Test]
		public void CustomSeriesConfigurationIsBoundedAndRoundTripsAdditively()
		{
			PerfMeterOverlayPresetJson preset = PerfMeterOverlayPresetDefaults.CreateGraphs();
			preset.customMetricGraphs = new[]
			{
				new PerfMeterCustomMetricGraphJson { metricId = "metric.0", min = -5d, max = 5d, displayScale = 2d, color = "#FF0000", unit = "m/s" },
				new PerfMeterCustomMetricGraphJson { metricId = "metric.1", min = 0d, max = 100d, color = "#00FF00", unit = "%" },
				new PerfMeterCustomMetricGraphJson { metricId = "metric.2", min = -1d, max = 1d, color = "#0000FF" },
				new PerfMeterCustomMetricGraphJson { metricId = "metric.3", min = 0d, max = 1d, color = "#FFFFFF" },
				new PerfMeterCustomMetricGraphJson { metricId = "metric.4", min = 0d, max = 1d, color = "#FFFF00" }
			};

			PerfMeterOverlayPresetValidationResult validation = PerfMeterOverlayPresetUtility.Validate(preset);
			Assert.That(validation.IsValid, Is.True);
			Assert.That(validation.Warning, Does.Contain("limit is 4"));

			string json = PerfMeterOverlayPresetUtility.ToJson(preset);
			Assert.That(PerfMeterOverlayPresetUtility.TryReadJson(json, out PerfMeterOverlayPresetJson parsed, out string warning), Is.True, warning);
			Assert.That(parsed.customMetricGraphs, Has.Length.EqualTo(5));
			Assert.That(parsed.customMetricGraphs[0].metricId, Is.EqualTo("metric.0"));
			Assert.That(parsed.customMetricGraphs[0].min, Is.EqualTo(-5d));
			Assert.That(parsed.customMetricGraphs[0].max, Is.EqualTo(5d));
			Assert.That(parsed.customMetricGraphs[0].displayScale, Is.EqualTo(2d));
			Assert.That(parsed.customMetricGraphs[0].unit, Is.EqualTo("m/s"));

			PerfMeterOverlay.PerfMeterFrameTimeStripElement strip = new PerfMeterOverlay.PerfMeterFrameTimeStripElement(16);
			strip.ConfigureCustomMetricSeries(parsed.customMetricGraphs);
			Assert.That(strip.CustomSeriesCount, Is.EqualTo(PerfMeterOverlayPresetUtility.MaxCustomMetricGraphSeries));
		}

		[Test]
		public void WarmedCustomSeriesUpdatesDoNotAllocate()
		{
			PerfMeterOverlay.PerfMeterFrameTimeStripElement strip = new PerfMeterOverlay.PerfMeterFrameTimeStripElement(120);
			strip.ConfigureCustomMetricSeries(new[]
			{
				new PerfMeterCustomMetricGraphJson { metricId = "speed.x", min = -20d, max = 20d, displayScale = 1d, color = "#FF3355", unit = "m/s" },
				new PerfMeterCustomMetricGraphJson { metricId = "speed.y", min = -10d, max = 10d, displayScale = 1d, color = "#33CCFF", unit = "m/s" }
			});
			PerfMeterCustomMetricSnapshot[] metrics =
			{
				new PerfMeterCustomMetricSnapshot("speed.x", "X", "movement", "m/s", -3d),
				new PerfMeterCustomMetricSnapshot("speed.y", "Y", "movement", "m/s", 4d)
			};
			strip.AddCustomMetricSamples(0, metrics, metrics.Length);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long before = GC.GetAllocatedBytesForCurrentThread();
			for (int frame = 1; frame <= 1000; frame++)
			{
				strip.AddCustomMetricSamples(frame, metrics, metrics.Length);
			}
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

			Assert.That(allocatedBytes, Is.Zero);
		}

		private static PerfMeterOverlay.PerfMeterGraphElement CreateGraph(int capacity)
		{
			return new PerfMeterOverlay.PerfMeterGraphElement(
				"test-smoothed-graph",
				PerfMeterOverlay.PerfMeterGraphMode.Line,
				50f,
				new Label(),
				new Label(),
				capacity);
		}
	}
}
