using System;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterPlatformTelemetryTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterPlatformTelemetryRegistry.ClearForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterPlatformTelemetryRegistry.ClearForTests();
		}

		[Test]
		public void MissingProviderReportsExplicitUnavailableState()
		{
			PerfMeterPlatformTelemetrySnapshot snapshot = PerformanceMeter.GetPlatformTelemetry();

			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(snapshot.ProviderId, Is.Empty);
			Assert.That(snapshot.TemperatureLevelAvailable, Is.False);
			Assert.That(snapshot.Warning, Does.Contain("No platform telemetry provider"));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void RegisteredProviderPublishesNormalizedSnapshotWithoutStartingRuntime()
		{
			FakeProvider provider = new FakeProvider("fake.platform", CreateTelemetry("spoofed", PerfMeterThermalWarningLevel.ThrottlingImminent));
			PerformanceMeter.RegisterPlatformTelemetryProvider(provider);

			PerfMeterPlatformTelemetrySnapshot snapshot = PerformanceMeter.GetPlatformTelemetry();

			Assert.That(snapshot.IsAvailable, Is.True);
			Assert.That(snapshot.ProviderId, Is.EqualTo("fake.platform"));
			Assert.That(snapshot.ProviderVersion, Is.EqualTo("1.2.3"));
			Assert.That(snapshot.ThermalWarningLevel, Is.EqualTo(PerfMeterThermalWarningLevel.ThrottlingImminent));
			Assert.That(snapshot.TemperatureLevel, Is.EqualTo(0.75f));
			Assert.That(snapshot.TemperatureTrend, Is.EqualTo(0.25f));
			Assert.That(snapshot.CpuPerformanceLevel, Is.EqualTo(2));
			Assert.That(snapshot.GpuPerformanceLevel, Is.EqualTo(3));
			Assert.That(snapshot.PerformanceBottleneck, Is.EqualTo(PerfMeterAdaptiveBottleneck.Gpu));
			Assert.That(snapshot.LastAttemptResult, Is.EqualTo(PerfMeterPlatformTelemetryCollectionResult.Collected));
			Assert.That(snapshot.Freshness, Is.EqualTo(PerfMeterPlatformTelemetryFreshness.Fresh));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void SamplerUsesBoundedCadenceAndPublishesSampleAge()
		{
			FakeProvider provider = new FakeProvider("cadence", CreateTelemetry("cadence", PerfMeterThermalWarningLevel.Nominal));
			PerformanceMeter.RegisterPlatformTelemetryProvider(provider);

			PerfMeterPlatformTelemetrySnapshot first = PerfMeterPlatformTelemetryRegistry.Sample(10d);
			PerfMeterPlatformTelemetrySnapshot cached = PerfMeterPlatformTelemetryRegistry.Sample(10.1d);
			PerfMeterPlatformTelemetrySnapshot next = PerfMeterPlatformTelemetryRegistry.Sample(10.25d);

			Assert.That(provider.CollectCount, Is.EqualTo(2));
			Assert.That(first.LastAttemptTimeSeconds, Is.EqualTo(10d));
			Assert.That(cached.LastAttemptTimeSeconds, Is.EqualTo(10d));
			Assert.That(cached.SampleAgeSeconds, Is.EqualTo(0.1d).Within(0.0001d));
			Assert.That(next.LastAttemptTimeSeconds, Is.EqualTo(10.25d));
			Assert.That(next.SampleAgeSeconds, Is.Zero);
		}

		[Test]
		public void ForcedCaptureBoundaryBypassesCadence()
		{
			FakeProvider provider = new FakeProvider("capture", CreateTelemetry("capture", PerfMeterThermalWarningLevel.Nominal));
			PerformanceMeter.RegisterPlatformTelemetryProvider(provider);
			PerfMeterPlatformTelemetryRegistry.Sample(20d);

			PerfMeterPlatformTelemetrySnapshot forced = PerfMeterPlatformTelemetryRegistry.Sample(20.01d, force: true, captureBoundary: true);

			Assert.That(provider.CollectCount, Is.EqualTo(2));
			Assert.That(forced.ForcedAtCaptureBoundary, Is.True);
			Assert.That(forced.LastAttemptTimeSeconds, Is.EqualTo(20.01d));
			Assert.That(forced.LastSuccessTimeSeconds, Is.EqualTo(20.01d));
		}

		[Test]
		public void MemoryCaptureBoundaryForcesRuntimeTelemetryAttempt()
		{
			FakeProvider provider = new FakeProvider("capture.runtime", CreateTelemetry("capture.runtime", PerfMeterThermalWarningLevel.Nominal));
			PerformanceMeter.RegisterPlatformTelemetryProvider(provider);

			PerfMeterMemorySnapshotRequestResult result = PerformanceMeter.RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions(
				"telemetry-boundary",
				minimumFreeDiskBytes: 0L,
				cooldownSeconds: 0d));
			PerfMeterPlatformTelemetrySnapshot telemetry = PerformanceMeter.GetPlatformTelemetry();

			Assert.That(result, Is.EqualTo(PerfMeterMemorySnapshotRequestResult.Unavailable));
			Assert.That(provider.CollectCount, Is.EqualTo(1));
			Assert.That(telemetry.ForcedAtCaptureBoundary, Is.True);
			Assert.That(telemetry.LastAttemptResult, Is.EqualTo(PerfMeterPlatformTelemetryCollectionResult.Collected));
		}

		[Test]
		public void FailedAttemptPreservesUnavailableProvenanceAndLastSuccessAge()
		{
			FakeProvider provider = new FakeProvider("failure", CreateTelemetry("failure", PerfMeterThermalWarningLevel.Nominal));
			PerformanceMeter.RegisterPlatformTelemetryProvider(provider);
			PerfMeterPlatformTelemetryRegistry.Sample(30d);
			provider.ReturnNoSample = true;

			PerfMeterPlatformTelemetrySnapshot failed = PerfMeterPlatformTelemetryRegistry.Sample(30.1d, force: true);

			Assert.That(failed.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(failed.ProviderId, Is.EqualTo("failure"));
			Assert.That(failed.LastAttemptResult, Is.EqualTo(PerfMeterPlatformTelemetryCollectionResult.ProviderReturnedNoSample));
			Assert.That(failed.LastSuccessTimeSeconds, Is.EqualTo(30d));
			Assert.That(failed.SampleAgeSeconds, Is.EqualTo(0.1d).Within(0.0001d));
			Assert.That(failed.Freshness, Is.EqualTo(PerfMeterPlatformTelemetryFreshness.Fresh));
		}

		[Test]
		public void CachedTelemetryBecomesExplicitlyStale()
		{
			PerfMeterPlatformTelemetrySampler sampler = new PerfMeterPlatformTelemetrySampler(sampleIntervalSeconds: 10d, staleAfterSeconds: 0.5d);
			PerfMeterPlatformTelemetryCollector collector = delegate(out PerfMeterPlatformTelemetryCollectionResult result)
			{
				result = PerfMeterPlatformTelemetryCollectionResult.Collected;
				return CreateTelemetry("stale", PerfMeterThermalWarningLevel.Nominal);
			};
			sampler.Sample(40d, false, false, collector);

			PerfMeterPlatformTelemetrySnapshot stale = sampler.Sample(41d, false, false, collector);

			Assert.That(stale.IsAvailable, Is.True);
			Assert.That(stale.Freshness, Is.EqualTo(PerfMeterPlatformTelemetryFreshness.Stale));
			Assert.That(stale.SampleAgeSeconds, Is.EqualTo(1d));
		}

		[Test]
		public void ProviderFailureIsContainedAndDuplicateProviderIsRejected()
		{
			FakeProvider provider = new FakeProvider("failing", CreateTelemetry("failing", PerfMeterThermalWarningLevel.Nominal)) { ThrowOnCollect = true };
			PerformanceMeter.RegisterPlatformTelemetryProvider(provider);
			Assert.Throws<InvalidOperationException>(() => PerformanceMeter.RegisterPlatformTelemetryProvider(new FakeProvider("other", CreateTelemetry("other", PerfMeterThermalWarningLevel.Nominal))));

			PerfMeterPlatformTelemetrySnapshot snapshot = PerformanceMeter.GetPlatformTelemetry();

			Assert.That(snapshot.IsAvailable, Is.False);
			Assert.That(snapshot.ProviderId, Is.EqualTo("failing"));
			Assert.That(snapshot.Warning, Does.Contain("InvalidOperationException"));
		}

		[Test]
		public void SessionExportsPreserveTelemetryAndNullUnavailableValues()
		{
			PerfMeterPlatformTelemetrySnapshot telemetry = CreateTelemetry("session.provider", PerfMeterThermalWarningLevel.Throttling);
			PerfMeterSessionSampleSnapshot sample = new PerfMeterSessionSampleSnapshot(7, 1.5d, "Scene", PerfMeterMetricsSnapshot.Stopped, Array.Empty<PerfMeterCustomMetricSnapshot>(), telemetry);

			string json = PerfMeterSessionExporter.BuildJson(PerfMeterSessionSummarySnapshot.Empty, new[] { sample }, PerformanceMeter.GetStatus());
			string csv = PerfMeterSessionExporter.BuildCsv(PerfMeterSessionSummarySnapshot.Empty, new[] { sample }, PerformanceMeter.GetStatus());
			PerfMeterSessionSampleSnapshot unavailableSample = new PerfMeterSessionSampleSnapshot(8, 2d, "Scene", PerfMeterMetricsSnapshot.Stopped);
			string unavailableJson = PerfMeterSessionExporter.BuildJson(PerfMeterSessionSummarySnapshot.Empty, new[] { unavailableSample }, PerformanceMeter.GetStatus());

			Assert.That(json, Does.Contain("\"platform_telemetry\""));
			Assert.That(json, Does.Contain("\"provider_id\":\"session.provider\""));
			Assert.That(json, Does.Contain("\"thermal_warning_level\":\"Throttling\""));
			Assert.That(json, Does.Contain("\"temperature_level\":0.75"));
			Assert.That(csv, Does.Contain("unavailable_counters,platform_telemetry_available,platform_telemetry_provider,platform_telemetry_provider_version,thermal_warning_level"));
			Assert.That(csv, Does.Contain("true,\"session.provider\",\"1.2.3\",\"Throttling\",0.75,0.25,2,3,\"Gpu\""));
			Assert.That(unavailableJson, Does.Contain("\"thermal_warning_level\":null"));
			Assert.That(unavailableJson, Does.Contain("\"temperature_level\":null"));
			Assert.That(unavailableJson, Does.Contain("\"cpu_performance_level\":null"));
			Assert.That(unavailableJson, Does.Contain("\"performance_bottleneck\":null"));
		}

		[Test]
		public void ThermalAlertRequiresAvailableImminentOrThrottlingState()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("thermal.throttling", PerfMeterMetric.ThermalWarningLevel, PerfMeterComparison.GreaterThanOrEqual, (int)PerfMeterThermalWarningLevel.ThrottlingImminent, 1, 0f, PerfMeterAlertAction.Callback)
			});
			int fired = 0;
			Action<PerfMeterAlertSnapshot> handler = _ => fired++;
			PerformanceMeter.AlertFired += handler;
			try
			{
				engine.Evaluate(PerfMeterMetricsSnapshot.Stopped, PerfMeterPlatformTelemetrySnapshot.Unavailable(), 1d, PerfMeterAlertClassification.SteadyState, string.Empty);
				engine.Evaluate(PerfMeterMetricsSnapshot.Stopped, CreateTelemetry("alert", PerfMeterThermalWarningLevel.Nominal), 2d, PerfMeterAlertClassification.SteadyState, string.Empty);
				engine.Evaluate(PerfMeterMetricsSnapshot.Stopped, CreateTelemetry("alert", PerfMeterThermalWarningLevel.ThrottlingImminent), 3d, PerfMeterAlertClassification.SteadyState, string.Empty);

				Assert.That(fired, Is.EqualTo(1));
				Assert.That(engine.GetLatestAlerts(), Has.Length.EqualTo(1));
				Assert.That(engine.GetLatestAlerts()[0].Metric, Is.EqualTo(PerfMeterMetric.ThermalWarningLevel));
			}
			finally
			{
				PerformanceMeter.AlertFired -= handler;
			}
		}

		[Test]
		public void PlatformTelemetryMcpIsRegisteredAndStructured()
		{
			FakeProvider provider = new FakeProvider("mcp.provider", CreateTelemetry("mcp.provider", PerfMeterThermalWarningLevel.Nominal));
			PerformanceMeter.RegisterPlatformTelemetryProvider(provider);
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();
			string json = PerfMeterMcpCommands.PlatformTelemetry();

			Assert.That(metadata, Does.Contain("perfmeter.platform.telemetry"));
			Assert.That(metadata, Does.Contain("PerfMeterMcpCommands.PlatformTelemetry"));
			Assert.That(json, Does.Contain("\"availability\":\"Available\""));
			Assert.That(json, Does.Contain("\"provider_id\":\"mcp.provider\""));
			Assert.That(json, Does.Contain("\"temperature_level\":0.75"));
			Assert.That(json, Does.Contain("\"last_attempt_result\":\"Collected\""));
			Assert.That(json, Does.Contain("\"freshness\":\"Fresh\""));
			Assert.That(json, Does.Contain("\"forced_at_capture_boundary\":false"));
			Assert.That(json, Does.Contain("\"is_playing\":"));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);

			PerformanceMeter.UnregisterPlatformTelemetryProvider(provider);
			string unavailableJson = PerfMeterMcpCommands.PlatformTelemetry();
			Assert.That(unavailableJson, Does.Contain("\"thermal_warning_level\":null"));
			Assert.That(unavailableJson, Does.Contain("\"performance_bottleneck\":null"));
		}

		[Test]
		public void OptionalAssemblyUsesVersionDefineWithoutCoreDependency()
		{
			string asmdef = PerfMeterTestAssets.ReadAdaptivePerformanceAsmdef();
			string packageJson = PerfMeterTestAssets.ReadPackageJson();

			Assert.That(asmdef, Does.Contain("\"Unity.AdaptivePerformance\""));
			Assert.That(asmdef, Does.Contain("\"com.unity.adaptiveperformance\""));
			Assert.That(asmdef, Does.Contain("\"expression\": \"5.1.0\""));
			Assert.That(asmdef, Does.Contain("SGG_PERFMETER_ADAPTIVE_PERFORMANCE_5_1_OR_NEWER"));
			Assert.That(packageJson, Does.Not.Contain("\"com.unity.adaptiveperformance\""));
		}

		private static PerfMeterPlatformTelemetrySnapshot CreateTelemetry(string providerId, PerfMeterThermalWarningLevel warningLevel)
		{
			return new PerfMeterPlatformTelemetrySnapshot(
				PerfMeterAvailability.Available,
				providerId,
				"1.2.3",
				10d,
				9d,
				true,
				warningLevel,
				true,
				0.75f,
				true,
				0.25f,
				true,
				2,
				true,
				3,
				true,
				PerfMeterAdaptiveBottleneck.Gpu);
		}

		private sealed class FakeProvider : IPerfMeterPlatformTelemetryProvider
		{
			private readonly PerfMeterPlatformTelemetrySnapshot _snapshot;

			internal FakeProvider(string id, PerfMeterPlatformTelemetrySnapshot snapshot)
			{
				Id = id;
				_snapshot = snapshot;
			}

			public string Id { get; }
			internal bool ThrowOnCollect { get; set; }
			internal bool ReturnNoSample { get; set; }
			internal int CollectCount { get; private set; }

			public bool TryCollect(out PerfMeterPlatformTelemetrySnapshot snapshot)
			{
				CollectCount++;
				if (ThrowOnCollect)
				{
					throw new InvalidOperationException("provider failure");
				}

				snapshot = _snapshot;
				return !ReturnNoSample;
			}
		}
	}
}
