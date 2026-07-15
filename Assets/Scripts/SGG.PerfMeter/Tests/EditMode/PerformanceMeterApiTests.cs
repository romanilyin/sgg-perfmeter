using System.IO;
using System.Text;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TestTools;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerformanceMeterApiTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.ClearCustomMetricProviders();
			PerformanceMeter.Stop();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerformanceMeter.ClearCustomMetricProviders();
		}

		[Test]
		public void QueryBeforeStartReturnsStoppedStatus()
		{
			PerfMeterStatusSnapshot status = default;
			Assert.DoesNotThrow(() => status = PerformanceMeter.GetStatus());

			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
			Assert.That(status.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(status.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Stopped));
			Assert.That(status.FrameTimingAvailability, Is.EqualTo(PerfMeterFrameTimingAvailability.NotCollected));
			Assert.That(status.CollectionFrame, Is.EqualTo(-1));
			Assert.That(status.GraphicsDeviceName, Is.Not.Null);
			Assert.That(status.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(status.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.MetricBars));
			AssertHasModule(status.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
			AssertDoesNotHaveModule(status.OverlayModules, PerfMeterOverlayModule.CpuCores);
			AssertDoesNotHaveModule(status.OverlayModules, PerfMeterOverlayModule.CpuCoreGraphs);
			Assert.That(status.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(status.OverdrawHeatmapVisible, Is.False);
			Assert.That(status.SessionState, Is.EqualTo(PerfMeterSessionState.Idle));
			Assert.That(status.IsSessionRecording, Is.False);
			Assert.That(status.ApplicationFocused, Is.True);
			Assert.That(status.ApplicationPaused, Is.False);
			Assert.That(status.EditorWarningsEnabled, Is.True);
			Assert.That(PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot tryStatus), Is.True);
			Assert.That(tryStatus.State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[Test]
		public void EnsureRunningCreatesRunningStatus()
		{
			Assert.DoesNotThrow(PerformanceMeter.EnsureRunning);

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(status.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Overlay));
			Assert.That(status.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(status.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(status.Warning, Is.Not.Null);
			Assert.That(status.Bottleneck, Is.EqualTo(PerfMeterBottleneck.Unknown));
			Assert.That(status.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.MetricBars));
			Assert.That(status.OverlayModules, Is.Not.EqualTo(PerfMeterOverlayModule.All));
			AssertHasModule(status.OverlayModules, PerfMeterOverlayModule.Graphs);
			AssertHasModule(status.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
			AssertDoesNotHaveModule(status.OverlayModules, PerfMeterOverlayModule.CpuCores);
			AssertDoesNotHaveModule(status.OverlayModules, PerfMeterOverlayModule.CpuCoreGraphs);
			Assert.That(status.SessionState, Is.EqualTo(PerfMeterSessionState.Idle));
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
			Assert.That(PerformanceMeter.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.MetricBars));
			AssertHasModule(PerformanceMeter.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
			AssertDoesNotHaveModule(PerformanceMeter.OverlayModules, PerfMeterOverlayModule.CpuCores);
			AssertDoesNotHaveModule(PerformanceMeter.OverlayModules, PerfMeterOverlayModule.CpuCoreGraphs);
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(PerformanceMeter.EditorWarningLogsEnabled, Is.True);
			Assert.That(PerformanceMeter.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Stopped));
			Assert.That(PerformanceMeter.IsOverdrawHeatmapVisible, Is.False);
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
			Assert.That(PerformanceMeter.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.Graphs));
			Assert.That(PerformanceMeter.GetStatus().OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));
			Assert.That(PerformanceMeter.GetStatus().OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.Graphs));
			Assert.That(PerformanceMeter.GetStatus().OverlayPreset, Is.EqualTo(PerfMeterOverlayPreset.Custom));

			Assert.DoesNotThrow(() => PerformanceMeter.SetEditorWarningLogsEnabled(false));
			Assert.That(PerformanceMeter.EditorWarningLogsEnabled, Is.False);
			Assert.That(PerformanceMeter.GetStatus().EditorWarningsEnabled, Is.False);
			Assert.DoesNotThrow(() => PerformanceMeter.SetEditorWarningLogsEnabled(true));
			Assert.That(PerformanceMeter.EditorWarningLogsEnabled, Is.True);
			Assert.That(PerformanceMeter.GetStatus().EditorWarningsEnabled, Is.True);

			Assert.DoesNotThrow(() => PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.GetStatus().TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.GetLatestMetrics().FrameBudgetMs, Is.EqualTo(1000d / 120d).Within(0.001d));

			Assert.DoesNotThrow(() => PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background));
			Assert.That(PerformanceMeter.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));
			Assert.That(PerformanceMeter.GetStatus().CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));

			Assert.DoesNotThrow(() => PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay));
			Assert.That(PerformanceMeter.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Overlay));
			Assert.That(PerformanceMeter.GetStatus().CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Overlay));

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverdrawHeatmapVisible(true));
			Assert.That(PerformanceMeter.IsOverdrawHeatmapVisible, Is.True);
			Assert.That(PerformanceMeter.GetStatus().OverdrawHeatmapVisible, Is.True);

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverdrawHeatmapVisible(false));
			Assert.That(PerformanceMeter.IsOverdrawHeatmapVisible, Is.False);
			Assert.That(PerformanceMeter.GetStatus().OverdrawHeatmapVisible, Is.False);

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverlayVisible(false));
			Assert.That(PerformanceMeter.GetStatus().OverlayVisible, Is.False);

			Assert.DoesNotThrow(() => PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Stopped));
			Assert.That(PerformanceMeter.GetStatus().CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Stopped));
		}

		[Test]
		public void OverlayTextCacheSkipsUnchangedStringMaterialization()
		{
			PerfMeterOverlay.PerfMeterOverlayCachedText cache = new PerfMeterOverlay.PerfMeterOverlayCachedText();
			StringBuilder builder = new StringBuilder("FPS 60.0");

			Assert.That(cache.TryUpdate(builder, out string firstText), Is.True);
			Assert.That(cache.TryUpdate(builder, out string secondText), Is.False);
			Assert.That(secondText, Is.SameAs(firstText));

			builder.Append(" | 1% 59.9");
			Assert.That(cache.TryUpdate(builder, out string thirdText), Is.True);
			Assert.That(thirdText, Is.Not.SameAs(firstText));
		}

		[Test]
		public void OverlayEnumTextUsesCachedNames()
		{
			string first = PerfMeterOverlay.GetBottleneckText(PerfMeterBottleneck.GpuBound);
			string second = PerfMeterOverlay.GetBottleneckText(PerfMeterBottleneck.GpuBound);

			Assert.That(first, Is.EqualTo("GpuBound"));
			Assert.That(second, Is.SameAs(first));
			Assert.That(PerfMeterOverlay.GetRuntimeStateText(PerfMeterRuntimeState.Running), Is.EqualTo("Running"));
			Assert.That(PerfMeterOverlay.GetOverdrawStateText(PerfMeterOverdrawMeasurementState.Unsupported), Is.EqualTo("Unsupported"));
		}

		[Test]
		public void FrameTimingSanityAllowsOneMinuteAndRejectsUnderflowSentinel()
		{
			Assert.That(PerfMeterCollector.IsValidFrameTimingSampleMs(16d), Is.True);
			Assert.That(PerfMeterCollector.IsValidFrameTimingSampleMs(60000d), Is.True);
			Assert.That(PerfMeterCollector.IsValidFrameTimingSampleMs(60000.001d), Is.False);
			Assert.That(PerfMeterCollector.IsValidFrameTimingSampleMs(1844674407370955.2d), Is.False);
			Assert.That(PerfMeterCollector.IsValidFrameTimingSampleMs(double.NaN), Is.False);
			Assert.That(PerfMeterCollector.IsValidFrameTimingSampleMs(double.PositiveInfinity), Is.False);
			Assert.That(PerfMeterCollector.IsValidFrameTimingComponentMs(0d), Is.True);
		}

		[Test]
		public void FrameStatsSamplerIgnoresInvalidHugeFrameTimingSamples()
		{
			PerfMeterFrameStatsSampler sampler = new PerfMeterFrameStatsSampler();
			sampler.AddSample(16d, true);
			sampler.AddSample(1844674407370955.2d, true);
			sampler.AddSample(60000d, true);

			PerfMeterFrameStatsSnapshot snapshot = sampler.GetSnapshot();
			Assert.That(snapshot.SampleCount, Is.EqualTo(2));
			Assert.That(snapshot.GpuValidSampleCount, Is.EqualTo(2));
			Assert.That(snapshot.FrameSpikeCount, Is.EqualTo(1));
			Assert.That(snapshot.SevereFrameSpikeCount, Is.EqualTo(1));

			sampler.Reset();
			sampler.AddSample(60000.001d, true);
			Assert.That(sampler.GetSnapshot().SampleCount, Is.EqualTo(0));
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

		[Test]
		public void SessionApiStartsStopsAndCapturesMetadata()
		{
			Assert.That(PerformanceMeter.IsSessionRecording, Is.False);

			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.01f, 2));

			Assert.That(PerformanceMeter.IsSessionRecording, Is.True);
			Assert.That(PerformanceMeter.GetStatus().SessionState, Is.EqualTo(PerfMeterSessionState.Recording));
			PerfMeterSessionSummarySnapshot recordingSummary = PerformanceMeter.GetSessionSummary();
			Assert.That(recordingSummary.State, Is.EqualTo(PerfMeterSessionState.Recording));
			Assert.That(recordingSummary.Options.MaxSamples, Is.EqualTo(2));
			Assert.That(recordingSummary.Settings.SessionMaxSamples, Is.GreaterThanOrEqualTo(1));
			Assert.That(recordingSummary.Device.UnityVersion, Is.Not.Empty);

			PerformanceMeter.StopSession();

			Assert.That(PerformanceMeter.IsSessionRecording, Is.False);
			Assert.That(PerformanceMeter.GetStatus().SessionState, Is.EqualTo(PerfMeterSessionState.Stopped));
			Assert.That(PerformanceMeter.GetSessionSummary().State, Is.EqualTo(PerfMeterSessionState.Stopped));
		}

		[Test]
		public void SessionCapturesConfiguredAndEffectiveRuntimeSettingsSeparately()
		{
			PerformanceMeter.EnsureRunning();
			PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.01f, 2));

			PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
			Assert.That(summary.ConfiguredSettings.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Overlay));
			Assert.That(summary.ConfiguredSettings.OverlayVisible, Is.True);
			Assert.That(summary.EffectiveSettings.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));
			Assert.That(summary.EffectiveSettings.OverlayVisible, Is.False);
			string json = PerfMeterSessionExporter.BuildJson(summary, System.Array.Empty<PerfMeterSessionSampleSnapshot>(), PerformanceMeter.GetStatus());
			Assert.That(json, Does.Contain("\"configured_settings\":{\"enabled\":true,\"auto_start\":true,\"collection_mode\":\"Overlay\",\"overlay_visible\":true"));
			Assert.That(json, Does.Contain("\"effective_settings\":{\"enabled\":true,\"auto_start\":true,\"collection_mode\":\"Background\",\"overlay_visible\":false"));

			PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
			summary = PerformanceMeter.GetSessionSummary();
			Assert.That(summary.EffectiveSettings.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));
			Assert.That(summary.EffectiveSettings.OverlayVisible, Is.False);
		}

		[Test]
		public void SessionRecorderUsesBoundedSampleStorage()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 2), default, default, settings, 10, 1d, CreateMetrics(10, 16d, PerfMeterBottleneck.Balanced));

			recorder.Update(CreateMetrics(11, 16d, PerfMeterBottleneck.GpuBound), 11, 1.01d);
			recorder.Update(CreateMetrics(12, 20d, PerfMeterBottleneck.CpuMainThreadBound), 12, 1.02d);
			recorder.Update(CreateMetrics(13, 25d, PerfMeterBottleneck.PresentLimited), 13, 1.03d);

			PerfMeterSessionSummarySnapshot summary = recorder.GetSummary();
			Assert.That(summary.SampleCount, Is.EqualTo(2));
			Assert.That(summary.DroppedSampleCount, Is.EqualTo(1));
			Assert.That(summary.FirstFrame, Is.EqualTo(11));
			Assert.That(summary.LastFrame, Is.EqualTo(12));
			Assert.That(summary.GpuBoundSampleCount, Is.EqualTo(1));
			Assert.That(summary.CpuMainThreadBoundSampleCount, Is.EqualTo(1));
			Assert.That(summary.Warning, Does.Contain("buffer is full"));
			Assert.That(summary.AverageFrameTimeMs, Is.EqualTo(18d).Within(0.001d));

			PerfMeterSessionSampleSnapshot[] samples = recorder.GetSamplesCopy();
			Assert.That(samples.Length, Is.EqualTo(2));
			samples[0] = default;
			Assert.That(recorder.GetSamplesCopy()[0].CollectionFrame, Is.EqualTo(11));
		}

		[Test]
		public void SessionSamplesCopyDoesNotShareCustomMetricArrays()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 2), default, default, settings, 10, 1d, CreateMetrics(10, 16d, PerfMeterBottleneck.Balanced));
			recorder.Update(CreateMetrics(11, 16d, PerfMeterBottleneck.GpuBound), 11, 1.01d, new[]
			{
				new PerfMeterCustomMetricSnapshot("custom.test", "Custom Test", "tests", "count", 3d)
			});

			PerfMeterSessionSampleSnapshot[] samples = recorder.GetSamplesCopy();
			samples[0].CustomMetrics[0] = new PerfMeterCustomMetricSnapshot("mutated", "Mutated", "tests", "count", 99d);

			Assert.That(recorder.GetSamplesCopy()[0].CustomMetrics[0].Id, Is.EqualTo("custom.test"));
		}

		[Test]
		public void SessionRecorderHonorsWarmupSecondsAndTracksWorstFrames()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;
			recorder.Start(new PerfMeterSessionOptions(1, 0.5f, 0.01f, 4, false, 0, 0f), default, default, settings, 10, 1d, CreateMetrics(10, 16d, PerfMeterBottleneck.Balanced));

			recorder.Update(CreateMetrics(11, 20d, PerfMeterBottleneck.CpuMainThreadBound), 11, 1.2d);
			recorder.Update(CreateMetrics(12, 18d, PerfMeterBottleneck.GpuBound), 12, 1.5d);
			recorder.Update(CreateMetrics(13, 30d, PerfMeterBottleneck.PresentLimited), 13, 1.6d);

			PerfMeterSessionSummarySnapshot summary = recorder.GetSummary();
			Assert.That(summary.SampleCount, Is.EqualTo(2));
			Assert.That(summary.FirstFrame, Is.EqualTo(12));
			Assert.That(summary.MaxFrameTimeMs, Is.EqualTo(30d).Within(0.001d));
			Assert.That(summary.WorstFrame.CollectionFrame, Is.EqualTo(13));
			Assert.That(summary.WorstFrame.Bottleneck, Is.EqualTo(PerfMeterBottleneck.PresentLimited));
			Assert.That(summary.WholeRun.SampleCount, Is.EqualTo(2));
			Assert.That(summary.CurrentScene.SampleCount, Is.EqualTo(2));
			Assert.That(summary.CurrentSceneWorstFrame.CollectionFrame, Is.EqualTo(13));
		}

		[Test]
		public void SessionRecorderResetStatsKeepsActiveSessionAndClearsSamples()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 4), default, default, settings, 10, 1d, CreateMetrics(10, 16d, PerfMeterBottleneck.Balanced));
			recorder.Update(CreateMetrics(11, 16d, PerfMeterBottleneck.GpuBound), 11, 1.01d);

			recorder.ResetStats(20, 2d, CreateMetrics(20, 12d, PerfMeterBottleneck.Balanced));
			recorder.Update(CreateMetrics(21, 14d, PerfMeterBottleneck.CpuRenderThreadBound), 21, 2.01d);

			PerfMeterSessionSummarySnapshot summary = recorder.GetSummary();
			Assert.That(summary.State, Is.EqualTo(PerfMeterSessionState.Recording));
			Assert.That(summary.SampleCount, Is.EqualTo(1));
			Assert.That(summary.FirstFrame, Is.EqualTo(21));
			Assert.That(summary.StartTimeSeconds, Is.EqualTo(2d).Within(0.001d));
			Assert.That(summary.CpuRenderThreadBoundSampleCount, Is.EqualTo(1));
		}

		[Test]
		public void SessionRecorderTracksFocusLossAndPauseTelemetry()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 4), default, default, settings, 10, 1d, CreateMetrics(10, 16d, PerfMeterBottleneck.Balanced));

			recorder.SetApplicationFocusState(false, false, 11, 1.25d);
			recorder.SetApplicationFocusState(false, true, 12, 1.50d);
			recorder.SetApplicationFocusState(true, true, 13, 1.75d);
			recorder.SetApplicationFocusState(true, false, 14, 2.00d);

			PerfMeterSessionSummarySnapshot summary = recorder.GetSummary();
			Assert.That(summary.FocusLossCount, Is.EqualTo(1));
			Assert.That(summary.PauseCount, Is.EqualTo(1));
			Assert.That(summary.FocusPausedDurationSeconds, Is.EqualTo(0.75d).Within(0.001d));

			recorder.SetApplicationFocusState(false, false, 15, 2.50d);
			summary = recorder.GetSummary();
			Assert.That(summary.FocusLossCount, Is.EqualTo(2));
			Assert.That(summary.FocusPausedDurationSeconds, Is.EqualTo(0.75d).Within(0.001d));

			recorder.Stop(3.00d);
			summary = recorder.GetSummary();
			Assert.That(summary.FocusPausedDurationSeconds, Is.EqualTo(1.25d).Within(0.001d));
		}

		[Test]
		public void SessionExportFormatsJsonAndCsv()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 2), PerformanceMeter.GetDeviceInfo(), default, settings, 10, 1d, CreateMetrics(10, 16d, PerfMeterBottleneck.Balanced));
			recorder.Update(CreateMetrics(11, 16d, PerfMeterBottleneck.GpuBound), 11, 1.01d, new[]
			{
				new PerfMeterCustomMetricSnapshot("combat.active_units", "Active Units", "combat", "count", 42d)
			});
			recorder.Stop(1.02d);

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			string json = PerfMeterSessionExporter.BuildJson(recorder.GetSummary(), recorder.GetSamplesCopy(), status);
			string csv = PerfMeterSessionExporter.BuildCsv(recorder.GetSummary(), recorder.GetSamplesCopy(), status);

			PackageInfo packageInfo = PackageInfo.FindForAssembly(typeof(PerformanceMeter).Assembly);
			string packageRoot = packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath)
				? packageInfo.resolvedPath
				: Path.Combine(Application.dataPath, "Scripts/SGG.PerfMeter");
			PackageManifest packageManifest = JsonUtility.FromJson<PackageManifest>(File.ReadAllText(Path.Combine(packageRoot, "package.json")));
			Assert.That(json, Does.Contain("\"schema_version\":2"));
			Assert.That(json, Does.Contain("\"package\":\"com.sungeargames.perfmeter\""));
			Assert.That(json, Does.Contain("\"package_version\":\"" + packageManifest.version + "\""));
			Assert.That(json, Does.Contain("\"package_version_source\":\"assembly_metadata\""));
			Assert.That(json, Does.Contain("\"summary\""));
			Assert.That(json, Does.Contain("\"metadata\""));
			Assert.That(json, Does.Contain("\"configured_settings\""));
			Assert.That(json, Does.Contain("\"effective_settings\""));
			Assert.That(json, Does.Contain("\"samples\""));
			Assert.That(json, Does.Contain("\"overlay_scale\""));
			Assert.That(json, Does.Contain("\"overdraw_default_frame_count\""));
			Assert.That(json, Does.Contain("\"alert_overdraw_ratio_threshold\""));
			Assert.That(json, Does.Contain("\"whole_run\""));
			Assert.That(json, Does.Contain("\"current_scene\""));
			Assert.That(json, Does.Contain("\"worst_frame\""));
			Assert.That(json, Does.Contain("\"cpu_frame_ms\":16"));
			Assert.That(json, Does.Contain("\"custom_metric_sample_count\":1"));
			Assert.That(json, Does.Contain("\"focus_loss_count\":0"));
			Assert.That(json, Does.Contain("\"application_focused\":"));
			Assert.That(json, Does.Contain("\"custom_metrics\""));
			Assert.That(json, Does.Contain("\"id\":\"combat.active_units\""));
			Assert.That(json, Does.Contain("\"value\":42"));
			Assert.That(csv, Does.StartWith("frame,time_seconds,scene,bottleneck,cpu_frame_ms"));
			Assert.That(csv, Does.Contain("GpuBound"));
			Assert.That(csv, Does.Contain("overdraw_ratio"));
			Assert.That(csv, Does.Contain("session_focus_loss_count"));
		}

		[Test]
		public void McpSessionExportRefusesExistingPathWithoutChangingArtifact()
		{
			string relativePath = "Temp/perfmeter-export-" + System.Guid.NewGuid().ToString("N") + ".json";
			string fullPath = Path.GetFullPath(relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

			try
			{
				PerformanceMeter.EnsureRunning();
				PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.01f, 2));
				string firstResult = PerfMeterMcpCommands.SessionExport("{\"path\":\"" + relativePath.Replace("\\", "/") + "\",\"format\":\"json\"}");
				byte[] firstArtifact = File.ReadAllBytes(fullPath);

				Assert.That(firstResult, Does.Contain("\"success\":true"));
				Assert.That(firstResult, Does.Contain("\"status\":\"exported\""));
				Assert.That(Encoding.UTF8.GetString(firstArtifact), Does.Contain("\"schema_version\":2"));

				string repeatedResult = PerfMeterMcpCommands.SessionExport("{\"path\":\"" + relativePath.Replace("\\", "/") + "\",\"format\":\"json\"}");
				Assert.That(repeatedResult, Does.Contain("\"success\":false"));
				Assert.That(repeatedResult, Does.Contain("\"error\":\"file_exists\""));
				Assert.That(repeatedResult, Does.Contain("\"status\":\"not_exported\""));
				Assert.That(File.ReadAllBytes(fullPath), Is.EqualTo(firstArtifact));
			}
			finally
			{
				if (File.Exists(fullPath))
				{
					File.Delete(fullPath);
				}
			}
		}

		[Test]
		public void RuntimeSessionExportAtomicallyReplacesExistingArtifact()
		{
			string path = Path.Combine(Application.temporaryCachePath, "perfmeter-export-" + System.Guid.NewGuid().ToString("N") + ".json");
			File.WriteAllText(path, "stale");

			try
			{
				Assert.That(PerformanceMeter.ExportSessionJson(path), Is.True);
				string artifact = File.ReadAllText(path);
				Assert.That(artifact, Does.StartWith("{\"schema_version\":2"));
				Assert.That(artifact, Does.Not.Contain("stale"));
			}
			finally
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
		}

		[Test]
		[Platform(Include = "Win")]
		public void RuntimeSessionExportLeavesExistingArtifactWhenAtomicReplacementFails()
		{
			string path = Path.Combine(Application.temporaryCachePath, "perfmeter-export-" + System.Guid.NewGuid().ToString("N") + ".json");
			File.WriteAllText(path, "stale");

			try
			{
				using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					Assert.That(PerformanceMeter.ExportSessionJson(path), Is.False);
				}

				Assert.That(File.ReadAllText(path), Is.EqualTo("stale"));
			}
			finally
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
		}

		[Test]
		public void CustomMetricProvidersRegisterUnregisterAndCollectSafely()
		{
			TestCustomMetricProvider provider = new TestCustomMetricProvider("game.wave", 7d);

			PerformanceMeter.RegisterCustomMetricProvider(provider);
			PerformanceMeter.RegisterCustomMetricProvider(provider);
			PerfMeterCustomMetricSnapshot[] metrics = PerformanceMeter.GetCustomMetrics();

			Assert.That(metrics.Length, Is.EqualTo(1));
			Assert.That(metrics[0].Id, Is.EqualTo("game.wave"));
			Assert.That(metrics[0].Name, Is.EqualTo("Wave"));
			Assert.That(metrics[0].Category, Is.EqualTo("gameplay"));
			Assert.That(metrics[0].Unit, Is.EqualTo("index"));
			Assert.That(metrics[0].Value, Is.EqualTo(7d));

			PerformanceMeter.UnregisterCustomMetricProvider(provider);
			Assert.That(PerformanceMeter.GetCustomMetrics(), Is.Empty);
		}

		[Test]
		public void CustomMetricProviderExceptionsReturnUnavailableSnapshot()
		{
			PerformanceMeter.RegisterCustomMetricProvider(new ThrowingCustomMetricProvider());

			PerfMeterCustomMetricSnapshot[] metrics = null;
			Assert.DoesNotThrow(() => metrics = PerformanceMeter.GetCustomMetrics());

			Assert.That(metrics, Is.Not.Null);
			Assert.That(metrics.Length, Is.EqualTo(1));
			Assert.That(metrics[0].Id, Is.EqualTo("broken.provider"));
			Assert.That(metrics[0].Available, Is.False);
			Assert.That(metrics[0].Warning, Does.Contain("InvalidOperationException"));
		}

		[Test]
		public void CpuCoreLoadsAreEmptyWithoutRuntime()
		{
			Assert.That(PerformanceMeter.GetCpuCoreLoads(), Is.Empty);
		}

		[Test]
		public void McpLatestMetricsIncludesCustomMetrics()
		{
			PerformanceMeter.RegisterCustomMetricProvider(new TestCustomMetricProvider("economy.gold", 123d));

			string json = PerfMeterMcpCommands.MetricsLatest();

			Assert.That(json, Does.Contain("\"custom_metrics\""));
			Assert.That(json, Does.Contain("\"id\":\"economy.gold\""));
			Assert.That(json, Does.Contain("\"value\":123"));
		}

		[Test]
		public void DeviceInfoIncludesRenderPipelineClassification()
		{
			PerfMeterDeviceSnapshot device = default;
			Assert.DoesNotThrow(() => device = PerformanceMeter.GetDeviceInfo());

			Assert.That(device.RenderPipeline, Is.Not.EqualTo((PerfMeterRenderPipelineKind)(-1)));
			Assert.That(device.RenderPipelineAssetName, Is.Not.Null);
			Assert.That(device.RenderPipelineAssetType, Is.Not.Null);
			Assert.That(device.RenderPipelineRuntimeType, Is.Not.Null);

			string json = PerfMeterMcpCommands.DeviceInfo();
			Assert.That(json, Does.Contain("\"render_pipeline\""));
			Assert.That(json, Does.Contain("\"render_pipeline_asset_name\""));
			Assert.That(json, Does.Contain("\"render_pipeline_runtime_type\""));
		}

		[Test]
		public void McpCameraSnapshotIncludesSrpCameraFields()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();

			Assert.That(metadata, Does.Contain("URP/HDRP camera settings"));

			string json = PerfMeterMcpCommands.CameraSnapshot("{}");
			Assert.That(json, Does.Contain("\"has_urp_additional_camera_data\""));
			Assert.That(json, Does.Contain("\"has_hdrp_additional_camera_data\""));
			Assert.That(json, Does.Contain("\"hdrp_antialiasing\""));
		}

		[Test]
		public void RenderGraphSnapshotBeforeFeatureRunsIsSafeDefault()
		{
			PerfMeterRenderGraphAnalytics.ResetForTests();

			PerfMeterRenderGraphSnapshot snapshot = default;
			Assert.DoesNotThrow(() => snapshot = PerformanceMeter.GetRenderGraphSnapshot());

			Assert.That(snapshot.IsAvailable, Is.False);
			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(snapshot.State, Is.EqualTo(PerfMeterRenderGraphState.NotObserved));
			Assert.That(snapshot.LastFrame, Is.EqualTo(-1));
			Assert.That(snapshot.RenderPipeline, Is.EqualTo(PerfMeterRenderPipelineKind.Unknown));
			Assert.That(snapshot.IntegrationName, Is.Empty);
			Assert.That(snapshot.ObservedInjectionPoint, Is.Empty);
			Assert.That(snapshot.RegisteredPassCount, Is.EqualTo(PerfMeterRenderGraphSnapshot.UnavailableCount));
			Assert.That(snapshot.Warning, Does.Contain("not recorded"));
			Assert.That(PerformanceMeter.TryGetRenderGraphSnapshot(out PerfMeterRenderGraphSnapshot trySnapshot), Is.False);
			Assert.That(trySnapshot.State, Is.EqualTo(PerfMeterRenderGraphState.NotObserved));
		}

		[Test]
		public void HdrpCustomPassSnapshotRecordsIntegrationMetadata()
		{
			PerfMeterRenderGraphAnalytics.ResetForTests();

			PerfMeterRenderGraphAnalytics.RecordHdrpCustomPassSnapshot("Main Camera", CameraType.Game.ToString(), "BeforePostProcess");

			PerfMeterRenderGraphSnapshot snapshot = PerformanceMeter.GetRenderGraphSnapshot();
			Assert.That(snapshot.IsAvailable, Is.True);
			Assert.That(snapshot.State, Is.EqualTo(PerfMeterRenderGraphState.Observed));
			Assert.That(snapshot.RenderPipeline, Is.EqualTo(PerfMeterRenderPipelineKind.HighDefinition));
			Assert.That(snapshot.IntegrationName, Is.EqualTo("HDRP Custom Pass"));
			Assert.That(snapshot.ObservedInjectionPoint, Is.EqualTo("BeforePostProcess"));
			Assert.That(snapshot.RegisteredPassCount, Is.EqualTo(PerfMeterRenderGraphSnapshot.UnavailableCount));
			Assert.That(PerformanceMeter.TryGetRenderGraphSnapshot(out PerfMeterRenderGraphSnapshot trySnapshot), Is.True);
			Assert.That(trySnapshot.RenderPipeline, Is.EqualTo(PerfMeterRenderPipelineKind.HighDefinition));
		}

		[Test]
		public void McpRenderGraphSnapshotExposesMetadataAndSafeDefault()
		{
			PerfMeterRenderGraphAnalytics.ResetForTests();
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();

			Assert.That(metadata, Does.Contain("perfmeter.rendergraph.snapshot"));
			Assert.That(metadata, Does.Contain("SGG.PerfMeter.Editor.Mcp.PerfMeterMcpCommands.RenderGraphSnapshot"));
			Assert.That(metadata, Does.Contain("HDRP Custom Pass"));

			string json = PerfMeterMcpCommands.RenderGraphSnapshot();
			Assert.That(json, Does.Contain("\"schema_version\":1"));
			Assert.That(json, Does.Contain("\"is_available\":false"));
			Assert.That(json, Does.Contain("\"state\":\"NotObserved\""));
			Assert.That(json, Does.Contain("\"render_pipeline\":\"Unknown\""));
			Assert.That(json, Does.Contain("\"integration_name\":\"\""));
			Assert.That(json, Does.Contain("\"observed_injection_point\":\"\""));
			Assert.That(json, Does.Contain("\"registered_pass_count\":-1"));
			Assert.That(json, Does.Contain("\"is_playing\""));
		}

		[Test]
		public void AlertEngineCompareCoversSupportedOperators()
		{
			Assert.That(PerfMeterAlertEngine.Compare(2d, PerfMeterComparison.GreaterThan, 1d), Is.True);
			Assert.That(PerfMeterAlertEngine.Compare(2d, PerfMeterComparison.GreaterThanOrEqual, 2d), Is.True);
			Assert.That(PerfMeterAlertEngine.Compare(1d, PerfMeterComparison.LessThan, 2d), Is.True);
			Assert.That(PerfMeterAlertEngine.Compare(2d, PerfMeterComparison.LessThanOrEqual, 2d), Is.True);
			Assert.That(PerfMeterAlertEngine.Compare(2d, PerfMeterComparison.Equal, 2d), Is.True);
			Assert.That(PerfMeterAlertEngine.Compare(2d, PerfMeterComparison.NotEqual, 3d), Is.True);
			Assert.That(PerfMeterAlertEngine.Compare(2d, PerfMeterComparison.GreaterThan, 2d), Is.False);
		}

		[Test]
		public void AlertEngineDefaultRulesUseSettingsTunables()
		{
			PerfMeterSettingsJson settingsJson = PerfMeterSettingsStore.CreateDefault();
			settingsJson.ruleDefaults.overdrawRatioThreshold = 2.25d;
			settingsJson.ruleDefaults.timingConsecutiveFrames = 6;
			settingsJson.ruleDefaults.fpsConsecutiveFrames = 21;
			settingsJson.ruleDefaults.gpuTimingUnavailableConsecutiveFrames = 17;
			settingsJson.ruleDefaults.overdrawConsecutiveFrames = 4;
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.ToSnapshot(settingsJson, PerfMeterSettingsLoadState.Loaded, string.Empty);

			PerfMeterRule[] rules = PerfMeterAlertEngine.CreateDefaultRules(PerfMeterTargetFps.Fps30, settings);

			Assert.That(FindRule(rules, "cpu.frame.over_budget").Threshold, Is.EqualTo(1000d / 30d).Within(0.001d));
			Assert.That(FindRule(rules, "cpu.frame.over_budget").ConsecutiveFrames, Is.EqualTo(6));
			Assert.That(FindRule(rules, "fps.below_target").Threshold, Is.EqualTo(30d));
			Assert.That(FindRule(rules, "fps.below_target").ConsecutiveFrames, Is.EqualTo(21));
			Assert.That(FindRule(rules, "gpu.timing.unavailable").ConsecutiveFrames, Is.EqualTo(17));
			Assert.That(FindRule(rules, "overdraw.ratio.high").Threshold, Is.EqualTo(2.25d).Within(0.001d));
			Assert.That(FindRule(rules, "overdraw.ratio.high").ConsecutiveFrames, Is.EqualTo(4));
		}

		[Test]
		public void AlertEngineHonorsConsecutiveFramesAndCallbackCooldown()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("cpu.test", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 10d, 2, 1f, PerfMeterAlertAction.Callback)
			});
			int callbackCount = 0;
			System.Action<PerfMeterAlertSnapshot> handler = alert => callbackCount++;
			PerformanceMeter.AlertFired += handler;

			try
			{
				engine.Evaluate(CreateMetrics(1, 20d, PerfMeterBottleneck.CpuMainThreadBound), 0d);
				Assert.That(engine.ActiveAlertCount, Is.EqualTo(0));

				engine.Evaluate(CreateMetrics(2, 20d, PerfMeterBottleneck.CpuMainThreadBound), 0.1d);
				Assert.That(engine.ActiveAlertCount, Is.EqualTo(1));
				Assert.That(engine.FiredAlertCount, Is.EqualTo(1));
				Assert.That(callbackCount, Is.EqualTo(1));

				engine.Evaluate(CreateMetrics(3, 20d, PerfMeterBottleneck.CpuMainThreadBound), 0.5d);
				Assert.That(engine.ActiveAlertCount, Is.EqualTo(1));
				Assert.That(engine.FiredAlertCount, Is.EqualTo(1));
				Assert.That(callbackCount, Is.EqualTo(1));

				engine.Evaluate(CreateMetrics(4, 20d, PerfMeterBottleneck.CpuMainThreadBound), 1.2d);
				Assert.That(engine.FiredAlertCount, Is.EqualTo(2));
				Assert.That(callbackCount, Is.EqualTo(2));

				engine.Evaluate(CreateMetrics(5, 5d, PerfMeterBottleneck.Balanced), 1.3d);
				Assert.That(engine.ActiveAlertCount, Is.EqualTo(0));
			}
			finally
			{
				PerformanceMeter.AlertFired -= handler;
			}
		}

		[Test]
		public void AlertEngineKeepsEditorWarningCooldownSeparate()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("editor.test", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 10d, 1, 0f, PerfMeterAlertAction.EditorWarning)
			});
			engine.ApplySettings(new PerfMeterSettingsSnapshot(
				true,
				true,
				PerfMeterCollectionMode.Overlay,
				true,
				PerfMeterOverlayCorner.TopRight,
				PerfMeterOverlayMode.Full,
				PerfMeterTargetFps.Fps60,
				PerfMeterSettingsStore.DefaultPresetId,
				PerfMeterOverlayModule.All,
				0,
				0f,
				0.25f,
				4096,
				false,
				0,
				0f,
				5f,
				0f,
				0f,
				PerfMeterSettingsLoadState.Loaded,
				string.Empty), PerfMeterTargetFps.Fps60);

			LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("\\[SGG PerfMeter Alert\\] editor.test"));
			engine.Evaluate(CreateMetrics(1, 20d, PerfMeterBottleneck.CpuMainThreadBound), 0d);
			engine.Evaluate(CreateMetrics(2, 20d, PerfMeterBottleneck.CpuMainThreadBound), 1d);
			LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("\\[SGG PerfMeter Alert\\] editor.test"));
			engine.Evaluate(CreateMetrics(3, 20d, PerfMeterBottleneck.CpuMainThreadBound), 5d);
			LogAssert.NoUnexpectedReceived();
			Assert.That(engine.FiredAlertCount, Is.EqualTo(2));
		}

		[Test]
		public void AlertEngineSuppressesEditorWarningsWhenDisabled()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("editor.disabled", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 10d, 1, 0f, PerfMeterAlertAction.EditorWarning)
			});
			engine.ApplySettings(new PerfMeterSettingsSnapshot(
				true,
				true,
				PerfMeterCollectionMode.Overlay,
				true,
				PerfMeterOverlayCorner.TopRight,
				PerfMeterOverlayMode.Full,
				PerfMeterTargetFps.Fps60,
				PerfMeterSettingsStore.DefaultPresetId,
				PerfMeterOverlayModule.All,
				0,
				0f,
				0.25f,
				4096,
				false,
				0,
				0f,
				5f,
				0f,
				0f,
				PerfMeterSettingsLoadState.Loaded,
				string.Empty,
				editorWarningsEnabled: false), PerfMeterTargetFps.Fps60);

			engine.Evaluate(CreateMetrics(1, 20d, PerfMeterBottleneck.CpuMainThreadBound), 0d);

			LogAssert.NoUnexpectedReceived();
			Assert.That(engine.ActiveAlertCount, Is.EqualTo(1));
			Assert.That(engine.FiredAlertCount, Is.EqualTo(0));
		}

		[Test]
		public void AlertEngineClearAlertsResetsStateAndCounters()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("clear.test", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 10d, 1, 10f, PerfMeterAlertAction.Callback)
			});
			int callbackCount = 0;
			System.Action<PerfMeterAlertSnapshot> handler = alert => callbackCount++;
			PerformanceMeter.AlertFired += handler;

			try
			{
				engine.Evaluate(CreateMetrics(1, 20d, PerfMeterBottleneck.CpuMainThreadBound), 0d);
				Assert.That(engine.ActiveAlertCount, Is.EqualTo(1));
				Assert.That(engine.FiredAlertCount, Is.EqualTo(1));

				engine.Clear();
				Assert.That(engine.ActiveAlertCount, Is.EqualTo(0));
				Assert.That(engine.FiredAlertCount, Is.EqualTo(0));
				Assert.That(string.IsNullOrEmpty(engine.LatestAlert.RuleId), Is.True);

				engine.Evaluate(CreateMetrics(2, 20d, PerfMeterBottleneck.CpuMainThreadBound), 1d);
				Assert.That(engine.FiredAlertCount, Is.EqualTo(1));
				Assert.That(callbackCount, Is.EqualTo(2));
			}
			finally
			{
				PerformanceMeter.AlertFired -= handler;
			}
		}

		[Test]
		public void McpSessionCommandsExposeMetadataAndBasicOutput()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();
			Assert.That(metadata, Does.Contain("perfmeter.runtime.reset_stats"));
			Assert.That(metadata, Does.Contain("perfmeter.runtime.mode.set"));
			Assert.That(metadata, Does.Contain("perfmeter.session.start"));
			Assert.That(metadata, Does.Contain("perfmeter.session.stop"));
			Assert.That(metadata, Does.Contain("perfmeter.session.summary"));
			Assert.That(metadata, Does.Contain("perfmeter.session.export"));

			string resetJson = PerfMeterMcpCommands.RuntimeResetStats();
			Assert.That(resetJson, Does.Contain("\"state\""));
			Assert.That(resetJson, Does.Contain("\"application_focused\""));
			string modeJson = PerfMeterMcpCommands.RuntimeModeSet("{\"mode\":\"Background\"}");
			Assert.That(modeJson, Does.Contain("\"collection_mode\":\"Background\""));

			string startJson = PerfMeterMcpCommands.SessionStart("{\"warmup_frames\":0,\"warmup_seconds\":0,\"sample_interval_seconds\":0.01,\"max_samples\":2,\"reset_on_scene_load\":true,\"scene_load_ignore_frames\":1,\"scene_load_ignore_seconds\":0}");
			Assert.That(startJson, Does.Contain("\"success\":true"));
			Assert.That(startJson, Does.Contain("\"status\":\"recording\""));
			Assert.That(startJson, Does.Contain("\"max_samples\":2"));
			Assert.That(startJson, Does.Contain("\"reset_on_scene_load\":true"));
			Assert.That(startJson, Does.Contain("\"scene_load_ignore_frames\":1"));

			string summaryJson = PerfMeterMcpCommands.SessionSummary();
			Assert.That(summaryJson, Does.Contain("\"summary\""));
			Assert.That(summaryJson, Does.Contain("\"state\":\"Recording\""));
			Assert.That(summaryJson, Does.Contain("\"whole_run\""));
			Assert.That(summaryJson, Does.Contain("\"focus_loss_count\""));
			Assert.That(summaryJson, Does.Contain("\"focus_paused_duration_seconds\""));

			string stopJson = PerfMeterMcpCommands.SessionStop();
			Assert.That(stopJson, Does.Contain("\"status\":\"stopped\""));
		}

		[Test]
		public void McpAlertCommandsExposeMetadataAndBasicOutput()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();
			Assert.That(metadata, Does.Contain("perfmeter.alerts.latest"));
			Assert.That(metadata, Does.Contain("perfmeter.alerts.clear"));

			string latestJson = PerfMeterMcpCommands.AlertsLatest();
			Assert.That(latestJson, Does.Contain("\"alerts\""));
			Assert.That(latestJson, Does.Contain("\"active_alert_count\":0"));
			Assert.That(latestJson, Does.Contain("\"is_playing\""));

			string clearJson = PerfMeterMcpCommands.AlertsClear();
			Assert.That(clearJson, Does.Contain("\"cleared\":true"));
			Assert.That(clearJson, Does.Contain("\"fired_alert_count\":0"));
		}

		private static PerfMeterMetricsSnapshot CreateMetrics(int frame, double frameTimeMs, PerfMeterBottleneck bottleneck)
		{
			return new PerfMeterMetricsSnapshot(
				PerfMeterRuntimeState.Running,
				PerfMeterAvailability.Available,
				frame,
				bottleneck,
				1000d / 60d,
				true,
				frameTimeMs,
				frameTimeMs * 0.5d,
				frameTimeMs * 0.25d,
				0d,
				frameTimeMs,
				1,
				1,
				1,
				1,
				0,
				0,
				0L,
				0L,
				0L,
				0L,
				0d);
		}

		[System.Serializable]
		private sealed class PackageManifest
		{
			public string version;
		}

		private static PerfMeterRule FindRule(PerfMeterRule[] rules, string id)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				if (rules[i].Id == id)
				{
					return rules[i];
				}
			}

			Assert.Fail("Rule not found: " + id);
			return default;
		}

		private static void AssertHasModule(PerfMeterOverlayModule actual, PerfMeterOverlayModule expected)
		{
			Assert.That((actual & expected) == expected, Is.True);
		}

		private static void AssertDoesNotHaveModule(PerfMeterOverlayModule actual, PerfMeterOverlayModule expected)
		{
			Assert.That((actual & expected) == 0, Is.True);
		}

		private sealed class TestCustomMetricProvider : IPerfMeterCustomMetricProvider
		{
			private readonly string _id;
			private readonly double _value;

			public TestCustomMetricProvider(string id, double value)
			{
				_id = id;
				_value = value;
			}

			public string Id => _id;

			public bool TryCollect(out PerfMeterCustomMetricSnapshot metric)
			{
				metric = new PerfMeterCustomMetricSnapshot(_id, "Wave", "gameplay", "index", _value);
				return true;
			}
		}

		private sealed class ThrowingCustomMetricProvider : IPerfMeterCustomMetricProvider
		{
			public string Id => "broken.provider";

			public bool TryCollect(out PerfMeterCustomMetricSnapshot metric)
			{
				metric = default;
				throw new System.InvalidOperationException("Provider failed.");
			}
		}
	}
}
