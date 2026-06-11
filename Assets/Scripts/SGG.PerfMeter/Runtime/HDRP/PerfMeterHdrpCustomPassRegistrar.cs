using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SGG.PerfMeter
{
	internal static class PerfMeterHdrpCustomPassRegistrar
	{
		private static PerfMeterHdrpCustomPass _registeredPass;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			Unregister();
			_registeredPass = null;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void RegisterIfHdrpActive()
		{
			if (_registeredPass != null || PerfMeterRenderPipelineDetector.GetActiveKind() != PerfMeterRenderPipelineKind.HighDefinition)
			{
				return;
			}

			PerfMeterHdrpCustomPass customPass = new PerfMeterHdrpCustomPass();
			if (CustomPassVolume.RegisterUniqueGlobalCustomPass(CustomPassInjectionPoint.BeforePostProcess, customPass))
			{
				_registeredPass = customPass;
				Application.quitting += Unregister;
			}
		}

		private static void Unregister()
		{
			if (_registeredPass == null)
			{
				return;
			}

			CustomPassVolume.UnregisterGlobalCustomPass(_registeredPass);
			Application.quitting -= Unregister;
			_registeredPass = null;
		}
	}
}
