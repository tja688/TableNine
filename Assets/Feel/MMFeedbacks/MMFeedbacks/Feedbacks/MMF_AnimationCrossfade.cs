using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// A feedback used to trigger an animation (bool, int, float or trigger) on the associated animator, with or without randomness
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可将目标 Animator 通过 CrossFade 过渡到指定状态。你可以固定指定一个状态，也可以从状态列表中随机选择。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Animation/Animation Crossfade")]
	public class MMF_AnimationCrossfade : MMF_Feedback 
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
        
		/// the possible modes that pilot triggers        
		public enum TriggerModes { SetTrigger, ResetTrigger }
        
		/// the possible ways to set a value
		public enum ValueModes { None, Constant, Random, Incremental }

		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.AnimationColor; } }
		public override bool EvaluateRequiresSetup() { return (BoundAnimator == null); }
		public override string RequiredTargetText { get { return BoundAnimator != null ? BoundAnimator.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先指定 BoundAnimator 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		
		/// the duration of this feedback is the declared duration 
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(DeclaredDuration); } set { DeclaredDuration = value;  } }
		public override bool HasRandomness => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => BoundAnimator = FindAutomatedTarget<Animator>();
		
		public enum Modes { Seconds, Normalized }

		[MMFInspectorGroup("Animation", true, 12, true)]
		/// the animator whose parameters you want to update
		[Tooltip("要更新动画器的参数")]
		public Animator BoundAnimator;
		/// the list of extra animators whose parameters you want to update
		[Tooltip("要一并更新参数的额外 Animator 列表")]
		public List<Animator> ExtraBoundAnimators;
		/// the duration for the player to consider. This won't impact your animation, but is a way to communicate to the MMF Player the duration of this feedback. Usually you'll want it to match your actual animation, and setting it can be useful to have this feedback work with holding pauses.
		[Tooltip("供播放器参考的持续时间。它不会直接影响你的动画，而是用于向 MMF_Player 声明此反馈应持续多久。通常建议将其设置为与你的实际动画时长一致，这样在使用 Holding Pause 时才能正确协同工作。")]
		public float DeclaredDuration = 0f;

		[MMFInspectorGroup("CrossFade", true, 16)]

		/// the name of the state towards which to transition. That's the name of the yellow or gray box in your Animator
		[Tooltip("要过渡到的状态名称，也就是 Animator 中黄色或灰色状态框的名字")]
		public string StateName = "NewState";
		/// an optional list of names of state towards which to transition. If left empty, StateName above will be used. If filled, a random state will be chosen from this list, ignoring the StateName specified above
		[Tooltip("可选的状态名称列表。若留空，将使用上方的 StateName；若填写，则会从此列表中随机选择一个状态，并忽略上方指定的 StateName")]
		public List<string> RandomStateNames = new List<string>();
		/// the ID of the Animator layer you want the crossfade to occur on
		[Tooltip("要执行交叉淡化的动画器层编号")]
		public int Layer = -1;
		/// the name of the Animator layer you want the crossfade to occur on. This is optional. If left empty, the layer ID above will be used, if not empty, the Layer id specified above will be ignored.
		[Tooltip("要执行 Crossfade 的 Animator 层名称。此项可选；若留空，将使用上方的层 ID；若填写此项，则上方指定的层 ID 将被忽略。")]
		public string LayerName = "";
		
		/// whether to specify timing data for the crossfade in seconds or in normalized (0-1) values  
		[Tooltip("Crossfade 的时间数据模式。选择 Seconds 时只使用下方秒数字段；选择 Normalized 时只使用下方标准化字段（0-1）。")] 
		public Modes Mode = Modes.Seconds;
		
		/// in Seconds mode, the duration of the transition, in seconds 
		[Tooltip("在 Seconds 模式下，过渡持续时间，单位为秒")]
		[MMFEnumCondition("Mode", (int)Modes.Seconds)]
		public float TransitionDuration = 0.1f;
		/// in Seconds mode, the offset at which to transition to, in seconds
		[Tooltip("在 Seconds 模式下，要过渡到的时间偏移，单位为秒")]
		[MMFEnumCondition("Mode", (int)Modes.Seconds)]
		public float TimeOffset = 0f;
		
		/// in Normalized mode, the duration of the transition, normalized between 0 and 1
		[Tooltip("在 Normalized 模式下，过渡持续时间，以 0 到 1 的标准化数值表示")]
		[MMFEnumCondition("Mode", (int)Modes.Normalized)]
		public float NormalizedTransitionDuration = 0.1f;
		/// in Normalized mode, the offset at which to transition to, normalized between 0 and 1
		[Tooltip("在 Normalized 模式下，要过渡到的时间偏移，以 0 到 1 的标准化数值表示")]
		[MMFEnumCondition("Mode", (int)Modes.Normalized)]
		public float NormalizedTimeOffset = 0f;
		
		/// according to Unity's docs, 'the time of the transition, normalized'. Really nobody's sure what this does. It's optional. 
		[Tooltip("按 Unity 文档描述，这是“normalized transition time”。该参数可选，通常保持 0 即可；仅在你明确需要微调过渡起始行为时再修改。")]
		public float NormalizedTransitionTime = 0f;

		protected int _stateHashName;
		protected int _layerID;

		/// <summary>
		/// Custom Init
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			_stateHashName = Animator.StringToHash(StateName);
			_layerID = Layer;
			if ((LayerName != "") && (BoundAnimator != null))
			{
				_layerID = BoundAnimator.GetLayerIndex(LayerName);
			}
		}

		/// <summary>
		/// On Play, checks if an animator is bound and crossfades to the specified state
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (BoundAnimator == null)
			{
				Debug.LogWarning("[Animation Crossfade Feedback] The animation crossfade feedback on "+Owner.name+" doesn't have a BoundAnimator, it won't work. You need to specify one in its inspector.");
				return;
			}
			
			if (RandomStateNames.Count > 0)
			{
				int randomIndex = UnityEngine.Random.Range(0, RandomStateNames.Count);
				StateName = RandomStateNames[randomIndex];
				_stateHashName = Animator.StringToHash(StateName);
			}

			CrossFade(BoundAnimator);
			foreach (Animator animator in ExtraBoundAnimators)
			{
				CrossFade(animator);
			}
		}

		/// <summary>
		/// Crossfades either via fixed time or regular (normalized) calls
		/// </summary>
		/// <param name="targetAnimator"></param>
		protected virtual void CrossFade(Animator targetAnimator)
		{
			switch (Mode)
			{
				case Modes.Seconds:
					targetAnimator.CrossFadeInFixedTime(_stateHashName, TransitionDuration, _layerID, TimeOffset, NormalizedTransitionTime);
					break;
				case Modes.Normalized:
					targetAnimator.CrossFade(_stateHashName, NormalizedTransitionDuration, _layerID, NormalizedTimeOffset, NormalizedTransitionTime);
					break;
			}
		}
	}
}

