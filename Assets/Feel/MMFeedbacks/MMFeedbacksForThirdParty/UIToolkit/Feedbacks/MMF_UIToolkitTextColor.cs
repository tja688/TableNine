using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的文本颜色。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的文本颜色。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Text Color")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitTextColor : MMF_UIToolkitColorBase
	{
		protected override void ApplyColor(Color newColor)
		{
			foreach (VisualElement element in _visualElements)
			{
				element.style.color = newColor;
				HandleMarkDirty(element);
			}
		}

		protected override Color GetInitialColor()
		{
			return _visualElements[0].resolvedStyle.color;
		}
	}
}