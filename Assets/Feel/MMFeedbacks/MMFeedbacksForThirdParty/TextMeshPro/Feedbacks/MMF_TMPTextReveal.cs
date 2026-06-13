using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
#if MM_UGUI2
using MoreMountains.Tools;
using TMPro;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// 这个反馈可让目标 TMP 按字符、单词或行逐步显示文本。
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("这个反馈可让目标 TMP 按字符、单词或行逐步显示文本。")]
	#if MM_UGUI2
	[FeedbackPath("TextMesh Pro/TMP Text Reveal")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.TextMeshPro")]
	public class MMF_TMPTextReveal : MMF_Feedback
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

		protected string _originalText;
		
		#if MM_UGUI2
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetTMPText = FindAutomatedTarget<TMP_Text>();

		protected TMP_TextInfo _textInfo;

		/// the duration of this feedback 
		public override float FeedbackDuration
		{
			get
			{
				if (DurationMode == DurationModes.TotalDuration)
				{
					return RevealDuration;
				}
				else
				{
					if (TargetTMPText == null)
					{
						return 0f;
					}
					
					if (TargetTMPText.textInfo == null)
					{
						bool initiallyActive = TargetTMPText.gameObject.activeSelf;
						TargetTMPText.gameObject.SetActive(true);
						TargetTMPText.ForceMeshUpdate(true);
						TargetTMPText.gameObject.SetActive(initiallyActive);
					}

					if (AllowHierarchyActivationForDurationComputation)
					{
						List<Transform> disabledParents = TargetTMPText.transform.MMEnumerateAllParents(true).Where(p => !p.gameObject.activeSelf).ToList();
						disabledParents.ForEach(p => p.gameObject.SetActive(true));
						TargetTMPText.ForceMeshUpdate(true);
						disabledParents.ForEach(p => p.gameObject.SetActive(false));
					}

					if (TargetTMPText.textInfo == null)
					{
						return 0f;
					}

					float foundLength = 0f;

					if (ReplaceText)
					{
						_originalText = TargetTMPText.text;
						TargetTMPText.text = NewText;
					}
					
					switch (RevealMode)
					{
						case RevealModes.Character:
							foundLength = RichTextLength(TargetTMPText.text) * IntervalBetweenReveals;
							break;
						case RevealModes.Lines:
							foundLength = TargetTMPText.textInfo.lineCount * IntervalBetweenReveals;
							break;
						case RevealModes.Words:
							foundLength = TargetTMPText.textInfo.wordCount * IntervalBetweenReveals;
							break;
					}

					if (ReplaceText)
					{
						TargetTMPText.text = _originalText;
					}

					return foundLength;
				}                
			}
			set
			{
				if (DurationMode == DurationModes.TotalDuration)
				{
					RevealDuration = value;
					
					if (TargetTMPText != null)
					{
						if (ReplaceText)
						{
							_originalText = TargetTMPText.text;
							TargetTMPText.text = NewText;
						}
						switch (RevealMode)
						{
							case RevealModes.Character:
								IntervalBetweenReveals = value / RichTextLength(TargetTMPText.text);
								break;
							case RevealModes.Lines:
								IntervalBetweenReveals = value / TargetTMPText.textInfo.lineCount;
								break;
							case RevealModes.Words:
								IntervalBetweenReveals = value / TargetTMPText.textInfo.wordCount;
								break;
						}
						if (ReplaceText)
						{
							TargetTMPText.text = _originalText;
						}
					}
				}
				else
				{
					if (TargetTMPText != null)
					{
						if (ReplaceText)
						{
							_originalText = TargetTMPText.text;
							TargetTMPText.text = NewText;
						}
						switch (RevealMode)
						{
							case RevealModes.Character:
								IntervalBetweenReveals = value / RichTextLength(TargetTMPText.text);
								break;
							case RevealModes.Lines:
								IntervalBetweenReveals = value / TargetTMPText.textInfo.lineCount;
								break;
							case RevealModes.Words:
								IntervalBetweenReveals = value / TargetTMPText.textInfo.wordCount;
								break;
						}
						if (ReplaceText)
						{
							TargetTMPText.text = _originalText;
						}
					}
				}
			}
		}
		
		#endif

		/// the possible ways to reveal the text
		public enum RevealModes { Character, Lines, Words }
		/// 持续时间的定义方式：使用每次显示单位之间的时间间隔，或使用整段揭示的总持续时间。
		public enum DurationModes { Interval, TotalDuration }

		#if MM_UGUI2
		[MMFInspectorGroup("Target", true, 12, true)]
		/// 要修改文本内容的目标 TMP_Text 组件。
		[Tooltip("要修改文本内容的目标 TMP_Text 组件。")]
		public TMP_Text TargetTMPText;
		#endif

		[MMFInspectorGroup("Change Text", true, 13)]

		/// 播放时是否替换当前 TMP 目标的文本内容。
		[Tooltip("播放时是否替换当前 TMP 目标的文本内容。")]
		public bool ReplaceText = false;
		/// 若启用，初始化时会将 maxVisible Characters/Lines/Words 设为 0。
		[Tooltip("若启用，初始化时将 最大可见字符/行/单词 设置为 0。")]
		public bool HideTextOnInitialization = false;
		/// 用于替换旧文本的新内容。
		[Tooltip("用于替换旧文本的新内容。")]
		[TextArea]
		public string NewText = "Hello World";

		[MMFInspectorGroup("Reveal", true, 14)]
		/// 文本逐步显示的方式：按字符、按单词或按行。
		[Tooltip("文本逐步显示的方式：按字符、按单词或按行。")]
		public RevealModes RevealMode = RevealModes.Character;
		/// 持续时间的定义方式：使用每次显示单位之间的时间间隔，或使用整段揭示的总持续时间。
		[Tooltip("持续时间的定义方式：使用每次显示单位之间的时间间隔，或使用整段揭示的总持续时间。")]
		public DurationModes DurationMode = DurationModes.Interval;
		/// 两次揭示之间的时间间隔（秒）。
		[Tooltip("两次揭示之间的时间间隔（秒）。")]
		[MMFEnumCondition("DurationMode", (int)DurationModes.Interval)]
		public float IntervalBetweenReveals = 0.05f;
		/// 整段文本揭示的总持续时间（秒）。
		[Tooltip("整段文本揭示的总持续时间（秒）。")]
		[MMFEnumCondition("DurationMode", (int)DurationModes.TotalDuration)]
		public float RevealDuration = 1f;
		/// 每次发生一次揭示（单词、行或字符）时调用的 UnityEvent。
		[Tooltip("每次发生一次揭示（单词、行或字符）时调用的 UnityEvent。")]
		public UnityEvent OnReveal;
		/// 这个选项有点特殊：由于 TextMeshPro 无法直接读取已禁用文本的长度，系统需要先临时启用它，再立刻恢复禁用。如果你的目标文本本身被禁用，或它位于被禁用的层级中，建议开启此项，以便系统正确计算持续时间。否则在目标 Transform 被禁用时，持续时间计算会不准确。
		[Tooltip("这个选项有点特殊：由于 TextMeshPro 无法直接读取已禁用文本的长度，系统需要先临时启用它，再立刻恢复禁用。如果你的目标文本本身被禁用，或它位于被禁用的层级中，建议开启此项，以便系统正确计算持续时间。否则在目标 Transform 被禁用时，持续时间计算会不准确。")]
		public bool AllowHierarchyActivationForDurationComputation = false;

		protected float _delay;
		protected Coroutine _coroutine;
		protected int _richTextLength;

		protected int _totalCharacters;
		protected int _totalLines;
		protected int _totalWords;
		protected string _initialText;
		protected int _indexLastTime = -1;

		/// <summary>
		/// Sets the maximum amount of visible characters/words/lines to 0 if needed 
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			#if MM_UGUI2
            
			if (TargetTMPText == null)
			{
				return;
			}
			
			if (HideTextOnInitialization)
			{
				switch (RevealMode)
				{
					case RevealModes.Character:
						TargetTMPText.maxVisibleCharacters = 0;
						break;
					case RevealModes.Lines:
						TargetTMPText.maxVisibleLines = 0;
						break;
					case RevealModes.Words:
						TargetTMPText.maxVisibleWords = 0;
						break;
				}
			}
			
			#endif
		}

		/// <summary>
		/// On play we change the text of our target TMPText
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

			if (DurationMode == DurationModes.TotalDuration)
			{
				FeedbackDuration = RevealDuration;
			}

			_initialText = TargetTMPText.text;
			_textInfo = TargetTMPText.textInfo;

			if (ReplaceText)
			{
				TargetTMPText.text = NewText;
				TargetTMPText.ForceMeshUpdate();
			}
			_richTextLength = RichTextLength(TargetTMPText.text);
			if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
			switch (RevealMode)
			{
				case RevealModes.Character:
					_delay = (DurationMode == DurationModes.Interval) ? IntervalBetweenReveals : RevealDuration / _richTextLength;
					TargetTMPText.maxVisibleCharacters = 0;
					_coroutine = Owner.StartCoroutine(RevealCharacters());
					break;
				case RevealModes.Lines:
					_delay = (DurationMode == DurationModes.Interval) ? IntervalBetweenReveals : RevealDuration / TargetTMPText.textInfo.lineCount;
					TargetTMPText.maxVisibleLines = 0;
					_coroutine = Owner.StartCoroutine(RevealLines());
					break;
				case RevealModes.Words:
					_delay = (DurationMode == DurationModes.Interval) ? IntervalBetweenReveals : RevealDuration / TargetTMPText.textInfo.wordCount;
					TargetTMPText.maxVisibleWords = 0;
					_coroutine = Owner.StartCoroutine(RevealWords());
					break;
			}
			#endif
		}

		#if MM_UGUI2

		/// <summary>
		/// Reveals characters one at a time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator RevealCharacters()
		{
			float startTime = FeedbackTime;
			_totalCharacters = _richTextLength;
			int visibleCharacters = 0;

			IsPlaying = true;
			TargetTMPText.maxVisibleCharacters = 0;

			while ((visibleCharacters < _totalCharacters) && !Owner.SkippingToTheEnd)
			{
				float currentTime = FeedbackTime;
				float elapsed = currentTime - startTime;

				int expectedVisibleCharacters = 0;

				if (DurationMode == DurationModes.Interval)
				{
					expectedVisibleCharacters = Mathf.FloorToInt(elapsed / IntervalBetweenReveals);
				}
				else 
				{
					expectedVisibleCharacters = Mathf.FloorToInt((_totalCharacters * elapsed) / RevealDuration);
				}

				expectedVisibleCharacters = Mathf.Clamp(expectedVisibleCharacters, 0, _totalCharacters);

				if (expectedVisibleCharacters > visibleCharacters)
				{
					visibleCharacters = expectedVisibleCharacters;
					TargetTMPText.maxVisibleCharacters = visibleCharacters;
					InvokeRevealEvents();
				}

				yield return null;
			}

			TargetTMPText.maxVisibleCharacters = _richTextLength;
			IsPlaying = false;
		}

		/// <summary>
		/// Reveals lines one at a time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator RevealLines()
		{
			_totalLines = TargetTMPText.textInfo.lineCount;
			int visibleLines = 0;

			IsPlaying = true;
			while ((visibleLines <= _totalLines) && !Owner.SkippingToTheEnd)
			{
				TargetTMPText.maxVisibleLines = visibleLines;
				InvokeRevealEvents();
				visibleLines++;

				yield return WaitFor(_delay);
			}
			TargetTMPText.maxVisibleLines = _totalLines;
			IsPlaying = false;
		}
	        
		/// <summary>
		/// Reveals words one at a time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator RevealWords()
		{
			_totalWords = TargetTMPText.textInfo.wordCount;
			int visibleWords = 0;

			IsPlaying = true;
			while ((visibleWords <= _totalWords) && !Owner.SkippingToTheEnd)
			{
				TargetTMPText.maxVisibleWords = visibleWords;
				InvokeRevealEvents();
				visibleWords++;
				yield return WaitFor(_delay);
			}
			TargetTMPText.maxVisibleWords = _totalWords;
			IsPlaying = false;
		}

		/// <summary>
		/// Invokes on reveal events
		/// </summary>
		protected virtual void InvokeRevealEvents()
		{
			if ( ((RevealMode == RevealModes.Character) && (TargetTMPText.maxVisibleCharacters == 0))
			    || ((RevealMode == RevealModes.Character) && !IsNewVisibleCharacter())
				|| ((RevealMode == RevealModes.Lines) && (TargetTMPText.maxVisibleLines == 0))
				|| ((RevealMode == RevealModes.Words) && (TargetTMPText.maxVisibleWords == 0)) )
			{
				return;
			}
			
			OnReveal?.Invoke();
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
		/// On skip, we display our entire text
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomSkipToTheEnd(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!IsPlaying)
			{
				return;
			}
		        
			switch (RevealMode)
			{
				case RevealModes.Character:
					TargetTMPText.maxVisibleCharacters = _totalCharacters;
					break;
				case RevealModes.Lines:
					TargetTMPText.maxVisibleLines = _totalLines;
					break;
				case RevealModes.Words:
					TargetTMPText.maxVisibleWords = _totalWords;
					break;
			}
		}
	        
		/// <summary>
		/// Returns the length of a rich text, excluding its tags
		/// </summary>
		/// <param name="richText"></param>
		/// <returns></returns>
		protected int RichTextLength(string richText)
		{
			int richTextLength = 0;
			bool insideTag = false;

			richText = richText.Replace("<br>", "-");
			var tagName = new StringBuilder();
			foreach (char character in richText)
			{
				if (character == '<')
				{
					insideTag = true;
					tagName.Clear();
					continue;
				}
				else if (character == '>')
				{
					if(tagName.ToString().StartsWith("sprite")) richTextLength++;
					insideTag = false;
				}
				else if (!insideTag)
				{
					richTextLength++;
				}
				else
				{
					tagName.Append(character);
				}
			}

			return richTextLength;
		}

		/// <summary>
		/// Returns true if the last visible letter of the TMP text is new and visible and a letter or digit
		/// </summary>
		/// <returns></returns>
		protected virtual bool IsNewVisibleCharacter()
		{
			int lastVisibleCharIndex = -1;
			_textInfo = TargetTMPText.GetTextInfo(TargetTMPText.text);

			for (int i = 0; i < _textInfo.characterCount; i++)
			{
				if (_textInfo.characterInfo[i].isVisible)
				{
					lastVisibleCharIndex = i;
				}
			}

			if ((lastVisibleCharIndex < 0) 
			    || (lastVisibleCharIndex > TargetTMPText.text.Length)
			    || (lastVisibleCharIndex == _indexLastTime))
			{
				return false;
			}
			
			_indexLastTime = lastVisibleCharIndex;
			return Char.IsLetterOrDigit(_textInfo.characterInfo[lastVisibleCharIndex].character);
		}
		
		#endif
		
		
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
