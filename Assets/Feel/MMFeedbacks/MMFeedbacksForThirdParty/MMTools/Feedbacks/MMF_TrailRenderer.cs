using System.Collections;
using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you control the length, width and color of a target TrailRenderer over time
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可控制目标 TrailRenderer 的宽度、颜色与保留时长（time）。在 Instant 模式下会立即写入；在 OverTime 模式下会按 Duration 和 Transition 曲线过渡。")]
	[System.Serializable]
	[FeedbackPath("Renderer/Trail Renderer")]
	public class MMF_TrailRenderer : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.RendererColor; } }
		public override bool EvaluateRequiresSetup() => (TargetTrailRenderer == null);
		public override string RequiredTargetText => TargetTrailRenderer != null ? TargetTrailRenderer.name : "";  
		public override string RequiresSetupText => "此反馈必须先指定 TargetTrailRenderer 才能正常工作。你可以在下方进行设置。"; 
		#endif
		public override bool HasRandomness => true;
		public override bool HasCustomInspectors => true; 

		/// the possible modes for this feedback
		public enum Modes { OverTime, Instant }

		[MMFInspectorGroup("Trail Renderer", true, 24, true)]
		/// the trail renderer whose properties you want to modify
		[Tooltip("要修改其属性轨迹渲染器。")]
		public TrailRenderer TargetTrailRenderer;
		/// whether the feedback should affect the sprite renderer instantly or over a period of time
		[Tooltip("选择生效方式：Instant 会立即应用当前启用的参数；OverTime 会在 Duration 内按 Transition 曲线逐步过渡。")]
		public Modes Mode = Modes.OverTime;
		/// how long the sprite renderer should change over time
		[Tooltip("仅在 OverTime 模式下生效，表示过渡总时长（秒）。Instant 模式会忽略该值。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public float Duration = 2f;
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("若启用，即使该反馈仍在执行中，再次调用也会立刻重新触发；若关闭，则当前一次播放结束前会阻止新的 Play 调用。")] 
		public bool AllowAdditivePlays = false;
		/// a curve to use to animate the trail renderer's density over time
		[Tooltip("仅在 OverTime 模式下生效，用于控制过渡进度。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public MMTweenType Transition = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));

		[MMFInspectorGroup("Width", true, 25)]
		/// whether or not to modify the trail renderer's width
		[Tooltip("是否修改 TrailRenderer 的宽度。关闭后下方宽度曲线不生效。")]
		public bool ModifyWidth = true;
		/// a curve defining the new width of the trail renderer, describing the world space width of the trail at each point along its length
		[Tooltip("用于定义新宽度的曲线，描述轨迹沿长度各点的世界空间宽度。若 ModifyWidth 为 false，此项会被忽略。")]
		public AnimationCurve NewWidth = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));

		[MMFInspectorGroup("Color", true, 28)]
		/// whether or not to modify the trail renderer's color
		[Tooltip("是否修改 TrailRenderer 的颜色。关闭后下方颜色设置不生效。")]
		public bool ModifyColor = true;
		/// the colors to apply to the sprite renderer over time
		[Tooltip("要应用的颜色渐变。若 ModifyColor 为 false，此项会被忽略。")]
		public Gradient NewColor = new Gradient();
		
		[MMFInspectorGroup("Trail Renderer Time", true, 28)]
		/// whether or not to modify the trail renderer's time (how long the trail should be in seconds)
		[Tooltip("是否修改 TrailRenderer 的 time（轨迹保留时长，秒）。关闭后下方时间值不生效。")]
		public bool ModifyTime = true;
		/// the new trail renderer's time (how long the trail should be in seconds) to apply
		[Tooltip("要应用的新 TrailRenderer.time（轨迹保留时长，单位为秒）。若 ModifyTime 为 false，此项会被忽略。")]
		public float NewTime = 2f;
		
		/// the duration of this feedback is the duration of the sprite renderer, or 0 if instant
		public override float FeedbackDuration { get { return (Mode == Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { if (Mode != Modes.Instant) { Duration = value; } } }
        
		protected Coroutine _coroutine;
		protected Gradient _initialColor;
		protected AnimationCurve _initialWidth;
		protected float _initialTime;
		
		protected Gradient _firstColor;
		protected AnimationCurve _firstWidth;
		protected float _firstTime;
		
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			if (Active)
			{
				if (TargetTrailRenderer == null)
				{
					Debug.LogWarning("[Trail Renderer Feedback] The trail renderer feedback on "+Owner.name+" doesn't have a TargetTrailRenderer, it won't work. You need to specify one in its inspector.");
					return;
				}
				
				_firstColor = TargetTrailRenderer.colorGradient;
				_firstWidth = TargetTrailRenderer.widthCurve;
				_firstTime = TargetTrailRenderer.time;
			}
		}

		/// <summary>
		/// On Play we change the values of our trail renderer
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetTrailRenderer == null))
			{
				return;
			}
			
			_initialColor = TargetTrailRenderer.colorGradient;
			_initialWidth = TargetTrailRenderer.widthCurve;
			_initialTime = TargetTrailRenderer.time;
            
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			switch (Mode)
			{
				case Modes.Instant:
					if (ModifyColor)
					{
						TargetTrailRenderer.colorGradient = NormalPlayDirection ? NewColor : _firstColor;
					}
					if (ModifyWidth)
					{
						TargetTrailRenderer.widthCurve = NormalPlayDirection ? NewWidth : _firstWidth;
					}
					if (ModifyTime)
					{
						TargetTrailRenderer.time = NormalPlayDirection ? NewTime : _firstTime;
					}
					break;
				case Modes.OverTime:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(TrailRendererSequence(intensityMultiplier));
					break;
			}
		}

		/// <summary>
		/// This coroutine will modify the values on the trail renderer over time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator TrailRendererSequence(float intensityMultiplier)
		{
			IsPlaying = true;
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);
				remappedTime = Transition.Evaluate(remappedTime);
				SetTrailRendererValues(remappedTime, intensityMultiplier);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			
			SetTrailRendererValues(Transition.Evaluate(FinalNormalizedTime), intensityMultiplier);    
			_coroutine = null;      
			IsPlaying = false;
			yield return null;
		}

		/// <summary>
		/// Sets the various values on the trail renderer on a specified time (between 0 and 1)
		/// </summary>
		/// <param name="time"></param>
		protected virtual void SetTrailRendererValues(float time, float intensityMultiplier)
		{
			if (ModifyColor)
			{
				if (NormalPlayDirection)
				{
					TargetTrailRenderer.colorGradient = MMColors.LerpGradients(_initialColor, NewColor, time);	
				}
				else
				{
					TargetTrailRenderer.colorGradient = MMColors.LerpGradients(NewColor, _firstColor, time);
				}
			}

			if (ModifyWidth)
			{
				if (NormalPlayDirection)
				{
					TargetTrailRenderer.widthCurve = MMAnimationCurves.LerpAnimationCurves(_initialWidth, NewWidth, time);	
				}
				else
				{
					TargetTrailRenderer.widthCurve = MMAnimationCurves.LerpAnimationCurves(NewWidth, _firstWidth, time);
				}
			}

			if (ModifyTime)
			{
				if (NormalPlayDirection)
				{
					TargetTrailRenderer.time = MMMaths.Lerp(_initialTime, NewTime, time, FeedbackDeltaTime);	
				}
				else
				{
					TargetTrailRenderer.time = MMMaths.Lerp(NewTime, _firstTime, time, FeedbackDeltaTime);
				}
				
			}
		}
        
		/// <summary>
		/// Stops this feedback
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized || (_coroutine == null))
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);
			IsPlaying = false;
			Owner.StopCoroutine(_coroutine);
			_coroutine = null;
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
			TargetTrailRenderer.widthCurve = _firstWidth;
			TargetTrailRenderer.colorGradient = _firstColor;
		}
	}
}
