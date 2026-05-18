using System;
using System.IO;
using System.Reflection;
using SGG.PerfMeter.Editor.Setup;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PerfMeterAndroidBuild
{
	private const string DefaultOutputPath = "Builds/Android/SGGPerfMeter-S23-dev.apk";
	private const string DefaultSdkRoot = "C:/Work/SDK/AndroidSDK";
	private const string RequiredNdkVersion = "27.2.12479018";

	public static void BuildDevelopmentApk()
	{
		ConfigureAndroidExternalTools();

		if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
		{
			throw new InvalidOperationException("Failed to switch Unity build target to Android.");
		}

		EditorUserBuildSettings.buildAppBundle = false;
		PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
		PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

		PerfMeterSetupActionResult setupResult = PerfMeterSetupActions.RunRecommendedSetup();
		if (!setupResult.Success)
		{
			throw new InvalidOperationException(setupResult.Message);
		}

		string outputPath = GetOutputPath();
		string outputDirectory = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrEmpty(outputDirectory))
		{
			Directory.CreateDirectory(outputDirectory);
		}

		BuildPlayerOptions options = new BuildPlayerOptions
		{
			scenes = GetEnabledScenes(),
			target = BuildTarget.Android,
			locationPathName = outputPath,
			options = BuildOptions.Development | BuildOptions.AllowDebugging
		};

		BuildReport report = BuildPipeline.BuildPlayer(options);
		if (report.summary.result != BuildResult.Succeeded)
		{
			throw new InvalidOperationException("Android build failed: " + report.summary.result);
		}

		Debug.Log("SGG PerfMeter Android build succeeded: " + outputPath + " (" + report.summary.totalSize + " bytes)");
	}

	private static string[] GetEnabledScenes()
	{
		int count = 0;
		for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
		{
			if (EditorBuildSettings.scenes[i].enabled)
			{
				count++;
			}
		}

		string[] scenes = new string[count];
		int sceneIndex = 0;
		for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
		{
			EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
			if (scene.enabled)
			{
				scenes[sceneIndex++] = scene.path;
			}
		}

		if (scenes.Length == 0)
		{
			throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
		}

		return scenes;
	}

	private static string GetOutputPath()
	{
		string outputPath = Environment.GetEnvironmentVariable("PERFMETER_ANDROID_APK");
		return string.IsNullOrEmpty(outputPath) ? DefaultOutputPath : outputPath.Replace('\\', '/');
	}

	private static void ConfigureAndroidExternalTools()
	{
		string sdkRoot = GetSdkRoot();
		string ndkRoot = GetPathFromEnvironment("ANDROID_NDK_ROOT", Path.Combine(sdkRoot, "ndk/" + RequiredNdkVersion));
		string jdkRoot = GetPathFromEnvironment("JAVA_HOME", Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/OpenJDK"));

		SetExternalToolPath("sdkRootPath", sdkRoot);
		SetExternalToolPath("ndkRootPath", ndkRoot);
		SetExternalToolPath("jdkRootPath", jdkRoot);

		EditorPrefs.SetString("AndroidSdkRoot", NormalizePath(sdkRoot));
		EditorPrefs.SetString("AndroidNdkRoot", NormalizePath(ndkRoot));
		EditorPrefs.SetString("JdkPath", NormalizePath(jdkRoot));
	}

	private static string GetPathFromEnvironment(string variableName, string fallbackPath)
	{
		string value = Environment.GetEnvironmentVariable(variableName);
		return string.IsNullOrEmpty(value) ? fallbackPath : value;
	}

	private static string GetSdkRoot()
	{
		string sdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
		if (!string.IsNullOrEmpty(sdkRoot))
		{
			return sdkRoot;
		}

		sdkRoot = Environment.GetEnvironmentVariable("ANDROID_HOME");
		return string.IsNullOrEmpty(sdkRoot) ? DefaultSdkRoot : sdkRoot;
	}

	private static void SetExternalToolPath(string propertyName, string path)
	{
		Type settingsType = FindType("UnityEditor.Android.AndroidExternalToolsSettings");
		PropertyInfo property = settingsType?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
		if (property == null || !property.CanWrite)
		{
			Debug.LogWarning("Could not set Android external tool path: " + propertyName + " = " + path);
			return;
		}

		property.SetValue(null, NormalizePath(path));
	}

	private static Type FindType(string typeName)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			Type type = assemblies[i].GetType(typeName);
			if (type != null)
			{
				return type;
			}
		}

		return null;
	}

	private static string NormalizePath(string path)
	{
		return (path ?? string.Empty).Replace('\\', '/');
	}
}
