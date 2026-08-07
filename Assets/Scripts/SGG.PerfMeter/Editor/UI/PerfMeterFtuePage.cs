using System;
using System.Collections.Generic;
using System.Globalization;
using SGG.PerfMeter.Editor.Setup;
using SGG.PerfMeter.Editor.UI.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RuntimePerformanceMeter = SGG.PerfMeter.PerformanceMeter;

namespace SGG.PerfMeter.Editor.UI
{
	internal sealed class PerfMeterFtuePage
	{
		private const long RefreshIntervalMilliseconds = 500L;
		private const string OkState = "ok";
		private const string WarnState = "warn";
		private const string ErrorState = "error";
		private const string OptionalState = "optional";
		internal const string RenderDocDownloadUrl = "https://renderdoc.org/builds";
		internal const string PixDownloadUrl = "https://devblogs.microsoft.com/pix/download/";

		internal const string FtueRootElementName = "ftue-root";
		internal const string FtueTitleElementName = "ftue-title";
		internal const string EditorWarningLogsToggleElementName = "ftue-editor-warning-logs";
		internal const string StructuredLogsToggleElementName = "ftue-structured-logs";
		internal const string RequiredImportCompatibilityElementName = "ftue-required-import-compatibility";
		internal const string RequiredCoreRuntimeCompatibilityElementName = "ftue-required-core-runtime-compatibility";
		internal const string RequiredRenderIntegrationElementName = "ftue-required-render-integration";
		internal const string RequiredFrameTimingElementName = "ftue-required-frame-timing";
		internal const string RequiredPackagePathElementName = "ftue-required-package-path";
		internal const string RequiredSettingsJsonElementName = "ftue-required-settings-json";
		internal const string OptionalMemoryProfilerElementName = "ftue-optional-memory-profiler";
		internal const string OptionalAdaptivePerformanceElementName = "ftue-optional-adaptive-performance";
		internal const string OptionalProfileAnalyzerElementName = "ftue-optional-profile-analyzer";
		internal const string OptionalGraphicsStateCollectionElementName = "ftue-optional-graphics-state-collection";
		internal const string OptionalRenderDocElementName = "ftue-optional-renderdoc";
		internal const string OptionalPixElementName = "ftue-optional-pix";
		internal const string SaveLoggingSettingsButtonElementName = "ftue-save-logging-settings";

		private readonly VisualElement _root;
		private readonly Action _selectSetup;
		private readonly Action _selectPresets;
		private readonly Action _selectRuntime;
		private readonly Action<string> _reportAction;
		private readonly Action<bool> _visibilityChanged;
		private readonly List<PackageRow> _packageRows = new List<PackageRow>();

		private ChecklistRow _importCompatibilityRow;
		private ChecklistRow _coreRuntimeCompatibilityRow;
		private ChecklistRow _renderIntegrationRow;
		private ChecklistRow _frameTimingRow;
		private ChecklistRow _packagePathRow;
		private ChecklistRow _settingsJsonRow;
		private ChecklistRow _profilerDiagnosticsRow;
		private ChecklistRow _graphicsDiagnosticsRow;
		private ChecklistRow _renderDiagnosticsRow;
		private ChecklistRow _sessionDiagnosticsRow;
		private ChecklistRow _graphicsStateCollectionDiagnosticsRow;
		private OptionalCapabilityRow _graphicsStateCollectionRow;
		private OptionalCapabilityRow _renderDocRow;
		private OptionalCapabilityRow _pixRow;
		private Toggle _editorWarningLogsToggle;
		private Toggle _structuredLogsToggle;
		private Label _loggingStatus;
		private bool _loggingDirty;
		private bool _isComplete;
		private bool _hasCompletionState;
		private IVisualElementScheduledItem _refreshSchedule;

		internal PerfMeterFtuePage(
			VisualElement root,
			Action selectSetup,
			Action selectPresets,
			Action selectRuntime,
			Action<string> reportAction,
			Action<bool> visibilityChanged)
		{
			_root = root ?? throw new ArgumentNullException(nameof(root));
			_selectSetup = selectSetup;
			_selectPresets = selectPresets;
			_selectRuntime = selectRuntime;
			_reportAction = reportAction;
			_visibilityChanged = visibilityChanged;

			Build();
			Refresh();
			_refreshSchedule = _root.schedule.Execute(Refresh).Every(RefreshIntervalMilliseconds);
		}

		internal bool IsComplete
		{
			get { return _isComplete; }
		}

		internal void Refresh()
		{
			PerfMeterOptionalDependencyInstaller.Update();
			RefreshLogging();

			PerfMeterSetupUtility.PerfMeterSetupStatus setupStatus = null;
			try
			{
				setupStatus = PerfMeterSetupUtility.GetStatus();
				RefreshRequiredSetup(setupStatus);
			}
			catch (Exception exception)
			{
				SetRequiredSetupUnavailable(exception);
				Report("FTUE status refresh failed: " + exception.Message);
			}

			RefreshDiagnostics();

			List<bool> requiredReady = BuildRequiredResolution(setupStatus);
			List<bool> optionalResolved = BuildOptionalResolution();
			UpdateCompletion(requiredReady, optionalResolved);
		}

		internal void ResetChoicesAndShow()
		{
			PerfMeterFtueState.ResetChoices();
			_visibilityChanged?.Invoke(true);
			_isComplete = false;
			_hasCompletionState = true;
			Report("FTUE optional choices reset.");
			Refresh();
		}

		private void Build()
		{
			_root.Clear();
			_root.name = FtueRootElementName;
			_root.AddToClassList("pm-ftue-page");

			ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
			scroll.AddToClassList("pm-ftue-scroll");
			scroll.style.flexGrow = 1f;
			_root.Add(scroll);

			Label title = new Label("SGG PerfMeter first-time setup");
			title.name = FtueTitleElementName;
			title.AddToClassList("pm-ftue-title");
			scroll.Add(title);
			AddInfo(scroll, "This step-by-step FTUE checks the required setup, recent diagnostics, and logging. Optional packages and external capture tools can be skipped.");

			BuildRequiredSection(scroll);
			BuildLoggingSection(scroll);
			BuildDiagnosticsSection(scroll);
			BuildOptionalSection(scroll);
		}

