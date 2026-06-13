using MoreMountains.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	public class MMF_FeedbackBaseTarget
	{
		/// the receiver to write the level to
		public MMPropertyReceiver Target;
		/// the curve to tween the intensity on
		public MMTweenType LevelCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));
		/// the value to remap the intensity curve's 0 to
		public float RemapLevelZero = 0f;
		/// the value to remap the intensity curve's 1 to
		public float RemapLevelOne = 1f;
		/// the value to move the intensity to in instant mode
		public float InstantLevel;
		/// the initial value for this level
		public float InitialLevel;
		/// the level to reach in ToDestination mode
		public float ToDestinationLevel;
	}
    
	public abstract class MMF_FeedbackBase : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		
		/// the possible modes for this feedback
		public enum Modes { OverTime, Instant, ToDestination } 
        
		[MMFInspectorGroup("Mode", true, 64)]
		/// whether the feedback should affect the target property instantly or over a period of time
		[Tooltip("该反馈应立即影响目标属性，还是在一段时间内逐步生效")]
		public Modes Mode = Modes.OverTime;
		/// how long the target property should change over time
		[Tooltip("目标属性在渐变模式下持续变化的时间")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime, (int)Modes.ToDestination)]
		public float Duration = 0.2f;
		/// whether or not that target property should be turned off on start
		[Tooltip("开始时是否应关闭该目标属性")]
		public bool StartsOff = false;
		/// whether or not that target property should be turned off once the feedback is done playing
		[Tooltip("反馈播放完成后是否应关闭该目标属性")]
		public bool EndsOff = false;
		/// whether or not the values should be relative or not
		[Tooltip("数值是否采用相对模式")]
		public bool RelativeValues = false;
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("若启用，即使该反馈仍在执行中，再次调用也会立刻重新触发；若关闭，则当前一次播放结束前会阻止新的 Play 调用。")] 
		public bool AllowAdditivePlays = false;
		/// if this is true, the target object will be disabled on stop
		[Tooltip("若启用，调用 Stop 时会禁用目标对象")]
		public bool DisableOnStop = false;
		/// if this is true, this feedback will only play if its target is active in hierarchy
		[Tooltip("若启用，仅当目标对象在 Hierarchy 中处于激活状态时，此反馈才会播放")]
		public bool OnlyPlayIfTargetIsActive = false;
		/// the duration of this feedback is the duration of the target property, or 0 if instant
		public override float FeedbackDuration { get { return (Mode == Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { if (Mode != Modes.Instant) { Duration = value; } } }
		public override bool HasRandomness => true;
		public override bool HasCustomInspectors => true;

		protected List<MMF_FeedbackBaseTarget> _targets;
		protected Coroutine _coroutine = null;

		/// <summary>
		/// On init we turn the target property off if needed
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			PrepareTargets();

			if (Active)
			{
				if (StartsOff)
				{
					Turn(false);
				}
			}
		}

		/// <summary>
		/// Creates a new list, fills the targets, and initializes them
		/// </summary>
		public virtual void PrepareTargets()
		{
			_targets = new List<MMF_FeedbackBaseTarget>();
			FillTargets();
			InitializeTargets();
		}

		/// <summary>
		/// On validate (if a value has changed in the inspector), we reinitialize what needs to be
		/// </summary>
		public override void OnValidate()
		{
			base.OnValidate();
			PrepareTargets();
		}

		/// <summary>
		/// Fills our list of targets, meant to be extended
		/// </summary>
		protected abstract void FillTargets();

		/// <summary>
		/// Initializes each target in the list
		/// </summary>
		protected virtual void InitializeTargets()
		{
			if (_targets.Count == 0)
			{
				return;
			}

			foreach(MMF_FeedbackBaseTarget target in _targets)
			{
				target.Target.Initialization(Owner.gameObject);
				target.InitialLevel = target.Target.GetLevel();;
			}
		}

		/// <summary>
		/// On Play we turn our target property on and start an over time coroutine if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (Active && FeedbackTypeAuthorized)
			{
				if (!CanPlay())
				{
					return;
				}
	            
				Turn(true);    
	            
				switch (Mode)
				{
					case Modes.Instant:
						Instant();
						break;
					case Modes.OverTime:
						if (!AllowAdditivePlays && (_coroutine != null))
						{
							return;
						}
						if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
						_coroutine = Owner.StartCoroutine(UpdateValueOverTimeCo(feedbacksIntensity, position));
						break;
					case Modes.ToDestination:
						if (!AllowAdditivePlays && (_coroutine != null))
						{
							return;
						}
						if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
						_coroutine = Owner.StartCoroutine(UpdateValueToDestinationCo(feedbacksIntensity, position));
						break;
				}
			}
		}

		/// <summary>
		/// Plays an instant feedback
		/// </summary>
		protected virtual void Instant()
		{
			if (_targets.Count == 0)
			{
				return;
			}

			foreach (MMF_FeedbackBaseTarget target in _targets)
			{
				float newLevel = NormalPlayDirection ? target.InstantLevel : target.InitialLevel; 
				target.Target.SetLevel(newLevel);
			}
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
			if (_targets.Count == 0)
			{
				return;
			}

			foreach (MMF_FeedbackBaseTarget target in _targets)
			{
				target.Target.SetLevel(target.InitialLevel);
			}
		}

		/// <summary>
		/// This coroutine will modify the values on the target property
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator UpdateValueOverTimeCo(float feedbacksIntensity, Vector3 position)
		{
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);
				SetValues(remappedTime, feedbacksIntensity, position);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetValues(FinalNormalizedTime, feedbacksIntensity, position);
			if (EndsOff)
			{
				Turn(false);
			}
			IsPlaying = false;
			_coroutine = null;
			yield return null;
		}

		protected virtual IEnumerator UpdateValueToDestinationCo(float feedbacksIntensity, Vector3 position)
		{
			InitializeTargets();
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);
				SetValues(remappedTime, feedbacksIntensity, position, Modes.ToDestination);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetValues(FinalNormalizedTime, feedbacksIntensity, position, Modes.ToDestination);
			if (EndsOff)
			{
				Turn(false);
			}
			IsPlaying = false;
			_coroutine = null;
			yield return null;
		}

		/// <summary>
		/// Sets the various values on the target property on a specified time (between 0 and 1)
		/// </summary>
		/// <param name="time"></param>
		protected virtual void SetValues(float time, float feedbacksIntensity, Vector3 position, Modes mode = Modes.OverTime)
		{
			if (_targets.Count == 0)
			{
				return;
			}
            
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
            
			foreach (MMF_FeedbackBaseTarget target in _targets)
			{
				float intensity = MMTween.Tween(time, 0f, 1f, target.RemapLevelZero, target.RemapLevelOne, target.LevelCurve);
				
				if (mode == Modes.ToDestination)
				{
					intensity = MMTween.Tween(time, 0f, 1f, target.InitialLevel, target.ToDestinationLevel, target.LevelCurve);
				}

				target.Target.SetLevel(intensity * intensityMultiplier);
			}
		}

		/// <summary>
		/// Turns the target property object off on stop if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			base.CustomStopFeedback(position, feedbacksIntensity);
			if (Active)
			{
				if (_coroutine != null)
				{
					Owner.StopCoroutine(_coroutine);
					_coroutine = null;
				}
				IsPlaying = false;
				if (DisableOnStop)
				{
					Turn(false);    
				}
			}
		}

		/// <summary>
		/// Turns the target object on or off
		/// </summary>
		/// <param name="status"></param>
		protected virtual void Turn(bool status)
		{
			if (_targets.Count == 0)
			{
				return;
			}
			foreach (MMF_FeedbackBaseTarget target in _targets)
			{
				if (target.Target.TargetComponent.gameObject != null)
				{
					target.Target.TargetComponent.gameObject.SetActive(status);
				}
			}
		}

		/// <summary>
		/// Checks whether or not this feedback should play according to the defined settings
		/// </summary>
		/// <returns></returns>
		protected virtual bool CanPlay()
		{
			if (_targets.Count == 0)
			{
				return false;
			}
			foreach (MMF_FeedbackBaseTarget target in _targets)
			{
				if (OnlyPlayIfTargetIsActive)
				{
					if (!target.Target.TargetComponent.gameObject.activeInHierarchy)
					{
						return false;
					}    
				}
			}

			return true;
		}
	}
}