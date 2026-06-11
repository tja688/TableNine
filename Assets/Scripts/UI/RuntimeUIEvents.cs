public readonly struct AttributeChoiceRequestedEvent
{
    public AttributeChoiceRequestedEvent(CardUid helpCardUid)
    {
        HelpCardUid = helpCardUid;
    }

    public CardUid HelpCardUid { get; }
}

public readonly struct AttributeChoiceResolvedEvent
{
    public AttributeChoiceResolvedEvent(CardUid helpCardUid, AttributeUpgradeChoice choice)
    {
        HelpCardUid = helpCardUid;
        Choice = choice;
    }

    public CardUid HelpCardUid { get; }
    public AttributeUpgradeChoice Choice { get; }
}

public readonly struct PopupRequestedEvent
{
    public PopupRequestedEvent(string message)
    {
        Message = message;
    }

    public string Message { get; }
}

public readonly struct RoomChoiceRequestedEvent
{
    public RoomChoiceRequestedEvent(System.Collections.Generic.IReadOnlyList<string> roomIds)
    {
        RoomIds = roomIds;
    }

    public System.Collections.Generic.IReadOnlyList<string> RoomIds { get; }
}

public readonly struct TutorSkillChoiceRequestedEvent
{
    public TutorSkillChoiceRequestedEvent(System.Collections.Generic.IReadOnlyList<string> skillIds)
    {
        SkillIds = skillIds;
    }

    public System.Collections.Generic.IReadOnlyList<string> SkillIds { get; }
}

public readonly struct ItemSlotChangedEvent
{
    public ItemSlotChangedEvent(int itemSlotIndex, CardUid? uid)
    {
        ItemSlotIndex = itemSlotIndex;
        Uid = uid;
    }

    public int ItemSlotIndex { get; }
    public CardUid? Uid { get; }
}
