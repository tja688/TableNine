using System.Collections.Generic;
using System.Text;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开发构建 Debug 面板：状态总览、存档/读档/重放与 QA 快捷操作。
/// </summary>
public sealed class UIDebugPanel : MonoBehaviour, IController, ICanSendEvent
{
    [SerializeField] private KeyCode mToggleKey = KeyCode.F3;
    [SerializeField] private Text mInfoText;
    [SerializeField] private GameObject mRoot;

    private float mRefreshTimer;
    private string mLastReplayHash;
    private bool mLastReplayHashMatched = true;

    private void Awake()
    {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        gameObject.SetActive(false);
        return;
#else
        if (mRoot != null)
        {
            mRoot.SetActive(false);
        }
#endif
    }

    private void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        this.RegisterEvent<RunReplayCompletedEvent>(OnReplayCompleted)
            .UnRegisterWhenDisabled(gameObject);
#endif
    }

    private void Update()
    {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        return;
#else
        if (!TableNine.IsInitialized)
        {
            return;
        }

        if (Input.GetKeyDown(mToggleKey) && mRoot != null)
        {
            mRoot.SetActive(!mRoot.activeSelf);
        }

        if (mRoot == null || !mRoot.activeSelf)
        {
            return;
        }

        mRefreshTimer -= Time.deltaTime;
        if (mRefreshTimer <= 0f)
        {
            mRefreshTimer = 0.25f;
            Refresh();
        }
#endif
    }

    public void OnClickSave()
    {
        this.SendCommand(new SaveRunCommand(SaveRunReason.Manual));
    }

    public void OnClickLoad()
    {
        this.SendCommand(new LoadRunCommand());
    }

    public void OnClickReplay()
    {
        var expectedHash = this.SendQuery(new GetRunSnapshotHashQuery());
        var replayUtility = this.GetUtility<ICommandReplayUtility>();
        this.SendCommand(new ReplayRunCommand(replayUtility.Export(), expectedHash));
    }

    public void OnClickCopyBugReport()
    {
        var report = this.SendCommand(new CopyBugReportCommand());
        GUIUtility.systemCopyBuffer = report;
        this.SendEvent(new PopupRequestedEvent("Bug report 已复制到剪贴板。"));
    }

    public void OnClickKillAllMonsters()
    {
        this.SendCommand(new DebugKillAllMonstersCommand());
    }

    public void OnClickAddPotion()
    {
        this.SendCommand(new DebugSpawnHelpCardCommand(DefaultGameConfigFactory.HelpPotionId));
    }

    public void OnClickAddPhoenixFeather()
    {
        this.SendCommand(new DebugAddRelicCommand(DefaultGameConfigFactory.RelicPhoenixFeatherId));
    }

    public void OnClickProceedNode()
    {
        this.SendCommand(new ProceedToNextNodeCommand());
    }

    private void OnReplayCompleted(RunReplayCompletedEvent e)
    {
        mLastReplayHash = e.RunHash;
        mLastReplayHashMatched = e.HashMatched;
    }

    private void Refresh()
    {
        if (mInfoText == null)
        {
            return;
        }

        var runModel = this.GetModel<IRunModel>();
        var flowModel = this.GetModel<IFlowModel>();
        var boardModel = this.GetModel<IBoardModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        var replay = this.GetUtility<ICommandReplayUtility>();
        var eventLog = this.GetUtility<IDebugEventLogUtility>();
        var stats = this.SendQuery(new GetEffectivePlayerStatsQuery());
        var player = collectionModel.GetCard(this.GetModel<IPlayerModel>().PlayerCardUid);
        var boardHash = this.SendQuery(new GetBoardSnapshotHashQuery());
        var runHash = this.SendQuery(new GetRunSnapshotHashQuery());
        var monsterDetail = this.SendQuery(new GetMonsterRemainingDetailQuery());

        var builder = new StringBuilder(4096);
        builder.AppendLine($"Seed: {runModel.Seed.Value}  Layer/Node: {runModel.Layer.Value}/{runModel.NodeInLayer.Value}");
        builder.AppendLine($"Phase: {flowModel.Phase.Value}  Locks: {FormatLocks(flowModel)}");
        builder.AppendLine($"BoardHash: {boardHash}  RunHash: {runHash}");
        builder.AppendLine($"Player: hp={player.CurrentHp}/{player.MaxHp} armor={player.CurrentArmor} atk={stats.Attack} def={stats.Defense} dr={stats.DamageReduction} gold={this.GetModel<IPlayerModel>().Gold.Value}");
        builder.AppendLine($"BattleDeck: {deckModel.BattleDrawPile.Count}  Next: {deckModel.NextBattleCardPreview.Value.DisplayName}");
        builder.AppendLine($"MonsterCheck: {monsterDetail}");
        builder.AppendLine($"Replay: entries={replay.Entries.Count} lastHash={mLastReplayHash} matched={mLastReplayHashMatched}");
        builder.AppendLine("Board:");

        for (var slot = 1; slot <= 9; slot++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(slot));
            if (!uid.HasValue)
            {
                builder.AppendLine($"  [{slot}] empty");
                continue;
            }

            var card = collectionModel.GetCard(uid.Value);
            builder.AppendLine($"  [{slot}] {card.DefinitionId} uid={card.Uid.Value} hp={card.CurrentHp}/{card.MaxHp} armor={card.CurrentArmor} atk={card.BaseAttack} def={card.BaseDefense}");
        }

        builder.AppendLine("HelpDeck:");
        var tempRemoved = new StringBuilder();
        var permRemoved = new StringBuilder();
        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (!deckModel.HelpCardStates.TryGetValue(uid.Value, out var state))
            {
                continue;
            }

            var restoreAfterNode = false;
            if (configModel.TryGetCardDefinition(state.DefinitionId, out var definition))
            {
                restoreAfterNode = definition.RestoreAfterNode;
            }

            builder.AppendLine($"  uid={uid.Value} {state.DefinitionId} restore={restoreAfterNode} board={state.IsOnBoard} item={state.IsInItemSlot}");
            if (state.IsTemporarilyRemoved)
            {
                tempRemoved.Append(state.DefinitionId).Append(',');
            }

            if (state.IsPermanentlyRemoved)
            {
                permRemoved.Append(state.DefinitionId).Append(',');
            }
        }

        builder.AppendLine($"TempRemoved: {(tempRemoved.Length > 0 ? tempRemoved.ToString() : "none")}");
        builder.AppendLine($"PermRemoved: {(permRemoved.Length > 0 ? permRemoved.ToString() : "none")}");
        builder.AppendLine("Recent Commands:");
        AppendRecentLines(builder, replay.Entries, entry => $"{entry.CommandType} | {entry.PayloadJson}");
        builder.AppendLine("Recent Events:");
        AppendRecentLines(builder, eventLog.RecentEvents, line => line);

        mInfoText.text = builder.ToString();
    }

    private static void AppendRecentLines<T>(StringBuilder builder, IReadOnlyList<T> items, System.Func<T, string> formatter)
    {
        var start = items.Count > 50 ? items.Count - 50 : 0;
        for (var i = start; i < items.Count; i++)
        {
            builder.AppendLine(formatter(items[i]));
        }
    }

    private static string FormatLocks(IFlowModel flowModel)
    {
        var locks = flowModel.ActiveLocks;
        if (locks == null || locks.Count == 0)
        {
            return "none";
        }

        var builder = new StringBuilder();
        foreach (var reason in locks)
        {
            if (builder.Length > 0)
            {
                builder.Append(',');
            }

            builder.Append(reason);
        }

        return builder.ToString();
    }

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }
}
