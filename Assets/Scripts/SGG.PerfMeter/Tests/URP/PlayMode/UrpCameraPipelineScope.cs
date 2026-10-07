using System;
using System.Collections;
using System.Collections.Generic;
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

		internal int CameraFrameCount { get; private set; }
		internal int ForeignCameraCallbacks { get; private set; }
		internal ulong PipelineEntityId => EntityId.ToULong(_pipeline.GetEntityId());
		internal bool IsStable => _runtimePipeline != null && ReferenceEquals(RenderPipelineManager.currentPipeline, _runtimePipeline) &&
			GraphicsSettings.currentRenderPipeline == _pipeline && QualitySettings.renderPipeline == _pipeline &&
			GraphicsSettings.defaultRenderPipeline == _default && QualitySettings.GetQualityLevel() == _qualityLevel &&
			EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>() == _globalsClone;

		internal static bool HasActiveUrp => (QualitySettings.renderPipeline != null
			? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline) is UniversalRenderPipelineAsset;

		internal void Create(ScriptableRendererFeature feature)
		{
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
			_qualityLevel = QualitySettings.GetQualityLevel();
			_saved = true;
			_feature = feature;
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
			foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
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
			_camera.useOcclusionCulling = false;
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
			EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(_globalsClone);
			QualitySettings.renderPipeline = _pipeline;
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
			if (_subscribed) RenderPipelineManager.endCameraRendering -= OnCameraRendered;
			if (_camera != null) { _camera.enabled = false; _camera.targetTexture = null; }
			if (_saved)
			{
				Cleanup(() => EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(_globals));
				Cleanup(() => QualitySettings.renderPipeline = _quality);
				foreach (var state in _cameras)
					if (state.Camera != null) Cleanup(() => state.Camera.enabled = state.Enabled);
				Application.runInBackground = _runInBackground;
			}
			yield return null;
			yield return null; // Retire the temporary pipeline before releasing its renderer, feature and RT.
			if (_feature != null) Cleanup(_feature.Dispose);
			if (_saved) RenderTexture.active = _activeTarget;
			if (_target != null) Cleanup(_target.Release);
			Destroy(_cameraObject);
			Destroy(_target);
			Destroy(_pipeline);
			Destroy(_feature);
			Destroy(_renderer);
			Destroy(_globalsClone);
			yield return null;
			if (_saved)
			{
				RenderTexture.active = _activeTarget;
				QualitySettings.antiAliasing = _antiAliasing;
				GraphicsSettings.useScriptableRenderPipelineBatching = _srpBatching;
				Assert.That(QualitySettings.renderPipeline, Is.SameAs(_quality));
				Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(_default));
				Assert.That(EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(), Is.SameAs(_globals));
				Assert.That(EditorJsonUtility.ToJson(_sourcePipeline), Is.EqualTo(_pipelineJson), "Authored pipeline changed.");
				Assert.That(EditorJsonUtility.ToJson(_sourceRenderer), Is.EqualTo(_rendererJson), "Authored renderer changed.");
				Assert.That(EditorJsonUtility.ToJson(_globals), Is.EqualTo(_globalsJson), "Authored global settings changed.");
				Assert.That(Application.runInBackground, Is.EqualTo(_runInBackground));
				Assert.That(RenderTexture.active, Is.SameAs(_activeTarget));
				Assert.That(QualitySettings.antiAliasing, Is.EqualTo(_antiAliasing));
				Assert.That(GraphicsSettings.useScriptableRenderPipelineBatching, Is.EqualTo(_srpBatching));
				Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(_qualityLevel));
				foreach (var state in _cameras)
					if (state.Camera != null) Assert.That(state.Camera.enabled, Is.EqualTo(state.Enabled));
			}
			Assert.That(_cameraObject == null && _target == null && _pipeline == null && _renderer == null && _globalsClone == null && _feature == null, Is.True);
			Assert.That(_cleanupErrors, Is.Empty, "Cleanup errors: " + string.Join("; ", _cleanupErrors));
		}

		private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
		{
			if (GraphicsSettings.currentRenderPipeline != _pipeline) return;
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
