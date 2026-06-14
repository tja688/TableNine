using System;
using System.Collections.Generic;
using QFramework;

// ============================================================
// Gold change extension — ensures GoldChangedEvent is always sent
// ============================================================

public static class PlayerGoldExtensions
{
    public static void ChangeGold(this ICanSendEvent sender, IPlayerModel playerModel, int delta)
    {
        if (delta == 0) return;
        var oldValue = playerModel.Gold.Value;
        playerModel.Gold.Value += delta;
        sender.SendEvent(new GoldChangedEvent(oldValue, playerModel.Gold.Value, delta));
    }
}

// ============================================================
// Core Systems (unchanged from M1/M2)
// ============================================================

public interface IRunSystem : ISystem
{
    int ResolveSeed(int? seedOverride);
}

public sealed class RunSystem : AbstractSystem, IRunSystem
{
    protected override void OnInit()
    {
    }

    public int ResolveSeed(int? seedOverride)
    {
        return seedOverride ?? Environment.TickCount;
    }
}

public interface ILevelFlowSystem : ISystem
{
    void EnterPhase(FlowPhase phase);
}

public sealed class LevelFlowSystem : AbstractSystem, ILevelFlowSystem
{
    protected override void OnInit()
    {
    }

    public void EnterPhase(FlowPhase phase)
    {
        this.GetModel<IFlowModel>().SetPhase(phase);
    }
}

public interface IBoardSystem : ISystem
{
    void ResetBoardWithPlayer(CardUid playerUid);
    bool IsOrthogonalAdjacentToPlayer(BoardSlotNo slot);
    IReadOnlyList<BoardSlotNo> GetEmptySlots();
    void PlaceCard(CardUid uid, BoardSlotNo slot, CardPlacementSource source);
    CardUid? RemoveCardAt(BoardSlotNo slot, RemoveReason reason = RemoveReason.None);
    void RotateClockwise(BoardMoveReason reason = BoardMoveReason.None);
    void RotateCounterclockwise(BoardMoveReason reason = BoardMoveReason.None);
}

public sealed class BoardSystem : AbstractSystem, IBoardSystem
{
    protected override void OnInit()
    {
    }

    public void ResetBoardWithPlayer(CardUid playerUid)
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();

        boardModel.Clear();
        boardModel.PlayerSlot = new BoardSlotNo(5);
        boardModel.SetCardAt(boardModel.PlayerSlot, playerUid);

        if (collectionModel.TryGetCard(playerUid, out var playerCard))
        {
            playerCard.BoardSlot = boardModel.PlayerSlot;
            playerCard.ItemSlotIndex = null;
        }
    }

    public bool IsOrthogonalAdjacentToPlayer(BoardSlotNo slot)
    {
        var boardModel = this.GetModel<IBoardModel>();
        return BoardSlotUtility.IsOrthogonalAdjacent(boardModel.PlayerSlot, slot);
    }

    public IReadOnlyList<BoardSlotNo> GetEmptySlots()
    {
        return this.GetModel<IBoardModel>().GetEmptySlots();
    }

    public void PlaceCard(CardUid uid, BoardSlotNo slot, CardPlacementSource source)
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        if (boardModel.GetCardAt(slot).HasValue)
        {
            throw new InvalidOperationException($"Board slot {slot.Value} is already occupied.");
        }

        var runtime = collectionModel.GetCard(uid);
        runtime.BoardSlot = slot;
        runtime.ItemSlotIndex = null;
        boardModel.SetCardAt(slot, uid);

        if (runtime.CardType == CardType.Help && deckModel.HelpCardStates.TryGetValue(uid.Value, out var helpState))
        {
            helpState.IsOnBoard = true;
            helpState.IsInItemSlot = false;
        }

        this.SendEvent(new CardPlacedEvent(uid, slot, source));
        this.SendEvent(new BoardSlotChangedEvent(slot, uid));
        this.SendEvent(new CardMovedEvent(uid, slot, null, source));
    }

    public CardUid? RemoveCardAt(BoardSlotNo slot, RemoveReason reason = RemoveReason.None)
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var existingUid = boardModel.GetCardAt(slot);
        if (!existingUid.HasValue)
        {
            return null;
        }

        var runtime = collectionModel.GetCard(existingUid.Value);
        runtime.BoardSlot = null;
        boardModel.SetCardAt(slot, null);

        if (runtime.CardType == CardType.Help && deckModel.HelpCardStates.TryGetValue(existingUid.Value.Value, out var helpState))
        {
            helpState.IsOnBoard = false;
        }

        this.SendEvent(new BoardSlotChangedEvent(slot, null));
        this.SendEvent(new CardRemovedEvent(existingUid.Value, slot, reason));
        return existingUid;
    }

    public void RotateClockwise(BoardMoveReason reason = BoardMoveReason.None)
    {
        RotateRing(stepOffset: -1, clockwise: true, reason: reason);
    }

    public void RotateCounterclockwise(BoardMoveReason reason = BoardMoveReason.None)
    {
        RotateRing(stepOffset: 1, clockwise: false, reason: reason);
    }

    private void RotateRing(int stepOffset, bool clockwise, BoardMoveReason reason)
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var ring = BoardSlotUtility.ClockwiseRing;
        var previousSlots = new Dictionary<int, BoardSlotNo>();
        var movedEvents = new List<CardMovedEvent>();
        var values = new CardUid?[ring.Length];
        for (var i = 0; i < ring.Length; i++)
        {
            values[i] = boardModel.GetCardAt(ring[i]);
            if (values[i].HasValue)
            {
                previousSlots[values[i].Value.Value] = ring[i];
            }
        }

        for (var i = 0; i < ring.Length; i++)
        {
            var fromIndex = (i + stepOffset + values.Length) % values.Length;
            var newValue = values[fromIndex];
            var slot = ring[i];
            boardModel.SetCardAt(slot, newValue);

            if (newValue.HasValue && collectionModel.TryGetCard(newValue.Value, out var runtime))
            {
                runtime.BoardSlot = slot;
            }

            this.SendEvent(new BoardSlotChangedEvent(slot, newValue));

            if (newValue.HasValue &&
                previousSlots.TryGetValue(newValue.Value.Value, out var previousSlot) &&
                previousSlot.Value != slot.Value)
            {
                movedEvents.Add(new CardMovedEvent(newValue.Value, slot, previousSlot, CardPlacementSource.BoardMove, true));
            }
        }

        for (var i = 0; i < movedEvents.Count; i++)
        {
            this.SendEvent(movedEvents[i]);
        }

        this.SendEvent(new BoardRotatedEvent(clockwise, reason, movedEvents));
    }
}

