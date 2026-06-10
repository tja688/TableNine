using QFramework;

public interface IRunModel : IModel
{
}

public sealed class RunModel : AbstractModel, IRunModel
{
    protected override void OnInit()
    {
    }
}

public interface IPlayerModel : IModel
{
}

public sealed class PlayerModel : AbstractModel, IPlayerModel
{
    protected override void OnInit()
    {
    }
}

public interface IBoardModel : IModel
{
}

public sealed class BoardModel : AbstractModel, IBoardModel
{
    protected override void OnInit()
    {
    }
}

public interface IDeckModel : IModel
{
}

public sealed class DeckModel : AbstractModel, IDeckModel
{
    protected override void OnInit()
    {
    }
}

public interface ICollectionModel : IModel
{
}

public sealed class CollectionModel : AbstractModel, ICollectionModel
{
    protected override void OnInit()
    {
    }
}

public interface IConfigModel : IModel
{
}

public sealed class ConfigModel : AbstractModel, IConfigModel
{
    protected override void OnInit()
    {
    }
}

public interface IFlowModel : IModel
{
}

public sealed class FlowModel : AbstractModel, IFlowModel
{
    protected override void OnInit()
    {
    }
}
