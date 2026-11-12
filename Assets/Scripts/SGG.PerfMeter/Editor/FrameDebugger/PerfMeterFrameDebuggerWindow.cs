using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGG.PerfMeter.Editor.FrameDebugger
{
	public sealed class PerfMeterFrameDebuggerWindow : EditorWindow
	{
		private Label _status;
		private Label _capability;
		private Button _export;
		private Button _cancel;
		private Button _reveal;
		private IntegerField _maxEvents;
		private Toggle _properties;
		private IVisualElementScheduledItem _refresh;

		[MenuItem("SGG/Perfmeter/Frame Debugger Export")]
		public static void Open()
		{
			PerfMeterFrameDebuggerWindow window = GetWindow<PerfMeterFrameDebuggerWindow>("SGG PerfMeter Frame Debugger");
			window.minSize = new Vector2(480f, 320f); window.Show();
		}

		public void CreateGUI()
		{
			_refresh?.Pause(); rootVisualElement.Clear();
			rootVisualElement.style.paddingLeft = rootVisualElement.style.paddingRight = 12;
			rootVisualElement.style.paddingTop = rootVisualElement.style.paddingBottom = 12;
			Label title = new Label("Frame Debugger export") { style = { fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold } };
			rootVisualElement.Add(title);
			rootVisualElement.Add(new HelpBox("Enable Frame Debugger for the local Editor target, then export its current events. Export temporarily selects events and restores your selection when it still owns it. Diagnostic frames are excluded from normal PerfMeter session samples.", HelpBoxMessageType.Info));
			_capability = new Label { enableRichText = false, style = { whiteSpace = WhiteSpace.Normal } }; rootVisualElement.Add(_capability);
			_maxEvents = new IntegerField("Maximum events") { value = 2048 }; rootVisualElement.Add(_maxEvents);
			_properties = new Toggle("Include shader properties") { value = false }; rootVisualElement.Add(_properties);
			rootVisualElement.Add(new Label("JSON + CSV • up to 60 seconds • 64 MiB output limit") { style = { whiteSpace = WhiteSpace.Normal } });
			rootVisualElement.Add(new Label("Saved under Temp/PerfMeter/FrameDebuggerExports. Keep a copy of exports you need; the latest 8 exports / 256 MiB are retained. Artifacts can contain project names and shader properties.") { style = { whiteSpace = WhiteSpace.Normal } });
			_export = new Button(() =>
			{
				PerfMeterFrameDebuggerExport.Request(new PerfMeterFrameDebuggerExportOptions { MaxEvents = _maxEvents.value, IncludeProperties = _properties.value }); Refresh();
			}) { text = "Export current capture" }; rootVisualElement.Add(_export);
			_cancel = new Button(() => { PerfMeterFrameDebuggerExport.Cancel(); Refresh(); }) { text = "Cancel export" }; rootVisualElement.Add(_cancel);
			_status = new Label { enableRichText = false, style = { whiteSpace = WhiteSpace.Normal } }; rootVisualElement.Add(_status);
			_reveal = new Button(() => EditorUtility.RevealInFinder(PerfMeterFrameDebuggerExport.Status.OutputDirectory)) { text = "Show exported files" }; rootVisualElement.Add(_reveal);
			Refresh(); _refresh = rootVisualElement.schedule.Execute(Refresh).Every(500);
		}

		private void OnDisable() { _refresh?.Pause(); _refresh = null; }
		private void Refresh()
		{
			PerfMeterFrameDebuggerExportStatus status = PerfMeterFrameDebuggerExport.Status;
			bool available = PerfMeterFrameDebuggerExport.CanExport(out string reason);
			_capability.text = available ? "Ready to export the current local capture." : reason;
			_status.text = status.State + " • " + status.ProcessedEvents + "/" + status.RequestedEvents + " events\n" + status.Message;
			_export.SetEnabled(available); _cancel.SetEnabled(status.IsBusy);
			_maxEvents.SetEnabled(!status.IsBusy); _properties.SetEnabled(!status.IsBusy);
			_reveal.SetEnabled(!string.IsNullOrEmpty(status.OutputDirectory));
		}
	}
}