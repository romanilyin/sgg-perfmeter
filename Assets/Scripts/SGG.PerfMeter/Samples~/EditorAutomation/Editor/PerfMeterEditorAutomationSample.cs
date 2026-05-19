using SGG.PerfMeter.Editor.Setup;
using UnityEditor;
using UnityEngine;

public static class PerfMeterEditorAutomationSample
{
	[MenuItem("SGG/PerfMeter Samples/Print Setup Status")]
	private static void PrintSetupStatus()
	{
		Debug.Log(PerfMeterSetupActions.GetStatusReport());
	}

	[MenuItem("SGG/PerfMeter Samples/Run Recommended Setup")]
	private static void RunRecommendedSetup()
	{
		PerfMeterSetupActionResult result = PerfMeterSetupActions.RunRecommendedSetup();
		Debug.Log(result.ToString());
	}

	[MenuItem("SGG/PerfMeter Samples/Create Default JSON Settings")]
	private static void CreateDefaultSettings()
	{
		PerfMeterSetupActionResult result = PerfMeterSetupActions.CreateDefaultSettings();
		Debug.Log(result.ToString());
	}

	[MenuItem("SGG/PerfMeter Samples/Apply JSON Settings To Runtime")]
	private static void ApplySettingsToRuntime()
	{
		PerfMeterSetupActionResult result = PerfMeterSetupActions.ApplySettingsToRuntime();
		Debug.Log(result.ToString());
	}
}
