using UnityEngine;
using MoreMountains.Feedbacks;
using UnityEngine.Scripting.APIUpdating;
#if MM_POSTPROCESSING
using UnityEngine.Rendering.PostProcessing;
#endif

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// This feedback allows you to control color grading post exposure, hue shift, saturation and contrast over time. 
	/// It requires you have in your scene an object with a PostProcessVolume 
	/// with Color Grading active, and a MMColorGradingShaker component.
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	#if MM_POSTPROCESSING
	[FeedbackPath("PostProcess/Color Grading")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.PostProcessing")]
	[FeedbackHelp("此反馈可让你随时间控制 Color Grading 的后曝光、色相偏移、饱和度与对比度。" +
	              "它要求你的场景中存在一个带有 PostProcessVolume 的对象，且该对象" +
	              "已启用 Color Grading，并挂有 MMColorGradingShaker 组件。")]
	public class MMF_ColorGrading : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.PostProcessColor; } }
		public override string RequiredTargetText => RequiredChannelText;
		public override bool HasCustomInspectors => true;
		public override bool HasAutomaticShakerSetup => true;
		#endif

		/// the duration of this feedback is the duration of the shake
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(ShakeDuration); }  set { ShakeDuration = value;  } }
		public override bool HasChannel => true;
		public override bool HasRandomness => true;

		[MMFInspectorGroup("Color Grading", true, 46)]
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

		[MMFInspectorGroup("Post Exposure", true, 47)]
		/// the curve used to animate the focus distance value on
		[Tooltip("用于驱动焦点距离值变化的曲线")]
		public AnimationCurve ShakePostExposure = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		public float RemapPostExposureZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		public float RemapPostExposureOne = 1f;

		[MMFInspectorGroup("Hue Shift", true, 48)]
		/// the curve used to animate the aperture value on
		[Tooltip("用于驱动光圈值变化的曲线")]
		public AnimationCurve ShakeHueShift = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[Range(-180f, 180f)]
		public float RemapHueShiftZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[Range(-180f, 180f)]
		public float RemapHueShiftOne = 180f;

		[MMFInspectorGroup("Saturation", true, 49)]
		/// the curve used to animate the focal length value on
		[Tooltip("用于驱动焦距值变化的曲线")]
		public AnimationCurve ShakeSaturation = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[Range(-100f, 100f)]
		public float RemapSaturationZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[Range(-100f, 100f)]
		public float RemapSaturationOne = 100f;

		[MMFInspectorGroup("Contrast", true, 50)]
		/// the curve used to animate the focal length value on
		[Tooltip("用于驱动焦距值变化的曲线")]
		public AnimationCurve ShakeContrast = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		[Range(-100f, 100f)]
		public float RemapContrastZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		[Range(-100f, 100f)]
		public float RemapContrastOne = 100f;

		[MMFInspectorGroup("Color Filter", true, 50)]
		/// if this is true, the color filter will be animated over the gradient below
		[Tooltip("若启用此项，颜色滤镜将按下方渐变随时间变化")]
		public bool ShakeColorFilter = false;
		/// the gradient to use to animate the color filter over time
		[Tooltip("用于随时间驱动颜色滤镜变化的渐变")]
		[GradientUsage(true)]
		public Gradient ColorFilterGradient;
		
		/// <summary>
		/// Triggers a color grading shake
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
			MMColorGradingShakeEvent.Trigger(ShakePostExposure, RemapPostExposureZero, RemapPostExposureOne, 
				ShakeHueShift, RemapHueShiftZero, RemapHueShiftOne, 
				ShakeSaturation, RemapSaturationZero, RemapSaturationOne, 
				ShakeContrast, RemapContrastZero, RemapContrastOne, 
				ShakeColorFilter, ColorFilterGradient,
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
            
			MMColorGradingShakeEvent.Trigger(ShakePostExposure, RemapPostExposureZero, RemapPostExposureOne, 
				ShakeHueShift, RemapHueShiftZero, RemapHueShiftOne, 
				ShakeSaturation, RemapSaturationZero, RemapSaturationOne, 
				ShakeContrast, RemapContrastZero, RemapContrastOne, 
				ShakeColorFilter, ColorFilterGradient,
				FeedbackDuration,                     
				stop:true);
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
            
			MMColorGradingShakeEvent.Trigger(ShakePostExposure, RemapPostExposureZero, RemapPostExposureOne, 
				ShakeHueShift, RemapHueShiftZero, RemapHueShiftOne, 
				ShakeSaturation, RemapSaturationZero, RemapSaturationOne, 
				ShakeContrast, RemapContrastZero, RemapContrastOne, 
				ShakeColorFilter, ColorFilterGradient,
				FeedbackDuration,                     
				restore:true);
		}
		
		/// <summary>
		/// Automaticall sets up the post processing profile and shaker
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			#if UNITY_EDITOR && MM_POSTPROCESSING
			MMPostProcessingHelpers.GetOrCreateVolume<ColorGrading, MMColorGradingShaker>(Owner, "Color Grading");
			#endif
		}
	}
}