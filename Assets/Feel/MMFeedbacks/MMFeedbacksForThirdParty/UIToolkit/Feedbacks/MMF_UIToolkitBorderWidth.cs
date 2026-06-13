using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的边框宽度。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的边框宽度。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Border Width")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitBorderWidth : MMF_UIToolkitFloatBase
	{
		/// 是否修改左边框宽度。
		[Tooltip("是否修改左边框宽度。")]
		public bool Left = true;
		/// 是否修改右边框宽度。
		[Tooltip("是否修改右边框宽度。")]
		public bool Right = true;
		/// 是否修改上边框宽度。
		[Tooltip("是否修改上边框宽度。")]
		public bool Top = true;
		/// 是否修改下边框宽度。
		[Tooltip("是否修改下边框宽度。")]
		public bool Bottom = true;
		
		protected override void SetValue(float newValue)
		{
			foreach (VisualElement element in _visualElements)
			{
				if (Left) element.style.borderLeftWidth = newValue;
				if (Right) element.style.borderRightWidth = newValue;
				if (Bottom) element.style.borderBottomWidth = newValue;
				if (Top) element.style.borderTopWidth = newValue;
				HandleMarkDirty(element);
			}
		}

		protected override float GetInitialValue()
		{
			if (Left) return _visualElements[0].resolvedStyle.borderLeftWidth;
			if (Right) return _visualElements[0].resolvedStyle.borderRightWidth;
			if (Bottom) return _visualElements[0].resolvedStyle.borderBottomWidth;
			if (Top) return _visualElements[0].resolvedStyle.borderTopWidth;
			return _visualElements[0].resolvedStyle.borderLeftWidth;
		}
	}
}