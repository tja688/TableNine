using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you modify the scale of an object on an axis while the other two axis (or only one) get automatically modified to conserve mass
	/// </summary>
	[AddComponentMenu("")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Transform/Squash and Stretch")]
	[FeedbackHelp("此反馈可在一个轴上压缩/拉伸目标缩放，并自动联动另外一个或两个轴以保持体积观感。目标对象建议使用标准化初始缩放（如 1,1,1），否则视觉结果可能异常。")]
	public class MMF_SquashAndStretch : MMF_Feedback
	{
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TransformColor; } }
		public override bool EvaluateRequiresSetup() { return (SquashAndStretchTarget == null); }
		public override string RequiredTargetText { get { return SquashAndStretchTarget != null ? SquashAndStretchTarget.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置 SquashAndStretchTarget 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => SquashAndStretchTarget = FindAutomatedTarget<Transform>();
		
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// the possible modes this feedback can operate on
		public enum Modes { Absolute, Additive, ToDestination }
		/// the various axis on which to apply the squash and stretch
		public enum PossibleAxis { XtoYZ, XtoY, XtoZ, YtoXZ, YtoX, YtoZ, ZtoXY, ZtoX, ZtoY }
		/// the possible timescales for the animation of the scale
		public enum TimeScales { Scaled, Unscaled }

		[MMFInspectorGroup("Squash & Stretch", true, 54, true)]

		/// the object to animate
		[Tooltip("要执行压缩/拉伸动画的目标对象。")]
		public Transform SquashAndStretchTarget;
		/// the mode this feedback should operate on
		/// Absolute : follows the curve
		/// Additive : adds to the current scale of the target
		/// ToDestination : sets the scale to the destination target, whatever the current scale is
		[Tooltip("模式：Absolute 按曲线直接计算目标缩放；Additive 在当前缩放基础上叠加；ToDestination 向目标缩放值过渡。")]
		public Modes Mode = Modes.Absolute;
		public PossibleAxis Axis = PossibleAxis.YtoXZ;
		/// the duration of the animation
		[Tooltip("动画持续时间（秒）。")]
		public float AnimateScaleDuration = 0.2f;
		/// the value to remap the curve's 0 value to
		[Tooltip("将曲线 0 端重映射到的缩放值。")]
		public float RemapCurveZero = 1f;
		/// the value to remap the curve's 1 value to
		[Tooltip("将曲线 1 端重映射到的缩放值。")]
		[FormerlySerializedAs("Multiplier")]
		public float RemapCurveOne = 2f;
		/// how much should be added to the curve
		[Tooltip("应用在曲线结果上的附加偏移。")]
		public float Offset = 0f;
		/// the curve along which to animate the scale
		[Tooltip("用于驱动压缩/拉伸的动画曲线。")]
		public AnimationCurve AnimateCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1.5f), new Keyframe(1, 0));
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("若开启，即使当前播放尚未结束，再次 Play 也会立刻重触发；若关闭，则会忽略新的 Play，直到本次播放结束。")] 
		public bool AllowAdditivePlays = false;
		/// if this is true, initial and destination scales will be recomputed on every play
		[Tooltip("若开启，每次 Play 前都会重新读取初始缩放。目标在运行时会改变缩放时建议开启。")]
		public bool DetermineScaleOnPlay = false;
		/// the scale to reach when in ToDestination mode
		[Tooltip("ToDestination 模式下要过渡到的目标缩放值。仅在 ToDestination 模式下生效。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public float DestinationScale = 2f;

		/// the duration of this feedback is the duration of the scale animation
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(AnimateScaleDuration); } set { AnimateScaleDuration = value; } }
		public override bool HasRandomness => true;

		protected Vector3 _initialScale;
		protected float _initialAxisScale;
		protected Vector3 _newScale;
		protected Coroutine _coroutine;

		/// <summary>
		/// On init we store our initial scale
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			if (Active && (SquashAndStretchTarget != null))
			{
				GetInitialScale();
			}
		}

		/// <summary>
		/// Stores initial scale for future use
		/// </summary>
		protected virtual void GetInitialScale()
		{
			_initialScale = SquashAndStretchTarget.localScale;
		}

		protected virtual void GetAxisScale()
		{
			switch (Axis)
			{
				case PossibleAxis.XtoYZ:
					_initialAxisScale = SquashAndStretchTarget.localScale.x;
					break;
				case PossibleAxis.XtoY:
					_initialAxisScale = SquashAndStretchTarget.localScale.x;
					break;
				case PossibleAxis.XtoZ:
					_initialAxisScale = SquashAndStretchTarget.localScale.x;
					break;
				case PossibleAxis.YtoXZ:
					_initialAxisScale = SquashAndStretchTarget.localScale.y;
					break;
				case PossibleAxis.YtoX:
					_initialAxisScale = SquashAndStretchTarget.localScale.y;
					break;
				case PossibleAxis.YtoZ:
					_initialAxisScale = SquashAndStretchTarget.localScale.y;
					break;
				case PossibleAxis.ZtoXY:
					_initialAxisScale = SquashAndStretchTarget.localScale.z;
					break;
				case PossibleAxis.ZtoX:
					_initialAxisScale = SquashAndStretchTarget.localScale.z;
					break;
				case PossibleAxis.ZtoY:
					_initialAxisScale = SquashAndStretchTarget.localScale.z;
					break;
			}
		}

		/// <summary>
		/// On Play, triggers the scale animation
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (SquashAndStretchTarget == null)) 
			{
				return;
			}
            
			if (DetermineScaleOnPlay && NormalPlayDirection)
			{
				GetInitialScale();
			}
            
			GetAxisScale();
            
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			if (Active || Owner.AutoPlayOnEnable)
			{
				if ((Mode == Modes.Absolute) || (Mode == Modes.Additive))
				{
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(AnimateScale(SquashAndStretchTarget, FeedbackDuration, AnimateCurve, Axis, RemapCurveZero, RemapCurveOne * intensityMultiplier));
				}
				if (Mode == Modes.ToDestination)
				{
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(ScaleToDestination());
				}                   
			}
		}

		/// <summary>
		/// An internal coroutine used to scale the target to its destination scale
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ScaleToDestination()
		{
			if (SquashAndStretchTarget == null)
			{
				yield break;
			}

			if (FeedbackDuration == 0f)
			{
				yield break;
			}

			float journey = NormalPlayDirection ? 0f : FeedbackDuration;

			_initialScale = SquashAndStretchTarget.localScale;
			_newScale = _initialScale;
			GetAxisScale();
			IsPlaying = true;
            
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float percent = Mathf.Clamp01(journey / FeedbackDuration);

				float newScale = Mathf.LerpUnclamped(_initialAxisScale, DestinationScale, AnimateCurve.Evaluate(percent) + Offset);
				newScale = MMFeedbacksHelpers.Remap(newScale, 0f, 1f, RemapCurveZero, RemapCurveOne);

				ApplyScale(newScale);
                
				SquashAndStretchTarget.localScale = _newScale;

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
                
				yield return null;
			}

			ApplyScale(DestinationScale);
			SquashAndStretchTarget.localScale = NormalPlayDirection ? _newScale : _initialScale;
			_coroutine = null;
			IsPlaying = false;
			yield return null;
		}

		/// <summary>
		/// An internal coroutine used to animate the scale over time
		/// </summary>
		/// <param name="targetTransform"></param>
		/// <param name="duration"></param>
		/// <param name="curveX"></param>
		/// <param name="curveY"></param>
		/// <param name="curveZ"></param>
		/// <param name="multiplier"></param>
		/// <returns></returns>
		protected virtual IEnumerator AnimateScale(Transform targetTransform, float duration, AnimationCurve curve, PossibleAxis axis, float remapCurveZero = 0f, float remapCurveOne = 1f)
		{
			if (targetTransform == null)
			{
				yield break;
			}

			if (duration == 0f)
			{
				yield break;
			}
            
			float journey = NormalPlayDirection ? 0f : duration;
            
			_initialScale = targetTransform.localScale;
			IsPlaying = true;
            
			while ((journey >= 0) && (journey <= duration) && (duration > 0))
			{
				float percent = Mathf.Clamp01(journey / duration);
				ComputeAndApplyScale(percent, curve, remapCurveZero, remapCurveOne, targetTransform);
				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}

			float endTime = NormalPlayDirection ? 1f : 0f;
			ComputeAndApplyScale(endTime, curve, remapCurveZero, remapCurveOne, targetTransform);
			_coroutine = null;
			IsPlaying = false;
			yield return null;
		}

		/// <summary>
		/// Computes the new scale based on the current percent, and applies it to the transform
		/// </summary>
		/// <param name="percent"></param>
		/// <param name="curve"></param>
		/// <param name="remapCurveZero"></param>
		/// <param name="remapCurveOne"></param>
		/// <param name="targetTransform"></param>
		protected virtual void ComputeAndApplyScale(float percent, AnimationCurve curve, float remapCurveZero, float remapCurveOne, Transform targetTransform)
		{
			float newScale = curve.Evaluate(percent) + Offset;
			newScale = MMFeedbacksHelpers.Remap(newScale, 0f, 1f, remapCurveZero, remapCurveOne);
			if (Mode == Modes.Additive)
			{
				newScale += _initialAxisScale;
			}
			newScale = Mathf.Abs(newScale);
			ApplyScale(newScale);
			targetTransform.localScale = _newScale;
		}

		/// <summary>
		/// Applies the new scale on the selected axis
		/// </summary>
		/// <param name="newScale"></param>
		protected virtual void ApplyScale(float newScale)
		{
			_newScale = _initialScale;
			float invertScale = 1 / Mathf.Sqrt(newScale);
			switch (Axis)
			{
				case PossibleAxis.XtoYZ:
					_newScale.x = _initialScale.x * newScale;
					_newScale.y = _initialScale.y * invertScale;
					_newScale.z = _initialScale.z * invertScale;
					break;
				case PossibleAxis.XtoY:
					_newScale.x = _initialScale.x * newScale;
					_newScale.y = _initialScale.y * invertScale;
					_newScale.z = _initialScale.z;
					break;
				case PossibleAxis.XtoZ:
					_newScale.x = _initialScale.x * newScale;
					_newScale.y = _initialScale.y;
					_newScale.z = _initialScale.z * invertScale;
					break;
				case PossibleAxis.YtoXZ:
					_newScale.x = _initialScale.x * invertScale;
					_newScale.y = _initialScale.y * newScale;
					_newScale.z = _initialScale.z * invertScale;
					break;
				case PossibleAxis.YtoX:
					_newScale.x = _initialScale.x * invertScale;
					_newScale.y = _initialScale.y * newScale;
					_newScale.z = _initialScale.z;
					break;
				case PossibleAxis.YtoZ:
					_newScale.x = _initialScale.x * newScale;
					_newScale.y = _initialScale.y;
					_newScale.z = _initialScale.z * invertScale;
					break;
				case PossibleAxis.ZtoXY:
					_newScale.x = _initialScale.x * invertScale;
					_newScale.y = _initialScale.y * invertScale;
					_newScale.z = _initialScale.z * newScale;
					break;
				case PossibleAxis.ZtoX:
					_newScale.x = _initialScale.x * invertScale;
					_newScale.y = _initialScale.y;
					_newScale.z = _initialScale.z * newScale;
					break;
				case PossibleAxis.ZtoY:
					_newScale.x = _initialScale.x;
					_newScale.y = _initialScale.y * invertScale;
					_newScale.z = _initialScale.z * newScale;
					break;
			}
		}

		/// <summary>
		/// On stop, we interrupt movement if it was active
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (Active && (_coroutine != null))
			{
				Owner.StopCoroutine(_coroutine);
				_coroutine = null;
				IsPlaying = false;
			}
		}

		/// <summary>
		/// On disable we reset our coroutine
		/// </summary>
		public override void OnDisable()
		{
			_coroutine = null;
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

			SquashAndStretchTarget.localScale = _initialScale;
		}
	}
}
