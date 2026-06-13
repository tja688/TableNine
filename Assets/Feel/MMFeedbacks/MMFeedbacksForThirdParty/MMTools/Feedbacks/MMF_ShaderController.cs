using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.Scripting.APIUpdating;

#if MM_UI
namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you control values on a target ShaderController, letting you modify the behaviour and aspect of a shader driven material at runtime
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可控制目标 ShaderController。Mode 会决定使用 OneTime 参数组还是 ToDestination 参数组；主目标与 TargetShaderControllerList 中的对象都会被应用同一组参数。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[System.Serializable]
	[FeedbackPath("Renderer/ShaderController")]
	public class MMF_ShaderController : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// the different possible modes 
		public enum Modes { OneTime, ToDestination }
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.RendererColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetShaderController == null); }
		public override string RequiredTargetText { get { return TargetShaderController != null ? TargetShaderController.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先指定 TargetShaderController 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasRandomness => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetShaderController = FindAutomatedTarget<ShaderController>();

		[MMFInspectorGroup("Shader Controller", true, 37, true)]
		/// the mode this controller is in
		[Tooltip("控制模式：OneTime 使用一次性抖动参数；ToDestination 使用目标值插值参数。切换模式会导致另一组参数暂不生效。")]
		public Modes Mode = Modes.OneTime;
		/// the float controller to trigger a one time play on
		[Tooltip("要触发的一次性播放目标 ShaderController。")]
		public ShaderController TargetShaderController;
		/// an optional list of float controllers to trigger a one time play on
		[Tooltip("可选的额外 ShaderController 列表。播放时会与主目标一起触发，并应用相同参数设置。")]
		public List<ShaderController> TargetShaderControllerList;
		/// whether this should revert to original at the end
		[Tooltip("播放结束后是否恢复初始值。注意：会覆盖目标 ShaderController 上同名选项的当前设置。")]
		public bool RevertToInitialValueAfterEnd = false;
		
		/// whether or not to initialize the initial value to the current value on a OneTime play
		[Tooltip("仅在 OneTime 模式下生效：每次播放前是否把“初始值”重设为当前值。")]
		[MMFEnumCondition("Mode", (int)Modes.OneTime)]
		public bool GetInitialValueOnOneTime = false;
		/// the duration of the One Time shake
		[Tooltip("仅在 OneTime 模式下生效：一次性抖动持续时间（秒）。")]
		[MMFEnumCondition("Mode", (int)Modes.OneTime)]
		public float OneTimeDuration = 1f;
		/// the amplitude of the One Time shake (this will be multiplied by the curve's height)
		[Tooltip("仅在 OneTime 模式下生效：抖动幅度（会再乘以曲线当前高度）。")]
		[MMFEnumCondition("Mode", (int)Modes.OneTime)]
		public float OneTimeAmplitude = 1f;
		/// the low value to remap the normalized curve value to 
		[Tooltip("仅在 OneTime 模式下生效：归一化曲线值重映射下限。")]
		[MMFEnumCondition("Mode", (int)Modes.OneTime)]
		public float OneTimeRemapMin = 0f;
		/// the high value to remap the normalized curve value to 
		[Tooltip("仅在 OneTime 模式下生效：归一化曲线值重映射上限。")]
		[MMFEnumCondition("Mode", (int)Modes.OneTime)]
		public float OneTimeRemapMax = 1f;
		/// the curve to apply to the one time shake
		[Tooltip("仅在 OneTime 模式下生效：应用到抖动过程的曲线。")]
		[MMFEnumCondition("Mode", (int)Modes.OneTime)]
		public AnimationCurve OneTimeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

		/// the new value towards which to move the current value
		[Tooltip("仅在 ToDestination 模式下生效：要过渡到的新数值。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public float ToDestinationValue = 1f;
		/// the duration over which to interpolate the target value
		[Tooltip("仅在 ToDestination 模式下生效：插值到目标值所用时长（秒）。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public float ToDestinationDuration = 1f;
		/// the color to aim for (when targetting a Color property
		[Tooltip("仅在 ToDestination 模式下生效：当目标属性为 Color 时，要过渡到的目标颜色。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public Color ToDestinationColor = Color.red;
		/// the curve over which to interpolate the value
		[Tooltip("仅在 ToDestination 模式下生效：用于插值到目标值的曲线。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public AnimationCurve ToDestinationCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

		/// the duration of this feedback is the duration of the one time hit
		public override float FeedbackDuration
		{
			get { return (Mode == Modes.OneTime) ? ApplyTimeMultiplier(OneTimeDuration) : ApplyTimeMultiplier(ToDestinationDuration); } 
			set { OneTimeDuration = value; ToDestinationDuration = value; }
		}

		protected float _oneTimeDurationStorage;
		protected float _oneTimeAmplitudeStorage;
		protected float _oneTimeRemapMinStorage;
		protected float _oneTimeRemapMaxStorage;
		protected AnimationCurve _oneTimeCurveStorage;
		protected float _toDestinationValueStorage;
		protected float _toDestinationDurationStorage;
		protected AnimationCurve _toDestinationCurveStorage;
		protected bool _revertToInitialValueAfterEndStorage;

		/// <summary>
		/// On init we grab our initial controller values
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			if (TargetShaderControllerList == null)
			{
				TargetShaderControllerList = new List<ShaderController>();
			}
			
			if (Active && (TargetShaderController != null))
			{
				_oneTimeDurationStorage = TargetShaderController.OneTimeDuration;
				_oneTimeAmplitudeStorage = TargetShaderController.OneTimeAmplitude;
				_oneTimeCurveStorage = TargetShaderController.OneTimeCurve;
				_oneTimeRemapMinStorage = TargetShaderController.OneTimeRemapMin;
				_oneTimeRemapMaxStorage = TargetShaderController.OneTimeRemapMax;
				_toDestinationCurveStorage = TargetShaderController.ToDestinationCurve;
				_toDestinationDurationStorage = TargetShaderController.ToDestinationDuration;
				_toDestinationValueStorage = TargetShaderController.ToDestinationValue;
				_revertToInitialValueAfterEndStorage = TargetShaderController.RevertToInitialValueAfterEnd;
			}
		}

		/// <summary>
		/// On play we trigger a OneTime or ToDestination play on our shader controller
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetShaderController == null))
			{
				return;
			}
            
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
            
			PerformPlay(TargetShaderController, intensityMultiplier);     

			foreach (ShaderController shaderController in TargetShaderControllerList)
			{
				PerformPlay(shaderController, intensityMultiplier);     
			}    
		}

		protected virtual void PerformPlay(ShaderController shaderController, float intensityMultiplier)
		{
			shaderController.RevertToInitialValueAfterEnd = RevertToInitialValueAfterEnd;
			if (Mode == Modes.OneTime)
			{
				shaderController.OneTimeDuration = FeedbackDuration;
				shaderController.GetInitialValueOnOneTime = GetInitialValueOnOneTime;
				shaderController.OneTimeAmplitude = OneTimeAmplitude;
				shaderController.OneTimeCurve = OneTimeCurve;
				if (NormalPlayDirection)
				{
					shaderController.OneTimeRemapMin = OneTimeRemapMin * intensityMultiplier;
					shaderController.OneTimeRemapMax = OneTimeRemapMax * intensityMultiplier;    
				}
				else
				{
					shaderController.OneTimeRemapMin = OneTimeRemapMax * intensityMultiplier;
					shaderController.OneTimeRemapMax = OneTimeRemapMin * intensityMultiplier;
				}
				shaderController.OneTime();
			}
			if (Mode == Modes.ToDestination)
			{
				shaderController.ToColor = ToDestinationColor;
				shaderController.ToDestinationCurve = ToDestinationCurve;
				shaderController.ToDestinationDuration = FeedbackDuration;
				shaderController.ToDestinationValue = ToDestinationValue;
				shaderController.ToDestination();
			}   
		}
        
		/// <summary>
		/// Sets the final value on the target shader controller(s)
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomSkipToTheEnd(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (Active && FeedbackTypeAuthorized)
			{
				TargetShaderController.SetFinalValue();     

				foreach (ShaderController shaderController in TargetShaderControllerList)
				{
					shaderController.SetFinalValue();
				}    
			}
		}
		
		/// <summary>
		/// Stops this feedback
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
            
			if (TargetShaderController != null)
			{
				TargetShaderController.Stop();
			}

			foreach (ShaderController shaderController in TargetShaderControllerList)
			{
				shaderController.Stop();
			}
		}

		/// <summary>
		/// On reset we restore our initial values
		/// </summary>
		protected override void CustomReset()
		{
			base.CustomReset();
			if (Active && FeedbackTypeAuthorized && (TargetShaderController != null))
			{
				PerformReset(TargetShaderController);
			}

			foreach (ShaderController shaderController in TargetShaderControllerList)
			{
				PerformReset(shaderController);
			}
		}

		protected virtual void PerformReset(ShaderController shaderController)
		{
			shaderController.OneTimeDuration = _oneTimeDurationStorage;
			shaderController.OneTimeAmplitude = _oneTimeAmplitudeStorage;
			shaderController.OneTimeCurve = _oneTimeCurveStorage;
			shaderController.OneTimeRemapMin = _oneTimeRemapMinStorage;
			shaderController.OneTimeRemapMax = _oneTimeRemapMaxStorage;
			shaderController.ToDestinationCurve = _toDestinationCurveStorage;
			shaderController.ToDestinationDuration = _toDestinationDurationStorage;
			shaderController.ToDestinationValue = _toDestinationValueStorage;
			shaderController.RevertToInitialValueAfterEnd = _revertToInitialValueAfterEndStorage;
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
			
			TargetShaderController.RestoreInitialValues();     

			foreach (ShaderController shaderController in TargetShaderControllerList)
			{
				shaderController.RestoreInitialValues();     
			}  
		}
	}
}
#endif
