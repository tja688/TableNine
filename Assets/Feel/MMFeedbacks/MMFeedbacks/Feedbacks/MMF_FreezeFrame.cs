using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will trigger a freeze frame event when played, pausing the game for the specified duration (usually short, but not necessarily)
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈会在指定时长内冻结时间缩放。通常我会用 0.01 秒或 0.02 秒，但你也可以按手感自行调整。要让它生效，场景中必须存在 MMTimeManager。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Time/Freeze Frame")]
	public class MMF_FreezeFrame : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TimeColor; } }
		public override bool HasCustomInspectors => true;
		public override bool HasAutomaticShakerSetup => true;
		#endif

		[MMFInspectorGroup("Freeze Frame", true, 63)]
		/// the duration of the freeze frame
		[Tooltip("定格持续时间")]
		public float FreezeFrameDuration = 0.1f;
		/// the minimum value the timescale should be at for this freeze frame to happen. This can be useful to avoid triggering freeze frames when the timescale is already frozen. 
		[Tooltip("发生此冻结帧的时间尺度应处于的最小值。这对于避免在时间刻度已经冻结时触发冻结帧很有用。")]
		public float MinimumTimescaleThreshold = 0.1f;

		/// the duration of this feedback is the duration of the freeze frame
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(FreezeFrameDuration); } set { FreezeFrameDuration = value; } }

		/// <summary>
		/// On Play we trigger a freeze frame event
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
            
			if (Time.timeScale < MinimumTimescaleThreshold)
			{
				return;
			}
            
			MMFreezeFrameEvent.Trigger(FeedbackDuration);
		}
		
		/// <summary>
		/// Automatically adds a MMTimeManager to the scene
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			(MMTimeManager timeManager, bool createdNew) = Owner.gameObject.MMFindOrCreateObjectOfType<MMTimeManager>("MMTimeManager", null);
			if (createdNew)
			{
				MMDebug.DebugLogInfo("Added a MMTimeManager to the scene. You're all set.");	
			}
		}
	}
}
