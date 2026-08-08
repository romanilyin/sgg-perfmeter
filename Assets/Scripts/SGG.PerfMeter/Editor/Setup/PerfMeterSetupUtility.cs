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
		internal const string ImportUnityVersionFloor = "2022.3";
		internal const string CoreRuntimeUnityVersionFloor = "6000.4";
		internal const string RenderIntegrationPipelinePackageVersionFloor = "17.4";

		private const string DefaultPackageAssetPath = "Assets/Scripts/SGG.PerfMeter";
		private const string UniversalRenderPipelinePackageName = "com.unity.render-pipelines.universal";
		private const string HighDefinitionRenderPipelinePackageName = "com.unity.render-pipelines.high-definition";
		private const string UniversalRenderPipelineAssetFullName = "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";
		private const string ScriptableRendererDataFullName = "UnityEngine.Rendering.Universal.ScriptableRendererData";
		private const string PerfMeterRenderGraphFeatureFullName = "SGG.PerfMeter.PerfMeterRenderGraphFeature";
		private const string PerfMeterRenderGraphFeatureAssemblyName = "SGG.PerfMeter.URP";
		private const string PerfMeterHdrpCustomPassFullName = "SGG.PerfMeter.PerfMeterHdrpCustomPass";
		private const string PerfMeterHdrpCustomPassAssemblyName = "SGG.PerfMeter.HDRP";

		internal static string InitializationSnippet => BuildInitializationSnippet(PerfMeterSettingsStore.Defaults);

		internal static bool IsOfficialUnityVersionSupported
		{
			get
			{
				return PerfMeterCompatibilityEvaluator.IsAtLeast(Application.unityVersion, CoreRuntimeUnityVersionFloor);
			}
		}

		internal static bool IsRenderGraphFeatureAvailable => GetRenderGraphFeatureType() != null;

		internal static bool IsRendererFeatureSetupSupported => IsOfficialUnityVersionSupported && IsRenderGraphFeatureAvailable;

		internal static bool IsHdrpCustomPassAvailable => GetHdrpCustomPassType() != null;

		internal static string BuildInitializationSnippet(PerfMeterSettingsSnapshot settings)
		{
			string json = PerfMeterSettingsStore.ToJson(PerfMeterSettingsStore.CreateFromSnapshot(settings));
			string verbatimJson = json.Replace("\"", "\"\"");
			return @"using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
	private const string SettingsJson = @""" + verbatimJson + @""";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
    {
		// Applies collection, overlay, logging, alert, session, and overdraw settings.
		if (!PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning))
		{
			Debug.LogWarning(""[SGG.PerfMeter] Bootstrap settings were rejected: "" + warning);
		}
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

		internal static PerfMeterCompatibilityStatus GetCompatibilityStatus()
		{
			return GetCompatibilityStatus(PerfMeterRenderPipelineDetector.CreateSnapshot());
		}

		private static PerfMeterCompatibilityStatus GetCompatibilityStatus(PerfMeterRenderPipelineSnapshot renderPipeline)
		{
			GetRegisteredPipelinePackage(renderPipeline.Kind, out string packageName, out string packageVersion);
			bool adapterAvailable = renderPipeline.Kind == PerfMeterRenderPipelineKind.Universal
				? IsRenderGraphFeatureAvailable
				: renderPipeline.Kind == PerfMeterRenderPipelineKind.HighDefinition && IsHdrpCustomPassAvailable;
			return PerfMeterCompatibilityEvaluator.Evaluate(
				Application.unityVersion,
				renderPipeline.Kind,
				packageName,
				packageVersion,
				adapterAvailable);
		}

		internal static PerfMeterSetupStatus GetStatus()
		{
			PerfMeterRenderPipelineSnapshot renderPipeline = PerfMeterRenderPipelineDetector.CreateSnapshot();
			PerfMeterCompatibilityStatus compatibilityStatus = GetCompatibilityStatus(renderPipeline);
			PerfMeterSetupStatus status = new PerfMeterSetupStatus
			{
				FrameTimingStatsEnabled = PlayerSettings.enableFrameTimingStats,
				CompatibilityStatus = compatibilityStatus,
				OfficialUnityVersionSupported = compatibilityStatus.CoreRuntimeCompatible,
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

		private static void GetRegisteredPipelinePackage(PerfMeterRenderPipelineKind pipelineKind, out string packageName, out string packageVersion)
		{
			string expectedPackageName = GetExpectedPipelinePackageName(pipelineKind);
			packageName = string.Empty;
			packageVersion = string.Empty;
			if (string.IsNullOrEmpty(expectedPackageName))
			{
				return;
			}

			UnityEditor.PackageManager.PackageInfo[] packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
			if (packages == null)
			{
				return;
			}

			for (int i = 0; i < packages.Length; i++)
			{
				UnityEditor.PackageManager.PackageInfo packageInfo = packages[i];
				if (packageInfo != null && string.Equals(packageInfo.name, expectedPackageName, StringComparison.OrdinalIgnoreCase))
				{
					packageName = packageInfo.name ?? expectedPackageName;
					packageVersion = packageInfo.version ?? string.Empty;
					return;
				}
			}
		}

		private static string GetExpectedPipelinePackageName(PerfMeterRenderPipelineKind pipelineKind)
		{
			if (pipelineKind == PerfMeterRenderPipelineKind.Universal)
			{
				return UniversalRenderPipelinePackageName;
			}

			if (pipelineKind == PerfMeterRenderPipelineKind.HighDefinition)
			{
				return HighDefinitionRenderPipelinePackageName;
			}

			return string.Empty;
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
			return RuntimePerformanceMeter.ApplySettings(settings)
				? InstallResult.Ok("PerfMeter JSON settings applied to the active Play Mode session.")
				: InstallResult.Fail("PerfMeter JSON settings are valid, but the active runtime could not apply them while cleanup or shutdown is pending.");
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

		internal static class PerfMeterCompatibilityEvaluator
		{
			internal static PerfMeterCompatibilityStatus Evaluate(
				string unityVersion,
				PerfMeterRenderPipelineKind pipelineKind,
				string pipelinePackageName,
				string pipelinePackageVersion,
				bool adapterAvailable)
			{
				string currentUnityVersion = unityVersion ?? string.Empty;
				string currentPipelinePackageName = pipelinePackageName ?? string.Empty;
				string currentPipelinePackageVersion = pipelinePackageVersion ?? string.Empty;
				bool unityVersionParsed = TryParseMajorMinor(currentUnityVersion, out _, out _);
				bool importCompatible = unityVersionParsed && IsAtLeast(currentUnityVersion, ImportUnityVersionFloor);
				bool coreRuntimeCompatible = unityVersionParsed && IsAtLeast(currentUnityVersion, CoreRuntimeUnityVersionFloor);

				string importReason = GetUnityReason(
					currentUnityVersion,
					unityVersionParsed,
					importCompatible,
					ImportUnityVersionFloor,
					"Import compatibility requires Unity 2022.3+.");
				string coreRuntimeReason = GetUnityReason(
					currentUnityVersion,
					unityVersionParsed,
					coreRuntimeCompatible,
					CoreRuntimeUnityVersionFloor,
					"Core runtime compatibility requires Unity 6000.4+ and does not require URP or HDRP.");
				if (coreRuntimeCompatible)
				{
					coreRuntimeReason += " Core runtime does not require URP or HDRP.";
				}
				bool renderIntegrationCompatible = false;
				string renderIntegrationReason;

				if (!coreRuntimeCompatible)
				{
					renderIntegrationReason = "Render integration requires core runtime compatibility on Unity 6000.4+.";
				}
				else if (pipelineKind == PerfMeterRenderPipelineKind.BuiltIn)
				{
					renderIntegrationReason = "Built-in Render Pipeline is unsupported; use URP or HDRP for render integration.";
				}
				else if (pipelineKind != PerfMeterRenderPipelineKind.Universal && pipelineKind != PerfMeterRenderPipelineKind.HighDefinition)
				{
					renderIntegrationReason = "The active render pipeline is unknown; use URP or HDRP for render integration.";
				}
				else
				{
					string expectedPackageName = pipelineKind == PerfMeterRenderPipelineKind.Universal
						? UniversalRenderPipelinePackageName
						: HighDefinitionRenderPipelinePackageName;
					string pipelineDisplayName = pipelineKind == PerfMeterRenderPipelineKind.Universal ? "URP" : "HDRP";
					bool packageNameMatches = string.Equals(currentPipelinePackageName, expectedPackageName, StringComparison.OrdinalIgnoreCase);
					bool packageVersionParsed = TryParseMajorMinor(currentPipelinePackageVersion, out _, out _);
					bool packageVersionCompatible = packageVersionParsed && IsAtLeast(currentPipelinePackageVersion, RenderIntegrationPipelinePackageVersionFloor);

					if (!packageNameMatches)
					{
						renderIntegrationReason = pipelineDisplayName + " requires registered package " + expectedPackageName + "; current package metadata is unavailable or names a different package.";
					}
					else if (!packageVersionParsed)
					{
						renderIntegrationReason = pipelineDisplayName + " package version '" + currentPipelinePackageVersion + "' is malformed; " + expectedPackageName + " 17.4+ is required.";
					}
					else if (!packageVersionCompatible)
					{
						renderIntegrationReason = pipelineDisplayName + " package version " + currentPipelinePackageVersion + " is below the required " + RenderIntegrationPipelinePackageVersionFloor + "+ floor; update " + expectedPackageName + ".";
					}
					else if (!adapterAvailable)
					{
						renderIntegrationReason = "PerfMeter " + pipelineDisplayName + " adapter assembly is unavailable; reimport the package with " + pipelineDisplayName + " " + RenderIntegrationPipelinePackageVersionFloor + "+ installed.";
					}
					else
					{
						renderIntegrationCompatible = true;
						renderIntegrationReason = pipelineDisplayName + " " + RenderIntegrationPipelinePackageVersionFloor + "+ is active and the corresponding PerfMeter adapter assembly is available.";
					}
				}

				return new PerfMeterCompatibilityStatus(
					currentUnityVersion,
					pipelineKind,
					currentPipelinePackageName,
					currentPipelinePackageVersion,
					importCompatible,
					coreRuntimeCompatible,
					renderIntegrationCompatible,
					importReason,
					coreRuntimeReason,
					renderIntegrationReason);
			}

			internal static bool IsAtLeast(string version, string floor)
			{
				if (!TryParseMajorMinor(version, out int major, out int minor) ||
					!TryParseMajorMinor(floor, out int floorMajor, out int floorMinor))
				{
					return false;
				}

				return IsAtLeast(major, minor, floorMajor, floorMinor);
			}

			internal static bool TryParseMajorMinor(string version, out int major, out int minor)
			{
				major = 0;
				minor = 0;
				if (string.IsNullOrEmpty(version))
				{
					return false;
				}

				string value = version.Trim();
				if (value.Length == 0)
				{
					return false;
				}

				int index = 0;
				if (!TryReadNumber(value, ref index, out major) || index >= value.Length || value[index] != '.')
				{
					return false;
				}

				index++;
				if (!TryReadNumber(value, ref index, out minor))
				{
					return false;
				}

				if (index == value.Length)
				{
					return true;
				}

				if (value[index] == '.')
				{
					index++;
					if (!TryReadNumber(value, ref index, out int _))
					{
						return false;
					}

					if (index == value.Length)
					{
						return true;
					}

					if (value[index] == '.')
					{
						return false;
					}
				}
				else if (value[index] != '-' && value[index] != '+' && !IsAsciiLetter(value[index]))
				{
					return false;
				}

				if (index == value.Length)
				{
					return false;
				}

				return IsValidVersionSuffix(value, index);
			}

			private static bool IsValidVersionSuffix(string value, int index)
			{
				char previous = '\0';
				for (; index < value.Length; index++)
				{
					char character = value[index];
					if (!IsAsciiLetterOrDigit(character) && character != '.' && character != '-' && character != '+')
					{
						return false;
					}

					if ((character == '.' || character == '-' || character == '+') &&
						(index == value.Length - 1 || previous == '.' || previous == '-' || previous == '+'))
					{
						return false;
					}

					previous = character;
				}

				return true;
			}

			private static string GetUnityReason(string version, bool parsed, bool compatible, string floor, string requirement)
			{
				if (!parsed)
				{
					return "Unity version '" + version + "' is malformed; " + requirement;
				}

				return compatible
					? "Unity " + version + " meets the " + floor + "+ floor."
					: "Unity " + version + " is below the " + floor + "+ floor; " + requirement;
			}

			private static bool TryReadNumber(string value, ref int index, out int number)
			{
				number = 0;
				int start = index;
				long parsed = 0;
				while (index < value.Length && IsAsciiDigit(value[index]))
				{
					parsed = parsed * 10 + (value[index] - '0');
					if (parsed > int.MaxValue)
					{
						return false;
					}

					index++;
				}

				if (index == start)
				{
					return false;
				}

				number = (int)parsed;
				return true;
			}

			private static bool IsAtLeast(int major, int minor, int floorMajor, int floorMinor)
			{
				return major > floorMajor || major == floorMajor && minor >= floorMinor;
			}

			private static bool IsAsciiDigit(char character)
			{
				return character >= '0' && character <= '9';
			}

			private static bool IsAsciiLetter(char character)
			{
				return character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
			}

			private static bool IsAsciiLetterOrDigit(char character)
			{
				return IsAsciiDigit(character) || IsAsciiLetter(character);
			}
		}

		internal sealed class PerfMeterSetupStatus
		{
			internal bool FrameTimingStatsEnabled;
			internal PerfMeterCompatibilityStatus CompatibilityStatus;
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
