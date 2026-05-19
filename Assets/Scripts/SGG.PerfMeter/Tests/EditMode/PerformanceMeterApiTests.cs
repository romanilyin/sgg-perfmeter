using System.IO;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using UnityEngine;
using UnityEngine.TestTools;

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
			Assert.That(status.OverdrawHeatmapVisible, Is.False);
			Assert.That(status.SessionState, Is.EqualTo(PerfMeterSessionState.Idle));
			Assert.That(status.IsSessionRecording, Is.False);
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
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
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
			Assert.That(PerformanceMeter.GetStatus().OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));

			Assert.DoesNotThrow(() => PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.GetStatus().TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.GetLatestMetrics().FrameBudgetMs, Is.EqualTo(1000d / 120d).Within(0.001d));

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverdrawHeatmapVisible(true));
			Assert.That(PerformanceMeter.IsOverdrawHeatmapVisible, Is.True);
			Assert.That(PerformanceMeter.GetStatus().OverdrawHeatmapVisible, Is.True);

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverdrawHeatmapVisible(false));
			Assert.That(PerformanceMeter.IsOverdrawHeatmapVisible, Is.False);
			Assert.That(PerformanceMeter.GetStatus().OverdrawHeatmapVisible, Is.False);

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
		public void SessionExportFormatsJsonAndCsv()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 2), PerformanceMeter.GetDeviceInfo(), default, settings, 10, 1d, CreateMetrics(10, 16d, PerfMeterBottleneck.Balanced));
			recorder.Update(CreateMetrics(11, 16d, PerfMeterBottleneck.GpuBound), 11, 1.01d);
			recorder.Stop(1.02d);

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			string json = PerfMeterSessionExporter.BuildJson(recorder.GetSummary(), recorder.GetSamplesCopy(), status);
			string csv = PerfMeterSessionExporter.BuildCsv(recorder.GetSummary(), recorder.GetSamplesCopy(), status);

			Assert.That(json, Does.Contain("\"package\":\"com.sungeargames.perfmeter\""));
			Assert.That(json, Does.Contain("\"summary\""));
			Assert.That(json, Does.Contain("\"metadata\""));
			Assert.That(json, Does.Contain("\"samples\""));
			Assert.That(json, Does.Contain("\"cpu_frame_ms\":16"));
			Assert.That(csv, Does.StartWith("frame,time_seconds,scene,bottleneck,cpu_frame_ms"));
			Assert.That(csv, Does.Contain("GpuBound"));
			Assert.That(csv, Does.Contain("overdraw_ratio"));
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
				true,
				PerfMeterOverlayCorner.TopRight,
				PerfMeterOverlayMode.Full,
				PerfMeterTargetFps.Fps60,
				PerfMeterSettingsStore.DefaultPresetId,
				PerfMeterOverlayModule.All,
				0,
				0.25f,
				4096,
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
			string metadataPath = Path.Combine(Application.dataPath, "Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json");
			string metadata = File.ReadAllText(metadataPath);
			Assert.That(metadata, Does.Contain("perfmeter.session.start"));
			Assert.That(metadata, Does.Contain("perfmeter.session.stop"));
			Assert.That(metadata, Does.Contain("perfmeter.session.summary"));
			Assert.That(metadata, Does.Contain("perfmeter.session.export"));

			string startJson = PerfMeterMcpCommands.SessionStart("{\"warmup_frames\":0,\"sample_interval_seconds\":0.01,\"max_samples\":2}");
			Assert.That(startJson, Does.Contain("\"success\":true"));
			Assert.That(startJson, Does.Contain("\"status\":\"recording\""));
			Assert.That(startJson, Does.Contain("\"max_samples\":2"));

			string summaryJson = PerfMeterMcpCommands.SessionSummary();
			Assert.That(summaryJson, Does.Contain("\"summary\""));
			Assert.That(summaryJson, Does.Contain("\"state\":\"Recording\""));

			string stopJson = PerfMeterMcpCommands.SessionStop();
			Assert.That(stopJson, Does.Contain("\"status\":\"stopped\""));
		}

		[Test]
		public void McpAlertCommandsExposeMetadataAndBasicOutput()
		{
			string metadataPath = Path.Combine(Application.dataPath, "Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json");
			string metadata = File.ReadAllText(metadataPath);
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
	}
}
