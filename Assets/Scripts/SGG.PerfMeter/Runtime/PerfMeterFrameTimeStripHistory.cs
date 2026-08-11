using System;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterFrameTimeStripHistory
	{
		private const int MaxCapacity = 600;

		private int[] _frames;
		private double[] _values;
		private bool[] _valid;
		private int _capacity;
		private int _index;
		private int _count;
		private int _lastFrame = -1;

		internal PerfMeterFrameTimeStripHistory(int capacity)
		{
			SetCapacity(capacity);
		}

		internal int Capacity => _capacity;
		internal int Count => _count;
		internal int LastFrame => _lastFrame;

		internal void SetCapacity(int capacity)
		{
			int normalized = Math.Max(1, Math.Min(MaxCapacity, capacity));
			if (_capacity == normalized && _values != null)
			{
				return;
			}

			int[] frames = new int[normalized];
			double[] values = new double[normalized];
			bool[] valid = new bool[normalized];
			int retainedCount = Math.Min(_count, normalized);
			int firstRetainedSample = _count - retainedCount;
			for (int sample = 0; sample < retainedCount; sample++)
			{
				int oldIndex = BufferIndex(firstRetainedSample + sample);
				frames[sample] = _frames[oldIndex];
				values[sample] = _values[oldIndex];
				valid[sample] = _valid[oldIndex];
			}

			_frames = frames;
			_values = values;
			_valid = valid;
			_capacity = normalized;
			_count = retainedCount;
			_index = retainedCount % normalized;
		}

		internal bool AddSample(int frame, double frameTimeMs, bool valid)
		{
			if (frame < 0 || frame <= _lastFrame)
			{
				return false;
			}

			bool finitePositive = frameTimeMs > 0d && !double.IsNaN(frameTimeMs) && !double.IsInfinity(frameTimeMs);
			_frames[_index] = frame;
			_values[_index] = finitePositive ? frameTimeMs : 0d;
			_valid[_index] = valid && finitePositive;
			_index = (_index + 1) % _capacity;
			_count = Math.Min(_count + 1, _capacity);
			_lastFrame = frame;
			return true;
		}

		internal bool TryGetSample(int sample, out int frame, out double frameTimeMs, out bool valid)
		{
			frame = -1;
			frameTimeMs = 0d;
			valid = false;
			if (sample < 0 || sample >= _count)
			{
				return false;
			}

			int index = BufferIndex(sample);
			frame = _frames[index];
			frameTimeMs = _values[index];
			valid = _valid[index];
			return true;
		}

		internal bool TryGetEnvelope(int column, int columnCount, out double min, out double max)
		{
			min = double.MaxValue;
			max = double.MinValue;
			if (_count == 0 || columnCount <= 0 || column < 0 || column >= columnCount || columnCount > _count)
			{
				return false;
			}

			int firstSample = column * _count / columnCount;
			int endSample = (column + 1) * _count / columnCount;
			bool hasValidSample = false;
			for (int sample = firstSample; sample < endSample; sample++)
			{
				int index = BufferIndex(sample);
				if (!_valid[index])
				{
					continue;
				}

				double value = _values[index];
				min = Math.Min(min, value);
				max = Math.Max(max, value);
				hasValidSample = true;
			}

			if (!hasValidSample)
			{
				min = 0d;
				max = 0d;
			}

			return hasValidSample;
		}

		internal bool TryGetLatest(out double frameTimeMs, out bool valid)
		{
			frameTimeMs = 0d;
			valid = false;
			if (_count == 0)
			{
				return false;
			}

			int index = BufferIndex(_count - 1);
			frameTimeMs = _values[index];
			valid = _valid[index];
			return true;
		}

		internal double GetPeak()
		{
			double peak = 0d;
			for (int sample = 0; sample < _count; sample++)
			{
				int index = BufferIndex(sample);
				if (_valid[index])
				{
					peak = Math.Max(peak, _values[index]);
				}
			}

			return peak;
		}

		private int BufferIndex(int sample)
		{
			return _capacity > 0 ? (_index - _count + sample + _capacity) % _capacity : 0;
		}
	}
}
