using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SGG.PerfMeter.Tests.URP.PlayMode
{
	public sealed class PerfMeterUrpEnqueuePlayModeTests
	{
		private UrpCameraPipelineScope _pipeline;
		private PerfMeterRenderGraphFeature _feature;
		private SentinelProvider _sentinel;
		private bool _runtimeOwned;
		private int _sessionStartFrame;
		private long _previousEpoch;
		private string _previousIdentity;
		private readonly List<string> _cleanupErrors = new List<string>();

		[UnitySetUp]
		public IEnumerator SetUp()
		{
			_cleanupErrors.Clear();
			_runtimeOwned = false;
			_previousEpoch = 0;
			_previousIdentity = null;
			_pipeline = null;
			_feature = null;
			_sentinel = null;
			Assert.That(Application.isPlaying, Is.True, "BLOCKED_TEST_MODE: this is an Editor PlayMode fixture.");
			if (!UrpCameraPipelineScope.HasActiveUrp)
				Assert.Ignore("URP-only integration fixture: non-URP is explicitly ignored, never a URP pass or HDRP PlayMode claim.");
			Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "BLOCKED_RENDER_DEVICE: do not use -nographics.");
			// Refuse to replace a live runtime/session or discard a user's retained history. The runner must be quiescent.
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped), "BLOCKED_RUNTIME_ISOLATION: stop any auto-started/runtime-owned workflow before running this fixture.");
			Assert.That(PerformanceMeter.IsSessionRecording, Is.False);
			Assert.That(PerformanceMeter.GetSessionSummary().SessionId, Is.Empty, "BLOCKED_RUNTIME_ISOLATION: a disabled runtime may retain a user's session.");
			Type runtimeType = typeof(PerformanceMeter).Assembly.GetType("SGG.PerfMeter.PerfMeterRuntime", true);
			Assert.That(Resources.FindObjectsOfTypeAll(runtimeType), Is.Empty, "BLOCKED_RUNTIME_ISOLATION: a hidden/disabled/authored runtime already exists; do not claim or stop it.");
			Assert.That(PerformanceMeter.GetCaptureStatus().CaptureId, Is.Empty, "BLOCKED_PENDING_CAPTURE: existing capture cleanup must complete before this fixture.");
			PerfMeterGraphicsStateCollectionStatusSnapshot graphics = PerformanceMeter.GetGraphicsStateCollectionStatus();
			Assert.That(graphics.IsBusy || graphics.HasPendingCleanup, Is.False, "BLOCKED_PENDING_GRAPHICS: existing graphics cleanup must complete before this fixture.");
			Assert.That(PerformanceMeter.GetMemorySnapshotStatus().IsActive, Is.False, "BLOCKED_PENDING_MEMORY: another snapshot workflow is active.");
			_pipeline = new UrpCameraPipelineScope();
			_feature = ScriptableObject.CreateInstance<PerfMeterRenderGraphFeature>();
			_feature.hideFlags = HideFlags.HideAndDontSave;
			ConfigureFeature(false);
			_pipeline.Create(_feature);
			Stopwatch activation = Stopwatch.StartNew();
			int polls = 0;
			while (_pipeline.CameraFrameCount == 0 && activation.Elapsed.TotalSeconds < 30d && polls++ < 10000)
				yield return null;
			_sentinel = new SentinelProvider();
			PerformanceMeter.RegisterCustomMetricProvider(_sentinel);
			_runtimeOwned = true; // Set before startup so partial initialization is cleaned up.
			PerformanceMeter.EnsureRunning();
			PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
			PerformanceMeter.SetOverdrawHeatmapVisible(false);
			PerformanceMeter.CancelOverdrawMeasurement();
			PerformanceMeter.SetOverlayVisible(true);
			yield return null;
			yield return null;
			// Overlay initialization can recreate the SRP instance. Freeze only after startup, before any bound window.
			_pipeline.FreezePipelineIdentity();
			AssertRunningGates();
		}

		[UnityTest]
		[Timeout(130000)]
		public IEnumerator RealCameraDormantActiveDormantSessionsDoNotReuseMeasurements()
		{
			string dormantId = BeginSession();
			yield return WaitForWindow(dormantId, 130, false);
			AssertDormant(EndSession(dormantId), PerfMeterSelfOverheadInactiveReason.PassNotEnqueued);

			ConfigureFeature(true);
			string activeId = BeginSession();
			yield return WaitForWindow(activeId, 120, true);
			PerfMeterSelfOverheadWindowSnapshot active = EndSession(activeId);
			PerfMeterSelfOverheadComponentSnapshot measured = active.UrpRenderIntegration;
			Assert.That(measured.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.Ready));
			Assert.That(measured.InactiveReason, Is.EqualTo(PerfMeterSelfOverheadInactiveReason.None));
			Assert.That(measured.InvocationCount, Is.GreaterThan(0));
			Assert.That(measured.CallbackFrameCount, Is.GreaterThanOrEqualTo(120));
			Assert.That(measured.WindowFrameCount, Is.GreaterThanOrEqualTo(120));
			Assert.That(measured.MeasurementFirstFrame, Is.InRange(active.WindowStartFrame, active.WindowEndFrame));
			Assert.That(measured.MeasurementLastFrame, Is.InRange(measured.MeasurementFirstFrame, active.WindowEndFrame));
			Assert.That(active.EnqueueCount, Is.GreaterThan(0));
			Assert.That(active.FeatureEnabled, Is.EqualTo(PerfMeterUrpFeatureEnabledState.Enabled));
			Assert.That(active.FirstEnqueueFrame, Is.InRange(active.WindowStartFrame, active.WindowEndFrame));
			Assert.That(active.LastEnqueueFrame, Is.InRange(active.FirstEnqueueFrame, active.WindowEndFrame));
			Assert.That(active.MeasurementContained, Is.True);
			Assert.That(active.Warning, Is.Empty);
			yield return null; // Real callbacks outside a stopped session must not change its frozen window.
			PerfMeterSelfOverheadWindowSnapshot frozen = Read(activeId);
			Assert.That(frozen.WindowEndFrame, Is.EqualTo(active.WindowEndFrame));
			Assert.That(frozen.UrpRenderIntegration.InvocationCount, Is.EqualTo(measured.InvocationCount));

			ConfigureFeature(false);
			string dormantAgainId = BeginSession();
			yield return WaitForWindow(dormantAgainId, 10, false);
			AssertDormant(EndSession(dormantAgainId), PerfMeterSelfOverheadInactiveReason.PassNotEnqueued);
		}

		[UnityTest]
		[Timeout(70000)]
		public IEnumerator DisabledSettingsWithMarkerRequestedNeverEnqueue()
		{
			ConfigureFeature(true, false); // isActive remains true: exercise AddRenderPasses' disabled-settings branch.
			Assert.That(_feature.isActive, Is.True);
			string id = BeginSession();
			yield return WaitForWindow(id, 10, false);
			AssertDormant(EndSession(id), PerfMeterSelfOverheadInactiveReason.RendererFeatureDisabled);
		}

		[UnityTearDown]
		public IEnumerator TearDown()
		{
			if (_runtimeOwned) Cleanup(PerformanceMeter.Stop);
			if (_sentinel != null) Cleanup(() => PerformanceMeter.UnregisterCustomMetricProvider(_sentinel));
			if (_pipeline != null) yield return _pipeline.Restore();
			// Covers feature creation followed by configuration/setup failure before pipeline ownership was captured.
			if (_feature != null) Cleanup(() => Object.DestroyImmediate(_feature));
			yield return null;
			if (_runtimeOwned)
			{
				Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
				Assert.That(PerformanceMeter.IsSessionRecording, Is.False);
			}
			if (_sentinel != null)
				foreach (PerfMeterCustomMetricSnapshot metric in PerformanceMeter.GetCustomMetrics()) Assert.That(metric.Id, Is.Not.EqualTo(_sentinel.Id));
			Assert.That(_cleanupErrors, Is.Empty, string.Join("; ", _cleanupErrors));
		}

		private void ConfigureFeature(bool marker, bool enabled = true)
		{
			using SerializedObject serialized = new SerializedObject(_feature);
			UrpCameraPipelineScope.Require(serialized, "_settings._recordOverlayMarkerPass").boolValue = marker;
			UrpCameraPipelineScope.Require(serialized, "_settings._enabled").boolValue = enabled;
			UrpCameraPipelineScope.Require(serialized, "_settings._gameCamerasOnly").boolValue = true;
			UrpCameraPipelineScope.Require(serialized, "_settings._cameraNameFilter").stringValue = "PerfMeter enqueue regression camera";
			serialized.ApplyModifiedPropertiesWithoutUndo();
			Assert.That(_feature.FeatureSettings.RecordOverlayMarkerPass, Is.EqualTo(marker));
			Assert.That(_feature.FeatureSettings.Enabled, Is.EqualTo(enabled));
		}

		private string BeginSession()
		{
			AssertRunningGates();
			_sessionStartFrame = Time.frameCount;
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.005f, 512));
			Assert.That(PerformanceMeter.IsSessionRecording, Is.True);
			string id = PerformanceMeter.GetSessionSummary().SessionId;
			Assert.That(id, Is.Not.Empty.And.Not.EqualTo(_previousIdentity));
			PerfMeterSelfOverheadWindowSnapshot fresh = Read(id);
			Assert.That(fresh.Epoch, Is.GreaterThan(_previousEpoch));
			Assert.That(fresh.WindowComplete, Is.False);
			Assert.That(fresh.WindowStartFrame, Is.EqualTo(_sessionStartFrame));
			Assert.That(fresh.EnqueueCount, Is.Zero);
			Assert.That(fresh.UrpRenderIntegration.InvocationCount, Is.Zero);
			Assert.That(fresh.UrpRenderIntegration.CallbackFrameCount, Is.Zero);
			Assert.That(fresh.UrpRenderIntegration.WindowFrameCount, Is.Zero);
			Assert.That(fresh.UrpRenderIntegration.MeasurementFirstFrame, Is.EqualTo(-1));
			Assert.That(fresh.UrpRenderIntegration.MeasurementLastFrame, Is.EqualTo(-1));
			_previousEpoch = fresh.Epoch;
			_previousIdentity = id;
			return id;
		}

		private IEnumerator WaitForWindow(string id, int frames, bool active)
		{
			Stopwatch deadline = Stopwatch.StartNew();
			int startFrame = Time.frameCount, cameraFrames = _pipeline.CameraFrameCount, collections = _sentinel.CollectionCount;
			for (int polls = 0; polls < 10000 && deadline.Elapsed.TotalSeconds < 30d; polls++)
			{
				AssertRunningGates();
				Assert.That(PerformanceMeter.IsSessionRecording, Is.True);
				PerfMeterSelfOverheadComponentSnapshot component = Read(id).UrpRenderIntegration;
				bool enough = active ? component.MeasurementFirstFrame >= 0 && Time.frameCount - component.MeasurementFirstFrame + 1 >= frames &&
					component.CallbackFrameCount >= frames && component.State == PerfMeterSelfOverheadComponentState.Ready :
					Time.frameCount - startFrame >= frames && _pipeline.CameraFrameCount - cameraFrames >= frames;
				if (enough)
				{
					Assert.That(_sentinel.CollectionCount, Is.GreaterThan(collections), "The available sentinel provider was not collected during this phase.");
					yield break;
				}
				yield return null;
			}
			PerfMeterSelfOverheadWindowSnapshot last = Read(id);
			Assert.Fail("BLOCKED_RENDER_WINDOW: 30s/10000-frame limit; active=" + active + ", camera frames=" + (_pipeline.CameraFrameCount - cameraFrames) +
				", first callback=" + last.UrpRenderIntegration.MeasurementFirstFrame + ", callbacks=" + last.UrpRenderIntegration.CallbackFrameCount + ", reason=" + last.UrpRenderIntegration.InactiveReason);
		}

		private PerfMeterSelfOverheadWindowSnapshot EndSession(string id)
		{
			PerformanceMeter.StopSession();
			Assert.That(PerformanceMeter.IsSessionRecording, Is.False);
			Assert.That(PerformanceMeter.GetSessionSummary().SessionId, Is.EqualTo(id));
			PerfMeterSelfOverheadWindowSnapshot window = Read(id);
			Assert.That(window.Kind, Is.EqualTo(PerfMeterSelfOverheadWindowKind.Session));
			Assert.That(window.Identity, Is.EqualTo(id));
			Assert.That(window.WindowComplete, Is.True);
			Assert.That(window.Epoch, Is.EqualTo(_previousEpoch));
			Assert.That(window.UrpRenderIntegration.Epoch, Is.EqualTo(window.Epoch));
			Assert.That(window.WindowEndFrame, Is.GreaterThanOrEqualTo(window.WindowStartFrame));
			Assert.That(window.WindowStartFrame, Is.EqualTo(_sessionStartFrame));
			Assert.That(window.WindowEndFrame, Is.EqualTo(Time.frameCount));
			Assert.That(window.RenderPipeline.Kind, Is.EqualTo(PerfMeterRenderPipelineKind.Universal));
			Assert.That(window.PipelineAssetSource, Is.EqualTo(PerfMeterRenderPipelineAssetSource.QualitySettings));
			Assert.That(window.PipelineAssetEntityId, Is.EqualTo(_pipeline.PipelineEntityId));
			Assert.That(window.FeatureInstallation, Is.EqualTo(PerfMeterUrpFeatureInstallationState.Installed));
			Assert.That(window.UrpRenderIntegration.GpuAttributionAvailability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			_pipeline.AssertStable(); // PipelineStable is not a member of the inspected public snapshot.
			return window;
		}

		private static void AssertDormant(PerfMeterSelfOverheadWindowSnapshot window, PerfMeterSelfOverheadInactiveReason reason)
		{
			Assert.That(window.FeatureEnabled, Is.EqualTo(reason == PerfMeterSelfOverheadInactiveReason.RendererFeatureDisabled ? PerfMeterUrpFeatureEnabledState.Disabled : PerfMeterUrpFeatureEnabledState.Enabled));
			Assert.That(window.UrpRenderIntegration.State, Is.EqualTo(PerfMeterSelfOverheadComponentState.NotMeasured));
			Assert.That(window.UrpRenderIntegration.InactiveReason, Is.EqualTo(reason));
			Assert.That(window.UrpRenderIntegration.InvocationCount, Is.Zero);
			Assert.That(window.UrpRenderIntegration.CallbackFrameCount, Is.Zero);
			Assert.That(window.UrpRenderIntegration.WindowFrameCount, Is.Zero);
			Assert.That(window.UrpRenderIntegration.MeasurementFirstFrame, Is.EqualTo(-1));
			Assert.That(window.UrpRenderIntegration.MeasurementLastFrame, Is.EqualTo(-1));
			Assert.That(window.EnqueueCount, Is.Zero);
			Assert.That(window.FirstEnqueueFrame, Is.EqualTo(-1));
			Assert.That(window.LastEnqueueFrame, Is.EqualTo(-1));
			Assert.That(window.MeasurementContained, Is.False);
		}

		private void AssertRunningGates()
		{
			_pipeline.AssertStable();
			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterRuntimeState.Running));
			Assert.That(status.OverlayVisible, Is.True);
			Assert.That(status.OverdrawState, Is.Not.EqualTo(PerfMeterOverdrawMeasurementState.Measuring));
			Assert.That(status.OverdrawHeatmapVisible, Is.False);
		}

		private static PerfMeterSelfOverheadWindowSnapshot Read(string id) => PerformanceMeter.GetSelfOverheadWindow(PerfMeterSelfOverheadWindowKind.Session, id);
		private void Cleanup(Action action)
		{
			try { action(); }
			catch (Exception exception) { _cleanupErrors.Add(exception.ToString()); }
		}

		private sealed class SentinelProvider : IPerfMeterCustomMetricProvider
		{
			public string Id { get; } = "test.urp.enqueue.sentinel." + Guid.NewGuid().ToString("N");
			internal int CollectionCount;
			public bool TryCollect(out PerfMeterCustomMetricSnapshot metric)
			{
				CollectionCount++;
				metric = new PerfMeterCustomMetricSnapshot(Id, "Enqueue sentinel", "Test", "count", 1d, true);
				return true;
			}
		}
	}
}
