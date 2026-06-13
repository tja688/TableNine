using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;

public sealed class UIGameplayPanel : MonoBehaviour, IController
{
    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private TMP_Text mCharacterNameText;
    [SerializeField] private TMP_Text mLifeNumberText;
    [SerializeField] private TMP_Text mCoinNumberText;
    [SerializeField] private TMP_Text mSkillText;
    [SerializeField] private TMP_Text mRelicText;
    [SerializeField] private TMP_Text mDescriptionText;

    private string mLastMessage;
    private bool mEventsRegistered;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Awake()
    {
        mLastMessage = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudDefaultHint);
        AutoBind();
    }

    private void OnEnable()
    {
        RegisterEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void Update()
    {
        if (TableNine.IsInitialized)
        {
            RefreshAll();
        }
    }

    private void AutoBind()
    {
        mCharacterNameText = mCharacterNameText != null ? mCharacterNameText : FindTmpText("CharacterName");
        mLifeNumberText = mLifeNumberText != null ? mLifeNumberText : FindTmpText("LifeNumber");
        mCoinNumberText = mCoinNumberText != null ? mCoinNumberText : FindTmpText("CoinNumber");
        mSkillText = mSkillText != null ? mSkillText : FindTmpText("SkillText");
        mRelicText = mRelicText != null ? mRelicText : FindTmpText("RelicText");
        mDescriptionText = mDescriptionText != null ? mDescriptionText : FindTmpText("DescriptionText");
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
            return;
        }

        if (mSkillText != null && string.IsNullOrWhiteSpace(mSkillText.text))
        {
            mSkillText.text = "技能";
        }

        if (mRelicText != null && string.IsNullOrWhiteSpace(mRelicText.text))
        {
            mRelicText.text = "遗物";
        }
    }

    private void RefreshDescription()
    {
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
