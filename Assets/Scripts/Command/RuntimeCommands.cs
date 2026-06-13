using System.Collections.Generic;
using System;
using QFramework;

public sealed class StartNewRunCommand : AbstractCommand
{
    public StartNewRunCommand(string characterId = GameConfigIds.CharacterImpId, int? seedOverride = null)
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
        this.GetUtility<ICommandReplayUtility>()?.Clear();
        this.SendCommand(new SaveRunCommand(SaveRunReason.NewRun));
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
        this.GetSystem<IStatSystem>().FillArmorFromDefenseAtNodeStart();
        this.GetSystem<ISkillSystem>().Trigger(SkillTrigger.OnNodeStart, new TriggerContext(), this);
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
                    this.SendCommand(new ResolveTargetingCommand(Slot));
                    break;
                case PendingHelpCardActionKind.SwapTarget:
                    this.SendCommand(new ResolveSwapTargetCommand(Slot));
                    break;
            }

            return;
        }

        var interaction = this.SendQuery(new CanInteractBoardSlotQuery(Slot));
        if (!interaction.CanInteract)
        {
            return;
        }

        switch (interaction.Kind)
        {
            case InteractionKind.EmptySlot:
                this.SendCommand(new CommitPlayerActionCommand());
                break;
            case InteractionKind.HelpCard:
                this.SendCommand(new PickHelpCardToItemSlotCommand(interaction.TargetUid));
                this.SendCommand(new CommitPlayerActionCommand());
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
            boardSystem.RemoveCardAt(runtime.BoardSlot.Value, RemoveReason.HelpCard);
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
        this.SendCommand(new RefillBoardCommand());
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
        var placedCards = false;

        do
        {
            deckModel.RefillPending = false;
            var emptySlots = boardModel.GetEmptySlots();
            for (var i = 0; i < emptySlots.Count && deckModel.BattleDrawPile.Count > 0; i++)
            {
                var uid = deckModel.BattleDrawPile.Dequeue();
                boardSystem.PlaceCard(uid, emptySlots[i], CardPlacementSource.Refill);
                placedCards = true;
            }
        }
        while (deckModel.RefillPending && boardModel.GetEmptySlots().Count > 0 && deckModel.BattleDrawPile.Count > 0);

        deckModel.RefillRunning = false;
        deckModel.RefillPending = false;
        deckSystem.UpdateNextBattlePreview();
        if (placedCards)
        {
            this.SendCommand(new PlayPresentationSequenceCommand(
                PresentationSequenceType.BoardRefill,
                SequenceCompletionAction.ResumeAfterBoardRefill));
            return;
        }

        this.SendCommand(new CompleteBoardRefillCommand());
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
        var combatSystem = this.GetSystem<ICombatSystem>();
        if (!combatSystem.CanStartCombat(MonsterUid, out _))
        {
            return;
        }

        var flowModel = this.GetModel<IFlowModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();

        inputLockSystem.Lock(InputLockReason.CombatResolving);
        flowModel.SetPhase(FlowPhase.CombatResolving);

        var context = combatSystem.BuildCombatContext(MonsterUid);
        this.SendEvent(new CombatStartedEvent(context.PlayerUid, context.MonsterUid));
        this.SendCommand(new ResolveCombatCommand(context));
        this.SendCommand(new PlayPresentationSequenceCommand(
            PresentationSequenceType.CombatResolution,
            SequenceCompletionAction.ResumeAfterCombat,
            context.Result.PlayerDied));
    }
}

public sealed class ResolveCombatCommand : AbstractCommand
{
    public ResolveCombatCommand(CombatContext context)
    {
        Context = context;
    }

    public CombatContext Context { get; }

    protected override void OnExecute()
    {
        var combatSystem = this.GetSystem<ICombatSystem>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var context = Context;

        this.SendEvent(new CombatBeforeResolvedEvent(context));
        combatSystem.PopulateCombatStats(context);

        BuildAndApplyHitGroup(
            combatSystem,
            context,
            CombatStep.FirstHit,
            context.PlayerActsFirst,
            context.PlayerStats,
            context.MonsterStats,
            context.FirstHitGroup);

        if (!IsCardAlive(collectionModel, context.MonsterUid))
        {
            context.Result.MonsterKilled = true;
            this.SendCommand(new KillMonsterCommand(context.MonsterUid));
        }
        else if (IsCardAlive(collectionModel, context.PlayerUid))
        {
            var playerStats = this.SendQuery(new GetEffectivePlayerStatsQuery());
            var monsterStats = this.SendQuery(new GetEffectiveMonsterStatsQuery(context.MonsterUid));
            BuildAndApplyHitGroup(
                combatSystem,
                context,
                CombatStep.CounterHit,
                !context.PlayerActsFirst,
                !context.PlayerActsFirst ? playerStats : monsterStats,
                !context.PlayerActsFirst ? monsterStats : playerStats,
                context.CounterHitGroup);

            if (!IsCardAlive(collectionModel, context.MonsterUid))
            {
                context.Result.MonsterKilled = true;
                this.SendCommand(new KillMonsterCommand(context.MonsterUid));
            }
        }

        ResolvePlayerDeath(context);

        this.SendEvent(new CombatResolvedEvent(context));
        this.SendEvent(new CombatAfterResolvedEvent(context));
        this.GetSystem<ISkillSystem>().Trigger(
            SkillTrigger.OnAfterCombat,
            new TriggerContext { Combat = context },
            this);
    }

    private void BuildAndApplyHitGroup(
        ICombatSystem combatSystem,
        CombatContext context,
        CombatStep step,
        bool playerAttacks,
        EffectiveStats attackerStats,
        EffectiveStats defenderStats,
        List<DamageContext> storage)
    {
        storage.Clear();
        var attacker = playerAttacks ? context.PlayerUid : context.MonsterUid;
        var defender = playerAttacks ? context.MonsterUid : context.PlayerUid;
        var causeId = ResolveCombatCauseId(step, playerAttacks);

        storage.Add(combatSystem.BuildCombatDamage(attacker, defender, attackerStats, defenderStats, causeId));
        var parallel = combatSystem.BuildParallelDamageGroup(
            context, step, attacker, defender, attackerStats, defenderStats);
        for (var i = 0; i < parallel.Count; i++)
        {
            storage.Add(parallel[i]);
        }

        this.SendCommand(new ApplyDamageGroupCommand(storage));
    }

