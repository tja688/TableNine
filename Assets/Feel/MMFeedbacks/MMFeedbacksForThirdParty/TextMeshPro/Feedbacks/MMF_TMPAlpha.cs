using MoreMountains.Tools;
using UnityEngine;
using System.Collections;
#if MM_UGUI2
using TMPro;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you control the alpha of a target TMP over time
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("这个反馈可随时间控制目标 TMP 的透明度。")]
	#if MM_UGUI2
	[FeedbackPath("TextMesh Pro/TMP Alpha")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.TextMeshPro")]
	public class MMF_TMPAlpha : MMF_Feedback
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
        
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		public enum AlphaModes { Instant, Interpolate, ToDestination }

		/// the duration of this feedback is the duration of the color transition, or 0 if instant
		public override float FeedbackDuration { get { return (AlphaMode == AlphaModes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { Duration = value; } }

		#if MM_UGUI2
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetTMPText = FindAutomatedTarget<TMP_Text>();
		public override bool HasCustomInspectors => true;
		
		[MMFInspectorGroup("Target", true, 12, true)]
		/// the要控制的 TMP_Text 组件。
		[Tooltip("要控制的文本组件。")]
		public TMP_Text TargetTMPText;
		#endif

		[MMFInspectorGroup("Alpha", true, 16)]
		/// 所选颜色模式：
		/// None：不执行任何操作；
		/// Gradient：沿渐变从左到右随时间评估颜色；
		/// Interpolate：从当前颜色插值到目标颜色。
		[Tooltip("所选颜色模式： 即时：透明度会立即切换到目标值； 曲线：透明度会沿曲线进行插值变化； 插值：从当前颜色插值到目标颜色。")]
		public AlphaModes AlphaMode = AlphaModes.Interpolate;
		/// 文本颜色/数值随时间变化时的持续时间。
		[Tooltip("文本颜色/数值随时间变化时的持续时间。")]
		[MMFEnumCondition("AlphaMode", (int)AlphaModes.Interpolate, (int)AlphaModes.ToDestination)]
		public float Duration = 0.2f;
		/// Instant 模式下要应用的 Alpha。
		[Tooltip("即时模式下要应用的透明度。")]
		[MMFEnumCondition("AlphaMode", (int)AlphaModes.Instant)]
		public float InstantAlpha = 1f;

		/// 插值到目标 Alpha 时使用的曲线。
		[Tooltip("插值到目标 Alpha 时使用的曲线。")]
		public MMTweenType Curve = new MMTweenType(MMTween.MMTweenCurve.EaseInCubic, "", "AlphaMode", (int)AlphaModes.Interpolate, (int)AlphaModes.ToDestination);
		/// 将曲线 0 端重新映射到的值。
		[Tooltip("将曲线 0 端重新映射到的值。")]
		[MMFEnumCondition("AlphaMode", (int)AlphaModes.Interpolate)]
		public float CurveRemapZero = 0f;
		/// 将曲线 1 端重新映射到的值。
		[Tooltip("将曲线 1 端重新映射到的值。")]
		[MMFEnumCondition("AlphaMode", (int)AlphaModes.Interpolate)]
		public float CurveRemapOne = 1f;
		/// ToDestination 模式下要逼近的目标 Alpha。
		[Tooltip("目的地模式下要逼近的目标漏洞。")]
		[MMFEnumCondition("AlphaMode", (int)AlphaModes.ToDestination)]
		public float DestinationAlpha = 1f;

		/// 若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。
		[Tooltip("若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。")] 
		public bool AllowAdditivePlays = false;

		protected float _initialAlpha;
		protected Coroutine _coroutine;

		/// <summary>
		/// On init we store our initial alpha
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
			_initialAlpha = TargetTMPText.alpha;
			#endif
		}

		/// <summary>
		/// On Play we change our text's alpha
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

			switch (AlphaMode)
			{
				case AlphaModes.Instant:
					TargetTMPText.alpha = NormalPlayDirection ? InstantAlpha : _initialAlpha;
					break;
				case AlphaModes.Interpolate:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(ChangeAlpha());
					break;
				case AlphaModes.ToDestination:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					_initialAlpha = TargetTMPText.alpha;
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(ChangeAlpha());
					break;
			}
			#endif
		}

		/// <summary>
		/// Changes the color of the text over time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ChangeAlpha()
		{
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				SetAlpha(remappedTime);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetAlpha(FinalNormalizedTime);
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
		/// Applies the alpha change
		/// </summary>
		/// <param name="time"></param>
		protected virtual void SetAlpha(float time)
		{
			#if MM_UGUI2
			float newAlpha = 0f;
			if (AlphaMode == AlphaModes.Interpolate)
			{
				newAlpha = MMTween.Tween(time, 0f, 1f, CurveRemapZero, CurveRemapOne, Curve);    
			}
			else if (AlphaMode == AlphaModes.ToDestination)
			{
				newAlpha = MMTween.Tween(time, 0f, 1f, _initialAlpha, DestinationAlpha, Curve);
			}
			TargetTMPText.alpha = newAlpha;
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
			TargetTMPText.alpha = _initialAlpha;
			#endif
		}
		
		/// <summary>
		/// On Validate, we init our curves conditions if needed
		/// </summary>
		public override void OnValidate()
		{
			base.OnValidate();
			if (string.IsNullOrEmpty(Curve.EnumConditionPropertyName))
			{
				Curve.EnumConditionPropertyName = "AlphaMode";
				Curve.EnumConditions = new bool[32];
				Curve.EnumConditions[(int)AlphaModes.Interpolate] = true;
				Curve.EnumConditions[(int)AlphaModes.ToDestination] = true;
			}
		}
	}
}
