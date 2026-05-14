using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SGG.PerfMeter
{
	/// <summary>
	/// URP Render Graph feature that exposes PerfMeter markers and optional overdraw instrumentation.
	/// </summary>
	[DisallowMultipleRendererFeature("SGG PerfMeter")]
	public sealed class PerfMeterRenderGraphFeature : ScriptableRendererFeature
	{
		public const string DefaultMarkerName = "SGG.PerfMeter.Overlay";
		public const string DefaultOverdrawMarkerName = "SGG.PerfMeter.Overdraw";

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

			_overlayMarkerPass.Setup(_settings.RenderPassEvent, GetSafeMarkerName(_settings.MarkerName));
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

			public bool Enabled => _enabled;

			public RenderPassEvent RenderPassEvent => _renderPassEvent;

			public string MarkerName => _markerName;
		}

		private sealed class OverlayMarkerPass : ScriptableRenderPass
		{
			private const string OverdrawShaderName = "Hidden/SGG/PerfMeter/OverdrawCounter";
			private const string OverdrawShaderResourcePath = "SGGPerfMeterOverdrawCounter";
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
			private ProfilingSampler _overdrawProfilingSampler = new ProfilingSampler(DefaultOverdrawMarkerName);
			private Material _overdrawMaterial;

			internal void Setup(RenderPassEvent passEvent, string markerName)
			{
				renderPassEvent = passEvent;

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

				using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<MarkerPassData>(_currentMarkerName, out MarkerPassData passData, profilingSampler))
				{
					builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
					builder.AllowPassCulling(false);
					builder.SetRenderFunc(static (MarkerPassData data, RasterGraphContext context) =>
					{
						// Intentionally empty: this pass preserves a dedicated profiling marker for overhead subtraction.
					});
				}

				if (!PerfMeterRuntime.IsOverdrawMeasurementActive)
				{
					return;
				}

				RecordOverdrawPass(renderGraph, frameData, resourceData);
			}

			internal void Dispose()
			{
				CoreUtils.Destroy(_overdrawMaterial);
				_overdrawMaterial = null;
			}

			private void RecordOverdrawPass(RenderGraph renderGraph, ContextContainer frameData, UniversalResourceData resourceData)
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

				RendererListHandle rendererListHandle = CreateOverdrawRendererList(renderGraph, frameData, overdrawMaterial);
				if (!rendererListHandle.IsValid())
				{
					PerfMeterRuntime.FailOverdrawMeasurement("PerfMeter overdraw renderer list could not be created.");
					return;
				}

				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				int screenPixelCount = GetScreenPixelCount(cameraData);
				if (!PerfMeterRuntime.TryBeginOverdrawRenderGraphFrame(Time.frameCount, screenPixelCount, out GraphicsBuffer counterBuffer))
				{
					return;
				}

				BufferHandle counterBufferHandle = renderGraph.ImportBuffer(counterBuffer);

				using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<OverdrawPassData>(DefaultOverdrawMarkerName, out OverdrawPassData passData, _overdrawProfilingSampler))
				{
					passData.RendererListHandle = rendererListHandle;
					passData.CounterBufferHandle = counterBufferHandle;

					builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
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
					builder.UseBuffer(passData.CounterBufferHandle, AccessFlags.Read);
					builder.AllowPassCulling(false);
					builder.SetRenderFunc(static (OverdrawReadbackPassData data, UnsafeGraphContext context) =>
					{
						context.cmd.RequestAsyncReadback(data.CounterBufferHandle, PerfMeterRuntime.CompleteOverdrawCounterReadback);
					});
				}
			}

			private RendererListHandle CreateOverdrawRendererList(RenderGraph renderGraph, ContextContainer frameData, Material overdrawMaterial)
			{
				UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
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

		private sealed class OverdrawReadbackPassData
		{
			internal BufferHandle CounterBufferHandle;
		}
	}
}
