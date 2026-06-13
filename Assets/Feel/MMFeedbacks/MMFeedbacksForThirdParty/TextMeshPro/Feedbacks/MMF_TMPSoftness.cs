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
	/// This feedback lets you tweak the softness of a TMP text over time
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("这个反馈可随时间微调 TMP 文本的柔化程度。")]
	#if MM_UGUI2
	[FeedbackPath("TextMesh Pro/TMP Softness")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.TextMeshPro")]
	public class MMF_TMPSoftness : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TMPColor; } }
		public override string RequiresSetupText { get { return "此反馈需要指定 TargetTMPText 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		#if UNITY_EDITOR && MM_UGUI2
		public override bool EvaluateRequiresSetup() { return (TargetTMPText == null); }
		public override string RequiredTargetText { get { return TargetTMPText != null ? TargetTMPText.name : "";  } }
		#endif
		public override bool HasCustomInspectors => true;
        
		/// the duration of this feedback is the duration of the transition, or 0 if instant
		public override float FeedbackDuration { get { return (Mode == MMFeedbackBase.Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { Duration = value; } }

		#if MM_UGUI2
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetTMPText = FindAutomatedTarget<TMP_Text>();

		[MMFInspectorGroup("Target", true, 12, true)]
		/// 要控制的 TMP_Text 组件。
		[Tooltip("要控制的文本组件。")]
		public TMP_Text TargetTMPText;
		#endif

		[MMFInspectorGroup("Softness", true, 13)]
		/// 是否按相对值应用。若启用，会在当前值基础上叠加；若禁用，则直接使用绝对值。
		[Tooltip("是否按相对值应用。若启用，会在当前值基础上叠加；若禁用，则直接使用绝对值。")]
		public bool RelativeValues = true;
		/// 所选模式。
		[Tooltip("所选模式。")]
		public MMFeedbackBase.Modes Mode = MMFeedbackBase.Modes.OverTime;
		/// 反馈持续时间（秒）。
		[Tooltip("反馈持续时间（秒）。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float Duration = 0.5f;
		/// 用于执行补间的曲线。
		[Tooltip("用于执行补间的曲线。")]
		public MMTweenType SoftnessCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0f), new Keyframe(0.3f, 1f), new Keyframe(1, 0f)), "", "Mode", (int)MMFeedbackBase.Modes.OverTime);
		/// 将曲线 0 端重新映射到的值。
		[Tooltip("将曲线 0 端重新映射到的值。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float RemapZero = 0f;
		/// 将曲线 1 端重新映射到的值。
		[Tooltip("将曲线 1 端重新映射到的值。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float RemapOne = 1f;
		/// Instant 模式下要移动到的值。
		[Tooltip("即时模式下要移动到的值。")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.Instant)]
		public float InstantSoftness;
		/// 若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。
		[Tooltip("若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。")] 
		public bool AllowAdditivePlays = false;

		protected float _initialSoftness;
		protected Coroutine _coroutine;

		/// <summary>
		/// On init we grab our initial softness
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			if (!Active)
			{
				return;
			}
			#if MM_UGUI2
			if (TargetTMPText == null)
			{
				Debug.LogWarning("[TMP Softness Feedback] The TMP Softness feedback on "+Owner.name+" doesn't have a TargetTMPText, it won't work. You need to specify one in its inspector.");
				return;
			}
			_initialSoftness = TargetTMPText.fontMaterial.GetFloat(ShaderUtilities.ID_OutlineSoftness);
			#endif
		}

		/// <summary>
		/// On Play we animate our softness
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			#if MM_UGUI2
			if (TargetTMPText == null)
			{
				return;
			}

			if (Active && FeedbackTypeAuthorized)
			{
				switch (Mode)
				{
					case MMFeedbackBase.Modes.Instant:
						float newSoftness = NormalPlayDirection ? InstantSoftness : _initialSoftness;
						TargetTMPText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, InstantSoftness);
						TargetTMPText.UpdateMeshPadding();
						break;
					case MMFeedbackBase.Modes.OverTime:
						if (!AllowAdditivePlays && (_coroutine != null))
						{
							return;
						}
						if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
						_coroutine = Owner.StartCoroutine(ApplyValueOverTime());
						break;
				}
			}
			#endif
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

		protected virtual IEnumerator ApplyValueOverTime()
		{
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				SetValue(remappedTime);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetValue(FinalNormalizedTime);
			_coroutine = null;
			IsPlaying = false;
			yield return null;
		}

		protected virtual void SetValue(float time)
		{
			#if MM_UGUI2
			float intensity = MMTween.Tween(time, 0f, 1f, RemapZero, RemapOne, SoftnessCurve);
			float newValue = intensity;
			if (RelativeValues)
			{
				newValue += _initialSoftness;
			}
			TargetTMPText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, newValue);
			TargetTMPText.UpdateMeshPadding();
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
			TargetTMPText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, _initialSoftness);
			TargetTMPText.UpdateMeshPadding();
			#endif
		}
		
		/// <summary>
		/// On Validate, we init our curves conditions if needed
		/// </summary>
		public override void OnValidate()
		{
			base.OnValidate();
			if (string.IsNullOrEmpty(SoftnessCurve.EnumConditionPropertyName))
			{
				SoftnessCurve.EnumConditionPropertyName = "Mode";
				SoftnessCurve.EnumConditions = new bool[32];
				SoftnessCurve.EnumConditions[(int)MMFeedbackBase.Modes.OverTime] = true;
			}
		}
	}
}
