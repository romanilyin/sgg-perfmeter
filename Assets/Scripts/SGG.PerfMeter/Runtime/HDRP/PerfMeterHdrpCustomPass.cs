using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SGG.PerfMeter
{
	public sealed class PerfMeterHdrpCustomPass : CustomPass
	{
		public const string DefaultMarkerName = "SGG.PerfMeter.HDRP.CustomPass";
		private const string BeforePostProcessInjectionPointName = "BeforePostProcess";
		private const string GpuResidentDrawerDisabledModeName = "Disabled";
		private const string GpuResidentDrawerInstancedDrawingModeName = "InstancedDrawing";
		private const string GpuResidentDrawerUnknownModeName = "Unknown";
		private const string GpuResidentDrawerUnavailableWarning = "The active render pipeline does not expose IGPUResidentRenderPipeline; GPU Resident Drawer context is unavailable.";
		private const string GpuResidentDrawerSupportErrorWarning = "GPU Resident Drawer support query failed for the active HDRP asset.";
		private const string GpuResidentDrawerSupportUnavailableWarning = "GPU Resident Drawer is not supported by the active HDRP asset.";
		private const string GpuResidentDrawerSupportExceptionWarningPrefix = "GPU Resident Drawer support query threw ";

		private static bool _hasCachedGpuResidentDrawerSnapshot;
		private static RenderPipelineAsset _cachedRenderPipelineAsset;
		private static bool _cachedHasGpuResidentRenderPipeline;
		private static bool _cachedGpuResidentDrawerModeReadFailed;
		private static GPUResidentDrawerMode _cachedGpuResidentDrawerMode;
		private static PerfMeterGpuResidentDrawerContextSnapshot _cachedGpuResidentDrawerSnapshot;

		public PerfMeterHdrpCustomPass()
		{
			name = DefaultMarkerName;
			targetColorBuffer = TargetBuffer.Camera;
			targetDepthBuffer = TargetBuffer.None;
			clearFlags = ClearFlag.None;
		}

		protected override bool executeInSceneView => false;

		protected override void Execute(CustomPassContext ctx)
		{
			Camera camera = ctx.hdCamera != null ? ctx.hdCamera.camera : null;
			if (camera == null || camera.cameraType != CameraType.Game)
			{
				return;
			}
			PerfMeterRenderGraphAnalytics.PrepareObservation(camera);
			using PerfMeterSelfObservability.MeasurementScope selfOverheadScope = PerfMeterSelfObservability.Measure(PerfMeterSelfOverheadComponent.HdrpRenderIntegration);

			PerfMeterRenderGraphAnalytics.RecordHdrpCustomPassSnapshot(
				camera,
				null,
				null,
				BeforePostProcessInjectionPointName,
				GetGpuResidentDrawerSnapshot());
		}

		private static PerfMeterGpuResidentDrawerContextSnapshot GetGpuResidentDrawerSnapshot()
		{
			try
			{
				PerfMeterGpuResidentDrawerContextSnapshot supportSnapshot = GetGpuResidentDrawerSupportSnapshot();
				PerfMeterAvailability projectConfigurationAvailability = PerfMeterAvailability.Unknown;
				bool isProjectConfigurationSupported = false;
				PerfMeterAvailability activityAvailability = supportSnapshot.ActivityAvailability;
				bool isObservedActive = supportSnapshot.IsObservedActive;
				string warning = supportSnapshot.Warning;
				if (supportSnapshot.Availability != PerfMeterAvailability.Unavailable)
				{
					try
					{
						isProjectConfigurationSupported = IGPUResidentRenderPipeline.IsGPUResidentDrawerSupportedByProjectConfiguration(false);
						projectConfigurationAvailability = PerfMeterAvailability.Available;
					}
					catch (System.Exception exception)
					{
						projectConfigurationAvailability = PerfMeterAvailability.Unavailable;
						warning = PerfMeterRenderGraphAnalytics.CombineWarnings(warning, "GPU Resident Drawer project-configuration query failed: " + exception.GetType().Name + ".");
					}

					try
					{
						isObservedActive = IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled();
						activityAvailability = PerfMeterAvailability.Available;
					}
					catch (System.Exception exception)
					{
						activityAvailability = PerfMeterAvailability.Unavailable;
						warning = PerfMeterRenderGraphAnalytics.CombineWarnings(warning, "GPU Resident Drawer runtime activity query failed: " + exception.GetType().Name + ".");
					}
				}

				PerfMeterGpuResidentDrawerContextSnapshot primitiveObservation = new PerfMeterGpuResidentDrawerContextSnapshot(
					supportSnapshot.Availability,
					supportSnapshot.ConfiguredMode,
					supportSnapshot.SupportAvailability,
					supportSnapshot.IsSupported,
					activityAvailability,
					isObservedActive,
					warning,
					projectConfigurationAvailability,
					isProjectConfigurationSupported,
					PerfMeterAvailability.Unknown,
					false,
					PerfMeterAvailability.Unknown,
					false,
					PerfMeterAvailability.Unknown,
					false,
					PerfMeterGpuResidentDrawerEffectivenessSnapshot.Unknown,
					PerfMeterGpuResidentDrawerReason.Unknown);
				return primitiveObservation;
			}
			catch (System.Exception exception)
			{
				return new PerfMeterGpuResidentDrawerContextSnapshot(
					PerfMeterAvailability.Available,
					string.Empty,
					PerfMeterAvailability.Unavailable,
					false,
					PerfMeterAvailability.Unknown,
					false,
					GpuResidentDrawerSupportExceptionWarningPrefix + exception.GetType().Name + ".");
			}
		}

		private static PerfMeterGpuResidentDrawerContextSnapshot GetGpuResidentDrawerSupportSnapshot()
		{
			RenderPipelineAsset renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
			IGPUResidentRenderPipeline gpuResidentRenderPipeline = renderPipelineAsset as IGPUResidentRenderPipeline;
			bool hasGpuResidentRenderPipeline = gpuResidentRenderPipeline != null;
			if (_hasCachedGpuResidentDrawerSnapshot &&
				_cachedGpuResidentDrawerModeReadFailed &&
				object.ReferenceEquals(_cachedRenderPipelineAsset, renderPipelineAsset) &&
				_cachedHasGpuResidentRenderPipeline == hasGpuResidentRenderPipeline)
			{
				return _cachedGpuResidentDrawerSnapshot;
			}

			GPUResidentDrawerMode configuredMode = default;
			if (hasGpuResidentRenderPipeline)
			{
				try
				{
					configuredMode = gpuResidentRenderPipeline.gpuResidentDrawerMode;
				}
				catch (System.Exception exception)
				{
					_cachedRenderPipelineAsset = renderPipelineAsset;
					_cachedHasGpuResidentRenderPipeline = true;
					_cachedGpuResidentDrawerModeReadFailed = true;
					_cachedGpuResidentDrawerSnapshot = new PerfMeterGpuResidentDrawerContextSnapshot(
						PerfMeterAvailability.Available,
						string.Empty,
						PerfMeterAvailability.Unavailable,
						false,
						PerfMeterAvailability.Unknown,
						false,
						GpuResidentDrawerSupportExceptionWarningPrefix + exception.GetType().Name + ".");
					_hasCachedGpuResidentDrawerSnapshot = true;
					return _cachedGpuResidentDrawerSnapshot;
				}
			}

			_hasCachedGpuResidentDrawerSnapshot = true;
			_cachedRenderPipelineAsset = renderPipelineAsset;
			_cachedHasGpuResidentRenderPipeline = hasGpuResidentRenderPipeline;
			_cachedGpuResidentDrawerModeReadFailed = false;
			_cachedGpuResidentDrawerMode = configuredMode;

			if (!hasGpuResidentRenderPipeline)
			{
				_cachedGpuResidentDrawerSnapshot = new PerfMeterGpuResidentDrawerContextSnapshot(
					PerfMeterAvailability.Unavailable,
					string.Empty,
					PerfMeterAvailability.Unavailable,
					false,
					PerfMeterAvailability.Unknown,
					false,
					GpuResidentDrawerUnavailableWarning);
				return _cachedGpuResidentDrawerSnapshot;
			}

			bool isSupported = false;
			string supportReason = string.Empty;
			LogType supportSeverity = LogType.Log;
			try
			{
				isSupported = gpuResidentRenderPipeline.IsGPUResidentDrawerSupportedBySRP(out supportReason, out supportSeverity);
			}
			catch (System.Exception exception)
			{
				_cachedGpuResidentDrawerSnapshot = new PerfMeterGpuResidentDrawerContextSnapshot(
					PerfMeterAvailability.Available,
					GetGpuResidentDrawerModeName(configuredMode),
					PerfMeterAvailability.Unavailable,
					false,
					PerfMeterAvailability.Unknown,
					false,
					GpuResidentDrawerSupportExceptionWarningPrefix + exception.GetType().Name + ".");
				return _cachedGpuResidentDrawerSnapshot;
			}

			_cachedGpuResidentDrawerSnapshot = new PerfMeterGpuResidentDrawerContextSnapshot(
				PerfMeterAvailability.Available,
				GetGpuResidentDrawerModeName(configuredMode),
				PerfMeterAvailability.Available,
				isSupported,
				PerfMeterAvailability.Unknown,
				false,
				GetGpuResidentDrawerWarning(isSupported, supportReason, supportSeverity));
			return _cachedGpuResidentDrawerSnapshot;
		}

		private static string GetGpuResidentDrawerModeName(GPUResidentDrawerMode mode)
		{
			switch (mode)
			{
				case GPUResidentDrawerMode.Disabled:
					return GpuResidentDrawerDisabledModeName;
				case GPUResidentDrawerMode.InstancedDrawing:
					return GpuResidentDrawerInstancedDrawingModeName;
				default:
					return GpuResidentDrawerUnknownModeName;
			}
		}

		private static string GetGpuResidentDrawerWarning(bool isSupported, string reason, LogType severity)
		{
			if (isSupported)
			{
				return string.Empty;
			}

			if (!string.IsNullOrEmpty(reason))
			{
				return reason;
			}

			return severity == LogType.Error
				? GpuResidentDrawerSupportErrorWarning
				: GpuResidentDrawerSupportUnavailableWarning;
		}
	}
}
