using System.Collections.Generic;

public enum SkillTrigger
{
    OnNodeStart,
    OnBeforeCombat,
    OnModifyDamage,
    OnAfterDamage,
    OnAfterCombat,
    OnCardMoved,
    OnMonsterKilled,
    OnNodeClear,
    OnNodeEnd,
    OnHelpCardUsed
}

public enum SkillOwnerKind
{
    Relic,
    PlayerSkill,
    MonsterSkill,
    HelpCardPassive
}

public sealed class SkillEffectBinding
{
    public string BindingId;
    public SkillOwnerKind OwnerKind;
    public string OwnerDefinitionId;
    public SkillTrigger Trigger;
    public string ConditionKey;
    public string EffectGraphId;
}

public sealed class TriggerContext
{
    public CardUid? OwnerUid;
    public string OwnerDefinitionId;
    public BoardSlotNo? CardSlot;
    public BoardSlotNo? PreviousSlot;
    public CombatContext Combat;
    public CombatStep? CombatStep;
    public CardUid? PrimaryAttacker;
    public CardUid? PrimaryDefender;
    public CardUid? HelpCardUsedUid;
    public List<DamageContext> ParallelDamage = new List<DamageContext>();
}

public sealed class SkillTriggerLog
{
    public string OwnerDefinitionId;
    public CardUid? OwnerUid;
    public SkillTrigger Trigger;
    public string EffectGraphId;
    public int Depth;
}

public readonly struct CardMovedEvent
{
    public CardMovedEvent(CardUid uid, BoardSlotNo newSlot, BoardSlotNo? previousSlot, CardPlacementSource source)
    {
        Uid = uid;
        NewSlot = newSlot;
        PreviousSlot = previousSlot;
        Source = source;
    }

    public CardUid Uid { get; }
    public BoardSlotNo NewSlot { get; }
    public BoardSlotNo? PreviousSlot { get; }
    public CardPlacementSource Source { get; }
}

public readonly struct SkillTriggeredEvent
{
    public SkillTriggeredEvent(string ownerDefinitionId, CardUid? ownerUid, SkillTrigger trigger, string effectGraphId)
    {
        OwnerDefinitionId = ownerDefinitionId;
        OwnerUid = ownerUid;
        Trigger = trigger;
        EffectGraphId = effectGraphId;
    }

    public string OwnerDefinitionId { get; }
    public CardUid? OwnerUid { get; }
    public SkillTrigger Trigger { get; }
    public string EffectGraphId { get; }
}
