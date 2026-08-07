using NUnit.Framework;
using SGG.PerfMeter.Editor.Setup;
using SGG.PerfMeter.Editor.UI;
using SGG.PerfMeter.Editor.UI.Localization;
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

		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			_previousLanguage = PerfMeterWindowLocalization.CurrentLanguage;
			PerfMeterWindowLocalization.CurrentLanguage = PerfMeterWindowLocalization.DefaultLanguage;
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			if (_window != null)
			{
				Object.DestroyImmediate(_window);
				_window = null;
			}

			PerfMeterWindowLocalization.CurrentLanguage = _previousLanguage;
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
			Assert.That(_window.rootVisualElement.Q<PopupField<string>>("settings-active-overlay-preset"), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Label>("settings-preset-schema-version"), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Label>("settings-preset-reserved-widget-metadata"), Is.Not.Null);

			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3SessionAnalysisButtonName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3ProfileAnalyzerButtonName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3RefreshButtonName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3StartSessionButtonName), Is.Not.Null);
			Assert.That(_window.rootVisualElement.Q<Button>(PerfMeterSetupWindow.RuntimeP3StopSessionButtonName), Is.Not.Null);
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

		private void CreateWindow()
		{
			_window = ScriptableObject.CreateInstance<PerfMeterSetupWindow>();
			Assert.DoesNotThrow(_window.CreateGUI);
		}
	}
}
