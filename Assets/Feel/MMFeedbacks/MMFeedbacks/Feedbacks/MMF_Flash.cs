using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
#if MM_UI
using UnityEngine.UI;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will trigger a flash event (to be caught by a MMFlash) when played
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("播放时，此反馈会发送 MMFlashEvent。场景中的 MMFlash 会按 FlashID/Channel 过滤后响应并执行闪白。你可以在此设置闪光颜色、时长、Alpha 与 FlashID。若填写了 TargetFlash，则会直接调用该目标，不再通过事件广播；此时 FlashID/Channel 对这次触发不再起筛选作用。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Camera/Flash")]
	public class MMF_Flash : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.CameraColor; } }
		public override string RequiredTargetText => RequiredChannelText;
		public override bool HasCustomInspectors => true;
		public override bool HasAutomaticShakerSetup => true;
		#endif
		/// the duration of this feedback is the duration of the flash
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(FlashDuration); } set { FlashDuration = value; } }
		public override bool HasChannel => true;
		public override bool HasRandomness => true;

		[MMFInspectorGroup("Flash", true, 37)]
		/// the color of the flash
		[Tooltip("闪光颜色")]
		public Color FlashColor = Color.white;
		/// the flash duration (in seconds)
		[Tooltip("闪光持续时间（秒）")]
		public float FlashDuration = 0.2f;
		/// the alpha of the flash
		[Tooltip("闪光 透明度（透明度）")]
		public float FlashAlpha = 1f;
		/// the ID of the flash (usually 0). You can specify on each MMFlash object an ID, allowing you to have different flash images in one scene and call them separately (one for damage, one for health pickups, etc)
		[Tooltip("闪光 ID（通常为 0）。需与目标 MMFlash 的 ID 一致才会响应。可用于在同一场景区分多组闪光（如受伤、回血等）。")]
		public int FlashID = 0;

		[Header("Optional Target")] 
		/// this field lets you bind a specific MMFlash to this feedback. If left empty, the feedback will trigger a MMFlashEvent instead, targeting all matching flashes. If you fill it, only that specific MMFlash will be targeted.
		[Tooltip("可绑定一个特定的MM闪存。留空时将广播MM闪存事件，并由所有匹配闪存编号/渠道的MM闪存响应；填写后只需触发该目标，则即覆盖广播路径。")]
		public MMFlash TargetFlash;

		/// <summary>
		/// On Play we trigger a flash event
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
			if (TargetFlash != null)
			{
				TargetFlash.Flash(FlashColor, FlashDuration * intensityMultiplier, FlashAlpha, ComputedTimescaleMode);
			}
			else
			{
				MMFlashEvent.Trigger(FlashColor, FeedbackDuration * intensityMultiplier, FlashAlpha, FlashID, ChannelData, ComputedTimescaleMode);	
			}
		}

		/// <summary>
		/// On stop we stop our transition
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
			MMFlashEvent.Trigger(FlashColor, FeedbackDuration, FlashAlpha, FlashID, ChannelData, ComputedTimescaleMode, stop:true);
		}
		
		/// <summary>
		/// On restore, we restore our initial state
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			MMFlashEvent.Trigger(FlashColor, FeedbackDuration, FlashAlpha, FlashID, ChannelData, ComputedTimescaleMode, stop:true);
		}
		
		/// <summary>
		/// Automatically tries to add a MMFlash setup to the scene
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			if (Object.FindAnyObjectByType<MMFlash>() != null)
			{
				return;
			}
			
			(Canvas canvas, bool createdNewCanvas) = Owner.gameObject.MMFindOrCreateObjectOfType<Canvas>("FlashCanvas", null);
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			(Image image, bool createdNewImage) = canvas.gameObject.MMFindOrCreateObjectOfType<Image>("FlashImage", canvas.transform, true);
			image.raycastTarget = false;
			image.color = Color.white;
			
			RectTransform rectTransform = image.GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			
			image.gameObject.AddComponent<MMFlash>();
			image.gameObject.GetComponent<CanvasGroup>().alpha = 0;
			image.gameObject.GetComponent<CanvasGroup>().interactable = false;

			MMDebug.DebugLogInfo("Added a MMFlash to the scene. You're all set.");
		}
	}
}
#endif
