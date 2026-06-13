using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoreMountains.Tools;

namespace MoreMountains.Feedbacks
{
	#region Events
	/// <summary>
	/// An event used (usually by feedbacks) to trigger the spawn of a new floating text
	/// </summary>
	public struct MMFloatingTextSpawnEvent
	{
		static private event Delegate OnEvent;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }

		public delegate void Delegate(MMChannelData channelData, Vector3 spawnPosition, string value, Vector3 direction, float intensity,
			bool forceLifetime = false, float lifetime = 1f, bool forceColor = false, Gradient animateColorGradient = null, bool useUnscaledTime = false, Transform attachmentTransform = null);
		static public void Trigger(MMChannelData channelData, Vector3 spawnPosition, string value, Vector3 direction, float intensity,
			bool forceLifetime = false, float lifetime = 1f, bool forceColor = false, Gradient animateColorGradient = null, bool useUnscaledTime = false, Transform attachmentTransform = null)
		{
			OnEvent?.Invoke(channelData, spawnPosition, value, direction, intensity, forceLifetime, lifetime, forceColor, animateColorGradient, useUnscaledTime, attachmentTransform);
		} 
	}
	#endregion

	/// <summary>
	/// This class will let you pool, recycle and spawn floating texts, usually to show damage info.
	/// It requires as input a MMFloatingText object.
	/// </summary>
	public class MMFloatingTextSpawner : MMMonoBehaviour
	{
		/// whether to spawn a single prefab or one at random 
		public  enum PoolerModes { Simple, Multiple }
		/// whether the spawned text should have a fixed alignment, orient to match the initial spawn direction, or its movement curve
		public enum AlignmentModes { Fixed, MatchInitialDirection, MatchMovementDirection }

		[MMInspectorGroup("General Settings", true, 10)]
        
		/// whether to listen on a channel defined by an int or by a MMChannel scriptable object. Ints are simple to setup but can get messy and make it harder to remember what int corresponds to what.
		/// MMChannel scriptable objects require you to create them in advance, but come with a readable name and are more scalable
		[Tooltip("监听通道是使用 int 定义，还是使用 MMChannel ScriptableObject 定义。 int 配置更简单，但项目一大就容易变乱，也更难记住每个数字代表什么。 MMChannel 可脚本化对象要求您提前创建它们，但带有可读的名称，并且更具可扩展性")]
		public MMChannelModes ChannelMode = MMChannelModes.Int;
		/// the channel to listen to - has to match the one on the feedback
		[Tooltip("要监听的通道，必须与反馈上设置的通道一致")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.Int)]
		public int Channel = 0;
		/// the MMChannel definition asset to use to listen for events. The feedbacks targeting this shaker will have to reference that same MMChannel definition to receive events - to create a MMChannel,
		/// right click anywhere in your project (usually in a Data folder) and go MoreMountains > MMChannel, then name it with some unique name
		[Tooltip("要使反馈默认也必须引用此预设器，相关的反馈也必须引用同一个通道资源定义才能接收事件。创建通道资源，右键单击项目中的任意位置（通常在数据文件夹中）并转到 更多山脉 > 通道资源，然后使用唯一名称的命名它")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.MMChannel)]
		public MMChannel MMChannelDefinition = null;
		/// whether or not this spawner can spawn at this time
		[Tooltip("产卵者当前是否允许生成")]
		public bool CanSpawn = true;
		/// whether or not this spawner should spawn objects on unscaled time
		[Tooltip("该 Spawner 是否使用不受时间缩放影响的时间来生成对象")]
		public bool UseUnscaledTime = false;
        
		[MMInspectorGroup("Pooler", true, 24)]

		/// the selected pooler mode (single prefab or multiple ones)
		[Tooltip("当前选择的对象池管理器模式（单 Prefab 或多个 Prefab）")]
		public PoolerModes PoolerMode = PoolerModes.Simple;
		/// the prefab to spawn (ignored if in multiple mode)
		[Tooltip("要生成的 Prefab（在 Multiple 模式下会被忽略）")]
		public MMFloatingText PooledSimpleMMFloatingText;
		/// the prefabs to spawn (ignored if in simple mode)
		[Tooltip("要生成的 Prefab 列表（在 Simple 模式下会被忽略）")]
		public List<MMFloatingText> PooledMultipleMMFloatingText;
		/// the amount of objects to pool to avoid having to instantiate them at runtime. Should be bigger than the max amount of texts you plan on having on screen at any given moment
		[Tooltip("预先放入对象池的对象数量，用于避免运行时频繁实例化。建议设置得大于你预计屏幕上任意时刻可能同时存在的最大文字数量")]
		public int PoolSize = 20;
		/// whether or not to nest the waiting pools
		[Tooltip("是否将待机对象池嵌套到父节点下")]
		public bool NestWaitingPool = true;
		/// whether or not to mutualize the waiting pools
		[Tooltip("是否让待机对象池共用")]
		public bool MutualizeWaitingPools = true;
		/// whether or not the text pool can expand if the pool is empty
		[Tooltip("当对象池为空时，文字对象池是否允许自动扩容")]
		public bool PoolCanExpand = true;

		[MMInspectorGroup("Spawn Settings", true, 14)]

		/// the random min and max lifetime duration for the spawned texts (in seconds)
		[Tooltip("生成文字生命周期的随机最小值与最大值（单位为秒）")]
		[MMVector("Min", "Max")] 
		public Vector2 Lifetime = Vector2.one;
        
		[Header("Spawn Position Offset")]
		/// the random min position at which to spawn the text, relative to its intended spawn position
		[Tooltip("相对其预期生成位置的随机最小生成偏移")]
		public Vector3 SpawnOffsetMin = Vector3.zero;
		/// the random max position at which to spawn the text, relative to its intended spawn position
		[Tooltip("相对其预期生成位置的随机最大生成偏移")]
		public Vector3 SpawnOffsetMax = Vector3.zero;

		[MMInspectorGroup("Animate Position", true, 15)] 
        
		[Header("Movement")]

		/// whether or not to animate the movement of spawned texts
		[Tooltip("是否为生成的文字播放位移动画")]
		public bool AnimateMovement = true;
		/// whether or not to animate the X movement of spawned texts
		[Tooltip("是否为生成文字的 X 方向播放位移动画")]
		public bool AnimateX = false;
		/// the value to which the x movement curve's zero should be remapped to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip("将 X 位移曲线 0 端重新映射到的值，会在 min 与 max 之间随机；若不需要随机，请将两者设为相同")]
		[MMCondition("AnimateX", true)] 
		[MMVector("Min", "Max")]
		public Vector2 RemapXZero = Vector2.zero;
		/// the value to which the x movement curve's one should be remapped to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip("将 X 位移曲线 1 端重新映射到的值，会在 min 与 max 之间随机；若不需要随机，请将两者设为相同")]
		[MMCondition("AnimateX", true)] 
		[MMVector("Min", "Max")]
		public Vector2 RemapXOne = Vector2.one;
		/// the curve on which to animate the x movement
		[Tooltip("用于驱动 X 方向移动的曲线")]
		[MMCondition("AnimateX", true)]
		public AnimationCurve AnimateXCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
		/// whether or not to animate the Y movement of spawned texts
		[Tooltip("是否为生成文字的 Y 方向播放位移动画")]
		public bool AnimateY = true;
		/// the value to which the y movement curve's zero should be remapped to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip("将 Y 位移曲线 0 端重新映射到的值，会在 min 与 max 之间随机；若不需要随机，请将两者设为相同")]
		[MMCondition("AnimateY", true)] 
		[MMVector("Min", "Max")]
		public Vector2 RemapYZero = Vector2.zero;
		/// the value to which the y movement curve's one should be remapped to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip("将 Y 位移曲线 1 端重新映射到的值，会在 min 与 max 之间随机；若不需要随机，请将两者设为相同")]
		[MMCondition("AnimateY", true)]
		[MMVector("Min", "Max")]
		public Vector2 RemapYOne = new Vector2(5f, 5f);
		/// the curve on which to animate the y movement
		[Tooltip("用于驱动 Y 方向移动的曲线")]
		[MMCondition("AnimateY", true)]
		public AnimationCurve AnimateYCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
		/// whether or not to animate the Z movement of spawned texts
		[Tooltip("是否为生成文字的 Z 方向播放位移动画")]
		public bool AnimateZ = false;
		/// the value to which the z movement curve's zero should be remapped to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip("将 Z 位移曲线 0 端重新映射到的值，会在 min 与 max 之间随机；若不需要随机，请将两者设为相同")]
		[MMCondition("AnimateZ", true)] 
		[MMVector("Min", "Max")]
		public Vector2 RemapZZero = Vector2.zero;
		/// the value to which the z movement curve's one should be remapped to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip("将 Z 位移曲线 1 端重新映射到的值，会在 min 与 max 之间随机；若不需要随机，请将两者设为相同")]
		[MMCondition("AnimateZ", true)] 
		[MMVector("Min", "Max")]
		public Vector2 RemapZOne = Vector2.one;
		/// the curve on which to animate the z movement
		[Tooltip("用于驱动 Z 方向移动的曲线")]
		[MMCondition("AnimateZ", true)]
		public AnimationCurve AnimateZCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
        
		[MMInspectorGroup("Facing Directions", true, 16)]
        
		[Header("Alignment")]

		/// the selected alignment mode (whether the spawned text should have a fixed alignment, orient to match the initial spawn direction, or its movement curve)
		[Tooltip("当前选择的对齐模式（生成的文字是保持固定朝向、对齐初始生成方向，还是跟随其移动曲线朝向）")]
		public AlignmentModes AlignmentMode = AlignmentModes.Fixed;
		/// when in fixed mode, the direction in which to keep the spawned texts
		[Tooltip("在 Fixed 模式下，生成文字应保持的方向")]
		[MMEnumCondition("AlignmentMode", (int)AlignmentModes.Fixed)]
		public Vector3 FixedAlignment = Vector3.up;

		[Header("Billboard")]

		/// whether or not spawned texts should always face the camera
		[Tooltip("生成的文字是否始终朝向相机")]
		public bool AlwaysFaceCamera;
		/// whether or not this spawner should automatically grab the main camera on start
		[Tooltip("该 Spawner 是否在启动时自动获取主相机")]
		[MMCondition("AlwaysFaceCamera", true)]
		public bool AutoGrabMainCameraOnStart = true;
		/// if not in auto grab mode, the camera to use for billboards
		[Tooltip("若未启用自动获取模式，用于 Billboard 的相机")]
		[MMCondition("AlwaysFaceCamera", true)]
		public Camera TargetCamera;
                
		[MMInspectorGroup("Animate Scale", true, 46)]

		/// whether or not to animate the scale of spawned texts
		[Tooltip("是否为生成文字播放缩放动画")]
		public bool AnimateScale = true;
		/// the value to which the scale curve's zero should be remapped to
		[Tooltip("将缩放曲线 0 端重新映射到的值")]
		[MMCondition("AnimateScale", true)]
		public Vector2 RemapScaleZero = Vector2.zero;
		/// the value to which the scale curve's one should be remapped to
		[Tooltip("将缩放曲线 1 端重新映射到的值")]
		[MMCondition("AnimateScale", true)]
		public Vector2 RemapScaleOne = Vector2.one;
		/// the curve on which to animate the scale
		[Tooltip("用于驱动缩放的曲线")]
		[MMCondition("AnimateScale", true)]
		public AnimationCurve AnimateScaleCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(0.85f, 1f), new Keyframe(1f, 0f));
        
		[MMInspectorGroup("Animate Color", true, 55)]

		/// whether or not to animate the spawned text's color over time
		[Tooltip("是否让生成文字的颜色随时间变化")]
		public bool AnimateColor = false;
		/// the gradient over which to animate the spawned text's color over time
		[Tooltip("用于驱动生成文字颜色随时间变化的渐变")]
		[GradientUsage(true)]
		public Gradient AnimateColorGradient = new Gradient();

		[MMInspectorGroup("Animate Opacity", true, 45)]

		/// whether or not to animate the opacity of the spawned texts
		[Tooltip("是否为生成文字播放透明度动画")]
		public bool AnimateOpacity = true;
		/// the value to which the opacity curve's zero should be remapped to
		[Tooltip("将透明度曲线 0 端重新映射到的值")]
		[MMCondition("AnimateOpacity", true)]
		public Vector2 RemapOpacityZero = Vector2.zero;
		/// the value to which the opacity curve's one should be remapped to
		[Tooltip("将透明度曲线 1 端重新映射到的值")]
		[MMCondition("AnimateOpacity", true)]
		public Vector2 RemapOpacityOne = Vector2.one;
		/// the curve on which to animate the opacity
		[Tooltip("用于驱动透明度的曲线")]
		[MMCondition("AnimateOpacity", true)]
		public AnimationCurve AnimateOpacityCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f));

		[MMInspectorGroup("Intensity Multipliers", true, 45)]

		/// whether or not the intensity multiplier should impact lifetime
		[Tooltip("强度乘数是否影响生命周期")]
		public bool IntensityImpactsLifetime = false;
		/// when getting an intensity multiplier, the value by which to multiply the lifetime
		[Tooltip("应用强度乘数时，对生命周期施加的乘数")]
		[MMCondition("IntensityImpactsLifetime", true)]
		public float IntensityLifetimeMultiplier = 1f;
		/// whether or not the intensity multiplier should impact movement
		[Tooltip("强度乘数是否影响移动")]
		public bool IntensityImpactsMovement = false;
		/// when getting an intensity multiplier, the value by which to multiply the movement values
		[Tooltip("应用强度乘数时，对移动数值施加的乘数")]
		[MMCondition("IntensityImpactsMovement", true)]
		public float IntensityMovementMultiplier = 1f;
		/// whether or not the intensity multiplier should impact scale
		[Tooltip("强度乘数是否影响缩放")]
		public bool IntensityImpactsScale = false;
		/// when getting an intensity multiplier, the value by which to multiply the scale values
		[Tooltip("应用强度乘数时，对缩放数值施加的乘数")]
		[MMCondition("IntensityImpactsScale", true)]
		public float IntensityScaleMultiplier = 1f;

		[MMInspectorGroup("Debug", true, 12)]

		/// a random value to display when pressing the TestSpawnOne button
		[Tooltip("按下 TestSpawnOne 按钮时要显示的随机值")]
		public Vector2Int DebugRandomValue = new Vector2Int(100, 500);
		/// the min and max bounds within which to pick a value to output when pressing the TestSpawnMany button
		[Tooltip("按下 TestSpawnMany 按钮时，用于随机输出数值的最小/最大范围")]
		[MMVector("Min", "Max")] 
		public Vector2 DebugInterval = new Vector2(0.3f, 0.5f);
		/// a button used to test the spawn of one text
		[Tooltip("用于测试生成单个文字的按钮")]
		[MMInspectorButton("TestSpawnOne")]
		public bool TestSpawnOneBtn;
		/// a button used to start/stop the spawn of texts at regular intervals
		[Tooltip("用于按固定间隔开始/停止生成文字的按钮")]
		[MMInspectorButton("TestSpawnMany")]
		public bool TestSpawnManyBtn;
        
		protected MMObjectPooler _pooler;
		protected MMFloatingText _floatingText;
		protected Coroutine _testSpawnCoroutine;
        
		protected float _lifetime;
		protected float _speed;
		protected Vector3 _spawnOffset;
		protected Vector3 _direction;
		protected Gradient _colorGradient;
		protected bool _animateColor;

		#region Initialization

		/// <summary>
		/// On awake we initialize our spawner
		/// </summary>
		protected virtual void Awake()
		{
			Initialization();
		}

		/// <summary>
		/// On Start we grab our main camera if needed
		/// </summary>
		protected virtual void Start()
		{
			GrabMainCamera();
		}

		/// <summary>
		/// On init, we instantiate our object pool and grab the main camera
		/// </summary>
		protected virtual void Initialization()
		{
			InstantiateObjectPool();
		}

		/// <summary>
		/// Instantiates the specified type of object pool
		/// </summary>
		protected virtual void InstantiateObjectPool()
		{
			if (_pooler == null)
			{
				if (PoolerMode == PoolerModes.Simple)
				{
					InstantiateSimplePool();
				}
				else
				{
					InstantiateMultiplePool();
				}
			}
		}

		/// <summary>
		/// Instantiates a simple object pooler and sets it up
		/// </summary>
		protected virtual void InstantiateSimplePool()
		{
			if (PooledSimpleMMFloatingText == null)
			{
				Debug.LogError(this.name + " : no PooledSimpleMMFloatingText prefab has been set.");
				return;
			}
			GameObject newPooler = new GameObject();
			SceneManager.MoveGameObjectToScene(newPooler, this.gameObject.scene);
			newPooler.name = PooledSimpleMMFloatingText.name + "_Pooler";
			newPooler.transform.SetParent(this.transform);
			MMSimpleObjectPooler simplePooler = newPooler.AddComponent<MMSimpleObjectPooler>();
			simplePooler.PoolSize = PoolSize;
			simplePooler.GameObjectToPool = PooledSimpleMMFloatingText.gameObject;
			simplePooler.NestWaitingPool = NestWaitingPool;
			simplePooler.MutualizeWaitingPools = MutualizeWaitingPools;
			simplePooler.PoolCanExpand = PoolCanExpand;
			simplePooler.FillObjectPool();
			_pooler = simplePooler;
		}

		/// <summary>
		/// Instantiates a multiple object pooler and sets it up
		/// </summary>
		protected virtual void InstantiateMultiplePool()
		{
			GameObject newPooler = new GameObject();
			SceneManager.MoveGameObjectToScene(newPooler, this.gameObject.scene);
			newPooler.name = this.name + "_Pooler";
			newPooler.transform.SetParent(this.transform);
			MMMultipleObjectPooler multiplePooler = newPooler.AddComponent<MMMultipleObjectPooler>();
			multiplePooler.Pool = new List<MMMultipleObjectPoolerObject>();
			foreach (MMFloatingText obj in PooledMultipleMMFloatingText)
			{
				MMMultipleObjectPoolerObject item = new MMMultipleObjectPoolerObject();
				item.GameObjectToPool = obj.gameObject;
				item.PoolCanExpand = PoolCanExpand;
				item.PoolSize = PoolSize;
				item.Enabled = true;
				multiplePooler.Pool.Add(item);
			}
			multiplePooler.NestWaitingPool = NestWaitingPool;
			multiplePooler.MutualizeWaitingPools = MutualizeWaitingPools;
			multiplePooler.FillObjectPool();
			_pooler = multiplePooler;
		}

		/// <summary>
		/// Grabs the main camera if needed
		/// </summary>
		protected virtual void GrabMainCamera()
		{
			if (AutoGrabMainCameraOnStart)
			{
				TargetCamera = Camera.main;
			}
		}

		#endregion
        
		/// <summary>
		/// Spawns a new floating text
		/// </summary>
		/// <param name="value"></param>
		/// <param name="position"></param>
		/// <param name="direction"></param>
		/// <param name="intensity"></param>
		/// <param name="forceLifetime"></param>
		/// <param name="lifetime"></param>
		/// <param name="forceColor"></param>
		/// <param name="animateColorGradient"></param>
		public virtual void Spawn(string value, Vector3 position, Vector3 direction, float intensity = 1f,
			bool forceLifetime = false, float lifetime = 1f, bool forceColor = false, Gradient animateColorGradient = null, 
			Transform attachmentTransform = null)
		{
			if (!CanSpawn)
			{
				return;
			}

			_direction = (direction != Vector3.zero) ? direction + this.transform.up : this.transform.up;

			this.transform.position = position;

			GameObject nextGameObject = _pooler.GetPooledGameObject();

			float lifetimeMultiplier = IntensityImpactsLifetime ? intensity * IntensityLifetimeMultiplier : 1f;
			float movementMultiplier = IntensityImpactsMovement ? intensity * IntensityMovementMultiplier : 1f;
			float scaleMultiplier = IntensityImpactsScale ? intensity * IntensityScaleMultiplier : 1f;

			_lifetime = UnityEngine.Random.Range(Lifetime.x, Lifetime.y) * lifetimeMultiplier;
			_spawnOffset = MMMaths.RandomVector3(SpawnOffsetMin, SpawnOffsetMax);
			_animateColor = AnimateColor;
			_colorGradient = AnimateColorGradient;

			float remapXZero = UnityEngine.Random.Range(RemapXZero.x, RemapXZero.y);
			float remapXOne = UnityEngine.Random.Range(RemapXOne.x, RemapXOne.y) * movementMultiplier;
			float remapYZero = UnityEngine.Random.Range(RemapYZero.x, RemapYZero.y);
			float remapYOne = UnityEngine.Random.Range(RemapYOne.x, RemapYOne.y) * movementMultiplier;
			float remapZZero = UnityEngine.Random.Range(RemapZZero.x, RemapZZero.y);
			float remapZOne = UnityEngine.Random.Range(RemapZOne.x, RemapZOne.y) * movementMultiplier;
			float remapOpacityZero = UnityEngine.Random.Range(RemapOpacityZero.x, RemapOpacityZero.y);
			float remapOpacityOne = UnityEngine.Random.Range(RemapOpacityOne.x, RemapOpacityOne.y);
			float remapScaleZero = UnityEngine.Random.Range(RemapScaleZero.x, RemapOpacityZero.y);
			float remapScaleOne = UnityEngine.Random.Range(RemapScaleOne.x, RemapScaleOne.y) * scaleMultiplier;

			if (forceLifetime)
			{
				_lifetime = lifetime;
			}

			if (forceColor)
			{
				_animateColor = true;
				_colorGradient = animateColorGradient;
			}

			// mandatory checks
			if (nextGameObject==null) { return; }
            
			// we activate the object
			nextGameObject.gameObject.SetActive(true);
			nextGameObject.gameObject.MMGetComponentNoAlloc<MMPoolableObject>().TriggerOnSpawnComplete();
			
			// we position the object
			nextGameObject.transform.position = this.transform.position + _spawnOffset;

			_floatingText = nextGameObject.MMGetComponentNoAlloc<MMFloatingText>();

			_floatingText.FollowTarget.Target = attachmentTransform;
			
			_floatingText.SetUseUnscaledTime(UseUnscaledTime, true);
			_floatingText.ResetPosition();
			_floatingText.SetProperties(value, _lifetime, _direction, AnimateMovement, 
				AlignmentMode, FixedAlignment, AlwaysFaceCamera, TargetCamera,
				AnimateX, AnimateXCurve, remapXZero, remapXOne,
				AnimateY, AnimateYCurve, remapYZero, remapYOne,
				AnimateZ, AnimateZCurve, remapZZero, remapZOne,
				AnimateOpacity, AnimateOpacityCurve, remapOpacityZero, remapOpacityOne,
				AnimateScale, AnimateScaleCurve, remapScaleZero, remapScaleOne,
				_animateColor, _colorGradient);            
		}

		/// <summary>
		/// When we get a floating text event on this spawner's Channel, we spawn a new floating text
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="spawnPosition"></param>
		/// <param name="value"></param>
		/// <param name="direction"></param>
		/// <param name="intensity"></param>
		/// <param name="forceLifetime"></param>
		/// <param name="lifetime"></param>
		/// <param name="forceColor"></param>
		/// <param name="animateColorGradient"></param>
		public virtual void OnMMFloatingTextSpawnEvent(MMChannelData channelData, Vector3 spawnPosition, string value, Vector3 direction, float intensity,
			bool forceLifetime = false, float lifetime = 1f, bool forceColor = false, Gradient animateColorGradient = null, bool useUnscaledTime = false, Transform attachmentTransform = null)
		{
			if (!MMChannel.Match(channelData, ChannelMode, Channel, MMChannelDefinition))
			{
				return;
			}

			UseUnscaledTime = useUnscaledTime;
			Spawn(value, spawnPosition, direction, intensity, forceLifetime, lifetime, forceColor, animateColorGradient, attachmentTransform);
		}
    
		/// <summary>
		/// On enable we start listening for floating text events
		/// </summary>
		protected virtual void OnEnable()
		{
			MMFloatingTextSpawnEvent.Register(OnMMFloatingTextSpawnEvent);
		}
    
		/// <summary>
		/// On disable we stop listening for floating text events
		/// </summary>
		protected virtual void OnDisable()
		{
			MMFloatingTextSpawnEvent.Unregister(OnMMFloatingTextSpawnEvent);
		}

		// Test methods ----------------------------------------------------------------------------------------

		#region TestMethods

		/// <summary>
		/// A test method that spawns one floating text
		/// </summary>
		protected virtual void TestSpawnOne()
		{
			string test = UnityEngine.Random.Range(DebugRandomValue.x, DebugRandomValue.y).ToString();
			Spawn(test, this.transform.position, Vector3.zero);
		}

		/// <summary>
		/// A method used to start/stop the regular spawning of debug floating texts 
		/// </summary>
		protected virtual void TestSpawnMany()
		{
			if (_testSpawnCoroutine == null)
			{
				_testSpawnCoroutine = StartCoroutine(TestSpawnManyCo());    
			}
			else
			{
				StopCoroutine(_testSpawnCoroutine);
				_testSpawnCoroutine = null;
			}
		}

		/// <summary>
		/// A coroutine used to spawn debug floating texts until stopped 
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator TestSpawnManyCo()
		{
			float lastSpawnAt = Time.time;
			float interval = UnityEngine.Random.Range(DebugInterval.x, DebugInterval.y);
			while (true)
			{
				if (Time.time - lastSpawnAt > interval)
				{
					TestSpawnOne();
					lastSpawnAt = Time.time;
					interval = UnityEngine.Random.Range(DebugInterval.x, DebugInterval.y);
				}
				yield return null;
			}
		}
        
		#endregion
	}
}
