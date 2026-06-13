using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可设置目标 UI Document 中元素的可见性。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可设置目标 UI Document 中元素的可见性。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Visible")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitVisible : MMF_UIToolkitBoolBase
	{
		public enum Modes { Set, Toggle }
		
		[Header("Visible")]
		/// 所选模式：Set 直接设置对象是否可见；Toggle 在可见与不可见之间切换。
		[Tooltip("所选模式：Set 直接设置对象是否可见；Toggle 在可见与不可见之间切换。")]
		public Modes Mode = Modes.Set;
		/// 是否将对象设为可见（true）。
		[Tooltip("是否将对象设为可见（true）。")]
		[MMFEnumCondition("Mode", (int)Modes.Set)]
		public bool Visible = false;
		
		protected override void SetValue()
		{
			foreach (VisualElement element in _visualElements)
			{
				switch (Mode)
				{
					case Modes.Set:
						element.visible = Visible;
						break;
					case Modes.Toggle:
						element.visible = !element.visible;
						break;
				}
				HandleMarkDirty(element);
			}
		}
		
		protected override void SetValue(bool newValue)
		{
			foreach (VisualElement element in _visualElements)
			{
				element.visible = newValue;
				HandleMarkDirty(element);
			}
		}

		protected override bool GetInitialValue()
		{
			return _visualElements[0].visible;
		}
	}
}