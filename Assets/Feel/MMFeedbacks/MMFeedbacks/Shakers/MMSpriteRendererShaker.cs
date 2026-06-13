using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// Add this to a SpriteRenderer to have it receive MMSpriteRendererShakeEvents from feedbacks or to shake it locally
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Renderer/MM Sprite Renderer Shaker")]
	[RequireComponent(typeof(SpriteRenderer))]
	public class MMSpriteRendererShaker : MMShaker
	{
		[MMInspectorGroup("SpriteRenderer", true, 39)]
		/// the SpriteRenderer to affect when playing the feedback
		[Tooltip("播放反馈时要作用的`精灵渲染器`。")]
		public SpriteRenderer BoundSpriteRenderer;
		/// whether or not that SpriteRenderer should be turned off on start
		[Tooltip("若启用，在开始时会先关闭该 `SpriteRenderer`。")]
		public bool StartsOff = true;

		[MMInspectorGroup("Color", true, 40)]
		/// whether or not this shaker should modify color 
		[Tooltip("此抖动器是否修改颜色。")]
		public bool ModifyColor = true;
		/// the colors to apply to the SpriteRenderer over time
		[Tooltip("随时间应用到 `SpriteRenderer` 上的颜色。")]
		public Gradient ColorOverTime;

		[MMInspectorGroup("Flip", true, 41)]
		/// whether or not to flip the sprite on X
		[Tooltip("是否在 X 轴上翻转精灵。")]
		public bool FlipX = false;
		/// whether or not to flip the sprite on Y
		[Tooltip("是否在 Y 轴上翻转精灵。")]
		public bool FlipY = false;

		protected Color _initialColor;
		protected bool _originalModifyColor;
		protected float _originalShakeDuration;
		protected Gradient _originalColorOverTime;
		protected bool _originalFlipX;
		protected bool _originalFlipY;

		/// <summary>
		/// On init we initialize our values
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			if (BoundSpriteRenderer == null)
			{
				BoundSpriteRenderer = this.gameObject.GetComponent<SpriteRenderer>();
			}
		}

		/// <summary>
		/// When that shaker gets added, we initialize its shake duration
		/// </summary>
		protected virtual void Reset()
		{
			ShakeDuration = 1f;
		}
               
		/// <summary>
		/// Shakes values over time
		/// </summary>
		protected override void Shake()
		{
			if (ModifyColor)
			{
				_remappedTimeSinceStart = MMFeedbacksHelpers.Remap(Time.time - _shakeStartedTimestamp, 0f, ShakeDuration, 0f, 1f);
				BoundSpriteRenderer.color = ColorOverTime.Evaluate(_remappedTimeSinceStart);
			}            
		}

		/// <summary>
		/// Collects initial values on the target
		/// </summary>
		protected override void GrabInitialValues()
		{
			_initialColor = BoundSpriteRenderer.color;
			_originalFlipX = BoundSpriteRenderer.flipX;
			_originalFlipY = BoundSpriteRenderer.flipY;
		}

		/// <summary>
		/// Resets the target's values
		/// </summary>
		protected override void ResetTargetValues()
		{
			base.ResetTargetValues();
			BoundSpriteRenderer.color = _initialColor;
			BoundSpriteRenderer.flipX = _originalFlipX;
			BoundSpriteRenderer.flipY = _originalFlipY;
		}

		/// <summary>
		/// Resets the shaker's values
		/// </summary>
		protected override void ResetShakerValues()
		{
			base.ResetShakerValues();
			ModifyColor = _originalModifyColor;
			ShakeDuration = _originalShakeDuration;
			ColorOverTime = _originalColorOverTime;
		}

		/// <summary>
		/// Starts listening for events
		/// </summary>
		public override void StartListening()
		{
			base.StartListening();
			MMSpriteRendererShakeEvent.Register(OnMMSpriteRendererShakeEvent);
		}

		/// <summary>
		/// Stops listening for events
		/// </summary>
		public override void StopListening()
		{
			base.StopListening();
			MMSpriteRendererShakeEvent.Unregister(OnMMSpriteRendererShakeEvent);
		}

		public virtual void OnMMSpriteRendererShakeEvent(float shakeDuration, bool modifyColor, Gradient colorOverTime,
			bool flipX, bool flipY,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true,
			bool useRange = false, float eventRange = 0f, Vector3 eventOriginPosition = default(Vector3))
		{
			if (!CheckEventAllowed(channelData, useRange, eventRange, eventOriginPosition) ||  (!Interruptible && Shaking))
			{
				return;
			}

			_resetShakerValuesAfterShake = resetShakerValuesAfterShake;
			_resetTargetValuesAfterShake = resetTargetValuesAfterShake;

			if (resetShakerValuesAfterShake)
			{
				_originalModifyColor = ModifyColor;
				_originalShakeDuration = ShakeDuration;
				_originalColorOverTime = ColorOverTime;
			}

			if (!OnlyUseShakerValues)
			{
				ModifyColor = modifyColor;
				ShakeDuration = shakeDuration;
				ColorOverTime = colorOverTime;
				FlipX = flipX;
				FlipY = flipY;
			}

			if (FlipX)
			{
				BoundSpriteRenderer.flipX = !BoundSpriteRenderer.flipX;
			}
			if (FlipY)
			{
				BoundSpriteRenderer.flipY = !BoundSpriteRenderer.flipY;
			}            

			Play();
		}
	}

	/// <summary>
	/// An event used (usually from MMFeeedbackSpriteRenderer) to shake the values of a SpriteRenderer
	/// </summary>
	public struct MMSpriteRendererShakeEvent
	{
		static private event Delegate OnEvent;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void RuntimeInitialization() { OnEvent = null; }
		static public void Register(Delegate callback) { OnEvent += callback; }
		static public void Unregister(Delegate callback) { OnEvent -= callback; }

		public delegate void Delegate(float shakeDuration, bool modifyColor, Gradient colorOverTime,
			bool flipX, bool flipY,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true,
			bool useRange = false, float eventRange = 0f, Vector3 eventOriginPosition = default(Vector3));

		static public void Trigger(float shakeDuration, bool modifyColor, Gradient colorOverTime,
			bool flipX, bool flipY,
			float feedbacksIntensity = 1.0f, MMChannelData channelData = null, bool resetShakerValuesAfterShake = true, bool resetTargetValuesAfterShake = true,
			bool useRange = false, float eventRange = 0f, Vector3 eventOriginPosition = default(Vector3))
		{
			OnEvent?.Invoke(shakeDuration, modifyColor, colorOverTime,
				flipX, flipY,
				feedbacksIntensity, channelData, resetShakerValuesAfterShake, resetTargetValuesAfterShake, 
				useRange, eventRange, eventOriginPosition);
		}
	}
}