		private void BuildRequiredSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Required Setup");
			AddInfo(section, "Required checks must be ready before the FTUE tab can hide. Use the existing Setup and Presets tabs for detailed configuration.");

			_importCompatibilityRow = AddChecklistRow(section, "Import compatibility", RequiredImportCompatibilityElementName, OpenSetup, "Open Setup");
			_coreRuntimeCompatibilityRow = AddChecklistRow(section, "Core runtime compatibility", RequiredCoreRuntimeCompatibilityElementName, OpenSetup, "Open Setup");
			_renderIntegrationRow = AddChecklistRow(section, "Render integration / configuration", RequiredRenderIntegrationElementName, InstallRenderIntegration, "Configure Render Integration", OpenSetup, "Open Setup");
			_frameTimingRow = AddChecklistRow(section, "Frame Timing Stats", RequiredFrameTimingElementName, EnableFrameTiming, "Enable Frame Timing");
			_packagePathRow = AddChecklistRow(section, "Package Path", RequiredPackagePathElementName, OpenSetup, "Open Setup");
			_settingsJsonRow = AddChecklistRow(section, "Settings JSON", RequiredSettingsJsonElementName, CreateDefaultSettings, "Create Default Settings", OpenPresets, "Open Presets");

			VisualElement actions = AddActions(section);
			Button recommendedButton = AddButton(actions, "Run Recommended Setup", RunRecommendedSetup);
			recommendedButton.name = "ftue-run-recommended-setup";
		}

		private void BuildLoggingSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Logging");
			AddInfo(section, "These toggles edit persisted project settings. Changes stay pending until Save Logging Settings is pressed.");

			_editorWarningLogsToggle = new Toggle { text = "Editor Warning Logs" };
			_editorWarningLogsToggle.name = EditorWarningLogsToggleElementName;
			_editorWarningLogsToggle.AddToClassList("pm-ftue-toggle");
			_editorWarningLogsToggle.RegisterValueChangedCallback(_ => OnLoggingToggleChanged());
			section.Add(_editorWarningLogsToggle);

			_structuredLogsToggle = new Toggle { text = "Structured Logs" };
			_structuredLogsToggle.name = StructuredLogsToggleElementName;
			_structuredLogsToggle.AddToClassList("pm-ftue-toggle");
			_structuredLogsToggle.RegisterValueChangedCallback(_ => OnLoggingToggleChanged());
			section.Add(_structuredLogsToggle);

			_loggingStatus = new Label();
			_loggingStatus.AddToClassList("pm-ftue-status");
			_loggingStatus.AddToClassList("pm-no-localize");
			section.Add(_loggingStatus);

