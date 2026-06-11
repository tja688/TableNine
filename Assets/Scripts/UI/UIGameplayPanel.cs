using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;

public sealed class UIGameplayPanelData : UIPanelData
{
}

public sealed class UIGameplayPanel : UIPanel, IController
{
    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private TMP_Text mCharacterNameText;
    [SerializeField] private TMP_Text mLifeNumberText;
    [SerializeField] private TMP_Text mCoinNumberText;
    [SerializeField] private TMP_Text mSkillText;
    [SerializeField] private TMP_Text mRelicText;
    [SerializeField] private TMP_Text mDescriptionText;

    private string mLastMessage = "左键点击玩家正交相邻格交互，拾取后的帮助卡点击下方道具槽使用。";

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    protected override void OnInit(IUIData uiData = null)
    {
        AutoBind();
        RegisterEvents();
        RefreshAll();
    }

    protected override void OnOpen(IUIData uiData = null)
    {
        RefreshAll();
    }

    protected override void OnClose()
    {
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
        mEventRegisters.Add(this.RegisterEvent<GameplayMessageEvent>(evt =>
        {
            mLastMessage = evt.Message;
            RefreshDescription();
        }));

        mEventRegisters.Add(this.RegisterEvent<LevelClearReadyEvent>(evt =>
        {
            mLastMessage = $"第 {evt.Layer} 层第 {evt.NodeInLayer} 节点已清空。";
            RefreshDescription();
        }));

        mEventRegisters.Add(this.RegisterEvent<MonsterKilledEvent>(_ =>
        {
            mLastMessage = "怪物被击杀，获得 5 金币。";
            RefreshAll();
        }));

        mEventRegisters.Add(this.RegisterEvent<BattleDeckChangedEvent>(_ => RefreshAll()));
        mEventRegisters.Add(this.RegisterEvent<DamageAppliedEvent>(_ => RefreshAll()));
        mEventRegisters.Add(this.RegisterEvent<ItemSlotChangedEvent>(_ => RefreshAll()));
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
            mDescriptionText.text = "正在准备首个可玩节点。";
            return;
        }

        var deckModel = this.GetModel<IDeckModel>();
        var flowModel = this.GetModel<IFlowModel>();
        if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.ThrowingKnifeTarget)
        {
            mDescriptionText.text = "飞刀待命：点击任意怪物结算 6 点伤害。";
            return;
        }

        if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.AttributeChoice)
        {
            mDescriptionText.text = "属性提升卡：请在覆盖层中选择属性。";
            return;
        }

        if (flowModel.Phase.Value == FlowPhase.ClearReady)
        {
            mDescriptionText.text = "节点已清空。你仍可拾取或使用帮助卡。";
            return;
        }

        mDescriptionText.text = $"[{flowModel.Phase.Value}] 牌堆 {deckModel.BattleDrawPile.Count}\n{mLastMessage}";
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
