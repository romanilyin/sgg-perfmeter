#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Globalization;
using SGG.PerfMeter;
using UnityEngine;

internal sealed class PerfMeterAndroidSmokeBootstrap : MonoBehaviour
{
	private const string LogPrefix = "SGG_PERFMETER_SMOKE";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void StartPerfMeter()
	{
		PerformanceMeter.EnsureRunning();
		PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
		PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
		PerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.Full);
		PerformanceMeter.SetOverlayVisible(true);

		GameObject gameObject = new GameObject("SGG PerfMeter Android Smoke Bootstrap");
		gameObject.hideFlags = HideFlags.DontSave;
		DontDestroyOnLoad(gameObject);
		gameObject.AddComponent<PerfMeterAndroidSmokeBootstrap>();

		Debug.Log(LogPrefix + " bootstrapped");
	}

	private IEnumerator Start()
	{
		yield return WaitForFrames(30);
		LogSnapshot("initial");

		PerformanceMeter.RequestOverdrawMeasurement(60);
		Debug.Log(LogPrefix + " overdraw_requested");

		yield return WaitForOverdrawResult(360);
		LogSnapshot("after_overdraw");
	}

	private static IEnumerator WaitForFrames(int frameCount)
	{
		for (int i = 0; i < frameCount; i++)
		{
			yield return null;
		}
	}

	private static IEnumerator WaitForOverdrawResult(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			if (status.OverdrawState == PerfMeterOverdrawMeasurementState.Completed ||
				status.OverdrawState == PerfMeterOverdrawMeasurementState.Unsupported ||
				status.OverdrawState == PerfMeterOverdrawMeasurementState.Error)
			{
				yield break;
			}

			yield return null;
		}
	}

	private static void LogSnapshot(string stage)
	{
		PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
		PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
		Debug.Log(LogPrefix +
			" stage=" + stage +
			" state=" + status.State +
			" collection_frame=" + status.CollectionFrame +
			" frame_timing=" + status.FrameTimingAvailability +
			" graphics=" + status.GraphicsDeviceType +
			" gpu_available=" + metrics.GpuFrameTimeAvailable +
			" cpu_ms=" + Format(metrics.CpuFrameTimeMs) +
			" gpu_ms=" + Format(metrics.GpuFrameTimeMs) +
			" draws=" + metrics.DrawCalls +
			" setpass=" + metrics.SetPassCalls +
			" overdraw_state=" + status.OverdrawState +
			" overdraw_progress=" + Format(status.OverdrawProgress) +
			" overdraw_ratio=" + Format(status.OverdrawRatio) +
			" warning=" + status.Warning +
			" last_error=" + status.LastError);
	}

	private static string Format(double value)
	{
		return value.ToString("0.00", CultureInfo.InvariantCulture);
	}
}
#endif
