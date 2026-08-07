using System;
using Unity.Profiling.Memory;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterUnityMemoryProfilerBackend : IPerfMeterMemorySnapshotBackend
	{
		internal const string BackendId = "com.unity.memoryprofiler";
		private static readonly string BackendVersion = typeof(Unity.MemoryProfiler.MetadataCollect).Assembly.GetName().Version?.ToString() ?? string.Empty;

		public string Id => BackendId;
		public string Version => BackendVersion;
		public PerfMeterMemoryCaptureFlags SupportedCaptureFlags =>
			PerfMeterMemoryCaptureFlags.ManagedObjects |
			PerfMeterMemoryCaptureFlags.NativeObjects |
			PerfMeterMemoryCaptureFlags.NativeAllocations |
			PerfMeterMemoryCaptureFlags.NativeAllocationSites |
			PerfMeterMemoryCaptureFlags.NativeStackTraces;

		public bool TryCapture(
			string path,
			PerfMeterMemoryCaptureFlags captureFlags,
			Action<PerfMeterMemorySnapshotBackendResult> completed,
			out string error)
		{
			if (string.IsNullOrEmpty(path) || completed == null)
			{
				error = "Memory snapshot path and completion callback are required.";
				return false;
			}

			CaptureFlags unityFlags = MapCaptureFlags(captureFlags);
			MemoryProfiler.TakeSnapshot(
				path,
				(resultPath, success) => completed(new PerfMeterMemorySnapshotBackendResult(success, resultPath, success ? string.Empty : "Unity Memory Profiler did not produce a snapshot.")),
				unityFlags);
			error = string.Empty;
			return true;
		}

		private static CaptureFlags MapCaptureFlags(PerfMeterMemoryCaptureFlags flags)
		{
			CaptureFlags result = 0;
			if ((flags & PerfMeterMemoryCaptureFlags.ManagedObjects) != 0)
			{
				result |= CaptureFlags.ManagedObjects;
			}

			if ((flags & PerfMeterMemoryCaptureFlags.NativeObjects) != 0)
			{
				result |= CaptureFlags.NativeObjects;
			}

			if ((flags & PerfMeterMemoryCaptureFlags.NativeAllocations) != 0)
			{
				result |= CaptureFlags.NativeAllocations;
			}

			if ((flags & PerfMeterMemoryCaptureFlags.NativeAllocationSites) != 0)
			{
				result |= CaptureFlags.NativeAllocationSites;
			}

			if ((flags & PerfMeterMemoryCaptureFlags.NativeStackTraces) != 0)
			{
				result |= CaptureFlags.NativeStackTraces;
			}

			return result;
		}
	}

	internal static class PerfMeterUnityMemoryProfilerBootstrap
	{
		private static PerfMeterUnityMemoryProfilerBackend _backend;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			if (_backend != null)
			{
				PerformanceMeter.UnregisterMemorySnapshotBackend(_backend);
				_backend = null;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Register()
		{
			if (_backend != null)
			{
				return;
			}

			_backend = new PerfMeterUnityMemoryProfilerBackend();
			try
			{
				PerformanceMeter.RegisterMemorySnapshotBackend(_backend);
			}
			catch (InvalidOperationException)
			{
				_backend = null;
			}
		}
	}
}
