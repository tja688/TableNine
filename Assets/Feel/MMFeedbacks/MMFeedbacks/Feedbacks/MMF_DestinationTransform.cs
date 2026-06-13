using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you animate the position/rotation/scale of a target transform to match the one of a destination transform.
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你对目标 Transform 的位置、旋转与缩放进行动画，使其匹配目标 Destination Transform。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Transform/Destination")]
	public class MMF_DestinationTransform : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// the possible timescales this feedback can animate on
		public enum TimeScales { Scaled, Unscaled }
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TransformColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetTransform == null) || (Destination == null); }
		public override string RequiredTargetText { get { return TargetTransform != null ? TargetTransform.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置a TargetTransform and a Destination才能正常工作。你可以在下方进行设置。"; } }
		public override bool HasCustomInspectors { get { return true; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetTransform = FindAutomatedTarget<Transform>();

		[MMFInspectorGroup("Target to animate", true, 61, true)]
		/// the target transform we want to animate properties on
		[Tooltip("要对其属性进行动画的目标 Transform")]
		public Transform TargetTransform;
        
		/// whether or not we want to force an origin transform. If not, the current position of the target transform will be used as origin instead
		[Tooltip("是否强制指定一个起始 Transform。若关闭，将改用目标 Transform 的当前位置作为起点")]
		public bool ForceOrigin = false;
		/// the transform to use as origin in ForceOrigin mode
		[Tooltip("在 ForceOrigin 模式下作为起点使用的 Transform")]
		[MMFCondition("ForceOrigin", true)] 
		public Transform Origin;
		/// the destination transform whose properties we want to match 
		[Tooltip("要匹配其属性的目标 目的地 转换")]
		public Transform Destination;
        
		[MMFInspectorGroup("Transition", true, 63)]
		/// a global curve to animate all properties on, unless dedicated ones are specified
		[Tooltip("全局动画曲线：在未启用独立曲线时，位置/旋转/缩放都会使用它")]
		public MMTweenType GlobalAnimationTween = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));
		/// the duration of the transition, in seconds
		[Tooltip("持续时间段落，单位为秒")]
		public float Duration = 0.2f;
		/// if this is true, the destination will be updated every frame, allowing for dynamic changes to the destination transform, otherwise the destination will be cached on init and not updated after that
		[Tooltip("若开启此项，Destination 会每帧更新，适合目标动态变化；若关闭，则仅在初始化时缓存一次，后续不再更新。")]
		public bool UpdateDestinationEveryFrame = false;

		[MMFInspectorGroup("Axis Locks", true, 64)]
        
		/// whether or not to animate the X position
		[Tooltip("是否动画 X 位置")]
		public bool AnimatePositionX = true;
		/// whether or not to animate the Y position
		[Tooltip("是否为 Y 位置设置动画")]
		public bool AnimatePositionY = true;
		/// whether or not to animate the Z position
		[Tooltip("是否为 Z 位置设置动画")]
		public bool AnimatePositionZ = true;
		/// whether or not to animate the X rotation
		[Tooltip("是否为 X 旋转设置动画")]
		public bool AnimateRotationX = true;
		/// whether or not to animate the Y rotation
		[Tooltip("是否设置 Y 旋转动画")]
		public bool AnimateRotationY = true;
		/// whether or not to animate the Z rotation
		[Tooltip("是否动画 Z 旋转")]
		public bool AnimateRotationZ = true;
		/// whether or not to animate the W rotation
		[Tooltip("是否动画 W 旋转")]
		public bool AnimateRotationW = true;
		/// whether or not to animate the X scale
		[Tooltip("是否对 X 比例进行动画处理")]
		public bool AnimateScaleX = true;
		/// whether or not to animate the Y scale
		[Tooltip("是否对 Y 比例进行动画处理")]
		public bool AnimateScaleY = true;
		/// whether or not to animate the Z scale
		[Tooltip("是否对 Z 比例进行动画处理")]
		public bool AnimateScaleZ = true;

		[MMFInspectorGroup("Separate Curves", true, 65)]
		/// whether or not to use a separate animation curve to animate the position
		[Tooltip("是否使用独立曲线来控制位置动画")]
		public bool SeparatePositionCurve = false;
		/// the curve to use to animate the position on
		[Tooltip("用于位置动画的曲线")]
		public MMTweenType AnimatePositionTween = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)), "SeparatePositionCurve");
        
		/// whether or not to use a separate animation curve to animate the rotation
		[Tooltip("是否使用独立曲线来控制旋转动画")]
		public bool SeparateRotationCurve = false;
		/// the curve to use to animate the rotation on
		[Tooltip("用于旋转动画的曲线")]
		public MMTweenType AnimateRotationTween = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)), "SeparateRotationCurve");
        
		/// whether or not to use a separate animation curve to animate the scale
		[Tooltip("是否使用独立曲线来控制缩放动画")]
		public bool SeparateScaleCurve = false;
		/// the curve to use to animate the scale on
		[Tooltip("用于缩放动画的曲线")] 
		public MMTweenType AnimateScaleTween = new MMTweenType( new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)), "SeparateScaleCurve");
        
		/// the duration of this feedback is the duration of the movement
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(Duration); } set { Duration = value; } }

		/// a global curve to animate all properties on, unless dedicated ones are specified
		[HideInInspector] public AnimationCurve GlobalAnimationCurve = null;
		/// the curve to use to animate the position on
		[HideInInspector] public AnimationCurve AnimateScaleCurve = null;
		/// the curve to use to animate the rotation on
		[HideInInspector] public AnimationCurve AnimatePositionCurve = null;
		/// the curve to use to animate the scale on
		[HideInInspector] public AnimationCurve AnimateRotationCurve = null;
		
		protected Coroutine _coroutine;
		protected Vector3 _newPosition;
		protected Quaternion _newRotation;
		protected Vector3 _newScale;
		protected Vector3 _pointAPosition;
		protected Vector3 _pointBPosition;
		protected Quaternion _pointARotation;
		protected Quaternion _pointBRotation;
		protected Vector3 _pointAScale;
		protected Vector3 _pointBScale;
		protected MMTweenType _animationTweenType;

		protected Vector3 _initialPosition;
		protected Vector3 _initialScale;
		protected Quaternion _initialRotation;
        
		/// <summary>
		/// On Play we animate the pos/rotation/scale of the target transform towards its destination
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetTransform == null))
			{
				return;
			}
			if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
			_coroutine = Owner.StartCoroutine(AnimateToDestination());
		}

		/// <summary>
		/// A coroutine used to animate the pos/rotation/scale of the target transform towards its destination
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator AnimateToDestination()
		{
			_initialPosition = TargetTransform.position;
			_initialRotation = TargetTransform.rotation;
			_initialScale = TargetTransform.localScale;

			_pointAPosition = ForceOrigin ? Origin.transform.position : TargetTransform.position;
			_pointARotation = ForceOrigin ? Origin.transform.rotation : TargetTransform.rotation;
			_pointAScale = ForceOrigin ? Origin.transform.localScale : TargetTransform.localScale;
			
			CacheDestinationValues();

			IsPlaying = true;
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				if (UpdateDestinationEveryFrame)
				{
					CacheDestinationValues();
				}
				float percent = Mathf.Clamp01(journey / FeedbackDuration);
				ChangeTransformValues(percent);
				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}

			// set final position
			ChangeTransformValues(1f);
			
			IsPlaying = false;
			_coroutine = null;
			yield break;
		}

		protected virtual void CacheDestinationValues()
		{
			_pointBPosition = Destination.transform.position;

			if (!AnimatePositionX) { _pointAPosition.x = TargetTransform.position.x; _pointBPosition.x = _pointAPosition.x; }
			if (!AnimatePositionY) { _pointAPosition.y = TargetTransform.position.y; _pointBPosition.y = _pointAPosition.y; }
			if (!AnimatePositionZ) { _pointAPosition.z = TargetTransform.position.z; _pointBPosition.z = _pointAPosition.z; }
            
			_pointBRotation = Destination.transform.rotation;
            
			if (!AnimateRotationX) { _pointARotation.x = TargetTransform.rotation.x; _pointBRotation.x = _pointARotation.x; }
			if (!AnimateRotationY) { _pointARotation.y = TargetTransform.rotation.y; _pointBRotation.y = _pointARotation.y; }
			if (!AnimateRotationZ) { _pointARotation.z = TargetTransform.rotation.z; _pointBRotation.z = _pointARotation.z; }
			if (!AnimateRotationW) { _pointARotation.w = TargetTransform.rotation.w; _pointBRotation.w = _pointARotation.w; }

			_pointBScale = Destination.transform.localScale;
            
			if (!AnimateScaleX) { _pointAScale.x = TargetTransform.localScale.x; _pointBScale.x = _pointAScale.x; }
			if (!AnimateScaleY) { _pointAScale.y = TargetTransform.localScale.y; _pointBScale.y = _pointAScale.y; }
			if (!AnimateScaleZ) { _pointAScale.z = TargetTransform.localScale.z; _pointBScale.z = _pointAScale.z; }
		}

		/// <summary>
		/// Computes the new position, rotation and scale for our transform, and applies it to the transform
		/// </summary>
		/// <param name="percent"></param>
		protected virtual void ChangeTransformValues(float percent)
		{
			_animationTweenType = SeparatePositionCurve ? AnimatePositionTween : GlobalAnimationTween;
			_newPosition = Vector3.LerpUnclamped(_pointAPosition, _pointBPosition, _animationTweenType.Evaluate(percent));
                
			_animationTweenType = SeparateRotationCurve ? AnimateRotationTween : GlobalAnimationTween;
			_newRotation = Quaternion.LerpUnclamped(_pointARotation, _pointBRotation, _animationTweenType.Evaluate(percent));
                
			_animationTweenType = SeparateScaleCurve ? AnimateScaleTween : GlobalAnimationTween;
			_newScale = Vector3.LerpUnclamped(_pointAScale, _pointBScale, _animationTweenType.Evaluate(percent));
			
			TargetTransform.position = _newPosition;
			TargetTransform.rotation = _newRotation;
			TargetTransform.localScale = _newScale;
		}

		/// <summary>
		/// On Stop we stop our coroutine if needed
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
			IsPlaying = false;
            
			if ((TargetTransform != null) && (_coroutine != null))
			{
				Owner.StopCoroutine(_coroutine);
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
			TargetTransform.position = _initialPosition;
			TargetTransform.rotation = _initialRotation;
			TargetTransform.localScale = _initialScale;
		}
		
		/// <summary>
		/// On Validate, we migrate our deprecated animation curves to our tween types if needed
		/// </summary>
		public override void OnValidate()
		{
			base.OnValidate();
			MMFeedbacksHelpers.MigrateCurve(GlobalAnimationCurve, GlobalAnimationTween, Owner);
			MMFeedbacksHelpers.MigrateCurve(AnimatePositionCurve, AnimatePositionTween, Owner);
			MMFeedbacksHelpers.MigrateCurve(AnimateRotationCurve, AnimateRotationTween, Owner);
			MMFeedbacksHelpers.MigrateCurve(AnimateScaleCurve, AnimateScaleTween, Owner);
			if (string.IsNullOrEmpty(AnimatePositionTween.ConditionPropertyName))
			{
				AnimatePositionTween.ConditionPropertyName = "SeparatePositionCurve";
				AnimateRotationTween.ConditionPropertyName = "SeparateRotationCurve";
				AnimateScaleTween.ConditionPropertyName = "SeparateScaleCurve";
			}
		}
	}    
}


