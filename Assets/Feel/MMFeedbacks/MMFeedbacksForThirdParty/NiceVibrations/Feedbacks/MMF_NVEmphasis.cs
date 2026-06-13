using UnityEngine;
using MoreMountains.Feedbacks;
#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
using Lofelt.NiceVibrations;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 使用这个反馈可播放 Emphasis haptics，也就是短促的触觉脉冲；其 amplitude 和 frequency 可实时控制，在 CoreHaptics/iOS 中也称为 Transients。
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
	[FeedbackPath("Haptics/Haptic Emphasis")]
	#endif 
	[MovedFrom(false, null, "MoreMountains.Feedbacks.NiceVibrations")]
	[FeedbackHelp("使用这个反馈可播放 Emphasis haptics，也就是短促的触觉脉冲；其 amplitude 和 frequency 可实时控制，在 CoreHaptics/iOS 中也称为 Transients。")]
	public class MMF_NVEmphasis : MMF_Feedback
	{
		#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override bool HasCustomInspectors => true;
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.HapticsColor; } }
		#endif
        
		[MMFInspectorGroup("Haptic Amplitude", true, 23)]
		/// 此 haptic 播放时的最小 amplitude（实际会在 MinAmplitude 和 MaxAmplitude 之间随机）。
		[Tooltip("此 haptic 播放时的最小 amplitude（实际会在 MinAmplitude 和 MaxAmplitude 之间随机）。")]
		[Range(0f, 1f)]
		public float MinAmplitude = 1f;
		/// 此 haptic 播放时的最大 amplitude（实际会在 MinAmplitude 和 MaxAmplitude 之间随机）。
		[Tooltip("此 haptic 播放时的最大 amplitude（实际会在 MinAmplitude 和 MaxAmplitude 之间随机）。")]
		[Range(0f, 1f)]
		public float MaxAmplitude = 1f;
        
		[MMFInspectorGroup("Haptic Frequency", true, 22)]
		/// 此 haptic 播放时的最小 frequency（实际会在 MinFrequency 和 MaxFrequency 之间随机）。
		[Tooltip("此 haptic 播放时的最小 frequency（实际会在 MinFrequency 和 MaxFrequency 之间随机）。")]
		[Range(0f, 1f)]
		public float MinFrequency = 1f;
		/// 此 haptic 播放时的最大 frequency（实际会在 MinFrequency 和 MaxFrequency 之间随机）。
		[Tooltip("此 haptic 播放时的最大 frequency（实际会在 MinFrequency 和 MaxFrequency 之间随机）。")]
		[Range(0f, 1f)]
		public float MaxFrequency = 1f;

		[MMFInspectorGroup("Settings", true, 16)]
		/// a debug button that lets you test the haptic file from its inspector
		public MMF_Button PlayEmphasisButton;
		
		/// 一组可调设置，用来精确控制这个 haptic 何时播放以及如何播放。
		[Tooltip("一组可调设置，用来精确控制这个 haptic 何时播放以及如何播放。")]
		public MMFeedbackNVSettings HapticSettings;
		
		/// <summary>
		/// Initializes custom buttons
		/// </summary>
		public override void InitializeCustomAttributes()
		{
			base.InitializeCustomAttributes();
			PlayEmphasisButton = new MMF_Button("Test Emphasis", PlayEmphasis);
		}
        
		/// <summary>
		/// On play, we randomize our amplitude and frequency and play our emphasis haptic
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || HapticSettings == null || !HapticSettings.CanPlay())
			{
				return;
			}

			PlayEmphasis();
		}

		/// <summary>
		/// Plays the specified emphasis haptic
		/// </summary>
		protected virtual void PlayEmphasis()
		{
			float amplitude = Random.Range(MinAmplitude, MaxAmplitude);
			float frequency = Random.Range(MinFrequency, MaxFrequency);
			HapticSettings.SetGamepad();
			HapticPatterns.PlayEmphasis(amplitude, frequency);
		}
		
		#else
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f) { }
		#endif
	}    
}