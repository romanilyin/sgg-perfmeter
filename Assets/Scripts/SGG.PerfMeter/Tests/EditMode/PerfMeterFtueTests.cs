using System.Reflection;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Setup;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterFtueTests
	{
		[SetUp]
		public void SetUp()
		{
			PerfMeterFtueState.ResetChoices();
		}

		[TearDown]
		public void TearDown()
		{
			PerfMeterFtueState.ResetChoices();
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
			Assert.That(PerfMeterFtueState.PackageVersion, Is.EqualTo("2026.8.7-2"));
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
	}
}
