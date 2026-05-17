using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RuntimePerformanceMeter = SGG.PerfMeter.PerformanceMeter;
using RuntimeRenderGraphFeature = SGG.PerfMeter.PerfMeterRenderGraphFeature;

namespace SGG.PerfMeter.Editor.Setup
{
	internal static class PerfMeterSetupUtility
	{
		internal static string InitializationSnippet => BuildInitializationSnippet(true, PerfMeterOverlayCorner.TopRight, PerfMeterOverlayMode.Full, PerfMeterTargetFps.Fps60);

		internal static string BuildInitializationSnippet(bool overlayVisible, PerfMeterOverlayCorner overlayCorner, PerfMeterOverlayMode overlayMode, PerfMeterTargetFps targetFps)
		{
			return @"using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
    {
        PerformanceMeter.EnsureRunning();
        PerformanceMeter.SetTargetFps(PerfMeterTargetFps." + targetFps + @");
        PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner." + overlayCorner + @");
        PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode." + overlayMode + @");
        PerformanceMeter.SetOverlayVisible(" + (overlayVisible ? "true" : "false") + @");
    }
}
";
		}

		private const string DefaultPackageAssetPath = "Assets/Scripts/SGG.PerfMeter";

		internal static string PackageAssetPath
		{
			get
			{
				UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(RuntimePerformanceMeter).Assembly);
				if (!string.IsNullOrEmpty(packageInfo?.assetPath))
				{
					return packageInfo.assetPath.Replace('\\', '/');
				}

				return AssetDatabase.IsValidFolder(DefaultPackageAssetPath) ? DefaultPackageAssetPath : string.Empty;
			}
		}

		internal static PerfMeterSetupStatus GetStatus()
		{
			PerfMeterSetupStatus status = new PerfMeterSetupStatus
			{
				FrameTimingStatsEnabled = PlayerSettings.enableFrameTimingStats,
				PackageAssetPath = PackageAssetPath
			};

			status.Renderers.AddRange(FindRendererStatuses());
			return status;
		}

		internal static InstallResult EnableFrameTimingStats()
		{
			if (PlayerSettings.enableFrameTimingStats)
			{
				return InstallResult.Ok("Frame Timing Stats is already enabled.");
			}

			PlayerSettings.enableFrameTimingStats = true;
			AssetDatabase.SaveAssets();
			return InstallResult.Ok("Frame Timing Stats enabled in Player Settings.");
		}

		internal static InstallResult InstallRendererFeatures()
		{
			List<RendererSetupStatus> renderers = FindRendererStatuses();
			if (renderers.Count == 0)
			{
				return InstallResult.Fail("No URP renderer assets referenced by UniversalRenderPipelineAsset assets were found.");
			}

			int installedCount = 0;
			for (int i = 0; i < renderers.Count; i++)
			{
				RendererSetupStatus rendererStatus = renderers[i];
				if (rendererStatus.RendererData == null || rendererStatus.HasPerfMeterFeature || !rendererStatus.IsEditable)
				{
					continue;
				}

				if (AddRendererFeature(rendererStatus.RendererData))
				{
					installedCount++;
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			if (installedCount == 0)
			{
				return InstallResult.Ok("PerfMeter Render Graph feature is already installed in all discovered URP renderers.");
			}

			return InstallResult.Ok("Installed PerfMeter Render Graph feature in " + installedCount + " renderer asset(s).");
		}

		internal static InstallResult InstallRendererFeatures(IEnumerable<string> rendererAssetPaths)
		{
			if (rendererAssetPaths == null)
			{
				return InstallResult.Fail("No renderer assets selected.");
			}

			HashSet<string> requestedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string rendererAssetPath in rendererAssetPaths)
			{
				if (!string.IsNullOrEmpty(rendererAssetPath))
				{
					requestedPaths.Add(rendererAssetPath.Replace('\\', '/'));
				}
			}

			if (requestedPaths.Count == 0)
			{
				return InstallResult.Fail("No renderer assets selected.");
			}

			List<RendererSetupStatus> renderers = FindRendererStatuses();
			if (renderers.Count == 0)
			{
				return InstallResult.Fail("No URP renderer assets were found.");
			}

			int matchedCount = 0;
			int installedCount = 0;
			for (int i = 0; i < renderers.Count; i++)
			{
				RendererSetupStatus rendererStatus = renderers[i];
				if (!requestedPaths.Contains(rendererStatus.AssetPath))
				{
					continue;
				}

				matchedCount++;
				if (rendererStatus.RendererData == null || rendererStatus.HasPerfMeterFeature || !rendererStatus.IsEditable)
				{
					continue;
				}

				if (AddRendererFeature(rendererStatus.RendererData))
				{
					installedCount++;
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			if (matchedCount == 0)
			{
				return InstallResult.Fail("No selected URP renderer assets were found.");
			}

			if (installedCount == 0)
			{
				return InstallResult.Ok("PerfMeter Render Graph feature is already installed or cannot be edited in the selected URP renderer asset(s).");
			}

			return InstallResult.Ok("Installed PerfMeter Render Graph feature in " + installedCount + " selected renderer asset(s).");
		}

		private static List<RendererSetupStatus> FindRendererStatuses()
		{
			List<RendererSetupStatus> result = new List<RendererSetupStatus>();
			HashSet<string> seenRendererPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			AddRendererStatusesFromActivePipelineAssets(result, seenRendererPaths);

			string[] pipelineGuids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" });
			for (int i = 0; i < pipelineGuids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(pipelineGuids[i]);
				UniversalRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
				if (pipelineAsset == null)
				{
					continue;
				}

				AddRendererStatusesFromPipeline(pipelineAsset, result, seenRendererPaths, false);
			}

			string[] rendererGuids = AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets" });
			for (int i = 0; i < rendererGuids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(rendererGuids[i]);
				ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
				AddRendererStatus(rendererData, result, seenRendererPaths, false);
			}

			result.Sort((left, right) => string.Compare(left.AssetPath, right.AssetPath, StringComparison.OrdinalIgnoreCase));
			return result;
		}

		private static void AddRendererStatusesFromActivePipelineAssets(List<RendererSetupStatus> result, HashSet<string> seenRendererPaths)
		{
			AddRendererStatusesFromPipeline(GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset, result, seenRendererPaths, true);
			AddRendererStatusesFromPipeline(QualitySettings.renderPipeline as UniversalRenderPipelineAsset, result, seenRendererPaths, true);
		}

		private static void AddRendererStatusesFromPipeline(UniversalRenderPipelineAsset pipelineAsset, List<RendererSetupStatus> result, HashSet<string> seenRendererPaths, bool isActive)
		{
			if (pipelineAsset == null)
			{
				return;
			}

			SerializedObject serializedPipeline = new SerializedObject(pipelineAsset);
			SerializedProperty rendererDataList = serializedPipeline.FindProperty("m_RendererDataList");
			if (rendererDataList != null && rendererDataList.isArray)
			{
				for (int i = 0; i < rendererDataList.arraySize; i++)
				{
					ScriptableRendererData rendererData = rendererDataList.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
					AddRendererStatus(rendererData, result, seenRendererPaths, isActive);
				}
			}

			SerializedProperty rendererDataProperty = serializedPipeline.FindProperty("m_RendererData");
			AddRendererStatus(rendererDataProperty?.objectReferenceValue as ScriptableRendererData, result, seenRendererPaths, isActive);
		}

		private static void AddRendererStatus(ScriptableRendererData rendererData, List<RendererSetupStatus> result, HashSet<string> seenRendererPaths, bool isActive)
		{
			if (rendererData == null)
			{
				return;
			}

			string assetPath = AssetDatabase.GetAssetPath(rendererData);
			if (string.IsNullOrEmpty(assetPath))
			{
				return;
			}

			if (!seenRendererPaths.Add(assetPath))
			{
				MarkRendererActive(result, assetPath, isActive);
				return;
			}

			bool isInPackage = IsPackageAssetPath(assetPath);

			result.Add(new RendererSetupStatus
			{
				RendererData = rendererData,
				Name = rendererData.name,
				AssetPath = assetPath,
				IsActive = isActive,
				IsInPackage = isInPackage,
				IsEditable = !isInPackage,
				HasPerfMeterFeature = HasPerfMeterFeature(rendererData),
				HasMissingFeatureReference = HasMissingFeatureReference(rendererData)
			});
		}

		private static void MarkRendererActive(List<RendererSetupStatus> renderers, string assetPath, bool isActive)
		{
			if (!isActive)
			{
				return;
			}

			for (int i = 0; i < renderers.Count; i++)
			{
				if (string.Equals(renderers[i].AssetPath, assetPath, StringComparison.OrdinalIgnoreCase))
				{
					renderers[i].IsActive = true;
					return;
				}
			}
		}

		private static bool IsPackageAssetPath(string assetPath)
		{
			return !string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
		}

		private static bool AddRendererFeature(ScriptableRendererData rendererData)
		{
			if (rendererData == null || HasPerfMeterFeature(rendererData))
			{
				return false;
			}

			string assetPath = AssetDatabase.GetAssetPath(rendererData);
			if (IsPackageAssetPath(assetPath))
			{
				return false;
			}

			SerializedObject serializedRenderer = new SerializedObject(rendererData);
			SerializedProperty rendererFeatures = serializedRenderer.FindProperty("m_RendererFeatures");
			SerializedProperty rendererFeatureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
			if (rendererFeatures == null || !rendererFeatures.isArray)
			{
				throw new InvalidOperationException("URP renderer data does not expose m_RendererFeatures: " + rendererData.name);
			}

			Undo.RegisterCompleteObjectUndo(rendererData, "Add PerfMeter Renderer Feature");
			RuntimeRenderGraphFeature feature = ScriptableObject.CreateInstance<RuntimeRenderGraphFeature>();
			feature.name = typeof(RuntimeRenderGraphFeature).Name;
			Undo.RegisterCreatedObjectUndo(feature, "Add PerfMeter Renderer Feature");

			if (EditorUtility.IsPersistent(rendererData))
			{
				AssetDatabase.AddObjectToAsset(feature, rendererData);
			}

			AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
			serializedRenderer.Update();
			rendererFeatures.arraySize++;
			SerializedProperty featureProperty = rendererFeatures.GetArrayElementAtIndex(rendererFeatures.arraySize - 1);
			featureProperty.objectReferenceValue = feature;

			if (rendererFeatureMap != null && rendererFeatureMap.isArray)
			{
				rendererFeatureMap.arraySize = rendererFeatures.arraySize;
				SerializedProperty mapProperty = rendererFeatureMap.GetArrayElementAtIndex(rendererFeatureMap.arraySize - 1);
				mapProperty.longValue = localId;
			}

			serializedRenderer.ApplyModifiedProperties();
			rendererData.SetDirty();
			EditorUtility.SetDirty(rendererData);
			EditorUtility.SetDirty(feature);
			return true;
		}

		private static bool HasPerfMeterFeature(ScriptableRendererData rendererData)
		{
			List<ScriptableRendererFeature> rendererFeatures = rendererData.rendererFeatures;
			for (int i = 0; i < rendererFeatures.Count; i++)
			{
				if (rendererFeatures[i] is RuntimeRenderGraphFeature)
				{
					return true;
				}
			}

			return false;
		}

		private static bool HasMissingFeatureReference(ScriptableRendererData rendererData)
		{
			List<ScriptableRendererFeature> rendererFeatures = rendererData.rendererFeatures;
			for (int i = 0; i < rendererFeatures.Count; i++)
			{
				if (rendererFeatures[i] == null)
				{
					return true;
				}
			}

			return false;
		}

		internal sealed class PerfMeterSetupStatus
		{
			internal bool FrameTimingStatsEnabled;
			internal string PackageAssetPath = string.Empty;
			internal readonly List<RendererSetupStatus> Renderers = new List<RendererSetupStatus>();

			internal int InstalledRendererCount
			{
				get
				{
					int count = 0;
					for (int i = 0; i < Renderers.Count; i++)
					{
						if (Renderers[i].HasPerfMeterFeature)
						{
							count++;
						}
					}

					return count;
				}
			}

			internal bool AllRenderersConfigured => Renderers.Count > 0 && InstalledRendererCount == Renderers.Count;

			internal bool HasRendererWarnings
			{
				get
				{
					for (int i = 0; i < Renderers.Count; i++)
					{
						if (Renderers[i].HasMissingFeatureReference || !Renderers[i].IsEditable)
						{
							return true;
						}
					}

					return false;
				}
			}

			internal string ProjectSettingsMessage => FrameTimingStatsEnabled
				? "Frame Timing Stats is enabled."
				: "Frame Timing Stats is disabled; GPU frame timing can be unavailable in builds.";

			internal string RendererMessage
			{
				get
				{
					if (Renderers.Count == 0)
					{
						return "No URP renderer assets were found from project URP assets.";
					}

					string warning = HasRendererWarnings ? " Missing feature references or package renderer assets require manual inspection." : string.Empty;
					return InstalledRendererCount + " / " + Renderers.Count + " URP renderer asset(s) have PerfMeter Render Graph feature." + warning;
				}
			}
		}

		internal sealed class RendererSetupStatus
		{
			internal ScriptableRendererData RendererData;
			internal string Name = string.Empty;
			internal string AssetPath = string.Empty;
			internal bool IsActive;
			internal bool IsInPackage;
			internal bool IsEditable = true;
			internal bool HasPerfMeterFeature;
			internal bool HasMissingFeatureReference;
		}

		internal sealed class InstallResult
		{
			internal bool Success;
			internal string Message = string.Empty;

			internal static InstallResult Ok(string message)
			{
				return new InstallResult { Success = true, Message = message };
			}

			internal static InstallResult Fail(string message)
			{
				return new InstallResult { Success = false, Message = message };
			}
		}
	}
}
