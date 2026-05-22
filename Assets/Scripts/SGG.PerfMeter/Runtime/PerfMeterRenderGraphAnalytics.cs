using System;
using System.Reflection;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal static class PerfMeterRenderGraphAnalytics
	{
		private const string RenderGraphFeatureFullName = "SGG.PerfMeter.PerfMeterRenderGraphFeature";
		private const string RenderGraphFeatureAssemblyName = "SGG.PerfMeter.URP";
		private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private static PerfMeterRenderGraphSnapshot _latestSnapshot = PerfMeterRenderGraphSnapshot.NotObserved;

		internal static PerfMeterRenderGraphSnapshot GetSnapshot()
		{
			return _latestSnapshot;
		}

		internal static void ResetForTests()
		{
			_latestSnapshot = PerfMeterRenderGraphSnapshot.NotObserved;
		}

		internal static bool IsRenderGraphFeatureAvailable()
		{
			return GetRenderGraphFeatureType() != null;
		}

		internal static void RecordFeatureSnapshot(object renderGraph, string observedCameraName, string observedCameraType, bool recordsOverlayMarkerPass, bool recordsOverdrawCounterPass, bool recordsOverdrawHeatmapPass)
		{
			try
			{
				int perfMeterPassCount = GetPerfMeterPassCount(recordsOverlayMarkerPass, recordsOverdrawCounterPass, recordsOverdrawHeatmapPass);

				int registeredPassCount = TryReadCount(renderGraph, "m_RenderPasses", "m_Passes", "m_CompiledPassInfos", "renderPasses");
				int mergedPassCount = TryReadCount(renderGraph, "m_NativePasses", "m_MergedPasses", "m_CompiledNativePasses", "nativePasses");
				int transientResourceCount = TryReadCount(renderGraph, "m_TextureResources", "m_TransientResources", "m_RenderGraphResources", "textureResources");
				int importedResourceCount = TryReadCount(renderGraph, "m_ImportedResources", "m_ImportedTextures", "importedResources");
				int aliasedResourceCount = TryReadCount(renderGraph, "m_AliasedResources", "m_AliasingResources", "aliasedResources");
				string warning = GetCounterWarning(registeredPassCount, mergedPassCount, transientResourceCount, importedResourceCount, aliasedResourceCount);

				_latestSnapshot = new PerfMeterRenderGraphSnapshot(
					PerfMeterAvailability.Available,
					PerfMeterRenderGraphState.Observed,
					Time.frameCount,
					observedCameraName,
					observedCameraType,
					perfMeterPassCount,
					registeredPassCount,
					mergedPassCount,
					transientResourceCount,
					importedResourceCount,
					aliasedResourceCount,
					warning);
			}
			catch (Exception exception)
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
					"PerfMeter Render Graph analytics unavailable: " + exception.GetType().Name + ".");
			}
		}

		private static Type GetRenderGraphFeatureType()
		{
			return Type.GetType(RenderGraphFeatureFullName + ", " + RenderGraphFeatureAssemblyName);
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

		private static bool HasAnyInternalCounter(params int[] values)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] >= 0)
				{
					return true;
				}
			}

			return false;
		}

		private static string GetCounterWarning(params int[] values)
		{
			if (!HasAnyInternalCounter(values))
			{
				return "Unity RenderGraph internal pass/resource counters are not exposed by this URP version; only PerfMeter feature observation is available.";
			}

			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] < 0)
				{
					return "Some Unity RenderGraph internal counters are not exposed by this URP version and are reported as -1.";
				}
			}

			return string.Empty;
		}

		private static int TryReadCount(object instance, params string[] memberNames)
		{
			if (instance == null)
			{
				return PerfMeterRenderGraphSnapshot.UnavailableCount;
			}

			Type type = instance.GetType();
			for (int i = 0; i < memberNames.Length; i++)
			{
				object value = TryReadMember(type, instance, memberNames[i]);
				if (TryGetCount(value, out int count))
				{
					return count;
				}
			}

			return PerfMeterRenderGraphSnapshot.UnavailableCount;
		}

		private static object TryReadMember(Type type, object instance, string memberName)
		{
			FieldInfo field = type.GetField(memberName, InstanceFlags);
			if (field != null)
			{
				return field.GetValue(instance);
			}

			PropertyInfo property = type.GetProperty(memberName, InstanceFlags);
			if (property != null && property.GetIndexParameters().Length == 0)
			{
				return property.GetValue(instance);
			}

			return null;
		}

		private static bool TryGetCount(object value, out int count)
		{
			count = PerfMeterRenderGraphSnapshot.UnavailableCount;
			if (value == null)
			{
				return false;
			}

			if (value is int intValue)
			{
				count = intValue;
				return count >= 0;
			}

			PropertyInfo countProperty = value.GetType().GetProperty("Count", InstanceFlags);
			if (countProperty == null || countProperty.GetIndexParameters().Length != 0)
			{
				return false;
			}

			object countValue = countProperty.GetValue(value);
			if (countValue is int reflectedCount && reflectedCount >= 0)
			{
				count = reflectedCount;
				return true;
			}

			return false;
		}
	}
}
