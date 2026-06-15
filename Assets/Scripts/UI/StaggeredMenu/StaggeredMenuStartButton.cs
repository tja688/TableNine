using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景内固定按钮 → StaggeredMenu 拉开 / 回收。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class StaggeredMenuStartButton : MonoBehaviour
{
    [SerializeField] private StaggeredMenuView mMenu;
    [SerializeField] private Button mButton;

    private void Awake()
    {
        mButton = mButton != null ? mButton : GetComponent<Button>();
        mMenu = mMenu != null
            ? mMenu
            : GetComponentInParent<StaggeredMenuView>(true);

        if (mButton == null || mMenu == null)
        {
            return;
        }

        mButton.onClick.RemoveListener(OnClick);
        mButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        mMenu.Toggle();
    }
}