    private void ResolvePlayerDeath(CombatContext context)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(context.PlayerUid, out var playerRuntime) || playerRuntime.CurrentHp > 0)
        {
            return;
        }

        var preventCommand = new ApplyDeathPreventCommand(context.PlayerUid);
        this.SendCommand(preventCommand);
        if (preventCommand.WasPrevented)
        {
            context.Result.PlayerDeathPrevented = true;
            return;
        }

        context.Result.PlayerDied = true;
    }

    private static string ResolveCombatCauseId(CombatStep step, bool playerAttacks)
    {
        if (step == CombatStep.FirstHit)
        {
            return playerAttacks
                ? CombatConstants.CauseCombatPlayerFirst
                : CombatConstants.CauseCombatMonsterFirst;
        }

        return playerAttacks
            ? CombatConstants.CauseCombatPlayerCounter
            : CombatConstants.CauseCombatMonsterCounter;
    }

    private static bool IsCardAlive(ICollectionModel collectionModel, CardUid uid)
    {
        return collectionModel.TryGetCard(uid, out var runtime) && runtime.CurrentHp > 0;
    }
}

public sealed class ApplyDamageGroupCommand : AbstractCommand
{
    public ApplyDamageGroupCommand(IReadOnlyList<DamageContext> contexts)
    {
        Contexts = contexts;
    }

    public IReadOnlyList<DamageContext> Contexts { get; }

    protected override void OnExecute()
    {
        if (Contexts == null || Contexts.Count == 0)
        {
            return;
        }

        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var grouped = new Dictionary<CardUid, List<DamageContext>>();
        for (var i = 0; i < Contexts.Count; i++)
        {
            var context = Contexts[i];
            if (!grouped.TryGetValue(context.Target, out var list))
            {
                list = new List<DamageContext>();
                grouped.Add(context.Target, list);
            }

            list.Add(context);
        }

        foreach (var pair in grouped)
        {
            if (!collectionModel.TryGetCard(pair.Key, out var runtime))
            {
                continue;
            }

            ApplyParallelDamageToTarget(runtime, pair.Value, deckModel);
        }
    }

    private void ApplyParallelDamageToTarget(CardRuntime runtime, List<DamageContext> contexts, IDeckModel deckModel)
    {
        var snapshotArmor = runtime.CurrentArmor;
        var oldArmor = runtime.CurrentArmor;
        var oldHp = runtime.CurrentHp;
        var normalDamageTotal = 0;
        var ignoreHpDamageTotal = 0;
        var appliedContexts = new List<DamageContext>();

        for (var i = 0; i < contexts.Count; i++)
        {
            var context = contexts[i];
            DamageApplication.ResolveAgainstSnapshot(snapshotArmor, context);
            if (context.DamageBeforeArmor <= 0)
            {
                continue;
            }

            if (TryPreventBlessing(runtime, context, deckModel))
            {
                this.SendEvent(new DamageAppliedEvent(context));
                continue;
            }

            if (context.IgnoreArmor)
            {
                ignoreHpDamageTotal += context.HpDamage;
            }
            else
            {
                normalDamageTotal += context.DamageBeforeArmor;
            }

            appliedContexts.Add(context);
        }

        if (normalDamageTotal <= 0 && ignoreHpDamageTotal <= 0)
        {
            return;
        }

        var armorAbsorbed = Math.Min(snapshotArmor, normalDamageTotal);
        var hpFromNormal = normalDamageTotal - armorAbsorbed;
        var totalHpDamage = hpFromNormal + ignoreHpDamageTotal;

        if (armorAbsorbed > 0)
        {
            runtime.CurrentArmor -= armorAbsorbed;
            if (runtime.CurrentArmor < 0)
            {
                runtime.CurrentArmor = 0;
            }
        }

        if (totalHpDamage > 0)
        {
            runtime.CurrentHp -= totalHpDamage;
            if (runtime.CurrentHp < 0)
            {
                runtime.CurrentHp = 0;
            }
        }

        DistributeResolvedAmounts(appliedContexts, snapshotArmor, normalDamageTotal, armorAbsorbed, hpFromNormal);

        if (oldArmor != runtime.CurrentArmor)
        {
            this.SendEvent(new ArmorChangedEvent(
                runtime.Uid,
                oldArmor,
                runtime.CurrentArmor,
                appliedContexts.Count > 0 ? appliedContexts[0].CauseId : "damage_group"));
        }

        if (oldArmor != runtime.CurrentArmor || oldHp != runtime.CurrentHp)
        {
            this.SendEvent(new StatsDirtyEvent(runtime.Uid));
        }

        for (var i = 0; i < appliedContexts.Count; i++)
        {
            this.SendEvent(new DamageAppliedEvent(appliedContexts[i]));
        }
    }

    private bool TryPreventBlessing(CardRuntime runtime, DamageContext context, IDeckModel deckModel)
    {
        if (!context.Preventable ||
            runtime.CardType != CardType.Player ||
            deckModel.PendingHelpCardAction.Kind != PendingHelpCardActionKind.BlessingShield ||
            context.DamageBeforeArmor <= 0)
        {
            return false;
        }

        deckModel.PendingHelpCardAction.Clear();
        context.WasPrevented = true;
        context.HpDamage = 0;
        context.ArmorAbsorbed = 0;
        this.SendEvent(new DamagePreventedEvent(runtime.Uid, GameConfigIds.HelpBlessingId));
        return true;
    }

    private static void DistributeResolvedAmounts(
        List<DamageContext> contexts,
        int snapshotArmor,
        int normalDamageTotal,
        int armorAbsorbed,
        int hpFromNormal)
    {
        if (normalDamageTotal <= 0)
        {
            return;
        }

        var remainingArmor = armorAbsorbed;
        var remainingHp = hpFromNormal;
        for (var i = 0; i < contexts.Count; i++)
        {
            var context = contexts[i];
            if (context.IgnoreArmor)
            {
                continue;
            }

            var share = context.DamageBeforeArmor;
            var armorShare = Math.Min(remainingArmor, share);
            var hpShare = share - armorShare;
            context.ArmorAbsorbed = armorShare;
            context.HpDamage = hpShare;
            remainingArmor -= armorShare;
            remainingHp -= hpShare;
        }
    }
}

