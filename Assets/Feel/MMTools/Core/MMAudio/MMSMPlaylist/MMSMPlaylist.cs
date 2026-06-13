using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MoreMountains.Tools
{
	/// <summary>
	/// A definition of a song, a part of a MMSM Playlist
	/// </summary>
	[Serializable]
	public class MMSMPlaylistSong
	{
		/// the name of the song, used only for organizational purposes in the inspector
		[Tooltip("歌曲名称，仅用于 Inspector 内组织与识别")]
		public string Name;
		/// the clip to play when this song plays
		[Tooltip("该歌曲播放时使用的音频剪辑")]
		public AudioClip Clip;
		/// the amount of time this song's been played
		[Tooltip("该歌曲累计被播放的次数")]
		[MMReadOnly] 
		public int PlayCount;
		/// the many options to control this song
		[Tooltip("用于控制该歌曲的选项")]
		public MMSoundManagerPlayOptions Options;

		/// <summary>
		/// On init, we reset our play count
		/// </summary>
		public virtual void Initialization()
		{
			PlayCount = 0;
		}
	}
	
	[CreateAssetMenu(menuName = "MoreMountains/Audio/MMSM Playlist")]
	[Serializable]
	public class MMSMPlaylist : ScriptableObject
	{
		public enum PlayModes { PlayForever, PlayOnce, PlayXTimes }
		public enum PlayOrders { Normal, ReverseOrder, Random, RandomUnique }
		
		[Header("Play Modes")]
		/// the sound manager track on which to play this playlist's songs
		[Tooltip("该播放列表歌曲输出到的 SoundManager 轨道")]
		public MMSoundManager.MMSoundManagerTracks Track = MMSoundManager.MMSoundManagerTracks.Music;
		/// the order in which to play songs (top to bottom, bottom to top, random, or random while trying to maintain playcount across songs
		[Tooltip("歌曲播放顺序（从上到下、从下到上、随机、或尽量均衡各歌曲播放次数的随机）")]
		public PlayOrders PlayOrder = PlayOrders.Normal;
		/// if this is true, random seed will be randomized by the system clock
		[Tooltip("若开启，随机种子将基于系统时钟自动随机化")]
		[MMEnumCondition("PlayOrder", (int)PlayOrders.Random, (int)PlayOrders.RandomUnique)]
		public bool RandomizeOrderSeed = true;
		/// whether to play this playlist forever, only once, or play songs until total playcount reaches MaxAmountOfPlays
		[Tooltip("播放模式：无限循环、仅播放一轮，或播放到总次数达到 MaxAmountOfPlays")]
		public PlayModes PlayMode = PlayModes.PlayForever;
		/// when in PlayXTimes mode, the max amount of plays before this playlist ends
		[Tooltip("在 PlayXTimes 模式下，播放列表结束前允许的最大播放次数")]
		[MMEnumCondition("PlayMode", (int)PlayModes.PlayXTimes)]
		public int MaxAmountOfPlays = 10;
		/// a playlist to switch to when reaching the end of this playlist
		[Tooltip("当前播放列表结束时要切换到的下一个播放列表")]
		[MMEnumCondition("PlayMode",(int)PlayModes.PlayOnce, (int)PlayModes.PlayXTimes)]
		public MMSMPlaylist NextPlaylist;
		/// the list of songs to play on this playlist
		[Tooltip("该播放列表中的歌曲列表")]
		public List<MMSMPlaylistSong> Songs;
		
		[Header("Debug")]
		/// the total number of times songs in this playlist have been played 
		[Tooltip("该播放列表内歌曲被播放的总次数")]
		[MMReadOnly] 
		public int PlayCount;

		protected List<int> _randomUniqueCandidates;

		/// <summary>
		/// On init, we initialize all our songs
		/// </summary>
		public virtual void Initialization()
		{
			PlayCount = 0;
			_randomUniqueCandidates = new List<int>();
			foreach (MMSMPlaylistSong song in Songs)
			{
				song.Initialization();
			}
		}
		
		/// <summary>
		/// Picks the index of the next song to play, returns the index of the song, or -2 if the end of the
		/// playlist's been reached, and -1 if the player should go idle
		/// </summary>
		/// <param name="direction"></param>
		/// <returns>
		/// -2 : end of playlist
		/// -1 : go to idle
		/// 0+ : next index to play in the playlist
		/// </returns>
		public virtual int PickNextIndex(int direction, int currentSongIndex, ref int queuedSongIndex, bool bypassLoop)
		{
			int newIndex = currentSongIndex;
			
			if (Songs.Count == 0)
			{
				return -1;
			}

			if (queuedSongIndex != -1)
			{
				int newRequestedIndex = queuedSongIndex;
				queuedSongIndex = -1;
				return newRequestedIndex;
			}
			
			if ((PlayCount >= Songs.Count) && (PlayMode == PlayModes.PlayOnce))
			{
				return -2;
			}

			if ((PlayMode == PlayModes.PlayXTimes) && (PlayCount >= MaxAmountOfPlays))
			{
				return -2;
			}
			
			if ((currentSongIndex >= 0) && (currentSongIndex < Songs.Count) && Songs[currentSongIndex].Options.Loop && !bypassLoop)
			{
				return currentSongIndex;
			}

			switch (PlayOrder)
			{
				case PlayOrders.Random:
					if (Songs.Count > 1)
					{
						while (newIndex == currentSongIndex)
						{
							newIndex = Random.Range(0, Songs.Count);
						}
					}
					else
					{
						newIndex = 0;
					}
					return newIndex;
				
				case PlayOrders.RandomUnique:
					
					bool allPlayed = true;
					int lowestPlayCount = int.MaxValue;
					_randomUniqueCandidates.Clear();
					
					for (int i = 0; i < Songs.Count; i++)
					{
						if (Songs[i].PlayCount <= lowestPlayCount && i != currentSongIndex)
						{
							allPlayed = false;
							lowestPlayCount = Songs[i].PlayCount;
							_randomUniqueCandidates.Add(i);	
						}
					}
					
					if (allPlayed)
					{
						if (Songs.Count > 1)
						{
							while (newIndex == currentSongIndex)
							{
								newIndex = Random.Range(0, Songs.Count);
							}	
						}
						else
						{
							newIndex = 0;
						}
					}
					else
					{
						int random = Random.Range(0, _randomUniqueCandidates.Count);
						
						newIndex = _randomUniqueCandidates[random];
					}

					return newIndex;
				
				case PlayOrders.Normal:
					break;
				
				case PlayOrders.ReverseOrder:
					direction = -1;
					break;
			}
			
			if (direction > 0)
			{
				newIndex = (currentSongIndex + 1) % Songs.Count;
			}
			else
			{
				newIndex = (currentSongIndex - 1);
				if (newIndex < 0)
				{
					newIndex = Songs.Count - 1;
				}
			}

			return newIndex;
		}

		/// <summary>
		/// Resets the playlist's play count and the playcount of all songs
		/// </summary>
		public virtual void ResetPlayCount()
		{
			PlayCount = 0;
			foreach (MMSMPlaylistSong song in Songs)
			{
				song.PlayCount = 0;
			}
		}
		
		/// <summary>
		/// On Validate we initialize our options
		/// </summary>
		protected virtual void OnValidate()
		{
			foreach (MMSMPlaylistSong song in Songs)
			{
				if (!song.Options.Initialized)
				{
					song.Options = MMSoundManagerPlayOptions.Default;
				}
			}
		}
	}
}
