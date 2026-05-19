using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterSettingsTests
	{
		[Test]
		public void DefaultSettingsUseJsonZeroCodeValues()
		{
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;

			Assert.That(settings.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.Missing));
			Assert.That(settings.Enabled, Is.True);
			Assert.That(settings.AutoStart, Is.True);
			Assert.That(settings.OverlayVisible, Is.True);
			Assert.That(settings.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.TopRight));
			Assert.That(settings.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(settings.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(settings.ActivePreset, Is.EqualTo(PerfMeterSettingsStore.DefaultPresetId));
			AssertHasModule(settings.OverlayModules, PerfMeterOverlayModule.Fps);
			AssertHasModule(settings.OverlayModules, PerfMeterOverlayModule.Overdraw);
		}

		[Test]
		public void SettingsJsonRoundTripsSnapshotValues()
		{
			PerfMeterSettingsSnapshot source = new PerfMeterSettingsSnapshot(
				true,
				true,
				false,
				PerfMeterOverlayCorner.BottomLeft,
				PerfMeterOverlayMode.Graphs,
				PerfMeterTargetFps.Fps120,
				"Timing",
				PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Timing | PerfMeterOverlayModule.Graphs,
				PerfMeterSettingsLoadState.Loaded,
				string.Empty);

			string json = PerfMeterSettingsStore.ToJson(PerfMeterSettingsStore.CreateFromSnapshot(source));

			Assert.That(json, Does.Contain("schemaVersion"));
			Assert.That(PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot loaded), Is.True);
			Assert.That(loaded.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.Loaded));
			Assert.That(loaded.OverlayVisible, Is.False);
			Assert.That(loaded.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomLeft));
			Assert.That(loaded.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));
			Assert.That(loaded.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(loaded.ActivePreset, Is.EqualTo("Timing"));
			AssertHasModule(loaded.OverlayModules, PerfMeterOverlayModule.Graphs);
			AssertHasModule(loaded.OverlayModules, PerfMeterOverlayModule.Timing);
		}

		[Test]
		public void ActivePresetControlsOverlayModules()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activePreset = "Memory";

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			Assert.That(snapshot.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Memory);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Gc);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Warnings);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.Overdraw);
		}

		[Test]
		public void UnsupportedSettingsSchemaFallsBackSafely()
		{
			bool loaded = PerfMeterSettingsStore.TryReadSnapshot("{\"schemaVersion\":999,\"enabled\":true}", out PerfMeterSettingsSnapshot settings);

			Assert.That(loaded, Is.False);
			Assert.That(settings.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.UnsupportedVersion));
			Assert.That(settings.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(settings.Warning, Does.Contain("newer than supported"));
		}

		[Test]
		public void InvalidSettingsJsonFallsBackSafely()
		{
			bool loaded = PerfMeterSettingsStore.TryReadSnapshot(string.Empty, out PerfMeterSettingsSnapshot settings);

			Assert.That(loaded, Is.False);
			Assert.That(settings.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.Invalid));
			Assert.That(settings.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(settings.Warning, Does.Contain("empty"));
		}

		private static void AssertHasModule(PerfMeterOverlayModule actual, PerfMeterOverlayModule expected)
		{
			Assert.That((actual & expected) == expected, Is.True);
		}

		private static void AssertDoesNotHaveModule(PerfMeterOverlayModule actual, PerfMeterOverlayModule expected)
		{
			Assert.That((actual & expected) == 0, Is.True);
		}
	}
}
