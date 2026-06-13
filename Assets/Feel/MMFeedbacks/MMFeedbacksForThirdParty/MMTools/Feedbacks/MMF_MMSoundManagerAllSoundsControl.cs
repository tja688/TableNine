using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using MoreMountains.Tools;
using UnityEngine.Audio;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// A feedback used to control all sounds playing on the MMSoundManager at once. It'll let you pause, play, stop and free (stop and returns the audiosource to the pool) sounds.  You will need a MMSoundManager in your scene for this to work.
	/// </summary>
	[AddComponentMenu("")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[System.Serializable]
	[FeedbackPath("Audio/MMSoundManager All Sounds Control")]
	[FeedbackHelp("此反馈用于一次性控制 MMSoundManager 当前播放的全部声音。你可以对它们执行暂停、播放、停止和释放（停止并将 AudioSource 归还到对象池）。要使其生效，场景中必须存在一个 MMSoundManager。")]
	public class MMF_MMSoundManagerAllSoundsControl : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.SoundsColor; } }
		public override string RequiredTargetText { get { return ControlMode.ToString();  } }
		#endif
        
		[MMFInspectorGroup("MMSoundManager All Sounds Control", true, 30)]
		/// The selected control mode. 
		[Tooltip("当前选定的控制模式")]
		public MMSoundManagerAllSoundsControlEventTypes ControlMode = MMSoundManagerAllSoundsControlEventTypes.Pause;

		/// <summary>
		/// On Play, we call the specified event, to be caught by the MMSoundManager
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
            
			switch (ControlMode)
			{
				case MMSoundManagerAllSoundsControlEventTypes.Pause:
					MMSoundManagerAllSoundsControlEvent.Trigger(MMSoundManagerAllSoundsControlEventTypes.Pause);
					break;
				case MMSoundManagerAllSoundsControlEventTypes.Play:
					MMSoundManagerAllSoundsControlEvent.Trigger(MMSoundManagerAllSoundsControlEventTypes.Play);
					break;
				case MMSoundManagerAllSoundsControlEventTypes.Stop:
					MMSoundManagerAllSoundsControlEvent.Trigger(MMSoundManagerAllSoundsControlEventTypes.Stop);
					break;
				case MMSoundManagerAllSoundsControlEventTypes.Free:
					MMSoundManagerAllSoundsControlEvent.Trigger(MMSoundManagerAllSoundsControlEventTypes.Free);
					break;
				case MMSoundManagerAllSoundsControlEventTypes.FreeAllButPersistent:
					MMSoundManagerAllSoundsControlEvent.Trigger(MMSoundManagerAllSoundsControlEventTypes.FreeAllButPersistent);
					break;
				case MMSoundManagerAllSoundsControlEventTypes.FreeAllLooping:
					MMSoundManagerAllSoundsControlEvent.Trigger(MMSoundManagerAllSoundsControlEventTypes.FreeAllLooping);
					break;
			}
		}
	}
}