using System;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.AdaptivePerformance.Provider;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterAdaptivePerformanceTelemetryProvider : IPerfMeterPlatformTelemetryProvider
	{
		internal const string ProviderId = "com.unity.adaptiveperformance";
		private static readonly string ProviderVersion = typeof(IAdaptivePerformance).Assembly.GetName().Version?.ToString() ?? string.Empty;
		private bool _hasSample;
		private PerfMeterThermalWarningLevel _warningLevel;
		private float _temperatureLevel;
		private float _temperatureTrend;
		private int _cpuLevel;
		private int _gpuLevel;
		private PerfMeterAdaptiveBottleneck _bottleneck;
		private bool _warningAvailable;
		private bool _temperatureLevelAvailable;
		private bool _temperatureTrendAvailable;
		private bool _cpuLevelAvailable;
		private bool _gpuLevelAvailable;
		private bool _bottleneckAvailable;
		private double _lastChangeTimeSeconds;

		public string Id => ProviderId;

		public bool TryCollect(out PerfMeterPlatformTelemetrySnapshot snapshot)
		{
			IAdaptivePerformance adaptivePerformance = Holder.Instance;
			double now = Time.realtimeSinceStartupAsDouble;
			if (adaptivePerformance == null || !adaptivePerformance.Initialized || !adaptivePerformance.Active)
			{
				_hasSample = false;
				snapshot = CreateUnavailable(now, "Unity Adaptive Performance is not initialized and active on this device.");
				return true;
			}

			ThermalMetrics thermal = adaptivePerformance.ThermalStatus != null ? adaptivePerformance.ThermalStatus.ThermalMetrics : default;
			PerformanceMetrics performance = adaptivePerformance.PerformanceStatus != null ? adaptivePerformance.PerformanceStatus.PerformanceMetrics : default;
			bool warningAvailable = adaptivePerformance.SupportedFeature(Feature.WarningLevel) && adaptivePerformance.ThermalStatus != null;
			bool temperatureLevelAvailable = adaptivePerformance.SupportedFeature(Feature.TemperatureLevel) && adaptivePerformance.ThermalStatus != null;
			bool temperatureTrendAvailable = adaptivePerformance.SupportedFeature(Feature.TemperatureTrend) && adaptivePerformance.ThermalStatus != null;
			bool cpuLevelAvailable = adaptivePerformance.SupportedFeature(Feature.CpuPerformanceLevel) && adaptivePerformance.PerformanceStatus != null && performance.CurrentCpuLevel >= 0;
			bool gpuLevelAvailable = adaptivePerformance.SupportedFeature(Feature.GpuPerformanceLevel) && adaptivePerformance.PerformanceStatus != null && performance.CurrentGpuLevel >= 0;
			bool bottleneckAvailable = adaptivePerformance.PerformanceStatus != null;
			PerfMeterThermalWarningLevel warningLevel = warningAvailable ? MapWarningLevel(thermal.WarningLevel) : PerfMeterThermalWarningLevel.Unknown;
			float temperatureLevel = temperatureLevelAvailable ? thermal.TemperatureLevel : 0f;
			float temperatureTrend = temperatureTrendAvailable ? thermal.TemperatureTrend : 0f;
			int cpuLevel = cpuLevelAvailable ? performance.CurrentCpuLevel : 0;
			int gpuLevel = gpuLevelAvailable ? performance.CurrentGpuLevel : 0;
			PerfMeterAdaptiveBottleneck bottleneck = bottleneckAvailable ? MapBottleneck(performance.PerformanceBottleneck) : PerfMeterAdaptiveBottleneck.Unknown;

			if (!_hasSample ||
				warningAvailable != _warningAvailable ||
				temperatureLevelAvailable != _temperatureLevelAvailable ||
				temperatureTrendAvailable != _temperatureTrendAvailable ||
				cpuLevelAvailable != _cpuLevelAvailable ||
				gpuLevelAvailable != _gpuLevelAvailable ||
				bottleneckAvailable != _bottleneckAvailable ||
				warningLevel != _warningLevel ||
				!Approximately(temperatureLevel, _temperatureLevel) ||
				!Approximately(temperatureTrend, _temperatureTrend) ||
				cpuLevel != _cpuLevel ||
				gpuLevel != _gpuLevel ||
				bottleneck != _bottleneck)
			{
				_lastChangeTimeSeconds = now;
			}

			_hasSample = true;
			_warningLevel = warningLevel;
			_temperatureLevel = temperatureLevel;
			_temperatureTrend = temperatureTrend;
			_cpuLevel = cpuLevel;
			_gpuLevel = gpuLevel;
			_bottleneck = bottleneck;
			_warningAvailable = warningAvailable;
			_temperatureLevelAvailable = temperatureLevelAvailable;
			_temperatureTrendAvailable = temperatureTrendAvailable;
			_cpuLevelAvailable = cpuLevelAvailable;
			_gpuLevelAvailable = gpuLevelAvailable;
			_bottleneckAvailable = bottleneckAvailable;
			snapshot = new PerfMeterPlatformTelemetrySnapshot(
				PerfMeterAvailability.Available,
				ProviderId,
				ProviderVersion,
				now,
				_lastChangeTimeSeconds,
				warningAvailable,
				warningLevel,
				temperatureLevelAvailable,
				temperatureLevel,
				temperatureTrendAvailable,
				temperatureTrend,
				cpuLevelAvailable,
				cpuLevel,
				gpuLevelAvailable,
				gpuLevel,
				bottleneckAvailable,
				bottleneck,
				warningAvailable || temperatureLevelAvailable || temperatureTrendAvailable || cpuLevelAvailable || gpuLevelAvailable || bottleneckAvailable
					? string.Empty
					: "The active Adaptive Performance provider exposes no supported telemetry fields.");
			return true;
		}

		private static PerfMeterPlatformTelemetrySnapshot CreateUnavailable(double now, string warning)
		{
			return new PerfMeterPlatformTelemetrySnapshot(
				PerfMeterAvailability.Unavailable,
				ProviderId,
				ProviderVersion,
				now,
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

		private static PerfMeterThermalWarningLevel MapWarningLevel(WarningLevel warningLevel)
		{
			switch (warningLevel)
			{
				case WarningLevel.NoWarning: return PerfMeterThermalWarningLevel.Nominal;
				case WarningLevel.ThrottlingImminent: return PerfMeterThermalWarningLevel.ThrottlingImminent;
				case WarningLevel.Throttling: return PerfMeterThermalWarningLevel.Throttling;
				default: return PerfMeterThermalWarningLevel.Unknown;
			}
		}

		private static PerfMeterAdaptiveBottleneck MapBottleneck(PerformanceBottleneck bottleneck)
		{
			switch (bottleneck)
			{
				case PerformanceBottleneck.CPU: return PerfMeterAdaptiveBottleneck.Cpu;
				case PerformanceBottleneck.GPU: return PerfMeterAdaptiveBottleneck.Gpu;
				case PerformanceBottleneck.TargetFrameRate: return PerfMeterAdaptiveBottleneck.TargetFrameRate;
				default: return PerfMeterAdaptiveBottleneck.Unknown;
			}
		}

		private static bool Approximately(float left, float right)
		{
			return Math.Abs(left - right) < 0.0001f;
		}
	}

	internal static class PerfMeterAdaptivePerformanceBootstrap
	{
		private static PerfMeterAdaptivePerformanceTelemetryProvider _provider;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			if (_provider != null)
			{
				PerformanceMeter.UnregisterPlatformTelemetryProvider(_provider);
				_provider = null;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Register()
		{
			if (_provider == null)
			{
				_provider = new PerfMeterAdaptivePerformanceTelemetryProvider();
			}

			PerformanceMeter.RegisterPlatformTelemetryProvider(_provider);
		}
	}
}
