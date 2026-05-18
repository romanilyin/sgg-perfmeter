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
		private int _measurementId;
		private int _pendingMeasurementId = -1;
		private int _pendingScreenPixelCount;
		private double _totalFragmentCount;
		private double _totalScreenPixelCount;
		private double _ratio;
		private string _lastError = string.Empty;
		private bool _readbackPending;
		private PerfMeterOverdrawMeasurementState _state = PerfMeterOverdrawMeasurementState.Off;

		internal PerfMeterOverdrawMeasurementState State => _state;
		internal int CurrentMeasurementId => _measurementId;
		internal int RequestedFrameCount => _requestedFrameCount;
		internal int RecordedFrameCount => _recordedFrameCount;
		internal double Ratio => _ratio;
		internal float Progress => _requestedFrameCount > 0 ? (float)_recordedFrameCount / _requestedFrameCount : 0f;
		internal bool IsMeasuring => _state == PerfMeterOverdrawMeasurementState.Measuring;
		internal string Warning => GetWarning();

		internal void RequestMeasurement(int frameCount)
		{
			RequestMeasurement(frameCount, GetUnsupportedReason());
		}

		internal void RequestMeasurement(int frameCount, string unsupportedReason)
		{
			if (!string.IsNullOrEmpty(unsupportedReason))
			{
				SetUnsupported(unsupportedReason);
				return;
			}

			BeginMeasurement(frameCount);
		}

		internal void MarkUnsupported(string reason)
		{
			SetUnsupported(reason);
		}

		private void BeginMeasurement(int frameCount)
		{
			ReleaseCounterBuffer();
			_measurementId++;
			_requestedFrameCount = frameCount > 0 ? frameCount : DefaultFrameCount;
			_recordedFrameCount = 0;
			_lastScheduledUnityFrame = -1;
			_pendingMeasurementId = -1;
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
			_pendingMeasurementId = -1;
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
			_pendingMeasurementId = -1;
			_pendingScreenPixelCount = 0;
			_totalFragmentCount = 0d;
			_totalScreenPixelCount = 0d;
			_ratio = 0d;
			_lastError = string.Empty;
			_readbackPending = false;
			_state = PerfMeterOverdrawMeasurementState.Off;
		}

		internal bool TryBeginRenderGraphFrame(int unityFrame, int screenPixelCount, out GraphicsBuffer counterBuffer, out int measurementId)
		{
			counterBuffer = null;
			measurementId = -1;

			if (!IsMeasuring || _readbackPending || unityFrame == _lastScheduledUnityFrame)
			{
				return false;
			}

			EnsureCounterBuffer();
			_zeroCounterData[0] = 0u;
			_counterBuffer.SetData(_zeroCounterData);

			_pendingScreenPixelCount = Mathf.Max(1, screenPixelCount);
			_lastScheduledUnityFrame = unityFrame;
			_pendingMeasurementId = _measurementId;
			_readbackPending = true;
			counterBuffer = _counterBuffer;
			measurementId = _pendingMeasurementId;
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
			_pendingMeasurementId = -1;
			_readbackPending = false;
			ReleaseCounterBuffer();
		}

		internal void CompleteCounterReadback(int measurementId, AsyncGPUReadbackRequest request)
		{
			if (measurementId != _pendingMeasurementId)
			{
				return;
			}

			_readbackPending = false;
			_pendingMeasurementId = -1;

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

		private void SetUnsupported(string reason)
		{
			ReleaseCounterBuffer();
			_requestedFrameCount = 0;
			_recordedFrameCount = 0;
			_lastScheduledUnityFrame = -1;
			_pendingMeasurementId = -1;
			_pendingScreenPixelCount = 0;
			_totalFragmentCount = 0d;
			_totalScreenPixelCount = 0d;
			_ratio = 0d;
			_lastError = string.IsNullOrEmpty(reason) ? "Overdraw measurement is unsupported on this platform." : reason;
			_readbackPending = false;
			_state = PerfMeterOverdrawMeasurementState.Unsupported;
		}

		private static string GetUnsupportedReason()
		{
			if (!SystemInfo.supportsAsyncGPUReadback)
			{
				return "Overdraw measurement is unsupported: AsyncGPUReadback is not available on this platform.";
			}

			if (!SystemInfo.supportsComputeShaders)
			{
				return "Overdraw measurement is unsupported: compute shaders are not available on this platform.";
			}

			GraphicsDeviceType graphicsDeviceType = SystemInfo.graphicsDeviceType;
			if (graphicsDeviceType == GraphicsDeviceType.OpenGLES3 || graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
			{
				return "Overdraw measurement is unsupported on " + graphicsDeviceType + ": fragment UAV/storage buffer instrumentation requires a modern graphics backend.";
			}

			return string.Empty;
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
