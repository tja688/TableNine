using System.Collections;
using UnityEngine;
#if MM_CINEMACHINE
using Cinemachine;
#elif MM_CINEMACHINE3
using Unity.Cinemachine;
#endif
using MoreMountains.Feedbacks;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// Add this to a Cinemachine virtual camera and it'll be able to listen to MMCinemachinePriorityEvent, usually triggered by a MMFeedbackCinemachineTransition
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Cinemachine/MM Cinemachine Priority Listener")]
	#if MM_CINEMACHINE || MM_CINEMACHINE3
	[RequireComponent(typeof(CinemachineVirtualCameraBase))]
	#endif
	public class MMCinemachinePriorityListener : MonoBehaviour
	{
        
		[HideInInspector] 
		public TimescaleModes TimescaleMode = TimescaleModes.Scaled;
        
        
		public virtual float GetTime() { return (TimescaleMode == TimescaleModes.Scaled) ? Time.time : Time.unscaledTime; }
		public virtual float GetDeltaTime() { return (TimescaleMode == TimescaleModes.Scaled) ? Time.deltaTime : Time.unscaledDeltaTime; }
        
		[Header("Priority Listener")]
		[Tooltip("选择使用 int 通道还是 MMChannel ScriptableObject 通道来接收事件。int 配置更简单，但项目变大后容易混乱，不利于记忆每个数字对应什么。" +
		         "MMChannel 需要预先创建，但具备可读名称，后期也更容易维护和扩展。")]
		public MMChannelModes ChannelMode = MMChannelModes.Int;
		/// 要监听的通道，必须与反馈上发送的通道一致。
		[Tooltip("要监听的通道，必须与反馈上发送的通道一致。")]
		[MMFEnumCondition("ChannelMode", (int)MMChannelModes.Int)]
		public int Channel = 0;
		/// the MMChannel definition asset to use to listen for events. The feedbacks targeting this shaker will have to reference that same MMChannel definition to receive events - to create a MMChannel,
		/// 在 Project 视图中任意位置（通常是 Data 文件夹）右键，选择 MoreMountains > MMChannel，然后为它起一个唯一名称。
		[Tooltip("用于监听事件的通道资源。要让目标反馈驱动这个通道器，反馈也必须引用同一个通道资源；否则将收不到事件。要创建通道资源，请在项目视图中的任何位置（通常是数据文件夹）右键，选择 更多山脉 > 通道资源，然后为它起一个唯一的名称。")]
		[MMFEnumCondition("ChannelMode", (int)MMChannelModes.MMChannel)]
		public MMChannel MMChannelDefinition = null;

		#if MM_CINEMACHINE || MM_CINEMACHINE3
		protected CinemachineVirtualCameraBase _camera;
		protected int _initialPriority;
		#endif
        
		/// <summary>
		/// On Awake we store our virtual camera
		/// </summary>
		protected virtual void Awake()
		{
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			_camera = this.gameObject.GetComponent<CinemachineVirtualCameraBase>();
			#endif
			#if MM_CINEMACHINE 
			_initialPriority = _camera.Priority;
			#elif MM_CINEMACHINE3
			_initialPriority = _camera.Priority.Value; 
			#endif
		}

		#if MM_CINEMACHINE || MM_CINEMACHINE3
		/// <summary>
		/// When we get an event we change our priorities if needed
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="forceMaxPriority"></param>
		/// <param name="newPriority"></param>
		/// <param name="forceTransition"></param>
		/// <param name="blendDefinition"></param>
		/// <param name="resetValuesAfterTransition"></param>
		public virtual void OnMMCinemachinePriorityEvent(MMChannelData channelData, bool forceMaxPriority, int newPriority, bool forceTransition, CinemachineBlendDefinition blendDefinition, bool resetValuesAfterTransition, TimescaleModes timescaleMode, bool restore = false)
		{
			StartCoroutine(OnMMCinemachinePriorityEventCo(channelData, forceMaxPriority, newPriority, forceTransition,
				blendDefinition, resetValuesAfterTransition, timescaleMode, restore));
		}

		protected virtual IEnumerator OnMMCinemachinePriorityEventCo(MMChannelData channelData, bool forceMaxPriority, int newPriority, bool forceTransition, CinemachineBlendDefinition blendDefinition, bool resetValuesAfterTransition, TimescaleModes timescaleMode, bool restore = false)
		{
			yield return null;
			TimescaleMode = timescaleMode;
			if (MMChannel.Match(channelData, ChannelMode, Channel, MMChannelDefinition))
			{
				if (restore)
				{
					SetPriority(_initialPriority);	
					yield break;
				}
				SetPriority(newPriority);
			}
			else
			{
				if (forceMaxPriority)
				{
					if (restore)
					{
						SetPriority(_initialPriority);	
						yield break;;
					}
					SetPriority(0);
				}
			}
		}
		#endif

		protected virtual void SetPriority(int newPriority)
		{
			#if MM_CINEMACHINE 
			_camera.Priority = newPriority;
			#elif MM_CINEMACHINE3
			PrioritySettings prioritySettings = _camera.Priority;
			prioritySettings.Value = newPriority;
			_camera.Priority = prioritySettings;
			#endif
		}

		/// <summary>
		/// On enable we start listening for events
		/// </summary>
		protected virtual void OnEnable()
		{
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			MMCinemachinePriorityEvent.Register(OnMMCinemachinePriorityEvent);
			#endif
		}

		/// <summary>
		/// Stops listening for events
		/// </summary>
		protected virtual void OnDisable()
		{
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			MMCinemachinePriorityEvent.Unregister(OnMMCinemachinePriorityEvent);
			#endif
		}
	}

	/// <summary>
	/// An event used to pilot priorities on cinemachine virtual cameras and brain transitions
	/// </summary>
	public struct MMCinemachinePriorityEvent
	{
		#if MM_CINEMACHINE || MM_CINEMACHINE3
		static private event Delegate OnEvent;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }

		public delegate void Delegate(MMChannelData channelData, bool forceMaxPriority, int newPriority, bool forceTransition, CinemachineBlendDefinition blendDefinition, bool resetValuesAfterTransition, TimescaleModes timescaleMode, bool restore = false);
		static public void Trigger(MMChannelData channelData, bool forceMaxPriority, int newPriority, bool forceTransition, CinemachineBlendDefinition blendDefinition, bool resetValuesAfterTransition, TimescaleModes timescaleMode, bool restore = false)
		{
			OnEvent?.Invoke(channelData, forceMaxPriority, newPriority, forceTransition, blendDefinition, resetValuesAfterTransition, timescaleMode, restore);
		}
		#endif
	}
}