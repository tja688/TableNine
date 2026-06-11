using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIPopupPanelData : UIPanelData
{
    public string Message;
}

public sealed class UIPopupPanel : UIPanel
{
    [SerializeField] private TMP_Text mInfoText;
    [SerializeField] private Button mCloseButton;

    protected override void OnInit(IUIData uiData = null)
    {
        AutoBind();
        if (mCloseButton != null)
        {
            mCloseButton.onClick.RemoveAllListeners();
            mCloseButton.onClick.AddListener(CloseSelf);
        }
    }

    protected override void OnOpen(IUIData uiData = null)
    {
        var data = uiData as UIPopupPanelData;
        var requestData = uiData as TableNineUIRequestPanelData;
        if (mInfoText != null)
        {
            mInfoText.text = requestData != null ? requestData.Message : data != null ? data.Message : string.Empty;
        }
    }

    protected override void OnClose()
    {
    }

    private void AutoBind()
    {
        mInfoText = mInfoText != null ? mInfoText : FindDeep(transform, "InfoText")?.GetComponent<TMP_Text>();
        mCloseButton = mCloseButton != null ? mCloseButton : FindDeep(transform, "CloseButton")?.GetComponent<Button>();
    }

    private static Transform FindDeep(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var result = FindDeep(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