public sealed class ApplyDeathPreventCommand : AbstractCommand
{
    public ApplyDeathPreventCommand(CardUid targetUid)
    {
        TargetUid = targetUid;
    }

    public CardUid TargetUid { get; }
    public bool WasPrevented { get; private set; }

    protected override void OnExecute()
    {
        var relicSystem = this.GetSystem<IRelicSystem>();
        if (!relicSystem.HasRelic(GameConfigIds.RelicPhoenixFeatherId))
        {
            return;
        }

        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(TargetUid, out var runtime) || runtime.CardType != CardType.Player)
        {
            return;
        }

        if (runtime.CurrentHp > 0)
        {
            return;
        }

        var maxHp = this.GetSystem<IStatSystem>().GetEffectivePlayerStats().MaxHp;
        runtime.CurrentHp = Math.Max(1, maxHp / 2);

        var playerModel = this.GetModel<IPlayerModel>();
        for (var i = 0; i < playerModel.Relics.Count; i++)
        {
            if (playerModel.Relics[i].RelicId != GameConfigIds.RelicPhoenixFeatherId)
            {
                continue;
            }

            playerModel.Relics[i].IsConsumed = true;
            break;
        }

        WasPrevented = true;
        this.SendEvent(new DamagePreventedEvent(TargetUid, GameConfigIds.RelicPhoenixFeatherId));
        this.SendEvent(new StatsDirtyEvent(TargetUid));
    }
}

public sealed class GameOverCommand : AbstractCommand
{
    public GameOverCommand(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }

    protected override void OnExecute()
    {
        var flowModel = this.GetModel<IFlowModel>();
        flowModel.SetPhase(FlowPhase.GameOver);
        this.SendEvent(new GameOverEvent(Reason));
        this.SendCommand(new SaveRunCommand(SaveRunReason.GameOver));
    }
}

public sealed class StartDialogueCommand : AbstractCommand
{
    public StartDialogueCommand(string message)
    {
        Message = message;
    }

    public string Message { get; }

    protected override void OnExecute()
    {
        if (string.IsNullOrWhiteSpace(Message))
        {
            return;
        }

        this.GetSystem<IInputLockSystem>().Lock(InputLockReason.DialogueRunning);
        this.SendEvent(new DialogueRequestedEvent(Message));
        this.GetUtility<ITextAnimatorUtility>().Show(
            Message,
            () => this.SendCommand(new FinishDialogueCommand(Message)));
    }
}

public sealed class FinishDialogueCommand : AbstractCommand
{
    public FinishDialogueCommand(string message)
    {
        Message = message;
    }

    public string Message { get; }

    protected override void OnExecute()
    {
        this.GetSystem<IInputLockSystem>().Unlock(InputLockReason.DialogueRunning);
        this.SendEvent(new DialogueCompletedEvent(Message));
    }
}

public sealed class PlayPresentationSequenceCommand : AbstractCommand
{
    public PlayPresentationSequenceCommand(
        PresentationSequenceType sequenceType,
        SequenceCompletionAction completionAction,
        bool playerDiedDuringCombat = false)
    {
        SequenceType = sequenceType;
        CompletionAction = completionAction;
        PlayerDiedDuringCombat = playerDiedDuringCombat;
    }

    public PresentationSequenceType SequenceType { get; }
    public SequenceCompletionAction CompletionAction { get; }
    public bool PlayerDiedDuringCombat { get; }

    protected override void OnExecute()
    {
        this.GetSystem<IInputLockSystem>().Lock(InputLockReason.SequenceRunning);
        this.SendEvent(new PresentationSequenceRequestedEvent(
            SequenceType,
            CompletionAction,
            PlayerDiedDuringCombat));
        this.GetUtility<ISequenceUtility>().Play(
            SequenceType,
            () => this.SendCommand(new FinishSequenceCommand(
                SequenceType,
                CompletionAction,
                PlayerDiedDuringCombat)));
    }
}

public sealed class FinishSequenceCommand : AbstractCommand
{
    public FinishSequenceCommand(
        PresentationSequenceType sequenceType,
        SequenceCompletionAction completionAction,
        bool playerDiedDuringCombat = false)
    {
        SequenceType = sequenceType;
        CompletionAction = completionAction;
        PlayerDiedDuringCombat = playerDiedDuringCombat;
    }

    public PresentationSequenceType SequenceType { get; }
    public SequenceCompletionAction CompletionAction { get; }
    public bool PlayerDiedDuringCombat { get; }

    protected override void OnExecute()
    {
        this.GetSystem<IInputLockSystem>().Unlock(InputLockReason.SequenceRunning);
        this.SendEvent(new PresentationSequenceCompletedEvent(
            SequenceType,
            CompletionAction,
            PlayerDiedDuringCombat));

        switch (CompletionAction)
        {
            case SequenceCompletionAction.ResumeAfterCombat:
                this.SendCommand(new CompleteCombatPresentationCommand(PlayerDiedDuringCombat));
                break;
            case SequenceCompletionAction.ResumeAfterBoardRotation:
                this.SendCommand(new CompleteBoardRotationCommand());
                break;
            case SequenceCompletionAction.ResumeAfterBoardRefill:
                this.SendCommand(new CompleteBoardRefillCommand());
                break;
        }
    }
}

public sealed class CompleteCombatPresentationCommand : AbstractCommand
{
    public CompleteCombatPresentationCommand(bool playerDiedDuringCombat)
    {
        PlayerDiedDuringCombat = playerDiedDuringCombat;
    }

    public bool PlayerDiedDuringCombat { get; }

    protected override void OnExecute()
    {
        this.GetSystem<IInputLockSystem>().Unlock(InputLockReason.CombatResolving);
        if (PlayerDiedDuringCombat)
        {
            this.SendCommand(new GameOverCommand("player_hp_zero"));
            return;
        }

        this.SendCommand(new CommitPlayerActionCommand());
    }
}

