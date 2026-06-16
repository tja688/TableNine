using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QFramework;
using QfPlayAgent;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class QfPlayAgentEditModeTests
{
    [SetUp]
    public void SetUp()
    {
        TableNine.ResetForTests();
        PlayAgentBootstrap.RegisterCatalog(QfPlayAgent.Samples.TableNine.TableNinePlayAgentCatalogFactory.CreateDefault());
        PlayAgentBootstrap.RegisterArchitectureProvider(new QfPlayAgent.Samples.TableNine.TableNinePlayAgentArchitectureProvider());
    }

    [TearDown]
    public void TearDown()
    {
        TableNine.ResetForTests();
        PlayAgentSession.ResetSession();
    }

    [Test]
    public void PlayAgentCatalog_Lists_Allowed_TableNine_Commands()
    {
        var commands = PlayAgentCommandCatalog.ListCommands(PlayAgentBootstrap.Catalog);
        Assert.That(commands.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(commands.Any(c => c.Name == "DebugPingCommand" || c.FullTypeName.EndsWith("DebugPingCommand")), Is.True);
    }

    [Test]
    public void PlayAgentInvoker_Executes_DebugPingCommand()
    {
        TableNine.InitArchitecture();
        var result = PlayAgentInvoker.Invoke(TableNine.Interface, "DebugPingCommand", new Dictionary<string, object>());
        Assert.That(result.Success, Is.True, result.Error);
    }

    [Test]
    public void PlayAgentInvoker_StartNewRun_WithSkipOpeningDeal()
    {
        TableNine.InitArchitecture();
        var args = new Dictionary<string, object>
        {
            { "skipOpeningDeal", true },
            { "seedOverride", 42 }
        };

        var result = PlayAgentInvoker.Invoke(TableNine.Interface, "StartNewRunCommand", args);
        Assert.That(result.Success, Is.True, result.Error);

        var flow = TableNine.Interface.GetModel<IFlowModel>();
        Assert.That(flow.Phase.Value, Is.EqualTo(FlowPhase.PlayerControl));
    }
}