public interface IDeckSystem : ISystem
{
    void GenerateDemonDeck(int layer, int nodeInLayer);
    void InjectHelpCardsToBattleDeck(IReadOnlyList<string> cardIds);
    void ShuffleExistingCardIntoBattleDeck(CardUid uid);
    void SnapshotHelpDeck();
    void UpdateNextBattlePreview();
    bool HasMonsterRemaining();
}

public sealed class DeckSystem : AbstractSystem, IDeckSystem
{
    protected override void OnInit()
    {
    }

    public void GenerateDemonDeck(int layer, int nodeInLayer)
    {
        var configModel = this.GetModel<IConfigModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();
        var rule = configModel.GetMonsterDeckRule(layer, nodeInLayer);

        deckModel.DemonDeckQueue.Clear();

        var cardIds = rule.LevelQuotas.Count > 0
            ? MonsterDeckComposer.Compose(rule, randomUtility)
            : ComposeLegacyDeck(rule, randomUtility);

        randomUtility.Shuffle(cardIds);
        for (var i = 0; i < cardIds.Count; i++)
        {
            var definition = configModel.GetCardDefinition(cardIds[i]);
            var runtime = collectionModel.CreateCard(definition);
            deckModel.DemonDeckQueue.Enqueue(runtime.Uid);
        }
    }

    public void InjectHelpCardsToBattleDeck(IReadOnlyList<string> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0)
        {
            return;
        }

        var configModel = this.GetModel<IConfigModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        var injectedIds = new List<string>();
        for (var i = 0; i < cardIds.Count; i++)
        {
            var definition = configModel.GetCardDefinition(cardIds[i]);
            var runtime = collectionModel.CreateCard(definition);
            deckModel.BattleDrawPile.Enqueue(runtime.Uid);
            injectedIds.Add(cardIds[i]);
        }

        var pile = new List<CardUid>(deckModel.BattleDrawPile.Count);
        while (deckModel.BattleDrawPile.Count > 0)
        {
            pile.Add(deckModel.BattleDrawPile.Dequeue());
        }

        randomUtility.Shuffle(pile);
        for (var i = 0; i < pile.Count; i++)
        {
            deckModel.BattleDrawPile.Enqueue(pile[i]);
        }

        UpdateNextBattlePreview();
        this.SendEvent(new BattleDeckCardsInjectedEvent(injectedIds));
    }

    public void ShuffleExistingCardIntoBattleDeck(CardUid uid)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        var randomUtility = this.GetUtility<IRandomUtility>();
        if (!collectionModel.TryGetCard(uid, out var runtime))
        {
            return;
        }

        if (runtime.BoardSlot.HasValue)
        {
            boardSystem.RemoveCardAt(runtime.BoardSlot.Value, RemoveReason.Effect);
        }

        runtime.BoardSlot = null;
        runtime.ItemSlotIndex = null;

        var pile = new List<CardUid> { uid };
        while (deckModel.BattleDrawPile.Count > 0)
        {
            pile.Add(deckModel.BattleDrawPile.Dequeue());
        }

        randomUtility.Shuffle(pile);
        for (var i = 0; i < pile.Count; i++)
        {
            deckModel.BattleDrawPile.Enqueue(pile[i]);
        }

        UpdateNextBattlePreview();
    }

    private static List<string> ComposeLegacyDeck(MonsterDeckRuleDefinition rule, IRandomUtility randomUtility)
    {
        var cardIds = new List<string>(rule.TotalCardCount);
        for (var i = 0; i < rule.TotalCardCount; i++)
        {
            cardIds.Add(rule.AllowedMonsterCardIds[randomUtility.Range(0, rule.AllowedMonsterCardIds.Count)]);
        }

        return cardIds;
    }

    public void SnapshotHelpDeck()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var snapshot = new HelpDeckSnapshot();
        foreach (var uid in deckModel.OwnedHelpCards)
        {
            if (deckModel.HelpCardStates.TryGetValue(uid.Value, out var state))
            {
                snapshot.Cards.Add(state.Clone());
            }
        }

        deckModel.NodeStartSnapshot = snapshot;
    }

    public void UpdateNextBattlePreview()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();

        if (deckModel.BattleDrawPile.Count == 0)
        {
            deckModel.NextBattleCardPreview.Value = CardPreview.Empty;
        }
        else
        {
            var uid = deckModel.BattleDrawPile.Peek();
            var runtime = collectionModel.GetCard(uid);
            deckModel.NextBattleCardPreview.Value = new CardPreview(uid, runtime.DefinitionId, runtime.DisplayName);
        }

        this.SendEvent(new BattleDeckChangedEvent(deckModel.BattleDrawPile.Count, deckModel.NextBattleCardPreview.Value));
    }

    public bool HasMonsterRemaining()
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();

        for (var i = 1; i <= 9; i++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(i));
            if (uid.HasValue && collectionModel.TryGetCard(uid.Value, out var runtime) && runtime.CardType == CardType.Monster)
            {
                return true;
            }
        }

        foreach (var uid in deckModel.BattleDrawPile)
        {
            if (collectionModel.TryGetCard(uid, out var runtime) && runtime.CardType == CardType.Monster)
            {
                return true;
            }
        }

        return false;
    }
}

public interface ICombatSystem : ISystem
{
    bool CanStartCombat(CardUid monsterUid, out string reason);
    CombatContext BuildCombatContext(CardUid monsterUid);
    void PopulateCombatStats(CombatContext context);
    bool PlayerActsFirst(EffectiveStats playerStats, EffectiveStats monsterStats);
    int CalculateDamage(EffectiveStats attacker, EffectiveStats defender);
    DamageContext BuildCombatDamage(CardUid source, CardUid target, EffectiveStats attacker, EffectiveStats defender, string causeId);
    IReadOnlyList<DamageContext> BuildParallelDamageGroup(
        CombatContext context,
        CombatStep step,
        CardUid primaryAttacker,
        CardUid primaryDefender,
        EffectiveStats attackerStats,
        EffectiveStats defenderStats);
}

