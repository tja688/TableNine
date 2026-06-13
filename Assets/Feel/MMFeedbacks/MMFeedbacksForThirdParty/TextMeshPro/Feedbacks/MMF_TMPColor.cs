using UnityEngine;
using System.Collections;
#if MM_UGUI2
using TMPro;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you control the color of a target TMP over time
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("这个反馈可随时间控制目标 TMP 的颜色。")]
	#if MM_UGUI2
	[FeedbackPath("TextMesh Pro/TMP Color")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.TextMeshPro")]
	public class MMF_TMPColor : MMF_Feedback
	{
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TMPColor; } }
		public override string RequiresSetupText { get { return "此反馈需要指定 TargetTMPText 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		#if MM_UGUI2
		public override bool EvaluateRequiresSetup() { return (TargetTMPText == null); }
		public override string RequiredTargetText { get { return TargetTMPText != null ? TargetTMPText.name : "";  } }
		#endif
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		public enum ColorModes { Instant, Gradient, Interpolate }

		/// the duration of this feedback is the duration of the color transition, or 0 if instant
		public override float FeedbackDuration { get { return (ColorMode == ColorModes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { Duration = value; } }

		#if MM_UGUI2
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetTMPText = FindAutomatedTarget<TMP_Text>();

		[MMFInspectorGroup("Target", true, 12, true)]
		/// the要控制的 TMP_Text 组件。
		[Tooltip("要控制的文本组件。")]
		public TMP_Text TargetTMPText;
		#endif

		[MMFInspectorGroup("Color", true, 16)]
		/// 所选颜色模式：
		/// None：不执行任何操作；
		/// Gradient：沿渐变从左到右随时间评估颜色；
		/// Interpolate：从当前颜色插值到目标颜色。
		[Tooltip("所选颜色模式：" +
		         "None：不执行任何操作；" +
		         "Gradient：沿渐变从左到右随时间评估颜色；" +
		         "Interpolate：从当前颜色插值到目标颜色。")]
		public ColorModes ColorMode = ColorModes.Interpolate;
		/// 文本颜色/数值随时间变化时的持续时间。
		[Tooltip("文本颜色/数值随时间变化时的持续时间。")]
		[MMFEnumCondition("ColorMode", (int)ColorModes.Interpolate, (int)ColorModes.Gradient)]
		public float Duration = 0.2f;
		/// 要应用的颜色。
		[Tooltip("要应用的颜色。")]
		[MMFEnumCondition("ColorMode", (int)ColorModes.Instant)]
		public Color InstantColor = Color.yellow;
		/// 用于随时间驱动颜色变化的渐变。
		[Tooltip("用于随时间驱动颜色变化的渐变。")]
		[MMFEnumCondition("ColorMode", (int)ColorModes.Gradient)]
		[GradientUsage(true)]
		public Gradient ColorGradient;
		/// Interpolate 模式下的目标颜色。
		[Tooltip("插入模式下的目标颜色。")]
		[MMFEnumCondition("ColorMode", (int)ColorModes.Interpolate)]
		public Color DestinationColor = Color.yellow;
		/// 插值到目标颜色时使用的曲线。
		[Tooltip("插值到目标颜色时使用的曲线。")]
		[MMFEnumCondition("ColorMode", (int)ColorModes.Interpolate)]
		public AnimationCurve ColorCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// 若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。
		[Tooltip("若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。")] 
		public bool AllowAdditivePlays = false;

		protected Color _initialColor;
		protected Coroutine _coroutine;

		/// <summary>
		/// On init we store our initial color
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			#if MM_UGUI2
			if (TargetTMPText == null)
			{
				return;
			}
			_initialColor = TargetTMPText.color;
			#endif
		}

		/// <summary>
		/// On Play we change our text's color
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
            
			#if MM_UGUI2
            
			if (TargetTMPText == null)
			{
				return;
			}

			switch (ColorMode)
			{
				case ColorModes.Instant:
					TargetTMPText.color = NormalPlayDirection ? InstantColor : _initialColor;;
					break;
				case ColorModes.Gradient:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(ChangeColor());
					break;
				case ColorModes.Interpolate:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(ChangeColor());
					break;
			}

			#endif
		}

		/// <summary>
		/// Changes the color of the text over time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ChangeColor()
		{
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				SetColor(remappedTime);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetColor(FinalNormalizedTime);
			_coroutine = null;
			IsPlaying = false;
			yield break;
		}

		/// <summary>
		/// Stops the animation if needed
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
			IsPlaying = false;
			if (_coroutine != null)
			{
				Owner.StopCoroutine(_coroutine);
				_coroutine = null;
			}
		}

		/// <summary>
		/// Applies the color change
		/// </summary>
		/// <param name="time"></param>
		protected virtual void SetColor(float time)
		{
			#if MM_UGUI2
			if (ColorMode == ColorModes.Gradient)
			{
				TargetTMPText.color = ColorGradient.Evaluate(time);
			}
			else if (ColorMode == ColorModes.Interpolate)
			{
				float factor = ColorCurve.Evaluate(time);
				TargetTMPText.color = Color.LerpUnclamped(_initialColor, DestinationColor, factor);
			}
			#endif
		}
		
		/// <summary>
		/// On restore, we put our object back at its initial position
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			#if MM_UGUI2
			TargetTMPText.color = _initialColor;
			#endif
		}
	}
}
