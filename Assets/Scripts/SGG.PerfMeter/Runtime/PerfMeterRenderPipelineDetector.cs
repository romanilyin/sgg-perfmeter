using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal static class PerfMeterRenderPipelineDetector
	{
		private static RenderPipelineAsset _cachedAsset;
		private static string _cachedAssetName = string.Empty;
		private static string _cachedAssetTypeName = string.Empty;
		private static ulong _cachedAssetEntityId;
		private static Type _cachedRuntimeType;
		private static string _cachedRuntimeTypeName = string.Empty;

		internal static PerfMeterRenderPipelineSnapshot CreateSnapshot()
		{
			return CreateSnapshot(out _);
		}

		internal static PerfMeterRenderPipelineSnapshot CreateSnapshot(out PerfMeterRenderPipelineAssetSource assetSource)
		{
			return CreateSnapshot(out assetSource, out _);
		}

		internal static PerfMeterRenderPipelineSnapshot CreateSnapshot(out PerfMeterRenderPipelineAssetSource assetSource, out ulong assetEntityId)
		{
			RenderPipelineAsset activeAsset;
			if (QualitySettings.renderPipeline != null)
			{
				activeAsset = QualitySettings.renderPipeline;
				assetSource = PerfMeterRenderPipelineAssetSource.QualitySettings;
			}
			else
			{
				activeAsset = GraphicsSettings.defaultRenderPipeline;
				assetSource = activeAsset != null
					? PerfMeterRenderPipelineAssetSource.GraphicsSettings
					: PerfMeterRenderPipelineAssetSource.None;
			}
			RenderPipeline runtimePipeline = RenderPipelineManager.currentPipeline;

			bool fakeNullAssetNeedsClear = activeAsset == null &&
				(!string.IsNullOrEmpty(_cachedAssetName) || !string.IsNullOrEmpty(_cachedAssetTypeName) || _cachedAssetEntityId != 0UL);
			if (!object.ReferenceEquals(_cachedAsset, activeAsset) || fakeNullAssetNeedsClear)
			{
				_cachedAsset = activeAsset;
				_cachedAssetName = activeAsset != null ? activeAsset.name : string.Empty;
				_cachedAssetTypeName = activeAsset != null ? activeAsset.GetType().FullName : string.Empty;
				_cachedAssetEntityId = GetObjectEntityId(activeAsset);
			}

			Type runtimeType = runtimePipeline != null ? runtimePipeline.GetType() : null;
			if (!object.ReferenceEquals(_cachedRuntimeType, runtimeType))
			{
				_cachedRuntimeType = runtimeType;
				_cachedRuntimeTypeName = runtimeType != null ? runtimeType.FullName : string.Empty;
			}

			string assetName = _cachedAssetName;
			string assetTypeName = _cachedAssetTypeName;
			string runtimeTypeName = _cachedRuntimeTypeName;
			assetEntityId = _cachedAssetEntityId;
			PerfMeterRenderPipelineKind kind = Classify(assetTypeName, runtimeTypeName, activeAsset == null && runtimePipeline == null);

			return new PerfMeterRenderPipelineSnapshot(kind, assetName, assetTypeName, runtimeTypeName);
		}

		private static ulong GetObjectEntityId(UnityEngine.Object value)
		{
			if (value == null)
			{
				return 0UL;
			}

		#if UNITY_6000_4_OR_NEWER
			return EntityId.ToULong(value.GetEntityId());
		#else
			return unchecked((uint)value.GetInstanceID());
		#endif
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