public sealed class CombatSystem : AbstractSystem, ICombatSystem
{
    protected override void OnInit()
    {
    }

    public bool CanStartCombat(CardUid monsterUid, out string reason)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(monsterUid, out var runtime) || runtime.CardType != CardType.Monster)
        {
            reason = "Target is not a monster";
            return false;
        }

        if (!runtime.BoardSlot.HasValue)
        {
            reason = "Monster is not on the board";
            return false;
        }

        if (!this.GetSystem<IBoardSystem>().IsOrthogonalAdjacentToPlayer(runtime.BoardSlot.Value))
        {
            reason = "Monster is not orthogonally adjacent to the player";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public CombatContext BuildCombatContext(CardUid monsterUid)
    {
        var playerModel = this.GetModel<IPlayerModel>();
        return new CombatContext
        {
            PlayerUid = playerModel.PlayerCardUid,
            MonsterUid = monsterUid
        };
    }

    public void PopulateCombatStats(CombatContext context)
    {
        var statSystem = this.GetSystem<IStatSystem>();
        var playerStats = statSystem.GetEffectivePlayerStats();
        var monsterStats = statSystem.GetEffectiveMonsterStats(context.MonsterUid);
        context.PlayerStats = playerStats;
        context.MonsterStats = monsterStats;
        context.PlayerActsFirst = PlayerActsFirst(playerStats, monsterStats);
    }

    public bool PlayerActsFirst(EffectiveStats playerStats, EffectiveStats monsterStats)
    {
        if (playerStats.HasFirstStrike == monsterStats.HasFirstStrike)
        {
            return true;
        }

        return playerStats.HasFirstStrike;
    }

    public int CalculateDamage(EffectiveStats attacker, EffectiveStats defender)
    {
        return Math.Max(0, attacker.Attack - defender.DamageReduction);
    }

    public DamageContext BuildCombatDamage(CardUid source, CardUid target, EffectiveStats attacker, EffectiveStats defender, string causeId)
    {
        var damageBeforeArmor = Math.Max(0, attacker.Attack - defender.DamageReduction);
        return new DamageContext
        {
            Source = source,
            Target = target,
            CauseId = causeId,
            Type = DamageType.Combat,
            RawAttack = attacker.Attack,
            DamageReduction = defender.DamageReduction,
            DamageBeforeArmor = damageBeforeArmor
        };
    }

    public IReadOnlyList<DamageContext> BuildParallelDamageGroup(
        CombatContext context,
        CombatStep step,
        CardUid primaryAttacker,
        CardUid primaryDefender,
        EffectiveStats attackerStats,
        EffectiveStats defenderStats)
    {
        var extras = new List<DamageContext>();
        var skillSystem = this.GetSystem<ISkillSystem>();
        var triggerContext = new TriggerContext
        {
            Combat = context,
            CombatStep = step,
            PrimaryAttacker = primaryAttacker,
            PrimaryDefender = primaryDefender
        };
        skillSystem.CollectParallelDamage(triggerContext, extras);
        return extras;
    }
}

// ============================================================
// StatSystem — [S1 FIX] now queries relic stat bonuses
// ============================================================

public interface IStatSystem : ISystem
{
    EffectiveStats GetEffectivePlayerStats();
    EffectiveStats GetEffectiveMonsterStats(CardUid monsterUid);
    void FillArmorFromArmorStatAtNodeStart();
}