public sealed class CompleteBoardRotationCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetSystem<IInputLockSystem>().Unlock(InputLockReason.BoardMoving);
        this.SendCommand(new RequestRefillBoardCommand());
    }
}

public sealed class CompleteBoardRefillCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var rewardSystem = this.GetSystem<IRewardSystem>();
        var rewardModel = this.GetModel<IRewardModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var flowModel = this.GetModel<IFlowModel>();

        inputLockSystem.Unlock(InputLockReason.BoardRefillRunning);

        if (deckModel.PendingTutorSkillChoice)
        {
            deckModel.PendingTutorSkillChoice = false;
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

public sealed class CommitPlayerActionCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.SendEvent(new PlayerActionCommittedEvent());

        var flowModel = this.GetModel<IFlowModel>();
        var phase = flowModel.Phase.Value;
        if (phase == FlowPhase.PlayerControl || phase == FlowPhase.CombatResolving)
        {
            this.GetSystem<IInputLockSystem>().Lock(InputLockReason.BoardMoving);
            flowModel.SetPhase(FlowPhase.BoardMoving);
            this.GetSystem<IBoardSystem>().RotateClockwise(BoardMoveReason.PlayerAction);
            this.SendCommand(new PlayPresentationSequenceCommand(
                PresentationSequenceType.BoardRotation,
                SequenceCompletionAction.ResumeAfterBoardRotation));
            return;
        }

        this.SendCommand(new CheckClearConditionCommand());
    }
}

internal static class DamageApplication
{
    public static void ResolveAgainstSnapshot(int snapshotArmor, DamageContext context)
    {
        if (context.DamageBeforeArmor <= 0 && context.RawAttack > 0)
        {
            context.DamageBeforeArmor = Math.Max(0, context.RawAttack - context.DamageReduction);
        }

        if (context.DamageBeforeArmor <= 0)
        {
            context.HpDamage = 0;
            context.ArmorAbsorbed = 0;
            return;
        }

        if (context.IgnoreArmor)
        {
            context.ArmorAbsorbed = 0;
            context.HpDamage = context.DamageBeforeArmor;
            return;
        }

        context.ArmorAbsorbed = Math.Min(snapshotArmor, context.DamageBeforeArmor);
        context.HpDamage = context.DamageBeforeArmor - context.ArmorAbsorbed;
    }
}

public sealed class ApplyDamageCommand : AbstractCommand
{
    public ApplyDamageCommand(CardUid targetUid, int damage)
    {
        Context = DamageContext.FromLegacyIntDamage(targetUid, damage);
    }

    public ApplyDamageCommand(DamageContext context)
    {
        Context = context;
    }

    public DamageContext Context { get; }

    protected override void OnExecute()
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(Context.Target, out var runtime))
        {
            return;
        }

        var deckModel = this.GetModel<IDeckModel>();
        if (Context.Preventable &&
            runtime.CardType == CardType.Player &&
            deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.BlessingShield &&
            Context.DamageBeforeArmor > 0)
        {
            deckModel.PendingHelpCardAction.Clear();
            Context.WasPrevented = true;
            Context.HpDamage = 0;
            Context.ArmorAbsorbed = 0;
            this.SendEvent(new DamagePreventedEvent(Context.Target, GameConfigIds.HelpBlessingId));
            this.SendEvent(new DamageAppliedEvent(Context));
            return;
        }

        DamageApplication.ResolveAgainstSnapshot(runtime.CurrentArmor, Context);
        if (Context.DamageBeforeArmor <= 0 && Context.HpDamage <= 0 && Context.ArmorAbsorbed <= 0)
        {
            return;
        }

        var oldArmor = runtime.CurrentArmor;
        var oldHp = runtime.CurrentHp;
        ApplyResolvedDamageToRuntime(runtime, Context);

        if (!Context.IgnoreArmor && oldArmor != runtime.CurrentArmor)
        {
            this.SendEvent(new ArmorChangedEvent(Context.Target, oldArmor, runtime.CurrentArmor, Context.CauseId));
        }

        if (oldArmor != runtime.CurrentArmor || oldHp != runtime.CurrentHp)
        {
            this.SendEvent(new StatsDirtyEvent(Context.Target));
        }

        this.SendEvent(new DamageAppliedEvent(Context));
    }

    private static void ApplyResolvedDamageToRuntime(CardRuntime runtime, DamageContext context)
    {
        if (!context.IgnoreArmor && context.ArmorAbsorbed > 0)
        {
            runtime.CurrentArmor -= context.ArmorAbsorbed;
            if (runtime.CurrentArmor < 0)
            {
                runtime.CurrentArmor = 0;
            }
        }

        if (context.HpDamage <= 0)
        {
            return;
        }

        runtime.CurrentHp -= context.HpDamage;
        if (runtime.CurrentHp < 0)
        {
            runtime.CurrentHp = 0;
        }
    }
}

public sealed class SetArmorCommand : AbstractCommand
{
    public SetArmorCommand(CardUid targetUid, int armor, string causeId)
    {
        TargetUid = targetUid;
        Armor = armor;
        CauseId = causeId;
    }

    public CardUid TargetUid { get; }
    public int Armor { get; }
    public string CauseId { get; }

    protected override void OnExecute()
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(TargetUid, out var runtime))
        {
            return;
        }

        var oldArmor = runtime.CurrentArmor;
        runtime.CurrentArmor = Armor < 0 ? 0 : Armor;
        this.SendEvent(new ArmorChangedEvent(TargetUid, oldArmor, runtime.CurrentArmor, CauseId));
        this.SendEvent(new StatsDirtyEvent(TargetUid));
    }
}

public sealed class ChangeArmorCommand : AbstractCommand
{
    public ChangeArmorCommand(CardUid targetUid, int delta, string causeId)
    {
        TargetUid = targetUid;
        Delta = delta;
        CauseId = causeId;
    }

    public CardUid TargetUid { get; }
    public int Delta { get; }
    public string CauseId { get; }

