using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you control the volume of a target AudioSource over time.
	/// </summary>
	[AddComponentMenu("")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Audio/AudioSource Volume")]
	[FeedbackHelp("此反馈可随时间控制目标 AudioSource 的音量（通过对应 Shaker / Channel 生效）。")]
	public class MMF_AudioSourceVolume : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.SoundsColor; } }
		public override string RequiredTargetText => RequiredChannelText;
		#endif
		/// returns the duration of the feedback
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(Duration); } set { Duration = value; } }
		public override bool HasChannel => true;
		public override bool HasRandomness => true;

		[MMFInspectorGroup("AudioSource Volume", true, 87)]
		/// the duration of the shake, in seconds
		[Tooltip("抖动持续时间（秒）")]
		public float Duration = 2f;
		/// whether or not to reset shaker values after shake
		[Tooltip("抖动结束后是否重置 Shaker 内部状态。关闭时，下一次触发会从上次末状态继续。")]
		public bool ResetShakerValuesAfterShake = true;
		/// whether or not to reset the target's values after shake
		[Tooltip("抖动结束后是否把目标属性恢复到初始值。关闭时，目标会停留在最后一帧结果。")]
		public bool ResetTargetValuesAfterShake = true;
		/// whether or not to add to the initial value
		[Tooltip("是否以“相对模式”叠加到初始值。关闭时会按重映射后的绝对值驱动。")]
		public bool RelativeVolume = false;
		/// the curve used to animate the intensity value on
		[Tooltip("用于驱动强度动画的曲线")]
		public AnimationCurve VolumeTween = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(0.5f, 0f), new Keyframe(1, 1f));
		/// the value to remap the curve's 0 to
		[Range(-1f, 1f)]
		[Tooltip("曲线取值为 0 时映射到的目标值")]
		public float RemapVolumeZero = 0f;
		/// the value to remap the curve's 1 to
		[Range(-1f, 1f)]
		[Tooltip("曲线取值为 1 时映射到的目标值")]
		public float RemapVolumeOne = 1f;

		/// <summary>
		/// Triggers the corresponding coroutine
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			MMAudioSourceVolumeShakeEvent.Trigger(VolumeTween, FeedbackDuration, RemapVolumeZero, RemapVolumeOne, RelativeVolume, 
				intensityMultiplier, ChannelData, ResetShakerValuesAfterShake, ResetTargetValuesAfterShake, NormalPlayDirection, ComputedTimescaleMode);
		}
        
		/// <summary>
		/// On stop we stop our transition
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);
			MMAudioSourceVolumeShakeEvent.Trigger(VolumeTween, FeedbackDuration, RemapVolumeZero, RemapVolumeOne, stop:true);
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
			MMAudioSourceVolumeShakeEvent.Trigger(VolumeTween, FeedbackDuration, RemapVolumeZero, RemapVolumeOne, restore:true);
		}
	}
}
