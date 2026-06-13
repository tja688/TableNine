using UnityEngine;
#if MM_UGUI2
using TMPro;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// 这个反馈可修改目标 TMP 文本组件的内容。
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("这个反馈可修改目标 TMP 文本组件的内容。")]
	#if MM_UGUI2
	[FeedbackPath("TextMesh Pro/TMP Text")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.TextMeshPro")]
	public class MMF_TMPText : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TMPColor; } }
		public override string RequiresSetupText { get { return "此反馈需要指定 TargetTMPText 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		#if UNITY_EDITOR && MM_UGUI2
		public override bool EvaluateRequiresSetup() { return (TargetTMPText == null); }
		public override string RequiredTargetText { get { return TargetTMPText != null ? TargetTMPText.name : "";  } }
		#endif
        
		#if MM_UGUI2
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetTMPText = FindAutomatedTarget<TMP_Text>();

		[MMFInspectorGroup("TextMeshPro Change Text", true, 12, true)]
		/// 要修改文本内容的目标 TMP_Text 组件。
		[Tooltip("要修改文本内容的目标 TMP_Text 组件。")]
		public TMP_Text TargetTMPText;
		/// 用于替换旧文本的新内容。
		[Tooltip("用于替换旧文本的新内容。")]
		[TextArea]
		public string NewText = "Hello World";
		#endif

		protected string _initialText;
        
		/// <summary>
		/// On play we change the text of our target TMPText
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			#if MM_UGUI2
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			if (TargetTMPText == null)
			{
				return;
			}

			_initialText = TargetTMPText.text;
			TargetTMPText.text = NewText;
			#endif
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
			#if MM_UGUI2
			TargetTMPText.text = _initialText;
			#endif
		}
	}
}
