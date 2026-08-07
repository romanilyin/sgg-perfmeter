using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace SGG.PerfMeter.Editor.Setup
{
	internal static class PerfMeterOptionalDependencyInstaller
	{
		internal const string MemoryProfilerPackageId = "com.unity.memoryprofiler";
		internal const string MemoryProfilerPackageVersion = "1.1.12";
		internal const string MemoryProfilerPackageSpec = MemoryProfilerPackageId + "@" + MemoryProfilerPackageVersion;
		internal const string AdaptivePerformancePackageId = "com.unity.adaptiveperformance";
		internal const string AdaptivePerformancePackageVersion = "5.1.6";
		internal const string AdaptivePerformancePackageSpec = AdaptivePerformancePackageId + "@" + AdaptivePerformancePackageVersion;
		internal const string ProfileAnalyzerPackageId = "com.unity.performance.profile-analyzer";
		internal const string ProfileAnalyzerPackageVersion = "1.4.0";
		internal const string ProfileAnalyzerPackageSpec = ProfileAnalyzerPackageId + "@" + ProfileAnalyzerPackageVersion;

		private static AddRequest _request;
		private static string _packageId;
		private static string _displayName;
		private static string _failedPackageId;
		private static string _failedMessage;

		internal static bool HasActiveInstall
		{
			get { return _request != null && !_request.IsCompleted; }
		}

		internal static string ActiveDisplayName
		{
			get { return _displayName; }
		}

		internal static bool Update()
		{
			if (_request == null || !_request.IsCompleted)
			{
				return false;
			}

			string packageId = _packageId;
			string displayName = string.IsNullOrEmpty(_displayName) ? packageId : _displayName;
			if (_request.Status == StatusCode.Success)
			{
				if (string.Equals(_failedPackageId, packageId, StringComparison.Ordinal))
				{
					_failedPackageId = null;
					_failedMessage = null;
				}

				Debug.Log("Optional dependency installed: " + displayName + ".");
			}
			else
			{
				_failedPackageId = packageId;
				_failedMessage = _request.Error == null ? "Unknown Package Manager error." : _request.Error.message;
				Debug.LogError("Optional dependency install failed: " + displayName + ". " + _failedMessage);
			}

			_request = null;
			_packageId = null;
			_displayName = null;
			return true;
		}

		internal static bool IsInstalling(string packageId)
		{
			return HasActiveInstall && string.Equals(_packageId, packageId, StringComparison.Ordinal);
		}

		internal static bool TryStartInstall(string packageId, string packageSpec, string displayName, out string error)
		{
			error = null;
			if (HasActiveInstall)
			{
				error = "Already installing " + ActiveDisplayName + ". Wait for Package Manager to finish.";
				return false;
			}

			if (_request != null)
			{
				Update();
			}

			if (string.IsNullOrEmpty(packageId))
			{
				error = "Package id is required.";
				return false;
			}

			if (string.IsNullOrEmpty(packageSpec))
			{
				error = "Package spec is required.";
				return false;
			}

			string resolvedDisplayName = string.IsNullOrEmpty(displayName) ? packageId : displayName;
			try
			{
				AddRequest request = Client.Add(packageSpec);
				if (request == null)
				{
					throw new InvalidOperationException("Unity Package Manager returned no install request.");
				}

				_request = request;
				_packageId = packageId;
				_displayName = resolvedDisplayName;
				_failedPackageId = null;
				_failedMessage = null;
				Debug.Log("Optional dependency install started: " + resolvedDisplayName + " from " + packageSpec + ".");
				return true;
			}
			catch (Exception exception)
			{
				_request = null;
				_packageId = null;
				_displayName = null;
				_failedPackageId = packageId;
				_failedMessage = exception.Message;
				error = exception.Message;
				Debug.LogError("Optional dependency install failed to start: " + resolvedDisplayName + ". " + exception);
				return false;
			}
		}

		internal static bool TryGetLastError(string packageId, out string message)
		{
			message = null;
			if (!string.Equals(_failedPackageId, packageId, StringComparison.Ordinal))
			{
				return false;
			}

			message = _failedMessage;
			return !string.IsNullOrEmpty(message);
		}

		internal static bool TryGetRegisteredPackageVersion(string packageId, out string version)
		{
			version = string.Empty;
			if (string.IsNullOrEmpty(packageId))
			{
				return false;
			}

			PackageInfo[] packages;
			try
			{
				packages = PackageInfo.GetAllRegisteredPackages();
			}
			catch (Exception exception)
			{
				Debug.LogWarning("Could not inspect registered packages: " + exception.Message);
				return false;
			}

			if (packages == null)
			{
				return false;
			}

			for (int index = 0; index < packages.Length; index++)
			{
				PackageInfo packageInfo = packages[index];
				if (packageInfo != null && string.Equals(packageInfo.name, packageId, StringComparison.OrdinalIgnoreCase))
				{
					version = packageInfo.version ?? string.Empty;
					return !string.IsNullOrEmpty(version);
				}
			}

			return false;
		}

		internal static string GetRegisteredPackageVersion(string packageId)
		{
			return TryGetRegisteredPackageVersion(packageId, out string version) ? version : string.Empty;
		}

		/// <summary>
		/// Compares numeric dotted version components. Valid suffixes are accepted and do not change the numeric floor.
		/// </summary>
		internal static bool IsVersionAtLeast(string current, string minimum)
		{
			if (!TryParseVersion(current, out List<string> currentParts) || !TryParseVersion(minimum, out List<string> minimumParts))
			{
				return false;
			}

			int partCount = Math.Max(currentParts.Count, minimumParts.Count);
			for (int index = 0; index < partCount; index++)
			{
				string currentPart = index < currentParts.Count ? currentParts[index] : "0";
				string minimumPart = index < minimumParts.Count ? minimumParts[index] : "0";
				if (currentPart.Length != minimumPart.Length)
				{
					return currentPart.Length > minimumPart.Length;
				}

				int comparison = string.CompareOrdinal(currentPart, minimumPart);
				if (comparison != 0)
				{
					return comparison > 0;
				}
			}

			return true;
		}

		private static bool TryParseVersion(string value, out List<string> numericParts)
		{
			numericParts = null;
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}

			string normalized = value.Trim();
			if (normalized.Length == 0)
			{
				return false;
			}

			List<string> parts = new List<string>();
			int index = 0;
			while (true)
			{
				int numberStart = index;
				while (index < normalized.Length && IsAsciiDigit(normalized[index]))
				{
					index++;
				}

				if (index == numberStart)
				{
					return false;
				}

				parts.Add(NormalizeNumber(normalized.Substring(numberStart, index - numberStart)));
				if (index == normalized.Length)
				{
					numericParts = parts;
					return true;
				}

				char next = normalized[index];
				if (next == '.' && index + 1 < normalized.Length && IsAsciiDigit(normalized[index + 1]))
				{
					index++;
					continue;
				}

				if (next == '-' || next == '+' || IsAsciiLetter(next))
				{
					if (!IsValidSuffix(normalized, index))
					{
						return false;
					}

					numericParts = parts;
					return true;
				}

				return false;
			}
		}

		private static bool IsValidSuffix(string value, int startIndex)
		{
			char previous = '\0';
			for (int index = startIndex; index < value.Length; index++)
			{
				char character = value[index];
				if (!IsAsciiLetterOrDigit(character) && character != '.' && character != '-' && character != '+')
				{
					return false;
				}

				if ((character == '.' && index == startIndex) ||
					(character == '.' || character == '-' || character == '+') &&
					(index == value.Length - 1 || previous == '.' || previous == '-' || previous == '+'))
				{
					return false;
				}

				previous = character;
			}

			return startIndex < value.Length;
		}

		private static string NormalizeNumber(string value)
		{
			int firstNonZero = 0;
			while (firstNonZero < value.Length - 1 && value[firstNonZero] == '0')
			{
				firstNonZero++;
			}

			return value.Substring(firstNonZero);
		}

		private static bool IsAsciiDigit(char character)
		{
			return character >= '0' && character <= '9';
		}

		private static bool IsAsciiLetter(char character)
		{
			return character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
		}

		private static bool IsAsciiLetterOrDigit(char character)
		{
			return IsAsciiDigit(character) || IsAsciiLetter(character);
		}
	}
}
