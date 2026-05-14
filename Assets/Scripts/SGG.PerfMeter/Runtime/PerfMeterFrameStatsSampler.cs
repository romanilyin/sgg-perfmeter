using System;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal readonly struct PerfMeterFrameStatsSnapshot
	{
		internal PerfMeterFrameStatsSnapshot(
			int sampleCount,
			int gpuValidSampleCount,
			double averageFps,
			double onePercentLowFps,
			double pointOnePercentLowFps,
			int frameSpikeCount,
			int severeFrameSpikeCount)
		{
			SampleCount = sampleCount;
			GpuValidSampleCount = gpuValidSampleCount;
			AverageFps = averageFps;
			OnePercentLowFps = onePercentLowFps;
			PointOnePercentLowFps = pointOnePercentLowFps;
			FrameSpikeCount = frameSpikeCount;
			SevereFrameSpikeCount = severeFrameSpikeCount;
		}

		internal int SampleCount { get; }
		internal int GpuValidSampleCount { get; }
		internal double AverageFps { get; }
		internal double OnePercentLowFps { get; }
		internal double PointOnePercentLowFps { get; }
		internal int FrameSpikeCount { get; }
		internal int SevereFrameSpikeCount { get; }
	}

	internal sealed class PerfMeterFrameStatsSampler
	{
		private const int Capacity = 600;
		private const double SpikeFrameTimeMs = 33.333d;
		private const double SevereSpikeFrameTimeMs = 50d;

		private readonly double[] _cpuFrameMs = new double[Capacity];
		private readonly bool[] _gpuValid = new bool[Capacity];
		private readonly double[] _scratch = new double[Capacity];
		private int _index;
		private int _count;

		internal void Reset()
		{
			_index = 0;
			_count = 0;
		}

		internal void AddSample(double cpuFrameTimeMs, bool gpuFrameTimeAvailable)
		{
			if (cpuFrameTimeMs <= 0d || double.IsNaN(cpuFrameTimeMs) || double.IsInfinity(cpuFrameTimeMs))
			{
				return;
			}

			_cpuFrameMs[_index] = cpuFrameTimeMs;
			_gpuValid[_index] = gpuFrameTimeAvailable;
			_index = (_index + 1) % Capacity;
			_count = Mathf.Min(_count + 1, Capacity);
		}

		internal PerfMeterFrameStatsSnapshot GetSnapshot()
		{
			if (_count == 0)
			{
				return default;
			}

			double totalFrameMs = 0d;
			int gpuValidSampleCount = 0;
			int frameSpikeCount = 0;
			int severeFrameSpikeCount = 0;

			for (int i = 0; i < _count; i++)
			{
				double frameMs = _cpuFrameMs[i];
				_scratch[i] = frameMs;
				totalFrameMs += frameMs;

				if (_gpuValid[i])
				{
					gpuValidSampleCount++;
				}

				if (frameMs >= SpikeFrameTimeMs)
				{
					frameSpikeCount++;
				}

				if (frameMs >= SevereSpikeFrameTimeMs)
				{
					severeFrameSpikeCount++;
				}
			}

			Array.Sort(_scratch, 0, _count);

			return new PerfMeterFrameStatsSnapshot(
				_count,
				gpuValidSampleCount,
				FpsFromFrameMs(totalFrameMs / _count),
				CalculateLowFps(0.01d),
				CalculateLowFps(0.001d),
				frameSpikeCount,
				severeFrameSpikeCount);
		}

		private double CalculateLowFps(double percentile)
		{
			int slowFrameCount = Mathf.Clamp(Mathf.CeilToInt((float)(_count * percentile)), 1, _count);
			double slowFrameTotalMs = 0d;
			for (int i = _count - slowFrameCount; i < _count; i++)
			{
				slowFrameTotalMs += _scratch[i];
			}

			return FpsFromFrameMs(slowFrameTotalMs / slowFrameCount);
		}

		private static double FpsFromFrameMs(double frameMs)
		{
			return frameMs > 0d ? 1000d / frameMs : 0d;
		}
	}
}
