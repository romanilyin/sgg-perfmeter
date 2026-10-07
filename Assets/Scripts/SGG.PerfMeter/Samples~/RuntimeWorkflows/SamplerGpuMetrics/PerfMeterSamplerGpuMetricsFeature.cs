using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SGG.PerfMeter.Samples
{
	/// <summary>Opt-in offscreen workload illustrating exact compute/raster sampler providers.</summary>
	public sealed class PerfMeterSamplerGpuMetricsFeature : ScriptableRendererFeature
	{
		[SerializeField] private ComputeShader computeShader;
		private SamplePass _pass;

		public override void Create()
		{
			_pass?.Dispose();
#if UNITY_6000_4_OR_NEWER
			string featureIdentity = EntityId.ToULong(GetEntityId()).ToString();
#else
			string featureIdentity = GetInstanceID().ToString();
#endif
			_pass = new SamplePass(computeShader, featureIdentity);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			// Ignored cameras do not publish a negative report that could overwrite another camera's scheduling.
			if (_pass != null && renderingData.cameraData.cameraType == CameraType.Game)
				renderer.EnqueuePass(_pass);
		}

		protected override void Dispose(bool disposing)
		{
			_pass?.Dispose();
			_pass = null;
		}

		private sealed class SamplePass : ScriptableRenderPass, IDisposable
		{
			private const int ElementCount = 65536;
			private static readonly int OutputId = Shader.PropertyToID("_Output");
			private readonly ComputeShader _shader;
			private readonly int _kernel;
			private readonly ProfilingSampler _computeSampler;
			private readonly ProfilingSampler _rasterSampler;
			private readonly PerfMeterProfilingSamplerMetricProvider _computeMetric;
			private readonly PerfMeterProfilingSamplerMetricProvider _rasterMetric;
			private bool _disposed;

			internal SamplePass(ComputeShader shader, string featureIdentity)
			{
				renderPassEvent = RenderPassEvent.AfterRendering;
				_shader = shader;
				_kernel = shader != null && SystemInfo.supportsComputeShaders ? shader.FindKernel("CSMain") : -1;
				// Private samplers and unique native marker names: this sample is their only recording owner.
				_computeSampler = new ProfilingSampler("PerfMeter sample compute " + featureIdentity);
				_rasterSampler = new ProfilingSampler("PerfMeter sample raster " + featureIdentity);
				_computeMetric = new PerfMeterProfilingSamplerMetricProvider("sample.exact.compute", "Sample compute", _computeSampler);
				_rasterMetric = new PerfMeterProfilingSamplerMetricProvider("sample.exact.raster", "Sample raster", _rasterSampler);
				_computeSampler.enableRecording = true;
				_rasterSampler.enableRecording = true;
				_computeMetric.SetEnabled(true);
				_rasterMetric.SetEnabled(true);
				PerformanceMeter.RegisterCustomMetricProvider(_computeMetric);
				PerformanceMeter.RegisterCustomMetricProvider(_rasterMetric);
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				if (_disposed)
					return;
				if (_kernel >= 0)
				{
					BufferHandle output = renderGraph.CreateBuffer(new BufferDesc(ElementCount, sizeof(float))
					{
						name = "PerfMeter sample compute output"
					});
					using (IComputeRenderGraphBuilder builder = renderGraph.AddComputePass<ComputePassData>(
						"PerfMeter sampler compute", out ComputePassData data, _computeSampler))
					{
						data.Shader = _shader;
						data.Kernel = _kernel;
						data.Output = output;
						data.Metric = _computeMetric;
						builder.UseBuffer(output, AccessFlags.Write);
						// Intentional synthetic workload with no consumer; do not copy this culling override into real passes.
						builder.AllowPassCulling(false);
						builder.SetRenderFunc(static (ComputePassData passData, ComputeGraphContext context) =>
						{
							context.cmd.SetComputeBufferParam(passData.Shader, passData.Kernel, OutputId, passData.Output);
							context.cmd.DispatchCompute(passData.Shader, passData.Kernel, ElementCount / 64, 1, 1);
							passData.Metric.ReportPassExecution();
						});
					}
					_computeMetric.ReportProducerState(PerfMeterGpuPassAvailability.Scheduled);
				}
				else
				{
					_computeMetric.ReportProducerState(PerfMeterGpuPassAvailability.Unavailable);
				}

				TextureHandle color = renderGraph.CreateTexture(new TextureDesc(256, 256)
				{
					name = "PerfMeter sample raster output",
					colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
					clearBuffer = false
				});
				using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<RasterPassData>(
					"PerfMeter sampler raster", out RasterPassData data, _rasterSampler))
				{
					data.Metric = _rasterMetric;
					builder.SetRenderAttachment(color, 0, AccessFlags.Write);
					builder.AllowPassCulling(false);
					builder.SetRenderFunc(static (RasterPassData passData, RasterGraphContext context) =>
					{
						context.cmd.ClearRenderTarget(false, true, Color.black);
						passData.Metric.ReportPassExecution();
					});
				}
				_rasterMetric.ReportProducerState(PerfMeterGpuPassAvailability.Scheduled);
			}

			public void Dispose()
			{
				if (_disposed)
					return;
				_disposed = true;
				PerformanceMeter.UnregisterCustomMetricProvider(_computeMetric);
				PerformanceMeter.UnregisterCustomMetricProvider(_rasterMetric);
				_computeMetric.SetEnabled(false);
				_rasterMetric.SetEnabled(false);
				_computeMetric.Dispose();
				_rasterMetric.Dispose();
				_computeSampler.enableRecording = false;
				_rasterSampler.enableRecording = false;
				// Public IDisposable is available in SRP 17.6; the same sample remains compilable on 17.4.
				(_computeSampler as IDisposable)?.Dispose();
				(_rasterSampler as IDisposable)?.Dispose();
			}

			private sealed class ComputePassData
			{
				internal ComputeShader Shader;
				internal int Kernel;
				internal BufferHandle Output;
				internal PerfMeterProfilingSamplerMetricProvider Metric;
			}

			private sealed class RasterPassData
			{
				internal PerfMeterProfilingSamplerMetricProvider Metric;
			}
		}
	}
}
