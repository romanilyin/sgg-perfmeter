using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal static class PerfMeterRenderGraphAnalytics
	{
		private const string RenderGraphFeatureFullName = "SGG.PerfMeter.PerfMeterRenderGraphFeature";
		private const string RenderGraphFeatureAssemblyName = "SGG.PerfMeter.URP";
		private const string HdrpCustomPassFullName = "SGG.PerfMeter.PerfMeterHdrpCustomPass";
		private const string HdrpCustomPassAssemblyName = "SGG.PerfMeter.HDRP";
		private static PerfMeterRenderGraphSnapshot _latestSnapshot = PerfMeterRenderGraphSnapshot.NotObserved;
		private static PerfMeterRenderIntegrationObservation _latestObservation;
		private static string _renderGraphFeatureVersion;
		private static string _hdrpCustomPassVersion;
		private static string _latestObservationFailureWarning = string.Empty;
		private static bool _hasPreparedObservation;
		private static PerfMeterRenderPipelineSnapshot _preparedPipeline;
		private static PerfMeterRenderPipelineAssetSource _preparedPipelineSource;
		private static ulong _preparedPipelineAssetEntityId;
		private static ulong _preparedCameraEntityId;
		private static string _preparedCameraName = string.Empty;
		private static string _preparedCameraType = string.Empty;

		internal static PerfMeterRenderGraphSnapshot GetSnapshot()
		{
			return _latestSnapshot;
		}

		internal static PerfMeterRenderIntegrationSnapshot GetRenderIntegrationSnapshot()
		{
			try
			{
				return CreateRenderIntegrationSnapshot();
			}
			catch (Exception exception)
			{
				return new PerfMeterRenderIntegrationSnapshot(
					PerfMeterAvailability.Unavailable,
					PerfMeterRenderIntegrationState.Unsupported,
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
					_latestSnapshot,
					"Render integration snapshot query failed: " + exception.GetType().Name + ".");
			}
		}

		private static PerfMeterRenderIntegrationSnapshot CreateRenderIntegrationSnapshot()
		{
			PerfMeterRenderPipelineSnapshot currentPipeline = PerfMeterRenderPipelineDetector.CreateSnapshot(
				out PerfMeterRenderPipelineAssetSource currentSource,
				out ulong currentPipelineAssetEntityId);
			bool supportedPipeline = currentPipeline.Kind == PerfMeterRenderPipelineKind.Universal || currentPipeline.Kind == PerfMeterRenderPipelineKind.HighDefinition;
			bool matchesCurrentPipeline = _latestObservation.IsObserved && PipelinesMatch(_latestObservation, currentPipeline, currentPipelineAssetEntityId);
			PerfMeterAvailability availability = supportedPipeline
				? PerfMeterAvailability.Available
				: currentPipeline.Kind == PerfMeterRenderPipelineKind.Unknown
					? PerfMeterAvailability.Unknown
					: PerfMeterAvailability.Unavailable;
			PerfMeterRenderIntegrationState state = supportedPipeline
				? matchesCurrentPipeline ? PerfMeterRenderIntegrationState.Observed : PerfMeterRenderIntegrationState.NotObserved
				: PerfMeterRenderIntegrationState.Unsupported;
			int observationAgeFrames = _latestObservation.IsObserved ? Math.Max(0, Time.frameCount - _latestObservation.Frame) : -1;
			PerfMeterVariableRateShadingContextSnapshot variableRateShading = CreateVariableRateShadingSnapshot(matchesCurrentPipeline);
			string warning = GetSnapshotWarning(supportedPipeline, matchesCurrentPipeline);

			return new PerfMeterRenderIntegrationSnapshot(
				availability,
				state,
				currentPipeline,
				currentSource,
				_latestObservation.IsObserved ? _latestObservation.Frame : -1,
				observationAgeFrames,
				matchesCurrentPipeline,
				_latestObservation.CameraEntityId,
				_latestObservation.CameraName,
				_latestObservation.CameraType,
				_latestObservation.IntegrationId,
				_latestObservation.IntegrationName,
				_latestObservation.IntegrationVersion,
				_latestObservation.PassKind,
				_latestObservation.PassName,
				_latestObservation.InjectionPoint,
				_latestObservation.PerfMeterPassCount,
				_latestObservation.EffectiveRenderingMode,
				matchesCurrentPipeline ? _latestObservation.GpuResidentDrawer : PerfMeterGpuResidentDrawerContextSnapshot.Unknown,
				variableRateShading,
				_latestSnapshot,
				CombineWarnings(CombineWarnings(warning, _latestObservationFailureWarning), _latestObservation.Warning));
		}

		internal static void ResetForTests()
		{
			_latestSnapshot = PerfMeterRenderGraphSnapshot.NotObserved;
			_latestObservation = default;
			_latestObservationFailureWarning = string.Empty;
			_hasPreparedObservation = false;
			_preparedPipeline = default;
			_preparedPipelineSource = PerfMeterRenderPipelineAssetSource.None;
			_preparedPipelineAssetEntityId = 0UL;
			_preparedCameraEntityId = 0UL;
			_preparedCameraName = string.Empty;
			_preparedCameraType = string.Empty;
		}

		internal static void PrepareObservation(Camera camera)
		{
			try
			{
				_preparedPipeline = PerfMeterRenderPipelineDetector.CreateSnapshot(
					out _preparedPipelineSource,
					out _preparedPipelineAssetEntityId);
				ulong cameraEntityId = GetCameraEntityId(camera);
				if (!_hasPreparedObservation || _preparedCameraEntityId != cameraEntityId)
				{
					_preparedCameraEntityId = cameraEntityId;
					_preparedCameraName = GetCameraName(camera);
					_preparedCameraType = GetCameraType(camera);
				}
				if (_preparedPipeline.Kind == PerfMeterRenderPipelineKind.Universal)
				{
					GetAssemblyVersion(RenderGraphFeatureAssemblyName);
				}
				else if (_preparedPipeline.Kind == PerfMeterRenderPipelineKind.HighDefinition)
				{
					GetAssemblyVersion(HdrpCustomPassAssemblyName);
				}

				_hasPreparedObservation = true;
			}
			catch (Exception)
			{
				_hasPreparedObservation = false;
			}
		}

		internal static bool IsRenderGraphFeatureAvailable()
		{
			return GetRenderGraphFeatureType() != null;
		}

		internal static bool IsHdrpCustomPassAvailable()
		{
			return GetHdrpCustomPassType() != null;
		}

		internal static void RecordFeatureSnapshot(
			Camera camera,
			string effectiveRenderingMode,
			string injectionPoint,
			PerfMeterGpuResidentDrawerContextSnapshot gpuResidentDrawer,
			bool recordsOverlayMarkerPass,
			bool recordsOverdrawCounterPass,
			bool recordsOverdrawHeatmapPass)
		{
			try
			{
				int perfMeterPassCount = GetPerfMeterPassCount(recordsOverlayMarkerPass, recordsOverdrawCounterPass, recordsOverdrawHeatmapPass);
				const string warning = "Unity RenderGraph internal pass/resource counters are not exposed through a stable public API; only PerfMeter-owned pass observation is available.";
				string cameraName = GetPreparedCameraName(camera);
				string cameraType = GetPreparedCameraType(camera);
				_latestSnapshot = new PerfMeterRenderGraphSnapshot(
					PerfMeterAvailability.Available,
					PerfMeterRenderGraphState.Observed,
					Time.frameCount,
					cameraName,
					cameraType,
					perfMeterPassCount,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					warning,
					PerfMeterRenderPipelineKind.Universal,
					"URP Render Graph Feature",
					string.Empty);
				RecordObservation(
					camera,
					PerfMeterRenderPipelineKind.Universal,
					"sgg.perfmeter.urp.render-graph",
					"URP Render Graph Feature",
					RenderGraphFeatureAssemblyName,
					PerfMeterRenderPassKind.RenderGraphRaster,
					"SGG PerfMeter Render Graph Pass",
					injectionPoint,
					perfMeterPassCount,
					effectiveRenderingMode,
					gpuResidentDrawer,
					warning,
					cameraName,
					cameraType);
				_latestObservationFailureWarning = string.Empty;
			}
			catch (Exception exception)
			{
				RecordFailure(PerfMeterRenderPipelineKind.Universal, "URP render integration observation failed: " + exception.GetType().Name + ".");
			}
		}

		internal static void RecordHdrpCustomPassSnapshot(string observedCameraName, string observedCameraType, string observedInjectionPoint)
		{
			RecordHdrpCustomPassSnapshot(null, observedCameraName, observedCameraType, observedInjectionPoint, PerfMeterGpuResidentDrawerContextSnapshot.Unknown);
		}

		internal static void RecordHdrpCustomPassSnapshot(
			Camera camera,
			string observedCameraName,
			string observedCameraType,
			string observedInjectionPoint,
			PerfMeterGpuResidentDrawerContextSnapshot gpuResidentDrawer)
		{
			try
			{
				string cameraName = observedCameraName ?? GetPreparedCameraName(camera);
				string cameraType = observedCameraType ?? GetPreparedCameraType(camera);
				_latestSnapshot = new PerfMeterRenderGraphSnapshot(
					PerfMeterAvailability.Available,
					PerfMeterRenderGraphState.Observed,
					Time.frameCount,
					cameraName,
					cameraType,
					1,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					PerfMeterRenderGraphSnapshot.UnavailableCount,
					"HDRP Custom Pass observed; HDRP internal Render Graph counters are not exposed.",
					PerfMeterRenderPipelineKind.HighDefinition,
					"HDRP Custom Pass",
					observedInjectionPoint);
				RecordObservation(
					camera,
					PerfMeterRenderPipelineKind.HighDefinition,
					"sgg.perfmeter.hdrp.custom-pass",
					"HDRP Custom Pass",
					HdrpCustomPassAssemblyName,
					PerfMeterRenderPassKind.CustomPass,
					"SGG PerfMeter HDRP Custom Pass",
					observedInjectionPoint,
					1,
					string.Empty,
					gpuResidentDrawer,
					"HDRP internal RenderGraph counters and effective rendering mode are not exposed through stable public APIs.",
					cameraName,
					cameraType);
				_latestObservationFailureWarning = string.Empty;
			}
			catch (Exception exception)
			{
				RecordFailure(PerfMeterRenderPipelineKind.HighDefinition, "HDRP render integration observation failed: " + exception.GetType().Name + ".");
			}
		}

		private static Type GetRenderGraphFeatureType()
		{
			return Type.GetType(RenderGraphFeatureFullName + ", " + RenderGraphFeatureAssemblyName);
		}

		private static Type GetHdrpCustomPassType()
		{
			return Type.GetType(HdrpCustomPassFullName + ", " + HdrpCustomPassAssemblyName);
		}

		private static int GetPerfMeterPassCount(bool recordsOverlayMarkerPass, bool recordsOverdrawCounterPass, bool recordsOverdrawHeatmapPass)
		{
			int count = recordsOverlayMarkerPass ? 1 : 0;
			if (recordsOverdrawCounterPass)
			{
				count += 2;
			}

			if (recordsOverdrawHeatmapPass)
			{
				count++;
			}

			return count;
		}

		private static void RecordObservation(
			Camera camera,
			PerfMeterRenderPipelineKind observedPipelineKind,
			string integrationId,
			string integrationName,
			string integrationVersion,
			PerfMeterRenderPassKind passKind,
			string passName,
			string injectionPoint,
			int passCount,
			string effectiveRenderingMode,
			PerfMeterGpuResidentDrawerContextSnapshot gpuResidentDrawer,
			string warning,
			string cameraName = null,
			string cameraType = null)
		{
			ulong cameraEntityId = GetCameraEntityId(camera);
			bool usePrepared = _hasPreparedObservation && (_preparedCameraEntityId == cameraEntityId || camera == null);
			PerfMeterRenderPipelineSnapshot pipeline;
			PerfMeterRenderPipelineAssetSource assetSource;
			ulong pipelineAssetEntityId;
			if (usePrepared)
			{
				pipeline = _preparedPipeline;
				assetSource = _preparedPipelineSource;
				pipelineAssetEntityId = _preparedPipelineAssetEntityId;
			}
			else
			{
				pipeline = PerfMeterRenderPipelineDetector.CreateSnapshot(out assetSource, out pipelineAssetEntityId);
			}
			pipeline = new PerfMeterRenderPipelineSnapshot(
				observedPipelineKind,
				pipeline.AssetName,
				pipeline.AssetTypeName,
				pipeline.RuntimeTypeName);
			_latestObservation = new PerfMeterRenderIntegrationObservation(
				pipeline,
				assetSource,
				pipelineAssetEntityId,
				Time.frameCount,
				cameraEntityId,
				cameraName ?? (usePrepared ? _preparedCameraName : GetCameraName(camera)),
				cameraType ?? (usePrepared ? _preparedCameraType : GetCameraType(camera)),
				integrationId,
				integrationName,
				GetAssemblyVersion(integrationVersion),
				passKind,
				passName,
				injectionPoint,
				passCount,
				effectiveRenderingMode,
				gpuResidentDrawer,
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterAvailability.Unknown,
				false,
				warning);
		}

		private static PerfMeterVariableRateShadingContextSnapshot CreateVariableRateShadingSnapshot(bool includeObservation)
		{
		#if UNITY_6000_4_OR_NEWER
			try
			{
				Vector2Int tileSize = ShadingRateInfo.imageTileSize;
				return new PerfMeterVariableRateShadingContextSnapshot(
					PerfMeterAvailability.Available,
					SystemInfo.supportsVariableRateShading,
					ShadingRateInfo.supportsPerDrawCall,
					ShadingRateInfo.supportsPerImageTile,
					tileSize.x,
					tileSize.y,
					ShadingRateInfo.graphicsFormat.ToString(),
					includeObservation ? _latestObservation.VrsConfigurationAvailability : PerfMeterAvailability.Unknown,
					includeObservation && _latestObservation.VrsConfigured,
					includeObservation ? _latestObservation.VrsActivityAvailability : PerfMeterAvailability.Unknown,
					includeObservation && _latestObservation.VrsObservedActive,
					"VRS hardware capability is authoritative; configuration and active use remain Unknown unless reported by a typed render integration.");
			}
			catch (Exception exception)
			{
				return new PerfMeterVariableRateShadingContextSnapshot(
					PerfMeterAvailability.Unavailable,
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
					"VRS capability query failed: " + exception.GetType().Name + ".");
			}
		#else
			return PerfMeterVariableRateShadingContextSnapshot.Unknown;
		#endif
		}

		private static string GetSnapshotWarning(bool supportedPipeline, bool matchesCurrentPipeline)
		{
			if (!supportedPipeline)
			{
				return "The active render pipeline is unsupported by the PerfMeter render integration.";
			}

			if (!_latestObservation.IsObserved)
			{
				return "The active PerfMeter render integration has not recorded a frame yet.";
			}

			return matchesCurrentPipeline
				? string.Empty
				: "The latest render observation belongs to a different render pipeline configuration.";
		}

		private static bool PipelinesMatch(PerfMeterRenderIntegrationObservation observed, PerfMeterRenderPipelineSnapshot current, ulong currentAssetEntityId)
		{
			bool assetMatches = observed.RenderPipelineAssetEntityId != 0UL && currentAssetEntityId != 0UL
				? observed.RenderPipelineAssetEntityId == currentAssetEntityId
				: string.Equals(observed.RenderPipeline.AssetName, current.AssetName, StringComparison.Ordinal) &&
					string.Equals(observed.RenderPipeline.AssetTypeName, current.AssetTypeName, StringComparison.Ordinal);
			return observed.RenderPipeline.Kind == current.Kind &&
				assetMatches &&
				string.Equals(observed.RenderPipeline.RuntimeTypeName, current.RuntimeTypeName, StringComparison.Ordinal);
		}

		private static string GetPreparedCameraName(Camera camera)
		{
			ulong cameraEntityId = GetCameraEntityId(camera);
			return _hasPreparedObservation && _preparedCameraEntityId == cameraEntityId
				? _preparedCameraName
				: GetCameraName(camera);
		}

		private static string GetPreparedCameraType(Camera camera)
		{
			ulong cameraEntityId = GetCameraEntityId(camera);
			return _hasPreparedObservation && _preparedCameraEntityId == cameraEntityId
				? _preparedCameraType
				: GetCameraType(camera);
		}

		private static void RecordFailure(PerfMeterRenderPipelineKind pipeline, string warning)
		{
			_latestSnapshot = new PerfMeterRenderGraphSnapshot(
				PerfMeterAvailability.Unavailable,
				PerfMeterRenderGraphState.Unsupported,
				Time.frameCount,
				string.Empty,
				string.Empty,
				0,
				PerfMeterRenderGraphSnapshot.UnavailableCount,
				PerfMeterRenderGraphSnapshot.UnavailableCount,
				PerfMeterRenderGraphSnapshot.UnavailableCount,
				PerfMeterRenderGraphSnapshot.UnavailableCount,
				PerfMeterRenderGraphSnapshot.UnavailableCount,
				warning,
				pipeline,
				string.Empty,
				string.Empty);
			_latestObservation = default;
			_latestObservationFailureWarning = warning ?? string.Empty;
		}

		private static ulong GetCameraEntityId(Camera camera)
		{
			if (camera == null)
			{
				return 0UL;
			}

		#if UNITY_6000_4_OR_NEWER
			return EntityId.ToULong(camera.GetEntityId());
		#else
			return unchecked((uint)camera.GetInstanceID());
		#endif
		}

		private static string GetCameraName(Camera camera)
		{
			return camera != null ? camera.name : string.Empty;
		}

		private static string GetCameraType(Camera camera)
		{
			if (camera == null)
			{
				return string.Empty;
			}

			switch (camera.cameraType)
			{
				case CameraType.Game:
					return "Game";
				case CameraType.SceneView:
					return "SceneView";
				case CameraType.Preview:
					return "Preview";
				case CameraType.Reflection:
					return "Reflection";
				case CameraType.VR:
					return "VR";
				default:
					return "Unknown";
			}
		}

		private static string GetAssemblyVersion(string assemblyName)
		{
			if (string.Equals(assemblyName, RenderGraphFeatureAssemblyName, StringComparison.Ordinal))
			{
				_renderGraphFeatureVersion ??= ResolveAssemblyVersion(RenderGraphFeatureFullName, RenderGraphFeatureAssemblyName);
				return _renderGraphFeatureVersion;
			}

			_hdrpCustomPassVersion ??= ResolveAssemblyVersion(HdrpCustomPassFullName, HdrpCustomPassAssemblyName);
			return _hdrpCustomPassVersion;
		}

		private static string ResolveAssemblyVersion(string typeName, string assemblyName)
		{
			Type type = Type.GetType(typeName + ", " + assemblyName);
			return type?.Assembly.GetName().Version?.ToString() ?? string.Empty;
		}

		private static string CombineWarnings(string first, string second)
		{
			if (string.IsNullOrEmpty(first))
			{
				return second ?? string.Empty;
			}

			if (string.IsNullOrEmpty(second) || first.IndexOf(second, StringComparison.Ordinal) >= 0)
			{
				return first;
			}

			return first + " " + second;
		}
	}
}
