using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(110)]
public sealed class UIGameplayPanel : MonoBehaviour, IController
{
    private enum SidePanelEntryKind
    {
        None,
        Skill,
        Relic
    }

    private sealed class SidePanelIconSlot
    {
        public Image Image;
        public string EntryId;
        public SidePanelEntryKind Kind;
    }

    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private TMP_Text mCharacterNameText;
    [SerializeField] private TMP_Text mLifeNumberText;
    [SerializeField] private TMP_Text mCoinNumberText;
    [SerializeField] private TMP_Text mSkillText;
    [SerializeField] private TMP_Text mRelicText;
    [SerializeField] private TMP_Text mDescriptionText;
    [SerializeField] private Transform mSkillIconRoot;
    [SerializeField] private Transform mRelicIconRoot;

    private SidePanelIconSlot[] mSkillSlots = System.Array.Empty<SidePanelIconSlot>();
    private SidePanelIconSlot[] mRelicSlots = System.Array.Empty<SidePanelIconSlot>();
    private string mLastMessage;
    private bool mEventsRegistered;

    public static bool IsSidePanelHovered { get; private set; }

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Awake()
    {
        mLastMessage = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudDefaultHint);
        AutoBind();
        BindSidePanelSlots();
    }

    private void OnEnable()
    {
        RegisterEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnregisterEvents();
        IsSidePanelHovered = false;
    }

    private void Update()
    {
        if (TableNine.IsInitialized)
        {
            RefreshAll();
        }
    }

    private void LateUpdate()
    {
        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionTextInScene();
        }

        RefreshSidePanelHoverDescription();
    }

    private void AutoBind()
    {
        mCharacterNameText = mCharacterNameText != null ? mCharacterNameText : FindTmpText("CharacterName");
        mLifeNumberText = mLifeNumberText != null ? mLifeNumberText : FindTmpText("LifeNumber");
        mCoinNumberText = mCoinNumberText != null ? mCoinNumberText : FindTmpText("CoinNumber");
        mSkillText = mSkillText != null ? mSkillText : FindTmpText("SkillText");
        mRelicText = mRelicText != null ? mRelicText : FindTmpText("RelicText");
        mDescriptionText = mDescriptionText != null ? mDescriptionText : FindTmpText("DescriptionText");
        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionTextInScene();
        }
        mSkillIconRoot = mSkillIconRoot != null ? mSkillIconRoot : FindDeep(transform, "SkillPanel/Grids");
        mRelicIconRoot = mRelicIconRoot != null ? mRelicIconRoot : FindDeep(transform, "RelicPanel/Grids");
    }

    private void BindSidePanelSlots()
    {
        mSkillSlots = CollectIconSlots(mSkillIconRoot);
        mRelicSlots = CollectIconSlots(mRelicIconRoot);
    }

    private static SidePanelIconSlot[] CollectIconSlots(Transform gridsRoot)
    {
        if (gridsRoot == null)
        {
            return System.Array.Empty<SidePanelIconSlot>();
        }

        var slots = new List<SidePanelIconSlot>(gridsRoot.childCount);
        for (var i = 0; i < gridsRoot.childCount; i++)
        {
            var image = gridsRoot.GetChild(i).GetComponent<Image>();
            if (image == null)
            {
                continue;
            }

            image.preserveAspect = true;
            slots.Add(new SidePanelIconSlot { Image = image });
        }

        return slots.ToArray();
    }

    private void RegisterEvents()
    {
        if (mEventsRegistered || !TableNine.IsInitialized)
        {
            return;
        }

        mEventsRegistered = true;
        mEventRegisters.Add(this.RegisterEvent<GameplayMessageEvent>(evt =>
        {
            mLastMessage = DescriptionPanelTexts.Sanitize(evt.Message);
            RefreshDescription();
        }));

        mEventRegisters.Add(this.RegisterEvent<LevelClearReadyEvent>(evt =>
        {
            mLastMessage = DescriptionPanelTexts.Format(
                DescriptionPanelTextKeys.HudLevelClear,
                evt.Layer,
                evt.NodeInLayer);
            RefreshDescription();
        }));

        mEventRegisters.Add(this.RegisterEvent<MonsterKilledEvent>(_ =>
        {
            mLastMessage = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudMonsterKilled);
            RefreshAll();
        }));

        mEventRegisters.Add(this.RegisterEvent<BattleDeckChangedEvent>(_ => RefreshAll()));
        mEventRegisters.Add(this.RegisterEvent<DamageAppliedEvent>(_ => RefreshAll()));
        mEventRegisters.Add(this.RegisterEvent<ItemSlotChangedEvent>(_ => RefreshAll()));
        mEventRegisters.Add(this.RegisterEvent<RelicAddedEvent>(_ => RefreshSidePanels()));
        mEventRegisters.Add(this.RegisterEvent<RelicDiscardedEvent>(_ => RefreshSidePanels()));
        mEventRegisters.Add(this.RegisterEvent<RelicStatsChangedEvent>(_ => RefreshSidePanels()));
        mEventRegisters.Add(this.RegisterEvent<TutorSkillChosenEvent>(_ => RefreshSidePanels()));
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

    private void RefreshAll()
    {
        RefreshPlayer();
        RefreshSidePanels();
        RefreshDescription();
    }

    private void RefreshPlayer()
    {
        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            if (mLifeNumberText != null) mLifeNumberText.text = "x--";
            if (mCoinNumberText != null) mCoinNumberText.text = "x0";
            return;
        }

        var runModel = this.GetModel<IRunModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();
        var stats = this.SendQuery(new GetEffectivePlayerStatsQuery());
        var characterDefinition = configModel.GetCharacterDefinition(runModel.CharacterId);

        if (mCharacterNameText != null)
        {
            mCharacterNameText.text = characterDefinition != null ? characterDefinition.DisplayName : "玩家";
        }

        if (mLifeNumberText != null)
        {
            mLifeNumberText.text = $"x{stats.CurrentHp}";
        }

        if (mCoinNumberText != null)
        {
            mCoinNumberText.text = $"x{playerModel.Gold.Value}";
        }
    }

    private void RefreshSidePanels()
    {
        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            ClearSidePanelSlots(mSkillSlots);
            ClearSidePanelSlots(mRelicSlots);
            return;
        }

        RefreshSkillIcons();
        RefreshRelicIcons();
    }

    private void RefreshSkillIcons()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();

        for (var i = 0; i < mSkillSlots.Length; i++)
        {
            var slot = mSkillSlots[i];
            if (i >= playerModel.SkillIds.Count)
            {
                ClearSlot(slot);
                continue;
            }

            var skillId = playerModel.SkillIds[i];
            var definition = configModel.GetSkillDefinition(skillId);
            SetSlot(slot, definition?.Image, skillId, SidePanelEntryKind.Skill);
        }
    }

    private void RefreshRelicIcons()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();
        var writeIndex = 0;

        for (var i = 0; i < playerModel.Relics.Count && writeIndex < mRelicSlots.Length; i++)
        {
            var relic = playerModel.Relics[i];
            if (relic == null || relic.IsConsumed)
            {
                continue;
            }

            var definition = configModel.GetRelicDefinition(relic.RelicId);
            SetSlot(mRelicSlots[writeIndex], definition?.Image, relic.RelicId, SidePanelEntryKind.Relic);
            writeIndex++;
        }

        for (var i = writeIndex; i < mRelicSlots.Length; i++)
        {
            ClearSlot(mRelicSlots[i]);
        }
    }

    private static void SetSlot(SidePanelIconSlot slot, Sprite sprite, string entryId, SidePanelEntryKind kind)
    {
        if (slot?.Image == null)
        {
            return;
        }

        slot.EntryId = entryId;
        slot.Kind = kind;
        slot.Image.sprite = sprite;
        slot.Image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        slot.Image.raycastTarget = sprite != null;
    }

    private static void ClearSlot(SidePanelIconSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.EntryId = null;
        slot.Kind = SidePanelEntryKind.None;
        if (slot.Image == null)
        {
            return;
        }

        slot.Image.sprite = null;
        slot.Image.color = new Color(1f, 1f, 1f, 0f);
        slot.Image.raycastTarget = false;
    }

    private static void ClearSidePanelSlots(SidePanelIconSlot[] slots)
    {
        if (slots == null)
        {
            return;
        }

        for (var i = 0; i < slots.Length; i++)
        {
            ClearSlot(slots[i]);
        }
    }

    private void RefreshSidePanelHoverDescription()
    {
        IsSidePanelHovered = false;
        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionTextInScene();
        }

        if (mDescriptionText == null || !TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            return;
        }

        var hoveredSlot = TryGetHoveredSlot();
        if (hoveredSlot == null)
        {
            return;
        }

        var description = ComposeHoverDescription(hoveredSlot);
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        IsSidePanelHovered = true;
        mDescriptionText.text = description;
    }

    private SidePanelIconSlot TryGetHoveredSlot()
    {
        var hovered = TryGetHoveredSlot(mSkillSlots);
        if (hovered != null)
        {
            return hovered;
        }

        return TryGetHoveredSlot(mRelicSlots);
    }

    private static SidePanelIconSlot TryGetHoveredSlot(SidePanelIconSlot[] slots)
    {
        if (slots == null)
        {
            return null;
        }

        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.Kind == SidePanelEntryKind.None || slot.Image == null)
            {
                continue;
            }

            if (IsPointerOver(slot.Image.rectTransform))
            {
                return slot;
            }
        }

        return null;
    }

    private static bool IsPointerOver(RectTransform rectTransform)
    {
        if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
        {
            return false;
        }

        var canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        Camera camera = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, camera))
        {
            return true;
        }

        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        var min = corners[0];
        var max = corners[2];
        for (var i = 1; i < corners.Length; i++)
        {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }

        var screenMin = RectTransformUtility.WorldToScreenPoint(camera, min);
        var screenMax = RectTransformUtility.WorldToScreenPoint(camera, max);
        var pointer = Input.mousePosition;
        return pointer.x >= screenMin.x && pointer.x <= screenMax.x
               && pointer.y >= screenMin.y && pointer.y <= screenMax.y;
    }

    private static TMP_Text FindDescriptionTextInScene()
    {
        var texts = Object.FindObjectsOfType<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "DescriptionText")
            {
                return texts[i];
            }
        }

        return null;
    }

    private string ComposeHoverDescription(SidePanelIconSlot slot)
    {
        if (slot == null || string.IsNullOrWhiteSpace(slot.EntryId))
        {
            return string.Empty;
        }

        var configModel = this.GetModel<IConfigModel>();
        return slot.Kind switch
        {
            SidePanelEntryKind.Skill => SidePanelDescriptionComposer.ComposeSkill(configModel, slot.EntryId),
            SidePanelEntryKind.Relic => SidePanelDescriptionComposer.ComposeRelic(configModel, slot.EntryId),
            _ => string.Empty
        };
    }

    private void RefreshDescription()
    {
        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionTextInScene();
        }

        if (mDescriptionText == null)
        {
            return;
        }

        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            mDescriptionText.text = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudPreparing);
            return;
        }

        var deckModel = this.GetModel<IDeckModel>();
        var flowModel = this.GetModel<IFlowModel>();
        if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.ThrowingKnifeTarget)
        {
            mDescriptionText.text = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudThrowingKnifeReady);
            return;
        }

        if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.AttributeChoice)
        {
            mDescriptionText.text = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudAttributeChoice);
            return;
        }

        if (flowModel.Phase.Value == FlowPhase.ClearReady)
        {
            mDescriptionText.text = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudNodeClearReady);
            return;
        }

        mDescriptionText.text = DescriptionPanelTexts.Sanitize(mLastMessage);
    }

    private TMP_Text FindTmpText(string childName)
    {
        var child = FindDeep(transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindDeep(Transform root, string childPath)
    {
        if (root == null || string.IsNullOrWhiteSpace(childPath))
        {
            return null;
        }

        var segments = childPath.Split('/');
        var current = root;
        for (var i = 0; i < segments.Length; i++)
        {
            current = FindChildByName(current, segments[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

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
