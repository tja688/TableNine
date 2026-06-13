using System.Collections;
using UnityEngine;
#if MM_UI
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you change the color of a target Graphic over time.
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你随时间修改目标 Graphic 的颜色。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("UI/Graphic")]
	public class MMF_Graphic : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetGraphic == null); }
		public override string RequiredTargetText { get { return TargetGraphic != null ? TargetGraphic.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈需要先指定 TargetGraphic 才能正常工作，可在下方设置。"; } }
		#endif

		/// the duration of this feedback is the duration of the Graphic, or 0 if instant
		public override float FeedbackDuration { get { return (Mode == Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { Duration = value; } }
		public override bool HasChannel => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetGraphic = FindAutomatedTarget<Graphic>();

		/// the possible modes for this feedback
		public enum Modes { OverTime, Instant }

		[MMFInspectorGroup("Graphic", true, 54, true)]
		/// the Graphic to affect when playing the feedback
		[Tooltip("播放该反馈时要作用的 Graphic")]
		public Graphic TargetGraphic;
		/// whether the feedback should affect the Graphic instantly or over a period of time
		[Tooltip("作用模式：`Instant` 会立即应用；`OverTime` 会在 `Duration` 时长内渐变。")]
		public Modes Mode = Modes.OverTime;
		/// how long the Graphic should change over time
		[Tooltip("在 `OverTime` 模式下，颜色变化持续时间（秒）。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public float Duration = 0.2f;
		/// whether or not that Graphic should be turned off on start
		[Tooltip("初始化时是否将目标 Graphic 关闭。")]
		public bool StartsOff = false;
		/// if this is true, the target will be disabled when this feedbacks is stopped
		[Tooltip("若开启，调用 Stop 时会关闭目标 Graphic。")]
		public bool DisableOnStop = false;
        
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("若开启此项，即使该反馈仍在执行中，再次调用也会立即触发；若关闭此项，在当前播放结束前将阻止新的 Play 调用")] 
		public bool AllowAdditivePlays = false;
		/// whether or not to modify the color of the Graphic
		[Tooltip("是否修改目标图形的颜色。")]
		public bool ModifyColor = true;
		/// the colors to apply to the Graphic over time
		[Tooltip("在 `OverTime` 模式下用于驱动颜色变化的渐变。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public Gradient ColorOverTime;
		/// the color to move to in instant mode
		[Tooltip("在 Instant 模式下要切换到的颜色")]
		[MMFEnumCondition("Mode", (int)Modes.Instant)]
		public Color InstantColor;

		protected Coroutine _coroutine;
		protected Color _initialColor;
		protected Color _initialInstantColor;

		/// <summary>
		/// On init we turn the Graphic off if needed
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			if (Active)
			{
				if (StartsOff)
				{
					Turn(false);
				}

				if (TargetGraphic == null)
				{
					Debug.LogWarning("[Graphic Feedback] The graphic feedback on "+Owner.name+" doesn't have a Target Graphic, it won't work. You need to specify a graphic in its inspector.");
				}
				else
				{
					_initialInstantColor = TargetGraphic.color;	
				}
			}
		}

		/// <summary>
		/// On Play we turn our Graphic on and start an over time coroutine if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetGraphic == null))
			{
				return;
			}

			_initialColor = TargetGraphic.color;
			Turn(true);
			switch (Mode)
			{
				case Modes.Instant:
					if (ModifyColor)
					{
						TargetGraphic.color = NormalPlayDirection ? InstantColor : _initialInstantColor;
					}
					break;
				case Modes.OverTime:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(GraphicSequence());
					break;
			}
		}

		/// <summary>
		/// This coroutine will modify the values on the Graphic
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator GraphicSequence()
		{
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;

			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				SetGraphicValues(remappedTime);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetGraphicValues(FinalNormalizedTime);
			if (StartsOff)
			{
				Turn(false);
			}
			IsPlaying = false;
			if (_coroutine != null)
			{
				Owner.StopCoroutine(_coroutine);	
			}
			_coroutine = null;
			yield return null;
		}

		/// <summary>
		/// Sets the various values on the Graphic on a specified time (between 0 and 1)
		/// </summary>
		/// <param name="time"></param>
		protected virtual void SetGraphicValues(float time)
		{
			if (ModifyColor)
			{
				TargetGraphic.color = ColorOverTime.Evaluate(time);
			}
		}

		/// <summary>
		/// Turns the Graphic off on stop
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			IsPlaying = false;
			base.CustomStopFeedback(position, feedbacksIntensity);
			if (Active && DisableOnStop)
			{
				Turn(false);    
			}

			if (_coroutine != null)
			{
				Owner.StopCoroutine(_coroutine);
			}

			_coroutine = null;
		}

		/// <summary>
		/// Turns the Graphic on or off
		/// </summary>
		/// <param name="status"></param>
		protected virtual void Turn(bool status)
		{
			TargetGraphic.gameObject.SetActive(status);
			TargetGraphic.enabled = status;
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
			TargetGraphic.color = _initialColor;
		}
	}
}
#endif
