using System.Collections;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will animate the scale of the target object over time when played
	/// </summary>
	[AddComponentMenu("")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Transform/Scale")]
	[FeedbackHelp("此反馈会在指定持续时间内，按照 3 条指定动画曲线分别控制目标的缩放。你还可以设置乘数，用来统一放大或缩小每条曲线的输出值。")]
	public class MMF_Scale : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// the possible modes this feedback can operate on
		public enum Modes { Absolute, Additive, ToDestination }
		/// whether to animate the scale over time or at a fixed speed
		public enum MovementModes { Duration, Speed }
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TransformColor; } }
		public override bool EvaluateRequiresSetup() { return (AnimateScaleTarget == null); }
		public override string RequiredTargetText { get { return AnimateScaleTarget != null ? AnimateScaleTarget.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置an AnimateScaleTarget才能正常工作。你可以在下方进行设置。"; } }
		public override bool HasCustomInspectors { get { return true; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		public override bool CanForceInitialValue => true;
		protected override void AutomateTargetAcquisition() => AnimateScaleTarget = FindAutomatedTarget<Transform>();

		[MMFInspectorGroup("Scale Mode", true, 12, true)]
		/// the mode this feedback should operate on
		/// Absolute : follows the curve
		/// Additive : adds to the current scale of the target
		/// ToDestination : sets the scale to the destination target, whatever the current scale is
		[Tooltip("此反馈模式应在 Absolute 上运行：遵循曲线 Additive：添加到目标的当前比例 ToDestination：将缩放设置为目标目标，无论当前比例是什么")]
		public Modes Mode = Modes.Absolute;
		/// the object to animate
		[Tooltip("要执行动画的对象")]
		public Transform AnimateScaleTarget;
        
		[MMFInspectorGroup("Scale Animation", true, 13)]
		/// whether movement should occur over a fixed duration, or at a certain speed. Note that speed mode will only apply in AtoB and ToDestination modes
		[Tooltip("移动应在固定持续时间内完成，还是按指定速度进行。注意：Speed 模式仅在 AtoB 和 ToDestination 模式下生效")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public MovementModes MovementMode = MovementModes.Duration;
		/// the duration of the animation
		[Tooltip("动画的持续时间")]
		[MMFEnumCondition("MovementMode", (int)MovementModes.Duration)]
		public float AnimateScaleDuration = 0.2f;
		/// in speed mode, the speed at which we should animate the position
		[Tooltip("在 Speed 模式下，用于驱动位置动画的速度")]
		[MMFEnumCondition("MovementMode", (int)MovementModes.Speed)]
		public float AnimatePositionSpeed = 1f;
		/// the value to remap the curve's 0 value to
		[Tooltip("将曲线 0 端重新映射到的值")]
		public float RemapCurveZero = 1f;
		/// the value to remap the curve's 1 value to
		[Tooltip("将曲线 1 端重新映射到的值")]
		[FormerlySerializedAs("Multiplier")]
		public float RemapCurveOne = 2f;
		/// how much should be added to the curve
		[Tooltip("曲线结果的附加偏移量")]
		public float Offset = 0f;
		/// if this is true, should animate the X scale value
		[Tooltip("若开启，动画 X 缩放")]
		public bool AnimateX = true;
		/// X 缩放动画定义
		[Tooltip("X 缩放动画定义")]
		public MMTweenType AnimateScaleTweenX = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1.5f), new Keyframe(1, 0)), "AnimateX");
		/// if this is true, should animate the Y scale value
		[Tooltip("若开启，动画 Y 缩放")]
		public bool AnimateY = true;
		/// Y 缩放动画定义
		[Tooltip("Y 缩放动画定义")]
		public MMTweenType AnimateScaleTweenY = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1.5f), new Keyframe(1, 0)), "AnimateY");
		/// if this is true, should animate the z scale value
		[Tooltip("若开启，动画 Z 缩放")]
		public bool AnimateZ = true;
		/// Z 缩放动画定义
		[Tooltip("Z 缩放动画定义")]
		public MMTweenType AnimateScaleTweenZ = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1.5f), new Keyframe(1, 0)), "AnimateZ");
		/// if this is true, the AnimateX curve only will be used, and applied to all axis
		[Tooltip("若开启，将仅使用 AnimateX 曲线，并应用到全部轴。")] 
		public bool UniformScaling = false;
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("若开启此项，即使该反馈仍在执行中，再次调用也会立即触发；若关闭此项，在当前播放结束前将阻止新的 Play 调用")] 
		public bool AllowAdditivePlays = false;
		/// if this is true, initial and destination scales will be recomputed on every play
		[Tooltip("若开启，每次播放都会重新计算初始值与目标值")]
		public bool DetermineScaleOnPlay = false;
		/// the scale to reach when in ToDestination mode
		[Tooltip("在 ToDestination 模式下要达到的缩放值")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public Vector3 DestinationScale = new Vector3(0.5f, 0.5f, 0.5f);

		/// the duration of this feedback is the duration of the scale animation
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(AnimateScaleDuration); } set { AnimateScaleDuration = value; } }
		public override bool HasRandomness => true;

		/// [DEPRECATED] X 缩放动画定义
		[HideInInspector] public AnimationCurve AnimateScaleX = null;
		/// [DEPRECATED] Y 缩放动画定义
		[HideInInspector] public AnimationCurve AnimateScaleY = null;
		/// [DEPRECATED] Z 缩放动画定义
		[HideInInspector] public AnimationCurve AnimateScaleZ = null;
		
		protected Vector3 _initialScale;
		protected Vector3 _newScale;
		protected Coroutine _coroutine;

		/// <summary>
		/// On init we store our initial scale
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			if (Active && (AnimateScaleTarget != null))
			{
				GetInitialScale();
			}
		}

		/// <summary>
		/// Stores initial scale for future use
		/// </summary>
		protected virtual void GetInitialScale()
		{
			_initialScale = AnimateScaleTarget.localScale;
		}
		
		/// <summary>
		/// In speed mode, computes the duration the feedback should last based on the distance between the two points and the speed
		/// </summary>
		/// <param name="pointA"></param>
		/// <param name="pointB"></param>
		/// <param name="duration"></param>
		/// <returns></returns>
		protected virtual float HandleSpeedMode(Vector3 pointA, Vector3 pointB, float duration)
		{
			if (MovementMode != MovementModes.Speed)
			{
				return duration;
			}
			return Vector3.Distance(pointA, pointB) / AnimatePositionSpeed;
		}

		/// <summary>
		/// On Play, triggers the scale animation
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (AnimateScaleTarget == null))
			{
				return;
			}
            
			if (DetermineScaleOnPlay && NormalPlayDirection)
			{
				GetInitialScale();
			}
            
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
					_coroutine = Owner.StartCoroutine(AnimateScale(AnimateScaleTarget, Vector3.zero, FeedbackDuration, AnimateScaleTweenX, AnimateScaleTweenY, AnimateScaleTweenZ, RemapCurveZero * intensityMultiplier, RemapCurveOne * intensityMultiplier));
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
			if (AnimateScaleTarget == null)
			{
				yield break;
			}

			if ((AnimateScaleTweenX == null) || (AnimateScaleTweenY == null) || (AnimateScaleTweenZ == null))
			{
				yield break;
			}

			if (AnimateScaleDuration == 0f)
			{
				AnimateScaleTarget.localScale = NormalPlayDirection ? DestinationScale : _initialScale;
				_coroutine = null;
				IsPlaying = false;
				yield break;
			}

			float journey = NormalPlayDirection ? 0f : FeedbackDuration;

			float duration = HandleSpeedMode(AnimateScaleTarget.localScale, DestinationScale, FeedbackDuration);

			_initialScale = AnimateScaleTarget.localScale;
			_newScale = _initialScale;
			IsPlaying = true;
			while ((journey >= 0) && (journey <= duration) && (duration > 0))
			{
				float percent = Mathf.Clamp01(journey / duration);

				if (AnimateX)
				{
					_newScale.x = Mathf.LerpUnclamped(_initialScale.x, DestinationScale.x, AnimateScaleTweenX.Evaluate(percent) + Offset);
					_newScale.x = MMFeedbacksHelpers.Remap(_newScale.x, 0f, 1f, RemapCurveZero, RemapCurveOne);
				}

				if (AnimateY)
				{
					_newScale.y = Mathf.LerpUnclamped(_initialScale.y, DestinationScale.y, AnimateScaleTweenY.Evaluate(percent) + Offset);
					_newScale.y = MMFeedbacksHelpers.Remap(_newScale.y, 0f, 1f, RemapCurveZero, RemapCurveOne);    
				}

				if (AnimateZ)
				{
					_newScale.z = Mathf.LerpUnclamped(_initialScale.z, DestinationScale.z, AnimateScaleTweenZ.Evaluate(percent) + Offset);
					_newScale.z = MMFeedbacksHelpers.Remap(_newScale.z, 0f, 1f, RemapCurveZero, RemapCurveOne);    
				}

				if (UniformScaling)
				{
					_newScale.y = _newScale.x;
					_newScale.z = _newScale.x;
				}
                
				AnimateScaleTarget.localScale = _newScale;

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
                
				yield return null;
			}

			AnimateScaleTarget.localScale = NormalPlayDirection ? DestinationScale : _initialScale;
			_coroutine = null;
			IsPlaying = false;
			yield return null;
		}

		/// <summary>
		/// An internal coroutine used to animate the scale over time
		/// </summary>
		/// <param name="targetTransform"></param>
		/// <param name="vector"></param>
		/// <param name="duration"></param>
		/// <param name="curveX"></param>
		/// <param name="curveY"></param>
		/// <param name="curveZ"></param>
		/// <param name="multiplier"></param>
		/// <returns></returns>
		protected virtual IEnumerator AnimateScale(Transform targetTransform, Vector3 vector, float duration, MMTweenType curveX, MMTweenType curveY, MMTweenType curveZ, float remapCurveZero = 0f, float remapCurveOne = 1f)
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
            
			_initialScale = targetTransform.localScale;
            
			IsPlaying = true;
			
			while ((journey >= 0) && (journey <= duration) && (duration > 0))
			{
				vector = Vector3.zero;
				float percent = Mathf.Clamp01(journey / duration);

				if (AnimateX)
				{
					vector.x = AnimateX ? curveX.Evaluate(percent) + Offset : targetTransform.localScale.x;
					vector.x = MMFeedbacksHelpers.Remap(vector.x, 0f, 1f, remapCurveZero, remapCurveOne);
					if (Mode == Modes.Additive)
					{
						vector.x += _initialScale.x;
					}
				}
				else
				{
					vector.x = targetTransform.localScale.x;
				}

				if (AnimateY)
				{
					vector.y = AnimateY ? curveY.Evaluate(percent) + Offset : targetTransform.localScale.y;
					vector.y = MMFeedbacksHelpers.Remap(vector.y, 0f, 1f, remapCurveZero, remapCurveOne);    
					if (Mode == Modes.Additive)
					{
						vector.y += _initialScale.y;
					}
				}
				else 
				{
					vector.y = targetTransform.localScale.y;
				}

				if (AnimateZ)
				{
					vector.z = AnimateZ ? curveZ.Evaluate(percent) + Offset : targetTransform.localScale.z;
					vector.z = MMFeedbacksHelpers.Remap(vector.z, 0f, 1f, remapCurveZero, remapCurveOne);    
					if (Mode == Modes.Additive)
					{
						vector.z += _initialScale.z;
					}
				}
				else 
				{
					vector.z = targetTransform.localScale.z;
				}

				if (UniformScaling)
				{
					vector.y = vector.x;
					vector.z = vector.x;
				}
                
				targetTransform.localScale = vector;

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
                
				yield return null;
			}
            
			vector = Vector3.zero;

			if (AnimateX)
			{
				vector.x = AnimateX ? curveX.Evaluate(FinalNormalizedTime) + Offset : targetTransform.localScale.x;
				vector.x = MMFeedbacksHelpers.Remap(vector.x, 0f, 1f, remapCurveZero, remapCurveOne);
				if (Mode == Modes.Additive)
				{
					vector.x += _initialScale.x;
				}
			}
			else 
			{
				vector.x = targetTransform.localScale.x;
			}

			if (AnimateY)
			{
				vector.y = AnimateY ? curveY.Evaluate(FinalNormalizedTime) + Offset : targetTransform.localScale.y;
				vector.y = MMFeedbacksHelpers.Remap(vector.y, 0f, 1f, remapCurveZero, remapCurveOne);
				if (Mode == Modes.Additive)
				{
					vector.y += _initialScale.y;
				}
			}
			else 
			{
				vector.y = targetTransform.localScale.y;
			}

			if (AnimateZ)
			{
				vector.z = AnimateZ ? curveZ.Evaluate(FinalNormalizedTime) + Offset : targetTransform.localScale.z;
				vector.z = MMFeedbacksHelpers.Remap(vector.z, 0f, 1f, remapCurveZero, remapCurveOne);    
				if (Mode == Modes.Additive)
				{
					vector.z += _initialScale.z;
				}
			}
			else 
			{
				vector.z = targetTransform.localScale.z;
			}

			if (UniformScaling)
			{
				vector.y = vector.x;
				vector.z = vector.x;
			}
            
			targetTransform.localScale = vector;
			IsPlaying = false;
			_coroutine = null;
			yield return null;
		}

		/// <summary>
		/// On stop, we interrupt movement if it was active
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (_coroutine == null))
			{
				return;
			}
			IsPlaying = false;
			Owner.StopCoroutine(_coroutine);
			_coroutine = null;
            
		}

		/// <summary>
		/// On disable we reset our coroutine
		/// </summary>
		public override void OnDisable()
		{
			_coroutine = null;
		}
		
		/// <summary>
		/// On Validate, we migrate our deprecated animation curves to our tween types if needed
		/// </summary>
		public override void OnValidate()
		{
			base.OnValidate();
			MMFeedbacksHelpers.MigrateCurve(AnimateScaleX, AnimateScaleTweenX, Owner);
			MMFeedbacksHelpers.MigrateCurve(AnimateScaleY, AnimateScaleTweenY, Owner);
			MMFeedbacksHelpers.MigrateCurve(AnimateScaleZ, AnimateScaleTweenZ, Owner);
			if (string.IsNullOrEmpty(AnimateScaleTweenX.ConditionPropertyName))
			{
				AnimateScaleTweenX.ConditionPropertyName = "AnimateX";
				AnimateScaleTweenY.ConditionPropertyName = "AnimateY";
				AnimateScaleTweenZ.ConditionPropertyName = "AnimateZ";	
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
			AnimateScaleTarget.localScale = _initialScale;
		}
	}
}

