using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace  MoreMountains.Feedbacks
{
	/// <summary>
	/// Events triggered by a MMFeedbacks when playing a series of feedbacks
	/// - play : when a MMFeedbacks starts playing
	/// - pause : when a holding pause is met
	/// - resume : after a holding pause resumes
	/// - changeDirection : when a MMFeedbacks changes its play direction
	/// - complete : when a MMFeedbacks has played its last feedback
	///
	/// to listen to these events :
	///
	/// public virtual void OnMMFeedbacksEvent(MMFeedbacks source, EventTypes type)
	/// {
	///     // do something
	/// }
	/// 
	/// protected virtual void OnEnable()
	/// {
	///     MMFeedbacksEvent.Register(OnMMFeedbacksEvent);
	/// }
	/// 
	/// protected virtual void OnDisable()
	/// {
	///     MMFeedbacksEvent.Unregister(OnMMFeedbacksEvent);
	/// }
	/// 
	/// </summary>
	public struct MMFeedbacksEvent
	{
		static private event Delegate OnEvent;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }

		public enum EventTypes { Play, Pause, Resume, ChangeDirection, Complete, SkipToTheEnd, RestoreInitialValues, Loop, Enable, Disable, InitializationComplete, Stop }
		public delegate void Delegate(MMFeedbacks source, EventTypes type);
		static public void Trigger(MMFeedbacks source, EventTypes type)
		{
			OnEvent?.Invoke(source, type);
		}
	}
	
	/// <summary>
	/// An event used to set the RangeCenter on all feedbacks that listen for it
	/// </summary>
	public struct MMSetFeedbackRangeCenterEvent
	{
		static private event Delegate OnEvent;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }
		
		public delegate void Delegate(Transform newCenter);

		static public void Trigger(Transform newCenter)
		{
			OnEvent?.Invoke(newCenter);
		}
	}
	
	/// <summary>
	/// A subclass of MMFeedbacks, contains UnityEvents that can be played, 
	/// </summary>
	[Serializable]
	public class MMFeedbacksEvents
	{
		/// 是否让此 `MMFeedbacks` 触发 `MMFeedbacksEvents`。
		[Tooltip("是否让该`反馈组`触发`反馈组事件`。")] 
		public bool TriggerMMFeedbacksEvents = false; 
		/// 是否让此 `MMFeedbacks` 触发 Unity Events。
		[Tooltip("是否让这个`反馈组`触发统一事件。")] 
		public bool TriggerUnityEvents = true;
		/// 每次此 `MMFeedbacks` 被播放时都会触发此事件。
		[Tooltip("每次此 `MMFeedbacks` 被播放时都会触发此事件。")]
		public UnityEvent OnPlay;
		/// 每次此 `MMFeedbacks` 开始进入 holding pause 时都会触发此事件。
		[Tooltip("每次此 `MMFeedbacks` 开始进入 holding pause 时都会触发此事件。")]
		public UnityEvent OnPause;
		/// 每次通过 `StopFeedbacks` 方法停止此 `MMFeedbacks` 时都会触发此事件。
		[Tooltip("每次通过 `StopFeedbacks` 方法停止此 `MMFeedbacks` 时都会触发此事件。")]
		public UnityEvent OnStop;
		/// 每次此 `MMFeedbacks` 在 holding pause 后恢复执行时都会触发此事件。
		[Tooltip("每次此 `MMFeedbacks` 在 holding pause 后恢复执行时都会触发此事件。")]
		public UnityEvent OnResume;
		/// 每次此 `MMFeedbacks` 切换播放方向时都会触发此事件。
		[FormerlySerializedAs("OnRevert")] 
		[Tooltip("每次此 `MMFeedbacks` 切换播放方向时都会触发此事件。")]
		public UnityEvent OnChangeDirection;
		/// 每次此 `MMFeedbacks` 播放到最后一个 `MMFeedback` 时都会触发此事件。
		[Tooltip("每次此 `MMFeedbacks` 播放到最后一个 `MMFeedback` 时都会触发此事件。")]
		public UnityEvent OnComplete;
		/// 每次此 `MMFeedbacks` 恢复初始值时都会触发此事件。
		[Tooltip("每次此 `MMFeedbacks` 恢复初始值时都会触发此事件。")]
		public UnityEvent OnRestoreInitialValues;
		/// 每次此 `MMFeedbacks` 被跳到结尾时都会触发此事件。
		[Tooltip("每次此 `MMFeedbacks` 被跳到结尾时都会触发此事件。")]
		public UnityEvent OnSkipToTheEnd;
		/// `MMF_Player` 完成初始化后会触发此事件。
		[Tooltip("`MMF_Player` 完成初始化后会触发此事件。")]
		public UnityEvent OnInitializationComplete;
		/// 每次此 `MMFeedbacks` 所在 GameObject 被启用时都会触发此事件。
		[Tooltip("每次此 `MMFeedbacks` 所在 GameObject 被启用时都会触发此事件。")]
		public UnityEvent OnEnable;
		/// 每次此 `MMFeedbacks` 所在 GameObject 被禁用时都会触发此事件。
		[Tooltip("每次此 `MMFeedbacks` 所在 GameObject 被禁用时都会触发此事件。")]
		public UnityEvent OnDisable;

		public virtual bool OnPlayIsNull { get; protected set; }
		public virtual bool OnPauseIsNull { get; protected set; }
		public virtual bool OnResumeIsNull { get; protected set; }
		public virtual bool OnChangeDirectionIsNull { get; protected set; }
		public virtual bool OnCompleteIsNull { get; protected set; }
		public virtual bool OnRestoreInitialValuesIsNull { get; protected set; }
		public virtual bool OnSkipToTheEndIsNull { get; protected set; }
		public virtual bool OnInitializationCompleteIsNull { get; protected set; }
		public virtual bool OnEnableIsNull { get; protected set; }
		public virtual bool OnDisableIsNull { get; protected set; }
		public virtual bool OnStopIsNull { get; protected set; }

		/// <summary>
		/// On init we store for each event whether or not we have one to invoke
		/// </summary>
		public virtual void Initialization()
		{
			OnPlayIsNull = OnPlay == null;
			OnPauseIsNull = OnPause == null;
			OnResumeIsNull = OnResume == null;
			OnChangeDirectionIsNull = OnChangeDirection == null;
			OnCompleteIsNull = OnComplete == null;
			OnRestoreInitialValuesIsNull = OnRestoreInitialValues == null;
			OnSkipToTheEndIsNull = OnSkipToTheEnd == null;
			OnInitializationCompleteIsNull = OnInitializationComplete == null;
			OnEnableIsNull = OnEnable == null;
			OnDisableIsNull = OnDisable == null;
			OnStopIsNull = OnStop == null;
		}

		/// <summary>
		/// Fires Play events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnPlay(MMFeedbacks source)
		{
			if (!OnPlayIsNull && TriggerUnityEvents)
			{
				OnPlay.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.Play);
			}
		}

		/// <summary>
		/// Fires pause events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnPause(MMFeedbacks source)
		{
			if (!OnPauseIsNull && TriggerUnityEvents)
			{
				OnPause.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.Pause);
			}
		}

		/// <summary>
		/// Fires resume events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnResume(MMFeedbacks source)
		{
			if (!OnResumeIsNull && TriggerUnityEvents)
			{
				OnResume.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.Resume);
			}
		}

		/// <summary>
		/// Fires change direction events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnChangeDirection(MMFeedbacks source)
		{
			if (!OnChangeDirectionIsNull && TriggerUnityEvents)
			{
				OnChangeDirection.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.ChangeDirection);
			}
		}

		/// <summary>
		/// Fires complete events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnComplete(MMFeedbacks source)
		{
			if (!OnCompleteIsNull && TriggerUnityEvents)
			{
				OnComplete.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.Complete);
			}
		}

		/// <summary>
		/// Fires skip events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnSkipToTheEnd(MMFeedbacks source)
		{
			if (!OnSkipToTheEndIsNull && TriggerUnityEvents)
			{
				OnSkipToTheEnd.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.SkipToTheEnd);
			}
		}

		public virtual void TriggerOnInitializationComplete(MMFeedbacks source)
		{
			if (!OnInitializationCompleteIsNull && TriggerUnityEvents)
			{
				OnInitializationComplete.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.InitializationComplete);
			}
		}

		/// <summary>
		/// Fires restore initial values events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnRestoreInitialValues(MMFeedbacks source)
		{
			if (!OnRestoreInitialValuesIsNull && TriggerUnityEvents)
			{
				OnRestoreInitialValues.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.RestoreInitialValues);
			}
		}

		/// <summary>
		/// Fires enable events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnEnable(MMF_Player source)
		{
			if (!OnEnableIsNull && TriggerUnityEvents)
			{
				OnEnable.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.Enable);
			}
		}

		/// <summary>
		/// Fires disable events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnDisable(MMF_Player source)
		{
			if (!OnDisableIsNull && TriggerUnityEvents)
			{
				OnDisable.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.Disable);
			}
		}

		/// <summary>
		/// Fires stop events if needed
		/// </summary>
		/// <param name="source"></param>
		public virtual void TriggerOnStop(MMF_Player source)
		{
			if (!OnDisableIsNull && TriggerUnityEvents)
			{
				OnStop.Invoke();
			}

			if (TriggerMMFeedbacksEvents)
			{
				MMFeedbacksEvent.Trigger(source, MMFeedbacksEvent.EventTypes.Stop);
			}
		}
	}
   
}