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

	public enum PerfMeterPlatformTelemetryFreshness
	{
		Unknown = 0,
		Fresh = 1,
		Stale = 2
	}

	public enum PerfMeterPlatformTelemetryCollectionResult
	{
		NotAttempted = 0,
		Collected = 1,
		NoProvider = 2,
		ProviderReturnedNoSample = 3,
		ProviderIdentityFailed = 4,
		ProviderFailed = 5
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
			string warning = "",
			double lastAttemptTimeSeconds = 0d,
			double lastSuccessTimeSeconds = 0d,
			double sampleAgeSeconds = 0d,
			PerfMeterPlatformTelemetryFreshness freshness = PerfMeterPlatformTelemetryFreshness.Unknown,
			PerfMeterPlatformTelemetryCollectionResult lastAttemptResult = PerfMeterPlatformTelemetryCollectionResult.NotAttempted,
			bool forcedAtCaptureBoundary = false)
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
			LastAttemptTimeSeconds = SanitizeNonNegative(lastAttemptTimeSeconds);
			LastSuccessTimeSeconds = SanitizeNonNegative(lastSuccessTimeSeconds);
			SampleAgeSeconds = SanitizeNonNegative(sampleAgeSeconds);
			Freshness = freshness;
			LastAttemptResult = lastAttemptResult;
			ForcedAtCaptureBoundary = forcedAtCaptureBoundary;
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
		public double LastAttemptTimeSeconds { get; }
		public double LastSuccessTimeSeconds { get; }
		public double SampleAgeSeconds { get; }
		public PerfMeterPlatformTelemetryFreshness Freshness { get; }
		public PerfMeterPlatformTelemetryCollectionResult LastAttemptResult { get; }
		public bool ForcedAtCaptureBoundary { get; }

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
				Warning,
				LastAttemptTimeSeconds,
				LastSuccessTimeSeconds,
				SampleAgeSeconds,
				Freshness,
				LastAttemptResult,
				ForcedAtCaptureBoundary);
		}

		internal PerfMeterPlatformTelemetrySnapshot WithCollectionMetadata(
			double lastAttemptTimeSeconds,
			double lastSuccessTimeSeconds,
			double sampleAgeSeconds,
			PerfMeterPlatformTelemetryFreshness freshness,
			PerfMeterPlatformTelemetryCollectionResult lastAttemptResult,
			bool forcedAtCaptureBoundary)
		{
			return new PerfMeterPlatformTelemetrySnapshot(
				Availability,
				ProviderId,
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
				Warning,
				lastAttemptTimeSeconds,
				lastSuccessTimeSeconds,
				sampleAgeSeconds,
				freshness,
				lastAttemptResult,
				forcedAtCaptureBoundary);
		}

		private static float Clamp(float value, float minimum, float maximum)
		{
			return value < minimum ? minimum : value > maximum ? maximum : value;
		}

		private static double SanitizeNonNegative(double value)
		{
			return double.IsNaN(value) || double.IsInfinity(value) ? 0d : Math.Max(0d, value);
		}
	}

	internal delegate PerfMeterPlatformTelemetrySnapshot PerfMeterPlatformTelemetryCollector(out PerfMeterPlatformTelemetryCollectionResult result);

	internal sealed class PerfMeterPlatformTelemetrySampler
	{
		internal const double DefaultSampleIntervalSeconds = 0.25d;
		internal const double DefaultStaleAfterSeconds = 1d;

		private readonly double _sampleIntervalSeconds;
		private readonly double _staleAfterSeconds;
		private PerfMeterPlatformTelemetrySnapshot _latest = PerfMeterPlatformTelemetrySnapshot.Unavailable();
		private bool _hasAttempt;
		private bool _hasSuccess;
		private double _lastAttemptTimeSeconds;
		private double _lastSuccessTimeSeconds;
		private PerfMeterPlatformTelemetryCollectionResult _lastAttemptResult;
		private bool _lastAttemptForcedAtCaptureBoundary;

		internal PerfMeterPlatformTelemetrySampler(
			double sampleIntervalSeconds = DefaultSampleIntervalSeconds,
			double staleAfterSeconds = DefaultStaleAfterSeconds)
		{
			_sampleIntervalSeconds = Math.Max(0d, sampleIntervalSeconds);
			_staleAfterSeconds = Math.Max(0d, staleAfterSeconds);
		}

		internal PerfMeterPlatformTelemetrySnapshot Sample(
			double nowSeconds,
			bool force,
			bool captureBoundary,
			PerfMeterPlatformTelemetryCollector collector)
		{
			double now = SanitizeTime(nowSeconds);
			if (!_hasAttempt || force || now - _lastAttemptTimeSeconds >= _sampleIntervalSeconds)
			{
				_latest = collector(out _lastAttemptResult);
				_hasAttempt = true;
				_lastAttemptTimeSeconds = now;
				_lastAttemptForcedAtCaptureBoundary = force && captureBoundary;
				if (_lastAttemptResult == PerfMeterPlatformTelemetryCollectionResult.Collected)
				{
					_hasSuccess = true;
					_lastSuccessTimeSeconds = now;
				}
			}

			return WithCurrentMetadata(now);
		}

		internal void Reset()
		{
			_latest = PerfMeterPlatformTelemetrySnapshot.Unavailable();
			_hasAttempt = false;
			_hasSuccess = false;
			_lastAttemptTimeSeconds = 0d;
			_lastSuccessTimeSeconds = 0d;
			_lastAttemptResult = PerfMeterPlatformTelemetryCollectionResult.NotAttempted;
			_lastAttemptForcedAtCaptureBoundary = false;
		}

		private PerfMeterPlatformTelemetrySnapshot WithCurrentMetadata(double nowSeconds)
		{
			double ageSeconds = _hasSuccess ? Math.Max(0d, nowSeconds - _lastSuccessTimeSeconds) : 0d;
			PerfMeterPlatformTelemetryFreshness freshness = !_hasSuccess
				? PerfMeterPlatformTelemetryFreshness.Unknown
				: ageSeconds > _staleAfterSeconds
					? PerfMeterPlatformTelemetryFreshness.Stale
					: PerfMeterPlatformTelemetryFreshness.Fresh;
			return _latest.WithCollectionMetadata(
				_hasAttempt ? _lastAttemptTimeSeconds : 0d,
				_hasSuccess ? _lastSuccessTimeSeconds : 0d,
				ageSeconds,
				freshness,
				_hasAttempt ? _lastAttemptResult : PerfMeterPlatformTelemetryCollectionResult.NotAttempted,
				_lastAttemptForcedAtCaptureBoundary);
		}

		private static double SanitizeTime(double value)
		{
			return double.IsNaN(value) || double.IsInfinity(value) ? 0d : Math.Max(0d, value);
		}
	}

	internal static class PerfMeterPlatformTelemetryRegistry
	{
		private static readonly object Sync = new object();
		private static readonly object SamplerSync = new object();
		private static readonly PerfMeterPlatformTelemetrySampler Sampler = new PerfMeterPlatformTelemetrySampler();
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

			lock (SamplerSync)
			{
				Sampler.Reset();
			}
		}

		internal static void Unregister(IPerfMeterPlatformTelemetryProvider provider)
		{
			if (provider == null)
			{
				return;
			}

			bool changed = false;
			lock (Sync)
			{
				if (ReferenceEquals(_provider, provider))
				{
					_provider = null;
					changed = true;
				}
			}

			if (changed)
			{
				lock (SamplerSync)
				{
					Sampler.Reset();
				}
			}
		}

		internal static PerfMeterPlatformTelemetrySnapshot Sample(double nowSeconds, bool force = false, bool captureBoundary = false)
		{
			lock (SamplerSync)
			{
				return Sampler.Sample(nowSeconds, force, captureBoundary, CollectProvider);
			}
		}

		private static PerfMeterPlatformTelemetrySnapshot CollectProvider(out PerfMeterPlatformTelemetryCollectionResult result)
		{
			IPerfMeterPlatformTelemetryProvider provider;
			lock (Sync)
			{
				provider = _provider;
			}

			if (provider == null)
			{
				result = PerfMeterPlatformTelemetryCollectionResult.NoProvider;
				return PerfMeterPlatformTelemetrySnapshot.Unavailable();
			}

			string providerId;
			try
			{
				providerId = string.IsNullOrWhiteSpace(provider.Id) ? provider.GetType().FullName : provider.Id.Trim();
			}
			catch (Exception exception)
			{
				result = PerfMeterPlatformTelemetryCollectionResult.ProviderIdentityFailed;
				return PerfMeterPlatformTelemetrySnapshot.Unavailable("Platform telemetry provider identity failed: " + exception.GetType().Name + ": " + exception.Message);
			}

			try
			{
				if (!provider.TryCollect(out PerfMeterPlatformTelemetrySnapshot snapshot))
				{
					result = PerfMeterPlatformTelemetryCollectionResult.ProviderReturnedNoSample;
					return PerfMeterPlatformTelemetrySnapshot.Unavailable("Platform telemetry provider '" + providerId + "' returned no sample.").WithProviderId(providerId);
				}

				result = PerfMeterPlatformTelemetryCollectionResult.Collected;
				return snapshot.WithProviderId(providerId);
			}
			catch (Exception exception)
			{
				result = PerfMeterPlatformTelemetryCollectionResult.ProviderFailed;
				return PerfMeterPlatformTelemetrySnapshot.Unavailable("Platform telemetry provider '" + providerId + "' failed: " + exception.GetType().Name + ": " + exception.Message).WithProviderId(providerId);
			}
		}

		internal static void ClearForTests()
		{
			lock (Sync)
			{
				_provider = null;
			}

			lock (SamplerSync)
			{
				Sampler.Reset();
			}
		}
	}
}
