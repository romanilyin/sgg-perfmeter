using System.Reflection;
using NUnit.Framework;
using SGG.PerfMeter.Editor.UI;
using SGG.PerfMeter.Editor.UI.Localization;
using SGG.PerfMeter.Editor.Setup;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterFtueTests
	{
		private string _previousLanguage;

		[SetUp]
		public void SetUp()
		{
			PerfMeterFtueState.ResetChoices();
			_previousLanguage = PerfMeterWindowLocalization.CurrentLanguage;
			PerfMeterWindowLocalization.CurrentLanguage = PerfMeterWindowLocalization.DefaultLanguage;
		}

		[TearDown]
		public void TearDown()
		{
			PerfMeterFtueState.ResetChoices();
			PerfMeterWindowLocalization.CurrentLanguage = _previousLanguage;
		}

		[Test]
		public void OptionalDependencySpecsUseRequiredVersions()
		{
			Assert.That(PerfMeterOptionalDependencyInstaller.MemoryProfilerPackageSpec, Is.EqualTo("com.unity.memoryprofiler@1.1.12"));
			Assert.That(PerfMeterOptionalDependencyInstaller.AdaptivePerformancePackageSpec, Is.EqualTo("com.unity.adaptiveperformance@5.1.6"));
			Assert.That(PerfMeterOptionalDependencyInstaller.ProfileAnalyzerPackageSpec, Is.EqualTo("com.unity.performance.profile-analyzer@1.4.0"));
		}

		[TestCase("1.1.12", "1.1.12", true)]
		[TestCase("1.1.13", "1.1.12", true)]
		[TestCase("1.10.0", "1.9.99", true)]
		[TestCase("1.1.12-preview.1", "1.1.12", true)]
		[TestCase("17.4.0f1", "17.4", true)]
		[TestCase("1.1.11", "1.1.12", false)]
		[TestCase("1.2", "1.2.1", false)]
		[TestCase("malformed", "1.1.12", false)]
		[TestCase("1.1.12_bad", "1.1.12", false)]
		public void VersionComparisonHandlesDottedVersionsAndSuffixes(string current, string minimum, bool expected)
		{
			Assert.That(PerfMeterOptionalDependencyInstaller.IsVersionAtLeast(current, minimum), Is.EqualTo(expected));
		}

		[Test]
		public void VersionComparisonRejectsMissingVersions()
		{
			Assert.That(PerfMeterOptionalDependencyInstaller.IsVersionAtLeast(null, "1.1.12"), Is.False);
			Assert.That(PerfMeterOptionalDependencyInstaller.IsVersionAtLeast("1.1.12", null), Is.False);
			Assert.That(PerfMeterOptionalDependencyInstaller.IsVersionAtLeast(string.Empty, "1.1.12"), Is.False);
		}

		[Test]
		public void CompletionRequiresAllRequiredAndOptionalChecks()
		{
			Assert.That(PerfMeterFtueState.AreAllStepsResolved(null, new[] { true }), Is.False);
			Assert.That(PerfMeterFtueState.AreAllStepsResolved(new bool[0], new[] { true }), Is.False);
			Assert.That(PerfMeterFtueState.AreAllStepsResolved(new[] { false }, new[] { true }), Is.False);
			Assert.That(PerfMeterFtueState.AreAllStepsResolved(new[] { true }, new[] { false }), Is.False);
			Assert.That(PerfMeterFtueState.AreAllStepsResolved(new[] { true, true }, new[] { true, true }), Is.True);
			Assert.That(PerfMeterFtueState.AreAllStepsResolved(new[] { true }, new bool[0]), Is.True);
			Assert.That(PerfMeterFtueState.AreAllStepsResolved(new[] { true }, null), Is.False);
		}

		[Test]
		public void AmbiguousExternalProfilerAttachmentDoesNotResolveBothTools()
		{
			Assert.That(PerfMeterFtueState.ResolveExternalToolAvailability(true, false, false), Is.True);
			Assert.That(PerfMeterFtueState.ResolveExternalToolAvailability(true, true, false), Is.False);
			Assert.That(PerfMeterFtueState.ResolveExternalToolAvailability(true, true, true), Is.True);
			Assert.That(PerfMeterFtueState.ResolveExternalToolAvailability(false, true, true), Is.False);
		}

		[Test]
		public void PackageVersionComesFromRuntimeAssemblyMetadata()
		{
			Assert.That(PerfMeterFtueState.PackageVersion, Is.Not.Null.And.Not.Empty);

			object[] attributes = typeof(PerformanceMeter).Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
			string metadataVersion = string.Empty;
			for (int index = 0; index < attributes.Length; index++)
			{
				AssemblyMetadataAttribute metadata = (AssemblyMetadataAttribute)attributes[index];
				if (metadata.Key == "SGG.PerfMeter.PackageVersion")
				{
					metadataVersion = metadata.Value;
					break;
				}
			}

			Assert.That(metadataVersion, Is.Not.Empty);
			Assert.That(PerfMeterFtueState.PackageVersion, Is.EqualTo(metadataVersion));
			Assert.That(PerfMeterFtueState.PackageVersion, Is.EqualTo("2026.8.9-1"));
			Assert.That(PerfMeterFtueState.ProjectKey, Is.Not.Empty);
		}

		[Test]
		public void SkipChoicesRoundTripAndReset()
		{
			Assert.That(PerfMeterFtueState.IsSkipped(PerfMeterFtueState.MemoryProfilerId), Is.False);

			PerfMeterFtueState.SetSkipped(PerfMeterFtueState.MemoryProfilerId);
			PerfMeterFtueState.SetSkipped(PerfMeterFtueState.PixId);
			Assert.That(PerfMeterFtueState.IsSkipped(PerfMeterFtueState.MemoryProfilerId), Is.True);
			Assert.That(PerfMeterFtueState.IsSkipped(PerfMeterFtueState.PixId), Is.True);
			Assert.That(PerfMeterFtueState.IsOptionalResolved(PerfMeterFtueState.MemoryProfilerId, false), Is.True);
			Assert.That(PerfMeterFtueState.IsOptionalResolved(PerfMeterFtueState.AdaptivePerformanceId, true), Is.True);

			PerfMeterFtueState.SetSkipped(PerfMeterFtueState.MemoryProfilerId, false);
			Assert.That(PerfMeterFtueState.IsSkipped(PerfMeterFtueState.MemoryProfilerId), Is.False);

			PerfMeterFtueState.ResetChoices();
			Assert.That(PerfMeterFtueState.IsSkipped(PerfMeterFtueState.MemoryProfilerId), Is.False);
			Assert.That(PerfMeterFtueState.IsSkipped(PerfMeterFtueState.PixId), Is.False);
		}

		[Test]
		public void OptionalContinuationSnippetsUsePublicOneShotApis()
		{
			string memorySnippet = PerfMeterFtuePage.BuildMemorySnapshotSnippet();
			Assert.That(memorySnippet, Does.Contain("PerformanceMeter.RequestMemorySnapshot("));
			Assert.That(memorySnippet, Does.Contain("new PerfMeterMemorySnapshotOptions(\"ftue-memory-snapshot\")"));
			Assert.That(memorySnippet, Does.Not.Contain("ConfigureMemorySnapshotTriggers"));
			string triggerSnippet = PerfMeterFtuePage.BuildMemorySnapshotTriggerSnippet();
			Assert.That(triggerSnippet, Does.Contain("PerformanceMeter.ConfigureMemorySnapshotTriggers("));
			Assert.That(triggerSnippet, Does.Contain("new PerfMeterMemorySnapshotTriggerOptions("));
			Assert.That(triggerSnippet, Does.Contain("systemMemoryThresholdBytes:"));

			string traceSnippet = PerfMeterFtuePage.BuildGraphicsStateTraceSnippet();
			Assert.That(traceSnippet, Does.Contain("PerformanceMeter.RequestGraphicsStateTrace("));
			Assert.That(traceSnippet, Does.Contain("new PerfMeterGraphicsStateTraceOptions(\"ftue-graphics-state-trace\", 60)"));

			string prewarmSnippet = PerfMeterFtuePage.BuildGraphicsStatePrewarmSnippet();
			Assert.That(prewarmSnippet, Does.Contain("PerformanceMeter.PrewarmGraphicsStateCollection("));
			Assert.That(prewarmSnippet, Does.Contain("new PerfMeterGraphicsStatePrewarmOptions(\"Temp/PerfMeter/GraphicsStateCollections/<trace-artifact-file>\")"));

			string renderDocSnippet = PerfMeterFtuePage.BuildRenderDocCaptureSnippet();
			Assert.That(renderDocSnippet, Does.Contain("PerformanceMeter.RequestCapture("));
			Assert.That(renderDocSnippet, Does.Contain("new PerfMeterCaptureOptions(\"ftue-renderdoc-capture\", PerfMeterCaptureTool.RenderDoc, 1)"));
		}

		[Test]
		public void InstalledOptionalStatusesDescribeContinuationWorkflows()
		{
			Assert.That(PerfMeterFtuePage.FormatMemoryProfilerInstalledStatus("1.1.12", "1.1.12"), Does.StartWith("Installed/Ready"));
			Assert.That(PerfMeterFtuePage.FormatMemoryProfilerInstalledStatus("1.1.12", "1.1.12"), Does.Contain("Memory Profiler 1.1.12"));

			string profileAnalyzerStatus = PerfMeterFtuePage.FormatProfileAnalyzerInstalledStatus("1.4.0", "1.4.0");
			Assert.That(profileAnalyzerStatus, Does.StartWith("Installed/Ready"));
			Assert.That(PerfMeterFtuePage.ProfileAnalyzerGuidance, Does.Contain("begin recording in Unity Profiler"));
			Assert.That(PerfMeterFtuePage.ProfileAnalyzerGuidance, Does.Contain("start and stop a PerfMeter session while recording"));

			Assert.That(PerfMeterFtuePage.FormatAdaptivePerformanceInstalledStatus("5.1.6", "5.1.6"), Does.Contain("Adaptive Performance 5.1.6"));
		}

		[Test]
		public void GraphicsStateCollectionStatusDescribesArtifactWorkflow()
		{
			PerfMeterGraphicsStateCollectionCapabilitiesSnapshot capabilities = new PerfMeterGraphicsStateCollectionCapabilitiesSnapshot(
				PerfMeterAvailability.Available,
				"fake-backend",
				"1.0",
				true,
				true,
				false,
				false,
				600,
				1024L,
				"Temp/PerfMeter/GraphicsStateCollections",
				string.Empty);

			string status = PerfMeterFtuePage.FormatGraphicsStateCollectionReadyStatus(capabilities);
			Assert.That(status, Does.StartWith("Installed/Ready"));
			Assert.That(status, Does.Contain("trace Available, prewarm Available"));
			Assert.That(PerfMeterFtuePage.GraphicsStateCollectionGuidance, Does.Contain("Temp/PerfMeter/GraphicsStateCollections"));
			Assert.That(PerfMeterFtuePage.GraphicsStateCollectionGuidance, Does.Contain("never starts trace or prewarm automatically"));
		}

		[Test]
		public void RenderDocStatusesAvoidInstallationAndArtifactClaims()
		{
			string unattached = PerfMeterFtuePage.FormatRenderDocUnattachedStatus("not attached");
			Assert.That(unattached, Does.StartWith("Not attached"));
			Assert.That(unattached, Does.Contain("not attached"));
			Assert.That(PerfMeterFtuePage.RenderDocGuidance, Does.Contain("Load RenderDoc from the Game or Scene View tab menu"));
			Assert.That(PerfMeterFtuePage.RenderDocGuidance, Does.Contain("Check attachment"));

			string attached = PerfMeterFtuePage.FormatRenderDocAttachedStatus();
			Assert.That(attached, Does.StartWith("Attached"));
			Assert.That(attached, Does.Contain("cannot identify RenderDoc versus PIX"));
			Assert.That(attached, Does.Contain("artifact path"));
		}

		[Test]
		public void AttachedExternalToolsHideDownloadAndSkipActions()
		{
			Assert.That(PerfMeterFtuePage.ShouldShowExternalDownload(false, false), Is.True);
			Assert.That(PerfMeterFtuePage.ShouldShowExternalDownload(true, false), Is.False);
			Assert.That(PerfMeterFtuePage.ShouldShowExternalDownload(false, true), Is.False);
			Assert.That(PerfMeterFtuePage.ShouldShowExternalSkip(false), Is.True);
			Assert.That(PerfMeterFtuePage.ShouldShowExternalSkip(true), Is.False);
		}
	}
}
