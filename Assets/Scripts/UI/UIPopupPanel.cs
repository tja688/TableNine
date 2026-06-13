using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIPopupPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text mInfoText;
    [SerializeField] private Button mCloseButton;

    private void Awake()
    {
        AutoBind();
        if (mCloseButton != null)
        {
            mCloseButton.onClick.RemoveAllListeners();
            mCloseButton.onClick.AddListener(Hide);
        }
    }

    public void Show(string message)
    {
        if (mInfoText != null)
        {
            mInfoText.text = message ?? string.Empty;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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
