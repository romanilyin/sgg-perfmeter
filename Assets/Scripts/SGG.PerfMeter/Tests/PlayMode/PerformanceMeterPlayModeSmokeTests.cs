using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
			PerformanceMeter.SetOverlayPreset(PerfMeterOverlayPreset.Memory);
			PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.BottomLeft);
			PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.TextCompact);
			PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps30);
			PerformanceMeter.SetOverdrawHeatmapVisible(true);
			PerformanceMeter.SetOverlayVisible(true);

			yield return null;
			yield return null;

			Assert.That(GameObject.Find(RuntimeObjectName), Is.Not.Null);
			Assert.That(GameObject.Find(OverlayObjectName), Is.Not.Null);
			Assert.That(PerformanceMeter.IsOverlayVisible, Is.True);

			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(metrics.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(status.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(metrics.CollectionFrame, Is.GreaterThanOrEqualTo(0));
			Assert.That(metrics.FrameSampleCount, Is.GreaterThanOrEqualTo(1));
			Assert.That(status.OverlayVisible, Is.True);
			Assert.That(status.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomLeft));
			Assert.That(status.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.TextCompact));
			Assert.That(status.OverlayPreset, Is.EqualTo(PerfMeterOverlayPreset.Memory));
			Assert.That((status.OverlayModules & PerfMeterOverlayModule.Memory) == PerfMeterOverlayModule.Memory, Is.True);
			Assert.That((status.OverlayModules & PerfMeterOverlayModule.Overdraw) == 0, Is.True);
			Assert.That(status.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps30));
			Assert.That(status.OverdrawHeatmapVisible, Is.True);
			Assert.That(metrics.FrameBudgetMs, Is.EqualTo(1000d / 30d).Within(0.001d));

			PerformanceMeter.SetOverdrawHeatmapVisible(false);
			yield return null;
			Assert.That(PerformanceMeter.GetStatus().OverdrawHeatmapVisible, Is.False);

			PerformanceMeter.SetOverlayVisible(false);
			yield return null;

			Assert.That(PerformanceMeter.IsOverlayVisible, Is.False);
			Assert.That(PerformanceMeter.GetStatus().OverlayVisible, Is.False);

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
