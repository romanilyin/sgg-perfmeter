using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterFocusCollectionTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterSettingsBootstrap.ResetExplicitSettingsApplication();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterSettingsBootstrap.ResetExplicitSettingsApplication();
		}

		[TestCase("{}", true)]
		[TestCase("{\"schemaVersion\":1}", true)]
		[TestCase("{\"collectWhenUnfocused\":true}", true)]
		[TestCase("{\"collectWhenUnfocused\":false}", false)]
		public void SettingsDefaultAndExplicitOptOutSurviveJson(string json, bool expected)
		{
			Assert.That(PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot settings), Is.True);
			Assert.That(settings.CollectWhenUnfocused, Is.EqualTo(expected));
			string roundTrip = PerfMeterSettingsStore.ToJson(PerfMeterSettingsStore.CreateFromSnapshot(settings));
			Assert.That(PerfMeterSettingsStore.TryReadSnapshot(roundTrip, out settings), Is.True);
			Assert.That(settings.CollectWhenUnfocused, Is.EqualTo(expected));
		}

		[Test]
		public void RuntimeMutationsAndExportsPreserveOptOut()
		{
			Assert.That(PerformanceMeter.TrySetCollectWhenUnfocused(false).Status, Is.EqualTo(PerfMeterMutationStatus.Applied));
			Assert.That(PerformanceMeter.TrySetCollectWhenUnfocused(false).Status, Is.EqualTo(PerfMeterMutationStatus.NoChange));
			PerformanceMeter.SetEditorWarningLogsEnabled(false);
			PerformanceMeter.SetStructuredLogsEnabled(false);
			PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.Memory);
			Assert.That(PerformanceMeter.CollectWhenUnfocused, Is.False);
			StringBuilder builder = new StringBuilder();
			PerfMeterSessionExporter.AppendSettings(builder, PerfMeterRuntime.Instance.ConfiguredSettings);
			Assert.That(builder.ToString(), Does.Contain("\"collect_when_unfocused\":false"));
		}

		[Test]
		public void ExplicitApiOptOutSurvivesAutoStartButExplicitSettingsCanReplaceIt()
		{
			PerformanceMeter.SetCollectWhenUnfocused(false);
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.ToSnapshot(PerfMeterSettingsStore.CreateDefault(), PerfMeterSettingsLoadState.Loaded, string.Empty);
			Assert.That(PerfMeterSettingsBootstrap.TryAutoStartFromSettings(settings), Is.True);
			Assert.That(PerformanceMeter.CollectWhenUnfocused, Is.False);
			Assert.That(PerformanceMeter.ApplySettings(settings), Is.True);
			Assert.That(PerformanceMeter.CollectWhenUnfocused, Is.True);
		}

		[TestCase(true, false, false, false)]
		[TestCase(false, false, false, true)]
		[TestCase(false, true, false, false)]
		[TestCase(true, false, true, true)]
		[TestCase(true, true, true, true)]
		[TestCase(false, true, true, true)]
		public void CollectionPolicySeparatesFocusFromPause(bool collectWhenUnfocused, bool focused, bool paused, bool skipped)
		{
			PerformanceMeter.SetCollectWhenUnfocused(collectWhenUnfocused);
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			SetField(runtime, "_applicationFocused", focused);
			SetField(runtime, "_applicationPaused", paused);
			Assert.That(SkipsCollection(runtime), Is.EqualTo(skipped));
		}

		[Test]
		public void RealPauseResumeStillWarmsUpWhenUnfocusedCollectionIsEnabled()
		{
			PerformanceMeter.SetCollectWhenUnfocused(true);
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			SetField(runtime, "_applicationFocused", false);
			SetField(runtime, "_applicationPaused", true);
			typeof(PerfMeterRuntime).GetMethod("OnApplicationPause", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(runtime, new object[] { false });
			for (int index = 0; index < 3; index++) Assert.That(SkipsCollection(runtime), Is.True);
			Assert.That(SkipsCollection(runtime), Is.False);
		}

		[Test]
		public void BackgroundTimingRejectsRepeatedMissingAndRegressingTimestamps()
		{
			PerfMeterCollector collector = new PerfMeterCollector();
			Assert.That(collector.AcceptFrameTimingTimestamp(100, false), Is.True);
			Assert.That(collector.AcceptFrameTimingTimestamp(100, true), Is.False);
			Assert.That(collector.AcceptFrameTimingTimestamp(0, true), Is.False);
			Assert.That(collector.AcceptFrameTimingTimestamp(99, true), Is.False);
			Assert.That(collector.AcceptFrameTimingTimestamp(101, true), Is.True);
			Assert.That(collector.AcceptFrameTimingTimestamp(101, true), Is.False);
			Assert.That(collector.AcceptFrameTimingTimestamp(102, true), Is.True);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void MissingTimingsRetainOtherMetricsWithoutInventingSessionFps(bool includeValidTimings)
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			recorder.Start(new PerfMeterSessionOptions(0, 0f, 0.01f, 8, false, 0, 0f), default, default, PerfMeterSettingsStore.Defaults, 0, 0d, default);
			PerfMeterCustomMetricSnapshot[] custom = { new PerfMeterCustomMetricSnapshot("focus.value", "Value", "tests", "count", 7d) };
			for (int frame = 1; frame <= 4; frame++)
			{
				double cpuMs = includeValidTimings && frame % 2 == 0 ? frame * 10d : 0d;
				PerfMeterMetricsSnapshot metrics = new PerfMeterMetricsSnapshot(
					PerfMeterRuntimeState.Running, PerfMeterAvailability.Available, frame,
					PerfMeterBottleneck.Unknown, 1000d / 240d, false, cpuMs,
					0d, 0d, 0d, 0d, 1, 1, 1, 1, 0, 0, 0L, 1024L, 0L, 0L, 0d);
				recorder.Update(metrics, frame, frame * 0.1d, custom);
			}

			PerfMeterSessionSummarySnapshot summary = recorder.GetSummary();
			PerfMeterSessionSampleSnapshot[] samples = recorder.GetSamplesCopy();
			Assert.That(summary.SampleCount, Is.EqualTo(4));
			Assert.That(summary.WholeRun.AverageFrameTimeMs, Is.EqualTo(includeValidTimings ? 30d : 0d));
			Assert.That(summary.CurrentScene.AverageFps, Is.EqualTo(includeValidTimings ? 37.5d : 0d));
			Assert.That(summary.MinFps, Is.EqualTo(includeValidTimings ? 25d : 0d));
			Assert.That(summary.MaxFps, Is.EqualTo(includeValidTimings ? 50d : 0d));
			Assert.That(summary.WorstFrame.IsAvailable, Is.EqualTo(includeValidTimings));
			if (includeValidTimings) Assert.That(summary.WorstFrame.CollectionFrame, Is.EqualTo(4));
			Assert.That(samples[0].CustomMetrics[0].Value, Is.EqualTo(7d));
			Assert.That(samples[0].Metrics.SystemUsedMemoryBytes, Is.EqualTo(1024L));
			string json = PerfMeterSessionExporter.BuildJson(summary, samples, default);
			Assert.That(json, Does.Contain("\"average_fps\":" + (includeValidTimings ? "37.5" : "0")));
			Assert.That(json, Does.Contain("focus.value"));
			Assert.That(json, Does.Not.Contain("\"average_fps\":240"));
		}

		private static void SetField(PerfMeterRuntime runtime, string name, bool value)
		{
			typeof(PerfMeterRuntime).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(runtime, value);
		}

		private static bool SkipsCollection(PerfMeterRuntime runtime)
		{
			return (bool)typeof(PerfMeterRuntime).GetMethod("TrySkipCollectionForFocusState", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(runtime, new object[] { null });
		}
	}
}