using UnityEngine;

namespace SGG.PerfMeter
{
	/// <summary>
	/// Public entry point for agent-readable performance meter state and latest metric snapshots.
	/// </summary>
	public static class PerformanceMeter
	{
		public static event System.Action<PerfMeterAlertSnapshot> AlertFired;

		public static void RegisterCustomMetricProvider(IPerfMeterCustomMetricProvider provider)
		{
			PerfMeterCustomMetricRegistry.Register(provider);
		}

		public static void UnregisterCustomMetricProvider(IPerfMeterCustomMetricProvider provider)
		{
			PerfMeterCustomMetricRegistry.Unregister(provider);
		}

		public static void ClearCustomMetricProviders()
		{
			PerfMeterCustomMetricRegistry.Clear();
		}

		public static PerfMeterCustomMetricSnapshot[] GetCustomMetrics()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetLatestCustomMetrics() : PerfMeterCustomMetricRegistry.Collect();
		}

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

		public static PerfMeterAlertSnapshot[] GetLatestAlerts()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetLatestAlerts() : System.Array.Empty<PerfMeterAlertSnapshot>();
		}

		public static void ClearAlerts()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.ClearAlerts();
			}
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

		public static PerfMeterRenderGraphSnapshot GetRenderGraphSnapshot()
		{
			return PerfMeterRenderGraphAnalytics.GetSnapshot();
		}

		public static bool TryGetRenderGraphSnapshot(out PerfMeterRenderGraphSnapshot renderGraphSnapshot)
		{
			renderGraphSnapshot = GetRenderGraphSnapshot();
			return renderGraphSnapshot.IsAvailable;
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

		public static void ResetStats()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.ResetStats();
			}
		}

		public static PerfMeterCollectionMode CollectionMode
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.CollectionMode : PerfMeterCollectionMode.Stopped;
			}
		}

		public static void SetCollectionMode(PerfMeterCollectionMode mode)
		{
			if (mode == PerfMeterCollectionMode.Stopped)
			{
				Stop();
				return;
			}

			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetCollectionMode(mode);
			}
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

		public static PerfMeterSessionSampleSnapshot[] GetSessionSamples()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetSessionSamples() : System.Array.Empty<PerfMeterSessionSampleSnapshot>();
		}

		public static bool ExportSessionJson(string path)
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			PerfMeterSessionSummarySnapshot summary = runtime != null ? runtime.GetSessionSummary() : PerfMeterSessionSummarySnapshot.Empty;
			PerfMeterSessionSampleSnapshot[] samples = runtime != null ? runtime.GetSessionSamples() : System.Array.Empty<PerfMeterSessionSampleSnapshot>();
			return PerfMeterSessionExporter.ExportJson(path, summary, samples, GetStatus());
		}

		public static bool ExportSessionCsv(string path)
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			PerfMeterSessionSummarySnapshot summary = runtime != null ? runtime.GetSessionSummary() : PerfMeterSessionSummarySnapshot.Empty;
			PerfMeterSessionSampleSnapshot[] samples = runtime != null ? runtime.GetSessionSamples() : System.Array.Empty<PerfMeterSessionSampleSnapshot>();
			return PerfMeterSessionExporter.ExportCsv(path, summary, samples, GetStatus());
		}

		public static void RequestOverdrawMeasurement(int frameCount = 0)
		{
			PerfMeterSettingsSnapshot settings = GetSettings();
			int normalizedFrameCount = frameCount <= 0
				? settings.OverdrawDefaultFrameCount
				: Mathf.Clamp(frameCount, 1, settings.OverdrawMaxFrameCount);
			PerfMeterRuntime.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.RequestOverdrawMeasurement(normalizedFrameCount);
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

		internal static void ApplyOverlayTuning(PerfMeterSettingsSnapshot settings)
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayTuning(settings);
			}
		}

		internal static void RaiseAlertFired(PerfMeterAlertSnapshot alert)
		{
			AlertFired?.Invoke(alert);
		}
	}

	internal static class PerfMeterCustomMetricRegistry
	{
		private static readonly System.Collections.Generic.List<IPerfMeterCustomMetricProvider> Providers = new System.Collections.Generic.List<IPerfMeterCustomMetricProvider>();
		private static readonly object SyncRoot = new object();
		private static IPerfMeterCustomMetricProvider[] _providerSnapshot = System.Array.Empty<IPerfMeterCustomMetricProvider>();

		internal static void Register(IPerfMeterCustomMetricProvider provider)
		{
			if (provider == null)
			{
				throw new System.ArgumentNullException(nameof(provider));
			}

			lock (SyncRoot)
			{
				if (!Providers.Contains(provider))
				{
					Providers.Add(provider);
					_providerSnapshot = Providers.ToArray();
				}
			}
		}

		internal static void Unregister(IPerfMeterCustomMetricProvider provider)
		{
			if (provider == null)
			{
				return;
			}

			lock (SyncRoot)
			{
				if (Providers.Remove(provider))
				{
					_providerSnapshot = Providers.ToArray();
				}
			}
		}

		internal static void Clear()
		{
			lock (SyncRoot)
			{
				Providers.Clear();
				_providerSnapshot = System.Array.Empty<IPerfMeterCustomMetricProvider>();
			}
		}

		internal static PerfMeterCustomMetricSnapshot[] Collect()
		{
			IPerfMeterCustomMetricProvider[] providers;
			lock (SyncRoot)
			{
				providers = _providerSnapshot;
				if (providers.Length == 0)
				{
					return System.Array.Empty<PerfMeterCustomMetricSnapshot>();
				}
			}

			PerfMeterCustomMetricSnapshot[] metrics = new PerfMeterCustomMetricSnapshot[providers.Length];
			int count = 0;
			for (int i = 0; i < providers.Length; i++)
			{
				IPerfMeterCustomMetricProvider provider = providers[i];
				string providerId = GetProviderId(provider, i);
				try
				{
					if (provider.TryCollect(out PerfMeterCustomMetricSnapshot metric))
					{
						metrics[count] = NormalizeMetric(metric, providerId);
						count++;
					}
				}
				catch (System.Exception exception)
				{
					metrics[count] = new PerfMeterCustomMetricSnapshot(providerId, providerId, "custom", string.Empty, 0d, false, exception.GetType().Name + ": " + exception.Message);
					count++;
				}
			}

			if (count == metrics.Length)
			{
				return metrics;
			}

			PerfMeterCustomMetricSnapshot[] compact = new PerfMeterCustomMetricSnapshot[count];
			System.Array.Copy(metrics, compact, count);
			return compact;
		}

		private static string GetProviderId(IPerfMeterCustomMetricProvider provider, int index)
		{
			try
			{
				string id = provider.Id;
				return string.IsNullOrEmpty(id) ? "custom_metric_" + index : id;
			}
			catch (System.Exception)
			{
				return "custom_metric_" + index;
			}
		}

		private static PerfMeterCustomMetricSnapshot NormalizeMetric(PerfMeterCustomMetricSnapshot metric, string providerId)
		{
			string id = string.IsNullOrEmpty(metric.Id) ? providerId : metric.Id;
			string name = string.IsNullOrEmpty(metric.Name) ? id : metric.Name;
			string category = string.IsNullOrEmpty(metric.Category) ? "custom" : metric.Category;
			return new PerfMeterCustomMetricSnapshot(id, name, category, metric.Unit, metric.Value, metric.Available, metric.Warning);
		}
	}
}
