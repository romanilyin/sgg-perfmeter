using System;
using UnityEngine;

namespace SGG.PerfMeter
{
	public readonly struct PerfMeterOverlayThemeManifest
	{
		internal PerfMeterOverlayThemeManifest(
			PerfMeterOverlayTheme theme,
			string id,
			string displayName,
			PerfMeterOverlayLayout defaultLayout,
			Color background,
			Color graphBackground,
			Color text,
			Color mutedText,
			Color frame,
			Color cpuMain,
			Color cpuRender,
			Color cpuOther,
			Color gpu,
			Color warning,
			Color accent,
			Color unavailable,
			Color grid,
			Color budget,
			string styleSheetResourcePath,
			string iconAtlasResourcePath)
		{
			Theme = theme;
			Id = id ?? string.Empty;
			DisplayName = displayName ?? string.Empty;
			DefaultLayout = defaultLayout;
			Background = background;
			GraphBackground = graphBackground;
			Text = text;
			MutedText = mutedText;
			Frame = frame;
			CpuMain = cpuMain;
			CpuRender = cpuRender;
			CpuOther = cpuOther;
			Gpu = gpu;
			Warning = warning;
			Accent = accent;
			Unavailable = unavailable;
			Grid = grid;
			Budget = budget;
			StyleSheetResourcePath = styleSheetResourcePath ?? string.Empty;
			IconAtlasResourcePath = iconAtlasResourcePath ?? string.Empty;
		}

		public PerfMeterOverlayTheme Theme { get; }
		public string Id { get; }
		public string DisplayName { get; }
		public PerfMeterOverlayLayout DefaultLayout { get; }
		public Color Background { get; }
		public Color GraphBackground { get; }
		public Color Text { get; }
		public Color MutedText { get; }
		public Color Frame { get; }
		public Color CpuMain { get; }
		public Color CpuRender { get; }
		public Color CpuOther { get; }
		public Color Gpu { get; }
		public Color Warning { get; }
		public Color Accent { get; }
		public Color Unavailable { get; }
		public Color Grid { get; }
		public Color Budget { get; }
		public string StyleSheetResourcePath { get; }
		public string IconAtlasResourcePath { get; }
	}

	public static class PerfMeterOverlayThemeRegistry
	{
		public static PerfMeterOverlayThemeManifest[] GetAllManifests()
		{
			PerfMeterOverlayTheme[] themes =
			{
				PerfMeterOverlayTheme.ClassicDark,
				PerfMeterOverlayTheme.Glass,
				PerfMeterOverlayTheme.Cyber,
				PerfMeterOverlayTheme.HighContrast
			};
			PerfMeterOverlayThemeManifest[] manifests = new PerfMeterOverlayThemeManifest[themes.Length];
			for (int i = 0; i < themes.Length; i++)
			{
				manifests[i] = PerfMeterOverlay.CreateThemeManifest(themes[i]);
			}

			return manifests;
		}

		public static PerfMeterOverlayThemeManifest GetManifest(PerfMeterOverlayTheme theme)
		{
			return PerfMeterOverlay.CreateThemeManifest(theme);
		}

		public static bool TryGetManifest(string id, out PerfMeterOverlayThemeManifest manifest)
		{
			if (Enum.TryParse(id, true, out PerfMeterOverlayTheme theme) && Enum.IsDefined(typeof(PerfMeterOverlayTheme), theme))
			{
				manifest = GetManifest(theme);
				return true;
			}

			manifest = GetManifest(PerfMeterOverlayTheme.ClassicDark);
			return false;
		}
	}
}
