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
    CardUid? RemoveCardAt(BoardSlotNo slot);
    void RotateClockwise();
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
    }

    public CardUid? RemoveCardAt(BoardSlotNo slot)
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
        return existingUid;
    }

    public void RotateClockwise()
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var values = new CardUid?[BoardSlotUtility.ClockwiseRing.Length];
        for (var i = 0; i < BoardSlotUtility.ClockwiseRing.Length; i++)
        {
            values[i] = boardModel.GetCardAt(BoardSlotUtility.ClockwiseRing[i]);
        }

        for (var i = 0; i < BoardSlotUtility.ClockwiseRing.Length; i++)
        {
            var fromIndex = (i - 1 + values.Length) % values.Length;
            var newValue = values[fromIndex];
            var slot = BoardSlotUtility.ClockwiseRing[i];
            boardModel.SetCardAt(slot, newValue);

            if (newValue.HasValue && collectionModel.TryGetCard(newValue.Value, out var runtime))
            {
                runtime.BoardSlot = slot;
            }

            this.SendEvent(new BoardSlotChangedEvent(slot, newValue));
        }
    }
}

public interface IDeckSystem : ISystem
{
    void GenerateDemonDeck(int layer, int nodeInLayer);
    void InjectHelpCardsToBattleDeck(IReadOnlyList<string> cardIds);
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
    bool PlayerActsFirst(EffectiveStats playerStats, EffectiveStats monsterStats);
    int CalculateDamage(EffectiveStats attacker, EffectiveStats defender);
}

public sealed class CombatSystem : AbstractSystem, ICombatSystem
{
    protected override void OnInit()
    {
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
        return Math.Max(0, attacker.Attack - defender.Defense);
    }
}

// ============================================================
// StatSystem — [S1 FIX] now queries relic stat bonuses
// ============================================================

public interface IStatSystem : ISystem
{
    EffectiveStats GetEffectivePlayerStats();
    EffectiveStats GetEffectiveMonsterStats(CardUid monsterUid);
}

public sealed class StatSystem : AbstractSystem, IStatSystem
{
    protected override void OnInit()
    {
    }

    public EffectiveStats GetEffectivePlayerStats()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);

        var attack = playerRuntime.BaseAttack;
        var defense = playerRuntime.BaseDefense;
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
            defense += relic.StatDefenseBonus;
            maxHp += relic.StatMaxHpBonus;
            currentHp += relic.StatMaxHpBonus;
        }

        return new EffectiveStats
        {
            CurrentHp = currentHp,
            MaxHp = maxHp,
            Attack = attack,
            Defense = defense,
            HasFirstStrike = HasFirstStrike(playerRuntime, configModel)
        };
    }

    public EffectiveStats GetEffectiveMonsterStats(CardUid monsterUid)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        var boardModel = this.GetModel<IBoardModel>();
        var monsterRuntime = collectionModel.GetCard(monsterUid);

        var attack = monsterRuntime.BaseAttack;
        var defense = monsterRuntime.BaseDefense;
        var maxHp = monsterRuntime.MaxHp;
        var currentHp = monsterRuntime.CurrentHp;

        if (monsterRuntime.BoardSlot.HasValue)
        {
            var slot = monsterRuntime.BoardSlot.Value;
            if (monsterRuntime.HasSkill(DefaultGameConfigFactory.SkillClubCubId) && slot.Value == 6)
            {
                attack += 2;
            }

            if (monsterRuntime.HasSkill(DefaultGameConfigFactory.SkillDiamondCubId) && slot.Value == 4)
            {
                defense += 2;
            }

            if (monsterRuntime.HasSkill(DefaultGameConfigFactory.SkillHeartCubId) && BoardSlotUtility.IsBottomRow(slot))
            {
                currentHp += 2;
                maxHp += 2;
            }

            if (!monsterRuntime.HasSkill(DefaultGameConfigFactory.SkillSpadeCubId) && HasSpadeCubAuraActive(boardModel, collectionModel))
            {
                attack += 2;
            }
        }

        return new EffectiveStats
        {
            CurrentHp = currentHp,
            MaxHp = maxHp,
            Attack = attack,
            Defense = defense,
            HasFirstStrike = HasFirstStrike(monsterRuntime, configModel)
        };
    }

    private static bool HasFirstStrike(CardRuntime runtime, IConfigModel configModel)
    {
        for (var i = 0; i < runtime.SkillIds.Count; i++)
        {
            if (configModel.GetSkillDefinition(runtime.SkillIds[i]).GrantsFirstStrike)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSpadeCubAuraActive(IBoardModel boardModel, ICollectionModel collectionModel)
    {
        for (var i = 0; i < 3; i++)
        {
            var slot = BoardSlotUtility.ClockwiseRing[i];
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue)
            {
                continue;
            }

            if (collectionModel.TryGetCard(uid.Value, out var runtime) &&
                runtime.CardType == CardType.Monster &&
                runtime.HasSkill(DefaultGameConfigFactory.SkillSpadeCubId))
            {
                return true;
            }
        }

        return false;
    }
}

