using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterProfilerMetricCatalogTests
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
		public void ResolveMetricNamePrefersExactNameOverAvailableAlias()
		{
			List<PerfMeterProfilerMetricDescriptor> availableMetrics = new List<PerfMeterProfilerMetricDescriptor>
			{
				new PerfMeterProfilerMetricDescriptor("Render", "Legacy Draw Calls"),
				new PerfMeterProfilerMetricDescriptor("Render", "Preferred Draw Calls")
			};

			PerfMeterProfilerMetricResolution resolution = PerfMeterProfilerMetricCatalog.ResolveMetricName(
				"Render",
				"Preferred Draw Calls",
				new[] { "Legacy Draw Calls" },
				availableMetrics,
				out PerfMeterProfilerMetricDescriptor descriptor);

			Assert.That(resolution, Is.EqualTo(PerfMeterProfilerMetricResolution.Exact));
			Assert.That(descriptor.Name, Is.EqualTo("Preferred Draw Calls"));
		}

		[Test]
		public void ResolveMetricNameUsesAliasesInDeclaredOrder()
		{
			List<PerfMeterProfilerMetricDescriptor> availableMetrics = new List<PerfMeterProfilerMetricDescriptor>
			{
				new PerfMeterProfilerMetricDescriptor("Render", "Second Alias"),
				new PerfMeterProfilerMetricDescriptor("Render", "First Alias")
			};

			PerfMeterProfilerMetricResolution resolution = PerfMeterProfilerMetricCatalog.ResolveMetricName(
				"Render",
				"Missing Exact Name",
				new[] { "First Alias", "Second Alias" },
				availableMetrics,
				out PerfMeterProfilerMetricDescriptor descriptor);

			Assert.That(resolution, Is.EqualTo(PerfMeterProfilerMetricResolution.Alias));
			Assert.That(descriptor.Name, Is.EqualTo("First Alias"));
		}

		[Test]
		public void ResolveMetricNameMatchesCategoryAlongsideName()
		{
			List<PerfMeterProfilerMetricDescriptor> wrongCategoryOnly = new List<PerfMeterProfilerMetricDescriptor>
			{
				new PerfMeterProfilerMetricDescriptor("Memory", "Shared Counter")
			};

			PerfMeterProfilerMetricResolution unresolved = PerfMeterProfilerMetricCatalog.ResolveMetricName(
				"Render",
				"Shared Counter",
				Array.Empty<string>(),
				wrongCategoryOnly,
				out PerfMeterProfilerMetricDescriptor unresolvedDescriptor);

			Assert.That(unresolved, Is.EqualTo(PerfMeterProfilerMetricResolution.None));
			Assert.That(unresolvedDescriptor.Name, Is.Null);

			wrongCategoryOnly.Add(new PerfMeterProfilerMetricDescriptor("Render", "Shared Counter"));
			PerfMeterProfilerMetricResolution resolved = PerfMeterProfilerMetricCatalog.ResolveMetricName(
				"Render",
				"Shared Counter",
				Array.Empty<string>(),
				wrongCategoryOnly,
				out PerfMeterProfilerMetricDescriptor resolvedDescriptor);

			Assert.That(resolved, Is.EqualTo(PerfMeterProfilerMetricResolution.Exact));
			Assert.That(resolvedDescriptor.Category, Is.EqualTo("Render"));
		}

		[Test]
		public void ResolveMetricNameReturnsUnresolvedForMissingExactAndAliases()
		{
			List<PerfMeterProfilerMetricDescriptor> availableMetrics = new List<PerfMeterProfilerMetricDescriptor>
			{
				new PerfMeterProfilerMetricDescriptor("Render", "Other Counter")
			};

			PerfMeterProfilerMetricResolution resolution = PerfMeterProfilerMetricCatalog.ResolveMetricName(
				"Render",
				"Missing Exact Name",
				new[] { "Missing Alias" },
				availableMetrics,
				out PerfMeterProfilerMetricDescriptor descriptor);

			Assert.That(resolution, Is.EqualTo(PerfMeterProfilerMetricResolution.None));
			Assert.That(descriptor.Category, Is.Null);
			Assert.That(descriptor.Name, Is.Null);
		}

		[Test]
		public void GraphicsCreationMarkersKeepExactAliasAndUnavailableProvenance()
		{
			List<PerfMeterProfilerMetricDescriptor> availableMetrics = new List<PerfMeterProfilerMetricDescriptor>
			{
				new PerfMeterProfilerMetricDescriptor("Render", "Shader.CompileGPUProgram", "Nanoseconds", "Int64"),
				new PerfMeterProfilerMetricDescriptor("Render", "CreatePSO.Job", "Nanoseconds", "Int64")
			};

			Assert.That(
				PerfMeterProfilerMetricCatalog.ResolveMetricName(
					"Render",
					"Shader.CreateGPUProgram",
					new[] { "Shader.CreateGPUPrograms", "Shader.CompileGPUProgram", "Shader.DynamicLoadGPUProgram" },
					availableMetrics,
					out PerfMeterProfilerMetricDescriptor shader),
				Is.EqualTo(PerfMeterProfilerMetricResolution.Alias));
			Assert.That(shader.Name, Is.EqualTo("Shader.CompileGPUProgram"));
			Assert.That(
				PerfMeterProfilerMetricCatalog.ResolveMetricName("Render", "CreatePSO.Job", Array.Empty<string>(), availableMetrics, out _),
				Is.EqualTo(PerfMeterProfilerMetricResolution.Exact));
			Assert.That(
				PerfMeterProfilerMetricCatalog.ResolveMetricName("Render", "MissingPSO", Array.Empty<string>(), availableMetrics, out _),
				Is.EqualTo(PerfMeterProfilerMetricResolution.None));
		}

		[Test]
		public void GetSampleStateDistinguishesUnavailableNoSampleAndSampled()
		{
			Assert.That(
				PerfMeterProfilerMetricCatalog.GetSampleState(0, 0),
				Is.EqualTo(PerfMeterProfilerMetricSampleState.Unavailable));
			Assert.That(
				PerfMeterProfilerMetricCatalog.GetSampleState(1, 0),
				Is.EqualTo(PerfMeterProfilerMetricSampleState.AvailableNoSample));

			// A zero-valued recorder is still sampled when its sample count is positive.
			Assert.That(
				PerfMeterProfilerMetricCatalog.GetSampleState(1, 1),
				Is.EqualTo(PerfMeterProfilerMetricSampleState.AvailableSampled));
		}

		[Test]
		public void StoppedPublicCatalogSnapshotAndRefreshDoNotStartRuntime()
		{
			PerfMeterProfilerMetricCatalogSnapshot snapshot = PerformanceMeter.GetProfilerMetricCatalog();

			Assert.That(snapshot.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.NotInitialized));
			Assert.That(snapshot.Revision, Is.Zero);
			Assert.That(snapshot.DiscoveryCount, Is.Zero);
			Assert.That(snapshot.Capabilities, Is.Empty);
			Assert.That(PerformanceMeter.GetProfilerMetricCapabilities(), Is.Empty);
			Assert.That(PerformanceMeter.TryRefreshProfilerMetricCatalog(), Is.False);
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));

			PerfMeterProfilerMetricCatalogSnapshot afterRefresh = PerformanceMeter.GetProfilerMetricCatalog();
			Assert.That(afterRefresh.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.NotInitialized));
			Assert.That(afterRefresh.DiscoveryCount, Is.Zero);
		}

		[Test]
		public void RefreshKeepsPreviousReadyCatalogWhenCandidateRecorderFails()
		{
			int discoveryCount = 0;
			bool failDiscovery = false;
			List<string> startAttempts = new List<string>();
			PerfMeterProfilerMetricCatalog catalog = new PerfMeterProfilerMetricCatalog(
				availableMetrics =>
				{
					if (failDiscovery)
					{
						throw new InvalidOperationException("Refresh discovery failed.");
					}

					if (discoveryCount++ > 0)
					{
						availableMetrics.Add(new PerfMeterProfilerMetricDescriptor("Render", "SetPass Calls Count"));
						availableMetrics.Add(new PerfMeterProfilerMetricDescriptor("Render", "SetPass Calls"));
					}
				},
				(_, name, _) =>
				{
					startAttempts.Add(name);
					if (name == "SetPass Calls Count")
					{
						throw new InvalidOperationException("SetPass Calls Count failed.");
					}

					return default;
				});

			try
			{
				catalog.Start();
				PerfMeterProfilerMetricCatalogSnapshot initial = catalog.GetSnapshot();
				Assert.That(initial.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.Ready));
				Assert.That(initial.Revision, Is.EqualTo(1));

				Assert.That(catalog.Refresh(), Is.False);
				PerfMeterProfilerMetricCatalogSnapshot afterFailure = catalog.GetSnapshot();
				Assert.That(afterFailure.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.Ready));
				Assert.That(afterFailure.Revision, Is.EqualTo(initial.Revision));
				Assert.That(afterFailure.DiscoveryCount, Is.EqualTo(initial.DiscoveryCount + 1));
				Assert.That(afterFailure.LastError, Does.Contain("SetPass Calls Count"));
				Assert.That(startAttempts, Is.EqualTo(new[] { "SetPass Calls Count", "SetPass Calls" }));
				Assert.That(afterFailure.Capabilities[(int)PerfMeterProfilerMetricSemantic.SetPassCalls].SampleState,
					Is.EqualTo(PerfMeterProfilerMetricSampleState.Unavailable));

				failDiscovery = true;
				Assert.That(catalog.Refresh(), Is.False);
				PerfMeterProfilerMetricCatalogSnapshot afterDiscoveryFailure = catalog.GetSnapshot();
				Assert.That(afterDiscoveryFailure.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.Ready));
				Assert.That(afterDiscoveryFailure.Revision, Is.EqualTo(initial.Revision));
				Assert.That(afterDiscoveryFailure.DiscoveryCount, Is.EqualTo(afterFailure.DiscoveryCount + 1));
				Assert.That(afterDiscoveryFailure.LastError, Is.EqualTo("Refresh discovery failed."));
			}
			finally
			{
				catalog.Stop();
			}
		}

		[Test]
		public void InitialDiscoveryFailureReportsEverySemanticCounterUnavailable()
		{
			PerfMeterProfilerMetricCatalog catalog = new PerfMeterProfilerMetricCatalog(
				_ => throw new InvalidOperationException("Discovery failed."),
				null);

			try
			{
				catalog.Start();
				PerfMeterProfilerMetricCatalogSnapshot snapshot = catalog.GetSnapshot();

				Assert.That(snapshot.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.Error));
				Assert.That(snapshot.Revision, Is.Zero);
				Assert.That(snapshot.DiscoveryCount, Is.EqualTo(1));
				Assert.That(snapshot.LastError, Is.EqualTo("Discovery failed."));
				Assert.That(snapshot.Capabilities, Has.Length.EqualTo(13));
				Assert.That(snapshot.Capabilities, Has.All.Property("SampleState").EqualTo(PerfMeterProfilerMetricSampleState.Unavailable));
				Assert.That(catalog.UnavailableCounters & PerfMeterCounterAvailability.DrawCalls,
					Is.EqualTo(PerfMeterCounterAvailability.DrawCalls));
				Assert.That(catalog.UnavailableCounters & PerfMeterCounterAvailability.GpuMemory,
					Is.EqualTo(PerfMeterCounterAvailability.GpuMemory));
			}
			finally
			{
				catalog.Stop();
			}
		}

	}
}
