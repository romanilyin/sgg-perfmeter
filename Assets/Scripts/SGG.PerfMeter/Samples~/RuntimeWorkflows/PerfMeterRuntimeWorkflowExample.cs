using System;
using System.Collections;
using System.Globalization;
using System.IO;
using SGG.PerfMeter;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PerfMeterRuntimeWorkflowExample : MonoBehaviour
{
	[SerializeField] private PerfMeterTargetFps targetFps = PerfMeterTargetFps.Fps60;
	[SerializeField] private PerfMeterOverlayPreset firstPreset = PerfMeterOverlayPreset.Timing;
	[SerializeField] private PerfMeterOverlayPreset secondPreset = PerfMeterOverlayPreset.Rendering;
	[SerializeField] private float presetSwitchIntervalSeconds = 4f;
	[SerializeField] private bool cyclePresets = true;
	[SerializeField] private bool startSessionOnEnable = true;
	[SerializeField] private bool stopAndExportSessionOnDisable;
	[SerializeField] private float sessionWarmupSeconds = 1f;
	[SerializeField] private float sessionSampleIntervalSeconds = 0.25f;
	[SerializeField] private int sessionMaxSamples = 512;
	[SerializeField] private bool requestOverdrawOnEnable;
	[SerializeField] private int overdrawFrameCount = 60;
	[SerializeField] private bool showHeatmapOnEnable;

	private Coroutine _presetRoutine;
	private bool _showSecondPreset;

	private void OnEnable()
	{
		PerformanceMeter.AlertFired += HandleAlertFired;
		ApplyOverlayPreset(firstPreset);

		if (startSessionOnEnable)
		{
			StartSession();
		}

		if (showHeatmapOnEnable)
		{
			PerformanceMeter.SetOverdrawHeatmapVisible(true);
		}

		if (requestOverdrawOnEnable)
		{
			RequestOverdrawMeasurement();
		}

		if (cyclePresets)
		{
			_presetRoutine = StartCoroutine(CycleOverlayPresets());
		}
	}

	private void OnDisable()
	{
		PerformanceMeter.AlertFired -= HandleAlertFired;

		if (_presetRoutine != null)
		{
			StopCoroutine(_presetRoutine);
			_presetRoutine = null;
		}

		if (stopAndExportSessionOnDisable && PerformanceMeter.IsSessionRecording)
		{
			StopAndExportSession();
		}

		if (PerformanceMeter.GetStatus().State == PerfMeterRuntimeState.Running)
		{
			PerformanceMeter.SetOverdrawHeatmapVisible(false);
		}
	}

	[ContextMenu("SGG PerfMeter/Apply First Preset")]
	public void ApplyFirstPreset()
	{
		ApplyOverlayPreset(firstPreset);
	}

	[ContextMenu("SGG PerfMeter/Apply Second Preset")]
	public void ApplySecondPreset()
	{
		ApplyOverlayPreset(secondPreset);
	}

	[ContextMenu("SGG PerfMeter/Start Session")]
	public void StartSession()
	{
		PerformanceMeter.StartSession(new PerfMeterSessionOptions(
			0,
			Mathf.Max(0f, sessionWarmupSeconds),
			Mathf.Max(0.02f, sessionSampleIntervalSeconds),
			Mathf.Max(1, sessionMaxSamples),
			false,
			2,
			0.25f));
	}

	[ContextMenu("SGG PerfMeter/Stop And Export Session")]
	public void StopAndExportSession()
	{
		if (!PerformanceMeter.IsSessionRecording)
		{
			Debug.Log("SGG PerfMeter sample: no active session to export.");
			return;
		}

		PerformanceMeter.StopSession();
		string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
		string basePath = Path.Combine(Application.persistentDataPath, "sgg-perfmeter-session-" + timestamp);
		string jsonPath = basePath + ".json";
		string csvPath = basePath + ".csv";
		bool jsonWritten = PerformanceMeter.ExportSessionJson(jsonPath);
		bool csvWritten = PerformanceMeter.ExportSessionCsv(csvPath);

		Debug.Log("SGG PerfMeter sample session export: JSON " + jsonWritten + " at " + jsonPath + ", CSV " + csvWritten + " at " + csvPath);
	}

	[ContextMenu("SGG PerfMeter/Request Overdraw Measurement")]
	public void RequestOverdrawMeasurement()
	{
		PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.OverdrawDiagnostic);
		PerformanceMeter.RequestOverdrawMeasurement(Mathf.Max(1, overdrawFrameCount));
	}

	[ContextMenu("SGG PerfMeter/Toggle Heatmap")]
	public void ToggleHeatmap()
	{
		PerformanceMeter.SetOverdrawHeatmapVisible(!PerformanceMeter.IsOverdrawHeatmapVisible);
	}

	[ContextMenu("SGG PerfMeter/Log Status")]
	public void LogStatus()
	{
		PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
		PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
		Debug.Log("SGG PerfMeter sample status: " + status.CollectionMode + ", " + metrics.Bottleneck + ", CPU " + metrics.CpuFrameTimeMs.ToString("0.00", CultureInfo.InvariantCulture) + " ms, GPU available " + metrics.GpuFrameTimeAvailable);
	}

	private IEnumerator CycleOverlayPresets()
	{
		WaitForSeconds wait = new WaitForSeconds(Mathf.Max(1f, presetSwitchIntervalSeconds));
		while (enabled)
		{
			yield return wait;
			_showSecondPreset = !_showSecondPreset;
			ApplyOverlayPreset(_showSecondPreset ? secondPreset : firstPreset);
		}
	}

	private void ApplyOverlayPreset(PerfMeterOverlayPreset preset)
	{
		PerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay);
		PerformanceMeter.SetTargetFps(targetFps);
		PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
		PerformanceMeter.SetOverlayPreset(preset);
		PerformanceMeter.SetOverlayVisible(true);
	}

	private static void HandleAlertFired(PerfMeterAlertSnapshot alert)
	{
		Debug.LogWarning("SGG PerfMeter alert: " + alert.Message);
	}
}
