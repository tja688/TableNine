using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Scripting.APIUpdating;
using Random = UnityEngine.Random;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Audio/AudioSource")]
	[FeedbackHelp("此反馈可控制目标 AudioSource 的播放行为（Play / Pause / UnPause / Stop），并可在播放时对音量、音高和可选的随机音频片段进行随机化。")]
	public class MMF_AudioSource : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.SoundsColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetAudioSource == null); }
		public override string RequiredTargetText { get { return TargetAudioSource != null ? TargetAudioSource.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先指定 TargetAudioSource 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasRandomness => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetAudioSource = FindAutomatedTarget<AudioSource>();

		/// the possible ways to interact with the audiosource
		public enum Modes { Play, Pause, UnPause, Stop }

		[MMFInspectorGroup("Audiosource", true, 5, true)]
		/// the target audio source to play
		[Tooltip("要控制的目标音频源")]
		public AudioSource TargetAudioSource;
		/// whether we should play the audio source or stop it or pause it
		[Tooltip("控制模式：播放、暂停、取消暂停或停止。只有播放模式会使用底部随机片段、音量和音高设置。")]
		public Modes Mode = Modes.Play;
        
		[Header("Random Sound")]
		/// an array to pick a random sfx from
		[Tooltip("随机音效列表。若不为空，Play 时会从此列表随机选择并覆盖 TargetAudioSource.clip。")]
		public AudioClip[] RandomSfx;

		[MMFInspectorGroup("Audio Settings", true, 29)]
        
		[Header("Volume")]
		/// the minimum volume to play the sound at
		[Tooltip("Play 模式下随机音量范围的最小值")]
		public float MinVolume = 1f;
		/// the maximum volume to play the sound at
		[Tooltip("Play 模式下随机音量范围的最大值")]
		public float MaxVolume = 1f;

		[Header("Pitch")]
		/// the minimum pitch to play the sound at
		[Tooltip("Play 模式下随机音高范围的最小值")]
		public float MinPitch = 1f;
		/// the maximum pitch to play the sound at
		[Tooltip("Play 模式下随机音高范围的最大值")]
		public float MaxPitch = 1f;

		[Header("Mixer")]
		/// the audiomixer to play the sound with (optional)
		[Tooltip("可选的 AudioMixerGroup。用于把该声音路由到指定混音分组。")]
		public AudioMixerGroup SfxAudioMixerGroup;
        
		/// the duration of this feedback is the duration of the clip being played
		public override float FeedbackDuration { get { return _duration; } set { _duration = value; } }

		protected AudioClip _randomClip;
		protected float _duration;

		protected virtual string OwnerName => Owner != null ? Owner.name : "this MMF_Player";
		
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			if (Active)
			{
				if (RandomSfx == null)
				{
					RandomSfx = Array.Empty<AudioClip>();
				}
			}
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

			if (TargetAudioSource == null)
			{
				Debug.LogWarning("[AudioSource Feedback] The audio source feedback on " + OwnerName + " doesn't have a TargetAudioSource, it won't work. You need to specify one in its inspector.");
				return;
			}
             
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			switch(Mode)
			{
				case Modes.Play:
					if ((RandomSfx != null) && (RandomSfx.Length > 0))
					{
						_randomClip = PickRandomClip();
						if (_randomClip != null)
						{
							TargetAudioSource.clip = _randomClip;
						}
					}
					float volume = Random.Range(MinVolume, MaxVolume) * intensityMultiplier;
					float pitch = Random.Range(MinPitch, MaxPitch);
					if (TargetAudioSource.clip == null)
					{
						_duration = 0f;
						Debug.LogWarning("[AudioSource Feedback] The audio source feedback on " + OwnerName + " doesn't have an AudioClip assigned to its TargetAudioSource, it won't play.");
						return;
					}
					_duration = TargetAudioSource.clip.length;
					PlayAudioSource(TargetAudioSource, volume, pitch);
					break;

				case Modes.Pause:
					_duration = 0.1f;
					TargetAudioSource.Pause();
					break;

				case Modes.UnPause:
					_duration = 0.1f;
					TargetAudioSource.UnPause();
					break;

				case Modes.Stop:
					_duration = 0.1f;
					TargetAudioSource.Stop();
					break;
			}
		}

		protected virtual AudioClip PickRandomClip()
		{
			int startIndex = Random.Range(0, RandomSfx.Length);
			for (int i = 0; i < RandomSfx.Length; i++)
			{
				AudioClip clip = RandomSfx[(startIndex + i) % RandomSfx.Length];
				if (clip != null)
				{
					return clip;
				}
			}
			return null;
		}
         
		/// <summary>
		/// Plays the audiosource at the selected volume and pitch
		/// </summary>
		/// <param name="audioSource"></param>
		/// <param name="volume"></param>
		/// <param name="pitch"></param>
		protected virtual void PlayAudioSource(AudioSource audioSource, float volume, float pitch)
		{
			if ((audioSource == null) || (audioSource.clip == null))
			{
				return;
			}

			// we set the audio source volume to the one in parameters
			audioSource.volume = volume;
			audioSource.pitch = pitch;
			audioSource.timeSamples = 0;

			if (!NormalPlayDirection)
			{
				audioSource.pitch = -1;
				audioSource.timeSamples = audioSource.clip.samples - 1;
			}
            
			// we start playing the sound
			audioSource.Play();
		}

		/// <summary>
		/// Stops the audiosource from playing
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		public override void Stop(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			base.Stop(position, feedbacksIntensity);
			if (TargetAudioSource != null)
			{
				TargetAudioSource?.Stop();
			}            
		}
	}
}

