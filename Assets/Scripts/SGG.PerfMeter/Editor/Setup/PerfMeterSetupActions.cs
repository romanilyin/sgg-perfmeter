using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SGG.PerfMeter.Editor.Setup
{
	/// <summary>
	/// Public Editor API for running the same setup actions that are exposed by the SGG PerfMeter setup window.
	/// </summary>
	public static class PerfMeterSetupActions
	{
		public static string InitializationSnippet => PerfMeterSetupUtility.InitializationSnippet;

		public static PerfMeterCompatibilityStatus GetCompatibilityStatus()
		{
			return PerfMeterSetupUtility.GetCompatibilityStatus();
		}

		public static PerfMeterSetupActionResult EnableFrameTimingStats()
		{
			return ToPublicResult(PerfMeterSetupUtility.EnableFrameTimingStats());
		}

		public static PerfMeterSetupActionResult InstallRendererFeatures()
		{
			return ToPublicResult(PerfMeterSetupUtility.InstallRendererFeatures());
		}

		public static PerfMeterSetupActionResult InstallRendererFeatures(IEnumerable<string> rendererAssetPaths)
		{
			return ToPublicResult(PerfMeterSetupUtility.InstallRendererFeatures(rendererAssetPaths));
		}

		public static PerfMeterSetupActionResult CopyInitializationSnippetToClipboard()
		{
			GUIUtility.systemCopyBuffer = InitializationSnippet;
			return PerfMeterSetupActionResult.Ok("Initialization code copied to clipboard.");
		}

		public static PerfMeterSettingsSnapshot LoadSettings()
		{
			return PerfMeterSetupUtility.LoadSettingsSnapshot();
		}

		public static PerfMeterSetupActionResult CreateDefaultSettings()
		{
			return ToPublicResult(PerfMeterSetupUtility.CreateDefaultSettings());
		}

		public static PerfMeterSetupActionResult EnsureDefaultOverlayPresets()
		{
			return ToPublicResult(PerfMeterOverlayPresetEditorUtility.EnsureDefaultOverlayPresets());
		}

		public static PerfMeterSetupActionResult SaveSettings(PerfMeterSettingsSnapshot settings)
		{
			return ToPublicResult(PerfMeterSetupUtility.SaveSettingsSnapshot(settings));
		}

		public static PerfMeterSetupActionResult SaveActiveOverlayPreset(string presetId)
		{
			return ToPublicResult(PerfMeterSetupUtility.SaveActiveOverlayPreset(presetId));
		}

		public static PerfMeterSetupActionResult ApplySettingsToRuntime()
		{
			return ToPublicResult(PerfMeterSetupUtility.ApplySettingsToRuntime());
		}

		public static PerfMeterSetupActionResult RunRecommendedSetup()
		{
			PerfMeterSetupActionResult frameTimingResult = EnableFrameTimingStats();
			PerfMeterSetupActionResult rendererResult = InstallRendererFeatures();
			PerfMeterSetupActionResult settingsResult = EnsureRecommendedSettings();
			bool success = frameTimingResult.Success && rendererResult.Success && settingsResult.Success;
			string message = frameTimingResult.Message + "\n" + rendererResult.Message + "\n" + settingsResult.Message;
			return success ? PerfMeterSetupActionResult.Ok(message) : PerfMeterSetupActionResult.Fail(message);
		}

		internal static PerfMeterSetupActionResult EnsureRecommendedSettings()
		{
			PerfMeterSettingsSnapshot existingSettings = LoadSettings();
			if (existingSettings.LoadState == PerfMeterSettingsLoadState.Loaded)
			{
				return PerfMeterSetupActionResult.Ok("Existing PerfMeter project settings were preserved.");
			}

			if (existingSettings.LoadState == PerfMeterSettingsLoadState.Missing)
			{
				return CreateDefaultSettings();
			}

			return PerfMeterSetupActionResult.Fail("Existing PerfMeter project settings were not overwritten. " + existingSettings.Warning);
		}

		public static string GetStatusReport()
		{
			PerfMeterSetupUtility.PerfMeterSetupStatus status = PerfMeterSetupUtility.GetStatus();
			StringBuilder builder = new StringBuilder(512);
			builder.Append("SGG PerfMeter Setup Status\n");
			builder.Append("Compatibility: ");
			builder.Append(status.CompatibilityMessage);
			builder.Append('\n');
			builder.Append("Import compatibility: ");
			builder.Append(status.CompatibilityStatus.ImportCompatible ? "Compatible" : "Incompatible");
			builder.Append(" - ");
			builder.Append(status.CompatibilityStatus.ImportReason);
			builder.Append('\n');
			builder.Append("Core runtime compatibility: ");
			builder.Append(status.CompatibilityStatus.CoreRuntimeCompatible ? "Compatible" : "Incompatible");
			builder.Append(" - ");
			builder.Append(status.CompatibilityStatus.CoreRuntimeReason);
			builder.Append('\n');
			builder.Append("Render integration compatibility: ");
			builder.Append(status.CompatibilityStatus.RenderIntegrationCompatible ? "Compatible" : "Incompatible");
			builder.Append(" - ");
			builder.Append(status.CompatibilityStatus.RenderIntegrationReason);
			builder.Append('\n');
			builder.Append("Compatibility environment: Unity ");
			builder.Append(status.CompatibilityStatus.CurrentUnityVersion);
			builder.Append(", pipeline ");
			builder.Append(status.CompatibilityStatus.CurrentPipelineKind);
			builder.Append(", package ");
			builder.Append(string.IsNullOrEmpty(status.CompatibilityStatus.CurrentPipelinePackageName)
				? "Not found"
				: status.CompatibilityStatus.CurrentPipelinePackageName + " " + status.CompatibilityStatus.CurrentPipelinePackageVersion);
			builder.Append(" (floors: import Unity ");
			builder.Append(status.CompatibilityStatus.ImportUnityVersionFloor);
			builder.Append(", core Unity ");
			builder.Append(status.CompatibilityStatus.CoreRuntimeUnityVersionFloor);
			builder.Append(", render package ");
			builder.Append(status.CompatibilityStatus.RenderIntegrationPipelinePackageVersionFloor);
			builder.Append(")\n");
			builder.Append("Frame Timing Stats: ");
			builder.Append(status.FrameTimingStatsEnabled ? "Enabled" : "Disabled");
			builder.Append('\n');
			builder.Append("Active Render Pipeline: ");
			builder.Append(status.ActiveRenderPipeline);
			if (!string.IsNullOrEmpty(status.RenderPipelineAssetName))
			{
				builder.Append(" (");
				builder.Append(status.RenderPipelineAssetName);
				builder.Append(')');
			}
			builder.Append('\n');
			if (status.ActiveRenderPipeline == PerfMeterRenderPipelineKind.HighDefinition)
			{
				builder.Append("HDRP Custom Pass: ");
				builder.Append(status.HdrpCustomPassAvailable ? "Available" : "Unavailable");
				builder.Append('\n');
			}
			builder.Append("Package Path: ");
			builder.Append(string.IsNullOrEmpty(status.PackageAssetPath) ? "Not found" : status.PackageAssetPath);
			builder.Append('\n');
			builder.Append("Settings: ");
			builder.Append(status.Settings.Message);
			builder.Append('\n');
			builder.Append("Settings Path: ");
			builder.Append(status.Settings.AssetPath);
			builder.Append('\n');
			builder.Append(status.RendererMessage);

			for (int i = 0; i < status.Renderers.Count; i++)
			{
				PerfMeterSetupUtility.RendererSetupStatus renderer = status.Renderers[i];
				builder.Append('\n');
				builder.Append(renderer.HasPerfMeterFeature ? "OK " : renderer.IsEditable ? "Missing " : "Not editable ");
				builder.Append(string.IsNullOrEmpty(renderer.Name) ? "Renderer" : renderer.Name);
				if (renderer.IsActive)
				{
					builder.Append(" (active)");
				}

				if (renderer.IsInPackage)
				{
					builder.Append(" (inside Packages)");
				}

				if (renderer.HasMissingFeatureReference)
				{
					builder.Append(" (has missing feature reference)");
				}

				builder.Append(" - ");
				builder.Append(renderer.AssetPath);
			}

			return builder.ToString();
		}

		private static PerfMeterSetupActionResult ToPublicResult(PerfMeterSetupUtility.InstallResult result)
		{
			return result.Success ? PerfMeterSetupActionResult.Ok(result.Message) : PerfMeterSetupActionResult.Fail(result.Message);
		}
	}

	public readonly struct PerfMeterCompatibilityStatus
	{
		internal PerfMeterCompatibilityStatus(
			string currentUnityVersion,
			PerfMeterRenderPipelineKind currentPipelineKind,
			string currentPipelinePackageName,
			string currentPipelinePackageVersion,
			bool importCompatible,
			bool coreRuntimeCompatible,
			bool renderIntegrationCompatible,
			string importReason,
			string coreRuntimeReason,
			string renderIntegrationReason)
		{
			CurrentUnityVersion = currentUnityVersion ?? string.Empty;
			CurrentPipelineKind = currentPipelineKind;
			CurrentPipelinePackageName = currentPipelinePackageName ?? string.Empty;
			CurrentPipelinePackageVersion = currentPipelinePackageVersion ?? string.Empty;
			ImportUnityVersionFloor = PerfMeterSetupUtility.ImportUnityVersionFloor;
			CoreRuntimeUnityVersionFloor = PerfMeterSetupUtility.CoreRuntimeUnityVersionFloor;
			RenderIntegrationPipelinePackageVersionFloor = PerfMeterSetupUtility.RenderIntegrationPipelinePackageVersionFloor;
			ImportCompatible = importCompatible;
			CoreRuntimeCompatible = coreRuntimeCompatible;
			RenderIntegrationCompatible = renderIntegrationCompatible;
			ImportReason = importReason ?? string.Empty;
			CoreRuntimeReason = coreRuntimeReason ?? string.Empty;
			RenderIntegrationReason = renderIntegrationReason ?? string.Empty;
		}

		public bool ImportCompatible { get; }

		public bool CoreRuntimeCompatible { get; }

		public bool RenderIntegrationCompatible { get; }

		public string CurrentUnityVersion { get; }

		public PerfMeterRenderPipelineKind CurrentPipelineKind { get; }

		public string CurrentPipelinePackageName { get; }

		public string CurrentPipelinePackageVersion { get; }

		public string ImportUnityVersionFloor { get; }

		public string CoreRuntimeUnityVersionFloor { get; }

		public string RenderIntegrationPipelinePackageVersionFloor { get; }

		public string ImportReason { get; }

		public string CoreRuntimeReason { get; }

		public string RenderIntegrationReason { get; }
	}

	public readonly struct PerfMeterSetupActionResult
	{
		private PerfMeterSetupActionResult(bool success, string message)
		{
			Success = success;
			Message = message ?? string.Empty;
		}

		public bool Success { get; }

		public string Message { get; }

		public static PerfMeterSetupActionResult Ok(string message)
		{
			return new PerfMeterSetupActionResult(true, message);
		}

		public static PerfMeterSetupActionResult Fail(string message)
		{
			return new PerfMeterSetupActionResult(false, message);
		}

		public override string ToString()
		{
			return (Success ? "OK: " : "Failed: ") + Message;
		}
	}
}
