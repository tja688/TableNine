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
