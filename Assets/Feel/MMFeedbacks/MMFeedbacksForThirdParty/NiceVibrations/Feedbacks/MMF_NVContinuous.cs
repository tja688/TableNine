using UnityEngine;
using System.Collections;
using MoreMountains.Feedbacks;
#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
using Lofelt.NiceVibrations;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 添加这个反馈后，可按指定 amplitude 与 frequency 播放一段持续型 haptic。它也支持对这些参数进行随机化，并可在播放过程中实时调制。
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
	[FeedbackPath("Haptics/Haptic Continuous")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.NiceVibrations")]
	[FeedbackHelp("添加这个反馈后，可按指定 amplitude 与 frequency 播放一段持续型 haptic。它也支持对这些参数进行随机化，并可在播放过程中实时调制。")]
	public class MMF_NVContinuous : MMF_Feedback
	{
		#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.HapticsColor; } }
		#endif
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(_duration); } set { _duration = value; } }
        
		[MMFInspectorGroup("Haptic Amplitude", true, 31)]
		/// 此 haptic 播放时的最小 amplitude（实际会在 MinAmplitude 和 MaxAmplitude 之间随机）。
		[Tooltip("此 haptic 播放时的最小 amplitude（实际会在 MinAmplitude 和 MaxAmplitude 之间随机）。")]
		[Range(0f, 1f)]
		public float MinAmplitude = 1f;
		/// 此 haptic 播放时的最大 amplitude（实际会在 MinAmplitude 和 MaxAmplitude 之间随机）。
		[Tooltip("此 haptic 播放时的最大 amplitude（实际会在 MinAmplitude 和 MaxAmplitude 之间随机）。")]
		[Range(0f, 1f)]
		public float MaxAmplitude = 1f;
        
		[MMFInspectorGroup("Haptic Frequency", true, 32)]
		/// 此 haptic 播放时的最小 frequency（实际会在 MinFrequency 和 MaxFrequency 之间随机）。
		[Tooltip("此 haptic 播放时的最小 frequency（实际会在 MinFrequency 和 MaxFrequency 之间随机）。")]
		[Range(0f, 1f)]
		public float MinFrequency = 1f;
		/// 此 haptic 播放时的最大 frequency（实际会在 MinFrequency 和 MaxFrequency 之间随机）。
		[Tooltip("此 haptic 播放时的最大 frequency（实际会在 MinFrequency 和 MaxFrequency 之间随机）。")]
		[Range(0f, 1f)]
		public float MaxFrequency = 1f;
        
		[MMFInspectorGroup("Duration", true, 33)]
		/// 此 haptic 播放时的最短持续时间（实际会在 MinDuration 和 MaxDuration 之间随机）。
		[Tooltip("此 haptic 播放时的最短持续时间（实际会在 MinDuration 和 MaxDuration 之间随机）。")]
		public float MinDuration = 1f;
		/// 此 haptic 播放时的最长持续时间（实际会在 MinDuration 和 MaxDuration 之间随机）。
		[Tooltip("此 haptic 播放时的最长持续时间（实际会在 MinDuration 和 MaxDuration 之间随机）。")]
		public float MaxDuration = 1f;
        
		[MMFInspectorGroup("Real-time Modulation", true, 34)]
		/// 是否在运行时调制 haptic 信号。
		[Tooltip("是否在运行时调制 haptic 信号。")]
		public bool UseRealTimeModulation = false;
		/// 若启用 UseRealTimeModulation，则使用这条曲线在整个持续时间内调制 amplitude。
		[Tooltip("若启用 UseRealTimeModulation，则使用这条曲线在整个持续时间内调制 amplitude。")]
		[MMFCondition("UseRealTimeModulation", true)]
		public AnimationCurve AmplitudeMultiplication = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1f), new Keyframe(1, 0f));
		/// 若启用 UseRealTimeModulation，则使用这条曲线在整个持续时间内调制 frequency。
		[Tooltip("若启用 UseRealTimeModulation，则使用这条曲线在整个持续时间内调制 frequency。")]
		[MMFCondition("UseRealTimeModulation", true)]
		public AnimationCurve ShiftFrequency = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1f), new Keyframe(1, 0f));

		[MMFInspectorGroup("Settings", true, 16)]
		/// 一组可调设置，用来精确控制这个 haptic 何时播放以及如何播放。
		[Tooltip("一组可调设置，用来精确控制这个 haptic 何时播放以及如何播放。")]
		public MMFeedbackNVSettings HapticSettings;
        
		protected Coroutine _coroutine;
		protected float _duration = 0f;
        
		/// <summary>
		/// On play we randomize our amplitude and frequency, trigger our haptic, and initialize real time modulation if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || HapticSettings == null || !HapticSettings.CanPlay())
			{
				return;
			}

			float amplitude = Random.Range(MinAmplitude, MaxAmplitude);
			float frequency = Random.Range(MinFrequency, MaxFrequency);
			_duration = Random.Range(MinDuration, MaxDuration);
			HapticSettings.SetGamepad();
			HapticPatterns.PlayConstant(amplitude, frequency, FeedbackDuration);

			if (UseRealTimeModulation)
			{
				_coroutine = Owner.StartCoroutine(RealtimeModulationCo());
			}
		}
        
		/// <summary>
		/// A coroutine used to modulate frequency and amplitude at runtime
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator RealtimeModulationCo()
		{
			IsPlaying = true;
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				HapticController.clipLevel = AmplitudeMultiplication.Evaluate(remappedTime);
				HapticController.clipFrequencyShift = ShiftFrequency.Evaluate(remappedTime);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			HapticController.clipLevel = AmplitudeMultiplication.Evaluate(FinalNormalizedTime);
			HapticController.clipFrequencyShift = ShiftFrequency.Evaluate(FinalNormalizedTime);       
            
			IsPlaying = false;
			_coroutine = null;
			yield return null;
		}
        
		/// <summary>
		/// On stop we stop haptics and our coroutine
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!FeedbackTypeAuthorized)
			{
				return;
			}
            
			base.CustomStopFeedback(position, feedbacksIntensity);
			IsPlaying = false;
			HapticController.Stop();
			if (Active && (_coroutine != null))
			{
				Owner.StopCoroutine(_coroutine);
				_coroutine = null;
			}
		}
		#else
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f) { }
		#endif
	}    
}