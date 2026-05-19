using UnityEngine;

namespace SGG.PerfMeter
{
	/// <summary>
	/// Public entry point for agent-readable performance meter state and latest metric snapshots.
	/// </summary>
	public static class PerformanceMeter
	{
		public static PerfMeterStatusSnapshot GetStatus()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.Status : PerfMeterRuntime.CreateStoppedStatus();
		}

		public static bool TryGetStatus(out PerfMeterStatusSnapshot status)
		{
			status = GetStatus();
			return status.Availability != PerfMeterAvailability.Unknown;
		}

		public static PerfMeterMetricsSnapshot GetLatestMetrics()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.LatestMetrics : PerfMeterMetricsSnapshot.Stopped;
		}

		public static bool TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)
		{
			metrics = GetLatestMetrics();
			return metrics.Availability != PerfMeterAvailability.Unknown;
		}

		public static PerfMeterDeviceSnapshot GetDeviceInfo()
		{
			return PerfMeterDeviceInfoProvider.CreateSnapshot();
		}

		public static bool TryGetDeviceInfo(out PerfMeterDeviceSnapshot deviceInfo)
		{
			deviceInfo = GetDeviceInfo();
			return !string.IsNullOrEmpty(deviceInfo.UnityVersion);
		}

		public static PerfMeterCameraSnapshot GetCameraSnapshot(PerfMeterCameraSource source = PerfMeterCameraSource.Auto, string cameraNameFilter = null)
		{
			return PerfMeterCameraSnapshotProvider.CreateSnapshot(source, cameraNameFilter);
		}

		public static bool TryGetCameraSnapshot(out PerfMeterCameraSnapshot cameraSnapshot, PerfMeterCameraSource source = PerfMeterCameraSource.Auto, string cameraNameFilter = null)
		{
			cameraSnapshot = GetCameraSnapshot(source, cameraNameFilter);
			return cameraSnapshot.IsAvailable;
		}

		public static PerfMeterSettingsSnapshot GetSettings()
		{
			return PerfMeterSettingsStore.LoadFromResources();
		}

		public static void EnsureRunning()
		{
			PerfMeterRuntime.EnsureRunning();
		}

		public static void Stop()
		{
			PerfMeterRuntime.StopRunning();
		}

		public static bool IsSessionRecording
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null && runtime.IsSessionRecording;
			}
		}

		public static void StartSession()
		{
			StartSession(PerfMeterSessionOptions.FromSettings(GetSettings()));
		}

		public static void StartSession(PerfMeterSessionOptions options)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.StartSession(options);
			}
		}

		public static void StopSession()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.StopSession();
			}
		}

		public static PerfMeterSessionSummarySnapshot GetSessionSummary()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetSessionSummary() : PerfMeterSessionSummarySnapshot.Empty;
		}

		public static void RequestOverdrawMeasurement(int frameCount = 60)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.RequestOverdrawMeasurement(frameCount);
			}
		}

		public static void CancelOverdrawMeasurement()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.CancelOverdrawMeasurement();
			}
		}

		public static bool IsOverdrawHeatmapVisible
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null && PerfMeterRuntime.IsOverdrawHeatmapVisible;
			}
		}

		public static void SetOverdrawHeatmapVisible(bool visible)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverdrawHeatmapVisible(visible);
			}
		}

		public static bool IsOverlayVisible
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null && runtime.IsOverlayVisible;
			}
		}

		public static PerfMeterOverlayCorner OverlayCorner
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayCorner : PerfMeterOverlayCorner.TopRight;
			}
		}

		public static PerfMeterOverlayMode OverlayMode
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayMode : PerfMeterOverlayMode.Full;
			}
		}

		public static PerfMeterOverlayPreset OverlayPreset
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayPreset : PerfMeterOverlayPreset.FullDiagnostics;
			}
		}

		public static PerfMeterOverlayModule OverlayModules
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayModules : PerfMeterOverlayModule.All;
			}
		}

		public static PerfMeterTargetFps TargetFps
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.TargetFps : PerfMeterTargetFps.Fps60;
			}
		}

		public static void SetOverlayVisible(bool visible)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayVisible(visible);
			}
		}

		public static void SetOverlayCorner(PerfMeterOverlayCorner corner)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayCorner(corner);
			}
		}

		public static void SetOverlayMode(PerfMeterOverlayMode mode)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayMode(mode);
			}
		}

		public static void SetOverlayPreset(PerfMeterOverlayPreset preset)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayPreset(preset);
			}
		}

		public static void SetOverlayModules(PerfMeterOverlayModule modules)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayModules(modules);
			}
		}

		public static void SetOverlayModuleVisible(PerfMeterOverlayModule module, bool visible)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayModuleVisible(module, visible);
			}
		}

		public static void SetTargetFps(PerfMeterTargetFps targetFps)
		{
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetTargetFps(targetFps);
			}
		}

		internal static void ApplySettings(PerfMeterSettingsSnapshot settings)
		{
			PerfMeterSettingsStore.ApplySnapshotToRuntime(settings);
		}
	}
}
