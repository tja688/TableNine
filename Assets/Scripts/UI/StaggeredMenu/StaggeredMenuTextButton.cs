using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 纯文字点击区域：无 Button 组件，靠 TMP/Text 的 Raycast + 本脚本接收点击。
/// </summary>
[DisallowMultipleComponent]
public sealed class StaggeredMenuTextButton : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke();
    }
}
