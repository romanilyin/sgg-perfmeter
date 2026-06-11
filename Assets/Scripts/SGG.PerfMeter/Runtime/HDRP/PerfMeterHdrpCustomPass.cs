using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SGG.PerfMeter
{
	public sealed class PerfMeterHdrpCustomPass : CustomPass
	{
		public const string DefaultMarkerName = "SGG.PerfMeter.HDRP.CustomPass";

		public PerfMeterHdrpCustomPass()
		{
			name = DefaultMarkerName;
			targetColorBuffer = TargetBuffer.Camera;
			targetDepthBuffer = TargetBuffer.None;
			clearFlags = ClearFlag.None;
		}

		protected override bool executeInSceneView => false;

		protected override void Execute(CustomPassContext ctx)
		{
			Camera camera = ctx.hdCamera != null ? ctx.hdCamera.camera : null;
			if (camera == null || camera.cameraType != CameraType.Game)
			{
				return;
			}

			PerfMeterRenderGraphAnalytics.RecordHdrpCustomPassSnapshot(
				camera.name,
				camera.cameraType.ToString(),
				CustomPassInjectionPoint.BeforePostProcess.ToString());
		}
	}
}
