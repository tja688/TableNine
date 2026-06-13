using UnityEngine;	
using MoreMountains.Feedbacks;	
using UnityEngine.Scripting.APIUpdating;
#if MM_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace MoreMountains.FeedbacksForThirdParty	
{	
	/// <summary>	
	/// This feedback allows you to control channel mixer's red, green and blue over time.	
	/// It requires you have in your scene an object with a Volume 	
	/// with Channel Mixer active, and a MMChannelMixerShaker_HDRP component.	
	/// </summary>	
	[AddComponentMenu("")]	
	[System.Serializable]
	#if MM_HDRP
	[FeedbackPath("PostProcess/Channel Mixer HDRP")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.HDRP")]
	[FeedbackHelp("此反馈可让你随时间控制 Channel Mixer 的红、绿、蓝通道。" +
	              "它要求你的场景中存在一个带有 Volume 的对象，且该对象" +
	              "已启用 Channel Mixer，并挂有 MMChannelMixerShaker_HDRP 组件。")]
	public class MMF_ChannelMixer_HDRP : MMF_Feedback	
	{	
		/// a static bool used to disable all feedbacks of this type at once	
		public static bool FeedbackTypeAuthorized = true;	
		/// sets the inspector color for this feedback	
		#if UNITY_EDITOR	
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.PostProcessColor; } }	
		public override bool HasCustomInspectors => true;
		public override bool HasAutomaticShakerSetup => true;
		#endif	
		/// the duration of this feedback is the duration of the shake	
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(ShakeDuration); } set { ShakeDuration = value;  } }	
		public override bool HasChannel => true;
		public override bool HasRandomness => true;
        
		[MMFInspectorGroup("Color Grading", true, 10)]
		/// the duration of the shake, in seconds
		[Tooltip("抖动持续时间，单位为秒")]
		public float ShakeDuration = 1f;
		/// whether or not to add to the initial intensity
		[Tooltip("是否在初始强度基础上叠加")]
		public bool RelativeIntensity = true;
		/// whether or not to reset shaker values after shake
		[Tooltip("抖动结束后是否重置抖动器的数值")]
		public bool ResetShakerValuesAfterShake = true;
		/// whether or not to reset the target's values after shake
		[Tooltip("抖动结束后是否重置目标对象的数值")]
		public bool ResetTargetValuesAfterShake = true;

		[MMFInspectorGroup("Red", true, 13)]
		/// the curve used to animate the red value on
		[Tooltip("用于驱动红色通道数值变化的曲线")]
		public AnimationCurve ShakeRed = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[Range(-200f, 200f)]
		public float RemapRedZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[Range(-200f, 200f)]
		public float RemapRedOne = 200f;

		[MMFInspectorGroup("Green", true, 12)]
		/// the curve used to animate the green value on
		[Tooltip("用于驱动绿色通道数值变化的曲线")]
		public AnimationCurve ShakeGreen = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[Range(-200f, 200f)]
		public float RemapGreenZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[Range(-200f, 200f)]
		public float RemapGreenOne = 200f;

		[MMFInspectorGroup("Blue", true, 11)]
		/// the curve used to animate the blue value on
		[Tooltip("用于驱动蓝色通道数值变化的曲线")]
		public AnimationCurve ShakeBlue = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[Range(-200f, 200f)]
		public float RemapBlueZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[Range(-200f, 200f)]
		public float RemapBlueOne = 200f;

		/// <summary>
		/// Triggers a color adjustments shake
		/// </summary>
		/// <param name="position"></param>
		/// <param name="attenuation"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			MMChannelMixerShakeEvent_HDRP.Trigger(ShakeRed, RemapRedZero, RemapRedOne,
				ShakeGreen, RemapGreenZero, RemapGreenOne,
				ShakeBlue, RemapBlueZero, RemapBlueOne,
				FeedbackDuration,
				RelativeIntensity, intensityMultiplier, ChannelData, ResetShakerValuesAfterShake, ResetTargetValuesAfterShake, NormalPlayDirection, ComputedTimescaleMode);
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
            
			MMChannelMixerShakeEvent_HDRP.Trigger(ShakeRed, RemapRedZero, RemapRedOne,
				ShakeGreen, RemapGreenZero, RemapGreenOne,
				ShakeBlue, RemapBlueZero, RemapBlueOne,
				FeedbackDuration,
				RelativeIntensity, channelData:ChannelData, stop:true);
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
            
			MMChannelMixerShakeEvent_HDRP.Trigger(ShakeRed, RemapRedZero, RemapRedOne,
				ShakeGreen, RemapGreenZero, RemapGreenOne,
				ShakeBlue, RemapBlueZero, RemapBlueOne,
				FeedbackDuration,
				RelativeIntensity, channelData:ChannelData, restore:true);
		}
		
		/// <summary>
		/// Automaticall sets up the post processing profile and shaker
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			#if MM_HDRP && UNITY_EDITOR
			MMHDRPHelpers.GetOrCreateVolume<ChannelMixer, MMChannelMixerShaker_HDRP>(Owner, "Channel Mixer");
			#endif
		}
	}	
}