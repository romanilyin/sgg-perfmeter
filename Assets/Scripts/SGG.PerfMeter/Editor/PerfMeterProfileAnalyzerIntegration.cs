using UnityEditor;
using UnityEngine;

namespace SGG.PerfMeter.Editor
{
	public static class PerfMeterProfileAnalyzerIntegration
	{
		public const string ProfileAnalyzerMenuItem = "Window/Analysis/Profile Analyzer";
		private const string OpenSessionMenuItem = "SGG/Perfmeter/Open Profile Analyzer For Session";

		[MenuItem(OpenSessionMenuItem)]
		public static void OpenProfileAnalyzerForSession()
		{
			TryOpenProfileAnalyzerForCurrentSession();
		}

		public static bool TryOpenProfileAnalyzerForCurrentSession()
		{
			PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
			if (string.IsNullOrEmpty(summary.SessionId))
			{
				Warn("No current PerfMeter session is available. Start or stop a session before opening Profile Analyzer.");
				return false;
			}

			if (!EditorApplication.ExecuteMenuItem(ProfileAnalyzerMenuItem))
			{
				Warn("Profile Analyzer is unavailable because the Window/Analysis/Profile Analyzer menu was not found. Install or enable the Profile Analyzer package.");
				return false;
			}

			EditorGUIUtility.systemCopyBuffer = summary.SessionId;
			return true;
		}

		public static string GetSessionMarkerPrefix(string sessionId)
		{
			return string.IsNullOrEmpty(sessionId) ? string.Empty : PerfMeterProfilerInstrumentation.SessionMarkerPrefix + sessionId + ".";
		}

		private static void Warn(string message)
		{
			Debug.LogWarning("[SGG.PerfMeter] " + message);
		}
	}
}
