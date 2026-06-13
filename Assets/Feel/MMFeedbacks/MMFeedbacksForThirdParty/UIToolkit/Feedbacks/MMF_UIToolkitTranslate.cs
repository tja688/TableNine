using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可平移目标 UI Document 中的元素。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可平移目标 UI Document 中的元素。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Translate")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitTranslate : MMF_UIToolkitVector2Base
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
				element.style.translate = new StyleTranslate(new Translate(new Length(newValue.x, LengthUnitX), new Length(newValue.y, LengthUnitY)));
				HandleMarkDirty(element);
			}
		}

		protected override Vector2 GetInitialValue()
		{
			return _visualElements[0].resolvedStyle.translate;
		}
	}
}