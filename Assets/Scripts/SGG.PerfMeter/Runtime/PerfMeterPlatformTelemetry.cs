using System;

namespace SGG.PerfMeter
{
	public enum PerfMeterThermalWarningLevel
	{
		Unknown = 0,
		Nominal = 1,
		ThrottlingImminent = 2,
		Throttling = 3
	}

	public enum PerfMeterAdaptiveBottleneck
	{
		Unknown = 0,
		Cpu = 1,
		Gpu = 2,
		TargetFrameRate = 3
	}

	public interface IPerfMeterPlatformTelemetryProvider
	{
		string Id { get; }
		bool TryCollect(out PerfMeterPlatformTelemetrySnapshot snapshot);
	}

	public readonly struct PerfMeterPlatformTelemetrySnapshot
	{
		public PerfMeterPlatformTelemetrySnapshot(
			PerfMeterAvailability availability,
			string providerId,
			string providerVersion,
			double sampleTimeSeconds,
			double lastChangeTimeSeconds,
			bool thermalWarningLevelAvailable,
			PerfMeterThermalWarningLevel thermalWarningLevel,
			bool temperatureLevelAvailable,
			float temperatureLevel,
			bool temperatureTrendAvailable,
			float temperatureTrend,
			bool cpuPerformanceLevelAvailable,
			int cpuPerformanceLevel,
			bool gpuPerformanceLevelAvailable,
			int gpuPerformanceLevel,
			bool performanceBottleneckAvailable,
			PerfMeterAdaptiveBottleneck performanceBottleneck,
			string warning = "")
		{
			Availability = availability;
			ProviderId = providerId ?? string.Empty;
			ProviderVersion = providerVersion ?? string.Empty;
			SampleTimeSeconds = Math.Max(0d, sampleTimeSeconds);
			LastChangeTimeSeconds = Math.Max(0d, lastChangeTimeSeconds);
			ThermalWarningLevelAvailable = thermalWarningLevelAvailable;
			ThermalWarningLevel = thermalWarningLevelAvailable ? thermalWarningLevel : PerfMeterThermalWarningLevel.Unknown;
			TemperatureLevelAvailable = temperatureLevelAvailable;
			TemperatureLevel = temperatureLevelAvailable ? Clamp(temperatureLevel, 0f, 1f) : 0f;
			TemperatureTrendAvailable = temperatureTrendAvailable;
			TemperatureTrend = temperatureTrendAvailable ? Clamp(temperatureTrend, -1f, 1f) : 0f;
			CpuPerformanceLevelAvailable = cpuPerformanceLevelAvailable;
			CpuPerformanceLevel = cpuPerformanceLevelAvailable ? Math.Max(0, cpuPerformanceLevel) : 0;
			GpuPerformanceLevelAvailable = gpuPerformanceLevelAvailable;
			GpuPerformanceLevel = gpuPerformanceLevelAvailable ? Math.Max(0, gpuPerformanceLevel) : 0;
			PerformanceBottleneckAvailable = performanceBottleneckAvailable;
			PerformanceBottleneck = performanceBottleneckAvailable ? performanceBottleneck : PerfMeterAdaptiveBottleneck.Unknown;
			Warning = warning ?? string.Empty;
		}

		public static PerfMeterPlatformTelemetrySnapshot Unavailable(string warning = "No platform telemetry provider is registered.")
		{
			return new PerfMeterPlatformTelemetrySnapshot(
				PerfMeterAvailability.Unavailable,
				string.Empty,
				string.Empty,
				0d,
				0d,
				false,
				PerfMeterThermalWarningLevel.Unknown,
				false,
				0f,
				false,
				0f,
				false,
				0,
				false,
				0,
				false,
				PerfMeterAdaptiveBottleneck.Unknown,
				warning);
		}

		public PerfMeterAvailability Availability { get; }
		public string ProviderId { get; }
		public string ProviderVersion { get; }
		public double SampleTimeSeconds { get; }
		public double LastChangeTimeSeconds { get; }
		public bool ThermalWarningLevelAvailable { get; }
		public PerfMeterThermalWarningLevel ThermalWarningLevel { get; }
		public bool TemperatureLevelAvailable { get; }
		public float TemperatureLevel { get; }
		public bool TemperatureTrendAvailable { get; }
		public float TemperatureTrend { get; }
		public bool CpuPerformanceLevelAvailable { get; }
		public int CpuPerformanceLevel { get; }
		public bool GpuPerformanceLevelAvailable { get; }
		public int GpuPerformanceLevel { get; }
		public bool PerformanceBottleneckAvailable { get; }
		public PerfMeterAdaptiveBottleneck PerformanceBottleneck { get; }
		public string Warning { get; }
		public bool IsAvailable => Availability == PerfMeterAvailability.Available;

		internal PerfMeterPlatformTelemetrySnapshot WithProviderId(string providerId)
		{
			return new PerfMeterPlatformTelemetrySnapshot(
				Availability,
				providerId,
				ProviderVersion,
				SampleTimeSeconds,
				LastChangeTimeSeconds,
				ThermalWarningLevelAvailable,
				ThermalWarningLevel,
				TemperatureLevelAvailable,
				TemperatureLevel,
				TemperatureTrendAvailable,
				TemperatureTrend,
				CpuPerformanceLevelAvailable,
				CpuPerformanceLevel,
				GpuPerformanceLevelAvailable,
				GpuPerformanceLevel,
				PerformanceBottleneckAvailable,
				PerformanceBottleneck,
				Warning);
		}

		private static float Clamp(float value, float minimum, float maximum)
		{
			return value < minimum ? minimum : value > maximum ? maximum : value;
		}
	}

