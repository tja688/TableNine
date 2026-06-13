using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feel
{
	[AddComponentMenu("")]
	public class FeelSpringsDemoManager : MonoBehaviour
	{
		[Header("Bindings")]
		public List<GameObject> DemoObjects;
		[MMReadOnly] public int CurrentIndex = 0;

		protected bool _canvasesConfigured;

		protected virtual void Start()
		{
			ConfigureCanvasesForCurrentPipeline();
			EnableCurrentDemo();
		}

		public virtual void NextDemo()
		{
			CurrentIndex++;
			if (CurrentIndex >= DemoObjects.Count)
			{
				CurrentIndex = 0;
			}
			EnableCurrentDemo();
		}

		public virtual void PreviousDemo()
		{
			CurrentIndex--;
			if (CurrentIndex < 0)
			{
				CurrentIndex = DemoObjects.Count - 1;
			}
			EnableCurrentDemo();
		}

		protected virtual void EnableCurrentDemo()
		{
			if ((DemoObjects == null) || (DemoObjects.Count == 0))
			{
				return;
			}

			CurrentIndex = Mathf.Clamp(CurrentIndex, 0, DemoObjects.Count - 1);

			foreach (GameObject demoObject in DemoObjects)
			{
				if (demoObject != null)
				{
					demoObject.SetActive(false);
				}
			}

			if (DemoObjects[CurrentIndex] != null)
			{
				DemoObjects[CurrentIndex].SetActive(true);
			}
		}

		protected virtual void ConfigureCanvasesForCurrentPipeline()
		{
			if (_canvasesConfigured)
			{
				return;
			}

			_canvasesConfigured = true;
			ConfigureCanvasesIn(gameObject);

			if (DemoObjects == null)
			{
				return;
			}

			foreach (GameObject demoObject in DemoObjects)
			{
				ConfigureCanvasesIn(demoObject);
			}
		}

		protected virtual void ConfigureCanvasesIn(GameObject root)
		{
			if (root == null)
			{
				return;
			}

			Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
			foreach (Canvas canvas in canvases)
			{
				if ((canvas != null) && (canvas.renderMode == RenderMode.ScreenSpaceCamera))
				{
					canvas.renderMode = RenderMode.ScreenSpaceOverlay;
					canvas.worldCamera = null;
				}
			}
		}
	}
}
