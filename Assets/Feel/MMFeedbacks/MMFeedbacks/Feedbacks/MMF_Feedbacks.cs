using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback allows you to trigger a target MMF_Player, or any MMF_Player on the specified Channel within a certain range. You'll need an MMFeedbacksShaker on them.
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你触发指定的 MMF_Player，或触发指定通道且处于一定范围内的任意 MMF_Player。被触发方需要挂有 MMFeedbacksShaker。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Feedbacks/Feedbacks Player")]
	public class MMF_Feedbacks : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.FeedbacksColor; } }
		public override string RequiredTargetText => RequiredChannelText;
		#endif
		/// the duration of this feedback is the duration of our target feedback
		public override float FeedbackDuration 
		{
			get
			{
				if (TargetFeedbacks == Owner)
				{
					return 0f;
				}
				if ((Mode == Modes.PlayTargetFeedbacks) && (TargetFeedbacks != null))
				{
					return TargetFeedbacks.TotalDuration;
				}
				else
				{
					return 0f;    
				}
			} 
		}
		public override bool HasChannel => true;
        
		public enum Modes { PlayFeedbacksInArea, PlayTargetFeedbacks, TriggerMMF_PlayerEvent }
        
		[MMFInspectorGroup("Feedbacks", true, 79)]
        
		/// the selected mode for this feedback
		[Tooltip("此反馈当前选择的模式")]
		public Modes Mode = Modes.PlayFeedbacksInArea;
        
		/// a specific MMFeedbacks / MMF_Player to play
		[MMFEnumCondition("Mode", (int)Modes.PlayTargetFeedbacks)]
		[Tooltip("要播放的特定反馈组/反馈播放器")]
		public MMFeedbacks TargetFeedbacks;
        
		/// whether or not to use a range
		[MMFEnumCondition("Mode", (int)Modes.PlayFeedbacksInArea)]
		[Tooltip("是否使用范围")]
		public bool OnlyTriggerPlayersInRange = false;
		/// the range of the event, in units
		[MMFEnumCondition("Mode", (int)Modes.PlayFeedbacksInArea)]
		[Tooltip("事件作用范围，单位为世界单位")]
		public float EventRange = 100f;
		/// the transform to use to broadcast the event as origin point
		[MMFEnumCondition("Mode", (int)Modes.PlayFeedbacksInArea)]
		[Tooltip("用于作为事件广播原点的 Transform")]
		public Transform EventOriginTransform;

		/// <summary>
		/// On init we turn the light off if needed
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
            
			if (EventOriginTransform == null)
			{
				EventOriginTransform = owner.transform;
			}
		}

		/// <summary>
		/// On Play we trigger our target feedback or trigger a feedback shake event to shake feedbacks in the area
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (TargetFeedbacks == Owner)
			{
				return;
			}
			
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (Mode == Modes.PlayFeedbacksInArea)
			{
				MMFeedbacksShakeEvent.Trigger(ChannelData, OnlyTriggerPlayersInRange, EventRange, EventOriginTransform.position);    
			}
			else if (Mode == Modes.PlayTargetFeedbacks)
			{
				TargetFeedbacks?.PlayFeedbacks(position, feedbacksIntensity);
			}
			else if (Mode == Modes.TriggerMMF_PlayerEvent)
			{
				MMF_PlayerEvent.Trigger(ChannelData, true, Owner.transform.position, MMF_PlayerEvent.Modes.PlayFeedbacks, feedbacksIntensity, false);	
			}
		}
	}
}
