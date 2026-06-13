using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的尺寸。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的尺寸。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Size")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitSize : MMF_UIToolkitVector2Base
	{
		protected override void SetValue(Vector2 newValue)
		{
			foreach (VisualElement element in _visualElements)
			{
				element.style.width = newValue.x;
				element.style.height = newValue.y;
				HandleMarkDirty(element);
			}
		}

		protected override Vector2 GetInitialValue()
		{
			return new Vector2(_visualElements[0].resolvedStyle.width, _visualElements[0].resolvedStyle.height);
		}
	}
}