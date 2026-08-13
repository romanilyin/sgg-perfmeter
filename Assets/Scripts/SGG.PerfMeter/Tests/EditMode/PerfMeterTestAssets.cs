using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	internal static class PerfMeterTestAssets
	{
		private const string PackageRoot = "Packages/com.sungeargames.perfmeter";
		private const string EmbeddedRoot = "Assets/Scripts/SGG.PerfMeter";

		internal static string ReadMcpCommandsJson()
		{
			TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PackageRoot + "/Editor/Mcp/mcp.commands.json")
				?? AssetDatabase.LoadAssetAtPath<TextAsset>(EmbeddedRoot + "/Editor/Mcp/mcp.commands.json");
			Assert.That(asset, Is.Not.Null, "mcp.commands.json must be available from package or embedded Assets path.");
			return asset.text;
		}

		internal static string ReadPackageJson()
		{
			TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PackageRoot + "/package.json")
				?? AssetDatabase.LoadAssetAtPath<TextAsset>(EmbeddedRoot + "/package.json");
			Assert.That(asset, Is.Not.Null, "package.json must be available from package or embedded Assets path.");
			return asset.text;
		}

		internal static string ReadAdaptivePerformanceAsmdef()
		{
			TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PackageRoot + "/Runtime/AdaptivePerformance/SGG.PerfMeter.AdaptivePerformance.asmdef")
				?? AssetDatabase.LoadAssetAtPath<TextAsset>(EmbeddedRoot + "/Runtime/AdaptivePerformance/SGG.PerfMeter.AdaptivePerformance.asmdef");
			Assert.That(asset, Is.Not.Null, "Adaptive Performance asmdef must be available from package or embedded Assets path.");
			return asset.text;
		}

		internal static string ReadRenderDocAnalyzerAsset(string relativePath)
		{
			TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PackageRoot + "/" + relativePath)
				?? AssetDatabase.LoadAssetAtPath<TextAsset>(EmbeddedRoot + "/" + relativePath);
			Assert.That(asset, Is.Not.Null, relativePath + " must be available from package or embedded Assets path.");
			return asset.text;
		}
	}
}