public sealed class StatSystem : AbstractSystem, IStatSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<CardMovedEvent>(OnMonsterSkillCardMoved);
        this.RegisterEvent<CardPlacedEvent>(OnMonsterSkillCardPlaced);
        this.RegisterEvent<MonsterKilledEvent>(OnMonsterSkillMonsterKilled);
        this.RegisterEvent<CombatAfterResolvedEvent>(OnMonsterSkillCombatAfterResolved);
    }

    public EffectiveStats GetEffectivePlayerStats()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);

        var attack = playerRuntime.BaseAttack;
        var armor = playerRuntime.BaseArmor;
        var maxHp = playerRuntime.MaxHp;
        var currentHp = playerRuntime.CurrentHp;

        // Apply relic stat bonuses
        var relics = playerModel.Relics;
        for (var i = 0; i < relics.Count; i++)
        {
            var relic = relics[i];
            if (relic.IsConsumed) continue;

            // Use instance fields (populated from RelicDefinition via FromDefinition)
            attack += relic.StatAttackBonus;
            armor += relic.StatArmorBonus;
            maxHp += relic.StatMaxHpBonus;
            currentHp += relic.StatMaxHpBonus;
        }

        if (HasWoodSet(playerModel))
        {
            attack += 2;
            armor += 2;
            maxHp += 8;
            currentHp += 8;
        }

        return new EffectiveStats
        {
            CurrentHp = currentHp,
            MaxHp = maxHp,
            CurrentArmor = playerRuntime.CurrentArmor,
            Attack = attack,
            Armor = armor,
            DamageReduction = 0,
            HasFirstStrike = HasFirstStrike(playerRuntime, configModel)
        };
    }

    public EffectiveStats GetEffectiveMonsterStats(CardUid monsterUid)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        var monsterRuntime = collectionModel.GetCard(monsterUid);

        var attack = monsterRuntime.BaseAttack;
        var armor = monsterRuntime.BaseArmor;
        var maxHp = monsterRuntime.MaxHp;
        var currentHp = monsterRuntime.CurrentHp;
        var damageReduction = 0;
        var hasFirstStrike = false;

        SkillBehaviorExecutor.ApplyEffectiveStatRules(
            monsterRuntime,
            monsterRuntime.BoardSlot,
            this.GetModel<IBoardModel>(),
            collectionModel,
            configModel.GetEffectiveStatBehaviorRules(),
            ref attack,
            ref armor,
            ref damageReduction,
            ref hasFirstStrike,
            configModel);

        return new EffectiveStats
        {
            CurrentHp = currentHp,
            MaxHp = maxHp,
            CurrentArmor = monsterRuntime.CurrentArmor,
            Attack = attack,
            Armor = armor,
            DamageReduction = damageReduction,
            HasFirstStrike = hasFirstStrike
        };
    }

    private static bool HasWoodSet(IPlayerModel playerModel)
    {
        var hasShield = false;
        var hasSword = false;
        var hasArmor = false;
        for (var i = 0; i < playerModel.Relics.Count; i++)
        {
            var relic = playerModel.Relics[i];
            if (relic.IsConsumed)
            {
                continue;
            }

            if (relic.RelicId == GameConfigIds.RelicWoodShieldId)
            {
                hasShield = true;
            }
            else if (relic.RelicId == GameConfigIds.RelicWoodSwordId)
            {
                hasSword = true;
            }
            else if (relic.RelicId == GameConfigIds.RelicWoodArmorId)
            {
                hasArmor = true;
            }
        }

        return hasShield && hasSword && hasArmor;
    }

    private static bool HasFirstStrike(CardRuntime runtime, IConfigModel configModel, BoardSlotNo? boardSlot = null)
    {
        for (var i = 0; i < runtime.SkillIds.Count; i++)
        {
            if (configModel.GetSkillDefinition(runtime.SkillIds[i]).GrantsFirstStrike)
            {
                return true;
            }
        }

        var rules = configModel.GetEffectiveStatBehaviorRules();
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule.BehaviorKind != SkillBehaviorKind.EffectiveStatGrantFirstStrike ||
                !runtime.HasSkill(rule.SkillId) ||
                !boardSlot.HasValue ||
                rule.ConditionKey != SkillBehaviorConditionKeys.AtSlot ||
                boardSlot.Value.Value != rule.IntValue2)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void OnMonsterSkillCardPlaced(CardPlacedEvent placed)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(placed.Uid, out var runtime) || runtime.CardType != CardType.Monster)
        {
            return;
        }

        var configModel = this.GetModel<IConfigModel>();
        SkillBehaviorExecutor.HandleEvent(
            SkillTrigger.OnCardPlaced,
            configModel.GetSkillBehaviorRules(SkillTrigger.OnCardPlaced),
            runtime,
            ((IBelongToArchitecture)this).GetArchitecture(),
            placed.Slot);
    }

    private void OnMonsterSkillCardMoved(CardMovedEvent moved)
    {
        if (!moved.IsBoardMovement)
        {
            return;
        }

        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(moved.Uid, out var runtime) || runtime.CardType != CardType.Monster)
        {
            return;
        }

        if (moved.PreviousSlot.HasValue && moved.PreviousSlot.Value.Value == moved.NewSlot.Value)
        {
            return;
        }

        var configModel = this.GetModel<IConfigModel>();
        SkillBehaviorExecutor.HandleEvent(
            SkillTrigger.OnCardMoved,
            configModel.GetSkillBehaviorRules(SkillTrigger.OnCardMoved),
            runtime,
            ((IBelongToArchitecture)this).GetArchitecture(),
            moved.NewSlot,
            moved.PreviousSlot);
    }

    private void OnMonsterSkillMonsterKilled(MonsterKilledEvent killed)
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        var rules = configModel.GetSkillBehaviorRules(SkillTrigger.OnMonsterKilled);

        for (var i = 0; i < BoardSlotUtility.ClockwiseRing.Length; i++)
        {
            var slot = BoardSlotUtility.ClockwiseRing[i];
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue || uid.Value.Equals(killed.MonsterUid))
            {
                continue;
            }

            if (!collectionModel.TryGetCard(uid.Value, out var runtime) || runtime.CardType != CardType.Monster)
            {
                continue;
            }

            SkillBehaviorExecutor.HandleEvent(
                SkillTrigger.OnMonsterKilled,
                rules,
                runtime,
                ((IBelongToArchitecture)this).GetArchitecture(),
                killedMonsterUid: killed.MonsterUid);
        }
    }

    private void OnMonsterSkillCombatAfterResolved(CombatAfterResolvedEvent resolved)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(resolved.Context.MonsterUid, out var monsterRuntime) ||
            monsterRuntime.CardType != CardType.Monster ||
            !monsterRuntime.BoardSlot.HasValue)
        {
            return;
        }

        var configModel = this.GetModel<IConfigModel>();
        SkillBehaviorExecutor.HandleEvent(
            SkillTrigger.OnAfterCombat,
            configModel.GetSkillBehaviorRules(SkillTrigger.OnAfterCombat),
            monsterRuntime,
            ((IBelongToArchitecture)this).GetArchitecture(),
            monsterRuntime.BoardSlot,
            combatContext: resolved.Context);
    }

    private void SendArchitectureCommand(ICommand command)
    {
        ((IBelongToArchitecture)this).GetArchitecture().SendCommand(command);
    }

    public void FillArmorFromArmorStatAtNodeStart()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var boardModel = this.GetModel<IBoardModel>();

        FillArmorFromArmorStat(playerModel.PlayerCardUid, GetEffectivePlayerStats());

        for (var i = 0; i < BoardSlotUtility.ClockwiseRing.Length; i++)
        {
            var slot = BoardSlotUtility.ClockwiseRing[i];
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue || uid.Value.Equals(playerModel.PlayerCardUid))
            {
                continue;
            }

            if (!collectionModel.TryGetCard(uid.Value, out var runtime) || runtime.CardType != CardType.Monster)
            {
                continue;
            }

            FillArmorFromArmorStat(uid.Value, GetEffectiveMonsterStats(uid.Value));
        }
    }

    private void FillArmorFromArmorStat(CardUid uid, EffectiveStats stats)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        var runtime = collectionModel.GetCard(uid);
        var oldArmor = runtime.CurrentArmor;
        var newArmor = stats.Armor < 0 ? 0 : stats.Armor;
        if (oldArmor == newArmor)
        {
            return;
        }

        runtime.CurrentArmor = newArmor;
        this.SendEvent(new ArmorChangedEvent(uid, oldArmor, newArmor, "node_start_armor"));
        this.SendEvent(new StatsDirtyEvent(uid));
    }
}

public interface IInputLockSystem : ISystem
{
    void Lock(InputLockReason reason);
    void Unlock(InputLockReason reason);
    bool IsLocked();
}

public sealed class InputLockSystem : AbstractSystem, IInputLockSystem
{
    protected override void OnInit()
    {
    }

    public void Lock(InputLockReason reason)
    {
        this.GetModel<IFlowModel>().AddLock(reason);
    }

