using MoreMountains.Feedbacks;
using UnityEngine;

namespace MoreMountains.Feel
{
	/// <summary>
	/// An example class part of the Feel demos
	/// This class acts as a character controller for the Duck in the FeelDuck demo scene
	/// It looks for input, and jumps when instructed to
	/// </summary>
	[AddComponentMenu("")]
	public class Duck : MonoBehaviour
	{
		[Header("Cooldown")]
		/// a duration, in seconds, between two jumps, during which jumps are prevented
		[Tooltip("两次跳跃之间的冷却持续时间（秒）；在此期间无法再次跳跃")]
		public float CooldownDuration = 1f;

		[Header("Feedbacks")]
		/// a feedback to call when jumping
		[Tooltip("跳跃时要触发的反馈")]
		public MMFeedbacks JumpFeedback;
		/// a feedback to call when landing
		[Tooltip("落地时要触发的反馈")]
		public MMFeedbacks LandingFeedback;
		/// a feedback to call when trying to jump while in cooldown
		[Tooltip("处于冷却期间仍尝试跳跃时要触发的反馈")]
		public MMFeedbacks DeniedFeedback;

		protected float _lastJumpStartedAt = -100f;

		/// <summary>
		/// On Update we look for input
		/// </summary>
		protected virtual void Update()
		{
			HandleInput();
		}

		/// <summary>
		/// Detects input
		/// </summary>
		protected virtual void HandleInput()
		{
			if (FeelDemosInputHelper.CheckMainActionInputPressedThisFrame())
			{
				Jump();
			}
		}

		/// <summary>
		/// Performs a jump if possible, otherwise plays a denied feedback
		/// </summary>
		protected virtual void Jump()
		{
			if (Time.time - _lastJumpStartedAt < CooldownDuration)
			{
				DeniedFeedback?.PlayFeedbacks();
			}
			else
			{
				JumpFeedback?.PlayFeedbacks();
				_lastJumpStartedAt = Time.time;
			}            
		}

		/// <summary>
		/// This method is called by the duck animator on the frame where it makes contact with the ground.
		/// In an actual game context, this may be called when you detect contact with the ground via a physics collision, a downward raycast, etc.
		/// </summary>
		public virtual void Land()
		{
			LandingFeedback?.PlayFeedbacks();
		}
	}
}
