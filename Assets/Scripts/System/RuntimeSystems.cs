using QFramework;

public interface IRunSystem : ISystem
{
}

public sealed class RunSystem : AbstractSystem, IRunSystem
{
    protected override void OnInit()
    {
    }
}

public interface ILevelFlowSystem : ISystem
{
}

public sealed class LevelFlowSystem : AbstractSystem, ILevelFlowSystem
{
    protected override void OnInit()
    {
    }
}

public interface IBoardSystem : ISystem
{
}

public sealed class BoardSystem : AbstractSystem, IBoardSystem
{
    protected override void OnInit()
    {
    }
}

public interface IDeckSystem : ISystem
{
}

public sealed class DeckSystem : AbstractSystem, IDeckSystem
{
    protected override void OnInit()
    {
    }
}

public interface ICombatSystem : ISystem
{
}

public sealed class CombatSystem : AbstractSystem, ICombatSystem
{
    protected override void OnInit()
    {
    }
}

public interface IStatSystem : ISystem
{
}

public sealed class StatSystem : AbstractSystem, IStatSystem
{
    protected override void OnInit()
    {
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
