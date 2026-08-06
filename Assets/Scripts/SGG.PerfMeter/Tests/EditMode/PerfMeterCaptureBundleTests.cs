using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterCaptureBundleTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
		}

		[Test]
		public void BundleCoordinatorFreezesCorrelatedCaptureData()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("bundle-data", includeScreenshot: false);

			PerfMeterCaptureBundleStatusSnapshot status = coordinator.GetStatus("bundle-data");
			Assert.That(status.State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(status.CaptureSampleCount, Is.EqualTo(1));
			Assert.That(status.BaselineSampleCount, Is.EqualTo(1));
			Assert.That(status.AlertEventCount, Is.EqualTo(1));
			Assert.That(status.AlertEventsTruncated, Is.True);
			Assert.That(status.ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.NotRequested));
			Assert.That(coordinator.TryGetExportData("bundle-data", out PerfMeterCaptureBundleExportData data), Is.True);
			Assert.That(data.CaptureSamples, Has.Length.EqualTo(1));
			Assert.That(data.BaselineSamples, Has.Length.EqualTo(1));
			Assert.That(data.AlertEvents[0].CaptureId, Is.EqualTo("bundle-data"));
		}

		[Test]
		public void ScreenshotRequestStaysPendingUntilExplicitCompletion()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("screenshot", includeScreenshot: true, completeScreenshot: false);

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.PendingScreenshot));
			Assert.That(coordinator.TryGetExportData("screenshot", out _), Is.False);
			Assert.That(coordinator.TryStartScreenshot(out string captureId, out string bundleId), Is.True);
			coordinator.CompleteScreenshot(captureId, bundleId, null, "batch mode", true);

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(coordinator.GetStatus().ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.Unavailable));
			Assert.That(coordinator.TryGetExportData("screenshot", out _), Is.True);
		}

		[Test]
		public void PendingScreenshotBecomesExportableWhenRuntimeStops()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("interrupted-screenshot", includeScreenshot: true, completeScreenshot: false);

			coordinator.CompletePendingScreenshotAsUnavailable(coordinator.GetStatus().BundleId, "runtime stopped");

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(coordinator.GetStatus().ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.Unavailable));
			Assert.That(coordinator.GetStatus().Warning, Does.Contain("runtime stopped"));
			Assert.That(coordinator.TryGetExportData("interrupted-screenshot", out _), Is.True);
		}

		[Test]
		public void StaleScreenshotCompletionCannotMutateReplacementBundle()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("reused", includeScreenshot: true, completeScreenshot: false);
			Assert.That(coordinator.TryStartScreenshot(out string captureId, out string oldBundleId), Is.True);
			PerfMeterCaptureOptions replacement = new PerfMeterCaptureOptions("reused", PerfMeterCaptureTool.RenderDoc);
			coordinator.Start(replacement, new PerfMeterCaptureBundleOptions(includeScreenshot: true), CaptureStatus("reused", PerfMeterCaptureState.Capturing));
			coordinator.ObserveCapture(
				CaptureStatus("reused", PerfMeterCaptureState.Completed),
				PerfMeterSessionSummarySnapshot.Empty,
				Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				Array.Empty<PerfMeterAlertSnapshot>(),
				false);

			coordinator.CompleteScreenshot(captureId, oldBundleId, new byte[] { 1 }, string.Empty, false);

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.PendingScreenshot));
			Assert.That(coordinator.GetStatus().ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.Pending));
		}

		[Test]
		public void PublicCaptureApiRejectsAdvertisedLimitViolationsBeforeStartingRuntime()
		{
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions(" ", PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions(new string('x', 129), PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("bad\u0001id", PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("too-many", PerfMeterCaptureTool.RenderDoc, 121, 0, 0)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("too-much-roll", PerfMeterCaptureTool.RenderDoc, 1, 601, 0)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void BundleExportCommitsAtomicallyAndRefusesExistingDestination()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("atomic", includeScreenshot: false);
			Assert.That(coordinator.TryGetExportData("atomic", out PerfMeterCaptureBundleExportData data), Is.True);
			string directoryName = "test-atomic-" + Guid.NewGuid().ToString("N");
			string relativePath = PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/" + directoryName;
			string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

			try
			{
				PerfMeterCaptureBundleExportResult first = PerfMeterCaptureBundleExporter.Export(data, relativePath, null, false);
				Assert.That(first.Success, Is.True, first.Error);
				Assert.That(first.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.Exported));
				Assert.That(File.Exists(Path.Combine(fullPath, "manifest.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, ".sgg-perfmeter-bundle")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "session.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "capture-samples.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "alerts.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "context.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "external-capture.json")), Is.True);
				string manifest = File.ReadAllText(Path.Combine(fullPath, "manifest.json"));
				Assert.That(manifest, Does.Contain("\"schema\":\"sgg.perfmeter.capture-bundle\""));
				Assert.That(manifest, Does.Contain(Sha256(File.ReadAllBytes(Path.Combine(fullPath, "session.json")))));
				string context = File.ReadAllText(Path.Combine(fullPath, "context.json"));
				Assert.That(context, Does.Contain("\"configured_settings\""));
				Assert.That(context, Does.Contain("\"effective_settings\""));
				byte[] originalManifest = File.ReadAllBytes(Path.Combine(fullPath, "manifest.json"));

				PerfMeterCaptureBundleExportResult second = PerfMeterCaptureBundleExporter.Export(data, relativePath, null, false);
				Assert.That(second.Success, Is.False);
				Assert.That(second.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.Conflict));
				Assert.That(File.ReadAllBytes(Path.Combine(fullPath, "manifest.json")), Is.EqualTo(originalManifest));
			}
			finally
			{
				if (Directory.Exists(fullPath))
				{
					Directory.Delete(fullPath, true);
				}
			}
		}

		[Test]
		public void BundleExportRejectsTraversalAndUnavailableAuthority()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("policy", includeScreenshot: false);
			coordinator.TryGetExportData("policy", out PerfMeterCaptureBundleExportData data);

			PerfMeterCaptureBundleExportResult traversal = PerfMeterCaptureBundleExporter.Export(data, "Temp/PerfMeter/CaptureBundles/../outside", null, false);
			PerfMeterCaptureBundleExportResult absolute = PerfMeterCaptureBundleExporter.Export(data, Path.GetFullPath(Path.Combine(Application.dataPath, "..", PerfMeterCaptureBundleExporter.RelativeBundleRoot, "absolute")), null, false);
			PerfMeterCaptureBundleExportResult malformed = PerfMeterCaptureBundleExporter.Export(data, "Temp/PerfMeter/CaptureBundles/bad\0path", null, false);
			PerfMeterCaptureBundleExportResult authority = PerfMeterCaptureBundleExporter.Export(data, PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/authority-" + Guid.NewGuid().ToString("N"), null, true);

			Assert.That(traversal.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(absolute.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(malformed.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(authority.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.AuthorityRequired));
			Assert.That(authority.Error, Does.Contain("authoritative"));
		}

		[Test]
		public void BundleExportUsesSafeDefaultAndCopiesObservedArtifactWithoutClaimingAuthority()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("unsafe/name", includeScreenshot: false);
			coordinator.TryGetExportData("unsafe/name", out PerfMeterCaptureBundleExportData data);
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string sourceDirectory = Path.Combine(projectRoot, "Temp", "PerfMeter", "TestArtifacts", Guid.NewGuid().ToString("N"));
			string sourcePath = Path.Combine(sourceDirectory, "observed.rdc");
			string sourceRelativePath = sourcePath.Substring(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');
			string unknownDirectory = Path.Combine(projectRoot, PerfMeterCaptureBundleExporter.RelativeBundleRoot, "unknown-" + Guid.NewGuid().ToString("N"));
			byte[] artifactBytes = { 1, 3, 3, 7 };
			string bundlePath = string.Empty;

			try
			{
				Directory.CreateDirectory(sourceDirectory);
				File.WriteAllBytes(sourcePath, artifactBytes);
				Directory.CreateDirectory(unknownDirectory);
				File.WriteAllText(Path.Combine(unknownDirectory, "manifest.json"), "{\"schema\":\"sgg.perfmeter.capture-bundle\",\"schema_version\":1,\"bundle_id\":\"fake\",\"files\":[]}");
				PerfMeterCaptureBundleExportResult result = PerfMeterCaptureBundleExporter.Export(data, null, sourceRelativePath, false);
				Assert.That(result.Success, Is.True, result.Error);
				Assert.That(result.RelativePath, Does.StartWith(PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/capture-"));
				Assert.That(result.RelativePath, Does.Not.Contain("unsafe"));
				Assert.That(result.Bundle.ExternalArtifactState, Is.EqualTo(PerfMeterCaptureExternalArtifactState.FileObserved));
				bundlePath = Path.Combine(projectRoot, result.RelativePath);
				Assert.That(File.ReadAllBytes(Path.Combine(bundlePath, "external-capture.rdc")), Is.EqualTo(artifactBytes));
				string metadata = File.ReadAllText(Path.Combine(bundlePath, "external-capture.json"));
				Assert.That(metadata, Does.Contain("\"artifact_file\":\"external-capture.rdc\""));
				Assert.That(metadata, Does.Contain("\"association_verified\":false"));
				Assert.That(metadata, Does.Not.Contain(sourceDirectory));
				Assert.That(File.ReadAllText(Path.Combine(bundlePath, "manifest.json")), Does.Contain(Sha256(artifactBytes)));
				Assert.That(Directory.Exists(unknownDirectory), Is.True);
			}
			finally
			{
				if (!string.IsNullOrEmpty(bundlePath) && Directory.Exists(bundlePath))
				{
					Directory.Delete(bundlePath, true);
				}

				if (Directory.Exists(sourceDirectory))
				{
					Directory.Delete(sourceDirectory, true);
				}

				if (Directory.Exists(unknownDirectory))
				{
					Directory.Delete(unknownDirectory, true);
				}
			}
		}

		[Test]
		public void BundleExportRejectsOversizedArtifactBeforeAllocatingIt()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("oversized-artifact", includeScreenshot: false);
			coordinator.TryGetExportData("oversized-artifact", out PerfMeterCaptureBundleExportData data);
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string sourceDirectory = Path.Combine(projectRoot, "Temp", "PerfMeter", "TestArtifacts", Guid.NewGuid().ToString("N"));
			string sourcePath = Path.Combine(sourceDirectory, "oversized.rdc");
			string sourceRelativePath = sourcePath.Substring(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');

			try
			{
				Directory.CreateDirectory(sourceDirectory);
				using (FileStream stream = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				{
					stream.SetLength(PerfMeterCaptureBundleExporter.MaxBundleBytes + 1L);
				}

				PerfMeterCaptureBundleExportResult result = PerfMeterCaptureBundleExporter.Export(data, PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/oversized-" + Guid.NewGuid().ToString("N"), sourceRelativePath, false);
				Assert.That(result.Success, Is.False);
				Assert.That(result.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.QuotaExceeded));
			}
			finally
			{
				if (Directory.Exists(sourceDirectory))
				{
					Directory.Delete(sourceDirectory, true);
				}
			}
		}

		[Test]
		public void TerminalCaptureWithoutScreenshotDoesNotRemainPending()
		{
			PerfMeterCaptureBundleCoordinator coordinator = new PerfMeterCaptureBundleCoordinator();
			PerfMeterCaptureOptions options = new PerfMeterCaptureOptions("canceled", PerfMeterCaptureTool.RenderDoc);
			coordinator.Start(options, new PerfMeterCaptureBundleOptions(includeScreenshot: true), CaptureStatus("canceled", PerfMeterCaptureState.Capturing));

			coordinator.ObserveCapture(
				CaptureStatus("canceled", PerfMeterCaptureState.Canceled),
				PerfMeterSessionSummarySnapshot.Empty,
				Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				Array.Empty<PerfMeterAlertSnapshot>(),
				false);

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.Canceled));
			Assert.That(coordinator.GetStatus().ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.Unavailable));
		}

		[Test]
		public void CaptureMcpCommandsAreRegisteredAndCapabilitiesDoNotStartRuntime()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();

			Assert.That(metadata, Does.Contain("perfmeter.capture.request"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.status"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.cancel"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.export"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.capabilities"));
			Assert.That(metadata, Does.Contain("require_authoritative_external_artifact"));
			Assert.That(metadata, Does.Not.Contain("require_authoritative_external_metadata"));
			Assert.That(metadata, Does.Contain("PerfMeterMcpCommands.CaptureRequest"));
			string capabilities = PerfMeterMcpCommands.CaptureCapabilities();
			string status = PerfMeterMcpCommands.CaptureStatus("{}");
			Assert.That(capabilities, Does.Contain("\"bundle_root\":\"Temp/PerfMeter/CaptureBundles\""));
			Assert.That(capabilities, Does.Contain("\"tool_identity\":\"unknown\""));
			Assert.That(capabilities, Does.Contain("\"tool_version\":\"unknown\""));
			Assert.That(status, Does.Contain("\"bundle\""));
			Assert.That(PerfMeterMcpCommands.CaptureStatus("{\"capture_id\":\"missing\"}"), Does.Contain("\"result\":\"not_found\""));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void AlertEngineKeepsBoundedCaptureEventHistory()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("capture.alert", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 0d, 1, 0f, PerfMeterAlertAction.Callback)
			});
			Action<PerfMeterAlertSnapshot> handler = _ => { };
			PerformanceMeter.AlertFired += handler;
			try
			{
				for (int i = 0; i < 300; i++)
				{
					engine.Evaluate(CreateMetrics(i), i, PerfMeterAlertClassification.Capture, "bounded");
				}

				PerfMeterAlertSnapshot[] events = engine.GetFiredCaptureEvents("bounded", out bool truncated);
				Assert.That(events, Has.Length.EqualTo(256));
				Assert.That(truncated, Is.True);
				Assert.That(events[0].CollectionFrame, Is.EqualTo(44));
			}
			finally
			{
				PerformanceMeter.AlertFired -= handler;
			}
		}

		[Test]
		public void ReusedCaptureIdDoesNotReusePriorAlertEvents()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("capture.alert", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 0d, 1, 0f, PerfMeterAlertAction.Callback)
			});
			Action<PerfMeterAlertSnapshot> handler = _ => { };
			PerformanceMeter.AlertFired += handler;
			try
			{
				engine.Evaluate(CreateMetrics(1), 1d, PerfMeterAlertClassification.Capture, "reused");
				engine.BeginCaptureEventCollection();
				engine.Evaluate(CreateMetrics(2), 2d, PerfMeterAlertClassification.Capture, "reused");

				PerfMeterAlertSnapshot[] events = engine.GetFiredCaptureEvents("reused", out bool truncated);
				Assert.That(events, Has.Length.EqualTo(1));
				Assert.That(events[0].CollectionFrame, Is.EqualTo(2));
				Assert.That(truncated, Is.False);
			}
			finally
			{
				PerformanceMeter.AlertFired -= handler;
			}
		}

		private static PerfMeterCaptureBundleCoordinator CreateReadyCoordinator(string captureId, bool includeScreenshot, bool completeScreenshot = true)
		{
			PerfMeterCaptureBundleCoordinator coordinator = new PerfMeterCaptureBundleCoordinator();
			PerfMeterCaptureOptions options = new PerfMeterCaptureOptions(captureId, PerfMeterCaptureTool.RenderDoc, 1);
			coordinator.Start(options, new PerfMeterCaptureBundleOptions(includeScreenshot), CaptureStatus(captureId, PerfMeterCaptureState.Capturing));
			PerfMeterSessionSampleSnapshot captureSample = new PerfMeterSessionSampleSnapshot(10, 1d, "Scene", CreateMetrics(10));
			coordinator.RecordCaptureFrame(captureSample, PerformanceMeter.GetDeviceInfo(), default, PerfMeterRenderGraphSnapshot.NotObserved, PerformanceMeter.GetStatus());
			PerfMeterSessionSampleSnapshot baseline = new PerfMeterSessionSampleSnapshot(9, 0.9d, "Scene", CreateMetrics(9));
			PerfMeterAlertSnapshot alert = new PerfMeterAlertSnapshot("capture.alert", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 0d, 16d, 10, 1d, 1, true, "capture alert", PerfMeterAlertClassification.Capture, captureId);
			coordinator.ObserveCapture(
				CaptureStatus(captureId, PerfMeterCaptureState.Completed),
				PerfMeterSessionSummarySnapshot.Empty,
				new[] { baseline },
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				new[] { alert },
				true);
			if (includeScreenshot && completeScreenshot)
			{
				coordinator.TryStartScreenshot(out string screenshotCaptureId, out string screenshotBundleId);
				coordinator.CompleteScreenshot(screenshotCaptureId, screenshotBundleId, null, "unavailable", true);
			}

			return coordinator;
		}

		private static PerfMeterCaptureStatusSnapshot CaptureStatus(string captureId, PerfMeterCaptureState state)
		{
			return new PerfMeterCaptureStatusSnapshot(
				state == PerfMeterCaptureState.Error || state == PerfMeterCaptureState.Unavailable ? PerfMeterAvailability.Unavailable : PerfMeterAvailability.Available,
				state,
				captureId,
				PerfMeterCaptureTool.RenderDoc,
				0,
				1,
				0,
				0,
				state == PerfMeterCaptureState.Completed ? 1 : 0,
				0,
				string.Empty);
		}

		private static PerfMeterMetricsSnapshot CreateMetrics(int frame)
		{
			return new PerfMeterMetricsSnapshot(
				PerfMeterRuntimeState.Running,
				PerfMeterAvailability.Available,
				frame,
				PerfMeterBottleneck.Balanced,
				16.666d,
				false,
				16d,
				8d,
				4d,
				1d,
				0d,
				0,
				0,
				0,
				0,
				0,
				0,
				0L,
				0L,
				0L,
				0L,
				0d,
				PerfMeterOverdrawMeasurementState.Off,
				0f);
		}

		private static string Sha256(byte[] bytes)
		{
			using (SHA256 sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(bytes);
				return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
			}
		}
	}
}
