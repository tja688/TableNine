using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可替换目标 UI Document 使用的样式表。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可替换目标 UI Document 使用的样式表。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Stylesheet")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitStylesheet : MMF_UIToolkit
	{
		[Header("Stylesheet")] 
		/// 要应用到该 Document 的新样式表。
		[Tooltip("要应用到该 Document 的新样式表。")]
		public StyleSheet NewStylesheet;
		
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			foreach (VisualElement element in _visualElements)
			{
				element.styleSheets.Add(NewStylesheet);
				HandleMarkDirty(element);
			}
		}
	}
}