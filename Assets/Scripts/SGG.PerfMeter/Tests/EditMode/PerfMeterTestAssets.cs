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
	}
}
