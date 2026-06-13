#if MM_UI
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MoreMountains.Tools
{
	/// <summary>
	/// A simple helper class you can use to trigger methods on Unity's pointer events
	/// Typically used on a UI Image
	/// </summary>
	public class MMOnPointer : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
	{
		[Header("Pointer movement")]
		/// an event to trigger when the pointer enters the associated game object
		[Tooltip("当指针进入关联 GameObject 时触发的事件")]
		public UnityEvent PointerEnter;
		/// an event to trigger when the pointer exits the associated game object
		[Tooltip("当指针离开关联 GameObject 时触发的事件")]
		public UnityEvent PointerExit;
		
		[Header("Clicks")]
		/// an event to trigger when the pointer is pressed down on the associated game object
		[Tooltip("当指针在关联 GameObject 上按下时触发的事件")]
		public UnityEvent PointerDown;
		/// an event to trigger when the pointer is pressed up on the associated game object
		[Tooltip("当指针在关联 GameObject 上抬起时触发的事件")]
		public UnityEvent PointerUp;
		/// an event to trigger when the pointer is clicked on the associated game object
		[Tooltip("当指针点击关联 GameObject 时触发的事件")]
		public UnityEvent PointerClick;
		
		/// <summary>
		/// IPointerEnterHandler implementation
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerEnter(PointerEventData eventData)
		{
			PointerEnter?.Invoke();
		}

		/// <summary>
		/// IPointerExitHandler implementation
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerExit(PointerEventData eventData)
		{
			PointerExit?.Invoke();
		}
		
		/// <summary>
		/// IPointerDownHandler implementation
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerDown(PointerEventData eventData)
		{
			PointerDown?.Invoke();
		}

		/// <summary>
		/// IPointerUpHandler implementation
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerUp(PointerEventData eventData)
		{
			PointerUp?.Invoke();
		}

		/// <summary>
		/// IPointerClickHandler implementation
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerClick(PointerEventData eventData)
		{
			PointerClick?.Invoke();
		}
	}
}
#endif
