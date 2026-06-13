using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// this feedback will let you apply forces and torques (relative or not) to a Rigidbody
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可对 Rigidbody 施加力或扭矩。支持 AddForce / AddRelativeForce / AddTorque / AddRelativeTorque，可按最小值与最大值随机。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("GameObject/Rigidbody")]
	public class MMF_Rigidbody : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.GameObjectColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetRigidbody == null); }
		public override string RequiredTargetText { get { return TargetRigidbody != null ? TargetRigidbody.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置 TargetRigidbody 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public enum Modes { AddForce, AddRelativeForce, AddTorque, AddRelativeTorque }
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetRigidbody = FindAutomatedTarget<Rigidbody>();

		[MMFInspectorGroup("Rigidbody", true, 61, true)]
		/// the rigidbody to target on play
		[Tooltip("播放时要作用的主体刚体。")]
		public Rigidbody TargetRigidbody;
		/// a list of extra rigidbodies to target on play
		[Tooltip("播放时要一并作用的额外 Rigidbody 列表。")]
		public List<Rigidbody> ExtraTargetRigidbodies;
		/// the selected mode for this feedback
		[Tooltip("作用模式：AddForce/ AddRelativeForce 施加线性力；AddTorque/ AddRelativeTorque 施加扭矩。")]
		public Modes Mode = Modes.AddForce;
		/// the min force or torque to apply
		[Tooltip("随机力/扭矩的最小值（每次播放会在 MinForce 与 MaxForce 之间随机）。")]
		public Vector3 MinForce;
		/// the max force or torque to apply
		[Tooltip("随机力/扭矩的最大值（每次播放会在 MinForce 与 MaxForce 之间随机）。")]
		public Vector3 MaxForce;
		/// the force mode to apply
		[Tooltip("施力模式（ForceMode）。会影响力/扭矩的施加方式。")]
		public ForceMode AppliedForceMode = ForceMode.Impulse;
		/// if this is true, the velocity of the rigidbody will be reset before applying the new force
		[Tooltip("若开启，在施加新力之前会先将线速度重置为 0。")]
		public bool ResetVelocityOnPlay = false;
		/// if this is true, the angular velocity of the rigidbody will be reset before applying the new force
		[Tooltip("若开启，在施加新力之前会先将角速度重置为 0。")]
		public bool ResetAngularVelocityOnPlay = false;
		/// if this is true, the magnitude of the min/max force will be applied in the target transform's forward direction
		[Tooltip("若开启，会忽略 Min/MaxForce 的原始方向，仅使用其随机后的“幅值”，并按目标 Transform.forward 方向施加。")] 
		public bool ForwardForce = false;

		protected Vector3 _force;

		/// <summary>
		/// On Custom Play, we apply our force or torque to the target rigidbody
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetRigidbody == null))
			{
				return;
			}

			_force.x = Random.Range(MinForce.x, MaxForce.x);
			_force.y = Random.Range(MinForce.y, MaxForce.y);
			_force.z = Random.Range(MinForce.z, MaxForce.z);

			if (!Timing.ConstantIntensity)
			{
				_force *= feedbacksIntensity;
			}
			
			ApplyForce(TargetRigidbody);
			if (ExtraTargetRigidbodies != null)
			{
				foreach (Rigidbody rb in ExtraTargetRigidbodies)
				{
					ApplyForce(rb);
				}	
			}
		}

		/// <summary>
		/// Applies the computed force to the target rigidbody
		/// </summary>
		/// <param name="rb"></param>
		protected virtual void ApplyForce(Rigidbody rb)
		{
			if(ResetVelocityOnPlay)
			{
				rb.velocity = Vector3.zero;
			}

			if (ResetAngularVelocityOnPlay)
			{
				rb.angularVelocity = Vector3.zero;
			}

			if (ForwardForce)
			{
				_force = _force.magnitude * rb.transform.forward;
			}
			
			switch (Mode)
			{
				case Modes.AddForce:
					rb.AddForce(_force, AppliedForceMode);
					break;
				case Modes.AddRelativeForce:
					rb.AddRelativeForce(_force, AppliedForceMode);
					break;
				case Modes.AddTorque:
					rb.AddTorque(_force, AppliedForceMode);
					break;
				case Modes.AddRelativeTorque:
					rb.AddRelativeTorque(_force, AppliedForceMode);
					break;
			}
		}
	}
}
