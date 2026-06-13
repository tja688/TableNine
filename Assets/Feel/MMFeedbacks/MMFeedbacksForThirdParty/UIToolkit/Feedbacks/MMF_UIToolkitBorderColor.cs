using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的边框颜色。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的边框颜色。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Border Color")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitBorderColor : MMF_UIToolkitColorBase
	{
		[MMFInspectorGroup("Borders", true, 55, true)]
		/// 此反馈是否修改左边框颜色。
		[Tooltip("此反馈是否修改左边框颜色。")]
		public bool BorderLeft = true;
		/// 此反馈是否修改右边框颜色。
		[Tooltip("此反馈是否修改右边框颜色。")]
		public bool BorderRight = true;
		/// 此反馈是否修改下边框颜色。
		[Tooltip("此反馈是否修改下边框颜色。")]
		public bool BorderBottom = true;
		/// 此反馈是否修改上边框颜色。
		[Tooltip("此反馈是否修改上边框颜色。")]
		public bool BorderTop = true;
		
		protected override void ApplyColor(Color newColor)
		{
			foreach (VisualElement element in _visualElements)
			{
				if (BorderLeft)
				{
					element.style.borderLeftColor = newColor;
				}
				if (BorderRight)
				{
					element.style.borderRightColor = newColor;
				}
				if (BorderBottom)
				{
					element.style.borderBottomColor = newColor;
				}
				if (BorderTop)
				{
					element.style.borderTopColor = newColor;
				}
				HandleMarkDirty(element);
			}
		}

		protected override Color GetInitialColor()
		{
			if (BorderLeft)
			{
				return _visualElements[0].resolvedStyle.borderLeftColor;
			}
			if (BorderRight)
			{
				return _visualElements[0].resolvedStyle.borderRightColor;
			}
			if (BorderBottom)
			{
				return _visualElements[0].resolvedStyle.borderBottomColor;
			}
			if (BorderTop)
			{
				return _visualElements[0].resolvedStyle.borderTopColor;
			}
			return Color.black;
		}
	}
}