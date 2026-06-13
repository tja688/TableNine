using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will trigger a MMBlink object, letting you blink something
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可在一个 MMBlink 对象上触发闪烁效果。")]
	[System.Serializable]
	[FeedbackPath("Renderer/MMBlink")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	public class MMF_Blink : MMF_Feedback
	{
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get => MMFeedbacksInspectorColors.RendererColor; }
		public override bool HasCustomInspectors { get { return true; } }
		public override bool EvaluateRequiresSetup() => (TargetBlink == null);
		public override string RequiredTargetText => TargetBlink != null ? TargetBlink.name : "";
		public override string RequiresSetupText => "This feedback requires that a TargetBlink be set to be able to work properly. You can set one below.";
		#endif
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(Duration); } set { Duration = value; } }
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetBlink = FindAutomatedTarget<MMBlink>();
        
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;

		/// the possible modes for this feedback, that correspond to MMBlink's public methods
		public enum BlinkModes { Toggle, Start, Stop }
        
		[MMFInspectorGroup("Blink", true, 61, true)]
		/// the target object to blink
		[Tooltip("要执行闪烁的目标对象")]
		public MMBlink TargetBlink;
		/// an optional list of extra target objects to blink
		[Tooltip("可选的额外闪烁目标对象列表")]
		public List<MMBlink> ExtraTargetBlinks;
		/// the selected mode for this feedback
		[Tooltip("该反馈当前使用的模式")]
		public BlinkModes BlinkMode = BlinkModes.Toggle;
		/// the duration of the blink. You can set it manually, or you can press the GrabDurationFromBlink button to automatically compute it. For performance reasons, this isn't updated unless you press the button, make sure you do so if you change the blink's duration.
		[Tooltip("闪烁持续时间。你可以手动设置，也可以点击 GrabDurationFromBlink 按钮自动计算。出于性能考虑，这个值不会自动实时刷新；如果你修改了 Blink 的持续时间，请记得再次点击该按钮重新计算。")]
		public float Duration;
		public MMF_Button GrabDurationFromBlinkButton;

		/// <summary>
		/// Initializes our duration button
		/// </summary>
		public override void InitializeCustomAttributes()
		{
			GrabDurationFromBlinkButton = new MMF_Button("Grab Duration From Blink Component", GrabDurationFromBlink);
		}

		/// <summary>
		/// On Custom play, we trigger our MMBlink object
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetBlink == null))
			{
				return;
			}
			HandleBlink(TargetBlink);
			foreach (MMBlink blink in ExtraTargetBlinks)
			{
				HandleBlink(blink);
			}
		}

		/// <summary>
		/// Toggles, starts or stops blink on the target
		/// </summary>
		/// <param name="target"></param>
		protected virtual void HandleBlink(MMBlink target)
		{
			target.TimescaleMode = ComputedTimescaleMode;
			switch (BlinkMode)
			{
				case BlinkModes.Toggle:
					target.ToggleBlinking();
					break;
				case BlinkModes.Start:
					target.StartBlinking();
					break;
				case BlinkModes.Stop:
					target.StopBlinking();
					break;
			}
		}
		
		/// <summary>
		/// On restore, we restore our initial state
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			
			TargetBlink.StopBlinking();
			foreach (MMBlink blink in ExtraTargetBlinks)
			{
				blink.StopBlinking();
			}
		}
		
		/// <summary>
		/// Grabs and stores the duration from our target blink component if one is set
		/// </summary>
		public virtual void GrabDurationFromBlink()
		{
			if (TargetBlink != null)
			{
				Duration = TargetBlink.Duration;	
			}
		}
	}
}