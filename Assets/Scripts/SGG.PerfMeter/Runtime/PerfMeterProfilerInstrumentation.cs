using System;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;

namespace SGG.PerfMeter
{
	internal static class PerfMeterProfilerInstrumentation
	{
		internal const string CollectMarkerName = "SGG.PerfMeter.Collect";
		internal const string FrameTimingMarkerName = "SGG.PerfMeter.Collect.FrameTiming";
		internal const string CustomMetricsMarkerName = "SGG.PerfMeter.Provider.CustomMetrics";
		internal const string CpuCoreMarkerName = "SGG.PerfMeter.Provider.CpuCore";
		internal const string DeviceSnapshotMarkerName = "SGG.PerfMeter.Provider.DeviceSnapshot";
		internal const string CameraSnapshotMarkerName = "SGG.PerfMeter.Provider.CameraSnapshot";
		internal const string BottleneckMarkerName = "SGG.PerfMeter.Bottleneck.Classify";
		internal const string SessionCaptureMarkerName = "SGG.PerfMeter.Capture.Session";
		internal const string AlertCaptureMarkerName = "SGG.PerfMeter.Capture.AlertScope";
		internal const string CaptureCoordinatorMarkerName = "SGG.PerfMeter.Capture.Coordinator";
		internal const string ExportJsonMarkerName = "SGG.PerfMeter.Export.Json";
		internal const string ExportCsvMarkerName = "SGG.PerfMeter.Export.Csv";
		internal const string ThermalSampleMarkerName = "SGG.PerfMeter.Thermal.Sample";
		internal const string SessionMarkerPrefix = "SGG.PerfMeter.Session.";
		internal const string SessionBeginMarkerSuffix = ".Begin";
		internal const string SessionEndMarkerSuffix = ".End";

		internal const string CpuFrameTimeCounterName = "SGG.PerfMeter.CPU.FrameTime";
		internal const string CpuMainThreadTimeCounterName = "SGG.PerfMeter.CPU.MainThreadTime";
		internal const string CpuRenderThreadTimeCounterName = "SGG.PerfMeter.CPU.RenderThreadTime";
		internal const string CpuPresentWaitTimeCounterName = "SGG.PerfMeter.CPU.PresentWaitTime";
		internal const string CpuFrameTimingAvailableCounterName = "SGG.PerfMeter.CPU.FrameTimingAvailable";
		internal const string GpuFrameTimeCounterName = "SGG.PerfMeter.GPU.FrameTime";
		internal const string GpuFrameTimingAvailableCounterName = "SGG.PerfMeter.GPU.FrameTimingAvailable";
		internal const string BottleneckCounterName = "SGG.PerfMeter.Bottleneck.Kind";
		internal const string CustomMetricCountCounterName = "SGG.PerfMeter.Provider.CustomMetricCount";
		internal const string SessionStateCounterName = "SGG.PerfMeter.Capture.SessionState";
		internal const string AlertScopeActiveCounterName = "SGG.PerfMeter.Capture.AlertScopeActive";
		internal const string OverdrawStateCounterName = "SGG.PerfMeter.Capture.OverdrawState";
		internal const string CaptureStateCounterName = "SGG.PerfMeter.Capture.State";
		internal const string ThermalAvailableCounterName = "SGG.PerfMeter.Thermal.Available";

