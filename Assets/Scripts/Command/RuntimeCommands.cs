using System.Collections.Generic;
using QFramework;

public sealed class StartNewRunCommand : AbstractCommand
{
    public StartNewRunCommand(string characterId = DefaultGameConfigFactory.CharacterImpId, int? seedOverride = null)
    {
        CharacterId = characterId;
        SeedOverride = seedOverride;
    }

    public string CharacterId { get; }
    public int? SeedOverride { get; }

    protected override void OnExecute()
    {
        var configModel = this.GetModel<IConfigModel>();
        var runModel = this.GetModel<IRunModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var boardModel = this.GetModel<IBoardModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var flowModel = this.GetModel<IFlowModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();
        var runSystem = this.GetSystem<IRunSystem>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        var characterDefinition = configModel.GetCharacterDefinition(CharacterId);
        var seed = runSystem.ResolveSeed(SeedOverride);

        randomUtility.SetSeed(seed);
        collectionModel.Clear();
        deckModel.ResetForNewRun();
        boardModel.Clear();
        flowModel.Reset();

        runModel.StartRun(CharacterId, seed);
        flowModel.SetPhase(FlowPhase.RunStarting);

        var playerRuntime = collectionModel.CreatePlayerCard(characterDefinition);
        playerModel.ResetFromCharacter(characterDefinition, playerRuntime.Uid);
        boardSystem.ResetBoardWithPlayer(playerRuntime.Uid);

        for (var i = 0; i < characterDefinition.InitialHelpCardIds.Count; i++)
        {
            var helpDefinition = configModel.GetCardDefinition(characterDefinition.InitialHelpCardIds[i]);
            var helpRuntime = collectionModel.CreateCard(helpDefinition);
            deckModel.OwnedHelpCards.Add(helpRuntime.Uid);
            deckModel.HelpCardStates[helpRuntime.Uid.Value] = new HelpCardState
            {
                Uid = helpRuntime.Uid,
                DefinitionId = helpRuntime.DefinitionId
            };
        }

        this.SendCommand(new StartNodeCommand(1, 1));
    }
}

public sealed class StartNodeCommand : AbstractCommand
{
    public StartNodeCommand(int layer, int nodeInLayer)
    {
        Layer = layer;
        NodeInLayer = nodeInLayer;
    }

    public int Layer { get; }
    public int NodeInLayer { get; }

    protected override void OnExecute()
    {
        var runModel = this.GetModel<IRunModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        var deckSystem = this.GetSystem<IDeckSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        runModel.SetNode(Layer, NodeInLayer);
        deckModel.ClearNodeState();
        flowModel.SetPhase(FlowPhase.NodeStarting);
        deckSystem.GenerateDemonDeck(Layer, NodeInLayer);
        deckSystem.SnapshotHelpDeck();
        boardSystem.ResetBoardWithPlayer(playerModel.PlayerCardUid);
        this.SendCommand(new DealOpeningCardsCommand());
    }
}

public sealed class DealOpeningCardsCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        var randomUtility = this.GetUtility<IRandomUtility>();
        var deckSystem = this.GetSystem<IDeckSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        flowModel.SetPhase(FlowPhase.OpeningDeal);

        var emptySlots = new List<BoardSlotNo>(boardSystem.GetEmptySlots());
        randomUtility.Shuffle(emptySlots);

        var helpCandidates = new List<CardUid>();
        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (deckModel.HelpCardStates.TryGetValue(uid.Value, out var state) &&
                !state.IsPermanentlyRemoved &&
                !state.IsTemporarilyRemoved)
            {
                helpCandidates.Add(uid);
            }
        }

        randomUtility.Shuffle(helpCandidates);

        var helpOpeningCount = helpCandidates.Count < 3 ? helpCandidates.Count : 3;
        for (var i = 0; i < helpOpeningCount; i++)
        {
            var uid = helpCandidates[i];
            boardSystem.PlaceCard(uid, emptySlots[0], CardPlacementSource.OpeningHelp);
            emptySlots.RemoveAt(0);
        }

        var demonOpeningCount = deckModel.DemonDeckQueue.Count < 3 ? deckModel.DemonDeckQueue.Count : 3;
        for (var i = 0; i < demonOpeningCount; i++)
        {
            var uid = deckModel.DemonDeckQueue.Dequeue();
            boardSystem.PlaceCard(uid, emptySlots[0], CardPlacementSource.OpeningDemon);
            emptySlots.RemoveAt(0);
        }

        var battlePool = new List<CardUid>();
        for (var i = helpOpeningCount; i < helpCandidates.Count; i++)
        {
            battlePool.Add(helpCandidates[i]);
        }

        while (deckModel.DemonDeckQueue.Count > 0)
        {
            battlePool.Add(deckModel.DemonDeckQueue.Dequeue());
        }

        randomUtility.Shuffle(battlePool);
        deckModel.BattleDrawPile.Clear();
        for (var i = 0; i < battlePool.Count; i++)
        {
            deckModel.BattleDrawPile.Enqueue(battlePool[i]);
        }

        var battleOpeningCount = deckModel.BattleDrawPile.Count < 2 ? deckModel.BattleDrawPile.Count : 2;
        for (var i = 0; i < battleOpeningCount; i++)
        {
            var uid = deckModel.BattleDrawPile.Dequeue();
            boardSystem.PlaceCard(uid, emptySlots[0], CardPlacementSource.OpeningBattle);
            emptySlots.RemoveAt(0);
        }

        deckSystem.UpdateNextBattlePreview();
        flowModel.SetPhase(FlowPhase.PlayerControl);
    }
}

