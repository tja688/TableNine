using UnityEngine;
#if MM_UI
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using System;
using MoreMountains.Tools;

namespace MoreMountains.Feedbacks
{
	public struct MMFlashEvent
	{
		static private event Delegate OnEvent;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }

		public delegate void Delegate(Color flashColor, float duration, float alpha, int flashID, MMChannelData channelData, TimescaleModes timescaleMode, bool stop = false);

		static public void Trigger(Color flashColor, float duration, float alpha, int flashID, MMChannelData channelData, TimescaleModes timescaleMode, bool stop = false)
		{
			OnEvent?.Invoke(flashColor, duration, alpha, flashID, channelData, timescaleMode, stop);
		}
	}

	[Serializable]
	public class MMFlashDebugSettings
	{
		/// whether to listen on a channel defined by an int or by a MMChannel scriptable object. Ints are simple to setup but can get messy and make it harder to remember what int corresponds to what.
		/// MMChannel scriptable objects require you to create them in advance, but come with a readable name and are more scalable
		[Tooltip("决定此抖动器是监听 `int` 定义的通道，还是监听 `MMChannel` ScriptableObject 定义的通道。`int` 配置简单，但项目一大就容易混乱，也不便记忆每个数字代表什么；`MMChannel` 需要预先创建资源，但名称更直观，也更适合扩展。")]
		public MMChannelModes ChannelMode = MMChannelModes.Int;
		/// 要监听的频道，必须与对应 feedback 上配置的频道一致。
		[Tooltip("要监听的通道，必须与触发它的反馈上配置的通道一致。")]
		[MMFEnumCondition("ChannelMode", (int)MMChannelModes.Int)]
		public int Channel = 0;
		/// the MMChannel definition asset to use to listen for events. The feedbacks targeting this shaker will have to reference that same MMChannel definition to receive events - to create a MMChannel,
		/// right click anywhere in your project (usually in a Data folder) and go MoreMountains > MMChannel, then name it with some unique name
		[Tooltip("用于监听事件的`通道资源`定义资源。只有引用同一个`通道资源`定义的反馈，才能触发这个增益器。若要创建`通道资源`，可以在项目视图中右键（通常放在数据文件夹中），选择更多山脉 >通道资源，并说明说明。")]
		[MMFEnumCondition("ChannelMode", (int)MMChannelModes.MMChannel)]
		public MMChannel MMChannelDefinition = null;
		/// the color of the flash
		public Color FlashColor = Color.white;
		/// the flash duration (in seconds)
		public float FlashDuration = 0.2f;
		/// the alpha of the flash
		public float FlashAlpha = 1f;
		/// the ID of the flash (usually 0). You can specify on each MMFlash object an ID, allowing you to have different flash images in one scene and call them separately (one for damage, one for health pickups, etc)
		public int FlashID = 0;
	}
    
	[RequireComponent(typeof(Image))]
	[RequireComponent(typeof(CanvasGroup))]
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Various/MM Flash")]
	/// <summary>
	/// Add this class to an image and it'll flash when getting a MMFlashEvent
	/// </summary>
	public class MMFlash : MMMonoBehaviour
	{
		[MMInspectorGroup("Flash", true, 121)] 
		/// whether to listen on a channel defined by an int or by a MMChannel scriptable object. Ints are simple to setup but can get messy and make it harder to remember what int corresponds to what.
		/// MMChannel scriptable objects require you to create them in advance, but come with a readable name and are more scalable
		[Tooltip("决定此抖动器是监听 `int` 定义的通道，还是监听 `MMChannel` ScriptableObject 定义的通道。`int` 配置简单，但项目一大就容易混乱，也不便记忆每个数字代表什么；`MMChannel` 需要预先创建资源，但名称更直观，也更适合扩展。")]
		public MMChannelModes ChannelMode = MMChannelModes.Int;
		/// 要监听的频道，必须与对应 feedback 上配置的频道一致。
		[Tooltip("要监听的通道，必须与触发它的反馈上配置的通道一致。")]
		[MMFEnumCondition("ChannelMode", (int)MMChannelModes.Int)]
		public int Channel = 0;
		/// the MMChannel definition asset to use to listen for events. The feedbacks targeting this shaker will have to reference that same MMChannel definition to receive events - to create a MMChannel,
		/// right click anywhere in your project (usually in a Data folder) and go MoreMountains > MMChannel, then name it with some unique name
		[Tooltip("用于监听事件的`通道资源`定义资源。只有引用同一个`通道资源`定义的反馈，才能触发这个增益器。若要创建`通道资源`，可以在项目视图中右键（通常放在数据文件夹中），选择更多山脉 >通道资源，并说明说明。")]
		[MMFEnumCondition("ChannelMode", (int)MMChannelModes.MMChannel)]
		public MMChannel MMChannelDefinition = null;
		/// the ID of this MMFlash object. When triggering a MMFlashEvent you can specify an ID, and only MMFlash objects with this ID will answer the call and flash, allowing you to have more than one flash object in a scene
		[Tooltip("此 `MMFlash` 对象的 ID。触发 `MMFlashEvent` 时可以指定一个 ID，只有 ID 相同的 `MMFlash` 才会响应并闪烁，因此你可以在同一个场景中放置多个独立的闪烁对象。")]
		public int FlashID = 0;
		/// if this is true, the MMFlash will stop before playing on every new event received
		[Tooltip("若启用，每次收到新的事件时，当前闪烁会先被停止，再重新开始播放。开启后，前一次尚未结束的闪烁将被中断。")]
		public bool Interruptable = false;
		
		[MMInspectorGroup("Interpolation", true, 122)] 
		/// the animation curve to use when flashing in
		[Tooltip("闪入阶段使用的曲线。")]
		public MMTweenType FlashInTween = new MMTweenType(MMTween.MMTweenCurve.LinearTween);
		/// the animation curve to use when flashing out
		[Tooltip("闪出阶段使用的曲线。")]
		public MMTweenType FlashOutTween = new MMTweenType(MMTween.MMTweenCurve.LinearTween);

		[MMInspectorGroup("Debug", true, 123)] 
		/// the set of test settings to use when pressing the DebugTest button
		[Tooltip("点击 `DebugTest` 按钮时所使用的一组测试设置。")]
		public MMFlashDebugSettings DebugSettings;
		/// a test button that calls the DebugTest method
		[Tooltip("调用 `调试测试` 方法的测试按钮。")]
		[MMFInspectorButton("DebugTest")]
		public bool DebugTestButton;

		public virtual float GetTime() { return (_timescaleMode == TimescaleModes.Scaled) ? Time.time : Time.unscaledTime; }
		public virtual float GetDeltaTime() { return (_timescaleMode == TimescaleModes.Scaled) ? Time.deltaTime : Time.unscaledDeltaTime; }

		protected Image _image;
		protected CanvasGroup _canvasGroup;
		protected bool _flashing = false;
		protected float _targetAlpha;
		protected Color _initialColor;
		protected float _delta;
		protected float _flashStartedTimestamp;
		protected int _direction = 1;
		protected float _duration;
		protected TimescaleModes _timescaleMode;
		protected MMTweenType _currentTween;

		/// <summary>
		/// On start we grab our image component
		/// </summary>
		protected virtual void Start()
		{
			_image = GetComponent<Image>();
			_canvasGroup = GetComponent<CanvasGroup>();
			_initialColor = _image.color;
		}

		/// <summary>
		/// On update we flash our image if needed
		/// </summary>
		protected virtual void Update()
		{
			if (_flashing)
			{
				_image.enabled = true;

				_currentTween = FlashInTween;
				if (GetTime() - _flashStartedTimestamp > _duration / 2f)
				{
					_direction = -1;
					_currentTween = FlashOutTween;
				}
				
				if (_direction == 1)
				{
					_delta += GetDeltaTime() / (_duration / 2f);
				}
				else
				{
					_delta -= GetDeltaTime() / (_duration / 2f);
				}
                
				if (GetTime() - _flashStartedTimestamp > _duration)
				{
					_flashing = false;
				}
				
				float percent = MMMaths.Remap(_delta, 0f, _duration/2f, 0f, 1f);
				float tweenValue = _currentTween.Evaluate(percent);

				_canvasGroup.alpha = Mathf.Lerp(0f, _targetAlpha, tweenValue);
			}
			else
			{
				_image.enabled = false;
			}
		}

		public virtual void DebugTest()
		{
			MMFlashEvent.Trigger(DebugSettings.FlashColor, DebugSettings.FlashDuration, DebugSettings.FlashAlpha, DebugSettings.FlashID, new MMChannelData(DebugSettings.ChannelMode, DebugSettings.Channel, DebugSettings.MMChannelDefinition), TimescaleModes.Unscaled);
		}

		/// <summary>
		/// When getting a flash event, we turn our image on
		/// </summary>
		public virtual void OnMMFlashEvent(Color flashColor, float duration, float alpha, int flashID, MMChannelData channelData, TimescaleModes timescaleMode, bool stop = false)
		{
			if (flashID != FlashID) 
			{
				return;
			}
            
			if (stop)
			{
				_flashing = false;
				return;
			}

			if (!MMChannel.Match(channelData, ChannelMode, Channel, MMChannelDefinition))
			{
				return;
			}

			Flash(flashColor, duration, alpha, timescaleMode);
		}

		public virtual void Flash(Color flashColor, float duration, float alpha, TimescaleModes timescaleMode)
		{
			if (_flashing && Interruptable)
			{
				_flashing = false;
			}

			if (!_flashing)
			{
				_flashing = true;
				_direction = 1;
				_canvasGroup.alpha = 0;
				_targetAlpha = alpha;
				_delta = 0f;
				_image.color = flashColor;
				_duration = duration;
				_timescaleMode = timescaleMode;
				_flashStartedTimestamp = GetTime();
			}
		}

		/// <summary>
		/// On enable we start listening for events
		/// </summary>
		protected virtual void OnEnable()
		{
			MMFlashEvent.Register(OnMMFlashEvent);
		}

		/// <summary>
		/// On disable we stop listening for events
		/// </summary>
		protected virtual void OnDisable()
		{
			MMFlashEvent.Unregister(OnMMFlashEvent);
		}		
	}
}
#endif