using System.Collections;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback animates the rotation of the specified object when played
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让目标对象按 3 条旋转曲线（X/Y/Z 各一条）执行旋转动画，持续指定时长。可用于绝对旋转、增量旋转，或向目标旋转过渡。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Transform/Rotation")]
	public class MMF_Rotation : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// the possible modes for this feedback (Absolute : always follow the curve from start to finish, Additive : add to the values found when this feedback gets played)
		public enum Modes { Absolute, Additive, ToDestination }
		/// whether to animate the scale over time or at a fixed speed
		public enum MovementModes { Duration, Speed }

		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TransformColor; } }
		public override bool EvaluateRequiresSetup() { return (AnimateRotationTarget == null); }
		public override string RequiredTargetText { get { return AnimateRotationTarget != null ? AnimateRotationTarget.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置 AnimateRotationTarget 才能正常工作。你可以在下方进行设置。"; } }
		public override bool HasCustomInspectors { get { return true; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		public override bool CanForceInitialValue => true;
		protected override void AutomateTargetAcquisition() => AnimateRotationTarget = FindAutomatedTarget<Transform>();

		[MMFInspectorGroup("Rotation Target", true, 61, true)]
		/// the object whose rotation you want to animate
		[Tooltip("要执行旋转动画的目标对象。")]
		public Transform AnimateRotationTarget;

		[MMFInspectorGroup("Transition", true, 63)]
		/// whether this feedback should animate in absolute values or additive
		[Tooltip("旋转模式：Absolute（按曲线直接得到目标旋转）、Additive（在初始旋转基础上叠加）、ToDestination（向目标旋转过渡）。")]
		public Modes Mode = Modes.Absolute;
		/// whether this feedback should play on local or world rotation
		[Tooltip("旋转空间：Local 使用本地旋转，World 使用世界旋转。")]
		public Space RotationSpace = Space.World;
		/// whether movement should occur over a fixed duration, or at a certain speed. Note that speed mode will only apply in AtoB and ToDestination modes
		[Tooltip("位移驱动方式：按固定时长（Duration）或按固定速度（Speed）。仅在 ToDestination 模式下生效。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public MovementModes MovementMode = MovementModes.Duration;
		/// the duration of the transition
		[Tooltip("过渡时长（秒）。仅在 MovementMode=Duration 时生效。")]
		[MMFEnumCondition("MovementMode", (int)MovementModes.Duration)]
		public float AnimateRotationDuration = 0.2f;
		/// in speed mode, the speed at which we should animate the position
		[Tooltip("过渡速度。仅在 运动模式=速度 时生效。")]
		[MMFEnumCondition("MovementMode", (int)MovementModes.Speed)]
		public float AnimatePositionSpeed = 1f;
		/// the value to remap the curve's 0 value to
		[Tooltip("将曲线 0 端重映射到的角度值。仅在 Absolute / Additive 模式下生效。")]
		[MMFEnumCondition("Mode", (int)Modes.Absolute, (int)Modes.Additive)]
		public float RemapCurveZero = 0f;
		/// the value to remap the curve's 1 value to
		[Tooltip("将曲线 1 端重映射到的角度值。仅在 Absolute / Additive 模式下生效。")]
		[MMFEnumCondition("Mode", (int)Modes.Absolute, (int)Modes.Additive)]
		public float RemapCurveOne = 360f;
		/// if this is true, should animate the X rotation
		[Tooltip("是否启用 X 轴旋转动画。仅在 Absolute / Additive 模式下生效。")]
		[MMFEnumCondition("Mode", (int)Modes.Absolute, (int)Modes.Additive)]
		public bool AnimateX = true;
		
		
		/// how the x part of the rotation should animate over time, in degrees
		[Tooltip("X 轴旋转曲线（角度）。仅在启用 X 轴动画时生效。")]
		public MMTweenType AnimateRotationTweenX = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)), "AnimateX");
		/// if this is true, should animate the Y rotation
		[Tooltip("是否启用 Y 轴旋转动画。仅在 Absolute / Additive 模式下生效。")]
		[MMFEnumCondition("Mode", (int)Modes.Absolute, (int)Modes.Additive)]
		public bool AnimateY = true;
		/// how the y part of the rotation should animate over time, in degrees
		[Tooltip("Y 轴旋转曲线（角度）。仅在启用 Y 轴动画时生效。")]
		public MMTweenType AnimateRotationTweenY = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)), "AnimateY");
		/// if this is true, should animate the Z rotation
		[Tooltip("是否启用 Z 轴旋转动画。仅在 Absolute / Additive 模式下生效。")]
		[MMFEnumCondition("Mode", (int)Modes.Absolute, (int)Modes.Additive)]
		public bool AnimateZ = true;
		/// how the z part of the rotation should animate over time, in degrees
		[Tooltip("Z 轴旋转曲线（角度）。仅在启用 Z 轴动画时生效。")]
		public MMTweenType AnimateRotationTweenZ = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)), "AnimateZ");
		
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("若开启，即使当前旋转尚未结束，再次 Play 也会立刻重触发；若关闭，则会忽略新的 Play，直到本次播放完成。")] 
		public bool AllowAdditivePlays = false;
		/// if this is true, initial and destination rotations will be recomputed on every play
		[Tooltip("若开启，每次 Play 都会重新计算初始旋转与目标旋转。适用于目标对象或目标 Transform 会动态变化的场景。")]
		public bool DetermineRotationOnPlay = false;
        
		[Header("To Destination")]
		/// the space in which the ToDestination mode should operate 
		[Tooltip("ToDestination 模式下使用的目标旋转空间（Local / World）。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public Space ToDestinationSpace = Space.World;
		/// the angles to match when in ToDestination mode
		[Tooltip("ToDestination 模式下要过渡到的目标角度。若下方 ToDestinationTransform 已设置，则此值会被忽略。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public Vector3 DestinationAngles = new Vector3(0f, 180f, 0f);
		/// an optional transform we want to match the rotation of. if one is set, DestinationAngles will be ignored 
		[Tooltip("可选目标 Transform。若设置，将以该 Transform 的旋转为准，并忽略 DestinationAngles。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public Transform ToDestinationTransform;
		/// how the x part of the rotation should animate over time, in degrees
		[Tooltip("ToDestination 过渡曲线。仅在 ToDestination 模式下生效。")]
		public MMTweenType ToDestinationTween = new MMTweenType(MMTween.MMTweenCurve.EaseInQuintic, "", "Mode", (int)Modes.ToDestination);
		
		/// the duration of this feedback is the duration of the rotation
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(AnimateRotationDuration); } set { AnimateRotationDuration = value; } }
		public override bool HasRandomness => true;
		
		/// [DEPRECATED] how the x part of the rotation should animate over time, in degrees
		[HideInInspector] public AnimationCurve AnimateRotationX = null;
		/// [DEPRECATED] how the y part of the rotation should animate over time, in degrees
		[HideInInspector] public AnimationCurve AnimateRotationY = null;
		/// [DEPRECATED] how the z part of the rotation should animate over time, in degrees
		[HideInInspector] public AnimationCurve AnimateRotationZ = null;
		/// [DEPRECATED] the animation curve to use when animating to destination (individual x,y,z curves above won't be used)
		[HideInInspector] public AnimationCurve ToDestinationCurve = null;

		protected Quaternion _initialRotation;
		protected Vector3 _initialToDestinationAngles;
		protected Quaternion _destinationRotation;
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
				GetInitialRotation();
			}
		}

		/// <summary>
		/// Stores initial rotation for future use
		/// </summary>
		protected virtual void GetInitialRotation()
		{
			_initialRotation = (RotationSpace == Space.World) ? AnimateRotationTarget.rotation : AnimateRotationTarget.localRotation;
			_initialToDestinationAngles = _initialRotation.eulerAngles;
		}
		
		/// <summary>
		/// In speed mode, computes the duration the feedback should last based on the distance between the two points and the speed
		/// </summary>
		/// <param name="pointA"></param>
		/// <param name="pointB"></param>
		/// <param name="duration"></param>
		/// <returns></returns>
		protected virtual float HandleSpeedMode(Quaternion pointA, Quaternion pointB, float duration)
		{
			if (MovementMode != MovementModes.Speed)
			{
				return duration;
			}

			float distance = 2 * Mathf.Acos(Quaternion.Dot(pointA, pointB));
			return distance / AnimatePositionSpeed;
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
				if ((Mode == Modes.Absolute) || (Mode == Modes.Additive))
				{
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (DetermineRotationOnPlay && NormalPlayDirection) { GetInitialRotation(); }
					ClearCoroutine();
					_coroutine = Owner.StartCoroutine(AnimateRotation(AnimateRotationTarget, Vector3.zero, FeedbackDuration, AnimateRotationTweenX, AnimateRotationTweenY, AnimateRotationTweenZ, RemapCurveZero * intensityMultiplier, RemapCurveOne * intensityMultiplier));
				}
				else if (Mode == Modes.ToDestination)
				{
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (DetermineRotationOnPlay && NormalPlayDirection) { GetInitialRotation(); }
					ClearCoroutine();
					_coroutine = Owner.StartCoroutine(RotateToDestination());
				}
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
		/// A coroutine used to rotate the target to its destination rotation
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator RotateToDestination()
		{
			if (AnimateRotationTarget == null)
			{
				yield break;
			}

			if ((AnimateRotationTweenX == null) || (AnimateRotationTweenY == null) || (AnimateRotationTweenZ == null))
			{
				yield break;
			}

			if (FeedbackDuration == 0f)
			{
				yield break;
			}

			Vector3 tempAngles = DestinationAngles;
			if (ToDestinationTransform != null)
			{
				tempAngles = ToDestinationTransform.eulerAngles;
			}
			
			Vector3 destinationAngles = NormalPlayDirection ? tempAngles : _initialToDestinationAngles;
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;

			_initialRotation = AnimateRotationTarget.transform.rotation;
			if (ToDestinationSpace == Space.Self)
			{
				AnimateRotationTarget.transform.localRotation = Quaternion.Euler(destinationAngles);
			}
			else
			{
				AnimateRotationTarget.transform.rotation = Quaternion.Euler(destinationAngles);
			}
            
			_destinationRotation = AnimateRotationTarget.transform.rotation;
			AnimateRotationTarget.transform.rotation = _initialRotation;
			IsPlaying = true;
			
			float duration = HandleSpeedMode(_initialRotation, _destinationRotation, FeedbackDuration);
            
			while ((journey >= 0) && (journey <= duration) && (duration > 0))
			{
				float percent = Mathf.Clamp01(journey / duration);
				percent = ToDestinationTween.Evaluate(percent);

				Quaternion newRotation = Quaternion.LerpUnclamped(_initialRotation, _destinationRotation, percent);
				AnimateRotationTarget.transform.rotation = newRotation;

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
                
				yield return null;
			}

			if (ToDestinationSpace == Space.Self)
			{
				AnimateRotationTarget.transform.localRotation = Quaternion.Euler(destinationAngles);
			}
			else
			{
				AnimateRotationTarget.transform.rotation = Quaternion.Euler(destinationAngles);
			}
			IsPlaying = false;
			_coroutine = null;
			yield break;
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
			MMTweenType curveX,
			MMTweenType curveY,
			MMTweenType curveZ,
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

			if (Mode == Modes.Additive)
			{
				_initialRotation = (RotationSpace == Space.World) ? targetTransform.rotation : targetTransform.localRotation;
			}

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
		protected virtual void ApplyRotation(Transform targetTransform, float remapZero, float remapOne, MMTweenType curveX, MMTweenType curveY, MMTweenType curveZ, float percent)
		{
			if (RotationSpace == Space.World)
			{
				targetTransform.transform.rotation = _initialRotation;    
			}
			else
			{
				targetTransform.transform.localRotation = _initialRotation;
			}

			if (AnimateX)
			{
				float x = MMTween.Tween(percent, 0f, 1f, remapZero, remapOne, curveX);
				targetTransform.Rotate(Vector3.right, x, RotationSpace);
			}
			if (AnimateY)
			{
				float y = MMTween.Tween(percent, 0f, 1f, remapZero, remapOne, curveY);
				targetTransform.Rotate(Vector3.up, y, RotationSpace);
			}
			if (AnimateZ)
			{
				float z = MMTween.Tween(percent, 0f, 1f, remapZero, remapOne, curveZ);
				targetTransform.Rotate(Vector3.forward, z, RotationSpace);
			}
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
			MMFeedbacksHelpers.MigrateCurve(AnimateRotationX, AnimateRotationTweenX, Owner);
			MMFeedbacksHelpers.MigrateCurve(AnimateRotationY, AnimateRotationTweenY, Owner);
			MMFeedbacksHelpers.MigrateCurve(AnimateRotationZ, AnimateRotationTweenZ, Owner);
			MMFeedbacksHelpers.MigrateCurve(ToDestinationCurve, ToDestinationTween, Owner);
			
			if (string.IsNullOrEmpty(AnimateRotationTweenX.ConditionPropertyName))
			{
				AnimateRotationTweenX.ConditionPropertyName = "AnimateX";
				AnimateRotationTweenY.ConditionPropertyName = "AnimateY";
				AnimateRotationTweenZ.ConditionPropertyName = "AnimateZ";
				ToDestinationTween.EnumConditionPropertyName = "Mode";
				ToDestinationTween.EnumConditions = new bool[32];
				ToDestinationTween.EnumConditions[(int)Modes.ToDestination] = true;
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

			if (RotationSpace == Space.World)
			{
				AnimateRotationTarget.rotation = _initialRotation;
			}
			else
			{
				AnimateRotationTarget.localRotation= _initialRotation;	
			}
		}
	}
}
