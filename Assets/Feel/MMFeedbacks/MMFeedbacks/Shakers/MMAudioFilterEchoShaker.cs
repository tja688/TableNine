using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// Add this to an audio echo filter to shake its values remapped along a curve 
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Audio/MM Audio Filter Echo Shaker")]
	[RequireComponent(typeof(AudioEchoFilter))]
	public class MMAudioFilterEchoShaker : MMShaker
	{
		[MMInspectorGroup("Echo", true, 52)]
		/// 是否叠加到初始值上。
		[Tooltip("是否叠加到初始值上。")]
		public bool RelativeEcho = false;
		/// 用于驱动强度数值变化的曲线。
		[Tooltip("用于驱动强度数值变化的曲线。")]
		public AnimationCurve ShakeEcho = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// 将曲线的 `0` 值重映射到的目标值。
		[Tooltip("将曲线的 `0` 值重映射到的目标值。")]
		[Range(0f, 1f)]
		public float RemapEchoZero = 0f;
		/// 将曲线的 `1` 值重映射到的目标值。
		[Tooltip("将曲线的 `1` 值重映射到的目标值。")]
		[Range(0f, 1f)]
		public float RemapEchoOne = 1f;

		/// the audio source to pilot
		protected AudioEchoFilter _targetAudioEchoFilter;
		protected float _initialEcho;
		protected float _originalShakeDuration;
		protected bool _originalRelativeEcho;
		protected AnimationCurve _originalShakeEcho;
		protected float _originalRemapEchoZero;
		protected float _originalRemapEchoOne;

		/// <summary>
		/// On init we initialize our values
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			_targetAudioEchoFilter = this.gameObject.GetComponent<AudioEchoFilter>();
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
			float newEchoLevel = ShakeFloat(ShakeEcho, RemapEchoZero, RemapEchoOne, RelativeEcho, _initialEcho);
			_targetAudioEchoFilter.wetMix = newEchoLevel;
		}

		/// <summary>
		/// Collects initial values on the target
		/// </summary>
		protected override void GrabInitialValues()
		{
			_initialEcho = _targetAudioEchoFilter.wetMix;
		}

		/// <summary>
		/// When we get the appropriate event, we trigger a shake
		/// </summary>
		/// <param name="echoCurve"></param>
		/// <param name="duration"></param>
		/// <param name="amplitude"></param>
		/// <param name="relativeEcho"></param>
		/// <param name="feedbacksIntensity"></param>
		/// <param name="channel"></param>
		public virtual void OnMMAudioFilterEchoShakeEvent(AnimationCurve echoCurve, float duration, float remapMin, float remapMax, bool relativeEcho = false,
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
				_originalShakeEcho = ShakeEcho;
				_originalRemapEchoZero = RemapEchoZero;
				_originalRemapEchoOne = RemapEchoOne;
				_originalRelativeEcho = RelativeEcho;
			}

			if (!OnlyUseShakerValues)
			{
				TimescaleMode = timescaleMode;
				ShakeDuration = duration;
				ShakeEcho = echoCurve;
				RemapEchoZero = remapMin * feedbacksIntensity;
				RemapEchoOne = remapMax * feedbacksIntensity;
				RelativeEcho = relativeEcho;
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
			_targetAudioEchoFilter.wetMix = _initialEcho;
		}

		/// <summary>
		/// Resets the shaker's values
		/// </summary>
		protected override void ResetShakerValues()
		{
			base.ResetShakerValues();
			ShakeDuration = _originalShakeDuration;
			ShakeEcho = _originalShakeEcho;
			RemapEchoZero = _originalRemapEchoZero;
			RemapEchoOne = _originalRemapEchoOne;
			RelativeEcho = _originalRelativeEcho;
		}

		/// <summary>
		/// Starts listening for events
		/// </summary>
		public override void StartListening()
		{
			base.StartListening();
			MMAudioFilterEchoShakeEvent.Register(OnMMAudioFilterEchoShakeEvent);
		}

		/// <summary>
		/// Stops listening for events
		/// </summary>
		public override void StopListening()
		{
			base.StopListening();
			MMAudioFilterEchoShakeEvent.Unregister(OnMMAudioFilterEchoShakeEvent);
		}
	}

	/// <summary>
	/// An event used to trigger vignette shakes
	/// </summary>
	public struct MMAudioFilterEchoShakeEvent
	{
		static private event Delegate OnEvent;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }

		public delegate void Delegate(AnimationCurve echoCurve, float duration, float remapMin, float remapMax, bool relativeEcho = false,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true, 
			bool forwardDirection = true, TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false);

		static public void Trigger(AnimationCurve echoCurve, float duration, float remapMin, float remapMax, bool relativeEcho = false,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true, 
			bool forwardDirection = true, TimescaleModes timescaleMode = TimescaleModes.Scaled, bool stop = false, bool restore = false)
		{
			OnEvent?.Invoke(echoCurve, duration, remapMin, remapMax, relativeEcho,
				feedbacksIntensity, channelData, resetShakerValuesAfterShake, resetTargetValuesAfterShake, forwardDirection, timescaleMode, stop, restore);
		}
	}
}