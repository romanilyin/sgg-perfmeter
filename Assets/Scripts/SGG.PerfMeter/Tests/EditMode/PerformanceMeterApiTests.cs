using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerformanceMeterApiTests
	{
		private const string HasMcpOverlayVisibilityKey = "SGG.PerfMeter.Mcp.OverlayVisibility.HasValue";
		private const string McpOverlayVisibilityKey = "SGG.PerfMeter.Mcp.OverlayVisibility.Value";
		private bool _hadMcpOverlayVisibility;
		private bool _mcpOverlayVisibility;

		[SetUp]
		public void SetUp()
		{
			_hadMcpOverlayVisibility = UnityEditor.SessionState.GetBool(HasMcpOverlayVisibilityKey, false);
			_mcpOverlayVisibility = UnityEditor.SessionState.GetBool(McpOverlayVisibilityKey, false);
			PerformanceMeter.ClearCustomMetricProviders();
			PerformanceMeter.Stop();
			PerfMeterSettingsBootstrap.ResetExplicitSettingsApplication();
			PerfMeterRenderGraphAnalytics.ResetForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterSettingsBootstrap.ResetExplicitSettingsApplication();
			PerformanceMeter.ClearCustomMetricProviders();
			PerfMeterRenderGraphAnalytics.ResetForTests();
			if (_hadMcpOverlayVisibility)
			{
				UnityEditor.SessionState.SetBool(HasMcpOverlayVisibilityKey, true);
				UnityEditor.SessionState.SetBool(McpOverlayVisibilityKey, _mcpOverlayVisibility);
			}
			else
			{
				UnityEditor.SessionState.EraseBool(HasMcpOverlayVisibilityKey);
				UnityEditor.SessionState.EraseBool(McpOverlayVisibilityKey);
			}
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
			AssertDoesNotHaveModule(status.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
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
			AssertDoesNotHaveModule(status.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
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
		public void DiagnosticsQueryBeforeStartIsExplicitlyUnavailable()
		{
			PerfMeterDiagnosticsSnapshot diagnostics = PerformanceMeter.GetDiagnostics();

			Assert.That(diagnostics.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(diagnostics.StableBottleneck, Is.EqualTo(PerfMeterBottleneck.Unknown));
			Assert.That(diagnostics.Freshness, Is.EqualTo(PerfMeterDiagnosticEvidenceFreshness.Unknown));
			Assert.That(diagnostics.RawWarning, Does.Contain("not running"));
		}

		[Test]
		public void TypedCollectionModeMutationReportsNormalizationAndUnavailableRuntime()
		{
			PerfMeterMutationResultSnapshot normalized = PerformanceMeter.TrySetCollectionMode((PerfMeterCollectionMode)999);

			Assert.That(normalized.Succeeded, Is.True);
			Assert.That(normalized.Status, Is.EqualTo(PerfMeterMutationStatus.Normalized));
			Assert.That(normalized.Reason, Is.EqualTo(PerfMeterMutationReason.ValueNormalized));
			Assert.That(normalized.RequestedValue, Is.EqualTo("999"));
			Assert.That(normalized.EffectiveValue, Is.EqualTo(nameof(PerfMeterCollectionMode.Overlay)));

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			runtime.enabled = false;
			try
			{
				PerfMeterMutationResultSnapshot unavailable = PerformanceMeter.TrySetCollectionMode(PerfMeterCollectionMode.Background);
				Assert.That(unavailable.Succeeded, Is.False);
				Assert.That(unavailable.Status, Is.EqualTo(PerfMeterMutationStatus.Unavailable));
				Assert.That(unavailable.Reason, Is.EqualTo(PerfMeterMutationReason.RuntimeUnavailable));
				Assert.That(unavailable.EffectiveValue, Is.EqualTo(nameof(PerfMeterCollectionMode.Stopped)));
			}
			finally
			{
				runtime.enabled = true;
			}
		}

		[Test]
		public void OverlayApiIsSafeInEditMode()
		{
			Assert.That(PerformanceMeter.IsOverlayVisible, Is.False);
			Assert.That(PerformanceMeter.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.TopRight));
			Assert.That(PerformanceMeter.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(PerformanceMeter.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.MetricBars));
			AssertDoesNotHaveModule(PerformanceMeter.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
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

			bool heatmapSupported = PerfMeterRenderPipelineDetector.GetActiveKind() != PerfMeterRenderPipelineKind.HighDefinition;
			Assert.DoesNotThrow(() => PerformanceMeter.SetOverdrawHeatmapVisible(true));
			Assert.That(PerformanceMeter.IsOverdrawHeatmapVisible, Is.EqualTo(heatmapSupported));
			Assert.That(PerformanceMeter.GetStatus().OverdrawHeatmapVisible, Is.EqualTo(heatmapSupported));

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverdrawHeatmapVisible(false));
			Assert.That(PerformanceMeter.IsOverdrawHeatmapVisible, Is.False);
			Assert.That(PerformanceMeter.GetStatus().OverdrawHeatmapVisible, Is.False);

			Assert.DoesNotThrow(() => PerformanceMeter.SetOverlayVisible(false));
			Assert.That(PerformanceMeter.GetStatus().OverlayVisible, Is.False);

			Assert.DoesNotThrow(() => PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Stopped));
			Assert.That(PerformanceMeter.GetStatus().CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Stopped));
		}

		[Test]
		public void StructuredLogApiDefaultsEnabledAndTogglesRuntimeState()
		{
			Assert.That(PerformanceMeter.GetSettings().StructuredLogsEnabled, Is.True);
			Assert.That(PerformanceMeter.StructuredLogsEnabled, Is.True);

			Assert.DoesNotThrow(() => PerformanceMeter.SetStructuredLogsEnabled(false));
			Assert.That(PerformanceMeter.StructuredLogsEnabled, Is.False);

			Assert.DoesNotThrow(() => PerformanceMeter.SetStructuredLogsEnabled(true));
			Assert.That(PerformanceMeter.StructuredLogsEnabled, Is.True);

			PerformanceMeter.Stop();
			Assert.That(PerformanceMeter.StructuredLogsEnabled, Is.True);
		}

		[Test]
		public void ApplySettingsUsesStructuredLogSettingAndStopRestoresApiFallback()
		{
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.WithStructuredLogsEnabled(PerfMeterSettingsStore.Defaults, false);

			PerformanceMeter.ApplySettings(settings);

			Assert.That(PerformanceMeter.StructuredLogsEnabled, Is.False);
			Assert.That(PerformanceMeter.GetSettings().StructuredLogsEnabled, Is.True);

			PerformanceMeter.Stop();
			Assert.That(PerformanceMeter.StructuredLogsEnabled, Is.True);
		}

		[Test]
		public void TryApplySettingsJsonAppliesNormalizedRuntimeSettings()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.collectionMode = nameof(PerfMeterCollectionMode.Background);
			settings.targetFps = (int)PerfMeterTargetFps.Fps120;
			settings.overlay.refreshIntervalSeconds = 0.5f;
			settings.overlay.graphHistoryLength = 64;
			settings.ruleDefaults.editorWarningsEnabled = false;
			settings.ruleDefaults.structuredLogsEnabled = false;
			settings.ruleDefaults.editorWarningCooldownSeconds = 17f;
			settings.session.warmupFrames = 7;
			settings.session.warmupSeconds = 1.5f;
			settings.session.sampleIntervalSeconds = 0.75f;
			settings.session.maxSamples = 23;
			settings.overdraw.defaultFrameCount = 9;
			settings.overdraw.maxFrameCount = 12;

			bool applied = PerformanceMeter.TryApplySettingsJson(PerfMeterSettingsStore.ToJson(settings), out string warning);

			Assert.That(applied, Is.True);
			Assert.That(warning, Is.Empty);
			Assert.That(PerformanceMeter.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.EditorWarningLogsEnabled, Is.False);
			Assert.That(PerformanceMeter.StructuredLogsEnabled, Is.False);

			PerformanceMeter.StartSession();
			PerfMeterSessionOptions sessionOptions = PerformanceMeter.GetSessionSummary().Options;
			Assert.That(sessionOptions.WarmupFrames, Is.EqualTo(7));
			Assert.That(sessionOptions.WarmupSeconds, Is.EqualTo(1.5f).Within(0.0001f));
			Assert.That(sessionOptions.SampleIntervalSeconds, Is.EqualTo(0.75f).Within(0.0001f));
			Assert.That(sessionOptions.MaxSamples, Is.EqualTo(23));
			PerformanceMeter.StopSession();

			PerformanceMeter.RequestOverdrawMeasurement();
			Assert.That(PerfMeterRuntime.Instance.OverdrawRequestedFrameCount, Is.EqualTo(9));
			PerformanceMeter.CancelOverdrawMeasurement();
		}

		[Test]
		public void TryApplySettingsJsonRejectsInvalidJsonWithoutStartingRuntime()
		{
			bool applied = PerformanceMeter.TryApplySettingsJson("{not-json", out string warning);

			Assert.That(applied, Is.False);
			Assert.That(warning, Is.Not.Empty);
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void TryApplySettingsJsonRejectsInvalidJsonWithoutMutatingRunningRuntime()
		{
			PerformanceMeter.EnsureRunning();
			PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps120);
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;

			bool applied = PerformanceMeter.TryApplySettingsJson("{not-json", out string warning);

			Assert.That(applied, Is.False);
			Assert.That(warning, Is.Not.Empty);
			Assert.That(PerfMeterRuntime.Instance, Is.SameAs(runtime));
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
		}

		[Test]
		public void ExplicitSettingsSuppressResourcesAutoStartForCurrentDomain()
		{
			PerfMeterSettingsSnapshot loaded = PerfMeterSettingsStore.ToSnapshot(
				PerfMeterSettingsStore.CreateDefault(),
				PerfMeterSettingsLoadState.Loaded,
				string.Empty);
			Assert.That(PerfMeterSettingsBootstrap.ShouldAutoStartFromSettings(loaded), Is.True);

			Assert.That(PerformanceMeter.TryApplySettingsJson(PerfMeterSettingsStore.ToJson(PerfMeterSettingsStore.CreateDefault()), out _), Is.True);

			Assert.That(PerfMeterSettingsBootstrap.ShouldAutoStartFromSettings(loaded), Is.False);
		}

		[Test]
		public void TryApplySettingsJsonHonorsDisabledSetting()
		{
			PerformanceMeter.EnsureRunning();
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.enabled = false;

			Assert.That(PerformanceMeter.TryApplySettingsJson(PerfMeterSettingsStore.ToJson(settings), out string warning), Is.True);
			Assert.That(warning, Is.Empty);
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void TryApplySettingsJsonDoesNotSuppressResourcesWhenRuntimeCannotApply()
		{
			PerformanceMeter.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			runtime.enabled = false;
			PerfMeterSettingsSnapshot loaded = PerfMeterSettingsStore.ToSnapshot(
				PerfMeterSettingsStore.CreateDefault(),
				PerfMeterSettingsLoadState.Loaded,
				string.Empty);

			try
			{
				bool applied = PerformanceMeter.TryApplySettingsJson(PerfMeterSettingsStore.ToJson(PerfMeterSettingsStore.CreateDefault()), out string warning);

				Assert.That(applied, Is.False);
				Assert.That(warning, Does.Contain("could not be applied"));
				Assert.That(PerfMeterSettingsBootstrap.ShouldAutoStartFromSettings(loaded), Is.True);
			}
			finally
			{
				runtime.enabled = true;
			}
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

			PerfMeterMutationResultSnapshot requestResult = PerformanceMeter.TryRequestOverdrawMeasurement(2);
			PerfMeterStatusSnapshot measuringStatus = PerformanceMeter.GetStatus();
			PerfMeterMetricsSnapshot measuringMetrics = PerformanceMeter.GetLatestMetrics();
			bool measurementAccepted = measuringStatus.OverdrawState == PerfMeterOverdrawMeasurementState.Measuring ||
				measuringStatus.OverdrawState == PerfMeterOverdrawMeasurementState.Unsupported;
			Assert.That(measurementAccepted, Is.True);
			Assert.That(measuringMetrics.OverdrawState, Is.EqualTo(measuringStatus.OverdrawState));
			Assert.That(measuringMetrics.OverdrawRatio, Is.EqualTo(0d));
			Assert.That(requestResult.Status, Is.EqualTo(
				measuringStatus.OverdrawState == PerfMeterOverdrawMeasurementState.Unsupported
					? PerfMeterMutationStatus.Unsupported
					: PerfMeterMutationStatus.Applied));
			Assert.That(requestResult.RequestedValue, Is.EqualTo("2"));
			string normalizedMcpJson = PerfMeterMcpCommands.OverdrawStart("{\"frame_count\":0}");
			Assert.That(normalizedMcpJson, Does.Contain("\"operation\":\"overdraw_start\""));
			Assert.That(normalizedMcpJson, Does.Contain("\"result\":\"Normalized\""));
			Assert.That(normalizedMcpJson, Does.Contain("\"requested\":\"0\""));
			Assert.That(normalizedMcpJson, Does.Contain("\"effective\":\"" + PerformanceMeter.GetSettings().OverdrawDefaultFrameCount + "\""));

			PerfMeterMutationResultSnapshot cancelResult = PerformanceMeter.TryCancelOverdrawMeasurement();
			Assert.That(PerformanceMeter.GetStatus().OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Canceled));
			Assert.That(cancelResult.Status, Is.EqualTo(PerfMeterMutationStatus.Applied));
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
			Assert.That(PerfMeterProfilerInstrumentation.SessionState, Is.EqualTo((int)PerfMeterSessionState.Recording));
			Assert.That(PerformanceMeter.GetStatus().SessionState, Is.EqualTo(PerfMeterSessionState.Recording));
			PerfMeterSessionSummarySnapshot recordingSummary = PerformanceMeter.GetSessionSummary();
			Assert.That(recordingSummary.State, Is.EqualTo(PerfMeterSessionState.Recording));
			Assert.That(recordingSummary.Options.MaxSamples, Is.EqualTo(2));
			Assert.That(recordingSummary.Settings.SessionMaxSamples, Is.GreaterThanOrEqualTo(1));
			Assert.That(recordingSummary.Device.UnityVersion, Is.Not.Empty);

			PerformanceMeter.StopSession();

			Assert.That(PerformanceMeter.IsSessionRecording, Is.False);
			Assert.That(PerfMeterProfilerInstrumentation.SessionState, Is.EqualTo((int)PerfMeterSessionState.Stopped));
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
			Assert.That(csv.Split('\n')[0].TrimEnd('\r'), Does.EndWith(",session_id"));
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
				string firstJson = Encoding.UTF8.GetString(firstArtifact);
				Assert.That(firstJson, Does.Contain("\"schema_version\":2"));
				Assert.That(firstJson, Does.Contain("\"timeline\":"));

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
			Assert.That(PerfMeterProfilerInstrumentation.CustomMetricCount, Is.EqualTo(1));

			PerformanceMeter.UnregisterCustomMetricProvider(provider);
			Assert.That(PerfMeterProfilerInstrumentation.CustomMetricCount, Is.Zero);
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
		public void CustomMetricCollectionReusesBufferAndDoesNotExposeStaleCapacity()
		{
			TestCustomMetricProvider reportingProvider = new TestCustomMetricProvider("reported.metric", 7d);
			NonReportingCustomMetricProvider nonReportingProvider = new NonReportingCustomMetricProvider("stale.metric");
			PerformanceMeter.RegisterCustomMetricProvider(reportingProvider);
			PerformanceMeter.RegisterCustomMetricProvider(nonReportingProvider);

			PerfMeterCustomMetricCollection first = PerfMeterCustomMetricRegistry.Collect();
			first.Buffer[1] = new PerfMeterCustomMetricSnapshot("stale.metric", "Stale", "tests", "count", 99d);
			PerfMeterCustomMetricCollection second = PerfMeterCustomMetricRegistry.Collect();

			Assert.That(second.Buffer, Is.SameAs(first.Buffer));
			Assert.That(second.Buffer.Length, Is.EqualTo(2));
			Assert.That(second.Count, Is.EqualTo(1));

			PerfMeterCustomMetricSnapshot[] publicMetrics = PerformanceMeter.GetCustomMetrics();
			Assert.That(publicMetrics, Has.Length.EqualTo(1));
			Assert.That(publicMetrics[0].Id, Is.EqualTo("reported.metric"));
		}

		[Test]
		public void WarmedCustomMetricCollectionDoesNotAllocate()
		{
			PerformanceMeter.RegisterCustomMetricProvider(new TestCustomMetricProvider("allocation.metric", 7d));
			PerfMeterCustomMetricRegistry.Collect();
			System.GC.Collect();
			System.GC.WaitForPendingFinalizers();
			System.GC.Collect();

			int lastCount = 0;
			long before = System.GC.GetAllocatedBytesForCurrentThread();
			for (int iteration = 0; iteration < 1000; iteration++)
			{
				PerfMeterCustomMetricCollection metrics = PerfMeterCustomMetricRegistry.Collect();
				lastCount = metrics.Count;
			}
			long allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - before;

			Assert.That(lastCount, Is.EqualTo(1));
			Assert.That(allocatedBytes, Is.Zero);
		}

		[Test]
		public void CustomMetricProviderIdentityIsCachedUntilLifecycleChange()
		{
			CountingIdCustomMetricProvider provider = new CountingIdCustomMetricProvider("cached.metric", 5d);
			PerformanceMeter.RegisterCustomMetricProvider(provider);
			int idReadsAfterRegister = provider.IdReadCount;

			PerfMeterCustomMetricRegistry.Collect();
			PerfMeterCustomMetricRegistry.Collect();

			Assert.That(idReadsAfterRegister, Is.EqualTo(1));
			Assert.That(provider.IdReadCount, Is.EqualTo(idReadsAfterRegister));
		}

		[Test]
		public void CollectedEmptyCustomMetricSnapshotDoesNotReinvokeProvidersOnRead()
		{
			NonReportingCustomMetricProvider provider = new NonReportingCustomMetricProvider("empty.metric");
			PerformanceMeter.RegisterCustomMetricProvider(provider);
			PerformanceMeter.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			Assert.That(runtime, Is.Not.Null);
			System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
			typeof(PerfMeterRuntime).GetField("_latestCustomMetricBuffer", flags).SetValue(runtime, System.Array.Empty<PerfMeterCustomMetricSnapshot>());
			typeof(PerfMeterRuntime).GetField("_latestCustomMetricCount", flags).SetValue(runtime, 0);
			typeof(PerfMeterRuntime).GetField("_hasLatestCustomMetrics", flags).SetValue(runtime, true);

			Assert.That(PerformanceMeter.GetCustomMetrics(), Is.Empty);
			Assert.That(runtime.PeekLatestCustomMetrics(out int count), Is.Empty);
			Assert.That(count, Is.Zero);
			Assert.That(provider.CollectCount, Is.Zero);

			System.Reflection.FieldInfo bufferField = typeof(PerfMeterRuntime).GetField("_latestCustomMetricBuffer", flags);
			System.Reflection.FieldInfo countField = typeof(PerfMeterRuntime).GetField("_latestCustomMetricCount", flags);
			System.Reflection.FieldInfo initializedField = typeof(PerfMeterRuntime).GetField("_hasLatestCustomMetrics", flags);
			bufferField.SetValue(runtime, new[] { new PerfMeterCustomMetricSnapshot("stale.metric", "Stale", "tests", "count", 1d) });
			countField.SetValue(runtime, 1);
			initializedField.SetValue(runtime, true);
			PerformanceMeter.ClearCustomMetricProviders();
			Assert.That(PerformanceMeter.GetCustomMetrics(), Is.Empty);

			initializedField.SetValue(runtime, true);
			typeof(PerfMeterRuntime).GetMethod("OnDisable", flags).Invoke(runtime, null);
			Assert.That((bool)initializedField.GetValue(runtime), Is.False);
		}

		[Test]
		public void SessionRecorderCopiesOnlyReportedCustomMetricCount()
		{
			PerfMeterCustomMetricSnapshot[] buffer = new PerfMeterCustomMetricSnapshot[2];
			buffer[0] = new PerfMeterCustomMetricSnapshot("reported.metric", "Reported", "tests", "count", 7d);
			buffer[1] = new PerfMeterCustomMetricSnapshot("stale.metric", "Stale", "tests", "count", 99d);
			PerfMeterCustomMetricCollection collection = new PerfMeterCustomMetricCollection(buffer, 1);
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 2), default, default, PerfMeterSettingsStore.Defaults, 10, 1d, CreateMetrics(10, 16d, PerfMeterBottleneck.Balanced));

			recorder.Update(CreateMetrics(11, 16d, PerfMeterBottleneck.GpuBound), 11, 1.01d, collection, PerfMeterPlatformTelemetrySnapshot.Unavailable());
			buffer[0] = new PerfMeterCustomMetricSnapshot("mutated.metric", "Mutated", "tests", "count", 100d);

			PerfMeterSessionSampleSnapshot[] samples = recorder.GetSamplesCopy();
			Assert.That(samples, Has.Length.EqualTo(1));
			Assert.That(samples[0].CustomMetrics, Has.Length.EqualTo(1));
			Assert.That(samples[0].CustomMetrics[0].Id, Is.EqualTo("reported.metric"));
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
			Assert.That(json, Does.Contain("\"diagnostics\""));
			Assert.That(json, Does.Contain("\"stable_bottleneck\":\"Unknown\""));
			Assert.That(json, Does.Contain("\"raw_warning\""));
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
		public void RenderIntegrationSnapshotBeforeObservationIsSafeAndReportsCurrentPipeline()
		{
			PerfMeterRenderGraphAnalytics.ResetForTests();

			Assert.That(QualitySettings.renderPipeline, Is.Not.Null);
			PerfMeterRenderPipelineSnapshot expectedPipeline = PerfMeterRenderPipelineDetector.CreateSnapshot(out PerfMeterRenderPipelineAssetSource expectedSource);
			Assert.That(expectedPipeline.Kind == PerfMeterRenderPipelineKind.Universal || expectedPipeline.Kind == PerfMeterRenderPipelineKind.HighDefinition, Is.True);
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));

			PerfMeterRenderIntegrationSnapshot snapshot = default;
			Assert.DoesNotThrow(() => snapshot = PerformanceMeter.GetRenderIntegrationSnapshot());

			Assert.That(snapshot.IsAvailable, Is.True);
			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(snapshot.State, Is.EqualTo(PerfMeterRenderIntegrationState.NotObserved));
			Assert.That(snapshot.RenderPipeline.Kind, Is.EqualTo(expectedPipeline.Kind));
			Assert.That(snapshot.RenderPipelineAssetSource, Is.EqualTo(expectedSource));
			Assert.That(snapshot.RenderPipeline.AssetName, Is.EqualTo(QualitySettings.renderPipeline.name));
			Assert.That(snapshot.RenderPipeline.AssetTypeName, Is.EqualTo(QualitySettings.renderPipeline.GetType().FullName));
			Assert.That(snapshot.ObservationMatchesCurrentPipeline, Is.False);
			Assert.That(snapshot.LastObservedFrame, Is.EqualTo(-1));
			Assert.That(snapshot.ObservationAgeFrames, Is.EqualTo(-1));
			Assert.That(snapshot.GpuResidentDrawer.Availability, Is.EqualTo(PerfMeterAvailability.Unknown));

			PerfMeterVariableRateShadingContextSnapshot vrs = snapshot.VariableRateShading;
			Assert.That(vrs.Availability, Is.Not.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(vrs.SupportsVariableRateShading, Is.EqualTo(SystemInfo.supportsVariableRateShading));
			Assert.That(vrs.ConfigurationAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(vrs.ActivityAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(vrs.IsConfigured, Is.False);
			Assert.That(vrs.IsObservedActive, Is.False);

			Assert.That(PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot trySnapshot), Is.True);
			Assert.That(trySnapshot.State, Is.EqualTo(PerfMeterRenderIntegrationState.NotObserved));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[Test]
		public void TypedUrpRenderIntegrationObservationReportsCameraPassAndCapabilitySemantics()
		{
			PerfMeterRenderGraphAnalytics.ResetForTests();
			if (PerfMeterRenderPipelineDetector.CreateSnapshot().Kind != PerfMeterRenderPipelineKind.Universal)
			{
				Assert.Pass("URP-specific observation coverage is not applicable to the active render pipeline.");
			}

			GameObject cameraObject = null;

			try
			{
				cameraObject = new GameObject("PerfMeter Synthetic URP Camera");
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.cameraType = CameraType.Game;
				PerfMeterGpuResidentDrawerContextSnapshot gpuResidentDrawer = new PerfMeterGpuResidentDrawerContextSnapshot(
					PerfMeterAvailability.Available,
					"InstancedDrawing",
					PerfMeterAvailability.Available,
					true,
					PerfMeterAvailability.Unknown,
					false,
					"GPU Resident Drawer activity was not reported by the synthetic observation.");

				PerfMeterRenderGraphAnalytics.RecordFeatureSnapshot(
					camera,
					"ForwardPlus",
					"AfterRenderingPostProcessing",
					gpuResidentDrawer,
					true,
					true,
					false);

				PerfMeterRenderIntegrationSnapshot snapshot = PerformanceMeter.GetRenderIntegrationSnapshot();
				Assert.That(snapshot.State, Is.EqualTo(PerfMeterRenderIntegrationState.Observed));
				Assert.That(snapshot.ObservationMatchesCurrentPipeline, Is.True);
				Assert.That(snapshot.RenderPipeline.Kind, Is.EqualTo(PerfMeterRenderPipelineKind.Universal));
				Assert.That(snapshot.RenderPipelineAssetSource, Is.EqualTo(PerfMeterRenderPipelineAssetSource.QualitySettings));
				Assert.That(snapshot.ObservedCameraEntityId, Is.GreaterThan(0UL));
				Assert.That(snapshot.ObservedCameraName, Is.EqualTo(cameraObject.name));
				Assert.That(snapshot.ObservedCameraType, Is.EqualTo("Game"));
				Assert.That(snapshot.EffectiveRenderingMode, Is.EqualTo("ForwardPlus"));
				Assert.That(snapshot.InjectionPoint, Is.EqualTo("AfterRenderingPostProcessing"));
				Assert.That(snapshot.PassKind, Is.EqualTo(PerfMeterRenderPassKind.RenderGraphRaster));
				Assert.That(snapshot.PassName, Is.EqualTo("SGG PerfMeter Render Graph Pass"));
				Assert.That(snapshot.PerfMeterPassCount, Is.EqualTo(3));

				Assert.That(snapshot.GpuResidentDrawer.Availability, Is.EqualTo(PerfMeterAvailability.Available));
				Assert.That(snapshot.GpuResidentDrawer.ConfiguredMode, Is.EqualTo("InstancedDrawing"));
				Assert.That(snapshot.GpuResidentDrawer.IsConfigured, Is.True);
				Assert.That(snapshot.GpuResidentDrawer.SupportAvailability, Is.EqualTo(PerfMeterAvailability.Available));
				Assert.That(snapshot.GpuResidentDrawer.IsSupported, Is.True);
				Assert.That(snapshot.GpuResidentDrawer.ActivityAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
				Assert.That(snapshot.GpuResidentDrawer.IsObservedActive, Is.False);

				Assert.That(snapshot.VariableRateShading.Availability, Is.Not.EqualTo(PerfMeterAvailability.Unknown));
				Assert.That(snapshot.VariableRateShading.ConfigurationAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
				Assert.That(snapshot.VariableRateShading.ActivityAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
				Assert.That(snapshot.VariableRateShading.IsConfigured, Is.False);
				Assert.That(snapshot.VariableRateShading.IsObservedActive, Is.False);

				Assert.That(snapshot.LegacyRenderGraph.State, Is.EqualTo(PerfMeterRenderGraphState.Observed));
				Assert.That(snapshot.LegacyRenderGraph.RenderPipeline, Is.EqualTo(PerfMeterRenderPipelineKind.Universal));
				Assert.That(snapshot.LegacyRenderGraph.IntegrationName, Is.EqualTo("URP Render Graph Feature"));
				Assert.That(snapshot.LegacyRenderGraph.PerfMeterPassCount, Is.EqualTo(3));
				Assert.That(snapshot.LegacyRenderGraph.RegisteredPassCount, Is.EqualTo(-1));
				Assert.That(snapshot.LegacyRenderGraph.MergedPassCount, Is.EqualTo(-1));
				Assert.That(snapshot.LegacyRenderGraph.TransientResourceCount, Is.EqualTo(-1));
				Assert.That(snapshot.LegacyRenderGraph.ImportedResourceCount, Is.EqualTo(-1));
				Assert.That(snapshot.LegacyRenderGraph.AliasedResourceCount, Is.EqualTo(-1));

				Assert.That(PerformanceMeter.TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot trySnapshot), Is.True);
				Assert.That(trySnapshot.ObservedCameraName, Is.EqualTo(cameraObject.name));
			}
			finally
			{
				if (cameraObject != null)
				{
					UnityEngine.Object.DestroyImmediate(cameraObject);
				}

				PerfMeterRenderGraphAnalytics.ResetForTests();
			}
		}

		[Test]
		public void GpuResidentDrawerLegacyConstructorPreservesExistingFieldsAndDefaultsTelemetry()
		{
			PerfMeterGpuResidentDrawerContextSnapshot snapshot = new PerfMeterGpuResidentDrawerContextSnapshot(
				PerfMeterAvailability.Available,
				"InstancedDrawing",
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				"legacy warning");

			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(snapshot.ConfiguredMode, Is.EqualTo("InstancedDrawing"));
			Assert.That(snapshot.SupportAvailability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(snapshot.IsSupported, Is.True);
			Assert.That(snapshot.ActivityAvailability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(snapshot.IsObservedActive, Is.True);
			Assert.That(snapshot.ActivitySource, Is.Empty);
			Assert.That(snapshot.ProjectConfigurationAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(snapshot.ComputeShaderAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(snapshot.ForwardPlusActivityAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(snapshot.RenderingModeCompatibilityAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(snapshot.DegradedReason, Is.EqualTo(PerfMeterGpuResidentDrawerReason.Unknown));
			Assert.That(snapshot.Effectiveness.BrgDrawCalls, Is.EqualTo(PerfMeterGpuResidentDrawerEffectivenessSnapshot.UnavailableCount));
			Assert.That(snapshot.Effectiveness.BrgInstances, Is.EqualTo(PerfMeterGpuResidentDrawerEffectivenessSnapshot.UnavailableCount));

			Assert.That(PerfMeterGpuResidentDrawerContextSnapshot.Unknown.DegradedReason, Is.EqualTo(PerfMeterGpuResidentDrawerReason.NotObserved));
			Assert.That(PerfMeterGpuResidentDrawerContextSnapshot.Unknown.ActivitySource, Is.Empty);
		}

		[Test]
		public void GpuResidentDrawerCompositionReportsStructuredReasonAndRuntimeActivity()
		{
			PerfMeterGpuResidentDrawerContextSnapshot primitiveObservation = new PerfMeterGpuResidentDrawerContextSnapshot(
				PerfMeterAvailability.Available,
				"InstancedDrawing",
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				string.Empty,
				PerfMeterAvailability.Available,
				false,
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				PerfMeterGpuResidentDrawerEffectivenessSnapshot.Unknown,
				PerfMeterGpuResidentDrawerReason.Unknown);

			PerfMeterGpuResidentDrawerContextSnapshot snapshot = PerfMeterRenderGraphAnalytics.ComposeGpuResidentDrawerSnapshot(
				primitiveObservation,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true);

			Assert.That(snapshot.ProjectConfigurationAvailability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(snapshot.IsProjectConfigurationSupported, Is.False);
			Assert.That(snapshot.ActivityAvailability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(snapshot.IsObservedActive, Is.True);
			Assert.That(snapshot.ActivitySource, Is.EqualTo(PerfMeterGpuResidentDrawerContextSnapshot.UnityRuntimeActivitySource));
			Assert.That(snapshot.ForwardPlusActivityAvailability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(snapshot.IsObservedForwardPlusActive, Is.True);
			Assert.That(snapshot.RenderingModeCompatibilityAvailability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(snapshot.IsRenderingModeCompatible, Is.True);
			Assert.That(snapshot.DegradedReason, Is.EqualTo(PerfMeterGpuResidentDrawerReason.ProjectConfigurationUnsupported));
			Assert.That(snapshot.Warning, Does.Contain("project configuration"));
		}

		[Test]
		public void GpuResidentDrawerObservedActiveStateOverridesDisabledConfiguredMode()
		{
			PerfMeterGpuResidentDrawerContextSnapshot primitiveObservation = new PerfMeterGpuResidentDrawerContextSnapshot(
				PerfMeterAvailability.Available,
				"Disabled",
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				string.Empty,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				PerfMeterGpuResidentDrawerEffectivenessSnapshot.Unknown,
				PerfMeterGpuResidentDrawerReason.Unknown);

			PerfMeterGpuResidentDrawerContextSnapshot snapshot = PerfMeterRenderGraphAnalytics.ComposeGpuResidentDrawerSnapshot(
				primitiveObservation,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true);

			Assert.That(snapshot.IsObservedActive, Is.True);
			Assert.That(snapshot.DegradedReason, Is.Not.EqualTo(PerfMeterGpuResidentDrawerReason.ModeDisabled));
			Assert.That(snapshot.DegradedReason, Is.Not.EqualTo(PerfMeterGpuResidentDrawerReason.RuntimeInactive));
		}

		[Test]
		public void GpuResidentDrawerQueryFailureIsDistinctFromUnavailablePipeline()
		{
			PerfMeterGpuResidentDrawerContextSnapshot queryFailure = PerfMeterRenderGraphAnalytics.ComposeGpuResidentDrawerSnapshot(
				new PerfMeterGpuResidentDrawerContextSnapshot(
					PerfMeterAvailability.Available,
					string.Empty,
					PerfMeterAvailability.Unavailable,
					false,
					PerfMeterAvailability.Unknown,
					false,
					"Synthetic mode query failed."));
			PerfMeterGpuResidentDrawerContextSnapshot missingInterface = PerfMeterRenderGraphAnalytics.ComposeGpuResidentDrawerSnapshot(
				new PerfMeterGpuResidentDrawerContextSnapshot(
					PerfMeterAvailability.Unavailable,
					string.Empty,
					PerfMeterAvailability.Unavailable,
					false,
					PerfMeterAvailability.Unknown,
					false,
					"Synthetic pipeline interface is unavailable."));

			Assert.That(queryFailure.DegradedReason, Is.EqualTo(PerfMeterGpuResidentDrawerReason.QueryFailed));
			Assert.That(missingInterface.DegradedReason, Is.EqualTo(PerfMeterGpuResidentDrawerReason.PipelineUnavailable));
		}

		[Test]
		public void GpuResidentDrawerEffectivenessNormalizesSampleStateAndSerializesUnavailableValuesAsNull()
		{
			PerfMeterProfilerMetricCapabilitySnapshot sampledDrawCalls = CreateBrgCapability(
				PerfMeterProfilerMetricSemantic.BrgDrawCalls,
				PerfMeterProfilerMetricSampleState.AvailableSampled,
				"BRG Draw Calls Count");
			PerfMeterProfilerMetricCapabilitySnapshot sampledInstances = CreateBrgCapability(
				PerfMeterProfilerMetricSemantic.BrgInstances,
				PerfMeterProfilerMetricSampleState.AvailableSampled,
				"BRG Instances Count");
			PerfMeterGpuResidentDrawerEffectivenessSnapshot sampled = new PerfMeterGpuResidentDrawerEffectivenessSnapshot(
				PerfMeterAvailability.Available,
				42,
				7,
				12,
				sampledDrawCalls,
				sampledInstances,
				string.Empty);
			Assert.That(sampled.BrgDrawCalls, Is.EqualTo(7));
			Assert.That(sampled.BrgInstances, Is.EqualTo(12));
			Assert.That(sampled.HasSample, Is.True);
			Assert.That(sampled.HasObservedBrgWorkload, Is.True);
			Assert.That(sampled.Warning, Does.Contain("aggregate BatchRendererGroup"));

			PerfMeterProfilerMetricCapabilitySnapshot unsampledDrawCalls = CreateBrgCapability(
				PerfMeterProfilerMetricSemantic.BrgDrawCalls,
				PerfMeterProfilerMetricSampleState.AvailableNoSample,
				"BRG Draw Calls Count");
			PerfMeterProfilerMetricCapabilitySnapshot unsampledInstances = CreateBrgCapability(
				PerfMeterProfilerMetricSemantic.BrgInstances,
				PerfMeterProfilerMetricSampleState.AvailableNoSample,
				"BRG Instances Count");
			PerfMeterGpuResidentDrawerEffectivenessSnapshot unsampled = new PerfMeterGpuResidentDrawerEffectivenessSnapshot(
				PerfMeterAvailability.Available,
				43,
				100,
				200,
				unsampledDrawCalls,
				unsampledInstances,
				"unsampled synthetic counters");
			Assert.That(unsampled.BrgDrawCalls, Is.EqualTo(PerfMeterGpuResidentDrawerEffectivenessSnapshot.UnavailableCount));
			Assert.That(unsampled.BrgInstances, Is.EqualTo(PerfMeterGpuResidentDrawerEffectivenessSnapshot.UnavailableCount));
			Assert.That(unsampled.HasSample, Is.False);
			Assert.That(unsampled.HasObservedBrgWorkload, Is.False);

			StringBuilder sampledJson = new StringBuilder();
			PerfMeterSessionExporter.AppendRenderIntegration(sampledJson, CreateRenderIntegrationSnapshot(new PerfMeterGpuResidentDrawerContextSnapshot(
				PerfMeterAvailability.Available,
				"InstancedDrawing",
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				string.Empty,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				sampled,
				PerfMeterGpuResidentDrawerReason.None)));
			Assert.That(sampledJson.ToString(), Does.Contain("\"brg_draw_calls\":7"));
			Assert.That(sampledJson.ToString(), Does.Contain("\"brg_instances\":12"));
			Assert.That(sampledJson.ToString(), Does.Contain("\"sample_state\":\"AvailableSampled\""));
			Assert.That(sampledJson.ToString(), Does.Contain("\"scope\":\"brg_aggregate\""));

			StringBuilder unsampledJson = new StringBuilder();
			PerfMeterSessionExporter.AppendRenderIntegration(unsampledJson, CreateRenderIntegrationSnapshot(new PerfMeterGpuResidentDrawerContextSnapshot(
				PerfMeterAvailability.Available,
				"InstancedDrawing",
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				string.Empty,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				unsampled,
				PerfMeterGpuResidentDrawerReason.None)));
			Assert.That(unsampledJson.ToString(), Does.Contain("\"brg_draw_calls\":null"));
			Assert.That(unsampledJson.ToString(), Does.Contain("\"brg_instances\":null"));
			Assert.That(unsampledJson.ToString(), Does.Contain("\"sample_state\":\"AvailableNoSample\""));
		}

		[Test]
		public void RenderIntegrationObservationUsesPipelineAssetIdentityBeyondNameAndType()
		{
			PerfMeterRenderGraphAnalytics.ResetForTests();
			RenderPipelineAsset originalAsset = QualitySettings.renderPipeline;
			SyntheticUniversalRenderPipelineAsset firstAsset = null;
			SyntheticUniversalRenderPipelineAsset secondAsset = null;

			try
			{
				firstAsset = ScriptableObject.CreateInstance<SyntheticUniversalRenderPipelineAsset>();
				secondAsset = ScriptableObject.CreateInstance<SyntheticUniversalRenderPipelineAsset>();
				Assert.That(firstAsset, Is.Not.SameAs(secondAsset));
				firstAsset.name = "Synthetic Universal Collision Asset";
				secondAsset.name = firstAsset.name;
				Assert.That(firstAsset.name, Is.EqualTo(secondAsset.name));
				Assert.That(firstAsset.GetType().FullName, Is.EqualTo(secondAsset.GetType().FullName));

				QualitySettings.renderPipeline = firstAsset;
				PerfMeterRenderGraphAnalytics.PrepareObservation(null);
				PerfMeterRenderGraphAnalytics.RecordFeatureSnapshot(
					null,
					"ForwardPlus",
					"AfterRenderingPostProcessing",
					new PerfMeterGpuResidentDrawerContextSnapshot(
						PerfMeterAvailability.Available,
						"InstancedDrawing",
						PerfMeterAvailability.Available,
						true,
						PerfMeterAvailability.Unknown,
						false,
						"Synthetic activity is unknown."),
					true,
					true,
					false);
				PerfMeterRenderIntegrationSnapshot firstSnapshot = PerformanceMeter.GetRenderIntegrationSnapshot();
				Assert.That(firstSnapshot.State, Is.EqualTo(PerfMeterRenderIntegrationState.Observed));
				Assert.That(firstSnapshot.ObservationMatchesCurrentPipeline, Is.True);

				QualitySettings.renderPipeline = secondAsset;
				PerfMeterRenderIntegrationSnapshot snapshot = PerformanceMeter.GetRenderIntegrationSnapshot();
				Assert.That(snapshot.RenderPipeline.Kind, Is.EqualTo(PerfMeterRenderPipelineKind.Universal));
				Assert.That(snapshot.RenderPipeline.AssetName, Is.EqualTo(secondAsset.name));
				Assert.That(snapshot.RenderPipeline.AssetTypeName, Is.EqualTo(secondAsset.GetType().FullName));
				Assert.That(snapshot.State, Is.EqualTo(PerfMeterRenderIntegrationState.NotObserved));
				Assert.That(snapshot.ObservationMatchesCurrentPipeline, Is.False);
				Assert.That(snapshot.Warning, Does.Contain("different render pipeline"));
			}
			finally
			{
				QualitySettings.renderPipeline = originalAsset;
				if (firstAsset != null)
				{
					UnityEngine.Object.DestroyImmediate(firstAsset);
				}

				if (secondAsset != null)
				{
					UnityEngine.Object.DestroyImmediate(secondAsset);
				}

				PerfMeterRenderGraphAnalytics.ResetForTests();
			}
		}

		[Test]
		public void PreparedRenderIntegrationObservationRecordDoesNotAllocate()
		{
			PerfMeterRenderGraphAnalytics.ResetForTests();
			const string effectiveRenderingMode = "ForwardPlus";
			const string injectionPoint = "AfterRenderingPostProcessing";
			PerfMeterGpuResidentDrawerContextSnapshot gpuResidentDrawer = new PerfMeterGpuResidentDrawerContextSnapshot(
				PerfMeterAvailability.Available,
				"InstancedDrawing",
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Unknown,
				false,
				"Synthetic activity is known through the typed observations.",
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				PerfMeterGpuResidentDrawerEffectivenessSnapshot.Unknown,
				PerfMeterGpuResidentDrawerReason.Unknown);
			GameObject cameraObject = null;

			try
			{
				cameraObject = new GameObject("PerfMeter Allocation Camera");
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.cameraType = CameraType.Game;

				PerfMeterRenderGraphAnalytics.PrepareObservation(camera);
				PerfMeterRenderGraphAnalytics.RecordFeatureSnapshot(
					camera,
					effectiveRenderingMode,
					injectionPoint,
					gpuResidentDrawer,
					true,
					true,
					false,
					PerfMeterAvailability.Available,
					true,
					PerfMeterAvailability.Available,
					true);
				PerfMeterRenderGraphAnalytics.PrepareObservation(camera);

				long before = System.GC.GetAllocatedBytesForCurrentThread();
				PerfMeterRenderGraphAnalytics.RecordFeatureSnapshot(
					camera,
					effectiveRenderingMode,
					injectionPoint,
					gpuResidentDrawer,
					true,
					true,
					false,
					PerfMeterAvailability.Available,
					true,
					PerfMeterAvailability.Available,
					true);
				long allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - before;

				Assert.That(allocatedBytes, Is.EqualTo(0L));
			}
			finally
			{
				if (cameraObject != null)
				{
					UnityEngine.Object.DestroyImmediate(cameraObject);
				}

				PerfMeterRenderGraphAnalytics.ResetForTests();
			}
		}

		[Test]
		public void HdrpCompatibilityFacadeReflectsCurrentPipelineFreshness()
		{
			PerfMeterRenderGraphAnalytics.ResetForTests();

			try
			{
				Assert.That(QualitySettings.renderPipeline, Is.Not.Null);
				PerfMeterRenderPipelineKind currentKind = PerfMeterRenderPipelineDetector.CreateSnapshot().Kind;
				bool observationMatches = currentKind == PerfMeterRenderPipelineKind.HighDefinition;
				PerfMeterRenderGraphAnalytics.RecordHdrpCustomPassSnapshot(
					"Synthetic HDRP Camera",
					"Game",
					"BeforePostProcess");

				PerfMeterRenderIntegrationSnapshot snapshot = PerformanceMeter.GetRenderIntegrationSnapshot();
				Assert.That(snapshot.RenderPipeline.Kind, Is.EqualTo(currentKind));
				Assert.That(snapshot.State, Is.EqualTo(observationMatches ? PerfMeterRenderIntegrationState.Observed : PerfMeterRenderIntegrationState.NotObserved));
				Assert.That(snapshot.ObservationMatchesCurrentPipeline, Is.EqualTo(observationMatches));
				Assert.That(snapshot.LastObservedFrame, Is.GreaterThanOrEqualTo(0));
				Assert.That(snapshot.ObservationAgeFrames, Is.GreaterThanOrEqualTo(0));
				Assert.That(snapshot.IntegrationId, Is.EqualTo("sgg.perfmeter.hdrp.custom-pass"));
				Assert.That(snapshot.PassKind, Is.EqualTo(PerfMeterRenderPassKind.CustomPass));
				if (!observationMatches)
				{
					Assert.That(snapshot.Warning, Does.Contain("different render pipeline"));
				}

				PerfMeterRenderGraphSnapshot legacy = PerformanceMeter.GetRenderGraphSnapshot();
				Assert.That(legacy.IsAvailable, Is.True);
				Assert.That(legacy.State, Is.EqualTo(PerfMeterRenderGraphState.Observed));
				Assert.That(legacy.RenderPipeline, Is.EqualTo(PerfMeterRenderPipelineKind.HighDefinition));
				Assert.That(legacy.IntegrationName, Is.EqualTo("HDRP Custom Pass"));
				Assert.That(legacy.ObservedCameraName, Is.EqualTo("Synthetic HDRP Camera"));
				Assert.That(legacy.ObservedCameraType, Is.EqualTo("Game"));
				Assert.That(legacy.ObservedInjectionPoint, Is.EqualTo("BeforePostProcess"));
				Assert.That(legacy.PerfMeterPassCount, Is.EqualTo(1));
				Assert.That(legacy.RegisteredPassCount, Is.EqualTo(-1));
				Assert.That(PerformanceMeter.TryGetRenderGraphSnapshot(out PerfMeterRenderGraphSnapshot tryLegacy), Is.True);
				Assert.That(tryLegacy.State, Is.EqualTo(PerfMeterRenderGraphState.Observed));
			}
			finally
			{
				PerfMeterRenderGraphAnalytics.ResetForTests();
			}
		}

		[Test]
		public void McpRenderIntegrationSnapshotExposesMetadataAndNestedOutputWithoutStartingRuntime()
		{
			PerfMeterRenderGraphAnalytics.ResetForTests();
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();

			Assert.That(metadata, Does.Contain("perfmeter.render.snapshot"));
			Assert.That(metadata, Does.Contain("SGG.PerfMeter.Editor.Mcp.PerfMeterMcpCommands.RenderIntegrationSnapshot"));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));

			PerfMeterRenderPipelineKind currentKind = PerfMeterRenderPipelineDetector.CreateSnapshot().Kind;
			string json = PerfMeterMcpCommands.RenderIntegrationSnapshot();
			Assert.That(json, Does.Contain("\"schema_version\":1"));
			Assert.That(json, Does.Contain("\"render_integration\":{"));
			Assert.That(json, Does.Contain("\"render_pipeline\":{"));
			Assert.That(json, Does.Contain("\"kind\":\"" + currentKind + "\""));
			Assert.That(json, Does.Contain("\"gpu_resident_drawer\":{"));
			Assert.That(json, Does.Contain("\"support_availability\""));
			Assert.That(json, Does.Contain("\"project_configuration_availability\""));
			Assert.That(json, Does.Contain("\"compute_shader_availability\""));
			Assert.That(json, Does.Contain("\"effectiveness\":{"));
			Assert.That(json, Does.Contain("\"activity_source\":\"\""));
			Assert.That(json, Does.Contain("\"degraded_reason\":\"NotObserved\""));
			Assert.That(json, Does.Contain("\"variable_rate_shading\":{"));
			Assert.That(json, Does.Contain("\"configuration_availability\""));
			Assert.That(json, Does.Contain("\"activity_availability\""));
			Assert.That(json, Does.Contain("\"legacy_render_graph\":{"));
			Assert.That(json, Does.Contain("\"registered_pass_count\":-1"));
			Assert.That(json, Does.Contain("\"is_playing\""));
			Assert.That(json, Does.Contain("\"is_paused\""));
			Assert.That(json, Does.Contain("\"is_compiling\""));
			Assert.That(json, Does.Contain("\"state\":\"NotObserved\""));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[Test]
		public void OverlayPanelSettingsAreSerializedWithTextAndThemeResources()
		{
			PanelSettings panelSettings = Resources.Load<PanelSettings>("PerfMeterOverlayPanelSettings");

			Assert.That(panelSettings, Is.Not.Null);
			Assert.That(panelSettings.scaleMode, Is.EqualTo(PanelScaleMode.ConstantPixelSize));
			Assert.That(panelSettings.sortingOrder, Is.EqualTo(short.MaxValue));
			Assert.That(panelSettings.textSettings, Is.Not.Null);
			Assert.That(panelSettings.themeStyleSheet, Is.Not.Null);
		#if UNITY_6000_5_OR_NEWER
			UnityEditor.SerializedObject serializedPanel = new UnityEditor.SerializedObject(panelSettings);
			UnityEditor.SerializedProperty icuData = serializedPanel.FindProperty("m_ICUDataAsset");
			Assert.That(icuData, Is.Not.Null);
			Assert.That(icuData.objectReferenceValue, Is.Not.Null);
		#endif
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
		public void AlertEngineStructuredLogToggleDoesNotAffectOtherActionsOrHistory()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("structured.toggle", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 10d, 1, 0f, PerfMeterAlertAction.StructuredLog | PerfMeterAlertAction.Callback | PerfMeterAlertAction.EditorWarning)
			});
			int callbackCount = 0;
			System.Action<PerfMeterAlertSnapshot> handler = alert => callbackCount++;
			PerformanceMeter.AlertFired += handler;

			try
			{
				engine.SetStructuredLogsEnabled(false);
				LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("\\[SGG PerfMeter Alert\\] structured.toggle"));
				engine.Evaluate(CreateMetrics(1, 20d, PerfMeterBottleneck.CpuMainThreadBound), 0d);
				LogAssert.NoUnexpectedReceived();

				Assert.That(callbackCount, Is.EqualTo(1));
				Assert.That(engine.ActiveAlertCount, Is.EqualTo(1));
				Assert.That(engine.FiredAlertCount, Is.EqualTo(1));
				Assert.That(engine.LatestAlert.RuleId, Is.EqualTo("structured.toggle"));
				Assert.That(engine.History.FiredCount, Is.EqualTo(1));

				engine.SetStructuredLogsEnabled(true);
				LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("\\[SGG PerfMeter Alert\\] structured.toggle"));
				engine.Evaluate(CreateMetrics(2, 20d, PerfMeterBottleneck.CpuMainThreadBound), 2d);
				LogAssert.NoUnexpectedReceived();

				Assert.That(callbackCount, Is.EqualTo(2));
				Assert.That(engine.ActiveAlertCount, Is.EqualTo(1));
				Assert.That(engine.FiredAlertCount, Is.EqualTo(2));
				Assert.That(engine.History.FiredCount, Is.EqualTo(2));
				Assert.That(engine.History.LatestFiredAlert.CollectionFrame, Is.EqualTo(2));
			}
			finally
			{
				PerformanceMeter.AlertFired -= handler;
			}
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
		public void AlertEngineClassifiesFiringsAndRecordsHistoryBoundary()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("classification.test", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 10d, 1, 0f, PerfMeterAlertAction.Callback)
			});
			System.Action<PerfMeterAlertSnapshot> handler = alert => { };
			PerformanceMeter.AlertFired += handler;

			try
			{
				engine.Evaluate(CreateMetrics(1, 20d, PerfMeterBottleneck.CpuMainThreadBound), 0d, PerfMeterAlertClassification.Lifecycle, string.Empty);
				engine.Evaluate(CreateMetrics(2, 20d, PerfMeterBottleneck.CpuMainThreadBound), 1d, PerfMeterAlertClassification.Capture, "capture-1");
				engine.Evaluate(CreateMetrics(3, 20d, PerfMeterBottleneck.CpuMainThreadBound), 2d, PerfMeterAlertClassification.SteadyState, string.Empty);

				PerfMeterAlertHistorySnapshot history = engine.History;
				Assert.That(history.FiredCount, Is.EqualTo(3));
				Assert.That(history.LifecycleFiredCount, Is.EqualTo(1));
				Assert.That(history.CaptureFiredCount, Is.EqualTo(1));
				Assert.That(history.SteadyStateFiredCount, Is.EqualTo(1));
				Assert.That(history.LatestFiredAlert.Classification, Is.EqualTo(PerfMeterAlertClassification.SteadyState));

				string previousIntervalId = history.IntervalId;
				engine.ResetHistory(4, 3d, PerfMeterAlertHistoryResetReason.ExplicitClear);
				history = engine.History;
				Assert.That(history.IntervalId, Is.Not.EqualTo(previousIntervalId));
				Assert.That(history.StartCollectionFrame, Is.EqualTo(4));
				Assert.That(history.StartTimeSeconds, Is.EqualTo(3d));
				Assert.That(history.ResetReason, Is.EqualTo(PerfMeterAlertHistoryResetReason.ExplicitClear));
				Assert.That(history.FiredCount, Is.Zero);
				Assert.That(string.IsNullOrEmpty(history.LatestFiredAlert.RuleId), Is.True);
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
			Assert.That(modeJson, Does.Contain("\"mutation\":{\"operation\":\"runtime_mode_set\""));
			Assert.That(modeJson, Does.Contain("\"success\":true"));
			Assert.That(modeJson, Does.Contain("\"effective\":\"Background\""));

			string startJson = PerfMeterMcpCommands.SessionStart("{\"warmup_frames\":0,\"warmup_seconds\":0,\"sample_interval_seconds\":0.01,\"max_samples\":2,\"reset_on_scene_load\":true,\"scene_load_ignore_frames\":1,\"scene_load_ignore_seconds\":0}");
			Assert.That(startJson, Does.Contain("\"success\":true"));
			Assert.That(startJson, Does.Contain("\"status\":\"recording\""));
			Assert.That(startJson, Does.Contain("\"max_samples\":2"));
			Assert.That(startJson, Does.Contain("\"reset_on_scene_load\":true"));
			Assert.That(startJson, Does.Contain("\"scene_load_ignore_frames\":1"));
			Assert.That(startJson, Does.Contain("\"mutation\":{\"operation\":\"session_start\""));
			Assert.That(startJson, Does.Contain("\"result\":\"Applied\""));

			string summaryJson = PerfMeterMcpCommands.SessionSummary();
			Assert.That(summaryJson, Does.Contain("\"summary\""));
			Assert.That(summaryJson, Does.Contain("\"state\":\"Recording\""));
			Assert.That(summaryJson, Does.Contain("\"whole_run\""));
			Assert.That(summaryJson, Does.Contain("\"focus_loss_count\""));
			Assert.That(summaryJson, Does.Contain("\"focus_paused_duration_seconds\""));

			string stopJson = PerfMeterMcpCommands.SessionStop();
			Assert.That(stopJson, Does.Contain("\"status\":\"stopped\""));
			Assert.That(stopJson, Does.Contain("\"operation\":\"session_stop\""));
		}

		[Test]
		public void McpSessionStartReportsUnavailableInsteadOfFalseSuccess()
		{
			PerformanceMeter.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			runtime.enabled = false;
			try
			{
				string json = PerfMeterMcpCommands.SessionStart("{}");

				Assert.That(json, Does.Contain("\"success\":false"));
				Assert.That(json, Does.Contain("\"result\":\"Unavailable\""));
				Assert.That(json, Does.Contain("\"reason\":\"RuntimeUnavailable\""));
				Assert.That(json, Does.Not.Contain("\"state\":\"Recording\""));
			}
			finally
			{
				runtime.enabled = true;
			}
		}

		[Test]
		public void McpOverlayCommandsReportDeferredRequestedStateInEditMode()
		{
			string hiddenJson = PerfMeterMcpCommands.OverlaySet("{\"visible\":false}");
			Assert.That(hiddenJson, Does.Contain("\"collection_mode\":\"Background\""));
			Assert.That(hiddenJson, Does.Contain("\"overlay_visible\":false"));
			Assert.That(hiddenJson, Does.Contain("\"overlay_requested_visible\":false"));
			Assert.That(hiddenJson, Does.Contain("\"overlay_request_persisted\":true"));
			Assert.That(hiddenJson, Does.Contain("\"overlay_apply_state\":\"edit_mode_deferred\""));
			Assert.That(hiddenJson, Does.Contain("\"repaint_requested\":true"));
			Assert.That(hiddenJson, Does.Contain("\"rendered_visibility\":\"unknown\""));
			Assert.That(hiddenJson, Does.Contain("\"mutation\":{\"operation\":\"overlay_set\""));
			Assert.That(hiddenJson, Does.Contain("\"success\":true"));

			string visibleJson = PerfMeterMcpCommands.OverlaySet("{\"visible\":true}");
			Assert.That(visibleJson, Does.Contain("\"collection_mode\":\"Overlay\""));
			Assert.That(visibleJson, Does.Contain("\"overlay_visible\":false"));
			Assert.That(visibleJson, Does.Contain("\"overlay_requested_visible\":true"));
			Assert.That(visibleJson, Does.Contain("\"overlay_apply_state\":\"edit_mode_deferred\""));

			string backgroundJson = PerfMeterMcpCommands.RuntimeModeSet("{\"mode\":\"Background\"}");
			Assert.That(backgroundJson, Does.Contain("\"overlay_requested_visible\":false"));
			Assert.That(PerfMeterMcpCommands.RuntimeStatus(), Does.Contain("\"repaint_requested\":false"));
		}

		[UnityTest]
		public IEnumerator McpHiddenVisibilitySurvivesPlayModeTransition()
		{
			PerfMeterMcpCommands.OverlaySet("{\"visible\":false}");

			yield return new EnterPlayMode();
			yield return null;
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
			PerfMeterMcpCommands.RuntimeEnsure();
			yield return null;

			Assert.That(PerformanceMeter.GetStatus().CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));
			Assert.That(PerformanceMeter.GetStatus().OverlayVisible, Is.False);
			Assert.That(GameObject.Find("SGG PerfMeter Overlay"), Is.Null);

			yield return new ExitPlayMode();
			PerfMeterMcpCommands.OverlaySet("{\"visible\":true}");
		}

		[Test]
		public void McpAlertCommandsExposeMetadataAndBasicOutput()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();
			Assert.That(metadata, Does.Contain("perfmeter.alerts.latest"));
			Assert.That(metadata, Does.Contain("perfmeter.alerts.clear"));
			Assert.That(metadata, Does.Contain("perfmeter.alerts.capture.begin"));
			Assert.That(metadata, Does.Contain("perfmeter.alerts.capture.end"));

			string latestJson = PerfMeterMcpCommands.AlertsLatest();
			Assert.That(latestJson, Does.Contain("\"alerts\""));
			Assert.That(latestJson, Does.Contain("\"active_alert_count\":0"));
			Assert.That(latestJson, Does.Contain("\"history\""));
			Assert.That(latestJson, Does.Contain("\"reset_reason\""));
			Assert.That(latestJson, Does.Contain("\"latest_fired_alert\":null"));
			Assert.That(latestJson, Does.Contain("\"is_playing\""));

			Assert.That(PerfMeterMcpCommands.AlertsCaptureBegin("{\"capture_id\":\"capture-test\"}"), Does.Contain("\"started\":true"));
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.EqualTo(1));
			Assert.That(PerfMeterMcpCommands.AlertsCaptureBegin("{\"capture_id\":\"other-capture\"}"), Does.Contain("\"capture_id\":\"capture-test\""));
			Assert.That(PerfMeterMcpCommands.AlertsCaptureBegin("{\"capture_id\":\"other-capture\"}"), Does.Contain("\"started\":false"));
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.EqualTo(1));
			Assert.That(PerfMeterMcpCommands.AlertsCaptureEnd("{\"capture_id\":\"other-capture\"}"), Does.Contain("\"capture_scope_active\":true"));
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.EqualTo(1));
			Assert.That(PerfMeterMcpCommands.AlertsCaptureEnd("{\"capture_id\":\"capture-test\"}"), Does.Contain("\"ended\":true"));
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.Zero);
			Assert.That(PerfMeterMcpCommands.AlertsCaptureEnd("{\"capture_id\":\"capture-test\"}"), Does.Contain("\"capture_scope_active\":false"));

			string clearJson = PerfMeterMcpCommands.AlertsClear();
			Assert.That(clearJson, Does.Contain("\"cleared\":true"));
			Assert.That(clearJson, Does.Contain("\"fired_alert_count\":0"));
			Assert.That(clearJson, Does.Contain("\"reset_reason\":\"ExplicitClear\""));
		}

		[Test]
		public void AlertCaptureScopesRejectEmptyIdsAndVisualChangesPreserveHistory()
		{
			Assert.Throws<System.ArgumentException>(() => PerformanceMeter.BeginAlertCapture(string.Empty));
			Assert.Throws<System.ArgumentException>(() => PerformanceMeter.EndAlertCapture(string.Empty));

			PerformanceMeter.EnsureRunning();
			string intervalId = PerformanceMeter.GetAlertHistory().IntervalId;
			PerformanceMeter.SetEditorWarningLogsEnabled(false);

			Assert.That(PerformanceMeter.GetAlertHistory().IntervalId, Is.EqualTo(intervalId));
		}

		[Test]
		public void ProfilerLeaseApiIsAdditiveAndEnforcesOwnership()
		{
			Assert.That(PerformanceMeter.GetProfilerLeaseCapabilities().Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(PerfMeterProfilerLeaseResourceKeys.ActiveGpu, Is.EqualTo("active-gpu"));
			Assert.That(PerfMeterProfilerLeaseResourceKeys.ExclusiveProfilingOperation, Is.EqualTo("exclusive-profiling-operation"));

			PerfMeterProfilerLeaseRequestOptions request = new PerfMeterProfilerLeaseRequestOptions(
				"provider-lease",
				"provider-owner",
				string.Empty,
				PerfMeterProfilerLeaseResourceKeys.ActiveGpu,
				PerfMeterProfilerLeaseResourceKeys.ExclusiveProfilingOperation,
				PerfMeterProfilerLeaseResource.Gpu | PerfMeterProfilerLeaseResource.Operation);

			Assert.That(PerformanceMeter.TryAcquireProfilerLease(request, out PerfMeterProfilerLeaseStatusSnapshot acquired), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));
			Assert.That(acquired.IsHeld, Is.True);
			Assert.That(acquired.Resources, Is.EqualTo(PerfMeterProfilerLeaseResource.Gpu | PerfMeterProfilerLeaseResource.Operation));
			Assert.That(PerformanceMeter.GetProfilerLeaseCapabilities().Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus(request.LeaseId).IsHeld, Is.True);

			Assert.That(PerformanceMeter.TryAcquireProfilerLease(request, out PerfMeterProfilerLeaseStatusSnapshot repeated), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.AlreadyHeld));
			Assert.That(repeated.IsHeld, Is.True);
			Assert.That(PerformanceMeter.ReleaseProfilerLease(request.LeaseId, "wrong-owner", out PerfMeterProfilerLeaseStatusSnapshot wrongOwner), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.WrongOwner));
			Assert.That(wrongOwner.IsHeld, Is.True);
			Assert.That(PerformanceMeter.ReleaseProfilerLease(request.LeaseId, request.OwnerId, out PerfMeterProfilerLeaseStatusSnapshot released), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.Released));
			Assert.That(released.IsHeld, Is.False);
			Assert.That(PerformanceMeter.ReleaseProfilerLease(request.LeaseId, request.OwnerId, out PerfMeterProfilerLeaseStatusSnapshot repeatedRelease), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.AlreadyReleased));
			Assert.That(repeatedRelease.State, Is.EqualTo(PerfMeterProfilerLeaseState.Released));
		}

		[Test]
		public void ExternalGpuOperationLeasePreservesOperationOverlapResults()
		{
			PerfMeterProfilerLeaseRequestOptions external = new PerfMeterProfilerLeaseRequestOptions(
				"external-gpu-operation",
				"external-owner",
				string.Empty,
				PerfMeterProfilerLeaseResourceKeys.ActiveGpu,
				PerfMeterProfilerLeaseResourceKeys.ExclusiveProfilingOperation,
				PerfMeterProfilerLeaseResource.Gpu | PerfMeterProfilerLeaseResource.Operation);

			Assert.That(PerformanceMeter.TryAcquireProfilerLease(external, out PerfMeterProfilerLeaseStatusSnapshot acquired), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));
			try
			{
				Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("lease-blocked-capture", PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.RejectedOverlap));
				Assert.That(PerformanceMeter.RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("lease-blocked-memory", minimumFreeDiskBytes: 0L, cooldownSeconds: 0d)), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.RejectedOverlap));

				PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.01f, 2));
				Assert.That(PerformanceMeter.RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("lease-blocked-trace", 1, 0L)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap));
				Assert.That(PerformanceMeter.PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions("lease-blocked-prewarm")), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap));
				Assert.That(PerformanceMeter.BeginAlertCapture("lease-blocked-alert"), Is.False);
				Assert.That(PerformanceMeter.GetProfilerLeaseStatus(external.LeaseId).IsHeld, Is.True);
			}
			finally
			{
				PerformanceMeter.ReleaseProfilerLease(external.LeaseId, external.OwnerId);
			}
		}

		[Test]
		public void InternalCaptureLeaseRetainsThroughCleanupFailureAndThenReleases()
		{
			PerformanceMeter.EnsureRunning();
			LeaseCaptureBackend backend = new LeaseCaptureBackend();
			PerfMeterRuntime.Instance.SetCaptureBackendForTests(backend);

			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("lease-capture", PerfMeterCaptureTool.RenderDoc, 1)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			PerfMeterProfilerLeaseStatusSnapshot held = PerformanceMeter.GetProfilerLeaseStatus();
			Assert.That(held.IsHeld, Is.True);
			Assert.That(held.OwnerId, Is.EqualTo("perfmeter-capture"));
			Assert.That(held.Resources, Is.EqualTo(PerfMeterProfilerLeaseResource.Gpu | PerfMeterProfilerLeaseResource.Operation));
			Assert.That(PerformanceMeter.ReleaseProfilerLease(held.LeaseId, held.OwnerId), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.WrongOwner));
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus(held.LeaseId).IsHeld, Is.True);

			backend.EndSucceeds = false;
			PerfMeterRuntime.Instance.TickCaptureForTests();
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus(held.LeaseId).IsHeld, Is.True);
			Assert.That(PerformanceMeter.CancelCapture("lease-capture"), Is.False);
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus(held.LeaseId).IsHeld, Is.True);

			backend.EndSucceeds = true;
			Assert.That(PerformanceMeter.CancelCapture("lease-capture"), Is.True);
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus(held.LeaseId).State, Is.EqualTo(PerfMeterProfilerLeaseState.Released));
		}

		[Test]
		public void StoppingRuntimeDoesNotLeaveHeldProfilerLease()
		{
			PerformanceMeter.EnsureRunning();
			Assert.That(PerformanceMeter.BeginAlertCapture("stop-lease"), Is.True);
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus().IsHeld, Is.True);

			PerformanceMeter.Stop();

			Assert.That(PerformanceMeter.GetProfilerLeaseStatus().IsHeld, Is.False);
			PerformanceMeter.EnsureRunning();
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus().IsHeld, Is.False);
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

		private static PerfMeterProfilerMetricCapabilitySnapshot CreateBrgCapability(
			PerfMeterProfilerMetricSemantic semantic,
			PerfMeterProfilerMetricSampleState sampleState,
			string recorderName)
		{
			return new PerfMeterProfilerMetricCapabilitySnapshot(
				semantic,
				sampleState,
				PerfMeterProfilerMetricResolution.Exact,
				"Render",
				recorderName,
				"Count",
				"Int64",
				1,
				sampleState == PerfMeterProfilerMetricSampleState.AvailableSampled ? 1 : 0);
		}

		private static PerfMeterRenderIntegrationSnapshot CreateRenderIntegrationSnapshot(PerfMeterGpuResidentDrawerContextSnapshot gpuResidentDrawer)
		{
			return new PerfMeterRenderIntegrationSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderIntegrationState.Observed,
				new PerfMeterRenderPipelineSnapshot(
					PerfMeterRenderPipelineKind.Universal,
					"Synthetic Universal",
					"SyntheticUniversal",
					"SyntheticUniversalRuntime"),
				PerfMeterRenderPipelineAssetSource.QualitySettings,
				42,
				0,
				true,
				0UL,
				"Synthetic Camera",
				"Game",
				"synthetic.integration",
				"Synthetic Integration",
				"1.0",
				PerfMeterRenderPassKind.RenderGraphRaster,
				"Synthetic Pass",
				"AfterRendering",
				1,
				"ForwardPlus",
				gpuResidentDrawer,
				PerfMeterVariableRateShadingContextSnapshot.Unknown,
				PerfMeterRenderGraphSnapshot.NotObserved,
				string.Empty);
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

		private sealed class NonReportingCustomMetricProvider : IPerfMeterCustomMetricProvider
		{
			private readonly string _id;

			public NonReportingCustomMetricProvider(string id)
			{
				_id = id;
			}

			public string Id => _id;
			public int CollectCount { get; private set; }

			public bool TryCollect(out PerfMeterCustomMetricSnapshot metric)
			{
				CollectCount++;
				metric = default;
				return false;
			}
		}

		private sealed class CountingIdCustomMetricProvider : IPerfMeterCustomMetricProvider
		{
			private readonly string _id;
			private readonly double _value;
			private int _idReadCount;

			public CountingIdCustomMetricProvider(string id, double value)
			{
				_id = id;
				_value = value;
			}

			public int IdReadCount => _idReadCount;

			public string Id
			{
				get
				{
					_idReadCount++;
					return _id;
				}
			}

			public bool TryCollect(out PerfMeterCustomMetricSnapshot metric)
			{
				metric = new PerfMeterCustomMetricSnapshot(string.Empty, string.Empty, "tests", "count", _value);
				return true;
			}
		}

		private sealed class LeaseCaptureBackend : IPerfMeterCaptureBackend
		{
			internal bool EndSucceeds { get; set; } = true;

			public PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool)
			{
				return new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Available, string.Empty);
			}

			public bool TryBegin(PerfMeterCaptureTool tool, out string error)
			{
				error = string.Empty;
				return true;
			}

			public bool TryEnd(out string error)
			{
				error = EndSucceeds ? string.Empty : "test capture cleanup failed";
				return EndSucceeds;
			}
		}

		private sealed class SyntheticUniversalRenderPipelineAsset : RenderPipelineAsset
		{
			protected override RenderPipeline CreatePipeline()
			{
				return null;
			}
		}
	}
}
