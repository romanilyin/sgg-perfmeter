using UnityEngine;
using UnityEngine.SceneManagement;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterSessionRecorder
	{
		private PerfMeterSessionSampleSnapshot[] _samples = System.Array.Empty<PerfMeterSessionSampleSnapshot>();
		private PerfMeterSessionOptions _options = PerfMeterSessionOptions.Default;
		private PerfMeterSessionState _state = PerfMeterSessionState.Idle;
		private PerfMeterDeviceSnapshot _device;
		private PerfMeterCameraSnapshot _camera;
		private PerfMeterSettingsSnapshot _settings;
		private PerfMeterSessionSummarySnapshot _summary = PerfMeterSessionSummarySnapshot.Empty;
		private string _startSceneName = string.Empty;
		private string _lastSceneName = string.Empty;
		private double _startTimeSeconds;
		private double _stopTimeSeconds;
		private double _nextSampleTimeSeconds;
		private double _frameTimeTotalMs;
		private double _minFrameTimeMs;
		private double _maxFrameTimeMs;
		private double _fpsTotal;
		private double _minFps;
		private double _maxFps;
		private int _sampleCount;
		private int _droppedSampleCount;
		private int _startFrame = -1;
		private int _firstFrame = -1;
		private int _lastFrame = -1;
		private int _gpuBoundSampleCount;
		private int _cpuMainThreadBoundSampleCount;
		private int _cpuRenderThreadBoundSampleCount;
		private int _presentLimitedSampleCount;
		private int _frameSpikeBaseline;
		private int _severeFrameSpikeBaseline;
		private int _latestFrameSpikeCount;
		private int _latestSevereFrameSpikeCount;

		internal PerfMeterSessionState State => _state;
		internal bool IsRecording => _state == PerfMeterSessionState.Recording;
		internal int SampleCount => _sampleCount;
		internal int DroppedSampleCount => _droppedSampleCount;

		internal void Start(PerfMeterSessionOptions options, PerfMeterDeviceSnapshot device, PerfMeterCameraSnapshot camera, PerfMeterSettingsSnapshot settings, int frame, double timeSeconds, PerfMeterMetricsSnapshot latestMetrics)
		{
			_options = options.MaxSamples > 0 ? options : PerfMeterSessionOptions.FromSettings(settings);
			_samples = new PerfMeterSessionSampleSnapshot[_options.MaxSamples];
			_state = PerfMeterSessionState.Recording;
			_device = device;
			_camera = camera;
			_settings = settings;
			_startSceneName = SceneManager.GetActiveScene().name;
			_lastSceneName = _startSceneName;
			_startTimeSeconds = timeSeconds;
			_startFrame = frame;
			_stopTimeSeconds = 0d;
			_nextSampleTimeSeconds = timeSeconds;
			_frameTimeTotalMs = 0d;
			_minFrameTimeMs = double.MaxValue;
			_maxFrameTimeMs = 0d;
			_fpsTotal = 0d;
			_minFps = double.MaxValue;
			_maxFps = 0d;
			_sampleCount = 0;
			_droppedSampleCount = 0;
			_firstFrame = -1;
			_lastFrame = -1;
			_gpuBoundSampleCount = 0;
			_cpuMainThreadBoundSampleCount = 0;
			_cpuRenderThreadBoundSampleCount = 0;
			_presentLimitedSampleCount = 0;
			_frameSpikeBaseline = latestMetrics.FrameSpikeCount;
			_severeFrameSpikeBaseline = latestMetrics.SevereFrameSpikeCount;
			_latestFrameSpikeCount = 0;
			_latestSevereFrameSpikeCount = 0;
			_summary = CreateSummary(timeSeconds, string.Empty);
		}

		internal void Stop(double timeSeconds)
		{
			if (_state != PerfMeterSessionState.Recording)
			{
				return;
			}

			_state = PerfMeterSessionState.Stopped;
			_stopTimeSeconds = timeSeconds;
			_summary = CreateSummary(timeSeconds, string.Empty);
		}

		internal void Reset()
		{
			_state = PerfMeterSessionState.Idle;
			_samples = System.Array.Empty<PerfMeterSessionSampleSnapshot>();
			_sampleCount = 0;
			_droppedSampleCount = 0;
			_summary = PerfMeterSessionSummarySnapshot.Empty;
		}

		internal void Update(PerfMeterMetricsSnapshot metrics, int frame, double timeSeconds)
		{
			if (_state != PerfMeterSessionState.Recording)
			{
				return;
			}

			if (frame < 0 || frame < _lastFrame)
			{
				return;
			}

			if (frame - _startFrame < _options.WarmupFrames)
			{
				return;
			}

			if (timeSeconds < _nextSampleTimeSeconds)
			{
				return;
			}

			_nextSampleTimeSeconds = timeSeconds + _options.SampleIntervalSeconds;
			_lastSceneName = SceneManager.GetActiveScene().name;

			if (_sampleCount >= _samples.Length)
			{
				_droppedSampleCount++;
				_summary = CreateSummary(timeSeconds, "Session sample buffer is full; additional samples are dropped.");
				return;
			}

			PerfMeterSessionSampleSnapshot sample = new PerfMeterSessionSampleSnapshot(frame, timeSeconds, _lastSceneName, metrics);
			_samples[_sampleCount] = sample;
			_sampleCount++;

			if (_firstFrame < 0)
			{
				_firstFrame = frame;
			}

			_lastFrame = frame;
			AddMetrics(metrics);
			_summary = CreateSummary(timeSeconds, string.Empty);
		}

		internal PerfMeterSessionSummarySnapshot GetSummary()
		{
			return _summary;
		}

		private void AddMetrics(PerfMeterMetricsSnapshot metrics)
		{
			double frameTimeMs = metrics.CpuFrameTimeMs > 0d ? metrics.CpuFrameTimeMs : metrics.FrameBudgetMs;
			double fps = frameTimeMs > 0d ? 1000d / frameTimeMs : 0d;

			_frameTimeTotalMs += frameTimeMs;
			_minFrameTimeMs = System.Math.Min(_minFrameTimeMs, frameTimeMs);
			_maxFrameTimeMs = System.Math.Max(_maxFrameTimeMs, frameTimeMs);
			_fpsTotal += fps;
			_minFps = System.Math.Min(_minFps, fps);
			_maxFps = System.Math.Max(_maxFps, fps);

			switch (metrics.Bottleneck)
			{
				case PerfMeterBottleneck.GpuBound:
					_gpuBoundSampleCount++;
					break;
				case PerfMeterBottleneck.CpuMainThreadBound:
					_cpuMainThreadBoundSampleCount++;
					break;
				case PerfMeterBottleneck.CpuRenderThreadBound:
					_cpuRenderThreadBoundSampleCount++;
					break;
				case PerfMeterBottleneck.PresentLimited:
					_presentLimitedSampleCount++;
					break;
			}

			_latestFrameSpikeCount = Mathf.Max(0, metrics.FrameSpikeCount - _frameSpikeBaseline);
			_latestSevereFrameSpikeCount = Mathf.Max(0, metrics.SevereFrameSpikeCount - _severeFrameSpikeBaseline);
		}

		private PerfMeterSessionSummarySnapshot CreateSummary(double currentTimeSeconds, string warning)
		{
			string summaryWarning = !string.IsNullOrEmpty(warning) || _droppedSampleCount == 0
				? warning
				: "Session sample buffer is full; additional samples are dropped.";
			double duration = _sampleCount > 0 ? System.Math.Max(0d, (_state == PerfMeterSessionState.Stopped ? _stopTimeSeconds : currentTimeSeconds) - _startTimeSeconds) : 0d;
			double averageFrameTimeMs = _sampleCount > 0 ? _frameTimeTotalMs / _sampleCount : 0d;
			double minFrameTimeMs = _sampleCount > 0 ? _minFrameTimeMs : 0d;
			double maxFrameTimeMs = _sampleCount > 0 ? _maxFrameTimeMs : 0d;
			double averageFps = _sampleCount > 0 ? _fpsTotal / _sampleCount : 0d;
			double minFps = _sampleCount > 0 ? _minFps : 0d;
			double maxFps = _sampleCount > 0 ? _maxFps : 0d;

			return new PerfMeterSessionSummarySnapshot(
				_state,
				_options,
				_sampleCount,
				_droppedSampleCount,
				_firstFrame,
				_lastFrame,
				_startTimeSeconds,
				_stopTimeSeconds,
				duration,
				averageFrameTimeMs,
				minFrameTimeMs,
				maxFrameTimeMs,
				averageFps,
				minFps,
				maxFps,
				_gpuBoundSampleCount,
				_cpuMainThreadBoundSampleCount,
				_cpuRenderThreadBoundSampleCount,
				_presentLimitedSampleCount,
				_latestFrameSpikeCount,
				_latestSevereFrameSpikeCount,
				summaryWarning,
				_device,
				_camera,
				_settings,
				_startSceneName,
				_lastSceneName);
		}

	}
}
