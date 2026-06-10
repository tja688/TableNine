using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplaySceneController : MonoBehaviour, IController
{
    private readonly Dictionary<int, GameplayCardVisual> mBoardCardViews = new Dictionary<int, GameplayCardVisual>();
    private readonly Dictionary<int, GameplayCardVisual> mItemCardViews = new Dictionary<int, GameplayCardVisual>();
    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    private Font mUiFont;
    private Transform mBoardRoot;
    private Transform mItemRoot;
    private GameObject mCardTemplate;
    private Text mPlayerInfoText;
    private Text mDeckInfoText;
    private Text mSkillInfoText;
    private Text mMessageText;
    private GameObject mClearBanner;
    private Text mClearBannerText;
    private GameObject mAttributeOverlay;

    private string mLastMessage = "左键点击玩家正交相邻格交互，拾取后的帮助卡点击下方道具槽使用。";

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Start()
    {
        if (!TableNine.IsInitialized)
        {
            TableNine.InitArchitecture();
        }

        CacheSceneReferences();
        BuildSlotInputs();
        BuildCardVisuals();
        BuildHud();
        RegisterGameplayEvents();

        if (!this.GetModel<IRunModel>().IsRunActive.Value)
        {
            this.SendCommand(new StartNewRunCommand());
        }
    }

    private void OnDestroy()
    {
        for (var i = 0; i < mEventRegisters.Count; i++)
        {
            mEventRegisters[i].UnRegister();
        }

        mEventRegisters.Clear();
    }

    private void LateUpdate()
    {
        if (!TableNine.IsInitialized)
        {
            return;
        }

        RefreshBoardCardViews();
        RefreshItemCardViews();
        RefreshHudTexts();
        RefreshOverlayState();
    }

    public void HandleBoardSlotClick(int slotNo)
    {
        this.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(slotNo)));
    }

    public void HandleItemSlotClick(int itemSlotIndex)
    {
        this.SendCommand(new ClickItemSlotCommand(itemSlotIndex));
    }

    private void CacheSceneReferences()
    {
        mUiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mBoardRoot = GameObject.Find("NineGrid CardSlots")?.transform;
        mItemRoot = GameObject.Find("Item CardSlots")?.transform;
        mCardTemplate = GameObject.Find("NineGrid CardSlots/CardExample");

        if (mBoardRoot == null || mItemRoot == null || mCardTemplate == null)
        {
            Debug.LogError("GameplaySceneController could not find required scene objects.");
            enabled = false;
            return;
        }

        mCardTemplate.SetActive(false);
    }

    private void BuildSlotInputs()
    {
        for (var slot = 1; slot <= 9; slot++)
        {
            var slotObject = FindChild(mBoardRoot, slot == 5 ? "CardSlot5ForPlayer" : $"CardSlot{slot}");
            if (slotObject == null)
            {
                continue;
            }

            if (slotObject.GetComponent<Collider2D>() == null)
            {
                slotObject.gameObject.AddComponent<BoxCollider2D>();
            }

            var clickProxy = slotObject.gameObject.GetComponent<BoardSlotClickProxy>();
            if (clickProxy == null)
            {
                clickProxy = slotObject.gameObject.AddComponent<BoardSlotClickProxy>();
            }

            clickProxy.Initialize(this, slot);
        }

        for (var slot = 0; slot < 5; slot++)
        {
            var slotObject = FindChild(mItemRoot, $"CardSlot{slot + 1}");
            if (slotObject == null)
            {
                continue;
            }

            if (slotObject.GetComponent<Collider2D>() == null)
            {
                slotObject.gameObject.AddComponent<BoxCollider2D>();
            }

            var clickProxy = slotObject.gameObject.GetComponent<ItemSlotClickProxy>();
            if (clickProxy == null)
            {
                clickProxy = slotObject.gameObject.AddComponent<ItemSlotClickProxy>();
            }

            clickProxy.Initialize(this, slot);
        }
    }

    private void BuildCardVisuals()
    {
        for (var slot = 1; slot <= 9; slot++)
        {
            var slotObject = FindChild(mBoardRoot, slot == 5 ? "CardSlot5ForPlayer" : $"CardSlot{slot}");
            if (slotObject == null)
            {
                continue;
            }

            mBoardCardViews[slot] = CreateCardVisual($"BoardCardView{slot}", slotObject.position, mBoardRoot, 1f);
        }

        for (var slot = 0; slot < 5; slot++)
        {
            var slotObject = FindChild(mItemRoot, $"CardSlot{slot + 1}");
            if (slotObject == null)
            {
                continue;
            }

            mItemCardViews[slot] = CreateCardVisual($"ItemCardView{slot + 1}", slotObject.position, mItemRoot, 0.85f);
        }
    }

    private void BuildHud()
    {
        var hudCanvas = GameObject.Find("HUDCanvas")?.transform;
        var overlayCanvas = GameObject.Find("OverlayCanvas")?.transform;
        if (hudCanvas == null || overlayCanvas == null)
        {
            Debug.LogError("GameplaySceneController could not find HUDCanvas or OverlayCanvas.");
            enabled = false;
            return;
        }

        mPlayerInfoText = CreateTextBlock(
            FindOrCreateRectTransform(hudCanvas, "PlayerInfoPanel"),
            "RuntimePlayerInfo",
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(12f, 12f),
            new Vector2(-12f, -12f),
            TextAnchor.UpperLeft,
            20);

        mDeckInfoText = CreateTextBlock(
            FindOrCreateRectTransform(hudCanvas, "DeckInfoPanel"),
            "RuntimeDeckInfo",
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(12f, 12f),
            new Vector2(-12f, -12f),
            TextAnchor.UpperLeft,
            18);

        mSkillInfoText = CreateTextBlock(
            FindOrCreateRectTransform(hudCanvas, "SkillPanel"),
            "RuntimeSkillInfo",
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(12f, 12f),
            new Vector2(-12f, -12f),
            TextAnchor.UpperLeft,
            18);

        mMessageText = CreateTextBlock(
            FindOrCreateRectTransform(hudCanvas, "InfoPanel"),
            "RuntimeMessageInfo",
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(12f, 12f),
            new Vector2(-12f, -12f),
            TextAnchor.UpperLeft,
            18);

        mClearBanner = CreateOverlayBanner(overlayCanvas, "ClearBanner", "节点已清空，可以继续处理帮助卡。");
        mClearBannerText = mClearBanner.GetComponentInChildren<Text>();
        mAttributeOverlay = CreateAttributeOverlay(overlayCanvas);
    }

    private void RegisterGameplayEvents()
    {
        mEventRegisters.Add(this.RegisterEvent<GameplayMessageEvent>(evt => mLastMessage = evt.Message));
        mEventRegisters.Add(this.RegisterEvent<LevelClearReadyEvent>(evt =>
        {
            mLastMessage = $"第 {evt.Layer} 层第 {evt.NodeInLayer} 节点已清空。";
        }));
        mEventRegisters.Add(this.RegisterEvent<MonsterKilledEvent>(_ => { mLastMessage = "怪物被击杀，获得 5 金币。"; }));
    }

    private void RefreshBoardCardViews()
    {
        var runModel = this.GetModel<IRunModel>();
        if (!runModel.IsRunActive.Value)
        {
            HideAllCardViews();
            return;
        }

        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        var playerStats = this.SendQuery(new GetEffectivePlayerStatsQuery());

        for (var slot = 1; slot <= 9; slot++)
        {
            if (!mBoardCardViews.TryGetValue(slot, out var view))
            {
                continue;
            }

            var uid = boardModel.GetCardAt(new BoardSlotNo(slot));
            if (!uid.HasValue || !collectionModel.TryGetCard(uid.Value, out var runtime))
            {
                view.Hide();
                continue;
            }

            if (runtime.CardType == CardType.Player)
            {
                view.Show(BuildPlayerCardText(playerStats), new Color(0.55f, 0.85f, 0.55f));
                continue;
            }

            if (runtime.CardType == CardType.Monster)
            {
                var stats = this.SendQuery(new GetEffectiveMonsterStatsQuery(runtime.Uid));
                view.Show(BuildMonsterCardText(runtime, stats), new Color(0.92f, 0.62f, 0.62f));
                continue;
            }

            var helpDefinition = configModel.GetCardDefinition(runtime.DefinitionId);
            view.Show(BuildHelpCardText(helpDefinition), ResolveHelpColor(helpDefinition.Quality));
        }
    }

    private void RefreshItemCardViews()
    {
        var runModel = this.GetModel<IRunModel>();
        if (!runModel.IsRunActive.Value)
        {
            HideAllCardViews();
            return;
        }

        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();

        for (var slot = 0; slot < deckModel.ItemSlots.Length; slot++)
        {
            if (!mItemCardViews.TryGetValue(slot, out var view))
            {
                continue;
            }

            var uid = deckModel.ItemSlots[slot];
            if (!uid.HasValue || !collectionModel.TryGetCard(uid.Value, out var runtime))
            {
                view.Hide();
                continue;
            }

            var helpDefinition = configModel.GetCardDefinition(runtime.DefinitionId);
            view.Show(BuildItemHelpCardText(helpDefinition, deckModel.PendingHelpCardAction, runtime.Uid), ResolveHelpColor(helpDefinition.Quality));
        }
    }

    private void RefreshHudTexts()
    {
        var runModel = this.GetModel<IRunModel>();
        if (!runModel.IsRunActive.Value)
        {
            mPlayerInfoText.text = "玩家\n等待开局";
            mDeckInfoText.text = "节点\n等待开局";
            mSkillInfoText.text = "技能\n等待开局";
            mMessageText.text = "正在准备首个可玩节点。";
            return;
        }

        var playerModel = this.GetModel<IPlayerModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var flowModel = this.GetModel<IFlowModel>();
        var playerStats = this.SendQuery(new GetEffectivePlayerStatsQuery());

        mPlayerInfoText.text =
            $"玩家\n生命 {playerStats.CurrentHp}/{playerStats.MaxHp}\n攻击 {playerStats.Attack}\n防御 {playerStats.Defense}\n金币 {playerModel.Gold.Value}";

        var previewText = deckModel.NextBattleCardPreview.Value.IsEmpty
            ? "无"
            : deckModel.NextBattleCardPreview.Value.DisplayName;

        mDeckInfoText.text =
            $"节点 L{runModel.Layer.Value}-N{runModel.NodeInLayer.Value}\nSeed {runModel.Seed.Value}\n阶段 {flowModel.Phase.Value}\n战斗牌堆 {deckModel.BattleDrawPile.Count}\n下一张 {previewText}";

        mSkillInfoText.text =
            $"技能\n{string.Join("\n", playerModel.SkillIds)}";

        mMessageText.text = BuildStatusMessage(flowModel, deckModel);
    }

    private void RefreshOverlayState()
    {
        var flowModel = this.GetModel<IFlowModel>();
        var deckModel = this.GetModel<IDeckModel>();

        var isClearReady = flowModel.Phase.Value == FlowPhase.ClearReady;
        mClearBanner.SetActive(isClearReady);
        if (isClearReady && mClearBannerText != null)
        {
            mClearBannerText.text = "节点已清空，可以继续使用道具牌格里的帮助卡。";
        }

        mAttributeOverlay.SetActive(deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.AttributeChoice);
    }

    private GameplayCardVisual CreateCardVisual(string objectName, Vector3 worldPosition, Transform parent, float scaleMultiplier)
    {
        var instance = Instantiate(mCardTemplate, worldPosition, Quaternion.identity, parent);
        instance.name = objectName;
        instance.SetActive(true);
        instance.transform.position = worldPosition;
        instance.transform.localScale = mCardTemplate.transform.localScale * scaleMultiplier;

        var collider = instance.GetComponent<Collider2D>();
        if (collider != null)
        {
            Destroy(collider);
        }

        var view = instance.GetComponent<GameplayCardVisual>();
        if (view == null)
        {
            view = instance.AddComponent<GameplayCardVisual>();
        }

        view.Initialize();
        view.Hide();
        return view;
    }

    private RectTransform FindOrCreateRectTransform(Transform parent, string childName)
    {
        var child = parent.Find(childName) as RectTransform;
        if (child != null)
        {
            return child;
        }

        var go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        child = go.GetComponent<RectTransform>();
        child.anchorMin = Vector2.zero;
        child.anchorMax = Vector2.one;
        child.offsetMin = Vector2.zero;
        child.offsetMax = Vector2.zero;
        return child;
    }

    private Text CreateTextBlock(
        RectTransform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        TextAnchor alignment,
        int fontSize)
    {
        var textTransform = parent.Find(objectName) as RectTransform;
        if (textTransform == null)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            textTransform = go.GetComponent<RectTransform>();
        }

        textTransform.anchorMin = anchorMin;
        textTransform.anchorMax = anchorMax;
        textTransform.offsetMin = offsetMin;
        textTransform.offsetMax = offsetMax;

        var text = textTransform.GetComponent<Text>();
        text.font = mUiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.15f, 0.15f, 0.15f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private GameObject CreateOverlayBanner(Transform overlayCanvas, string objectName, string text)
    {
        var root = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(overlayCanvas, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.25f, 0.78f);
        rect.anchorMax = new Vector2(0.75f, 0.92f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = root.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.65f);

        var label = CreateTextBlock(rect, "Label", Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f), TextAnchor.MiddleCenter, 24);
        label.color = Color.white;
        label.text = text;
        root.SetActive(false);
        return root;
    }

    private GameObject CreateAttributeOverlay(Transform overlayCanvas)
    {
        var root = new GameObject("AttributeChoiceOverlay", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(overlayCanvas, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0.65f);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.35f, 0.25f);
        panelRect.anchorMax = new Vector2(0.65f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.95f, 0.94f, 0.86f, 0.98f);

        var title = CreateTextBlock(panelRect, "Title", new Vector2(0f, 0.7f), new Vector2(1f, 1f), new Vector2(12f, 12f), new Vector2(-12f, -12f), TextAnchor.MiddleCenter, 24);
        title.text = "属性提升卡";

        CreateChoiceButton(panelRect, "AttackButton", "攻击 +1", new Vector2(0.15f, 0.46f), new Vector2(0.85f, 0.62f), () =>
        {
            this.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Attack));
        });

        CreateChoiceButton(panelRect, "DefenseButton", "防御 +1", new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.41f), () =>
        {
            this.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Defense));
        });

        CreateChoiceButton(panelRect, "HpButton", "生命 +2", new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.20f), () =>
        {
            this.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.MaxHp));
        });

        root.SetActive(false);
        return root;
    }

    private void CreateChoiceButton(RectTransform parent, string objectName, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.35f, 0.42f, 0.96f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var text = CreateTextBlock(rect, "Label", Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f), TextAnchor.MiddleCenter, 20);
        text.color = Color.white;
        text.text = label;
    }

    private string BuildPlayerCardText(EffectiveStats stats)
    {
        return $"玩家\nHP {stats.CurrentHp}/{stats.MaxHp}\nATK {stats.Attack} DEF {stats.Defense}";
    }

    private string BuildMonsterCardText(CardRuntime runtime, EffectiveStats stats)
    {
        var extra = stats.HasFirstStrike ? "\n先攻" : string.Empty;
        return $"{runtime.DisplayName}\nHP {stats.CurrentHp}/{stats.MaxHp}\nATK {stats.Attack} DEF {stats.Defense}{extra}";
    }

    private string BuildHelpCardText(CardDefinition helpDefinition)
    {
        return $"{helpDefinition.DisplayName}\n帮助卡\n点击拾取";
    }

    private string BuildItemHelpCardText(CardDefinition helpDefinition, PendingHelpCardAction pendingAction, CardUid uid)
    {
        var suffix = pendingAction.IsActive && pendingAction.HelpCardUid.Equals(uid) ? "\n等待操作" : "\n点击使用";
        return $"{helpDefinition.DisplayName}\n道具槽{suffix}";
    }

    private string BuildStatusMessage(IFlowModel flowModel, IDeckModel deckModel)
    {
        if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.ThrowingKnifeTarget)
        {
            return "飞刀待命：点击任意怪物结算 6 点伤害。";
        }

        if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.AttributeChoice)
        {
            return "属性提升卡：请在覆盖层中选择属性。";
        }

        if (flowModel.Phase.Value == FlowPhase.ClearReady)
        {
            return "节点已清空。你仍可拾取或使用帮助卡。";
        }

        return mLastMessage;
    }

    private Color ResolveHelpColor(CardQuality quality)
    {
        switch (quality)
        {
            case CardQuality.White:
                return new Color(0.96f, 0.96f, 0.92f);
            case CardQuality.Blue:
                return new Color(0.62f, 0.79f, 0.96f);
            case CardQuality.Gold:
                return new Color(0.95f, 0.84f, 0.42f);
            case CardQuality.Red:
                return new Color(0.92f, 0.46f, 0.46f);
            default:
                return new Color(0.9f, 0.9f, 0.9f);
        }
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private void HideAllCardViews()
    {
        foreach (var pair in mBoardCardViews)
        {
            pair.Value.Hide();
        }

        foreach (var pair in mItemCardViews)
        {
            pair.Value.Hide();
        }
    }
}