    protected override void OnExecute()
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(TargetUid, out var runtime))
        {
            return;
        }

        var oldArmor = runtime.CurrentArmor;
        var newArmor = oldArmor + Delta;
        runtime.CurrentArmor = newArmor < 0 ? 0 : newArmor;
        this.SendEvent(new ArmorChangedEvent(TargetUid, oldArmor, runtime.CurrentArmor, CauseId));
        this.SendEvent(new StatsDirtyEvent(TargetUid));
    }
}

public sealed class ApplyStatChangeCommand : AbstractCommand
{
    public ApplyStatChangeCommand(CardUid targetUid, StatType statType, int delta, string causeId)
    {
        TargetUid = targetUid;
        StatType = statType;
        Delta = delta;
        CauseId = causeId;
    }

    public CardUid TargetUid { get; }
    public StatType StatType { get; }
    public int Delta { get; }
    public string CauseId { get; }

    protected override void OnExecute()
    {
        if (StatType == StatType.Armor)
        {
            this.SendCommand(new ChangeArmorCommand(TargetUid, Delta, CauseId));
            return;
        }

        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(TargetUid, out var runtime))
        {
            return;
        }

        switch (StatType)
        {
            case StatType.Attack:
                runtime.BaseAttack = ApplyNonNegativeDelta(runtime.BaseAttack, Delta);
                break;
            case StatType.Defense:
                runtime.BaseDefense = ApplyNonNegativeDelta(runtime.BaseDefense, Delta);
                if (Delta > 0)
                {
                    var oldArmor = runtime.CurrentArmor;
                    runtime.CurrentArmor = ApplyNonNegativeDelta(runtime.CurrentArmor, Delta);
                    if (oldArmor != runtime.CurrentArmor)
                    {
                        this.SendEvent(new ArmorChangedEvent(TargetUid, oldArmor, runtime.CurrentArmor, CauseId));
                    }
                }
                break;
            case StatType.MaxHp:
                runtime.MaxHp = ApplyNonNegativeDelta(runtime.MaxHp, Delta);
                runtime.CurrentHp = ApplyNonNegativeDelta(runtime.CurrentHp, Delta);
                break;
            case StatType.CurrentHp:
                runtime.CurrentHp = ApplyNonNegativeDelta(runtime.CurrentHp, Delta);
                break;
        }

        this.SendEvent(new StatsDirtyEvent(TargetUid));
    }

    private static int ApplyNonNegativeDelta(int current, int delta)
    {
        var next = current + delta;
        return next < 0 ? 0 : next;
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
            boardSystem.RemoveCardAt(monsterRuntime.BoardSlot.Value, RemoveReason.Combat);
        }

        this.ChangeGold(playerModel, RewardConstants.MonsterKillGold);
        collectionModel.RemoveCard(MonsterUid);
        this.SendEvent(new MonsterKilledEvent(MonsterUid, monsterRuntime.DefinitionId));

        if (monsterLevel == MonsterLevel.Elite)
        {
            deckSystem.InjectHelpCardsToBattleDeck(new[]
            {
                GameConfigIds.HelpBlueChestId,
                GameConfigIds.HelpGoldCardId,
                GameConfigIds.HelpAttributeUpId
            });
            deckModel.PendingTutorSkillChoice = true;
        }
        else if (monsterLevel == MonsterLevel.Boss)
        {
            deckSystem.InjectHelpCardsToBattleDeck(new[]
            {
                GameConfigIds.HelpGoldChestId,
                GameConfigIds.HelpGoldCardId,
                GameConfigIds.HelpGoldCardId,
                GameConfigIds.HelpAttributeUpId
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
            flowModel.SetPhase(FlowPhase.ClearReady);
            this.SendEvent(new LevelClearReadyEvent(runModel.Layer.Value, runModel.NodeInLayer.Value));
            this.GetSystem<ISkillSystem>().Trigger(SkillTrigger.OnNodeClear, new TriggerContext(), this);
            this.SendCommand(new GenerateHelpRewardCommand());
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
        var helpDefinition = configModel.GetCardDefinition(helpRuntime.DefinitionId);
        if (string.IsNullOrWhiteSpace(helpDefinition.EffectGraphId))
        {
            UnityEngine.Debug.LogError($"[UseHelpCardCommand] Help card {helpDefinition.CardId} missing EffectGraphId.");
            this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
                DescriptionPanelTextKeys.MsgNotImplemented,
                helpDefinition.DisplayName)));
            return;
        }

        var context = new EffectContext
        {
            Source = EffectSource.HelpCard,
            Caster = HelpCardUid
        };
        this.SendCommand(new ResolveEffectGraphCommand(helpDefinition.EffectGraphId, context));
        this.GetSystem<ISkillSystem>().Trigger(
            SkillTrigger.OnHelpCardUsed,
            new TriggerContext { HelpCardUsedUid = HelpCardUid },
            this);
    }
}

public sealed class ResolveEffectGraphCommand : AbstractCommand
{
    public ResolveEffectGraphCommand(string effectGraphId, EffectContext context)
    {
        EffectGraphId = effectGraphId;
        Context = context;
    }

    public string EffectGraphId { get; }
    public EffectContext Context { get; }

    protected override void OnExecute()
    {
        this.GetSystem<IEffectSystem>().ResolveEffectGraph(EffectGraphId, Context, this);
        this.SendEvent(new EffectResolvedEvent(
            EffectGraphId,
            Context != null ? Context.Source : EffectSource.HelpCard,
            Context?.Caster,
            Context != null ? new List<CardUid>(Context.Targets) : new List<CardUid>(),
            Context != null ? new List<string>(Context.Tags) : new List<string>()));
    }
}

public sealed class ResolveTargetingCommand : AbstractCommand
{
    public ResolveTargetingCommand(BoardSlotNo slot)
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
        var playerModel = this.GetModel<IPlayerModel>();
        var targetUid = boardModel.GetCardAt(Slot);
        if (!targetUid.HasValue || !collectionModel.TryGetCard(targetUid.Value, out var targetRuntime))
        {
            return;
        }