public sealed class ClickBoardSlotCommand : AbstractCommand
{
    public ClickBoardSlotCommand(BoardSlotNo slot)
    {
        Slot = slot;
    }

    public BoardSlotNo Slot { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        if (deckModel.PendingHelpCardAction.IsActive)
        {
            switch (deckModel.PendingHelpCardAction.Kind)
            {
                case PendingHelpCardActionKind.ThrowingKnifeTarget:
                    this.SendCommand(new ResolveThrowingKnifeTargetCommand(Slot));
                    break;
            }

            return;
        }

        var interaction = this.SendQuery(new CanInteractBoardSlotQuery(Slot));
        if (!interaction.CanInteract)
        {
            return;
        }

        var boardSystem = this.GetSystem<IBoardSystem>();
        switch (interaction.Kind)
        {
            case InteractionKind.EmptySlot:
                boardSystem.RotateClockwise();
                this.SendCommand(new RequestRefillBoardCommand());
                break;
            case InteractionKind.HelpCard:
                this.SendCommand(new PickHelpCardToItemSlotCommand(interaction.TargetUid));
                boardSystem.RotateClockwise();
                this.SendCommand(new RequestRefillBoardCommand());
                break;
            case InteractionKind.MonsterCard:
                this.SendCommand(new StartCombatCommand(interaction.TargetUid));
                break;
        }
    }
}

public sealed class ClickItemSlotCommand : AbstractCommand
{
    public ClickItemSlotCommand(int itemSlotIndex)
    {
        ItemSlotIndex = itemSlotIndex;
    }

    public int ItemSlotIndex { get; }

    protected override void OnExecute()
    {
        var flowModel = this.GetModel<IFlowModel>();
        if (flowModel.IsInputLocked)
        {
            return;
        }

        var deckModel = this.GetModel<IDeckModel>();
        if (deckModel.PendingHelpCardAction.IsActive ||
            ItemSlotIndex < 0 ||
            ItemSlotIndex >= deckModel.ItemSlots.Length ||
            !deckModel.ItemSlots[ItemSlotIndex].HasValue)
        {
            return;
        }

        this.SendCommand(new UseHelpCardCommand(deckModel.ItemSlots[ItemSlotIndex].Value));
    }
}

public sealed class PickHelpCardToItemSlotCommand : AbstractCommand
{
    public PickHelpCardToItemSlotCommand(CardUid helpCardUid)
    {
        HelpCardUid = helpCardUid;
    }

    public CardUid HelpCardUid { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        var itemSlotIndex = deckModel.FindFirstEmptyItemSlot();
        if (itemSlotIndex < 0)
        {
            this.SendEvent(new PopupRequestedEvent("道具牌格已满。"));
            return;
        }

        var runtime = collectionModel.GetCard(HelpCardUid);
        if (runtime.BoardSlot.HasValue)
        {
            boardSystem.RemoveCardAt(runtime.BoardSlot.Value);
        }

        deckModel.ItemSlots[itemSlotIndex] = HelpCardUid;
        runtime.ItemSlotIndex = itemSlotIndex;
        runtime.BoardSlot = null;

        if (deckModel.HelpCardStates.TryGetValue(HelpCardUid.Value, out var state))
        {
            state.IsOnBoard = false;
            state.IsInItemSlot = true;
        }

        this.SendEvent(new ItemSlotChangedEvent(itemSlotIndex, HelpCardUid));
    }
}

public sealed class RequestRefillBoardCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var boardModel = this.GetModel<IBoardModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        if (boardModel.GetEmptySlots().Count == 0)
        {
            this.SendCommand(new CheckClearConditionCommand());
            return;
        }

        if (deckModel.RefillRunning)
        {
            deckModel.RefillPending = true;
            return;
        }

        deckModel.RefillRunning = true;
        deckModel.RefillPending = false;
        inputLockSystem.Lock(InputLockReason.BoardRefillRunning);
        flowModel.SetPhase(FlowPhase.BoardRefilling);

        this.GetUtility<ISequenceUtility>().Run(() => this.SendCommand(new RefillBoardCommand()));
    }
}

