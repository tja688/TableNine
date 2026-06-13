using UnityEngine;
#if MM_UI
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you control the RaycastTarget parameter of a target image, turning it on or off on play
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你在播放时开启或关闭目标 Image 的 RaycastTarget 参数。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("UI/Image RaycastTarget")]
	public class MMF_ImageRaycastTarget : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetImage == null); }
		public override string RequiredTargetText { get { return TargetImage != null ? TargetImage.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈需要先指定 TargetImage 才能正常工作，可在下方设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetImage = FindAutomatedTarget<Image>();
        
		[MMFInspectorGroup("Image", true, 12, true)]
		/// the target Image we want to control the RaycastTarget parameter on
		[Tooltip("要控制 光线投射目标 参数的目标图像。")]
		public Image TargetImage;
		/// if this is true, when played, the target image will become a raycast target
		[Tooltip("播放时目标是否应成为 RaycastTarget。反向播放会自动取反该结果。")]
		public bool ShouldBeRaycastTarget = true;

		protected bool _initialState;
		
		/// <summary>
		/// On play we turn raycastTarget on or off
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (TargetImage == null)
			{
				return;
			}

			_initialState = TargetImage.raycastTarget;
			TargetImage.raycastTarget = NormalPlayDirection ? ShouldBeRaycastTarget : !ShouldBeRaycastTarget;
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
			TargetImage.raycastTarget = _initialState;
		}
	}
}
#endif