		internal static readonly ProfilerMarker CollectMarker = new ProfilerMarker(CollectMarkerName);
		internal static readonly ProfilerMarker FrameTimingMarker = new ProfilerMarker(FrameTimingMarkerName);
		internal static readonly ProfilerMarker CustomMetricsMarker = new ProfilerMarker(CustomMetricsMarkerName);
		internal static readonly ProfilerMarker CpuCoreMarker = new ProfilerMarker(CpuCoreMarkerName);
		internal static readonly ProfilerMarker DeviceSnapshotMarker = new ProfilerMarker(DeviceSnapshotMarkerName);
		internal static readonly ProfilerMarker CameraSnapshotMarker = new ProfilerMarker(CameraSnapshotMarkerName);
		internal static readonly ProfilerMarker BottleneckMarker = new ProfilerMarker(BottleneckMarkerName);
		internal static readonly ProfilerMarker SessionCaptureMarker = new ProfilerMarker(SessionCaptureMarkerName);
		internal static readonly ProfilerMarker AlertCaptureMarker = new ProfilerMarker(AlertCaptureMarkerName);
		internal static readonly ProfilerMarker CaptureCoordinatorMarker = new ProfilerMarker(CaptureCoordinatorMarkerName);
		internal static readonly ProfilerMarker ExportJsonMarker = new ProfilerMarker(ExportJsonMarkerName);
		internal static readonly ProfilerMarker ExportCsvMarker = new ProfilerMarker(ExportCsvMarkerName);
		internal static readonly ProfilerMarker ThermalSampleMarker = new ProfilerMarker(ThermalSampleMarkerName);

		private const ProfilerCounterOptions GaugeOptions = ProfilerCounterOptions.FlushOnEndOfFrame;
		private static readonly PerfMeterProfilerCounterLong CpuFrameTimeCounter = CreateTimeCounter(CpuFrameTimeCounterName);
		private static readonly PerfMeterProfilerCounterLong CpuMainThreadTimeCounter = CreateTimeCounter(CpuMainThreadTimeCounterName);
		private static readonly PerfMeterProfilerCounterLong CpuRenderThreadTimeCounter = CreateTimeCounter(CpuRenderThreadTimeCounterName);
		private static readonly PerfMeterProfilerCounterLong CpuPresentWaitTimeCounter = CreateTimeCounter(CpuPresentWaitTimeCounterName);
		private static readonly PerfMeterProfilerCounterInt CpuFrameTimingAvailableCounter = CreateCountCounter(CpuFrameTimingAvailableCounterName);
		private static readonly PerfMeterProfilerCounterLong GpuFrameTimeCounter = CreateTimeCounter(GpuFrameTimeCounterName);
		private static readonly PerfMeterProfilerCounterInt GpuFrameTimingAvailableCounter = CreateCountCounter(GpuFrameTimingAvailableCounterName);
		private static readonly PerfMeterProfilerCounterInt BottleneckCounter = CreateCountCounter(BottleneckCounterName);
		private static readonly PerfMeterProfilerCounterInt CustomMetricCountCounter = CreateCountCounter(CustomMetricCountCounterName);
		private static readonly PerfMeterProfilerCounterInt SessionStateCounter = CreateCountCounter(SessionStateCounterName);
		private static readonly PerfMeterProfilerCounterInt AlertScopeActiveCounter = CreateCountCounter(AlertScopeActiveCounterName);
		private static readonly PerfMeterProfilerCounterInt OverdrawStateCounter = CreateCountCounter(OverdrawStateCounterName);
		private static readonly PerfMeterProfilerCounterInt CaptureStateCounter = CreateCountCounter(CaptureStateCounterName);
		private static readonly PerfMeterProfilerCounterInt ThermalAvailableCounter = CreateCountCounter(ThermalAvailableCounterName);
		private static string _sessionMarkerId = string.Empty;
		private static ProfilerMarker _sessionBeginMarker;
		private static ProfilerMarker _sessionEndMarker;

