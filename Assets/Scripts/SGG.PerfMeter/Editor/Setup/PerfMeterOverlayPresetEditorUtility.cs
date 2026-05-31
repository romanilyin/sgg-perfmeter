using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SGG.PerfMeter.Editor.Setup
{
	internal static class PerfMeterOverlayPresetEditorUtility
	{
		internal const string ProjectPresetFolder = "Assets/SGG PerfMeter/Presets/Overlay";
		internal const string CustomPresetFolder = ProjectPresetFolder + "/custom";
		internal const string PresetFileSuffix = ".perfmeter.overlay.json";

		[MenuItem("Tools/SGG PerfMeter/Ensure Default Overlay Presets")]
		private static void EnsureDefaultOverlayPresetsMenu()
		{
			PerfMeterSetupUtility.InstallResult result = EnsureDefaultOverlayPresets();
			if (!result.Success)
			{
				EditorUtility.DisplayDialog("Ensure Default Overlay Presets Failed", result.Message, "OK");
			}
		}

		internal static PerfMeterSetupUtility.InstallResult EnsureDefaultOverlayPresets()
		{
			try
			{
				EnsurePresetFolders();
				int created = 0;
				PerfMeterOverlayPresetJson[] defaults = PerfMeterOverlayPresetDefaults.CreateDefaultPresets();
				for (int i = 0; i < defaults.Length; i++)
				{
					PerfMeterOverlayPresetJson preset = defaults[i];
					string path = PresetPathForId(preset.id, ProjectPresetFolder);
					if (File.Exists(path))
					{
						continue;
					}

					File.WriteAllText(path, PerfMeterOverlayPresetUtility.ToJson(preset));
					AssetDatabase.ImportAsset(path);
					created++;
				}

				AssetDatabase.Refresh();
				return PerfMeterSetupUtility.InstallResult.Ok(created == 0
					? "Default overlay presets already exist."
					: "Created " + created + " default overlay preset JSON file(s) in " + ProjectPresetFolder + ".");
			}
			catch (Exception exception)
			{
				return PerfMeterSetupUtility.InstallResult.Fail("Failed to ensure default overlay presets: " + exception.Message);
			}
		}

		internal static List<OverlayPresetAsset> LoadProjectPresets(bool ensureDefaults)
		{
			if (ensureDefaults)
			{
				EnsureDefaultOverlayPresets();
			}

			List<OverlayPresetAsset> presets = new List<OverlayPresetAsset>();
			if (!Directory.Exists(ProjectPresetFolder))
			{
				return presets;
			}

			string[] files = Directory.GetFiles(ProjectPresetFolder, "*" + PresetFileSuffix, SearchOption.AllDirectories);
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < files.Length; i++)
			{
				string path = NormalizeAssetPath(files[i]);
				string json = File.ReadAllText(path);
				bool valid = PerfMeterOverlayPresetUtility.TryReadJson(json, out PerfMeterOverlayPresetJson preset, out string warning);
				presets.Add(new OverlayPresetAsset(path, json, preset, valid, warning, IsReadOnlyPath(path)));
			}

			return presets;
		}

		internal static PerfMeterOverlayPresetJson[] LoadValidProjectPresetDtos(bool ensureDefaults)
		{
			List<OverlayPresetAsset> assets = LoadProjectPresets(ensureDefaults);
			List<PerfMeterOverlayPresetJson> presets = new List<PerfMeterOverlayPresetJson>();
			for (int i = 0; i < assets.Count; i++)
			{
				if (assets[i].IsValid && assets[i].Preset != null)
				{
					presets.Add(PerfMeterOverlayPresetUtility.Clone(assets[i].Preset));
				}
			}

			return presets.ToArray();
		}

		internal static void BakeOverlayPresetsIntoSettings(PerfMeterSettingsJson settings)
		{
			if (settings == null)
			{
				return;
			}

			PerfMeterOverlayPresetJson[] presets = LoadValidProjectPresetDtos(true);
			if (presets.Length == 0)
			{
				presets = PerfMeterOverlayPresetDefaults.CreateDefaultPresets();
			}

			string activeId = settings.activeOverlayPresetId;
			if (string.IsNullOrEmpty(activeId) || PerfMeterOverlayPresetUtility.FindById(presets, activeId) == null)
			{
				PerfMeterOverlayPresetJson defaultPreset = PerfMeterOverlayPresetUtility.FindById(presets, PerfMeterOverlayPresetDefaults.DefaultId);
				activeId = defaultPreset != null ? defaultPreset.id : presets[0].id;
			}

			settings.activeOverlayPresetId = activeId;
			settings.overlayPresets = presets;
		}

		internal static PerfMeterSetupUtility.InstallResult SavePreset(string assetPath, PerfMeterOverlayPresetJson preset)
		{
			try
			{
				if (string.IsNullOrEmpty(assetPath))
				{
					return PerfMeterSetupUtility.InstallResult.Fail("Overlay preset path is empty.");
				}

				if (preset == null)
				{
					return PerfMeterSetupUtility.InstallResult.Fail("Overlay preset is empty.");
				}

				EnsurePresetFolders();
				string directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
				if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				File.WriteAllText(assetPath, PerfMeterOverlayPresetUtility.ToJson(preset));
				AssetDatabase.ImportAsset(assetPath);
				AssetDatabase.Refresh();
				return PerfMeterSetupUtility.InstallResult.Ok("Overlay preset saved to " + assetPath + ".");
			}
			catch (Exception exception)
			{
				return PerfMeterSetupUtility.InstallResult.Fail("Failed to save overlay preset: " + exception.Message);
			}
		}

		internal static string SaveCustomPresetWithPanel(PerfMeterOverlayPresetJson preset, string defaultName)
		{
			EnsurePresetFolders();
			string safeName = Slug(defaultName);
			if (string.IsNullOrEmpty(safeName))
			{
				safeName = "custom-overlay";
			}

			string path = EditorUtility.SaveFilePanelInProject(
				"Save custom overlay preset",
				safeName + PresetFileSuffix,
				"json",
				"Choose where to save the custom PerfMeter overlay preset.",
				CustomPresetFolder);

			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			PerfMeterOverlayPresetJson copy = PerfMeterOverlayPresetUtility.Clone(preset);
			copy.id = Slug(Path.GetFileName(path).Replace(PresetFileSuffix, string.Empty));
			copy.displayName = string.IsNullOrEmpty(copy.displayName) ? defaultName : copy.displayName;
			SavePreset(path, copy);
			return path;
		}

		internal static void Reveal(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				return;
			}

			UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
			if (asset != null)
			{
				EditorGUIUtility.PingObject(asset);
				Selection.activeObject = asset;
			}
			else
			{
				EditorUtility.RevealInFinder(assetPath);
			}
		}

		internal static string PresetPathForId(string id, string folder)
		{
			return folder.TrimEnd('/') + "/" + Slug(id) + PresetFileSuffix;
		}

		internal static string Slug(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			string lower = value.Trim().ToLowerInvariant();
			char[] buffer = new char[lower.Length];
			int count = 0;
			bool previousDash = false;
			for (int i = 0; i < lower.Length; i++)
			{
				char character = lower[i];
				bool allowed = (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9');
				if (allowed)
				{
					buffer[count++] = character;
					previousDash = false;
				}
				else if (!previousDash && count > 0)
				{
					buffer[count++] = '-';
					previousDash = true;
				}
			}

			if (count > 0 && buffer[count - 1] == '-')
			{
				count--;
			}

			return count <= 0 ? string.Empty : new string(buffer, 0, count);
		}

		private static void EnsurePresetFolders()
		{
			EnsureFolder("Assets", "SGG PerfMeter");
			EnsureFolder("Assets/SGG PerfMeter", "Presets");
			EnsureFolder("Assets/SGG PerfMeter/Presets", "Overlay");
			EnsureFolder(ProjectPresetFolder, "custom");
		}

		private static void EnsureFolder(string parent, string name)
		{
			string path = parent + "/" + name;
			if (!AssetDatabase.IsValidFolder(path))
			{
				AssetDatabase.CreateFolder(parent, name);
			}
		}

		private static bool IsReadOnlyPath(string path)
		{
			return !string.IsNullOrEmpty(path) && path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
		}

		private static string NormalizeAssetPath(string path)
		{
			return path.Replace('\\', '/');
		}

		internal sealed class OverlayPresetAsset
		{
			internal OverlayPresetAsset(string assetPath, string json, PerfMeterOverlayPresetJson preset, bool isValid, string warning, bool readOnly)
			{
				AssetPath = assetPath ?? string.Empty;
				Json = json ?? string.Empty;
				Preset = preset;
				IsValid = isValid;
				Warning = warning ?? string.Empty;
				ReadOnly = readOnly;
			}

			internal string AssetPath { get; }
			internal string Json { get; }
			internal PerfMeterOverlayPresetJson Preset { get; }
			internal bool IsValid { get; }
			internal string Warning { get; }
			internal bool ReadOnly { get; }

			internal string DisplayName
			{
				get
				{
					if (Preset != null && !string.IsNullOrEmpty(Preset.displayName))
					{
						return Preset.displayName;
					}

					return Path.GetFileNameWithoutExtension(AssetPath);
				}
			}
		}
	}
}
