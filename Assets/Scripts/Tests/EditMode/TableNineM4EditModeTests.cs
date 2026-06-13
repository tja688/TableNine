using System.Collections.Generic;
using NUnit.Framework;
using QFramework;

/// <summary>
/// M4 一层可玩测试：9 节点恶魔卡组、精英/层主、击杀奖励注入、导师技能。
/// </summary>
[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineM4EditModeTests
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
    public void Node5_DemonDeck_Contains_Elite_And_Total_Is_14()
    {
        GenerateDemonDeckOnly(1, 5);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        Assert.That(deckModel.DemonDeckQueue.Count, Is.EqualTo(14), "第 5 节点恶魔卡组应为 9+5=14 张");

        var hasElite = false;
        foreach (var uid in deckModel.DemonDeckQueue)
        {
            var runtime = collectionModel.GetCard(uid);
            if (runtime.DefinitionId == GameConfigIds.MonsterSpadeEliteId)
            {
                hasElite = true;
            }
        }

        Assert.That(hasElite, Is.True, "第 5 节点恶魔卡组应包含精英黑桃A");
    }

    [Test]
    public void Node9_DemonDeck_Contains_Boss_And_Total_Is_18()
    {
        GenerateDemonDeckOnly(1, 9);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        Assert.That(deckModel.DemonDeckQueue.Count, Is.EqualTo(18), "第 9 节点恶魔卡组应为 9+9=18 张");

        var hasBoss = false;
        foreach (var uid in deckModel.DemonDeckQueue)
        {
            var runtime = collectionModel.GetCard(uid);
            if (runtime.DefinitionId == GameConfigIds.MonsterSpadeBossId)
            {
                hasBoss = true;
            }
        }

        Assert.That(hasBoss, Is.True, "第 9 节点恶魔卡组应包含层主黑桃J");
    }

    [Test]
    public void All_Layer1_Nodes_Have_Correct_DemonDeck_Total()
    {
        for (var node = 1; node <= 9; node++)
        {
            TableNine.ResetForTests();
            GenerateDemonDeckOnly(1, node);
            var deckModel = TableNine.Interface.GetModel<IDeckModel>();
            Assert.That(deckModel.DemonDeckQueue.Count, Is.EqualTo(9 + node),
                $"节点 {node} 恶魔卡组数量应为 {9 + node}");
        }
    }

    [Test]
    public void KillElite_Injects_Reward_Cards_Into_BattleDeck()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        var eliteDef = configModel.GetCardDefinition(GameConfigIds.MonsterSpadeEliteId);
        var elite = collectionModel.CreateCard(eliteDef);
        var battleCountBefore = deckModel.BattleDrawPile.Count;

        var injectedEvents = new List<BattleDeckCardsInjectedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<BattleDeckCardsInjectedEvent>(injectedEvents.Add);

        TableNine.Interface.SendCommand(new KillMonsterCommand(elite.Uid));

        unReg.UnRegister();

        Assert.That(deckModel.BattleDrawPile.Count, Is.EqualTo(battleCountBefore + 3),
            "击杀精英应向战斗牌堆注入 3 张帮助卡");
        Assert.That(injectedEvents.Count, Is.EqualTo(1));
        Assert.That(injectedEvents[0].CardIds, Contains.Item(GameConfigIds.HelpBlueChestId));
        Assert.That(injectedEvents[0].CardIds, Contains.Item(GameConfigIds.HelpGoldCardId));
        Assert.That(injectedEvents[0].CardIds, Contains.Item(GameConfigIds.HelpAttributeUpId));
        Assert.That(deckModel.PendingTutorSkillChoice, Is.True, "击杀精英应标记待处理导师技能");
    }

    [Test]
    public void KillBoss_Injects_Reward_Cards_Into_BattleDeck()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        var bossDef = configModel.GetCardDefinition(GameConfigIds.MonsterSpadeBossId);
        var boss = collectionModel.CreateCard(bossDef);
        var battleCountBefore = deckModel.BattleDrawPile.Count;

        TableNine.Interface.SendCommand(new KillMonsterCommand(boss.Uid));

        Assert.That(deckModel.BattleDrawPile.Count, Is.EqualTo(battleCountBefore + 4),
            "击杀层主应向战斗牌堆注入 4 张帮助卡");

        var goldCardCount = 0;
        foreach (var uid in deckModel.BattleDrawPile)
        {
            var runtime = collectionModel.GetCard(uid);
            if (runtime.DefinitionId == GameConfigIds.HelpGoldCardId)
            {
                goldCardCount++;
            }
        }

        Assert.That(goldCardCount, Is.GreaterThanOrEqualTo(2), "层主奖励应包含至少 2 张金币卡");
    }

    [Test]
    public void Refill_After_EliteKill_Opens_TutorSkillChoice()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        deckModel.PendingTutorSkillChoice = true;

        var tutorEvents = new List<TutorSkillChoiceRequestedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<TutorSkillChoiceRequestedEvent>(tutorEvents.Add);

        TableNine.Interface.SendCommand(new RefillBoardCommand());

        unReg.UnRegister();

        Assert.That(tutorEvents.Count, Is.EqualTo(1), "补牌结束后应弹出导师技能选择");
        Assert.That(tutorEvents[0].SkillIds.Count, Is.EqualTo(3));
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.TutorSkillChoosing));
    }

    [Test]
    public void GenerateTutorSkillCandidates_Excludes_Owned_Skills()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();

        for (var i = 0; i < GameConfigIds.TutorSkillPoolIds.Length; i++)
        {
            var skillId = GameConfigIds.TutorSkillPoolIds[i];
            var alreadyOwned = false;
            for (var j = 0; j < playerModel.SkillIds.Count; j++)
            {
                if (playerModel.SkillIds[j] == skillId)
                {
                    alreadyOwned = true;
                    break;
                }
            }

            if (!alreadyOwned)
            {
                playerModel.AddSkill(skillId);
            }
        }

        rewardSystem.GenerateTutorSkillCandidates();
        Assert.That(rewardModel.TutorSkillIds.Count, Is.EqualTo(0),
            "全部导师技能已拥有时不应再生成候选");
    }

    [Test]
    public void ProceedToNextNode_Layer3_Node9_Enters_Victory()
    {
        StartRun(42);
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        runModel.SetNode(3, 9);
        TableNine.Interface.SendCommand(new ProceedToNextNodeCommand());

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.Victory));
    }

    [Test]
    public void ConfigValidator_Passes_For_Default_Config()
    {
        var errors = ConfigValidator.Validate(TableNineTestConfig.LoadProductionRuntimeBundle());
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
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
