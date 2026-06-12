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
    BoardMoving,
    BoardRefilling,
    ClearReady,
    RoomChoosing,
    RoomResolving,
    HelpRewardChoosing,
    ChestRewardChoosing,
    TutorSkillChoosing,
    Shop,
    GameOver,
    NodeEnding,
    LayerComplete,
    Victory
}

public enum InputLockReason
{
    BoardRefillRunning,
    CombatResolving,
    BoardMoving,
    SequenceRunning,
    DialogueRunning,
    OverlayVisible,
    RewardOverlay,
    ShopOverlay
}

public enum PresentationSequenceType
{
    None,
    CombatResolution,
    BoardRotation,
    BoardRefill
}

public enum SequenceCompletionAction
{
    None,
    ResumeAfterCombat,
    ResumeAfterBoardRotation,
    ResumeAfterBoardRefill
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
    Refill,
    BoardMove
}

public enum PendingHelpCardActionKind
{
    None,
    ThrowingKnifeTarget,
    AttributeChoice,
    BlessingShield,
    SwapTarget
}

public enum RewardSource
{
    None,
    NodeClear,
    ChestCard,
    EliteKill,
    BossKill,
    Room
}

public enum ChestTier
{
    Normal,
    Blue,
    Gold
}

public enum AttributeUpgradeChoice
{
    Attack,
    Defense,
    MaxHp
}

public enum DamageType
{
    Combat,
    HelpCard,
    Reflect,
    Relic,
    Skill,
    Room,
    Debug
}

public enum RemoveReason
{
    None,
    Combat,
    HelpCard,
    Effect,
    Refill,
    Debug
}

public enum BoardMoveReason
{
    None,
    PlayerAction,
    HelpCard,
    Refill,
    Combat,
    Debug
}

public enum HelpCardConsumeReason
{
    None,
    Used,
    Sold,
    Discarded,
    Debug
}

public enum StatType
{
    Attack,
    Defense,
    MaxHp,
    CurrentHp,
    Armor
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
    public bool RestoreAfterNode;
    public bool RestoreAfterNodeAuthoritative;
    public string EffectGraphId;
    public List<string> SkillIds = new List<string>();
}

[Serializable]
public sealed class SkillDefinition
{
    public string SkillId;
    public string DisplayName;
    public string Description;
    public bool GrantsFirstStrike;
    public bool HasRuntimeBinding;
    public SkillTrigger Trigger;
    public string ConditionKey;
    public string EffectGraphId;
    public int MaxHpOnAcquire;
}

[Serializable]
public sealed class MonsterLevelQuotaDefinition
{
    public MonsterLevel Level;
    public int MinCount;
    public int MaxCount;
    public List<string> PoolCardIds = new List<string>();
}

[Serializable]
public sealed class MonsterDeckRuleDefinition
{
    public int Layer;
    public int NodeInLayer;
    public int TotalCardCount;
    public List<string> AllowedMonsterCardIds = new List<string>();
    public List<MonsterLevelQuotaDefinition> LevelQuotas = new List<MonsterLevelQuotaDefinition>();
    public List<string> MandatoryMonsterCardIds = new List<string>();
}

[CreateAssetMenu(fileName = "GameConfigDatabase", menuName = "TableNine/Game Config Database")]
public sealed class GameConfigDatabase : ScriptableObject
{
    public List<CharacterDefinition> Characters = new List<CharacterDefinition>();
    public List<CardDefinition> Cards = new List<CardDefinition>();
    public List<SkillDefinition> Skills = new List<SkillDefinition>();
    public List<MonsterDeckRuleDefinition> MonsterDeckRules = new List<MonsterDeckRuleDefinition>();
    public List<RelicDefinition> Relics = new List<RelicDefinition>();
    public List<RoomDefinition> Rooms = new List<RoomDefinition>();

    public GameConfigSet ToConfigSet()
    {
        var config = new GameConfigSet();
        config.Characters.AddRange(Characters);
        config.Cards.AddRange(Cards);
        config.Skills.AddRange(Skills);
        config.MonsterDeckRules.AddRange(MonsterDeckRules);
        config.Relics.AddRange(Relics);
        config.Rooms.AddRange(Rooms);
        CardDefinitionMigration.MigrateHelpCardSemantics(config.Cards);
        EffectGraphRegistry.AssignToConfig(config);
        return config;
    }
}

public sealed class GameConfigSet
{
    public List<CharacterDefinition> Characters { get; } = new List<CharacterDefinition>();
    public List<CardDefinition> Cards { get; } = new List<CardDefinition>();
    public List<SkillDefinition> Skills { get; } = new List<SkillDefinition>();
    public List<MonsterDeckRuleDefinition> MonsterDeckRules { get; } = new List<MonsterDeckRuleDefinition>();
    public List<RelicDefinition> Relics { get; } = new List<RelicDefinition>();
    public List<RoomDefinition> Rooms { get; } = new List<RoomDefinition>();
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
    public int CurrentArmor;
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
    public int TargetingDamage;
    public string TargetingCauseId;
    public string TargetingMode;
    public bool SwapFirstTargetSelected;
    public CardUid? SwapFirstTargetUid;

    public bool IsActive => Kind != PendingHelpCardActionKind.None;

