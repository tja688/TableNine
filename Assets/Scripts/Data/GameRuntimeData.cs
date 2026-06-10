using System;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Player,
    Monster,
    Help,
    Room,
    Tutor
}

public enum CardQuality
{
    Initial,
    White,
    Blue,
    Gold,
    Red
}

public enum MonsterLevel
{
    None,
    Level1,
    Level2,
    Level3,
    Level4,
    Elite,
    Boss
}

public enum Suit
{
    None,
    Spade,
    Heart,
    Diamond,
    Club
}

public enum RoomType
{
    None,
    Shop,
    Gold,
    Chest,
    Attribute
}

public enum FlowPhase
{
    None,
    Boot,
    RunStarting,
    NodeStarting,
    OpeningDeal,
    PlayerControl,
    CombatResolving,
    BoardRefilling,
    ClearReady,
    GameOver
}

public enum InputLockReason
{
    BoardRefillRunning,
    CombatResolving,
    BoardMoving,
    OverlayVisible
}

public enum InteractionKind
{
    None,
    EmptySlot,
    MonsterCard,
    HelpCard
}

public enum CardPlacementSource
{
    OpeningHelp,
    OpeningDemon,
    OpeningBattle,
    Refill
}

public enum PendingHelpCardActionKind
{
    None,
    ThrowingKnifeTarget,
    AttributeChoice
}

public enum AttributeUpgradeChoice
{
    Attack,
    Defense,
    MaxHp
}

[Serializable]
public struct CardUid : IEquatable<CardUid>
{
    public CardUid(int value)
    {
        Value = value;
    }

    public int Value;

