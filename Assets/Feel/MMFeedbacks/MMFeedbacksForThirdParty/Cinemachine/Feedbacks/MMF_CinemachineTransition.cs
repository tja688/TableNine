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
	/// <summary>
	/// This feedback will let you change the priorities of your cameras. 
	/// It requires a bit of setup : adding a MMCinemachinePriorityListener to your different cameras, with unique Channel values on them.
	/// Optionally, you can add a MMCinemachinePriorityBrainListener on your Cinemachine Brain to handle different transition types and durations.
	/// 之后只需要在反馈上指定通道和新的优先级并播放即可，镜头就会自动完成切换。
	/// </summary>
	[AddComponentMenu("")]
	#if MM_CINEMACHINE || MM_CINEMACHINE3
	[System.Serializable]
	[FeedbackPath("Camera/Cinemachine Transition")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.Cinemachine")]
	[FeedbackHelp("这个反馈可修改相机的优先级，但需要先做一些设置：" +
	              "给不同相机添加 MMCinemachinePriorityListener，并为它们设置各自唯一的通道值。" +
	              "你也可以选择在 Cinemachine Brain 上添加 MMCinemachinePriorityBrainListener，用来处理不同的过渡类型和持续时间。" +
	              "之后只需要在反馈上指定通道和新的优先级并播放即可，镜头就会自动完成切换。")]
	public class MMF_CinemachineTransition : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		public enum Modes { Event, Binding }
        
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.CameraColor; } }
		public override string RequiredTargetText => RequiredChannelText;
		#endif
		#if MM_CINEMACHINE
		/// the duration of this feedback is the duration of the shake
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(BlendDefintion.m_Time); } set { BlendDefintion.m_Time = value; } }
		#elif MM_CINEMACHINE3
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(BlendDefintion.Time); } set { BlendDefintion.Time = value; } }
		#endif
		#if MM_CINEMACHINE || MM_CINEMACHINE3
		public override bool HasAutomatedTargetAcquisition => true;
		#endif
		#if MM_CINEMACHINE
		protected override void AutomateTargetAcquisition() => TargetVirtualCamera = FindAutomatedTarget<CinemachineVirtualCamera>();
		#elif MM_CINEMACHINE3
		protected override void AutomateTargetAcquisition() => TargetCinemachineCamera = FindAutomatedTarget<CinemachineCamera>();
		#endif
		public override bool HasChannel => true;

		[MMFInspectorGroup("Cinemachine Transition", true, 52)]
		/// 选择工作模式：通过事件触发，或直接绑定某个特定相机。
		[Tooltip("选择工作模式：通过事件触发，或直接绑定某个特定相机。")]
		public Modes Mode = Modes.Event;
		#if MM_CINEMACHINE
		/// 要作用的虚拟相机。
		[Tooltip("要作用的虚拟相机。")]
		[MMFEnumCondition("Mode", (int)Modes.Binding)]
		public CinemachineVirtualCamera TargetVirtualCamera;
		#elif MM_CINEMACHINE3 
		/// 要作用的 Cinemachine 相机。
		[Tooltip("要作用的 电影机 相机。")]
		[MMFEnumCondition("Mode", (int)Modes.Binding)]
		public CinemachineCamera TargetCinemachineCamera;
		#endif
		/// 抖动结束后是否将目标值恢复到初始状态。
		[Tooltip("抖动结束后是否将目标值恢复到初始状态。")]
		public bool ResetValuesAfterTransition = true;

		[Header("Priority")]
		/// 要应用到指定通道中所有虚拟相机上的新优先级。
		[Tooltip("要应用到指定通道中所有虚拟相机上的新优先级。")]
		public int NewPriority = 10;
		/// 是否强制将其他通道中的所有虚拟相机优先级重置为 0。若开启，其他通道相机会被压低优先级。
		[Tooltip("是否强制将其他通道中的所有虚拟相机优先级重置为 0。若开启，其他通道相机会被压低优先级。")]
		public bool ForceMaxPriority = true;
		/// 是否强制应用新的 Blend 设置。若关闭，则继续使用当前已有的过渡设置。
		[Tooltip("是否强制应用新的 Blend 设置。若关闭，则继续使用当前已有的过渡设置。")]
		public bool ForceTransition = false;
		#if MM_CINEMACHINE || MM_CINEMACHINE3
		/// 要应用的新 Blend 定义。
		[Tooltip("要应用的新混合定义。")]
		[MMFCondition("ForceTransition", true)]
		public CinemachineBlendDefinition BlendDefintion;

		protected CinemachineBlendDefinition _tempBlend;
		#endif

		/// <summary>
		/// Triggers a priority change on listening virtual cameras
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
            
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			_tempBlend = BlendDefintion;
			#endif
			#if MM_CINEMACHINE
			_tempBlend.m_Time = FeedbackDuration;
			#elif MM_CINEMACHINE3
			_tempBlend.Time = FeedbackDuration;
			#endif
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			if (Mode == Modes.Event)
			{
				MMCinemachinePriorityEvent.Trigger(ChannelData, ForceMaxPriority, NewPriority, ForceTransition, _tempBlend, ResetValuesAfterTransition, ComputedTimescaleMode);    
			}
			else
			{
				MMCinemachinePriorityEvent.Trigger(ChannelData, ForceMaxPriority, 0, ForceTransition, _tempBlend, ResetValuesAfterTransition, ComputedTimescaleMode); 
				SetPriority(NewPriority);
			}
			#endif
		}
		
		protected virtual void SetPriority(int newPriority)
		{
			#if MM_CINEMACHINE 
			TargetVirtualCamera.Priority = newPriority;
			#elif MM_CINEMACHINE3
			PrioritySettings prioritySettings = TargetCinemachineCamera.Priority;
			prioritySettings.Value = newPriority;
			TargetCinemachineCamera.Priority = prioritySettings;
			#endif
		}
		
		/// <summary>
		/// On restore, we restore our initial state
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			MMCinemachinePriorityEvent.Trigger(ChannelData, ForceMaxPriority, 0, ForceTransition, _tempBlend, ResetValuesAfterTransition, ComputedTimescaleMode, true); 
			#endif
		}
	}
}