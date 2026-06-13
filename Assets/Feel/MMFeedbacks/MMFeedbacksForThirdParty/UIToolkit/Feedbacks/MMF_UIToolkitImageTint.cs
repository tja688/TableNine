using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的图片染色。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的图片染色。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Image Tint")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitImageTint : MMF_UIToolkitColorBase
	{
		protected override void ApplyColor(Color newColor)
		{
			foreach (VisualElement element in _visualElements)
			{
				element.style.unityBackgroundImageTintColor = newColor;
				HandleMarkDirty(element);
			}
		}

		protected override Color GetInitialColor()
		{
			return _visualElements[0].resolvedStyle.unityBackgroundImageTintColor;
		}
	}
}