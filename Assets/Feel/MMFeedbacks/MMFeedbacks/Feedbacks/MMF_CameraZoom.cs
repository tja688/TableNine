using MoreMountains.FeedbacksForThirdParty;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// A feedback that will allow you to change the zoom of a (3D) camera when played
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("Define zoom properties : For will set the zoom to the specified parameters for a certain 持续时间, " +
	              "Set will leave them like that forever. Zoom properties include the field of view, 持续时间of the " +
	              "zoom transition (单位为秒) and the zoom 持续时间 (the time the camera should remain zoomed in, 单位为秒). " +
	              "For this to work, you'll need to add a MMCameraZoom component to your Camera, or a MMCinemachineZoom if you're " +
	              "using virtual cameras.")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Camera/Camera Zoom")]
	public class MMF_CameraZoom : MMF_Feedback
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

		/// the duration of this feedback is the duration of the zoom
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(ZoomDuration); } set { ZoomDuration = value; } }
		public override bool HasChannel => true;
		public override bool CanForceInitialValue => true;

		[MMFInspectorGroup("Camera Zoom", true, 72)]
		/// the zoom mode (for : forward for TransitionDuration, static for Duration, backwards for TransitionDuration)
		[Tooltip("缩放模式（对于：向前为 TransitionDuration，静态为 Duration，向后为 TransitionDuration）")]
		public MMCameraZoomModes ZoomMode = MMCameraZoomModes.For;
		/// the target field of view
		[Tooltip("目标视野角")]
		public float ZoomFieldOfView = 30f;
		/// the zoom transition duration
		[Tooltip("扩展继承 持续时间")]
		public float ZoomTransitionDuration = 0.05f;
		/// the duration for which the zoom is at max zoom
		[Tooltip("变焦处于最大变焦时的持续时间")]
		public float ZoomDuration = 0.1f;
		/// whether or not ZoomFieldOfView should add itself to the current camera's field of view value
		[Tooltip("ZoomFieldOfView 是否应将自身添加到当前相机的视野值中")]
		public bool RelativeFieldOfView = false;
		[Header("Transition Speed")]
		/// the animation curve to apply to the zoom transition
		[Tooltip("应用于缩放过渡的动画曲线")]
		public MMTweenType ZoomTween = new MMTweenType( new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f)));

		/// <summary>
		/// On Play, triggers a zoom event
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			MMCameraZoomEvent.Trigger(ZoomMode, ZoomFieldOfView, ZoomTransitionDuration, FeedbackDuration, ChannelData, 
				ComputedTimescaleMode == TimescaleModes.Unscaled, false, RelativeFieldOfView, tweenType: ZoomTween);
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
			MMCameraZoomEvent.Trigger(ZoomMode, ZoomFieldOfView, ZoomTransitionDuration, FeedbackDuration, ChannelData, 
				ComputedTimescaleMode == TimescaleModes.Unscaled, stop:true, tweenType: ZoomTween);
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
			MMCameraZoomEvent.Trigger(ZoomMode, ZoomFieldOfView, ZoomTransitionDuration, FeedbackDuration, ChannelData, 
				ComputedTimescaleMode == TimescaleModes.Unscaled, restore:true, tweenType: ZoomTween);
		}
		
		/// <summary>
		/// Automatically tries to add a MMCameraZoom to the main camera, if none are present
		/// </summary>
		public override void AutomaticShakerSetup()
		{
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			bool virtualCameraFound = false;
			#endif
			
			#if MMCINEMACHINE 
				CinemachineVirtualCamera virtualCamera = (CinemachineVirtualCamera)Object.FindObjectOfType(typeof(CinemachineVirtualCamera));
				virtualCameraFound = (virtualCamera != null);
			#elif MMCINEMACHINE3
				CinemachineCamera virtualCamera = (CinemachineCamera)Object.FindObjectOfType(typeof(CinemachineCamera));
				virtualCameraFound = (virtualCamera != null);
			#endif
			
			#if MM_CINEMACHINE || MM_CINEMACHINE3
			if (virtualCameraFound)
			{
				MMCinemachineHelpers.AutomaticCinemachineShakersSetup(Owner, "CinemachineImpulse");
				return;
			}
			#endif
			
			MMCameraZoom camZoom = (MMCameraZoom)Object.FindAnyObjectByType(typeof(MMCameraZoom));
			if (camZoom != null)
			{
				return;
			}

			Camera.main.gameObject.MMGetOrAddComponent<MMCameraZoom>(); 
			MMDebug.DebugLogInfo( "Added a MMCameraZoom to the main camera. You're all set.");
		}
	}
}
