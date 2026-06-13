using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// This feedback will let you pilot a Global PostProcessing Volume AutoBlend component. A GPPVAB component is placed on a PostProcessing Volume, and will let you control and blend its weight over time on demand.    
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("此反馈可用于驱动 Global PostProcessing Volume AutoBlend 组件。" +
	              "GPPVAB 组件挂在 PostProcessing Volume 上，可让你按需随时间控制并混合它的权重。")]
	#if MM_POSTPROCESSING
	[FeedbackPath("PostProcess/Global PP Volume Auto Blend")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.PostProcessing")]
	public class MMF_GlobalPPVolumeAutoBlend : MMF_Feedback
	{
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.PostProcessColor; } }
		public override string RequiredTargetText { get { return TargetAutoBlend != null ? TargetAutoBlend.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先指定 TargetAutoBlend 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetAutoBlend = FindAutomatedTarget<MMGlobalPostProcessingVolumeAutoBlend>();
		public override bool HasChannel => true;
        
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// the possible modes for this feedback :
		/// - default : will let you trigger Blend() and BlendBack() on the blender
		/// - override : lets you specify new initial, final, duration and curve values on the blender, and triggers a Blend()
		public enum Modes { Default, Override }
		/// the possible actions when in Default mode
		public enum Actions { Blend, BlendBack }
		/// defines the duration of the feedback
		public override float FeedbackDuration
		{
			get
			{
				if (Mode == Modes.Override)
				{
					return ApplyTimeMultiplier(BlendDuration);
				}
				else
				{
					if (TargetAutoBlend == null)
					{
						return 0.1f;
					}
					else
					{
						return ApplyTimeMultiplier(TargetAutoBlend.BlendDuration);
					}
				}
			}
		}
               
		[MMFInspectorGroup("PostProcess Volume Blend", true, 53, true)]
		/// the target auto blend to pilot with this feedback
		[Tooltip("此反馈要驱动的自动混合目标")]
		public MMGlobalPostProcessingVolumeAutoBlend TargetAutoBlend;
		/// the chosen mode
		[Tooltip("所选模式")]
		public Modes Mode = Modes.Default;
		/// the chosen action when in default mode
		[Tooltip("默认模式下要执行的操作")]
		[MMFEnumCondition("Mode", (int)Modes.Default)]
		public Actions BlendAction = Actions.Blend;
		/// the duration of the blend, in seconds when in override mode
		[Tooltip("Override 模式下的混合持续时间（秒）")]
		[MMFEnumCondition("Mode", (int)Modes.Override)]
		public float BlendDuration = 1f;
		/// the curve to apply to the blend
		[Tooltip("应用到混合过程中的曲线")]
		[MMFEnumCondition("Mode", (int)Modes.Override)]
		public AnimationCurve BlendCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1f));
		/// the weight to blend from
		[Tooltip("混合起始权重")]
		[MMFEnumCondition("Mode", (int)Modes.Override)]
		public float InitialWeight = 0f;
		/// the weight to blend to
		[Tooltip("混合目标权重")]
		[MMFEnumCondition("Mode", (int)Modes.Override)]
		public float FinalWeight = 1f;
		/// whether or not to reset to the initial value at the end of the shake
		[Tooltip("抖动结束时是否恢复为初始值")]
		[MMFEnumCondition("Mode", (int)Modes.Override)]
		public bool ResetToInitialValueOnEnd = true;
        

		/// <summary>
		/// On custom play, triggers a blend on the target blender, overriding its settings if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			#if MM_POSTPROCESSING
			MMGlobalPostProcessingVolumeAutoBlend.TimeScales timeScale = (ComputedTimescaleMode == TimescaleModes.Scaled) ? MMGlobalPostProcessingVolumeAutoBlend.TimeScales.Scaled : MMGlobalPostProcessingVolumeAutoBlend.TimeScales.Unscaled;
			MMPostProcessingVolumeAutoBlendShakeEvent.Trigger(ChannelData, TargetAutoBlend, Mode, BlendAction, ApplyTimeMultiplier(BlendDuration), BlendCurve, InitialWeight, FinalWeight, ResetToInitialValueOnEnd, NormalPlayDirection, timeScale);
			#endif
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
			#if MM_POSTPROCESSING
			base.CustomStopFeedback(position, feedbacksIntensity);
            
			if (TargetAutoBlend != null)
			{
				TargetAutoBlend.StopBlending();
			}
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

			#if UNITY_EDITOR && MM_POSTPROCESSING
			TargetAutoBlend.RestoreInitialValues();
			#endif
		}
	}
}
