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
	/// Add this to a Cinemachine virtual camera and it'll let you control its orthographic size over time, can be piloted by a MMFeedbackCameraOrthographicSize
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Cinemachine/MM Cinemachine Orthographic Size Shaker")]
	#if MM_CINEMACHINE
	[RequireComponent(typeof(CinemachineVirtualCamera))]
	#elif MM_CINEMACHINE3
	[RequireComponent(typeof(CinemachineCamera))]
	#endif
	public class MMCinemachineOrthographicSizeShaker : MMShaker
	{
		[MMInspectorGroup("Orthographic Size", true, 43)]
		/// 是否按相对值应用。若启用，会在初始 Orthographic Size 基础上叠加；若禁用，则直接使用下方数值。
		[Tooltip("是否按相对值应用。若启用，会在初始 Orthographic Size 基础上叠加；若禁用，则直接使用下方数值。")]
		public bool RelativeOrthographicSize = false;
		/// 用于驱动 Orthographic Size 变化的曲线。
		[Tooltip("用于驱动正交尺寸变化的曲线。")]
		public AnimationCurve ShakeOrthographicSize = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// 将曲线 0 端重新映射到的值。
		[Tooltip("将曲线 0 端重新映射到的值。")]
		public float RemapOrthographicSizeZero = 5f;
		/// 将曲线 1 端重新映射到的值。
		[Tooltip("将曲线 1 端重新映射到的值。")]
		public float RemapOrthographicSizeOne = 10f;

		#if MM_CINEMACHINE
		protected CinemachineVirtualCamera _targetCamera;
		#elif  MM_CINEMACHINE3	
		protected CinemachineCamera _targetCamera;
		#endif
		protected float _initialOrthographicSize;
		protected float _originalShakeDuration;
		protected bool _originalRelativeOrthographicSize;
		protected AnimationCurve _originalShakeOrthographicSize;
		protected float _originalRemapOrthographicSizeZero;
		protected float _originalRemapOrthographicSizeOne;

		/// <summary>
		/// On init we initialize our values
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			#if MM_CINEMACHINE
				_targetCamera = this.gameObject.GetComponent<CinemachineVirtualCamera>();
			#elif  MM_CINEMACHINE3	
				_targetCamera = this.gameObject.GetComponent<CinemachineCamera>();
			#endif
		}

		/// <summary>
		/// When that shaker gets added, we initialize its shake duration
		/// </summary>
		protected virtual void Reset()
		{
			ShakeDuration = 0.5f;
		}

		/// <summary>
		/// Shakes values over time
		/// </summary>
		protected override void Shake()
		{
			float newOrthographicSize = ShakeFloat(ShakeOrthographicSize, RemapOrthographicSizeZero, RemapOrthographicSizeOne, RelativeOrthographicSize, _initialOrthographicSize);
			#if MM_CINEMACHINE
			_targetCamera.m_Lens.OrthographicSize = newOrthographicSize;
			#elif  MM_CINEMACHINE3	
			_targetCamera.Lens.OrthographicSize = newOrthographicSize;
			#endif
		}

		/// <summary>
		/// Collects initial values on the target
		/// </summary>
		protected override void GrabInitialValues()
		{
			#if MM_CINEMACHINE
			_initialOrthographicSize = _targetCamera.m_Lens.OrthographicSize;
			#elif  MM_CINEMACHINE3	
			_initialOrthographicSize = _targetCamera.Lens.OrthographicSize;
			#endif
		}

		/// <summary>
		/// When we get the appropriate event, we trigger a shake
		/// </summary>
		/// <param name="distortionCurve"></param>
		/// <param name="duration"></param>
		/// <param name="amplitude"></param>
		/// <param name="relativeDistortion"></param>
		/// <param name="feedbacksIntensity"></param>
		/// <param name="channel"></param>
		public virtual void OnMMCameraOrthographicSizeShakeEvent(AnimationCurve distortionCurve, float duration, float remapMin, float remapMax, bool relativeDistortion = false,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true, bool forwardDirection = true, 
			TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false)
		{
			if (!CheckEventAllowed(channelData))
			{
				return;
			}
            
			if (stop)
			{
				Stop();
				return;
			}
            
			if (restore)
			{
				ResetTargetValues();
				return;
			}
            
			if (!Interruptible && Shaking)
			{
				return;
			}
            
			_resetShakerValuesAfterShake = resetShakerValuesAfterShake;
			_resetTargetValuesAfterShake = resetTargetValuesAfterShake;

			if (resetShakerValuesAfterShake)
			{
				_originalShakeDuration = ShakeDuration;
				_originalShakeOrthographicSize = ShakeOrthographicSize;
				_originalRemapOrthographicSizeZero = RemapOrthographicSizeZero;
				_originalRemapOrthographicSizeOne = RemapOrthographicSizeOne;
				_originalRelativeOrthographicSize = RelativeOrthographicSize;
			}

			if (!OnlyUseShakerValues)
			{
				TimescaleMode = timescaleMode;
				ShakeDuration = duration;
				ShakeOrthographicSize = distortionCurve;
				RemapOrthographicSizeZero = remapMin * feedbacksIntensity;
				RemapOrthographicSizeOne = remapMax * feedbacksIntensity;
				RelativeOrthographicSize = relativeDistortion;
				ForwardDirection = forwardDirection;
			}

			Play();
		}

		/// <summary>
		/// Resets the target's values
		/// </summary>
		protected override void ResetTargetValues()
		{
			base.ResetTargetValues();
			#if MM_CINEMACHINE
			_targetCamera.m_Lens.OrthographicSize = _initialOrthographicSize;
			#elif  MM_CINEMACHINE3	
			_targetCamera.Lens.OrthographicSize = _initialOrthographicSize;
			#endif
		}

		/// <summary>
		/// Resets the shaker's values
		/// </summary>
		protected override void ResetShakerValues()
		{
			base.ResetShakerValues();
			ShakeDuration = _originalShakeDuration;
			ShakeOrthographicSize = _originalShakeOrthographicSize;
			RemapOrthographicSizeZero = _originalRemapOrthographicSizeZero;
			RemapOrthographicSizeOne = _originalRemapOrthographicSizeOne;
			RelativeOrthographicSize = _originalRelativeOrthographicSize;
		}

		/// <summary>
		/// Starts listening for events
		/// </summary>
		public override void StartListening()
		{
			base.StartListening();
			MMCameraOrthographicSizeShakeEvent.Register(OnMMCameraOrthographicSizeShakeEvent);
		}

		/// <summary>
		/// Stops listening for events
		/// </summary>
		public override void StopListening()
		{
			base.StopListening();
			MMCameraOrthographicSizeShakeEvent.Unregister(OnMMCameraOrthographicSizeShakeEvent);
		}
	}
}
