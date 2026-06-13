using System.Collections;
using System.Collections.Generic;
using UnityEngine;using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you turn the BlocksRaycast parameter of a target CanvasGroup on or off on play
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你在播放时开启或关闭目标 CanvasGroup 的 BlocksRaycast 参数。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("UI/CanvasGroup BlocksRaycasts")]
	public class MMF_CanvasGroupBlocksRaycasts : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetCanvasGroup == null); }
		public override string RequiredTargetText { get { return TargetCanvasGroup != null ? TargetCanvasGroup.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置a TargetCanvasGroup才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetCanvasGroup = FindAutomatedTarget<CanvasGroup>();
        
		[MMFInspectorGroup("Block Raycasts", true, 54, true)]
		/// the target canvas group we want to control the BlocksRaycasts parameter on 
		[Tooltip("要控制 块射线广播 参数的目标功率组")]
		public CanvasGroup TargetCanvasGroup;
		/// if this is true, on play, the target canvas group will block raycasts, if false it won't
		[Tooltip("若开启，播放时目标 CanvasGroup 将阻挡射线；关闭则不阻挡。")]
		public bool ShouldBlockRaycasts = true;

		protected bool _initialState;
        
		/// <summary>
		/// On play we turn raycast block on or off
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (TargetCanvasGroup == null)
			{
				return;
			}

			_initialState = TargetCanvasGroup.blocksRaycasts;
			TargetCanvasGroup.blocksRaycasts = ShouldBlockRaycasts;
		}
		
		/// <summary>
		/// On restore, we restore our initial state
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			TargetCanvasGroup.blocksRaycasts = _initialState;
		}
	}
}


