using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will animate the target's position (not its rotation), on an arc around the specified rotation center, for the specified duration (in seconds).
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈会让目标物体围绕指定中心做圆弧位移动画（修改的是位置，不是自转角度），并在设定时长内完成。可分别启用 X/Y/Z 轴角度曲线。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Transform/Rotate Position Around")]
	public class MMF_RotatePositionAround : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// the timescale modes this feedback can operate on
		public enum TimeScales { Scaled, Unscaled }

		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TransformColor; } }
		public override bool EvaluateRequiresSetup() { return (AnimateRotationTarget == null); }
		public override string RequiredTargetText { get { return ((AnimateRotationTarget != null) || (AnimateRotationCenter != null)) ? AnimateRotationTarget.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置 AnimateRotationTarget 和 AnimateRotationCenter 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => AnimateRotationTarget = FindAutomatedTarget<Transform>();

		[MMFInspectorGroup("Animation Targets", true, 61, true)]
		/// the object whose rotation you want to animate
		[Tooltip("要执行环绕位移动画的目标对象")]
		public Transform AnimateRotationTarget;
		/// the object around which to rotate AnimateRotationTarget
		[Tooltip("旋转中心：AnimateRotationTarget 将围绕该对象做圆弧移动")]
		public Transform AnimateRotationCenter;
		
		[MMFInspectorGroup("Transition", true, 63)]
		/// the duration of the transition
		[Tooltip("过渡持续时间")]
		public float AnimateRotationDuration = 0.2f;
		/// the value to remap the curve's 0 value to
		[Tooltip("将曲线 0 端重新映射到的值")]
		public float RemapCurveZero = 0f;
		/// the value to remap the curve's 1 value to
		[Tooltip("将曲线 1 端重新映射到的值")]
		public float RemapCurveOne = 180f;
		/// if this is true, should animate movement on the X axis
		[Tooltip("是否启用 X 轴角度曲线")]
		public bool AnimateX = false;
		/// how the x part of the movement should animate over time, in degrees
		[Tooltip("X 轴角度曲线（单位：度），用于控制随时间的环绕幅度")]
		[MMCondition("AnimateX", true)]
		public AnimationCurve AnimateRotationX = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0));
		/// if this is true, should animate movement on the Y axis
		[Tooltip("是否启用 Y 轴角度曲线")]
		public bool AnimateY = true;
		/// how the y part of the rotation should animate over time, in degrees
		[Tooltip("Y 轴角度曲线（单位：度），用于控制随时间的环绕幅度")]
		[MMCondition("AnimateY", true)]
		public AnimationCurve AnimateRotationY = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0));
		/// if this is true, should animate movement on the Z axis
		[Tooltip("是否启用 Z 轴角度曲线")]
		public bool AnimateZ = false;
		/// how the z part of the rotation should animate over time, in degrees
		[Tooltip("Z 轴角度曲线（单位：度），用于控制随时间的环绕幅度")]
		[MMCondition("AnimateZ", true)]
		public AnimationCurve AnimateRotationZ = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0));
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("若开启此项，即使该反馈仍在执行中，再次调用也会立即触发；若关闭此项，在当前播放结束前将阻止新的 Play 调用")] 
		public bool AllowAdditivePlays = false;
		/// if this is true, initial and destination rotations will be recomputed on every play
		[Tooltip("若开启，每次播放都会重新记录初始位置。关闭时会复用上次初始化时的位置作为起点")]
		public bool DetermineRotationOnPlay = false;
        
		/// the duration of this feedback is the duration of the rotation
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(AnimateRotationDuration); } set { AnimateRotationDuration = value; } }
		public override bool HasRandomness => true;

		protected Vector3 _initialPosition;
		protected Vector3 _rotationAngles;
		protected Coroutine _coroutine;

		/// <summary>
		/// On init we store our initial rotation
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			
			if (Active && (AnimateRotationTarget != null))
			{
				GetInitialPosition();
			}
		}

		/// <summary>
		/// Stores initial rotation for future use
		/// </summary>
		protected virtual void GetInitialPosition()
		{
			_initialPosition = AnimateRotationTarget.transform.position;
		}

		/// <summary>
		/// On play, we trigger our rotation animation
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (AnimateRotationTarget == null))
			{
				return;
			}
            
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			if (Active || Owner.AutoPlayOnEnable)
			{
				if (!AllowAdditivePlays && (_coroutine != null))
				{
					return;
				}
				if (DetermineRotationOnPlay && NormalPlayDirection) { GetInitialPosition(); }
				ClearCoroutine();
				_coroutine = Owner.StartCoroutine(AnimateRotation(AnimateRotationTarget, Vector3.zero, FeedbackDuration, AnimateRotationX, AnimateRotationY, AnimateRotationZ, RemapCurveZero * intensityMultiplier, RemapCurveOne * intensityMultiplier));
			}
		}

		protected virtual void ClearCoroutine()
		{
			if (_coroutine != null)
			{
				Owner.StopCoroutine(_coroutine);
				_coroutine = null;
			}
		}

		/// <summary>
		/// A coroutine used to compute the rotation over time
		/// </summary>
		/// <param name="targetTransform"></param>
		/// <param name="vector"></param>
		/// <param name="duration"></param>
		/// <param name="curveX"></param>
		/// <param name="curveY"></param>
		/// <param name="curveZ"></param>
		/// <param name="multiplier"></param>
		/// <returns></returns>
		protected virtual IEnumerator AnimateRotation(Transform targetTransform,
			Vector3 vector,
			float duration,
			AnimationCurve curveX,
			AnimationCurve curveY,
			AnimationCurve curveZ,
			float remapZero,
			float remapOne)
		{
			if (targetTransform == null)
			{
				yield break;
			}

			if ((curveX == null) || (curveY == null) || (curveZ == null))
			{
				yield break;
			}

			if (duration == 0f)
			{
				yield break;
			}
            
			float journey = NormalPlayDirection ? 0f : duration;

			IsPlaying = true;
            
			while ((journey >= 0) && (journey <= duration) && (duration > 0))
			{
				float percent = Mathf.Clamp01(journey / duration);
                
				ApplyRotation(targetTransform, remapZero, remapOne, curveX, curveY, curveZ, percent);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
                
				yield return null;
			}

			ApplyRotation(targetTransform, remapZero, remapOne, curveX, curveY, curveZ, FinalNormalizedTime);
			_coroutine = null;
			IsPlaying = false;
            
			yield break;
		}
		
		/// <summary>
		/// Computes and applies the rotation to the object
		/// </summary>
		/// <param name="targetTransform"></param>
		/// <param name="multiplier"></param>
		/// <param name="curveX"></param>
		/// <param name="curveY"></param>
		/// <param name="curveZ"></param>
		/// <param name="percent"></param> 
		protected virtual void ApplyRotation(Transform targetTransform, float remapZero, float remapOne, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, float percent)
		{
			targetTransform.position = _initialPosition;

			_rotationAngles.x = 0f;
			_rotationAngles.y = 0f;
			_rotationAngles.z= 0f;
			
			if (AnimateX)
			{
				_rotationAngles.x = curveX.Evaluate(percent);
				_rotationAngles.x = MMFeedbacksHelpers.Remap(_rotationAngles.x, 0f, 1f, remapZero, remapOne);
			}
			if (AnimateY)
			{
				_rotationAngles.y = curveY.Evaluate(percent);
				_rotationAngles.y = MMFeedbacksHelpers.Remap(_rotationAngles.y, 0f, 1f, remapZero, remapOne);
			}
			if (AnimateZ)
			{
				_rotationAngles.z = curveZ.Evaluate(percent);
				_rotationAngles.z = MMFeedbacksHelpers.Remap(_rotationAngles.z, 0f, 1f, remapZero, remapOne);
			}

			targetTransform.position = MMMaths.RotatePointAroundPivot(targetTransform.position, AnimateRotationCenter.position, _rotationAngles);
		}
        
		/// <summary>
		/// On stop, we interrupt movement if it was active
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (Active && FeedbackTypeAuthorized && (_coroutine != null))
			{
				Owner.StopCoroutine(_coroutine);
				_coroutine = null;
				IsPlaying = false;
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
			AnimateRotationTarget.transform.position = _initialPosition;
		}

		/// <summary>
		/// On disable we reset our coroutine
		/// </summary>
		public override void OnDisable()
		{
			_coroutine = null;
		}
	}
}