public sealed class GameplayCardVisual : MonoBehaviour
{
    private SpriteRenderer mSpriteRenderer;
    private TextMesh mTextMesh;

    public void Initialize()
    {
        mSpriteRenderer = GetComponent<SpriteRenderer>();
        mTextMesh = GetComponentInChildren<TextMesh>();
        if (mTextMesh == null)
        {
            var textObject = new GameObject("CardText", typeof(TextMesh));
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            mTextMesh = textObject.GetComponent<TextMesh>();
        }

        mTextMesh.anchor = TextAnchor.MiddleCenter;
        mTextMesh.alignment = TextAlignment.Center;
        mTextMesh.fontSize = 40;
        mTextMesh.characterSize = 0.08f;
        mTextMesh.color = new Color(0.14f, 0.14f, 0.14f);

        var renderer = mTextMesh.GetComponent<MeshRenderer>();
        if (renderer != null && mSpriteRenderer != null)
        {
            renderer.sortingOrder = mSpriteRenderer.sortingOrder + 1;
        }
    }

    public void Show(string cardText, Color cardColor)
    {
        gameObject.SetActive(true);
        if (mSpriteRenderer != null)
        {
            mSpriteRenderer.color = cardColor;
        }

        if (mTextMesh != null)
        {
            mTextMesh.text = cardText;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

public sealed class BoardSlotClickProxy : MonoBehaviour
{
    private GameplaySceneController mController;
    private int mSlotNo;

    public void Initialize(GameplaySceneController controller, int slotNo)
    {
        mController = controller;
        mSlotNo = slotNo;
    }

    private void OnMouseUpAsButton()
    {
        mController?.HandleBoardSlotClick(mSlotNo);
    }
}

public sealed class ItemSlotClickProxy : MonoBehaviour
{
    private GameplaySceneController mController;
    private int mItemSlotIndex;

    public void Initialize(GameplaySceneController controller, int itemSlotIndex)
    {
        mController = controller;
        mItemSlotIndex = itemSlotIndex;
    }

    private void OnMouseUpAsButton()
    {
        mController?.HandleItemSlotClick(mItemSlotIndex);
    }
}
