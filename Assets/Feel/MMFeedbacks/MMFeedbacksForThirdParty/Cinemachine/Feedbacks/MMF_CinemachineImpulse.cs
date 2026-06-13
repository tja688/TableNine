using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
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
	[FeedbackPath("Camera/Cinemachine Impulse")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.Cinemachine")]
	[FeedbackHelp("这个反馈可触发一个 Cinemachine Impulse 事件。要让它生效，你的相机上需要挂有 Cinemachine Impulse Listener。")]
	public class MMF_CinemachineImpulse : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.CameraColor; } }
		public override bool HasCustomInspectors => true;
		public override bool HasAutomaticShakerSetup => true;
		#endif
		public override bool HasRandomness => true;

		#if MM_CINEMACHINE || MM_CINEMACHINE3
		[MMFInspectorGroup("Cinemachine Impulse", true, 28)]
		/// 要广播的 impulse 定义。
		[Tooltip("要的广播冲击定义。")]
		public CinemachineImpulseDefinition m_ImpulseDefinition = new CinemachineImpulseDefinition();
		/// 应用到 impulse 抖动上的速度。
		[Tooltip("应用于冲击上的速度。")]
		public Vector3 Velocity;
		/// 当此反馈调用 Stop 方法时，是否清除 impulse（即停止相机震动）。
		[Tooltip("当此反馈调用 Stop 方法时，是否清除 impulse（即停止相机震动）。")]
		public bool ClearImpulseOnStop = false;
		#endif
		
		[Header("Gizmos")]
		/// 在适用时，是否绘制 gizmos 来展示此反馈的各项距离参数。蓝色表示 Dissipation Distance，黄色表示 Impact Radius。
		[Tooltip("在适用时，是否有较远的小玩意来显示反馈的元件距离参数。蓝色表示耗散距离，黄色表示影响半径。")]
		public bool DrawGizmos = false;
		
		#if MM_CINEMACHINE
		/// the duration of this feedback is the duration of the impulse
		public override float FeedbackDuration { get { return m_ImpulseDefinition != null ? m_ImpulseDefinition.m_TimeEnvelope.Duration : 0f; } }
		#elif MM_CINEMACHINE3
		/// the duration of this feedback is the duration of the impulse
		public override float FeedbackDuration { get { return m_ImpulseDefinition != null ? m_ImpulseDefinition.TimeEnvelope.Duration : 0f; } }
		#endif

		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			#if MM_CINEMACHINE || MM_CINEMACHINE3
			CinemachineImpulseManager.Instance.IgnoreTimeScale = !InScaledTimescaleMode;
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			m_ImpulseDefinition.CreateEvent(position, Velocity * intensityMultiplier);
			#endif
		}

		/// <summary>
		/// Stops the animation if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			if (!Active || !FeedbackTypeAuthorized || !ClearImpulseOnStop)
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);
			CinemachineImpulseManager.Instance.Clear();
			#endif
		}

		/// <summary>
		/// When adding the feedback we initialize its cinemachine impulse definition
		/// </summary>
		public override void OnAddFeedback()
		{
			#if MM_CINEMACHINE 
			// sets the feedback properties
			if (this.m_ImpulseDefinition == null)
			{
				this.m_ImpulseDefinition = new CinemachineImpulseDefinition();
			}
			this.m_ImpulseDefinition.m_RawSignal = Resources.Load<NoiseSettings>("MM_6D_Shake");
			this.Velocity = new Vector3(5f, 5f, 5f);
			#elif MM_CINEMACHINE3
			// sets the feedback properties
			if (this.m_ImpulseDefinition == null)
			{
				this.m_ImpulseDefinition = new CinemachineImpulseDefinition();
			}
			this.m_ImpulseDefinition.RawSignal = Resources.Load<NoiseSettings>("MM_6D_Shake");
			this.Velocity = new Vector3(5f, 5f, 5f);
			#endif
		}

		/// <summary>
		/// Draws dissipation distance and impact distance gizmos if necessary
		/// </summary>
		public override void OnDrawGizmosSelectedHandler()
		{
			if (!DrawGizmos)
			{
				return;
			}
			#if MM_CINEMACHINE 
			{
				if ( (this.m_ImpulseDefinition.m_ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Dissipating)
				     || (this.m_ImpulseDefinition.m_ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Propagating)
				     || (this.m_ImpulseDefinition.m_ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Legacy) )
				{
					Gizmos.color = MMColors.Aqua;
					Gizmos.DrawWireSphere(Owner.transform.position, this.m_ImpulseDefinition.m_DissipationDistance);
				}
				if (this.m_ImpulseDefinition.m_ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Legacy)
				{
					Gizmos.color = MMColors.ReunoYellow;
					Gizmos.DrawWireSphere(Owner.transform.position, this.m_ImpulseDefinition.m_ImpactRadius);
				}
			}
			#elif MM_CINEMACHINE3
			if (this.m_ImpulseDefinition != null)
			{
				if ( (this.m_ImpulseDefinition.ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Dissipating)
					 || (this.m_ImpulseDefinition.ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Propagating)
					 || (this.m_ImpulseDefinition.ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Legacy) )
				{
					Gizmos.color = MMColors.Aqua;
					Gizmos.DrawWireSphere(Owner.transform.position, this.m_ImpulseDefinition.DissipationDistance);
				}
				if (this.m_ImpulseDefinition.ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Legacy)
				{
					Gizmos.color = MMColors.ReunoYellow;
					Gizmos.DrawWireSphere(Owner.transform.position, this.m_ImpulseDefinition.ImpactRadius);
				}
			}
			#endif
		}
		
		/// <summary>
		/// Automatically adds a Cinemachine Impulse Listener to the camera
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			MMCinemachineHelpers.AutomaticCinemachineShakersSetup(Owner, "CinemachineImpulse");
		}
	}
}