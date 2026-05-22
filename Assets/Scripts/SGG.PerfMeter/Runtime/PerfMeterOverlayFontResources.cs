using UnityEngine;

namespace SGG.PerfMeter
{
	public sealed class PerfMeterOverlayFontResources : ScriptableObject
	{
		[SerializeField] private Font _manropeRegular;
		[SerializeField] private Font _manropeMedium;
		[SerializeField] private Font _manropeSemiBold;
		[SerializeField] private Font _manropeBold;
		[SerializeField] private Font _jetBrainsMonoRegular;
		[SerializeField] private Font _jetBrainsMonoMedium;

		internal Font ManropeRegular => _manropeRegular;
		internal Font ManropeMedium => _manropeMedium != null ? _manropeMedium : _manropeRegular;
		internal Font ManropeSemiBold => _manropeSemiBold != null ? _manropeSemiBold : ManropeMedium;
		internal Font ManropeBold => _manropeBold != null ? _manropeBold : ManropeSemiBold;
		internal Font JetBrainsMonoRegular => _jetBrainsMonoRegular;
		internal Font JetBrainsMonoMedium => _jetBrainsMonoMedium != null ? _jetBrainsMonoMedium : _jetBrainsMonoRegular;
	}
}
