using NUnit.Framework;
using QFramework;

public sealed class TableNineArchitectureEditModeTests
{
    [SetUp]
    public void SetUp()
    {
        TableNine.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        TableNine.ResetForTests();
    }

    [Test]
    public void TableNine_InitArchitecture_Registers_All_M0_Services()
    {
        TableNine.InitArchitecture();

        Assert.That(TableNine.Current, Is.Not.Null);
        Assert.That(TableNine.Interface.GetUtility<IRandomUtility>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetUtility<ISaveUtility>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetUtility<IConfigUtility>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetUtility<ISequenceUtility>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetUtility<ICommandTraceUtility>(), Is.Not.Null);

        Assert.That(TableNine.Interface.GetModel<IRunModel>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetModel<IPlayerModel>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetModel<IBoardModel>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetModel<IDeckModel>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetModel<ICollectionModel>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetModel<IConfigModel>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetModel<IFlowModel>(), Is.Not.Null);

        Assert.That(TableNine.Interface.GetSystem<IRunSystem>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetSystem<ILevelFlowSystem>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetSystem<IBoardSystem>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetSystem<IDeckSystem>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetSystem<ICombatSystem>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetSystem<IStatSystem>(), Is.Not.Null);
        Assert.That(TableNine.Interface.GetSystem<IEffectSystem>(), Is.Not.Null);
    }

    [Test]
    public void DebugPingCommand_Produces_Before_And_After_Trace()
    {
        TableNine.InitArchitecture();
        var trace = TableNine.Interface.GetUtility<ICommandTraceUtility>();

        trace.Clear();
        TableNine.Interface.SendCommand(new DebugPingCommand());

        Assert.That(trace.Records.Count, Is.EqualTo(2));
        Assert.That(trace.Records[0].CommandType, Is.EqualTo(nameof(DebugPingCommand)));
        Assert.That(trace.Records[0].Phase, Is.EqualTo("before"));
        Assert.That(trace.Records[1].CommandType, Is.EqualTo(nameof(DebugPingCommand)));
        Assert.That(trace.Records[1].Phase, Is.EqualTo("after"));
    }

    [Test]
    public void Architecture_Init_Is_Idempotent()
    {
        TableNine.InitArchitecture();
        var first = TableNine.Current;

        TableNine.InitArchitecture();
        var second = TableNine.Current;

        Assert.That(second, Is.SameAs(first));
    }
}
