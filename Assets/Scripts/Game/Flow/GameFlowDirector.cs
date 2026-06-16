using System.Collections.Generic;
using QFramework;
using UnityEngine;

/// <summary>
/// 初版 Demo 的总流程编排器。
///
/// 职责（只编排，不持有游戏规则）：
/// 1. 进入场景后开一局「真实」运行（含开局发牌，不再 skipOpeningDeal）；GameOver/Victory 后提供重开。
/// 2. 监听领域层发出的「需要玩家做选择」覆盖层事件，路由到对应表现组件并回传选择命令：
///    - 三选一帮助卡 / 导师技能 → <see cref="BounceCardsWorldDemo"/> 多选一（默认按钮做跳过）
///    - 选房间 / 宝箱选遗物 / 商店 → <see cref="FlowChoiceButtonOverlay"/> 兜底按钮（暂无专属卡面）
///
/// 领域层始终是权威：本类只是把已有的 *RequestedEvent / *GeneratedEvent 接到表现层，
/// 并把玩家选择翻译回既有 Command，不重复发事件、不绕过 InputLock / Phase。
/// </summary>
[DefaultExecutionOrder(50)]
public sealed class GameFlowDirector : MonoBehaviour, IController
{
    private const string TutorCardIdPrefix = "tutor_";

    [SerializeField] private bool mAutoStartRun = true;
    [SerializeField] private BounceCardsWorldDemo mCardChooser;
    [SerializeField] private FlowChoiceButtonOverlay mButtonOverlay;

    private readonly List<IUnRegister> mRegisters = new List<IUnRegister>();
    private bool mSubscribed;

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

        ResolveReferences();
        Subscribe();

