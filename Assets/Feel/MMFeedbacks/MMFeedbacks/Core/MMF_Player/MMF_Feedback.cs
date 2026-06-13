using System;
using System.Collections;
using UnityEngine;
using MoreMountains.Tools;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoreMountains.Feedbacks
{
	[Serializable]
	public abstract class MMF_Feedback
	{
		#region Properties

		public const string _randomnessGroupName = "Feedback Randomness";
		public const string _rangeGroupName = "Feedback Range";
		public const string _automaticSetupGroupName = "Automatic Setup";
		
		[MMFInspectorGroup("Feedback Settings", true, 0, false, true)]
		/// 此 feedback 是否启用。
		[Tooltip("此反馈是否启用。")]
		public bool Active = true;

		[HideInInspector] public int UniqueID;

		/// 此 feedback 在 Inspector 中显示的名称。
		[Tooltip("此反馈在 Inspector 中显示的名称。")]
		public string Label = "MMFeedback";

		/// you can override this when creating a custom feedback to have it behave differently and display a different label 
		public virtual string GetLabel() => Label;

		/// the original label of this feedback, used to display next to the custom label in case we set one
		[MMFHidden]
		public string OriginalLabel = "";

		/// whether to broadcast this feedback's message using an int or a scriptable object. Ints are simple to setup but can get messy and make it harder to remember what int corresponds to what.
		/// MMChannel scriptable objects require you to create them in advance, but come with a readable name and are more scalable
		[Tooltip("决定此反馈是通过 `int` 还是 `MMChannel` ScriptableObject 来广播消息。`int` 配置简单，但项目变大后容易混乱，也不便记忆每个数字的含义；`MMChannel` 需要预先创建资源，但名称可读性更高，也更易扩展。")]
		public MMChannelModes ChannelMode = MMChannelModes.Int;

		/// 此 feedback 通信所使用的频道 ID。 
		[Tooltip("此反馈通信所使用的通道 ID。")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.Int)]
		public int Channel = 0;

		/// the MMChannel definition asset to use to broadcast this feedback. The shaker will have to reference that same MMChannel definition to receive events - to create a MMChannel,
		/// right click anywhere in your project (usually in a Data folder) and go MoreMountains > MMChannel, then name it with some unique name
		[Tooltip("用于此广播反馈的`通道资源`定义资源。要接收必须引用此反馈事件的增益器同一个`通道资源`。若要创建`通道资源`，可在项目视图中右键（通常在数据文件夹中），选择更多山脉 > 通道资源，并说明命名。")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.MMChannel)]
		public MMChannel MMChannelDefinition = null;

		/// 此 feedback 的触发概率（百分比）。`100` 表示每次都触发，`0` 表示永不触发，`50` 大致表示两次触发中出现一次。
		[Tooltip(
			"此 feedback 的触发概率（百分比）。`100` 表示每次都触发，`0` 表示永不触发，`50` 大致表示两次触发中出现一次。")]
		[Range(0, 100)]
		public float Chance = 100f;

		/// 用于自定义此 feedback 在 `MMF_Player` 列表中的背景颜色。
		[Tooltip("用于自定义此反馈在 `MMF_Player` 列表中的背景颜色。")]
		public virtual Color DisplayColor => Color.black;

		/// 与时间相关的一组设置，例如延迟、重复等。
		[Tooltip("与时间相关的一组设置，例如延迟、重复等。")]
		public MMFeedbackTiming Timing;
		
		/// 用于定义此 feedback 的自动目标获取规则，例如自动从当前 GameObject、父物体、子物体或 Reference Holder 上抓取目标。
		[Tooltip("用于定义此反馈的自动目标获取规则，例如自动从当前 GameObject、父物体、子物体或 Reference Holder 上抓取目标。")]
		public MMFeedbackTargetAcquisition AutomatedTargetAcquisition;
		
		[MMFInspectorGroup(_randomnessGroupName, true, 58, false, true)]
		/// 若启用，播放时会把强度乘以一个随机值，该值取自 `RandomMultiplier.x` 到 `RandomMultiplier.y` 之间。
		[Tooltip(
			"若启用，播放时会把强度乘以一个随机值，该值取自 `RandomMultiplier.x` 到 `RandomMultiplier.y` 之间。")]
		public bool RandomizeOutput = false;

		/// 当 `RandomizeOutput` 为 true 时，用于乘到此 feedback 输出结果上的随机倍率范围（`x` 为最小值，`y` 为最大值）。
		[Tooltip(
			"当 `RandomizeOutput` 为 true 时，用于乘到此 feedback 输出结果上的随机倍率范围（`x` 为最小值，`y` 为最大值）。")]
		[MMFCondition("RandomizeOutput", true)]
		[MMFVector("Min", "Max")]
		public Vector2 RandomMultiplier = new Vector2(0.8f, 1f);

		/// 若启用，此 feedback 的时长在播放时会乘以一个随机倍率，取值范围为 `RandomDurationMultiplier.x` 到 `RandomDurationMultiplier.y`。
		[Tooltip(
			"若启用，此 feedback 的时长在播放时会乘以一个随机倍率，取值范围为 `RandomDurationMultiplier.x` 到 `RandomDurationMultiplier.y`。")]
		public bool RandomizeDuration = false;

		/// 当 `RandomizeDuration` 为 true 时，用于乘到此 feedback 时长上的随机倍率范围（`x` 为最小值，`y` 为最大值）。
		[Tooltip(
			"当 `RandomizeDuration` 为 true 时，用于乘到此 feedback 时长上的随机倍率范围（`x` 为最小值，`y` 为最大值）。")]
		[MMFCondition("RandomizeDuration", true)]
		[MMFVector("Min", "Max")]
		public Vector2 RandomDurationMultiplier = new Vector2(0.5f, 2f);

		[MMFInspectorGroup(_rangeGroupName, true, 47)]
		/// 若启用，只有位于指定范围内的 shaker 会响应此 feedback。
		[Tooltip("若启用，只有位于指定范围内的抖动器会响应此反馈。")]
		public bool UseRange = false;

		/// 在 `UseRange` 模式下，只有距离不超过该值的 shaker 会响应此 feedback。
		[Tooltip("在 `UseRange` 模式下，只有距离不超过该值的抖动器会响应此反馈。")]
		public float RangeDistance = 5f;

		/// 在 `UseRange` 模式下，是否根据 `RangeFallOff` 曲线衰减 shake 强度。  
		[Tooltip("在 `UseRange` 模式下，是否根据 `RangeFallOff` 曲线衰减抖动强度。")]
		public bool UseRangeFalloff = false;

		/// 用于定义衰减的动画曲线。横轴 `x` 中，`0` 表示范围中心，`1` 表示最大作用距离。
		[Tooltip(
			"用于定义衰减的动画曲线。横轴 `x` 中，`0` 表示范围中心，`1` 表示最大作用距离。")]
		public AnimationCurve RangeFalloff = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

		/// 将衰减曲线 `y` 轴上 `0` 和 `1` 重映射到的目标值。
		[Tooltip("将衰减曲线 `y` 轴上 `0` 和 `1` 重映射到的目标值。")] 
		[MMFVector("Zero", "One")]
		public Vector2 RemapRangeFalloff = new Vector2(0f, 1f);
		
		[MMFInspectorGroup(_automaticSetupGroupName, true, 49, false, true)]
		
		/// 用于尝试自动为此 feedback 完成 shaker 配置的按钮，会把所需 shaker 自动添加到场景中。
		[Tooltip("用于尝试自动为此反馈完成抖动器配置的按钮，会把所需抖动器自动添加到场景中。")]
		public MMF_Button AutomaticShakerSetupButton;

		/// the Owner of the feedback, as defined when calling the Initialization method
		[HideInInspector] public MMF_Player Owner;

		[HideInInspector]
		/// whether or not this feedback is in debug mode
		public bool DebugActive = false;

		/// set this to true if your feedback should pause the execution of the feedback sequence
		public virtual IEnumerator Pause => null;

		/// if this is true, this feedback will wait until all previous feedbacks have run
		public virtual bool HoldingPause => false;

		/// if this is true, this feedback will wait until all previous feedbacks have run, then run all previous feedbacks again
		public virtual bool LooperPause => false;

		/// if this is true, this feedback will pause and wait until ResumeFeedbacks() is called on its parent MMF_Player to resume execution
		public virtual bool ScriptDrivenPause { get; set; }

		/// if this is a positive value, the feedback will auto resume after that duration if it hasn't been resumed via script already
		public virtual float ScriptDrivenPauseAutoResume { get; set; }

		/// if this is true, this feedback will wait until all previous feedbacks have run, then run all previous feedbacks again
		public virtual bool LooperStart => false;

		/// if this is true, the Channel property will be displayed, otherwise it'll be hidden        
		public virtual bool HasChannel => false;

		/// if this is true, this feedback will display an automatic shaker setup button       
		public virtual bool HasAutomaticShakerSetup => false;

		/// if this is true, the Randomness group will be displayed, otherwise it'll be hidden        
		public virtual bool HasRandomness => false;
		
		/// if this is true, this feedback implements ForceInitialState, otherwise calling that method will have no effect
		public virtual bool CanForceInitialValue => false;

		/// if this is true, force initial value will happen over two frames
		public virtual bool ForceInitialValueDelayed => false;

		/// whether or not this feedback can automatically grab the target on this game object, or a parent, a child, or on a reference holder
		public virtual bool HasAutomatedTargetAcquisition => false;
		/// when in forced reference mode, this will contain the forced reference holder that will be used (usually set by itself)
		public virtual MMF_ReferenceHolder ForcedReferenceHolder { get; set; }

		/// if this is true, the Range group will be displayed, otherwise it'll be hidden        
		public virtual bool HasRange => false;

		/// the total amount of plays this feedback has left
		public virtual int PlaysLeft => _playsLeft;

		public virtual bool HasCustomInspectors => false;
		/// an overridable color for your feedback, that can be redefined per feedback. White is the only reserved color, and the feedback will revert to 
		/// normal (light or dark skin) when left to White
		#if UNITY_EDITOR
		public virtual Color FeedbackColor => Color.white;
		#endif
		/// returns true if this feedback is in cooldown at this time (and thus can't play), false otherwise
		public virtual bool InCooldown => (Timing.CooldownDuration > 0f) &&
		                                  (FeedbackTime - _lastPlayTimestamp < Timing.CooldownDuration);

		/// if this is true, this feedback is currently playing
		public virtual bool IsPlaying { get; set; }

		/// <summary>
		/// Computes the new intensity, taking into account constant intensity and potential randomness
		/// </summary>
		/// <param name="intensity"></param>
		/// <returns></returns>
		public virtual float ComputeIntensity(float intensity, Vector3 position)
		{
			float result = Timing.ConstantIntensity ? 1f : intensity;
			result *= ComputedRandomMultiplier;
			result *= Owner.ComputeRangeIntensityMultiplier(position);
			return result;
		}

		/// <summary>
		/// Returns the random multiplier to apply to this feedback's output
		/// </summary>
		public virtual float ComputedRandomMultiplier =>
			RandomizeOutput ? Random.Range(RandomMultiplier.x, RandomMultiplier.y) : 1f;

		/// <summary>
		/// Returns the timescale mode to use in logic, taking into account the one set at the feedback level and the player level
		/// </summary>
		public virtual TimescaleModes ComputedTimescaleMode
		{
			get
			{
				if (Owner.ForceTimescaleMode)
				{
					return Owner.ForcedTimescaleMode;
				}

				return Timing.TimescaleMode;
			}
		}

		/// returns true if this feedback is in Scaled timescale mode, false otherwise
		public virtual bool InScaledTimescaleMode
		{
			get
			{
				if (Owner.ForceTimescaleMode)
				{
					return (Owner.ForcedTimescaleMode == TimescaleModes.Scaled);
				}

				return (Timing.TimescaleMode == TimescaleModes.Scaled);
			}
		}

		/// the time (or unscaled time) based on the selected Timing settings
		public virtual float FeedbackTime
		{
			get
			{
				float timescaleMultiplier = Owner.TimescaleMultiplier;
				
				#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					return (float)EditorApplication.timeSinceStartup * timescaleMultiplier;
				}
				#endif

				if (Timing.UseScriptDrivenTimescale)
				{
					return Timing.ScriptDrivenTime * timescaleMultiplier;
				}

				if (Owner.ForceTimescaleMode)
				{
					if (Owner.ForcedTimescaleMode == TimescaleModes.Scaled)
					{
						return Time.time * timescaleMultiplier;
					}
					else
					{
						return Time.unscaledTime * timescaleMultiplier;
					}
				}

				if (Timing.TimescaleMode == TimescaleModes.Scaled)
				{
					return Time.time * timescaleMultiplier;
				}
				else
				{
					return Time.unscaledTime * timescaleMultiplier;
				}
			}
		}

		/// the delta time (or unscaled delta time) based on the selected Timing settings
		public virtual float FeedbackDeltaTime
		{
			get
			{
				float timescaleMultiplier = Owner.TimescaleMultiplier;
				
				if (Timing.UseScriptDrivenTimescale)
				{
					return Timing.ScriptDrivenDeltaTime * timescaleMultiplier;
				}

				if (Owner.ForceTimescaleMode)
				{
					if (Owner.ForcedTimescaleMode == TimescaleModes.Scaled)
					{
						return Time.deltaTime * timescaleMultiplier;
					}
					else
					{
						return Time.unscaledDeltaTime * timescaleMultiplier;
					}
				}

				if (Owner.SkippingToTheEnd)
				{
					return float.MaxValue;
				}

				if (Timing.TimescaleMode == TimescaleModes.Scaled)
				{
					return Time.deltaTime * timescaleMultiplier;
				}
				else
				{
					return Time.unscaledDeltaTime * timescaleMultiplier;
				}
			}
		}

		/// <summary>
		/// The total duration of this feedback :
		/// total = initial delay + duration * (number of repeats + delay between repeats)  
		/// </summary>
		public virtual float TotalDuration
		{
			get
			{
				return _totalDuration;
			}
		}

		public virtual bool IsExpanded { get; set; }

		/// <summary>
		/// A flag used to determine if a feedback has all it needs, or if it requires some extra setup.
		/// This flag will be used to display a warning icon in the inspector if the feedback is not ready to be played.
		/// </summary>
		public virtual bool RequiresSetup => _requiresSetup;
		public virtual string RequiredTarget => _requiredTarget;

		public virtual void CacheRequiresSetup()
		{
			#if UNITY_EDITOR
			
			_requiresSetup = EvaluateRequiresSetup();
			if (_requiresSetup && HasAutomatedTargetAcquisition && (AutomatedTargetAcquisition != null) && (AutomatedTargetAcquisition.Mode != MMFeedbackTargetAcquisition.Modes.None))
			{
				_requiresSetup = false;
			}
			if ((RequiredTargetText != _requiredTargetTextCached) || (RequiredTargetTextExtra != _requiredTargetTextCachedExtra))
			{
				_requiredTarget = RequiredTargetText == "" ? "" : "[" + RequiredTargetText + "]" + RequiredTargetTextExtra;
				_requiredTargetTextCached = RequiredTargetText;
				_requiredTargetTextCachedExtra = RequiredTargetTextExtra;
			}
			
			#endif
		}
		/// if this is true, group inspectors will be displayed within this feedback
		public virtual bool DrawGroupInspectors => true;
		/// if this is true, the feedback will be displayed in the MMF Player's list with a full color background, as opposed to just a small line on the left
		public virtual bool DisplayFullHeaderColor => false;
		/// defines the setup text that will be displayed on the feedback, should setup be required
		public virtual string RequiresSetupText => "This feedback requires some additional setup.";
		/// the text used to describe the required target
		public virtual string RequiredTargetText => "";
		/// the text used to describe the required target, if more info is needed
		public virtual string RequiredTargetTextExtra => "";

		/// <summary>
		/// Override this method to determine if a feedback requires setup 
		/// </summary>
		/// <returns></returns>
		public virtual bool EvaluateRequiresSetup() => false;

		public virtual string RequiredChannelText
		{
			get
			{
				if (ChannelMode == MMChannelModes.MMChannel)
				{
					if (MMChannelDefinition == null)
					{
						return "None";
					}

					return MMChannelDefinition.name;
				}

				return "Channel "+Channel;
			}
		}

		// the timestamp at which this feedback was last played
		public virtual float FeedbackStartedAt => Application.isPlaying ? _lastPlayTimestamp : -1f;

		// the perceived duration of the feedback, to be used to display its progress bar, meant to be overridden with meaningful data by each feedback
		public virtual float FeedbackDuration
		{
			get { return 0f; }
			set {  }
		}

		/// <summary>
		/// Use this method to change the duration of this feedback
		/// </summary>
		/// <param name="newDuration"></param>
		public virtual void SetFeedbackDuration(float newDuration)
		{
			FeedbackDuration = newDuration;
			Owner.ComputeCachedTotalDuration();
		}

		/// whether or not this feedback is playing right now
		public virtual bool FeedbackPlaying =>
			((FeedbackStartedAt > 0f) && (Time.time - FeedbackStartedAt < FeedbackDuration));

		/// a ChannelData object, ready to pass to an event
		public virtual MMChannelData ChannelData => _channelData.Set(ChannelMode, Channel, MMChannelDefinition);
		
		public virtual bool InInitialDelay { get; set; }

		protected float _lastPlayTimestamp = -float.MaxValue;
		protected int _playsLeft;
		protected bool _initialized = false;
		protected Coroutine _playCoroutine;
		protected Coroutine _infinitePlayCoroutine;
		protected Coroutine _sequenceCoroutine;
		protected Coroutine _repeatedPlayCoroutine;
		protected bool _requiresSetup = false;
		protected string _requiredTarget = "";
		protected float _randomDurationMultiplier = 1f;
		protected int _sequenceTrackID = 0;
		protected float _beatInterval;
		protected bool BeatThisFrame = false;
		protected int LastBeatIndex = 0;
		protected int CurrentSequenceIndex = 0;
		protected float LastBeatTimestamp = 0f;
		protected MMChannelData _channelData;
		protected float _totalDuration = 0f;
		protected int _indexInOwnerFeedbackList = 0;
		protected string _requiredTargetTextCached = ".";
		protected string _requiredTargetTextCachedExtra = "";
		protected float _repeatOffset = 0f;

		#endregion Properties

		#region Initialization

		/// <summary>
		/// Runs at Awake, lets you preinitialize your custom feedback before Initialization
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="index"></param>
		public virtual void PreInitialization(MMF_Player owner, int index)
		{
			_channelData = new MMChannelData(ChannelMode, Channel, MMChannelDefinition);
		}

		/// <summary>
		/// Typically runs on Start, Initializes the feedback and its timing related variables
		/// </summary>
		/// <param name="owner"></param>
		public virtual void Initialization(MMF_Player owner, int index)
		{
			if (Timing == null)
			{
				Timing = new MMFeedbackTiming();
			}

			SetIndexInFeedbacksList(index);
			ResetCooldown();
			InInitialDelay = false;
			Timing.PlayCount = 0;
			_initialized = true;
			Owner = owner;
			_playsLeft = Timing.NumberOfRepeats + 1;
			_repeatOffset = 0f;
			_channelData = new MMChannelData(ChannelMode, Channel, MMChannelDefinition);
			AutomateTargetAcquisitionInternal();
			SetInitialDelay(Timing.InitialDelay);
			SetDelayBetweenRepeats(Timing.DelayBetweenRepeats);
			SetSequence(Timing.Sequence);
			CustomInitialization(owner);
		}

		/// <summary>
		/// Lets you specify at what index this feedback is in the list - use carefully (or don't use at all)
		/// </summary>
		/// <param name="index"></param>
		public virtual void SetIndexInFeedbacksList(int index)
		{
			_indexInOwnerFeedbackList = index;
		}

		/// <summary>
		/// Call this method (either directly or via the inspector button) to try and automatically setup this feedback's
		/// corresponding shaker in the scene
		/// </summary>
		public virtual void AutomaticShakerSetup()
		{
			
		}

		#endregion Initialization
		
		#region Automation
		
		/// <summary>
		/// Performs automated target acquisition, if needed
		/// </summary>
		protected virtual void AutomateTargetAcquisitionInternal()
		{
			if (!HasAutomatedTargetAcquisition)
			{
				return;
			}
			
			if (AutomatedTargetAcquisition == null)
			{
				AutomatedTargetAcquisition = new MMFeedbackTargetAcquisition();
			}

			if (AutomatedTargetAcquisition.Mode == MMFeedbackTargetAcquisition.Modes.None)
			{
				return;
			}

			AutomateTargetAcquisition();
			CacheRequiresSetup();
		}

		/// <summary>
		/// Lets you force target acquisition, outside of initialization where it usually occurs
		/// </summary>
		public virtual void ForceAutomateTargetAcquisition()
		{
			AutomateTargetAcquisition();
			CacheRequiresSetup();
		}

		/// <summary>
		/// A method meant to be implemented per feedback letting you specify what happens (usually setting a target)
		/// </summary>
		protected virtual void AutomateTargetAcquisition()
		{
			
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected virtual GameObject FindAutomatedTargetGameObject()
		{
			return MMFeedbackTargetAcquisition.FindAutomatedTargetGameObject(AutomatedTargetAcquisition, Owner, _indexInOwnerFeedbackList);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected virtual T FindAutomatedTarget<T>()
		{
			return MMFeedbackTargetAcquisition.FindAutomatedTarget<T>(AutomatedTargetAcquisition, Owner, _indexInOwnerFeedbackList);
		}
		
		#endregion Automation

		#region Play

		/// <summary>
		/// Plays the feedback
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		public virtual void Play(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active)
			{
				return;
			}

			if (!_initialized)
			{
				string feedbackName = this.ToString().Replace("MoreMountains.Feedbacks.", "");
				Debug.LogWarning("The " + feedbackName +
				                 " feedback on "+Owner.gameObject.name+" is being played without having been initialized. Always call the Initialization() method first. This can be done manually, or on Start or Awake (automatically on Start is the default). If you're auto playing your feedback on Start or on Enable, initialize on Awake (which runs before Start and Enable). You can change that setting on your MMF Player, unfold the Settings foldout at the top, and change the Initialization Mode.", Owner.gameObject);
			}

			// we check the cooldown
			if (InCooldown)
			{
				return;
			}

			if (Timing.InitialDelay > 0f)
			{
				_playCoroutine = Owner.StartCoroutine(PlayCoroutine(position, feedbacksIntensity));
			}
			else
			{
				RegularPlay(position, feedbacksIntensity);
			}
		}

		/// <summary>
		/// An internal coroutine delaying the initial play of the feedback
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		/// <returns></returns>
		protected virtual IEnumerator PlayCoroutine(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			InInitialDelay = true;
			yield return WaitFor(ApplyTimeMultiplier(Timing.InitialDelay));
			InInitialDelay = false;
			RegularPlay(position, feedbacksIntensity);
		}

		/// <summary>
		/// Triggers delaying coroutines if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected virtual void RegularPlay(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (Chance == 0f)
			{
				return;
			}

			if (Chance != 100f)
			{
				// determine the odds
				float random = Random.Range(0f, 100f);
				if (random > Chance)
				{
					return;
				}
			}
			
			if (Timing.LimitPlayCount && (Timing.PlayCount >= Timing.MaxPlayCount))
			{
				return;
			}

			if (Timing.UseIntensityInterval)
			{
				if ((feedbacksIntensity < Timing.IntensityIntervalMin) ||
				    (feedbacksIntensity >= Timing.IntensityIntervalMax))
				{
					return;
				}
			}
			
			_repeatOffset = 0f;

			if (Timing.RepeatForever)
			{
				_infinitePlayCoroutine = Owner.StartCoroutine(InfinitePlay(position, feedbacksIntensity));
				return;
			}

			if (Timing.NumberOfRepeats > 0)
			{
				_repeatedPlayCoroutine = Owner.StartCoroutine(RepeatedPlay(position, feedbacksIntensity));
				return;
			}

			if (Timing.Sequence == null)
			{
				TriggerCustomPlay(position, feedbacksIntensity);
			}
			else
			{
				_sequenceCoroutine = Owner.StartCoroutine(SequenceCoroutine(position, feedbacksIntensity));
			}
		}

		/// <summary>
		/// Triggers a custom play
		/// </summary>
		/// <param name="position"></param>
		/// <param name="intensity"></param>
		protected virtual void TriggerCustomPlay(Vector3 position, float intensity)
		{
			Timing.PlayCount++;
			_lastPlayTimestamp = FeedbackTime;
			CustomPlayFeedback(position, intensity);
		}

		/// <summary>
		/// Internal coroutine used for repeated play without end
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		/// <returns></returns>
		protected virtual IEnumerator InfinitePlay(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			while (true)
			{
				yield return TriggerRepeatedPlay(position, feedbacksIntensity);
			}
		}

		/// <summary>
		/// Internal coroutine used for repeated play
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		/// <returns></returns>
		protected virtual IEnumerator RepeatedPlay(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			while (_playsLeft > 0)
			{
				_playsLeft--;
				yield return TriggerRepeatedPlay(position, feedbacksIntensity);
			}

			_playsLeft = Timing.NumberOfRepeats + 1;
		}

		protected virtual IEnumerator TriggerRepeatedPlay(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (Timing.Sequence == null)
			{
				float time = InScaledTimescaleMode ? Time.time : Time.unscaledTime;
				TriggerCustomPlay(position, feedbacksIntensity);
				float repeatStartTime = time;
					
				float repeatDuration = Timing.DelayBetweenRepeats + FeedbackDuration;
				if (_repeatOffset <= Timing.DelayBetweenRepeats)
				{
					repeatDuration = Timing.DelayBetweenRepeats + FeedbackDuration - _repeatOffset;	
				}
				
				yield return WaitFor(repeatDuration);
				yield return null;
				time = InScaledTimescaleMode ? Time.time : Time.unscaledTime;
				_repeatOffset = (time - repeatStartTime - (Timing.DelayBetweenRepeats + FeedbackDuration));
			}
			else
			{
				_sequenceCoroutine = Owner.StartCoroutine(SequenceCoroutine(position, feedbacksIntensity));
				float delay = ApplyTimeMultiplier(Timing.DelayBetweenRepeats) + Timing.Sequence.Length;
				yield return WaitFor(delay);
			}
		}

		#endregion Play

		#region Sequence

		/// <summary>
		/// A coroutine used to play this feedback on a sequence
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		/// <returns></returns>
		protected virtual IEnumerator SequenceCoroutine(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			yield return null;
			float timeStartedAt = FeedbackTime;
			float lastFrame = FeedbackTime;

			BeatThisFrame = false;
			LastBeatIndex = 0;
			CurrentSequenceIndex = 0;
			LastBeatTimestamp = 0f;

			if (Timing.Quantized)
			{
				while (CurrentSequenceIndex < Timing.Sequence.QuantizedSequence[0].Line.Count)
				{
					_beatInterval = 60f / Timing.TargetBPM;

					if ((FeedbackTime - LastBeatTimestamp >= _beatInterval) || (LastBeatTimestamp == 0f))
					{
						BeatThisFrame = true;
						LastBeatIndex = CurrentSequenceIndex;
						LastBeatTimestamp = FeedbackTime;

						for (int i = 0; i < Timing.Sequence.SequenceTracks.Count; i++)
						{
							if (Timing.Sequence.QuantizedSequence[i].Line[CurrentSequenceIndex].ID == Timing.TrackID)
							{
								TriggerCustomPlay(position, feedbacksIntensity);
							}
						}

						CurrentSequenceIndex++;
					}

					yield return null;
				}
			}
			else
			{
				while (FeedbackTime - timeStartedAt < Timing.Sequence.Length)
				{
					foreach (MMSequenceNote item in Timing.Sequence.OriginalSequence.Line)
					{
						if ((item.ID == Timing.TrackID) && (item.Timestamp >= lastFrame) &&
						    (item.Timestamp <= FeedbackTime - timeStartedAt))
						{
							TriggerCustomPlay(position, feedbacksIntensity);
						}
					}

					lastFrame = FeedbackTime - timeStartedAt;
					yield return null;
				}
			}
		}

		/// <summary>
		/// Use this method to change this feedback's sequence at runtime
		/// </summary>
		/// <param name="newSequence"></param>
		public virtual void SetSequence(MMSequence newSequence)
		{
			Timing.Sequence = newSequence;
			if (Timing.Sequence != null)
			{
				for (int i = 0; i < Timing.Sequence.SequenceTracks.Count; i++)
				{
					if (Timing.Sequence.SequenceTracks[i].ID == Timing.TrackID)
					{
						_sequenceTrackID = i;
					}
				}
			}
		}

		#endregion Sequence

		#region Controls

		/// <summary>
		/// Stops all feedbacks from playing. Will stop repeating feedbacks, and call custom stop implementations
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		public virtual void Stop(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (_playCoroutine != null)
			{
				Owner.StopCoroutine(_playCoroutine);
			}

			if (_infinitePlayCoroutine != null)
			{
				Owner.StopCoroutine(_infinitePlayCoroutine);
			}

			if (_repeatedPlayCoroutine != null)
			{
				Owner.StopCoroutine(_repeatedPlayCoroutine);
			}

			if (_sequenceCoroutine != null)
			{
				Owner.StopCoroutine(_sequenceCoroutine);
			}

			_playsLeft = Timing.NumberOfRepeats + 1;
			_lastPlayTimestamp = -1f;
			
			if (Timing.InterruptsOnStop)
			{
				CustomStopFeedback(position, feedbacksIntensity);
			}
		}

		/// <summary>
		/// Called when skipping to the end of MMF_Player, calls custom Skip on all feedbacks
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		public virtual void SkipToTheEnd(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			CustomSkipToTheEnd(position, feedbacksIntensity);
		}

		/// <summary>
		/// Forces the feedback to set its initial value (behavior will change from feedback to feedback,
		/// but for example, a Position feedback that moves a Transform from point A to B would
		/// automatically move the Transform to point A when ForceInitialState is called
		/// </summary>
		public virtual void ForceInitialValue(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!CanForceInitialValue)
			{
				return;
			}
			if (ForceInitialValueDelayed)
			{
				Owner.StartCoroutine(ForceInitialValueDelayedCo(position, feedbacksIntensity));
			}
			else
			{
				Play(position, feedbacksIntensity);
				Stop(position, feedbacksIntensity);	
			}
		}

		/// <summary>
		/// A coroutine used to delay the Stop when forcing initial values (used mostly with shaker based feedbacks)
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		/// <returns></returns>
		protected virtual IEnumerator ForceInitialValueDelayedCo(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			Play(position, feedbacksIntensity);
			yield return new WaitForEndOfFrame();
			Stop(position, feedbacksIntensity);
			
		}

		/// <summary>
		/// Called when restoring the initial state of a player, calls custom Restore on all feedbacks
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		public virtual void RestoreInitialValues()
		{
			CustomRestoreInitialValues();
		}

		/// <summary>
		/// Calls this feedback's custom reset 
		/// </summary>
		public virtual void ResetFeedback()
		{
			_playsLeft = Timing.NumberOfRepeats + 1;
			if (Timing.SetPlayCountToZeroOnReset)
			{
				ResetPlayCount();
			}
			CustomReset();
		}

		/// <summary>
		/// Resets the cooldown for this feedback, allowing it to be played again instantly
		/// </summary>
		public virtual void ResetCooldown()
		{
			_lastPlayTimestamp = -float.MaxValue; 
		}

		/// <summary>
		/// This gets called by the MMF Player when all feedbacks have completed playing 
		/// </summary>
		public virtual void PlayerComplete()
		{
			CustomPlayerComplete();
		}

		#endregion

		#region Time

		/// <summary>
		/// Use this method to specify a new delay between repeats at runtime
		/// </summary>
		/// <param name="delay"></param>
		public virtual void SetDelayBetweenRepeats(float delay)
		{
			Timing.DelayBetweenRepeats = delay;
		}

		/// <summary>
		/// Use this method to specify a new initial delay at runtime
		/// </summary>
		/// <param name="delay"></param>
		public virtual void SetInitialDelay(float delay)
		{
			Timing.InitialDelay = delay;
		}

		/// <summary>
		/// Returns the t value at which to evaluate a curve at the end of this feedback's play time
		/// </summary>
		protected virtual float FinalNormalizedTime
		{
			get { return NormalPlayDirection ? 1f : 0f; }
		}

		/// <summary>
		/// Computes a new random duration multiplier
		/// </summary>
		public virtual void ComputeNewRandomDurationMultiplier()
		{
			_randomDurationMultiplier = Random.Range(RandomDurationMultiplier.x, RandomDurationMultiplier.y);
		}
		
		/// <summary>
		/// Resets the play count of this feedback
		/// </summary>
		public virtual void ResetPlayCount()
		{
			Timing.PlayCount = 0;
		}

		/// <summary>
		/// Applies the host MMFeedbacks' time multiplier to this feedback
		/// </summary>
		/// <param name="duration"></param>
		/// <returns></returns>
		protected virtual float ApplyTimeMultiplier(float duration)
		{
			if (Owner == null)
			{
				return 0f;
			}

			if (RandomizeDuration)
			{
				duration = duration * _randomDurationMultiplier;
			}

			return Owner.ApplyTimeMultiplier(duration);
		}

		/// <summary>
		/// Internal method used to wait for a duration, on scaled or unscaled time
		/// </summary>
		/// <param name="delay"></param>
		/// <returns></returns>
		protected virtual IEnumerator WaitFor(float delay)
		{
			if (InScaledTimescaleMode)
			{
				yield return MMFeedbacksCoroutine.WaitFor(delay);
			}
			else
			{
				yield return MMFeedbacksCoroutine.WaitForUnscaled(delay);
			}
		}

		/// <summary>
		/// Computes the total duration of this feedback
		/// </summary>
		public virtual void ComputeTotalDuration()
		{
			if ((Timing != null) && (!Timing.ContributeToTotalDuration))
			{
				_totalDuration = 0f;
				return;
			}

			float totalTime = 0f;

			if (Timing == null)
			{
				_totalDuration = 0f;
				return;
			}

			if (Timing.InitialDelay != 0)
			{
				totalTime += ApplyTimeMultiplier(Timing.InitialDelay);
			}

			totalTime += FeedbackDuration;

			if (Timing.NumberOfRepeats != 0)
			{
				float delayBetweenRepeats = ApplyTimeMultiplier(Timing.DelayBetweenRepeats);

				totalTime += Timing.NumberOfRepeats * (FeedbackDuration + delayBetweenRepeats);
			}
				
			_totalDuration = totalTime;
		}

		#endregion Time

		#region Direction

		/// <summary>
		/// Returns a new value of the normalized time based on the current play direction of this feedback
		/// </summary>
		/// <param name="normalizedTime"></param>
		/// <returns></returns>
		protected virtual float ApplyDirection(float normalizedTime)
		{
			return NormalPlayDirection ? normalizedTime : 1 - normalizedTime;
		}

		/// <summary>
		/// Returns true if this feedback should play normally, or false if it should play in rewind
		/// </summary>
		public virtual bool NormalPlayDirection
		{
			get
			{
				switch (Timing.PlayDirection)
				{
					case MMFeedbackTiming.PlayDirections.FollowMMFeedbacksDirection:
						return (Owner.Direction == MMF_Player.Directions.TopToBottom);
					case MMFeedbackTiming.PlayDirections.AlwaysNormal:
						return true;
					case MMFeedbackTiming.PlayDirections.AlwaysRewind:
						return false;
					case MMFeedbackTiming.PlayDirections.OppositeMMFeedbacksDirection:
						return !(Owner.Direction == MMF_Player.Directions.TopToBottom);
				}

				return true;
			}
		}

		/// <summary>
		/// Returns true if this feedback should play in the current parent MMFeedbacks direction, according to its MMFeedbacksDirectionCondition setting
		/// </summary>
		public virtual bool ShouldPlayInThisSequenceDirection
		{
			get
			{
				if (Timing == null)
				{
					return true;
				}
				switch (Timing.MMFeedbacksDirectionCondition)
				{
					case MMFeedbackTiming.MMFeedbacksDirectionConditions.Always:
						return true;
					case MMFeedbackTiming.MMFeedbacksDirectionConditions.OnlyWhenForwards:
						return (Owner.Direction == MMF_Player.Directions.TopToBottom);
					case MMFeedbackTiming.MMFeedbacksDirectionConditions.OnlyWhenBackwards:
						return (Owner.Direction == MMF_Player.Directions.BottomToTop);
				}

				return true;
			}
		}

		#endregion Direction

		#region Overrides

		/// <summary>
		/// This method describes all custom initialization processes the feedback requires, in addition to the main Initialization method
		/// </summary>
		/// <param name="owner"></param>
		protected virtual void CustomInitialization(MMF_Player owner) { }

		/// <summary>
		/// This method describes what happens when the feedback gets played
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected abstract void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f);

		/// <summary>
		/// This method describes what happens when the feedback gets stopped
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected virtual void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f) { }

		/// <summary>
		/// This method describes what happens when the feedback gets skipped to the end
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected virtual void CustomSkipToTheEnd(Vector3 position, float feedbacksIntensity = 1.0f) { }

		/// <summary>
		/// This method describes what happens when the feedback gets restored
		/// </summary>
		protected virtual void CustomRestoreInitialValues() { }
		/// <summary>
		/// This method describes what happens when the player this feedback belongs to completes playing
		/// </summary>
		protected virtual void CustomPlayerComplete() { }

		/// <summary>
		/// This method describes what happens when the feedback gets reset
		/// </summary>
		protected virtual void CustomReset() { }

		/// <summary>
		/// Use this method to initialize any custom attributes you may have
		/// </summary>
		public virtual void InitializeCustomAttributes()
		{
			if (HasAutomaticShakerSetup)
			{
				AutomaticShakerSetupButton = new MMF_Button("Automatic Shaker Setup", AutomaticShakerSetup);
			}
		}

		#endregion Overrides

		#region Event functions

		/// <summary>
		/// Triggered when a change happens in the inspector
		/// </summary>
		public virtual void OnValidate()
		{
			InitializeCustomAttributes();
			ComputeTotalDuration();
		}

		/// <summary>
		/// Triggered when the feedback gets added to the player
		/// </summary>
		public virtual void OnAddFeedback()
		{
			
		}

		/// <summary>
		/// Triggered when that feedback gets destroyed
		/// </summary>
		public virtual void OnDestroy() { }

		/// <summary>
		/// Triggered when the host MMF Player gets disabled
		/// </summary>
		public virtual void OnDisable() { }

		/// <summary>
		/// Triggered when the host MMF Player gets selected, can be used to draw gizmos
		/// </summary>
		public virtual void OnDrawGizmosSelectedHandler() { }

		#endregion
	}
}
