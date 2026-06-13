using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using MoreMountains.Tools;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;
using Random = UnityEngine.Random;

namespace MoreMountains.Feedbacks
{
	[ExecuteAlways]
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackPath("Audio/Sound")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[FeedbackHelp("警告：这是一个非常基础的声音反馈，只负责播放声音。若你需要更完整的控制能力（如轨道控制、淡变、Solo、更细分的播放选项），建议改用 MMSoundManager Sound 反馈。\n\n此反馈可播放指定的 AudioClip，可通过 Event 模式（场景中需要有对象接收 MMSfxEvent，例如 MMSoundManager）、Cached 模式（初始化时创建并缓存 AudioSource）、OnDemand 模式（每次 Play 时按需创建），或 Pool 模式（从对象池复用 AudioSource）进行播放。若 RandomSfx 非空，会优先于单个 Sfx。无论使用哪种方式，你都可以设置随机音量范围（若不想随机，将最小值与最大值设为相同）、随机音高，以及可选的 AudioMixerGroup。")]
	public class MMF_Sound : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.SoundsColor; } }
		public override bool HasCustomInspectors => true;
		public override bool HasAutomaticShakerSetup => true;
		public override bool EvaluateRequiresSetup()
		{
			bool requiresSetup = false;
			if (Sfx == null)
			{
				requiresSetup = true;
			}
			if ((RandomSfx != null) && (RandomSfx.Length > 0))
			{
				requiresSetup = false;
				foreach (AudioClip clip in RandomSfx)
				{
					if (clip == null)
					{
						requiresSetup = true;
					}
				}    
			}
			return requiresSetup;
		}
		public override string RequiredTargetText { get { return Sfx != null ? Sfx.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈至少需要在下方 Sfx 指定一个 AudioClip，或在 RandomSfx 数组中提供一个或多个 AudioClip。"; } }
		#endif
		public override bool HasRandomness => true;
		/// the duration of this feedback is the duration of the clip being played
		public override float FeedbackDuration { get { return GetDuration(); } }

		/// <summary>
		/// The possible methods to play the sound with. 
		/// Event : sends a MMSfxEvent, you'll need a class to catch this event and play the sound
		/// Cached : creates and stores an audiosource to play the sound with, parented to the owner
		/// OnDemand : creates an audiosource and destroys it everytime you want to play the sound
		/// </summary>
		public enum PlayMethods { Event, Cached, OnDemand, Pool }

		[MMFInspectorGroup("Sound", true, 14, true)]
		/// the sound clip to play
		[Tooltip("要播放的 AudioClip。若下方 RandomSfx 非空，播放时会优先使用 RandomSfx。")]
		public AudioClip Sfx;

		/// an array to pick a random sfx from
		[Tooltip("用于随机挑选音效片段的 AudioClip 数组。若非空，将覆盖上方 Sfx。")]
		public AudioClip[] RandomSfx;

		/// a test button used to play the sound in inspector
		public MMF_Button TestPlayButton;
		/// a test button used to stop the sound in inspector
		public MMF_Button TestStopButton;

		[MMFInspectorGroup("Play Method", true, 27)]
		/// the play method to use when playing the sound (event, cached or on demand)
		[Tooltip("播放声音时使用的方式：事件（发送事件）、缓存（缓存音频源）、一经请求（重复创建）、水池（对象池复用）。")]
		public PlayMethods PlayMethod = PlayMethods.Event;
		/// the size of the pool when in Pool mode
		[Tooltip("Pool 模式下对象池的大小（仅在 PlayMethod=Pool 时生效）。")]
		[MMFEnumCondition("PlayMethod", (int)PlayMethods.Pool)]
		public int PoolSize = 10;
		/// in event mode, whether to use legacy events (MMSfxEvent) or the current events (MMSoundManagerSoundPlayEvent)
		[Tooltip("在 Event 模式下，决定使用旧版事件（MMSfxEvent）还是当前事件（MMSoundManagerSoundPlayEvent）。")]
		[MMFEnumCondition("PlayMethod", (int)PlayMethods.Event)]
		public bool UseLegacyEventsMode = false;
		/// if this is true, calling Stop on this feedback will also stop the sound from playing further
		[Tooltip("若启用，对该反馈调用 Stop 时，也会一并停止声音继续播放")]
		public bool StopSoundOnFeedbackStop = true;
		
		[MMFInspectorGroup("Sound Properties", true, 28)]
        
		[Header("Volume")]
		/// the minimum volume to play the sound at
		[Tooltip("播放时随机音量范围的最小值。若与最大值相同则不随机。")]
		[Range(0f,2f)]
		public float MinVolume = 1f;
		/// the maximum volume to play the sound at
		[Tooltip("播放时随机音量范围的最大值。若与最小值相同则不随机。")]
		[Range(0f,2f)]
		public float MaxVolume = 1f;

		[Header("Pitch")]
		/// the minimum pitch to play the sound at
		[Tooltip("播放时随机音高范围的最小值。若与最大值相同则不随机。")]
		[Range(-3f,3f)]
		public float MinPitch = 1f;
		/// the maximum pitch to play the sound at
		[Tooltip("播放时随机音高范围的最大值。若与最小值相同则不随机。")]
		[Range(-3f,3f)]
		public float MaxPitch = 1f;

		[Header("Mixer")]
		/// the audiomixer to play the sound with (optional)
		[Tooltip("播放该声音时使用的 AudioMixer（可选）")]
		public AudioMixerGroup SfxAudioMixerGroup;
		/// the audiosource priority
		[Tooltip("AudioSource 的优先级；若有需要，可在 0（最高）到 256 之间指定")] 
		public int Priority = 128;

		[MMFInspectorGroup("Spatial Settings", true, 33, false, true)]
		/// Pans a playing sound in a stereo way (left or right). This only applies to sounds that are Mono or Stereo.
		[Tooltip("设置立体声声像（左/右）。仅对 Mono 或 Stereo 声音有效。")]
		[Range(-1f,1f)]
		public float PanStereo;
		/// Sets how much this AudioSource is affected by 3D spatialisation calculations (attenuation, doppler etc). 0.0 makes the sound full 2D, 1.0 makes it full 3D.
		[Tooltip("设置 3D 空间化混合比例（衰减、Doppler 等）。0 表示完全 2D，1 表示完全 3D。")]
		[Range(0f,1f)]
		public float SpatialBlend;
		
		[MMFInspectorGroup("3D Sound Settings", true, 37, false, true)]
		/// Sets the Doppler scale for this AudioSource.
		[Tooltip("设置此音频源的多普勒强度。")]
		[Range(0f,5f)]
		public float DopplerLevel = 1f;
		/// Sets the spread angle (in degrees) of a 3d stereo or multichannel sound in speaker space.
		[Tooltip("设置 3D 立体声/多声道在扬声器空间中的扩散角度（度）。")]
		[Range(0,360)]
		public int Spread = 0;
		/// Sets/Gets how the AudioSource attenuates over distance.
		[Tooltip("设置音频源的距离衰减模式。")]
		public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;
		/// Within the Min distance the AudioSource will cease to grow louder in volume.
		[Tooltip("最小距离。位于该距离内时，声音不再继续变大。")]
		public float MinDistance = 1f;
		/// (Logarithmic rolloff) MaxDistance is the distance a sound stops attenuating at.
		[Tooltip("最大距离。在对数衰减模式下，超过该距离后声音不再继续衰减。")]
		public float MaxDistance = 500f;
		/// whether or not to use a custom curve for custom volume rolloff
		[Tooltip("是否使用自定义音量衰减曲线。开启后才会使用下方曲线。")]
		public bool UseCustomRolloffCurve = false;
		/// the curve to use for custom volume rolloff if UseCustomRolloffCurve is true
		[Tooltip("自定义音量衰减曲线（仅在 UseCustomRolloffCurve 开启时生效）。")]
		[MMFCondition("UseCustomRolloffCurve", true)]
		public AnimationCurve CustomRolloffCurve;
		/// whether or not to use a custom curve for spatial blend
		[Tooltip("是否使用自定义 Spatial Blend 曲线。开启后才会使用下方曲线。")]
		public bool UseSpatialBlendCurve = false;
		/// the curve to use for custom spatial blend if UseSpatialBlendCurve is true
		[Tooltip("自定义 Spatial Blend 曲线（仅在 UseSpatialBlendCurve 开启时生效）。")]
		[MMFCondition("UseSpatialBlendCurve", true)]
		public AnimationCurve SpatialBlendCurve;
		/// whether or not to use a custom curve for reverb zone mix
		[Tooltip("是否使用自定义 Reverb Zone Mix 曲线。开启后才会使用下方曲线。")]
		public bool UseReverbZoneMixCurve = false;
		/// the curve to use for custom reverb zone mix if UseReverbZoneMixCurve is true
		[Tooltip("自定义 Reverb Zone Mix 曲线（仅在 UseReverbZoneMixCurve 开启时生效）。")]
		[MMFCondition("UseReverbZoneMixCurve", true)]
		public AnimationCurve ReverbZoneMixCurve;
		/// whether or not to use a custom curve for spread
		[Tooltip("是否使用自定义 Spread 曲线。开启后才会使用下方曲线。")]
		public bool UseSpreadCurve = false;
		/// the curve to use for custom spread if UseSpreadCurve is true
		[Tooltip("自定义 Spread 曲线（仅在 UseSpreadCurve 开启时生效）。")]
		[MMFCondition("UseSpreadCurve", true)]
		public AnimationCurve SpreadCurve;

		protected AudioClip _randomClip;
		protected AudioSource _cachedAudioSource;
		protected AudioSource[] _pool;
		protected AudioSource _tempAudioSource;
		protected float _duration;
		protected AudioSource _editorAudioSource;
		protected AudioSource _audioSource;
		protected AudioClip _lastPlayedClip;

		public override void InitializeCustomAttributes()
		{
			base.InitializeCustomAttributes();
			TestPlayButton = new MMF_Button("Debug Play Sound", TestPlaySound);
			TestStopButton = new MMF_Button("Debug Stop Sound", TestStopSound);
		}

		/// <summary>
		/// Custom init to cache the audiosource if required
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			if (RandomSfx == null)
			{
				RandomSfx = Array.Empty<AudioClip>();
			}
			if ((PlayMethod == PlayMethods.Cached) && (_cachedAudioSource == null))
			{
				_cachedAudioSource = CreateAudioSource(owner.gameObject, "CachedFeedbackAudioSource");
			}
			_lastPlayedClip = null;
			if (PlayMethod == PlayMethods.Pool)
			{
				// create a pool
				_pool = new AudioSource[PoolSize];
				for (int i = 0; i < PoolSize; i++)
				{
					_pool[i] = CreateAudioSource(owner.gameObject, "PooledAudioSource"+i);
				}
			}
		}

		protected virtual AudioSource CreateAudioSource(GameObject owner, string audioSourceName)
		{
			// we create a temporary game object to host our audio source
			GameObject temporaryAudioHost = new GameObject(audioSourceName);
			SceneManager.MoveGameObjectToScene(temporaryAudioHost.gameObject, Owner.gameObject.scene);
			// we set the temp audio's position
			temporaryAudioHost.transform.position = owner.transform.position;
			temporaryAudioHost.transform.SetParent(owner.transform);
			// we add an audio source to that host
			_tempAudioSource = temporaryAudioHost.AddComponent<AudioSource>() as AudioSource;
			_tempAudioSource.playOnAwake = false;
			return _tempAudioSource; 
		}

		/// <summary>
		/// Plays either a random sound or the specified sfx
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
            
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
            
			if (Sfx != null)
			{
				_duration = Sfx.length;
				PlaySound(Sfx, position, intensityMultiplier);
				return;
			}

			if (RandomSfx.Length > 0)
			{
				_randomClip = RandomSfx[Random.Range(0, RandomSfx.Length)];

				if (_randomClip != null)
				{
					_duration = _randomClip.length;
					PlaySound(_randomClip, position, intensityMultiplier);
				}
                
			}
		}

		protected virtual float GetDuration()
		{
			if (Sfx != null)
			{
				return Sfx.length;
			}

			float longest = 0f;
			if ((RandomSfx != null) && (RandomSfx.Length > 0))
			{
				if (_lastPlayedClip != null)
				{
					return _lastPlayedClip.length;	
				}
				
				foreach (AudioClip clip in RandomSfx)
				{
					if ((clip != null) && (clip.length > longest))
					{
						longest = clip.length;
					}
				}

				return longest;
			}

			return 0f;
		}

		/// <summary>
		/// Plays a sound differently based on the selected play method
		/// </summary>
		/// <param name="sfx"></param>
		/// <param name="position"></param>
		protected virtual void PlaySound(AudioClip sfx, Vector3 position, float intensity)
		{
			float volume = Random.Range(MinVolume, MaxVolume);
            
			if (!Timing.ConstantIntensity)
			{
				volume = volume * intensity;
			}
            
			float pitch = Random.Range(MinPitch, MaxPitch);

			int timeSamples = NormalPlayDirection ? 0 : sfx.samples - 1;
            
			if (!NormalPlayDirection)
			{
				pitch = -pitch;
			}
			
			_lastPlayedClip = sfx;
			Owner.ComputeCachedTotalDuration();

			switch (PlayMethod)
			{
				case PlayMethods.Event:
					if (UseLegacyEventsMode)
					{
						MMSfxEvent.Trigger(sfx, SfxAudioMixerGroup, volume, pitch, Priority);
					}
					else
					{
						MMSoundManagerPlayOptions options = new MMSoundManagerPlayOptions();
						options = MMSoundManagerPlayOptions.Default;
						options.Location = Owner.transform.position;
						options.AudioGroup = SfxAudioMixerGroup;
						options.DoNotAutoRecycleIfNotDonePlaying = true;
						options.Volume = volume;
						options.Pitch = pitch;
						options.PanStereo = PanStereo;
						options.SpatialBlend = SpatialBlend;
						options.Priority = Priority;
						options.DopplerLevel = DopplerLevel;
						options.Spread = Spread;
						options.RolloffMode = RolloffMode;
						options.MinDistance = MinDistance;
						options.MaxDistance = MaxDistance;
						options.UseSpreadCurve = UseSpreadCurve;
						options.SpreadCurve = SpreadCurve;
						options.UseCustomRolloffCurve = UseCustomRolloffCurve;
						options.CustomRolloffCurve = CustomRolloffCurve;
						options.UseSpatialBlendCurve = UseSpatialBlendCurve;
						options.SpatialBlendCurve = SpatialBlendCurve;
						options.UseReverbZoneMixCurve = UseReverbZoneMixCurve;
						options.ReverbZoneMixCurve = ReverbZoneMixCurve;

						if (Priority >= 0)
						{
							options.Priority = Mathf.Min(Priority, 256);
						}
						options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
						options.Loop = false;
						_audioSource = MMSoundManagerSoundPlayEvent.Trigger(sfx, options);	
					}
					break;
				case PlayMethods.Cached:
					// we set that audio source clip to the one in paramaters
					PlayAudioSource(_cachedAudioSource, sfx, volume, pitch, timeSamples, SfxAudioMixerGroup, Priority);
					break;
				case PlayMethods.OnDemand:
					// we create a temporary game object to host our audio source
					GameObject temporaryAudioHost = new GameObject("TempAudio");
					SceneManager.MoveGameObjectToScene(temporaryAudioHost.gameObject, Owner.gameObject.scene);
					// we set the temp audio's position
					temporaryAudioHost.transform.position = position;
					// we add an audio source to that host
					AudioSource audioSource = temporaryAudioHost.AddComponent<AudioSource>() as AudioSource;
					PlayAudioSource(audioSource, sfx, volume, pitch, timeSamples, SfxAudioMixerGroup, Priority);
					// we destroy the host after the clip has played
					Owner.ProxyDestroy(temporaryAudioHost, sfx.length * Time.timeScale);
					break;
				case PlayMethods.Pool:
					_tempAudioSource = GetAudioSourceFromPool();
					if (_tempAudioSource != null)
					{
						PlayAudioSource(_tempAudioSource, sfx, volume, pitch, timeSamples, SfxAudioMixerGroup, Priority);
					}
					break;
			}
		}

		/// <summary>
		/// On Stop, we stop our sound if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
            
			if (StopSoundOnFeedbackStop && (_audioSource != null))
			{
				_audioSource.Stop();
			}
		}

		/// <summary>
		/// Plays the audio source with the specified volume and pitch
		/// </summary>
		/// <param name="audioSource"></param>
		/// <param name="sfx"></param>
		/// <param name="volume"></param>
		/// <param name="pitch"></param>
		protected virtual void PlayAudioSource(AudioSource audioSource, AudioClip sfx, float volume, float pitch, int timeSamples, AudioMixerGroup audioMixerGroup = null, int priority = 128)
		{
			_audioSource = audioSource;
			// we set that audio source clip to the one in paramaters
			audioSource.clip = sfx;
			audioSource.timeSamples = timeSamples;
			// we set the audio source volume to the one in parameters
			audioSource.volume = volume;
			audioSource.pitch = pitch;
			audioSource.priority = priority;
			// we set spatial settings
			audioSource.panStereo = PanStereo;
			audioSource.spatialBlend = SpatialBlend;
			audioSource.dopplerLevel = DopplerLevel;
			audioSource.spread = Spread;
			audioSource.rolloffMode = RolloffMode;
			audioSource.minDistance = MinDistance;
			audioSource.maxDistance = MaxDistance;
			if (UseSpreadCurve) { audioSource.SetCustomCurve(AudioSourceCurveType.Spread, SpreadCurve); }
			if (UseCustomRolloffCurve) { audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CustomRolloffCurve); }
			if (UseSpatialBlendCurve) { audioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, SpatialBlendCurve); }
			if (UseReverbZoneMixCurve) { audioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, ReverbZoneMixCurve); }
			// we set our loop setting
			audioSource.loop = false;
			if (audioMixerGroup != null)
			{
				audioSource.outputAudioMixerGroup = audioMixerGroup;
			}
			// we start playing the sound
			audioSource.Play(); 
		}

		/// <summary>
		/// Gets an audio source from the pool if possible
		/// </summary>
		/// <returns></returns>
		protected virtual AudioSource GetAudioSourceFromPool()
		{
			for (int i = 0; i < PoolSize; i++)
			{
				if (!_pool[i].isPlaying)
				{
					return _pool[i];
				}
			}
			return null;
		}

		/// <summary>
		/// A test method that creates an audiosource, plays it, and destroys itself after play
		/// </summary>
		protected virtual async void TestPlaySound()
		{
			AudioClip tmpAudioClip = null;

			if (Sfx != null)
			{
				tmpAudioClip = Sfx;
			}

			if (RandomSfx.Length > 0)
			{
				tmpAudioClip = RandomSfx[Random.Range(0, RandomSfx.Length)];
			}

			if (tmpAudioClip == null)
			{
				Debug.LogError(Label + " on " + Owner.gameObject.name + " can't play in editor mode, you haven't set its Sfx.");
				return;
			}

			float volume = Random.Range(MinVolume, MaxVolume);
			float pitch = Random.Range(MinPitch, MaxPitch);
			GameObject temporaryAudioHost = new GameObject("EditorTestAS_WillAutoDestroy");
			if (!Application.isPlaying)
			{
				temporaryAudioHost.AddComponent<MMForceDestroyInPlayMode>();
			}
			SceneManager.MoveGameObjectToScene(temporaryAudioHost.gameObject, Owner.gameObject.scene);
			temporaryAudioHost.transform.position = Owner.transform.position;
			_editorAudioSource = temporaryAudioHost.AddComponent<AudioSource>() as AudioSource;
			PlayAudioSource(_editorAudioSource, tmpAudioClip, volume, pitch, 0);
			float length = 1000 * tmpAudioClip.length;
			await Task.Delay((int)length);
			Owner.ProxyDestroyImmediate(temporaryAudioHost);
		}

		/// <summary>
		/// A test method that stops the test sound
		/// </summary>
		protected virtual void TestStopSound()
		{
			if (_editorAudioSource != null)
			{
				_editorAudioSource.Stop();
			}            
		}
		
		/// <summary>
		/// Automatically tries to add a MMSoundManager to the scene if none are present
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			if (PlayMethod != PlayMethods.Event)
			{
				return;
			}
			MMSoundManager soundManager = (MMSoundManager)UnityEngine.Object.FindAnyObjectByType(typeof(MMSoundManager));
			if (soundManager == null)
			{
				GameObject soundManagerGo = new GameObject("MMSoundManager");
				soundManagerGo.AddComponent<MMSoundManager>();
				MMDebug.DebugLogInfo( "Added a MMSoundManager to the scene. You're all set.");
			}
		}
	}
}
