using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的字体大小。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的字体大小。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Font Size")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitFontSize : MMF_UIToolkitFloatBase
	{
		protected override void SetValue(float newValue)
		{
			foreach (VisualElement element in _visualElements)
			{
				int newSize = Mathf.FloorToInt(newValue);
				element.style.fontSize = newSize; 
				HandleMarkDirty(element);
			}
		}

		protected override float GetInitialValue()
		{
			return _visualElements[0].resolvedStyle.fontSize;
		}
	}
}