public sealed class RefillBoardCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var boardModel = this.GetModel<IBoardModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        var deckSystem = this.GetSystem<IDeckSystem>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        do
        {
            deckModel.RefillPending = false;
            var emptySlots = boardModel.GetEmptySlots();
            for (var i = 0; i < emptySlots.Count && deckModel.BattleDrawPile.Count > 0; i++)
            {
                var uid = deckModel.BattleDrawPile.Dequeue();
                boardSystem.PlaceCard(uid, emptySlots[i], CardPlacementSource.Refill);
            }
        }
        while (deckModel.RefillPending && boardModel.GetEmptySlots().Count > 0 && deckModel.BattleDrawPile.Count > 0);

        deckModel.RefillRunning = false;
        deckModel.RefillPending = false;
        deckSystem.UpdateNextBattlePreview();
        inputLockSystem.Unlock(InputLockReason.BoardRefillRunning);

        if (deckModel.PendingTutorSkillChoice)
        {
            deckModel.PendingTutorSkillChoice = false;
            var rewardSystem = this.GetSystem<IRewardSystem>();
            var rewardModel = this.GetModel<IRewardModel>();
            rewardSystem.GenerateTutorSkillCandidates();
            if (rewardModel.TutorSkillIds.Count > 0)
            {
                flowModel.SetPhase(FlowPhase.TutorSkillChoosing);
                inputLockSystem.Lock(InputLockReason.OverlayVisible);
                this.SendEvent(new TutorSkillChoiceRequestedEvent(rewardModel.TutorSkillIds));
                return;
            }
        }

        flowModel.SetPhase(FlowPhase.PlayerControl);
        this.SendCommand(new CheckClearConditionCommand());
    }
}

public sealed class StartCombatCommand : AbstractCommand
{
    public StartCombatCommand(CardUid monsterUid)
    {
        MonsterUid = monsterUid;
    }

    public CardUid MonsterUid { get; }

    protected override void OnExecute()
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(MonsterUid, out var monsterRuntime))
        {
            return;
        }

        var playerModel = this.GetModel<IPlayerModel>();
        var flowModel = this.GetModel<IFlowModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var combatSystem = this.GetSystem<ICombatSystem>();
        var boardSystem = this.GetSystem<IBoardSystem>();

        inputLockSystem.Lock(InputLockReason.CombatResolving);
        flowModel.SetPhase(FlowPhase.CombatResolving);

        var playerStats = this.SendQuery(new GetEffectivePlayerStatsQuery());
        var monsterStats = this.SendQuery(new GetEffectiveMonsterStatsQuery(MonsterUid));
        var playerActsFirst = combatSystem.PlayerActsFirst(playerStats, monsterStats);
        var playerUid = playerModel.PlayerCardUid;

        if (playerActsFirst)
        {
            var playerDamage = combatSystem.CalculateDamage(playerStats, monsterStats);
            this.SendCommand(new ApplyDamageCommand(MonsterUid, playerDamage));
            if (collectionModel.TryGetCard(MonsterUid, out monsterRuntime) && monsterRuntime.CurrentHp <= 0)
            {
                this.SendCommand(new KillMonsterCommand(MonsterUid));
            }
            else
            {
                var updatedMonsterStats = this.SendQuery(new GetEffectiveMonsterStatsQuery(MonsterUid));
                var monsterDamage = combatSystem.CalculateDamage(updatedMonsterStats, playerStats);
                this.SendCommand(new ApplyDamageCommand(playerUid, monsterDamage));
            }
        }
        else
        {
            var monsterDamage = combatSystem.CalculateDamage(monsterStats, playerStats);
            this.SendCommand(new ApplyDamageCommand(playerUid, monsterDamage));
            var updatedPlayerStats = this.SendQuery(new GetEffectivePlayerStatsQuery());
            if (updatedPlayerStats.CurrentHp > 0)
            {
                var playerDamage = combatSystem.CalculateDamage(updatedPlayerStats, monsterStats);
                this.SendCommand(new ApplyDamageCommand(MonsterUid, playerDamage));
                if (collectionModel.TryGetCard(MonsterUid, out monsterRuntime) && monsterRuntime.CurrentHp <= 0)
                {
                    this.SendCommand(new KillMonsterCommand(MonsterUid));
                }
            }
        }

        inputLockSystem.Unlock(InputLockReason.CombatResolving);
        if (this.SendQuery(new GetEffectivePlayerStatsQuery()).CurrentHp <= 0)
        {
            flowModel.SetPhase(FlowPhase.GameOver);
            return;
        }

        boardSystem.RotateClockwise();
        this.SendCommand(new RequestRefillBoardCommand());
    }
}

