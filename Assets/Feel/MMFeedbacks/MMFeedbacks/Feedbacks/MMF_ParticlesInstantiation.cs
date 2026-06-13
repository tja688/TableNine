using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will instantiate a particle system and play/stop it when playing/stopping the feedback
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈会在 Start 或 Play 时，于指定位置实例化指定的 ParticleSystem，并可选择是否将其嵌套到父层级下。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Particles/Particles Instantiation")]
	public class MMF_ParticlesInstantiation : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(DeclaredDuration); } set { DeclaredDuration = value;  } }
		#if UNITY_EDITOR
		/// sets the inspector color for this feedback
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.ParticlesColor; } }
		public override bool EvaluateRequiresSetup() { return (ParticlesPrefab == null); }
		public override string RequiredTargetText { get { return ParticlesPrefab != null ? ParticlesPrefab.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置a ParticlesPrefab才能正常工作。你可以在下方进行设置。"; } }
		#endif
		/// the different ways to position the instantiated object :
		/// - FeedbackPosition : object will be instantiated at the position of the feedback, plus an optional offset
		/// - Transform : the object will be instantiated at the specified Transform's position, plus an optional offset
		/// - WorldPosition : the object will be instantiated at the specified world position vector, plus an optional offset
		/// - Script : the position passed in parameters when calling the feedback
		public enum PositionModes { FeedbackPosition, Transform, WorldPosition, Script }
		/// the possible delivery modes
		/// - cached : will cache a copy of the particle system and reuse it
		/// - on demand : will instantiate a new particle system for every play
		public enum Modes { Cached, OnDemand, Pool }

		[MMFInspectorGroup("Particles Instantiation", true, 37, true)]
		/// 粒子系统是否在首次使用时缓存（否则按需即时创建）
		[Tooltip("粒子系统是否在首次使用时缓存（否则按需即时创建）")]
		public Modes Mode = Modes.Pool;
		
		/// the initial and planned size of this object pool
		[Tooltip("该对象池的初始/计划容量")]
		[MMFEnumCondition("Mode", (int)Modes.Pool)]
		public int ObjectPoolSize = 5;
		/// whether or not to create a new pool even if one already exists for that same prefab
		[Tooltip("即使该 Prefab 已存在对象池，是否仍强制创建新池")]
		[MMFEnumCondition("Mode", (int)Modes.Pool)]
		public bool MutualizePools = false;
		/// if specified, the instantiated object (or the pool of objects) will be parented to this transform 
		[Tooltip("若指定该项，实例化对象（或对象池中的对象）会挂到此 Transform 下")]
		[MMFEnumCondition("Mode", (int)Modes.Pool)]
		public Transform ParentTransform;
		
		/// if this is false, a brand new particle system will be created every time
		[Tooltip("若关闭，每次都会创建全新的粒子系统实例")]
		[MMFEnumCondition("Mode", (int)Modes.OnDemand)]
		public bool CachedRecycle = true;
		
		[Header("Particle Prefabs")]
		/// 要实例化的粒子系统
		[Tooltip("要实例化的粒子系统")]
		public ParticleSystem ParticlesPrefab;
		/// 可用于随机实例化的粒子系统列表
		[Tooltip("可用于随机实例化的粒子系统列表")]
		public List<ParticleSystem> RandomParticlePrefabs;

		[Header("Weights")] 
		public int MainParticlesPrefabWeight = 1;
		public List<int> RandomParticleWeights = new List<int>();
		
		[Header("Settings")]
		/// if this is true, the particle system game object will be activated on Play, useful if you've somehow disabled it in a past Play
		[Tooltip("若开启，Play 时会激活粒子系统 GameObject（适合之前被关闭过的情况）")]
		public bool ForceSetActiveOnPlay = false;
		/// if this is true, the particle system will be stopped every time the feedback is reset - usually before play
		[Tooltip("若开启，每次反馈 Reset（通常在播放前）都会先停止粒子系统")]
		public bool StopOnReset = false;
		/// the duration for the player to consider. This won't impact your particle system, but is a way to communicate to the MMF Player the duration of this feedback. Usually you'll want it to match your actual particle system, and setting it can be useful to have this feedback work with holding pauses.
		[Tooltip("供播放器参考的持续时间。它不会直接影响你的粒子系统，而是用于向 MMF_Player 声明此反馈应持续多久。通常建议将其设置为与你的实际粒子时长一致，这样在使用 Holding Pause 时才能正确协同工作。")]
		public float DeclaredDuration = 0f;
		/// set this to true to override the target particle system(s) StopAction, forcing a disable or destroy for instance when the particle system stops. If you're pooling your particle systems, don't have them destroy on stop
		[Tooltip("若开启，将覆盖目标粒子系统的 StopAction（例如强制 Disable/Destroy）。若你在使用对象池，请勿设置为 Destroy。")]
		public bool ForceStopAction = false;
		/// if ForceStopAction is true, this will override the target particle system(s) StopAction 
		[Tooltip("当 ForceStopAction 开启时，此项会覆盖目标粒子系统的 StopAction")]
		[MMFCondition("ForceStopAction", true)]
		public ParticleSystemStopAction StopAction = ParticleSystemStopAction.None;

		[MMFInspectorGroup("Position", true, 29)]
		/// the selected position mode
		[Tooltip("当前位置模式")]
		public PositionModes PositionMode = PositionModes.FeedbackPosition;
		/// the position at which to spawn this particle system
		[Tooltip("生成该粒子系统的位置")]
		[MMFEnumCondition("PositionMode", (int)PositionModes.Transform)]
		public Transform InstantiateParticlesPosition;
		/// the world position to move to when in WorldPosition mode 
		[Tooltip("在 WorldPosition 模式下要移动到的世界坐标")]
		[MMFEnumCondition("PositionMode", (int)PositionModes.WorldPosition)]
		public Vector3 TargetWorldPosition;
		/// 生成位置偏移
		[Tooltip("生成位置偏移")]
		public Vector3 Offset;
		/// whether or not the particle system should be nested in hierarchy or floating on its own
		[Tooltip("粒子系统是作为层级子物体嵌套，还是独立存在")]
		public bool NestParticles = true;
		/// whether or not to also apply rotation
		[Tooltip("是否也应用轮换")]
		public bool ApplyRotation = false;
		/// whether or not to also apply scale
		[Tooltip("是否同时应用缩放")]
		public bool ApplyScale = false;

		[MMFInspectorGroup("Simulation Speed", true, 43, false)]
		/// whether or not to force a specific simulation speed on the target particle system(s)
		[Tooltip("是否强制目标粒子系统使用指定 Simulation Speed")]
		public bool ForceSimulationSpeed = false;
		/// The min and max values at which to randomize the simulation speed, if ForceSimulationSpeed is true. A new value will be randomized every time this feedback plays
		[Tooltip("当 ForceSimulationSpeed 开启时，Simulation Speed 会在该范围内随机；每次播放都会重新随机。")]
		[MMFCondition("ForceSimulationSpeed", true)]
		public Vector2 ForcedSimulationSpeed = new Vector2(0.1f,1f);

		/// the particle system instantiated by this feedback
		public ParticleSystem InstantiatedParticleSystem => _instantiatedParticleSystem;
		/// the particle systems instantiated by this feedback
		public List<ParticleSystem> InstantiatedRandomParticleSystems => _instantiatedRandomParticleSystems;

		protected ParticleSystem _instantiatedParticleSystem;
		protected List<ParticleSystem> _instantiatedRandomParticleSystems;

		protected MMMiniObjectPooler _objectPooler; 
		protected List<MMMiniObjectPooler> _objectPoolers;
		protected GameObject _newGameObject;
		protected bool _poolCreatedOrFound = false;
		protected Vector3 _scriptPosition;
		protected MMShufflebag<int> _weightShuffleBag;
		
		/// <summary>
		/// On init, instantiates the particle system, positions it and nests it if needed
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			if (!Active)
			{
				return;
			}
			
			CacheParticleSystem();
			CreatePools(owner);
			InitializeWeights();
		}
		
		protected virtual bool ShouldCache => (Mode == Modes.OnDemand && CachedRecycle) || (Mode == Modes.Cached);

		protected virtual void CreatePools(MMF_Player owner)
		{
			if (Mode != Modes.Pool)
			{
				return;
			}

			if (RandomParticlePrefabs == null)
			{
				RandomParticlePrefabs = new List<ParticleSystem>();
			}

			if ((ParticlesPrefab == null) && (RandomParticlePrefabs.Count == 0))
			{
				return;
			}

			if (!_poolCreatedOrFound)
			{
				if (_objectPooler != null)
				{
					_objectPooler.DestroyObjectPool();
					owner.ProxyDestroy(_objectPooler.gameObject);
				}

				GameObject objectPoolGo = new GameObject();
				objectPoolGo.name = Owner.name+"_ObjectPooler";
				_objectPooler = objectPoolGo.AddComponent<MMMiniObjectPooler>();
				_objectPooler.GameObjectToPool = ParticlesPrefab.gameObject;
				_objectPooler.PoolSize = ObjectPoolSize;
				_objectPooler.NestWaitingPool = NestParticles;
				if (ParentTransform != null)
				{
					_objectPooler.transform.SetParent(ParentTransform);
				}
				else
				{
					_objectPooler.transform.SetParent(Owner.transform);
				}
				_objectPooler.MutualizeWaitingPools = MutualizePools;
				_objectPooler.FillObjectPool();
				if ((Owner != null) && (objectPoolGo.transform.parent == null))
				{
					SceneManager.MoveGameObjectToScene(objectPoolGo, Owner.gameObject.scene);    
				}
				_poolCreatedOrFound = true;

				if (RandomParticlePrefabs.Count > 0)
				{
					_objectPoolers = new List<MMMiniObjectPooler>();
					_objectPoolers.Add(_objectPooler);
					foreach (ParticleSystem ps in RandomParticlePrefabs)
					{
						GameObject randomObjectPoolGo = new GameObject();
						randomObjectPoolGo.name = Owner.name+"_"+ps.name+"_ObjectPooler";
						MMMiniObjectPooler objectPooler = randomObjectPoolGo.AddComponent<MMMiniObjectPooler>();
						objectPooler.GameObjectToPool = ps.gameObject;
						objectPooler.PoolSize = ObjectPoolSize;
						objectPooler.NestWaitingPool = NestParticles;
						if (ParentTransform != null)
						{
							objectPooler.transform.SetParent(ParentTransform);
						}
						else
						{
							objectPooler.transform.SetParent(Owner.transform);
						}
						objectPooler.MutualizeWaitingPools = MutualizePools;
						objectPooler.FillObjectPool();
						if ((Owner != null) && (randomObjectPoolGo.transform.parent == null))
						{
							SceneManager.MoveGameObjectToScene(randomObjectPoolGo, Owner.gameObject.scene);    
						}
						_objectPoolers.Add(objectPooler);
					}
				}
			}
			
		}
		
		protected virtual void CacheParticleSystem()
		{
			if (!ShouldCache)
			{
				return;
			}

			InstantiateParticleSystem();
		}

		protected virtual void InitializeWeights()
		{
			if (RandomParticleWeights.Count != RandomParticlePrefabs.Count)
			{
				RandomParticleWeights = new List<int>();
				for (int i = 0; i < RandomParticlePrefabs.Count; i++)
				{
					RandomParticleWeights.Add(1);
				}
			}

			int size = Mode == Modes.Pool ? RandomParticleWeights.Count + 1 : RandomParticleWeights.Count;
			_weightShuffleBag = new MMShufflebag<int>(size);
			if (Mode == Modes.Pool)
			{
				_weightShuffleBag.Add(0, MainParticlesPrefabWeight);	
			}
			for (int i = 0; i < RandomParticleWeights.Count; i++)
			{
				int newIndex = Mode == Modes.Pool ? i+1 : i;
				_weightShuffleBag.Add(newIndex, RandomParticleWeights[i]);
			}
		}

		/// <summary>
		/// Instantiates the particle system
		/// </summary>
		protected virtual void InstantiateParticleSystem()
		{
			Transform newParent = null;
            
			if (NestParticles)
			{
				if (PositionMode == PositionModes.FeedbackPosition)
				{
					newParent = Owner.transform;
				}
				if (PositionMode == PositionModes.Transform)
				{
					newParent = InstantiateParticlesPosition;
				}
			}
			
			if (RandomParticlePrefabs.Count > 0)
			{
				if (ShouldCache)
				{
					_instantiatedRandomParticleSystems = new List<ParticleSystem>();
					foreach(ParticleSystem system in RandomParticlePrefabs)
					{
						ParticleSystem newSystem = GameObject.Instantiate(system, newParent) as ParticleSystem;
						if (newParent == null)
						{
							SceneManager.MoveGameObjectToScene(newSystem.gameObject, Owner.gameObject.scene);    
						}
						newSystem.Stop();
						_instantiatedRandomParticleSystems.Add(newSystem);
					}
				}
				else
				{
					int random = _weightShuffleBag.Pick();
					_instantiatedParticleSystem = GameObject.Instantiate(RandomParticlePrefabs[random], newParent) as ParticleSystem;
					if (newParent == null)
					{
						SceneManager.MoveGameObjectToScene(_instantiatedParticleSystem.gameObject, Owner.gameObject.scene);    
					}
				}
			}
			else
			{
				if (ParticlesPrefab == null)
				{
					return;
				}
				_instantiatedParticleSystem = GameObject.Instantiate(ParticlesPrefab, newParent) as ParticleSystem;
				_instantiatedParticleSystem.Stop();
				if (newParent == null)
				{
					SceneManager.MoveGameObjectToScene(_instantiatedParticleSystem.gameObject, Owner.gameObject.scene);    
				}
			}
			
			if (_instantiatedParticleSystem != null)
			{
				PositionParticleSystem(_instantiatedParticleSystem);
			}

			if ((_instantiatedRandomParticleSystems != null) && (_instantiatedRandomParticleSystems.Count > 0))
			{
				foreach (ParticleSystem system in _instantiatedRandomParticleSystems)
				{
					PositionParticleSystem(system);
				}
			}
		}

		protected virtual void PositionParticleSystem(ParticleSystem system)
		{
			if (InstantiateParticlesPosition == null)
			{
				if (Owner != null)
				{
					InstantiateParticlesPosition = Owner.transform;
				}
			}

			if (system != null)
			{
				system.Stop();
				
				system.transform.position = GetPosition(Owner.transform.position);
				
				if (ApplyRotation)
				{
					system.transform.rotation = GetRotation(Owner.transform);    
				}

				if (ApplyScale)
				{
					system.transform.localScale = GetScale(Owner.transform);    
				}
            
				system.Clear();
			}
		}

		/// <summary>
		/// Gets the desired rotation of that particle system
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		protected virtual Quaternion GetRotation(Transform target)
		{
			switch (PositionMode)
			{
				case PositionModes.FeedbackPosition:
					return Owner.transform.rotation;
				case PositionModes.Transform:
					return InstantiateParticlesPosition.rotation;
				case PositionModes.WorldPosition:
					return Quaternion.identity;
				case PositionModes.Script:
					return Owner.transform.rotation;
				default:
					return Owner.transform.rotation;
			}
		}

		/// <summary>
		/// Gets the desired scale of that particle system
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		protected virtual Vector3 GetScale(Transform target)
		{
			switch (PositionMode)
			{
				case PositionModes.FeedbackPosition:
					return Owner.transform.localScale;
				case PositionModes.Transform:
					return InstantiateParticlesPosition.localScale;
				case PositionModes.WorldPosition:
					return Owner.transform.localScale;
				case PositionModes.Script:
					return Owner.transform.localScale;
				default:
					return Owner.transform.localScale;
			}
		}

		/// <summary>
		/// Gets the position 
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		protected virtual Vector3 GetPosition(Vector3 position)
		{
			switch (PositionMode)
			{
				case PositionModes.FeedbackPosition:
					return Owner.transform.position + Offset;
				case PositionModes.Transform:
					return InstantiateParticlesPosition.position + Offset;
				case PositionModes.WorldPosition:
					return TargetWorldPosition + Offset;
				case PositionModes.Script:
					return _scriptPosition + Offset;
				default:
					return _scriptPosition + Offset;
			}
		}

		/// <summary>
		/// On Play, plays the feedback
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			_scriptPosition = position;
			
			if (Mode == Modes.Pool)
			{
				if (RandomParticlePrefabs.Count == 0)
				{
					if (_objectPooler != null)
					{
						_newGameObject = _objectPooler.GetPooledGameObject();
						_instantiatedParticleSystem = _newGameObject.MMFGetComponentNoAlloc<ParticleSystem>();
						if (_instantiatedParticleSystem != null)
						{
							PositionParticleSystem(_instantiatedParticleSystem);
							_newGameObject.SetActive(true);
						}
					}	
				}
				else
				{
					int randomIndex = _weightShuffleBag.Pick();
					_newGameObject = _objectPoolers[randomIndex].GetPooledGameObject();
					_instantiatedParticleSystem = _newGameObject.MMFGetComponentNoAlloc<ParticleSystem>();
					if (_instantiatedParticleSystem != null)
					{
						PositionParticleSystem(_instantiatedParticleSystem);
						_newGameObject.SetActive(true);
					}
				}
			}
			else
			{
				if (!ShouldCache)
				{
					InstantiateParticleSystem();
				}
				else
				{
					GrabCachedParticleSystem();
				}
			}
			
			if (_instantiatedParticleSystem != null)
			{
				if (ForceSetActiveOnPlay)
				{
					_instantiatedParticleSystem.gameObject.SetActive(true);
				}
				_instantiatedParticleSystem.Stop();
				_instantiatedParticleSystem.transform.position = GetPosition(position);
				PositionParticleSystem(_instantiatedParticleSystem);
				_instantiatedParticleSystem.gameObject.SetActive(true);
				PlayTargetParticleSystem(_instantiatedParticleSystem);
			}

			if ((_instantiatedRandomParticleSystems != null) && (_instantiatedRandomParticleSystems.Count > 0))
			{
				foreach (ParticleSystem system in _instantiatedRandomParticleSystems)
				{
                    
					if (ForceSetActiveOnPlay)
					{
						system.gameObject.SetActive(true);
					}
					system.Stop();
					system.transform.position = GetPosition(position);
				}
				int random = _weightShuffleBag.Pick();
				PlayTargetParticleSystem(_instantiatedRandomParticleSystems[random]);
			}
		}

		/// <summary>
		/// Forces the sim speed if needed, then plays the target particle system
		/// </summary>
		/// <param name="targetParticleSystem"></param>
		protected virtual void PlayTargetParticleSystem(ParticleSystem targetParticleSystem)
		{
			if (ForceStopAction)
			{
				ParticleSystem.MainModule main = targetParticleSystem.main;
				main.stopAction = StopAction;
			}
			if (ForceSimulationSpeed)
			{
				ParticleSystem.MainModule main = targetParticleSystem.main;
				main.simulationSpeed = Random.Range(ForcedSimulationSpeed.x, ForcedSimulationSpeed.y);
			}
			targetParticleSystem.Play();
		}

		/// <summary>
		/// Grabs and stores a random particle prefab
		/// </summary>
		protected virtual void GrabCachedParticleSystem()
		{
			if (RandomParticlePrefabs.Count > 0)
			{
				int random = Random.Range(0, RandomParticlePrefabs.Count);
				_instantiatedParticleSystem = _instantiatedRandomParticleSystems[random];
			}
		}

		/// <summary>
		/// On Stop, stops the feedback
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			if (_instantiatedParticleSystem != null)
			{
				_instantiatedParticleSystem?.Stop();
			}    
			if ((_instantiatedRandomParticleSystems != null) && (_instantiatedRandomParticleSystems.Count > 0))
			{
				foreach(ParticleSystem system in _instantiatedRandomParticleSystems)
				{
					system.Stop();
				}
			}
		}

		/// <summary>
		/// On Reset, stops the feedback
		/// </summary>
		protected override void CustomReset()
		{
			base.CustomReset();
            
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (InCooldown)
			{
				return;
			}

			if (StopOnReset && (_instantiatedParticleSystem != null))
			{
				_instantiatedParticleSystem.Stop();
			}
			if ((_instantiatedRandomParticleSystems != null) && (_instantiatedRandomParticleSystems.Count > 0))
			{
				foreach (ParticleSystem system in _instantiatedRandomParticleSystems)
				{
					system.Stop();
				}
			}
		}
	}
}


