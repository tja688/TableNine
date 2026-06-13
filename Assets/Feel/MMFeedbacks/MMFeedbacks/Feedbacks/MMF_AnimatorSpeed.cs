using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you change the speed of a target animator, either once, or instantly and then reset it, or interpolate it over time
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可修改目标 Animator 的速度。可选模式：Once（单次改速）、InstantThenReset（立即改速并在持续时间后恢复）、OverTime（按曲线平滑过渡后恢复）。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Animation/Animator Speed")]
	public class MMF_AnimatorSpeed : MMF_Feedback 
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
        
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.AnimationColor; } }
		public override bool EvaluateRequiresSetup() { return (BoundAnimator == null); }
		public override string RequiredTargetText { get { return BoundAnimator != null ? BoundAnimator.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先指定 BoundAnimator 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasRandomness => true;
		public override bool CanForceInitialValue => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => BoundAnimator = FindAutomatedTarget<Animator>();

		public enum SpeedModes { Once, InstantThenReset, OverTime }
		
		[MMFInspectorGroup("Animation", true, 12, true)]
		/// the animator whose parameters you want to update
		[Tooltip("要更新动画器的参数")]
		public Animator BoundAnimator;

		[MMFInspectorGroup("Speed", true, 14, true)]
		/// whether to change the speed of the target animator once, instantly and reset it later, or have it change over time
		[Tooltip("修改模式：一次（只设置一次）、立即后重置（立即速度设置并在持续时间后恢复）、随时间变化（在持续时间内按曲线过渡后恢复）。")]
		public SpeedModes Mode = SpeedModes.Once; 
		/// the new minimum speed at which to set the animator - value will be randomized between min and max
		[Tooltip("要设置给 Animator 的最小速度。实际值会在 NewSpeedMin 与 NewSpeedMax 之间随机；两者相同则不随机。")]
		public float NewSpeedMin = 0f; 
		/// the new maximum speed at which to set the animator - value will be randomized between min and max
		[Tooltip("要设置给 Animator 的最大速度。实际值会在 NewSpeedMin 与 NewSpeedMax 之间随机；两者相同则不随机。")]
		public float NewSpeedMax = 0f;
		/// when in instant then reset or over time modes, the duration of the effect
		[Tooltip("效果持续时间（秒）。仅在 InstantThenReset 与 OverTime 模式下生效；Once 模式会忽略此项。")]
		[MMFEnumCondition("Mode", (int)SpeedModes.InstantThenReset, (int)SpeedModes.OverTime)]
		public float Duration = 1f;
		/// when in over time mode, the curve against which to evaluate the new speed
		[Tooltip("速度过渡曲线。仅在 OverTime 模式下生效；其他模式会忽略此项。")]
		[MMFEnumCondition("Mode", (int)SpeedModes.OverTime)]
		public AnimationCurve Curve = new AnimationCurve(new Keyframe(0, 0f), new Keyframe(0.5f, 1f), new Keyframe(1, 0f));

		protected Coroutine _coroutine;
		protected float _initialSpeed;
		protected float _startedAt;
        
		/// <summary>
		/// On Play, checks if an animator is bound and triggers parameters
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (BoundAnimator == null)
			{
				Debug.LogWarning("[Animator Speed Feedback] The animator speed feedback on "+Owner.name+" doesn't have a BoundAnimator, it won't work. You need to specify one in its inspector.");
				return;
			}

			if (!IsPlaying)
			{
				_initialSpeed = BoundAnimator.speed;	
			}

			if (Mode == SpeedModes.Once)
			{
				BoundAnimator.speed = ComputeIntensity(DetermineNewSpeed(), position);
			}
			else
			{
				if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
				_coroutine = Owner.StartCoroutine(ChangeSpeedCo());
			}
		}

		/// <summary>
		/// A coroutine used in ForDuration mode
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ChangeSpeedCo()
		{
			if (Mode == SpeedModes.InstantThenReset)
			{
				IsPlaying = true;
				BoundAnimator.speed = DetermineNewSpeed();
				yield return WaitFor(Duration);
				BoundAnimator.speed = _initialSpeed;	
				IsPlaying = false;
			}
			else if (Mode == SpeedModes.OverTime)
			{
				IsPlaying = true;
				_startedAt = FeedbackTime;
				float newTargetSpeed = DetermineNewSpeed();
				while (FeedbackTime - _startedAt < Duration)
				{
					float time = MMFeedbacksHelpers.Remap(FeedbackTime - _startedAt, 0f, Duration, 0f, 1f);
					float t = Curve.Evaluate(time);
					BoundAnimator.speed = Mathf.Max(0f, MMFeedbacksHelpers.Remap(t, 0f, 1f, _initialSpeed, newTargetSpeed));
					yield return null;
				}
				BoundAnimator.speed = _initialSpeed;	
				IsPlaying = false;
			}
		}

		/// <summary>
		/// Determines the new speed for the target animator
		/// </summary>
		/// <returns></returns>
		protected virtual float DetermineNewSpeed()
		{
			return Mathf.Abs(Random.Range(NewSpeedMin, NewSpeedMax));
		}
        
		/// <summary>
		/// On stop, turns the bool parameter to false
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (_coroutine != null)
			{
				Owner.StopCoroutine(_coroutine);	
			}

			BoundAnimator.speed = _initialSpeed;
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

			BoundAnimator.speed = _initialSpeed;
		}
	}
}