        var pending = deckModel.PendingHelpCardAction;
        var mode = string.IsNullOrEmpty(pending.TargetingMode) ? "damage" : pending.TargetingMode;
        var helpCardUid = pending.HelpCardUid;
        var causeId = string.IsNullOrEmpty(pending.TargetingCauseId) ? "help_throwing_knife" : pending.TargetingCauseId;

        if (mode == "reduce_armor")
        {
            if (targetUid.Value.Equals(playerModel.PlayerCardUid))
            {
                return;
            }

            var armorDelta = pending.TargetingDamage != 0 ? -Math.Abs(pending.TargetingDamage) : -10;
            deckModel.PendingHelpCardAction.Clear();
            this.SendCommand(new ChangeArmorCommand(targetUid.Value, armorDelta, causeId));
            this.SendCommand(new ConsumeHelpCardCommand(helpCardUid, HelpCardConsumeReason.Used));
            this.SendCommand(new CheckClearConditionCommand());
            return;
        }

        if (mode == "teleport_to_deck")
        {
            if (targetUid.Value.Equals(playerModel.PlayerCardUid))
            {
                return;
            }

            deckModel.PendingHelpCardAction.Clear();
            this.GetSystem<IDeckSystem>().ShuffleExistingCardIntoBattleDeck(targetUid.Value);
            this.SendCommand(new ConsumeHelpCardCommand(helpCardUid, HelpCardConsumeReason.Used));
            this.SendCommand(new CheckClearConditionCommand());
            return;
        }

        if (mode == "kidnap")
        {
            if (targetUid.Value.Equals(playerModel.PlayerCardUid) ||
                targetRuntime.CardType != CardType.Monster ||
                targetRuntime.MonsterLevel == MonsterLevel.Elite ||
                targetRuntime.MonsterLevel == MonsterLevel.Boss)
            {
                return;
            }

            var armorGain = targetRuntime.CurrentArmor;
            deckModel.PendingHelpCardAction.Clear();

            var boardSystem = this.GetSystem<IBoardSystem>();
            if (targetRuntime.BoardSlot.HasValue)
            {
                boardSystem.RemoveCardAt(targetRuntime.BoardSlot.Value, RemoveReason.Effect);
            }
            else
            {
                boardSystem.RemoveCardAt(Slot, RemoveReason.Effect);
            }

            collectionModel.RemoveCard(targetUid.Value);
            this.SendEvent(new MonsterKilledEvent(targetUid.Value, targetRuntime.DefinitionId));

            if (armorGain > 0)
            {
                this.SendCommand(new ChangeArmorCommand(
                    playerModel.PlayerCardUid,
                    armorGain,
                    causeId));
            }

            this.SendCommand(new ConsumeHelpCardCommand(helpCardUid, HelpCardConsumeReason.Used));
            this.SendCommand(new RequestRefillBoardCommand());
            return;
        }

        if (targetRuntime.CardType != CardType.Monster)
        {
            return;
        }

        var damageAmount = ResolveTargetingDamage(pending, playerModel, collectionModel);
        if (damageAmount <= 0)
        {
            return;
        }

        deckModel.PendingHelpCardAction.Clear();

        var damage = new DamageContext
        {
            Target = targetUid.Value,
            CauseId = causeId,
            Type = DamageType.HelpCard,
            RawAttack = damageAmount,
            DamageBeforeArmor = damageAmount
        };
        this.SendCommand(new ApplyDamageCommand(damage));
        var targetDied = collectionModel.TryGetCard(targetUid.Value, out targetRuntime) && targetRuntime.CurrentHp <= 0;
        if (targetDied)
        {
            this.SendCommand(new KillMonsterCommand(targetUid.Value));
        }

        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
            DescriptionPanelTextKeys.MsgThrowingKnifeHit,
            targetRuntime.DisplayName)));
        this.SendCommand(new ConsumeHelpCardCommand(helpCardUid, HelpCardConsumeReason.Used));

        if (targetDied)
        {
            this.SendCommand(new RequestRefillBoardCommand());
        }
        else
        {
            this.SendCommand(new CheckClearConditionCommand());
        }
    }

    private static int ResolveTargetingDamage(
        PendingHelpCardAction pending,
        IPlayerModel playerModel,
        ICollectionModel collectionModel)
    {
        var mode = string.IsNullOrEmpty(pending.TargetingMode) ? "damage" : pending.TargetingMode;
        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
        switch (mode)
        {
            case "player_attack":
                return playerRuntime.BaseAttack;
            case "player_current_hp":
                return playerRuntime.CurrentHp;
            case "player_current_armor":
                return playerRuntime.CurrentArmor;
            default:
                return pending.TargetingDamage > 0 ? pending.TargetingDamage : 6;
        }
    }
}

public sealed class ResolveSwapTargetCommand : AbstractCommand
{
    public ResolveSwapTargetCommand(BoardSlotNo slot)
    {
        Slot = slot;
    }

    public BoardSlotNo Slot { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        if (deckModel.PendingHelpCardAction.Kind != PendingHelpCardActionKind.SwapTarget)
        {
            return;
        }

        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var targetUid = boardModel.GetCardAt(Slot);
        if (!targetUid.HasValue ||
            targetUid.Value.Equals(playerModel.PlayerCardUid) ||
            !collectionModel.TryGetCard(targetUid.Value, out _))
        {
            return;
        }

        var pending = deckModel.PendingHelpCardAction;
        var helpCardUid = pending.HelpCardUid;

        if (!pending.SwapFirstTargetSelected)
        {
            pending.SwapFirstTargetSelected = true;
            pending.SwapFirstTargetUid = targetUid.Value;
            this.SendEvent(new GameplayMessageEvent("交换卡：请选择第二张卡牌。"));
            return;
        }

        if (pending.SwapFirstTargetUid.HasValue && pending.SwapFirstTargetUid.Value.Equals(targetUid.Value))
        {
            return;
        }

        var firstTarget = pending.SwapFirstTargetUid.Value;
        deckModel.PendingHelpCardAction.Clear();

        this.SendCommand(new SwapBoardCardsCommand(firstTarget, targetUid.Value));
        this.SendCommand(new ConsumeHelpCardCommand(helpCardUid, HelpCardConsumeReason.Used));
        this.SendCommand(new CheckClearConditionCommand());
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
                this.SendCommand(new ApplyStatChangeCommand(playerModel.PlayerCardUid, StatType.Attack, 1, "help_attribute"));
                message = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgAttrAttack);
                break;
            case AttributeUpgradeChoice.Defense:
                this.SendCommand(new ApplyStatChangeCommand(playerModel.PlayerCardUid, StatType.Defense, 1, "help_attribute"));
                message = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgAttrDefense);
                break;
            case AttributeUpgradeChoice.MaxHp:
                this.SendCommand(new ApplyStatChangeCommand(playerModel.PlayerCardUid, StatType.MaxHp, 2, "help_attribute"));
                message = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgAttrMaxHp);
                break;
        }

        var helpCardUid = deckModel.PendingHelpCardAction.HelpCardUid;
        deckModel.PendingHelpCardAction.Clear();
        inputLockSystem.Unlock(InputLockReason.OverlayVisible);

        this.SendEvent(new GameplayMessageEvent(message));
        this.SendEvent(new AttributeChoiceResolvedEvent(helpCardUid, Choice));
        this.SendCommand(new ConsumeHelpCardCommand(helpCardUid, HelpCardConsumeReason.Used));
    }
}

