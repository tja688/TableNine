using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的边框圆角半径。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的边框圆角半径。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Border Radius")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitBorderRadius : MMF_UIToolkitFloatBase
	{
		/// 是否修改左下角边框圆角半径。
		[Tooltip("是否修改左下角边框圆角半径。")]
		public bool BottomLeft = true;
		/// 是否修改右下角边框圆角半径。
		[Tooltip("是否修改右下角边框圆角半径。")]
		public bool BottomRight = true;
		/// 是否修改左上角边框圆角半径。
		[Tooltip("是否修改左上角边框圆角半径。")]
		public bool TopLeft = true;
		/// 是否修改右上角边框圆角半径。
		[Tooltip("是否修改右上角边框圆角半径。")]
		public bool TopRight = true;
		
		protected override void SetValue(float newValue)
		{
			foreach (VisualElement element in _visualElements)
			{
				if (BottomLeft) element.style.borderBottomLeftRadius = newValue;
				if (BottomRight) element.style.borderBottomRightRadius = newValue;
				if (TopLeft) element.style.borderTopLeftRadius = newValue;
				if (TopRight) element.style.borderTopRightRadius = newValue;
				HandleMarkDirty(element);
			}
		}

		protected override float GetInitialValue()
		{
			if (BottomLeft) return _visualElements[0].resolvedStyle.borderBottomLeftRadius;
			if (BottomRight) return _visualElements[0].resolvedStyle.borderBottomRightRadius;
			if (TopLeft) return _visualElements[0].resolvedStyle.borderTopLeftRadius;
			if (TopRight) return _visualElements[0].resolvedStyle.borderTopRightRadius;
			return _visualElements[0].resolvedStyle.borderBottomLeftRadius;
		}
	}
}