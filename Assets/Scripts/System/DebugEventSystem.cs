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
    }

    private void Record(string message)
    {
        this.GetUtility<IDebugEventLogUtility>().Record(message);
    }
}
