using SGG.PerfMeter;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PerfMeterCameraSnapshotReplayExample : MonoBehaviour
{
	[SerializeField] private Camera targetCamera;

	private PerfMeterCameraSnapshot _snapshot;
	private bool _hasSnapshot;

	[ContextMenu("SGG PerfMeter/Capture Camera Snapshot")]
	public void CaptureCameraSnapshot()
	{
		if (targetCamera != null)
		{
			_snapshot = PerformanceMeter.GetCameraSnapshot(PerfMeterCameraSource.NameFilter, targetCamera.name);
		}
		else
		{
			_snapshot = PerformanceMeter.GetCameraSnapshot();
		}

		_hasSnapshot = _snapshot.IsAvailable;
		Debug.Log(_hasSnapshot
			? "SGG PerfMeter camera snapshot captured for " + _snapshot.CameraName + " in scene " + _snapshot.SceneName
			: "SGG PerfMeter camera snapshot unavailable: " + _snapshot.Warning);
	}

	[ContextMenu("SGG PerfMeter/Replay Camera Snapshot")]
	public void ReplayCameraSnapshot()
	{
		if (!_hasSnapshot)
		{
			Debug.LogWarning("SGG PerfMeter sample: capture a camera snapshot before replay.");
			return;
		}

		Camera camera = ResolveCamera();
		if (camera == null)
		{
			Debug.LogWarning("SGG PerfMeter sample: no target camera is available for replay.");
			return;
		}

		camera.transform.SetPositionAndRotation(_snapshot.Position, _snapshot.Rotation);
		camera.orthographic = _snapshot.Projection == PerfMeterCameraProjection.Orthographic;
		camera.fieldOfView = _snapshot.FieldOfView;
		camera.orthographicSize = _snapshot.OrthographicSize;
		camera.nearClipPlane = _snapshot.NearClipPlane;
		camera.farClipPlane = _snapshot.FarClipPlane;
		camera.aspect = _snapshot.Aspect > 0f ? _snapshot.Aspect : camera.aspect;
		camera.pixelRect = _snapshot.PixelRect;
		camera.targetDisplay = _snapshot.TargetDisplay;
		camera.depth = _snapshot.Depth;
		camera.clearFlags = _snapshot.ClearFlags;
		camera.cullingMask = _snapshot.CullingMask;
		camera.allowHDR = _snapshot.AllowHdr;
		camera.allowMSAA = _snapshot.AllowMsaa;
	}

	private Camera ResolveCamera()
	{
		if (targetCamera != null)
		{
			return targetCamera;
		}

		return Camera.main;
	}
}