    public void Clear()
    {
        HelpCardUid = default;
        Kind = PendingHelpCardActionKind.None;
        TargetingDamage = 0;
        TargetingCauseId = null;
        TargetingMode = null;
        SwapFirstTargetSelected = false;
        SwapFirstTargetUid = null;
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
    public int CurrentArmor;
    public int Attack;
    public int Defense;
    public int DamageReduction;
    public bool HasFirstStrike;
}

public enum CombatStep
{
    FirstHit,
    CounterHit
}

public sealed class CombatResult
{
    public bool MonsterKilled;
    public bool PlayerDied;
    public bool PlayerDeathPrevented;
}

public sealed class CombatContext
{
    public CardUid PlayerUid;
    public CardUid MonsterUid;
    public EffectiveStats PlayerStats;
    public EffectiveStats MonsterStats;
    public bool PlayerActsFirst;
    public List<DamageContext> FirstHitGroup = new List<DamageContext>();
    public List<DamageContext> CounterHitGroup = new List<DamageContext>();
    public CombatResult Result = new CombatResult();
}

public static class CombatConstants
{
    public const int ThornArmorDamage = 2;

    public const string CauseCombatPlayerFirst = "combat_player_first";
    public const string CauseCombatPlayerCounter = "combat_player_counter";
    public const string CauseCombatMonsterFirst = "combat_monster_first";
    public const string CauseCombatMonsterCounter = "combat_monster_counter";
    public const string CauseThornArmor = "relic_thorn_armor";
    public const string CauseThornSkin = "skill_thorn_skin";
}

public sealed class DamageContext
{
    public CardUid? Source;
    public CardUid Target;
    public string CauseId;
    public DamageType Type;
    public int RawAttack;
    public int DamageReduction;
    public int DamageBeforeArmor;
    public int ArmorAbsorbed;
    public int HpDamage;
    public bool IgnoreArmor;
    public bool Preventable = true;
    public bool WasPrevented;
    public bool WasFatalBeforePrevention;
    public List<string> Tags = new List<string>();

    public static DamageContext FromLegacyIntDamage(CardUid targetUid, int damage, DamageType type = DamageType.Debug)
    {
        var beforeArmor = damage < 0 ? 0 : damage;
        return new DamageContext
        {
            Target = targetUid,
            CauseId = "legacy_int_damage",
            Type = type,
            RawAttack = beforeArmor,
            DamageBeforeArmor = beforeArmor
        };
    }
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

public struct DeckCapacity
{
    public DeckCapacity(int maxTotal, int sameNameLimit)
    {
        MaxTotal = maxTotal;
        SameNameLimit = sameNameLimit;
    }

    public int MaxTotal;
    public int SameNameLimit;

    public static DeckCapacity ForLayer(int layer)
    {
        switch (layer)
        {
            case 1: return new DeckCapacity(12, 3);
            case 2: return new DeckCapacity(18, 3);
            default: return new DeckCapacity(24, 3);
        }
    }
}

public sealed class HelpRewardCandidate
{
    public string CardId;
    public string DisplayName;
    public CardQuality Quality;
    public int Price;
}

public sealed class ShopItem
{
    public string CardId;
    public string DisplayName;
    public CardQuality Quality;
    public int Price;
    public bool IsSold;
}

[Serializable]
public sealed class RelicDefinition
{
    public string RelicId;
    public string DisplayName;
    public CardQuality Quality;
    public int StatAttackBonus;
    public int StatDefenseBonus;
    public int StatMaxHpBonus;
    public bool IsOneShot;
    public bool ExcludeFromPool;
    public string TriggerDescription;
}

[Serializable]
public sealed class RoomDefinition
{
    public string RoomId;
    public string DisplayName;
    public RoomType RoomType;
    public int RewardGold;
    public int StatMaxHpBonus;
    public string InjectCardId;
}

[Serializable]
public sealed class RelicInstance
{
    public string RelicId;
    public string DisplayName;
    public CardQuality Quality;
    public int StatAttackBonus;
    public int StatDefenseBonus;
    public int StatMaxHpBonus;
    public bool IsOneShot;
    public bool HasTriggered;
    public bool IsConsumed;

    public static RelicInstance FromDefinition(RelicDefinition definition)
    {
        return new RelicInstance
        {
            RelicId = definition.RelicId,
            DisplayName = definition.DisplayName,
            Quality = definition.Quality,
            StatAttackBonus = definition.StatAttackBonus,
            StatDefenseBonus = definition.StatDefenseBonus,
            StatMaxHpBonus = definition.StatMaxHpBonus,
            IsOneShot = definition.IsOneShot,
            HasTriggered = false,
            IsConsumed = false
        };
    }
}

public static class RewardConstants
{
    public const int UnusedHelpCardGold = 10;
    public const int SkipHelpRewardGold = 10;
    public const int SkipChestRewardGold = 20;
    public const int DeleteHelpCardGold = 10;
    public const int DiscardRelicGold = 20;
    public const int MonsterKillGold = 5;
    public const int MaxRelicSlots = 12;
    public const int ShopDisplayCount = 6;
    public const int HelpRewardCandidateCount = 3;
    public const int RoomCandidateCount = 2;
    public const int ChestRewardCandidateCount = 3;
    public const int AttributeRoomMaxHpBonus = 4;

    public static readonly float[] HelpRewardQualityWeights = { 65f, 30f, 5f, 0f };
    public static readonly float[] NormalChestQualityWeights = { 65f, 30f, 5f };
    public static readonly float[] BlueChestQualityWeights = { 50f, 50f, 10f };
    public static readonly float[] GoldChestQualityWeights = { 0f, 50f, 50f };
}

[Serializable]
public sealed class WeightedItem<T>
{
    public T Value;
    public float Weight;

    public WeightedItem(T value, float weight)
    {
        Value = value;
        Weight = weight;
    }
}

public sealed class ChestRewardCandidate
{
    public string RelicId;
    public string DisplayName;
    public CardQuality Quality;
}
