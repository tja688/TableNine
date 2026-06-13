using UnityEngine;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will let you change the sprite of a target SpriteRenderer
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你切换目标 SpriteRenderer 的 Sprite。")]
	[System.Serializable]
	[FeedbackPath("Renderer/Sprite")]
	public class MMF_Sprite : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.UIColor; } }
		public override bool EvaluateRequiresSetup() { return (BoundSpriteRenderer == null); }
		public override string RequiredTargetText { get { return BoundSpriteRenderer != null ? BoundSpriteRenderer.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈需要先指定 BoundSpriteRenderer 才能正常工作，可在下方设置。"; } }
		#endif

		public override float FeedbackDuration => 0f;
		public override bool HasChannel => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => BoundSpriteRenderer = FindAutomatedTarget<SpriteRenderer>();

		[MMFInspectorGroup("Sprite", true, 54, true)]
		/// the SpriteRenderer to affect when playing the feedback
		[Tooltip("播放该反馈时要作用的 SpriteRenderer")]
		public SpriteRenderer BoundSpriteRenderer;
		/// the Sprite to apply to the BoundSpriteRenderer when this feedback plays
		[Tooltip("播放此反馈时要应用到 BoundSpriteRenderer 的新 Sprite。")]
		public Sprite NewSprite;
		
		protected Sprite _initialSprite;

		/// <summary>
		/// On init we store our initial sprite
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			if (Active)
			{
				if (BoundSpriteRenderer == null)
				{
					Debug.LogWarning("[Sprite Feedback] The Sprite feedback on "+Owner.name+" doesn't have a BoundSpriteRenderer, it won't work. You need to specify a Sprite Renderer in its inspector.");
				}
				else
				{
					_initialSprite = BoundSpriteRenderer.sprite;
				}
			}
		}

		/// <summary>
		/// On Play we change our sprite
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (BoundSpriteRenderer == null))
			{
				return;
			}
			
			SetSprite(NormalPlayDirection ? NewSprite : _initialSprite);
		}

		/// <summary>
		/// Sets the sprite on the BoundSpriteRenderer
		/// </summary>
		/// <param name="newSprite"></param>
		protected virtual void SetSprite(Sprite newSprite)
		{
			BoundSpriteRenderer.sprite = newSprite;
		}

		/// <summary>
		/// Called on stop
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			IsPlaying = false;
		}
		
		/// <summary>
		/// On restore, we restore our initial sprite
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			SetSprite(_initialSprite);
		}
	}
}