    public void Unlock(InputLockReason reason)
    {
        this.GetModel<IFlowModel>().RemoveLock(reason);
    }

    public bool IsLocked()
    {
        return this.GetModel<IFlowModel>().IsInputLocked;
    }
}

// ============================================================
// RewardSystem — [S1/S2 FIX] dynamic card pool, instance methods,
//   no try/catch, uses RewardConstants, uses IConfigModel properly
// ============================================================

public interface IRewardSystem : ISystem
{
    void GenerateHelpRewardCandidates();
    void GenerateRoomCandidates();
    void GenerateTutorSkillCandidates();
    void SettleUnusedHelpCards();
    void RestoreHelpDeckSnapshotByRestoreAfterNode();
    bool CanAddHelpCard(string cardId, HelpCardAddPolicy policy = HelpCardAddPolicy.Normal);
    bool TryAddHelpCard(string cardId, HelpCardAddPolicy policy = HelpCardAddPolicy.Normal);
    void TrimHelpDeckOverflow();
}

public sealed class RewardSystem : AbstractSystem, IRewardSystem
{
    protected override void OnInit() { }

    public void GenerateHelpRewardCandidates()
    {
        var configModel = this.GetModel<IConfigModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var runModel = this.GetModel<IRunModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        rewardModel.ClearHelpRewardCardIds();

        // [S1 FIX] Use IConfigModel to dynamically query all help cards
        var allHelpCards = configModel.GetAllHelpCardDefinitions();
        var capacity = deckModel.GetHelpDeckCapacity(runModel.Layer.Value);
        var currentCount = CountActiveHelpCards(deckModel);

        for (var i = 0; i < RewardConstants.HelpRewardCandidateCount; i++)
        {
            var targetQuality = RollQuality(randomUtility);
            var candidates = FilterCandidates(allHelpCards, targetQuality, deckModel, configModel, capacity, currentCount + i);
            if (candidates.Count == 0)
            {
                // Fallback to any quality
                candidates = FilterCandidates(allHelpCards, CardQuality.White, deckModel, configModel, capacity, currentCount + i);
                if (candidates.Count == 0)
                {
                    candidates = GetAllAvailableHelpCards(allHelpCards, deckModel, configModel, capacity, currentCount + i);
                }
            }
            if (candidates.Count > 0)
            {
                var pick = candidates[randomUtility.Range(0, candidates.Count)];
                rewardModel.AddHelpRewardCardId(pick);
            }
        }
    }

    public void GenerateRoomCandidates()
    {
        var rewardModel = this.GetModel<IRewardModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();
        rewardModel.ClearRoomCandidateIds();

        var pool = new List<string>
        {
            GameConfigIds.RoomGoldId,
            GameConfigIds.RoomChestId,
            GameConfigIds.RoomAttributeId,
            GameConfigIds.RoomShopId
        };

        var pickCount = pool.Count < RewardConstants.RoomCandidateCount
            ? pool.Count
            : RewardConstants.RoomCandidateCount;
        for (var i = 0; i < pickCount; i++)
        {
            var index = randomUtility.Range(0, pool.Count);
            rewardModel.AddRoomCandidateId(pool[index]);
            pool.RemoveAt(index);
        }
    }

    public void GenerateTutorSkillCandidates()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        rewardModel.ClearTutorSkillIds();

        var pool = new List<string>();
        var tutorPool = GameConfigIds.TutorSkillPoolIds;
        for (var i = 0; i < tutorPool.Length; i++)
        {
            var skillId = tutorPool[i];
            var alreadyOwned = false;
            for (var j = 0; j < playerModel.SkillIds.Count; j++)
            {
                if (playerModel.SkillIds[j] == skillId)
                {
                    alreadyOwned = true;
                    break;
                }
            }

            if (!alreadyOwned)
            {
                pool.Add(skillId);
            }
        }

