using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ChoiceOverlayMode
{
    AttributeUpgrade
}

public sealed class UIChoiceOverlayPanel : MonoBehaviour, IController
{
    private const string ThreeOrTwoWindowName = "3or2for1ChoiseWindow";
    private const string ShopWindowName = "ShopChoiseWindow";
    private const string DeleteCardWindowName = "DeleteCardChoiseWindow";

    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private Button[] mChoiceButtons;
    [SerializeField] private Image[] mChoiceItemImages;
    [SerializeField] private TMP_Text[] mChoiceNameTexts;
    [SerializeField] private Graphic[] mChoiceArrowGraphics;
    [SerializeField] private Button mPassButton;
    [SerializeField] private TMP_Text mPassText;

    private Transform mThreeOrTwoWindow;
    private Transform mShopWindow;
    private Transform mDeleteCardWindow;
    private Transform mActiveSubWindow;
    private ChoiceOverlayMode mMode = ChoiceOverlayMode.AttributeUpgrade;
    private bool mEventsRegistered;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Awake()
    {
        CacheSubWindows();
    }

    private void OnEnable()
    {
        RegisterEvents();
        ShowAttributeUpgradeLayout();
        AutoBind(mActiveSubWindow);
        BindAttributeButtons();
    }

    private void OnDisable()
    {
        UnregisterEvents();
        ClearButtonListeners();
        HideAllSubWindows();
        mActiveSubWindow = null;
        mChoiceButtons = null;
        mChoiceItemImages = null;
        mChoiceNameTexts = null;
        mChoiceArrowGraphics = null;
        mPassButton = null;
        mPassText = null;
    }

    public void Show(ChoiceOverlayMode mode = ChoiceOverlayMode.AttributeUpgrade)
    {
        mMode = mode;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void RegisterEvents()
    {
        if (mEventsRegistered || !TableNine.IsInitialized)
        {
            return;
        }

        mEventsRegistered = true;
        mEventRegisters.Add(this.RegisterEvent<AttributeChoiceResolvedEvent>(_ => Hide()));
    }

    private void UnregisterEvents()
    {
        for (var i = 0; i < mEventRegisters.Count; i++)
        {
            mEventRegisters[i].UnRegister();
        }

        mEventRegisters.Clear();
        mEventsRegistered = false;
    }

    private void CacheSubWindows()
    {
        mThreeOrTwoWindow = transform.Find(ThreeOrTwoWindowName);
        mShopWindow = transform.Find(ShopWindowName);
        mDeleteCardWindow = transform.Find(DeleteCardWindowName);
    }

    private void ShowAttributeUpgradeLayout()
    {
        SetWindowActive(mThreeOrTwoWindow, true);
        SetWindowActive(mShopWindow, false);
        SetWindowActive(mDeleteCardWindow, false);
        mActiveSubWindow = mThreeOrTwoWindow;
    }

    private void HideAllSubWindows()
    {
        SetWindowActive(mThreeOrTwoWindow, false);
        SetWindowActive(mShopWindow, false);
        SetWindowActive(mDeleteCardWindow, false);
    }

    private static void SetWindowActive(Transform window, bool active)
    {
        if (window != null)
        {
            window.gameObject.SetActive(active);
        }
    }

    private void AutoBind(Transform searchRoot)
    {
        if (searchRoot == null)
        {
            mChoiceButtons = System.Array.Empty<Button>();
            mPassButton = null;
            mPassText = null;
            return;
        }

        var buttons = new List<Button>();
        var itemImages = new List<Image>();
        var nameTexts = new List<TMP_Text>();
        var arrowGraphics = new List<Graphic>();
        for (var i = 1; i <= 3; i++)
        {
            var buttonRoot = FindDeep(searchRoot, $"ChoiseButton{i}");
            var button = buttonRoot != null ? buttonRoot.GetComponent<Button>() : null;
            if (button != null)
            {
                buttons.Add(button);
                itemImages.Add(
                    FindDeep(buttonRoot, "ChoiseItemImage1")?.GetComponent<Image>()
                    ?? buttonRoot.GetComponent<Image>());
                nameTexts.Add(FindDeep(buttonRoot, "Name")?.GetComponent<TMP_Text>());
                arrowGraphics.Add(FindDeep(buttonRoot, "ChoiseArrowImage")?.GetComponent<Graphic>());
            }
        }

        mChoiceButtons = buttons.ToArray();
        mChoiceItemImages = itemImages.ToArray();
        mChoiceNameTexts = nameTexts.ToArray();
        mChoiceArrowGraphics = arrowGraphics.ToArray();
        mPassButton = FindDeep(searchRoot, "PassButton")?.GetComponent<Button>();
        mPassText = FindDeep(searchRoot, "PassText")?.GetComponent<TMP_Text>();
    }

    private void BindAttributeButtons()
    {
        ClearButtonListeners();

        if (mPassButton != null)
        {
            mPassButton.gameObject.SetActive(false);
        }

        if (mPassText != null)
        {
            mPassText.text = mMode == ChoiceOverlayMode.AttributeUpgrade ? "左:攻 中:甲 右:血" : mPassText.text;
        }

        if (mChoiceButtons == null || mChoiceButtons.Length < 3)
        {
            return;
        }

        mChoiceButtons[0].onClick.AddListener(() => this.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Attack)));
        mChoiceButtons[1].onClick.AddListener(() => this.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Armor)));
        mChoiceButtons[2].onClick.AddListener(() => this.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.MaxHp)));
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

    public void SetChoiceCardPreview(int index, Sprite sprite, string displayName)
    {
        if (index < 0 || mChoiceItemImages == null || index >= mChoiceItemImages.Length)
        {
            return;
        }

        var image = mChoiceItemImages[index];
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;

        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.08f, 0.08f);
        rect.anchorMax = new Vector2(0.92f, 0.92f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (mChoiceNameTexts != null && index < mChoiceNameTexts.Length && mChoiceNameTexts[index] != null)
        {
            mChoiceNameTexts[index].text = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName;
            mChoiceNameTexts[index].gameObject.SetActive(false);
        }

        if (mChoiceArrowGraphics != null && index < mChoiceArrowGraphics.Length && mChoiceArrowGraphics[index] != null)
        {
            mChoiceArrowGraphics[index].gameObject.SetActive(false);
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
