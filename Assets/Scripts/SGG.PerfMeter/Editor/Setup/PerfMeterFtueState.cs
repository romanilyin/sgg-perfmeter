using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RuntimePerformanceMeter = SGG.PerfMeter.PerformanceMeter;

namespace SGG.PerfMeter.Editor.Setup
{
	internal static class PerfMeterFtueState
	{
		internal const string MemoryProfilerId = "memory-profiler";
		internal const string AdaptivePerformanceId = "adaptive-performance";
		internal const string ProfileAnalyzerId = "profile-analyzer";
		internal const string GraphicsStateCollectionId = "graphics-state-collection";
		internal const string RenderDocId = "renderdoc";
		internal const string PixId = "pix";

		private const string PackageVersionMetadataKey = "SGG.PerfMeter.PackageVersion";
		private const string PreferencesPrefix = "SGG.PerfMeter.Ftue.";
		private static readonly string[] OptionalIds =
		{
			MemoryProfilerId,
			AdaptivePerformanceId,
			ProfileAnalyzerId,
			GraphicsStateCollectionId,
			RenderDocId,
			PixId
		};

		internal static string PackageVersion
		{
			get { return ResolvePackageVersion(); }
		}

		internal static string ProjectKey
		{
			get { return BuildProjectKey(Application.dataPath); }
		}

		internal static bool IsSkipped(string optionalId)
		{
			if (!IsKnownOptionalId(optionalId))
			{
				return false;
			}

			return EditorPrefs.GetBool(BuildPreferenceKey(optionalId), false);
		}

		internal static void SetSkipped(string optionalId)
		{
			SetSkipped(optionalId, true);
		}

		internal static void SetSkipped(string optionalId, bool skipped)
		{
			if (!IsKnownOptionalId(optionalId))
			{
				return;
			}

			string key = BuildPreferenceKey(optionalId);
			if (skipped)
			{
				EditorPrefs.SetBool(key, true);
			}
			else
			{
				EditorPrefs.DeleteKey(key);
			}
		}

		internal static void ResetChoices()
		{
			for (int index = 0; index < OptionalIds.Length; index++)
			{
				EditorPrefs.DeleteKey(BuildPreferenceKey(OptionalIds[index]));
			}
		}

		internal static bool IsOptionalResolved(string optionalId, bool available)
		{
			return available || IsSkipped(optionalId);
		}

		internal static bool ResolveExternalToolAvailability(bool capabilityAvailable, bool otherCapabilityAvailable, bool otherToolSkipped)
		{
			return capabilityAvailable && (!otherCapabilityAvailable || otherToolSkipped);
		}

		/// <summary>
		/// Returns true only when at least one required check exists, every required check is ready,
		/// and every optional check is either available or skipped by the caller.
		/// A null or empty sequence is not a valid required checklist; a null optional sequence is invalid.
		/// </summary>
		internal static bool AreAllStepsResolved(IEnumerable<bool> requiredReady, IEnumerable<bool> optionalResolved)
		{
			if (requiredReady == null || optionalResolved == null)
			{
				return false;
			}

			bool hasRequiredStep = false;
			foreach (bool ready in requiredReady)
			{
				hasRequiredStep = true;
				if (!ready)
				{
					return false;
				}
			}

			if (!hasRequiredStep)
			{
				return false;
			}

			foreach (bool resolved in optionalResolved)
			{
				if (!resolved)
				{
					return false;
				}
			}

			return true;
		}

		private static string ResolvePackageVersion()
		{
			object[] attributes = typeof(RuntimePerformanceMeter).Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
			for (int index = 0; index < attributes.Length; index++)
			{
				AssemblyMetadataAttribute metadata = (AssemblyMetadataAttribute)attributes[index];
				if (string.Equals(metadata.Key, PackageVersionMetadataKey, StringComparison.Ordinal))
				{
					return metadata.Value ?? string.Empty;
				}
			}

			return string.Empty;
		}

		private static bool IsKnownOptionalId(string optionalId)
		{
			if (string.IsNullOrEmpty(optionalId))
			{
				return false;
			}

			for (int index = 0; index < OptionalIds.Length; index++)
			{
				if (string.Equals(OptionalIds[index], optionalId, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		private static string BuildPreferenceKey(string optionalId)
		{
			string version = string.IsNullOrEmpty(PackageVersion) ? "unknown" : PackageVersion;
			return PreferencesPrefix + ProjectKey + "." + version + "." + optionalId;
		}

		private static string BuildProjectKey(string dataPath)
		{
			string normalizedPath = (dataPath ?? string.Empty).Replace('\\', '/').TrimEnd('/');
			uint hash = 2166136261u;
			for (int index = 0; index < normalizedPath.Length; index++)
			{
				char character = char.ToUpperInvariant(normalizedPath[index]);
				hash ^= character;
				hash *= 16777619u;
			}

			return hash.ToString("X8", CultureInfo.InvariantCulture);
		}
	}
}
