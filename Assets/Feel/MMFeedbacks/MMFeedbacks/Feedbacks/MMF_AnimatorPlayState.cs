using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// A feedback used to play the specified state on the target Animator, either in normalized or fixed time.
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可在目标 Animator 上播放指定状态。你可以使用标准化时间（0-1）或固定时间（秒）来指定播放偏移。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Animation/Animator Play State")]
	public class MMF_AnimatorPlayState : MMF_Feedback 
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
		
		public enum Modes { NormalizedTime, FixedTime } 

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
        
		[MMFInspectorGroup("State", true, 16)]
		/// The name of the state to play on the target animator
		[Tooltip("要在目标 Animator 上播放的状态名称")]
		public string StateName;
		/// Whether to play the state at a normalized time (between 0 and 1) or a fixed time (in seconds)
		[Tooltip("状态播放时间模式。NormalizedTime 使用下方 NormalizedTime（0-1）；FixedTime 使用下方 FixedTime（秒）。")]
		public Modes Mode = Modes.NormalizedTime;
		/// The time offset between zero and one at which to play the specified state
		[Tooltip("播放指定状态时使用的标准化时间偏移（0 到 1）")]
		[MMFEnumCondition("Mode", (int)Modes.NormalizedTime)]
		public float NormalizedTime = 0f;
		/// The time offset (in seconds) at which to play the specified state
		[Tooltip("播放指定状态时使用的时间偏移，单位为秒")]
		[MMFEnumCondition("Mode", (int)Modes.FixedTime)]
		public float FixedTime = 0f;
		/// The layer index. If layer is -1, it plays the first state with the given state name or hash.
		[Tooltip("目标层索引。若层为 -1，则会播放第一个名称或 hash 与给定值匹配的状态。")]
		public int LayerIndex = -1;
		/// the name of the Animator layer you want the state to play on. This is optional. If left empty, the layer ID above will be used, if not empty, the Layer id specified above will be ignored.
		[Tooltip("要播放该状态的 Animator 层名称。此项可选；若留空，将使用上方的层 ID；若填写此项，则上方指定的层 ID 将被忽略。")]
		public string LayerName = "";

		[MMFInspectorGroup("Layer Weights", true, 22)]
		/// whether or not to set layer weights on the specified layer when playing this feedback
		[Tooltip("播放此反馈时是否同时设置 Animator 层权重。关闭后，下方层权重相关字段将不生效。")]
		public bool SetLayerWeight = false;
		/// the index of the layer to target when changing layer weights
		[Tooltip("在 SetLayerWeight 开启时，用于修改层权重的目标层索引")]
		[MMFCondition("SetLayerWeight", true)]
		public int TargetLayerIndex = 1;
		/// the new weight to set on the target animator layer
		[Tooltip("在 SetLayerWeight 开启时，要设置到目标 Animator 层的新权重值")]
		[MMFCondition("SetLayerWeight", true)]
		public float NewWeight = 0.5f;

		protected int _targetParameter;
		protected int _layerID;

		/// <summary>
		/// Custom Init
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			_targetParameter = Animator.StringToHash(StateName);
			_layerID = TargetLayerIndex;
			if ((LayerName != "") && (BoundAnimator != null))
			{
				_layerID = BoundAnimator.GetLayerIndex(LayerName);
			}
		}

		/// <summary>
		/// On Play, checks if an animator is bound and plays the specified state
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
				Debug.LogWarning("[Animator Play State Feedback] The animator play state feedback on "+Owner.name+" doesn't have a BoundAnimator, it won't work. You need to specify one in its inspector.");
				return;
			}

			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);

			PlayState(BoundAnimator, intensityMultiplier);
			foreach (Animator animator in ExtraBoundAnimators)
			{
				PlayState(animator, intensityMultiplier);
			}
		}

		/// <summary>
		/// Plays the specified state on the target animator
		/// </summary>
		/// <param name="targetAnimator"></param>
		/// <param name="intensityMultiplier"></param>
		protected virtual void PlayState(Animator targetAnimator, float intensityMultiplier)
		{
			if (SetLayerWeight)
			{
				targetAnimator.SetLayerWeight(_layerID, NewWeight);
			}
			
			if (Mode == Modes.NormalizedTime)
			{
				targetAnimator.Play(_targetParameter, LayerIndex, NormalizedTime);
			}
			else
			{
				targetAnimator.PlayInFixedTime(_targetParameter, LayerIndex, FixedTime);
			}
		}
        
		/// <summary>
		/// On stop, we do nothing
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
		}
	}
}

