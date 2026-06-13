using UnityEngine;

namespace MoreMountains.Tools
{	
	[RequireComponent(typeof(CanvasGroup))]
	[AddComponentMenu("More Mountains/Tools/Controls/MM Touch Controls")]
	public class MMTouchControls : MonoBehaviour 
	{
		public enum InputForcedMode { None, Mobile, Desktop }
		[MMInformation("如果勾选 Auto Mobile Detection，当构建目标是 Android 或 iOS 时，系统会自动切换到移动端控制。你也可以通过下方下拉框强制使用移动端或桌面端（键盘、手柄）控制。\n注意：如果你并不需要移动端控制和/或 GUI，这个组件也可以独立工作，只需把它挂到一个空的 GameObject 上即可。", MMInformationAttribute.InformationType.Info,false)]
		/// If you check Auto Mobile Detection, the engine will automatically switch to mobile controls when your build target is Android or iOS. 
		/// You can also force mobile or desktop (keyboard, gamepad) controls using the dropdown below.Note that if you don't need mobile controls 
		/// and/or GUI this component can also work on its own, just put it on an empty GameObject instead.
		[Tooltip("如果勾选 Auto Mobile Detection，当构建目标是 Android 或 iOS 时，系统会自动切换到移动端控制。" +
		         "你也可以通过下方下拉框强制使用移动端或桌面端（键盘、手柄）控制。注意：如果你并不需要移动端控制 " +
		         "和/或 GUI，这个组件也可以独立工作，只需把它挂到一个空的 GameObject 上即可。")]
		public bool AutoMobileDetection = true;
		/// Force desktop mode (gamepad, keyboard...) or mobile (touch controls) 
		[Tooltip("强制使用桌面模式（手柄、键盘等）或移动模式（触控控制）")]
		public InputForcedMode ForcedMode;
		public virtual bool IsMobile { get; protected set; }

		protected CanvasGroup _canvasGroup;
		protected float _initialMobileControlsAlpha;

		/// <summary>
		/// We get the player from its tag.
		/// </summary>
		protected virtual void Start()
		{
			_canvasGroup = GetComponent<CanvasGroup>();

			_initialMobileControlsAlpha = _canvasGroup.alpha;
			SetMobileControlsActive(false);
			IsMobile=false;
			if (AutoMobileDetection)
			{
				#if UNITY_ANDROID || UNITY_IPHONE
					SetMobileControlsActive(true);
					IsMobile = true;
				#endif
			}
			if (ForcedMode==InputForcedMode.Mobile)
			{
				SetMobileControlsActive(true);
				IsMobile = true;
			}
			if (ForcedMode==InputForcedMode.Desktop)
			{
				SetMobileControlsActive(false);
				IsMobile = false;		
			}
		}
		
		/// <summary>
		/// Use this method to enable or disable mobile controls
		/// </summary>
		/// <param name="state"></param>
		public virtual void SetMobileControlsActive(bool state)
		{
			if (_canvasGroup!=null)
			{
				_canvasGroup.gameObject.SetActive(state);
				if (state)
				{
					_canvasGroup.alpha=_initialMobileControlsAlpha;
				}
				else
				{
					_canvasGroup.alpha=0;
				}
			}
		}
	}
}
