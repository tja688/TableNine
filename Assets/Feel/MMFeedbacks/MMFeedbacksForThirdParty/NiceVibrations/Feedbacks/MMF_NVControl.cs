using UnityEngine;
using MoreMountains.Feedbacks;
#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
using Lofelt.NiceVibrations;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// Add this feedback to interact with haptics at a global level, stopping them all, enabling or disabling them, adjusting their global level or initializing/release the haptic engine
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
	[FeedbackPath("Haptics/Haptic Control")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.NiceVibrations")]
	[FeedbackHelp("添加这个反馈后，可在全局层面控制 haptics：全部停止、启用或禁用、调整全局强度，或初始化/释放 haptic 引擎。")]
	public class MMF_NVControl : MMF_Feedback
	{
		#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.HapticsColor; } }
		public override string RequiredTargetText { get { return ControlType.ToString();  } }
		#endif
    
		public enum ControlTypes { Stop, EnableHaptics, DisableHaptics, AdjustHapticsLevel, Initialize, Release }

		[MMFInspectorGroup("Haptic Control", true, 24)]
		/// 播放此反馈时要执行的控制命令类型。具体行为请参考 Nice Vibrations 文档。 
		[Tooltip("播放此反馈时要执行的控制命令类型。具体行为请参考 Nice Vibrations 文档。")]
		public ControlTypes ControlType = ControlTypes.Stop;
		/// 在 AdjustHapticsLevel 模式下要设置的输出强度。
		[Tooltip("在 AdjustHapticsLevel 模式下要设置的输出强度。")]
		[MMFEnumCondition("ControlType", (int)ControlTypes.AdjustHapticsLevel)]
		public float OutputLevel = 1f;
        
		/// <summary>
		/// On play we apply the specified order
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			switch (ControlType)
			{
				case ControlTypes.Stop:
					HapticController.Stop();
					break;
				case ControlTypes.EnableHaptics:
					HapticController.hapticsEnabled = true;
					break;
				case ControlTypes.DisableHaptics:
					HapticController.hapticsEnabled = false;
					break;
				case ControlTypes.AdjustHapticsLevel:
					HapticController.outputLevel = OutputLevel;
					break;
				case ControlTypes.Initialize:
					LofeltHaptics.Initialize();
					HapticController.Init();
					break;
				case ControlTypes.Release:
					LofeltHaptics.Release();
					break;
			}
		}
		#else
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f) { }
		#endif
	}    
}