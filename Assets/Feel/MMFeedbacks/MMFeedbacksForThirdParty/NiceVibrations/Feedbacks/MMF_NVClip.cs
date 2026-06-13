using UnityEngine;
using MoreMountains.Feedbacks;

#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
using Lofelt.NiceVibrations;
using MoreMountains.Tools;
#endif

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// Add this feedback to play a .haptic clip, optionally randomizing its level and frequency
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
	[FeedbackPath("Haptics/Haptic Clip")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.NiceVibrations")]
	[FeedbackHelp("这个反馈可播放 haptic clip，并可随机化其 level 与 frequency。")]
	public class MMF_NVClip : MMF_Feedback
	{
		#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override bool HasCustomInspectors => true;
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.HapticsColor; } }
		public override bool EvaluateRequiresSetup() { return (Clip == null); }
		public override string RequiredTargetText { get { return Clip != null ? Clip.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈需要指定 Clip 才能正常工作。你可以在下方进行设置。"; } }
		#endif
        
		[MMFInspectorGroup("Haptic Clip", true, 13, true)]
		/// 此反馈要播放的 haptic clip。
		[Tooltip("这里反馈要播放触觉剪辑。")]
		public HapticClip Clip;
		/// 如果当前设备不支持播放 haptic clip，则回退播放这个 preset。
		[Tooltip("如果当前设备不支持播放 haptic clip，则回退播放这个 preset。")]
		public HapticPatterns.PresetType FallbackPreset = HapticPatterns.PresetType.LightImpact;
		/// 此 clip 是否循环播放直到被停止（在 gamepad 上无效）。
		[Tooltip("此 clip 是否循环播放直到被停止（在 gamepad 上无效）。")]
		public bool Loop = false;
		/// 此 clip 从哪个时间点开始播放。
		[Tooltip("此 clip 从哪个时间点开始播放。")]
		public float SeekTime = 0f;
		/// a debug button that lets you test the haptic file from its inspector
		public MMF_Button TestHapticButton;

		[MMFInspectorGroup("Audio To Haptic", true, 14)]
		
		/// 要从哪个 MMSM Sound 反馈中提取音频 clip 进行转换。若留空，则会在当前 MMF Player 上查找第一个。
		[Tooltip("要从哪个MMSM声音输入中提取音频进行转换。若留空，则在当前MMF播放器上查找第一个。")]
		[MMFInformation("除了直接在上方字段里指定 clip 外，这个反馈还支持自动将某个 MMSM Sound 反馈中的音频 clip 转换为 haptic clip。" +
		                "这种方式既能节省时间，也依然保留对 amplitude 和 frequency 的细致控制。\n\n" +
		                "要使用这个功能，同一个 MMF_Player 上需要存在一个带有音频 clip 的 MMSM Sound 反馈。如果有多个，可在下方字段中填写目标反馈的 Label。" +
		                "然后点击 Convert 按钮。转换完成后，可以再点击下方的测试按钮，同时试听音频与触觉效果，确认是否符合预期。\n\n" +
		                "之后你还可以按需要为 gamepad 归一化 amplitude 和/或 frequency。第一条曲线表示 iOS/Android 的 haptic 数据，第二条曲线表示手柄 rumble 数据。", MMFInformationAttribute.InformationType.Info, false)]
		public string MMSMSoundFeedbackLabel;
		
		/// 采样数，决定生成 haptic clip 时的分辨率。
		[Tooltip("采样数，决定生成 haptic clip 时的分辨率。")]
		public int SampleCount = 256;

		[Header("Amplitude")] 
		/// 是否对 gamepad rumble 的 amplitude 做归一化处理。
		[Tooltip("是否对 gamepad rumble 的 amplitude 做归一化处理。")]
		public bool NormalizeAmplitude = true;
		/// amplitude 归一化时使用的系数。
		[Tooltip("振幅归一化时使用的系数。")]
		[MMFCondition("NormalizeAmplitude", true)]
		public float NormalizeAmplitudeFactor = 1f;
	
		[Header("Frequency")]
		/// 是否对 gamepad rumble 的 frequency 做归一化处理。
		[Tooltip("是否对 gamepad rumble 的 frequency 做归一化处理。")]
		public bool NormalizeFrequency = true;
		/// frequency 归一化时使用的系数。
		[Tooltip("频率归一化时使用的系数。")]
		[MMFCondition("NormalizeFrequency", true)]
		public float NormalizeFrequencyFactor = 1f;
		
		/// a test button to convert the MMSM Sound feedback's audio clip into a haptic clip and assign it to this feedback
		public MMF_Button ConvertButton;
		/// a test button to play both the haptic and sound at once
		public MMF_Button TestHapticAudioButton;
		
		public NVHapticData HapticData;

		[MMFInspectorGroup("Level", true, 14)]
		/// 此 clip 播放时的最小 level（实际会在 MinLevel 和 MaxLevel 之间随机）。
		[Tooltip("此 clip 播放时的最小 level（实际会在 MinLevel 和 MaxLevel 之间随机）。")]
		[Range(0f, 5f)]
		public float MinLevel = 1f;
		/// 此 clip 播放时的最大 level（实际会在 MinLevel 和 MaxLevel 之间随机）。
		[Tooltip("此 clip 播放时的最大 level（实际会在 MinLevel 和 MaxLevel 之间随机）。")]
		[Range(0f, 5f)]
		public float MaxLevel = 1f;
        
		[MMFInspectorGroup("Frequency Shift", true, 15)]
		/// the minimum frequency shift at which this clip should play (frequency shift will be randomized between MinFrequencyShift and MaxLevel)
		[Tooltip("此片段播放时的最小频移（实际会在 最小频移 和 最大频移 之间随机）。")]
		[Range(-1f, 1f)]
		public float MinFrequencyShift = 0f;
		/// the maximum frequency shift at which this clip should play (frequency shift will be randomized between MinFrequencyShift and MaxLevel)
		[Tooltip("该片段播放时的最大频移（实际上会在 最小频移 和 最大频移 之间随机）。")]
		[Range(-1f, 1f)]
		public float MaxFrequencyShift = 0f;

		[MMFInspectorGroup("Settings", true, 16)]
		/// 一组可调设置，用来精确控制这个 haptic 何时播放以及如何播放。
		[Tooltip("一组可调设置，用来精确控制这个 haptic 何时播放以及如何播放。")]
		public MMFeedbackNVSettings HapticSettings;

		
		/// <summary>
		/// On play, we load our clip, set its settings and play it
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || HapticSettings == null || !HapticSettings.CanPlay() || (Clip == null))
			{
				return;
			}

			PlayHapticClip();
		}

		/// <summary>
		/// Plays the haptic clip
		/// </summary>
		protected virtual void PlayHapticClip()
		{
			if (Clip == null)
			{
				return;
			}
			HapticSettings.SetGamepad();
			HapticController.Load(Clip);
			HapticController.fallbackPreset = FallbackPreset;
			HapticController.Loop(Loop);
			HapticController.Seek(SeekTime);
			HapticController.clipLevel = Random.Range(MinLevel, MaxLevel);
			HapticController.clipFrequencyShift = Random.Range(MinFrequencyShift, MaxFrequencyShift);
			HapticController.Play();
		}
        
		/// <summary>
		/// On stop we stop haptics
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
		}
		
		/// <summary>
		/// Initializes custom buttons
		/// </summary>
		public override void InitializeCustomAttributes()
		{
			base.InitializeCustomAttributes();
			ConvertButton = new MMF_Button("Convert MMSM Sound feedback Audio Clip to Haptic", Convert);
			TestHapticAudioButton = new MMF_Button("Test Haptic and Audio", TestHapticAndAudio);
			TestHapticButton = new MMF_Button("Test Haptic", PlayHapticClip);
		}
		
		/// <summary>
		/// A debug method used from the inspector to test both the haptic and audio files playing at once
		/// </summary>
		protected virtual void TestHapticAndAudio()
		{
			MMF_MMSoundManagerSound soundFeedback = Owner.GetFeedbackOfType<MMF_MMSoundManagerSound>(MMSMSoundFeedbackLabel);
			if (soundFeedback != null)
			{
				soundFeedback.TestPlaySound();	
			}
			PlayHapticClip();
		}

		/// <summary>
		/// Tries and converts the MMSM Sound feedback's audio clip on the same MMF Player into a haptic clip and sets it as this feedback's haptic clip 
		/// </summary>
		protected virtual void Convert()
		{
			#if UNITY_EDITOR
			MMF_MMSoundManagerSound soundFeedback;
			if ((MMSMSoundFeedbackLabel == null) || (MMSMSoundFeedbackLabel == ""))
			{
				soundFeedback = Owner.GetFeedbackOfType<MMF_MMSoundManagerSound>();
				if (soundFeedback != null)
				{
					MMSMSoundFeedbackLabel = soundFeedback.Label;
				}
				else
				{
					Debug.LogError(this.Owner.name + " - NV Clip feedback : there is no MM Sound Manager Sound feedback on this MMF Player, nothing to convert.");
					return;
				}
			}
			else
			{
				soundFeedback = Owner.GetFeedbackOfType<MMF_MMSoundManagerSound>(MMSMSoundFeedbackLabel);
				if (soundFeedback == null)
				{
					Debug.LogError(this.Owner.name + " - NV Clip feedback : couldn't find a MM Sound Manager Sound feedback with this label: " + MMSMSoundFeedbackLabel);
					return;
				}
			}
			
			AudioClip clip = soundFeedback.Sfx;

			if (clip == null)
			{
				if (soundFeedback.RandomSfx.Length > 0)
				{
					clip = soundFeedback.RandomSfx[0];
				}

				if (clip == null)
				{
					Debug.LogError(this.Owner.name + " - NV Clip feedback : thee MM Sound Manager Sound feedback on this MMF Player doesn't have a clip, nothing to convert.");
					return;	
				}
			}
			
			string filePath = AssetDatabase.GetAssetPath(clip);
			string folderPath = Path.GetDirectoryName(filePath);
			string newFileName = Path.GetFileNameWithoutExtension(filePath)+".haptic";


			HapticData = AudioToHapticConverter.GenerateHapticFile(clip, folderPath, newFileName, 
																		NormalizeAmplitude, NormalizeAmplitudeFactor, 
																		NormalizeFrequency, NormalizeFrequencyFactor, 
																		SampleCount);
			Clip = HapticData.Clip;
			CacheRequiresSetup();
			#endif
		}
		
		#else
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f) { }
		#endif
	}    
}
