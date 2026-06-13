using System;
using UnityEngine;
#if MM_VISUALEFFECTGRAPH
using UnityEngine.VFX;
#endif
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可对目标 VisualEffect 执行基础控制。
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("这个反馈可对目标 VisualEffect 执行基础控制。")]
	#if MM_VISUALEFFECTGRAPH
	[FeedbackPath("Particles/VisualEffect")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.VisualEffectGraph")]
	public class MMF_VisualEffect : MMF_Feedback 
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.ParticlesColor; } }
		#endif

		/// the duration of this feedback is the duration of the shake
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(DeclaredDuration); } set { DeclaredDuration = value;  } }
		public override bool HasChannel => true;
		public override bool HasRandomness => true;
		
		[MMFInspectorGroup("Visual Effect", true, 41)]
		/// 这是提供给 MMF_Player 参考的反馈持续时间，不会直接影响你的 VisualEffect。通常建议让它与实际视觉效果的持续时间一致；这样在使用 Holding Pause 时，本反馈的时序会更准确。
		[Tooltip("这是提供给 MMF_Player 参考的反馈持续时间，不会直接影响你的 VisualEffect。通常建议让它与实际视觉效果的持续时间一致；这样在使用 Holding Pause 时，本反馈的时序会更准确。")]
		public float DeclaredDuration = 0f;
		
		#if MM_VISUALEFFECTGRAPH
		
		/// the various modes to control the target visual effect
		public enum Modes { Play, Stop, Pause, Unpause, AdvanceOneFrame, Reinit, SetPlayRate, Simulate }
		
		/// 播放此反馈时要控制的 VisualEffect。
		[Tooltip("播放此反馈时要控制的 VisualEffect。")]
		public VisualEffect TargetVisualEffect;
		/// 播放此反馈时发送给目标 VisualEffect 的控制模式。
		[Tooltip("播放此反馈时发送给目标 VisualEffect 的控制模式。")]
		public Modes Mode = Modes.Play;
		/// 在 SetPlayRate 模式下要应用的新播放速率。
		[Tooltip("在 SetPlayRate 模式下要应用的新播放速率。")]
		[MMFEnumCondition("Mode", (int)Modes.SetPlayRate)]
		public float NewPlayRate = 1f;
		/// 在 Simulate 模式下使用的 delta time。
		[Tooltip("在模拟模式下使用的增量时间。")]
		[MMFEnumCondition("Mode", (int)Modes.Simulate)]
		public float StepDeltaTime = 1f;
		/// 在 Simulate 模式下要模拟的步数。
		[Tooltip("在 Simulate 模式下要模拟的步数。")]
		[MMFEnumCondition("Mode", (int)Modes.Simulate)]
		public uint StepCount = 5;
		/// 停止此反馈时，是否一并停止该 VisualEffect。
		[Tooltip("停止此反馈时，是否一并停止该 VisualEffect。")] 
		public bool StopVisualEffectOnStopFeedback = false;
		/// 重置此反馈时，是否一并停止该 VisualEffect。
		[Tooltip("重置此反馈时，是否一并停止该 VisualEffect。")] 
		public bool StopVisualEffectOnReset = false;
		/// 初始化此反馈时，是否一并停止该 VisualEffect。
		[Tooltip("初始化此反馈时，是否一并停止该 VisualEffect。")] 
		public bool StopVisualEffectOnInit = false;

		protected VFXEventAttribute _eventAttribute;

		/// <summary>
		/// On init we stop our visual effect if needed
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			
			if (StopVisualEffectOnInit)
			{
				StopVisualEffect();
			}
		}

		/// <summary>
		/// On play we pass the selected instruction to our target visual effect
		/// </summary>
		/// <param name="position"></param>
		/// <param name="attenuation"></param>
		protected override void CustomPlayFeedback(Vector3 position, float attenuation = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetVisualEffect == null))
			{
				return;
			}

			switch (Mode)
			{
				case Modes.Play:
					TargetVisualEffect.Play();
					break;
				case Modes.Stop:
					StopVisualEffect();
					break;
				case Modes.Pause:
					TargetVisualEffect.pause = true;
					break;
				case Modes.Unpause:
					TargetVisualEffect.pause = false;
					break;
				case Modes.AdvanceOneFrame:
					TargetVisualEffect.AdvanceOneFrame();
					break;
				case Modes.Reinit:
					TargetVisualEffect.Reinit();
					break;
				case Modes.SetPlayRate:
					TargetVisualEffect.playRate = NewPlayRate;
					break;
				case Modes.Simulate:
					TargetVisualEffect.Simulate(StepDeltaTime, StepCount);
					break;
			}
		}
		
		/// <summary>
		/// On stop we stop our visual effect if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);

			if (StopVisualEffectOnStopFeedback)
			{
				StopVisualEffect();
			}
		}

		/// <summary>
		/// On Reset, stops the visual effect if needed
		/// </summary>
		protected override void CustomReset()
		{
			base.CustomReset();

			if (InCooldown)
			{
				return;
			}

			if (StopVisualEffectOnReset)
			{
				StopVisualEffect();
			}
		}

		/// <summary>
		/// Stops the target visual effect
		/// </summary>
		protected virtual void StopVisualEffect()
		{
			if (TargetVisualEffect == null)
			{
				return;
			}
			
			TargetVisualEffect.Stop();
		}
		#else
		protected override void CustomPlayFeedback(Vector3 position, float attenuation = 1.0f) { }
		#endif
	}
}