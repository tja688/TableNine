using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的文本内容。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的文本内容。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Text")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitText : MMF_UIToolkit
	{
		[Header("Text")]
		/// 要设置到目标对象上的新文本。
		[Tooltip("要设置到目标对象上的新文本。")]
		public string NewText = "";

		protected string _initialText;
		
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			SetValue(NewText);
		}

		protected virtual void SetValue(string newValue)
		{
			foreach (VisualElement element in _visualElements)
			{
				(element as TextElement).text = newValue;
				HandleMarkDirty(element);
			}
		}
		
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			if ((_visualElements == null) || (_visualElements.Count == 0))
			{
				return;
			}
			_initialText = GetInitialValue();
		}

		protected virtual string GetInitialValue()
		{
			return (_visualElements[0] as TextElement).text;
		}
		
		/// <summary>
		/// On restore, we put our object back at its initial position
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			SetValue(_initialText);
		}
	}
}