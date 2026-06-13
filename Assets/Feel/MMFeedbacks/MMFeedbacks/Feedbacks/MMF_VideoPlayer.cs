using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you control video players in all sorts of ways (Play, Pause, Toggle, Stop, Prepare, StepForward, StepBackward, SetPlaybackSpeed, SetDirectAudioVolume, SetDirectAudioMute, GoToFrame, ToggleLoop)
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可用多种方式控制 Video Player，包括 Play、Pause、Toggle、Stop、Prepare、StepForward、StepBackward、SetPlaybackSpeed、SetDirectAudioVolume、SetDirectAudioMute、GoToFrame 与 ToggleLoop。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("UI/Video Player")]
	public class MMF_VideoPlayer : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		public enum VideoActions { Play, Pause, Toggle, Stop, Prepare, StepForward, StepBackward, SetPlaybackSpeed, SetDirectAudioVolume, SetDirectAudioMute, GoToFrame, ToggleLoop  }

		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetVideoPlayer == null); }
		public override string RequiredTargetText { get { return TargetVideoPlayer != null ? TargetVideoPlayer.name + " " + VideoAction.ToString() : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置 TargetVideoPlayer 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetVideoPlayer = FindAutomatedTarget<VideoPlayer>();

		[MMFInspectorGroup("Video Player", true, 58, true)]
		/// the Video Player to control with this feedback
		[Tooltip("此反馈要控制视频播放器。")]
		public VideoPlayer TargetVideoPlayer;
		/// the Video Player to control with this feedback
		[Tooltip("要执行的控制动作。不同动作仅会启用其对应参数，其他字段将被忽略。")]
		public VideoActions VideoAction = VideoActions.Pause;
		/// the frame at which to jump when in GoToFrame mode
		[Tooltip("在 GoToFrame 动作下要跳转到的帧号。")]
		[MMFEnumCondition("VideoAction", (int)VideoActions.GoToFrame)]
		public long TargetFrame = 10;
		/// the new playback speed (between 0 and 10)
		[Tooltip("在 SetPlaybackSpeed 动作下要设置的播放速度（0 到 10）。")]
		[MMFEnumCondition("VideoAction", (int)VideoActions.SetPlaybackSpeed)]
		public float PlaybackSpeed = 2f;
		/// the track index on which to control volume
		[Tooltip("在 SetDirectAudioVolume / SetDirectAudioMute 动作下要控制的音轨索引。")]
		[MMFEnumCondition("VideoAction", (int)VideoActions.SetDirectAudioMute, (int)VideoActions.SetDirectAudioVolume)]
		public int TrackIndex = 0;
		/// the new volume for the specified track, between 0 and 1
		[Tooltip("在 SetDirectAudioVolume 动作下要设置的音量（0 到 1）。")]
		[MMFEnumCondition("VideoAction", (int)VideoActions.SetDirectAudioVolume)]
		public float Volume = 1f;
		/// whether to mute the track or not when that feedback plays
		[Tooltip("在 SetDirectAudioMute 动作下是否将该音轨静音。")]
		[MMFEnumCondition("VideoAction", (int)VideoActions.SetDirectAudioMute)]
		public bool Mute = true;

		/// <summary>
		/// On play we apply the selected command to our target video player
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (TargetVideoPlayer == null)
			{
				return;
			}

			switch (VideoAction)
			{
				case VideoActions.Play:
					TargetVideoPlayer.Play();
					break;
				case VideoActions.Pause:
					TargetVideoPlayer.Pause();
					break;
				case VideoActions.Toggle:
					if (TargetVideoPlayer.isPlaying)
					{
						TargetVideoPlayer.Pause();
					}
					else
					{
						TargetVideoPlayer.Play();
					}
					break;
				case VideoActions.Stop:
					TargetVideoPlayer.Stop();
					break;
				case VideoActions.Prepare:
					TargetVideoPlayer.Prepare();
					break;
				case VideoActions.StepForward:
					TargetVideoPlayer.StepForward();
					break;
				case VideoActions.StepBackward:
					TargetVideoPlayer.Pause();
					TargetVideoPlayer.frame = TargetVideoPlayer.frame - 1;
					break;
				case VideoActions.SetPlaybackSpeed:
					TargetVideoPlayer.playbackSpeed = PlaybackSpeed;
					break;
				case VideoActions.SetDirectAudioVolume:
					TargetVideoPlayer.SetDirectAudioVolume((ushort)TrackIndex, Volume);
					break;
				case VideoActions.SetDirectAudioMute:
					TargetVideoPlayer.SetDirectAudioMute((ushort)TrackIndex, Mute);
					break;
				case VideoActions.GoToFrame:
					TargetVideoPlayer.frame = TargetFrame;
					break;
				case VideoActions.ToggleLoop:
					TargetVideoPlayer.isLooping = !TargetVideoPlayer.isLooping;
					break;
			}

		}
	}
}

