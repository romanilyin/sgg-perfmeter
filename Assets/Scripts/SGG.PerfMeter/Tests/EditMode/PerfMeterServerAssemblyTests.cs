using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterServerAssemblyTests
	{
		[TestCase("UNITY_STANDALONE_WIN")]
		[TestCase("UNITY_STANDALONE_LINUX")]
		[TestCase("UNITY_STANDALONE_OSX")]
		public void DedicatedServerExcludesPlayerAssembliesAndPreservesEditor(string platformDefine)
		{
			string corePath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("SGG.PerfMeter");
			UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(corePath);
			string packageRoot = package != null ? package.resolvedPath : Path.GetDirectoryName(Path.GetDirectoryName(corePath));
			int checkedAssemblies = 0;
			foreach (string folder in new[] { "Runtime", "Tests", "Samples~" })
			{
				foreach (string path in Directory.GetFiles(Path.Combine(packageRoot, folder), "*.asmdef", SearchOption.AllDirectories))
				{
					AssemblyDefinition definition = JsonUtility.FromJson<AssemblyDefinition>(File.ReadAllText(path));
					if (Array.IndexOf(definition.includePlatforms ?? Array.Empty<string>(), "Editor") >= 0)
					{
						continue;
					}
					Assert.That(definition.excludePlatforms, Does.Contain("CloudRendering"), definition.name + " Linux headless simulation");
					foreach (string excludedPlatform in definition.excludePlatforms)
					{
						Assert.That(Array.Exists(CompilationPipeline.GetAssemblyDefinitionPlatforms(), platform => platform.Name == excludedPlatform),
							Is.True, definition.name + " unknown platform: " + excludedPlatform);
					}

					// Enable every optional integration so its package/version gate cannot hide a server regression.
					List<string> defines = new List<string>
					{
						"UNITY_6000_4_OR_NEWER", "UNITY_STANDALONE", platformDefine, "UNITY_INCLUDE_TESTS"
					};
					foreach (VersionDefine version in definition.versionDefines ?? Array.Empty<VersionDefine>())
					{
						defines.Add(version.define);
					}

					bool editorOnlyTest = Array.IndexOf(definition.defineConstraints, "UNITY_EDITOR") >= 0;
					Assert.That(CompilationPipeline.IsDefineConstraintsCompatible(defines.ToArray(), definition.defineConstraints),
						Is.EqualTo(!editorOnlyTest), definition.name + " client");
					defines.Add("UNITY_EDITOR");
					Assert.That(CompilationPipeline.IsDefineConstraintsCompatible(defines.ToArray(), definition.defineConstraints),
						Is.True, definition.name + " editor with client target");
					defines.Add("UNITY_SERVER");
					Assert.That(CompilationPipeline.IsDefineConstraintsCompatible(defines.ToArray(), definition.defineConstraints),
						Is.True, definition.name + " editor with server target");
					defines.Remove("UNITY_EDITOR");
					Assert.That(CompilationPipeline.IsDefineConstraintsCompatible(defines.ToArray(), definition.defineConstraints),
						Is.False, definition.name + " dedicated server");
					checkedAssemblies++;
				}
			}

			Assert.That(checkedAssemblies, Is.GreaterThanOrEqualTo(11), "Include runtime integrations, PlayMode tests and imported samples.");
		}

		[Serializable]
		private sealed class AssemblyDefinition
		{
			public string name;
			public string[] includePlatforms;
			public string[] excludePlatforms;
			public string[] defineConstraints;
			public VersionDefine[] versionDefines;
		}

		[Serializable]
		private sealed class VersionDefine
		{
			public string define;
		}
	}
}