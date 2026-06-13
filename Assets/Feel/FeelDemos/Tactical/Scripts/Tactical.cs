using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace MoreMountains.Feel
{
	[AddComponentMenu("")]
	/// <summary>
	/// A class used to handle the characters in Feel's Tactical demo scene, detects input,
	/// shoots while a button is pressed, stops shooting when released, handles reload
	/// </summary>
	public class Tactical : MonoBehaviour
	{
		[Header("Cooldown")]
		/// a duration, in seconds, between two shots, during which shots are prevented
		[Tooltip("两次射击之间的冷却持续时间（秒）；在此期间无法开枪")]
		public float CooldownDuration = 0.1f;

		[Header("Bindings")] 
		/// the position of the shot's impact
		[Tooltip("子弹命中点位置")]
		public Transform ImpactPosition;
        
		[Header("Feedbacks")]
		/// a feedback to call when shooting
		[Tooltip("射击时要触发的反馈")]
		public MMFeedbacks ShootFeedback;
		/// a feedback to call when shooting stops
		[Tooltip("停止射击时要触发的反馈")]
		public MMFeedbacks ShootStopFeedback;
		/// a feedback to call when a reload happens
		[Tooltip("换弹时要触发的反馈")]
		public MMFeedbacks ReloadFeedback;

		protected float _lastJumpStartedAt = -100f;
		protected int _magazine = 15;
        
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
			if (FeelDemosInputHelper.CheckMainActionInputPressed())
			{
				Shoot();
			}
			if (FeelDemosInputHelper.CheckMainActionInputUpThisFrame())
			{
				ShootStop();
			}
		}

		/// <summary>
		/// Shoots if possible
		/// </summary>
		protected virtual void Shoot()
		{
			if (Time.time - _lastJumpStartedAt > CooldownDuration)
			{
				float damage = Random.Range(20, 200);
				ShootFeedback?.PlayFeedbacks(ImpactPosition.position, damage);
				_lastJumpStartedAt = Time.time;
				_magazine--;
			}         
		}

		/// <summary>
		/// Stops shooting
		/// </summary>
		protected virtual void ShootStop()
		{
			ShootStopFeedback?.PlayFeedbacks();
			if (_magazine < 0)
			{
				ReloadFeedback?.PlayFeedbacks();
				_magazine = 15;
			}
		}
	}
}
