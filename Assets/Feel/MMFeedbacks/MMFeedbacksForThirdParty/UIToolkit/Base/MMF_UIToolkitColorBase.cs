using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// A base feedback to set a color on a target UI Document
	/// </summary>
	[AddComponentMenu("")]
	public class MMF_UIToolkitColorBase : MMF_UIToolkit
	{
		/// the duration of this feedback is whatever value's been defined for it
		public override float FeedbackDuration { get { return (Mode == Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { Duration = value; } }
		public override bool HasChannel => true;

		/// the possible modes for this feedback
		public enum Modes { OverTime, Instant }
		
		[MMFInspectorGroup("Color", true, 55, true)]
		/// 此反馈是立即修改 Image，还是在一段时间内逐步变化。
		[Tooltip("此反馈是立即修改 Image，还是在一段时间内逐步变化。")]
		public Modes Mode = Modes.OverTime;
		/// Image 在一段时间内变化时的持续时间。
		[Tooltip("Image 在一段时间内变化时的持续时间。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public float Duration = 0.2f;
		/// 若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。
		[Tooltip("若启用，即使当前反馈仍在执行中，再次调用也会重新触发；若关闭，在本次播放结束前新的 Play 调用将被忽略。")] 
		public bool AllowAdditivePlays = false;
		/// 是否修改 Image 的颜色。
		[Tooltip("是否修改图片的颜色。")]
		public bool ModifyColor = true;
		/// Image 在时间轴上要使用的颜色渐变。
		[Tooltip("Image 在时间轴上要使用的颜色渐变。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public Gradient ColorOverTime = 
			new Gradient()
			{
				colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(Color.white, 0f),
					new GradientColorKey(Color.red, 0.5f),
					new GradientColorKey(Color.white, 1f)
				},
				alphaKeys = new GradientAlphaKey[]
				{
					new GradientAlphaKey(1f, 0f),
					new GradientAlphaKey(1f, 0.5f),
					new GradientAlphaKey(1f, 1f)
				}
			};
		/// Instant 模式下要立即切换到的颜色。
		[Tooltip("Instant 模式下要立即切换到的颜色。")]
		[MMFEnumCondition("Mode", (int)Modes.Instant)]
		public Color InstantColor;
		/// 若启用，初始颜色会写入渐变起点。
		[Tooltip("若启用，初始颜色会写入渐变起点。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public bool ApplyInitialColorToGradientStart = false;
		/// 若启用，初始颜色会写入渐变终点。
		[Tooltip("若启用，初始颜色会写入渐变终点。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public bool ApplyInitialColorToGradientEnd = false;
		/// 若启用，初始颜色会写入渐变起点。 and end on play
		[FormerlySerializedAs("GrabInitialColorsOnPlay")]
		[Tooltip("若启用，初始颜色会写入渐变起点。 and end on play")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public bool ApplyInitialColorsOnPlay = true;

		protected Coroutine _coroutine;
		protected Color _initialColor;
		protected Color _initialInstantColor;

		/// <summary>
		/// On init we turn the Image off if needed
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			HandleApplyInitialColors();
        
			if ((_visualElements == null) || (_visualElements.Count == 0))
			{
				return;
			}
			
			_initialInstantColor = GetInitialColor();
		}

		protected virtual void HandleApplyInitialColors()
		{
			var colorKeys = ColorOverTime.colorKeys;
			var alphaKeys = ColorOverTime.alphaKeys;
			
			if (ApplyInitialColorToGradientStart)
			{
				colorKeys[0] = new GradientColorKey(GetInitialColor(),0f);
				alphaKeys[0] = new GradientAlphaKey(GetInitialColor().a,0f);
			}

			if (ApplyInitialColorToGradientEnd)
			{
				int lastIndex = ColorOverTime.colorKeys.Length - 1; 
				colorKeys[lastIndex] = new GradientColorKey(GetInitialColor(),1f);
				alphaKeys[lastIndex] = new GradientAlphaKey(GetInitialColor().a,1f);
			}
			
			if (ApplyInitialColorToGradientEnd || ApplyInitialColorToGradientStart)
			{
				ColorOverTime.SetKeys(colorKeys, alphaKeys);
			}
		}

		protected virtual void ApplyColor(Color newColor)
		{
			
		}

		protected virtual Color GetInitialColor()
		{
			return Color.white;
		}

		/// <summary>
		/// On Play we turn our Image on and start an over time coroutine if needed
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
        
			_initialColor = GetInitialColor();

			if (ApplyInitialColorsOnPlay)
			{
				HandleApplyInitialColors();
			}
			
			switch (Mode)
			{
				case Modes.Instant:
					if (ModifyColor)
					{
						if (NormalPlayDirection)
						{
							ApplyColor(InstantColor);
						}
						else
						{
							ApplyColor(_initialInstantColor);
						}
					}
					break;
				case Modes.OverTime:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(ImageSequence());
					break;
			}
		}

		/// <summary>
		/// This coroutine will modify the values on the Image
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator ImageSequence()
		{
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;

			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				SetImageValues(remappedTime);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetImageValues(FinalNormalizedTime);
			
			IsPlaying = false;
			_coroutine = null;
			yield return null;
		}

		/// <summary>
		/// Sets the various values on the sprite renderer on a specified time (between 0 and 1)
		/// </summary>
		/// <param name="time"></param>
		protected virtual void SetImageValues(float time)
		{
			if (ModifyColor)
			{
				ApplyColor(ColorOverTime.Evaluate(time));
			}
		}

		/// <summary>
		/// Turns the sprite renderer off on stop
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
			_coroutine = null;
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
			ApplyColor(_initialColor);
		}
	}
}