			VisualElement actions = AddActions(section);
			Button saveButton = AddButton(actions, "Save Logging Settings", SaveLoggingSettings);
			saveButton.name = SaveLoggingSettingsButtonElementName;
		}

		private void BuildDiagnosticsSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Built-in Diagnostics");
			AddInfo(section, "Read-only diagnostics use current snapshots and do not start PerfMeter. No memory or GraphicsStateCollection capture request parameters are configured here.");

			_profilerDiagnosticsRow = AddChecklistRow(section, "Profiler metric catalog / self-overhead", "ftue-diagnostics-profiler", OpenRuntime, "Open Runtime");
			_graphicsDiagnosticsRow = AddChecklistRow(section, "Shader / PSO creation diagnostics", "ftue-diagnostics-shader-pso", OpenRuntime, "Open Runtime");
			_renderDiagnosticsRow = AddChecklistRow(section, "Render context / GRD / VRS", "ftue-diagnostics-render-context", OpenRuntime, "Open Runtime");
			_sessionDiagnosticsRow = AddChecklistRow(section, "Session Analysis", "ftue-diagnostics-session-analysis", OpenSessionAnalysis, "Open Session Analysis", OpenRuntime, "Open Runtime");
			_graphicsStateCollectionDiagnosticsRow = AddChecklistRow(section, "GraphicsStateCollection", "ftue-diagnostics-graphics-state-collection", OpenRuntime, "Open Runtime");

			VisualElement actions = AddActions(section);
			Button refreshButton = AddButton(actions, "Refresh Diagnostics", Refresh);
			refreshButton.name = "ftue-refresh-diagnostics";
			Button refreshCatalogButton = AddButton(actions, "Refresh Profiler Metric Catalog", RefreshProfilerMetricCatalog);
			refreshCatalogButton.name = "ftue-refresh-profiler-catalog";
		}

		private void BuildOptionalSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Optional Packages and Capture Tools");
			AddInfo(section, "Optional packages are detected by registered package version. RenderDoc and PIX are external tools, not bundled with PerfMeter. GraphicsStateCollection is bundled as an optional integration and needs no package install.");

			_packageRows.Add(AddPackageRow(
				section,
				"Memory Profiler (Optional)",
				OptionalMemoryProfilerElementName,
				PerfMeterFtueState.MemoryProfilerId,
				PerfMeterOptionalDependencyInstaller.MemoryProfilerPackageId,
				PerfMeterOptionalDependencyInstaller.MemoryProfilerPackageSpec,
				PerfMeterOptionalDependencyInstaller.MemoryProfilerPackageVersion,
				"Memory Profiler"));
			_packageRows.Add(AddPackageRow(
				section,
				"Adaptive Performance (Optional)",
				OptionalAdaptivePerformanceElementName,
				PerfMeterFtueState.AdaptivePerformanceId,
				PerfMeterOptionalDependencyInstaller.AdaptivePerformancePackageId,
				PerfMeterOptionalDependencyInstaller.AdaptivePerformancePackageSpec,
				PerfMeterOptionalDependencyInstaller.AdaptivePerformancePackageVersion,
				"Adaptive Performance"));
			_packageRows.Add(AddPackageRow(
				section,
				"Profile Analyzer (Optional)",
				OptionalProfileAnalyzerElementName,
				PerfMeterFtueState.ProfileAnalyzerId,
				PerfMeterOptionalDependencyInstaller.ProfileAnalyzerPackageId,
				PerfMeterOptionalDependencyInstaller.ProfileAnalyzerPackageSpec,
				PerfMeterOptionalDependencyInstaller.ProfileAnalyzerPackageVersion,
				"Profile Analyzer"));

			_graphicsStateCollectionRow = AddCapabilityRow(
				section,
				"GraphicsStateCollection (Optional, bundled)",
				OptionalGraphicsStateCollectionElementName,
				PerfMeterFtueState.GraphicsStateCollectionId,
				OpenRuntime,
				"Open Runtime");
			_renderDocRow = AddCapabilityRow(
				section,
				"RenderDoc (Optional, external, not bundled)",
				OptionalRenderDocElementName,
				PerfMeterFtueState.RenderDocId,
				() => OpenExternalTool("RenderDoc", RenderDocDownloadUrl),
				"Download RenderDoc");
			_pixRow = AddCapabilityRow(
				section,
				"PIX (Optional, external, not bundled)",
				OptionalPixElementName,
				PerfMeterFtueState.PixId,
				() => OpenExternalTool("PIX", PixDownloadUrl),
				"Download PIX");

			VisualElement actions = AddActions(section);
			Button resetButton = AddButton(actions, "Reset FTUE Choices", ResetChoicesAndShow);
			resetButton.name = "ftue-reset-choices";
		}

		private PackageRow AddPackageRow(
			VisualElement parent,
			string displayName,
			string elementName,
			string optionalId,
			string packageId,
			string packageSpec,
			string minimumVersion,
			string installName)
		{
			PackageRow row = new PackageRow(optionalId, packageId, packageSpec, minimumVersion, installName);
			row.Checklist = AddChecklistRow(
				parent,
				displayName,
				elementName,
				() => InstallPackage(row),
				"Install",
				() => SkipOptional(row.OptionalId),
				"Skip");
			row.InstallButton = row.Checklist.PrimaryButton;
			row.SkipButton = row.Checklist.SecondaryButton;
			row.InstallButton.name = elementName + "-install";
			row.SkipButton.name = elementName + "-skip";
			return row;
		}

		private OptionalCapabilityRow AddCapabilityRow(
			VisualElement parent,
			string displayName,
			string elementName,
			string optionalId,
			Action openAction,
			string openButtonText)
		{
			OptionalCapabilityRow row = new OptionalCapabilityRow(optionalId);
			row.Checklist = AddChecklistRow(
				parent,
				displayName,
				elementName,
				openAction,
				openButtonText,
				() => SkipOptional(row.OptionalId),
				"Skip");
			row.OpenButton = row.Checklist.PrimaryButton;
			row.SkipButton = row.Checklist.SecondaryButton;
			row.OpenButton.name = elementName + "-open";
			row.SkipButton.name = elementName + "-skip";
			return row;
		}

		private VisualElement AddSection(VisualElement parent, string caption)
		{
			VisualElement section = new VisualElement();
			section.AddToClassList("pm-ftue-section");
			Label header = new Label(caption);
			header.AddToClassList("pm-ftue-section-caption");
			VisualElement content = new VisualElement();
			content.AddToClassList("pm-ftue-section-content");
			section.Add(header);
			section.Add(content);
			parent.Add(section);
			return content;
		}

		private void AddInfo(VisualElement parent, string text)
		{
			Label info = new Label(text);
			info.AddToClassList("pm-ftue-intro");
			parent.Add(info);
		}

		private ChecklistRow AddChecklistRow(
			VisualElement parent,
			string key,
			string elementName,
			Action primaryAction,
			string primaryText,
			Action secondaryAction = null,
			string secondaryText = null)
		{
			VisualElement row = new VisualElement();
			row.name = elementName;
			row.AddToClassList("pm-checklist-row");
			row.AddToClassList("pm-ftue-checklist-row");

			Label keyLabel = new Label(key);
			keyLabel.AddToClassList("pm-ftue-checklist-key");

			VisualElement field = new VisualElement();
			field.AddToClassList("pm-checklist-field");
			field.AddToClassList("pm-ftue-checklist-field");
			Label icon = new Label("?");
			icon.AddToClassList("pm-checklist-icon");
			icon.AddToClassList("pm-no-localize");
			Label value = new Label();
			value.AddToClassList("pm-checklist-value");
			value.AddToClassList("pm-ftue-checklist-value");
			// Dynamic diagnostics can contain raw runtime identifiers, paths, and provider warnings.
			value.AddToClassList("pm-no-localize");
			field.Add(icon);
			field.Add(value);

			row.Add(keyLabel);
			row.Add(field);

			Button primaryButton = null;
			if (primaryAction != null && !string.IsNullOrEmpty(primaryText))
			{
				primaryButton = AddButton(row, primaryText, primaryAction);
			}

			Button secondaryButton = null;
			if (secondaryAction != null && !string.IsNullOrEmpty(secondaryText))
			{
				secondaryButton = AddButton(row, secondaryText, secondaryAction);
			}

			parent.Add(row);
			return new ChecklistRow(row, field, icon, value, primaryButton, secondaryButton);
		}

		private VisualElement AddActions(VisualElement parent)
		{
			VisualElement actions = new VisualElement();
			actions.AddToClassList("pm-actions");
			actions.AddToClassList("pm-ftue-actions");
			parent.Add(actions);
			return actions;
		}

		private Button AddButton(VisualElement parent, string text, Action action)
		{
			Button button = new Button(action) { text = text };
			button.AddToClassList("pm-button");
			button.AddToClassList("pm-ftue-button");
			parent.Add(button);
			return button;
		}

		private void RefreshLogging()
		{
			if (_editorWarningLogsToggle == null || _structuredLogsToggle == null)
			{
				return;
			}

			if (!_loggingDirty)
			{
				try
				{
					PerfMeterSettingsSnapshot settings = PerfMeterSetupActions.LoadSettings();
					_editorWarningLogsToggle.SetValueWithoutNotify(settings.EditorWarningsEnabled);
					_structuredLogsToggle.SetValueWithoutNotify(settings.StructuredLogsEnabled);
				}
				catch (Exception exception)
				{
					_loggingStatus.text = "Unavailable - could not read persisted logging settings: " + exception.Message;
					return;
				}
			}

			UpdateLoggingStatus();
		}

		private void OnLoggingToggleChanged()
		{
			_loggingDirty = true;
			UpdateLoggingStatus();
		}

		private void UpdateLoggingStatus()
		{
			if (_loggingStatus == null || _editorWarningLogsToggle == null || _structuredLogsToggle == null)
			{
				return;
			}

			string editorWarnings = _editorWarningLogsToggle.value ? "Enabled" : "Disabled";
			string structuredLogs = _structuredLogsToggle.value ? "Enabled" : "Disabled";
			_loggingStatus.text = (_loggingDirty ? "Pending project settings" : "Saved project settings") + ": Editor Warning Logs " + editorWarnings + ", Structured Logs " + structuredLogs + ".";
		}

		private void SaveLoggingSettings()
		{
			try
			{
				PerfMeterSettingsSnapshot settings = PerfMeterSetupActions.LoadSettings();
				settings = PerfMeterSettingsStore.WithEditorWarningsEnabled(settings, _editorWarningLogsToggle.value);
				settings = PerfMeterSettingsStore.WithStructuredLogsEnabled(settings, _structuredLogsToggle.value);
				PerfMeterSetupActionResult result = PerfMeterSetupActions.SaveSettings(settings);
				Report("Save Logging Settings: " + result.Message);
				if (!result.Success)
				{
					Refresh();
					return;
				}

				_loggingDirty = false;
				if (EditorApplication.isPlaying)
				{
					RuntimePerformanceMeter.SetEditorWarningLogsEnabled(_editorWarningLogsToggle.value);
					RuntimePerformanceMeter.SetStructuredLogsEnabled(_structuredLogsToggle.value);
				}
				Refresh();
			}
			catch (Exception exception)
			{
				Report("Save Logging Settings failed: " + exception.Message);
				Refresh();
			}
		}

		private void RefreshRequiredSetup(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			PerfMeterCompatibilityStatus compatibility = status.CompatibilityStatus;
			SetChecklist(
				_importCompatibilityRow,
				compatibility.ImportCompatible ? OkState : ErrorState,
				(compatibility.ImportCompatible ? "Ready - " : "Error - ") + compatibility.ImportReason);
			SetChecklist(
				_coreRuntimeCompatibilityRow,
				compatibility.CoreRuntimeCompatible ? OkState : ErrorState,
				(compatibility.CoreRuntimeCompatible ? "Ready - " : "Error - ") + compatibility.CoreRuntimeReason);

			bool renderConfigured = IsRenderIntegrationConfigured(status);
			string renderText;
			if (!compatibility.RenderIntegrationCompatible)
			{
				renderText = "Error - " + compatibility.RenderIntegrationReason;
			}
			else if (renderConfigured)
			{
				renderText = "Ready - " + status.RendererMessage;
			}
			else
			{
				renderText = "Next action - " + status.RendererMessage;
			}
			SetChecklist(_renderIntegrationRow, renderConfigured ? OkState : compatibility.RenderIntegrationCompatible ? WarnState : ErrorState, renderText);

			SetChecklist(
				_frameTimingRow,
				status.FrameTimingStatsEnabled ? OkState : WarnState,
				status.FrameTimingStatsEnabled ? "Ready - Frame Timing Stats is enabled." : "Next action - enable Frame Timing Stats before relying on GPU timing in builds.");

			bool packagePathReady = !string.IsNullOrEmpty(status.PackageAssetPath);
			SetChecklist(
				_packagePathRow,
				packagePathReady ? OkState : ErrorState,
				packagePathReady ? "Ready - package path " + status.PackageAssetPath + "." : "Error - PerfMeter package path could not be discovered by setup tooling.");

			bool settingsLoaded = status.Settings != null && status.Settings.FileExists && status.Settings.Snapshot.LoadState == PerfMeterSettingsLoadState.Loaded;
			SetChecklist(
				_settingsJsonRow,
				settingsLoaded ? OkState : WarnState,
				settingsLoaded ? "Ready - " + status.Settings.Message : "Next action - create and load the project Settings JSON. " + status.Settings.Message);
		}

		private void SetRequiredSetupUnavailable(Exception exception)
		{
			string text = "Unavailable - required setup status could not be read: " + exception.Message;
			SetChecklist(_importCompatibilityRow, ErrorState, text);
			SetChecklist(_coreRuntimeCompatibilityRow, ErrorState, text);
			SetChecklist(_renderIntegrationRow, ErrorState, text);
			SetChecklist(_frameTimingRow, ErrorState, text);
			SetChecklist(_packagePathRow, ErrorState, text);
			SetChecklist(_settingsJsonRow, ErrorState, text);
		}

		private static bool IsRenderIntegrationConfigured(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			if (status == null || !status.CompatibilityStatus.RenderIntegrationCompatible)
			{
				return false;
			}

			if (status.ActiveRenderPipeline != PerfMeterRenderPipelineKind.Universal && status.ActiveRenderPipeline != PerfMeterRenderPipelineKind.HighDefinition)
			{
				return false;
			}

			return status.AllRenderersConfigured && !status.HasRendererWarnings;
		}

		internal static bool AreRequiredSetupStepsReady(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			return PerfMeterFtueState.AreAllStepsResolved(BuildRequiredResolution(status), Array.Empty<bool>());
		}

		private static List<bool> BuildRequiredResolution(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			List<bool> required = new List<bool>(6);
			if (status == null)
			{
				required.Add(false);
				required.Add(false);
				required.Add(false);
				required.Add(false);
				required.Add(false);
				required.Add(false);
				return required;
			}

			required.Add(status.CompatibilityStatus.ImportCompatible);
			required.Add(status.CompatibilityStatus.CoreRuntimeCompatible);
			required.Add(IsRenderIntegrationConfigured(status));
			required.Add(status.FrameTimingStatsEnabled);
			required.Add(!string.IsNullOrEmpty(status.PackageAssetPath));
			required.Add(status.Settings != null && status.Settings.FileExists && status.Settings.Snapshot.LoadState == PerfMeterSettingsLoadState.Loaded);
			return required;
		}

		private List<bool> BuildOptionalResolution()
		{
			List<bool> optional = new List<bool>(6);
			for (int index = 0; index < _packageRows.Count; index++)
			{
				PackageRow row = _packageRows[index];
				optional.Add(PerfMeterFtueState.IsOptionalResolved(row.OptionalId, row.IsAvailable));
			}

			optional.Add(_graphicsStateCollectionRow != null && PerfMeterFtueState.IsOptionalResolved(_graphicsStateCollectionRow.OptionalId, _graphicsStateCollectionRow.IsAvailable));
			optional.Add(_renderDocRow != null && PerfMeterFtueState.IsOptionalResolved(_renderDocRow.OptionalId, _renderDocRow.IsAvailable));
			optional.Add(_pixRow != null && PerfMeterFtueState.IsOptionalResolved(_pixRow.OptionalId, _pixRow.IsAvailable));
			return optional;
		}

		private void UpdateCompletion(IEnumerable<bool> requiredReady, IEnumerable<bool> optionalResolved)
		{
			bool complete = PerfMeterFtueState.AreAllStepsResolved(requiredReady, optionalResolved);
			bool changed = !_hasCompletionState || _isComplete != complete;
			_isComplete = complete;
			if (changed)
			{
				_hasCompletionState = true;
				_visibilityChanged?.Invoke(!IsComplete);
			}
		}

		private void RefreshDiagnostics()
		{
			PerfMeterProfilerMetricCatalogSnapshot catalog = RuntimePerformanceMeter.GetProfilerMetricCatalog();
			PerfMeterSelfOverheadSnapshot selfOverhead = RuntimePerformanceMeter.GetSelfOverhead();
			SetChecklist(_profilerDiagnosticsRow, catalog.State == PerfMeterProfilerMetricCatalogState.Ready ? OkState : WarnState, FormatProfilerDiagnostics(catalog, selfOverhead));

			PerfMeterGraphicsDiagnosticsSnapshot graphics = RuntimePerformanceMeter.GetGraphicsDiagnostics();
			SetChecklist(_graphicsDiagnosticsRow, graphics.Availability == PerfMeterAvailability.Available ? OkState : WarnState, FormatGraphicsDiagnostics(graphics));

			PerfMeterRenderIntegrationSnapshot render = RuntimePerformanceMeter.GetRenderIntegrationSnapshot();
			SetChecklist(_renderDiagnosticsRow, render.Availability == PerfMeterAvailability.Available && render.State == PerfMeterRenderIntegrationState.Observed ? OkState : WarnState, FormatRenderDiagnostics(render));

			PerfMeterSessionSummarySnapshot session = RuntimePerformanceMeter.GetSessionSummary();
			SetChecklist(_sessionDiagnosticsRow, session.State == PerfMeterSessionState.Idle ? WarnState : OkState, FormatSessionDiagnostics(session));

			PerfMeterGraphicsStateCollectionCapabilitiesSnapshot capabilities = RuntimePerformanceMeter.GetGraphicsStateCollectionCapabilities();
			PerfMeterGraphicsStateCollectionStatusSnapshot state = RuntimePerformanceMeter.GetGraphicsStateCollectionStatus();
			SetChecklist(_graphicsStateCollectionDiagnosticsRow, capabilities.Availability == PerfMeterAvailability.Available ? OkState : WarnState, FormatGraphicsStateCollectionDiagnostics(capabilities, state));

			RefreshOptionalRows(capabilities);
		}

		private void RefreshOptionalRows(PerfMeterGraphicsStateCollectionCapabilitiesSnapshot graphicsStateCapabilities)
		{
			for (int index = 0; index < _packageRows.Count; index++)
			{
				RefreshPackageRow(_packageRows[index]);
			}

			RefreshGraphicsStateCollectionRow(_graphicsStateCollectionRow, graphicsStateCapabilities);
			RefreshExternalRows();
		}

		private void RefreshPackageRow(PackageRow row)
		{
			bool skipped = PerfMeterFtueState.IsSkipped(row.OptionalId);
			bool registered = PerfMeterOptionalDependencyInstaller.TryGetRegisteredPackageVersion(row.PackageId, out string version);
			row.IsAvailable = registered && PerfMeterOptionalDependencyInstaller.IsVersionAtLeast(version, row.MinimumVersion);

			if (skipped)
			{
				SetChecklist(row.Checklist, OptionalState, "Optional - skipped. Reset FTUE Choices to reconsider " + row.DisplayName + ".");
			}
			else if (PerfMeterOptionalDependencyInstaller.IsInstalling(row.PackageId))
			{
				SetChecklist(row.Checklist, WarnState, "Installing optional package " + row.DisplayName + " " + row.PackageSpec + ".");
			}
			else if (row.IsAvailable)
			{
				SetChecklist(row.Checklist, OkState, "Available - " + row.DisplayName + " " + version + " meets the " + row.MinimumVersion + "+ floor.");
			}
			else if (PerfMeterOptionalDependencyInstaller.TryGetLastError(row.PackageId, out string error))
			{
				SetChecklist(row.Checklist, ErrorState, "Install error for optional package " + row.DisplayName + ": " + error);
			}
			else if (registered)
			{
				SetChecklist(row.Checklist, WarnState, "Optional - registered version " + version + " does not meet the " + row.MinimumVersion + "+ floor.");
			}
			else
			{
				SetChecklist(row.Checklist, OptionalState, "Optional - " + row.DisplayName + " is not installed. Required floor: " + row.MinimumVersion + ".");
			}

			bool packageActionEnabled = !PerfMeterOptionalDependencyInstaller.HasActiveInstall && !skipped && !row.IsAvailable;
			row.InstallButton.text = PerfMeterWindowLocalization.Text(PerfMeterOptionalDependencyInstaller.IsInstalling(row.PackageId) ? "Installing..." : "Install");
			row.InstallButton.SetEnabled(packageActionEnabled);
			row.SkipButton.text = PerfMeterWindowLocalization.Text(skipped ? "Skipped" : "Skip");
			row.SkipButton.SetEnabled(packageActionEnabled);
		}

		private void RefreshGraphicsStateCollectionRow(OptionalCapabilityRow row, PerfMeterGraphicsStateCollectionCapabilitiesSnapshot capabilities)
		{
			if (row == null)
			{
				return;
			}

			bool skipped = PerfMeterFtueState.IsSkipped(row.OptionalId);
			row.IsAvailable = capabilities.Availability == PerfMeterAvailability.Available;
			if (skipped)
			{
				SetChecklist(row.Checklist, OptionalState, "Optional - skipped. Reset FTUE Choices to reconsider GraphicsStateCollection.");
			}
			else if (row.IsAvailable)
			{
				SetChecklist(row.Checklist, OkState, "Available - bundled GraphicsStateCollection backend " + FormatOptionalValue(capabilities.BackendId) + "; trace " + (capabilities.SupportsTrace ? "yes" : "no") + ", prewarm " + (capabilities.SupportsPrewarm ? "yes" : "no") + ".");
			}
			else
			{
				SetChecklist(row.Checklist, OptionalState, "Optional - GraphicsStateCollection unavailable: " + FormatOptionalValue(capabilities.Warning) + ".");
			}

			row.SkipButton.text = PerfMeterWindowLocalization.Text(skipped ? "Skipped" : "Skip");
			row.SkipButton.SetEnabled(!skipped);
		}

		private void RefreshExternalRows()
		{
			PerfMeterCaptureBackendCapability renderDocCapability = GetExternalCapability(PerfMeterCaptureTool.RenderDoc);
			PerfMeterCaptureBackendCapability pixCapability = GetExternalCapability(PerfMeterCaptureTool.Pix);
			bool renderDocCapabilityAvailable = renderDocCapability.Availability == PerfMeterAvailability.Available;
			bool pixCapabilityAvailable = pixCapability.Availability == PerfMeterAvailability.Available;
			bool renderDocAvailable = PerfMeterFtueState.ResolveExternalToolAvailability(
				renderDocCapabilityAvailable,
				pixCapabilityAvailable,
				PerfMeterFtueState.IsSkipped(PerfMeterFtueState.PixId));
			bool pixAvailable = PerfMeterFtueState.ResolveExternalToolAvailability(
				pixCapabilityAvailable,
				renderDocCapabilityAvailable,
				PerfMeterFtueState.IsSkipped(PerfMeterFtueState.RenderDocId));

			RefreshExternalRow(_renderDocRow, "RenderDoc", renderDocCapability, renderDocAvailable, renderDocCapabilityAvailable && pixCapabilityAvailable);
			RefreshExternalRow(_pixRow, "PIX", pixCapability, pixAvailable, renderDocCapabilityAvailable && pixCapabilityAvailable);
		}

		private static PerfMeterCaptureBackendCapability GetExternalCapability(PerfMeterCaptureTool tool)
		{
			try
			{
				return new PerfMeterExternalGpuProfilerBackend().GetCapability(tool);
			}
			catch (Exception exception)
			{
				return new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Unavailable, exception.GetType().Name + ": " + exception.Message);
			}
		}

		private void RefreshExternalRow(
			OptionalCapabilityRow row,
			string displayName,
			PerfMeterCaptureBackendCapability capability,
			bool available,
			bool ambiguousAttachment)
		{
			if (row == null)
			{
				return;
			}

			bool skipped = PerfMeterFtueState.IsSkipped(row.OptionalId);
			row.IsAvailable = available;
			if (skipped)
			{
				SetChecklist(row.Checklist, OptionalState, "Optional - skipped. Reset FTUE Choices to reconsider " + displayName + ".");
			}
			else if (row.IsAvailable)
			{
				SetChecklist(row.Checklist, OkState, "Available - " + displayName + " is attached. The tool is external and not bundled with PerfMeter.");
			}
			else if (ambiguousAttachment)
			{
				SetChecklist(row.Checklist, OptionalState, "Optional - Unity reports an external GPU profiler is attached but cannot identify RenderDoc versus PIX. Skip the tool you are not using.");
			}
			else
			{
				SetChecklist(row.Checklist, OptionalState, "Optional - " + displayName + " is external and not bundled. " + FormatOptionalValue(capability.Warning));
			}

			row.SkipButton.text = PerfMeterWindowLocalization.Text(skipped ? "Skipped" : "Skip");
			row.SkipButton.SetEnabled(!skipped);
		}

		private void SetChecklist(ChecklistRow row, string state, string text)
		{
			if (row == null)
			{
				return;
			}

			row.Value.text = text ?? string.Empty;
			RemoveChecklistState(row.Field);
			RemoveChecklistState(row.Icon);
			row.Field.AddToClassList("pm-checklist--" + state);
			row.Icon.AddToClassList("pm-checklist-icon--" + state);
			row.Icon.text = StatusIcon(state);
			row.Icon.tooltip = PerfMeterWindowLocalization.Text(StatusTooltip(state));
		}

		private static void RemoveChecklistState(VisualElement element)
		{
			if (element == null)
			{
				return;
			}

			element.RemoveFromClassList("pm-checklist--ok");
			element.RemoveFromClassList("pm-checklist--warn");
			element.RemoveFromClassList("pm-checklist--error");
			element.RemoveFromClassList("pm-checklist--optional");
			element.RemoveFromClassList("pm-checklist-icon--ok");
			element.RemoveFromClassList("pm-checklist-icon--warn");
			element.RemoveFromClassList("pm-checklist-icon--error");
			element.RemoveFromClassList("pm-checklist-icon--optional");
		}

		private static string StatusIcon(string state)
		{
			switch (state)
			{
				case OkState:
					return "OK";
				case ErrorState:
					return "!!";
				case OptionalState:
					return "i";
				default:
					return "!";
			}
		}

		private static string StatusTooltip(string state)
		{
			switch (state)
			{
				case OkState:
					return "Ready";
				case ErrorState:
					return "Error";
				case OptionalState:
					return "Optional";
				default:
					return "Next action";
			}
		}

		private void RunRecommendedSetup()
		{
			RunSetupAction("Run Recommended Setup", PerfMeterSetupActions.RunRecommendedSetup);
		}

		private void EnableFrameTiming()
		{
			RunSetupAction("Enable Frame Timing", PerfMeterSetupActions.EnableFrameTimingStats);
		}

		private void InstallRenderIntegration()
		{
			RunSetupAction("Configure Render Integration", PerfMeterSetupActions.InstallRendererFeatures);
		}

		private void CreateDefaultSettings()
		{
			RunSetupAction("Create Default Settings", PerfMeterSetupActions.CreateDefaultSettings);
		}

		private void RunSetupAction(string title, Func<PerfMeterSetupActionResult> action)
		{
			try
			{
				PerfMeterSetupActionResult result = action();
				Report(title + ": " + result.Message);
				Refresh();
			}
			catch (Exception exception)
			{
				Report(title + " failed: " + exception.Message);
				Refresh();
			}
		}

		private void OpenSetup()
		{
			_selectSetup?.Invoke();
		}

		private void OpenPresets()
		{
			_selectPresets?.Invoke();
		}

		private void OpenRuntime()
		{
			_selectRuntime?.Invoke();
		}

		private void RefreshProfilerMetricCatalog()
		{
			bool refreshed = RuntimePerformanceMeter.TryRefreshProfilerMetricCatalog();
			Report(refreshed
				? "Profiler metric catalog refresh requested without starting a new PerfMeter runtime."
				: "Profiler metric catalog unavailable: PerfMeter is not running, so no sample was collected.");
			Refresh();
		}

		private void OpenSessionAnalysis()
		{
			try
			{
				PerfMeterSessionAnalysisWindow.Open();
				Report("Session Analysis opened.");
			}
			catch (Exception exception)
			{
				Report("Session Analysis failed to open: " + exception.Message);
			}
		}

		private void OpenExternalTool(string displayName, string url)
		{
			Application.OpenURL(url);
			Report(displayName + " official download page opened: " + url);
		}

		private void InstallPackage(PackageRow row)
		{
			if (row == null || PerfMeterOptionalDependencyInstaller.HasActiveInstall)
			{
				Refresh();
				return;
			}

			bool confirmed = EditorUtility.DisplayDialog(
				"Install Optional Package",
				"Install " + row.DisplayName + " " + row.PackageSpec + " through Unity Package Manager?",
				"Install",
				"Cancel");
			if (!confirmed)
			{
				return;
			}

			if (!PerfMeterOptionalDependencyInstaller.TryStartInstall(row.PackageId, row.PackageSpec, row.DisplayName, out string error))
			{
				Report("Install " + row.DisplayName + " failed: " + error);
			}
			else
			{
				Report("Install " + row.DisplayName + " started.");
			}
			Refresh();
		}

		private void SkipOptional(string optionalId)
		{
			PerfMeterFtueState.SetSkipped(optionalId);
			Report("Optional FTUE step skipped: " + optionalId + ".");
			Refresh();
		}

		private static string FormatProfilerDiagnostics(
			PerfMeterProfilerMetricCatalogSnapshot catalog,
			PerfMeterSelfOverheadSnapshot selfOverhead)
		{
			string catalogText;
			if (catalog.State == PerfMeterProfilerMetricCatalogState.NotInitialized)
			{
				catalogText = "Profiler metric catalog unavailable - not initialized; no sample.";
			}
			else if (catalog.State == PerfMeterProfilerMetricCatalogState.Error)
			{
				catalogText = "Profiler metric catalog unavailable - " + FormatOptionalValue(catalog.LastError) + "; no sample.";
			}
			else
			{
				PerfMeterProfilerMetricCapabilitySnapshot shader = FindCapability(catalog.Capabilities, PerfMeterProfilerMetricSemantic.ShaderGpuProgramCreation);
				PerfMeterProfilerMetricCapabilitySnapshot pso = FindCapability(catalog.Capabilities, PerfMeterProfilerMetricSemantic.GraphicsPipelineCreation);
				catalogText = "Profiler metric catalog ready, revision " + catalog.Revision.ToString(CultureInfo.InvariantCulture) + "; shader " + FormatMetricCapability(shader, 0L) + "; PSO " + FormatMetricCapability(pso, 0L) + ".";
			}

			string overheadText;
			if (selfOverhead.State == PerfMeterSelfOverheadState.NotInitialized)
			{
				overheadText = "Self-overhead unavailable - not initialized; no sample.";
			}
			else if (selfOverhead.State == PerfMeterSelfOverheadState.Collecting)
			{
				overheadText = "Self-overhead collecting - no complete sample yet.";
			}
			else
			{
				overheadText = "Self-overhead ready" + (selfOverhead.HasBudgetViolation ? " with a budget warning." : " with samples.");
			}

			return catalogText + " " + overheadText;
		}

		private static string FormatGraphicsDiagnostics(PerfMeterGraphicsDiagnosticsSnapshot diagnostics)
		{
			if (diagnostics.Availability != PerfMeterAvailability.Available)
			{
				return "Shader / PSO creation diagnostics unavailable - no sample. " + FormatOptionalValue(diagnostics.Warning);
			}

			return "Shader GPU program creation " + FormatMetricCapability(diagnostics.ShaderGpuProgramCreationCapability, diagnostics.ShaderGpuProgramCreationValue) + "; PSO creation " + FormatMetricCapability(diagnostics.GraphicsPipelineCreationCapability, diagnostics.GraphicsPipelineCreationValue) + ".";
		}

		private static string FormatRenderDiagnostics(PerfMeterRenderIntegrationSnapshot render)
		{
			if (render.Availability == PerfMeterAvailability.Unknown)
			{
				return "Render context unavailable - no observation yet; GRD/VRS have no sample.";
			}

			if (render.Availability != PerfMeterAvailability.Available)
			{
				return "Render context unavailable - GRD/VRS unavailable; no sample. " + FormatOptionalValue(render.Warning);
			}

			string observation = render.State == PerfMeterRenderIntegrationState.Observed
				? "observed " + FormatOptionalValue(render.IntegrationName) + ", passes " + render.PerfMeterPassCount.ToString(CultureInfo.InvariantCulture)
				: "available but no observation yet";
			string grd = FormatAvailabilityBoolean(render.GpuResidentDrawer.SupportAvailability, render.GpuResidentDrawer.IsSupported, "supported", "unsupported");
			string vrs = FormatAvailabilityBoolean(render.VariableRateShading.ConfigurationAvailability, render.VariableRateShading.IsConfigured, "configured", "not configured");
			return "Render context " + observation + "; GRD " + grd + "; VRS " + vrs + ".";
		}

		private static string FormatSessionDiagnostics(PerfMeterSessionSummarySnapshot session)
		{
			if (session.State == PerfMeterSessionState.Idle)
			{
				return "Session Analysis has no session sample yet.";
			}

			return "Session Analysis " + session.State + "; " + session.SampleCount.ToString(CultureInfo.InvariantCulture) + " samples" + (session.SampleCount == 0 ? " (no sample yet)" : ".");
		}

		private static string FormatGraphicsStateCollectionDiagnostics(
			PerfMeterGraphicsStateCollectionCapabilitiesSnapshot capabilities,
			PerfMeterGraphicsStateCollectionStatusSnapshot state)
		{
			if (capabilities.Availability != PerfMeterAvailability.Available)
			{
				return "GraphicsStateCollection unavailable - no backend and no sample. " + FormatOptionalValue(capabilities.Warning);
			}

			if (state.State == PerfMeterGraphicsStateCollectionState.Idle)
			{
				return "GraphicsStateCollection backend available, but no trace sample yet.";
			}

			return "GraphicsStateCollection " + state.State + "; trace " + state.CompletedTraceFrames.ToString(CultureInfo.InvariantCulture) + "/" + state.RequestedTraceFrames.ToString(CultureInfo.InvariantCulture) + ".";
		}

		private static PerfMeterProfilerMetricCapabilitySnapshot FindCapability(
			PerfMeterProfilerMetricCapabilitySnapshot[] capabilities,
			PerfMeterProfilerMetricSemantic semantic)
		{
			if (capabilities != null)
			{
				for (int index = 0; index < capabilities.Length; index++)
				{
					if (capabilities[index].Semantic == semantic)
					{
						return capabilities[index];
					}
				}
			}

			return new PerfMeterProfilerMetricCapabilitySnapshot(
				semantic,
				PerfMeterProfilerMetricSampleState.Unavailable,
				PerfMeterProfilerMetricResolution.None,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				0,
				0);
		}

		private static string FormatMetricCapability(PerfMeterProfilerMetricCapabilitySnapshot capability, long value)
		{
			switch (capability.SampleState)
			{
				case PerfMeterProfilerMetricSampleState.AvailableSampled:
					return value.ToString(CultureInfo.InvariantCulture) + " (" + capability.ResolvedRecorderNames + ")";
				case PerfMeterProfilerMetricSampleState.AvailableNoSample:
					return "AvailableNoSample";
				default:
					return "Unavailable";
			}
		}

		private static string FormatAvailabilityBoolean(PerfMeterAvailability availability, bool value, string availableText, string unavailableText)
		{
			switch (availability)
			{
				case PerfMeterAvailability.Available:
					return value ? availableText : unavailableText;
				case PerfMeterAvailability.Unavailable:
					return "unavailable";
				default:
					return "unknown";
			}
		}

		private static string FormatOptionalValue(string value)
		{
			return string.IsNullOrEmpty(value) ? "none" : value;
		}

		private void Report(string message)
		{
			_reportAction?.Invoke(message ?? string.Empty);
		}

		private sealed class ChecklistRow
		{
			internal ChecklistRow(VisualElement row, VisualElement field, Label icon, Label value, Button primaryButton, Button secondaryButton)
			{
				Row = row;
				Field = field;
				Icon = icon;
				Value = value;
				PrimaryButton = primaryButton;
				SecondaryButton = secondaryButton;
			}

			internal VisualElement Row { get; }
			internal VisualElement Field { get; }
			internal Label Icon { get; }
			internal Label Value { get; }
			internal Button PrimaryButton { get; }
			internal Button SecondaryButton { get; }
		}

		private sealed class PackageRow
		{
			internal PackageRow(string optionalId, string packageId, string packageSpec, string minimumVersion, string displayName)
			{
				OptionalId = optionalId;
				PackageId = packageId;
				PackageSpec = packageSpec;
				MinimumVersion = minimumVersion;
				DisplayName = displayName;
			}

			internal string OptionalId { get; }
			internal string PackageId { get; }
			internal string PackageSpec { get; }
			internal string MinimumVersion { get; }
			internal string DisplayName { get; }
			internal ChecklistRow Checklist { get; set; }
			internal Button InstallButton { get; set; }
			internal Button SkipButton { get; set; }
			internal bool IsAvailable { get; set; }
		}

		private sealed class OptionalCapabilityRow
		{
			internal OptionalCapabilityRow(string optionalId)
			{
				OptionalId = optionalId;
			}

			internal string OptionalId { get; }
			internal ChecklistRow Checklist { get; set; }
			internal Button OpenButton { get; set; }
			internal Button SkipButton { get; set; }
			internal bool IsAvailable { get; set; }
		}
	}
}
