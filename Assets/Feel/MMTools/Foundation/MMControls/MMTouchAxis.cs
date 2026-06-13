#if MM_UI
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MoreMountains.Tools
{	
	[System.Serializable]
	public class AxisEvent : UnityEvent<float> {}

	/// <summary>
	/// Add this component to a GUI Image to have it act as an axis. 
	/// Bind pressed down, pressed continually and released actions to it from the inspector
	/// Handles mouse and multi touch
	/// </summary>
	[RequireComponent(typeof(Rect))]
	[RequireComponent(typeof(CanvasGroup))]
	[AddComponentMenu("More Mountains/Tools/Controls/MM Touch Axis")]
	public class MMTouchAxis : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
	{
		public enum ButtonStates { Off, ButtonDown, ButtonPressed, ButtonUp }
		[Header("Binding")]
		/// The method(s) to call when the axis gets pressed down
		[Tooltip("轴首次被按下时要调用的方法")]
		public UnityEvent AxisPressedFirstTime;
		/// The method(s) to call when the axis gets released
		[Tooltip("轴释放时要调用的方法")]
		public UnityEvent AxisReleased;
		/// The method(s) to call while the axis is being pressed
		[Tooltip("轴持续按下期间要调用的方法")]
		public AxisEvent AxisPressed;

		[Header("Pressed Behaviour")]
		[MMInformation("你可以在这里设置轴被按下时的透明度，用于提供更明显的视觉反馈。",MMInformationAttribute.InformationType.Info,false)]
		/// the new opacity to apply to the canvas group when the axis is pressed
		[Tooltip("轴被按下时要应用到 CanvasGroup 的透明度")]
		public float PressedOpacity = 0.5f;
		/// the value to send the bound method when the axis is pressed
		[Tooltip("轴按下时传递给绑定方法的值")]
		public float AxisValue;

		[Header("Mouse Mode")]
		[MMInformation("如果将此项设为 true，必须真的按下这个轴才会触发；否则仅仅悬停就会触发（更适合触控输入）。", MMInformationAttribute.InformationType.Info,false)]
		/// If you set this to true, you'll need to actually press the axis for it to be triggered, otherwise a simple hover will trigger it (better for touch input).
		[Tooltip("如果将此项设为 true，必须真的按下这个轴才会触发；否则仅仅悬停就会触发（更适合触控输入）。")]
		public bool MouseMode = false;

		public virtual ButtonStates CurrentState { get; protected set; }

		protected CanvasGroup _canvasGroup;
		protected float _initialOpacity;

		/// <summary>
		/// On Start, we get our canvasgroup and set our initial alpha
		/// </summary>
		protected virtual void Awake()
		{
			_canvasGroup = GetComponent<CanvasGroup>();
			if (_canvasGroup!=null)
			{
				_initialOpacity = _canvasGroup.alpha;
			}
			ResetButton();
		}

		/// <summary>
		/// Every frame, if the touch zone is pressed, we trigger the bound method if it exists
		/// </summary>
		protected virtual void Update()
		{
			if (AxisPressed != null)
			{
				if (CurrentState == ButtonStates.ButtonPressed)
				{
					AxisPressed.Invoke(AxisValue);
				}
			}
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
		/// Triggers the bound pointer down action
		/// </summary>
		public virtual void OnPointerDown(PointerEventData data)
		{
			if (CurrentState != ButtonStates.Off)
			{
				return;
			}

			CurrentState = ButtonStates.ButtonDown;
			if (_canvasGroup!=null)
			{
				_canvasGroup.alpha=PressedOpacity;
			}
			if (AxisPressedFirstTime!=null)
			{
				AxisPressedFirstTime.Invoke();
			}
		}

		/// <summary>
		/// Triggers the bound pointer up action
		/// </summary>
		public virtual void OnPointerUp(PointerEventData data)
		{
			if (CurrentState != ButtonStates.ButtonPressed && CurrentState != ButtonStates.ButtonDown)
			{
				return;
			}

			CurrentState = ButtonStates.ButtonUp;
			if (_canvasGroup!=null)
			{
				_canvasGroup.alpha=_initialOpacity;
			}
			if (AxisReleased != null)
			{
				AxisReleased.Invoke();
			}
			AxisPressed.Invoke(0);
		}

		/// <summary>
		/// OnEnable, we reset our button state
		/// </summary>
		protected virtual void OnEnable()
		{
			ResetButton();
		}

		/// <summary>
		/// Resets the button's state and opacity
		/// </summary>
		protected virtual void ResetButton()
		{
			CurrentState = ButtonStates.Off;
			_canvasGroup.alpha = _initialOpacity;
			CurrentState = ButtonStates.Off;
		}

		/// <summary>
		/// Triggers the bound pointer enter action when touch enters zone
		/// </summary>
		public virtual void OnPointerEnter(PointerEventData data)
		{
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
			if (!MouseMode)
			{
				OnPointerUp(data);	
			}
		}
	}
}
#endif