public sealed class ApplyDamageCommand : AbstractCommand
{
    public ApplyDamageCommand(CardUid targetUid, int damage)
    {
        TargetUid = targetUid;
        Damage = damage;
    }

    public CardUid TargetUid { get; }
    public int Damage { get; }

    protected override void OnExecute()
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(TargetUid, out var runtime))
        {
            return;
        }

        runtime.CurrentHp -= Damage;
        if (runtime.CurrentHp < 0)
        {
            runtime.CurrentHp = 0;
        }

        this.SendEvent(new DamageAppliedEvent(TargetUid, Damage));
    }
}

public sealed class KillMonsterCommand : AbstractCommand
{
    public KillMonsterCommand(CardUid monsterUid)
    {
        MonsterUid = monsterUid;
    }

    public CardUid MonsterUid { get; }

    protected override void OnExecute()
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var deckSystem = this.GetSystem<IDeckSystem>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        if (!collectionModel.TryGetCard(MonsterUid, out var monsterRuntime))
        {
            return;
        }

        var monsterLevel = monsterRuntime.MonsterLevel;

        if (monsterRuntime.BoardSlot.HasValue)
        {
            boardSystem.RemoveCardAt(monsterRuntime.BoardSlot.Value);
        }

        this.ChangeGold(playerModel, RewardConstants.MonsterKillGold);
        collectionModel.RemoveCard(MonsterUid);
        this.SendEvent(new MonsterKilledEvent(MonsterUid, monsterRuntime.DefinitionId));

        if (monsterLevel == MonsterLevel.Elite)
        {
            deckSystem.InjectHelpCardsToBattleDeck(new[]
            {
                DefaultGameConfigFactory.HelpBlueChestId,
                DefaultGameConfigFactory.HelpGoldCardId,
                DefaultGameConfigFactory.HelpAttributeUpId
            });
            deckModel.PendingTutorSkillChoice = true;
        }
        else if (monsterLevel == MonsterLevel.Boss)
        {
            deckSystem.InjectHelpCardsToBattleDeck(new[]
            {
                DefaultGameConfigFactory.HelpGoldChestId,
                DefaultGameConfigFactory.HelpGoldCardId,
                DefaultGameConfigFactory.HelpGoldCardId,
                DefaultGameConfigFactory.HelpAttributeUpId
            });
        }
    }
}

public sealed class CheckClearConditionCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var deckSystem = this.GetSystem<IDeckSystem>();
        var flowModel = this.GetModel<IFlowModel>();
        var runModel = this.GetModel<IRunModel>();
        if (!deckSystem.HasMonsterRemaining() && flowModel.Phase.Value != FlowPhase.ClearReady)
        {
            var rewardSystem = this.GetSystem<IRewardSystem>();
            var rewardModel = this.GetModel<IRewardModel>();
            flowModel.SetPhase(FlowPhase.ClearReady);
            rewardSystem.GenerateRoomCandidates();
            this.SendEvent(new LevelClearReadyEvent(runModel.Layer.Value, runModel.NodeInLayer.Value));
            this.SendEvent(new RoomChoiceRequestedEvent(rewardModel.RoomCandidateIds));
        }
    }
}

public sealed class UseHelpCardCommand : AbstractCommand
{
    public UseHelpCardCommand(CardUid helpCardUid)
    {
        HelpCardUid = helpCardUid;
    }

    public CardUid HelpCardUid { get; }

