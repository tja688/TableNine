using System.IO;
using System.Linq;
using NUnit.Framework;
using QFramework;
using UnityEngine;

/// <summary>
/// R7：存档、重放、状态 Hash、Debug 支撑能力。
/// </summary>
[Category(TableNineTestCategories.NewRuleTests)]
public sealed class TableNineR7SaveReplayEditModeTests
{
    private string mFallbackSaveRoot;

    [SetUp]
    public void SetUp()
    {
        TableNine.ResetForTests();
        mFallbackSaveRoot = Path.Combine(Application.persistentDataPath, "table_nine_save");
        if (Directory.Exists(mFallbackSaveRoot))
        {
            Directory.Delete(mFallbackSaveRoot, true);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(mFallbackSaveRoot))
        {
            Directory.Delete(mFallbackSaveRoot, true);
        }

        TableNine.ResetForTests();
    }

    [Test]
    public void RuntimePersistence_Uses_EasySaveUtility_When_Configured()
    {
        TableNine.ConfigureRuntimePersistence();
        TableNine.InitArchitecture();
        Assert.That(TableNine.Interface.GetUtility<ISaveUtility>(), Is.TypeOf<EasySaveUtility>());
    }

    [Test]
    public void EasySave_Persists_Across_Architecture_Reset()
    {
        TableNine.ConfigureRuntimePersistence();
        TableNine.InitArchitecture();
        TableNine.Interface.GetUtility<ISaveUtility>().SaveString("r7_probe", "persisted");

        TableNine.ResetForTests();
        TableNine.ConfigureRuntimePersistence();
        TableNine.InitArchitecture();

        Assert.That(TableNine.Interface.GetUtility<ISaveUtility>().TryLoadString("r7_probe", out var value), Is.True);
        Assert.That(value, Is.EqualTo("persisted"));
    }

    [Test]
    public void SaveLoad_Roundtrip_Preserves_Run_Hash()
    {
        StartRun(42);
        DamagePlayerAndBoard();

        var hashBefore = TableNine.Interface.SendQuery(new GetRunSnapshotHashQuery());
        var save = TableNine.Interface.GetSystem<ISaveSystem>().CaptureCurrentRun(SaveRunReason.Manual);

        TableNine.ResetForTests();
        TableNine.InitArchitecture();
        TableNine.Interface.GetSystem<ISaveSystem>().ApplySaveData(save);

        var hashAfter = TableNine.Interface.SendQuery(new GetRunSnapshotHashQuery());
        Assert.That(hashAfter, Is.EqualTo(hashBefore));
    }

    [Test]
    public void SaveLoad_Roundtrip_Preserves_Board_Hash()
    {
        StartRun(77);
        DamagePlayerAndBoard();

        var hashBefore = TableNine.Interface.SendQuery(new GetBoardSnapshotHashQuery());
        var save = TableNine.Interface.GetSystem<ISaveSystem>().CaptureCurrentRun(SaveRunReason.Manual);

        TableNine.ResetForTests();
        TableNine.InitArchitecture();
        TableNine.Interface.GetSystem<ISaveSystem>().ApplySaveData(save);

        Assert.That(TableNine.Interface.SendQuery(new GetBoardSnapshotHashQuery()), Is.EqualTo(hashBefore));
    }

    [Test]
    public void CommandReplay_SameSeed_And_Commands_Preserves_Run_Hash()
    {
        StartRun(99);
        TableNine.Interface.SendCommand(new DebugSpawnHelpCardCommand(GameConfigIds.HelpPotionId));
        var expectedHash = TableNine.Interface.SendQuery(new GetRunSnapshotHashQuery());
        var replayData = TableNine.Interface.GetUtility<ICommandReplayUtility>().Export();

        TableNine.ResetForTests();
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new ReplayRunCommand(replayData, expectedHash));

