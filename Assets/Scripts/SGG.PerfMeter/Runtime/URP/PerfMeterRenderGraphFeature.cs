using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SGG.PerfMeter
{
	/// <summary>
	/// URP Render Graph feature that exposes PerfMeter markers, overdraw measurement, and heatmap passes.
	/// </summary>
	[DisallowMultipleRendererFeature("SGG PerfMeter")]
	public sealed class PerfMeterRenderGraphFeature : ScriptableRendererFeature
	{
		public const string DefaultMarkerName = "SGG.PerfMeter.Overlay";
		public const string DefaultOverdrawMarkerName = "SGG.PerfMeter.Overdraw";
		public const string DefaultOverdrawHeatmapMarkerName = "SGG.PerfMeter.OverdrawHeatmap";

		[SerializeField]
		private Settings _settings = new Settings();

		private OverlayMarkerPass _overlayMarkerPass;

		public Settings FeatureSettings => _settings;

		public override void Create()
		{
			_overlayMarkerPass ??= new OverlayMarkerPass();
			ApplySettingsToPass();
		}

		protected override void Dispose(bool disposing)
		{
			_overlayMarkerPass?.Dispose();
			_overlayMarkerPass = null;
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (_settings == null || !_settings.Enabled)
			{
				return;
			}

			if (!_settings.RecordOverlayMarkerPass && !PerfMeterRuntime.IsOverdrawMeasurementActive && !PerfMeterRuntime.IsOverdrawHeatmapVisible)
			{
				return;
			}

			_overlayMarkerPass ??= new OverlayMarkerPass();
			ApplySettingsToPass();
			PerfMeterRenderGraphAnalytics.PrepareObservation(renderingData.cameraData.camera);
			renderer.EnqueuePass(_overlayMarkerPass);
		}

		private void ApplySettingsToPass()
		{
			if (_settings == null)
			{
				_settings = new Settings();
			}

			_overlayMarkerPass.Setup(
				_settings.RenderPassEvent,
				GetSafeMarkerName(_settings.MarkerName),
				_settings.RecordOverlayMarkerPass,
				_settings.GameCamerasOnly,
				_settings.CameraNameFilter);
		}

		private static string GetSafeMarkerName(string markerName)
		{
			return string.IsNullOrWhiteSpace(markerName) ? DefaultMarkerName : markerName;
		}

		[System.Serializable]
		public sealed class Settings
		{
			[SerializeField]
			private bool _enabled = true;

			[SerializeField]
			private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

			[SerializeField]
			private string _markerName = DefaultMarkerName;

			[SerializeField]
			private bool _recordOverlayMarkerPass;

			[SerializeField]
			private bool _gameCamerasOnly = true;

			[SerializeField]
			private string _cameraNameFilter = string.Empty;

			public bool Enabled => _enabled;

			public RenderPassEvent RenderPassEvent => _renderPassEvent;

			public string MarkerName => _markerName;

			public bool RecordOverlayMarkerPass => _recordOverlayMarkerPass;

			public bool GameCamerasOnly => _gameCamerasOnly;

			public string CameraNameFilter => _cameraNameFilter;
		}

		private sealed class OverlayMarkerPass : ScriptableRenderPass
		{
			private const string OverdrawShaderName = "Hidden/SGG/PerfMeter/OverdrawCounter";
			private const string OverdrawShaderResourcePath = "SGGPerfMeterOverdrawCounter";
			private const string OverdrawHeatmapShaderName = "Hidden/SGG/PerfMeter/OverdrawHeatmap";
			private const string OverdrawHeatmapShaderResourcePath = "SGGPerfMeterOverdrawHeatmap";
			private const int OverdrawCounterUavIndex = 1;
			private const string GpuResidentDrawerDisabledModeName = "Disabled";
			private const string GpuResidentDrawerInstancedDrawingModeName = "InstancedDrawing";
			private const string GpuResidentDrawerUnknownModeName = "Unknown";
			private const string GpuResidentDrawerUnavailableWarning = "The active render pipeline does not expose IGPUResidentRenderPipeline; GPU Resident Drawer context is unavailable.";
			private const string GpuResidentDrawerSupportErrorWarning = "GPU Resident Drawer support query failed for the active URP asset.";
			private const string GpuResidentDrawerSupportUnavailableWarning = "GPU Resident Drawer is not supported by the active URP asset.";
			private const string GpuResidentDrawerSupportExceptionWarningPrefix = "GPU Resident Drawer support query failed for the active URP asset: ";

			private static bool _hasCachedGpuResidentDrawerSnapshot;
			private static RenderPipelineAsset _cachedRenderPipelineAsset;
			private static bool _cachedHasGpuResidentRenderPipeline;
			private static bool _cachedGpuResidentDrawerModeReadFailed;
			private static GPUResidentDrawerMode _cachedGpuResidentDrawerMode;
			private static PerfMeterGpuResidentDrawerContextSnapshot _cachedGpuResidentDrawerSnapshot;

			private readonly List<ShaderTagId> _overdrawShaderTagIds = new List<ShaderTagId>
			{
				new ShaderTagId("UniversalForwardOnly"),
				new ShaderTagId("UniversalForward"),
				new ShaderTagId("UniversalGBuffer"),
				new ShaderTagId("SRPDefaultUnlit"),
				new ShaderTagId("LightweightForward")
			};

			private string _currentMarkerName;
			private bool _recordOverlayMarkerPass;
			private bool _gameCamerasOnly = true;
			private string _cameraNameFilter = string.Empty;
			private RenderPassEvent _cachedRenderPassEvent;
			private string _injectionPoint = string.Empty;
			private bool _hasCachedRenderPassEvent;
			private ProfilingSampler _overdrawProfilingSampler = new ProfilingSampler(DefaultOverdrawMarkerName);
			private ProfilingSampler _overdrawHeatmapProfilingSampler = new ProfilingSampler(DefaultOverdrawHeatmapMarkerName);
			private Material _overdrawMaterial;
			private Material _overdrawHeatmapMaterial;

			internal void Setup(RenderPassEvent passEvent, string markerName, bool recordOverlayMarkerPass, bool gameCamerasOnly, string cameraNameFilter)
			{
				renderPassEvent = passEvent;
				_recordOverlayMarkerPass = recordOverlayMarkerPass;
				_gameCamerasOnly = gameCamerasOnly;
				_cameraNameFilter = cameraNameFilter ?? string.Empty;

				if (!_hasCachedRenderPassEvent || _cachedRenderPassEvent != passEvent)
				{
					_cachedRenderPassEvent = passEvent;
					_injectionPoint = passEvent.ToString();
					_hasCachedRenderPassEvent = true;
				}

				if (_currentMarkerName == markerName)
				{
					return;
				}

				_currentMarkerName = markerName;
				profilingSampler = new ProfilingSampler(markerName);
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				using PerfMeterSelfObservability.MeasurementScope selfOverheadScope = PerfMeterSelfObservability.Measure(PerfMeterSelfOverheadComponent.UrpRenderIntegration);
				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
				if (!resourceData.activeColorTexture.IsValid())
				{
					return;
				}

				UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
				bool shouldMeasureCamera = (PerfMeterRuntime.IsOverdrawMeasurementActive || PerfMeterRuntime.IsOverdrawHeatmapVisible) && ShouldMeasureCamera(cameraData);
				bool recordsOverlayMarkerPass = false;
				if (_recordOverlayMarkerPass)
				{
					using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<MarkerPassData>(_currentMarkerName, out MarkerPassData passData, profilingSampler))
					{
						builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
						builder.AllowPassCulling(false);
						builder.SetRenderFunc(static (MarkerPassData data, RasterGraphContext context) =>
						{
							// Intentionally empty: opt-in diagnostic marker for measuring PerfMeter overhead.
						});
					}

					recordsOverlayMarkerPass = true;
				}

				bool recordsOverdrawCounterPass = false;
				bool recordsOverdrawHeatmapPass = false;
				if (PerfMeterRuntime.IsOverdrawMeasurementActive || PerfMeterRuntime.IsOverdrawHeatmapVisible)
				{
					RecordOverdrawPass(
						renderGraph,
						frameData,
						resourceData,
						cameraData,
						shouldMeasureCamera,
						out recordsOverdrawCounterPass,
						out recordsOverdrawHeatmapPass);
				}

				PerfMeterRenderGraphAnalytics.RecordFeatureSnapshot(
					cameraData.camera,
					GetEffectiveRenderingMode(renderingData.renderingMode),
					_injectionPoint,
					GetGpuResidentDrawerSnapshot(),
					recordsOverlayMarkerPass,
					recordsOverdrawCounterPass,
					recordsOverdrawHeatmapPass,
					PerfMeterAvailability.Available,
					renderingData.renderingMode == RenderingMode.ForwardPlus,
					PerfMeterAvailability.Available,
					renderingData.renderingMode == RenderingMode.ForwardPlus || renderingData.renderingMode == RenderingMode.DeferredPlus);
			}

			internal void Dispose()
			{
				CoreUtils.Destroy(_overdrawMaterial);
				_overdrawMaterial = null;
				CoreUtils.Destroy(_overdrawHeatmapMaterial);
				_overdrawHeatmapMaterial = null;
			}

			private void RecordOverdrawPass(
				RenderGraph renderGraph,
				ContextContainer frameData,
				UniversalResourceData resourceData,
				UniversalCameraData cameraData,
				bool shouldMeasureCamera,
				out bool recordsOverdrawCounterPass,
				out bool recordsOverdrawHeatmapPass)
			{
				recordsOverdrawCounterPass = false;
				recordsOverdrawHeatmapPass = false;
				if (!shouldMeasureCamera)
				{
					return;
				}

				if (PerfMeterRuntime.IsOverdrawMeasurementActive)
				{
					recordsOverdrawCounterPass = RecordOverdrawCounterPass(renderGraph, frameData, resourceData, cameraData);
				}

				if (PerfMeterRuntime.IsOverdrawHeatmapVisible)
				{
					recordsOverdrawHeatmapPass = RecordOverdrawHeatmapPass(renderGraph, frameData, resourceData, cameraData);
				}
			}

			private bool RecordOverdrawCounterPass(RenderGraph renderGraph, ContextContainer frameData, UniversalResourceData resourceData, UniversalCameraData cameraData)
			{
				Material overdrawMaterial = GetOverdrawMaterial(out string materialError, out bool unsupported);
				if (overdrawMaterial == null)
				{
					if (unsupported)
					{
						PerfMeterRuntime.MarkOverdrawMeasurementUnsupported(materialError);
					}
					else
					{
						PerfMeterRuntime.FailOverdrawMeasurement(materialError);
					}

					return false;
				}

				RendererListHandle rendererListHandle = CreateOverdrawRendererList(renderGraph, frameData, cameraData, overdrawMaterial);
				if (!rendererListHandle.IsValid())
				{
					PerfMeterRuntime.FailOverdrawMeasurement("PerfMeter overdraw renderer list could not be created.");
					return false;
				}

				int screenPixelCount = GetScreenPixelCount(cameraData);
				if (!PerfMeterRuntime.TryBeginOverdrawRenderGraphFrame(Time.frameCount, screenPixelCount, out GraphicsBuffer counterBuffer, out int measurementId))
				{
					return false;
				}

				BufferHandle counterBufferHandle = renderGraph.ImportBuffer(counterBuffer);

				using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<OverdrawPassData>(DefaultOverdrawMarkerName, out OverdrawPassData passData, _overdrawProfilingSampler))
				{
					passData.RendererListHandle = rendererListHandle;
					passData.CounterBufferHandle = counterBufferHandle;

					builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
					builder.UseRendererList(passData.RendererListHandle);
					builder.UseBufferRandomAccess(passData.CounterBufferHandle, OverdrawCounterUavIndex, AccessFlags.ReadWrite);
					builder.AllowPassCulling(false);
					builder.SetRenderFunc(static (OverdrawPassData data, RasterGraphContext context) =>
					{
						context.cmd.DrawRendererList(data.RendererListHandle);
					});
				}

				using (IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass<OverdrawReadbackPassData>(DefaultOverdrawMarkerName + ".Readback", out OverdrawReadbackPassData passData))
				{
					passData.CounterBufferHandle = counterBufferHandle;
					passData.MeasurementId = measurementId;
					builder.UseBuffer(passData.CounterBufferHandle, AccessFlags.Read);
					builder.AllowPassCulling(false);
					builder.SetRenderFunc(static (OverdrawReadbackPassData data, UnsafeGraphContext context) =>
					{
						int callbackMeasurementId = data.MeasurementId;
						context.cmd.RequestAsyncReadback(data.CounterBufferHandle, request => PerfMeterRuntime.CompleteOverdrawCounterReadback(callbackMeasurementId, request));
					});
				}

				return true;
			}

			private bool RecordOverdrawHeatmapPass(RenderGraph renderGraph, ContextContainer frameData, UniversalResourceData resourceData, UniversalCameraData cameraData)
			{
				Material heatmapMaterial = GetOverdrawHeatmapMaterial();
				if (heatmapMaterial == null)
				{
					return false;
				}

				RendererListHandle rendererListHandle = CreateOverdrawRendererList(renderGraph, frameData, cameraData, heatmapMaterial);
				if (!rendererListHandle.IsValid())
				{
					return false;
				}

				using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<OverdrawHeatmapPassData>(DefaultOverdrawHeatmapMarkerName, out OverdrawHeatmapPassData passData, _overdrawHeatmapProfilingSampler))
				{
					passData.RendererListHandle = rendererListHandle;
					builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
					builder.UseRendererList(passData.RendererListHandle);
					builder.AllowPassCulling(false);
					builder.SetRenderFunc(static (OverdrawHeatmapPassData data, RasterGraphContext context) =>
					{
						context.cmd.DrawRendererList(data.RendererListHandle);
					});
				}

				return true;
			}

			private RendererListHandle CreateOverdrawRendererList(RenderGraph renderGraph, ContextContainer frameData, UniversalCameraData cameraData, Material overdrawMaterial)
			{
				UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
				UniversalLightData lightData = frameData.Get<UniversalLightData>();
				DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
					_overdrawShaderTagIds,
					renderingData,
					cameraData,
					lightData,
					SortingCriteria.None);
				drawingSettings.overrideMaterial = overdrawMaterial;
				drawingSettings.overrideMaterialPassIndex = 0;
				drawingSettings.perObjectData = PerObjectData.None;

				int layerMask = cameraData.camera != null ? cameraData.camera.cullingMask : ~0;
				FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask)
				{
					batchLayerMask = uint.MaxValue
				};

				RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
				return renderGraph.CreateRendererList(rendererListParams);
			}

			private bool ShouldMeasureCamera(UniversalCameraData cameraData)
			{
				Camera camera = cameraData.camera;
				if (camera == null)
				{
					return false;
				}

				if (_gameCamerasOnly && camera.cameraType != CameraType.Game)
				{
					return false;
				}

				return string.IsNullOrWhiteSpace(_cameraNameFilter) || camera.name.IndexOf(_cameraNameFilter, StringComparison.OrdinalIgnoreCase) >= 0;
			}

			private static PerfMeterGpuResidentDrawerContextSnapshot GetGpuResidentDrawerSnapshot()
			{
				try
				{
					PerfMeterGpuResidentDrawerContextSnapshot supportSnapshot = GetGpuResidentDrawerSupportSnapshot();
					PerfMeterAvailability projectConfigurationAvailability = PerfMeterAvailability.Unknown;
					bool isProjectConfigurationSupported = false;
					PerfMeterAvailability activityAvailability = supportSnapshot.ActivityAvailability;
					bool isObservedActive = supportSnapshot.IsObservedActive;
					string warning = supportSnapshot.Warning;
					if (supportSnapshot.Availability != PerfMeterAvailability.Unavailable)
					{
						try
						{
							isProjectConfigurationSupported = IGPUResidentRenderPipeline.IsGPUResidentDrawerSupportedByProjectConfiguration(false);
							projectConfigurationAvailability = PerfMeterAvailability.Available;
						}
						catch (Exception exception)
						{
							projectConfigurationAvailability = PerfMeterAvailability.Unavailable;
							warning = PerfMeterRenderGraphAnalytics.CombineWarnings(warning, "GPU Resident Drawer project-configuration query failed: " + exception.GetType().Name + ".");
						}

						try
						{
							isObservedActive = IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled();
							activityAvailability = PerfMeterAvailability.Available;
						}
						catch (Exception exception)
						{
							activityAvailability = PerfMeterAvailability.Unavailable;
							warning = PerfMeterRenderGraphAnalytics.CombineWarnings(warning, "GPU Resident Drawer runtime activity query failed: " + exception.GetType().Name + ".");
						}
					}

					PerfMeterGpuResidentDrawerContextSnapshot primitiveObservation = new PerfMeterGpuResidentDrawerContextSnapshot(
						supportSnapshot.Availability,
						supportSnapshot.ConfiguredMode,
						supportSnapshot.SupportAvailability,
						supportSnapshot.IsSupported,
						activityAvailability,
						isObservedActive,
						warning,
						projectConfigurationAvailability,
						isProjectConfigurationSupported,
						PerfMeterAvailability.Unknown,
						false,
						PerfMeterAvailability.Unknown,
						false,
						PerfMeterAvailability.Unknown,
						false,
						PerfMeterGpuResidentDrawerEffectivenessSnapshot.Unknown,
						PerfMeterGpuResidentDrawerReason.Unknown);
					return primitiveObservation;
				}
				catch (Exception exception)
				{
					return new PerfMeterGpuResidentDrawerContextSnapshot(
						PerfMeterAvailability.Available,
						string.Empty,
						PerfMeterAvailability.Unavailable,
						false,
						PerfMeterAvailability.Unknown,
						false,
						GpuResidentDrawerSupportExceptionWarningPrefix + exception.GetType().Name + ".");
				}
			}

			private static PerfMeterGpuResidentDrawerContextSnapshot GetGpuResidentDrawerSupportSnapshot()
			{
				RenderPipelineAsset renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
				IGPUResidentRenderPipeline gpuResidentRenderPipeline = renderPipelineAsset as IGPUResidentRenderPipeline;
				bool hasGpuResidentRenderPipeline = gpuResidentRenderPipeline != null;
				if (_hasCachedGpuResidentDrawerSnapshot &&
					_cachedGpuResidentDrawerModeReadFailed &&
					object.ReferenceEquals(_cachedRenderPipelineAsset, renderPipelineAsset) &&
					_cachedHasGpuResidentRenderPipeline == hasGpuResidentRenderPipeline)
				{
					return _cachedGpuResidentDrawerSnapshot;
				}

				GPUResidentDrawerMode configuredMode = default;
				if (hasGpuResidentRenderPipeline)
				{
					try
					{
						configuredMode = gpuResidentRenderPipeline.gpuResidentDrawerMode;
					}
					catch (Exception exception)
					{
						_cachedRenderPipelineAsset = renderPipelineAsset;
						_cachedHasGpuResidentRenderPipeline = true;
						_cachedGpuResidentDrawerModeReadFailed = true;
						_cachedGpuResidentDrawerSnapshot = new PerfMeterGpuResidentDrawerContextSnapshot(
							PerfMeterAvailability.Available,
							string.Empty,
							PerfMeterAvailability.Unavailable,
							false,
							PerfMeterAvailability.Unknown,
							false,
							GpuResidentDrawerSupportExceptionWarningPrefix + exception.GetType().Name + ".");
						_hasCachedGpuResidentDrawerSnapshot = true;
						return _cachedGpuResidentDrawerSnapshot;
					}
				}

				_hasCachedGpuResidentDrawerSnapshot = true;
				_cachedRenderPipelineAsset = renderPipelineAsset;
				_cachedHasGpuResidentRenderPipeline = hasGpuResidentRenderPipeline;
				_cachedGpuResidentDrawerModeReadFailed = false;
				_cachedGpuResidentDrawerMode = configuredMode;

				if (!hasGpuResidentRenderPipeline)
				{
					_cachedGpuResidentDrawerSnapshot = new PerfMeterGpuResidentDrawerContextSnapshot(
						PerfMeterAvailability.Unavailable,
						string.Empty,
						PerfMeterAvailability.Unavailable,
						false,
						PerfMeterAvailability.Unknown,
						false,
						GpuResidentDrawerUnavailableWarning);
					return _cachedGpuResidentDrawerSnapshot;
				}

				try
				{
					bool isSupported = gpuResidentRenderPipeline.IsGPUResidentDrawerSupportedBySRP(out string supportReason, out LogType supportSeverity);
					_cachedGpuResidentDrawerSnapshot = new PerfMeterGpuResidentDrawerContextSnapshot(
						PerfMeterAvailability.Available,
						GetGpuResidentDrawerModeName(configuredMode),
						PerfMeterAvailability.Available,
						isSupported,
						PerfMeterAvailability.Unknown,
						false,
						GetGpuResidentDrawerWarning(isSupported, supportReason, supportSeverity));
				}
				catch (Exception exception)
				{
					_cachedGpuResidentDrawerSnapshot = new PerfMeterGpuResidentDrawerContextSnapshot(
						PerfMeterAvailability.Available,
						GetGpuResidentDrawerModeName(configuredMode),
						PerfMeterAvailability.Unavailable,
						false,
						PerfMeterAvailability.Unknown,
						false,
						GpuResidentDrawerSupportExceptionWarningPrefix + exception.GetType().Name + ".");
				}

				return _cachedGpuResidentDrawerSnapshot;
			}

			private static string GetEffectiveRenderingMode(RenderingMode renderingMode)
			{
				switch (renderingMode)
				{
					case RenderingMode.Forward:
						return "Forward";
					case RenderingMode.Deferred:
						return "Deferred";
					case RenderingMode.ForwardPlus:
						return "ForwardPlus";
					case RenderingMode.DeferredPlus:
						return "DeferredPlus";
					default:
						return "Unknown";
				}
			}

			private static string GetGpuResidentDrawerModeName(GPUResidentDrawerMode mode)
			{
				switch (mode)
				{
					case GPUResidentDrawerMode.Disabled:
						return GpuResidentDrawerDisabledModeName;
					case GPUResidentDrawerMode.InstancedDrawing:
						return GpuResidentDrawerInstancedDrawingModeName;
					default:
						return GpuResidentDrawerUnknownModeName;
				}
			}

			private static string GetGpuResidentDrawerWarning(bool isSupported, string reason, LogType severity)
			{
				if (isSupported)
				{
					return string.Empty;
				}

				if (!string.IsNullOrEmpty(reason))
				{
					return reason;
				}

				return severity == LogType.Error
					? GpuResidentDrawerSupportErrorWarning
					: GpuResidentDrawerSupportUnavailableWarning;
			}

			private Material GetOverdrawMaterial(out string error, out bool unsupported)
			{
				error = string.Empty;
				unsupported = false;

				if (_overdrawMaterial != null)
				{
					return _overdrawMaterial;
				}

				Shader overdrawShader = Shader.Find(OverdrawShaderName);
				if (overdrawShader == null)
				{
					overdrawShader = Resources.Load<Shader>(OverdrawShaderResourcePath);
				}

				if (overdrawShader == null)
				{
					error = "PerfMeter overdraw shader '" + OverdrawShaderName + "' is missing or stripped from the build.";
					return null;
				}

				if (!overdrawShader.isSupported)
				{
					error = "PerfMeter overdraw shader '" + OverdrawShaderName + "' is unsupported on " + SystemInfo.graphicsDeviceType + ".";
					unsupported = true;
					return null;
				}

				_overdrawMaterial = CoreUtils.CreateEngineMaterial(overdrawShader);
				_overdrawMaterial.hideFlags = HideFlags.HideAndDontSave;
				return _overdrawMaterial;
			}

			private Material GetOverdrawHeatmapMaterial()
			{
				if (_overdrawHeatmapMaterial != null)
				{
					return _overdrawHeatmapMaterial;
				}

				Shader heatmapShader = Shader.Find(OverdrawHeatmapShaderName);
				if (heatmapShader == null)
				{
					heatmapShader = Resources.Load<Shader>(OverdrawHeatmapShaderResourcePath);
				}

				if (heatmapShader == null || !heatmapShader.isSupported)
				{
					return null;
				}

				_overdrawHeatmapMaterial = CoreUtils.CreateEngineMaterial(heatmapShader);
				_overdrawHeatmapMaterial.hideFlags = HideFlags.HideAndDontSave;
				return _overdrawHeatmapMaterial;
			}

			private static int GetScreenPixelCount(UniversalCameraData cameraData)
			{
				int width = cameraData.scaledWidth > 0 ? cameraData.scaledWidth : Screen.width;
				int height = cameraData.scaledHeight > 0 ? cameraData.scaledHeight : Screen.height;
				return Mathf.Max(1, width) * Mathf.Max(1, height);
			}
		}

		private sealed class MarkerPassData
		{
		}

		private sealed class OverdrawPassData
		{
			internal RendererListHandle RendererListHandle;
			internal BufferHandle CounterBufferHandle;
		}

		private sealed class OverdrawHeatmapPassData
		{
			internal RendererListHandle RendererListHandle;
		}

		private sealed class OverdrawReadbackPassData
		{
			internal BufferHandle CounterBufferHandle;
			internal int MeasurementId;
		}
	}

}
