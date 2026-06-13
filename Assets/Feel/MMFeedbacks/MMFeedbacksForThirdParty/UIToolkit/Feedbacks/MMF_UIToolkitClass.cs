using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可修改目标 UI Document 中元素的 class 列表。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这个反馈可修改目标 UI Document 中元素的 class 列表。")]
	[System.Serializable]
	[FeedbackPath("UI Toolkit/UITK Class")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.UIToolkit")]
	public class MMF_UIToolkitClass : MMF_UIToolkit
	{
		public enum Modes { AddToClassList, EnableInClassList, ToggleInClassList, RemoveFromClassList, ClearClassList}

		[Header("Class Manipulation")] 
		/// 对 class 列表执行的操作：添加、启用、切换、移除或清空。
		[Tooltip("对 class 列表执行的操作：添加、启用、切换、移除或清空。")]
		public Modes Mode = Modes.AddToClassList;
		/// 要添加、启用、切换或移除的 class 名称。
		[Tooltip("要添加、启用、切换或移除的 class 名称。")]
		[MMFEnumCondition("Mode", (int)Modes.AddToClassList, (int)Modes.EnableInClassList, (int)Modes.ToggleInClassList, (int)Modes.RemoveFromClassList)]
		public string ClassName = "";
		/// 在 EnableInClassList 模式下，决定该 class 是启用还是禁用。
		[Tooltip("在 EnableInClassList 模式下，决定该 class 是启用还是禁用。")]
		[MMFEnumCondition("Mode", (int)Modes.EnableInClassList)]
		public bool Enable = true;
		
		
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			foreach (VisualElement element in _visualElements)
			{
				switch (Mode)
				{
					case Modes.AddToClassList:
						element.AddToClassList(ClassName);
						break;
					case Modes.EnableInClassList:
						element.EnableInClassList(ClassName, Enable);
						break;
					case Modes.ToggleInClassList:
						element.ToggleInClassList(ClassName);
						break;
					case Modes.RemoveFromClassList:
						element.RemoveFromClassList(ClassName);
						break;
					case Modes.ClearClassList:
						element.ClearClassList();
						break;
				}
				HandleMarkDirty(element);
			}
		}
	}
}