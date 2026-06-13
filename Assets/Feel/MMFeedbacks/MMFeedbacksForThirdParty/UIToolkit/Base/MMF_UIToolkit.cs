using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这是 UI Toolkit 系列反馈的基础类。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("这是 UI Toolkit 系列反馈的基础类。")]
	public class MMF_UIToolkit : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetDocument == null); }
		public override string RequiredTargetText { get { return TargetDocument != null ? TargetDocument.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈需要指定目标 UI Document。请在下方的 TargetDocument 字段中设置。"; } }
		#endif

		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetDocument = FindAutomatedTarget<UIDocument>();
		public enum QueryModes { Name, Class }

		[MMFInspectorGroup("Target", true, 54, true)]
		/// 要修改的 UI Document。 
		[Tooltip("要修改的用户界面文档。")]
		public UIDocument TargetDocument;
		/// 查询元素的方式：按元素名称，或按 class。 
		[Tooltip("查询元素的方式：按元素名称，或按 class。")]
		public QueryModes QueryMode = QueryModes.Name;
		/// 要执行的查询内容（替换成你自己的元素名称或 class 名称）。
		[Tooltip("要执行的查询内容（替换成你自己的元素名称或 class 名称）。")]
		public string Query = "ButtonA";
		/// 操作完成后是否将 UI Document 标记为 dirty。若你的修改需要强制重绘，例如使用 generateVisualContent 渲染网格且网格数据已发生变化，请启用此项。
		[Tooltip("操作完成后是否将 UI Document 标记为 dirty。若你的修改需要强制重绘，例如使用 generateVisualContent 渲染网格且网格数据已发生变化，请启用此项。")]
		public bool MarkDirty = false;
		
		protected List<VisualElement> _visualElements = new List<VisualElement>();

		/// <summary>
		/// On init we turn the Image off if needed
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			PerformQuery();
		}

		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
		}

		/// <summary>
		/// Performs the query and sets _visualElements with the result
		/// </summary>
		protected virtual void PerformQuery()
		{
			if (TargetDocument == null)
			{
				Debug.LogWarning("[UI Toolkit] The UI Toolkit feedback on "+Owner.name+" doesn't have a TargetDocument, it won't work. You need to specify one in its inspector.");
				return;
			}
			switch (QueryMode)
			{
				case QueryModes.Name:
					_visualElements = TargetDocument.rootVisualElement.Query(Query).ToList();
					break;
				case QueryModes.Class:
					_visualElements = TargetDocument.rootVisualElement.Query(className: Query).ToList();
					break;
			}
		}
		
		protected virtual void HandleMarkDirty(VisualElement element)
		{
			if (MarkDirty)
			{
				element.MarkDirtyRepaint();
			}
		}

	}
}
