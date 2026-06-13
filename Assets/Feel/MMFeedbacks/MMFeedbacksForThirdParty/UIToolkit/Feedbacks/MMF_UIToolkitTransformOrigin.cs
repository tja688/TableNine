using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的 transform origin。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的 transform origin。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Transform Origin")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitTransformOrigin : MMF_UIToolkitVector2Base
	{
		[Header("Units")]
		/// X 值的解释方式。
		[Tooltip("X 值的解释方式。")]
		public LengthUnit LengthUnitX = LengthUnit.Pixel;
		/// Y 值的解释方式。
		[Tooltip("Y 值的解释方式。")]
		public LengthUnit LengthUnitY = LengthUnit.Pixel;

		protected override void SetValue(Vector2 newValue)
		{
			foreach (VisualElement element in _visualElements)
			{
				element.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(new Length(newValue.x, LengthUnitX), new Length(newValue.y, LengthUnitY)));
				HandleMarkDirty(element);
			}
		}

		protected override Vector2 GetInitialValue()
		{
			return _visualElements[0].resolvedStyle.transformOrigin;
		}
	}
}