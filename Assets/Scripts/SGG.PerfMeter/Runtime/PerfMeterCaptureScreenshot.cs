using System;
using System.Collections;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal static class PerfMeterCaptureScreenshot
	{
		internal static IEnumerator Capture(Action<byte[], string, bool> completed)
		{
			if (Application.isBatchMode || !Application.isPlaying)
			{
				completed?.Invoke(null, "Runtime screenshot is unavailable outside non-batch Play Mode.", true);
				yield break;
			}

			yield return new WaitForEndOfFrame();
			Texture2D texture = null;
			try
			{
				texture = ScreenCapture.CaptureScreenshotAsTexture();
				if (texture == null)
				{
					completed?.Invoke(null, "ScreenCapture returned no texture.", false);
					yield break;
				}

				byte[] bytes = ImageConversion.EncodeToPNG(texture);
				completed?.Invoke(bytes, string.Empty, false);
			}
			catch (Exception exception)
			{
				completed?.Invoke(null, exception.GetType().Name + ": " + exception.Message, false);
			}
			finally
			{
				if (texture != null)
				{
					UnityEngine.Object.Destroy(texture);
				}
			}
		}
	}
}
