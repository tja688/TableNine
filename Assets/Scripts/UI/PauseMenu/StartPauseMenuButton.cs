using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景内固定按钮 → PauseMenu 拉开 / 回收。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class StartPauseMenuButton : MonoBehaviour
{
    [SerializeField] private PauseMenuView mMenu;
    [SerializeField] private Button mButton;

    private void Awake()
    {
        TryBind();
    }

    private void OnEnable()
    {
        TryBind();
    }

    private void TryBind()
    {
        mButton = mButton != null ? mButton : GetComponent<Button>();
        if (mMenu == null)
        {
            mMenu = FindObjectOfType<PauseMenuView>(true);
        }

        if (mButton == null || mMenu == null)
        {
            return;
        }

        mButton.onClick.RemoveListener(OnClick);
        mButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (mMenu == null)
        {
            mMenu = FindObjectOfType<PauseMenuView>(true);
        }

        if (mMenu == null)
        {
            Debug.LogWarning($"{nameof(StartPauseMenuButton)}: 未找到 {nameof(PauseMenuView)}，请把 PauseMenu prefab 放进场景。");
            return;
        }

        mMenu.Toggle();
    }
}
