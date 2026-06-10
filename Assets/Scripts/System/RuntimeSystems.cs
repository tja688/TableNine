using System;
using System.Collections.Generic;
using QFramework;

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
        for (var i = 0; i < rule.TotalCardCount; i++)
        {
            var cardId = rule.AllowedMonsterCardIds[randomUtility.Range(0, rule.AllowedMonsterCardIds.Count)];
            var definition = configModel.GetCardDefinition(cardId);
            var runtime = collectionModel.CreateCard(definition);
            deckModel.DemonDeckQueue.Enqueue(runtime.Uid);
        }
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

        return new EffectiveStats
        {
            CurrentHp = playerRuntime.CurrentHp,
            MaxHp = playerRuntime.MaxHp,
            Attack = playerRuntime.BaseAttack,
            Defense = playerRuntime.BaseDefense,
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

public interface IRewardSystem : ISystem
{
    void GenerateHelpRewardCandidates();
    void GenerateRoomCandidates();
    void SettleUnusedHelpCards();
    void RestoreHelpDeckSnapshot();
    bool CanAddHelpCard(string cardId);
}

public sealed class RewardSystem : AbstractSystem, IRewardSystem
{
    private static readonly float[] QualityWeights = { 65f, 30f, 5f, 0f }; // White, Blue, Gold, Red

    protected override void OnInit() { }

    public void GenerateHelpRewardCandidates()
    {
        var configModel = this.GetModel<IConfigModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var runModel = this.GetModel<IRunModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        rewardModel.HelpRewardCardIds.Clear();

        var allHelpCards = GetAllHelpCardIdsByQuality(configModel);
        var capacity = deckModel.GetHelpDeckCapacity(runModel.Layer.Value);
        var currentCount = CountActiveHelpCards(deckModel);

        for (var i = 0; i < 3; i++)
        {
            var targetQuality = RollQuality(randomUtility);
            var candidates = FilterCandidates(allHelpCards, targetQuality, deckModel, configModel, capacity, currentCount + i);
            if (candidates.Count == 0)
            {
                // Fallback to any quality
                candidates = FilterCandidates(allHelpCards, CardQuality.White, deckModel, configModel, capacity, currentCount + i);
                if (candidates.Count == 0)
                {
                    candidates = GetAllAvailableHelpCards(configModel, deckModel, capacity, currentCount + i);
                }
            }
            if (candidates.Count > 0)
            {
                var pick = candidates[randomUtility.Range(0, candidates.Count)];
                rewardModel.HelpRewardCardIds.Add(pick);
            }
        }
    }

    public void GenerateRoomCandidates()
    {
        var rewardModel = this.GetModel<IRewardModel>();
        rewardModel.RoomCandidateIds.Clear();
        // Room candidates are always the 4 types: Gold, Chest, Attribute, Shop
        rewardModel.RoomCandidateIds.Add(DefaultGameConfigFactory.RoomGoldId);
        rewardModel.RoomCandidateIds.Add(DefaultGameConfigFactory.RoomChestId);
        rewardModel.RoomCandidateIds.Add(DefaultGameConfigFactory.RoomAttributeId);
        rewardModel.RoomCandidateIds.Add(DefaultGameConfigFactory.RoomShopId);
    }

    public void SettleUnusedHelpCards()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var playerModel = this.GetModel<IPlayerModel>();

        // Count help cards on board or in item slots that are NOT permanently removed
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

        playerModel.Gold.Value += unusedCount * 10;
        this.SendEvent(new HelpCardsSettledEvent(unusedCount, unusedCount * 10));
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

        // Same name limit: max 3
        var sameNameCount = CountSameNameCards(deckModel, cardId, configModel);
        if (sameNameCount >= 3)
        {
            return false;
        }

        return true;
    }

    private static List<string> GetAllHelpCardIdsByQuality(IConfigModel configModel)
    {
        // This returns all help card IDs grouped by quality - simplified to return all help cards
        // The actual quality filtering happens in FilterCandidates
        return new List<string>
        {
            DefaultGameConfigFactory.HelpPotionId,
            DefaultGameConfigFactory.HelpThrowingKnifeId,
            DefaultGameConfigFactory.HelpCommonChestId,
            DefaultGameConfigFactory.HelpAttributeUpId
        };
    }

    private static CardQuality RollQuality(IRandomUtility random)
    {
        var roll = random.Value() * 100f;
        var cumulative = 0f;
        // QualityWeights: [0]=White, [1]=Blue, [2]=Gold, [3]=Red
        // CardQuality enum: Initial=0, White=1, Blue=2, Gold=3, Red=4
        var qualityMap = new[] { CardQuality.White, CardQuality.Blue, CardQuality.Gold, CardQuality.Red };
        for (var i = 0; i < QualityWeights.Length; i++)
        {
            cumulative += QualityWeights[i];
            if (roll < cumulative)
            {
                return qualityMap[i];
            }
        }
        return CardQuality.White;
    }

    private static List<string> FilterCandidates(List<string> allCards, CardQuality quality, IDeckModel deckModel, IConfigModel configModel, int capacity, int currentCount)
    {
        var result = new List<string>();
        for (var i = 0; i < allCards.Count; i++)
        {
            var cardId = allCards[i];
            try
            {
                var def = configModel.GetCardDefinition(cardId);
                if (def.CardType == CardType.Help && def.Quality == quality)
                {
                    if (currentCount < capacity)
                    {
                        var sameName = CountSameNameCards(deckModel, cardId, configModel);
                        if (sameName < 3)
                        {
                            result.Add(cardId);
                        }
                    }
                }
            }
            catch
            {
                // Skip cards that don't exist in config
            }
        }
        return result;
    }

    private static List<string> GetAllAvailableHelpCards(IConfigModel configModel, IDeckModel deckModel, int capacity, int currentCount)
    {
        var result = new List<string>();
        if (currentCount >= capacity) return result;

        var allCards = new[] {
            DefaultGameConfigFactory.HelpPotionId,
            DefaultGameConfigFactory.HelpThrowingKnifeId,
            DefaultGameConfigFactory.HelpAttributeUpId
        };
        for (var i = 0; i < allCards.Length; i++)
        {
            var sameName = CountSameNameCards(deckModel, allCards[i], configModel);
            if (sameName < 3)
            {
                result.Add(allCards[i]);
            }
        }
        return result;
    }

    private static int CountActiveHelpCards(IDeckModel deckModel)
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

    private static int CountSameNameCards(IDeckModel deckModel, string cardId, IConfigModel configModel)
    {
        var count = 0;
        string targetDisplayName;
        try
        {
            targetDisplayName = configModel.GetCardDefinition(cardId).DisplayName;
        }
        catch
        {
            return 0;
        }

        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (deckModel.HelpCardStates.TryGetValue(uid.Value, out var state) && !state.IsPermanentlyRemoved)
            {
                try
                {
                    var existingDef = configModel.GetCardDefinition(state.DefinitionId);
                    if (existingDef.DisplayName == targetDisplayName)
                    {
                        count++;
                    }
                }
                catch
                {
                    // Skip
                }
            }
        }
        return count;
    }
}

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
        playerModel.AddRelic(new RelicRuntime
        {
            RelicId = relicId,
            DisplayName = definition.DisplayName
        });

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

        playerModel.Gold.Value += 20;
        ApplyRelicStats();
        this.SendEvent(new RelicDiscardedEvent(relicId, 20));
        return true;
    }

    public void GenerateChestRewardCandidates()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        rewardModel.ChestRewardRelicIds.Clear();

        // All available relics excluding owned
        var pool = new List<string>
        {
            DefaultGameConfigFactory.RelicWoodShieldId,
            DefaultGameConfigFactory.RelicWoodSwordId,
            DefaultGameConfigFactory.RelicWoodArmorId,
            DefaultGameConfigFactory.RelicLivingFleshId,
            DefaultGameConfigFactory.RelicThornArmorId,
            DefaultGameConfigFactory.RelicPhoenixFeatherId
        };

        // Remove owned relics
        pool.RemoveAll(id => playerModel.HasRelic(id));

        // If player is full, no candidates
        if (playerModel.Relics.Count >= playerModel.MaxRelicCount || pool.Count == 0)
        {
            return;
        }

        // Pick up to 3 random relics
        var pickCount = pool.Count < 3 ? pool.Count : 3;
        for (var i = 0; i < pickCount; i++)
        {
            var index = randomUtility.Range(0, pool.Count);
            rewardModel.ChestRewardRelicIds.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    public bool HasRelic(string relicId)
    {
        return this.GetModel<IPlayerModel>().HasRelic(relicId);
    }

    public void ApplyRelicStats()
    {
        // Relic stats are applied as modifiers to effective stats
        // For now, this is a notification mechanism; StatSystem will query relics
        this.SendEvent(new RelicStatsChangedEvent());
    }
}

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
        var randomUtility = this.GetUtility<IRandomUtility>();

        rewardModel.ShopCardIds.Clear();

        var pool = new List<string>
        {
            DefaultGameConfigFactory.HelpPotionId,
            DefaultGameConfigFactory.HelpThrowingKnifeId,
            DefaultGameConfigFactory.HelpCommonChestId,
            DefaultGameConfigFactory.HelpAttributeUpId
        };

        randomUtility.Shuffle(pool);
        var count = pool.Count < 6 ? pool.Count : 6;
        for (var i = 0; i < count; i++)
        {
            rewardModel.ShopCardIds.Add(pool[i]);
        }
    }

    public bool BuyHelpCard(string cardId)
    {
        var configModel = this.GetModel<IConfigModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
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

        playerModel.Gold.Value -= definition.Price;
        var runtime = collectionModel.CreateCard(definition);
        deckModel.OwnedHelpCards.Add(runtime.Uid);
        deckModel.HelpCardStates[runtime.Uid.Value] = new HelpCardState
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId
        };

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

        // Check if card is in owned list
        if (!deckModel.OwnedHelpCards.Contains(helpCardUid))
        {
            return false;
        }

        // Mark as permanently removed
        if (deckModel.HelpCardStates.TryGetValue(helpCardUid.Value, out var state))
        {
            state.IsPermanentlyRemoved = true;
            state.IsOnBoard = false;
            state.IsInItemSlot = false;
        }

        // Remove from board if present
        if (runtime.BoardSlot.HasValue)
        {
            var boardSystem = this.GetSystem<IBoardSystem>();
            boardSystem.RemoveCardAt(runtime.BoardSlot.Value);
        }

        // Remove from item slot if present
        if (runtime.ItemSlotIndex.HasValue)
        {
            deckModel.ItemSlots[runtime.ItemSlotIndex.Value] = null;
            this.SendEvent(new ItemSlotChangedEvent(runtime.ItemSlotIndex.Value, null));
        }

        playerModel.Gold.Value += 10;
        this.SendEvent(new HelpCardDeletedForGoldEvent(helpCardUid, 10));
        return true;
    }
}