        if (mAutoStartRun && !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            this.SendCommand(new StartNewRunCommand());
        }
    }

    private void OnDestroy()
    {
        for (var i = 0; i < mRegisters.Count; i++)
        {
            mRegisters[i].UnRegister();
        }

        mRegisters.Clear();
        mSubscribed = false;
    }

    private void ResolveReferences()
    {
        if (mCardChooser == null)
        {
            mCardChooser = FindObjectOfType<BounceCardsWorldDemo>(true);
        }

        if (mButtonOverlay == null)
        {
            mButtonOverlay = FindObjectOfType<FlowChoiceButtonOverlay>(true);
        }

        if (mButtonOverlay == null)
        {
            var overlayObject = new GameObject("FlowChoiceButtonOverlay");
            overlayObject.transform.SetParent(transform, false);
            mButtonOverlay = overlayObject.AddComponent<FlowChoiceButtonOverlay>();
            mButtonOverlay.Hide();
        }
    }

    private void Subscribe()
    {
        if (mSubscribed)
        {
            return;
        }

        mSubscribed = true;
        mRegisters.Add(this.RegisterEvent<HelpRewardGeneratedEvent>(OnHelpRewardGenerated));
        mRegisters.Add(this.RegisterEvent<TutorSkillChoiceRequestedEvent>(OnTutorSkillRequested));
        mRegisters.Add(this.RegisterEvent<RoomChoiceRequestedEvent>(OnRoomChoiceRequested));
        mRegisters.Add(this.RegisterEvent<ChestRewardGeneratedEvent>(OnChestRewardGenerated));
        mRegisters.Add(this.RegisterEvent<ShopOpenedEvent>(OnShopOpened));
        mRegisters.Add(this.RegisterEvent<GameOverEvent>(OnGameOver));
        mRegisters.Add(this.RegisterEvent<VictoryEvent>(_ => OnVictory()));
        mRegisters.Add(this.RegisterEvent<FlowPhaseChangedEvent>(OnFlowPhaseChanged));
    }

    // ---------------------------------------------------------------------
    // 覆盖层路由
    // ---------------------------------------------------------------------

    private void OnHelpRewardGenerated(HelpRewardGeneratedEvent evt)
    {
        HideAllOverlays();

        var definitions = ResolveCardDefinitions(evt.CardIds);
        if (mCardChooser != null && definitions.Count > 0)
        {
            mCardChooser.BeginCardChoice(definitions, def => this.SendCommand(new PickHelpCardRewardCommand(def.CardId)));
            ShowSkipButton("三选一 · 帮助卡（点卡选择，或跳过换金币）",
                () => this.SendCommand(new SkipHelpRewardCommand()));
            return;
        }

        // 兜底：没有卡面时退按钮
        var options = new List<FlowChoiceButtonOverlay.Option>();
        for (var i = 0; i < evt.CardIds.Count; i++)
        {
            var cardId = evt.CardIds[i];
            options.Add(new FlowChoiceButtonOverlay.Option(
                ResolveCardLabel(cardId),
                () => this.SendCommand(new PickHelpCardRewardCommand(cardId))));
        }

        options.Add(new FlowChoiceButtonOverlay.Option("跳过（+金币）",
            () => this.SendCommand(new SkipHelpRewardCommand())));
        mButtonOverlay.Show("三选一 · 帮助卡", options);
    }

    private void OnTutorSkillRequested(TutorSkillChoiceRequestedEvent evt)
    {
        HideAllOverlays();

        var configModel = this.GetModel<IConfigModel>();
        var definitions = new List<CardDefinition>();
        var skillByCardId = new Dictionary<string, string>();
        for (var i = 0; i < evt.SkillIds.Count; i++)
        {
            var skillId = evt.SkillIds[i];
            if (configModel.TryGetCardDefinition(TutorCardIdPrefix + skillId, out var def) && def != null)
            {
                definitions.Add(def);
                skillByCardId[def.CardId] = skillId;
            }
        }

        if (mCardChooser != null && definitions.Count > 0)
        {
            mCardChooser.BeginCardChoice(definitions, def =>
            {
                if (skillByCardId.TryGetValue(def.CardId, out var skillId))
                {
                    this.SendCommand(new ChooseTutorSkillCommand(skillId));
                }
            });
            return;
        }

        var options = new List<FlowChoiceButtonOverlay.Option>();
        for (var i = 0; i < evt.SkillIds.Count; i++)
        {
            var skillId = evt.SkillIds[i];
            var skillDef = configModel.GetSkillDefinition(skillId);
            options.Add(new FlowChoiceButtonOverlay.Option(
                skillDef != null ? skillDef.DisplayName : skillId,
                () => this.SendCommand(new ChooseTutorSkillCommand(skillId))));
        }

        mButtonOverlay.Show("精英奖励 · 导师技能", options);
    }

    private void OnRoomChoiceRequested(RoomChoiceRequestedEvent evt)
    {
        HideAllOverlays();

        var configModel = this.GetModel<IConfigModel>();
        var options = new List<FlowChoiceButtonOverlay.Option>();
        for (var i = 0; i < evt.RoomIds.Count; i++)
        {
            var roomId = evt.RoomIds[i];
            var roomDef = configModel.GetRoomDefinition(roomId);
            var label = roomDef != null ? roomDef.DisplayName : roomId;
            options.Add(new FlowChoiceButtonOverlay.Option(
                label,
                () => this.SendCommand(new ChooseRoomCommand(roomId))));
        }

        mButtonOverlay.Show("选择下一个房间", options);
    }

    private void OnChestRewardGenerated(ChestRewardGeneratedEvent evt)
    {
        HideAllOverlays();

        var configModel = this.GetModel<IConfigModel>();
        var options = new List<FlowChoiceButtonOverlay.Option>();
        for (var i = 0; i < evt.RelicIds.Count; i++)
        {
            var relicId = evt.RelicIds[i];
            var relicDef = configModel.GetRelicDefinition(relicId);
            var label = relicDef != null ? relicDef.DisplayName : relicId;
            options.Add(new FlowChoiceButtonOverlay.Option(
                label,
                () => this.SendCommand(new PickRelicRewardCommand(relicId))));
        }

        options.Add(new FlowChoiceButtonOverlay.Option("跳过（+金币）",
            () => this.SendCommand(new SkipChestRewardCommand())));
        mButtonOverlay.Show("宝箱 · 选择遗物", options);
    }

    private void OnShopOpened(ShopOpenedEvent evt)
    {
        HideAllOverlays();

        var configModel = this.GetModel<IConfigModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var options = new List<FlowChoiceButtonOverlay.Option>();
        for (var i = 0; i < evt.CardIds.Count; i++)
        {
            var cardId = evt.CardIds[i];
            configModel.TryGetCardDefinition(cardId, out var def);
            var price = def != null ? def.Price : 0;
            var name = def != null ? def.DisplayName : cardId;
            options.Add(new FlowChoiceButtonOverlay.Option(
                $"购买 {name}（{price}金）",
                () => this.SendCommand(new BuyHelpCardCommand(cardId)),
                playerModel.Gold.Value >= price));
        }

        options.Add(new FlowChoiceButtonOverlay.Option("离开商店",
            () => this.SendCommand(new CloseShopCommand())));
        mButtonOverlay.Show("商店", options);
    }

    private void OnGameOver(GameOverEvent evt)
    {
        HideAllOverlays();
        var reason = string.IsNullOrEmpty(evt.Reason) ? "你被击败了" : evt.Reason;
        mButtonOverlay.Show($"游戏结束 · {reason}", new List<FlowChoiceButtonOverlay.Option>
        {
            new FlowChoiceButtonOverlay.Option("重新开始", RestartRun)
        });
    }

    private void OnVictory()
    {
        HideAllOverlays();
        mButtonOverlay.Show("胜利！通关达成", new List<FlowChoiceButtonOverlay.Option>
        {
            new FlowChoiceButtonOverlay.Option("再来一局", RestartRun)
        });
    }

    private void OnFlowPhaseChanged(FlowPhaseChangedEvent evt)
    {
        // 回到玩家可控阶段时，确保覆盖层都收掉（兜底，避免某条分支没显式关）。
        if (evt.NewPhase == FlowPhase.PlayerControl)
        {
            HideAllOverlays();
        }
    }

    // ---------------------------------------------------------------------
    // 辅助
    // ---------------------------------------------------------------------

    private void RestartRun()
    {
        HideAllOverlays();
        this.SendCommand(new StartNewRunCommand());
    }

    private void ShowSkipButton(string title, System.Action onSkip)
    {
        if (mButtonOverlay == null)
        {
            return;
        }

        mButtonOverlay.Show(title, new List<FlowChoiceButtonOverlay.Option>
        {
            new FlowChoiceButtonOverlay.Option("跳过（+金币）", onSkip)
        }, dimBackground: false, anchorBottom: true);
    }

    private void HideAllOverlays()
    {
        if (mButtonOverlay != null)
        {
            mButtonOverlay.Hide();
        }

        if (mCardChooser != null)
        {
            mCardChooser.HideChoice();
        }
    }

    private List<CardDefinition> ResolveCardDefinitions(IReadOnlyList<string> cardIds)
    {
        var configModel = this.GetModel<IConfigModel>();
        var result = new List<CardDefinition>();
        if (cardIds == null)
        {
            return result;
        }

        for (var i = 0; i < cardIds.Count; i++)
        {
            if (configModel.TryGetCardDefinition(cardIds[i], out var def) && def != null)
            {
                result.Add(def);
            }
        }

        return result;
    }

    private string ResolveCardLabel(string cardId)
    {
        var configModel = this.GetModel<IConfigModel>();
        return configModel.TryGetCardDefinition(cardId, out var def) && def != null
            ? def.DisplayName
            : cardId;
    }
}
