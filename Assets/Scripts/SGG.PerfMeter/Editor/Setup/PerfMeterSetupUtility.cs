using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using RuntimePerformanceMeter = SGG.PerfMeter.PerformanceMeter;

namespace SGG.PerfMeter.Editor.Setup
{
	internal static class PerfMeterSetupUtility
	{
		internal const string UnsupportedCompatibilityMessage = "SGG PerfMeter officially supports Unity 6000.4+ with URP 17.4+ or HDRP 17.4+. Older Unity/SRP versions are import-safe only; runtime render integrations are unsupported and bug reports for older versions are not accepted.";

		private const string DefaultPackageAssetPath = "Assets/Scripts/SGG.PerfMeter";
		private const string UniversalRenderPipelineAssetFullName = "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";
		private const string ScriptableRendererDataFullName = "UnityEngine.Rendering.Universal.ScriptableRendererData";
		private const string PerfMeterRenderGraphFeatureFullName = "SGG.PerfMeter.PerfMeterRenderGraphFeature";
		private const string PerfMeterRenderGraphFeatureAssemblyName = "SGG.PerfMeter.URP";
		private const string PerfMeterHdrpCustomPassFullName = "SGG.PerfMeter.PerfMeterHdrpCustomPass";
		private const string PerfMeterHdrpCustomPassAssemblyName = "SGG.PerfMeter.HDRP";

		internal static string InitializationSnippet => BuildInitializationSnippet(true, PerfMeterOverlayCorner.TopRight, PerfMeterTargetFps.Fps60, PerfMeterOverlayTheme.ClassicDark, PerfMeterOverlayLayout.MetricBars, PerfMeterOverlayFontFamily.Manrope);

		internal static bool IsOfficialUnityVersionSupported
		{
			get
			{
			#if UNITY_6000_4_OR_NEWER
				return true;
			#else
				return false;
			#endif
			}
		}

		internal static bool IsRenderGraphFeatureAvailable => GetRenderGraphFeatureType() != null;

		internal static bool IsRendererFeatureSetupSupported => IsOfficialUnityVersionSupported && IsRenderGraphFeatureAvailable;

		internal static bool IsHdrpCustomPassAvailable => GetHdrpCustomPassType() != null;

