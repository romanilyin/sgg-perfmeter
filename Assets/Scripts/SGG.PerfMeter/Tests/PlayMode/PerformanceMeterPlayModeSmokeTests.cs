using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SGG.PerfMeter.Tests.PlayMode
{
	public sealed class PerformanceMeterPlayModeSmokeTests
	{
		private const string RuntimeObjectName = "SGG PerfMeter Runtime";
		private const string OverlayObjectName = "SGG PerfMeter Overlay";

		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
			PerfMeterPlatformTelemetryRegistry.ClearForTests();
			PerfMeterMemorySnapshotBackendRegistry.ClearForTests();
			PerfMeterGraphicsStateCollectionBackendRegistry.ClearForTests();
			PerfMeterRenderGraphAnalytics.ResetForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
			PerfMeterPlatformTelemetryRegistry.ClearForTests();
			PerfMeterMemorySnapshotBackendRegistry.ClearForTests();
			PerfMeterGraphicsStateCollectionBackendRegistry.ClearForTests();
			PerfMeterRenderGraphAnalytics.ResetForTests();
		}

		[UnityTest]
		public IEnumerator OverlayLifecycleAndSnapshotsUpdateAcrossFrames()
		{
			PerformanceMeter.EnsureRunning();
			PerfMeterAlertHistorySnapshot startupAlertHistory = PerformanceMeter.GetAlertHistory();
			Assert.That(startupAlertHistory.IntervalId, Is.Not.Empty);
			Assert.That(startupAlertHistory.ResetReason, Is.EqualTo(PerfMeterAlertHistoryResetReason.RuntimeStarted));
			PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.Memory);
			PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.BottomLeft);
			PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme.Glass);
			PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.CompactCards);
			PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily.JetBrainsMono);
			PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps30);
			bool heatmapSupported = PerfMeterRenderPipelineDetector.GetActiveKind() != PerfMeterRenderPipelineKind.HighDefinition;
			PerformanceMeter.SetOverdrawHeatmapVisible(true);
			PerformanceMeter.SetOverlayVisible(true);

			yield return null;
			yield return null;

			Assert.That(GameObject.Find(RuntimeObjectName), Is.Not.Null);
		#if UNITY_6000_4_OR_NEWER
			GameObject overlayObject = GameObject.Find(OverlayObjectName);
			Assert.That(overlayObject, Is.Not.Null);
			Assert.That(PerformanceMeter.IsOverlayVisible, Is.True);
			Transform hostTransform = overlayObject.transform.Find(PerfMeterOverlayPanelHost.HostObjectName);
			PanelSettings panelSettings = Resources.Load<PanelSettings>("PerfMeterOverlayPanelSettings");
			Assert.That(hostTransform, Is.Not.Null);
		#if UNITY_6000_5_OR_NEWER
			PanelRenderer panelRenderer = hostTransform.GetComponent<PanelRenderer>();
			Assert.That(panelRenderer, Is.Not.Null);
			Assert.That(hostTransform.GetComponent<UIDocument>(), Is.Null);
			Assert.That(panelRenderer.panelSettings, Is.SameAs(panelSettings));
		#else
			UIDocument document = hostTransform.GetComponent<UIDocument>();
			Assert.That(document, Is.Not.Null);
			Assert.That(document.panelSettings, Is.SameAs(panelSettings));
		#endif
			Assert.That(panelSettings.textSettings, Is.Not.Null);
			Assert.That(panelSettings.themeStyleSheet, Is.Not.Null);
		#else
			Assert.That(GameObject.Find(OverlayObjectName), Is.Null);
			Assert.That(PerformanceMeter.IsOverlayVisible, Is.False);
		#endif

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
			PerfMeterSelfOverheadSnapshot selfOverhead = PerformanceMeter.GetSelfOverhead();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(metrics.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(status.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(metrics.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(metrics.FrameSampleCount, Is.GreaterThanOrEqualTo(1));
			Assert.That(selfOverhead.State, Is.EqualTo(PerfMeterSelfOverheadState.Collecting));
			Assert.That(selfOverhead.CpuTimingAvailable, Is.True);
			Assert.That(selfOverhead.GpuTimingAvailability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(selfOverhead.Collector.InvocationCount, Is.GreaterThanOrEqualTo(1));
			Assert.That(status.SelfOverhead.Collector.InvocationCount, Is.GreaterThanOrEqualTo(1));
			PerfMeterAlertHistorySnapshot alertHistory = PerformanceMeter.GetAlertHistory();
			Assert.That(alertHistory.IntervalId, Is.Not.Empty);
			Assert.That(alertHistory.ResetReason, Is.EqualTo(PerfMeterAlertHistoryResetReason.RulesChanged));
		#if UNITY_6000_4_OR_NEWER
			Assert.That(status.OverlayVisible, Is.True);
		#else
			Assert.That(status.OverlayVisible, Is.False);
		#endif
			Assert.That(status.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomLeft));
			Assert.That(status.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(status.OverlayTheme, Is.EqualTo(PerfMeterOverlayTheme.Glass));
			Assert.That(status.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.CompactCards));
			Assert.That(status.OverlayFontFamily, Is.EqualTo(PerfMeterOverlayFontFamily.JetBrainsMono));
			Assert.That(status.OverlayPreset, Is.EqualTo(PerfMeterOverlayPreset.Custom));
			Assert.That((status.OverlayModules & PerfMeterOverlayModule.Memory) == PerfMeterOverlayModule.Memory, Is.True);
			Assert.That((status.OverlayModules & PerfMeterOverlayModule.Overdraw) == 0, Is.True);
			Assert.That(status.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps30));
			Assert.That(status.OverdrawHeatmapVisible, Is.EqualTo(heatmapSupported));
			Assert.That(metrics.FrameBudgetMs, Is.EqualTo(1000d / 30d).Within(0.001d));

			PerformanceMeter.SetOverdrawHeatmapVisible(false);
			yield return null;
			Assert.That(PerformanceMeter.GetStatus().OverdrawHeatmapVisible, Is.False);
			if (!heatmapSupported)
			{
				Assert.That(PerformanceMeter.GetStatus().OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Unsupported));
				PerformanceMeter.CancelOverdrawMeasurement();
			}

			PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background);
			yield return null;
			Assert.That(PerformanceMeter.GetStatus().CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));

			PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
			yield return null;
			Assert.That(PerformanceMeter.GetStatus().CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Overlay));

			PerformanceMeter.SetOverlayVisible(false);
			yield return null;

			Assert.That(PerformanceMeter.IsOverlayVisible, Is.False);
			Assert.That(PerformanceMeter.GetStatus().OverlayVisible, Is.False);
			Assert.That(GameObject.Find(OverlayObjectName), Is.Null);

			PerformanceMeter.SetOverlayVisible(true);
			yield return null;
		#if UNITY_6000_4_OR_NEWER
			Assert.That(GameObject.Find(OverlayObjectName), Is.Not.Null);
			Assert.That(PerformanceMeter.GetStatus().OverlayVisible, Is.True);
		#endif

			PerformanceMeter.SetOverlayVisible(false);
			Assert.That(GameObject.Find(OverlayObjectName), Is.Null);

			PerformanceMeter.Stop();
			yield return null;

			Assert.That(GameObject.Find(RuntimeObjectName), Is.Null);
			Assert.That(GameObject.Find(OverlayObjectName), Is.Null);
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[UnityTest]
		public IEnumerator CameraSnapshotCapturesNamedCameraTransform()
		{
			GameObject cameraObject = new GameObject("PerfMeter Camera Snapshot Test");
			Camera camera = cameraObject.AddComponent<Camera>();
			camera.transform.position = new Vector3(3f, 4f, 5f);
			camera.transform.rotation = Quaternion.Euler(20f, 35f, 5f);
			camera.fieldOfView = 47f;
			camera.nearClipPlane = 0.2f;
			camera.farClipPlane = 321f;
			camera.depth = 7f;

			yield return null;

			Assert.That(PerformanceMeter.TryGetCameraSnapshot(out PerfMeterCameraSnapshot snapshot, PerfMeterCameraSource.NameFilter, "Snapshot Test"), Is.True);
			Assert.That(snapshot.CameraName, Is.EqualTo(cameraObject.name));
		#if UNITY_6000_4_OR_NEWER
			Assert.That(snapshot.CameraEntityId, Is.EqualTo(EntityId.ToULong(camera.GetEntityId())));
		#else
			Assert.That(snapshot.CameraInstanceId, Is.EqualTo(camera.GetInstanceID()));
		#endif
			Assert.That(snapshot.Source, Is.EqualTo(PerfMeterCameraSource.NameFilter));
			Assert.That(snapshot.Projection, Is.EqualTo(PerfMeterCameraProjection.Perspective));
			Assert.That(snapshot.Position.x, Is.EqualTo(3f).Within(0.001f));
			Assert.That(snapshot.Position.y, Is.EqualTo(4f).Within(0.001f));
			Assert.That(snapshot.Position.z, Is.EqualTo(5f).Within(0.001f));
			Assert.That(snapshot.FieldOfView, Is.EqualTo(47f).Within(0.001f));
			Assert.That(snapshot.NearClipPlane, Is.EqualTo(0.2f).Within(0.001f));
			Assert.That(snapshot.FarClipPlane, Is.EqualTo(321f).Within(0.001f));
			Assert.That(snapshot.Depth, Is.EqualTo(7f).Within(0.001f));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));

			UnityEngine.Object.Destroy(cameraObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator SessionRecorderCollectsBoundedSamplesAcrossFrames()
		{
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.001f, 2));

			yield return null;
			yield return null;
			yield return null;

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			PerfMeterSessionSummarySnapshot recordingSummary = PerformanceMeter.GetSessionSummary();
			Assert.That(status.IsSessionRecording, Is.True);
			Assert.That(status.SessionState, Is.EqualTo(PerfMeterSessionState.Recording));
			Assert.That(recordingSummary.SampleCount, Is.LessThanOrEqualTo(2));
			Assert.That(recordingSummary.Options.MaxSamples, Is.EqualTo(2));

			PerformanceMeter.StopSession();
			yield return null;

			PerfMeterSessionSummarySnapshot stoppedSummary = PerformanceMeter.GetSessionSummary();
			Assert.That(stoppedSummary.State, Is.EqualTo(PerfMeterSessionState.Stopped));
			Assert.That(PerformanceMeter.IsSessionRecording, Is.False);
			Assert.That(stoppedSummary.Device.UnityVersion, Is.Not.Empty);

			string jsonPath = Path.Combine(Application.temporaryCachePath, "sgg-perfmeter-session-smoke.json");
			string csvPath = Path.Combine(Application.temporaryCachePath, "sgg-perfmeter-session-smoke.csv");
			if (File.Exists(jsonPath))
			{
				File.Delete(jsonPath);
			}

			if (File.Exists(csvPath))
			{
				File.Delete(csvPath);
			}

			Assert.That(PerformanceMeter.ExportSessionJson(jsonPath), Is.True);
			Assert.That(PerformanceMeter.ExportSessionCsv(csvPath), Is.True);
			Assert.That(File.Exists(jsonPath), Is.True);
			Assert.That(File.Exists(csvPath), Is.True);
			Assert.That(File.ReadAllText(jsonPath), Does.Contain("\"samples\""));
			Assert.That(File.ReadAllText(csvPath), Does.StartWith("frame,time_seconds,scene,bottleneck"));
		}

		[UnityTest]
		public IEnumerator PlatformTelemetryIsSampledByRuntimeAndCorrelatedWithSessionFrames()
		{
			PlayModeFakePlatformTelemetryProvider provider = new PlayModeFakePlatformTelemetryProvider();
			PerformanceMeter.RegisterPlatformTelemetryProvider(provider);
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.001f, 8));

			yield return null;
			yield return null;
			yield return null;

			PerfMeterPlatformTelemetrySnapshot telemetry = PerformanceMeter.GetPlatformTelemetry();
			int samplesAfterRuntimeCollection = provider.CollectionCount;
			Assert.That(telemetry.IsAvailable, Is.True);
			Assert.That(telemetry.ProviderId, Is.EqualTo(provider.Id));
			Assert.That(telemetry.ThermalWarningLevel, Is.EqualTo(PerfMeterThermalWarningLevel.ThrottlingImminent));
			Assert.That(PerfMeterProfilerInstrumentation.ThermalAvailable, Is.EqualTo(1));
			Assert.That(PerformanceMeter.GetPlatformTelemetry().SampleTimeSeconds, Is.EqualTo(telemetry.SampleTimeSeconds));
			Assert.That(provider.CollectionCount, Is.EqualTo(samplesAfterRuntimeCollection), "Public reads must use the frame-cached runtime snapshot.");

			PerfMeterSessionSampleSnapshot[] samples = PerformanceMeter.GetSessionSamples();
			Assert.That(samples, Is.Not.Empty);
			for (int index = 0; index < samples.Length; index++)
			{
				Assert.That(samples[index].PlatformTelemetry.ProviderId, Is.EqualTo(provider.Id));
				Assert.That(samples[index].PlatformTelemetry.SampleTimeSeconds, Is.GreaterThan(0d));
			}

			GameObject runtimeObject = GameObject.Find(RuntimeObjectName);
			runtimeObject.SetActive(false);
			Assert.That(PerformanceMeter.GetPlatformTelemetry().IsAvailable, Is.False);
			Assert.That(PerfMeterProfilerInstrumentation.ThermalAvailable, Is.Zero);
			runtimeObject.SetActive(true);
			yield return null;
			Assert.That(PerformanceMeter.GetPlatformTelemetry().IsAvailable, Is.True);

			PerformanceMeter.UnregisterPlatformTelemetryProvider(provider);
			yield return null;

			Assert.That(PerformanceMeter.GetPlatformTelemetry().IsAvailable, Is.False);
			Assert.That(PerfMeterProfilerInstrumentation.ThermalAvailable, Is.Zero);
		}

		[UnityTest]
		public IEnumerator MemoryThresholdTriggerCreatesExportableBundleAndHonorsCooldown()
		{
			PlayModeFakeMemorySnapshotBackend backend = new PlayModeFakeMemorySnapshotBackend();
			PerformanceMeter.RegisterMemorySnapshotBackend(backend);
			Assert.That(PerformanceMeter.ConfigureMemorySnapshotTriggers(new PerfMeterMemorySnapshotTriggerOptions(
				true,
				1L,
				0L,
				30,
				PerfMeterMemorySnapshotOptions.DefaultCaptureFlags,
				0L,
				3600d)), Is.True);

			for (int frame = 0; frame < 8 && backend.CaptureCount == 0; frame++)
			{
				yield return null;
			}

			Assert.That(backend.CaptureCount, Is.EqualTo(1));
			PerfMeterMemorySnapshotStatusSnapshot status = PerformanceMeter.GetMemorySnapshotStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterMemorySnapshotState.Completed));
			Assert.That(status.Trigger, Is.EqualTo(PerfMeterMemorySnapshotTrigger.SystemMemoryThreshold));
			Assert.That(status.ArtifactSizeBytes, Is.GreaterThan(0L));
			Assert.That(status.CaptureId, Does.StartWith("memory-systemmemorythreshold-"));
			Assert.That(PerformanceMeter.GetCaptureBundleStatus(status.CaptureId).State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));

			yield return null;
			yield return null;
			Assert.That(backend.CaptureCount, Is.EqualTo(1));

			string relativePath = PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/playmode-memory-" + System.Guid.NewGuid().ToString("N");
			string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
			try
			{
				PerfMeterCaptureBundleExportResult result = PerformanceMeter.ExportCaptureBundle(status.CaptureId, relativePath);
				Assert.That(result.Success, Is.True, result.Error);
				Assert.That(File.Exists(Path.Combine(fullPath, "memory-snapshot.snap")), Is.True);
				Assert.That(File.Exists(backend.LastPath), Is.False, "Owned temporary snapshot should be deleted after atomic bundle export.");
			}
			finally
			{
				if (Directory.Exists(fullPath))
				{
					Directory.Delete(fullPath, true);
				}
			}
		}

		[UnityTest]
		public IEnumerator GraphicsStateTraceTicksAcrossFramesBlocksOverlapAndPrewarmsArtifact()
		{
			PlayModeFakeGraphicsStateCollectionBackend backend = new PlayModeFakeGraphicsStateCollectionBackend();
			PerformanceMeter.RegisterGraphicsStateCollectionBackend(backend);
			string artifactPath = string.Empty;
			try
			{
				PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.001f, 16, false, 0, 0f));
				PerfMeterRuntime.Instance.SetCaptureBackendForTests(new PlayModeFakeCaptureBackend());
				Assert.That(
					PerformanceMeter.RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("graphics-trace", 2, 0L)),
					Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Started));
				PerfMeterProfilerLeaseStatusSnapshot graphicsLease = PerformanceMeter.GetProfilerLeaseStatus();
				Assert.That(graphicsLease.IsHeld, Is.True);
				Assert.That(graphicsLease.Resources, Is.EqualTo(PerfMeterProfilerLeaseResource.Gpu | PerfMeterProfilerLeaseResource.Operation));
				Assert.That(
					PerformanceMeter.RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions("overlap", minimumFreeDiskBytes: 0L, cooldownSeconds: 0d)),
					Is.EqualTo(PerfMeterMemorySnapshotRequestResult.RejectedOverlap));
				Assert.That(
					PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("overlap-gpu", PerfMeterCaptureTool.RenderDoc, 1)),
					Is.EqualTo(PerfMeterCaptureRequestResult.RejectedOverlap));
				Assert.That(PerformanceMeter.BeginAlertCapture("overlap-alert"), Is.False);

				PerfMeterGraphicsStateCollectionStatusSnapshot completed = PerformanceMeter.GetGraphicsStateCollectionStatus();
				for (int frame = 0; frame < 6 && completed.State == PerfMeterGraphicsStateCollectionState.Tracing; frame++)
				{
					yield return null;
					completed = PerformanceMeter.GetGraphicsStateCollectionStatus();
				}

				Assert.That(completed.State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Completed));
				Assert.That(completed.CompletedTraceFrames, Is.EqualTo(2));
				Assert.That(completed.TotalGraphicsStateCount, Is.EqualTo(7));
				Assert.That(completed.VariantCount, Is.EqualTo(3));
				Assert.That(PerformanceMeter.GetProfilerLeaseStatus(graphicsLease.LeaseId).State, Is.EqualTo(PerfMeterProfilerLeaseState.Released));
				Assert.That(completed.ArtifactRelativePath, Does.StartWith(PerfMeterGraphicsStateCollectionStorage.RelativeGraphicsStateCollectionRoot + "/"));
				artifactPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", completed.ArtifactRelativePath));
				Assert.That(File.Exists(artifactPath), Is.True);
				PerformanceMeter.StopSession();
				PerfMeterSessionSampleSnapshot[] samples = PerformanceMeter.GetSessionSamples();
				bool correlatedSampleFound = false;
				for (int i = 0; i < samples.Length; i++)
				{
					correlatedSampleFound |= samples[i].GraphicsStateTraceId == "graphics-trace";
				}
				Assert.That(correlatedSampleFound, Is.True);

				Assert.That(
					PerformanceMeter.PrewarmGraphicsStateCollection(new PerfMeterGraphicsStatePrewarmOptions(completed.ArtifactRelativePath)),
					Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Completed));
				PerfMeterGraphicsStateCollectionStatusSnapshot prewarmed = PerformanceMeter.GetGraphicsStateCollectionStatus();
				Assert.That(prewarmed.State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Prewarmed));
				Assert.That(prewarmed.CompletedWarmupCount, Is.EqualTo(7));
				Assert.That(prewarmed.IsWarmedUp, Is.True);
				Assert.That(File.Exists(artifactPath), Is.True);
			}
			finally
			{
				if (!string.IsNullOrEmpty(artifactPath) && File.Exists(artifactPath))
				{
					File.Delete(artifactPath);
				}
			}
		}

		[UnityTest]
		public IEnumerator StoppingSessionCancelsActiveGraphicsStateTrace()
		{
			PlayModeFakeGraphicsStateCollectionBackend backend = new PlayModeFakeGraphicsStateCollectionBackend();
			PerformanceMeter.RegisterGraphicsStateCollectionBackend(backend);
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.001f, 16, false, 0, 0f));
			Assert.That(
				PerformanceMeter.RequestGraphicsStateTrace(new PerfMeterGraphicsStateTraceOptions("stop-session-trace", 10, 0L)),
				Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Started));

			PerformanceMeter.StopSession();
			PerfMeterGraphicsStateCollectionStatusSnapshot status = PerformanceMeter.GetGraphicsStateCollectionStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Canceled));
			Assert.That(status.IsBusy, Is.False);
			Assert.That(backend.CancelCount, Is.EqualTo(1));

			yield return null;
			Assert.That(backend.EndCount, Is.Zero);
		}

		[UnityTest]
		public IEnumerator SessionRecorderTracksCurrentSceneScopeAfterSceneSwitch()
		{
			Scene originalScene = SceneManager.GetActiveScene();
			Scene scopeScene = SceneManager.CreateScene("PerfMeter Scope Smoke");

			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.001f, 16, false, 1, 0f));
			yield return null;

			SceneManager.SetActiveScene(scopeScene);
			PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
			for (int frame = 0; frame < 16 && (summary.CurrentScene.SceneName != scopeScene.name || summary.CurrentScene.SampleCount < 1); frame++)
			{
				yield return null;
				summary = PerformanceMeter.GetSessionSummary();
			}

			Assert.That(summary.WholeRun.SampleCount, Is.GreaterThanOrEqualTo(1));
			Assert.That(summary.CurrentScene.SceneName, Is.EqualTo(scopeScene.name));
			Assert.That(summary.CurrentScene.SampleCount, Is.GreaterThanOrEqualTo(1));
			Assert.That(summary.CurrentSceneWorstFrame.IsAvailable, Is.True);

			SceneManager.SetActiveScene(originalScene);
			AsyncOperation unload = SceneManager.UnloadSceneAsync(scopeScene);
			while (unload != null && !unload.isDone)
			{
				yield return null;
			}
		}

		[UnityTest]
		public IEnumerator ProfilerMetricCatalogDiscoversOnceAcrossFramesAndRefreshesExplicitly()
		{
			PerformanceMeter.EnsureRunning();
			PerfMeterProfilerMetricCatalogSnapshot startupCatalog = PerformanceMeter.GetProfilerMetricCatalog();
			PerfMeterMetricsSnapshot startupMetrics = PerformanceMeter.GetLatestMetrics();
			Assert.That(startupMetrics.ProfilerMetricCatalogRevision, Is.EqualTo(startupCatalog.Revision));
			Assert.That(startupMetrics.ShaderGpuProgramCreationCapability.Semantic, Is.EqualTo(PerfMeterProfilerMetricSemantic.ShaderGpuProgramCreation));
			Assert.That(startupMetrics.GraphicsPipelineCreationCapability.Semantic, Is.EqualTo(PerfMeterProfilerMetricSemantic.GraphicsPipelineCreation));
			yield return null;

			PerfMeterProfilerMetricCatalogSnapshot initial = PerformanceMeter.GetProfilerMetricCatalog();
			Assert.That(initial.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.Ready));
			Assert.That(initial.Capabilities, Has.Length.EqualTo(13));
			Assert.That(initial.DiscoveryCount, Is.GreaterThanOrEqualTo(1));
			Assert.That(initial.Revision, Is.GreaterThanOrEqualTo(1));
			initial.Capabilities[0] = default;
			Assert.That(
				PerformanceMeter.GetProfilerMetricCatalog().Capabilities[0].Semantic,
				Is.EqualTo(PerfMeterProfilerMetricSemantic.DrawCalls));

			for (int frame = 0; frame < 3; frame++)
			{
				yield return null;
				PerfMeterProfilerMetricCatalogSnapshot repeated = PerformanceMeter.GetProfilerMetricCatalog();
				Assert.That(repeated.DiscoveryCount, Is.EqualTo(initial.DiscoveryCount));
				Assert.That(repeated.Revision, Is.EqualTo(initial.Revision));
			}

			Assert.That(PerformanceMeter.TryRefreshProfilerMetricCatalog(), Is.True);
			PerfMeterProfilerMetricCatalogSnapshot refreshed = PerformanceMeter.GetProfilerMetricCatalog();
			Assert.That(refreshed.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.Ready));
			Assert.That(refreshed.DiscoveryCount, Is.EqualTo(initial.DiscoveryCount + 1));
			Assert.That(refreshed.Revision, Is.EqualTo(initial.Revision + 1));
			Assert.That(refreshed.Capabilities, Has.Length.EqualTo(13));
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.01f, 4));
			Assert.That(PerformanceMeter.BeginAlertCapture("disable-reenable"), Is.True);
			Assert.That(PerfMeterProfilerInstrumentation.SessionState, Is.EqualTo((int)PerfMeterSessionState.Recording));
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.EqualTo(1));

			GameObject runtimeObject = GameObject.Find(RuntimeObjectName);
			Assert.That(runtimeObject, Is.Not.Null);
			runtimeObject.SetActive(false);
			yield return null;
			PerfMeterProfilerMetricCatalogSnapshot disabled = PerformanceMeter.GetProfilerMetricCatalog();
			Assert.That(disabled.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.NotInitialized));
			Assert.That(disabled.Capabilities, Is.Empty);
			Assert.That(PerformanceMeter.IsSessionRecording, Is.False);
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.Empty);
			Assert.That(PerfMeterProfilerInstrumentation.SessionState, Is.EqualTo((int)PerfMeterSessionState.Idle));
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.Zero);

			runtimeObject.SetActive(true);
			yield return null;
			PerfMeterProfilerMetricCatalogSnapshot reenabled = PerformanceMeter.GetProfilerMetricCatalog();
			Assert.That(reenabled.State, Is.EqualTo(PerfMeterProfilerMetricCatalogState.Ready));
			Assert.That(reenabled.DiscoveryCount, Is.EqualTo(1));
			Assert.That(reenabled.Revision, Is.EqualTo(1));
			Assert.That(PerformanceMeter.GetStatus().SessionState, Is.EqualTo(PerfMeterSessionState.Stopped));
			Assert.That(PerfMeterProfilerInstrumentation.SessionState, Is.EqualTo((int)PerfMeterSessionState.Stopped));
			Assert.That(PerfMeterProfilerInstrumentation.AlertScopeActive, Is.Zero);
		}

		[UnityTest]
		public IEnumerator OverdrawRequestReportsTerminalOrActionableWaitingState()
		{
			PerformanceMeter.EnsureRunning();
			PerformanceMeter.RequestOverdrawMeasurement(1);

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			for (int frame = 0; frame < 12; frame++)
			{
				yield return null;
				status = PerformanceMeter.GetStatus();

				if (status.OverdrawState == PerfMeterOverdrawMeasurementState.Completed ||
					status.OverdrawState == PerfMeterOverdrawMeasurementState.Unsupported ||
					status.OverdrawState == PerfMeterOverdrawMeasurementState.Error)
				{
					break;
				}
			}

			PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
			Assert.That(metrics.OverdrawState, Is.EqualTo(status.OverdrawState));

			bool terminalState = status.OverdrawState == PerfMeterOverdrawMeasurementState.Completed ||
				status.OverdrawState == PerfMeterOverdrawMeasurementState.Unsupported;
			bool waitingForRendererFeature = status.OverdrawState == PerfMeterOverdrawMeasurementState.Measuring &&
				status.Warning.Contains("Render Graph pass");

			Assert.That(terminalState || waitingForRendererFeature, Is.True, status.Warning);
			Assert.That(status.OverdrawState, Is.Not.EqualTo(PerfMeterOverdrawMeasurementState.Error));
			PerfMeterRenderIntegrationSnapshot renderIntegration = PerformanceMeter.GetRenderIntegrationSnapshot();
			if (renderIntegration.State != PerfMeterRenderIntegrationState.Observed)
			{
				Assert.That(renderIntegration.State, Is.EqualTo(PerfMeterRenderIntegrationState.NotObserved));
				Assert.That(waitingForRendererFeature || status.OverdrawState == PerfMeterOverdrawMeasurementState.Unsupported, Is.True, status.Warning);
			}
			else
			{
				Assert.That(renderIntegration.State, Is.EqualTo(PerfMeterRenderIntegrationState.Observed));
				Assert.That(renderIntegration.RenderPipeline.Kind, Is.EqualTo(PerfMeterRenderPipelineKind.Universal));
				Assert.That(renderIntegration.ObservationMatchesCurrentPipeline, Is.True);
				Assert.That(renderIntegration.PassKind, Is.EqualTo(PerfMeterRenderPassKind.RenderGraphRaster));
				Assert.That(renderIntegration.IntegrationId, Is.EqualTo("sgg.perfmeter.urp.render-graph"));
				PerfMeterGpuResidentDrawerContextSnapshot gpuResidentDrawer = renderIntegration.GpuResidentDrawer;
				Assert.That(gpuResidentDrawer.ActivityAvailability, Is.EqualTo(PerfMeterAvailability.Available));
				Assert.That(gpuResidentDrawer.ActivitySource, Is.EqualTo(PerfMeterGpuResidentDrawerContextSnapshot.UnityRuntimeActivitySource));
				Assert.That(gpuResidentDrawer.ComputeShaderAvailability, Is.EqualTo(PerfMeterAvailability.Available));
				Assert.That(gpuResidentDrawer.SupportsComputeShaders, Is.EqualTo(SystemInfo.supportsComputeShaders));
				Assert.That(gpuResidentDrawer.ForwardPlusActivityAvailability, Is.EqualTo(PerfMeterAvailability.Available));
				Assert.That(gpuResidentDrawer.RenderingModeCompatibilityAvailability, Is.EqualTo(PerfMeterAvailability.Available));
				Assert.That(gpuResidentDrawer.Effectiveness.Availability, Is.Not.EqualTo(PerfMeterAvailability.Unknown));
				Assert.That(gpuResidentDrawer.Effectiveness.Scope, Is.EqualTo(PerfMeterGpuResidentDrawerEffectivenessSnapshot.AggregateScope));
				Assert.That(renderIntegration.VariableRateShading.ConfigurationAvailability, Is.EqualTo(PerfMeterAvailability.Unknown));
				Assert.That(renderIntegration.LegacyRenderGraph.State, Is.EqualTo(PerfMeterRenderGraphState.Observed));
			}

			if (status.OverdrawState == PerfMeterOverdrawMeasurementState.Measuring)
			{
				PerformanceMeter.CancelOverdrawMeasurement();
				Assert.That(PerformanceMeter.GetStatus().OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Canceled));
			}
		}

		[UnityTest]
		public IEnumerator CaptureCoordinatorTransitionsAcrossFramesAndCleansUpOnStop()
		{
			PerformanceMeter.EnsureRunning();
			PlayModeFakeCaptureBackend backend = new PlayModeFakeCaptureBackend();
			PerfMeterRuntime.Instance.SetCaptureBackendForTests(backend);
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.001f, 64));

			PerfMeterCaptureRequestResult result = PerformanceMeter.RequestCapture(
				new PerfMeterCaptureOptions("playmode-capture", PerfMeterCaptureTool.RenderDoc, 2, 1, 1),
				new PerfMeterCaptureBundleOptions());
			Assert.That(result, Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.PreRoll));
			PerfMeterProfilerLeaseStatusSnapshot captureLease = PerformanceMeter.GetProfilerLeaseStatus();
			Assert.That(captureLease.IsHeld, Is.True);
			Assert.That(captureLease.OwnerId, Is.EqualTo("perfmeter-capture"));

			yield return null;
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Capturing));
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.EqualTo("playmode-capture"));
			Assert.That(PerfMeterRuntime.Instance.LastAlertClassification, Is.Not.EqualTo(PerfMeterAlertClassification.Capture));
			yield return null;
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Capturing));
			Assert.That(PerfMeterRuntime.Instance.LastAlertClassification, Is.EqualTo(PerfMeterAlertClassification.Capture));
			yield return null;
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.PostRoll));
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.Empty);
			Assert.That(PerfMeterRuntime.Instance.LastAlertClassification, Is.EqualTo(PerfMeterAlertClassification.Capture));
			yield return null;

			PerfMeterCaptureStatusSnapshot completed = PerformanceMeter.GetCaptureStatus();
			Assert.That(completed.State, Is.EqualTo(PerfMeterCaptureState.Completed));
			Assert.That(completed.CompletedPreRollFrames, Is.EqualTo(1));
			Assert.That(completed.CompletedCaptureFrames, Is.EqualTo(2));
			Assert.That(completed.CompletedPostRollFrames, Is.EqualTo(1));
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus(captureLease.LeaseId).State, Is.EqualTo(PerfMeterProfilerLeaseState.Released));
			Assert.That(backend.BeginCount, Is.EqualTo(1));
			Assert.That(backend.EndCount, Is.EqualTo(1));
			PerfMeterCaptureBundleStatusSnapshot completedBundle = PerformanceMeter.GetCaptureBundleStatus("playmode-capture");
			Assert.That(completedBundle.State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(completedBundle.CaptureSampleCount, Is.EqualTo(2));
			Assert.That(completedBundle.BaselineSampleCount, Is.GreaterThanOrEqualTo(1));
			Assert.That(PerformanceMeter.GetSessionSummary().SampleCount, Is.EqualTo(completedBundle.BaselineSampleCount));

			Assert.That(PerformanceMeter.RequestCapture(
				new PerfMeterCaptureOptions("zero-roll", PerfMeterCaptureTool.RenderDoc),
				new PerfMeterCaptureBundleOptions(includeScreenshot: true)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Capturing));
			yield return null;
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Completed));
			Assert.That(PerfMeterRuntime.Instance.LastAlertClassification, Is.EqualTo(PerfMeterAlertClassification.Capture));
			Assert.That(backend.BeginCount, Is.EqualTo(2));
			Assert.That(backend.EndCount, Is.EqualTo(2));
			for (int frame = 0; frame < 4 && PerformanceMeter.GetCaptureBundleStatus("zero-roll").State == PerfMeterCaptureBundleState.PendingScreenshot; frame++)
			{
				yield return null;
			}

			PerfMeterCaptureBundleStatusSnapshot screenshotBundle = PerformanceMeter.GetCaptureBundleStatus("zero-roll");
			Assert.That(screenshotBundle.State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(screenshotBundle.ScreenshotState, Is.Not.EqualTo(PerfMeterCaptureScreenshotState.Pending));

			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("disable-cleanup", PerfMeterCaptureTool.RenderDoc, 10)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			GameObject runtimeObject = GameObject.Find(RuntimeObjectName);
			backend.EndFailuresRemaining = 1;
			runtimeObject.SetActive(false);
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(backend.EndCount, Is.EqualTo(3));
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.Empty);

			runtimeObject.SetActive(true);
			yield return null;
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Idle));
			Assert.That(backend.EndCount, Is.EqualTo(4));

			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("stop-cleanup", PerfMeterCaptureTool.RenderDoc, 10)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.EqualTo("stop-cleanup"));
			PerformanceMeter.SetOverlayVisible(false);
			backend.EndFailuresRemaining = 2;
			PerformanceMeter.Stop();

			Assert.That(backend.EndCount, Is.EqualTo(5));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(PerformanceMeter.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Stopped));
			Assert.That(GameObject.Find(RuntimeObjectName), Is.Not.Null);
			PerformanceMeter.SetOverlayVisible(true);
			Assert.That(PerformanceMeter.IsOverlayVisible, Is.False);
			Assert.That(PerformanceMeter.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Stopped));
			Assert.That(PerformanceMeter.CancelCapture("stop-cleanup"), Is.True);
			Assert.That(backend.EndCount, Is.EqualTo(7));
			PerformanceMeter.Stop();

			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.Empty);
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Idle));
			Assert.That(PerfMeterProfilerInstrumentation.CaptureState, Is.EqualTo((int)PerfMeterCaptureState.Idle));
		}

		[UnityTest]
		public IEnumerator DestroyedRuntimeRetainsFailedCaptureCleanupOwnerForRetry()
		{
			PerformanceMeter.EnsureRunning();
			PlayModeFakeCaptureBackend backend = new PlayModeFakeCaptureBackend();
			PerfMeterRuntime.Instance.SetCaptureBackendForTests(backend);
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("destroyed-capture", PerfMeterCaptureTool.RenderDoc, 10)), Is.EqualTo(PerfMeterCaptureRequestResult.Started));

			backend.EndFailuresRemaining = 2;
			GameObject runtimeObject = GameObject.Find(RuntimeObjectName);
			Assert.That(runtimeObject, Is.Not.Null);
			Object.Destroy(runtimeObject);
			yield return null;

			Assert.That(PerfMeterRuntime.Instance, Is.Null);
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(PerformanceMeter.ActiveAlertCaptureId, Is.Empty);
			Assert.That(PerformanceMeter.CancelCapture(), Is.True);
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Canceled));
			Assert.That(PerformanceMeter.CancelCapture("wrong-capture"), Is.False);
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Canceled));

			PerformanceMeter.EnsureRunning();
			yield return null;
			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Idle));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Running));
		}

		private sealed class PlayModeFakeCaptureBackend : IPerfMeterCaptureBackend
		{
			internal int BeginCount { get; private set; }
			internal int EndCount { get; private set; }
			internal int EndFailuresRemaining { get; set; }

			public PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool)
			{
				return new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Available, string.Empty);
			}

			public bool TryBegin(PerfMeterCaptureTool tool, out string error)
			{
				BeginCount++;
				error = string.Empty;
				return true;
			}

			public bool TryEnd(out string error)
			{
				EndCount++;
				if (EndFailuresRemaining > 0)
				{
					EndFailuresRemaining--;
					error = "transient end failure";
					return false;
				}

				error = string.Empty;
				return true;
			}
		}

		private sealed class PlayModeFakePlatformTelemetryProvider : IPerfMeterPlatformTelemetryProvider
		{
			public string Id => "playmode.fake";
			internal int CollectionCount { get; private set; }

			public bool TryCollect(out PerfMeterPlatformTelemetrySnapshot snapshot)
			{
				CollectionCount++;
				snapshot = new PerfMeterPlatformTelemetrySnapshot(
					PerfMeterAvailability.Available,
					Id,
					"test",
					Time.realtimeSinceStartupAsDouble,
					Time.realtimeSinceStartupAsDouble,
					true,
					PerfMeterThermalWarningLevel.ThrottlingImminent,
					true,
					0.8f,
					true,
					0.3f,
					true,
					2,
					true,
					3,
					true,
					PerfMeterAdaptiveBottleneck.Gpu);
				return true;
			}
		}

		private sealed class PlayModeFakeMemorySnapshotBackend : IPerfMeterMemorySnapshotBackend
		{
			public string Id => "playmode.memory";
			public string Version => "test";
			public PerfMeterMemoryCaptureFlags SupportedCaptureFlags => PerfMeterMemorySnapshotCoordinator.AllCaptureFlags;
			internal int CaptureCount { get; private set; }
			internal string LastPath { get; private set; }

			public bool TryCapture(string path, PerfMeterMemoryCaptureFlags captureFlags, System.Action<PerfMeterMemorySnapshotBackendResult> completed, out string error)
			{
				CaptureCount++;
				LastPath = path;
				File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
				completed(new PerfMeterMemorySnapshotBackendResult(true, path, string.Empty));
				error = string.Empty;
				return true;
			}
		}

		private sealed class PlayModeFakeGraphicsStateCollectionBackend : IPerfMeterGraphicsStateCollectionBackend
		{
			public string Id => "fake.graphics.playmode";
			public string Version => "1.0";
			public bool SupportsCacheMissTracing => false;
			public bool SupportsParallelPsoCreation => true;
			public int EndCount { get; private set; }
			public int CancelCount { get; private set; }

			public bool TryBeginTrace(out string error)
			{
				error = string.Empty;
				return true;
			}

			public bool TryEndTrace(string outputPath, out PerfMeterGraphicsStateTraceBackendResult result, out string error)
			{
				EndCount++;
				File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3, 5, 8 });
				result = new PerfMeterGraphicsStateTraceBackendResult(true, 7, 3);
				error = string.Empty;
				return true;
			}

			public void CancelTrace()
			{
				CancelCount++;
			}

			public bool TryPrewarm(string inputPath, int maxStateCount, bool traceCacheMisses, out PerfMeterGraphicsStatePrewarmBackendResult result, out string error)
			{
				result = new PerfMeterGraphicsStatePrewarmBackendResult(File.Exists(inputPath), 7, 7, true);
				error = result.Success ? string.Empty : "artifact missing";
				return result.Success;
			}
		}
	}
}