        var pickCount = pool.Count < RewardConstants.HelpRewardCandidateCount
            ? pool.Count
            : RewardConstants.HelpRewardCandidateCount;
        for (var i = 0; i < pickCount; i++)
        {
            var index = randomUtility.Range(0, pool.Count);
            rewardModel.AddTutorSkillId(pool[index]);
            pool.RemoveAt(index);
        }
    }

    public void SettleUnusedHelpCards()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var playerModel = this.GetModel<IPlayerModel>();

        var unusedCount = 0;
        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (deckModel.HelpCardStates.TryGetValue(uid.Value, out var state))
            {
                if (!state.IsPermanentlyRemoved && (state.IsOnBoard || state.IsInItemSlot))
                {
                    unusedCount++;
                }
            }
        }

        // [S2 FIX] Use RewardConstants + send GoldChangedEvent
        var goldGained = unusedCount * RewardConstants.UnusedHelpCardGold;
        this.ChangeGold(playerModel, goldGained);
        this.SendEvent(new HelpCardsSettledEvent(unusedCount, goldGained));
    }

    public void RestoreHelpDeckSnapshotByRestoreAfterNode()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var snapshot = deckModel.NodeStartSnapshot;
        if (snapshot == null || snapshot.Cards.Count == 0)
        {
            return;
        }

        // 1. Collect permanently removed UIDs — these should NOT be restored
        var permanentlyRemovedUids = new HashSet<int>();
        foreach (var pair in deckModel.HelpCardStates)
        {
            if (pair.Value.IsPermanentlyRemoved)
            {
                permanentlyRemovedUids.Add(pair.Key);
            }
        }

        // 2. Collect new cards acquired during node (in OwnedHelpCards but not in snapshot)
        var snapshotUids = new HashSet<int>();
        for (var i = 0; i < snapshot.Cards.Count; i++)
        {
            snapshotUids.Add(snapshot.Cards[i].Uid.Value);
        }

        var newCards = new List<CardUid>();
        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (!snapshotUids.Contains(uid.Value) && !permanentlyRemovedUids.Contains(uid.Value))
            {
                newCards.Add(uid);
            }
        }

        // 3. Restore snapshot cards, skipping permanently removed
        var restoredCount = 0;
        for (var i = 0; i < snapshot.Cards.Count; i++)
        {
            var snapState = snapshot.Cards[i];
            if (permanentlyRemovedUids.Contains(snapState.Uid.Value))
            {
                collectionModel.RemoveCard(snapState.Uid);
                deckModel.HelpCardStates.Remove(snapState.Uid.Value);
                deckModel.OwnedHelpCards.Remove(snapState.Uid);
                continue;
            }

            if (deckModel.HelpCardStates.TryGetValue(snapState.Uid.Value, out var currentState))
            {
                currentState.IsTemporarilyRemoved = false;
                currentState.IsOnBoard = false;
                currentState.IsInItemSlot = false;
                restoredCount++;
            }
        }

        // 4. Ensure new cards are in clean state
        for (var i = 0; i < newCards.Count; i++)
        {
            if (deckModel.HelpCardStates.TryGetValue(newCards[i].Value, out var state))
            {
                state.IsOnBoard = false;
                state.IsInItemSlot = false;
                state.IsTemporarilyRemoved = false;
            }
        }

        // 5. Clear item slots
        for (var i = 0; i < deckModel.ItemSlots.Length; i++)
        {
            deckModel.ItemSlots[i] = null;
        }

        this.SendEvent(new HelpDeckRestoredEvent(restoredCount, permanentlyRemovedUids.Count, newCards.Count));
    }

    public bool CanAddHelpCard(string cardId, HelpCardAddPolicy policy = HelpCardAddPolicy.Normal)
    {
        var deckModel = this.GetModel<IDeckModel>();
        var configModel = this.GetModel<IConfigModel>();
        var runModel = this.GetModel<IRunModel>();
        var capacity = deckModel.GetHelpDeckCapacity(runModel.Layer.Value);
        var currentCount = CountActiveHelpCards(deckModel);

        if (policy == HelpCardAddPolicy.Normal && currentCount >= capacity)
        {
            return false;
        }

        var sameNameCount = CountSameNameCards(deckModel, cardId, configModel);
        if (sameNameCount >= 3)
        {
            return false;
        }

        return true;
    }

    public bool TryAddHelpCard(string cardId, HelpCardAddPolicy policy = HelpCardAddPolicy.Normal)
    {
        if (!CanAddHelpCard(cardId, policy))
        {
            return false;
        }

        var configModel = this.GetModel<IConfigModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var definition = configModel.GetCardDefinition(cardId);
        var runtime = collectionModel.CreateCard(definition);
        deckModel.OwnedHelpCards.Add(runtime.Uid);
        deckModel.HelpCardStates[runtime.Uid.Value] = new HelpCardState
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId
        };
        return true;
    }

    public void TrimHelpDeckOverflow()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var runModel = this.GetModel<IRunModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var capacity = deckModel.GetHelpDeckCapacity(runModel.Layer.Value);
        var trimmedCount = 0;

        while (CountActiveHelpCards(deckModel) > capacity)
        {
            CardUid? uidToRemove = null;
            for (var i = deckModel.OwnedHelpCards.Count - 1; i >= 0; i--)
            {
                var uid = deckModel.OwnedHelpCards[i];
                if (deckModel.HelpCardStates.TryGetValue(uid.Value, out var state) && !state.IsPermanentlyRemoved)
                {
                    uidToRemove = uid;
                    break;
                }
            }

            if (!uidToRemove.HasValue)
            {
                break;
            }

            PermanentlyRemoveOverflowHelpCard(uidToRemove.Value);
            trimmedCount++;
        }

        if (trimmedCount > 0)
        {
            this.SendEvent(new GameplayMessageEvent(HelpDeckMessages.OverflowTrimmed));
        }
    }

    private void PermanentlyRemoveOverflowHelpCard(CardUid helpCardUid)
    {
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(helpCardUid, out var runtime))
        {
            return;
        }

        if (deckModel.HelpCardStates.TryGetValue(helpCardUid.Value, out var state))
        {
            state.IsPermanentlyRemoved = true;
            state.IsOnBoard = false;
            state.IsInItemSlot = false;
        }

        if (runtime.BoardSlot.HasValue)
        {
            this.GetSystem<IBoardSystem>().RemoveCardAt(runtime.BoardSlot.Value, RemoveReason.HelpCard);
        }

        if (runtime.ItemSlotIndex.HasValue)
        {
            deckModel.ItemSlots[runtime.ItemSlotIndex.Value] = null;
            this.SendEvent(new ItemSlotChangedEvent(runtime.ItemSlotIndex.Value, null));
        }

        deckModel.OwnedHelpCards.Remove(helpCardUid);
        deckModel.HelpCardStates.Remove(helpCardUid.Value);
        collectionModel.RemoveCard(helpCardUid);
    }

    // ---- Instance helper methods (converted from static) ----

    private CardQuality RollQuality(IRandomUtility random)
    {
        var roll = random.Value() * 100f;
        var cumulative = 0f;
        var qualityMap = new[] { CardQuality.White, CardQuality.Blue, CardQuality.Gold, CardQuality.Red };
        var weights = RewardConstants.HelpRewardQualityWeights;
        for (var i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
            {
                return qualityMap[i];
            }
        }
        return CardQuality.White;
    }

    /// <summary>
    /// Filters help card definitions by quality, capacity, and same-name limit.
    /// Uses IConfigModel properly — no try/catch.
    /// </summary>
    private List<string> FilterCandidates(IReadOnlyList<CardDefinition> allHelpCards, CardQuality quality,
        IDeckModel deckModel, IConfigModel configModel, int capacity, int currentCount)
    {
        var result = new List<string>();
        if (currentCount >= capacity) return result;

        for (var i = 0; i < allHelpCards.Count; i++)
        {
            var def = allHelpCards[i];
            if (def.CardType == CardType.Help && def.Quality == quality)
            {
                var sameName = CountSameNameCards(deckModel, def.CardId, configModel);
                if (sameName < 3)
                {
                    result.Add(def.CardId);
                }
            }
        }
        return result;
    }

    private List<string> GetAllAvailableHelpCards(IReadOnlyList<CardDefinition> allHelpCards,
        IDeckModel deckModel, IConfigModel configModel, int capacity, int currentCount)
    {
        var result = new List<string>();
        if (currentCount >= capacity) return result;

        for (var i = 0; i < allHelpCards.Count; i++)
        {
            var def = allHelpCards[i];
            var sameName = CountSameNameCards(deckModel, def.CardId, configModel);
            if (sameName < 3)
            {
                result.Add(def.CardId);
            }
        }
        return result;
    }

    private int CountActiveHelpCards(IDeckModel deckModel)
    {
        var count = 0;
        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (deckModel.HelpCardStates.TryGetValue(uid.Value, out var state) && !state.IsPermanentlyRemoved)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Counts how many active help cards share the same DisplayName.
    /// [S2 FIX] Uses TryGetCardDefinition instead of try/catch.
    /// </summary>
    private int CountSameNameCards(IDeckModel deckModel, string cardId, IConfigModel configModel)
    {
        if (!configModel.TryGetCardDefinition(cardId, out var targetDef))
        {
            return 0;
        }

        var count = 0;
        var targetDisplayName = targetDef.DisplayName;

        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (deckModel.HelpCardStates.TryGetValue(uid.Value, out var state) && !state.IsPermanentlyRemoved)
            {
                if (configModel.TryGetCardDefinition(state.DefinitionId, out var existingDef) &&
                    existingDef.DisplayName == targetDisplayName)
                {
                    count++;
                }
            }
        }
        return count;
    }
}

