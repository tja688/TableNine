using System;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	public class MMShaker : MMMonoBehaviour
	{
		[MMInspectorGroup("Shaker Settings", true, 3)]
		/// whether to listen on a channel defined by an int or by a MMChannel scriptable object. Ints are simple to setup but can get messy and make it harder to remember what int corresponds to what.
		/// MMChannel scriptable objects require you to create them in advance, but come with a readable name and are more scalable
		[Tooltip("决定此抖动器是监听 `int` 定义的通道，还是监听 `MMChannel` ScriptableObject 定义的通道。`int` 配置简单，但项目一大就容易混乱，也不便记忆每个数字代表什么；`MMChannel` 需要预先创建资源，但名称更直观，也更适合扩展。")]
		public MMChannelModes ChannelMode = MMChannelModes.Int;
		/// 要监听的频道，必须与对应 feedback 上配置的频道一致。
		[Tooltip("要监听的通道，必须与触发它的反馈上配置的通道一致。")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.Int)]
		public int Channel = 0;
		/// the MMChannel definition asset to use to listen for events. The feedbacks targeting this shaker will have to reference that same MMChannel definition to receive events - to create a MMChannel,
		/// right click anywhere in your project (usually in a Data folder) and go MoreMountains > MMChannel, then name it with some unique name
		[Tooltip("用于监听事件的`通道资源`定义资源。只有引用同一个`通道资源`定义的反馈，才能触发这个增益器。若要创建`通道资源`，可以在项目视图中右键（通常放在数据文件夹中），选择更多山脉 >通道资源，并说明说明。")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.MMChannel)]
		public MMChannel MMChannelDefinition = null;
		/// 本次 shake 的持续时间，单位为秒。
		[Tooltip("本次抖动的持续时间，单位为秒。")]
		public float ShakeDuration = 0.2f;
		/// 若启用，shaker 会在 `Awake` 时立即播放。
		[Tooltip("若启用，抖动器会在 `Awake` 时立即开始播放。")]
		public bool PlayOnAwake = false;
		/// 若启用，只要该 GameObject 处于激活状态，shaker 就会持续摇动。
		[Tooltip("若启用，只要该 GameObject 处于激活状态，抖动器就会持续运行。")]
		public bool PermanentShake = false;
		/// 若启用，在当前 shake 尚未结束时也允许再次触发新的 shake。
		[Tooltip("若启用，在当前抖动尚未结束时也允许再次触发新的抖动。")]
		public bool Interruptible = true;
		/// 若启用，无论 shaker 是通过何种方式触发，结束后都会强制把目标值重置回原始状态。
		[Tooltip("若启用，无论抖动器是通过何种方式触发，结束后都会强制把目标值重置回初始状态。")]
		public bool AlwaysResetTargetValuesAfterShake = false;
		/// 若启用，shaker 会忽略触发事件传入的参数，改为始终使用 Inspector 中当前配置的数值。
		[Tooltip("若启用，抖动器会忽略触发事件传入的参数，改为始终使用 Inspector 中当前配置的数值。开启后，事件里携带的参数将失效。")]
		public bool OnlyUseShakerValues = false;
		/// 一次 shake 结束后的冷却时间，单位为秒；冷却期间不会开始新的 shake。
		[Tooltip("一次抖动结束后的冷却时间，单位为秒；冷却期间不会开始新的抖动。")]
		public float CooldownBetweenShakes = 0f;
		/// 该 shaker 当前是否正处于摇动中。
		[Tooltip("该抖动器当前是否正处于运行中。")]
		[MMFReadOnly]
		public bool Shaking = false;
        
		[HideInInspector] 
		public bool ForwardDirection = true;

		[HideInInspector] 
		public TimescaleModes TimescaleMode = TimescaleModes.Scaled;

		public virtual float GetTime() { return (TimescaleMode == TimescaleModes.Scaled) ? Time.time : Time.unscaledTime; }
		public virtual float GetDeltaTime() { return (TimescaleMode == TimescaleModes.Scaled) ? Time.deltaTime : Time.unscaledDeltaTime; }
		public virtual MMChannelData ChannelData => new MMChannelData(ChannelMode, Channel, MMChannelDefinition);
        
		public virtual bool ListeningToEvents => _listeningToEvents;

		[HideInInspector]
		internal bool _listeningToEvents = false;
		protected float _shakeStartedTimestamp = -Single.MaxValue;
		protected float _shakeStartedTimestampUnscaled = -Single.MaxValue;
		protected float _remappedTimeSinceStart;
		protected bool _resetShakerValuesAfterShake;
		protected bool _resetTargetValuesAfterShake;
		protected float _journey;
        
		/// <summary>
		/// On Awake we grab our volume and profile
		/// </summary>
		protected virtual void Awake()
		{
			Initialization();
			// in case someone else trigger StartListening before Awake
			if (!_listeningToEvents)
			{
				StartListening();
			}
			Shaking = PlayOnAwake;
			this.enabled = PlayOnAwake;
		}

		/// <summary>
		/// Override this method to initialize your shaker
		/// </summary>
		protected virtual void Initialization()
		{
		}

		/// <summary>
		/// Call this externally if you need to force a new initialization
		/// </summary>
		public virtual void ForceInitialization()
		{
			Initialization();
		}

		/// <summary>
		/// Starts shaking the values
		/// </summary>
		public virtual void StartShaking()
		{
			_journey = ForwardDirection ? 0f : ShakeDuration;

			if (InCooldown)
			{
				return;
			}
            
			if (Shaking)
			{
				return;
			}
			else
			{
				this.enabled = true;
				SetShakeStartedTimestamp();
				Shaking = true;
				GrabInitialValues();
				ShakeStarts();
			}
		}

		/// <summary>
		/// Logs the start timestamp for this shaker
		/// </summary>
		protected virtual void SetShakeStartedTimestamp()
		{
			if (TimescaleMode == TimescaleModes.Scaled)
			{
				_shakeStartedTimestamp = GetTime();	
			}
			else
			{
				_shakeStartedTimestampUnscaled = GetTime();
			}
		}

		/// <summary>
		/// Describes what happens when a shake starts
		/// </summary>
		protected virtual void ShakeStarts()
		{

		}

		/// <summary>
		/// A method designed to collect initial values
		/// </summary>
		protected virtual void GrabInitialValues()
		{

		}

		/// <summary>
		/// On Update, we shake our values if needed, or reset if our shake has ended
		/// </summary>
		protected virtual void Update()
		{
			if (Shaking || PermanentShake)
			{
				Shake();
				_journey += ForwardDirection ? GetDeltaTime() : -GetDeltaTime();
			}

			if (Shaking && !PermanentShake && ((_journey < 0) || (_journey > ShakeDuration)))
			{
				Shaking = false;
				ShakeComplete();
			}

			if (PermanentShake)
			{
				if (_journey < 0)
				{
					_journey = ShakeDuration;
				}

				if (_journey > ShakeDuration)
				{
					_journey = 0;
				}
			}
		}

		/// <summary>
		/// Override this method to implement shake over time
		/// </summary>
		protected virtual void Shake()
		{

		}

		/// <summary>
		/// A method used to "shake" a flot over time along a curve
		/// </summary>
		/// <param name="curve"></param>
		/// <param name="remapMin"></param>
		/// <param name="remapMax"></param>
		/// <param name="relativeIntensity"></param>
		/// <param name="initialValue"></param>
		/// <returns></returns>
		protected virtual float ShakeFloat(AnimationCurve curve, float remapMin, float remapMax, bool relativeIntensity, float initialValue)
		{
			float newValue = 0f;
            
			float remappedTime = MMFeedbacksHelpers.Remap(_journey, 0f, ShakeDuration, 0f, 1f);
            
			float curveValue = curve.Evaluate(remappedTime);
			newValue = MMFeedbacksHelpers.Remap(curveValue, 0f, 1f, remapMin, remapMax);
			if (relativeIntensity)
			{
				newValue += initialValue;
			}
			return newValue;
		}

		protected virtual Color ShakeGradient(Gradient gradient)
		{
			float remappedTime = MMFeedbacksHelpers.Remap(_journey, 0f, ShakeDuration, 0f, 1f);
			return gradient.Evaluate(remappedTime);
		}

		/// <summary>
		/// Resets the values on the target
		/// </summary>
		protected virtual void ResetTargetValues()
		{

		}

		/// <summary>
		/// Resets the values on the shaker
		/// </summary>
		protected virtual void ResetShakerValues()
		{

		}

		/// <summary>
		/// Describes what happens when the shake is complete
		/// </summary>
		protected virtual void ShakeComplete()
		{
			_journey = ForwardDirection ? ShakeDuration : 0f;
			Shake();
			
			if (_resetTargetValuesAfterShake || AlwaysResetTargetValuesAfterShake)
			{
				ResetTargetValues();
			}   
			if (_resetShakerValuesAfterShake)
			{
				ResetShakerValues();
			}            
			this.enabled = false;
		}

		/// <summary>
		/// On enable we start shaking if needed
		/// </summary>
		protected virtual void OnEnable()
		{
			StartShaking();
		}
             
		/// <summary>
		/// On destroy we stop listening for events
		/// </summary>
		protected virtual void OnDestroy()
		{
			StopListening();
		}

		/// <summary>
		/// On disable we complete our shake if it was in progress
		/// </summary>
		protected virtual void OnDisable()
		{
			if (Shaking)
			{
				ShakeComplete();
			}
		}

		/// <summary>
		/// Starts this shaker
		/// </summary>
		public virtual void Play()
		{
			if (InCooldown)
			{
				return;
			}
			this.enabled = true;
		}

		/// <summary>
		/// Stops this shaker
		/// </summary>
		public virtual void Stop()
		{
			Shaking = false;
			ShakeComplete();
		}
        
		/// <summary>
		/// Starts listening for events
		/// </summary>
		public virtual void StartListening()
		{
			_listeningToEvents = true;
		}

		/// <summary>
		/// Stops listening for events
		/// </summary>
		public virtual void StopListening()
		{
			_listeningToEvents = false;
		}

		/// <summary>
		/// Returns true if this shaker should listen to events, false otherwise
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		protected virtual bool CheckEventAllowed(MMChannelData channelData, bool useRange = false, float range = 0f, Vector3 eventOriginPosition = default(Vector3))
		{
			if (!MMChannel.Match(channelData, ChannelMode, Channel, MMChannelDefinition))
			{
				return false;
			}
			if (!this.gameObject.activeInHierarchy)
			{
				return false;
			}
			else
			{
				if (useRange)
				{
					if (Vector3.Distance(this.transform.position, eventOriginPosition) > range)
					{
						return false;
					}
				}

				return true;
			}
		}
		
		/// <summary>
		/// Returns true if this shaker is currently in cooldown, false otherwise
		/// </summary>
		public virtual bool InCooldown
		{
			get
			{
				float startedTimeStamp = TimescaleMode == TimescaleModes.Scaled ? _shakeStartedTimestamp : _shakeStartedTimestampUnscaled;

				float test = GetTime() - startedTimeStamp;
				return (GetTime() - startedTimeStamp < CooldownBetweenShakes);	
			}
		}
		
		public virtual float ComputeRangeIntensity(bool useRange, float rangeDistance, bool useRangeFalloff, AnimationCurve rangeFalloff, Vector2 remapRangeFalloff, Vector3 rangePosition)
		{
			if (!useRange)
			{
				return 1f;
			}

			float distanceToCenter = Vector3.Distance(rangePosition, this.transform.position);

			if (distanceToCenter > rangeDistance)
			{
				return 0f;
			}

			if (!useRangeFalloff)
			{
				return 1f;
			}

			float normalizedDistance = MMMaths.Remap(distanceToCenter, 0f, rangeDistance, 0f, 1f);
			float curveValue = rangeFalloff.Evaluate(normalizedDistance);
			float newIntensity = MMMaths.Remap(curveValue, 0f, 1f, remapRangeFalloff.x, remapRangeFalloff.y);
			return newIntensity;
		}
	}
}