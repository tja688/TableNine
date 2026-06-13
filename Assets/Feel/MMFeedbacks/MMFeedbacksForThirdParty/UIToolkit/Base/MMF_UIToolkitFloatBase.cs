using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// A base feedback to set a float on a target UI Document
	/// </summary>
	[AddComponentMenu("")]
	public class MMF_UIToolkitFloatBase : MMF_UIToolkit
	{
		/// a static bool used to disable all feedbacks of this type at once
		public enum Modes { Instant, Interpolate, ToDestination }

		/// the duration of this feedback is the duration of the color transition, or 0 if instant
		public override float FeedbackDuration { get { return (Mode == Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { Duration = value; } }
		public override bool HasCustomInspectors => true;
		
		[MMFInspectorGroup("Value", true, 16)]
		/// the selected color mode :
		/// None : nothing will happen,
		/// gradient : evaluates the color over time on that gradient, from left to right,
		/// interpolate : lerps from the current color to the destination one 
		[Tooltip("所选模式：" +
		         "Instant：数值会立即切换到目标值；" +
		         "Curve：数值会沿曲线进行插值变化；" +
		         "ToDestination：从当前值插值到目标值。")]
		public Modes Mode = Modes.Interpolate;
		/// 该值是否基于初始值相对应用。若启用，输入值会在初始值基础上叠加；若关闭，则按绝对值应用。
		[Tooltip("该值是否基于初始值相对应用。若启用，输入值会在初始值基础上叠加；若关闭，则按绝对值应用。")]
		[MMFEnumCondition("Mode", (int)Modes.Interpolate, (int)Modes.Instant)]
		public bool RelativeValue = false;
		/// 若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。
		[Tooltip("若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。")] 
		public bool AllowAdditivePlays = false;
		/// 数值/颜色随时间变化时的持续时间。
		[Tooltip("数值/颜色随时间变化时的持续时间。")]
		[MMFEnumCondition("Mode", (int)Modes.Interpolate, (int)Modes.ToDestination)]
		public float Duration = 0.2f;
		/// Instant 模式下要应用的值。
		[Tooltip("即时模式下要应用的价值。")]
		[MMFEnumCondition("Mode", (int)Modes.Instant)]
		public float InstantValue = 1f;

		/// 插值到目标值时使用的曲线。
		[Tooltip("插值到目标值时使用的曲线。")]
		public MMTweenType Curve = new MMTweenType(MMTween.MMTweenCurve.EaseInCubic, "", "Mode", (int)Modes.Interpolate, (int)Modes.ToDestination);
		/// 将曲线 0 端重新映射到的值。
		[Tooltip("将曲线 0 端重新映射到的值。")]
		[MMFEnumCondition("Mode", (int)Modes.Interpolate)]
		public float CurveRemapZero = 0f;
		/// 将曲线 1 端重新映射到的值。
		[Tooltip("将曲线 1 端重新映射到的值。")]
		[MMFEnumCondition("Mode", (int)Modes.Interpolate)]
		public float CurveRemapOne = 1f;
		/// ToDestination 模式下要逼近的目标值。
		[Tooltip("ToDestination 模式下要逼近的目标值。")]
		[MMFEnumCondition("Mode", (int)Modes.ToDestination)]
		public float DestinationValue = 1f;

		protected float _initialValue;
		protected Coroutine _coroutine;

		/// <summary>
		/// On init we store our initial value
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			if ((_visualElements == null) || (_visualElements.Count == 0))
			{
				return;
			}
			_initialValue = GetInitialValue();
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
        
			if ((_visualElements == null) || (_visualElements.Count == 0))
			{
				return;
			}

			if (RelativeValue)
			{
				_initialValue = GetInitialValue();
			}

			switch (Mode)
			{
				case Modes.Instant:
					float newInstantValue = RelativeValue ? InstantValue + _initialValue : InstantValue;
					if (!NormalPlayDirection)
					{
						newInstantValue = _initialValue;
					}
					SetValue(newInstantValue);
					break;
				case Modes.Interpolate:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(ChangeValue());
					break;
				case Modes.ToDestination:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					_initialValue = GetInitialValue();
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(ChangeValue());
					break;
			}
		}

		/// <summary>
		/// Changes the color of the text over time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ChangeValue()
		{
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);
				ApplyTime(remappedTime);
				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			ApplyTime(FinalNormalizedTime);
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
		protected virtual void ApplyTime(float time)
		{
			float newValue = 0f;
			if (Mode == Modes.Interpolate)
			{
				float startValue = RelativeValue ? CurveRemapZero + _initialValue : CurveRemapZero;
				float endValue = RelativeValue ? CurveRemapOne + _initialValue : CurveRemapOne;
				
				newValue = MMTween.Tween(time, 0f, 1f, startValue, endValue, Curve);    
			}
			else if (Mode == Modes.ToDestination)
			{
				newValue = MMTween.Tween(time, 0f, 1f, _initialValue, DestinationValue, Curve);
			}

			SetValue(newValue);
		}

		protected virtual void SetValue(float newValue)
		{
			
		}

		protected virtual float GetInitialValue()
		{
			return 0f;
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
			SetValue(_initialValue);
		}
		
		/// <summary>
		/// On Validate, we init our curves conditions if needed
		/// </summary>
		public override void OnValidate()
		{
			base.OnValidate();
			if (string.IsNullOrEmpty(Curve.EnumConditionPropertyName))
			{
				Curve.EnumConditionPropertyName = "Mode";
				Curve.EnumConditions = new bool[32];
				Curve.EnumConditions[(int)Modes.Interpolate] = true;
				Curve.EnumConditions[(int)Modes.ToDestination] = true;
			}
		}
	}
}