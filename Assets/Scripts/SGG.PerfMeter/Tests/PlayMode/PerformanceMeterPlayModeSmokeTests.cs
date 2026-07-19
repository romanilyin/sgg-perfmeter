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
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
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
			PerformanceMeter.SetOverdrawHeatmapVisible(true);
			PerformanceMeter.SetOverlayVisible(true);

			yield return null;
			yield return null;

			Assert.That(GameObject.Find(RuntimeObjectName), Is.Not.Null);
		#if UNITY_6000_4_OR_NEWER
			GameObject overlayObject = GameObject.Find(OverlayObjectName);
			Assert.That(overlayObject, Is.Not.Null);
			Assert.That(PerformanceMeter.IsOverlayVisible, Is.True);
			UIDocument document = overlayObject.GetComponent<UIDocument>();
			PanelSettings panelSettings = Resources.Load<PanelSettings>("PerfMeterOverlayPanelSettings");
			Assert.That(document, Is.Not.Null);
			Assert.That(document.panelSettings, Is.SameAs(panelSettings));
			Assert.That(panelSettings.textSettings, Is.Not.Null);
			Assert.That(panelSettings.themeStyleSheet, Is.Not.Null);
		#else
			Assert.That(GameObject.Find(OverlayObjectName), Is.Null);
			Assert.That(PerformanceMeter.IsOverlayVisible, Is.False);
		#endif

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(metrics.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(status.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(metrics.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(metrics.FrameSampleCount, Is.GreaterThanOrEqualTo(1));
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
			Assert.That(status.OverdrawHeatmapVisible, Is.True);
			Assert.That(metrics.FrameBudgetMs, Is.EqualTo(1000d / 30d).Within(0.001d));

			PerformanceMeter.SetOverdrawHeatmapVisible(false);
			yield return null;
			Assert.That(PerformanceMeter.GetStatus().OverdrawHeatmapVisible, Is.False);

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
		public IEnumerator SessionRecorderTracksCurrentSceneScopeAfterSceneSwitch()
		{
			Scene originalScene = SceneManager.GetActiveScene();
			Scene scopeScene = SceneManager.CreateScene("PerfMeter Scope Smoke");

			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.001f, 16, false, 1, 0f));
			yield return null;

			SceneManager.SetActiveScene(scopeScene);
			yield return null;
			yield return null;
			yield return null;

			PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
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

			if (status.OverdrawState == PerfMeterOverdrawMeasurementState.Measuring)
			{
				PerformanceMeter.CancelOverdrawMeasurement();
				Assert.That(PerformanceMeter.GetStatus().OverdrawState, Is.EqualTo(PerfMeterOverdrawMeasurementState.Canceled));
			}
		}
	}
}
