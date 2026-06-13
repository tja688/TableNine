using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// Add this to an AudioSource to shake its volume remapped along a curve 
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Audio/MM Audio Source Volume Shaker")]
	[RequireComponent(typeof(AudioSource))]
	public class MMAudioSourceVolumeShaker : MMShaker
	{
		[MMInspectorGroup("Volume", true, 59)]
		/// 是否叠加到初始值上。
		[Tooltip("是否叠加到初始值上。")]
		public bool RelativeVolume = false;
		/// 用于驱动强度数值变化的曲线。
		[Tooltip("用于驱动强度数值变化的曲线。")]
		public AnimationCurve ShakeVolume = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(0.5f, 0f), new Keyframe(1, 1f));
		/// 将曲线的 `0` 值重映射到的目标值。
		[Tooltip("将曲线的 `0` 值重映射到的目标值。")]
		[Range(-1f, 1f)]
		public float RemapVolumeZero = 0f;
		/// 将曲线的 `1` 值重映射到的目标值。
		[Tooltip("将曲线的 `1` 值重映射到的目标值。")]
		[Range(-1f, 1f)]
		public float RemapVolumeOne = 1f;

		/// the audio source to pilot
		protected AudioSource _targetAudioSource;
		protected float _initialVolume;
		protected float _originalShakeDuration;
		protected bool _originalRelativeValues;
		protected AnimationCurve _originalShakeVolume;
		protected float _originalRemapVolumeZero;
		protected float _originalRemapVolumeOne;

		/// <summary>
		/// On init we initialize our values
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			_targetAudioSource = this.gameObject.GetComponent<AudioSource>();
		}
               
		/// <summary>
		/// When that shaker gets added, we initialize its shake duration
		/// </summary>
		protected virtual void Reset()
		{
			ShakeDuration = 2f;
		}

		/// <summary>
		/// Shakes values over time
		/// </summary>
		protected override void Shake()
		{
			float newVolume = ShakeFloat(ShakeVolume, RemapVolumeZero, RemapVolumeOne, RelativeVolume, _initialVolume);
			_targetAudioSource.volume = newVolume;
		}

		/// <summary>
		/// Collects initial values on the target
		/// </summary>
		protected override void GrabInitialValues()
		{
			_initialVolume = _targetAudioSource.volume;
		}

		/// <summary>
		/// When we get the appropriate event, we trigger a shake
		/// </summary>
		/// <param name="volumeCurve"></param>
		/// <param name="duration"></param>
		/// <param name="amplitude"></param>
		/// <param name="relativeVolume"></param>
		/// <param name="feedbacksIntensity"></param>
		/// <param name="channel"></param>
		public virtual void OnMMAudioSourceVolumeShakeEvent(AnimationCurve volumeCurve, float duration, float remapMin, float remapMax, bool relativeVolume = false,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true, 
			bool forwardDirection = true, TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false)
		{
			if (!CheckEventAllowed(channelData) || (!Interruptible && Shaking))
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
            
			_resetShakerValuesAfterShake = resetShakerValuesAfterShake;
			_resetTargetValuesAfterShake = resetTargetValuesAfterShake;

			if (resetShakerValuesAfterShake)
			{
				_originalShakeDuration = ShakeDuration;
				_originalShakeVolume = ShakeVolume;
				_originalRemapVolumeZero = RemapVolumeZero;
				_originalRemapVolumeOne = RemapVolumeOne;
				_originalRelativeValues = RelativeVolume;
			}

			if (!OnlyUseShakerValues)
			{
				TimescaleMode = timescaleMode;
				ShakeDuration = duration;
				ShakeVolume = volumeCurve;
				RemapVolumeZero = remapMin * feedbacksIntensity;
				RemapVolumeOne = remapMax * feedbacksIntensity;
				RelativeVolume = relativeVolume;
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
			_targetAudioSource.volume = _initialVolume;
		}

		/// <summary>
		/// Resets the shaker's values
		/// </summary>
		protected override void ResetShakerValues()
		{
			base.ResetShakerValues();
			ShakeDuration = _originalShakeDuration;
			ShakeVolume = _originalShakeVolume;
			RemapVolumeZero = _originalRemapVolumeZero;
			RemapVolumeOne = _originalRemapVolumeOne;
			RelativeVolume = _originalRelativeValues;
		}

		/// <summary>
		/// Starts listening for events
		/// </summary>
		public override void StartListening()
		{
			base.StartListening();
			MMAudioSourceVolumeShakeEvent.Register(OnMMAudioSourceVolumeShakeEvent);
		}

		/// <summary>
		/// Stops listening for events
		/// </summary>
		public override void StopListening()
		{
			base.StopListening();
			MMAudioSourceVolumeShakeEvent.Unregister(OnMMAudioSourceVolumeShakeEvent);
		}
	}

	/// <summary>
	/// An event used to trigger vignette shakes
	/// </summary>
	public struct MMAudioSourceVolumeShakeEvent
	{
		static private event Delegate OnEvent;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }

		public delegate void Delegate(AnimationCurve volumeCurve, float duration, float remapMin, float remapMax, bool relativeVolume = false,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true, 
			bool forwardDirection = true, TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false);

		static public void Trigger(AnimationCurve volumeCurve, float duration, float remapMin, float remapMax, bool relativeVolume = false,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true, 
			bool forwardDirection = true, TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false)
		{
			OnEvent?.Invoke(volumeCurve, duration, remapMin, remapMax, relativeVolume,
				feedbacksIntensity, channelData, resetShakerValuesAfterShake, resetTargetValuesAfterShake, forwardDirection, timescaleMode, stop, restore);
		}
	}
}