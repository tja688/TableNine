using MoreMountains.FeedbacksForThirdParty;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// 用于驱动 float 弹簧的反馈。
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("用于驱动 float 弹簧的反馈。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Springs/Spring Float")]
	public class MMF_SpringFloat : MMF_Feedback
	{
		/// a static bool used to disable all 反馈s of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this 反馈
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.SpringColor; } }
		public override string RequiredTargetText => RequiredChannelText;
		public override bool HasCustomInspectors => true; 
		#endif

		/// the duration of this 反馈 is the duration of the zoom
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(DeclaredDuration); } set { DeclaredDuration = value;  } }
		public override bool HasChannel => true;
		public override bool CanForceInitialValue => true;

		[MMFInspectorGroup("Spring", true, 72)] 
		
		/// 此反馈要控制的弹簧。若这里指定了具体弹簧，则只会作用于它；若留空，则会向所有通道匹配的弹簧广播事件。
		[Tooltip("此反馈要控制的弹簧。若这里指定了具体弹簧，则只会作用于它；若留空，则会向所有通道匹配的弹簧广播事件。")]
		public MMSpringComponentBase TargetSpring;
		
		/// 供 `MMF_Player` 参考的持续时间。它不会直接影响你的 Particle System，只是用于告诉 `MMF_Player` 此反馈应被视为持续多久。通常建议把它设置为与实际效果时长一致，这样在 `Holding Pause` 等场景下行为会更准确。
		[Tooltip("提供`MMF_Player`参考的持续时间。它不会直接影响你的粒子系统，只是为了告诉`MMF_Player`这个反馈应该被认为是持续多久。行为通常建议把它设置为与实际效果时间长一致，这样在`Holding Pause`等场景下会更准确。")]
		public float DeclaredDuration = 0f;
		
		/// 要对该弹簧执行的命令。
		[Tooltip("要对该弹簧执行的命令。")]
		public SpringCommands Command = SpringCommands.Bump;
		[MMEnumCondition("Command", (int)SpringCommands.MoveTo, (int)SpringCommands.MoveToAdditive, (int)SpringCommands.MoveToSubtractive, (int)SpringCommands.MoveToInstant)]
		/// 此弹簧要移动到的新目标值。
		[Tooltip("此弹簧要移动到的新目标值。")]
		public float MoveToValue = 2f;
		/// 要额外加到弹簧当前速度上的扰动值，用来制造一次弹跳效果。
		[Tooltip("要额外加到弹簧当前速度上的扰动值，用来制造一次弹跳效果。")]
		[MMEnumCondition("Command", (int)SpringCommands.Bump)]
		public float BumpAmount = 75f;
		/// 调用 `MoveToRandom` 时，用于随机选择目标值的最小 / 最大范围。
		[Tooltip("调用 `MoveToRandom` 时，用于随机选择目标值的最小 / 最大范围。")]
		[MMEnumCondition("Command", (int)SpringCommands.MoveToRandom)]
		public Vector2 MoveToRandomValue = new Vector2(-2f, 2f);
		/// 调用 `BumpRandom` 时，用于随机选择并加到 弹簧 速度上的最小 / 最大范围。
		[Tooltip("调用 `BumpRandom` 时，用于随机选择并加到 弹簧 速度上的最小 / 最大范围。")]
		[MMEnumCondition("Command", (int)SpringCommands.BumpRandom)]
		public Vector2 BumpAmountRandomValue = new Vector2(-50f, 50f);
		
		[Header("Overrides")]
		/// 是否用下方指定的 `NewDamping` 覆盖目标弹簧当前的 `Damping` 值。若启用，目标弹簧的原阻尼设置会被覆盖。
		[Tooltip("是否用下方指定的 `NewDamping` 覆盖目标弹簧当前的 `Damping` 值。若启用，目标弹簧的原阻尼设置会被覆盖。")]
		public bool OverrideDamping = false;
		/// 当 `OverrideDamping` 为 true 时，要应用到目标弹簧的新阻尼值。
		[Tooltip("当 `OverrideDamping` 为 true 时，要应用到目标弹簧的新阻尼值。")]
		[MMFCondition("OverrideDamping", true)]
		public float NewDamping = 0.8f;
		/// 是否用下方指定的 `NewFrequency` 覆盖目标弹簧当前的 `Frequency` 值。若启用，目标弹簧的原频率设置会被覆盖。
		[Tooltip("是否用下方指定的 `NewFrequency` 覆盖目标弹簧当前的 `Frequency` 值。若启用，目标弹簧的原频率设置会被覆盖。")]
		public bool OverrideFrequency = false;
		/// 当 `OverrideFrequency` 为 true 时，要应用到目标弹簧的新频率值。
		[Tooltip("当 `OverrideFrequency` 为 true 时，要应用到目标弹簧的新频率值。")]
		[MMFCondition("OverrideFrequency", true)]
		public float NewFrequency = 5f;

		protected MMChannelData _eventChannelData;

		/// <summary>
		/// On Play, triggers a spring event with the selected settings
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			_eventChannelData = (TargetSpring == null) ? ChannelData : null;
			MMSpringFloatEvent.Trigger(Command, TargetSpring, _eventChannelData, MoveToValue, BumpAmount, MoveToRandomValue, BumpAmountRandomValue, OverrideDamping, NewDamping, OverrideFrequency, NewFrequency);
		}

		/// <summary>
		/// On stop, triggers a spring stop event
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);
			_eventChannelData = (TargetSpring == null) ? ChannelData : null;
			MMSpringFloatEvent.Trigger(SpringCommands.Stop, TargetSpring, _eventChannelData);
		}
		
		/// <summary>
		/// On restore, triggers a spring RestoreInitialValue event
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			_eventChannelData = (TargetSpring == null) ? ChannelData : null;
			MMSpringFloatEvent.Trigger(SpringCommands.RestoreInitialValue, TargetSpring, _eventChannelData);
		}
	}
}