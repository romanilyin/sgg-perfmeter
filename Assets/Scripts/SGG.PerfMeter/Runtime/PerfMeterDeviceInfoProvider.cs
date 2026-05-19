using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal static class PerfMeterDeviceInfoProvider
	{
		private static readonly List<DisplayInfo> DisplaysBuffer = new List<DisplayInfo>(4);

		internal static PerfMeterDeviceSnapshot CreateSnapshot()
		{
			Resolution currentResolution = Screen.currentResolution;
			RefreshRate currentRefreshRate = currentResolution.refreshRateRatio;
			Vector2Int mainWindowPosition = default;
			bool mainWindowPositionAvailable = TryGetMainWindowPosition(out mainWindowPosition);
			bool hasMainWindowDisplay = TryGetMainWindowDisplay(out DisplayInfo mainWindowDisplay);
			PerfMeterDisplaySnapshot[] displays = BuildDisplays(hasMainWindowDisplay, mainWindowDisplay, currentResolution, out bool displayLayoutAvailable, out string displayLayoutWarning);

			return new PerfMeterDeviceSnapshot(
				Application.unityVersion,
				Application.platform,
				Application.isEditor,
				SystemInfo.operatingSystem,
				SystemInfo.deviceModel,
				SystemInfo.deviceType,
				SystemInfo.processorType,
				SystemInfo.processorCount,
				SystemInfo.processorFrequency,
				SystemInfo.systemMemorySize,
				SystemInfo.graphicsDeviceType,
				SystemInfo.graphicsDeviceName,
				SystemInfo.graphicsDeviceVendor,
				SystemInfo.graphicsDeviceVersion,
				SystemInfo.graphicsMemorySize,
				SystemInfo.graphicsShaderLevel,
				SystemInfo.graphicsMultiThreaded,
				SystemInfo.maxTextureSize,
				SystemInfo.supportsComputeShaders,
				SystemInfo.supportsAsyncGPUReadback,
				SystemInfo.supportsInstancing,
				SystemInfo.supportsGraphicsFence,
				Screen.width,
				Screen.height,
				currentResolution.width,
				currentResolution.height,
				currentRefreshRate.numerator,
				currentRefreshRate.denominator,
				currentRefreshRate.value,
				Screen.dpi,
				Screen.fullScreen,
				Screen.fullScreenMode,
				mainWindowPositionAvailable,
				mainWindowPosition.x,
				mainWindowPosition.y,
				displayLayoutAvailable,
				displayLayoutWarning,
				displays);
		}

		private static PerfMeterDisplaySnapshot[] BuildDisplays(bool hasMainWindowDisplay, DisplayInfo mainWindowDisplay, Resolution currentResolution, out bool displayLayoutAvailable, out string warning)
		{
			DisplaysBuffer.Clear();
			displayLayoutAvailable = false;
			warning = string.Empty;

			try
			{
				Screen.GetDisplayLayout(DisplaysBuffer);
				displayLayoutAvailable = DisplaysBuffer.Count > 0;
			}
			catch (Exception exception)
			{
				warning = "Display layout is unavailable: " + exception.Message;
			}

			if (DisplaysBuffer.Count == 0)
			{
				RefreshRate fallbackRefreshRate = currentResolution.refreshRateRatio;
				return new[]
				{
					new PerfMeterDisplaySnapshot(
						0,
						"Current Resolution",
						currentResolution.width,
						currentResolution.height,
						0,
						0,
						currentResolution.width,
						currentResolution.height,
						fallbackRefreshRate.numerator,
						fallbackRefreshRate.denominator,
						fallbackRefreshRate.value,
						true,
						true)
				};
			}

			PerfMeterDisplaySnapshot[] displays = new PerfMeterDisplaySnapshot[DisplaysBuffer.Count];
			for (int i = 0; i < DisplaysBuffer.Count; i++)
			{
				DisplayInfo display = DisplaysBuffer[i];
				RectInt workArea = display.workArea;
				RefreshRate refreshRate = display.refreshRate;
				displays[i] = new PerfMeterDisplaySnapshot(
					i,
					display.name,
					display.width,
					display.height,
					workArea.x,
					workArea.y,
					workArea.width,
					workArea.height,
					refreshRate.numerator,
					refreshRate.denominator,
					refreshRate.value,
					hasMainWindowDisplay && DisplayMatches(display, mainWindowDisplay),
					false);
			}

			return displays;
		}

		private static bool TryGetMainWindowPosition(out Vector2Int position)
		{
			try
			{
				position = Screen.mainWindowPosition;
				return true;
			}
			catch
			{
				position = default;
				return false;
			}
		}

		private static bool TryGetMainWindowDisplay(out DisplayInfo display)
		{
			try
			{
				display = Screen.mainWindowDisplayInfo;
				return display.width > 0 || display.height > 0 || !string.IsNullOrEmpty(display.name);
			}
			catch
			{
				display = default;
				return false;
			}
		}

		private static bool DisplayMatches(DisplayInfo left, DisplayInfo right)
		{
			return string.Equals(left.name, right.name, StringComparison.Ordinal) &&
				left.width == right.width &&
				left.height == right.height &&
				left.workArea.Equals(right.workArea) &&
				left.refreshRate.numerator == right.refreshRate.numerator &&
				left.refreshRate.denominator == right.refreshRate.denominator;
		}
	}
}
