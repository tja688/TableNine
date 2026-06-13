using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
#if MM_UI
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you control the texture offset of a target UI Image over time
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你随时间控制目标 UI Image 的纹理偏移。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("UI/Image Texture Offset")]
	public class MMF_ImageTextureOffset : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetImage == null); }
		public override string RequiredTargetText { get { return TargetImage != null ? TargetImage.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈需要先指定 TargetImage 才能正常工作，可在下方设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetImage = FindAutomatedTarget<Image>();

		/// the possible modes for this feedback
		public enum Modes { OverTime, Instant }
		//
		public enum MaterialPropertyTypes { Main, TextureID }

		[MMFInspectorGroup("Texture Scale", true, 63, true)]
		/// the UI Image on which to change texture offset on
		[Tooltip("要修改纹理偏移的目标 UI Image。")]
		public Image TargetImage;
		/// whether to target the main texture property, or one specified in MaterialPropertyName
		[Tooltip("属性来源：`Main` 使用主纹理属性；`TextureID` 使用 `MaterialPropertyName` 指定的属性。")]
		public MaterialPropertyTypes MaterialPropertyType = MaterialPropertyTypes.Main;
		/// the property name, for example _MainTex_ST, or _MainTex if you don't have UseMaterialPropertyBlocks set to true
		[Tooltip("仅在 `TextureID` 模式下生效的材质属性名，例如 `_MainTex_ST`。")]
		[MMEnumCondition("MaterialPropertyType", (int)MaterialPropertyTypes.TextureID)]
		public string MaterialPropertyName = "_MainTex_ST";
		/// whether the feedback should affect the material instantly or over a period of time
		[Tooltip("作用模式：`Instant` 立即设置偏移；`OverTime` 在 `Duration` 内按曲线变化。")]
		public Modes Mode = Modes.OverTime;
		/// how long the material should change over time
		[Tooltip("在 `OverTime` 模式下的变化时长（秒）。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public float Duration = 0.2f;
		/// whether or not the values should be relative 
		[Tooltip("若开启，偏移值将基于初始值叠加；若关闭，则直接使用计算结果覆盖。")]
		public bool RelativeValues = true;
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("若开启此项，即使该反馈仍在执行中，再次调用也会立即触发；若关闭此项，在当前播放结束前将阻止新的 Play 调用")] 
		public bool AllowAdditivePlays = false;
        
		[MMFInspectorGroup("Intensity", true, 65)]
		/// the curve to tween the offset on
		[Tooltip("在 `OverTime` 模式下用于驱动偏移变化的曲线。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		public AnimationCurve OffsetCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0));
		/// the value to remap the offset curve's 0 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip("将曲线值 0 重映射到的偏移范围（Min/Max）。若不需要随机，请将 Min 与 Max 设为相同值。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		[MMFVector("Min", "Max")]
		public Vector2 RemapZero = Vector2.zero;
		/// the value to remap the offset curve's 1 to, randomized between its min and max - put the same value in both min and max if you don't want any randomness
		[Tooltip("将曲线值 1 重映射到的偏移范围（Min/Max）。若不需要随机，请将 Min 与 Max 设为相同值。")]
		[MMFEnumCondition("Mode", (int)Modes.OverTime)]
		[MMFVector("Min", "Max")]
		public Vector2 RemapOne = Vector2.one;
		/// the value to move the intensity to in instant mode
		[Tooltip("在 Instant 模式下要将强度设置到的值")]
		[MMFEnumCondition("Mode", (int)Modes.Instant)]
		public Vector2 InstantOffset;

		protected Vector2 _initialValue;
		protected Coroutine _coroutine;
		protected Vector2 _newValue;
		protected Material _material;

		/// the duration of this feedback is the duration of the transition
		public override float FeedbackDuration { get { return (Mode == Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { Duration = value; } }
		public override bool HasRandomness => true;

		/// <summary>
		/// On init we store our initial texture offset
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			
			if (TargetImage == null)
			{
				Debug.LogWarning("[Image Texture Offset Feedback] The image texture offset feedback on "+Owner.name+" doesn't have a TargetImage, it won't work. You need to specify an Image in its inspector.");
				return;
			}

			_material = TargetImage.materialForRendering;

			switch (MaterialPropertyType)
			{
				case MaterialPropertyTypes.Main:
					_initialValue = _material.mainTextureOffset;
					break;
				case MaterialPropertyTypes.TextureID:
					_initialValue = _material.GetTextureOffset(MaterialPropertyName);
					break;
			}
		}

		/// <summary>
		/// On Play we initiate our offset change
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetImage == null))
			{
				return;
			}
            
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
            
			switch (Mode)
			{
				case Modes.Instant:
					if (NormalPlayDirection)
					{
						ApplyValue(InstantOffset * intensityMultiplier);	
					}
					else
					{
						ApplyValue(_initialValue);
					}
					break;
				case Modes.OverTime:
					if (!AllowAdditivePlays && (_coroutine != null))
					{
						return;
					}
					if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
					_coroutine = Owner.StartCoroutine(TransitionCo(intensityMultiplier));
					break;
			}
		}

		/// <summary>
		/// This coroutine will modify the offset value over time
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator TransitionCo(float intensityMultiplier)
		{
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				SetMaterialValues(remappedTime, intensityMultiplier);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetMaterialValues(FinalNormalizedTime, intensityMultiplier);
			IsPlaying = false;
			_coroutine = null;
			yield return null;
		}

		/// <summary>
		/// Applies offset to the target material 
		/// </summary>
		/// <param name="time"></param>
		protected virtual void SetMaterialValues(float time, float intensityMultiplier)
		{
			_newValue.x = MMFeedbacksHelpers.Remap(OffsetCurve.Evaluate(time), 0f, 1f, RemapZero.x, RemapOne.x);
			_newValue.y = MMFeedbacksHelpers.Remap(OffsetCurve.Evaluate(time), 0f, 1f, RemapZero.y, RemapOne.y);

			if (RelativeValues)
			{
				_newValue += _initialValue;
			}

			ApplyValue(_newValue * intensityMultiplier);
		}

		/// <summary>
		/// Applies the specified value to the material
		/// </summary>
		/// <param name="newValue"></param>
		protected virtual void ApplyValue(Vector2 newValue)
		{
			switch (MaterialPropertyType)
			{
				case MaterialPropertyTypes.Main:
					_material.mainTextureOffset = newValue;
					break;
				case MaterialPropertyTypes.TextureID:
					_material.SetTextureOffset(MaterialPropertyName, newValue);
					break;
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
	}
}
#endif