public sealed class ConsumeHelpCardCommand : AbstractCommand
{
    public ConsumeHelpCardCommand(CardUid helpCardUid, HelpCardConsumeReason reason)
    {
        HelpCardUid = helpCardUid;
        Reason = reason;
    }

    public CardUid HelpCardUid { get; }
    public HelpCardConsumeReason Reason { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        if (!collectionModel.TryGetCard(HelpCardUid, out var helpRuntime))
        {
            return;
        }

        if (helpRuntime.BoardSlot.HasValue)
        {
            boardSystem.RemoveCardAt(helpRuntime.BoardSlot.Value, RemoveReason.HelpCard);
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

        var definition = configModel.GetCardDefinition(helpRuntime.DefinitionId);
        state.IsOnBoard = false;
        state.IsInItemSlot = false;
        state.IsTemporarilyRemoved = definition.RestoreAfterNode;
        state.IsPermanentlyRemoved = !definition.RestoreAfterNode;
    }
}

// ============================================================
// M3: Reward Loop Commands
// ============================================================

public sealed class SettleNodeEndCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var rewardSystem = this.GetSystem<IRewardSystem>();

        rewardSystem.SettleUnusedHelpCards();
        rewardSystem.RestoreHelpDeckSnapshotByRestoreAfterNode();
        rewardSystem.TrimHelpDeckOverflow();
        deckModel.ClearNodeState();
    }
}

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
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var playerModel = this.GetModel<IPlayerModel>();
        var collectionModel = this.GetModel<ICollectionModel>();

        var rewardModel = this.GetModel<IRewardModel>();
        flowModel.SetPhase(FlowPhase.RoomResolving);
        rewardModel.CurrentRewardSource = RewardSource.Room;

        this.SendCommand(new SettleNodeEndCommand());

        var roomDef = configModel.GetRoomDefinition(RoomId);
        this.SendEvent(new RoomChosenEvent(RoomId, roomDef.RoomType));

        switch (roomDef.RoomType)
        {
            case RoomType.Gold:
                this.ChangeGold(playerModel, roomDef.RewardGold);
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
                    DescriptionPanelTextKeys.MsgRoomGold,
                    roomDef.RewardGold)));
                this.SendCommand(new ProceedToNextNodeCommand());
                break;

            case RoomType.Chest:
                flowModel.SetPhase(FlowPhase.ChestRewardChoosing);
                inputLockSystem.Lock(InputLockReason.OverlayVisible);
                this.GetSystem<IRelicSystem>().GenerateChestRewardCandidates();
                this.SendEvent(new ChestRewardGeneratedEvent(rewardModel.ChestRewardRelicIds));
                break;

            case RoomType.Attribute:
            {
                var hpBonus = roomDef.StatMaxHpBonus > 0
                    ? roomDef.StatMaxHpBonus
                    : RewardConstants.AttributeRoomMaxHpBonus;
                this.SendCommand(new ApplyStatChangeCommand(
                    playerModel.PlayerCardUid,
                    StatType.MaxHp,
                    hpBonus,
                    "room_attribute"));
                var effectiveMax = this.GetSystem<IStatSystem>().GetEffectivePlayerStats().MaxHp;
                var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
                playerRuntime.CurrentHp = effectiveMax;
                this.SendEvent(new StatsDirtyEvent(playerModel.PlayerCardUid));
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
                    DescriptionPanelTextKeys.MsgRoomAttribute,
                    hpBonus)));
                this.SendCommand(new ProceedToNextNodeCommand());
                break;
            }

            case RoomType.Shop:
                flowModel.SetPhase(FlowPhase.Shop);
                inputLockSystem.Lock(InputLockReason.OverlayVisible);
                this.GetSystem<IShopSystem>().GenerateShopCards();
                this.SendEvent(new ShopOpenedEvent(rewardModel.ShopCardIds));
                break;
        }
    }
}


public sealed class EnterRoomChoosingCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var flowModel = this.GetModel<IFlowModel>();
        var rewardSystem = this.GetSystem<IRewardSystem>();
        var rewardModel = this.GetModel<IRewardModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();

        inputLockSystem.Unlock(InputLockReason.OverlayVisible);
        rewardModel.CurrentRewardSource = RewardSource.None;
        flowModel.SetPhase(FlowPhase.RoomChoosing);
        rewardSystem.GenerateRoomCandidates();
        this.SendEvent(new RoomChoiceRequestedEvent(rewardModel.RoomCandidateIds));
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

public sealed class AddHelpCardCommand : AbstractCommand
{
    public AddHelpCardCommand(string cardId, HelpCardAddPolicy policy = HelpCardAddPolicy.Normal, bool showBlockedPopup = true)
    {
        CardId = cardId;
        Policy = policy;
        ShowBlockedPopup = showBlockedPopup;
    }

    public string CardId { get; }
    public HelpCardAddPolicy Policy { get; }
    public bool ShowBlockedPopup { get; }

