using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using MoreMountains.Tools;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// A scriptable object used to store data for MMSoundManager play
	/// </summary>
	[Serializable]
	[CreateAssetMenu(menuName = "MoreMountains/Audio/MMF_SoundData")]
	public class MMF_MMSoundManagerSoundData : ScriptableObject
	{
		[Header("Sound")]
		/// the sound clip to play
		[Tooltip("要播放的 AudioClip。若 RandomSfx 非空，播放时会优先使用 RandomSfx。")]
		public AudioClip Sfx;

		[Header("Random Sound")]
		/// an array to pick a random sfx from
		[Tooltip("用于随机挑选音效片段的 AudioClip 数组。若非空，将覆盖上方 Sfx。")]
		public AudioClip[] RandomSfx;
		/// if this is true, random sfx audio clips will be played in sequential order instead of at random
		[Tooltip("若开启，将按顺序播放 RandomSfx，而不是随机播放。开启后 RandomUnique 不生效。")]
		public bool SequentialOrder = false;
		/// if we're in sequential order, determines whether or not to hold at the last index, until either a cooldown is met, or the ResetSequentialIndex method is called
		[Tooltip("顺序播放到最后一个索引后，是否停留在最后一个片段。可通过冷却时间或调用 ResetSequentialIndex() 重置。")]
		[MMFCondition("SequentialOrder", true)]
		public bool SequentialOrderHoldLast = false;
		/// if we're in sequential order hold last mode, index will reset to 0 automatically after this duration, unless it's 0, in which case it'll be ignored
		[Tooltip("顺序停留模式下的重置冷却时间（秒）。大于 0 时会在达到条件后自动回到索引 0；为 0 时忽略。")]
		[MMFCondition("SequentialOrderHoldLast", true)]
		public float SequentialOrderHoldCooldownDuration = 2f;
		/// if this is true, sfx will be picked at random until all have been played. once this happens, the list is shuffled again, and it starts over
		[Tooltip("若开启，随机播放时会先保证每个片段都至少播放一次，再重新洗牌循环。仅在非顺序播放时生效。")]
		public bool RandomUnique = false;
        
		[Header("Sound Properties")]
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

		[Header("Time")]
		/// a timestamp (in seconds, randomized between the defined min and max) at which the sound will start playing, equivalent to the Audiosource API's Time) 
		[Tooltip("播放起始时间（秒），会在 Min/Max 之间随机；等价于 AudioSource.time。")]
		[MMFVector("Min", "Max")]
		public Vector2 PlaybackTime = new Vector2(0f, 0f);
		/// a duration (in seconds, randomized between the defined min and max) for which the sound will play before stopping. Ignored if min and max are zero.
		[Tooltip("播放时长（秒），会在 Min/Max 之间随机。若 Min 与 Max 都为 0，则忽略此限制并按片段原始时长播放。")]
		[MMVector("Min", "Max")]
		public Vector2 PlaybackDuration = new Vector2(0f, 0f);
		
		[Header("Sound Manager Options")]
		/// the track on which to play the sound. Pick the one that matches the nature of your sound
		[Tooltip("要播放到的目标轨道。请按声音用途选择（Master/UI/Music/Sfx）。")]
		public MMSoundManager.MMSoundManagerTracks MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
		/// the ID of the sound. This is useful if you plan on using sound control feedbacks on it afterwards. 
		[Tooltip("声音 ID。后续若需要用控制类反馈（如 Fade/TrackControl）定位该声音，请保持 ID 一致。")]
		public int ID = 0;
		/// the AudioGroup on which to play the sound. If you're already targeting a preset track, you can leave it blank, otherwise the group you specify here will override it.
		[Tooltip("要路由到的 AudioMixerGroup。若已使用预设轨道可留空；填写后会覆盖轨道默认分组路由。")]
		public AudioMixerGroup AudioGroup = null;
		/// if (for some reason) you've already got an audiosource and wouldn't like to use the built-in pool system, you can specify it here 
		[Tooltip("可选：指定已有 AudioSource，绕过内置对象池。填写后将优先复用该 AudioSource。")]
		public AudioSource RecycleAudioSource = null;
		/// whether or not this sound should loop
		[Tooltip("是否循环播放该声音。")]
		public bool Loop = false;
		/// whether or not this sound should continue playing when transitioning to another scene
		[Tooltip("切换场景时是否继续播放该声音。")]
		public bool Persistent = false;
		/// whether or not this sound should play if the same sound clip is already playing
		[Tooltip("同一 AudioClip 已在播放时，是否允许再次播放。关闭可避免重复叠播。")]
		public bool DoNotPlayIfClipAlreadyPlaying = false;
		/// the maximum amount of instances of this sound allowed to play at once. use -1 for unlimited concurrent plays 
		[Tooltip("同一声音允许同时播放的最大实例数。设为 -1 表示不限制。")]
		public int MaximumConcurrentInstances = -1;
		/// if this is true, this sound will stop playing when stopping the feedback
		[Tooltip("若用于反馈播放链，停止反馈时是否同时停止该声音。")]
		public bool StopSoundOnFeedbackStop = false;
        
		[Header("Fade In")]
		/// whether or not to fade this sound in when playing it
		[Tooltip("播放时是否先执行淡入。")]
		public bool FadeIn = false;
		/// if fading, the volume at which to start the fade
		[Tooltip("淡入起始音量（仅在 FadeIn 开启时生效）。")]
		[MMCondition("FadeIn", true)]
		public float FadeInInitialVolume = 0f;
		/// if fading, the duration of the fade, in seconds
		[Tooltip("淡入持续时间（秒，仅在 FadeIn 开启时生效）。")]
		[MMCondition("FadeIn", true)]
		public float FadeInDuration = 1f;
		/// if fading, the tween over which to fade the sound 
		[Tooltip("淡入使用的 Tween 曲线（仅在 FadeIn 开启时生效）。")]
		public MMTweenType FadeInTween = new MMTweenType(MMTween.MMTweenCurve.EaseInOutQuartic, "FadeIn");
		
		[Header("Fade Out")]
		/// whether or not to fade this sound in when stopping the feedback
		[Tooltip("停止时是否执行淡出。")]
		public bool FadeOutOnStop = false;
		/// if fading out, the duration of the fade, in seconds
		[Tooltip("淡出持续时间（秒，仅在 FadeOutOnStop 开启时生效）。")]
		[MMCondition("FadeOutOnStop", true)]
		public float FadeOutDuration = 1f;
		/// if fading out, the tween over which to fade the sound 
		[Tooltip("淡出使用的 Tween 曲线（仅在 FadeOutOnStop 开启时生效）。")]
		public MMTweenType FadeOutTween = new MMTweenType(MMTween.MMTweenCurve.EaseInOutQuartic, "FadeOutOnStop");
        
		[Header("Solo")]
		/// whether or not this sound should play in solo mode over its destination track. If yes, all other sounds on that track will be muted when this sound starts playing
		[Tooltip("是否在目标轨道启用 Solo。开启后，该轨道上的其他声音会在本声音开始播放时被静音。")]
		public bool SoloSingleTrack = false;
		/// whether or not this sound should play in solo mode over all other tracks. If yes, all other tracks will be muted when this sound starts playing
		[Tooltip("是否全局 Solo。开启后，其他所有轨道会在本声音开始播放时被静音。")]
		public bool SoloAllTracks = false;
		/// if in any of the above solo modes, AutoUnSoloOnEnd will unmute the track(s) automatically once that sound stops playing
		[Tooltip("若启用了任一 Solo 模式，开启后会在本声音结束时自动取消静音对应轨道。")]
		public bool AutoUnSoloOnEnd = false;

		[Header("Spatial Settings")]
		/// Pans a playing sound in a stereo way (left or right). This only applies to sounds that are Mono or Stereo.
		[Tooltip("设置立体声声像（左/右）。仅对 Mono 或 Stereo 声音有效。")]
		[Range(-1f,1f)]
		public float PanStereo;
		/// Sets how much this AudioSource is affected by 3D spatialisation calculations (attenuation, doppler etc). 0.0 makes the sound full 2D, 1.0 makes it full 3D.
		[Tooltip("设置 3D 空间化混合比例（衰减、Doppler 等）。0 表示完全 2D，1 表示完全 3D。")]
		[Range(0f,1f)]
		public float SpatialBlend;
		/// a Transform this sound can 'attach' to and follow it along as it plays - when used on a feedback, will only apply if the feedback's AttachToTransform is empty
		[Tooltip("声音跟随的 Transform。用于反馈时，只有当反馈自身的 AttachToTransform 为空时才会采用此值。")]
		public Transform AttachToTransform;
		
		[Header("Effects")]
		/// Bypass effects (Applied from filter components or global listener filters).
		[Tooltip("是否绕过效果器（组件滤波器或全局 Listener 滤波器）。")]
		public bool BypassEffects = false;
		/// When set global effects on the AudioListener will not be applied to the audio signal generated by the AudioSource. Does not apply if the AudioSource is playing into a mixer group.
		[Tooltip("开启后，不应用 AudioListener 的全局效果。若该 AudioSource 已路由到 Mixer Group，则此项通常不生效。")]
		public bool BypassListenerEffects = false;
		/// When set doesn't route the signal from an AudioSource into the global reverb associated with reverb zones.
		[Tooltip("开启后，不把该 AudioSource 信号发送到 Reverb Zone 的全局混响。")]
		public bool BypassReverbZones = false;
		/// Sets the priority of the AudioSource.
		[Tooltip("设置 AudioSource 优先级。数值越小优先级越高。")]
		[Range(0, 256)]
		public int Priority = 128;
		/// The amount by which the signal from the AudioSource will be mixed into the global reverb associated with the Reverb Zones.
		[Tooltip("该 AudioSource 发送到 Reverb Zone 全局混响的混合比例。")]
		[Range(0f,1.1f)]
		public float ReverbZoneMix = 1f;
        
		[Header("3D Sound Settings")]
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
		[MMCondition("UseCustomRolloffCurve", true)]
		public AnimationCurve CustomRolloffCurve;
		/// whether or not to use a custom curve for spatial blend
		[Tooltip("是否使用自定义 Spatial Blend 曲线。开启后才会使用下方曲线。")]
		public bool UseSpatialBlendCurve = false;
		/// the curve to use for custom spatial blend if UseSpatialBlendCurve is true
		[Tooltip("自定义 Spatial Blend 曲线（仅在 UseSpatialBlendCurve 开启时生效）。")]
		[MMCondition("UseSpatialBlendCurve", true)]
		public AnimationCurve SpatialBlendCurve;
		/// whether or not to use a custom curve for reverb zone mix
		[Tooltip("是否使用自定义 Reverb Zone Mix 曲线。开启后才会使用下方曲线。")]
		public bool UseReverbZoneMixCurve = false;
		/// the curve to use for custom reverb zone mix if UseReverbZoneMixCurve is true
		[Tooltip("自定义 Reverb Zone Mix 曲线（仅在 UseReverbZoneMixCurve 开启时生效）。")]
		[MMCondition("UseReverbZoneMixCurve", true)]
		public AnimationCurve ReverbZoneMixCurve;
		/// whether or not to use a custom curve for spread
		[Tooltip("是否使用自定义 Spread 曲线。开启后才会使用下方曲线。")]
		public bool UseSpreadCurve = false;
		/// the curve to use for custom spread if UseSpreadCurve is true
		[Tooltip("自定义 Spread 曲线（仅在 UseSpreadCurve 开启时生效）。")]
		[MMCondition("UseSpreadCurve", true)]
		public AnimationCurve SpreadCurve;
		
		[MMInspectorButton("TestPlaySound")]
		public bool TestPlaySoundButton;
		
		protected AudioClip _randomClip;
		protected MMShufflebag<int> _randomUniqueShuffleBag;
		protected int _currentIndex;
		protected float _randomPlaybackTime;
		protected float _randomPlaybackDuration;
		protected MMSoundManagerPlayOptions _options;
		protected AudioSource _playedAudioSource;
		protected AudioClip _lastPlayedClip;
		protected bool _initialized = false;
		protected AudioSource _editorAudioSource;
		protected float _lastPlayTimestamp = -float.MaxValue;

		protected virtual void Initialization()
		{
			_lastPlayedClip = null;
			
			if (RandomSfx == null)
			{
				RandomSfx = Array.Empty<AudioClip>();
			}
			
			if (RandomUnique)
			{
				_randomUniqueShuffleBag = new MMShufflebag<int>(RandomSfx.Length);
				for (int i = 0; i < RandomSfx.Length; i++)
				{
					_randomUniqueShuffleBag.Add(i,1);
				}
			}
			
			_initialized  = true;
		}
		
		public virtual void Play(Vector3 position)
		{
			if (!_initialized || (RandomUnique && _randomUniqueShuffleBag == null))
			{
				Initialization();
			}
			
			if (RandomSfx.Length > 0)
			{
				_randomClip = PickRandomClip();

				if (_randomClip != null)
				{
					PlaySound(_randomClip, position);
					return;
				}
			}
			
			if ((Sfx != null))
			{
				PlaySound(Sfx, position);
				return;
			}
		}
		
		protected virtual AudioSource PlaySound(AudioClip sfx, Vector3 position)
		{
			if (DoNotPlayIfClipAlreadyPlaying) 
			{
				if ((MMSoundManager.Instance.FindByClip(sfx) != null) && (MMSoundManager.Instance.FindByClip(sfx).isPlaying))
				{
					return null;
				}
			}

			if (MaximumConcurrentInstances >= 0)
			{
				if (MMSoundManager.Instance.CurrentlyPlayingCount(sfx) >= MaximumConcurrentInstances)
				{
					return null;
				}
			}
			
			_lastPlayedClip = null;
            
			float volume = Random.Range(MinVolume, MaxVolume);
            
			float pitch = Random.Range(MinPitch, MaxPitch);
			RandomizeTimes();

			_options.MmSoundManagerTrack = MmSoundManagerTrack;
			_options.Location = position;
			_options.Loop = Loop;
			_options.Volume = volume;
			_options.ID = ID;
			_options.Fade = FadeIn;
			_options.FadeInitialVolume = FadeInInitialVolume;
			_options.FadeDuration = FadeInDuration;
			_options.FadeTween = FadeInTween;
			_options.Persistent = Persistent;
			_options.RecycleAudioSource = RecycleAudioSource;
			_options.AudioGroup = AudioGroup;
			_options.Pitch = pitch;
			_options.PlaybackTime = _randomPlaybackTime;
			_options.PlaybackDuration = _randomPlaybackDuration;
			_options.PanStereo = PanStereo;
			_options.SpatialBlend = SpatialBlend;
			_options.SoloSingleTrack = SoloSingleTrack;
			_options.SoloAllTracks = SoloAllTracks;
			_options.AutoUnSoloOnEnd = AutoUnSoloOnEnd;
			_options.BypassEffects = BypassEffects;
			_options.BypassListenerEffects = BypassListenerEffects;
			_options.BypassReverbZones = BypassReverbZones;
			_options.Priority = Priority;
			_options.ReverbZoneMix = ReverbZoneMix;
			_options.DopplerLevel = DopplerLevel;
			_options.Spread = Spread;
			_options.RolloffMode = RolloffMode;
			_options.MinDistance = MinDistance;
			_options.MaxDistance = MaxDistance;
			_options.AttachToTransform = AttachToTransform;
			_options.UseSpreadCurve = UseSpreadCurve;
			_options.SpreadCurve = SpreadCurve;
			_options.UseCustomRolloffCurve = UseCustomRolloffCurve;
			_options.CustomRolloffCurve = CustomRolloffCurve;
			_options.UseSpatialBlendCurve = UseSpatialBlendCurve;
			_options.SpatialBlendCurve = SpatialBlendCurve;
			_options.UseReverbZoneMixCurve = UseReverbZoneMixCurve;
			_options.ReverbZoneMixCurve = ReverbZoneMixCurve;
			_options.DoNotAutoRecycleIfNotDonePlaying = true;
			
			_playedAudioSource = MMSoundManagerSoundPlayEvent.Trigger(sfx, _options);
			_lastPlayedClip = sfx;

			return _playedAudioSource;
		}
		
		public virtual void RandomizeTimes()
		{
			_randomPlaybackTime = Random.Range(PlaybackTime.x, PlaybackTime.y);
			_randomPlaybackDuration = Random.Range(PlaybackDuration.x, PlaybackDuration.y);
		}
		
		protected virtual AudioClip PickRandomClip()
		{
			int newIndex = 0;
	        
			if (!SequentialOrder)
			{
				if (RandomUnique)
				{
					newIndex = _randomUniqueShuffleBag.Pick();
				}
				else
				{
					newIndex = Random.Range(0, RandomSfx.Length);	
				}
			}
			else
			{
				newIndex = _currentIndex;
		        
				if (newIndex >= RandomSfx.Length)
				{
					if (SequentialOrderHoldLast)
					{
						newIndex--;
						if (SequentialOrderHoldCooldownDuration > 0)
						{
							newIndex = 0;    
						}
					}
					else
					{
						newIndex = 0;
					}
				}
				_currentIndex = newIndex + 1;
			}
			return RandomSfx[newIndex];
		}
		
		public virtual async void TestPlaySound()
		{
			if (!_initialized || (RandomUnique && _randomUniqueShuffleBag == null))
			{
				Initialization();
			}
			
			AudioClip tmpAudioClip = null;
			
			if (Sfx != null)
			{
				tmpAudioClip = Sfx;
			}

			if ((RandomSfx != null) && (RandomSfx.Length > 0))
			{
				tmpAudioClip = PickRandomClip();
			}

			if (tmpAudioClip == null) 
			{
				Debug.LogError("This SoundData can't play in editor mode, you haven't set its Sfx.");
				return;
			}

			float volume = Random.Range(MinVolume, MaxVolume);
			float pitch = Random.Range(MinPitch, MaxPitch);
			RandomizeTimes();
			GameObject temporaryAudioHost = new GameObject("EditorTestAS_WillAutoDestroy");
			SceneManager.MoveGameObjectToScene(temporaryAudioHost.gameObject, SceneManager.GetActiveScene());
			temporaryAudioHost.transform.position = Vector3.zero;
			if (!Application.isPlaying)
			{
				temporaryAudioHost.AddComponent<MMForceDestroyInPlayMode>();
			}
			_editorAudioSource = temporaryAudioHost.AddComponent<AudioSource>() as AudioSource;
			PlayAudioSource(_editorAudioSource, tmpAudioClip, volume, pitch, _randomPlaybackTime, _randomPlaybackDuration);
			_lastPlayTimestamp = Time.time;
			_lastPlayedClip = tmpAudioClip;
			float length = 0f;
			if (tmpAudioClip != null)
			{
				length = (_randomPlaybackDuration > 0) ? _randomPlaybackDuration : tmpAudioClip.length;
			}
			else
			{
				length = 10f;
			}
			length *= 1000;
			length = length / Mathf.Abs(pitch);
			await Task.Delay((int)length);
			Object.DestroyImmediate(temporaryAudioHost);
		}
		
		protected virtual void PlayAudioSource(AudioSource audioSource, AudioClip sfx, float volume, float pitch, float time, float playbackDuration)
		{
			audioSource.clip = sfx;
			audioSource.time = time;
			audioSource.volume = volume;
			audioSource.pitch = pitch;
			audioSource.loop = false;
			audioSource.Play(); 
		}
	}
}
