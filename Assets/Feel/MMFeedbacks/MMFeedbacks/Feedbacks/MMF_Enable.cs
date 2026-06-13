using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// Turns an object active or inactive at the various stages of the feedback
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你 change the state of a behaviour on a target GameObject from active to inactive (or the opposite), on init, play, stop or reset. " +
	              "For each of these you can specify if you want to force a state (enabled or disabled), or 切换 it (enabled becomes disabled, disabled becomes enabled).")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("GameObject/Enable Behaviour")]
	public class MMF_Enable : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.GameObjectColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetBehaviour == null); }
		public override string RequiredTargetText { get { return TargetBehaviour != null ? TargetBehaviour.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置a TargetBehaviour才能正常工作。你可以在下方进行设置。"; } }
		#endif

		/// the possible effects the feedback can have on the target object's status 
		public enum PossibleStates { Enabled, Disabled, Toggle }

		[MMFInspectorGroup("Enable Target Monobehaviour", true, 86, true)]
		/// the gameobject we want to change the active state of
		[Tooltip("要修改激活状态的游戏对象")]
		public Behaviour TargetBehaviour;
		/// a list of extra gameobjects we want to change the active state of
		[Tooltip("我们想要更改其活动状态的额外游戏对象的列表")]
		public List<Behaviour> ExtraTargetBehaviours;
		/// whether or not we should alter the state of the target object on init
		[Tooltip("我们是否应该改变 init 上目标对象的状态")]
		public bool SetStateOnInit = false;
		/// how to change the state on init
		[MMFCondition("SetStateOnInit", true)]
		[Tooltip("如何更改 init 上的状态")]
		public PossibleStates StateOnInit = PossibleStates.Disabled;
		/// whether or not we should alter the state of the target object on play
		[Tooltip("我们是否应该改变游戏中目标对象的状态")]
		public bool SetStateOnPlay = false;
		/// how to change the state on play
		[MMFCondition("SetStateOnPlay", true)]
		[Tooltip("如何更改播放状态")]
		public PossibleStates StateOnPlay = PossibleStates.Disabled;
		/// whether or not we should alter the state of the target object on stop
		[Tooltip("停止时是否修改目标对象的状态")]
		public bool SetStateOnStop = false;
		/// how to change the state on stop
		[Tooltip("如何更改停止时的状态")]
		[MMFCondition("SetStateOnStop", true)]
		public PossibleStates StateOnStop = PossibleStates.Disabled;
		/// whether or not we should alter the state of the target object on reset
		[Tooltip("我们是否应该在重置时改变目标对象的状态")]
		public bool SetStateOnReset = false;
		/// how to change the state on reset
		[Tooltip("如何更改重置时的状态")]
		[MMFCondition("SetStateOnReset", true)]
		public PossibleStates StateOnReset = PossibleStates.Disabled;

		protected bool _initialState;
		
		/// <summary>
		/// On init we change the state of our Behaviour if needed
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			if (Active && (TargetBehaviour != null))
			{
				if (SetStateOnInit)
				{
					SetStatus(StateOnInit);
				}
			}
		}

		/// <summary>
		/// On Play we change the state of our Behaviour if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetBehaviour == null))
			{
				return;
			}
			if (SetStateOnPlay)
			{
				SetStatus(StateOnPlay);
			}
		}

		/// <summary>
		/// On Stop we change the state of our Behaviour if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetBehaviour == null))
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);

			if (SetStateOnStop)
			{
				SetStatus(StateOnStop);
			}
		}

		/// <summary>
		/// On Reset we change the state of our Behaviour if needed
		/// </summary>
		protected override void CustomReset()
		{
			base.CustomReset();

			if (InCooldown)
			{
				return;
			}
            
			if (!Active || !FeedbackTypeAuthorized || (TargetBehaviour == null))
			{
				return;
			}
            
			if (SetStateOnReset)
			{
				SetStatus(StateOnReset);
			}
		}

		/// <summary>
		/// Changes the status of the Behaviour
		/// </summary>
		/// <param name="state"></param>
		protected virtual void SetStatus(PossibleStates state)
		{
			SetStatus(state, TargetBehaviour);
			foreach (Behaviour extra in ExtraTargetBehaviours)
			{
				SetStatus(state, extra);
			}
		}

		/// <summary>
		/// Sets the specified status on the target Behaviour
		/// </summary>
		/// <param name="state"></param>
		/// <param name="target"></param>
		protected virtual void SetStatus(PossibleStates state, Behaviour target)
		{
			_initialState = target.enabled;
			switch (state)
			{
				case PossibleStates.Enabled:
					target.enabled = NormalPlayDirection ? true : false;
					break;
				case PossibleStates.Disabled:
					target.enabled = NormalPlayDirection ? false : true;
					break;
				case PossibleStates.Toggle:
					target.enabled = !target.enabled;
					break;
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
			
			TargetBehaviour.enabled = _initialState;
			foreach (Behaviour extra in ExtraTargetBehaviours)
			{
				extra.enabled = _initialState;
			}
		}
	}
}