public interface IEffectSystem : ISystem
{
}

public sealed class EffectSystem : AbstractSystem, IEffectSystem
{
    protected override void OnInit()
    {
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
    void RestoreHelpDeckSnapshot();
    bool CanAddHelpCard(string cardId);
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
        rewardModel.ClearRoomCandidateIds();
        rewardModel.AddRoomCandidateId(DefaultGameConfigFactory.RoomGoldId);
        rewardModel.AddRoomCandidateId(DefaultGameConfigFactory.RoomChestId);
        rewardModel.AddRoomCandidateId(DefaultGameConfigFactory.RoomAttributeId);
        rewardModel.AddRoomCandidateId(DefaultGameConfigFactory.RoomShopId);
    }

    public void GenerateTutorSkillCandidates()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        rewardModel.ClearTutorSkillIds();

        var pool = new List<string>();
        var tutorPool = DefaultGameConfigFactory.TutorSkillPoolIds;
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

    public void RestoreHelpDeckSnapshot()
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

    public bool CanAddHelpCard(string cardId)
    {
        var deckModel = this.GetModel<IDeckModel>();
        var configModel = this.GetModel<IConfigModel>();
        var runModel = this.GetModel<IRunModel>();
        var capacity = deckModel.GetHelpDeckCapacity(runModel.Layer.Value);
        var currentCount = CountActiveHelpCards(deckModel);

        if (currentCount >= capacity)
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
    void GenerateChestRewardCandidates();
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

    public void GenerateChestRewardCandidates()
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

        // Remove owned relics
        var pool = new List<string>();
        for (var i = 0; i < allRelics.Count; i++)
        {
            if (!playerModel.HasRelic(allRelics[i].RelicId))
            {
                pool.Add(allRelics[i].RelicId);
            }
        }

        if (playerModel.Relics.Count >= playerModel.MaxRelicCount || pool.Count == 0)
        {
            return;
        }

        var pickCount = pool.Count < RewardConstants.ChestRewardCandidateCount ? pool.Count : RewardConstants.ChestRewardCandidateCount;
        for (var i = 0; i < pickCount; i++)
        {
            var index = randomUtility.Range(0, pool.Count);
            rewardModel.AddChestRewardRelicId(pool[index]);
            pool.RemoveAt(index);
        }
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
            this.SendEvent(new PopupRequestedEvent("帮助卡组已满或同名卡达到上限。"));
            return false;
        }

        // [S2 FIX] Use RewardConstants + send GoldChangedEvent
        this.ChangeGold(playerModel, -definition.Price);
        var runtime = collectionModel.CreateCard(definition);
        deckModel.OwnedHelpCards.Add(runtime.Uid);
        deckModel.HelpCardStates[runtime.Uid.Value] = new HelpCardState
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId
        };

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
            state.IsPermanentlyRemoved = true;
            state.IsOnBoard = false;
            state.IsInItemSlot = false;
        }

        if (runtime.BoardSlot.HasValue)
        {
            var boardSystem = this.GetSystem<IBoardSystem>();
            boardSystem.RemoveCardAt(runtime.BoardSlot.Value);
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
