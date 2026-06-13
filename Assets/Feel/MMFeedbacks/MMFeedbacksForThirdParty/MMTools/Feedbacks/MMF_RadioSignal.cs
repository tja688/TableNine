using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you trigger a play on a target MMRadioSignal (usually used by a MMRadioBroadcaster to emit a value that can then be listened to by MMRadioReceivers. From this feedback you can also specify a duration, timescale and multiplier.
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈会在目标 MMRadioSignal 上触发播放。常用于由 MMRadioBroadcaster 发射信号，再由 MMRadioReceiver 监听。你也可以在此设置持续时间、时间缩放模式与全局倍率。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[System.Serializable]
	[FeedbackPath("GameObject/MMRadioSignal")]
	public class MMF_RadioSignal : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
        
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.GameObjectColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetSignal == null); }
		public override string RequiredTargetText { get { return TargetSignal != null ? TargetSignal.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先指定 TargetSignal 才能正常工作。你可以在下方进行设置。"; } }
		#endif
        
		/// the duration of this feedback is 0
		public override float FeedbackDuration { get { return 0f; } }
		public override bool HasRandomness => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetSignal = FindAutomatedTarget<MMRadioSignal>();

		[MMFInspectorGroup("Radio Signal", true, 72)]
		/// The target MMRadioSignal to trigger
		[Tooltip("要触发的目标MM无线电信号。")]
		public MMRadioSignal TargetSignal;
		/// the timescale to operate on
		[Tooltip("该信号采用的时间缩放模式（例如受时间缩放或不受时间缩放影响）。")]
		public MMRadioSignal.TimeScales TimeScale = MMRadioSignal.TimeScales.Unscaled;
		/// the duration of the shake, in seconds
		[Tooltip("信号持续时间，单位为秒。")]
		public float Duration = 1f;
		/// a global multiplier to apply to the end result of the combination
		[Tooltip("应用到最终信号结果的全局倍率。")]
		public float GlobalMultiplier = 1f;
        

		/// <summary>
		/// On Play we set the values on our target signal and make it start shaking its level
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (Active && FeedbackTypeAuthorized)
			{
				if (TargetSignal != null)
				{
					float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
                    
					TargetSignal.Duration = Duration;
					TargetSignal.GlobalMultiplier = GlobalMultiplier * intensityMultiplier;
					TargetSignal.TimeScale = TimeScale;
					TargetSignal.StartShaking();
				}
			}
		}

		/// <summary>
		/// On Stop, stops the target signal
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			base.CustomStopFeedback(position, feedbacksIntensity);
			if (Active)
			{
				if (TargetSignal != null)
				{
					TargetSignal.Stop();
				}
			}
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
			
			if (TargetSignal != null)
			{
				TargetSignal.Stop();
				TargetSignal.ApplyLevel(0f);
			}
		}
	}
}