    protected override void OnExecute()
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(HelpCardUid, out var helpRuntime) || helpRuntime.CardType != CardType.Help)
        {
            return;
        }

        var configModel = this.GetModel<IConfigModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var flowModel = this.GetModel<IFlowModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var helpDefinition = configModel.GetCardDefinition(helpRuntime.DefinitionId);

        switch (helpDefinition.CardId)
        {
            case DefaultGameConfigFactory.HelpPotionId:
            {
                var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
                playerRuntime.CurrentHp += 10;
                if (playerRuntime.CurrentHp > playerRuntime.MaxHp)
                {
                    playerRuntime.CurrentHp = playerRuntime.MaxHp;
                }

                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgPotionHeal)));
                this.SendCommand(new ConsumeHelpCardCommand(HelpCardUid, helpDefinition.IsPermanentRemoveOnUse));
                break;
            }
            case DefaultGameConfigFactory.HelpThrowingKnifeId:
                deckModel.PendingHelpCardAction.HelpCardUid = HelpCardUid;
                deckModel.PendingHelpCardAction.Kind = PendingHelpCardActionKind.ThrowingKnifeTarget;
                flowModel.SetPhase(FlowPhase.PlayerControl);
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgThrowingKnifeSelect)));
                break;
            case DefaultGameConfigFactory.HelpAttributeUpId:
                deckModel.PendingHelpCardAction.HelpCardUid = HelpCardUid;
                deckModel.PendingHelpCardAction.Kind = PendingHelpCardActionKind.AttributeChoice;
                inputLockSystem.Lock(InputLockReason.OverlayVisible);
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgAttributeSelect)));
                this.SendEvent(new AttributeChoiceRequestedEvent(HelpCardUid));
                break;
            case DefaultGameConfigFactory.HelpCommonChestId:
            case DefaultGameConfigFactory.HelpChestCardId:
            case DefaultGameConfigFactory.HelpBlueChestId:
            case DefaultGameConfigFactory.HelpGoldChestId:
            {
                var rewardModel = this.GetModel<IRewardModel>();
                rewardModel.CurrentRewardSource = RewardSource.ChestCard;
                flowModel.SetPhase(FlowPhase.ChestRewardChoosing);
                inputLockSystem.Lock(InputLockReason.OverlayVisible);
                this.GetSystem<IRelicSystem>().GenerateChestRewardCandidates();
                this.SendEvent(new ChestRewardGeneratedEvent(
                    this.GetModel<IRewardModel>().ChestRewardRelicIds));
                this.SendCommand(new ConsumeHelpCardCommand(HelpCardUid, helpDefinition.IsPermanentRemoveOnUse));
                break;
            }
            case DefaultGameConfigFactory.HelpGoldCardId:
            {
                this.ChangeGold(playerModel, 50);
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
                    DescriptionPanelTextKeys.MsgRoomGold,
                    50)));
                this.SendCommand(new ConsumeHelpCardCommand(HelpCardUid, true));
                break;
            }
            default:
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
                    DescriptionPanelTextKeys.MsgNotImplemented,
                    helpDefinition.DisplayName)));
                break;
        }
    }
}

public sealed class ResolveThrowingKnifeTargetCommand : AbstractCommand
{
    public ResolveThrowingKnifeTargetCommand(BoardSlotNo slot)
    {
        Slot = slot;
    }

    public BoardSlotNo Slot { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        if (deckModel.PendingHelpCardAction.Kind != PendingHelpCardActionKind.ThrowingKnifeTarget)
        {
            return;
        }

        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var targetUid = boardModel.GetCardAt(Slot);
        if (!targetUid.HasValue || !collectionModel.TryGetCard(targetUid.Value, out var targetRuntime) || targetRuntime.CardType != CardType.Monster)
        {
            return;
        }

        var helpCardUid = deckModel.PendingHelpCardAction.HelpCardUid;
        deckModel.PendingHelpCardAction.Clear();

        this.SendCommand(new ApplyDamageCommand(targetUid.Value, 6));
        var targetDied = collectionModel.TryGetCard(targetUid.Value, out targetRuntime) && targetRuntime.CurrentHp <= 0;
        if (targetDied)
        {
            this.SendCommand(new KillMonsterCommand(targetUid.Value));
        }

        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
            DescriptionPanelTextKeys.MsgThrowingKnifeHit,
            targetRuntime.DisplayName)));
        this.SendCommand(new ConsumeHelpCardCommand(helpCardUid, true));

        if (targetDied)
        {
            this.SendCommand(new RequestRefillBoardCommand());
        }
        else
        {
            this.SendCommand(new CheckClearConditionCommand());
        }
    }
}

public sealed class ResolveAttributeChoiceCommand : AbstractCommand
{
    public ResolveAttributeChoiceCommand(AttributeUpgradeChoice choice)
    {
        Choice = choice;
    }

    public AttributeUpgradeChoice Choice { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        if (deckModel.PendingHelpCardAction.Kind != PendingHelpCardActionKind.AttributeChoice)
        {
            return;
        }

        var playerModel = this.GetModel<IPlayerModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
        var message = string.Empty;

        switch (Choice)
        {
            case AttributeUpgradeChoice.Attack:
                playerRuntime.BaseAttack += 1;
                message = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgAttrAttack);
                break;
            case AttributeUpgradeChoice.Defense:
                playerRuntime.BaseDefense += 1;
                message = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgAttrDefense);
                break;
            case AttributeUpgradeChoice.MaxHp:
                playerRuntime.MaxHp += 2;
                playerRuntime.CurrentHp += 2;
                message = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgAttrMaxHp);
                break;
        }

        var helpCardUid = deckModel.PendingHelpCardAction.HelpCardUid;
        deckModel.PendingHelpCardAction.Clear();
        inputLockSystem.Unlock(InputLockReason.OverlayVisible);

        this.SendEvent(new GameplayMessageEvent(message));
        this.SendEvent(new AttributeChoiceResolvedEvent(helpCardUid, Choice));
        this.SendCommand(new ConsumeHelpCardCommand(helpCardUid, true));
    }
}

