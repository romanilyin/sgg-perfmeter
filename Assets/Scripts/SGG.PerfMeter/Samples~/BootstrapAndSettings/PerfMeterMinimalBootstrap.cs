using SGG.PerfMeter;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class PerfMeterMinimalBootstrap : MonoBehaviour
{
	[SerializeField] private PerfMeterCollectionMode collectionMode = PerfMeterCollectionMode.Overlay;
	[SerializeField] private PerfMeterTargetFps targetFps = PerfMeterTargetFps.Fps60;
	[SerializeField] private PerfMeterOverlayPreset overlayPreset = PerfMeterOverlayPreset.FullDiagnostics;
	[SerializeField] private PerfMeterOverlayCorner overlayCorner = PerfMeterOverlayCorner.TopRight;

	private void Awake()
	{
		if (collectionMode == PerfMeterCollectionMode.Stopped)
		{
			PerformanceMeter.Stop();
			return;
		}

		PerformanceMeter.SetCollectionMode(collectionMode);
		PerformanceMeter.SetTargetFps(targetFps);
		PerformanceMeter.SetOverlayPreset(overlayPreset);
		PerformanceMeter.SetOverlayCorner(overlayCorner);
		PerformanceMeter.SetOverlayVisible(collectionMode != PerfMeterCollectionMode.Background);
	}
}
