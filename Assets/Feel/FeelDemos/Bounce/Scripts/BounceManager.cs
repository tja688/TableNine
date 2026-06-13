using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.Feel
{
	/// <summary>
	/// An example class part of the Feel demos
	/// This class acts as a character controller for the Duck in the FeelDuck demo scene
	/// It looks for input, and jumps when instructed to
	/// </summary>
	[AddComponentMenu("")]
	public class BounceManager : MonoBehaviour
	{ 
		[Header("Cooldown")]
		/// a duration, in seconds, between two jumps, during which jumps are prevented
		[Tooltip("两次跳跃之间的冷却持续时间（秒）；在此期间无法再次跳跃")]
		public float CooldownDuration = 1f;

		[Header("Bindings")]
		/// the animator of the 'no feedback' version  
		[Tooltip("“无反馈”版本使用的动画器")]
		public Animator NoFeedbackAnimator;
		/// the animator of the 'feedback' version  
		[Tooltip("使用动画器的“带反馈”版本")]
		public Animator FeedbackAnimator;

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
                
			}
			else
			{
				if (FeedbackAnimator.isActiveAndEnabled)
				{
					FeedbackAnimator.SetTrigger("Jump");
				}
				if (NoFeedbackAnimator.isActiveAndEnabled)
				{
					NoFeedbackAnimator.SetTrigger("Jump");
				}
				_lastJumpStartedAt = Time.time;
			}            
		}
	}
}