		internal static long CpuFrameTimeNanoseconds => CpuFrameTimeCounter.Value;
		internal static long CpuMainThreadTimeNanoseconds => CpuMainThreadTimeCounter.Value;
		internal static long CpuRenderThreadTimeNanoseconds => CpuRenderThreadTimeCounter.Value;
		internal static long CpuPresentWaitTimeNanoseconds => CpuPresentWaitTimeCounter.Value;
		internal static int CpuFrameTimingAvailable => CpuFrameTimingAvailableCounter.Value;
		internal static long GpuFrameTimeNanoseconds => GpuFrameTimeCounter.Value;
		internal static int GpuFrameTimingAvailable => GpuFrameTimingAvailableCounter.Value;
		internal static int Bottleneck => BottleneckCounter.Value;
		internal static int CustomMetricCount => CustomMetricCountCounter.Value;
		internal static int SessionState => SessionStateCounter.Value;
		internal static int AlertScopeActive => AlertScopeActiveCounter.Value;
		internal static int OverdrawState => OverdrawStateCounter.Value;
		internal static int CaptureState => CaptureStateCounter.Value;
		internal static int ThermalAvailable => ThermalAvailableCounter.Value;

		internal static void RecordFrameTimings(
			bool cpuAvailable,
			double cpuFrameTimeMs,
			double cpuMainThreadTimeMs,
			double cpuRenderThreadTimeMs,
			double cpuPresentWaitTimeMs,
			bool gpuAvailable,
			double gpuFrameTimeMs)
		{
			CpuFrameTimingAvailableCounter.Value = cpuAvailable ? 1 : 0;
			CpuFrameTimeCounter.Value = cpuAvailable ? MillisecondsToNanoseconds(cpuFrameTimeMs) : 0L;
			CpuMainThreadTimeCounter.Value = cpuAvailable ? MillisecondsToNanoseconds(cpuMainThreadTimeMs) : 0L;
			CpuRenderThreadTimeCounter.Value = cpuAvailable ? MillisecondsToNanoseconds(cpuRenderThreadTimeMs) : 0L;
			CpuPresentWaitTimeCounter.Value = cpuAvailable ? MillisecondsToNanoseconds(cpuPresentWaitTimeMs) : 0L;
			GpuFrameTimingAvailableCounter.Value = gpuAvailable ? 1 : 0;
			GpuFrameTimeCounter.Value = gpuAvailable ? MillisecondsToNanoseconds(gpuFrameTimeMs) : 0L;
		}

		internal static void ResetFrameTimings()
		{
			RecordFrameTimings(false, 0d, 0d, 0d, 0d, false, 0d);
			RecordBottleneck(PerfMeterBottleneck.Unknown);
		}

		internal static void RecordBottleneck(PerfMeterBottleneck bottleneck)
		{
			BottleneckCounter.Value = (int)bottleneck;
		}

		internal static void RecordCustomMetricCount(int count)
		{
			CustomMetricCountCounter.Value = Math.Max(0, count);
		}

		internal static void RecordSessionState(PerfMeterSessionState state)
		{
			SessionStateCounter.Value = (int)state;
		}

		internal static string GetSessionBoundaryMarkerName(string sessionId, bool isBegin)
		{
			if (string.IsNullOrEmpty(sessionId))
			{
				return string.Empty;
			}

			return SessionMarkerPrefix + sessionId + (isBegin ? SessionBeginMarkerSuffix : SessionEndMarkerSuffix);
		}

		internal static void RecordSessionBegin(string sessionId)
		{
			RecordSessionBoundary(sessionId, true);
		}

		internal static void RecordSessionEnd(string sessionId)
		{
			RecordSessionBoundary(sessionId, false);
		}

		internal static void RecordAlertScopeActive(bool active)
		{
			AlertScopeActiveCounter.Value = active ? 1 : 0;
		}

		internal static void RecordOverdrawState(PerfMeterOverdrawMeasurementState state)
		{
			OverdrawStateCounter.Value = (int)state;
		}

		internal static void RecordCaptureState(PerfMeterCaptureState state)
		{
			CaptureStateCounter.Value = (int)state;
		}

		internal static void RecordThermalAvailability(bool available)
		{
			using (ThermalSampleMarker.Auto())
			{
				ThermalAvailableCounter.Value = available ? 1 : 0;
			}
		}

