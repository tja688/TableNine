using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
#if MM_UGUI2
using TMPro;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// 这个反馈可让 TMP 文本中的 long 数值沿曲线随时间从 A 变化到 B。
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("这个反馈可让 TMP 文本中的 long 数值沿曲线随时间从 A 变化到 B。")]
	#if MM_UGUI2
	[FeedbackPath("TextMesh Pro/TMP Count To Long")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.TextMeshPro")]
	public class MMF_TMPCountToLong : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TMPColor; } }
		public override string RequiresSetupText { get { return "此反馈需要指定 TargetTMPText 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		#if UNITY_EDITOR && MM_UGUI2
		public override bool EvaluateRequiresSetup() { return (TargetTMPText == null); }
		public override string RequiredTargetText { get { return TargetTMPText != null ? TargetTMPText.name : "";  } }
		#endif

		/// the duration of this feedback is the duration of the scale animation
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(Duration); } set { Duration = value; } }
        
		#if MM_UGUI2
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetTMPText = FindAutomatedTarget<TMP_Text>();

		[MMFInspectorGroup("TextMeshPro Target Text", true, 12, true)]
		/// 要修改文本内容的目标 TMP_Text 组件。
		[Tooltip("要修改文本内容的目标 TMP_Text 组件。")]
		public TMP_Text TargetTMPText;
		#endif
        
		[MMFInspectorGroup("Count Settings", true, 13)]
		/// 计数起始值。
		[Tooltip("计数起始值。")]
		public long CountFrom = 0;
		/// 计数目标值。
		[Tooltip("计数目标值。")]
		public long CountTo = 100000001;
		/// 用于驱动计数变化的曲线。
		[Tooltip("用于驱动计数变化的曲线。")]
		public MMTweenType CountingCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1f)));
		/// 计数持续时间（秒）。
		[Tooltip("计数持续时间（秒）。")]
		public float Duration = 5f;
		/// 计数显示格式。
		[Tooltip("计数显示格式。")]
		public string Format = "00.00";
		/// 刷新文本字段的最小间隔（秒）。
		[Tooltip("刷新文本字段的最小间隔（秒）。")]
		public float MinRefreshFrequency = 0f;

		protected string _newText;
		protected float _startTime;
		protected float _lastRefreshAt;
		protected string _initialText;
		protected Coroutine _coroutine;
        
		/// <summary>
		/// On play we change the text of our target TMPText over time
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

			_initialText = TargetTMPText.text;
			#endif
			_coroutine = Owner.StartCoroutine(CountCo());
		}

		/// <summary>
		/// A coroutine used to animate the text
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator CountCo()
		{
			IsPlaying = true;
			_lastRefreshAt = -float.MaxValue;
			long currentValue = CountFrom;
			_startTime = FeedbackTime;
	        
			while (FeedbackTime - _startTime <= Duration)
			{
				if (FeedbackTime - _lastRefreshAt >= MinRefreshFrequency)
				{
					currentValue = ProcessCount();
					UpdateText(currentValue);
					_lastRefreshAt = FeedbackTime;
				}
		        
				yield return null;
			}
			UpdateText(CountTo);
			IsPlaying = false;
		}

		/// <summary>
		/// Updates the text of the target TMPText component with the updated value
		/// </summary>
		/// <param name="currentValue"></param>
		protected virtual void UpdateText(long currentValue)
		{
			_newText = currentValue.ToString(Format);
			#if MM_UGUI2
			TargetTMPText.text = _newText;
			#endif
		}

		/// <summary>
		/// Computes the new value of the count for the current time
		/// </summary>
		/// <param name="currentValue"></param>
		/// <returns></returns>
		protected virtual long ProcessCount()
		{
			float currentTime = FeedbackTime - _startTime;
			return MMTween.Tween(currentTime, 0f, Duration, CountFrom, CountTo, CountingCurve);
		}
		
		/// <summary>
		/// On stop, we interrupt counting if it was active
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (_coroutine == null))
			{
				return;
			}
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
			#if MM_UGUI2
			TargetTMPText.text = _initialText;
			#endif
		}
	}
}
