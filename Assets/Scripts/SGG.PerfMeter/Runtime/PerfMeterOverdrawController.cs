using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterOverdrawController
	{
		internal const int DefaultFrameCount = 60;
		internal const string WaitingForRenderGraphPassWarning = "Overdraw measurement is waiting for the PerfMeter URP Render Graph pass; verify the active renderer feature and hidden shader if progress stays at 0.";

		private readonly uint[] _zeroCounterData = new uint[1];
		private GraphicsBuffer _counterBuffer;
		private int _requestedFrameCount;
		private int _recordedFrameCount;
		private int _lastScheduledUnityFrame = -1;
		private int _pendingScreenPixelCount;
		private double _totalFragmentCount;
		private double _totalScreenPixelCount;
		private double _ratio;
		private string _lastError = string.Empty;
		private bool _readbackPending;
		private PerfMeterOverdrawMeasurementState _state = PerfMeterOverdrawMeasurementState.Off;

		internal PerfMeterOverdrawMeasurementState State => _state;
		internal int RequestedFrameCount => _requestedFrameCount;
		internal int RecordedFrameCount => _recordedFrameCount;
		internal double Ratio => _ratio;
		internal float Progress => _requestedFrameCount > 0 ? (float)_recordedFrameCount / _requestedFrameCount : 0f;
		internal bool IsMeasuring => _state == PerfMeterOverdrawMeasurementState.Measuring;
		internal string Warning => GetWarning();

		internal void RequestMeasurement(int frameCount)
		{
			_requestedFrameCount = frameCount > 0 ? frameCount : DefaultFrameCount;
			_recordedFrameCount = 0;
			_lastScheduledUnityFrame = -1;
			_pendingScreenPixelCount = 0;
			_totalFragmentCount = 0d;
			_totalScreenPixelCount = 0d;
			_ratio = 0d;
			_lastError = string.Empty;
			_readbackPending = false;
			_state = PerfMeterOverdrawMeasurementState.Measuring;
		}

		internal void CancelMeasurement()
		{
			ReleaseCounterBuffer();
			_requestedFrameCount = 0;
			_recordedFrameCount = 0;
			_lastScheduledUnityFrame = -1;
			_pendingScreenPixelCount = 0;
			_totalFragmentCount = 0d;
			_totalScreenPixelCount = 0d;
			_ratio = 0d;
			_lastError = string.Empty;
			_readbackPending = false;
			_state = PerfMeterOverdrawMeasurementState.Canceled;
		}

		internal void Reset()
		{
			ReleaseCounterBuffer();
			_requestedFrameCount = 0;
			_recordedFrameCount = 0;
			_lastScheduledUnityFrame = -1;
			_pendingScreenPixelCount = 0;
			_totalFragmentCount = 0d;
			_totalScreenPixelCount = 0d;
			_ratio = 0d;
			_lastError = string.Empty;
			_readbackPending = false;
			_state = PerfMeterOverdrawMeasurementState.Off;
		}

		internal bool TryBeginRenderGraphFrame(int unityFrame, int screenPixelCount, out GraphicsBuffer counterBuffer)
		{
			counterBuffer = null;

			if (!IsMeasuring || _readbackPending || unityFrame == _lastScheduledUnityFrame)
			{
				return false;
			}

			EnsureCounterBuffer();
			_zeroCounterData[0] = 0u;
			_counterBuffer.SetData(_zeroCounterData);

			_pendingScreenPixelCount = Mathf.Max(1, screenPixelCount);
			_lastScheduledUnityFrame = unityFrame;
			_readbackPending = true;
			counterBuffer = _counterBuffer;
			return true;
		}

		internal void FailMeasurement(string error)
		{
			if (!IsMeasuring)
			{
				return;
			}

			_lastError = string.IsNullOrEmpty(error) ? WaitingForRenderGraphPassWarning : error;
			_state = PerfMeterOverdrawMeasurementState.Error;
			_readbackPending = false;
			ReleaseCounterBuffer();
		}

		internal void CompleteCounterReadback(AsyncGPUReadbackRequest request)
		{
			_readbackPending = false;

			if (!IsMeasuring)
			{
				return;
			}

			if (request.hasError)
			{
				_lastError = "AsyncGPUReadback failed during overdraw measurement.";
				_state = PerfMeterOverdrawMeasurementState.Error;
				ReleaseCounterBuffer();
				return;
			}

			uint fragmentCount = request.GetData<uint>()[0];
			_totalFragmentCount += fragmentCount;
			_totalScreenPixelCount += Mathf.Max(1, _pendingScreenPixelCount);
			_ratio = _totalScreenPixelCount > 0d ? _totalFragmentCount / _totalScreenPixelCount : 0d;
			_recordedFrameCount++;

			if (_recordedFrameCount >= _requestedFrameCount)
			{
				_state = PerfMeterOverdrawMeasurementState.Completed;
				ReleaseCounterBuffer();
			}
		}

		private void EnsureCounterBuffer()
		{
			if (_counterBuffer != null)
			{
				return;
			}

			_counterBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, sizeof(uint));
			_counterBuffer.name = "SGG PerfMeter Overdraw Counter";
		}

		private void ReleaseCounterBuffer()
		{
			if (_counterBuffer == null)
			{
				return;
			}

			_counterBuffer.Release();
			_counterBuffer = null;
		}

		private string GetWarning()
		{
			if (!string.IsNullOrEmpty(_lastError))
			{
				return _lastError;
			}

			return IsMeasuring && _recordedFrameCount == 0 && !_readbackPending ? WaitingForRenderGraphPassWarning : string.Empty;
		}
	}
}
