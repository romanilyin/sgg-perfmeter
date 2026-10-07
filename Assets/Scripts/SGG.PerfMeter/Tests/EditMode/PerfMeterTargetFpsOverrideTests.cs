using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterTargetFpsOverrideTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterSettingsBootstrap.ResetExplicitSettingsApplication();
			PerformanceMeter.ClearCustomMetricProviders();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterSettingsBootstrap.ResetExplicitSettingsApplication();
			PerformanceMeter.ClearCustomMetricProviders();
		}

		[Test]
		public void EveryDeclaredTargetFpsSurvivesSettingsSerialization()
		{
			foreach (PerfMeterTargetFps target in System.Enum.GetValues(typeof(PerfMeterTargetFps)))
			{
				PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
				settings.targetFps = (int)target;
				foreach (PerfMeterPresetSettingsJson preset in settings.presets) preset.targetFps = (int)target;
				Assert.That(PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty).TargetFps, Is.EqualTo(target));
				Assert.That(PerfMeterSettingsStore.TryReadSnapshot(PerfMeterSettingsStore.ToJson(settings), out PerfMeterSettingsSnapshot roundTrip), Is.True);
				Assert.That(roundTrip.TargetFps, Is.EqualTo(target));
			}
		}

		[Test]
		public void EarlyOverrideSurvivesResourcesWhileOtherSettingsAndProvidersAreApplied()
		{
			PerformanceMeter.RegisterCustomMetricProvider(new SentinelProvider());
			Assert.That(PerformanceMeter.TrySetTargetFps(PerfMeterTargetFps.Fps120).Succeeded, Is.True);
			Assert.That(PerfMeterSettingsBootstrap.TryAutoStartFromSettings(ResourcesSettings()), Is.True);
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(PerformanceMeter.GetLatestMetrics().FrameBudgetMs, Is.EqualTo(1000d / 120).Within(0.0001d));
			Assert.That(PerformanceMeter.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));
			Assert.That(PerformanceMeter.StructuredLogsEnabled, Is.False);
			Assert.That(PerfMeterRuntime.Instance.ConfiguredSettings.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(PerformanceMeter.GetCustomMetrics()[0].Id, Is.EqualTo("override.sentinel"));
		}

		[Test]
		public void NoOverrideUsesResourcesTargetAndLateSetterWins()
		{
			Assert.That(PerfMeterSettingsBootstrap.TryAutoStartFromSettings(ResourcesSettings()), Is.True);
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps120);
			Assert.That(PerformanceMeter.GetStatus().TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
		}

		[Test]
		public void SameValueExplicitSetterIsStillAnOverride()
		{
			PerformanceMeter.EnsureRunning();
			PerfMeterTargetFps target = PerformanceMeter.TargetFps;
			Assert.That(PerformanceMeter.TrySetTargetFps(target).Status, Is.EqualTo(PerfMeterMutationStatus.NoChange));
			Assert.That(PerfMeterSettingsBootstrap.TryAutoStartFromSettings(ResourcesSettings()), Is.True);
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(target));
		}

		[Test]
		public void ExplicitSnapshotRemainsAuthoritativeAndSuppressesAutomaticBootstrap()
		{
			PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps120);
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.targetFps = 30;
			Assert.That(PerformanceMeter.TryApplySettingsJson(PerfMeterSettingsStore.ToJson(settings), out _), Is.True);
			Assert.That(PerfMeterSettingsBootstrap.TryAutoStartFromSettings(ResourcesSettings()), Is.False);
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps30));
		}

		[Test]
		public void UnavailableSetterDoesNotRecordOverrideAndDomainResetClearsPreviousOverrides()
		{
			PerformanceMeter.EnsureRunning();
			PerfMeterRuntime.Instance.enabled = false;
			Assert.That(PerformanceMeter.TrySetTargetFps(PerfMeterTargetFps.Fps120).Succeeded, Is.False);
			PerfMeterRuntime.Instance.enabled = true;
			Assert.That(PerfMeterSettingsBootstrap.TryAutoStartFromSettings(ResourcesSettings()), Is.True);
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps120);
			PerfMeterSettingsBootstrap.ResetExplicitSettingsApplication();
			Assert.That(PerfMeterSettingsBootstrap.TryAutoStartFromSettings(ResourcesSettings()), Is.True);
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
		}

		[Test]
		public void InvalidTargetReportsNormalizationAndExplicitOverlayConfigurationIsPreserved()
		{
			Assert.That(PerformanceMeter.TrySetTargetFps((PerfMeterTargetFps)99).Status, Is.EqualTo(PerfMeterMutationStatus.Normalized));
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(PerfMeterSettingsStore.DefaultTargetFps));
			PerfMeterOverlayConfiguration configuration = new PerfMeterOverlayConfiguration(
				true, PerfMeterOverlayCorner.TopRight, PerfMeterOverlayPreset.FullDiagnostics,
				PerfMeterOverlayTheme.ClassicDark, PerfMeterOverlayLayout.MetricBars,
				PerfMeterOverlayFontFamily.Manrope, PerfMeterSettingsStore.DefaultOverlayModules, PerfMeterTargetFps.Fps120);
			Assert.That(PerformanceMeter.TryApplyOverlayConfiguration(configuration).Succeeded, Is.True);
			Assert.That(PerfMeterSettingsBootstrap.TryAutoStartFromSettings(ResourcesSettings()), Is.True);
			Assert.That(PerformanceMeter.TargetFps, Is.EqualTo(configuration.TargetFps));
		}

		private static PerfMeterSettingsSnapshot ResourcesSettings()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.targetFps = 60;
			foreach (PerfMeterPresetSettingsJson preset in settings.presets)
			{
				preset.targetFps = 60;
			}
			settings.collectionMode = "Background";
			settings.ruleDefaults.structuredLogsEnabled = false;
			return PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);
		}

		private sealed class SentinelProvider : IPerfMeterCustomMetricProvider
		{
			public string Id => "override.sentinel";
			public bool TryCollect(out PerfMeterCustomMetricSnapshot metric)
			{
				metric = new PerfMeterCustomMetricSnapshot(Id, "Sentinel", "test", "count", 1);
				return true;
			}
		}
	}
}