	internal static class PerfMeterPlatformTelemetryRegistry
	{
		private static readonly object Sync = new object();
		private static IPerfMeterPlatformTelemetryProvider _provider;

		internal static void Register(IPerfMeterPlatformTelemetryProvider provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException(nameof(provider));
			}

			lock (Sync)
			{
				if (_provider != null && !ReferenceEquals(_provider, provider))
				{
					throw new InvalidOperationException("A platform telemetry provider is already registered.");
				}

				_provider = provider;
			}
		}

		internal static void Unregister(IPerfMeterPlatformTelemetryProvider provider)
		{
			if (provider == null)
			{
				return;
			}

			lock (Sync)
			{
				if (ReferenceEquals(_provider, provider))
				{
					_provider = null;
				}
			}
		}

		internal static PerfMeterPlatformTelemetrySnapshot Collect()
		{
			IPerfMeterPlatformTelemetryProvider provider;
			lock (Sync)
			{
				provider = _provider;
			}

			if (provider == null)
			{
				return PerfMeterPlatformTelemetrySnapshot.Unavailable();
			}

			string providerId;
			try
			{
				providerId = string.IsNullOrWhiteSpace(provider.Id) ? provider.GetType().FullName : provider.Id.Trim();
			}
			catch (Exception exception)
			{
				return PerfMeterPlatformTelemetrySnapshot.Unavailable("Platform telemetry provider identity failed: " + exception.GetType().Name + ": " + exception.Message);
			}

			try
			{
				if (!provider.TryCollect(out PerfMeterPlatformTelemetrySnapshot snapshot))
				{
					return PerfMeterPlatformTelemetrySnapshot.Unavailable("Platform telemetry provider '" + providerId + "' returned no sample.").WithProviderId(providerId);
				}

				return snapshot.WithProviderId(providerId);
			}
			catch (Exception exception)
			{
				return PerfMeterPlatformTelemetrySnapshot.Unavailable("Platform telemetry provider '" + providerId + "' failed: " + exception.GetType().Name + ": " + exception.Message).WithProviderId(providerId);
			}
		}

		internal static void ClearForTests()
		{
			lock (Sync)
			{
				_provider = null;
			}
		}
	}
}
