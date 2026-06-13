using MoreMountains.Tools;
using UnityEngine;
#if MM_UGUI2
using TMPro;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you control the character spacing of a target TMP over time
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("这个反馈可随时间控制目标 TMP 的字符间距。")]
	#if MM_UGUI2
	[FeedbackPath("TextMesh Pro/TMP Character Spacing")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.TextMeshPro")]
	public class MMF_TMPCharacterSpacing : MMF_FeedbackBase
	{
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TMPColor; } }
		public override string RequiresSetupText { get { return "此反馈需要指定 TargetTMPText 才能正常工作。你可以在下方进行设置。"; } }
		#endif
        
		#if UNITY_EDITOR && MM_UGUI2
		public override bool EvaluateRequiresSetup() { return (TargetTMPText == null); }
		public override string RequiredTargetText { get { return TargetTMPText != null ? TargetTMPText.name : "";  } }
		#endif
        
		#if MM_UGUI2
		public override bool HasAutomatedTargetAcquisition => true;
		public override bool CanForceInitialValue => true;
		protected override void AutomateTargetAcquisition() => TargetTMPText = FindAutomatedTarget<TMP_Text>();
		
		[MMFInspectorGroup("Target", true, 12, true)]
		/// 要控制的 TMP_Text 组件。
		[Tooltip("要控制的文本组件。")]
		public TMP_Text TargetTMPText;
		#endif

		[MMFInspectorGroup("Character Spacing", true, 16)]
		/// 用于执行补间的曲线。
		[Tooltip("用于执行补间的曲线。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime, (int)Modes.ToDestination)]
		public MMTweenType CharacterSpacingCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));
		/// 将曲线 0 端重新映射到的值。
		[Tooltip("将曲线 0 端重新映射到的值。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float RemapZero = 0f;
		/// 将曲线 1 端重新映射到的值。
		[Tooltip("将曲线 1 端重新映射到的值。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float RemapOne = 1f;
		/// Instant 模式下要立即设置的值。
		[Tooltip("Instant 模式下要立即设置的值。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.Instant)]
		public float InstantSpacing;
		/// ToDestination 模式下要插值到的目标值。
		[Tooltip("ToDestination 模式下要插值到的目标值。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public float DestinationSpacing;
        
		protected override void FillTargets()
		{
			#if MM_UGUI2
			if (TargetTMPText == null)
			{
				return;
			}

			MMF_FeedbackBaseTarget target = new MMF_FeedbackBaseTarget();
			MMPropertyReceiver receiver = new MMPropertyReceiver();
			receiver.TargetObject = TargetTMPText.gameObject;
			receiver.TargetComponent = TargetTMPText;
			receiver.TargetPropertyName = "characterSpacing";
			receiver.RelativeValue = RelativeValues;
			target.Target = receiver;
			target.LevelCurve = CharacterSpacingCurve;
			target.RemapLevelZero = RemapZero;
			target.RemapLevelOne = RemapOne;
			target.InstantLevel = InstantSpacing;
			target.ToDestinationLevel = DestinationSpacing;

			_targets.Add(target);
			#endif
		}

	}
}
