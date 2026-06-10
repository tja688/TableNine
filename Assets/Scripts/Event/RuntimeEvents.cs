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
