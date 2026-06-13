using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will play the associated particles system on play, and stop it on stop
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可在播放时控制场景中的 ParticleSystem（Play / Stop / Pause / Emit）。你可以指定主粒子系统，也可以提供随机粒子系统列表；当随机列表不为空时，每次会优先从列表中随机选择一个执行。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Particles/Particles Play")]
	public class MMF_Particles : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(DeclaredDuration); } set { DeclaredDuration = value;  } }
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => BoundParticleSystem = FindAutomatedTarget<ParticleSystem>();
		
		#if UNITY_EDITOR
		/// sets the inspector color for this feedback
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.ParticlesColor; } }
		public override bool EvaluateRequiresSetup() { return (BoundParticleSystem == null); }
		public override string RequiredTargetText { get { return BoundParticleSystem != null ? BoundParticleSystem.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置 BoundParticleSystem 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		
		public enum Modes { Play, Stop, Pause, Emit }

		[MMFInspectorGroup("Bound Particles", true, 41, true)]
		/// whether to Play, Stop or Pause the target particle system when that feedback is played
		[Tooltip("播放模式：播放/停止/暂停/发射。仅在发射模式下会使用发射计数。")]
		public Modes Mode = Modes.Play;
		/// 在 Emit 模式下，每次发射的粒子数量
		[Tooltip("在 Emit 模式下，每次发射的粒子数量")]
		[MMFEnumCondition("Mode", (int)Modes.Emit)]
		public int EmitCount = 100;
		/// the particle system to play with this feedback
		[Tooltip("要控制的粒子系统")]
		public ParticleSystem BoundParticleSystem;
		/// 可选的额外 ParticleSystem 列表 
		[Tooltip("可选随机粒子系统列表。若列表不为空，每次会从该列表随机选取一个执行，BoundParticleSystem 将不参与本次执行。")]
		public List<ParticleSystem> RandomParticleSystems;
		/// if this is true, the particles will be moved to the position passed in parameters
		[Tooltip("若开启，会把粒子系统移动到传入位置。注意：Emit 模式下不会移动 Transform，而是把该位置写入 Emit 参数。")]
		public bool MoveToPosition = false;
		/// if this is true, the particle system's object will be set active on play
		[Tooltip("若开启，播放前会强制激活目标粒子系统对象。")]
		public bool ActivateOnPlay = false;
		/// if this is true, the particle system will be stopped on initialization
		[Tooltip("若开启，初始化时会先停止粒子系统。")]
		public bool StopSystemOnInit = true;
		/// if this is true, the particle system will be stopped on reset
		[Tooltip("若开启，反馈 Reset 时会停止粒子系统。")]
		public bool StopSystemOnReset = true;
		/// if this is true, the particle system will be stopped on feedback stop
		[Tooltip("若开启，反馈 Stop 时会停止粒子系统。")]
		public bool StopSystemOnStopFeedback = true;

		/// the duration for the player to consider. This won't impact your particle system, but is a way to communicate to the MMF Player the duration of this feedback. Usually you'll want it to match your actual particle system, and setting it can be useful to have this feedback work with holding pauses.
		[Tooltip("供播放器参考的持续时间。它不会直接影响你的粒子系统，而是用于向 MMF_Player 声明此反馈应持续多久。通常建议将其设置为与你的实际粒子时长一致，这样在使用 Holding Pause 时才能正确协同工作。")]
		public float DeclaredDuration = 0f;

		[MMFInspectorGroup("Simulation Speed", true, 43, false)]
		/// whether or not to force a specific simulation speed on the target particle system(s)
		[Tooltip("是否强制目标粒子系统使用指定的 Simulation Speed。")]
		public bool ForceSimulationSpeed = false;
		/// The min and max values at which to randomize the simulation speed, if ForceSimulationSpeed is true. A new value will be randomized every time this feedback plays
		[Tooltip("仅在 ForceSimulationSpeed 开启时生效：Simulation Speed 会在该范围内随机，每次播放都会重新随机。")]
		[MMFCondition("ForceSimulationSpeed", true)]
		public Vector2 ForcedSimulationSpeed = new Vector2(0.1f,1f);

		protected ParticleSystem.EmitParams _emitParams;

		/// <summary>
		/// On init we stop our particle system
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			if (RandomParticleSystems == null)
			{
				RandomParticleSystems = new List<ParticleSystem>();
			}
			if (StopSystemOnInit)
			{
				StopParticles();
			}
		}

		/// <summary>
		/// On play we play our particle system
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			PlayParticles(position);
		}
        
		/// <summary>
		/// On Stop, stops the particle system
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (StopSystemOnStopFeedback)
			{
				StopParticles();
			}
		}

		/// <summary>
		/// On Reset, stops the particle system 
		/// </summary>
		protected override void CustomReset()
		{
			base.CustomReset();

			if (InCooldown)
			{
				return;
			}

			if (StopSystemOnReset)
			{
				StopParticles();
			}
		}

		/// <summary>
		/// Plays a particle system
		/// </summary>
		/// <param name="position"></param>
		protected virtual void PlayParticles(Vector3 position)
		{
			if (MoveToPosition)
			{
				if (Mode != Modes.Emit)
				{
					BoundParticleSystem.transform.position = position;
					foreach (ParticleSystem system in RandomParticleSystems)
					{
						system.transform.position = position;
					}	
				}
				else
				{
					_emitParams.position = position;
				}
			}

			if (ActivateOnPlay)
			{
				BoundParticleSystem.gameObject.SetActive(true);
				foreach (ParticleSystem system in RandomParticleSystems)
				{
					system.gameObject.SetActive(true);
				}
			}

			if (RandomParticleSystems.Count > 0)
			{
				int random = Random.Range(0, RandomParticleSystems.Count);
				HandleParticleSystemAction(RandomParticleSystems[random]);
			}
			else if (BoundParticleSystem != null)
			{
				HandleParticleSystemAction(BoundParticleSystem);
			}
		}

		/// <summary>
		/// Changes the target particle system's sim speed if needed, and calls the specified action on it
		/// </summary>
		/// <param name="targetParticleSystem"></param>
		protected virtual void HandleParticleSystemAction(ParticleSystem targetParticleSystem)
		{
			if (ForceSimulationSpeed)
			{
				ParticleSystem.MainModule main = targetParticleSystem.main;
				main.simulationSpeed = Random.Range(ForcedSimulationSpeed.x, ForcedSimulationSpeed.y);
			}
			
			switch (Mode)
			{
				case Modes.Play:
					targetParticleSystem?.Play();
					break;
				case Modes.Emit:
					_emitParams.applyShapeToPosition = true;
					targetParticleSystem.Emit(_emitParams, EmitCount);
					break;
				case Modes.Stop:
					targetParticleSystem?.Stop();
					break;
				case Modes.Pause:
					targetParticleSystem?.Pause();
					break;
			}
		}

		/// <summary>
		/// Stops all particle systems
		/// </summary>
		protected virtual void StopParticles()
		{
			foreach(ParticleSystem system in RandomParticleSystems)
			{
				system?.Stop();
			}
			if (BoundParticleSystem != null)
			{
				BoundParticleSystem.Stop();
			}            
		}
	}
}

