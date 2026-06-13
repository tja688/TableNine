using System;
using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
#if MM_POSTPROCESSING
using UnityEngine.Rendering.PostProcessing;
#endif 

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// Use this class to have a global PP volume auto blend its weight on cue, between a start and end values
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/PostProcessing/MM Global Post Processing Volume Auto Blend")]
	#if MM_POSTPROCESSING
	[RequireComponent(typeof(PostProcessVolume))]
	#endif
	public class MMGlobalPostProcessingVolumeAutoBlend : MonoBehaviour, MMEventListener<MMPostProcessingVolumeAutoBlendShakeEvent>
	{
		/// the possible timescales this blend can operate on
		public enum TimeScales { Scaled, Unscaled }
		/// the possible blend trigger modes 
		public enum BlendTriggerModes { OnEnable, Script }
		
		[Header("Channel")]
		/// whether to listen on a channel defined by an int or by a MMChannel scriptable object. Ints are simple to setup but can get messy and make it harder to remember what int corresponds to what.
		/// MMChannel scriptable objects require you to create them in advance, but come with a readable name and are more scalable
		[Tooltip("是由监听整数定义的通道，还是监听由通道资源脚本化对象定义的通道。 密码配置简单，但项目变大后容易混乱，也更难记住每个数字对应的含义。 通道资源脚本化对象需要预先创建，但具有相同的名称，也更适合扩展。")]
		public MMChannelModes ChannelMode = MMChannelModes.Int;
		/// the channel to listen to - has to match the one on the feedback
		[Tooltip("要监听的通道，必须与反馈上配置的通道一致")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.Int)]
		public int Channel = 0;
		/// the MMChannel definition asset to use to listen for events. The feedbacks targeting this shaker will have to reference that same MMChannel definition to receive events - to create a MMChannel,
		/// right click anywhere in your project (usually in a Data folder) and go MoreMountains > MMChannel, then name it with some unique name
		[Tooltip("要触发此通道器的输入也必须引用同一个通道资源定义，否则将无法接收到事件。要创建通道资源，请在工程任何位置（通常是数据文件夹）右键，选择 更多山脉 > 通道资源，然后说明命名一个名称。")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.MMChannel)]
		public MMChannel MMChannelDefinition = null;

		[Header("Blend")]
		/// the trigger mode for this MMGlobalPostProcessingVolumeAutoBlend
		[Tooltip("此 MM全局后处理体积自动混合 的触发模式")]
		public BlendTriggerModes BlendTriggerMode = BlendTriggerModes.Script;
		/// the duration of the blend (in seconds)
		[Tooltip("混合持续时间（秒）")]
		public float BlendDuration = 1f;
		/// the curve to use to blend
		[Tooltip("用于混合的曲线")]
		public AnimationCurve Curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1f));

		[Header("Weight")]
		/// the weight at the start of the blend
		[Tooltip("混合开始时的权重")]
		[Range(0f, 1f)]
		public float InitialWeight = 0f;
		/// the desired weight at the end of the blend
		[Tooltip("混合结束时的目标权重")]
		[Range(0f, 1f)]
		public float FinalWeight = 1f;

		[Header("Behaviour")]
		/// the timescale to operate on
		[Tooltip("使用哪种时间尺度")]
		public TimeScales TimeScale = TimeScales.Unscaled;
		/// whether or not the associated volume should be disabled at 0
		[Tooltip("当权重为 0 时是否禁用关联的 Volume")]
		public bool DisableVolumeOnZeroWeight = true;
		/// whether or not this blender should disable itself at 0
		[Tooltip("当权重为 0 时此混合器是否禁用自身")]
		public bool DisableSelfAfterEnd = true;
		/// whether or not this blender can be interrupted
		[Tooltip("此混合器是否允许被打断")]
		public bool Interruptable = true;
		/// whether or not this blender should pick the current value as its starting point
		[Tooltip("是否以当前值作为混合起点")]
		public bool StartFromCurrentValue = true;
		/// reset to initial value on end 
		[Tooltip("结束时重置回初始值")]
		public bool ResetToInitialValueOnEnd = false;

		[Header("Tests")]
		/// test blend button
		[Tooltip("测试混合按钮")]
		[MMFInspectorButton("Blend")]
		public bool TestBlend;
		/// test blend back button
		[Tooltip("测试反向混合按钮")]
		[MMFInspectorButton("BlendBack")]
		public bool TestBlendBackwards;

		/// <summary>
		/// Returns the correct timescale based on the chosen settings
		/// </summary>
		/// <returns></returns>
		protected float GetTime()
		{
			return (TimeScale == TimeScales.Unscaled) ? Time.unscaledTime : Time.time;
		}

		protected float _initial;
		protected float _destination;
		protected float _startTime;
		protected bool _blending = false;
		#if MM_POSTPROCESSING
		protected PostProcessVolume _volume;
		
		/// <summary>
		/// On Awake we store our volume
		/// </summary>
		protected virtual void Awake()
		{
			#if MM_POSTPROCESSING
			_volume = this.gameObject.GetComponent<PostProcessVolume>();
			_volume.weight = InitialWeight;
			this.MMEventStartListening<MMPostProcessingVolumeAutoBlendShakeEvent>();
			#endif
		}
		
		/// <summary>
		/// On start we start blending if needed
		/// </summary>
		protected virtual void OnEnable()
		{
			if ((BlendTriggerMode == BlendTriggerModes.OnEnable) && !_blending)
			{
				Blend();
			}
		}

		/// <summary>
		/// Blends the volume's weight from the initial value to the final one
		/// </summary>
		public virtual void Blend()
		{
			if (_blending && !Interruptable)
			{
				return;
			}
			_initial = StartFromCurrentValue ? _volume.weight : InitialWeight;
			_destination = FinalWeight;
			StartBlending();
		}

		/// <summary>
		/// Blends the volume's weight from the final value to the initial one
		/// </summary>
		public virtual void BlendBack()
		{
			if (_blending && !Interruptable)
			{
				return;
			}
			_initial = StartFromCurrentValue ? _volume.weight : FinalWeight;
			_destination = InitialWeight;
			StartBlending();
		}

		/// <summary>
		/// Internal method used to start blending
		/// </summary>
		protected virtual void StartBlending()
		{
			_startTime = GetTime();
			_blending = true;
			this.enabled = true;
			if (DisableVolumeOnZeroWeight)
			{
				_volume.enabled = true;
			}
		}

		/// <summary>
		/// Stops any blending that may be in progress
		/// </summary>
		public virtual void StopBlending()
		{
			_blending = false;
		}

		/// <summary>
		/// On update, processes the blend if needed
		/// </summary>
		protected virtual void Update()
		{
			if (!_blending)
			{
				return;
			}

			float timeElapsed = (GetTime() - _startTime);
			if (timeElapsed < BlendDuration)
			{                
				float remapped = MMFeedbacksHelpers.Remap(timeElapsed, 0f, BlendDuration, 0f, 1f);
				_volume.weight = Mathf.LerpUnclamped(_initial, _destination, Curve.Evaluate(remapped));
			}
			else
			{
				// after end is reached
				_volume.weight = ResetToInitialValueOnEnd ? _initial : _destination;
				_blending = false;
				if (DisableVolumeOnZeroWeight && (_volume.weight == 0f))
				{
					_volume.enabled = false;
				}
				if (DisableSelfAfterEnd)
				{
					this.enabled = false;
				}
			}            
		}
	
		/// <summary>
		/// Restores the volume's weight to its initial value
		/// </summary>
		public virtual void RestoreInitialValues()
		{
			_volume.weight = _initial;
		}

		#endif
		
		/// <summary>
		/// When we catch a MMPostProcessingVolumeAutoBlendShakeEvent, we start blending
		/// </summary>
		/// <param name="eventType"></param>
		/// <exception cref="NotImplementedException"></exception>
		public void OnMMEvent(MMPostProcessingVolumeAutoBlendShakeEvent shakeEvent)
		{
			
			#if MM_POSTPROCESSING
			if (shakeEvent.TargetAutoBlend != null)
			{
				if (!shakeEvent.TargetAutoBlend.Equals(this))
				{
					return;
				}
			}
			else
			{
				bool eventMatch = shakeEvent.ChannelData != null && MMChannel.Match(shakeEvent.ChannelData, ChannelMode, Channel, MMChannelDefinition);
				if (!eventMatch)
				{
					return;
				}
			}
			
			if (shakeEvent.Mode == MMF_GlobalPPVolumeAutoBlend.Modes.Default)
			{
				if (!shakeEvent.NormalPlayDirection)
				{
					if (shakeEvent.BlendAction == MMF_GlobalPPVolumeAutoBlend.Actions.Blend)
					{
						BlendBack();
						return;
					}
					if (shakeEvent.BlendAction == MMF_GlobalPPVolumeAutoBlend.Actions.BlendBack)
					{
						Blend();
						return;
					}
				}
				else
				{
					if (shakeEvent.BlendAction == MMF_GlobalPPVolumeAutoBlend.Actions.Blend)
					{
						Blend();
						return;
					}
					if (shakeEvent.BlendAction == MMF_GlobalPPVolumeAutoBlend.Actions.BlendBack)
					{
						BlendBack();
						return;
					}    
				}
			}
			else
			{
				BlendDuration = shakeEvent.BlendDuration;
				Curve = shakeEvent.BlendCurve;
				TimeScale = shakeEvent.TimeScale;
				if (!shakeEvent.NormalPlayDirection)
				{
					InitialWeight = shakeEvent.FinalWeight;
					FinalWeight = shakeEvent.InitialWeight;   
				}
				else
				{
					InitialWeight = shakeEvent.InitialWeight;
					FinalWeight = shakeEvent.FinalWeight;    
				}
				ResetToInitialValueOnEnd = shakeEvent.ResetToInitialValueOnEnd;
				Blend();
			}
			#endif
		}

		/// <summary>
		/// On Destroy, we stop listening for events
		/// </summary>
		protected void OnDestroy()
		{
			this.MMEventStopListening<MMPostProcessingVolumeAutoBlendShakeEvent>();
		}
	}
	
	/// <summary>
	/// An event used to trigger vignette shakes
	/// </summary>
	public struct MMPostProcessingVolumeAutoBlendShakeEvent
	{
		static MMPostProcessingVolumeAutoBlendShakeEvent e;
		
		public MMChannelData ChannelData;
		public MMGlobalPostProcessingVolumeAutoBlend TargetAutoBlend;
		public MMF_GlobalPPVolumeAutoBlend.Modes Mode;
		public MMF_GlobalPPVolumeAutoBlend.Actions BlendAction;
		public float BlendDuration;
		public AnimationCurve BlendCurve;
		public float InitialWeight;
		public float FinalWeight;
		public bool ResetToInitialValueOnEnd;
		public bool NormalPlayDirection;
		public MMGlobalPostProcessingVolumeAutoBlend.TimeScales TimeScale;

		public static void Trigger(
			MMChannelData channelData,
			MMGlobalPostProcessingVolumeAutoBlend targetAutoBlend,
			MMF_GlobalPPVolumeAutoBlend.Modes mode,
			MMF_GlobalPPVolumeAutoBlend.Actions blendAction,
			float blendDuration,
			AnimationCurve blendCurve,
			float initialWeight,
			float finalWeight,
			bool resetToInitialValueOnEnd,
			bool normalPlayDirection,
			MMGlobalPostProcessingVolumeAutoBlend.TimeScales timeScale)
		{
			e.ChannelData = channelData;
			e.TargetAutoBlend = targetAutoBlend;
			e.Mode = mode;
			e.BlendAction = blendAction;
			e.BlendDuration = blendDuration;
			e.BlendCurve = blendCurve;
			e.InitialWeight = initialWeight;
			e.FinalWeight = finalWeight;
			e.ResetToInitialValueOnEnd = resetToInitialValueOnEnd;
			e.NormalPlayDirection = normalPlayDirection;
			e.TimeScale = timeScale;
			MMEventManager.TriggerEvent(e);
		}
	}	
}