        Assert.That(TableNine.Interface.SendQuery(new GetRunSnapshotHashQuery()), Is.EqualTo(expectedHash));
    }

    [Test]
    public void SaveData_SchemaVersion_Is_Current()
    {
        StartRun(11);
        var save = TableNine.Interface.GetSystem<ISaveSystem>().CaptureCurrentRun(SaveRunReason.Manual);
        Assert.That(save.SchemaVersion, Is.EqualTo(RunSaveData.CurrentSchemaVersion));
        Assert.That(save.RandomOperationIndex, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void BugReportBuilder_Includes_Seed_And_Hash()
    {
        StartRun(55);
        var report = BugReportBuilder.Build(TableNine.Interface);
        Assert.That(report, Does.Contain("Seed: 55"));
        Assert.That(report, Does.Contain("RunHash:"));
        Assert.That(report, Does.Contain("BoardHash:"));
    }

    [Test]
    public void Load_In_Overlay_Phase_Restores_Input_Locks()
    {
        StartRun(42);
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        flowModel.SetPhase(FlowPhase.HelpRewardChoosing);
        flowModel.AddLock(InputLockReason.OverlayVisible);

        var save = TableNine.Interface.GetSystem<ISaveSystem>().CaptureCurrentRun(SaveRunReason.Manual);
        TableNine.ResetForTests();
        TableNine.InitArchitecture();
        TableNine.Interface.GetSystem<ISaveSystem>().ApplySaveData(save);

        flowModel = TableNine.Interface.GetModel<IFlowModel>();
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.HelpRewardChoosing));
        Assert.That(flowModel.HasLock(InputLockReason.OverlayVisible), Is.True);
        Assert.That(flowModel.IsInputLocked, Is.True);
    }

    [Test]
    public void Replay_Does_Not_Record_Internal_Flow_Commands()
    {
        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        TableNine.Interface.SendCommand(new ChooseRoomCommand(GameConfigIds.RoomGoldId));

        var entries = TableNine.Interface.GetUtility<ICommandReplayUtility>().Entries;
        Assert.That(entries.Any(entry => entry.CommandType == nameof(ProceedToNextNodeCommand)), Is.False);
        Assert.That(entries.Any(entry => entry.CommandType == nameof(EnterRoomChoosingCommand)), Is.False);
    }

    [Test]
    public void ChooseRoom_Replay_Does_Not_Double_Advance_Node()
    {
        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        var replayUtility = TableNine.Interface.GetUtility<ICommandReplayUtility>();
        replayUtility.Clear();

        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var nodeBefore = runModel.NodeInLayer.Value;
        TableNine.Interface.SendCommand(new ChooseRoomCommand(GameConfigIds.RoomGoldId));

        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(nodeBefore + 1));
        Assert.That(replayUtility.Entries.Count, Is.EqualTo(1));
        Assert.That(replayUtility.Entries[0].CommandType, Is.EqualTo(nameof(ChooseRoomCommand)));

        runModel.SetNode(runModel.Layer.Value, nodeBefore);
        TableNine.Interface.GetModel<IFlowModel>().SetPhase(FlowPhase.RoomChoosing);
        TableNine.Interface.SendCommand(CommandReplayFactory.Create(replayUtility.Entries[0]));

        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(nodeBefore + 1));
    }

    [Test]
    public void ChestRoom_Replay_Does_Not_Double_Advance_Node()
    {
        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        TableNine.Interface.SendCommand(new ChooseRoomCommand(GameConfigIds.RoomChestId));

        var replayUtility = TableNine.Interface.GetUtility<ICommandReplayUtility>();
        replayUtility.Clear();

        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var nodeBefore = runModel.NodeInLayer.Value;
        TableNine.Interface.SendCommand(new SkipChestRewardCommand());

        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(nodeBefore + 1));
        Assert.That(replayUtility.Entries.Count, Is.EqualTo(1));
        Assert.That(replayUtility.Entries[0].CommandType, Is.EqualTo(nameof(SkipChestRewardCommand)));
        Assert.That(replayUtility.Entries.Any(entry => entry.CommandType == nameof(ProceedToNextNodeCommand)), Is.False);

        runModel.SetNode(runModel.Layer.Value, nodeBefore);
        TableNine.Interface.GetModel<IFlowModel>().SetPhase(FlowPhase.ChestRewardChoosing);
        TableNine.Interface.GetModel<IRewardModel>().CurrentRewardSource = RewardSource.Room;
        TableNine.Interface.SendCommand(CommandReplayFactory.Create(replayUtility.Entries[0]));

        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(nodeBefore + 1));
    }

    [Test]
    public void Load_In_HelpReward_Phase_Restores_Overlay_UI()
    {
        StartRunAndClearNode();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        Assert.That(rewardModel.HelpRewardCardIds.Count, Is.GreaterThan(0));

        var save = TableNine.Interface.GetSystem<ISaveSystem>().CaptureCurrentRun(SaveRunReason.Manual);
        TableNine.ResetForTests();
        TableNine.InitArchitecture();

        TableNineUIRuntimeTestRecorder.Reset();
        TableNineUIRuntimeTestRecorder.SkipActualPanelOpen = true;
        var registry = ScriptableObject.CreateInstance<TableNineUIPanelRegistry>();
        var router = new TableNineUIRouter(registry);
        router.Start();
        TableNine.Interface.GetSystem<ISaveSystem>().ApplySaveData(save);

        Assert.That(TableNineUIRuntimeTestRecorder.OpenedKeys, Does.Contain(TableNineUIKeys.HelpReward));

        router.Dispose();
        Object.DestroyImmediate(registry);
        TableNineUIRuntimeTestRecorder.Reset();
    }

    [Test]
    public void Load_In_RoomChoosing_Phase_Restores_Overlay_UI()
    {
        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        Assert.That(TableNine.Interface.GetModel<IRewardModel>().RoomCandidateIds.Count, Is.GreaterThan(0));

        var save = TableNine.Interface.GetSystem<ISaveSystem>().CaptureCurrentRun(SaveRunReason.Manual);
        TableNine.ResetForTests();
        TableNine.InitArchitecture();

        TableNineUIRuntimeTestRecorder.Reset();
        TableNineUIRuntimeTestRecorder.SkipActualPanelOpen = true;
        var registry = ScriptableObject.CreateInstance<TableNineUIPanelRegistry>();
        var router = new TableNineUIRouter(registry);
        router.Start();
        TableNine.Interface.GetSystem<ISaveSystem>().ApplySaveData(save);

        Assert.That(TableNineUIRuntimeTestRecorder.OpenedKeys, Does.Contain(TableNineUIKeys.RoomChoice));

        router.Dispose();
        Object.DestroyImmediate(registry);
        TableNineUIRuntimeTestRecorder.Reset();
    }

    [Test]
    public void RunHash_Distinguishes_ItemSlot_Positions()
    {
        StartRun(42);
        TableNine.Interface.SendCommand(new DebugSpawnHelpCardCommand(GameConfigIds.HelpPotionId));
        TableNine.Interface.SendCommand(new DebugSpawnHelpCardCommand(GameConfigIds.HelpThrowingKnifeId));

        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var firstUid = deckModel.OwnedHelpCards[deckModel.OwnedHelpCards.Count - 2];
        var secondUid = deckModel.OwnedHelpCards[deckModel.OwnedHelpCards.Count - 1];
        TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(firstUid));
        TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(secondUid));

        var hashSlot01 = TableNine.Interface.SendQuery(new GetRunSnapshotHashQuery());

        deckModel.ItemSlots[0] = secondUid;
        deckModel.ItemSlots[1] = firstUid;
        collectionModel.GetCard(firstUid).ItemSlotIndex = 1;
        collectionModel.GetCard(secondUid).ItemSlotIndex = 0;

        var hashSwapped = TableNine.Interface.SendQuery(new GetRunSnapshotHashQuery());
        Assert.That(hashSwapped, Is.Not.EqualTo(hashSlot01));
    }

    [Test]
    public void UIDebugPanel_Save_Load_Actions_Work()
    {
        StartRun(42);
        DamagePlayerAndBoard();

        var panelObject = new GameObject("UIDebugPanelTest");
        var panel = panelObject.AddComponent<UIDebugPanel>();
        panel.OnClickSave();

        var hashBefore = TableNine.Interface.SendQuery(new GetRunSnapshotHashQuery());
        TableNine.Interface.GetModel<IPlayerModel>().Gold.Value = 999;
        Assert.That(TableNine.Interface.SendQuery(new GetRunSnapshotHashQuery()), Is.Not.EqualTo(hashBefore));

        panel.OnClickLoad();
        Assert.That(TableNine.Interface.SendQuery(new GetRunSnapshotHashQuery()), Is.EqualTo(hashBefore));

        var report = panel.GetArchitecture().SendCommand(new CopyBugReportCommand());
        Assert.That(report, Does.Contain("Seed: 42"));

        Object.DestroyImmediate(panelObject);
    }

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static void DamagePlayerAndBoard()
    {
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        runModel.SetNode(2, 3);
        flowModel.SetPhase(FlowPhase.PlayerControl);
        playerModel.Gold.Value = 88;

        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentHp -= 2;
        player.CurrentArmor = 3;
    }

    private static void StartRunAndClearNode()
    {
        StartRun(42);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        deckModel.BattleDrawPile.Clear();
        TableNine.Interface.GetSystem<IDeckSystem>().UpdateNextBattlePreview();

        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue || uid.Value.Equals(playerModel.PlayerCardUid))
            {
                continue;
            }

            var runtime = collectionModel.GetCard(uid.Value);
            if (runtime.CardType == CardType.Monster)
            {
                boardSystem.RemoveCardAt(slot);
                collectionModel.RemoveCard(uid.Value);
            }
        }

        TableNine.Interface.SendCommand(new CheckClearConditionCommand());
    }

    private static TableNineUIRouter CreateUIRouterForTests(out TableNineUIPanelRegistry registry)
    {
        registry = ScriptableObject.CreateInstance<TableNineUIPanelRegistry>();
        TableNineUIRuntimeTestRecorder.SkipActualPanelOpen = true;
        var router = new TableNineUIRouter(registry);
        router.Start();
        return router;
    }
}
