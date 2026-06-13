#if MM_UI
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MoreMountains.Tools
{
	/// <summary>
	/// Add this component to a GUI Image to have it act as a button. 
	/// Bind pressed down, pressed continually and released actions to it from the inspector
	/// Handles mouse and multi touch
	/// </summary>
	[RequireComponent(typeof(Rect))]
	[RequireComponent(typeof(CanvasGroup))]
	[AddComponentMenu("More Mountains/Tools/Controls/MM Touch Button")]
	public class MMTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler, ISubmitHandler
	{
		[Header("Interaction")] 
		/// whether or not this button can be interacted with
		public bool Interactable = true;
		
		/// The different possible states for the button : 
		/// Off (default idle state), ButtonDown (button pressed for the first time), ButtonPressed (button being pressed), ButtonUp (button being released), Disabled (unclickable but still present on screen)
		/// ButtonDown and ButtonUp will only last one frame, the others will last however long you press them / disable them / do nothing
		public enum ButtonStates { Off, ButtonDown, ButtonPressed, ButtonUp, Disabled }
		[Header("Binding")]
		/// The method(s) to call when the button gets pressed down
		[Tooltip("按钮首次被按下时要调用的方法")]
		public UnityEvent ButtonPressedFirstTime;
		/// The method(s) to call when the button gets released
		[Tooltip("按钮释放时要调用的方法")]
		public UnityEvent ButtonReleased;
		/// The method(s) to call while the button is being pressed
		[Tooltip("按钮持续按下期间要调用的方法")]
		public UnityEvent ButtonPressed;

		[Header("Sprite Swap")]
		[MMInformation("你可以在这里为禁用和按下状态分别指定不同的 Sprite 与颜色。", MMInformationAttribute.InformationType.Info,false)]
		/// the sprite to use on the button when it's in the disabled state
		[Tooltip("按钮处于禁用状态时使用的 Sprite")]
		public Sprite DisabledSprite;
		/// whether or not to change color when the button is disabled
		[Tooltip("按钮禁用时是否切换颜色")]
		public bool DisabledChangeColor = false;
		/// the color to use when the button is disabled
		[Tooltip("按钮禁用时使用的颜色")]
		[MMCondition("DisabledChangeColor", true)]
		public Color DisabledColor = Color.white;
		/// the sprite to use on the button when it's in the pressed state
		[Tooltip("按钮处于按下状态时使用的 Sprite")]
		public Sprite PressedSprite;
		/// whether or not to change the button color on press
		[Tooltip("按钮按下时是否切换颜色")]
		public bool PressedChangeColor = false;
		/// the color to use when the button is pressed
		[Tooltip("按钮按下时使用的颜色")]
		[MMCondition("PressedChangeColor", true)]
		public Color PressedColor= Color.white;
		/// the sprite to use on the button when it's in the highlighted state
		[Tooltip("按钮处于高亮状态时使用的 Sprite")]
		public Sprite HighlightedSprite;
		/// whether or not to change color when highlighting the button
		[Tooltip("按钮高亮时是否切换颜色")]
		public bool HighlightedChangeColor = false;
		/// the color to use when the button is highlighted 
		[Tooltip("按钮高亮时使用的颜色")]
		[MMCondition("HighlightedChangeColor", true)]
		public Color HighlightedColor = Color.white;

		[Header("Opacity")]
		[MMInformation("你可以在这里分别设置按钮在按下、空闲、禁用状态下的透明度，用于增强视觉反馈。",MMInformationAttribute.InformationType.Info,false)]
		/// the new opacity to apply to the canvas group when the button is pressed
		[Tooltip("按钮按下时要应用到 CanvasGroup 的透明度")]
		public float PressedOpacity = 1f;
		/// the new opacity to apply to the canvas group when the button is idle
		[Tooltip("按钮空闲时要应用到 CanvasGroup 的透明度")]
		public float IdleOpacity = 1f;
		/// the new opacity to apply to the canvas group when the button is disabled
		[Tooltip("按钮禁用时要应用到 CanvasGroup 的透明度")]
		public float DisabledOpacity = 1f;

		[Header("Delays")]
		[MMInformation("在这里指定按钮首次按下和释放时要附加的延迟。通常保持为 0 即可。",MMInformationAttribute.InformationType.Info,false)]
		/// the delay to apply to events when the button gets pressed for the first time
		[Tooltip("按钮首次按下时，事件触发前要等待的延迟")]
		public float PressedFirstTimeDelay = 0f;
		/// the delay to apply to events when the button gets released
		[Tooltip("按钮释放时，事件触发前要等待的延迟")]
		public float ReleasedDelay = 0f;

		[Header("Buffer")]
		/// the duration (in seconds) after a press during which the button can't be pressed again
		[Tooltip("每次按下后按钮不可再次按下的缓冲持续时间（秒）")]
		public float BufferDuration = 0f;

		[Header("Animation")]
		[MMInformation("你可以在这里绑定 Animator，并为各个状态指定对应的动画参数名。",MMInformationAttribute.InformationType.Info,false)]
		/// an animator you can bind to this button to have its states updated to reflect the button's states
		[Tooltip("可绑定到此按钮的 Animator；绑定后会随按钮状态同步更新")]
		public Animator Animator;
		/// the name of the animation parameter to turn true when the button is idle
		[Tooltip("按钮空闲时要设为 true 的动画参数名")]
		public string IdleAnimationParameterName = "Idle";
		/// the name of the animation parameter to turn true when the button is disabled
		[Tooltip("按钮禁用时要设为 true 的动画参数名")]
		public string DisabledAnimationParameterName = "Disabled";
		/// the name of the animation parameter to turn true when the button is pressed
		[Tooltip("按钮按下时要设为 true 的动画参数名")]
		public string PressedAnimationParameterName = "Pressed";

		[Header("Mouse Mode")]
		[MMInformation("如果将此项设为 true，必须真的按下按钮才会触发；否则仅仅悬停就会触发。若以触控输入为主，通常建议不要勾选。", MMInformationAttribute.InformationType.Info,false)]
		/// If you set this to true, you'll need to actually press the button for it to be triggered, otherwise a simple hover will trigger it (better for touch input).
		[Tooltip("如果将此项设为 true，必须真的按下按钮才会触发；否则仅仅悬停就会触发。若以触控输入为主，通常建议关闭此项。")]
		public bool MouseMode = false;

		public bool PreventLeftClick = false;
		public bool PreventMiddleClick = true;
		public bool PreventRightClick = true;

		public virtual bool ReturnToInitialSpriteAutomatically { get; set; }

		/// the current state of the button (off, down, pressed or up)
		public virtual ButtonStates CurrentState { get; protected set; }

		public event System.Action<PointerEventData.FramePressState, PointerEventData> ButtonStateChange;

		protected bool _zonePressed = false;
		protected CanvasGroup _canvasGroup;
		protected float _initialOpacity;
		protected Animator _animator;
		protected Image _image;
		protected Sprite _initialSprite;
		protected Color _initialColor;
		protected float _lastClickTimestamp = 0f;
		protected Selectable _selectable;

		/// <summary>
		/// On Start, we get our canvasgroup and set our initial alpha
		/// </summary>
		protected virtual void Awake()
		{
			Initialization ();
		}

		/// <summary>
		/// On init we grab our Image, Animator and CanvasGroup and set them up
		/// </summary>
		protected virtual void Initialization()
		{
			ReturnToInitialSpriteAutomatically = true;

			_selectable = GetComponent<Selectable> ();

			_image = GetComponent<Image> ();
			if (_image != null)
			{
				_initialColor = _image.color;
				_initialSprite = _image.sprite;
			}

			_animator = GetComponent<Animator> ();
			if (Animator != null)
			{
				_animator = Animator;
			}

			_canvasGroup = GetComponent<CanvasGroup>();
			if (_canvasGroup!=null)
			{
				_initialOpacity = IdleOpacity;
				_canvasGroup.alpha = _initialOpacity;
				_initialOpacity = _canvasGroup.alpha;
			}
			ResetButton();
		}

		/// <summary>
		/// Every frame, if the touch zone is pressed, we trigger the OnPointerPressed method, to detect continuous press
		/// </summary>
		protected virtual void Update()
		{
			switch (CurrentState)
			{
				case ButtonStates.Off:
					SetOpacity (IdleOpacity);
					if ((_image != null) && (ReturnToInitialSpriteAutomatically))
					{
						_image.color = _initialColor;
						_image.sprite = _initialSprite;
					}
					if (_selectable != null)
					{
						_selectable.interactable = true;
						if (EventSystem.current.currentSelectedGameObject == this.gameObject)
						{
							if ((_image != null) && HighlightedChangeColor)
							{
								_image.color = HighlightedColor;
							}
							if (HighlightedSprite != null)
							{
								_image.sprite = HighlightedSprite;
							}
						}
					}
					break;

				case ButtonStates.Disabled:
					SetOpacity (DisabledOpacity);
					if (_image != null)
					{
						if (DisabledSprite != null)
						{
							_image.sprite = DisabledSprite;	
						}
						if (DisabledChangeColor)
						{
							_image.color = DisabledColor;	
						}
					}
					if (_selectable != null)
					{
						_selectable.interactable = false;
					}
					break;

				case ButtonStates.ButtonDown:

					break;

				case ButtonStates.ButtonPressed:
					SetOpacity (PressedOpacity);
					OnPointerPressed();
					if (_image != null)
					{
						if (PressedSprite != null)
						{
							_image.sprite = PressedSprite;
						}
						if (PressedChangeColor)
						{
							_image.color = PressedColor;	
						}
					}
					break;

				case ButtonStates.ButtonUp:

					break;
			}
			UpdateAnimatorStates ();
		}

		/// <summary>
		/// At the end of every frame, we change our button's state if needed
		/// </summary>
		protected virtual void LateUpdate()
		{
			if (CurrentState == ButtonStates.ButtonUp)
			{
				CurrentState = ButtonStates.Off;
			}
			if (CurrentState == ButtonStates.ButtonDown)
			{
				CurrentState = ButtonStates.ButtonPressed;
			}
		}

		/// <summary>
		/// Triggers the ButtonStateChange event for the specified state
		/// </summary>
		/// <param name="newState"></param>
		/// <param name="data"></param>
		public virtual void InvokeButtonStateChange(PointerEventData.FramePressState newState, PointerEventData data)
		{
			ButtonStateChange?.Invoke(newState, data);
		}

		/// <summary>
		/// Checks whether or not the specified click is allowed, if in mouse mode
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected virtual bool AllowedClick(PointerEventData data)
		{
			if (!MouseMode)
			{
				return true;
			}
			if (PreventLeftClick && data.button == PointerEventData.InputButton.Left)
			{
				return false;
			}
			if (PreventMiddleClick && data.button == PointerEventData.InputButton.Middle)
			{
				return false;
			}
			if (PreventRightClick && data.button == PointerEventData.InputButton.Right)
			{
				return false;
			}
			return true;
		}
			
		/// <summary>
		/// Triggers the bound pointer down action
		/// </summary>
		public virtual void OnPointerDown(PointerEventData data)
		{
			if (!Interactable)
			{
				return;
			}

			if (!AllowedClick(data))
			{
				return;
			}
			
			if (Time.unscaledTime - _lastClickTimestamp < BufferDuration)
			{
				return;
			}

			if (CurrentState != ButtonStates.Off)
			{
				return;
			}
			CurrentState = ButtonStates.ButtonDown;
			_lastClickTimestamp = Time.unscaledTime;
			InvokeButtonStateChange(PointerEventData.FramePressState.Pressed, data);
			if ((Time.timeScale != 0) && (PressedFirstTimeDelay > 0))
			{
				Invoke ("InvokePressedFirstTime", PressedFirstTimeDelay);	
			}
			else
			{
				ButtonPressedFirstTime.Invoke();
			}
		}
		
		/// <summary>
		/// Raises the ButtonPressedFirstTime event
		/// </summary>
		protected virtual void InvokePressedFirstTime()
		{
			if (ButtonPressedFirstTime!=null)
			{
				ButtonPressedFirstTime.Invoke();
			}
		}

		/// <summary>
		/// Triggers the bound pointer up action
		/// </summary>
		public virtual void OnPointerUp(PointerEventData data)
		{
			if (!Interactable)
			{
				return;
			}
			if (!AllowedClick(data))
			{
				return;
			}
			if (CurrentState != ButtonStates.ButtonPressed && CurrentState != ButtonStates.ButtonDown)
			{
				return;
			}

			CurrentState = ButtonStates.ButtonUp;
			InvokeButtonStateChange(PointerEventData.FramePressState.Released, data);
			if ((Time.timeScale != 0) && (ReleasedDelay > 0))
			{
				Invoke ("InvokeReleased", ReleasedDelay);
			}
			else
			{
				ButtonReleased.Invoke();
			}
		}

		/// <summary>
		/// Invokes the ButtonReleased event
		/// </summary>
		protected virtual void InvokeReleased()
		{
			if (ButtonReleased != null)
			{
				ButtonReleased.Invoke();
			}			
		}

		/// <summary>
		/// Triggers the bound pointer pressed action
		/// </summary>
		public virtual void OnPointerPressed()
		{
			if (!Interactable)
			{
				return;
			}
			CurrentState = ButtonStates.ButtonPressed;
			if (ButtonPressed != null)
			{
				ButtonPressed.Invoke();
			}
		}

		/// <summary>
		/// Resets the button's state and opacity
		/// </summary>
		protected virtual void ResetButton()
		{
			SetOpacity(_initialOpacity);
			CurrentState = ButtonStates.Off;
		}

		/// <summary>
		/// Triggers the bound pointer enter action when touch enters zone
		/// </summary>
		public virtual void OnPointerEnter(PointerEventData data)
		{
			if (!Interactable)
			{
				return;
			}
			if (!AllowedClick(data))
			{
				return;
			}
			if (!MouseMode)
			{
				OnPointerDown (data);
			}
		}

		/// <summary>
		/// Triggers the bound pointer exit action when touch is out of zone
		/// </summary>
		public virtual void OnPointerExit(PointerEventData data)
		{
			if (!Interactable)
			{
				return;
			}
			if (!AllowedClick(data))
			{
				return;
			}
			if (!MouseMode)
			{
				OnPointerUp(data);	
			}
		}
		/// <summary>
		/// OnEnable, we reset our button state
		/// </summary>
		protected virtual void OnEnable()
		{
			ResetButton();
		}

		/// <summary>
		/// On disable we reset our flags and disable the button
		/// </summary>
		private void OnDisable()
		{
			bool wasActive = CurrentState != ButtonStates.Off && CurrentState != ButtonStates.Disabled && CurrentState != ButtonStates.ButtonUp;
			DisableButton();
			CurrentState = ButtonStates.Off; 
			if (wasActive)
			{
				InvokeButtonStateChange(PointerEventData.FramePressState.Released, null);
				ButtonReleased?.Invoke();
			}
		}

		/// <summary>
		/// Prevents the button from receiving touches
		/// </summary>
		public virtual void DisableButton()
		{
			CurrentState = ButtonStates.Disabled;
		}

		/// <summary>
		/// Allows the button to receive touches
		/// </summary>
		public virtual void EnableButton()
		{
			if (CurrentState == ButtonStates.Disabled)
			{
				CurrentState = ButtonStates.Off;	
			}
		}

		/// <summary>
		/// Sets the canvas group's opacity to the requested value
		/// </summary>
		/// <param name="newOpacity"></param>
		protected virtual void SetOpacity(float newOpacity)
		{
			if (_canvasGroup!=null)
			{
				_canvasGroup.alpha = newOpacity;
			}
		}

		/// <summary>
		/// Updates animator states based on the current state of the button
		/// </summary>
		protected virtual void UpdateAnimatorStates ()
		{
			if (_animator == null)
			{
				return;
			}
			if (DisabledAnimationParameterName != null)
			{
				_animator.SetBool (DisabledAnimationParameterName, (CurrentState == ButtonStates.Disabled));
			}
			if (PressedAnimationParameterName != null)
			{
				_animator.SetBool (PressedAnimationParameterName, (CurrentState == ButtonStates.ButtonPressed));
			}
			if (IdleAnimationParameterName != null)
			{
				_animator.SetBool (IdleAnimationParameterName, (CurrentState == ButtonStates.Off));
			}
		}

		/// <summary>
		/// On submit, raises the appropriate events
		/// </summary>
		/// <param name="eventData"></param>
		public virtual void OnSubmit(BaseEventData eventData)
		{
			if (ButtonPressedFirstTime!=null)
			{
				ButtonPressedFirstTime.Invoke();
			}
			if (ButtonReleased!=null)
			{
				ButtonReleased.Invoke ();
			}
		}
	}
}
#endif
