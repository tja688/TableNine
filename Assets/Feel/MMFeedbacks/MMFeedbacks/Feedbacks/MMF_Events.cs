using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// A feedback to bind Unity events to and trigger them when played
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你把任意类型的 UnityEvent 绑定到该反馈的 Play、Stop、Initialization 与 Reset 方法上。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Events/Unity Events")]
	public class MMF_Events : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.EventsColor; } }
		#endif

		[MMFInspectorGroup("Events", true, 44)]
		/// the events to trigger when the feedback is played
		[Tooltip("反馈播放时触发的事件")]
		public UnityEvent PlayEvents;
		/// the events to trigger when the feedback is stopped
		[Tooltip("反馈停止时触发的事件")]
		public UnityEvent StopEvents;
		/// the events to trigger when the feedback is initialized
		[Tooltip("反馈初始化时触发的事件")]
		public UnityEvent InitializationEvents;
		/// the events to trigger when the feedback is reset
		[Tooltip("反馈重置时触发的事件")]
		public UnityEvent ResetEvents;

		/// <summary>
		/// On init, triggers the init events
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			if (Active && (InitializationEvents != null))
			{
				InitializationEvents.Invoke();
			}
		}

		/// <summary>
		/// On Play, triggers the play events
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (PlayEvents == null))
			{
				return;
			}
			PlayEvents.Invoke();    
		}

		/// <summary>
		/// On Stop, triggers the stop events
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (StopEvents == null))
			{
				return;
			}
			StopEvents.Invoke();
		}

		/// <summary>
		/// On reset, triggers the reset events
		/// </summary>
		protected override void CustomReset()
		{
			if (!Active || !FeedbackTypeAuthorized || (ResetEvents == null))
			{
				return;
			}
			base.CustomReset();
			ResetEvents.Invoke();
		}
	}
}

