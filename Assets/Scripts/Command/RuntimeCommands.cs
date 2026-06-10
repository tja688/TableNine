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
        var boardSystem = this.GetSystem<IBoardSystem>();
        if (!collectionModel.TryGetCard(MonsterUid, out var monsterRuntime))
        {
            return;
        }

        if (monsterRuntime.BoardSlot.HasValue)
        {
            boardSystem.RemoveCardAt(monsterRuntime.BoardSlot.Value);
        }

        playerModel.Gold.Value += 5;
        collectionModel.RemoveCard(MonsterUid);
        this.SendEvent(new MonsterKilledEvent(MonsterUid, monsterRuntime.DefinitionId));
    }
}

public sealed class CheckClearConditionCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var deckSystem = this.GetSystem<IDeckSystem>();
        var flowModel = this.GetModel<IFlowModel>();
        var runModel = this.GetModel<IRunModel>();
        if (!deckSystem.HasMonsterRemaining())
        {
            flowModel.SetPhase(FlowPhase.ClearReady);
            this.SendEvent(new LevelClearReadyEvent(runModel.Layer.Value, runModel.NodeInLayer.Value));
        }
    }
}
