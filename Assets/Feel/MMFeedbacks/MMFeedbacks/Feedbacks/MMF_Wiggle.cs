using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// When played, this feedback will activate the Wiggle method of a MMWiggle object based on the selected settings, wiggling either its position, rotation, scale, or all of these.
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让挂有 MMWiggle 组件的对象，在指定持续时间内触发位置、旋转和/或缩放摆动。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Transform/Wiggle")]
	public class MMF_Wiggle : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TransformColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetWiggle == null); }
		public override string RequiredTargetText { get { return TargetWiggle != null ? TargetWiggle.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置a TargetWiggle才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetWiggle = FindAutomatedTarget<MMWiggle>();

		[MMFInspectorGroup("Target", true, 54, true)]
		/// 要控制的 Wiggle 组件
		[Tooltip("要控制 摆动 组件")]
		public MMWiggle TargetWiggle;
        
		[MMFInspectorGroup("Position", true, 55)]
		/// whether or not to wiggle position
		[Tooltip("是否摆动位置")]
		public bool WigglePosition = true;
		/// the duration (in seconds) of the position wiggle
		[Tooltip("位置 摆动 的持续时间（秒）")]
		public float WigglePositionDuration;

		[MMFInspectorGroup("Rotation", true, 26)]
		/// whether or not to wiggle rotation
		[Tooltip("是否摆动旋转")]
		public bool WiggleRotation;
		/// the duration (in seconds) of the rotation wiggle
		[Tooltip("旋转 摆动 的持续时间（秒）")]
		public float WiggleRotationDuration;

		[MMFInspectorGroup("Scale", true, 57)]
		/// whether or not to wiggle scale
		[Tooltip("是否有摆动秤")]
		public bool WiggleScale;
		/// the duration (in seconds) of the scale wiggle
		[Tooltip("缩放 摆动 的持续时间（秒）")]
		public float WiggleScaleDuration;


		/// the duration of this feedback is the duration of the clip being played
		public override float FeedbackDuration
		{
			get { return Mathf.Max(ApplyTimeMultiplier(WigglePositionDuration), ApplyTimeMultiplier(WiggleRotationDuration), ApplyTimeMultiplier(WiggleScaleDuration)); }
			set { WigglePositionDuration = value;
				WiggleRotationDuration = value;
				WiggleScaleDuration = value;
			} 
		}

		/// <summary>
		/// On Play we trigger the desired wiggles
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetWiggle == null))
			{
				return;
			}
            
			TargetWiggle.enabled = true;
			if (WigglePosition)
			{
				TargetWiggle.PositionWiggleProperties.UseUnscaledTime = !InScaledTimescaleMode;
				TargetWiggle.WigglePosition(ApplyTimeMultiplier(WigglePositionDuration));
			}
			if (WiggleRotation)
			{
				TargetWiggle.RotationWiggleProperties.UseUnscaledTime = !InScaledTimescaleMode;
				TargetWiggle.WiggleRotation(ApplyTimeMultiplier(WiggleRotationDuration));
			}
			if (WiggleScale)
			{
				TargetWiggle.ScaleWiggleProperties.UseUnscaledTime = !InScaledTimescaleMode;
				TargetWiggle.WiggleScale(ApplyTimeMultiplier(WiggleScaleDuration));
			}
		}

		/// <summary>
		/// On Stop we change the state of our object if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetWiggle == null))
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);

			TargetWiggle.enabled = false;
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

			TargetWiggle.RestoreInitialValues();
		}
	}
}

