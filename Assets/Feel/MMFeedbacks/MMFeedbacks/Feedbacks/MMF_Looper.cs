using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will move the current "head" of an MMF_Player sequence back to another feedback above in the list.
	/// What feedback the head lands on depends on your settings : you can decide to have it loop at last pause, or at the last LoopStart feedback in the list (or both).
	/// Furthermore, you can decide to have it loop multiple times and cause a pause when met.
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈会将 MMF_Player 序列当前的“播放头（head）”回退到列表中位于它上方的其他反馈。播放头回退到哪里由你的设置决定：可回退到上一个 Pause，或上一个 LoopStart（也可两者同时启用）。你还可以设置循环次数，并在命中该反馈时触发暂停。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Loop/Looper")]
	public class MMF_Looper : MMF_Pause
	{
		[MMFInspectorGroup("Loop", true, 34)]
        
		[Header("Loop conditions")]
		/// if this is true, this feedback, when met, will cause the MMF_Player to reposition its 'head' to the first pause found above it (going from this feedback to the top), or to the start if none is found
		[Tooltip("若开启，命中本反馈时会将 MMF_Player 的播放头回退到其上方最近的 Pause；若上方没有 Pause，则回到序列起点。")]
		public bool LoopAtLastPause = true;
		/// if this is true, this feedback, when met, will cause the MMF_Player to reposition its 'head' to the first LoopStart feedback found above it (going from this feedback to the top), or to the start if none is found
		[Tooltip("若开启，命中本反馈时会将 MMF_Player 的播放头回退到其上方最近的 LoopStart；若上方没有 LoopStart，则回到序列起点。")]
		public bool LoopAtLastLoopStart = true;

		[Header("Loop")]
		/// if this is true, the looper will loop forever
		[Tooltip("若开启，循环将不受 NumberOfLoops 限制并持续执行，直到外部停止该反馈或停止播放器。")]
		public bool InfiniteLoop = false;
		/// how many times this loop should run
		[Tooltip("在 InfiniteLoop 关闭时，本循环应执行的总次数。")]
		[MMCondition("InfiniteLoop", true, true)]
		public int NumberOfLoops = 2;
		/// the amount of loops left (updated at runtime)
		[Tooltip("剩余循环次数（运行时更新，仅用于查看）。")]
		[MMFReadOnly]
		public int NumberOfLoopsLeft = 1;
		/// whether we are in an infinite loop at this time or not
		[Tooltip("当前是否处于无限循环状态（运行时更新，仅用于查看）。")]
		[MMFReadOnly]
		public bool InInfiniteLoop = false;
		/// whether or not to trigger a Loop MMFeedbacksEvent when this looper is reached
		[Tooltip("命中此 Looper 时是否触发一次 Loop 类型的 MMFeedbacksEvent。")]
		public bool TriggerMMFeedbacksEvents = true;

		[Header("Events")] 
		/// a Unity Event to invoke when the looper is reached
		[Tooltip("命中此活套时要调用的统一事件。")]
		public UnityEvent OnLoop;

		/// sets the color of this feedback in the inspector
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.LooperColor; } }
		public override Color DisplayColor { get { return MMFeedbacksInspectorColors.LooperColor.MMDarken(0.25f); } }
		#endif
		public override bool LooperPause { get { return true; } }

		/// the duration of this feedback is the duration of the pause
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(PauseDuration); } set { PauseDuration = value; } }

		/// <summary>
		/// On init we initialize our number of loops left
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			InInfiniteLoop = InfiniteLoop;
			NumberOfLoopsLeft = NumberOfLoops;
			if (OnLoop == null)
			{
				OnLoop = new UnityEvent();
			}
		}

		/// <summary>
		/// On play we decrease our counter and play our pause
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (Active)
			{
				ProcessNewPauseDuration();
				InInfiniteLoop = InfiniteLoop;
				NumberOfLoopsLeft--;
				Owner.StartCoroutine(PlayPause());
				TriggerOnLoop(Owner);
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnLoop(MMFeedbacks source)
		{
			OnLoop.Invoke();

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.Loop);
			}
		}

		/// <summary>
		/// On custom stop, we exit our infinite loop
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			base.CustomStopFeedback(position, feedbacksIntensity);
			InInfiniteLoop = false;
		}

		/// <summary>
		/// On reset we reset our amount of loops left
		/// </summary>
		protected override void CustomReset()
		{
			base.CustomReset();
			InInfiniteLoop = InfiniteLoop;
			NumberOfLoopsLeft = NumberOfLoops;
		}
	}
}

