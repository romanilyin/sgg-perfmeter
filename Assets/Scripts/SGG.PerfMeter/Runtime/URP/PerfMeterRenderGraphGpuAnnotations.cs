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

	/// <summary>Reusable non-nested Render Graph annotation scope with zero managed allocations after warm-up.</summary>
	public sealed class PerfMeterRenderGraphGpuAnnotationWorkspace : IDisposable
	{
		private readonly PerfMeterGpuAnnotationWorkspace _workspace = new PerfMeterGpuAnnotationWorkspace();
		private readonly RasterSink _rasterSink = new RasterSink();
		private readonly ComputeSink _computeSink = new ComputeSink();
		private readonly UnsafeSink _unsafeSink = new UnsafeSink();

		public bool IsActive => _workspace.IsActive;

		public PerfMeterRenderGraphGpuAnnotationWorkspace BeginScope(RasterCommandBuffer commandBuffer, PerfMeterGpuAnnotationBatch annotations)
		{
			if (commandBuffer == null)
				throw new ArgumentNullException(nameof(commandBuffer));
			if (IsActive)
				return null;
			_rasterSink.CommandBuffer = commandBuffer;
			try
			{
				if (PerfMeterGpuAnnotations.TryGetReadyProvider(out IPerfMeterGpuAnnotationProvider provider) &&
					_workspace.TryBegin(_rasterSink, annotations, provider))
					return this;
				_rasterSink.CommandBuffer = null;
				return null;
			}
			catch
			{
				_rasterSink.CommandBuffer = null;
				throw;
			}
		}

		public PerfMeterRenderGraphGpuAnnotationWorkspace BeginScope(ComputeCommandBuffer commandBuffer, PerfMeterGpuAnnotationBatch annotations)
		{
			if (commandBuffer == null)
				throw new ArgumentNullException(nameof(commandBuffer));
			if (IsActive)
				return null;
			_computeSink.CommandBuffer = commandBuffer;
			try
			{
				if (PerfMeterGpuAnnotations.TryGetReadyProvider(out IPerfMeterGpuAnnotationProvider provider) &&
					_workspace.TryBegin(_computeSink, annotations, provider))
					return this;
				_computeSink.CommandBuffer = null;
				return null;
			}
			catch
			{
				_computeSink.CommandBuffer = null;
				throw;
			}
		}

		public PerfMeterRenderGraphGpuAnnotationWorkspace BeginScope(UnsafeCommandBuffer commandBuffer, PerfMeterGpuAnnotationBatch annotations)
		{
			if (commandBuffer == null)
				throw new ArgumentNullException(nameof(commandBuffer));
			if (IsActive)
				return null;
			_unsafeSink.CommandBuffer = commandBuffer;
			try
			{
				if (PerfMeterGpuAnnotations.TryGetReadyProvider(out IPerfMeterGpuAnnotationProvider provider) &&
					_workspace.TryBegin(_unsafeSink, annotations, provider))
					return this;
				_unsafeSink.CommandBuffer = null;
				return null;
			}
			catch
			{
				_unsafeSink.CommandBuffer = null;
				throw;
			}
		}

		public void Dispose()
		{
			try
			{
				_workspace.Dispose();
			}
			finally
			{
				_rasterSink.CommandBuffer = null;
				_computeSink.CommandBuffer = null;
				_unsafeSink.CommandBuffer = null;
			}
		}

		private sealed class RasterSink : IPerfMeterGpuAnnotationCommandSink
		{
			internal RasterCommandBuffer CommandBuffer;
			public void Issue(IntPtr callback, int eventId, IntPtr eventData) => CommandBuffer.IssuePluginEventAndData(callback, eventId, eventData);
		}

		private sealed class ComputeSink : IPerfMeterGpuAnnotationCommandSink
		{
			internal ComputeCommandBuffer CommandBuffer;
			public void Issue(IntPtr callback, int eventId, IntPtr eventData) => CommandBuffer.IssuePluginEventAndData(callback, eventId, eventData);
		}

		private sealed class UnsafeSink : IPerfMeterGpuAnnotationCommandSink
		{
			internal UnsafeCommandBuffer CommandBuffer;
			public void Issue(IntPtr callback, int eventId, IntPtr eventData) => CommandBuffer.IssuePluginEventAndData(callback, eventId, eventData);
		}
	}
}
