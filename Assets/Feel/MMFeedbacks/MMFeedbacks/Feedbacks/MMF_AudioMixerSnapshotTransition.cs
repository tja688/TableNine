using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you transition to a target AudioMixer Snapshot over a specified time
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可在指定时长内过渡到目标 AudioMixerSnapshot。若反向播放并且设置了 OriginalSnapshot，则会过渡回 OriginalSnapshot。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Audio/AudioMixer Snapshot Transition")]
	public class MMF_AudioMixerSnapshotTransition : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
		public override bool EvaluateRequiresSetup() { return ((TargetSnapshot == null) || (OriginalSnapshot == null)); }
		public override string RequiredTargetText { get { return ((TargetSnapshot != null) && (OriginalSnapshot != null)) ? TargetSnapshot.name + " to "+ OriginalSnapshot.name : "";  } }
		public override string RequiresSetupText { get { return "至少需要设置 TargetSnapshot 才能播放；若你计划反向播放，也请同时设置 OriginalSnapshot。"; } }
		#endif
        
		[MMFInspectorGroup("AudioMixer Snapshot", true, 44)]
		/// the target audio mixer snapshot we want to transition to 
		[Tooltip("正向播放时要过渡到的目标 AudioMixerSnapshot")]
		public AudioMixerSnapshot TargetSnapshot;
		/// the audio mixer snapshot we want to transition from, optional, only needed if you plan to play this feedback in reverse 
		[Tooltip("反向播放时要过渡到的起始 AudioMixerSnapshot（可选）。若留空，反向播放将退化为过渡到 TargetSnapshot。")]
		public AudioMixerSnapshot OriginalSnapshot;
		/// the duration, in seconds, over which to transition to the selected snapshot
		[Tooltip("快照过渡时长（秒）。正向与反向过渡都会使用该时长。")]
		public float TransitionDuration = 1f;
        
		/// <summary>
		/// On play we transition to the selected snapshot
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (TargetSnapshot == null)
			{
				return;
			}

			if (!NormalPlayDirection)
			{
				if (OriginalSnapshot != null)
				{
					OriginalSnapshot.TransitionTo(TransitionDuration);     
				}
				else
				{
					TargetSnapshot.TransitionTo(TransitionDuration);
				}
			}
			else
			{
				TargetSnapshot.TransitionTo(TransitionDuration);     
			}
		}
	}
}

