using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This component will be automatically added by the MMF_Broadcast feedback
	/// </summary>
	public class MMF_BroadcastProxy : MonoBehaviour
	{
		/// the channel on which to broadcast
		[Tooltip("要广播到的通道")]
		[MMReadOnly]
		public int Channel;
		/// a debug view of the current level being broadcasted
		[Tooltip("当前广播数值的调试预览")]
		[MMReadOnly]
		public float DebugLevel;
		/// whether or not a broadcast is in progress (will be false while the value is not changing, and thus not broadcasting)
		[Tooltip("当前是否正在广播中（当数值未变化、因此不会发送广播时，该值为 false）")]
		[MMReadOnly]
		public bool BroadcastInProgress = false;

		public virtual float ThisLevel { get; set; }
		protected float _levelLastFrame;

		/// <summary>
		/// On Update we process our broadcast
		/// </summary>
		protected virtual void Update()
		{
			ProcessBroadcast();
		}

		/// <summary>
		/// Broadcasts the value if needed
		/// </summary>
		protected virtual void ProcessBroadcast()
		{
			BroadcastInProgress = false;
			if (ThisLevel != _levelLastFrame)
			{
				MMRadioLevelEvent.Trigger(Channel, ThisLevel);
				BroadcastInProgress = true;
			}
			DebugLevel = ThisLevel;
			_levelLastFrame = ThisLevel;
		}
	}    
}