using System.Collections;
using UnityEngine;
#if MM_CINEMACHINE
using Cinemachine;
#elif MM_CINEMACHINE3
using Unity.Cinemachine;
#endif
using MoreMountains.Feedbacks;
using MoreMountains.Tools;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// Add this component to your Cinemachine Virtual Camera to have it shake when calling its ShakeCamera methods.
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Cinemachine/MM Cinemachine Camera Shaker")]
	#if MM_CINEMACHINE
	[RequireComponent(typeof(CinemachineVirtualCamera))]
	#elif MM_CINEMACHINE3
	[RequireComponent(typeof(CinemachineCamera))]
	#endif
	public class MMCinemachineCameraShaker : MonoBehaviour 
	{
		[Header("Settings")]
		/// whether to listen on a channel defined by an int or by a MMChannel scriptable object. Ints are simple to setup but can get messy and make it harder to remember what int corresponds to what.
		/// MMChannel 需要预先创建，但具备可读名称，后期也更容易维护和扩展。
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
		/// 如果事件里没有单独指定 amplitude，将使用这里的默认强度。
		[Tooltip("如果事件里没有单独指定 amplitude，将使用这里的默认强度。")]
		public float DefaultShakeAmplitude = .5f;
		/// 如果事件里没有单独指定 frequency，将使用这里的默认频率。
		[Tooltip("如果事件里没有单独指定 frequency，将使用这里的默认频率。")]
		public float DefaultShakeFrequency = 10f;
		/// 相机处于空闲时的噪声强度。
		[Tooltip("相机处于空闲时的噪声强度。")]
		[MMFReadOnly]
		public float IdleAmplitude;
		/// 相机处于空闲时的噪声频率。
		[Tooltip("相机处于空闲时的噪声频率。")]
		[MMFReadOnly]
		public float IdleFrequency = 1f;
		/// 抖动插值的速度。
		[Tooltip("抖动插值的速度。")]
		public float LerpSpeed = 5f;

		[Header("Test")]
		/// 通过 TestShake 按钮测试时使用的持续时间（秒）。
		[Tooltip("通过 TestShake 按钮测试时使用的持续时间（秒）。")]
		public float TestDuration = 0.3f;
		/// 通过 TestShake 按钮测试时使用的强度。
		[Tooltip("通过 TestShake 按钮测试时使用的强度。")]
		public float TestAmplitude = 2f;
		/// 通过 TestShake 按钮测试时使用的频率。
		[Tooltip("通过 TestShake 按钮测试时使用的频率。")]
		public float TestFrequency = 20f;

		[MMFInspectorButton("TestShake")]
		public bool TestShakeButton;

		public virtual float GetTime() { return (_timescaleMode == TimescaleModes.Scaled) ? Time.time : Time.unscaledTime; }
		public virtual float GetDeltaTime() { return (_timescaleMode == TimescaleModes.Scaled) ? Time.deltaTime : Time.unscaledDeltaTime; }

		protected TimescaleModes _timescaleMode;
		protected Vector3 _initialPosition;
		protected Quaternion _initialRotation;
		#if MM_CINEMACHINE
		protected Cinemachine.CinemachineBasicMultiChannelPerlin _perlin;
		protected Cinemachine.CinemachineVirtualCamera _virtualCamera;
		#elif MM_CINEMACHINE3
		protected CinemachineBasicMultiChannelPerlin _perlin;
		protected CinemachineCamera _virtualCamera;
		#endif
		protected float _targetAmplitude;
		protected float _targetFrequency;
		private Coroutine _shakeCoroutine;

		/// <summary>
		/// On awake we grab our components
		/// </summary>
		protected virtual void Awake()
		{
			#if MM_CINEMACHINE
			_virtualCamera = this.gameObject.GetComponent<CinemachineVirtualCamera>();
			_perlin = _virtualCamera.GetCinemachineComponent<Cinemachine.CinemachineBasicMultiChannelPerlin>();
			#elif MM_CINEMACHINE3
			_virtualCamera = this.gameObject.GetComponent<CinemachineCamera>();
			_perlin = _virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;
			#endif
		}

		/// <summary>
		/// On Start we reset our camera to apply our base amplitude and frequency
		/// </summary>
		protected virtual void Start()
		{
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			if (_perlin != null)
			{
				#if MM_CINEMACHINE
				IdleAmplitude = _perlin.m_AmplitudeGain;
				IdleFrequency = _perlin.m_FrequencyGain;
				#elif MM_CINEMACHINE3
				IdleAmplitude = _perlin.AmplitudeGain;
				IdleFrequency = _perlin.FrequencyGain;
				#endif
			}            
			#endif

			_targetAmplitude = IdleAmplitude;
			_targetFrequency = IdleFrequency;
		}

		protected virtual void Update()
		{
			#if MM_CINEMACHINE
			if (_perlin != null)
			{
				_perlin.m_AmplitudeGain = _targetAmplitude;
				_perlin.m_FrequencyGain = Mathf.Lerp(_perlin.m_FrequencyGain, _targetFrequency, GetDeltaTime() * LerpSpeed);
			}
			#elif MM_CINEMACHINE3
			if (_perlin != null)
			{
				_perlin.AmplitudeGain = _targetAmplitude;
				_perlin.FrequencyGain = Mathf.Lerp(_perlin.FrequencyGain, _targetFrequency, GetDeltaTime() * LerpSpeed);
			}
			#endif
		}

		/// <summary>
		/// Use this method to shake the camera for the specified duration (in seconds) with the default amplitude and frequency
		/// </summary>
		/// <param name="duration">Duration.</param>
		public virtual void ShakeCamera(float duration, bool infinite, bool useUnscaledTime = false)
		{
			StartCoroutine(ShakeCameraCo(duration, DefaultShakeAmplitude, DefaultShakeFrequency, infinite, useUnscaledTime));
		}

		/// <summary>
		/// Use this method to shake the camera for the specified duration (in seconds), amplitude and frequency
		/// </summary>
		/// <param name="duration">Duration.</param>
		/// <param name="amplitude">Amplitude.</param>
		/// <param name="frequency">Frequency.</param>
		public virtual void ShakeCamera(float duration, float amplitude, float frequency, bool infinite, bool useUnscaledTime = false)
		{
			if (_shakeCoroutine != null)
			{
				StopCoroutine(_shakeCoroutine);
			}
			_shakeCoroutine = StartCoroutine(ShakeCameraCo(duration, amplitude, frequency, infinite, useUnscaledTime));
		}

		/// <summary>
		/// This coroutine will shake the 
		/// </summary>
		/// <returns>The camera co.</returns>
		/// <param name="duration">Duration.</param>
		/// <param name="amplitude">Amplitude.</param>
		/// <param name="frequency">Frequency.</param>
		protected virtual IEnumerator ShakeCameraCo(float duration, float amplitude, float frequency, bool infinite, bool useUnscaledTime)
		{
			_targetAmplitude  = amplitude;
			_targetFrequency = frequency;
			_timescaleMode = useUnscaledTime ? TimescaleModes.Unscaled : TimescaleModes.Scaled;
			if (!infinite)
			{
				yield return MMCoroutine.WaitFor(duration);
				CameraReset();
			}                        
		}

		/// <summary>
		/// Resets the camera's noise values to their idle values
		/// </summary>
		public virtual void CameraReset()
		{
			_targetAmplitude = IdleAmplitude;
			_targetFrequency = IdleFrequency;
		}

		public virtual void OnCameraShakeEvent(float duration, float amplitude, float frequency, float amplitudeX, float amplitudeY, float amplitudeZ, bool infinite, MMChannelData channelData, bool useUnscaledTime)
		{
			if (!MMChannel.Match(channelData, ChannelMode, Channel, MMChannelDefinition))
			{
				return;
			}
			this.ShakeCamera(duration, amplitude, frequency, infinite, useUnscaledTime);
		}

		public virtual void OnCameraShakeStopEvent(MMChannelData channelData)
		{
			if (!MMChannel.Match(channelData, ChannelMode, Channel, MMChannelDefinition))
			{
				return;
			}
			if (_shakeCoroutine != null)
			{
				StopCoroutine(_shakeCoroutine);
			}            
			CameraReset();
		}

		protected virtual void OnEnable()
		{
			MMCameraShakeEvent.Register(OnCameraShakeEvent);
			MMCameraShakeStopEvent.Register(OnCameraShakeStopEvent);
		}

		protected virtual void OnDisable()
		{
			MMCameraShakeEvent.Unregister(OnCameraShakeEvent);
			MMCameraShakeStopEvent.Unregister(OnCameraShakeStopEvent);
		}

		protected virtual void TestShake()
		{
			MMCameraShakeEvent.Trigger(TestDuration, TestAmplitude, TestFrequency, 0f, 0f, 0f, false, new MMChannelData(ChannelMode, Channel, MMChannelDefinition));
		}
	}
}