// ============================================================
// RelicSystem — [S2 FIX] uses RelicInstance, RewardConstants, GoldChangedEvent
// ============================================================

public interface IRelicSystem : ISystem
{
    bool AddRelic(string relicId);
    bool DiscardRelic(string relicId);
    void GenerateChestRewardCandidates(ChestTier chestTier = ChestTier.Normal);
    bool HasRelic(string relicId);
    void ApplyRelicStats();
}

public sealed class RelicSystem : AbstractSystem, IRelicSystem
{
    protected override void OnInit() { }

    public bool AddRelic(string relicId)
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();

        if (playerModel.Relics.Count >= playerModel.MaxRelicCount)
        {
            this.SendEvent(new PopupRequestedEvent("遗物栏已满。"));
            return false;
        }

        if (playerModel.HasRelic(relicId))
        {
            return false;
        }

        var definition = configModel.GetRelicDefinition(relicId);
        // [S2 FIX] Use unified RelicInstance.FromDefinition
        playerModel.AddRelic(RelicInstance.FromDefinition(definition));

        ApplyRelicStats();
        this.SendEvent(new RelicAddedEvent(relicId));

        if (relicId == GameConfigIds.RelicGoldenChestId)
        {
            var rewardSystem = this.GetSystem<IRewardSystem>();
            rewardSystem.TryAddHelpCard(GameConfigIds.HelpGoldChestId, HelpCardAddPolicy.BypassDeckCapacity);
            rewardSystem.TryAddHelpCard(GameConfigIds.HelpGoldChestId, HelpCardAddPolicy.BypassDeckCapacity);
        }

        return true;
    }

    public bool DiscardRelic(string relicId)
    {
        var playerModel = this.GetModel<IPlayerModel>();
        if (!playerModel.RemoveRelic(relicId))
        {
            return false;
        }

        // [S2 FIX] Use RewardConstants + send GoldChangedEvent
        this.ChangeGold(playerModel, RewardConstants.DiscardRelicGold);
        ApplyRelicStats();
        this.SendEvent(new RelicDiscardedEvent(relicId, RewardConstants.DiscardRelicGold));
        return true;
    }

    public void GenerateChestRewardCandidates(ChestTier chestTier = ChestTier.Normal)
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var configModel = this.GetModel<IConfigModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        rewardModel.ClearChestRewardRelicIds();

        var allRelics = new List<RelicDefinition>();
        foreach (var pair in configModel.GetAllRelicDefinitions())
        {
            if (!pair.ExcludeFromPool)
            {
                allRelics.Add(pair);
            }
        }

        if (playerModel.Relics.Count >= playerModel.MaxRelicCount || allRelics.Count == 0)
        {
            return;
        }

        var pickCount = allRelics.Count < RewardConstants.ChestRewardCandidateCount
            ? allRelics.Count
            : RewardConstants.ChestRewardCandidateCount;
        var picked = new HashSet<string>();
        for (var i = 0; i < pickCount; i++)
        {
            var relicId = PickChestRelicId(allRelics, playerModel, randomUtility, chestTier, picked);
            if (string.IsNullOrEmpty(relicId))
            {
                break;
            }

            picked.Add(relicId);
            rewardModel.AddChestRewardRelicId(relicId);
        }
    }

    private static string PickChestRelicId(
        IReadOnlyList<RelicDefinition> allRelics,
        IPlayerModel playerModel,
        IRandomUtility randomUtility,
        ChestTier chestTier,
        HashSet<string> picked)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var quality = RollChestRelicQuality(randomUtility, chestTier);
            var candidates = new List<string>();
            for (var i = 0; i < allRelics.Count; i++)
            {
                var relic = allRelics[i];
                if (relic.Quality != quality ||
                    playerModel.HasRelic(relic.RelicId) ||
                    picked.Contains(relic.RelicId))
                {
                    continue;
                }

                candidates.Add(relic.RelicId);
            }

            if (candidates.Count == 0)
            {
                continue;
            }

            return candidates[randomUtility.Range(0, candidates.Count)];
        }

        return null;
    }

    private static CardQuality RollChestRelicQuality(IRandomUtility randomUtility, ChestTier chestTier)
    {
        var weights = chestTier switch
        {
            ChestTier.Blue => RewardConstants.BlueChestQualityWeights,
            ChestTier.Gold => RewardConstants.GoldChestQualityWeights,
            _ => RewardConstants.NormalChestQualityWeights
        };

        var roll = randomUtility.Value() * 100f;
        var cumulative = 0f;
        var qualityMap = new[] { CardQuality.White, CardQuality.Blue, CardQuality.Gold };
        for (var i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
            {
                return qualityMap[i];
            }
        }

        return CardQuality.White;
    }

    public bool HasRelic(string relicId)
    {
        return this.GetModel<IPlayerModel>().HasRelic(relicId);
    }

    public void ApplyRelicStats()
    {
        this.SendEvent(new RelicStatsChangedEvent());
    }
}

// ============================================================
// ShopSystem — [S2 FIX] BuyHelpCard removes from list, uses RewardConstants, GoldChangedEvent
// ============================================================

public interface IShopSystem : ISystem
{
    void GenerateShopCards();
    bool BuyHelpCard(string cardId);
    bool DeleteHelpCardForGold(CardUid helpCardUid);
}

