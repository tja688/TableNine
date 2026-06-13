using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will cause a pause when met, preventing any other feedback lower in the sequence to run until it's complete.
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("当序列执行到此反馈时，会进入暂停状态；在它执行完之前，列表中位于其下方的其他反馈都不会继续运行。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Pause/Pause")]
	public class MMF_Pause : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.PauseColor; } }
		public override Color DisplayColor { get { return MMFeedbacksInspectorColors.PauseColor.MMDarken(0.25f); } }
		public override bool DisplayFullHeaderColor => true;
		#endif
		public override IEnumerator Pause { get { return PauseWait(); } }
        
		[MMFInspectorGroup("Pause", true, 32)]
		/// the duration of the pause, in seconds
		[Tooltip("暂停持续时间（秒）。当 ScriptDriven 关闭时，到时会自动恢复；当 ScriptDriven 开启时，恢复方式由下方 ScriptDriven/AutoResume 设置决定。")]
		public float PauseDuration = 1f;

		public bool RandomizePauseDuration = false;

		[MMFCondition("RandomizePauseDuration", true)]
		public float MinPauseDuration = 1f;
		[MMFCondition("RandomizePauseDuration", true)]
		public float MaxPauseDuration = 3f;
		[MMFCondition("RandomizePauseDuration", true)]
		public bool RandomizeOnEachPlay = true;
        
		/// if this is true, you'll need to call the ResumeFeedbacks() method on the host MMF_Player for this pause to stop, and the rest of the sequence to play
		[Tooltip("若开启，暂停将改为脚本驱动：默认必须由外部调用宿主 MMF_Player 的 ResumeFeedbacks() 才会继续；若同时开启 AutoResume，则达到延迟后也会自动恢复。")]
		public bool ScriptDriven = false;
		/// if this is true, a script driven pause will resume after its AutoResumeAfter delay, whether it has been manually resumed or not 
		[Tooltip("仅在 ScriptDriven 开启时生效。若开启，脚本驱动暂停会在 AutoResumeAfter 延迟后自动恢复（即使尚未手动调用 ResumeFeedbacks()）。")] 
		[MMFCondition("ScriptDriven", true)]
		public bool AutoResume = false;
		/// the duration after which to auto resume, regardless of manual resume calls beforehand
		[Tooltip("仅在 AutoResume 开启时生效。自动恢复延迟时间（秒），到时后会自动恢复暂停。")] 
		[MMFCondition("AutoResume", true)]
		public float AutoResumeAfter = 0.25f;
		
		protected Coroutine _pauseCoroutine;
        
		/// the duration of this feedback is the duration of the pause
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(PauseDuration); } set { PauseDuration = value; } }

		/// <summary>
		/// An IEnumerator used to wait for the duration of the pause, on scaled or unscaled time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator PauseWait()
		{
			yield return WaitFor(ApplyTimeMultiplier(PauseDuration));
		}

		/// <summary>
		/// On init we cache our wait for seconds
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			ScriptDrivenPause = ScriptDriven;
			ScriptDrivenPauseAutoResume = AutoResume ? AutoResumeAfter : -1f;
			if (RandomizePauseDuration)
			{
				PauseDuration = Random.Range(MinPauseDuration, MaxPauseDuration);
			}
		}

		/// <summary>
		/// On play we trigger our pause
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			ProcessNewPauseDuration();
			_pauseCoroutine = Owner.StartCoroutine(PlayPause());
		}

		/// <summary>
		/// On Stop, we stop our pause
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
            
			if (_pauseCoroutine != null)
			{
				Owner.StopCoroutine(_pauseCoroutine);
			}
		}

		/// <summary>
		/// Computes a new pause duration if needed
		/// </summary>
		protected virtual void ProcessNewPauseDuration()
		{
			if (RandomizePauseDuration && RandomizeOnEachPlay)
			{
				PauseDuration = Random.Range(MinPauseDuration, MaxPauseDuration);
			}
		}

		/// <summary>
		/// Pause coroutine
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator PlayPause()
		{
			yield return Pause;
		}
	}
}

