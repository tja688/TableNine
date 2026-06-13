#if MM_UI
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you control the font size of a target Text over time
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可控制目标 Text 的字体大小：Instant 模式会立即设置，OverTime 模式会按曲线变化，ToDestination 模式会在时长内插值到目标字号。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[System.Serializable]
	[FeedbackPath("UI/Text Font Size")]
	public class MMF_TextFontSize : MMF_FeedbackBase
	{
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TMPColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetText == null); }
		public override string RequiredTargetText { get { return TargetText != null ? TargetText.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先指定 TargetText 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		public override bool CanForceInitialValue => true;
		protected override void AutomateTargetAcquisition() => TargetText = FindAutomatedTarget<Text>();

		[MMFInspectorGroup("Target", true, 58, true)]
		/// the TMP_Text component to control
		[Tooltip("要控制字体大小的 Text 组件。")]
		public Text TargetText;

		[MMFInspectorGroup("Font Size", true, 59)]
		/// the curve to tween on
		[Tooltip("用于驱动字体大小变化的曲线。OverTime 与 ToDestination 模式会使用此曲线，Instant 模式会忽略。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime, (int)Modes.ToDestination)]
		public MMTweenType FontSizeCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));
		/// the value to remap the curve's 0 to
		[Tooltip("仅在 OverTime 模式下生效：将曲线 0 端重映射到的字体大小值。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float RemapZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("仅在 OverTime 模式下生效：将曲线 1 端重映射到的字体大小值。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float RemapOne = 1f;
		/// the value to move to in instant mode
		[Tooltip("仅在 Instant 模式下生效：播放时会立即把字号设置为该值。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.Instant)]
		public float InstantFontSize;
		/// the value to move to in destination mode
		[Tooltip("仅在 ToDestination 模式下生效：字号会按曲线插值到该目标值。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public float DestinationFontSize;
        
		protected override void FillTargets()
		{
			if (TargetText == null)
			{
				return;
			}

			MMF_FeedbackBaseTarget target = new MMF_FeedbackBaseTarget();
			MMPropertyReceiver receiver = new MMPropertyReceiver();
			receiver.TargetObject = TargetText.gameObject;
			receiver.TargetComponent = TargetText;
			receiver.TargetPropertyName = "fontSize";
			receiver.RelativeValue = RelativeValues;
			target.Target = receiver;
			target.LevelCurve = FontSizeCurve;
			target.RemapLevelZero = RemapZero;
			target.RemapLevelOne = RemapOne;
			target.InstantLevel = InstantFontSize;
			target.ToDestinationLevel = DestinationFontSize;

			_targets.Add(target);
		}
	}
}
#endif
