using System.Collections.Generic;

public readonly struct CardPlacedEvent
{
    public CardPlacedEvent(CardUid uid, BoardSlotNo slot, CardPlacementSource source)
    {
        Uid = uid;
        Slot = slot;
        Source = source;
    }

    public CardUid Uid { get; }
    public BoardSlotNo Slot { get; }
    public CardPlacementSource Source { get; }
}

public readonly struct BoardSlotChangedEvent
{
    public BoardSlotChangedEvent(BoardSlotNo slot, CardUid? uid)
    {
        Slot = slot;
        Uid = uid;
    }

    public BoardSlotNo Slot { get; }
    public CardUid? Uid { get; }
}

public readonly struct BattleDeckChangedEvent
{
    public BattleDeckChangedEvent(int remainingCount, CardPreview preview)
    {
        RemainingCount = remainingCount;
        Preview = preview;
    }

    public int RemainingCount { get; }
    public CardPreview Preview { get; }
}

public readonly struct DamageAppliedEvent
{
    public DamageAppliedEvent(CardUid targetUid, int damage)
    {
        TargetUid = targetUid;
        Damage = damage;
    }

    public CardUid TargetUid { get; }
    public int Damage { get; }
}

public readonly struct MonsterKilledEvent
{
    public MonsterKilledEvent(CardUid monsterUid, string definitionId)
    {
        MonsterUid = monsterUid;
        DefinitionId = definitionId;
    }

    public CardUid MonsterUid { get; }
    public string DefinitionId { get; }
}

public readonly struct LevelClearReadyEvent
{
    public LevelClearReadyEvent(int layer, int nodeInLayer)
    {
        Layer = layer;
        NodeInLayer = nodeInLayer;
    }

    public int Layer { get; }
    public int NodeInLayer { get; }
}

public readonly struct GameplayMessageEvent
{
    public GameplayMessageEvent(string message)
    {
        Message = message;
    }

    public string Message { get; }
}

public readonly struct HelpRewardGeneratedEvent
{
    public HelpRewardGeneratedEvent(IReadOnlyList<string> cardIds)
    {
        CardIds = cardIds;
    }

    public IReadOnlyList<string> CardIds { get; }
}

public readonly struct HelpRewardPickedEvent
{
    public HelpRewardPickedEvent(string cardId)
    {
        CardId = cardId;
    }

    public string CardId { get; }
}

public readonly struct HelpRewardSkippedEvent
{
    public HelpRewardSkippedEvent(int goldGained)
    {
        GoldGained = goldGained;
    }

    public int GoldGained { get; }
}

public readonly struct ShopOpenedEvent
{
    public ShopOpenedEvent(IReadOnlyList<string> cardIds)
    {
        CardIds = cardIds;
    }

    public IReadOnlyList<string> CardIds { get; }
}

public readonly struct NodeCompletedEvent
{
    public NodeCompletedEvent(int layer, int nodeInLayer)
    {
        Layer = layer;
        NodeInLayer = nodeInLayer;
    }

    public int Layer { get; }
    public int NodeInLayer { get; }
}

public readonly struct RoomChosenEvent
{
    public RoomChosenEvent(string roomId, RoomType roomType)
    {
        RoomId = roomId;
        RoomType = roomType;
    }

    public string RoomId { get; }
    public RoomType RoomType { get; }
}

public readonly struct HelpCardsSettledEvent
{
    public HelpCardsSettledEvent(int unusedCount, int goldGained)
    {
        UnusedCount = unusedCount;
        GoldGained = goldGained;
    }
    public int UnusedCount { get; }
    public int GoldGained { get; }
}

public readonly struct HelpDeckRestoredEvent
{
    public HelpDeckRestoredEvent(int restoredCount, int permanentRemovedCount, int newCardCount)
    {
        RestoredCount = restoredCount;
        PermanentRemovedCount = permanentRemovedCount;
        NewCardCount = newCardCount;
    }

    public int RestoredCount { get; }
    public int PermanentRemovedCount { get; }
    public int NewCardCount { get; }
}

public readonly struct GoldChangedEvent
{
    public GoldChangedEvent(int oldValue, int newValue, int delta)
    {
        OldValue = oldValue;
        NewValue = newValue;
        Delta = delta;
    }

    public int OldValue { get; }
    public int NewValue { get; }
    public int Delta { get; }
}

public readonly struct RelicAddedEvent
{
    public RelicAddedEvent(string relicId)
    {
        RelicId = relicId;
    }
    public string RelicId { get; }
}

public readonly struct RelicDiscardedEvent
{
    public RelicDiscardedEvent(string relicId, int goldGained)
    {
        RelicId = relicId;
        GoldGained = goldGained;
    }
    public string RelicId { get; }
    public int GoldGained { get; }
}

public readonly struct RelicStatsChangedEvent { }

public readonly struct ChestRewardGeneratedEvent
{
    public ChestRewardGeneratedEvent(IReadOnlyList<string> relicIds)
    {
        RelicIds = relicIds;
    }
    public IReadOnlyList<string> RelicIds { get; }
}

public readonly struct RelicRewardPickedEvent
{
    public RelicRewardPickedEvent(string relicId)
    {
        RelicId = relicId;
    }
    public string RelicId { get; }
}

public readonly struct ChestRewardSkippedEvent
{
    public ChestRewardSkippedEvent(int goldGained)
    {
        GoldGained = goldGained;
    }
    public int GoldGained { get; }
}

public readonly struct HelpCardPurchasedEvent
{
    public HelpCardPurchasedEvent(string cardId, int price)
    {
        CardId = cardId;
        Price = price;
    }
    public string CardId { get; }
    public int Price { get; }
}

public readonly struct HelpCardDeletedForGoldEvent
{
    public HelpCardDeletedForGoldEvent(CardUid cardUid, int goldGained)
    {
        CardUid = cardUid;
        GoldGained = goldGained;
    }
    public CardUid CardUid { get; }
    public int GoldGained { get; }
}

public readonly struct TutorSkillChosenEvent
{
    public TutorSkillChosenEvent(string skillId)
    {
        SkillId = skillId;
    }
    public string SkillId { get; }
}

public readonly struct BattleDeckCardsInjectedEvent
{
    public BattleDeckCardsInjectedEvent(IReadOnlyList<string> cardIds)
    {
        CardIds = cardIds;
    }

    public IReadOnlyList<string> CardIds { get; }
}

public readonly struct LayerCompletedEvent
{
    public LayerCompletedEvent(int layer)
    {
        Layer = layer;
    }

    public int Layer { get; }
}

public readonly struct NodeAdvancedEvent
{
    public NodeAdvancedEvent(int layer, int fromNode, int toNode)
    {
        Layer = layer;
        FromNode = fromNode;
        ToNode = toNode;
    }
    public int Layer { get; }
    public int FromNode { get; }
    public int ToNode { get; }
}
