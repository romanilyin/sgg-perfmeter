using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace SGG.PerfMeter.Tests.URP.PlayMode
{
	// Editor-only, in-memory pipeline/camera isolation; no PerfMeter evidence or runtime manipulation.
	internal sealed class UrpCameraPipelineScope
	{
		internal enum ConstructionStage { ClonesCreated, GlobalsInstalled, QualityInstalled }
		private static readonly HashSet<UrpCameraPipelineScope> RetainedScopes = new HashSet<UrpCameraPipelineScope>();
		internal static int RetainedScopeCount => RetainedScopes.Count;
		private readonly List<(Camera Camera, bool Enabled)> _cameras = new List<(Camera, bool)>();
		private readonly List<string> _cleanupErrors = new List<string>();
		private RenderPipelineAsset _quality, _default;
		private UniversalRenderPipelineAsset _sourcePipeline, _pipeline;
		private UniversalRendererData _sourceRenderer, _renderer;
		private RenderPipelineGlobalSettings _globals, _globalsClone;
		private ScriptableRendererFeature _feature;
		private RenderPipeline _runtimePipeline;
		private GameObject _cameraObject;
		private Camera _camera;
		private RenderTexture _target, _activeTarget;
		private string _pipelineJson, _rendererJson, _globalsJson;
		private bool _saved, _subscribed, _runInBackground, _srpBatching;
		private int _antiAliasing, _qualityLevel, _lastCameraFrame = -1;
		private string[] _qualityNames;
		private bool _createCalled, _qualityWriteAttempted, _globalsWriteAttempted, _backgroundWriteAttempted;
		private bool _featureDisposed, _restoreComplete;
		private RenderPipeline _observedOwnedPipeline;

		internal bool CleanupPending => _createCalled && !_restoreComplete;
		internal IReadOnlyList<string> CleanupErrors => _cleanupErrors;
		internal UniversalRenderPipelineAsset OwnedPipeline => _pipeline;
		internal RenderPipelineGlobalSettings OwnedGlobals => _globalsClone;
		internal bool HasFeatureCustody(ScriptableRendererFeature feature) => _createCalled && ReferenceEquals(_feature, feature);
		internal bool HasOwnedAssets => _pipeline != null || _renderer != null || _globalsClone != null ||
			_feature != null || _cameraObject != null || _target != null;

		internal int CameraFrameCount { get; private set; }
		internal int ForeignCameraCallbacks { get; private set; }
		internal ulong PipelineEntityId => EntityId.ToULong(_pipeline.GetEntityId());
		internal bool IsStable => _runtimePipeline != null && ReferenceEquals(RenderPipelineManager.currentPipeline, _runtimePipeline) &&
			GraphicsSettings.currentRenderPipeline == _pipeline && QualitySettings.renderPipeline == _pipeline &&
			GraphicsSettings.defaultRenderPipeline == _default && QualitySettings.GetQualityLevel() == _qualityLevel &&
			EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>() == _globalsClone;

		internal static bool HasActiveUrp => (QualitySettings.renderPipeline != null
			? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline) is UniversalRenderPipelineAsset;

		internal void Create(ScriptableRendererFeature feature, Action<ConstructionStage> checkpoint = null)
		{
			if (_createCalled) throw new InvalidOperationException("A pipeline scope is single-use.");
			if (RetainedScopes.Count != 0) throw new InvalidOperationException("An earlier scope still owns or retains its graph; do not replace its caller state.");
			UrpCameraPipelineControlCustody.RequireEmpty();
			_createCalled = true;
			_feature = feature; // Custody precedes all fallible snapshots and partial construction.
			RetainedScopes.Add(this);
			Assert.That(feature, Is.Not.Null);
			_qualityLevel = QualitySettings.GetQualityLevel();
			_qualityNames = QualitySettings.names;
			_quality = QualitySettings.renderPipeline;
			_default = GraphicsSettings.defaultRenderPipeline;
			_sourcePipeline = (_quality != null ? _quality : _default) as UniversalRenderPipelineAsset;
			Assert.That(_sourcePipeline, Is.Not.Null, "BLOCKED_URP_CONFIGURATION: an active URP asset is required.");
			using (SerializedObject serialized = new SerializedObject(_sourcePipeline))
			{
				SerializedProperty list = Require(serialized, "m_RendererDataList");
				int index = Require(serialized, "m_DefaultRendererIndex").intValue;
				Assert.That(index, Is.InRange(0, list.arraySize - 1));
				_sourceRenderer = list.GetArrayElementAtIndex(index).objectReferenceValue as UniversalRendererData;
			}
			Assert.That(_sourceRenderer, Is.Not.Null, "BLOCKED_URP_CONFIGURATION: the default renderer must be UniversalRendererData, not 2D.");
			_globals = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();
			Assert.That(_globals, Is.Not.Null, "BLOCKED_URP_CONFIGURATION: configure URP global settings first; this fixture never creates persistent assets.");
			_pipelineJson = EditorJsonUtility.ToJson(_sourcePipeline);
			_rendererJson = EditorJsonUtility.ToJson(_sourceRenderer);
			_globalsJson = EditorJsonUtility.ToJson(_globals);
			_activeTarget = RenderTexture.active;
			_runInBackground = Application.runInBackground;
			_srpBatching = GraphicsSettings.useScriptableRenderPipelineBatching;
			_antiAliasing = QualitySettings.antiAliasing;
			_saved = true;
			_pipeline = Clone(_sourcePipeline, "PerfMeter enqueue regression pipeline");
			_renderer = Clone(_sourceRenderer, "PerfMeter enqueue regression renderer");
			_renderer.rendererFeatures.Clear(); // Shared source feature objects are never created/disposed by the clone.
			_renderer.rendererFeatures.Add(feature);
			_renderer.SetDirty();
			using (SerializedObject serialized = new SerializedObject(_pipeline))
			{
				SerializedProperty list = Require(serialized, "m_RendererDataList");
				list.arraySize = 1;
				list.GetArrayElementAtIndex(0).objectReferenceValue = _renderer;
				Require(serialized, "m_DefaultRendererIndex").intValue = 0;
				serialized.ApplyModifiedPropertiesWithoutUndo();
			}
			// The concrete URP globals type is internal in 17.4/17.6; retain its public base handle.
			_globalsClone = Clone(_globals, "PerfMeter enqueue regression globals");
			checkpoint?.Invoke(ConstructionStage.ClonesCreated);
			foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
			{
				if (camera.cameraType != CameraType.Game) continue;
				_cameras.Add((camera, camera.enabled));
				camera.enabled = false;
			}
			_cameraObject = new GameObject("PerfMeter enqueue regression camera") { hideFlags = HideFlags.HideAndDontSave };
			_camera = _cameraObject.AddComponent<Camera>();
			_camera.enabled = false;
			_camera.clearFlags = CameraClearFlags.SolidColor;
			_camera.backgroundColor = Color.black;
			_camera.cullingMask = 0;
			_camera.allowHDR = false;
			_camera.allowMSAA = false;
#if !UNITY_6000_7_OR_NEWER && !UNITY_7000_0_OR_NEWER
			_camera.useOcclusionCulling = false;
#endif // The owned camera has cullingMask=0; newer URP no longer needs the obsolete legacy switch.
			_target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32)
			{
				name = "PerfMeter enqueue regression target", hideFlags = HideFlags.HideAndDontSave, antiAliasing = 1
			};
			_target.Create();
			Assert.That(_target.IsCreated(), Is.True, "BLOCKED_RENDER_TARGET: camera RT allocation failed.");
			_camera.targetTexture = _target;
			UniversalAdditionalCameraData data = _cameraObject.AddComponent<UniversalAdditionalCameraData>();
			data.SetRenderer(0);
			data.renderType = CameraRenderType.Base;
			data.renderPostProcessing = false;
			RenderPipelineManager.endCameraRendering += OnCameraRendered;
			_subscribed = true;
			RequireAcquisitionState(false);
			_globalsWriteAttempted = true;
			EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(_globalsClone);
			checkpoint?.Invoke(ConstructionStage.GlobalsInstalled);
			RequireAcquisitionState(true);
			_qualityWriteAttempted = true;
			QualitySettings.renderPipeline = _pipeline;
			checkpoint?.Invoke(ConstructionStage.QualityInstalled);
			_backgroundWriteAttempted = true;
			Application.runInBackground = true;
			_camera.enabled = true; // Automatic URP rendering only: never Camera.Render or synthetic callback calls.
		}

		internal void FreezePipelineIdentity()
		{
			Assert.That(CameraFrameCount, Is.GreaterThan(0), "BLOCKED_RENDER_LOOP: the configured RT camera never rendered.");
			Assert.That(RenderPipelineManager.currentPipeline, Is.InstanceOf<UniversalRenderPipeline>());
			_runtimePipeline = RenderPipelineManager.currentPipeline;
			Assert.That(IsStable, Is.True, "BLOCKED_PIPELINE_ACTIVATION: the temporary pipeline/global settings are not active.");
		}

		internal void AssertStable()
		{
			Assert.That(IsStable, Is.True, "BLOCKED_PIPELINE_CHANGED: runtime=" + ReferenceEquals(RenderPipelineManager.currentPipeline, _runtimePipeline) +
				", active=" + (GraphicsSettings.currentRenderPipeline == _pipeline) + ", quality=" + (QualitySettings.renderPipeline == _pipeline) +
				", default=" + (GraphicsSettings.defaultRenderPipeline == _default) + ", level=" + (QualitySettings.GetQualityLevel() == _qualityLevel) +
				", globals=" + (EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>() == _globalsClone));
			Assert.That(ForeignCameraCallbacks, Is.Zero, "BLOCKED_CAMERA_ISOLATION: another camera rendered on the clone; close Scene/Preview views and run alone.");
			Assert.That(_camera.enabled && _camera.targetTexture == _target && _target.IsCreated(), Is.True);
		}

		internal IEnumerator Restore()
		{
			if (_restoreComplete) yield break;
			_cleanupErrors.Clear();
			if (_subscribed) Cleanup(() => { RenderPipelineManager.endCameraRendering -= OnCameraRendered; _subscribed = false; });
			if (_camera != null)
			{
				Cleanup(() => _camera.enabled = false);
				Cleanup(() =>
				{
					if (_camera.targetTexture != null && _camera.targetTexture != _target)
						throw new InvalidOperationException("Owned camera has a foreign target; camera and native assets retained.");
					_camera.targetTexture = null;
				});
			}
			bool sdkQuiescent = false;
			Cleanup(() => { RequireSdkQuiescent(); sdkQuiescent = true; });
			if (!sdkQuiescent) yield break;
			if (_saved)
			{
				Cleanup(RestoreGlobals);
				Cleanup(RestoreQualitySlot);
				foreach (var state in _cameras)
					if (state.Camera != null) Cleanup(() =>
					{
						if (state.Camera.enabled != state.Enabled && state.Camera.enabled)
							throw new InvalidOperationException("A caller camera changed after the owned disable; preserving it.");
						state.Camera.enabled = state.Enabled;
					});
				Cleanup(() =>
				{
					if (!_backgroundWriteAttempted || Application.runInBackground == _runInBackground) return;
					if (!Application.runInBackground) throw new InvalidOperationException("Foreign background policy; preserving it.");
					Application.runInBackground = _runInBackground;
				});
			}
			yield return null;
			yield return null; // Allow ordinary pipeline transition, then prove ownership again before retirement.
			if (_saved)
			{
				bool bindingsRestored = false;
				Cleanup(() => { RequireCallerBindingsRestored(); bindingsRestored = true; });
				if (bindingsRestored)
					RestoreKnownScalars();
			}
			Cleanup(RequireNoForeignOwnedReferences);
			Cleanup(RequireSdkQuiescent);
			if (_cleanupErrors.Count != 0) yield break; // Static custody retains the whole remaining graph for retry.
			if (_feature != null && !_featureDisposed)
				Cleanup(() => { _feature.Dispose(); _featureDisposed = true; }); // URP swallows these exceptions; call authoritatively first.
			if (_cleanupErrors.Count != 0) yield break;
			if (_saved) Cleanup(RequireCallerBindingsRestored); // Dispose callbacks may themselves change global state.
			Cleanup(RequireNoForeignOwnedReferences);
			Cleanup(RequireSdkQuiescent);
			if (_cleanupErrors.Count != 0) yield break;
			Destroy(_pipeline); // OnDisable retires this asset's cached renderers and only its current pipeline instance.
			if (_observedOwnedPipeline != null && ReferenceEquals(RenderPipelineManager.currentPipeline, _observedOwnedPipeline))
				_cleanupErrors.Add("The observed owned runtime pipeline is still current after asset retirement; native assets retained.");
			if (_saved) Cleanup(RequireCallerBindingsRestored);
			Cleanup(RequireNoForeignOwnedReferences);
			if (_cleanupErrors.Count != 0) yield break;
			if (_target != null) Cleanup(_target.Release);
			if (_cleanupErrors.Count != 0) yield break;
			Destroy(_cameraObject);
			if (_cleanupErrors.Count != 0) yield break;
			Destroy(_target);
			if (_cleanupErrors.Count != 0) yield break;
			Destroy(_feature);
			if (_cleanupErrors.Count != 0) yield break;
			Destroy(_renderer);
			if (_cleanupErrors.Count != 0) yield break;
			Destroy(_globalsClone);
			yield return null;
			if (_saved)
			{
				Cleanup(RequireCallerBindingsRestored);
				if (_cleanupErrors.Count == 0) RestoreKnownScalars(); // A restored URP instance may have just synchronized MSAA/batching.
				Cleanup(RequireCallerValuesRestored);
			}
			if (HasOwnedAssets) _cleanupErrors.Add("Owned objects remain after cleanup.");
			if (_cleanupErrors.Count != 0) yield break;
			_restoreComplete = true;
			RetainedScopes.Remove(this);
		}

		internal void AssertRestored()
		{
			Assert.That(CleanupPending, Is.False, "Owned scope retained: " + string.Join("; ", _cleanupErrors));
			Assert.That(_cleanupErrors, Is.Empty);
		}

		private bool QualityDomainMatches() => _qualityNames != null && QualitySettings.names.SequenceEqual(_qualityNames);
		private void RequireAcquisitionState(bool globalsInstalled)
		{
			if (!QualityDomainMatches() || QualitySettings.GetQualityLevel() != _qualityLevel ||
				QualitySettings.renderPipeline != _quality || GraphicsSettings.defaultRenderPipeline != _default ||
				EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>() != (globalsInstalled ? _globalsClone : _globals))
				throw new InvalidOperationException("Caller state changed during partial pipeline construction; no further mutation is admitted.");
		}

		private void RestoreQualitySlot()
		{
			if (!_qualityWriteAttempted) return;
			if (!QualityDomainMatches() || QualitySettings.GetQualityLevel() != _qualityLevel)
				throw new InvalidOperationException("Captured quality slot is not current; no indexed public setter exists. Original slot and clones retained, foreign level untouched.");
			if (QualitySettings.renderPipeline == _quality) { _qualityWriteAttempted = false; return; }
			if (QualitySettings.renderPipeline != _pipeline)
				throw new InvalidOperationException("Captured quality slot has a foreign pipeline; preserving it and retaining custody.");
			QualitySettings.renderPipeline = _quality;
			if (QualitySettings.renderPipeline != _quality) throw new InvalidOperationException("Captured quality slot restoration did not complete.");
			_qualityWriteAttempted = false;
		}

		private void RestoreGlobals()
		{
			if (!_globalsWriteAttempted) return;
			var current = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();
			if (current == _globals) { _globalsWriteAttempted = false; return; }
			if (current != _globalsClone) throw new InvalidOperationException("Foreign URP globals; preserving them and retaining custody.");
			EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(_globals);
			if (EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>() != _globals)
				throw new InvalidOperationException("URP globals restoration did not complete.");
			_globalsWriteAttempted = false;
		}

		private void RequireCallerBindingsRestored()
		{
			if (!QualityDomainMatches() || QualitySettings.GetQualityLevel() != _qualityLevel ||
				QualitySettings.renderPipeline != _quality || GraphicsSettings.defaultRenderPipeline != _default ||
				EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>() != _globals ||
				EditorJsonUtility.ToJson(_sourcePipeline) != _pipelineJson || EditorJsonUtility.ToJson(_sourceRenderer) != _rendererJson ||
				EditorJsonUtility.ToJson(_globals) != _globalsJson)
				throw new InvalidOperationException("Caller slot/global/source identity remains changed; no owned assets may be destroyed.");
		}

		private void RestoreKnownScalars()
		{
			// Both installed URP constructors synchronize MSAA/batching. Every setter rechecks the slot/global
			// owner so a callback or an earlier setter's exception cannot authorize the next independent write.
			Cleanup(() =>
			{
				RequireCallerBindingsRestored();
				if (QualitySettings.antiAliasing != _antiAliasing && QualitySettings.antiAliasing != _sourcePipeline.msaaSampleCount)
					throw new InvalidOperationException("Foreign MSAA value; preserving it.");
				QualitySettings.antiAliasing = _antiAliasing;
			});
			Cleanup(() =>
			{
				RequireCallerBindingsRestored();
				if (GraphicsSettings.useScriptableRenderPipelineBatching != _srpBatching && GraphicsSettings.useScriptableRenderPipelineBatching != _sourcePipeline.useSRPBatcher)
					throw new InvalidOperationException("Foreign SRP batching value; preserving it.");
				GraphicsSettings.useScriptableRenderPipelineBatching = _srpBatching;
			});
			Cleanup(() =>
			{
				if (RenderTexture.active != _activeTarget && (_target == null || RenderTexture.active != _target))
					throw new InvalidOperationException("Foreign active render target; preserving it.");
				RenderTexture.active = _activeTarget;
			});
		}

		private void RequireCallerValuesRestored()
		{
			if (Application.runInBackground != _runInBackground || QualitySettings.antiAliasing != _antiAliasing ||
				GraphicsSettings.useScriptableRenderPipelineBatching != _srpBatching || RenderTexture.active != _activeTarget ||
				_cameras.Any(x => x.Camera != null && x.Camera.enabled != x.Enabled))
				throw new InvalidOperationException("Caller scalar/camera restoration remains incomplete.");
		}

		private void RequireNoForeignOwnedReferences()
		{
			if (_pipeline != null)
			{
				if (GraphicsSettings.currentRenderPipeline == _pipeline || GraphicsSettings.defaultRenderPipeline == _pipeline ||
					Enumerable.Range(0, QualitySettings.names.Length).Any(i => QualitySettings.GetRenderPipelineAssetAt(i) == _pipeline))
					throw new InvalidOperationException("An active/default/quality slot still references the owned pipeline.");
			}
			if (_globalsClone != null && EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>() == _globalsClone)
				throw new InvalidOperationException("Owned globals are still registered.");
			if (_renderer != null && Resources.FindObjectsOfTypeAll<UniversalRenderPipelineAsset>().Any(p => p != _pipeline && ReferencesOwnedRenderer(p)))
				throw new InvalidOperationException("A foreign pipeline references the owned renderer.");
			if (_feature != null && Resources.FindObjectsOfTypeAll<ScriptableRendererData>().Any(r => r != _renderer && r.rendererFeatures.Contains(_feature)))
				throw new InvalidOperationException("A foreign renderer references the owned feature.");
			if (_target != null && Resources.FindObjectsOfTypeAll<Camera>().Any(c => c != _camera && c.targetTexture == _target))
				throw new InvalidOperationException("A foreign camera references the owned render target.");
		}

		internal static void RequireSdkQuiescent()
		{
			// The owning test stops only its runtime. A failed stop, retained capture or another workflow
			// cannot be converted into permission to retire the camera/pipeline/material/RT graph here.
			var graphics = PerformanceMeter.GetGraphicsStateCollectionStatus();
			if (PerformanceMeter.GetStatus().State != PerfMeterRuntimeState.Stopped || PerformanceMeter.IsSessionRecording ||
				!string.IsNullOrEmpty(PerformanceMeter.GetCaptureStatus().CaptureId) || PerformanceMeter.GetMemorySnapshotStatus().IsActive ||
				graphics.IsBusy || graphics.HasPendingCleanup)
				throw new InvalidOperationException("SDK runtime/native work is not known quiescent; scope assets retained without stopping foreign work.");
		}

		private bool ReferencesOwnedRenderer(UniversalRenderPipelineAsset pipeline)
		{
			var renderers = pipeline.rendererDataList; // Array in older URP; ReadOnlySpan in the current API.
			for (int i = 0; i < renderers.Length; i++) if (renderers[i] == _renderer) return true;
			return false;
		}

		private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
		{
			if (GraphicsSettings.currentRenderPipeline != _pipeline) return;
			_observedOwnedPipeline = RenderPipelineManager.currentPipeline;
			if (camera != _camera) { ForeignCameraCallbacks++; return; }
			if (_lastCameraFrame == Time.frameCount) return;
			_lastCameraFrame = Time.frameCount;
			CameraFrameCount++;
		}

		private static T Clone<T>(T source, string name) where T : Object
		{
			T clone = Object.Instantiate(source);
			clone.name = name;
			clone.hideFlags = HideFlags.HideAndDontSave;
			return clone;
		}

		internal static SerializedProperty Require(SerializedObject serialized, string name)
		{
			SerializedProperty property = serialized.FindProperty(name);
			Assert.That(property, Is.Not.Null, "BLOCKED_FIXTURE_DRIFT: missing serialized field " + name);
			return property;
		}

		private void Cleanup(Action action)
		{
			try { action(); }
			catch (Exception exception) { _cleanupErrors.Add(exception.ToString()); }
		}

		private void Destroy(Object value)
		{
			if (value != null) Cleanup(() => Object.DestroyImmediate(value));
		}
	}
}
