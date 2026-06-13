using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback changes the timescale by sending a TimeScale event on play
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈会触发 MMTimeScaleEvent。若场景中存在 MMTimeManager，它会捕获该事件，并按你设置的参数修改时间缩放。这些参数包括新的 timescale 值、持续时间，以及从正常时间过渡到目标时间的可选过渡速度。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Time/Timescale Modifier")]
	public class MMF_TimescaleModifier : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// <summary>
		/// The possible modes for this feedback :
		/// - shake : changes the timescale for a certain duration
		/// - change : sets the timescale to a new value, forever (until you change it again)
		/// - reset : resets the timescale to its previous value
		/// </summary>
		public enum Modes { Shake, Change, Reset, Unfreeze }

		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TimeColor; } }
		public override string RequiredTargetText { get { return Mode.ToString() + " x" + TimeScale ;  } }
		public override bool HasCustomInspectors => true;
		public override bool HasAutomaticShakerSetup => true;
		#endif

		[MMFInspectorGroup("Timescale Modifier", true, 63)]
		/// the selected mode
		[Tooltip("当前模式说明：再次 - 摇动：将时间刻度改为临时新值，持续 TimeScaleDuration 后恢复到修改前的值 \\\\ - 更改：将时间刻度改为新值并持续保留，直到下一次修改 \\\\ - 重置：将时间刻度重置为 MMTimeManager 中定义的 NormalTimescale \\\\ - 取消冻结：将时间刻度恢复到上一次之前的值")]
		public Modes Mode = Modes.Shake;

		/// the new timescale to apply
		[Tooltip("要应用新的时间刻度值。")]
		public float TimeScale = 0.5f;
		/// the duration of the timescale modification
		[Tooltip("Timescale 修改持续时间（秒）。仅在 Shake 模式下生效。")]
		[MMFEnumCondition("Mode", (int)Modes.Shake)]
		public float TimeScaleDuration = 1f;
		/// whether to reset the timescale on Stop or not
		[Tooltip("停止此反馈时，是否将 timescale 重置为 NormalTimescale。")]
		public bool ResetTimescaleOnStop = false;
		/// whether to unfreeze the timescale on Stop or not - if you set this to true, ResetTimescaleOnStop will be ignored
		[Tooltip("停止此反馈时，是否执行 Unfreeze。若开启，ResetTimescaleOnStop 将被忽略（Unfreeze 优先）。")]
		public bool UnfreezeTimescaleOnStop = false;
		
		[MMFInspectorGroup("Interpolation", true, 63)]
		/// whether or not we should lerp the timescale
		[Tooltip("是否启用 timescale 插值过渡。关闭时会立即切换到目标 timescale。")]
		public bool TimeScaleLerp = false;
		/// whether to lerp over a set duration, or at a certain speed
		[Tooltip("插值模式：按固定时长插值，或按固定速度插值。")]
		public MMTimeScaleLerpModes TimescaleLerpMode = MMTimeScaleLerpModes.Speed;
		/// in Speed mode, the speed at which to lerp the timescale
		[Tooltip("在 Speed 模式下使用的插值速度。")]
		[MMFEnumCondition("TimescaleLerpMode", (int)MMTimeScaleLerpModes.Speed)]
		public float TimeScaleLerpSpeed = 1f;
		/// in Duration mode, the curve to use to lerp the timescale
		[Tooltip("在 Duration 模式下使用的插值曲线。")]
		public MMTweenType TimescaleLerpCurve = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)), 
			enumConditionPropertyName:"TimescaleLerpMode", enumConditionValues:(int)MMTimeScaleLerpModes.Duration); 
		/// in Duration mode, the duration of the timescale interpolation, in unscaled time seconds
		[Tooltip("在 Duration 模式下的插值时长（不受时间缩放影响的时间，单位秒）。")]
		[MMFEnumCondition("TimescaleLerpMode", (int)MMTimeScaleLerpModes.Duration)]
		public float TimescaleLerpDuration = 1f;
		/// whether or not we should lerp the timescale as it goes back to normal afterwards when using Unfreeze mode
		[FormerlySerializedAs("TimeScaleLerpOnReset")]
		[Tooltip("在 Unfreeze 时是否也执行插值过渡。仅在 Duration 模式下生效。")]
		[MMFEnumCondition("TimescaleLerpMode", (int)MMTimeScaleLerpModes.Duration)]
		public bool TimeScaleLerpOnUnfreeze = false;
		/// in Duration mode, the curve to use to lerp the timescale when unfreezing if TimeScaleLerpOnUnfreeze is true
		[FormerlySerializedAs("TimescaleLerpCurveOnReset")] 
		[Tooltip("在 Duration 模式下，且 TimeScaleLerpOnUnfreeze 为 true 时，Unfreeze 使用的插值曲线。")]
		public MMTweenType TimescaleLerpCurveOnUnfreeze = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)), 
			enumConditionPropertyName:"TimescaleLerpMode", enumConditionValues:(int)MMTimeScaleLerpModes.Duration);
		/// in Duration mode, the duration of the timescale interpolation, in unscaled time seconds when unfreezing if TimeScaleLerpOnUnfreeze is true
		[FormerlySerializedAs("TimescaleLerpDurationOnReset")]
		[Tooltip("在 Duration 模式下，且 TimeScaleLerpOnUnfreeze 为 true 时，Unfreeze 插值时长（不受时间缩放影响的时间，单位秒）。")]
		[MMFEnumCondition("TimescaleLerpMode", (int)MMTimeScaleLerpModes.Duration)]
		public float TimescaleLerpDurationOnUnfreeze = 1f;

		/// the duration of this feedback is the duration of the time modification
		public override float FeedbackDuration {
			get
			{
				float totalDuration = (Mode == Modes.Shake) ? TimeScaleDuration : 0f;
				if (TimescaleLerpMode == MMTimeScaleLerpModes.Duration)
				{
					totalDuration += TimeScaleLerp ? TimescaleLerpDuration : 0f;
					if (Mode == Modes.Shake)
					{
						totalDuration += TimeScaleLerpOnUnfreeze ? TimescaleLerpDurationOnUnfreeze : 0f;
					}
				}
				return ApplyTimeMultiplier(totalDuration);
			}
			set
			{
				TimeScaleDuration = value;
			} }

		/// <summary>
		/// On Play, triggers a time scale event
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			switch (Mode)
			{
				case Modes.Shake:
					MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, TimeScale, TimeScaleDuration, TimeScaleLerp, TimeScaleLerpSpeed, false, TimescaleLerpMode, TimescaleLerpCurve, TimescaleLerpDuration, TimeScaleLerpOnUnfreeze, TimescaleLerpCurveOnUnfreeze, TimescaleLerpDurationOnUnfreeze);
					break;
				case Modes.Change:
					MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, TimeScale, 0f, TimeScaleLerp, TimeScaleLerpSpeed, true, TimescaleLerpMode, TimescaleLerpCurve, TimescaleLerpDuration, TimeScaleLerpOnUnfreeze, TimescaleLerpCurveOnUnfreeze, TimescaleLerpDurationOnUnfreeze);
					break;
				case Modes.Reset:
					MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, TimeScale, 0f, false, 0f, true);
					break;
				case Modes.Unfreeze:
					MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Unfreeze, TimeScale, 0f, false, 0f, true);
					break;
			}     
		}

		/// <summary>
		/// On stop, we reset timescale if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (!ResetTimescaleOnStop && !UnfreezeTimescaleOnStop))
			{
				return;
			}
			if (UnfreezeTimescaleOnStop)
			{
				MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Unfreeze, TimeScale, 0f, false, 0f, true);
				return;
			}
			if (ResetTimescaleOnStop)
			{
				MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, TimeScale, 0f, false, 0f, true);
				return;
			}
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
			MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, TimeScale, 0f, false, 0f, true);
		}
		
		/// <summary>
		/// Automatically adds a MMTimeManager to the scene
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			(MMTimeManager timeManager, bool createdNew) = Owner.gameObject.MMFindOrCreateObjectOfType<MMTimeManager>("MMTimeManager", null);
			if (createdNew)
			{
				MMDebug.DebugLogInfo("Added a MMTimeManager to the scene. You're all set.");	
			}
		}
		
		/// <summary>
		/// On Validate, we init our curves conditions if needed
		/// </summary>
		public override void OnValidate()
		{
			base.OnValidate();
			if (string.IsNullOrEmpty(TimescaleLerpCurve.EnumConditionPropertyName))
			{
				TimescaleLerpCurve.EnumConditionPropertyName = "TimescaleLerpMode";
				TimescaleLerpCurveOnUnfreeze.EnumConditionPropertyName = "TimescaleLerpMode";
				TimescaleLerpCurve.EnumConditions = new bool[32];
			}
			if (TimescaleLerpCurve.EnumConditions[(int)MMTimeScaleLerpModes.Duration] == false)
			{
				TimescaleLerpCurve.EnumConditions[(int)MMTimeScaleLerpModes.Duration] = true;
				TimescaleLerpCurveOnUnfreeze.EnumConditions[(int)MMTimeScaleLerpModes.Duration] = true;
			}
		}
	}
}


