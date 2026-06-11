using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal static class PerfMeterRenderPipelineDetector
	{
		internal static PerfMeterRenderPipelineSnapshot CreateSnapshot()
		{
			RenderPipelineAsset activeAsset = QualitySettings.renderPipeline != null
				? QualitySettings.renderPipeline
				: GraphicsSettings.defaultRenderPipeline;
			RenderPipeline runtimePipeline = RenderPipelineManager.currentPipeline;

			string assetName = activeAsset != null ? activeAsset.name : string.Empty;
			string assetTypeName = activeAsset != null ? activeAsset.GetType().FullName : string.Empty;
			string runtimeTypeName = runtimePipeline != null ? runtimePipeline.GetType().FullName : string.Empty;
			PerfMeterRenderPipelineKind kind = Classify(assetTypeName, runtimeTypeName, activeAsset == null && runtimePipeline == null);

			return new PerfMeterRenderPipelineSnapshot(kind, assetName, assetTypeName, runtimeTypeName);
		}

		internal static PerfMeterRenderPipelineKind GetActiveKind()
		{
			return CreateSnapshot().Kind;
		}

		private static PerfMeterRenderPipelineKind Classify(string assetTypeName, string runtimeTypeName, bool noSrpAssetOrRuntime)
		{
			PerfMeterRenderPipelineKind assetKind = ClassifyTypeName(assetTypeName);
			if (assetKind != PerfMeterRenderPipelineKind.Unknown)
			{
				return assetKind;
			}

			PerfMeterRenderPipelineKind runtimeKind = ClassifyTypeName(runtimeTypeName);
			if (runtimeKind != PerfMeterRenderPipelineKind.Unknown)
			{
				return runtimeKind;
			}

			return noSrpAssetOrRuntime ? PerfMeterRenderPipelineKind.BuiltIn : PerfMeterRenderPipelineKind.Unknown;
		}

		private static PerfMeterRenderPipelineKind ClassifyTypeName(string typeName)
		{
			if (string.IsNullOrEmpty(typeName))
			{
				return PerfMeterRenderPipelineKind.Unknown;
			}

			if (typeName.IndexOf("UniversalRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0 ||
				typeName.IndexOf(".Universal.", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return PerfMeterRenderPipelineKind.Universal;
			}

			if (typeName.IndexOf("HDRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0 ||
				typeName.IndexOf("HighDefinition", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return PerfMeterRenderPipelineKind.HighDefinition;
			}

			return PerfMeterRenderPipelineKind.Unknown;
		}
	}
}
