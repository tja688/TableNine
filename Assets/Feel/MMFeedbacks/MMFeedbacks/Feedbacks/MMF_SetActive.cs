using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// Turns an object active or inactive at the various stages of the feedback
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你在 init、play、stop 或 reset 时切换目标 GameObject 的激活状态。每个阶段都可以选择强制设为启用/禁用，或执行切换；若选择切换，则启用会变为禁用，禁用会变为启用。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("GameObject/Set Active")]
	public class MMF_SetActive : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor => MMFeedbacksInspectorColors.GameObjectColor; 
		public override bool EvaluateRequiresSetup() => (TargetGameObject == null); 
		public override string RequiredTargetText => TargetGameObject != null ? TargetGameObject.name : "";
		public override string RequiredTargetTextExtra
		{
			get
			{
				if (ExtraTargetGameObjects == null)
				{
					return "";
				}
				if (ExtraTargetGameObjects.Count > 0)
				{
					return " (+"+ExtraTargetGameObjects.Count+")";
				}
				return "";
			}
		}
		public override string RequiresSetupText => "This feedback requires that a TargetGameObject be set to be able to work properly. You can set one below."; 
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetGameObject = FindAutomatedTargetGameObject();

		/// the possible effects the feedback can have on the target object's status 
		public enum PossibleStates { Active, Inactive, Toggle }
        
		[MMFInspectorGroup("Set Active Target", true, 12, true)]
		/// the gameobject we want to change the active state of
		[Tooltip("要修改激活状态的游戏对象")]
		public GameObject TargetGameObject;
		/// a list of extra gameobjects we want to change the active state of
		[Tooltip("我们想要更改其活动状态的额外游戏对象的列表")]
		public List<GameObject> ExtraTargetGameObjects = new List<GameObject>();
		/// if this is true, the applied state will be the one you select below. if this is false the applied state will be impacted by the play direction (inverting the choice set below if playing in reverse)
		[Tooltip("若开启一项，应用的状态将是您在下面选择的状态。如果为 false，则应用的状态将受到播放方向的影响（如果反向播放，则反转下面的选择集）")]
		public bool IgnorePlayDirection = false;
        
		[MMFInspectorGroup("States", true, 14)]
		/// whether or not we should alter the state of the target object on init
		[Tooltip("我们是否应该改变 init 上目标对象的状态")]
		public bool SetStateOnInit = false;
		[MMFCondition("SetStateOnInit", true)]
		/// how to change the state on init
		[Tooltip("如何更改 init 上的状态")]
		public PossibleStates StateOnInit = PossibleStates.Inactive;
		/// whether or not we should alter the state of the target object on play
		[Tooltip("我们是否应该改变游戏中目标对象的状态")]
		public bool SetStateOnPlay = false;
		/// how to change the state on play
		[Tooltip("如何更改播放状态")]
		[MMFCondition("SetStateOnPlay", true)]
		public PossibleStates StateOnPlay = PossibleStates.Inactive;
		/// whether or not we should alter the state of the target object on stop
		[Tooltip("停止时是否修改目标对象的状态")]
		public bool SetStateOnStop = false;
		/// how to change the state on stop
		[Tooltip("如何更改停止时的状态")]
		[MMFCondition("SetStateOnStop", true)]
		public PossibleStates StateOnStop = PossibleStates.Inactive;
		/// whether or not we should alter the state of the target object on reset
		[Tooltip("我们是否应该在重置时改变目标对象的状态")]
		public bool SetStateOnReset = false;
		/// how to change the state on reset
		[Tooltip("如何更改重置时的状态")]
		[MMFCondition("SetStateOnReset", true)]
		public PossibleStates StateOnReset = PossibleStates.Inactive;
		/// whether or not we should alter the state of the target object on skip
		[Tooltip("我们是否应该在跳过时改变目标对象的状态")]
		public bool SetStateOnSkip = false;
		/// how to change the state on skip
		[Tooltip("如何设置跳过时的状态")]
		[MMFCondition("SetStateOnSkip", true)]
		public PossibleStates StateOnSkip = PossibleStates.Inactive;
		/// whether or not we should alter the state of the target object when the player this feedback belongs to is done playing all its feedbacks
		[Tooltip("当该反馈所属的玩家播放完所有反馈后，我们是否应该改变目标对象的状态")]
		public bool SetStateOnPlayerComplete = false;
		/// how to change the state on player complete
		[Tooltip("如何更改播放器完成时的状态")]
		[MMFCondition("SetStateOnPlayerComplete", true)]
		public PossibleStates StateOnPlayerComplete = PossibleStates.Inactive;

		protected bool _initialState;
		protected List<bool> _initialStates;
        
		/// <summary>
		/// On init we change the state of our object if needed
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			_initialStates = new List<bool>(ExtraTargetGameObjects.Count);
			
			if (Active && (TargetGameObject != null))
			{
				_initialState = TargetGameObject.activeInHierarchy;

				if (ExtraTargetGameObjects != null)
				{
					for (int i = 0; i < ExtraTargetGameObjects.Count; i++)
					{
						_initialStates.Add(ExtraTargetGameObjects[i].activeInHierarchy);
					}	
				}
				
				if (SetStateOnInit)
				{
					SetStatus(StateOnInit);
				}
			}
		}

		/// <summary>
		/// On Play we change the state of our object if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetGameObject == null))
			{
				return;
			}
            
			if (SetStateOnPlay)
			{
				SetStatus(StateOnPlay);
			}
		}

		/// <summary>
		/// On Stop we change the state of our object if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			base.CustomStopFeedback(position, feedbacksIntensity);

			if (Active && FeedbackTypeAuthorized && (TargetGameObject != null))
			{
				if (SetStateOnStop)
				{
					SetStatus(StateOnStop);
				}
			}
		}

		/// <summary>
		/// On Reset we change the state of our object if needed
		/// </summary>
		protected override void CustomReset()
		{
			base.CustomReset();

			if (InCooldown)
			{
				return;
			}

			if (Active && FeedbackTypeAuthorized && (TargetGameObject != null))
			{
				if (SetStateOnReset)
				{
					SetStatus(StateOnReset);
				}
			}
		}
		
		/// <summary>
		/// On PlayerComplete we change the state of our object if needed
		/// </summary>
		protected override void CustomPlayerComplete()
		{
			base.CustomPlayerComplete();

			if (InCooldown)
			{
				return;
			}

			if (Active && FeedbackTypeAuthorized && (TargetGameObject != null))
			{
				if (SetStateOnPlayerComplete)
				{
					SetStatus(StateOnPlayerComplete);
				}
			}
		}
		
		
		/// <summary>
		/// On Skip, changes the state of our target object if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomSkipToTheEnd(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			base.CustomSkipToTheEnd(position, feedbacksIntensity);

			if (InCooldown)
			{
				return;
			}

			if (Active && FeedbackTypeAuthorized && (TargetGameObject != null))
			{
				if (SetStateOnSkip)
				{
					SetStatus(StateOnSkip);
				}
			}
		}

		/// <summary>
		/// Changes the status of the object
		/// </summary>
		/// <param name="state"></param>
		protected virtual void SetStatus(PossibleStates state)
		{
			bool newState = false;
			switch (state)
			{
				case PossibleStates.Active:
					newState = NormalPlayDirection ? true : false;
					if (IgnorePlayDirection)
					{
						newState = true;
					}
					break;
				case PossibleStates.Inactive:
					newState = NormalPlayDirection ? false : true;
					if (IgnorePlayDirection)
					{
						newState = false;
					}
					break;
				case PossibleStates.Toggle:
					newState = !TargetGameObject.activeInHierarchy;
					break;
			}
			
			ApplyStatus(TargetGameObject, newState);
			foreach (GameObject go in ExtraTargetGameObjects)
			{
				ApplyStatus(go, newState);
			}
		}

		/// <summary>
		/// Applies the status to the target game object
		/// </summary>
		/// <param name="target"></param>
		/// <param name="newState"></param>
		protected virtual void ApplyStatus(GameObject target, bool newState)
		{
			target.SetActive(newState);
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
			TargetGameObject.SetActive(_initialState);
			for (int i = 0; i < ExtraTargetGameObjects.Count; i++)
			{
				ExtraTargetGameObjects[i].SetActive(_initialStates[i]);
			}
		}
	}
}