		internal static void Reset()
		{
			ResetFrameTimings();
			RecordCustomMetricCount(0);
			RecordSessionState(PerfMeterSessionState.Idle);
			RecordAlertScopeActive(false);
			RecordOverdrawState(PerfMeterOverdrawMeasurementState.Off);
			RecordCaptureState(PerfMeterCaptureState.Idle);
			ThermalAvailableCounter.Value = 0;
			_sessionMarkerId = string.Empty;
			_sessionBeginMarker = default;
			_sessionEndMarker = default;
		}

		internal static long MillisecondsToNanoseconds(double milliseconds)
		{
			if (milliseconds <= 0d || double.IsNaN(milliseconds) || double.IsInfinity(milliseconds))
			{
				return 0L;
			}

			double nanoseconds = milliseconds * 1000000d;
			return nanoseconds >= long.MaxValue ? long.MaxValue : (long)Math.Round(nanoseconds);
		}

		private static void RecordSessionBoundary(string sessionId, bool isBegin)
		{
			if (string.IsNullOrEmpty(sessionId))
			{
				return;
			}

			if (!string.Equals(_sessionMarkerId, sessionId, StringComparison.Ordinal))
			{
				_sessionMarkerId = sessionId;
				_sessionBeginMarker = new ProfilerMarker(GetSessionBoundaryMarkerName(sessionId, true));
				_sessionEndMarker = new ProfilerMarker(GetSessionBoundaryMarkerName(sessionId, false));
			}

			ProfilerMarker marker = isBegin ? _sessionBeginMarker : _sessionEndMarker;
			marker.Begin();
			marker.End();
		}

		private static PerfMeterProfilerCounterLong CreateTimeCounter(string name)
		{
			return new PerfMeterProfilerCounterLong(name, ProfilerMarkerDataUnit.TimeNanoseconds, GaugeOptions);
		}

		private static PerfMeterProfilerCounterInt CreateCountCounter(string name)
		{
			return new PerfMeterProfilerCounterInt(name, ProfilerMarkerDataUnit.Count, GaugeOptions);
		}
	}

	internal readonly unsafe struct PerfMeterProfilerCounterLong
	{
	#if ENABLE_PROFILER
		private readonly long* _value;
	#endif

		internal PerfMeterProfilerCounterLong(string name, ProfilerMarkerDataUnit unit, ProfilerCounterOptions options)
		{
		#if ENABLE_PROFILER
			_value = (long*)ProfilerUnsafeUtility.CreateCounterValue(
				out _,
				name,
				ProfilerUnsafeUtility.CategoryScripts,
				MarkerFlags.Default,
				(byte)ProfilerMarkerDataType.Int64,
				(byte)unit,
				sizeof(long),
				options);
		#endif
		}

		internal long Value
		{
			get
			{
			#if ENABLE_PROFILER
				return *_value;
			#else
				return 0L;
			#endif
			}
			set
			{
			#if ENABLE_PROFILER
				*_value = value;
			#endif
			}
		}
	}

	internal readonly unsafe struct PerfMeterProfilerCounterInt
	{
	#if ENABLE_PROFILER
		private readonly int* _value;
	#endif

		internal PerfMeterProfilerCounterInt(string name, ProfilerMarkerDataUnit unit, ProfilerCounterOptions options)
		{
		#if ENABLE_PROFILER
			_value = (int*)ProfilerUnsafeUtility.CreateCounterValue(
				out _,
				name,
				ProfilerUnsafeUtility.CategoryScripts,
				MarkerFlags.Default,
				(byte)ProfilerMarkerDataType.Int32,
				(byte)unit,
				sizeof(int),
				options);
		#endif
		}

		internal int Value
		{
			get
			{
			#if ENABLE_PROFILER
				return *_value;
			#else
				return 0;
			#endif
			}
			set
			{
			#if ENABLE_PROFILER
				*_value = value;
			#endif
			}
		}
	}
}
