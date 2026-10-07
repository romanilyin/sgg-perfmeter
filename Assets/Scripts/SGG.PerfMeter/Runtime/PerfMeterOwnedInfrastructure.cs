using UnityEngine;

namespace SGG.PerfMeter
{
	internal enum PerfMeterInfrastructureKind
	{
		Runtime,
		Overlay,
		PanelHost
	}

	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	internal sealed class PerfMeterOwnedInfrastructure : MonoBehaviour
	{
		[SerializeField] private PerfMeterInfrastructureKind _kind;
		private bool _destroyRequested;

		internal PerfMeterInfrastructureKind Kind => _kind;
		internal bool DestroyRequested => _destroyRequested;

		internal static void Mark(GameObject target, PerfMeterInfrastructureKind kind)
		{
			PerfMeterOwnedInfrastructure marker = target.GetComponent<PerfMeterOwnedInfrastructure>();
			if (marker == null)
			{
				marker = target.AddComponent<PerfMeterOwnedInfrastructure>();
			}
			marker._kind = kind;
		}

		internal static bool IsTransient(GameObject target)
		{
			if (target == null || (target.hideFlags & HideFlags.DontSave) != HideFlags.DontSave)
			{
				return false;
			}
#if UNITY_EDITOR
			if (UnityEditor.EditorUtility.IsPersistent(target))
			{
				return false;
			}
#endif
			return true;
		}

		internal static bool DestroyOwned(GameObject target, PerfMeterInfrastructureKind kind)
		{
			if (!IsTransient(target))
			{
				return false;
			}
			PerfMeterOwnedInfrastructure marker = target.GetComponent<PerfMeterOwnedInfrastructure>();
			bool legacyOwned = kind == PerfMeterInfrastructureKind.Runtime && target.GetComponent<PerfMeterRuntime>() != null ||
				kind == PerfMeterInfrastructureKind.Overlay && target.GetComponent<PerfMeterOverlay>() != null;
			if (marker == null && !legacyOwned)
			{
				return false;
			}
			if (marker != null && marker._destroyRequested)
			{
				return false;
			}
			Mark(target, kind);
			marker = target.GetComponent<PerfMeterOwnedInfrastructure>();
			marker._destroyRequested = true;
			target.SetActive(false);
			if (Application.isPlaying)
			{
				Object.Destroy(target);
			}
			else
			{
				Object.DestroyImmediate(target);
			}
			return true;
		}
	}
}
