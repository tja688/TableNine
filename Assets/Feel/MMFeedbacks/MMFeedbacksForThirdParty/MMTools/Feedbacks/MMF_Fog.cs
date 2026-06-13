using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you animate the density, color, end and start distance of your scene's fog
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可为场景雾效的密度、颜色、起始距离和结束距离制作动画。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[System.Serializable]
	[FeedbackPath("Renderer/Fog")]
	public class MMF_Fog : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.RendererColor; } }
		public override string RequiredTargetText { get { return Mode.ToString();  } }
		#endif
		public override bool HasRandomness => true;
		public override bool HasCustomInspectors => true; 

		/// the possible modes for this feedback
		public enum Modes { OverTime, Instant }

		[MMFInspectorGroup("Fog", true, 24)]
		/// whether the feedback should affect the sprite renderer instantly or over a period of time
		[Tooltip("该反馈应立即生效，还是在一段时间内逐步作用")]
		public Modes Mode = Modes.OverTime;
		/// how long the sprite renderer should change over time
		[Tooltip("该效果在渐变模式下持续变化的时间")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public float Duration = 2f;
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("若启用，即使该反馈仍在执行中，再次调用也会立刻重新触发；若关闭，则当前一次播放结束前会阻止新的 Play 调用。")] 
		public bool AllowAdditivePlays = false;

		[MMFInspectorGroup("Fog Density", true, 25)]
		/// whether or not to modify the fog's density
		[Tooltip("是否修改雾的密度")]
		public bool ModifyFogDensity = true;
		/// a curve to use to animate the fog's density over time
		[Tooltip("用于随时间驱动雾密度变化的曲线")]
		public MMTweenType DensityCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));
		/// the value to remap the fog's density curve zero value to
		[Tooltip("将雾密度曲线 0 端重映射到的值")]
		public float DensityRemapZero = 0.01f;
		/// the value to remap the fog's density curve one value to
		[Tooltip("将雾密度曲线 1 端重映射到的值")]
		public float DensityRemapOne = 0.05f;
		/// the value to change the fog's density to when in instant mode
		[Tooltip("Instant 模式下雾密度将直接设置到的值")]
		public float DensityInstantChange;
        
		[MMFInspectorGroup("Fog Start Distance", true, 26)]
		/// whether or not to modify the fog's start distance
		[Tooltip("是否修改雾的起始距离")]
		public bool ModifyStartDistance = true;
		/// a curve to use to animate the fog's start distance over time
		[Tooltip("用于随时间驱动雾起始距离变化的曲线")]
		public MMTweenType StartDistanceCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));
		/// the value to remap the fog's start distance curve zero value to
		[Tooltip("将雾起始距离曲线 0 端重映射到的值")]
		public float StartDistanceRemapZero = 0f;
		/// the value to remap the fog's start distance curve one value to
		[Tooltip("将雾起始距离曲线 1 端重映射到的值")]
		public float StartDistanceRemapOne = 0f;
		/// the value to change the fog's start distance to when in instant mode
		[Tooltip("Instant 模式下雾起始距离将直接设置到的值")]
		public float StartDistanceInstantChange;
        
		[MMFInspectorGroup("Fog End Distance", true, 27)]
		/// whether or not to modify the fog's end distance
		[Tooltip("是否修改雾的结束距离")]
		public bool ModifyEndDistance = true;
		/// a curve to use to animate the fog's end distance over time
		[Tooltip("用于随时间驱动雾结束距离变化的曲线")]
		public MMTweenType EndDistanceCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));
		/// the value to remap the fog's end distance curve zero value to
		[Tooltip("将雾结束距离曲线 0 端重映射到的值")]
		public float EndDistanceRemapZero = 0f;
		/// the value to remap the fog's end distance curve one value to
		[Tooltip("将雾结束距离曲线 1 端重映射到的值")]
		public float EndDistanceRemapOne = 300f;
		/// the value to change the fog's end distance to when in instant mode
		[Tooltip("Instant 模式下雾结束距离将直接设置到的值")]
		public float EndDistanceInstantChange;
        
		[MMFInspectorGroup("Fog Color", true, 28)]
		/// whether or not to modify the fog's color
		[Tooltip("是否修改雾的颜色")]
		public bool ModifyColor = true;
		/// the colors to apply to the sprite renderer over time
		[Tooltip("随时间应用的颜色变化")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public Gradient ColorOverTime;
		/// the color to move to in instant mode
		[Tooltip("Instant 模式下要直接切换到的颜色")]
		[MMFEnumCondition("Mode", (int)Modes.Instant)]
		public Color InstantColor;

		/// the duration of this feedback is the duration of the sprite renderer, or 0 if instant
		public override float FeedbackDuration { get { return (Mode == Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { if (Mode != Modes.Instant) { Duration = value; } } }
        
		protected Coroutine _coroutine;
		protected Color _initialColor;
		protected float _initialStartDistance;
		protected float _initialEndDistance;
		protected float _initialDensity;
		
		protected Color _initialInstantColor;
		protected float _initialInstantStartDistance;
		protected float _initialInstantEndDistance;
		protected float _initialInstantDensity;
		
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			if (Active)
			{
				_initialInstantColor = RenderSettings.fogColor;
				_initialInstantStartDistance = RenderSettings.fogStartDistance;
				_initialInstantEndDistance = RenderSettings.fogEndDistance;
				_initialInstantDensity = RenderSettings.fogDensity;

				if (ColorOverTime == null)
				{
					ColorOverTime = new Gradient();
				}
			}
		}

		/// <summary>
		/// On Play we change the values of our fog
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			
			_initialColor = RenderSettings.fogColor;
			_initialStartDistance = RenderSettings.fogStartDistance;
			_initialEndDistance = RenderSettings.fogEndDistance;
			_initialDensity = RenderSettings.fogDensity;
            
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			switch (Mode)
			{
				case Modes.Instant:
					if (ModifyColor)
					{
						RenderSettings.fogColor = NormalPlayDirection ? InstantColor : _initialColor;
					}

					if (ModifyStartDistance)
					{
						RenderSettings.fogStartDistance = NormalPlayDirection ? StartDistanceInstantChange : _initialInstantStartDistance;
					}

					if (ModifyEndDistance)
					{
						RenderSettings.fogEndDistance = NormalPlayDirection ? EndDistanceInstantChange : _initialInstantEndDistance;
					}

					if (ModifyFogDensity)
					{
						RenderSettings.fogDensity = NormalPlayDirection ? DensityInstantChange * intensityMultiplier : _initialInstantDensity;
					}
					break;
				case Modes.OverTime:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(FogSequence(intensityMultiplier));
					break;
			}
		}

		/// <summary>
		/// This coroutine will modify the values on the fog settings
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator FogSequence(float intensityMultiplier)
		{
			IsPlaying = true;
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				SetFogValues(remappedTime, intensityMultiplier);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetFogValues(FinalNormalizedTime, intensityMultiplier);    
			_coroutine = null;      
			IsPlaying = false;
			yield return null;
		}

		/// <summary>
		/// Sets the various values on the fog on a specified time (between 0 and 1)
		/// </summary>
		/// <param name="time"></param>
		protected virtual void SetFogValues(float time, float intensityMultiplier)
		{
			if (ModifyColor)
			{
				RenderSettings.fogColor = ColorOverTime.Evaluate(time); 
			}

			if (ModifyFogDensity)
			{
				RenderSettings.fogDensity = MMTween.Tween(time, 0f, 1f, DensityRemapZero, DensityRemapOne, DensityCurve) * intensityMultiplier;
			}

			if (ModifyStartDistance)
			{
				RenderSettings.fogStartDistance = MMTween.Tween(time, 0f, 1f, StartDistanceRemapZero, StartDistanceRemapOne, StartDistanceCurve);
			}

			if (ModifyEndDistance)
			{
				RenderSettings.fogEndDistance = MMTween.Tween(time, 0f, 1f, EndDistanceRemapZero, EndDistanceRemapOne, EndDistanceCurve);
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

			RenderSettings.fogColor = _initialColor;
			RenderSettings.fogStartDistance = _initialStartDistance;
			RenderSettings.fogEndDistance = _initialEndDistance;
			RenderSettings.fogDensity = _initialDensity;
		}
	}
}