    public bool Equals(CardUid other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object obj)
    {
        return obj is CardUid other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

[Serializable]
public struct BoardSlotNo : IEquatable<BoardSlotNo>
{
    public BoardSlotNo(int value)
    {
        Value = value;
    }

    public int Value;

    public bool Equals(BoardSlotNo other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object obj)
    {
        return obj is BoardSlotNo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

public static class BoardSlotUtility
{
    public static readonly BoardSlotNo[] AllSlots =
    {
        new BoardSlotNo(1),
        new BoardSlotNo(2),
        new BoardSlotNo(3),
        new BoardSlotNo(4),
        new BoardSlotNo(5),
        new BoardSlotNo(6),
        new BoardSlotNo(7),
        new BoardSlotNo(8),
        new BoardSlotNo(9)
    };

    public static readonly BoardSlotNo[] ClockwiseRing =
    {
        new BoardSlotNo(1),
        new BoardSlotNo(2),
        new BoardSlotNo(3),
        new BoardSlotNo(6),
        new BoardSlotNo(9),
        new BoardSlotNo(8),
        new BoardSlotNo(7),
        new BoardSlotNo(4)
    };

    public static bool IsValid(BoardSlotNo slot)
    {
        return slot.Value >= 1 && slot.Value <= 9;
    }

    public static bool IsOrthogonalAdjacent(BoardSlotNo from, BoardSlotNo to)
    {
        var fromX = (from.Value - 1) % 3;
        var fromY = (from.Value - 1) / 3;
        var toX = (to.Value - 1) % 3;
        var toY = (to.Value - 1) / 3;
        return Mathf.Abs(fromX - toX) + Mathf.Abs(fromY - toY) == 1;
    }

    public static bool IsTopRow(BoardSlotNo slot)
    {
        return slot.Value >= 1 && slot.Value <= 3;
    }

    public static bool IsBottomRow(BoardSlotNo slot)
    {
        return slot.Value >= 7 && slot.Value <= 9;
    }
}

[Serializable]
public sealed class CharacterDefinition
{
    public string CharacterId;
    public string DisplayName;
    public int BaseHp;
    public int BaseAttack;
    public int BaseDefense;
    public List<string> InitialSkillIds = new List<string>();
    public List<string> InitialHelpCardIds = new List<string>();
}

[Serializable]
public sealed class CardDefinition
{
    public string CardId;
    public string DisplayName;
    public CardType CardType;
    public CardQuality Quality;
    public MonsterLevel MonsterLevel;
    public Suit Suit;
    public int Rank;
    public int Price;
    public int BaseHp;
    public int BaseAttack;
    public int BaseDefense;
    public bool IsPermanentRemoveOnUse;
    public List<string> SkillIds = new List<string>();
}

[Serializable]
public sealed class SkillDefinition
{
    public string SkillId;
    public string DisplayName;
    public bool GrantsFirstStrike;
}

[Serializable]
public sealed class MonsterDeckRuleDefinition
{
    public int Layer;
    public int NodeInLayer;
    public int TotalCardCount;
    public List<string> AllowedMonsterCardIds = new List<string>();
}

[CreateAssetMenu(fileName = "GameConfigDatabase", menuName = "TableNine/Game Config Database")]
public sealed class GameConfigDatabase : ScriptableObject
{
    public List<CharacterDefinition> Characters = new List<CharacterDefinition>();
    public List<CardDefinition> Cards = new List<CardDefinition>();
    public List<SkillDefinition> Skills = new List<SkillDefinition>();
    public List<MonsterDeckRuleDefinition> MonsterDeckRules = new List<MonsterDeckRuleDefinition>();

    public GameConfigSet ToConfigSet()
    {
        var config = new GameConfigSet();
        config.Characters.AddRange(Characters);
        config.Cards.AddRange(Cards);
        config.Skills.AddRange(Skills);
        config.MonsterDeckRules.AddRange(MonsterDeckRules);
        return config;
    }
}

public sealed class GameConfigSet
{
    public List<CharacterDefinition> Characters { get; } = new List<CharacterDefinition>();
    public List<CardDefinition> Cards { get; } = new List<CardDefinition>();
    public List<SkillDefinition> Skills { get; } = new List<SkillDefinition>();
    public List<MonsterDeckRuleDefinition> MonsterDeckRules { get; } = new List<MonsterDeckRuleDefinition>();
}

public sealed class CardRuntime
{
    public CardUid Uid;
    public string DefinitionId;
    public string DisplayName;
    public CardType CardType;
    public MonsterLevel MonsterLevel;
    public Suit Suit;
    public BoardSlotNo? BoardSlot;
    public int? ItemSlotIndex;
    public int CurrentHp;
    public int MaxHp;
    public int BaseAttack;
    public int BaseDefense;
    public List<string> SkillIds = new List<string>();

    public bool HasSkill(string skillId)
    {
        return SkillIds.Contains(skillId);
    }
}

public sealed class HelpCardState
{
    public CardUid Uid;
    public string DefinitionId;
    public bool IsTemporarilyRemoved;
    public bool IsPermanentlyRemoved;
    public bool IsOnBoard;
    public bool IsInItemSlot;

    public HelpCardState Clone()
    {
        return new HelpCardState
        {
            Uid = Uid,
            DefinitionId = DefinitionId,
            IsTemporarilyRemoved = IsTemporarilyRemoved,
            IsPermanentlyRemoved = IsPermanentlyRemoved,
            IsOnBoard = IsOnBoard,
            IsInItemSlot = IsInItemSlot
        };
    }
}

public sealed class HelpDeckSnapshot
{
    public List<HelpCardState> Cards = new List<HelpCardState>();

    public HelpDeckSnapshot Clone()
    {
        var clone = new HelpDeckSnapshot();
        for (var i = 0; i < Cards.Count; i++)
        {
            clone.Cards.Add(Cards[i].Clone());
        }

        return clone;
    }
}

public sealed class PendingHelpCardAction
{
    public CardUid HelpCardUid;
    public PendingHelpCardActionKind Kind;

    public bool IsActive => Kind != PendingHelpCardActionKind.None;

    public void Clear()
    {
        HelpCardUid = default;
        Kind = PendingHelpCardActionKind.None;
    }
}

public struct CardPreview
{
    public CardPreview(CardUid uid, string definitionId, string displayName)
    {
        Uid = uid;
        DefinitionId = definitionId;
        DisplayName = displayName;
    }

    public CardUid Uid;
    public string DefinitionId;
    public string DisplayName;

    public bool IsEmpty => string.IsNullOrEmpty(DefinitionId);

    public static CardPreview Empty => new CardPreview(default, string.Empty, string.Empty);
}

public struct EffectiveStats
{
    public int CurrentHp;
    public int MaxHp;
    public int Attack;
    public int Defense;
    public bool HasFirstStrike;
}

public struct InteractionResult
{
    public InteractionResult(bool canInteract, InteractionKind kind, CardUid targetUid, string reason)
    {
        CanInteract = canInteract;
        Kind = kind;
        TargetUid = targetUid;
        Reason = reason;
    }

    public bool CanInteract;
    public InteractionKind Kind;
    public CardUid TargetUid;
    public string Reason;
}