    protected override void OnExecute()
    {
        var rewardSystem = this.GetSystem<IRewardSystem>();
        if (rewardSystem.TryAddHelpCard(CardId, Policy))
        {
            return;
        }

        if (ShowBlockedPopup)
        {
            this.SendEvent(new PopupRequestedEvent(HelpDeckMessages.CapacityOrSameNameBlocked));
        }
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
        var rewardSystem = this.GetSystem<IRewardSystem>();

        if (!rewardSystem.CanAddHelpCard(CardId))
        {
            this.SendEvent(new PopupRequestedEvent(HelpDeckMessages.CapacityOrSameNameBlocked));
            return;
        }

        if (!rewardSystem.TryAddHelpCard(CardId))
        {
            return;
        }

        var definition = configModel.GetCardDefinition(CardId);

        this.SendEvent(new HelpRewardPickedEvent(CardId));
        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
            DescriptionPanelTextKeys.MsgHelpCardGained,
            definition.DisplayName)));
        this.SendCommand(new EnterRoomChoosingCommand());
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
        this.SendCommand(new EnterRoomChoosingCommand());
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

        var rewardModel = this.GetModel<IRewardModel>();
        if (rewardModel.CurrentRewardSource == RewardSource.ChestCard)
        {
            ChestRewardFlow.ResumeAfterChestReward(this, flowModel, rewardModel, inputLockSystem);
            return;
        }

        if (rewardModel.CurrentRewardSource == RewardSource.Room)
        {
            rewardModel.CurrentRewardSource = RewardSource.None;
            inputLockSystem.Unlock(InputLockReason.OverlayVisible);
            this.SendCommand(new ProceedToNextNodeCommand());
        }
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

        var rewardModel = this.GetModel<IRewardModel>();
        if (rewardModel.CurrentRewardSource == RewardSource.ChestCard)
        {
            ChestRewardFlow.ResumeAfterChestReward(this, flowModel, rewardModel, inputLockSystem);
            return;
        }

        if (rewardModel.CurrentRewardSource == RewardSource.Room)
        {
            rewardModel.CurrentRewardSource = RewardSource.None;
            inputLockSystem.Unlock(InputLockReason.OverlayVisible);
            this.SendCommand(new ProceedToNextNodeCommand());
            return;
        }
    }
}

internal static class ChestRewardFlow
{
    public static void ResumeAfterChestReward(
        ICanSendCommand commandSender,
        IFlowModel flowModel,
        IRewardModel rewardModel,
        IInputLockSystem inputLockSystem)
    {
        if (rewardModel.CurrentRewardSource != RewardSource.ChestCard)
        {
            return;
        }

        var resumePhase = rewardModel.RewardResumePhase ?? FlowPhase.PlayerControl;
        rewardModel.CurrentRewardSource = RewardSource.None;
        rewardModel.RewardResumePhase = null;
        inputLockSystem.Unlock(InputLockReason.OverlayVisible);
        flowModel.SetPhase(resumePhase);

        if (resumePhase == FlowPhase.RoomChoosing)
        {
            return;
        }

        commandSender.SendCommand(new CheckClearConditionCommand());
    }
}

public sealed class ProceedToNextNodeCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var runModel = this.GetModel<IRunModel>();
        var flowModel = this.GetModel<IFlowModel>();

        if (flowModel.Phase.Value == FlowPhase.LayerComplete)
        {
            var nextLayer = runModel.Layer.Value + 1;
            if (nextLayer > 3)
            {
                flowModel.SetPhase(FlowPhase.Victory);
                this.SendEvent(new VictoryEvent());
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgVictory)));
                this.SendCommand(new SaveRunCommand(SaveRunReason.Victory));
                return;
            }

            this.SendEvent(new LayerAdvancedEvent(runModel.Layer.Value, nextLayer));
            this.SendCommand(new StartNodeCommand(nextLayer, 1));
            this.SendCommand(new SaveRunCommand(SaveRunReason.NodeStart));
            return;
        }

        this.SendEvent(new NodeCompletedEvent(runModel.Layer.Value, runModel.NodeInLayer.Value));

        var nextNode = runModel.NodeInLayer.Value + 1;
        if (nextNode > 9)
        {
            if (runModel.Layer.Value >= 3)
            {
                flowModel.SetPhase(FlowPhase.Victory);
                this.SendEvent(new VictoryEvent());
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgVictory)));
                this.SendCommand(new SaveRunCommand(SaveRunReason.Victory));
            }
            else
            {
                flowModel.SetPhase(FlowPhase.LayerComplete);
                this.SendEvent(new LayerCompletedEvent(runModel.Layer.Value));
                this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
                    DescriptionPanelTextKeys.MsgLayerComplete,
                    runModel.Layer.Value)));
                this.SendCommand(new SaveRunCommand(SaveRunReason.LayerComplete));
            }

            return;
        }

        this.SendEvent(new NodeAdvancedEvent(runModel.Layer.Value, runModel.NodeInLayer.Value, nextNode));
        this.SendCommand(new StartNodeCommand(runModel.Layer.Value, nextNode));
        this.SendCommand(new SaveRunCommand(SaveRunReason.NodeStart));
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

        if (rewardModel.CurrentRewardSource == RewardSource.Room)
        {
            rewardModel.CurrentRewardSource = RewardSource.None;
            inputLockSystem.Unlock(InputLockReason.OverlayVisible);
            this.SendCommand(new ProceedToNextNodeCommand());
            return;
        }

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
        if (skillDef.MaxHpOnAcquire > 0)
        {
            this.SendCommand(new ApplyStatChangeCommand(
                playerModel.PlayerCardUid,
                StatType.MaxHp,
                skillDef.MaxHpOnAcquire,
                SkillId));
        }

        this.SendEvent(new TutorSkillChosenEvent(SkillId));
        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
            DescriptionPanelTextKeys.MsgTutorSkill,
            skillDef.DisplayName)));
        inputLockSystem.Unlock(InputLockReason.OverlayVisible);
        flowModel.SetPhase(FlowPhase.PlayerControl);
        this.SendCommand(new CheckClearConditionCommand());
    }
}
