using System;
using System.IO;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Setup;
using SGG.PerfMeter.Editor.UI;
using SGG.PerfMeter.Editor.UI.Localization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterSetupWindowTests
	{
		private static readonly string[] ExpectedPersistedSettingElements =
		{
			"settings-status",
			"settings-schema-version",
			"settings-json-path",
			"settings-resources-path",
			"settings-preset-description",
			"settings-preset-tags",
			"settings-preset-schema-version",
			"settings-preset-reserved-widget-metadata",
			"settings-preset-max-width",
			"settings-preset-gap",
			"settings-widget-composition",
			"settings-enabled",
			"settings-auto-start",
			"settings-collect-metrics",
			"settings-collection-mode",
			"settings-overlay-visible",
			"settings-overdraw-diagnostics",
			"settings-target-fps",
			"settings-overlay-corner",
			"settings-overlay-theme",
			"settings-overlay-layout",
			"settings-overlay-font-family",
			"settings-overlay-scale",
			"settings-overlay-opacity",
			"settings-overlay-font-size",
			"settings-overlay-refresh-interval",
			"settings-overlay-graph-history",
			"settings-editor-warnings-enabled",
			"settings-structured-logs-enabled",
			"settings-editor-warning-cooldown",
			"settings-structured-log-cooldown",
			"settings-callback-cooldown",
			"settings-alert-overdraw-threshold",
			"settings-alert-timing-frames",
			"settings-alert-fps-frames",
			"settings-alert-gpu-timing-unavailable-frames",
			"settings-alert-overdraw-frames",
			"settings-session-warmup-frames",
			"settings-session-warmup-seconds",
			"settings-session-sample-interval",
			"settings-session-max-samples",
			"settings-session-reset-on-scene-load",
			"settings-session-scene-load-ignore-frames",
			"settings-session-scene-load-ignore-seconds",
			"settings-overdraw-default-frame-count",
			"settings-overdraw-max-frame-count",
			"settings-active-overlay-preset",
			"settings-legacy-active-preset",
			"settings-legacy-overlay-modules"
		};

		private PerfMeterSetupWindow _window;
		private string _previousLanguage;
		private bool _presetRootExisted;
		private bool _resourcesRootExisted;
		private bool _settingsRootExisted;

		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterFtueState.ResetChoices();
			_previousLanguage = PerfMeterWindowLocalization.CurrentLanguage;
			PerfMeterWindowLocalization.CurrentLanguage = PerfMeterWindowLocalization.DefaultLanguage;
			_presetRootExisted = AssetDatabase.IsValidFolder("Assets/SGG PerfMeter");
			_resourcesRootExisted = AssetDatabase.IsValidFolder("Assets/Resources");
			_settingsRootExisted = AssetDatabase.IsValidFolder("Assets/Resources/SGG.PerfMeter");
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterFtueState.ResetChoices();
			if (_window != null)
			{
				UnityEngine.Object.DestroyImmediate(_window);
				_window = null;
			}

			PerfMeterWindowLocalization.CurrentLanguage = _previousLanguage;
			if (!_presetRootExisted && AssetDatabase.IsValidFolder("Assets/SGG PerfMeter"))
			{
				AssetDatabase.DeleteAsset("Assets/SGG PerfMeter");
			}

			if (!_settingsRootExisted && AssetDatabase.IsValidFolder("Assets/Resources/SGG.PerfMeter"))
			{
				AssetDatabase.DeleteAsset("Assets/Resources/SGG.PerfMeter");
			}

			if (!_resourcesRootExisted && AssetDatabase.IsValidFolder("Assets/Resources"))
			{
				AssetDatabase.DeleteAsset("Assets/Resources");
			}
		}

		[Test]
		public void SetupWindowExposesAllPersistedSettingsAndP3Actions()
		{
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
			CreateWindow();

			for (int index = 0; index < ExpectedPersistedSettingElements.Length; index++)
			{
				string elementName = ExpectedPersistedSettingElements[index];
				Assert.That(_window.rootVisualElement.Q<VisualElement>(elementName), Is.Not.Null, "Missing persisted settings element: " + elementName);
			}

			Assert.That(_window.rootVisualElement.Q<Toggle>("settings-enabled"), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Toggle>(PerfMeterSetupWindow.SettingsStructuredLogsEnabledElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<PopupField<string>>("settings-active-overlay-preset"), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.VisualPresetSaveButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Label>("settings-preset-schema-version"), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Label>("settings-preset-reserved-widget-metadata"), Is.Not.Null);

			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3SessionAnalysisButtonName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3ProfileAnalyzerButtonName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3RefreshButtonName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3StartSessionButtonName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3StopSessionButtonName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<ToolbarToggle>(PerfMeterSetupWindow.FtueTabElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<VisualElement>(PerfMeterFtuePage.FtueRootElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Toggle>(PerfMeterFtuePage.EditorWarningLogsToggleElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Toggle>(PerfMeterFtuePage.StructuredLogsToggleElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.ReviewFtueButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RefreshInitializationCodeButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.CopyInitializationCodeButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<VisualElement>(PerfMeterFtuePage.RequiredPackagePathElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalMemoryProfilerElementName + "-install").text, Is.EqualTo("Install"));
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalMemoryProfilerOpenWindowButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalMemoryProfilerCopySnippetButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalMemoryProfilerCopyTriggerSnippetButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalMemoryProfilerRevealSnapshotsButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalProfileAnalyzerOpenButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalGraphicsStateCollectionCopyTraceButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalGraphicsStateCollectionCopyPrewarmButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalGraphicsStateCollectionRevealArtifactsButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalRenderDocElementName + "-open").text, Is.EqualTo("Download RenderDoc"));
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalRenderDocDownloadBridgeButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalRenderDocInstallLocalBridgeButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalRenderDocCancelBridgeDownloadButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalRenderDocRemoveBridgeButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalRenderDocCheckAttachmentButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalRenderDocCopySnippetButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalRenderDocGuideButtonElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterFtuePage.OptionalPixElementName + "-open").text, Is.EqualTo("Download PIX"));
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterFtuePage.FtueTitleElementName).text, Does.Contain("first-time setup"));
			Assert.That(_window.rootVisualElement.Q<Label>(className: "pm-title").text, Does.Contain(PerfMeterFtueState.PackageVersion));
			Assert.That(_window.titleContent.text, Does.Contain(PerfMeterFtueState.PackageVersion));
			Assert.That(PerfMeterFtuePage.RenderDocDownloadUrl, Is.EqualTo("https://renderdoc.org/builds"));
			Assert.That(PerfMeterFtuePage.RenderDocIntegrationGuideUrl, Is.EqualTo("https://docs.unity3d.com/6000.0/Documentation/Manual/RenderDocIntegration.html"));
			Assert.That(PerfMeterFtuePage.PixDownloadUrl, Is.EqualTo("https://devblogs.microsoft.com/pix/download/"));
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3SessionAnalysisButtonName).enabledSelf, Is.True);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3ProfileAnalyzerButtonName).enabledSelf, Is.True);
			Button startSession = _window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3StartSessionButtonName);
			Button stopSession = _window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3StopSessionButtonName);
			Assert.That(startSession.enabledSelf, Is.False);
			Assert.That(stopSession.enabledSelf, Is.False);
			Assert.That(startSession.ClassListContains("pm-button--active"), Is.False);
			Assert.That(stopSession.ClassListContains("pm-button--active"), Is.True);
			Assert.That(PerfMeterSetupWindow.ProjectDefaultOverdrawRequestFrameCount, Is.EqualTo(0));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void SavingFpsOnlyPresetMakesItActiveForProjectReload()
		{
			string settingsPath = PerfMeterSettingsStore.ResourcesAssetPath;
			string presetPath = PerfMeterOverlayPresetEditorUtility.PresetPathForId(PerfMeterOverlayPresetDefaults.FpsOnlyId, PerfMeterOverlayPresetEditorUtility.ProjectPresetFolder);
			FileBackup settingsBackup = FileBackup.Capture(settingsPath);
			FileBackup presetBackup = FileBackup.Capture(presetPath);
			try
			{
				Assert.That(PerfMeterSetupActions.CreateDefaultSettings().Success, Is.True);
				Assert.That(PerfMeterSettingsStore.TryReadJson(File.ReadAllText(settingsPath), out PerfMeterSettingsJson initialSettings, out PerfMeterSettingsLoadState _, out string warning), Is.True, warning);
				initialSettings.enabled = false;
				initialSettings.session.maxSamples = 1234;
				initialSettings.presets = new[]
				{
					new PerfMeterPresetSettingsJson
					{
						id = "project-legacy",
						overlayVisible = false,
						targetFps = 30,
						modules = new[] { nameof(PerfMeterOverlayModule.Fps) }
					}
				};
				string initialJson = PerfMeterSettingsStore.ToJson(initialSettings);
				initialJson = initialJson.Substring(0, initialJson.LastIndexOf('}')) + ",\n  \"futureSameSchema\": {\"threshold\": 7}\n}";
				File.WriteAllText(settingsPath, initialJson);
				AssetDatabase.ImportAsset(settingsPath);

				CreateWindow();
				PopupField<string> presetField = _window.rootVisualElement.Q<PopupField<string>>(PerfMeterSetupWindow.SettingsActiveOverlayPresetElementName);
				string fpsOnlyLabel = string.Empty;
				for (int i = 0; i < presetField.choices.Count; i++)
				{
					if (presetField.choices[i].Contains("[" + PerfMeterOverlayPresetDefaults.FpsOnlyId + "]"))
					{
						fpsOnlyLabel = presetField.choices[i];
						break;
					}
				}

				Assert.That(fpsOnlyLabel, Is.Not.Empty);
				_window.SelectOverlayPresetByLabel(fpsOnlyLabel);
				Assert.That(_window.rootVisualElement.Q<EnumField>(PerfMeterSetupWindow.SettingsOverlayLayoutElementName).value, Is.EqualTo(PerfMeterOverlayLayout.FpsOnly));
				_window.rootVisualElement.Q<Toggle>(PerfMeterSetupWindow.SettingsEnabledElementName).value = true;
				_window.rootVisualElement.Q<IntegerField>(PerfMeterSetupWindow.SettingsSessionMaxSamplesElementName).value = 9876;

				PerfMeterSetupActionResult result = _window.SaveCurrentOverlayPresetForProject();
				PerfMeterSettingsSnapshot reloaded = PerfMeterSetupActions.LoadSettings();
				Assert.That(PerfMeterSettingsStore.TryReadJson(File.ReadAllText(settingsPath), out PerfMeterSettingsJson savedSettings, out PerfMeterSettingsLoadState _, out warning), Is.True, warning);

				Assert.That(result.Success, Is.True, result.Message);
				Assert.That(reloaded.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.Loaded));
				Assert.That(reloaded.ActiveOverlayPresetId, Is.EqualTo(PerfMeterOverlayPresetDefaults.FpsOnlyId));
				Assert.That(reloaded.ActiveOverlayPreset, Is.Not.Null);
				Assert.That(reloaded.ActiveOverlayPreset.id, Is.EqualTo(PerfMeterOverlayPresetDefaults.FpsOnlyId));
				Assert.That(reloaded.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.FpsOnly));
				Assert.That(reloaded.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.FpsOnly));
				Assert.That(savedSettings.enabled, Is.False);
				Assert.That(savedSettings.session.maxSamples, Is.EqualTo(1234));
				Assert.That(savedSettings.presets, Has.Length.EqualTo(1));
				Assert.That(savedSettings.presets[0].id, Is.EqualTo("project-legacy"));
				Assert.That(File.ReadAllText(settingsPath), Does.Contain("\"futureSameSchema\": {\"threshold\": 7}"));
			}
			finally
			{
				settingsBackup.Restore();
				presetBackup.Restore();
				AssetDatabase.Refresh();
			}
		}

		[Test]
		public void BuiltInPresetUpgradeDoesNotRewriteCustomPresetWidth()
		{
			PerfMeterOverlayPresetJson builtIn = PerfMeterOverlayPresetDefaults.CreateDefault();
			builtIn.style.maxWidth = 720;
			for (int i = 0; i < builtIn.widgets.Length; i++)
			{
				if (builtIn.widgets[i] != null && builtIn.widgets[i].id == "cpu.cores-bars")
				{
					builtIn.widgets[i].enabled = true;
				}
			}

			PerfMeterOverlayPresetJson custom = PerfMeterOverlayPresetDefaults.CreateCompactTiming();
			custom.id = "project-custom";
			custom.style.maxWidth = 444;

			Assert.That(PerfMeterOverlayPresetDefaults.UpgradeBuiltInPreset(builtIn), Is.True);
			Assert.That(builtIn.style.maxWidth, Is.EqualTo(PerfMeterOverlayLayoutLimits.MaxWidth));
			AssertDoesNotHaveEnabledWidget(builtIn, "cpu.cores-bars");
			Assert.That(PerfMeterOverlayPresetDefaults.UpgradeBuiltInPreset(custom), Is.False);
			Assert.That(custom.style.maxWidth, Is.EqualTo(444));
		}

		[Test]
		public void CustomPresetIdsCannotCollideWithBuiltIns()
		{
			Assert.That(PerfMeterOverlayPresetEditorUtility.CustomPresetIdForPath("Assets/default.perfmeter.overlay.json"), Is.EqualTo("default-custom"));
			Assert.That(PerfMeterOverlayPresetEditorUtility.CustomPresetIdForPath("Assets/project-wide.perfmeter.overlay.json"), Is.EqualTo("project-wide"));
			Assert.That(PerfMeterOverlayPresetEditorUtility.CustomPresetIdForPath("Assets/---.perfmeter.overlay.json"), Is.EqualTo("custom-overlay"));
			Assert.That(PerfMeterOverlayPresetEditorUtility.IsCanonicalBuiltInPresetPath("Assets/SGG PerfMeter/Presets/Overlay/default.perfmeter.overlay.json", "default"), Is.True);
			Assert.That(PerfMeterOverlayPresetEditorUtility.IsCanonicalBuiltInPresetPath("Assets/SGG PerfMeter/Presets/Overlay/custom/default.perfmeter.overlay.json", "default"), Is.False);
		}

		[Test]
		public void SettingsJsonPatcherRejectsEscapedOrLiteralDuplicateProperties()
		{
			const string escapedDuplicate = "{\"activeOverlayPresetId\":\"default\",\"activeOverlayPreset\\u0049d\":\"graphs\"}";
			const string literalDuplicate = "{\"overlayPresets\":[],\"overlayPresets\":[]}";

			Assert.That(PerfMeterSetupUtility.TrySetTopLevelJsonProperty(escapedDuplicate, "activeOverlayPresetId", "\"fps-only\"", out string escapedResult), Is.False);
			Assert.That(escapedResult, Is.EqualTo(escapedDuplicate));
			Assert.That(PerfMeterSetupUtility.TrySetTopLevelJsonProperty(literalDuplicate, "overlayPresets", "[]", out string literalResult), Is.False);
			Assert.That(literalResult, Is.EqualTo(literalDuplicate));
		}

		[Test]
		public void DuplicateProjectPresetIdsAreAllInvalid()
		{
			PerfMeterOverlayPresetJson first = PerfMeterOverlayPresetDefaults.CreateCompactTiming();
			PerfMeterOverlayPresetJson second = PerfMeterOverlayPresetDefaults.CreateGraphs();
			first.id = "duplicate-id";
			second.id = "DUPLICATE-ID";
			var assets = new System.Collections.Generic.List<PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset>
			{
				new PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset("Assets/one.json", string.Empty, first, true, string.Empty, false),
				new PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset("Assets/two.json", string.Empty, second, true, string.Empty, false)
			};

			PerfMeterOverlayPresetEditorUtility.InvalidateDuplicatePresetIds(assets);

			Assert.That(assets[0].IsValid, Is.False);
			Assert.That(assets[1].IsValid, Is.False);
			Assert.That(assets[0].Warning, Does.Contain("duplicated"));
			Assert.That(assets[1].Warning, Does.Contain("duplicated"));
		}

		[Test]
		public void RecommendedSettingsDoNotOverwriteUnsupportedJson()
		{
			string settingsPath = PerfMeterSettingsStore.ResourcesAssetPath;
			FileBackup settingsBackup = FileBackup.Capture(settingsPath);
			try
			{
				Assert.That(PerfMeterSetupActions.CreateDefaultSettings().Success, Is.True);
				const string unsupportedJson = "{\"schemaVersion\":999,\"sentinel\":\"keep\"}";
				File.WriteAllText(settingsPath, unsupportedJson);
				AssetDatabase.ImportAsset(settingsPath);

				PerfMeterSetupActionResult result = PerfMeterSetupActions.EnsureRecommendedSettings();

				Assert.That(result.Success, Is.False);
				Assert.That(result.Message, Does.Contain("not overwritten"));
				Assert.That(File.ReadAllText(settingsPath), Is.EqualTo(unsupportedJson));
			}
			finally
			{
				settingsBackup.Restore();
				AssetDatabase.Refresh();
			}
		}

		[Test]
		public void InitializationSnippetEmbedsCompleteProjectSettingsSnapshot()
		{
			string snippet = PerfMeterSetupUtility.BuildInitializationSnippet(PerfMeterSettingsStore.Defaults);

			Assert.That(snippet, Does.Contain("PerformanceMeter.TryApplySettingsJson(SettingsJson, out string warning)"));
			Assert.That(snippet, Does.Contain("\"\"editorWarningsEnabled\"\""));
			Assert.That(snippet, Does.Contain("\"\"structuredLogsEnabled\"\""));
			Assert.That(snippet, Does.Contain("\"\"editorWarningCooldownSeconds\"\""));
			Assert.That(snippet, Does.Contain("\"\"overdrawRatioThreshold\"\""));
			Assert.That(snippet, Does.Contain("\"\"warmupFrames\"\""));
			Assert.That(snippet, Does.Contain("\"\"defaultFrameCount\"\""));
			Assert.That(snippet, Does.Contain("\"\"refreshIntervalSeconds\"\""));
			Assert.That(snippet, Does.Not.Contain("PerformanceMeter.RequestCapture"));
			Assert.That(snippet, Does.Not.Contain("PerformanceMeter.StartSession"));
		}

		[Test]
		public void LegacyCollectionControlIsNotReintroducedAsHiddenEnum()
		{
			CreateWindow();

			Assert.That(_window.rootVisualElement.Q<EnumField>(PerfMeterSetupWindow.SettingsCollectionModeElementName), Is.Null);
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.SettingsLegacyActivePresetElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.SettingsLegacyOverlayModulesElementName), Is.Not.Null);
		}

		[Test]
		public void NumericSettingsNormalizeOnFocusLossAndKeepOverdrawDependency()
		{
			CreateWindow();

			FloatField scale = _window.rootVisualElement.Q<FloatField>(PerfMeterSetupWindow.SettingsOverlayScaleElementName);
			Assert.That(PerfMeterSetupWindow.NormalizeOverlayScale(-10f), Is.EqualTo(PerfMeterSettingsStore.MinOverlayScale).Within(0.0001f));
			Assert.That(scale.tooltip, Does.Contain("focus loss"));

			IntegerField maxFrames = _window.rootVisualElement.Q<IntegerField>(PerfMeterSetupWindow.SettingsOverdrawMaxFrameCountElementName);
			IntegerField defaultFrames = _window.rootVisualElement.Q<IntegerField>(PerfMeterSetupWindow.SettingsOverdrawDefaultFrameCountElementName);
			maxFrames.value = 120;
			defaultFrames.value = PerfMeterSetupWindow.NormalizeOverdrawDefaultFrameCount(240, maxFrames);
			Assert.That(defaultFrames.value, Is.EqualTo(120));
			Assert.That(maxFrames.tooltip, Does.Contain("focus loss"));
			Assert.That(PerfMeterSetupWindow.NormalizeOverdrawDefaultFrameCount(999, maxFrames), Is.EqualTo(120));
			Assert.That(PerfMeterSetupWindow.NormalizeSessionSampleInterval(0.001f), Is.EqualTo(0.02f).Within(0.0001f));
			Assert.That(PerfMeterSetupWindow.NormalizeSessionMaxSamples(100001), Is.EqualTo(100000));
		}

		[Test]
		public void SetupChecklistRejectsInvalidJsonAndBrokenRendererReferences()
		{
			PerfMeterSetupUtility.PerfMeterSettingsSetupStatus settings = new PerfMeterSetupUtility.PerfMeterSettingsSetupStatus
			{
				FileExists = true,
				Snapshot = PerfMeterSettingsStore.Defaults
			};
			Assert.That(PerfMeterSetupWindow.IsSettingsJsonActive(settings), Is.False);
			settings.Snapshot = PerfMeterSettingsStore.ToSnapshot(PerfMeterSettingsStore.CreateDefault(), PerfMeterSettingsLoadState.Loaded, string.Empty);
			Assert.That(PerfMeterSetupWindow.IsSettingsJsonActive(settings), Is.True);

			PerfMeterSetupUtility.PerfMeterSetupStatus status = new PerfMeterSetupUtility.PerfMeterSetupStatus
			{
				OfficialUnityVersionSupported = true,
				RenderGraphFeatureAvailable = true,
				ActiveRenderPipeline = PerfMeterRenderPipelineKind.Universal
			};
			PerfMeterSetupUtility.RendererSetupStatus renderer = new PerfMeterSetupUtility.RendererSetupStatus
			{
				IsEditable = true,
				HasPerfMeterFeature = true
			};
			status.Renderers.Add(renderer);
			Assert.That(PerfMeterSetupWindow.IsRendererChecklistActive(status), Is.True);
			renderer.HasMissingFeatureReference = true;
			Assert.That(PerfMeterSetupWindow.IsRendererChecklistActive(status), Is.False);
		}

		[Test]
		public void FtueRequiredSetupRejectsMissingPackagePath()
		{
			PerfMeterSetupUtility.PerfMeterSetupStatus status = CreateReadyFtueStatus();
			Assert.That(PerfMeterFtuePage.AreRequiredSetupStepsReady(status), Is.True);

			status.PackageAssetPath = string.Empty;

			Assert.That(PerfMeterFtuePage.AreRequiredSetupStepsReady(status), Is.False);
		}

		[Test]
		public void RuntimeReadOnlyRefreshDoesNotStartRuntime()
		{
			CreateWindow();

			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.RuntimeCollectionModeElementName).text, Is.EqualTo("Stopped"));
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.RuntimeCurrentFpsElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.RuntimeCpuFrameElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.RuntimeGpuFrameElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.RuntimeStyleSummaryElementName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.RuntimeCurrentFpsElementName).text, Is.EqualTo("Unavailable"));
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.RuntimeCpuFrameElementName).text, Is.EqualTo("Unavailable"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-session-samples").text, Is.EqualTo("No session"));
			string memoryCapabilities = _window.rootVisualElement.Q<Label>("runtime-p3-memory-capabilities").text;
			Assert.That(memoryCapabilities, Is.Not.Empty);
			Assert.That(memoryCapabilities, Does.Contain("backend"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-memory-status").text, Does.Contain("cooldown unavailable"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-memory-status").text, Does.Not.Contain("0.0 s"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-graphics-state-status").text, Does.Contain("trace progress unavailable"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-graphics-state-status").text, Does.Not.Contain("0/0"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-graphics-markers").text, Does.Contain("shader creation Unavailable"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-render-integration").text, Does.Contain("passes unavailable"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-render-integration").text, Does.Contain("mode unavailable"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-grd").text, Does.Contain("support unknown"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-grd").text, Does.Contain("activity unknown"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-grd").text, Does.Not.Contain("unsupported"));
			Assert.That(_window.rootVisualElement.Q<Label>("runtime-p3-grd").text, Does.Not.Contain("inactive"));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void P3FormattingUsesExplicitSampleStates()
		{
			PerfMeterProfilerMetricCapabilitySnapshot noSample = CreateCapability(PerfMeterProfilerMetricSampleState.AvailableNoSample);
			PerfMeterProfilerMetricCapabilitySnapshot sampled = CreateCapability(PerfMeterProfilerMetricSampleState.AvailableSampled);

			Assert.That(PerfMeterSetupWindow.FormatProfilerMetric(noSample, 0L), Is.EqualTo("AvailableNoSample"));
			Assert.That(PerfMeterSetupWindow.FormatProfilerMetric(sampled, 7L), Is.EqualTo("7"));
			Assert.That(PerfMeterSetupWindow.FormatMemorySnapshotStatus(PerfMeterMemorySnapshotStatusSnapshot.NotRunning), Does.Not.Contain("0.0 s"));
			Assert.That(PerfMeterSetupWindow.FormatGraphicsStateCollectionStatus(PerfMeterGraphicsStateCollectionStatusSnapshot.Idle), Does.Not.Contain("0/0"));
			Assert.That(PerfMeterSetupWindow.FormatRenderIntegration(PerfMeterRenderIntegrationSnapshot.NotObserved), Does.Contain("passes unavailable"));
			Assert.That(PerfMeterSetupWindow.FormatRenderIntegration(CreateAvailableNotObservedRenderIntegration()), Does.Contain("no observation yet"));
			Assert.That(PerfMeterSetupWindow.FormatRenderIntegration(CreateAvailableNotObservedRenderIntegration()), Does.Not.Contain("integration unavailable"));
			Assert.That(PerfMeterSetupWindow.FormatGrdTelemetry(PerfMeterGpuResidentDrawerContextSnapshot.Unknown), Does.Contain("support unknown"));
			Assert.That(PerfMeterSetupWindow.FormatGraphicsStateCollectionCapabilities(PerfMeterGraphicsStateCollectionCapabilitiesSnapshot.Unavailable), Does.Contain("trace unavailable"));
			Assert.That(PerfMeterSetupWindow.FormatGraphicsStateCollectionCapabilities(PerfMeterGraphicsStateCollectionCapabilitiesSnapshot.Unavailable), Does.Not.Contain("trace no"));
		}

		[Test]
		public void LocalizationCanSkipRawDynamicValues()
		{
			PerfMeterWindowLocalization.CurrentLanguage = "ru";
			VisualElement root = new VisualElement();
			Label rawValue = new Label("Warning");
			rawValue.AddToClassList("pm-no-localize");
			root.Add(rawValue);

			PerfMeterWindowLocalization.ApplyTo(root);

			Assert.That(rawValue.text, Is.EqualTo("Warning"));
		}

		[Test]
		public void DynamicPresetNamesAndNamedStatusValuesSkipLocalization()
		{
			PerfMeterWindowLocalization.CurrentLanguage = "ru";
			PerfMeterOverlayPresetJson preset = PerfMeterOverlayPresetDefaults.CreateDefault();
			preset.displayName = "Memory";
			PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset asset = new PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset("Assets/memory.json", "{}", preset, true, string.Empty, false);

			Assert.That(PerfMeterSetupWindow.GetOverlayPresetChoiceLabel(asset), Does.StartWith("Memory ["));

			CreateWindow();
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.SettingsCollectionModeElementName).ClassListContains("pm-no-localize"), Is.True);
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.SettingsLegacyActivePresetElementName).ClassListContains("pm-no-localize"), Is.True);
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.RuntimeCollectionModeElementName).ClassListContains("pm-no-localize"), Is.True);
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterSetupWindow.RuntimeCurrentFpsElementName).ClassListContains("pm-no-localize"), Is.True);
		}

		[Test]
		public void FtueStatusTooltipRemainsLocalizedAfterRefresh()
		{
			PerfMeterWindowLocalization.CurrentLanguage = "ru";
			CreateWindow();

			Label icon = _window.rootVisualElement.Q<Label>(className: "pm-checklist-icon");
			Assert.That(icon, Is.Not.Null);
			Assert.That(icon.tooltip, Is.Not.Empty);
			Assert.That(icon.tooltip == "Ready" || icon.tooltip == "Error" || icon.tooltip == "Optional" || icon.tooltip == "Next action", Is.False);
		}

		[Test]
		public void FtueContinuationGuidanceIsLocalized()
		{
			PerfMeterWindowLocalization.CurrentLanguage = "ru";
			CreateWindow();

			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterFtuePage.OptionalMemoryProfilerGuidanceElementName).text, Does.StartWith("Сценарий:"));
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterFtuePage.OptionalProfileAnalyzerGuidanceElementName).text, Does.StartWith("Сценарий:"));
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterFtuePage.OptionalGraphicsStateCollectionGuidanceElementName).text, Does.StartWith("Сценарий:"));
			Assert.That(_window.rootVisualElement.Q<Label>(PerfMeterFtuePage.OptionalRenderDocGuidanceElementName).text, Does.StartWith("Сценарий:"));
		}

		[Test]
		public void DebugRowsLocalizePackageMetadataButPreserveProjectIdentity()
		{
			PerfMeterWindowLocalization.CurrentLanguage = "ru";
			VisualElement packageRow = PerfMeterSetupWindow.CreateDebugWidgetRow("Inside this package", "Memory", "Metric", "Memory", "Memory", false);
			VisualElement projectRow = PerfMeterSetupWindow.CreateDebugWidgetRow("In project", "Memory", "Custom metric provider", "CustomMetrics", "Assets/Memory.cs", false, true);

			PerfMeterWindowLocalization.ApplyTo(packageRow);
			PerfMeterWindowLocalization.ApplyTo(projectRow);

			Assert.That(packageRow.Q<Label>(className: "pm-debug-cell--name").text, Is.EqualTo("Память"));
			Assert.That(projectRow.Q<Label>(className: "pm-debug-cell--name").text, Is.EqualTo("Memory"));
			Assert.That(projectRow.Q<Label>(className: "pm-debug-cell--details").text, Is.EqualTo("Assets/Memory.cs"));
		}

		private static PerfMeterRenderIntegrationSnapshot CreateAvailableNotObservedRenderIntegration()
		{
			return new PerfMeterRenderIntegrationSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderIntegrationState.NotObserved,
				new PerfMeterRenderPipelineSnapshot(PerfMeterRenderPipelineKind.Universal, "URP", "UniversalRenderPipelineAsset", "UniversalRenderPipeline"),
				PerfMeterRenderPipelineAssetSource.GraphicsSettings,
				-1,
				-1,
				true,
				0UL,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				PerfMeterRenderPassKind.Unknown,
				string.Empty,
				string.Empty,
				0,
				string.Empty,
				PerfMeterGpuResidentDrawerContextSnapshot.Unknown,
				PerfMeterVariableRateShadingContextSnapshot.Unknown,
				default,
				string.Empty);
		}

		private static PerfMeterProfilerMetricCapabilitySnapshot CreateCapability(PerfMeterProfilerMetricSampleState sampleState)
		{
			return new PerfMeterProfilerMetricCapabilitySnapshot(
				PerfMeterProfilerMetricSemantic.ShaderGpuProgramCreation,
				sampleState,
				PerfMeterProfilerMetricResolution.Exact,
				"Render",
				"",
				"",
				"Int64",
				1,
				sampleState == PerfMeterProfilerMetricSampleState.AvailableSampled ? 1 : 0);
		}

		private static void AssertDoesNotHaveEnabledWidget(PerfMeterOverlayPresetJson preset, string widgetId)
		{
			for (int i = 0; i < preset.widgets.Length; i++)
			{
				if (preset.widgets[i] != null && string.Equals(preset.widgets[i].id, widgetId, StringComparison.Ordinal))
				{
					Assert.That(preset.widgets[i].enabled, Is.False);
					return;
				}
			}

			Assert.Fail("Missing overlay preset widget " + widgetId);
		}

		private static PerfMeterSetupUtility.PerfMeterSetupStatus CreateReadyFtueStatus()
		{
			return new PerfMeterSetupUtility.PerfMeterSetupStatus
			{
				FrameTimingStatsEnabled = true,
				CompatibilityStatus = new PerfMeterCompatibilityStatus(
					"6000.4.12f1",
					PerfMeterRenderPipelineKind.HighDefinition,
					"com.unity.render-pipelines.high-definition",
					"17.4.0",
					true,
					true,
					true,
					"Ready",
					"Ready",
					"Ready"),
				OfficialUnityVersionSupported = true,
				HdrpCustomPassAvailable = true,
				ActiveRenderPipeline = PerfMeterRenderPipelineKind.HighDefinition,
				PackageAssetPath = "Assets/Scripts/SGG.PerfMeter",
				Settings = new PerfMeterSetupUtility.PerfMeterSettingsSetupStatus
				{
					FileExists = true,
					Snapshot = PerfMeterSettingsStore.ToSnapshot(
						PerfMeterSettingsStore.CreateDefault(),
						PerfMeterSettingsLoadState.Loaded,
						string.Empty)
				}
			};
		}

		private void CreateWindow()
		{
			_window = ScriptableObject.CreateInstance<PerfMeterSetupWindow>();
			Assert.DoesNotThrow(_window.CreateGUI);
		}

		private readonly struct FileBackup
		{
			private FileBackup(string path, bool existed, string content)
			{
				_path = path;
				_existed = existed;
				_content = content;
			}

			private readonly string _path;
			private readonly bool _existed;
			private readonly string _content;

			internal static FileBackup Capture(string path)
			{
				return new FileBackup(path, File.Exists(path), File.Exists(path) ? File.ReadAllText(path) : string.Empty);
			}

			internal void Restore()
			{
				if (_existed)
				{
					File.WriteAllText(_path, _content);
					AssetDatabase.ImportAsset(_path);
				}
				else if (File.Exists(_path))
				{
					AssetDatabase.DeleteAsset(_path);
				}
			}
		}
	}
}
