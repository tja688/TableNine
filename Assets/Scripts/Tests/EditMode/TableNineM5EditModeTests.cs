using System.Collections.Generic;
using NUnit.Framework;
using QFramework;

/// <summary>
/// M5 全 playtest：27 节点配置、存档读档、重放、层间推进。
/// </summary>
[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineM5EditModeTests
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
    public void ConfigValidator_Passes_For_Full_Playtest_Config()
    {
        var config = DefaultGameConfigFactory.Create();
        var errors = ConfigValidator.Validate(config);
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    [Test]
    public void All_27_Nodes_Have_Correct_DemonDeck_Total()
    {
        for (var layer = 1; layer <= 3; layer++)
        {
            for (var node = 1; node <= 9; node++)
            {
                TableNine.ResetForTests();
                GenerateDemonDeckOnly(layer, node);
                var deckModel = TableNine.Interface.GetModel<IDeckModel>();
                Assert.That(deckModel.DemonDeckQueue.Count, Is.EqualTo(9 + node),
                    $"Layer {layer} node {node} demon deck should be {9 + node}");
            }
        }
    }

    [Test]
    public void Layer2_Node5_Contains_Heart_Elite()
    {
        AssertMandatoryMonster(2, 5, DefaultGameConfigFactory.MonsterHeartEliteId);
    }

    [Test]
    public void Layer3_Node9_Contains_Diamond_Boss()
    {
        AssertMandatoryMonster(3, 9, DefaultGameConfigFactory.MonsterDiamondBossId);
    }

    [Test]
    public void All_Playtest_HelpCards_Exist()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        for (var i = 0; i < DefaultGameConfigFactory.PlaytestHelpCardIds.Length; i++)
        {
            var id = DefaultGameConfigFactory.PlaytestHelpCardIds[i];
            Assert.That(configModel.TryGetCardDefinition(id, out var def), Is.True, $"Missing help card {id}");
            Assert.That(def.CardType, Is.EqualTo(CardType.Help));
        }
    }

    [Test]
    public void SaveLoad_Roundtrip_Preserves_Core_State()
    {
        StartRun(42);
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        playerModel.Gold.Value = 77;
        runModel.SetNode(2, 3);
        flowModel.SetPhase(FlowPhase.PlayerControl);

        var saveSystem = TableNine.Interface.GetSystem<ISaveSystem>();
        var captured = saveSystem.CaptureCurrentRun(SaveRunReason.Manual);

        TableNine.ResetForTests();
        TableNine.InitArchitecture();
        TableNine.Interface.GetSystem<ISaveSystem>().ApplySaveData(captured);

        runModel = TableNine.Interface.GetModel<IRunModel>();
        playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        flowModel = TableNine.Interface.GetModel<IFlowModel>();

        Assert.That(runModel.Seed.Value, Is.EqualTo(42));
        Assert.That(runModel.Layer.Value, Is.EqualTo(2));
        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(3));
        Assert.That(playerModel.Gold.Value, Is.EqualTo(77));
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.PlayerControl));
    }

    [Test]
    public void ProceedToNextNode_LayerComplete_Advances_To_Next_Layer_Node1()
    {
        StartRun(42);
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        runModel.SetNode(1, 9);
        flowModel.SetPhase(FlowPhase.LayerComplete);

        TableNine.Interface.SendCommand(new ProceedToNextNodeCommand());

        Assert.That(runModel.Layer.Value, Is.EqualTo(2));
        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(1));
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.PlayerControl));
    }

    [Test]
    public void CommandReplay_SameSeed_Replays_StartNewRun()
    {
        StartRun(99);
        TableNine.Interface.SendCommand(new DebugPingCommand());
        var replayUtility = TableNine.Interface.GetUtility<ICommandReplayUtility>();
        var replayData = replayUtility.Export();

        TableNine.ResetForTests();
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new ReplayRunCommand(replayData));

        var runModel = TableNine.Interface.GetModel<IRunModel>();
        Assert.That(runModel.Seed.Value, Is.EqualTo(99));
        Assert.That(runModel.Layer.Value, Is.EqualTo(1));
        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(1));
    }

    [Test]
    public void Blessing_Prevents_First_Damage_Only()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        deckModel.PendingHelpCardAction.Kind = PendingHelpCardActionKind.BlessingShield;
        var playerUid = playerModel.PlayerCardUid;
        var player = collectionModel.GetCard(playerUid);
        var hpBefore = player.CurrentHp;

        TableNine.Interface.SendCommand(new ApplyDamageCommand(playerUid, 5));
        Assert.That(player.CurrentHp, Is.EqualTo(hpBefore));
        Assert.That(deckModel.PendingHelpCardAction.IsActive, Is.False);

        player.CurrentArmor = 0;
        TableNine.Interface.SendCommand(new ApplyDamageCommand(playerUid, 3));
        Assert.That(player.CurrentHp, Is.EqualTo(hpBefore - 3));
    }

    private static void AssertMandatoryMonster(int layer, int node, string expectedId)
    {
        GenerateDemonDeckOnly(layer, node);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var found = false;

        foreach (var uid in deckModel.DemonDeckQueue)
        {
            var runtime = collectionModel.GetCard(uid);
            if (runtime.DefinitionId == expectedId)
            {
                found = true;
                break;
            }
        }

        Assert.That(found, Is.True, $"Layer {layer} node {node} should contain {expectedId}");
    }

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static void GenerateDemonDeckOnly(int layer, int nodeInLayer)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.GetSystem<IDeckSystem>().GenerateDemonDeck(layer, nodeInLayer);
    }
}
