using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MoreMountains.Tools
{
	/// <summary>
	/// Attach this class to a collider and it'll let you trigger events when the user clicks/drags/enters/etc that collider
	/// </summary>
	public class MMOnMouse : MonoBehaviour
	{
		/// OnMouseDown is called when the user has pressed the mouse button while over the Collider.
		[Tooltip("当鼠标位于该 Collider 上并按下按键时触发（OnMouseDown）")]
		public UnityEvent OnMouseDownEvent;
		/// OnMouseDrag is called when the user has clicked on a Collider and is still holding down the mouse.
		[Tooltip("当鼠标在该 Collider 上按下后持续拖动时触发（OnMouseDrag）")]
		public UnityEvent OnMouseDragEvent;
		/// Called when the mouse enters the Collider.
		[Tooltip("当鼠标进入该碰撞体时触发（鼠标输入）")]
		public UnityEvent OnMouseEnterEvent;
		/// Called when the mouse is not any longer over the Collider.
		[Tooltip("当鼠标离开该碰撞体时触发（鼠标退出时）")]
		public UnityEvent OnMouseExitEvent;
		/// Called every frame while the mouse is over the Collider.
		[Tooltip("当鼠标停留在该 Collider 上时每帧触发（OnMouseOver）")]
		public UnityEvent OnMouseOverEvent;
		/// OnMouseUp is called when the user has released the mouse button.
		[Tooltip("当鼠标按键释放时触发（OnMouseUp）")]
		public UnityEvent OnMouseUpEvent;
		/// OnMouseUpAsButton is only called when the mouse is released over the same Collider as it was pressed.
		[Tooltip("仅当按下与释放都发生在同一个 Collider 上时触发（OnMouseUpAsButton）")]
		public UnityEvent OnMouseUpAsButtonEvent;

		protected virtual void OnMouseDown()
		{
			OnMouseDownEvent.Invoke();
		}
		
		protected virtual void OnMouseDrag()
		{
			OnMouseDragEvent.Invoke();
		}
		
		protected virtual void OnMouseEnter()
		{
			OnMouseEnterEvent.Invoke();
		}
		
		protected virtual void OnMouseExit()
		{
			OnMouseExitEvent.Invoke();
		}
		
		protected virtual void OnMouseOver()
		{
			OnMouseOverEvent.Invoke();
		}
		
		protected virtual void OnMouseUp()
		{
			OnMouseUpEvent.Invoke();
		}
		
		protected virtual void OnMouseUpAsButton()
		{
			OnMouseUpAsButtonEvent.Invoke();
		}
		
	}	
}
