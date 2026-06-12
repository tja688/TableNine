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
        this.RegisterEvent<RunReplayCompletedEvent>(e => Record($"Replay done count={e.CommandCount} hashOk={e.HashMatched}"));
    }

    private void Record(string message)
    {
        this.GetUtility<IDebugEventLogUtility>().Record(message);
    }
}