public sealed class ConsumeHelpCardCommand : AbstractCommand
{
    public ConsumeHelpCardCommand(CardUid helpCardUid, bool permanentlyRemove)
    {
        HelpCardUid = helpCardUid;
        PermanentlyRemove = permanentlyRemove;
    }

    public CardUid HelpCardUid { get; }
    public bool PermanentlyRemove { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        if (!collectionModel.TryGetCard(HelpCardUid, out var helpRuntime))
        {
            return;
        }

        if (helpRuntime.BoardSlot.HasValue)
        {
            boardSystem.RemoveCardAt(helpRuntime.BoardSlot.Value);
        }

        if (helpRuntime.ItemSlotIndex.HasValue)
        {
            var itemSlotIndex = helpRuntime.ItemSlotIndex.Value;
            deckModel.ItemSlots[helpRuntime.ItemSlotIndex.Value] = null;
            this.SendEvent(new ItemSlotChangedEvent(itemSlotIndex, null));
        }

        helpRuntime.BoardSlot = null;
        helpRuntime.ItemSlotIndex = null;

        if (!deckModel.HelpCardStates.TryGetValue(HelpCardUid.Value, out var state))
        {
            return;
        }

        state.IsOnBoard = false;
        state.IsInItemSlot = false;
        state.IsTemporarilyRemoved = !PermanentlyRemove;
        state.IsPermanentlyRemoved = PermanentlyRemove;
    }
}

// ============================================================
// M3: Reward Loop Commands
// ============================================================

public sealed class ChooseRoomCommand : AbstractCommand
{
    public ChooseRoomCommand(string roomId)
    {
        RoomId = roomId;
    }

    public string RoomId { get; }

    protected override void OnExecute()
    {
        var configModel = this.GetModel<IConfigModel>();
        var flowModel = this.GetModel<IFlowModel>();
        var rewardSystem = this.GetSystem<IRewardSystem>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var playerModel = this.GetModel<IPlayerModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();

        // 1. Settle unused help cards (+10 gold each)
        rewardSystem.SettleUnusedHelpCards();

        // 2. Restore help deck snapshot (temp removed cards come back, item slots cleared)
        rewardSystem.RestoreHelpDeckSnapshot();

        // 3. Get room definition and apply effect
        var roomDef = configModel.GetRoomDefinition(RoomId);
        this.SendEvent(new RoomChosenEvent(RoomId, roomDef.RoomType));

        switch (roomDef.RoomType)
        {
            case RoomType.Gold:
                this.ChangeGold(playerModel, roomDef.RewardGold);
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
                    DescriptionPanelTextKeys.MsgRoomGold,
                    roomDef.RewardGold)));
                flowModel.SetPhase(FlowPhase.HelpRewardChoosing);
                inputLockSystem.Lock(InputLockReason.OverlayVisible);
                rewardSystem.GenerateHelpRewardCandidates();
                this.SendEvent(new HelpRewardGeneratedEvent(
                    this.GetModel<IRewardModel>().HelpRewardCardIds));
                break;

            case RoomType.Chest:
                flowModel.SetPhase(FlowPhase.ChestRewardChoosing);
                inputLockSystem.Lock(InputLockReason.OverlayVisible);
                this.GetSystem<IRelicSystem>().GenerateChestRewardCandidates();
                this.SendEvent(new ChestRewardGeneratedEvent(
                    this.GetModel<IRewardModel>().ChestRewardRelicIds));
                break;

            case RoomType.Attribute:
                if (!string.IsNullOrEmpty(roomDef.InjectCardId))
                {
                    var attrDef = configModel.GetCardDefinition(roomDef.InjectCardId);
                    var attrRuntime = collectionModel.CreateCard(attrDef);
                    deckModel.OwnedHelpCards.Add(attrRuntime.Uid);
                    deckModel.HelpCardStates[attrRuntime.Uid.Value] = new HelpCardState
                    {
                        Uid = attrRuntime.Uid,
                        DefinitionId = attrRuntime.DefinitionId
                    };
                    this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
                        DescriptionPanelTextKeys.MsgRoomAttribute,
                        attrDef.DisplayName)));
                }
                flowModel.SetPhase(FlowPhase.HelpRewardChoosing);
                inputLockSystem.Lock(InputLockReason.OverlayVisible);
                rewardSystem.GenerateHelpRewardCandidates();
                this.SendEvent(new HelpRewardGeneratedEvent(
                    this.GetModel<IRewardModel>().HelpRewardCardIds));
                break;

            case RoomType.Shop:
                flowModel.SetPhase(FlowPhase.Shop);
                inputLockSystem.Lock(InputLockReason.OverlayVisible);
                this.GetSystem<IShopSystem>().GenerateShopCards();
                this.SendEvent(new ShopOpenedEvent(
                    this.GetModel<IRewardModel>().ShopCardIds));
                break;
        }
    }
}

