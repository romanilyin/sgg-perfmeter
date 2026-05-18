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

				if (_currentMarkerName == markerName)
				{
					return;
				}

				_currentMarkerName = markerName;
				profilingSampler = new ProfilingSampler(markerName);
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
				if (!resourceData.activeColorTexture.IsValid())
				{
					return;
				}

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
				}

				if (!PerfMeterRuntime.IsOverdrawMeasurementActive && !PerfMeterRuntime.IsOverdrawHeatmapVisible)
				{
					return;
				}

				RecordOverdrawPass(renderGraph, frameData, resourceData);
			}

			internal void Dispose()
			{
				CoreUtils.Destroy(_overdrawMaterial);
				_overdrawMaterial = null;
				CoreUtils.Destroy(_overdrawHeatmapMaterial);
				_overdrawHeatmapMaterial = null;
			}

			private void RecordOverdrawPass(RenderGraph renderGraph, ContextContainer frameData, UniversalResourceData resourceData)
			{
				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				if (!ShouldMeasureCamera(cameraData))
				{
					return;
				}

				if (PerfMeterRuntime.IsOverdrawMeasurementActive)
				{
					RecordOverdrawCounterPass(renderGraph, frameData, resourceData, cameraData);
				}

				if (PerfMeterRuntime.IsOverdrawHeatmapVisible)
				{
					RecordOverdrawHeatmapPass(renderGraph, frameData, resourceData, cameraData);
				}
			}

			private void RecordOverdrawCounterPass(RenderGraph renderGraph, ContextContainer frameData, UniversalResourceData resourceData, UniversalCameraData cameraData)
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

					return;
				}

				RendererListHandle rendererListHandle = CreateOverdrawRendererList(renderGraph, frameData, cameraData, overdrawMaterial);
				if (!rendererListHandle.IsValid())
				{
					PerfMeterRuntime.FailOverdrawMeasurement("PerfMeter overdraw renderer list could not be created.");
					return;
				}

				int screenPixelCount = GetScreenPixelCount(cameraData);
				if (!PerfMeterRuntime.TryBeginOverdrawRenderGraphFrame(Time.frameCount, screenPixelCount, out GraphicsBuffer counterBuffer, out int measurementId))
				{
					return;
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
			}

			private void RecordOverdrawHeatmapPass(RenderGraph renderGraph, ContextContainer frameData, UniversalResourceData resourceData, UniversalCameraData cameraData)
			{
				Material heatmapMaterial = GetOverdrawHeatmapMaterial();
				if (heatmapMaterial == null)
				{
					return;
				}

				RendererListHandle rendererListHandle = CreateOverdrawRendererList(renderGraph, frameData, cameraData, heatmapMaterial);
				if (!rendererListHandle.IsValid())
				{
					return;
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
