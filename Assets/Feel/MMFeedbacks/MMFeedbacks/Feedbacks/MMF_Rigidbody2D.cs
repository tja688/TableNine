using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	#if MM_PHYSICS2D
	/// <summary>
	/// this feedback will let you apply forces and torques (relative or not) to a Rigidbody
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可对 Rigidbody2D 施加力或扭矩。支持 AddForce / AddRelativeForce / AddTorque，可按最小值与最大值随机。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("GameObject/Rigidbody2D")]
	public class MMF_Rigidbody2D : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.GameObjectColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetRigidbody2D == null); }
		public override string RequiredTargetText { get { return TargetRigidbody2D != null ? TargetRigidbody2D.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置 TargetRigidbody2D 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		
		protected override void AutomateTargetAcquisition() => TargetRigidbody2D = FindAutomatedTarget<Rigidbody2D>();

		public enum Modes { AddForce, AddRelativeForce, AddTorque}

		[MMFInspectorGroup("Rigidbody2D", true, 32, true)]
		/// the rigidbody to target on play
		[Tooltip("播放时要作用的主二维刚体。")]
		public Rigidbody2D TargetRigidbody2D;
		/// an extra list of rigidbodies to target on play
		[Tooltip("播放时要一并作用的额外 Rigidbody2D 列表。")]
		public List<Rigidbody2D> ExtraTargetRigidbodies2D;
		/// the selected mode for this feedback
		[Tooltip("模式：添加力/添加相对力使用底部最小力~最大力；添加增值税使用底部最小增值税~最大增值税作用。")]
		public Modes Mode = Modes.AddForce;
		/// the min force or torque to apply
		[Tooltip("随机施力的最小值（仅在 AddForce / AddRelativeForce 模式下生效）。")]
		[MMFEnumCondition("Mode", (int)Modes.AddForce, (int)Modes.AddRelativeForce)]
		public Vector2 MinForce;
		/// the max force or torque to apply
		[Tooltip("随机施力的最大值（仅在 AddForce / AddRelativeForce 模式下生效）。")]
		[MMFEnumCondition("Mode", (int)Modes.AddForce, (int)Modes.AddRelativeForce)]
		public Vector2 MaxForce;
		/// the min torque to apply to this rigidbody on play
		[Tooltip("随机扭矩的最小值（仅在 AddTorque 模式下生效）。")]
		[MMFEnumCondition("Mode", (int)Modes.AddTorque)]
		public float MinTorque;
		/// the max torque to apply to this rigidbody on play
		[Tooltip("随机扭矩的最大值（仅在 AddTorque 模式下生效）。")]
		[MMFEnumCondition("Mode", (int)Modes.AddTorque)]
		public float MaxTorque;
		/// the force mode to apply
		[Tooltip("施力模式（ForceMode2D）。会影响力/扭矩的施加方式。")]
		public ForceMode2D AppliedForceMode = ForceMode2D.Impulse;
		/// if this is true, the velocity of the rigidbody 2D will be reset before applying the new force
		[Tooltip("若开启，在施加新力之前会先将线速度重置为 0。")]
		public bool ResetVelocityOnPlay = false;
		/// if this is true, the angular velocity of the rigidbody 2D will be reset before applying the new force
		[Tooltip("若开启，在施加新力之前会先将角速度重置为 0。")]
		public bool ResetAngularVelocityOnPlay = false;

		protected Vector2 _force;
		protected float _torque;

		/// <summary>
		/// On Custom Play, we apply our force or torque to the target rigidbody
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetRigidbody2D == null))
			{
				return;
			}
			
			ApplyForce(TargetRigidbody2D, feedbacksIntensity);
			foreach (Rigidbody2D rb in ExtraTargetRigidbodies2D)
			{
				ApplyForce(rb, feedbacksIntensity);
			}
		}

		/// <summary>
		/// Applies the computed force to the target rigidbody
		/// </summary>
		/// <param name="rb"></param>
		/// <param name="feedbacksIntensity"></param>
		protected virtual void ApplyForce(Rigidbody2D rb, float feedbacksIntensity)
		{
			if(ResetVelocityOnPlay)
			{
				rb.velocity = Vector2.zero;
			}

			if (ResetAngularVelocityOnPlay)
			{
				rb.angularVelocity = 0f;
			}
			
			switch (Mode)
			{
				case Modes.AddForce:
					_force.x = Random.Range(MinForce.x, MaxForce.x);
					_force.y = Random.Range(MinForce.y, MaxForce.y);
					if (!Timing.ConstantIntensity) { _force *= feedbacksIntensity; }
					rb.AddForce(_force, AppliedForceMode);
					break;
				case Modes.AddRelativeForce:
					_force.x = Random.Range(MinForce.x, MaxForce.x);
					_force.y = Random.Range(MinForce.y, MaxForce.y);
					if (!Timing.ConstantIntensity) { _force *= feedbacksIntensity; }
					rb.AddRelativeForce(_force, AppliedForceMode);
					break;
				case Modes.AddTorque:
					_torque = Random.Range(MinTorque, MaxTorque);
					if (!Timing.ConstantIntensity) { _torque *= feedbacksIntensity; }
					rb.AddTorque(_torque, AppliedForceMode);
					break;
			}
		}
	}
	#endif
}