public sealed class GenerateHelpRewardCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var flowModel = this.GetModel<IFlowModel>();
        var rewardSystem = this.GetSystem<IRewardSystem>();
        var rewardModel = this.GetModel<IRewardModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();

        rewardModel.CurrentRewardSource = RewardSource.NodeClear;
        flowModel.SetPhase(FlowPhase.HelpRewardChoosing);
        inputLockSystem.Lock(InputLockReason.OverlayVisible);

        rewardSystem.GenerateHelpRewardCandidates();
        this.SendEvent(new HelpRewardGeneratedEvent(rewardModel.HelpRewardCardIds));
    }
}

public sealed class PickHelpCardRewardCommand : AbstractCommand
{
    public PickHelpCardRewardCommand(string cardId)
    {
        CardId = cardId;
    }

    public string CardId { get; }

    protected override void OnExecute()
    {
        var configModel = this.GetModel<IConfigModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var rewardSystem = this.GetSystem<IRewardSystem>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        if (!rewardSystem.CanAddHelpCard(CardId))
        {
            this.SendEvent(new PopupRequestedEvent("帮助卡组已满或同名卡达到上限。"));
            return;
        }

        var definition = configModel.GetCardDefinition(CardId);
        var runtime = collectionModel.CreateCard(definition);
        deckModel.OwnedHelpCards.Add(runtime.Uid);
        deckModel.HelpCardStates[runtime.Uid.Value] = new HelpCardState
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId
        };

        this.SendEvent(new HelpRewardPickedEvent(CardId));
        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
            DescriptionPanelTextKeys.MsgHelpCardGained,
            definition.DisplayName)));
        inputLockSystem.Unlock(InputLockReason.OverlayVisible);
        flowModel.SetPhase(FlowPhase.PlayerControl);
    }
}

public sealed class SkipHelpRewardCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        this.ChangeGold(playerModel, RewardConstants.SkipHelpRewardGold);
        this.SendEvent(new HelpRewardSkippedEvent(RewardConstants.SkipHelpRewardGold));
        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgHelpRewardSkip)));
        inputLockSystem.Unlock(InputLockReason.OverlayVisible);
        flowModel.SetPhase(FlowPhase.PlayerControl);
    }
}

public sealed class PickRelicRewardCommand : AbstractCommand
{
    public PickRelicRewardCommand(string relicId)
    {
        RelicId = relicId;
    }

    public string RelicId { get; }

    protected override void OnExecute()
    {
        var relicSystem = this.GetSystem<IRelicSystem>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        if (!relicSystem.AddRelic(RelicId))
        {
            return;
        }

        this.SendEvent(new RelicRewardPickedEvent(RelicId));

        if (this.GetModel<IRewardModel>().CurrentRewardSource == RewardSource.ChestCard)
        {
            this.GetModel<IRewardModel>().CurrentRewardSource = RewardSource.None;
            inputLockSystem.Unlock(InputLockReason.OverlayVisible);
            flowModel.SetPhase(FlowPhase.PlayerControl);
            this.SendCommand(new CheckClearConditionCommand());
            return;
        }

        // After chest room reward, go to help reward
        flowModel.SetPhase(FlowPhase.HelpRewardChoosing);
        inputLockSystem.Lock(InputLockReason.OverlayVisible);
        this.GetSystem<IRewardSystem>().GenerateHelpRewardCandidates();
        this.SendEvent(new HelpRewardGeneratedEvent(
            this.GetModel<IRewardModel>().HelpRewardCardIds));
    }
}

public sealed class SkipChestRewardCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        this.ChangeGold(playerModel, RewardConstants.SkipChestRewardGold);
        this.SendEvent(new ChestRewardSkippedEvent(RewardConstants.SkipChestRewardGold));
        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgChestSkip)));

        if (this.GetModel<IRewardModel>().CurrentRewardSource == RewardSource.ChestCard)
        {
            this.GetModel<IRewardModel>().CurrentRewardSource = RewardSource.None;
            inputLockSystem.Unlock(InputLockReason.OverlayVisible);
            flowModel.SetPhase(FlowPhase.PlayerControl);
            this.SendCommand(new CheckClearConditionCommand());
            return;
        }

        // After chest room skip, go to help reward
        flowModel.SetPhase(FlowPhase.HelpRewardChoosing);
        inputLockSystem.Lock(InputLockReason.OverlayVisible);
        this.GetSystem<IRewardSystem>().GenerateHelpRewardCandidates();
        this.SendEvent(new HelpRewardGeneratedEvent(
            this.GetModel<IRewardModel>().HelpRewardCardIds));
    }
}

