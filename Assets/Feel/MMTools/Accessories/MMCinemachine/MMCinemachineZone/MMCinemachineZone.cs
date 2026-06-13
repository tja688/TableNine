using System.Collections;
using System.Collections.Generic;
#if MM_CINEMACHINE
using Cinemachine;
#elif MM_CINEMACHINE3
using Unity.Cinemachine;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace MoreMountains.Tools
{
	/// <summary>
	/// An abstract class that lets you define a zone that, when entered, enables a virtual camera, and takes care
	/// of all the boilerplate setup
	/// </summary>
	[AddComponentMenu("")]
	[ExecuteAlways]
	public abstract class MMCinemachineZone : MonoBehaviour
	{
		public enum Modes { Enable, Priority }
        
		[Header("Virtual Camera")]
		/// whether to enable/disable virtual cameras, or to play on their priority for transitions
		[Tooltip("相机切换方式。Enable 通过启用/禁用虚拟相机切换；Priority 通过优先级切换（此时会使用下方优先级字段）")]
		public Modes Mode = Modes.Priority;
		/// whether or not the camera in this zone should start active
		[Tooltip("该区域对应相机是否在启动时处于激活状态")]
		public bool CameraStartsActive = false;
		#if MM_CINEMACHINE
		/// the virtual camera associated to this zone (will try to grab one in children if none is set) 
		[Tooltip("与该区域关联的虚拟相机。若留空，运行时会尝试从子物体自动获取")]
		public CinemachineVirtualCamera VirtualCamera;
		#elif MM_CINEMACHINE3
		/// the virtual camera associated to this zone (will try to grab one in children if none is set)
		[Tooltip("与该区域关联的虚拟相机。若留空，运行时会尝试从子物体自动获取")]
		public CinemachineCamera VirtualCamera;
		#endif

		/// when in priority mode, the priority this camera should have when the zone is active
		[Tooltip("Mode 为 Priority 且区域激活时，此相机使用的优先级")]
		[MMEnumCondition("Mode", (int)Modes.Priority)]
		public int EnabledPriority = 10;
		/// when in priority mode, the priority this camera should have when the zone is inactive
		[Tooltip("Mode 为 Priority 且区域未激活时，此相机使用的优先级")]
		[MMEnumCondition("Mode", (int)Modes.Priority)]
		public int DisabledPriority = 0;

		[Header("Collisions")] 
		/// a layermask containing all the layers that should activate this zone
		[Tooltip("可触发该区域的 LayerMask；仅这些层的对象进入/离开时会触发切换")]
		public LayerMask TriggerMask;
        
		[Header("Confiner Setup")] 
		/// whether or not the zone should auto setup its camera's confiner on start - alternative is to manually click the ManualSetupConfiner, or do your own setup
		[Tooltip("是否在启动时自动配置相机 Confiner。关闭后需手动点击 ManualSetupConfiner，或自行完成 Confiner 配置")]
		public bool SetupConfinerOnStart = false;

		/// a debug button used to setup the confiner on click
		[MMInspectorButton("ManualSetupConfiner")]
		public bool GenerateConfinerSetup;
		
		[Header("State")]
		/// whether this room is the current room or not
		[Tooltip("该区域当前是否为激活区域（运行时状态）")]
		[MMReadOnly]
		public bool CurrentRoom = false;
		/// whether this room has already been visited or not
		[Tooltip("该区域是否曾被进入过（运行时状态）")]
		public bool RoomVisited = false;

		[Header("Events")] 
		/// a UnityEvent to trigger when entering the zone for the first time
		[Tooltip("第一次进入该区域时触发的 UnityEvent（仅首次触发）")]
		public UnityEvent OnEnterZoneForTheFirstTimeEvent;
		/// a UnityEvent to trigger when entering the zone
		[Tooltip("每次进入该区域时触发的 UnityEvent")]
		public UnityEvent OnEnterZoneEvent;
		/// a UnityEvent to trigger when exiting the zone
		[Tooltip("离开该区域时触发的 统一事件")]
		public UnityEvent OnExitZoneEvent;

		[Header("Activation")]

		/// a list of gameobjects to enable when entering the zone, and disable when exiting it
		[Tooltip("进入区域时启用、离开区域时禁用的 GameObject 列表")]
		public List<GameObject> ActivationList;

		[Header("Debug")] 
		/// whether or not to draw shape gizmos to help visualize the zone's bounds
		[Tooltip("是否绘制 Gizmo 以辅助可视化区域边界")]
		public bool DrawGizmos = true;
		/// the color of the gizmos to draw in edit mode
		[Tooltip("编辑模式下 Gizmo 的显示颜色")] 
		public Color GizmosColor;
        
		protected GameObject _confinerGameObject;
		protected Vector3 _gizmoSize;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		protected static void InitializeStatics()
		{
			foreach (var zone in FindObjectsByType<MMCinemachineZone>(FindObjectsSortMode.None))
			{
				zone.Awake();
			}
		}
        
		/// <summary>
		/// On Awake we proceed to init if app is playing
		/// </summary>
		protected virtual void Awake()
		{
			AlwaysInitialization();
			if (!Application.isPlaying)
			{
				return;
			}
			Initialization();
		}

		/// <summary>
		/// On Awake we initialize our collider
		/// </summary>
		protected virtual void AlwaysInitialization()
		{
			InitializeCollider();
		}

		/// <summary>
		/// On init we grab our virtual camera 
		/// </summary>
		protected virtual void Initialization()
		{
			#if MM_CINEMACHINE
			if (VirtualCamera == null)
			{
				VirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
			}
			#elif MM_CINEMACHINE3
			if (VirtualCamera == null)
			{
				VirtualCamera = GetComponentInChildren<CinemachineCamera>();
			}
			#endif

			#if MM_CINEMACHINE || MM_CINEMACHINE3
			if (VirtualCamera == null)
			{
				Debug.LogWarning("[MMCinemachineZone2D] " + this.name + " : no virtual camera is attached to this zone. Set one in its inspector.");
			}
			#endif

			if (SetupConfinerOnStart)
			{
				SetupConfinerGameObject();	
			}
            
			foreach (GameObject go in ActivationList)
			{
				go.SetActive(false);
			}
		}

		/// <summary>
		/// On Start we setup the confiner
		/// </summary>
		protected virtual void Start()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (SetupConfinerOnStart)
			{
				SetupConfiner();	
			}
			
			StartCoroutine(EnableCamera(CameraStartsActive, 1));
		}

		/// <summary>
		/// Describes what happens when initializing the collider
		/// </summary>
		protected abstract void InitializeCollider();

		/// <summary>
		/// Describes what happens when setting up the confiner
		/// </summary>
		protected abstract void SetupConfiner();

		/// <summary>
		/// A method used to manually create a confiner
		/// </summary>
		protected virtual void ManualSetupConfiner()
		{
			Initialization();
			SetupConfiner();
		}

		/// <summary>
		/// Creates an object to host the confiner
		/// </summary>
		protected virtual void SetupConfinerGameObject()
		{
			// we remove the object if needed
			Transform child = this.transform.Find("Confiner");
			if (child != null)
			{
				DestroyImmediate(child.gameObject);
			}
            
			// we create an empty child object
			_confinerGameObject = new GameObject();
			_confinerGameObject.transform.localPosition = Vector3.zero;
			_confinerGameObject.transform.SetParent(this.transform);
			_confinerGameObject.name = "Confiner";
		}

		/// <summary>
		/// An extra test you can override to add extra collider conditions
		/// </summary>
		/// <param name="collider"></param>
		/// <returns></returns>
		protected virtual bool TestCollidingGameObject(GameObject collider)
		{
			return true;
		}
        
		/// <summary>
		/// Enables the camera, either via enabled state or priority
		/// </summary>
		/// <param name="state"></param>
		/// <param name="frames"></param>
		/// <returns></returns>
		protected virtual IEnumerator EnableCamera(bool state, int frames)
		{
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			if (VirtualCamera == null)
			{
				yield break;
			}
			#endif

			if (frames > 0)
			{
				yield return MMCoroutine.WaitForFrames(frames);    
			}

			#if MM_CINEMACHINE
			if (Mode == Modes.Enable)
			{
				VirtualCamera.enabled = state;
			}
			else if (Mode == Modes.Priority)
			{
				VirtualCamera.Priority = state ? EnabledPriority : DisabledPriority;
			}
			#elif MM_CINEMACHINE3
			if (Mode == Modes.Enable)
			{
				VirtualCamera.enabled = state;
			}
			else if (Mode == Modes.Priority)
			{
				PrioritySettings settings = VirtualCamera.Priority;
				settings.Value = state ? EnabledPriority : DisabledPriority;
				VirtualCamera.Priority = settings;
			}
			#endif
		}

		protected virtual void EnterZone()
		{
			if (!RoomVisited)
			{
				OnEnterZoneForTheFirstTimeEvent.Invoke();	
			}
			
			CurrentRoom = true;
			RoomVisited = true;

			OnEnterZoneEvent.Invoke();
			StartCoroutine(EnableCamera(true, 0));
			foreach(GameObject go in ActivationList)
			{
				go.SetActive(true);
			}
		}

		protected virtual void ExitZone()
		{
			CurrentRoom = false;
			OnExitZoneEvent.Invoke();
			if (this.gameObject.activeInHierarchy)
			{
				StartCoroutine(EnableCamera(false, 0));	
			}
			foreach (GameObject go in ActivationList)
			{
				go.SetActive(false);
			}
		}

		/// <summary>
		/// On Reset we initialize our gizmo color
		/// </summary>
		protected virtual void Reset()
		{
			GizmosColor = MMColors.RandomColor();
			GizmosColor.a = 0.2f;
		}
	}    
}
