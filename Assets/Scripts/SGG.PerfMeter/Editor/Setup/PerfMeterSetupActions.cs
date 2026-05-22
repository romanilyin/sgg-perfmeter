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

		public static PerfMeterSetupActionResult SaveSettings(PerfMeterSettingsSnapshot settings)
		{
			return ToPublicResult(PerfMeterSetupUtility.SaveSettingsSnapshot(settings));
		}

		public static PerfMeterSetupActionResult ApplySettingsToRuntime()
		{
			return ToPublicResult(PerfMeterSetupUtility.ApplySettingsToRuntime());
		}

		public static PerfMeterSetupActionResult RunRecommendedSetup()
		{
			PerfMeterSetupActionResult frameTimingResult = EnableFrameTimingStats();
			PerfMeterSetupActionResult rendererResult = InstallRendererFeatures();
			PerfMeterSetupActionResult settingsResult = CreateDefaultSettings();
			bool success = frameTimingResult.Success && rendererResult.Success && settingsResult.Success;
			string message = frameTimingResult.Message + "\n" + rendererResult.Message + "\n" + settingsResult.Message;
			return success ? PerfMeterSetupActionResult.Ok(message) : PerfMeterSetupActionResult.Fail(message);
		}

		public static string GetStatusReport()
		{
			PerfMeterSetupUtility.PerfMeterSetupStatus status = PerfMeterSetupUtility.GetStatus();
			StringBuilder builder = new StringBuilder(512);
			builder.Append("SGG PerfMeter Setup Status\n");
			builder.Append("Compatibility: ");
			builder.Append(status.CompatibilityMessage);
			builder.Append('\n');
			builder.Append("Frame Timing Stats: ");
			builder.Append(status.FrameTimingStatsEnabled ? "Enabled" : "Disabled");
			builder.Append('\n');
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