		internal static string BuildInitializationSnippet(bool overlayVisible, PerfMeterOverlayCorner overlayCorner, PerfMeterTargetFps targetFps, PerfMeterOverlayTheme overlayTheme, PerfMeterOverlayLayout overlayLayout, PerfMeterOverlayFontFamily overlayFontFamily)
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
        PerformanceMeter.SetOverlayTheme(PerfMeterOverlayTheme." + overlayTheme + @");
        PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout." + overlayLayout + @");
        PerformanceMeter.SetOverlayFontFamily(PerfMeterOverlayFontFamily." + overlayFontFamily + @");
        PerformanceMeter.SetOverlayVisible(" + (overlayVisible ? "true" : "false") + @");
    }
}
";
		}

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
			PerfMeterRenderPipelineSnapshot renderPipeline = PerfMeterRenderPipelineDetector.CreateSnapshot();
			PerfMeterSetupStatus status = new PerfMeterSetupStatus
			{
				FrameTimingStatsEnabled = PlayerSettings.enableFrameTimingStats,
				OfficialUnityVersionSupported = IsOfficialUnityVersionSupported,
				RenderGraphFeatureAvailable = IsRenderGraphFeatureAvailable,
				HdrpCustomPassAvailable = IsHdrpCustomPassAvailable,
				ActiveRenderPipeline = renderPipeline.Kind,
				RenderPipelineAssetName = renderPipeline.AssetName,
				RenderPipelineAssetType = renderPipeline.AssetTypeName,
				RenderPipelineRuntimeType = renderPipeline.RuntimeTypeName,
				PackageAssetPath = PackageAssetPath,
				Settings = GetSettingsStatus()
			};

			if (renderPipeline.Kind == PerfMeterRenderPipelineKind.Universal || renderPipeline.Kind == PerfMeterRenderPipelineKind.Unknown)
			{
				status.Renderers.AddRange(FindRendererStatuses());
			}

			return status;
		}

		internal static PerfMeterSettingsSnapshot LoadSettingsSnapshot()
		{
			string path = PerfMeterSettingsStore.ResourcesAssetPath;
			if (!File.Exists(path))
			{
				return PerfMeterSettingsStore.Defaults;
			}

			string json = File.ReadAllText(path);
			PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot snapshot);
			return snapshot;
		}

		internal static InstallResult CreateDefaultSettings()
		{
			return SaveSettingsSnapshot(PerfMeterSettingsStore.ToSnapshot(PerfMeterSettingsStore.CreateDefault(), PerfMeterSettingsLoadState.Loaded, string.Empty));
		}

		internal static InstallResult SaveSettingsSnapshot(PerfMeterSettingsSnapshot snapshot)
		{
			try
			{
				EnsureSettingsFolder();
				PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateFromSnapshot(snapshot);
				PerfMeterOverlayPresetEditorUtility.BakeOverlayPresetsIntoSettings(settings);
				string json = PerfMeterSettingsStore.ToJson(settings);
				File.WriteAllText(PerfMeterSettingsStore.ResourcesAssetPath, json);
				AssetDatabase.ImportAsset(PerfMeterSettingsStore.ResourcesAssetPath);
				AssetDatabase.Refresh();
				return InstallResult.Ok("PerfMeter JSON settings saved to " + PerfMeterSettingsStore.ResourcesAssetPath + ". Runtime zero-code setup will use Resources path " + PerfMeterSettingsStore.ResourcesLoadPath + ".");
			}
			catch (Exception exception)
			{
				return InstallResult.Fail("Failed to save PerfMeter JSON settings: " + exception.Message);
			}
		}

		internal static InstallResult ApplySettingsToRuntime()
		{
			if (!EditorApplication.isPlaying)
			{
				return InstallResult.Fail("Enter Play Mode to apply PerfMeter settings to the active runtime session.");
			}

			PerfMeterSettingsSnapshot settings = LoadSettingsSnapshot();
			RuntimePerformanceMeter.ApplySettings(settings);
			return InstallResult.Ok("PerfMeter JSON settings applied to the active Play Mode session.");
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
			if (TrySkipRendererFeatureInstallForActivePipeline(out InstallResult skipResult))
			{
				return skipResult;
			}

			if (!CanInstallRenderGraphFeature(out string unsupportedMessage))
			{
				return InstallResult.Fail(unsupportedMessage);
			}

			List<RendererSetupStatus> renderers = FindRendererStatuses();
			if (renderers.Count == 0)
			{
				return InstallResult.Fail("No URP renderer assets referenced by UniversalRenderPipelineAsset assets were found.");
			}

			int installedCount = 0;
			for (int i = 0; i < renderers.Count; i++)
			{
				RendererSetupStatus rendererStatus = renderers[i];
				if (rendererStatus.RendererData == null || rendererStatus.HasPerfMeterFeature || !rendererStatus.CanInstallFeature)
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
			if (TrySkipRendererFeatureInstallForActivePipeline(out InstallResult skipResult))
			{
				return skipResult;
			}

			if (rendererAssetPaths == null)
			{
				return InstallResult.Fail("No renderer assets selected.");
			}

			if (!CanInstallRenderGraphFeature(out string unsupportedMessage))
			{
				return InstallResult.Fail(unsupportedMessage);
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
				if (rendererStatus.RendererData == null || rendererStatus.HasPerfMeterFeature || !rendererStatus.CanInstallFeature)
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
				UnityEngine.Object pipelineAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
				AddRendererStatusesFromPipeline(pipelineAsset, result, seenRendererPaths, false);
			}

			string[] rendererGuids = AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets" });
			for (int i = 0; i < rendererGuids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(rendererGuids[i]);
				UnityEngine.Object rendererData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
				AddRendererStatus(rendererData, result, seenRendererPaths, false);
			}

			result.Sort((left, right) => string.Compare(left.AssetPath, right.AssetPath, StringComparison.OrdinalIgnoreCase));
			return result;
		}

		private static PerfMeterSettingsSetupStatus GetSettingsStatus()
		{
			PerfMeterSettingsSnapshot snapshot = LoadSettingsSnapshot();
			return new PerfMeterSettingsSetupStatus
			{
				AssetPath = PerfMeterSettingsStore.ResourcesAssetPath,
				ResourcesLoadPath = PerfMeterSettingsStore.ResourcesLoadPath,
				FileExists = File.Exists(PerfMeterSettingsStore.ResourcesAssetPath),
				Snapshot = snapshot
			};
		}

		private static void EnsureSettingsFolder()
		{
			if (!AssetDatabase.IsValidFolder("Assets/Resources"))
			{
				AssetDatabase.CreateFolder("Assets", "Resources");
			}

			if (!AssetDatabase.IsValidFolder("Assets/Resources/SGG.PerfMeter"))
			{
				AssetDatabase.CreateFolder("Assets/Resources", "SGG.PerfMeter");
			}
		}

		private static void AddRendererStatusesFromActivePipelineAssets(List<RendererSetupStatus> result, HashSet<string> seenRendererPaths)
		{
			AddRendererStatusesFromPipeline(GraphicsSettings.defaultRenderPipeline, result, seenRendererPaths, true);
			AddRendererStatusesFromPipeline(QualitySettings.renderPipeline, result, seenRendererPaths, true);
		}

		private static void AddRendererStatusesFromPipeline(UnityEngine.Object pipelineAsset, List<RendererSetupStatus> result, HashSet<string> seenRendererPaths, bool isActive)
		{
			if (pipelineAsset == null || !IsUniversalRenderPipelineAsset(pipelineAsset))
			{
				return;
			}

			SerializedObject serializedPipeline = new SerializedObject(pipelineAsset);
			SerializedProperty rendererDataList = serializedPipeline.FindProperty("m_RendererDataList");
			if (rendererDataList != null && rendererDataList.isArray)
			{
				for (int i = 0; i < rendererDataList.arraySize; i++)
				{
					AddRendererStatus(rendererDataList.GetArrayElementAtIndex(i).objectReferenceValue, result, seenRendererPaths, isActive);
				}
			}

			SerializedProperty rendererDataProperty = serializedPipeline.FindProperty("m_RendererData");
			if (rendererDataProperty != null)
			{
				AddRendererStatus(rendererDataProperty.objectReferenceValue, result, seenRendererPaths, isActive);
			}
		}

		private static void AddRendererStatus(UnityEngine.Object rendererData, List<RendererSetupStatus> result, HashSet<string> seenRendererPaths, bool isActive)
		{
			if (rendererData == null || !IsRendererDataAsset(rendererData))
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
			bool hasFeature = HasPerfMeterFeature(rendererData);
			bool hasMissingReference = HasMissingFeatureReference(rendererData);

			result.Add(new RendererSetupStatus
			{
				RendererData = rendererData,
				Name = rendererData.name,
				AssetPath = assetPath,
				IsActive = isActive,
				IsInPackage = isInPackage,
				IsEditable = !isInPackage,
				CanInstallFeature = !isInPackage && IsRendererFeatureSetupSupported,
				HasPerfMeterFeature = hasFeature,
				HasMissingFeatureReference = hasMissingReference
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

		private static bool AddRendererFeature(UnityEngine.Object rendererData)
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

			Type featureType = GetRenderGraphFeatureType();
			if (featureType == null || !typeof(ScriptableObject).IsAssignableFrom(featureType))
			{
				throw new InvalidOperationException(GetRenderGraphFeatureUnavailableMessage());
			}

			SerializedObject serializedRenderer = new SerializedObject(rendererData);
			SerializedProperty rendererFeatures = serializedRenderer.FindProperty("m_RendererFeatures");
			SerializedProperty rendererFeatureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
			if (rendererFeatures == null || !rendererFeatures.isArray)
			{
				throw new InvalidOperationException("URP renderer data does not expose m_RendererFeatures: " + rendererData.name);
			}

			Undo.RegisterCompleteObjectUndo(rendererData, "Add PerfMeter Renderer Feature");
			ScriptableObject feature = ScriptableObject.CreateInstance(featureType);
			feature.name = featureType.Name;
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
			EditorUtility.SetDirty(rendererData);
			EditorUtility.SetDirty(feature);
			return true;
		}

		private static bool HasPerfMeterFeature(UnityEngine.Object rendererData)
		{
			SerializedProperty rendererFeatures = GetRendererFeaturesProperty(rendererData);
			if (rendererFeatures == null)
			{
				return false;
			}

			for (int i = 0; i < rendererFeatures.arraySize; i++)
			{
				UnityEngine.Object feature = rendererFeatures.GetArrayElementAtIndex(i).objectReferenceValue;
				if (IsPerfMeterRenderGraphFeature(feature))
				{
					return true;
				}
			}

			return false;
		}

		private static bool HasMissingFeatureReference(UnityEngine.Object rendererData)
		{
			SerializedProperty rendererFeatures = GetRendererFeaturesProperty(rendererData);
			if (rendererFeatures == null)
			{
				return false;
			}

			for (int i = 0; i < rendererFeatures.arraySize; i++)
			{
				if (rendererFeatures.GetArrayElementAtIndex(i).objectReferenceValue == null)
				{
					return true;
				}
			}

			return false;
		}

		private static SerializedProperty GetRendererFeaturesProperty(UnityEngine.Object rendererData)
		{
			if (rendererData == null)
			{
				return null;
			}

			SerializedObject serializedRenderer = new SerializedObject(rendererData);
			SerializedProperty rendererFeatures = serializedRenderer.FindProperty("m_RendererFeatures");
			return rendererFeatures != null && rendererFeatures.isArray ? rendererFeatures : null;
		}

		private static bool CanInstallRenderGraphFeature(out string message)
		{
			if (!IsOfficialUnityVersionSupported)
			{
				message = UnsupportedCompatibilityMessage;
				return false;
			}

			if (!IsRenderGraphFeatureAvailable)
			{
				message = GetRenderGraphFeatureUnavailableMessage();
				return false;
			}

			message = string.Empty;
			return true;
		}

		private static bool TrySkipRendererFeatureInstallForActivePipeline(out InstallResult result)
		{
			PerfMeterRenderPipelineKind activePipeline = PerfMeterRenderPipelineDetector.GetActiveKind();
			if (activePipeline == PerfMeterRenderPipelineKind.HighDefinition)
			{
				result = InstallResult.Ok("HDRP detected. URP Renderer Feature installation is skipped; PerfMeter HDRP Custom Pass is runtime-registered when the optional HDRP assembly is available. HDRP overdraw and heatmap are unsupported.");
				return true;
			}

			if (activePipeline == PerfMeterRenderPipelineKind.BuiltIn)
			{
				result = InstallResult.Fail("Built-in Render Pipeline detected. PerfMeter render integration requires URP 17.4+ or HDRP 17.4+ in Unity 6000.4+.");
				return true;
			}

			result = null;
			return false;
		}

		private static string GetRenderGraphFeatureUnavailableMessage()
		{
			return "PerfMeter Render Graph feature assembly is unavailable. Install/use URP 17.4+ in Unity 6000.4+; older Unity/URP versions are import-safe only and unsupported.";
		}

		private static Type GetRenderGraphFeatureType()
		{
			return Type.GetType(PerfMeterRenderGraphFeatureFullName + ", " + PerfMeterRenderGraphFeatureAssemblyName);
		}

		private static Type GetHdrpCustomPassType()
		{
			return Type.GetType(PerfMeterHdrpCustomPassFullName + ", " + PerfMeterHdrpCustomPassAssemblyName);
		}

		private static bool IsUniversalRenderPipelineAsset(UnityEngine.Object asset)
		{
			return HasTypeInHierarchy(asset, UniversalRenderPipelineAssetFullName);
		}

		private static bool IsRendererDataAsset(UnityEngine.Object asset)
		{
			return HasTypeInHierarchy(asset, ScriptableRendererDataFullName);
		}

		private static bool IsPerfMeterRenderGraphFeature(UnityEngine.Object feature)
		{
			return feature != null && feature.GetType().FullName == PerfMeterRenderGraphFeatureFullName;
		}

		private static bool HasTypeInHierarchy(UnityEngine.Object asset, string fullName)
		{
			if (asset == null)
			{
				return false;
			}

			Type type = asset.GetType();
			while (type != null)
			{
				if (type.FullName == fullName)
				{
					return true;
				}

				type = type.BaseType;
			}

			return false;
		}

		internal sealed class PerfMeterSetupStatus
		{
			internal bool FrameTimingStatsEnabled;
			internal bool OfficialUnityVersionSupported;
			internal bool RenderGraphFeatureAvailable;
			internal bool HdrpCustomPassAvailable;
			internal PerfMeterRenderPipelineKind ActiveRenderPipeline = PerfMeterRenderPipelineKind.Unknown;
			internal string RenderPipelineAssetName = string.Empty;
			internal string RenderPipelineAssetType = string.Empty;
			internal string RenderPipelineRuntimeType = string.Empty;
			internal string PackageAssetPath = string.Empty;
			internal PerfMeterSettingsSetupStatus Settings = new PerfMeterSettingsSetupStatus();
			internal readonly List<RendererSetupStatus> Renderers = new List<RendererSetupStatus>();

			internal bool RendererFeatureSetupSupported => ActiveRenderPipeline == PerfMeterRenderPipelineKind.HighDefinition
				? OfficialUnityVersionSupported && HdrpCustomPassAvailable
				: OfficialUnityVersionSupported && RenderGraphFeatureAvailable;

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

			internal bool AllRenderersConfigured => ActiveRenderPipeline == PerfMeterRenderPipelineKind.HighDefinition
				? RendererFeatureSetupSupported
				: RendererFeatureSetupSupported && Renderers.Count > 0 && InstalledRendererCount == Renderers.Count;

			internal bool HasRendererWarnings
			{
				get
				{
					if (!RendererFeatureSetupSupported)
					{
						return true;
					}

					if (ActiveRenderPipeline == PerfMeterRenderPipelineKind.HighDefinition)
					{
						return false;
					}

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

			internal string CompatibilityMessage => OfficialUnityVersionSupported
				? "Official target: Unity 6000.4+ with URP 17.4+ or HDRP 17.4+."
				: UnsupportedCompatibilityMessage;

			internal string ProjectSettingsMessage
			{
				get
				{
					string frameTimingMessage = FrameTimingStatsEnabled
						? "Frame Timing Stats is enabled."
						: "Frame Timing Stats is disabled; GPU frame timing can be unavailable in builds.";

					return CompatibilityMessage + " " + frameTimingMessage;
				}
			}

			internal string RendererMessage
			{
				get
				{
					if (!OfficialUnityVersionSupported)
					{
						return UnsupportedCompatibilityMessage;
					}

					if (ActiveRenderPipeline == PerfMeterRenderPipelineKind.HighDefinition)
					{
						return HdrpCustomPassAvailable
							? "HDRP detected. PerfMeter HDRP Custom Pass assembly is available and runtime-registered globally when the runtime starts. HDRP overdraw and heatmap are unsupported."
							: "HDRP detected, but PerfMeter HDRP Custom Pass assembly is unavailable. Reimport the package with HDRP 17.4+ installed.";
					}

					if (ActiveRenderPipeline == PerfMeterRenderPipelineKind.BuiltIn)
					{
						return "Built-in Render Pipeline detected. Runtime render integration and overdraw are unsupported; core FPS/CPU/GPU/memory diagnostics remain available.";
					}

					if (!RenderGraphFeatureAvailable)
					{
						return GetRenderGraphFeatureUnavailableMessage();
					}

					if (Renderers.Count == 0)
					{
						return "No URP renderer assets were found from project URP assets.";
					}

					string warning = HasRendererWarnings ? " Missing feature references or package renderer assets require manual inspection." : string.Empty;
					return InstalledRendererCount + " / " + Renderers.Count + " URP renderer asset(s) have PerfMeter Render Graph feature." + warning;
				}
			}
		}

		internal sealed class PerfMeterSettingsSetupStatus
		{
			internal bool FileExists;
			internal string AssetPath = PerfMeterSettingsStore.ResourcesAssetPath;
			internal string ResourcesLoadPath = PerfMeterSettingsStore.ResourcesLoadPath;
			internal PerfMeterSettingsSnapshot Snapshot = PerfMeterSettingsStore.Defaults;

			internal string Message
			{
				get
				{
					if (!FileExists)
					{
						return "No JSON settings file. Zero-code setup is disabled until settings are saved.";
					}

					string warning = string.IsNullOrEmpty(Snapshot.Warning) ? string.Empty : " Warning: " + Snapshot.Warning;
					string preset = string.IsNullOrEmpty(Snapshot.ActiveOverlayPresetId) ? Snapshot.ActivePreset : Snapshot.ActiveOverlayPresetId;
					return "JSON settings " + Snapshot.LoadState + ": " + (Snapshot.Enabled ? "enabled" : "disabled") + ", auto-start " + (Snapshot.AutoStart ? "on" : "off") + ", preset " + preset + "." + warning;
				}
			}
		}

		internal sealed class RendererSetupStatus
		{
			internal UnityEngine.Object RendererData;
			internal string Name = string.Empty;
			internal string AssetPath = string.Empty;
			internal bool IsActive;
			internal bool IsInPackage;
			internal bool IsEditable = true;
			internal bool CanInstallFeature;
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
