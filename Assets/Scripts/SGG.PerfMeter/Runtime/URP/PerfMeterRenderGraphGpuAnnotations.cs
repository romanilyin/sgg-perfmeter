using System;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	/// <summary>Safe RenderGraph command-buffer adapters for PerfMeter GPU annotation scopes.</summary>
	public static class PerfMeterRenderGraphGpuAnnotations
	{
		public static PerfMeterGpuAnnotationScope BeginScope(
			RasterCommandBuffer commandBuffer,
			PerfMeterGpuAnnotationBatch annotations)
		{
			if (commandBuffer == null)
			{
				throw new ArgumentNullException(nameof(commandBuffer));
			}
			return PerfMeterGpuAnnotations.TryGetReadyProvider(out IPerfMeterGpuAnnotationProvider provider)
				? PerfMeterGpuAnnotations.BeginScope(new RasterSink(commandBuffer), annotations, provider)
				: null;
		}

		public static PerfMeterGpuAnnotationScope BeginScope(
			ComputeCommandBuffer commandBuffer,
			PerfMeterGpuAnnotationBatch annotations)
		{
			if (commandBuffer == null)
			{
				throw new ArgumentNullException(nameof(commandBuffer));
			}
			return PerfMeterGpuAnnotations.TryGetReadyProvider(out IPerfMeterGpuAnnotationProvider provider)
				? PerfMeterGpuAnnotations.BeginScope(new ComputeSink(commandBuffer), annotations, provider)
				: null;
		}

		public static PerfMeterGpuAnnotationScope BeginScope(
			UnsafeCommandBuffer commandBuffer,
			PerfMeterGpuAnnotationBatch annotations)
		{
			if (commandBuffer == null)
			{
				throw new ArgumentNullException(nameof(commandBuffer));
			}
			return PerfMeterGpuAnnotations.TryGetReadyProvider(out IPerfMeterGpuAnnotationProvider provider)
				? PerfMeterGpuAnnotations.BeginScope(new UnsafeSink(commandBuffer), annotations, provider)
				: null;
		}

		private sealed class RasterSink : IPerfMeterGpuAnnotationCommandSink
		{
			private readonly RasterCommandBuffer _commandBuffer;

			internal RasterSink(RasterCommandBuffer commandBuffer)
			{
				_commandBuffer = commandBuffer;
			}

			public void Issue(IntPtr callback, int eventId, IntPtr eventData)
			{
				_commandBuffer.IssuePluginEventAndData(callback, eventId, eventData);
			}
		}

		private sealed class ComputeSink : IPerfMeterGpuAnnotationCommandSink
		{
			private readonly ComputeCommandBuffer _commandBuffer;

			internal ComputeSink(ComputeCommandBuffer commandBuffer)
			{
				_commandBuffer = commandBuffer;
			}

			public void Issue(IntPtr callback, int eventId, IntPtr eventData)
			{
				_commandBuffer.IssuePluginEventAndData(callback, eventId, eventData);
			}
		}

		private sealed class UnsafeSink : IPerfMeterGpuAnnotationCommandSink
		{
			private readonly UnsafeCommandBuffer _commandBuffer;

			internal UnsafeSink(UnsafeCommandBuffer commandBuffer)
			{
				_commandBuffer = commandBuffer;
			}

			public void Issue(IntPtr callback, int eventId, IntPtr eventData)
			{
				_commandBuffer.IssuePluginEventAndData(callback, eventId, eventData);
			}
		}
	}
}
