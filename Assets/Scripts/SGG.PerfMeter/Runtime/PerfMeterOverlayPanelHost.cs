using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterOverlayPanelHost : IDisposable
	{
		internal const string HostObjectName = "SGG PerfMeter UI Host";

		private GameObject _hostObject;
		private VisualElement _root;
		private Action<VisualElement> _rootChanged;

#if UNITY_6000_5_OR_NEWER
		private PanelRenderer _panelRenderer;
		private PanelRenderer.VersionedUIReloadCallback _reloadCallback;
		private int _rootVersion = -1;
#else
		private UIDocument _document;
#endif

		internal GameObject HostObject => _hostObject;
		internal VisualElement Root => _root;

		internal bool Attach(GameObject owner, PanelSettings panelSettings, Action<VisualElement> rootChanged)
		{
			if (owner == null || panelSettings == null)
			{
				return false;
			}

			_rootChanged = rootChanged;
			if (_hostObject != null)
			{
				return true;
			}

			_hostObject = new GameObject(HostObjectName)
			{
				hideFlags = HideFlags.DontSave
			};
			_hostObject.transform.SetParent(owner.transform, false);

#if UNITY_6000_5_OR_NEWER
			_panelRenderer = _hostObject.AddComponent<PanelRenderer>();
			_reloadCallback = OnUiReload;
			_panelRenderer.RegisterUIReloadCallback(_reloadCallback);
			_panelRenderer.panelSettings = panelSettings;
#else
			_document = _hostObject.AddComponent<UIDocument>();
			_document.panelSettings = panelSettings;
			SetRoot(_document.rootVisualElement);
#endif
			return true;
		}

		internal void MarkDirtyRepaint()
		{
			_root?.MarkDirtyRepaint();
		}

		public void Dispose()
		{
#if UNITY_6000_5_OR_NEWER
			if (_panelRenderer != null && _reloadCallback != null)
			{
				_panelRenderer.UnregisterUIReloadCallback(_reloadCallback);
			}

			_reloadCallback = null;
			_panelRenderer = null;
			_rootVersion = -1;
#else
			_document = null;
#endif
			_root = null;
			_rootChanged = null;
			if (_hostObject != null)
			{
				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(_hostObject);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(_hostObject);
				}
			}

			_hostObject = null;
		}

#if UNITY_6000_5_OR_NEWER
		private void OnUiReload(PanelRenderer panelRenderer, VisualElement root, int version)
		{
			if (panelRenderer != _panelRenderer || root == null || ReferenceEquals(root, _root) && version == _rootVersion)
			{
				return;
			}

			_rootVersion = version;
			SetRoot(root);
		}
#endif

		private void SetRoot(VisualElement root)
		{
			_root = root;
			_rootChanged?.Invoke(root);
		}
	}
}
