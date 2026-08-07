using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	public enum PerfMeterRenderIntegrationState
	{
		NotObserved = 0,
		Observed = 1,
		Unsupported = 2
	}

	public enum PerfMeterRenderPipelineAssetSource
	{
		None = 0,
		GraphicsSettings = 1,
		QualitySettings = 2
	}

	public enum PerfMeterRenderPassKind
	{
		Unknown = 0,
		RenderGraphRaster = 1,
		CustomPass = 2
	}

	public enum PerfMeterGpuResidentDrawerReason
	{
		Unknown = 0,
		None = 1,
		NotObserved = 2,
		PipelineUnavailable = 3,
		ModeDisabled = 4,
		SrpUnsupported = 5,
		ProjectConfigurationUnsupported = 6,
		ComputeShadersUnsupported = 7,
		IncompatibleRenderingMode = 8,
		RuntimeInactive = 9,
		QueryFailed = 10
	}

	public readonly struct PerfMeterGpuResidentDrawerEffectivenessSnapshot
	{
		public const int UnavailableCount = -1;
		public const string AggregateScope = "brg_aggregate";
		public const string AggregateWarning = "Counters are aggregate BatchRendererGroup telemetry and are not authoritative GPU Resident Drawer or per-renderer effectiveness.";

		public PerfMeterGpuResidentDrawerEffectivenessSnapshot(
			PerfMeterAvailability availability,
			int collectionFrame,
			int brgDrawCalls,
			int brgInstances,
			PerfMeterProfilerMetricCapabilitySnapshot brgDrawCallsCapability,
			PerfMeterProfilerMetricCapabilitySnapshot brgInstancesCapability,
			string warning)
		{
			Availability = availability;
			CollectionFrame = collectionFrame;
			BrgDrawCallsCapability = NormalizeCapability(brgDrawCallsCapability, PerfMeterProfilerMetricSemantic.BrgDrawCalls);
			BrgInstancesCapability = NormalizeCapability(brgInstancesCapability, PerfMeterProfilerMetricSemantic.BrgInstances);
			BrgDrawCalls = IsSampled(BrgDrawCallsCapability) ? brgDrawCalls : UnavailableCount;
			BrgInstances = IsSampled(BrgInstancesCapability) ? brgInstances : UnavailableCount;
			HasSample = IsSampled(BrgDrawCallsCapability) || IsSampled(BrgInstancesCapability);
			HasObservedBrgWorkload = (IsSampled(BrgDrawCallsCapability) && BrgDrawCalls > 0) ||
				(IsSampled(BrgInstancesCapability) && BrgInstances > 0);
			Warning = string.IsNullOrEmpty(warning) ? AggregateWarning : warning;
		}

		public static PerfMeterGpuResidentDrawerEffectivenessSnapshot Unknown => new PerfMeterGpuResidentDrawerEffectivenessSnapshot(
			PerfMeterAvailability.Unknown,
			-1,
			UnavailableCount,
			UnavailableCount,
			UnavailableCapability(PerfMeterProfilerMetricSemantic.BrgDrawCalls),
			UnavailableCapability(PerfMeterProfilerMetricSemantic.BrgInstances),
			"BRG aggregate effectiveness was not sampled because the PerfMeter runtime is not running.");

		public PerfMeterAvailability Availability { get; }
		public int CollectionFrame { get; }
		public int BrgDrawCalls { get; }
		public int BrgInstances { get; }
		public PerfMeterProfilerMetricCapabilitySnapshot BrgDrawCallsCapability { get; }
		public PerfMeterProfilerMetricCapabilitySnapshot BrgInstancesCapability { get; }
		public bool HasSample { get; }
		public bool HasObservedBrgWorkload { get; }
		public string Scope => AggregateScope;
		public string Warning { get; }

		private static bool IsSampled(PerfMeterProfilerMetricCapabilitySnapshot capability)
		{
			return capability.SampleState == PerfMeterProfilerMetricSampleState.AvailableSampled;
		}

		private static PerfMeterProfilerMetricCapabilitySnapshot NormalizeCapability(
			PerfMeterProfilerMetricCapabilitySnapshot capability,
			PerfMeterProfilerMetricSemantic semantic)
		{
			return capability.Semantic == semantic ? capability : UnavailableCapability(semantic);
		}

		private static PerfMeterProfilerMetricCapabilitySnapshot UnavailableCapability(PerfMeterProfilerMetricSemantic semantic)
		{
			return new PerfMeterProfilerMetricCapabilitySnapshot(
				semantic,
				PerfMeterProfilerMetricSampleState.Unavailable,
				PerfMeterProfilerMetricResolution.None,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				0,
				0);
		}
	}

	public readonly struct PerfMeterGpuResidentDrawerContextSnapshot
	{
		public PerfMeterGpuResidentDrawerContextSnapshot(
			PerfMeterAvailability availability,
			string configuredMode,
			PerfMeterAvailability supportAvailability,
			bool isSupported,
			PerfMeterAvailability activityAvailability,
			bool isObservedActive,
			string warning)
			: this(
				availability,
				configuredMode,
				supportAvailability,
				isSupported,
				activityAvailability,
				isObservedActive,
				warning,
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterGpuResidentDrawerEffectivenessSnapshot.Unknown,
				PerfMeterGpuResidentDrawerReason.Unknown)
		{
			ActivitySource = string.Empty;
		}

		public PerfMeterGpuResidentDrawerContextSnapshot(
			PerfMeterAvailability availability,
			string configuredMode,
			PerfMeterAvailability supportAvailability,
			bool isSupported,
			PerfMeterAvailability activityAvailability,
			bool isObservedActive,
			string warning,
			PerfMeterAvailability projectConfigurationAvailability,
			bool isProjectConfigurationSupported,
			PerfMeterAvailability computeShaderAvailability,
			bool supportsComputeShaders,
			PerfMeterAvailability forwardPlusActivityAvailability,
			bool isObservedForwardPlusActive,
			PerfMeterAvailability renderingModeCompatibilityAvailability,
			bool isRenderingModeCompatible,
			PerfMeterGpuResidentDrawerEffectivenessSnapshot effectiveness,
			PerfMeterGpuResidentDrawerReason degradedReason)
		{
			Availability = availability;
			ConfiguredMode = configuredMode ?? string.Empty;
			SupportAvailability = supportAvailability;
			IsSupported = isSupported;
			ActivityAvailability = activityAvailability;
			IsObservedActive = isObservedActive;
			Warning = warning ?? string.Empty;
			ProjectConfigurationAvailability = projectConfigurationAvailability;
			IsProjectConfigurationSupported = isProjectConfigurationSupported;
			ComputeShaderAvailability = computeShaderAvailability;
			SupportsComputeShaders = supportsComputeShaders;
			ForwardPlusActivityAvailability = forwardPlusActivityAvailability;
			IsObservedForwardPlusActive = isObservedForwardPlusActive;
			RenderingModeCompatibilityAvailability = renderingModeCompatibilityAvailability;
			IsRenderingModeCompatible = isRenderingModeCompatible;
			ActivitySource = activityAvailability != PerfMeterAvailability.Unknown ? UnityRuntimeActivitySource : string.Empty;
			Effectiveness = effectiveness.BrgDrawCallsCapability.Semantic == PerfMeterProfilerMetricSemantic.BrgDrawCalls &&
				effectiveness.BrgInstancesCapability.Semantic == PerfMeterProfilerMetricSemantic.BrgInstances
				? effectiveness
				: PerfMeterGpuResidentDrawerEffectivenessSnapshot.Unknown;
			DegradedReason = degradedReason;
		}

		public static PerfMeterGpuResidentDrawerContextSnapshot Unknown => new PerfMeterGpuResidentDrawerContextSnapshot(
			PerfMeterAvailability.Unknown,
			string.Empty,
			PerfMeterAvailability.Unknown,
			false,
			PerfMeterAvailability.Unknown,
			false,
			"GPU Resident Drawer context has not been reported by the active render integration.",
			PerfMeterAvailability.Unknown,
			false,
			PerfMeterAvailability.Unknown,
			false,
			PerfMeterAvailability.Unknown,
			false,
			PerfMeterAvailability.Unknown,
			false,
			PerfMeterGpuResidentDrawerEffectivenessSnapshot.Unknown,
			PerfMeterGpuResidentDrawerReason.NotObserved);

		public PerfMeterAvailability Availability { get; }
		public string ConfiguredMode { get; }
		public bool IsConfigured => Availability == PerfMeterAvailability.Available &&
			!string.IsNullOrEmpty(ConfiguredMode) &&
			!string.Equals(ConfiguredMode, "Disabled", StringComparison.Ordinal) &&
			!string.Equals(ConfiguredMode, "Unknown", StringComparison.Ordinal);
		public PerfMeterAvailability SupportAvailability { get; }
		public bool IsSupported { get; }
		public PerfMeterAvailability ActivityAvailability { get; }
		public bool IsObservedActive { get; }
		public PerfMeterAvailability ProjectConfigurationAvailability { get; }
		public bool IsProjectConfigurationSupported { get; }
		public PerfMeterAvailability ComputeShaderAvailability { get; }
		public bool SupportsComputeShaders { get; }
		public PerfMeterAvailability ForwardPlusActivityAvailability { get; }
		public bool IsObservedForwardPlusActive { get; }
		public PerfMeterAvailability RenderingModeCompatibilityAvailability { get; }
		public bool IsRenderingModeCompatible { get; }
		public const string UnityRuntimeActivitySource = "IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled";
		public string ActivitySource { get; }
		public PerfMeterGpuResidentDrawerEffectivenessSnapshot Effectiveness { get; }
		public PerfMeterGpuResidentDrawerReason DegradedReason { get; }
		public string Warning { get; }
	}

	public readonly struct PerfMeterVariableRateShadingContextSnapshot
	{
		public PerfMeterVariableRateShadingContextSnapshot(
			PerfMeterAvailability availability,
			bool supportsVariableRateShading,
			bool supportsPerDrawCall,
			bool supportsPerImageTile,
			int imageTileWidth,
			int imageTileHeight,
			string graphicsFormat,
			PerfMeterAvailability configurationAvailability,
			bool isConfigured,
			PerfMeterAvailability activityAvailability,
			bool isObservedActive,
			string warning)
		{
			Availability = availability;
			SupportsVariableRateShading = supportsVariableRateShading;
			SupportsPerDrawCall = supportsPerDrawCall;
			SupportsPerImageTile = supportsPerImageTile;
			ImageTileWidth = Mathf.Max(0, imageTileWidth);
			ImageTileHeight = Mathf.Max(0, imageTileHeight);
			GraphicsFormat = graphicsFormat ?? string.Empty;
			ConfigurationAvailability = configurationAvailability;
			IsConfigured = isConfigured;
			ActivityAvailability = activityAvailability;
			IsObservedActive = isObservedActive;
			Warning = warning ?? string.Empty;
		}

		public static PerfMeterVariableRateShadingContextSnapshot Unknown => new PerfMeterVariableRateShadingContextSnapshot(
			PerfMeterAvailability.Unknown,
			false,
			false,
			false,
			0,
			0,
			string.Empty,
			PerfMeterAvailability.Unknown,
			false,
			PerfMeterAvailability.Unknown,
			false,
			"Variable Rate Shading capabilities are unavailable on this Unity version.");

		public PerfMeterAvailability Availability { get; }
		public bool SupportsVariableRateShading { get; }
		public bool SupportsPerDrawCall { get; }
		public bool SupportsPerImageTile { get; }
		public int ImageTileWidth { get; }
		public int ImageTileHeight { get; }
		public string GraphicsFormat { get; }
		public PerfMeterAvailability ConfigurationAvailability { get; }
		public bool IsConfigured { get; }
		public PerfMeterAvailability ActivityAvailability { get; }
		public bool IsObservedActive { get; }
		public string Warning { get; }
	}

	public readonly struct PerfMeterRenderIntegrationSnapshot
	{
		public PerfMeterRenderIntegrationSnapshot(
			PerfMeterAvailability availability,
			PerfMeterRenderIntegrationState state,
			PerfMeterRenderPipelineSnapshot renderPipeline,
			PerfMeterRenderPipelineAssetSource renderPipelineAssetSource,
			int lastObservedFrame,
			int observationAgeFrames,
			bool observationMatchesCurrentPipeline,
			ulong observedCameraEntityId,
			string observedCameraName,
			string observedCameraType,
			string integrationId,
			string integrationName,
			string integrationVersion,
			PerfMeterRenderPassKind passKind,
			string passName,
			string injectionPoint,
			int perfMeterPassCount,
			string effectiveRenderingMode,
			PerfMeterGpuResidentDrawerContextSnapshot gpuResidentDrawer,
			PerfMeterVariableRateShadingContextSnapshot variableRateShading,
			PerfMeterRenderGraphSnapshot legacyRenderGraph,
			string warning)
		{
			Availability = availability;
			State = state;
			RenderPipeline = renderPipeline;
			RenderPipelineAssetSource = renderPipelineAssetSource;
			LastObservedFrame = lastObservedFrame;
			ObservationAgeFrames = observationAgeFrames;
			ObservationMatchesCurrentPipeline = observationMatchesCurrentPipeline;
			ObservedCameraEntityId = observedCameraEntityId;
			ObservedCameraName = observedCameraName ?? string.Empty;
			ObservedCameraType = observedCameraType ?? string.Empty;
			IntegrationId = integrationId ?? string.Empty;
			IntegrationName = integrationName ?? string.Empty;
			IntegrationVersion = integrationVersion ?? string.Empty;
			PassKind = passKind;
			PassName = passName ?? string.Empty;
			InjectionPoint = injectionPoint ?? string.Empty;
			PerfMeterPassCount = Mathf.Max(0, perfMeterPassCount);
			EffectiveRenderingMode = effectiveRenderingMode ?? string.Empty;
			GpuResidentDrawer = gpuResidentDrawer;
			VariableRateShading = variableRateShading;
			LegacyRenderGraph = legacyRenderGraph;
			Warning = warning ?? string.Empty;
		}

		public static PerfMeterRenderIntegrationSnapshot NotObserved => new PerfMeterRenderIntegrationSnapshot(
			PerfMeterAvailability.Unknown,
			PerfMeterRenderIntegrationState.NotObserved,
			default,
			PerfMeterRenderPipelineAssetSource.None,
			-1,
			-1,
			false,
			0UL,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			PerfMeterRenderPassKind.Unknown,
			string.Empty,
			string.Empty,
			0,
			string.Empty,
			PerfMeterGpuResidentDrawerContextSnapshot.Unknown,
			PerfMeterVariableRateShadingContextSnapshot.Unknown,
			PerfMeterRenderGraphSnapshot.NotObserved,
			"PerfMeter render integration has not recorded a frame yet.");

		public bool IsAvailable => Availability == PerfMeterAvailability.Available;
		public PerfMeterAvailability Availability { get; }
		public PerfMeterRenderIntegrationState State { get; }
		public PerfMeterRenderPipelineSnapshot RenderPipeline { get; }
		public PerfMeterRenderPipelineAssetSource RenderPipelineAssetSource { get; }
		public int LastObservedFrame { get; }
		public int ObservationAgeFrames { get; }
		public bool ObservationMatchesCurrentPipeline { get; }
		public ulong ObservedCameraEntityId { get; }
		public string ObservedCameraName { get; }
		public string ObservedCameraType { get; }
		public string IntegrationId { get; }
		public string IntegrationName { get; }
		public string IntegrationVersion { get; }
		public PerfMeterRenderPassKind PassKind { get; }
		public string PassName { get; }
		public string InjectionPoint { get; }
		public int PerfMeterPassCount { get; }
		public string EffectiveRenderingMode { get; }
		public PerfMeterGpuResidentDrawerContextSnapshot GpuResidentDrawer { get; }
		public PerfMeterVariableRateShadingContextSnapshot VariableRateShading { get; }
		public PerfMeterRenderGraphSnapshot LegacyRenderGraph { get; }
		public string Warning { get; }
	}

	internal readonly struct PerfMeterRenderIntegrationObservation
	{
		internal PerfMeterRenderIntegrationObservation(
			PerfMeterRenderPipelineSnapshot renderPipeline,
			PerfMeterRenderPipelineAssetSource renderPipelineAssetSource,
			ulong renderPipelineAssetEntityId,
			int frame,
			ulong cameraEntityId,
			string cameraName,
			string cameraType,
			string integrationId,
			string integrationName,
			string integrationVersion,
			PerfMeterRenderPassKind passKind,
			string passName,
			string injectionPoint,
			int perfMeterPassCount,
			string effectiveRenderingMode,
			PerfMeterGpuResidentDrawerContextSnapshot gpuResidentDrawer,
			PerfMeterAvailability vrsConfigurationAvailability,
			bool vrsConfigured,
			PerfMeterAvailability vrsActivityAvailability,
			bool vrsObservedActive,
			string warning)
		{
			RenderPipeline = renderPipeline;
			RenderPipelineAssetSource = renderPipelineAssetSource;
			RenderPipelineAssetEntityId = renderPipelineAssetEntityId;
			Frame = frame;
			CameraEntityId = cameraEntityId;
			CameraName = cameraName ?? string.Empty;
			CameraType = cameraType ?? string.Empty;
			IntegrationId = integrationId ?? string.Empty;
			IntegrationName = integrationName ?? string.Empty;
			IntegrationVersion = integrationVersion ?? string.Empty;
			PassKind = passKind;
			PassName = passName ?? string.Empty;
			InjectionPoint = injectionPoint ?? string.Empty;
			PerfMeterPassCount = Mathf.Max(0, perfMeterPassCount);
			EffectiveRenderingMode = effectiveRenderingMode ?? string.Empty;
			GpuResidentDrawer = gpuResidentDrawer;
			VrsConfigurationAvailability = vrsConfigurationAvailability;
			VrsConfigured = vrsConfigured;
			VrsActivityAvailability = vrsActivityAvailability;
			VrsObservedActive = vrsObservedActive;
			Warning = warning ?? string.Empty;
		}

		internal bool IsObserved => Frame >= 0 && !string.IsNullOrEmpty(IntegrationId);
		internal PerfMeterRenderPipelineSnapshot RenderPipeline { get; }
		internal PerfMeterRenderPipelineAssetSource RenderPipelineAssetSource { get; }
		internal ulong RenderPipelineAssetEntityId { get; }
		internal int Frame { get; }
		internal ulong CameraEntityId { get; }
		internal string CameraName { get; }
		internal string CameraType { get; }
		internal string IntegrationId { get; }
		internal string IntegrationName { get; }
		internal string IntegrationVersion { get; }
		internal PerfMeterRenderPassKind PassKind { get; }
		internal string PassName { get; }
		internal string InjectionPoint { get; }
		internal int PerfMeterPassCount { get; }
		internal string EffectiveRenderingMode { get; }
		internal PerfMeterGpuResidentDrawerContextSnapshot GpuResidentDrawer { get; }
		internal PerfMeterAvailability VrsConfigurationAvailability { get; }
		internal bool VrsConfigured { get; }
		internal PerfMeterAvailability VrsActivityAvailability { get; }
		internal bool VrsObservedActive { get; }
		internal string Warning { get; }
	}
}
