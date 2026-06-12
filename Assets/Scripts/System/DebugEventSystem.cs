using QFramework;

public interface IDebugEventSystem : ISystem
{
}

public sealed class DebugEventSystem : AbstractSystem, IDebugEventSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<MonsterKilledEvent>(e => Record($"MonsterKilled {e.DefinitionId}"));
        this.RegisterEvent<LevelClearReadyEvent>(e => Record($"ClearReady L{e.Layer}-N{e.NodeInLayer}"));
        this.RegisterEvent<RunSavedEvent>(e => Record($"Saved ({e.Reason})"));
        this.RegisterEvent<RunLoadedEvent>(_ => Record("Loaded save"));
        this.RegisterEvent<GameOverEvent>(e => Record($"GameOver {e.Reason}"));
        this.RegisterEvent<VictoryEvent>(_ => Record("Victory"));
        this.RegisterEvent<FlowPhaseChangedEvent>(e => Record($"Phase {e.PreviousPhase}->{e.NewPhase}"));
        this.RegisterEvent<DamageAppliedEvent>(e => Record($"Damage target={e.TargetUid.Value} amount={e.Damage}"));
        this.RegisterEvent<ArmorChangedEvent>(e => Record($"Armor {e.TargetUid.Value} {e.OldArmor}->{e.NewArmor} cause={e.CauseId}"));
        this.RegisterEvent<HealAppliedEvent>(e => Record($"Heal {e.TargetUid.Value} {e.OldHp}->{e.NewHp} cause={e.CauseId}"));
        this.RegisterEvent<CardPlacedEvent>(e => Record($"CardPlaced {e.Uid.Value} slot={e.Slot.Value} source={e.Source}"));
        this.RegisterEvent<CardRemovedEvent>(e => Record($"CardRemoved {e.Uid.Value} slot={e.Slot.Value} reason={e.Reason}"));
        this.RegisterEvent<BoardRotatedEvent>(e => Record($"BoardRotated clockwise={e.Clockwise} reason={e.Reason} moves={e.MovedCards.Count}"));
        this.RegisterEvent<DialogueRequestedEvent>(e => Record($"DialogueRequested {e.Message}"));
        this.RegisterEvent<DialogueCompletedEvent>(e => Record($"DialogueCompleted {e.Message}"));
        this.RegisterEvent<PresentationSequenceRequestedEvent>(e => Record($"SequenceRequested {e.SequenceType} -> {e.CompletionAction}"));
        this.RegisterEvent<PresentationSequenceCompletedEvent>(e => Record($"SequenceCompleted {e.SequenceType} -> {e.CompletionAction}"));
        this.RegisterEvent<EffectResolvedEvent>(e => Record($"EffectResolved {e.EffectGraphId} source={e.Source}"));
        this.RegisterEvent<OverlayOpenedEvent>(e => Record($"OverlayOpened {e.UiKey} blocks={e.BlocksGameplayInput}"));
        this.RegisterEvent<OverlayClosedEvent>(e => Record($"OverlayClosed {e.UiKey}"));
        this.RegisterEvent<RunReplayCompletedEvent>(e => Record($"Replay done count={e.CommandCount} hashOk={e.HashMatched}"));
    }

    private void Record(string message)
    {
        this.GetUtility<IDebugEventLogUtility>().Record(message);
    }
}
