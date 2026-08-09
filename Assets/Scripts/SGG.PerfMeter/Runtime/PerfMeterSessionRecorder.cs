using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterSessionRecorder
	{
		private PerfMeterSessionSampleSnapshot[] _samples = System.Array.Empty<PerfMeterSessionSampleSnapshot>();
		private readonly PerfMeterSessionTimelineStore _timeline = new PerfMeterSessionTimelineStore();
		private PerfMeterSessionOptions _options = PerfMeterSessionOptions.Default;
		private PerfMeterSessionState _state = PerfMeterSessionState.Idle;
		private PerfMeterDeviceSnapshot _device;
		private PerfMeterCameraSnapshot _camera;
		private PerfMeterSettingsSnapshot _configuredSettings;
		private PerfMeterSettingsSnapshot _effectiveSettings;
		private PerfMeterSessionSummarySnapshot _summary = PerfMeterSessionSummarySnapshot.Empty;
		private string _sessionId = string.Empty;
		private SessionStats _wholeRunStats;
		private SessionStats _currentSceneStats;
		private string _startSceneName = string.Empty;
		private string _lastSceneName = string.Empty;
		private double _startTimeSeconds;
		private double _stopTimeSeconds;
		private double _nextSampleTimeSeconds;
		private double _sceneIgnoreUntilTimeSeconds;
		private double _focusPausedStartTimeSeconds;
		private double _focusPausedDurationSeconds;
		private int _sampleCount;
		private int _droppedSampleCount;
		private int _startFrame = -1;
		private int _sceneIgnoreUntilFrame = -1;
		private int _focusLossCount;
		private int _pauseCount;
		private bool _applicationFocused = true;
		private bool _applicationPaused;
		private bool _focusPaused;

		internal PerfMeterSessionState State => _state;
		internal bool IsRecording => _state == PerfMeterSessionState.Recording;
		internal int SampleCount => _sampleCount;
		internal int DroppedSampleCount => _droppedSampleCount;

		internal void Start(PerfMeterSessionOptions options, PerfMeterDeviceSnapshot device, PerfMeterCameraSnapshot camera, PerfMeterSettingsSnapshot configuredSettings, int frame, double timeSeconds, PerfMeterMetricsSnapshot latestMetrics, bool applicationFocused = true, bool applicationPaused = false, PerfMeterSettingsSnapshot? effectiveSettings = null)
		{
			if (_state == PerfMeterSessionState.Recording)
			{
				PerfMeterProfilerInstrumentation.RecordSessionEnd(_sessionId);
			}

			_sessionId = Guid.NewGuid().ToString("N");
			PerfMeterProfilerInstrumentation.RecordSessionBegin(_sessionId);
			_options = options.MaxSamples > 0 ? options : PerfMeterSessionOptions.FromSettings(configuredSettings);
			_samples = new PerfMeterSessionSampleSnapshot[_options.MaxSamples];
			_timeline.Start(_options.MaxSamples);
			_state = PerfMeterSessionState.Recording;
			PerfMeterProfilerInstrumentation.RecordSessionState(_state);
			_device = device;
			_camera = camera;
			_configuredSettings = configuredSettings;
			_effectiveSettings = effectiveSettings ?? configuredSettings;
			_applicationFocused = applicationFocused;
			_applicationPaused = applicationPaused;
			ResetRecordingWindow(GetActiveSceneName(), frame, timeSeconds, latestMetrics);
			_summary = CreateSummary(timeSeconds, string.Empty);
		}

		internal void Stop(double timeSeconds)
		{
			if (_state != PerfMeterSessionState.Recording)
			{
				return;
			}

			_state = PerfMeterSessionState.Stopped;
			PerfMeterProfilerInstrumentation.RecordSessionState(_state);
			_stopTimeSeconds = timeSeconds;
			PerfMeterProfilerInstrumentation.RecordSessionEnd(_sessionId);
			_summary = CreateSummary(timeSeconds, string.Empty);
		}

		internal void Reset()
		{
			if (_state == PerfMeterSessionState.Recording)
			{
				PerfMeterProfilerInstrumentation.RecordSessionEnd(_sessionId);
			}

			_state = PerfMeterSessionState.Idle;
			PerfMeterProfilerInstrumentation.RecordSessionState(_state);
			_options = PerfMeterSessionOptions.Default;
			_samples = System.Array.Empty<PerfMeterSessionSampleSnapshot>();
			_timeline.Reset();
			_sampleCount = 0;
			_droppedSampleCount = 0;
			_startFrame = -1;
			_sceneIgnoreUntilFrame = -1;
			_sceneIgnoreUntilTimeSeconds = 0d;
			_startSceneName = string.Empty;
			_lastSceneName = string.Empty;
			_wholeRunStats = default;
			_currentSceneStats = default;
			_applicationFocused = true;
			_applicationPaused = false;
			_sessionId = string.Empty;
			ResetFocusStats(0d);
			_summary = PerfMeterSessionSummarySnapshot.Empty;
		}

		internal void ResetStats(int frame, double timeSeconds, PerfMeterMetricsSnapshot latestMetrics, bool applicationFocused = true, bool applicationPaused = false)
		{
			if (_state != PerfMeterSessionState.Recording)
			{
				Reset();
				return;
			}

			_applicationFocused = applicationFocused;
			_applicationPaused = applicationPaused;
			ResetRecordingWindow(GetActiveSceneName(), frame, timeSeconds, latestMetrics);
			_summary = CreateSummary(timeSeconds, string.Empty);
		}

		internal void SetApplicationFocusState(bool applicationFocused, bool applicationPaused, int frame, double timeSeconds)
		{
			if (_state != PerfMeterSessionState.Recording)
			{
				_applicationFocused = applicationFocused;
				_applicationPaused = applicationPaused;
				_focusPaused = !applicationFocused || applicationPaused;
				_focusPausedStartTimeSeconds = _focusPaused ? timeSeconds : 0d;
				return;
			}

			if (_applicationFocused && !applicationFocused)
			{
				_focusLossCount++;
			}

			if (!_applicationPaused && applicationPaused)
			{
				_pauseCount++;
			}

			_applicationFocused = applicationFocused;
			_applicationPaused = applicationPaused;
			UpdateFocusPausedWindow(timeSeconds);
			_summary = CreateSummary(timeSeconds, string.Empty);
		}

		internal void Update(PerfMeterMetricsSnapshot metrics, int frame, double timeSeconds, PerfMeterCustomMetricSnapshot[] customMetrics = null)
		{
			Update(metrics, frame, timeSeconds, customMetrics, PerfMeterPlatformTelemetrySnapshot.Unavailable());
		}

		internal void Update(
			PerfMeterMetricsSnapshot metrics,
			int frame,
			double timeSeconds,
			PerfMeterCustomMetricSnapshot[] customMetrics,
			PerfMeterPlatformTelemetrySnapshot platformTelemetry,
			string graphicsStateTraceId = "")
		{
			Update(
				metrics,
				frame,
				timeSeconds,
				new PerfMeterCustomMetricCollection(customMetrics, customMetrics == null ? 0 : customMetrics.Length),
				platformTelemetry,
				graphicsStateTraceId);
		}

		internal void Update(
			PerfMeterMetricsSnapshot metrics,
			int frame,
			double timeSeconds,
			PerfMeterCustomMetricCollection customMetrics,
			PerfMeterPlatformTelemetrySnapshot platformTelemetry,
			string graphicsStateTraceId = "")
		{
			if (_state != PerfMeterSessionState.Recording)
			{
				return;
			}

			using (PerfMeterProfilerInstrumentation.SessionCaptureMarker.Auto())
			{
				UpdateRecording(metrics, frame, timeSeconds, customMetrics, platformTelemetry, graphicsStateTraceId);
			}
		}

		private void UpdateRecording(PerfMeterMetricsSnapshot metrics, int frame, double timeSeconds, PerfMeterCustomMetricCollection customMetrics, PerfMeterPlatformTelemetrySnapshot platformTelemetry, string graphicsStateTraceId)
		{
			if (frame < 0 || frame < _wholeRunStats.LastFrame)
			{
				return;
			}

			string activeSceneName = GetActiveSceneName();
			if (!string.Equals(activeSceneName, _lastSceneName, System.StringComparison.Ordinal))
			{
				HandleSceneChanged(activeSceneName, frame, timeSeconds, metrics);
			}

			if (frame - _startFrame < _options.WarmupFrames)
			{
				_timeline.AddMissingBaseline(frame, frame, timeSeconds, timeSeconds, PerfMeterSessionTimelineReasonFlags.Warmup);
				return;
			}

			if (timeSeconds - _startTimeSeconds < _options.WarmupSeconds)
			{
				_timeline.AddMissingBaseline(frame, frame, timeSeconds, timeSeconds, PerfMeterSessionTimelineReasonFlags.Warmup);
				return;
			}

			if ((_sceneIgnoreUntilFrame >= 0 && frame < _sceneIgnoreUntilFrame) || timeSeconds < _sceneIgnoreUntilTimeSeconds)
			{
				_timeline.AddMissingBaseline(frame, frame, timeSeconds, timeSeconds, PerfMeterSessionTimelineReasonFlags.SceneLoadIgnore);
				return;
			}

			if (timeSeconds < _nextSampleTimeSeconds)
			{
				return;
			}

			_nextSampleTimeSeconds = timeSeconds + _options.SampleIntervalSeconds;

			if (_sampleCount >= _samples.Length)
			{
				_droppedSampleCount++;
				_timeline.AddMissingBaseline(frame, frame, timeSeconds, timeSeconds, PerfMeterSessionTimelineReasonFlags.SampleBufferFull);
				_summary = CreateSummary(timeSeconds, "Session sample buffer is full; additional samples are dropped.");
				return;
			}

			PerfMeterSessionSampleSnapshot sample = new PerfMeterSessionSampleSnapshot(frame, timeSeconds, _lastSceneName, metrics, CopyCustomMetrics(customMetrics), platformTelemetry, graphicsStateTraceId);
			_samples[_sampleCount] = sample;
			_sampleCount++;
			_timeline.AddValidBaseline(frame, timeSeconds, _sampleCount - 1);
			_wholeRunStats.Add(sample, metrics);
			_currentSceneStats.Add(sample, metrics);
			_summary = CreateSummary(timeSeconds, string.Empty);
		}

		internal PerfMeterSessionSummarySnapshot GetSummary()
		{
			return _summary;
		}

		internal PerfMeterSessionSampleSnapshot[] GetSamplesCopy()
		{
			if (_sampleCount == 0)
			{
				return System.Array.Empty<PerfMeterSessionSampleSnapshot>();
			}

			PerfMeterSessionSampleSnapshot[] copy = new PerfMeterSessionSampleSnapshot[_sampleCount];
			for (int i = 0; i < _sampleCount; i++)
			{
				PerfMeterSessionSampleSnapshot sample = _samples[i];
				copy[i] = new PerfMeterSessionSampleSnapshot(sample.CollectionFrame, sample.CollectionTimeSeconds, sample.SceneName, sample.Metrics, CopyCustomMetrics(sample.CustomMetrics), sample.PlatformTelemetry, sample.GraphicsStateTraceId);
			}

			return copy;
		}

		internal PerfMeterSessionTimelineSnapshot GetTimelineCopy()
		{
			return _timeline.GetSnapshotCopy();
		}

		internal void RecordMissingCollection(
			int frame,
			double timeSeconds,
			PerfMeterSessionTimelineReasonFlags reason,
			PerfMeterCaptureStatusSnapshot captureStatus,
			string bundleId)
		{
			if (_state != PerfMeterSessionState.Recording)
			{
				return;
			}

			if (captureStatus.State == PerfMeterCaptureState.Capturing)
			{
				_timeline.AddMissingCapture(
					frame,
					frame,
					timeSeconds,
					timeSeconds,
					captureStatus.CaptureId,
					bundleId,
					captureStatus.CompletedCaptureFrames + 1,
					captureStatus.RequestedCaptureFrames,
					reason);
			}
			else
			{
				_timeline.AddMissingBaseline(frame, frame, timeSeconds, timeSeconds, reason);
			}
		}

		internal void RecordValidCapture(
			int frame,
			double timeSeconds,
			PerfMeterCaptureStatusSnapshot captureStatus,
			string bundleId,
			int captureSampleIndex)
		{
			if (_state != PerfMeterSessionState.Recording || captureStatus.State != PerfMeterCaptureState.Capturing)
			{
				return;
			}

			_timeline.AddValidCapture(
				frame,
				timeSeconds,
				captureStatus.CaptureId,
				bundleId,
				captureStatus.CompletedCaptureFrames + 1,
				captureStatus.RequestedCaptureFrames,
				captureSampleIndex);
		}

		internal void RecordCaptureBoundary(
			int frame,
			double timeSeconds,
			PerfMeterCaptureStatusSnapshot captureStatus,
			string bundleId,
			PerfMeterSessionTimelineCaptureBoundary boundary)
		{
			if (_state != PerfMeterSessionState.Recording)
			{
				return;
			}

			_timeline.AddCaptureBoundary(
				frame,
				timeSeconds,
				captureStatus.CaptureId,
				bundleId,
				boundary,
				PerfMeterSessionTimelineUtility.GetCapturePhase(captureStatus.State),
				captureStatus.RequestedCaptureFrames,
				captureStatus.State == PerfMeterCaptureState.Unavailable || captureStatus.State == PerfMeterCaptureState.Error
					? PerfMeterSessionTimelineReasonFlags.CaptureFrameMissing
					: PerfMeterSessionTimelineReasonFlags.None);
		}

		private void ResetRecordingWindow(string sceneName, int frame, double timeSeconds, PerfMeterMetricsSnapshot latestMetrics)
		{
			_timeline.Reset();
			_startSceneName = sceneName;
			_lastSceneName = sceneName;
			_startTimeSeconds = timeSeconds;
			_startFrame = frame;
			_stopTimeSeconds = 0d;
			_nextSampleTimeSeconds = timeSeconds;
			_sceneIgnoreUntilFrame = -1;
			_sceneIgnoreUntilTimeSeconds = 0d;
			_sampleCount = 0;
			_droppedSampleCount = 0;
			ResetFocusStats(timeSeconds);
			_wholeRunStats.Reset(sceneName, frame, timeSeconds, latestMetrics.FrameSpikeCount, latestMetrics.SevereFrameSpikeCount);
			_currentSceneStats.Reset(sceneName, frame, timeSeconds, latestMetrics.FrameSpikeCount, latestMetrics.SevereFrameSpikeCount);
		}

		private void ResetFocusStats(double timeSeconds)
		{
			_focusLossCount = 0;
			_pauseCount = 0;
			_focusPausedDurationSeconds = 0d;
			_focusPaused = !_applicationFocused || _applicationPaused;
			_focusPausedStartTimeSeconds = _focusPaused ? timeSeconds : 0d;
		}

		private void UpdateFocusPausedWindow(double timeSeconds)
		{
			bool focusPaused = !_applicationFocused || _applicationPaused;
			if (_focusPaused == focusPaused)
			{
				return;
			}

			if (focusPaused)
			{
				_focusPausedStartTimeSeconds = timeSeconds;
			}
			else
			{
				_focusPausedDurationSeconds += System.Math.Max(0d, timeSeconds - _focusPausedStartTimeSeconds);
				_focusPausedStartTimeSeconds = 0d;
			}

			_focusPaused = focusPaused;
		}

		private double GetFocusPausedDurationSeconds(double currentTimeSeconds)
		{
			double durationSeconds = _focusPausedDurationSeconds;
			if (_focusPaused)
			{
				durationSeconds += System.Math.Max(0d, currentTimeSeconds - _focusPausedStartTimeSeconds);
			}

			return durationSeconds;
		}

		private void HandleSceneChanged(string sceneName, int frame, double timeSeconds, PerfMeterMetricsSnapshot metrics)
		{
			if (_options.ResetOnSceneLoad)
			{
				ResetRecordingWindow(sceneName, frame, timeSeconds, metrics);
			}
			else
			{
				_lastSceneName = sceneName;
				_currentSceneStats.Reset(sceneName, frame, timeSeconds, metrics.FrameSpikeCount, metrics.SevereFrameSpikeCount);
			}

			_sceneIgnoreUntilFrame = _options.SceneLoadIgnoreFrames > 0 ? frame + _options.SceneLoadIgnoreFrames : -1;
			_sceneIgnoreUntilTimeSeconds = _options.SceneLoadIgnoreSeconds > 0f ? timeSeconds + _options.SceneLoadIgnoreSeconds : 0d;
			_summary = CreateSummary(timeSeconds, string.Empty);
		}

		private PerfMeterSessionSummarySnapshot CreateSummary(double currentTimeSeconds, string warning)
		{
			string summaryWarning = !string.IsNullOrEmpty(warning) || _droppedSampleCount == 0
				? warning
				: "Session sample buffer is full; additional samples are dropped.";
			bool stopped = _state == PerfMeterSessionState.Stopped;
			PerfMeterSessionScopeSummarySnapshot wholeRun = _wholeRunStats.ToSnapshot(currentTimeSeconds, stopped, _stopTimeSeconds, _startSceneName);
			PerfMeterSessionScopeSummarySnapshot currentScene = _currentSceneStats.ToSnapshot(currentTimeSeconds, stopped, _stopTimeSeconds, _lastSceneName);

			return new PerfMeterSessionSummarySnapshot(
				_state,
				_options,
				_sampleCount,
				_droppedSampleCount,
				wholeRun.FirstFrame,
				wholeRun.LastFrame,
				_startTimeSeconds,
				_stopTimeSeconds,
				wholeRun.DurationSeconds,
				wholeRun.AverageFrameTimeMs,
				wholeRun.MinFrameTimeMs,
				wholeRun.MaxFrameTimeMs,
				wholeRun.AverageFps,
				wholeRun.MinFps,
				wholeRun.MaxFps,
				wholeRun.GpuBoundSampleCount,
				wholeRun.CpuMainThreadBoundSampleCount,
				wholeRun.CpuRenderThreadBoundSampleCount,
				wholeRun.PresentLimitedSampleCount,
				wholeRun.FrameSpikeCount,
				wholeRun.SevereFrameSpikeCount,
				summaryWarning,
				_device,
				_camera,
				_configuredSettings,
				_effectiveSettings,
				_startSceneName,
				_lastSceneName,
				wholeRun,
				currentScene,
				_focusLossCount,
				_pauseCount,
				GetFocusPausedDurationSeconds(stopped ? _stopTimeSeconds : currentTimeSeconds),
				_sessionId);
		}

		private static string GetActiveSceneName()
		{
			Scene scene = SceneManager.GetActiveScene();
			return string.IsNullOrEmpty(scene.name) ? scene.path : scene.name;
		}

		private static PerfMeterCustomMetricSnapshot[] CopyCustomMetrics(PerfMeterCustomMetricSnapshot[] customMetrics)
		{
			return CopyCustomMetrics(new PerfMeterCustomMetricCollection(customMetrics, customMetrics == null ? 0 : customMetrics.Length));
		}

		private static PerfMeterCustomMetricSnapshot[] CopyCustomMetrics(PerfMeterCustomMetricCollection customMetrics)
		{
			if (customMetrics.Count == 0)
			{
				return System.Array.Empty<PerfMeterCustomMetricSnapshot>();
			}

			PerfMeterCustomMetricSnapshot[] copy = new PerfMeterCustomMetricSnapshot[customMetrics.Count];
			System.Array.Copy(customMetrics.Buffer, copy, customMetrics.Count);
			return copy;
		}

		private struct SessionStats
		{
			private double _frameTimeTotalMs;
			private double _fpsTotal;

			public string SceneName;
			public int SampleCount;
			public int FirstFrame;
			public int LastFrame;
			public double StartTimeSeconds;
			public double LastSampleTimeSeconds;
			public double MinFrameTimeMs;
			public double MaxFrameTimeMs;
			public double MinFps;
			public double MaxFps;
			public int GpuBoundSampleCount;
			public int CpuMainThreadBoundSampleCount;
			public int CpuRenderThreadBoundSampleCount;
			public int PresentLimitedSampleCount;
			public int FrameSpikeBaseline;
			public int SevereFrameSpikeBaseline;
			public int LatestFrameSpikeCount;
			public int LatestSevereFrameSpikeCount;
			public PerfMeterSessionWorstFrameSnapshot WorstFrame;

			public void Reset(string sceneName, int frame, double timeSeconds, int frameSpikeBaseline, int severeFrameSpikeBaseline)
			{
				SceneName = sceneName ?? string.Empty;
				SampleCount = 0;
				FirstFrame = -1;
				LastFrame = -1;
				StartTimeSeconds = timeSeconds;
				LastSampleTimeSeconds = timeSeconds;
				_frameTimeTotalMs = 0d;
				_fpsTotal = 0d;
				MinFrameTimeMs = double.MaxValue;
				MaxFrameTimeMs = 0d;
				MinFps = double.MaxValue;
				MaxFps = 0d;
				GpuBoundSampleCount = 0;
				CpuMainThreadBoundSampleCount = 0;
				CpuRenderThreadBoundSampleCount = 0;
				PresentLimitedSampleCount = 0;
				FrameSpikeBaseline = frameSpikeBaseline;
				SevereFrameSpikeBaseline = severeFrameSpikeBaseline;
				LatestFrameSpikeCount = 0;
				LatestSevereFrameSpikeCount = 0;
				WorstFrame = PerfMeterSessionWorstFrameSnapshot.Empty;
			}

			public void Add(PerfMeterSessionSampleSnapshot sample, PerfMeterMetricsSnapshot metrics)
			{
				double frameTimeMs = GetFrameTimeMs(metrics);
				double fps = frameTimeMs > 0d ? 1000d / frameTimeMs : 0d;

				if (SampleCount == 0)
				{
					FirstFrame = sample.CollectionFrame;
				}

				SampleCount++;
				LastFrame = sample.CollectionFrame;
				LastSampleTimeSeconds = sample.CollectionTimeSeconds;
				_frameTimeTotalMs += frameTimeMs;
				_fpsTotal += fps;
				MinFrameTimeMs = System.Math.Min(MinFrameTimeMs, frameTimeMs);
				MaxFrameTimeMs = System.Math.Max(MaxFrameTimeMs, frameTimeMs);
				MinFps = System.Math.Min(MinFps, fps);
				MaxFps = System.Math.Max(MaxFps, fps);

				switch (metrics.Bottleneck)
				{
					case PerfMeterBottleneck.GpuBound:
						GpuBoundSampleCount++;
						break;
					case PerfMeterBottleneck.CpuMainThreadBound:
						CpuMainThreadBoundSampleCount++;
						break;
					case PerfMeterBottleneck.CpuRenderThreadBound:
						CpuRenderThreadBoundSampleCount++;
						break;
					case PerfMeterBottleneck.PresentLimited:
						PresentLimitedSampleCount++;
						break;
				}

				LatestFrameSpikeCount = Mathf.Max(0, metrics.FrameSpikeCount - FrameSpikeBaseline);
				LatestSevereFrameSpikeCount = Mathf.Max(0, metrics.SevereFrameSpikeCount - SevereFrameSpikeBaseline);

				if (!WorstFrame.IsAvailable || frameTimeMs > WorstFrame.FrameTimeMs)
				{
					WorstFrame = new PerfMeterSessionWorstFrameSnapshot(sample.CollectionFrame, sample.CollectionTimeSeconds, sample.SceneName, frameTimeMs, fps, metrics.Bottleneck);
				}
			}

			public PerfMeterSessionScopeSummarySnapshot ToSnapshot(double currentTimeSeconds, bool stopped, double stopTimeSeconds, string fallbackSceneName)
			{
				string sceneName = string.IsNullOrEmpty(SceneName) ? fallbackSceneName : SceneName;
				double endTimeSeconds = stopped ? stopTimeSeconds : currentTimeSeconds;
				double durationSeconds = SampleCount > 0 ? System.Math.Max(0d, endTimeSeconds - StartTimeSeconds) : 0d;
				return new PerfMeterSessionScopeSummarySnapshot(
					sceneName,
					SampleCount,
					FirstFrame,
					LastFrame,
					StartTimeSeconds,
					LastSampleTimeSeconds,
					durationSeconds,
					SampleCount > 0 ? _frameTimeTotalMs / SampleCount : 0d,
					SampleCount > 0 ? MinFrameTimeMs : 0d,
					SampleCount > 0 ? MaxFrameTimeMs : 0d,
					SampleCount > 0 ? _fpsTotal / SampleCount : 0d,
					SampleCount > 0 ? MinFps : 0d,
					SampleCount > 0 ? MaxFps : 0d,
					GpuBoundSampleCount,
					CpuMainThreadBoundSampleCount,
					CpuRenderThreadBoundSampleCount,
					PresentLimitedSampleCount,
					LatestFrameSpikeCount,
					LatestSevereFrameSpikeCount,
					WorstFrame);
			}
		}

		private static double GetFrameTimeMs(PerfMeterMetricsSnapshot metrics)
		{
			return metrics.CpuFrameTimeMs > 0d ? metrics.CpuFrameTimeMs : metrics.FrameBudgetMs;
		}

	}
}
