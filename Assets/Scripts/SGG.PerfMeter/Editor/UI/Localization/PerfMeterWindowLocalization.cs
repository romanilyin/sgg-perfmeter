using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using SGG.PerfMeter.Editor.Setup;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SGG.PerfMeter.Editor.UI.Localization
{
	internal static class PerfMeterWindowLocalization
	{
		internal const string DefaultLanguage = "en";
		internal const string LanguagePrefsKey = "SGG.PerfMeter.SetupWindow.Language";

		private const string LocalizationFolder = "Editor/UI/Localization";
		private const string FilePrefix = "perfmeter-window.";
		private const string FileExtension = ".xlf";

		private static readonly Dictionary<string, LocalizationFile> Files = new Dictionary<string, LocalizationFile>(StringComparer.OrdinalIgnoreCase);
		private static List<LanguageOption> _availableLanguages;

		internal sealed class LanguageOption
		{
			internal LanguageOption(string code, string displayName)
			{
				Code = code;
				DisplayName = displayName;
			}

			internal string Code { get; }
			internal string DisplayName { get; }
		}

		private sealed class LocalizationFile
		{
			internal readonly Dictionary<string, string> SourceToTarget = new Dictionary<string, string>(StringComparer.Ordinal);
			internal readonly Dictionary<string, string> IdToTarget = new Dictionary<string, string>(StringComparer.Ordinal);
		}

		internal static string CurrentLanguage
		{
			get => NormalizeLanguage(EditorPrefs.GetString(LanguagePrefsKey, DefaultLanguage));
			set => EditorPrefs.SetString(LanguagePrefsKey, NormalizeLanguage(value));
		}

		internal static List<LanguageOption> AvailableLanguages()
		{
			if (_availableLanguages != null)
			{
				return new List<LanguageOption>(_availableLanguages);
			}

			List<LanguageOption> languages = new List<LanguageOption>();
			string folder = LocalizationDirectoryPath();
			if (Directory.Exists(folder))
			{
				string[] files = Directory.GetFiles(folder, FilePrefix + "*" + FileExtension, SearchOption.TopDirectoryOnly);
				Array.Sort(files, StringComparer.OrdinalIgnoreCase);
				for (int i = 0; i < files.Length; i++)
				{
					string code = LanguageCodeFromFile(files[i]);
					if (!string.IsNullOrEmpty(code) && !ContainsLanguage(languages, code))
					{
						languages.Add(new LanguageOption(code, LanguageDisplayName(code)));
					}
				}
			}

			if (!ContainsLanguage(languages, DefaultLanguage))
			{
				languages.Insert(0, new LanguageOption(DefaultLanguage, "English"));
			}

			languages.Sort((left, right) =>
			{
				if (string.Equals(left.Code, DefaultLanguage, StringComparison.OrdinalIgnoreCase))
				{
					return -1;
				}

				if (string.Equals(right.Code, DefaultLanguage, StringComparison.OrdinalIgnoreCase))
				{
					return 1;
				}

				return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
			});

			_availableLanguages = languages;
			return new List<LanguageOption>(_availableLanguages);
		}

		internal static string CurrentLanguageDisplayName()
		{
			return LanguageDisplayName(CurrentLanguage);
		}

		internal static string LanguageCodeForDisplayName(string displayName)
		{
			List<LanguageOption> languages = AvailableLanguages();
			for (int i = 0; i < languages.Count; i++)
			{
				if (string.Equals(languages[i].DisplayName, displayName, StringComparison.Ordinal))
				{
					return languages[i].Code;
				}
			}

			return CurrentLanguage;
		}

		internal static int LanguageIndex(string code)
		{
			List<LanguageOption> languages = AvailableLanguages();
			for (int i = 0; i < languages.Count; i++)
			{
				if (string.Equals(languages[i].Code, code, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return 0;
		}

		internal static List<string> LanguageDisplayNames()
		{
			List<string> names = new List<string>();
			List<LanguageOption> languages = AvailableLanguages();
			for (int i = 0; i < languages.Count; i++)
			{
				names.Add(languages[i].DisplayName);
			}

			return names;
		}

		internal static string Text(string source)
		{
			if (string.IsNullOrEmpty(source))
			{
				return source;
			}

			string exact = ExactText(source);
			return string.IsNullOrEmpty(exact) ? PatternText(source) : exact;
		}

		internal static string Format(string source, params object[] args)
		{
			string exact = ExactText(source);
			return string.Format(string.IsNullOrEmpty(exact) ? source : exact, args);
		}

		internal static void ApplyTo(VisualElement root)
		{
			if (root == null)
			{
				return;
			}

			ApplyElement(root);
			foreach (VisualElement child in root.Children())
			{
				ApplyTo(child);
			}
		}

		private static string ExactText(string source)
		{
			LocalizationFile file = Load(CurrentLanguage);
			return file.SourceToTarget.TryGetValue(source, out string value) && !string.IsNullOrEmpty(value) ? value : string.Empty;
		}

		private static void ApplyElement(VisualElement element)
		{
			if (!string.IsNullOrEmpty(element.tooltip))
			{
				element.tooltip = Text(element.tooltip);
			}

			if (element is Button button && !string.IsNullOrEmpty(button.text))
			{
				button.text = Text(button.text);
				return;
			}

			if (element is ToolbarToggle toolbarToggle && !string.IsNullOrEmpty(toolbarToggle.text))
			{
				toolbarToggle.text = Text(toolbarToggle.text);
				return;
			}

			if (element is Label label && !string.IsNullOrEmpty(label.text))
			{
				label.text = Text(label.text);
				return;
			}

			if (element is Toggle toggle && !string.IsNullOrEmpty(toggle.text))
			{
				toggle.text = Text(toggle.text);
				return;
			}

			if (element is Toggle toggleWithLabel && !string.IsNullOrEmpty(toggleWithLabel.label))
			{
				toggleWithLabel.label = Text(toggleWithLabel.label);
				return;
			}

			if (element is TextField textField && !string.IsNullOrEmpty(textField.label))
			{
				textField.label = Text(textField.label);
				return;
			}

			if (element is PopupField<string> popupField && !string.IsNullOrEmpty(popupField.label))
			{
				popupField.label = Text(popupField.label);
				return;
			}

			if (element is Foldout foldout && !string.IsNullOrEmpty(foldout.text))
			{
				foldout.text = Text(foldout.text);
			}
		}

		private static string PatternText(string source)
		{
			Match match;
			if (TryMatch(source, "^(\\d+) / (\\d+) URP renderer asset\\(s\\) have PerfMeter Render Graph feature\\.( Missing feature references or package renderer assets require manual inspection\\.)?$", out match))
			{
				string warning = string.IsNullOrEmpty(match.Groups[3].Value) ? string.Empty : " " + Text(match.Groups[3].Value.TrimStart());
				return Format("{0} / {1} URP renderer asset(s) have PerfMeter Render Graph feature.{2}", match.Groups[1].Value, match.Groups[2].Value, warning);
			}

			if (TryMatch(source, "^JSON settings (.+): (enabled|disabled), auto-start (on|off), preset (.+)\\.( Warning: .+)?$", out match))
			{
				string warning = string.IsNullOrEmpty(match.Groups[5].Value) ? string.Empty : " " + Text(match.Groups[5].Value.TrimStart());
				return Format("JSON settings {0}: {1}, auto-start {2}, preset {3}.{4}", match.Groups[1].Value, Text(match.Groups[2].Value), Text(match.Groups[3].Value), match.Groups[4].Value, warning);
			}

			if (TryMatch(source, "^Selected (\\d+) renderer asset\\(s\\) missing PerfMeter feature\\.$", out match))
			{
				return Format("Selected {0} renderer asset(s) missing PerfMeter feature.", match.Groups[1].Value);
			}

			if (TryMatch(source, "^(\\d+) widgets: (\\d+) inside this package, (\\d+) in project\\.$", out match))
			{
				return Format("{0} widgets: {1} inside this package, {2} in project.", match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
			}

			if (TryMatch(source, "^Installed PerfMeter Render Graph feature in (\\d+) renderer asset\\(s\\)\\.$", out match))
			{
				return Format("Installed PerfMeter Render Graph feature in {0} renderer asset(s).", match.Groups[1].Value);
			}

			if (TryMatch(source, "^Installed PerfMeter Render Graph feature in (\\d+) selected renderer asset\\(s\\)\\.$", out match))
			{
				return Format("Installed PerfMeter Render Graph feature in {0} selected renderer asset(s).", match.Groups[1].Value);
			}

			if (TryMatch(source, "^PerfMeter JSON settings saved to (.+)\\. Runtime zero-code setup will use Resources path (.+)\\.$", out match))
			{
				return Format("PerfMeter JSON settings saved to {0}. Runtime zero-code setup will use Resources path {1}.", match.Groups[1].Value, match.Groups[2].Value);
			}

			if (TryMatch(source, "^PerfMeter settings schema (.+) is newer than supported schema (.+); defaults are used\\.$", out match))
			{
				return Format("PerfMeter settings schema {0} is newer than supported schema {1}; defaults are used.", match.Groups[1].Value, match.Groups[2].Value);
			}

			if (TryMatch(source, "^PerfMeter settings JSON is invalid: (.+)$", out match))
			{
				return Format("PerfMeter settings JSON is invalid: {0}", match.Groups[1].Value);
			}

			if (TryMatch(source, "^Active - package path (.+)\\.$", out match))
			{
				return Format("Active - package path {0}.", match.Groups[1].Value);
			}

			if (TryMatch(source, "^Active - (\\d+) / (\\d+) renderers have PerfMeter Render Graph feature\\.$", out match))
			{
				return Format("Active - {0} / {1} renderers have PerfMeter Render Graph feature.", match.Groups[1].Value, match.Groups[2].Value);
			}

			if (TryMatch(source, "^Active - (\\d+) visual overlay preset JSON file\\(s\\) available\\.$", out match))
			{
				return Format("Active - {0} visual overlay preset JSON file(s) available.", match.Groups[1].Value);
			}

			if (TryMatch(source, "^Warning - (.+)$", out match))
			{
				return Format("Warning - {0}", Text(match.Groups[1].Value));
			}

			if (TryMatch(source, "^Active - (.+)$", out match))
			{
				return Format("Active - {0}", Text(match.Groups[1].Value));
			}

			if (TryMatch(source, "^(.+) · (.+)$", out match))
			{
				return Format("{0} · {1}", Text(match.Groups[1].Value), Text(match.Groups[2].Value));
			}

			if (TryMatch(source, "^(.+) Preset block: (yes|no)\\.$", out match))
			{
				return Format("{0} Preset block: {1}.", Text(match.Groups[1].Value), Text(match.Groups[2].Value));
			}

			if (TryMatch(source, "^(\\d+) widgets$", out match))
			{
				return Format("{0} widgets", match.Groups[1].Value);
			}

			if (TryMatch(source, "^(.+) \\[([^\\]]+)\\]$", out match))
			{
				return Format("{0} [{1}]", Text(match.Groups[1].Value), match.Groups[2].Value);
			}

			if (TryMatch(source, "^Copy (.+)$", out match))
			{
				return Format("Copy {0}", Text(match.Groups[1].Value));
			}

			if (TryMatch(source, "^(.+) copied to clipboard\\.$", out match))
			{
				return Format("{0} copied to clipboard.", Text(match.Groups[1].Value));
			}

			if (TryMatch(source, "^(.+) (\\d+)% / heatmap (on|off)$", out match))
			{
				return Format("{0} {1}% / heatmap {2}", Text(match.Groups[1].Value), match.Groups[2].Value, Text(match.Groups[3].Value));
			}

			if (TryMatch(source, "^(.+): (.+)$", out match))
			{
				return Format("{0}: {1}", Text(match.Groups[1].Value), Text(match.Groups[2].Value));
			}

			return source;
		}

		private static string NormalizeLanguage(string language)
		{
			if (string.IsNullOrEmpty(language))
			{
				return DefaultLanguage;
			}

			string normalized = language.Trim().ToLowerInvariant();
			List<LanguageOption> languages = AvailableLanguages();
			for (int i = 0; i < languages.Count; i++)
			{
				if (string.Equals(languages[i].Code, normalized, StringComparison.OrdinalIgnoreCase))
				{
					return languages[i].Code;
				}
			}

			return DefaultLanguage;
		}

		private static string LanguageDisplayName(string code)
		{
			LocalizationFile file = Load(code);
			if (file.IdToTarget.TryGetValue("language.name", out string displayName) && !string.IsNullOrEmpty(displayName))
			{
				return displayName;
			}

			if (string.Equals(code, "ru", StringComparison.OrdinalIgnoreCase))
			{
				return "Русский";
			}

			return "English";
		}

		private static bool ContainsLanguage(List<LanguageOption> languages, string code)
		{
			for (int i = 0; i < languages.Count; i++)
			{
				if (string.Equals(languages[i].Code, code, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static LocalizationFile Load(string language)
		{
			string code = string.IsNullOrEmpty(language) ? DefaultLanguage : language;
			if (Files.TryGetValue(code, out LocalizationFile cached))
			{
				return cached;
			}

			LocalizationFile file = new LocalizationFile();
			string path = LocalizationFilePath(code);
			if (File.Exists(path))
			{
				ParseXliff(File.ReadAllText(path), file);
			}

			Files[code] = file;
			return file;
		}

		private static void ParseXliff(string source, LocalizationFile file)
		{
			foreach (Match match in Regex.Matches(source, "<trans-unit\\s+[^>]*id=\"([^\"]+)\"[^>]*>.*?<source[^>]*>(.*?)</source>.*?<target[^>]*>(.*?)</target>.*?</trans-unit>", RegexOptions.Singleline))
			{
				string id = XmlDecode(match.Groups[1].Value);
				string sourceText = XmlDecode(match.Groups[2].Value);
				string targetText = XmlDecode(match.Groups[3].Value);
				if (string.IsNullOrEmpty(targetText))
				{
					targetText = sourceText;
				}

				if (!string.Equals(id, "language.name", StringComparison.Ordinal) && !string.IsNullOrEmpty(sourceText) && !file.SourceToTarget.ContainsKey(sourceText))
				{
					file.SourceToTarget.Add(sourceText, targetText);
				}

				if (!string.IsNullOrEmpty(id) && !file.IdToTarget.ContainsKey(id))
				{
					file.IdToTarget.Add(id, targetText);
				}
			}
		}

		private static bool TryMatch(string source, string pattern, out Match match)
		{
			match = Regex.Match(source, pattern, RegexOptions.Singleline);
			return match.Success;
		}

		private static string XmlDecode(string value)
		{
			return WebUtility.HtmlDecode(value).Replace("&#10;", "\n").Replace("&#13;", "\r");
		}

		private static string LocalizationDirectoryPath()
		{
			UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PerfMeterWindowLocalization).Assembly);
			if (!string.IsNullOrEmpty(packageInfo?.resolvedPath))
			{
				return Path.Combine(packageInfo.resolvedPath, LocalizationFolder.Replace('/', Path.DirectorySeparatorChar));
			}

			return Path.Combine(PerfMeterSetupUtility.PackageAssetPath, LocalizationFolder.Replace('/', Path.DirectorySeparatorChar));
		}

		private static string LocalizationFilePath(string language)
		{
			return Path.Combine(LocalizationDirectoryPath(), FilePrefix + language + FileExtension);
		}

		private static string LanguageCodeFromFile(string filePath)
		{
			string name = Path.GetFileName(filePath);
			if (string.IsNullOrEmpty(name) || !name.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase) || !name.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}

			return name.Substring(FilePrefix.Length, name.Length - FilePrefix.Length - FileExtension.Length).ToLowerInvariant();
		}
	}
}