public sealed class ShopSystem : AbstractSystem, IShopSystem
{
    protected override void OnInit() { }

    public void GenerateShopCards()
    {
        var rewardModel = this.GetModel<IRewardModel>();
        var configModel = this.GetModel<IConfigModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        rewardModel.ClearShopCardIds();

        // [S2 FIX] Use IConfigModel to dynamically query all help cards
        var allHelpCards = configModel.GetAllHelpCardDefinitions();
        var pool = new List<string>();
        for (var i = 0; i < allHelpCards.Count; i++)
        {
            pool.Add(allHelpCards[i].CardId);
        }

        randomUtility.Shuffle(pool);
        var count = pool.Count < RewardConstants.ShopDisplayCount ? pool.Count : RewardConstants.ShopDisplayCount;
        for (var i = 0; i < count; i++)
        {
            rewardModel.AddShopCardId(pool[i]);
        }
    }

    public bool BuyHelpCard(string cardId)
    {
        var configModel = this.GetModel<IConfigModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var rewardSystem = this.GetSystem<IRewardSystem>();

        var definition = configModel.GetCardDefinition(cardId);
        if (playerModel.Gold.Value < definition.Price)
        {
            this.SendEvent(new PopupRequestedEvent("金币不足。"));
            return false;
        }

        if (!rewardSystem.CanAddHelpCard(cardId))
        {
            this.SendEvent(new PopupRequestedEvent(HelpDeckMessages.CapacityOrSameNameBlocked));
            return false;
        }

        if (!rewardSystem.TryAddHelpCard(cardId))
        {
            return false;
        }

        this.ChangeGold(playerModel, -definition.Price);

        // [S2 FIX] Remove purchased card from shop display
        rewardModel.RemoveShopCardId(cardId);

        this.SendEvent(new HelpCardPurchasedEvent(cardId, definition.Price));
        return true;
    }

    public bool DeleteHelpCardForGold(CardUid helpCardUid)
    {
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var playerModel = this.GetModel<IPlayerModel>();

        if (!collectionModel.TryGetCard(helpCardUid, out var runtime) || runtime.CardType != CardType.Help)
        {
            return false;
        }

        if (!deckModel.OwnedHelpCards.Contains(helpCardUid))
        {
            return false;
        }

        if (deckModel.HelpCardStates.TryGetValue(helpCardUid.Value, out var state))
        {
            if (state.IsPermanentlyRemoved || state.IsTemporarilyRemoved)
            {
                return false;
            }

            state.IsPermanentlyRemoved = true;
            state.IsOnBoard = false;
            state.IsInItemSlot = false;
        }

        if (runtime.BoardSlot.HasValue)
        {
            var boardSystem = this.GetSystem<IBoardSystem>();
            boardSystem.RemoveCardAt(runtime.BoardSlot.Value, RemoveReason.HelpCard);
        }

        if (runtime.ItemSlotIndex.HasValue)
        {
            deckModel.ItemSlots[runtime.ItemSlotIndex.Value] = null;
            this.SendEvent(new ItemSlotChangedEvent(runtime.ItemSlotIndex.Value, null));
        }

        // [S2 FIX] Use RewardConstants + send GoldChangedEvent
        this.ChangeGold(playerModel, RewardConstants.DeleteHelpCardGold);
        this.SendEvent(new HelpCardDeletedForGoldEvent(helpCardUid, RewardConstants.DeleteHelpCardGold));
        return true;
    }
}

public static class MonsterDeckComposer
{
    private const int MaxComposeAttempts = 64;

    public static List<string> Compose(MonsterDeckRuleDefinition rule, IRandomUtility randomUtility)
    {
        for (var attempt = 0; attempt < MaxComposeAttempts; attempt++)
        {
            var result = TryCompose(rule, randomUtility);
            if (result != null)
            {
                return result;
            }
        }

        return BuildFallbackDeck(rule, randomUtility);
    }

    private static List<string> TryCompose(MonsterDeckRuleDefinition rule, IRandomUtility randomUtility)
    {
        var cardIds = new List<string>(rule.TotalCardCount);
        for (var i = 0; i < rule.MandatoryMonsterCardIds.Count; i++)
        {
            cardIds.Add(rule.MandatoryMonsterCardIds[i]);
        }

        var remaining = rule.TotalCardCount - cardIds.Count;
        if (rule.LevelQuotas.Count == 0)
        {
            return remaining == 0 ? cardIds : null;
        }

        var counts = new int[rule.LevelQuotas.Count];
        for (var i = 0; i < rule.LevelQuotas.Count - 1; i++)
        {
            var quota = rule.LevelQuotas[i];
            counts[i] = quota.MinCount == quota.MaxCount
                ? quota.MinCount
                : randomUtility.Range(quota.MinCount, quota.MaxCount + 1);
            remaining -= counts[i];
        }

        var lastQuota = rule.LevelQuotas[rule.LevelQuotas.Count - 1];
        counts[rule.LevelQuotas.Count - 1] = remaining;
        if (remaining < lastQuota.MinCount || remaining > lastQuota.MaxCount)
        {
            return null;
        }

        for (var i = 0; i < rule.LevelQuotas.Count; i++)
        {
            var quota = rule.LevelQuotas[i];
            if (quota.PoolCardIds.Count == 0 || counts[i] < 0)
            {
                return null;
            }

            for (var c = 0; c < counts[i]; c++)
            {
                var pick = quota.PoolCardIds[randomUtility.Range(0, quota.PoolCardIds.Count)];
                cardIds.Add(pick);
            }
        }

        return cardIds.Count == rule.TotalCardCount ? cardIds : null;
    }

    private static List<string> BuildFallbackDeck(MonsterDeckRuleDefinition rule, IRandomUtility randomUtility)
    {
        var cardIds = new List<string>(rule.TotalCardCount);
        for (var i = 0; i < rule.MandatoryMonsterCardIds.Count; i++)
        {
            cardIds.Add(rule.MandatoryMonsterCardIds[i]);
        }

        while (cardIds.Count < rule.TotalCardCount)
        {
            cardIds.Add(rule.AllowedMonsterCardIds[randomUtility.Range(0, rule.AllowedMonsterCardIds.Count)]);
        }

        return cardIds;
    }
}
