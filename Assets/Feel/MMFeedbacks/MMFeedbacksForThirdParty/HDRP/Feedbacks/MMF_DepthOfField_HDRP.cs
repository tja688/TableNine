using UnityEngine;
using MoreMountains.Feedbacks;
using UnityEngine.Scripting.APIUpdating;
#if MM_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// This feedback allows you to control HDRP Depth of Field focus distance or near/far ranges over time.
	/// It requires you have in your scene an object with a Volume 
	/// with Depth of Field active, and a MMDepthOfFieldShaker_HDRP component.
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	#if MM_HDRP
	[FeedbackPath("PostProcess/Depth of Field HDRP")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.HDRP")]
	[FeedbackHelp("此反馈可让你随时间控制 HDRP 景深的焦点距离或近景/远景范围。" +
	              "它要求你的场景中存在一个带有 Volume 的对象，且该对象" +
	              "已启用 Depth of Field，并挂有 MMDepthOfFieldShaker_HDRP 组件。")]
	public class MMF_DepthOfField_HDRP : MMF_Feedback
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
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(Duration); } set { Duration = value; } }
		public override bool HasChannel => true;
		public override bool HasRandomness => true;

		[MMFInspectorGroup("Depth of Field", true, 28)]
		/// the duration of the shake, in seconds
		[Tooltip("抖动持续时间，单位为秒")]
		public float Duration = 0.2f;
		/// whether or not to reset shaker values after shake
		[Tooltip("抖动结束后是否重置抖动器的数值")]
		public bool ResetShakerValuesAfterShake = true;
		/// whether or not to reset the target's values after shake
		[Tooltip("抖动结束后是否重置目标对象的数值")]
		public bool ResetTargetValuesAfterShake = true;
		
		[MMFInspectorGroup("Focus Distance", true, 53)]
		/// whether or not to animate the focus distance
		[Tooltip("是否驱动焦点距离变化")]
		public bool AnimateFocusDistance = true;
		/// the curve used to animate the focus distance value on
		[Tooltip("用于驱动焦点距离值变化的曲线")]
		[MMFCondition("AnimateFocusDistance", true)]
		public AnimationCurve ShakeFocusDistance = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[MMFCondition("AnimateFocusDistance", true)]
		public float RemapFocusDistanceZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[MMFCondition("AnimateFocusDistance", true)]
		public float RemapFocusDistanceOne = 3f;
		
		
		[MMFInspectorGroup("Near Range", true, 52)]
		
		[Header("Near Range Start")]
		/// whether or not to animate the near range start
		[Tooltip("是否驱动近景范围起点变化")]
		public bool AnimateNearRangeStart = false;
		/// the curve used to animate the near range start on
		[Tooltip("用于驱动近景范围起点变化的曲线")]
		[MMFCondition("AnimateNearRangeStart", true)]
		public AnimationCurve ShakeNearRangeStart = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[MMFCondition("AnimateNearRangeStart", true)]
		public float RemapNearRangeStartZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[MMFCondition("AnimateNearRangeStart", true)]
		public float RemapNearRangeStartOne = 3f;
		
		[Header("Near Range End")]
		/// whether or not to animate the near range end
		[Tooltip("是否驱动近景范围终点变化")]
		public bool AnimateNearRangeEnd = false;
		/// the curve used to animate the near range end on
		[Tooltip("用于驱动近景范围终点变化的曲线")]
		[MMFCondition("AnimateNearRangeEnd", true)]
		public AnimationCurve ShakeNearRangeEnd = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[MMFCondition("AnimateNearRangeEnd", true)]
		public float RemapNearRangeEndZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[MMFCondition("AnimateNearRangeEnd", true)]
		public float RemapNearRangeEndOne = 3f;
		
		[MMFInspectorGroup("Far Range", true, 51)]
		
		[Header("Far Range Start")]
		/// whether or not to animate the far range start
		[Tooltip("是否驱动远景范围起点变化")]
		public bool AnimateFarRangeStart = false;
		/// the curve used to animate the far range start on
		[Tooltip("用于驱动远景范围起点变化的曲线")]
		[MMFCondition("AnimateFarRangeStart", true)]
		public AnimationCurve ShakeFarRangeStart = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[MMFCondition("AnimateFarRangeStart", true)]
		public float RemapFarRangeStartZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[MMFCondition("AnimateFarRangeStart", true)]
		public float RemapFarRangeStartOne = 3f;
		
		[Header("Far Range End")]
		/// whether or not to animate the far range end
		[Tooltip("是否驱动远景范围终点变化")]
		public bool AnimateFarRangeEnd = false;
		/// the curve used to animate the far range end on
		[Tooltip("用于驱动远景范围终点变化的曲线")]
		[MMFCondition("AnimateFarRangeEnd", true)]
		public AnimationCurve ShakeFarRangeEnd = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[MMFCondition("AnimateFarRangeEnd", true)]
		public float RemapFarRangeEndZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[MMFCondition("AnimateFarRangeEnd", true)]
		public float RemapFarRangeEndOne = 3f;

		/// <summary>
		/// Triggers a vignette shake
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
			MMDepthOfFieldShakeEvent_HDRP.Trigger(Duration, intensityMultiplier, ChannelData, ResetShakerValuesAfterShake, 
				ResetTargetValuesAfterShake, NormalPlayDirection, ComputedTimescaleMode, false, false, 
				AnimateFocusDistance, ShakeFocusDistance, RemapFocusDistanceZero, RemapFocusDistanceOne,
				AnimateNearRangeStart, ShakeNearRangeStart, RemapNearRangeStartZero, RemapNearRangeStartOne,
				AnimateNearRangeEnd, ShakeNearRangeEnd, RemapNearRangeEndZero, RemapNearRangeEndOne,
				AnimateFarRangeStart, ShakeFarRangeStart, RemapFarRangeStartZero, RemapFarRangeStartOne,
				AnimateFarRangeEnd, ShakeFarRangeEnd,RemapFarRangeEndZero,RemapFarRangeEndOne);
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
			MMDepthOfFieldShakeEvent_HDRP.Trigger(Duration, channelData: ChannelData, stop:true);
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
			
			MMDepthOfFieldShakeEvent_HDRP.Trigger(Duration, channelData: ChannelData, restore:true);
		}
		
		/// <summary>
		/// Automaticall sets up the post processing profile and shaker
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			#if MM_HDRP && UNITY_EDITOR
			MMHDRPHelpers.GetOrCreateVolume<DepthOfField, MMDepthOfFieldShaker_HDRP>(Owner, "DepthOfField");
			#endif
		}
	}
}