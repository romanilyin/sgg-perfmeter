using System;
using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterOverlayDescriptorTests
	{
		[TearDown]
		public void TearDown()
		{
			PerfMeterWidgetRegistry.ClearExtensionDescriptorsForTests();
		}

		[Test]
		public void ThemeRegistryPublishesBoundedSemanticManifestsAndFallback()
		{
			PerfMeterOverlayThemeManifest[] manifests = PerfMeterOverlayThemeRegistry.GetAllManifests();

			Assert.That(manifests, Has.Length.EqualTo(4));
			Assert.That(manifests[0].Id, Is.EqualTo(nameof(PerfMeterOverlayTheme.ClassicDark)));
			Assert.That(manifests[0].Text.a, Is.GreaterThan(0f));
			Assert.That(manifests[0].GraphBackground, Is.Not.EqualTo(manifests[0].Text));
			Assert.That(manifests[1].Accent, Is.Not.EqualTo(manifests[2].Accent));
			Assert.That(PerfMeterOverlayThemeRegistry.TryGetManifest("highcontrast", out PerfMeterOverlayThemeManifest highContrast), Is.True);
			Assert.That(highContrast.Theme, Is.EqualTo(PerfMeterOverlayTheme.HighContrast));
			Assert.That(PerfMeterOverlayThemeRegistry.TryGetManifest("missing-theme", out PerfMeterOverlayThemeManifest fallback), Is.False);
			Assert.That(fallback.Theme, Is.EqualTo(PerfMeterOverlayTheme.ClassicDark));
		}

		[Test]
		public void ExtensionWidgetDescriptorsAreModuleBackedUniqueAndBounded()
		{
			PerfMeterWidgetDescriptor descriptor = CreateExtensionDescriptor("project.movement-panel", PerfMeterOverlayModule.CustomMetrics);
			Assert.That(PerfMeterWidgetRegistry.TryRegisterDescriptor(descriptor, out string warning), Is.True, warning);
			Assert.That(PerfMeterWidgetRegistry.TryGetDescriptor("project.movement-panel", out PerfMeterWidgetDescriptor registered), Is.True);
			Assert.That(registered.OverlayModules, Is.EqualTo(PerfMeterOverlayModule.CustomMetrics));
			Assert.That(PerfMeterWidgetRegistry.TryRegisterDescriptor(descriptor, out warning), Is.False);
			Assert.That(warning, Does.Contain("already registered"));

			PerfMeterOverlayPresetJson preset = PerfMeterOverlayPresetDefaults.CreateCompactTiming();
			preset.widgets = new[]
			{
				new PerfMeterOverlayPresetWidgetJson { id = "project.movement-panel", enabled = true, order = 10 }
			};
			PerfMeterOverlayModule modules = PerfMeterOverlayPresetUtility.GetEnabledModules(preset, out warning);
			Assert.That(warning, Is.Empty);
			Assert.That(modules, Is.EqualTo(PerfMeterOverlayModule.CustomMetrics));

			PerfMeterWidgetRegistry.ClearExtensionDescriptorsForTests();
			for (int i = 0; i < PerfMeterWidgetRegistry.MaxExtensionDescriptors; i++)
			{
				Assert.That(PerfMeterWidgetRegistry.TryRegisterDescriptor(CreateExtensionDescriptor("project.widget-" + i, PerfMeterOverlayModule.Fps), out warning), Is.True, warning);
			}

			Assert.That(PerfMeterWidgetRegistry.TryRegisterDescriptor(CreateExtensionDescriptor("project.overflow", PerfMeterOverlayModule.Fps), out warning), Is.False);
			Assert.That(warning, Does.Contain("limit is " + PerfMeterWidgetRegistry.MaxExtensionDescriptors));
		}

		[Test]
		public void LayoutDescriptorNormalizationEnforcesSafetyLimitsAndRawWidgetMetadata()
		{
			PerfMeterOverlayPresetJson preset = PerfMeterOverlayPresetDefaults.CreateGraphs();
			preset.style.maxWidth = 5000;
			preset.style.gap = 100;
			PerfMeterOverlayPresetWidgetJson rawStrip = FindWidget(preset, "graphs.raw-frame-time");
			rawStrip.height = 1000;

			PerfMeterOverlayPresetValidationResult validation = PerfMeterOverlayPresetUtility.Validate(preset);
			Assert.That(validation.IsValid, Is.True);
			Assert.That(validation.Warning, Does.Contain("maxWidth was clamped"));
			Assert.That(validation.Warning, Does.Contain("gap was clamped"));
			Assert.That(validation.Warning, Does.Contain("height was clamped"));

			string json = PerfMeterOverlayPresetUtility.ToJson(preset);
			Assert.That(PerfMeterOverlayPresetUtility.TryReadJson(json, out PerfMeterOverlayPresetJson parsed, out string warning), Is.True, warning);
			Assert.That(parsed.style.maxWidth, Is.EqualTo(PerfMeterOverlayLayoutLimits.MaxWidth));
			Assert.That(parsed.style.gap, Is.EqualTo(PerfMeterOverlayLayoutLimits.MaxGap));
			Assert.That(FindWidget(parsed, "graphs.raw-frame-time").height, Is.EqualTo(PerfMeterOverlayLayoutLimits.MaxExplicitWidgetHeight));
			Assert.That(PerfMeterWidgetRegistry.TryGetDescriptor("graphs.raw-frame-time", out PerfMeterWidgetDescriptor rawDescriptor), Is.True);
			Assert.That(rawDescriptor.IsPresetBlock, Is.True);
			Assert.That((rawDescriptor.OverlayModules & PerfMeterOverlayModule.Graphs) != 0, Is.True);
		}

		private static PerfMeterWidgetDescriptor CreateExtensionDescriptor(string id, PerfMeterOverlayModule modules)
		{
			return new PerfMeterWidgetDescriptor(id, id, "Project", "Panel", modules.ToString(), "Test module-backed extension descriptor.", true, false, modules, "CustomMetrics");
		}

		private static PerfMeterOverlayPresetWidgetJson FindWidget(PerfMeterOverlayPresetJson preset, string id)
		{
			for (int i = 0; i < preset.widgets.Length; i++)
			{
				if (preset.widgets[i] != null && string.Equals(preset.widgets[i].id, id, StringComparison.Ordinal))
				{
					return preset.widgets[i];
				}
			}

			Assert.Fail("Missing widget " + id);
			return null;
		}
	}
}
