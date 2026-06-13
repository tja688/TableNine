using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	/// the possible modes for the timescale
	public enum TimescaleModes { Scaled, Unscaled }

	/// <summary>
	/// A class collecting delay, cooldown and repeat values, to be used to define the behaviour of each MMFeedback
	/// </summary>
	[System.Serializable]
	public class MMFeedbackTiming
	{
		/// the possible ways this feedback can play based on the host MMFeedbacks' directions
		public enum MMFeedbacksDirectionConditions { Always, OnlyWhenForwards, OnlyWhenBackwards };
		/// the possible ways this feedback can play
		public enum PlayDirections { FollowMMFeedbacksDirection, OppositeMMFeedbacksDirection, AlwaysNormal, AlwaysRewind }

		[Header("Timescale")]
		/// 决定这里使用 `scaled time` 还是 `unscaled time`。
		[Tooltip("决定这里使用 `受时间缩放影响的时间` 还是 `不受时间缩放影响的时间`。")]
		public TimescaleModes TimescaleMode = TimescaleModes.Scaled;
        
		[Header("Exceptions")]
		/// 若启用，`Holding Pause` 不会等待这个 feedback 执行完毕。 
		[Tooltip("若启用，`Holding Pause` 不会等待这个反馈执行完毕。")]
		public bool ExcludeFromHoldingPauses = false;
		/// 是否将此 feedback 计入父级 `MMFeedbacks/MMF_Player` 的总时长。
		[Tooltip("是否将此反馈计入父级 `MMFeedbacks/MMF_Player` 的总持续时间。")]
		public bool ContributeToTotalDuration = true;

		[Header("Delays")]
		/// 正式播放前要施加的初始延迟，单位为秒。
		[Tooltip("正式播放前要施加的初始延迟，单位为秒。")]
		public float InitialDelay = 0f;
		/// 两次播放之间必须等待的冷却时长。
		[Tooltip("两次播放之间必须等待的冷却时长。")]
		public float CooldownDuration = 0f;

		[Header("Stop")]
		/// 若启用，当父级 `MMFeedbacks` 调用 `Stop` 时，此 feedback 会被中断；否则它会继续执行到结束。
		[Tooltip("若启用，当父级 `MMFeedbacks` 调用 `Stop` 时，此反馈会被中断；否则它会继续执行到结束。")]
		public bool InterruptsOnStop = true;

		[Header("Repeat")]
		/// 重复播放设置，决定此 feedback 是只播放一次、重复多次，还是无限循环。
		[Tooltip("重复播放设置，决定此反馈是只播放一次、重复多次，还是无限循环。")]
		public int NumberOfRepeats = 0;
		/// 若启用，此 feedback 会无限重复播放。
		[Tooltip("若启用，此反馈会无限重复播放。")]
		public bool RepeatForever = false;
		/// 两次触发此 feedback 之间的间隔，单位为秒。注意：这里不包含 feedback 自身的持续时间。 
		[Tooltip("两次触发此反馈之间的间隔，单位为秒。注意：这里不包含反馈自身的持续时间。")]
		public float DelayBetweenRepeats = 1f;

		[Header("PlayCount")]
		/// 自初始化以来，此 feedback 已经播放的次数；若 `SetPlayCountToZeroOnReset` 为 true，则按最近一次重置后开始计数。 
		[Tooltip("自初始化以来，此反馈已经播放的次数；若 `SetPlayCountToZeroOnReset` 为 true，则按最近一次重置后开始计数。")]
		[MMFReadOnly]
		public int PlayCount = 0;
		/// 是否限制此 feedback 的最大播放次数。达到上限后将不再播放。 
		[Tooltip("是否限制此反馈的最大播放次数。达到上限后将不再播放。")]
		public bool LimitPlayCount = false;
		/// 当 `LimitPlayCount` 为 true 时，此 feedback 允许播放的最大次数。
		[Tooltip("当 `LimitPlayCount` 为 true 时，此反馈允许播放的最大次数。")]
		[MMFCondition("LimitPlayCount", true)]
		public int MaxPlayCount = 3;
		/// 当 `LimitPlayCount` 为 true 时，决定在重置 feedback 时是否把播放计数清零。
		[Tooltip("当 `LimitPlayCount` 为 true 时，决定在重置反馈时是否把播放计数清零。")]
		[MMFCondition("LimitPlayCount", true)]
		public bool SetPlayCountToZeroOnReset = false;
		
		[Header("Play Direction")]
		/// this defines how this feedback should play when the host MMFeedbacks is played :
		/// - always (default) : this feedback will always play
		/// - OnlyWhenForwards : this feedback will only play if the host MMFeedbacks is played in the top to bottom direction (forwards)
		/// - OnlyWhenBackwards : this feedback will only play if the host MMFeedbacks is played in the bottom to top direction (backwards)
		[Tooltip("- 规定当假设`反馈组`播放时，此反馈在什么条件下会执行： - 始终（默认）：此反馈最会播放。 - 仅当假设`反馈组`以前进方向（从上到下）播放时才会执行。 - 最多后退时：仅当假设`反馈组`以后退方向（从下到上）播放时才会执行。")]
		public MMFeedbacksDirectionConditions MMFeedbacksDirectionCondition = MMFeedbacksDirectionConditions.Always;
		/// this defines the way this feedback will play. It can play in its normal direction, or in rewind (a sound will play backwards, 
		/// an object normally scaling up will scale down, a curve will be evaluated from right to left, etc)
		/// - BasedOnMMFeedbacksDirection : will play normally when the host MMFeedbacks is played forwards, in rewind when it's played backwards
		/// - OppositeMMFeedbacksDirection : will play in rewind when the host MMFeedbacks is played forwards, and normally when played backwards
		/// - Always Normal : will always play normally, regardless of the direction of the host MMFeedbacks
		/// - Always Rewind : will always play in rewind, regardless of the direction of the host MMFeedbacks
		[Tooltip("定义此反馈的播放。既可以按正常播放，也可以倒放（如声音会倒放、未知放大的物体会缩小、轮廓会从右往左读取等）。 - 紧接着反馈方向组：补充`反馈组`正向播放时方向正常执行，反向播放时方向倒放。 - 与反馈方向组则：补充`反馈组`正向播放时倒放，向后播放时正常执行。始终倒放：无论前进方向如何，总是按倒放方向播放。")]
		public PlayDirections PlayDirection = PlayDirections.FollowMMFeedbacksDirection;

		[Header("Intensity")]
		/// 若启用，即使父级 `MMFeedbacks` 以较低强度播放，此 feedback 仍会以恒定强度执行。
		[Tooltip("若启用，即使父级 `MMFeedbacks` 以较低强度播放，此反馈仍会以恒定强度执行。")]
		public bool ConstantIntensity = false;
		/// 若启用，只有当当前强度 `>= IntensityIntervalMin` 且 `< IntensityIntervalMax` 时，此 feedback 才会播放。
		[Tooltip("若启用，只有当当前强度 `>= IntensityIntervalMin` 且 `< IntensityIntervalMax` 时，此反馈才会播放。")]
		public bool UseIntensityInterval = false;
		/// 此 feedback 允许播放所需的最小强度。
		[Tooltip("此反馈允许播放所需的最小强度。")]
		[MMFCondition("UseIntensityInterval", true)]
		public float IntensityIntervalMin = 0f;
		/// 此 feedback 允许播放的最大强度上限。
		[Tooltip("此反馈允许播放的最大强度上限。")]
		[MMFCondition("UseIntensityInterval", true)]
		public float IntensityIntervalMax = 0f;

		[Header("Sequence")]
		/// 用于播放这些 feedback 的 `MMSequence`。
		[Tooltip("使用这些播放器反馈的`反馈序列`。")]
		public MMSequence Sequence;
		/// 要使用的 `MMSequence` 轨道 ID。
		[Tooltip("要使用‘反馈序列’的轨道编号。")]
		public int TrackID = 0;
		/// 是否使用目标序列的量化版本。
		[Tooltip("是否使用目标序列的量化版本。")]
		public bool Quantized = false;
		/// 若使用目标序列的量化版本，这里定义播放该序列时采用的 BPM。
		[Tooltip("若使用目标序列的量化版本，这里定义播放该序列时采用的 BPM。")]
		[MMFCondition("Quantized", true)]
		public int TargetBPM = 120;
		
		/// from any class, you can set UseScriptDrivenTimescale:true, from there, instead of looking at Time.time, Time.deltaTime (or their unscaled equivalents), this feedback will compute time based on the values you feed them via ScriptDrivenDeltaTime and ScriptDrivenTime
		public virtual bool UseScriptDrivenTimescale { get; set; }
		/// the value this feedback should use for delta time
		public virtual float ScriptDrivenDeltaTime { get; set; }
		/// the value this feedback should use for time
		public virtual float ScriptDrivenTime { get; set; }
	}
}