public sealed class ProceedToNextNodeCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var runModel = this.GetModel<IRunModel>();
        var flowModel = this.GetModel<IFlowModel>();

        this.SendEvent(new NodeCompletedEvent(runModel.Layer.Value, runModel.NodeInLayer.Value));

        var nextNode = runModel.NodeInLayer.Value + 1;
        if (nextNode > 9)
        {
            if (runModel.Layer.Value >= 3)
            {
                flowModel.SetPhase(FlowPhase.Victory);
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgVictory)));
            }
            else
            {
                flowModel.SetPhase(FlowPhase.LayerComplete);
                this.SendEvent(new LayerCompletedEvent(runModel.Layer.Value));
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
                    DescriptionPanelTextKeys.MsgLayerComplete,
                    runModel.Layer.Value)));
            }

            return;
        }

        this.SendEvent(new NodeAdvancedEvent(runModel.Layer.Value, runModel.NodeInLayer.Value, nextNode));
        this.SendCommand(new StartNodeCommand(runModel.Layer.Value, nextNode));
    }
}

public sealed class OpenShopCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var flowModel = this.GetModel<IFlowModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var shopSystem = this.GetSystem<IShopSystem>();
        var rewardModel = this.GetModel<IRewardModel>();

        flowModel.SetPhase(FlowPhase.Shop);
        inputLockSystem.Lock(InputLockReason.OverlayVisible);

        shopSystem.GenerateShopCards();
        this.SendEvent(new ShopOpenedEvent(rewardModel.ShopCardIds));
    }
}

public sealed class BuyHelpCardCommand : AbstractCommand
{
    public BuyHelpCardCommand(string cardId)
    {
        CardId = cardId;
    }

    public string CardId { get; }

    protected override void OnExecute()
    {
        var shopSystem = this.GetSystem<IShopSystem>();
        shopSystem.BuyHelpCard(CardId);
    }
}

public sealed class DeleteHelpCardForGoldCommand : AbstractCommand
{
    public DeleteHelpCardForGoldCommand(CardUid helpCardUid)
    {
        HelpCardUid = helpCardUid;
    }

    public CardUid HelpCardUid { get; }

    protected override void OnExecute()
    {
        var shopSystem = this.GetSystem<IShopSystem>();
        shopSystem.DeleteHelpCardForGold(HelpCardUid);
    }
}

public sealed class CloseShopCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var flowModel = this.GetModel<IFlowModel>();
        var rewardSystem = this.GetSystem<IRewardSystem>();
        var rewardModel = this.GetModel<IRewardModel>();

        // After shop, go to help reward
        flowModel.SetPhase(FlowPhase.HelpRewardChoosing);
        inputLockSystem.Lock(InputLockReason.OverlayVisible);
        rewardSystem.GenerateHelpRewardCandidates();
        this.SendEvent(new HelpRewardGeneratedEvent(rewardModel.HelpRewardCardIds));
    }
}

public sealed class DiscardRelicCommand : AbstractCommand
{
    public DiscardRelicCommand(string relicId)
    {
        RelicId = relicId;
    }

    public string RelicId { get; }

    protected override void OnExecute()
    {
        var relicSystem = this.GetSystem<IRelicSystem>();
        relicSystem.DiscardRelic(RelicId);
    }
}

public sealed class ChooseTutorSkillCommand : AbstractCommand
{
    public ChooseTutorSkillCommand(string skillId)
    {
        SkillId = skillId;
    }

    public string SkillId { get; }

    protected override void OnExecute()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        // Player skills: same skill can't stack
        for (var i = 0; i < playerModel.SkillIds.Count; i++)
        {
            if (playerModel.SkillIds[i] == SkillId)
            {
                this.SendEvent(new PopupRequestedEvent("已拥有该技能，无法叠加。"));
                return;
            }
        }

        var skillDef = configModel.GetSkillDefinition(SkillId);
        playerModel.AddSkill(SkillId);

        this.SendEvent(new TutorSkillChosenEvent(SkillId));
        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
            DescriptionPanelTextKeys.MsgTutorSkill,
            skillDef.DisplayName)));
        inputLockSystem.Unlock(InputLockReason.OverlayVisible);
        flowModel.SetPhase(FlowPhase.PlayerControl);
        this.SendCommand(new CheckClearConditionCommand());
    }
}
