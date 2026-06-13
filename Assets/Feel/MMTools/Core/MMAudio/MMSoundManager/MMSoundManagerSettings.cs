using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Tools
{
	/// <summary>
	/// This class stores MMSoundManager settings and lets you tweak them from the MMSoundManagerSettingsSO's inspector
	/// </summary>
	[Serializable]
	public class MMSoundManagerSettings
	{
		public const float _minimalVolume = 0.0001f;
		public const float _maxVolume = 10f;
		public const float _defaultVolume = 1f;
        
		[Header("Audio Mixer Control")] 
		/// whether or not the settings described below should override the ones defined in the AudioMixer 
		[Tooltip("是否使用下方设置覆盖 AudioMixer 中定义的参数。若关闭，将以 AudioMixer 参数为准")]
		public bool OverrideMixerSettings = true;

		[Header("Audio Mixer Exposed Parameters")]
		/// the name of the exposed MasterVolume parameter in the AudioMixer
		[Tooltip("音频混合器 中引入的 主音量 参数名")]
		public string MasterVolumeParameter = "MasterVolume";
		/// the name of the exposed MusicVolume parameter in the AudioMixer
		[Tooltip("音频混合器 中引入的 音乐音量 参数名")]
		public string MusicVolumeParameter = "MusicVolume";
		/// the name of the exposed SfxVolume parameter in the AudioMixer
		[Tooltip("音频混合器 中引入的 音效音量 参数名")]
		public string SfxVolumeParameter = "SfxVolume";
		/// the name of the exposed UIVolume parameter in the AudioMixer
		[Tooltip("音频混合器 中引入的 UI音量 参数名")]
		public string UIVolumeParameter = "UIVolume";
        
		[Header("Master")]
		/// the master volume
		[Range(_minimalVolume,_maxVolume)]
		[Tooltip("主音量")]
		[MMReadOnly]
		public float MasterVolume = _defaultVolume;
		/// whether the master track is active at the moment or not
		[Tooltip("Master 轨道当前是否处于激活状态")]
		[MMReadOnly] 
		public bool MasterOn = true;
		/// the volume of the master track before it was muted
		[Tooltip("Master 轨道被静音前的音量")]
		[MMReadOnly] 
		public float MutedMasterVolume;

		[Header("Music")]
		/// the music volume
		[Range(_minimalVolume,_maxVolume)]
		[Tooltip("音乐音量")]
		[MMReadOnly]
		public float MusicVolume = _defaultVolume; 
		/// whether the music track is active at the moment or not
		[Tooltip("音乐轨道当前是否处于激活状态")]
		[MMReadOnly] 
		public bool MusicOn = true;
		/// the volume of the music track before it was muted
		[Tooltip("音乐轨道被静音前的音量")]
		[MMReadOnly] 
		public float MutedMusicVolume;
        
		[Header("Sound Effects")]
		/// the sound fx volume
		[Range(_minimalVolume,_maxVolume)]
		[Tooltip("音效（特效）音量")]
		[MMReadOnly]
		public float SfxVolume = _defaultVolume;
		/// whether the SFX track is active at the moment or not
		[Tooltip("SFX 轨道当前是否处于激活状态")]
		[MMReadOnly] 
		public bool SfxOn = true;
		/// the volume of the SFX track before it was muted
		[Tooltip("SFX 轨道被静音前的音量")]
		[MMReadOnly] 
		public float MutedSfxVolume;
        
		[Header("UI")]
		/// the UI sounds volume
		[Range(_minimalVolume,_maxVolume)]
		[Tooltip("UI 声音音量")]
		[MMReadOnly]
		public float UIVolume = _defaultVolume;
		/// whether the UI track is active at the moment or not
		[Tooltip("UI 轨道当前是否处于激活状态")]
		[MMReadOnly] 
		public bool UIOn = true;
		/// the volume of the UI track before it was muted
		[Tooltip("UI 轨道被静音前的音量")]
		[MMReadOnly] 
		public float MutedUIVolume;
        
		[Header("Save & Load")]
		/// whether or not the MMSoundManager should automatically load settings when starting
		[Tooltip("MMSoundManager 启动时是否自动加载设置")]
		public bool AutoLoad = true;
		/// whether or not each change in the settings should be automaticall saved. If not, you'll have to call a save MMSoundManager event for settings to be saved.
		[Tooltip("设置每次变化后是否自动保存。若关闭，需要手动触发保存 MMSoundManager 设置的事件")]
		public bool AutoSave = false;
	}
}
