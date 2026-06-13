using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// Add this to a camera and it'll let you control its orthographic size over time, can be piloted by a MMFeedbackCameraOrthographicSize
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Camera/MM Camera Orthographic Size Shaker")]
	[RequireComponent(typeof(Camera))]
	public class MMCameraOrthographicSizeShaker : MMShaker
	{
		[MMInspectorGroup("Orthographic Size", true, 37)]
		/// 是否叠加到初始值上。
		[Tooltip("是否叠加到初始值上。")]
		public bool RelativeOrthographicSize = false;
		/// 用于驱动强度数值变化的曲线。
		[Tooltip("用于驱动强度数值变化的曲线。")]
		public AnimationCurve ShakeOrthographicSize = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// 将曲线的 `0` 值重映射到的目标值。
		[Tooltip("将曲线的 `0` 值重映射到的目标值。")]
		public float RemapOrthographicSizeZero = 5f;
		/// 将曲线的 `1` 值重映射到的目标值。
		[Tooltip("将曲线的 `1` 值重映射到的目标值。")]
		public float RemapOrthographicSizeOne = 10f;

		protected Camera _targetCamera;
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
			_targetCamera = this.gameObject.GetComponent<Camera>();
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
			_targetCamera.orthographicSize = newOrthographicSize;
		}

		/// <summary>
		/// Collects initial values on the target
		/// </summary>
		protected override void GrabInitialValues()
		{
			_initialOrthographicSize = _targetCamera.orthographicSize;
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
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true, 
			bool forwardDirection = true, TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false)
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
			_targetCamera.orthographicSize = _initialOrthographicSize;
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

	/// <summary>
	/// An event used to trigger vignette shakes
	/// </summary>
	public struct MMCameraOrthographicSizeShakeEvent 
	{
		static private event Delegate OnEvent;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }

		public delegate void Delegate(AnimationCurve animCurve, float duration, float remapMin, float remapMax, bool relativeValue = false,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true, bool forwardDirection = true, 
			TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false);

		static public void Trigger(AnimationCurve animCurve, float duration, float remapMin, float remapMax, bool relativeValue = false,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true, bool forwardDirection = true, 
			TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false)
		{
			OnEvent?.Invoke(animCurve, duration, remapMin, remapMax, relativeValue,
				feedbacksIntensity, channelData, resetShakerValuesAfterShake, resetTargetValuesAfterShake, forwardDirection, timescaleMode, stop, restore);
		}
	}
}