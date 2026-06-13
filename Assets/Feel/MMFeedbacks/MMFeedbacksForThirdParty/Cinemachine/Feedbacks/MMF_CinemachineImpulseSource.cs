using UnityEngine;
using MoreMountains.Feedbacks;
#if MM_CINEMACHINE
using Cinemachine;
#elif MM_CINEMACHINE3
using Unity.Cinemachine;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	[System.Serializable]
	[AddComponentMenu("")]
	#if MM_CINEMACHINE || MM_CINEMACHINE3
	[FeedbackPath("Camera/Cinemachine Impulse Source")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.Cinemachine")]
	[FeedbackHelp("这个反馈可在 Cinemachine Impulse Source 上生成 impulse。要让它生效，你的相机上需要挂有 Cinemachine Impulse Listener。")]
	public class MMF_CinemachineImpulseSource : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
			public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.CameraColor; } }
			#if MM_CINEMACHINE || MM_CINEMACHINE3
				public override bool EvaluateRequiresSetup() { return (ImpulseSource == null); }
				public override string RequiredTargetText { get { return ImpulseSource != null ? ImpulseSource.name : "";  } }
			#endif
			public override string RequiresSetupText { get { return "此反馈需要指定 ImpulseSource 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		

		[MMFInspectorGroup("Cinemachine Impulse Source", true, 28)]

		/// 应用到 impulse 抖动上的速度。
		[Tooltip("应用于冲击上的速度。")]
		public Vector3 Velocity = new Vector3(1f,1f,1f);
		#if MM_CINEMACHINE || MM_CINEMACHINE3
			/// 要广播的 impulse 定义。
			[Tooltip("要的广播冲击定义。")]
			public CinemachineImpulseSource ImpulseSource;
			
			public override bool HasAutomatedTargetAcquisition => true;
			protected override void AutomateTargetAcquisition() => ImpulseSource = FindAutomatedTarget<CinemachineImpulseSource>();
		#endif
		/// 当此反馈调用 Stop 方法时，是否清除 impulse（即停止相机震动）。
		[Tooltip("当此反馈调用 Stop 方法时，是否清除 impulse（即停止相机震动）。")]
		public bool ClearImpulseOnStop = false;
        
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			#if MM_CINEMACHINE || MM_CINEMACHINE3
			if (ImpulseSource != null)
			{
				ImpulseSource.GenerateImpulse(Velocity);
			}
			#endif
		}

		/// <summary>
		/// Stops the animation if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized || !ClearImpulseOnStop)
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);
            
			#if MM_CINEMACHINE || MM_CINEMACHINE3
				CinemachineImpulseManager.Instance.Clear();
			#endif
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
            
			#if MM_CINEMACHINE || MM_CINEMACHINE3
				CinemachineImpulseManager.Instance.Clear();
			#endif
		}
	}
}
