using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ChoiceOverlayMode
{
    AttributeUpgrade
}

public sealed class UIChoiceOverlayPanelData : UIPanelData
{
    public ChoiceOverlayMode Mode;
}

public sealed class UIChoiceOverlayPanel : UIPanel, IController
{
    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private Button[] mChoiceButtons;
    [SerializeField] private Button mPassButton;
    [SerializeField] private TMP_Text mPassText;

    private ChoiceOverlayMode mMode;
    private TableNineUIRequestPanelData mRequestData;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    protected override void OnInit(IUIData uiData = null)
    {
        AutoBind();
        mEventRegisters.Add(this.RegisterEvent<AttributeChoiceResolvedEvent>(_ => CloseSelf()));
    }

    protected override void OnOpen(IUIData uiData = null)
    {
        mRequestData = uiData as TableNineUIRequestPanelData;
        var data = uiData as UIChoiceOverlayPanelData;
        mMode = data != null ? data.Mode : ChoiceOverlayMode.AttributeUpgrade;
        BindButtons();
    }

    protected override void OnClose()
    {
        ClearButtonListeners();
        mRequestData = null;
    }

    protected override void OnBeforeDestroy()
    {
        for (var i = 0; i < mEventRegisters.Count; i++)
        {
            mEventRegisters[i].UnRegister();
        }

        mEventRegisters.Clear();
        base.OnBeforeDestroy();
    }

    private void AutoBind()
    {
        if (mChoiceButtons == null || mChoiceButtons.Length == 0)
        {
            var buttons = new List<Button>();
            for (var i = 1; i <= 3; i++)
            {
                var button = FindDeep(transform, $"ChoiseButton{i}")?.GetComponent<Button>();
                if (button != null)
                {
                    buttons.Add(button);
                }
            }

            mChoiceButtons = buttons.ToArray();
        }

        mPassButton = mPassButton != null ? mPassButton : FindDeep(transform, "PassButton")?.GetComponent<Button>();
        mPassText = mPassText != null ? mPassText : FindDeep(transform, "PassText")?.GetComponent<TMP_Text>();
    }

    private void BindButtons()
    {
        ClearButtonListeners();

        if (mRequestData != null)
        {
            BindRequestButtons();
            return;
        }

        if (mPassButton != null)
        {
            mPassButton.gameObject.SetActive(false);
        }

        if (mPassText != null)
        {
            mPassText.text = mMode == ChoiceOverlayMode.AttributeUpgrade ? "左:攻 中:防 右:血" : mPassText.text;
        }

        if (mChoiceButtons == null || mChoiceButtons.Length < 3)
        {
            return;
        }

        mChoiceButtons[0].onClick.AddListener(() => this.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Attack)));
        mChoiceButtons[1].onClick.AddListener(() => this.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Defense)));
        mChoiceButtons[2].onClick.AddListener(() => this.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.MaxHp)));
    }

    private void BindRequestButtons()
    {
        if (mPassText != null)
        {
            mPassText.text = string.IsNullOrWhiteSpace(mRequestData.Message)
                ? mRequestData.Title
                : mRequestData.Message;
        }

        if (mChoiceButtons != null)
        {
            for (var i = 0; i < mChoiceButtons.Length; i++)
            {
                var button = mChoiceButtons[i];
                if (button == null)
                {
                    continue;
                }

                var hasChoice = i < mRequestData.Choices.Count;
                button.gameObject.SetActive(hasChoice);
                if (!hasChoice)
                {
                    continue;
                }

                var choice = mRequestData.Choices[i];
                var capturedChoice = choice;
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = string.IsNullOrWhiteSpace(capturedChoice.MetaText)
                        ? capturedChoice.Label
                        : $"{capturedChoice.Label}\n{capturedChoice.MetaText}";
                }

                button.interactable = capturedChoice.IsEnabled;
                button.onClick.AddListener(() =>
                {
                    if (!capturedChoice.IsEnabled)
                    {
                        return;
                    }

                    capturedChoice.Action?.Invoke(this);
                    if (mRequestData.CloseOnChoice && capturedChoice.CloseAfterClick)
                    {
                        CloseSelf();
                    }
                });
            }
        }

        if (mPassButton == null)
        {
            return;
        }

        var hasCloseAction = !string.IsNullOrWhiteSpace(mRequestData.CloseLabel) || mRequestData.CloseAction != null;
        mPassButton.gameObject.SetActive(hasCloseAction);
        if (!hasCloseAction)
        {
            return;
        }

        if (mPassText != null)
        {
            mPassText.text = string.IsNullOrWhiteSpace(mRequestData.CloseLabel)
                ? mPassText.text
                : mRequestData.CloseLabel;
        }

        mPassButton.onClick.AddListener(() =>
        {
            mRequestData.CloseAction?.Invoke(this);
            if (mRequestData.CloseAction == null)
            {
                CloseSelf();
            }
        });
    }

    private void ClearButtonListeners()
    {
        if (mChoiceButtons != null)
        {
            for (var i = 0; i < mChoiceButtons.Length; i++)
            {
                if (mChoiceButtons[i] != null)
                {
                    mChoiceButtons[i].onClick.RemoveAllListeners();
                }
            }
        }

        if (mPassButton != null)
        {
            mPassButton.onClick.RemoveAllListeners();
        }
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
