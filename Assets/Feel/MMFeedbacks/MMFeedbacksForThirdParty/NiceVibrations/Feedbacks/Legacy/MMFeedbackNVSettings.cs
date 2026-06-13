using UnityEngine;
#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
using Lofelt.NiceVibrations;
#endif

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// A class used to store and manage common Nice Vibrations feedback settings
	/// </summary>
	[System.Serializable]
	public class MMFeedbackNVSettings
	{
		#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
		/// 是否强制在指定的 gamepad 上播放这个 haptic。
		[Tooltip("是否强制在指定的 gamepad 上播放这个 haptic。")]
		public bool ForceGamepadID = false;
		/// 要在其上播放此 haptic 的 gamepad ID。
		[Tooltip("要在其上播放此触觉的游戏手柄编号。")]
		public int GamepadID = 0;
		/// 是否仅在设备支持 haptics 时才播放该 haptic。
		[Tooltip("是否仅在设备支持 haptics 时才播放该 haptic。")]
		public bool OnlyPlayIfHapticsSupported = true;
		/// 是否仅在设备满足高级 haptics 要求时才播放该 haptic。
		[Tooltip("是否仅在设备满足高级 haptics 要求时才播放该 haptic。")]
		public bool OnlyPlayIfAdvancedRequirementsMet = false;
		/// 是否仅在设备支持 amplitude modulation 时才播放该 haptic。
		[Tooltip("是否仅在设备支持 amplitude modulation 时才播放该 haptic。")]
		public bool OnlyPlayIfAmplitudeModulationSupported = false;
		/// 是否仅在设备支持 frequency modulation 时才播放该 haptic。
		[Tooltip("是否仅在设备支持 frequency modulation 时才播放该 haptic。")]
		public bool OnlyPlayIfFrequencyModulationSupported = false;

		/// <summary>
		/// If necessary, forces the current haptic to play on a specific gamepad
		/// </summary>
		public virtual void SetGamepad()
		{
			if (ForceGamepadID)
			{
				GamepadRumbler.SetCurrentGamepad(GamepadID);
			}
		}
        
		/// <summary>
		/// Whether or not this haptic can play based on the specified conditions
		/// </summary>
		/// <returns></returns>
		public virtual bool CanPlay()
		{
			#if UNITY_IOS || UNITY_ANDROID
            if (OnlyPlayIfHapticsSupported && !DeviceCapabilities.isVersionSupported)
            {
                return false;
            }
			#endif

			if (OnlyPlayIfAdvancedRequirementsMet && !DeviceCapabilities.meetsAdvancedRequirements)
			{
				return false;
			}

			if (OnlyPlayIfAmplitudeModulationSupported && !DeviceCapabilities.hasAmplitudeModulation)
			{
				return false;
			}

			if (OnlyPlayIfFrequencyModulationSupported && !DeviceCapabilities.hasFrequencyModulation)
			{
				return false;
			}

			return true;
		}
		#endif
	}
}