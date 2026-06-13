using UnityEngine;
using MoreMountains.Feedbacks;
using UnityEngine.Scripting.APIUpdating;
#if MM_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// This feedback allows you to control color adjustments' post exposure, hue shift, saturation and contrast over time.
	/// It requires you have in your scene an object with a Volume 
	/// with Color Adjustments active, and a MMColorAdjustmentsShaker_HDRP component.
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	#if MM_HDRP
	[FeedbackPath("PostProcess/Color Adjustments HDRP")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.HDRP")]
	[FeedbackHelp("此反馈可让你随时间控制 Color Adjustments 的后曝光、色相偏移、饱和度与对比度。" +
	              "它要求你的场景中存在一个带有 Volume 的对象，且该对象" +
	              "已启用 Color Adjustments，并挂有 MMColorAdjustmentsShaker_HDRP 组件。")]
	public class MMF_ColorAdjustments_HDRP : MMF_Feedback
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
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(ShakeDuration); } set { ShakeDuration = value; } }
		public override bool HasChannel => true;
		public override bool HasRandomness => true;

		[MMFInspectorGroup("Color Grading", true, 16)]
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

		[MMFInspectorGroup("Post Exposure", true, 15)]
		/// the curve used to animate the focus distance value on
		[Tooltip("用于驱动焦点距离值变化的曲线")]
		public AnimationCurve ShakePostExposure = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// the value to remap the curve's 0 to
		[Tooltip("将曲线 0 端重映射到的值")]
		public float RemapPostExposureZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("将曲线 1 端重映射到的值")]
		public float RemapPostExposureOne = 1f;

		[MMFInspectorGroup("Hue Shift", true, 14)]
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

		[MMFInspectorGroup("Saturation", true, 13)]
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

		[MMFInspectorGroup("Contrast", true, 12)]
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
        
		[Header("Color Filter")] 
		/// the selected color filter mode :
		/// None : nothing will happen,
		/// gradient : evaluates the color over time on that gradient, from left to right,
		/// interpolate : lerps from the current color to the destination one 
		[Tooltip("颜色滤镜模式：" +
		         "None：不执行任何操作，" +
		         "gradient：按时间从左到右采样该渐变来得到颜色，" +
		         "interpolate：从当前颜色插值到目标颜色 ")]
		public MMColorAdjustmentsShaker_HDRP.ColorFilterModes ColorFilterMode = MMColorAdjustmentsShaker_HDRP.ColorFilterModes.None;
		/// the gradient to use to animate the color filter over time
		[Tooltip("用于随时间驱动颜色滤镜变化的渐变")]
		[MMFEnumCondition("ColorFilterMode", (int)MMColorAdjustmentsShaker_HDRP.ColorFilterModes.Gradient)]
		[GradientUsage(true)]
		public Gradient ColorFilterGradient;
		/// the destination color when in interpolate mode
		[Tooltip("处于 interpolate 模式时要过渡到的目标颜色")]
		[MMFEnumCondition("ColorFilterMode", (int) MMColorAdjustmentsShaker_HDRP.ColorFilterModes.Interpolate)]
		public Color ColorFilterDestination = Color.yellow;
		/// the curve to use when interpolating towards the destination color
		[Tooltip("插值到目标颜色时使用的曲线")]
		[MMFEnumCondition("ColorFilterMode", (int) MMColorAdjustmentsShaker_HDRP.ColorFilterModes.Interpolate)]
		public AnimationCurve ColorFilterCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));


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
			MMColorAdjustmentsShakeEvent_HDRP.Trigger(ShakePostExposure, RemapPostExposureZero, RemapPostExposureOne,
				ShakeHueShift, RemapHueShiftZero, RemapHueShiftOne,
				ShakeSaturation, RemapSaturationZero, RemapSaturationOne,
				ShakeContrast, RemapContrastZero, RemapContrastOne,
				ColorFilterMode, ColorFilterGradient, ColorFilterDestination, ColorFilterCurve,
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
            
			MMColorAdjustmentsShakeEvent_HDRP.Trigger(ShakePostExposure, RemapPostExposureZero, RemapPostExposureOne,
				ShakeHueShift, RemapHueShiftZero, RemapHueShiftOne,
				ShakeSaturation, RemapSaturationZero, RemapSaturationOne,
				ShakeContrast, RemapContrastZero, RemapContrastOne,
				ColorFilterMode, ColorFilterGradient, ColorFilterDestination, ColorFilterCurve,
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
			
			MMColorAdjustmentsShakeEvent_HDRP.Trigger(ShakePostExposure, RemapPostExposureZero, RemapPostExposureOne,
				ShakeHueShift, RemapHueShiftZero, RemapHueShiftOne,
				ShakeSaturation, RemapSaturationZero, RemapSaturationOne,
				ShakeContrast, RemapContrastZero, RemapContrastOne,
				ColorFilterMode, ColorFilterGradient, ColorFilterDestination, ColorFilterCurve,
				FeedbackDuration,
				RelativeIntensity, channelData:ChannelData, restore:true);
		}
		
		/// <summary>
		/// Automaticall sets up the post processing profile and shaker
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			#if MM_HDRP && UNITY_EDITOR
			MMHDRPHelpers.GetOrCreateVolume<ColorAdjustments, MMColorAdjustmentsShaker_HDRP>(Owner, "ColorAdjustments");
			#endif
		}
	}
}