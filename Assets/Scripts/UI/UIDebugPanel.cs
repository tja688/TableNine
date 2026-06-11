using System.Text;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开发构建 Debug 面板：状态总览、存档/读档/重放与 QA 快捷操作。
/// </summary>
public sealed class UIDebugPanel : MonoBehaviour, IController
{
    [SerializeField] private KeyCode mToggleKey = KeyCode.F3;
    [SerializeField] private Text mInfoText;
    [SerializeField] private GameObject mRoot;

    private float mRefreshTimer;

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
        var replayUtility = this.GetUtility<ICommandReplayUtility>();
        this.SendCommand(new ReplayRunCommand(replayUtility.Export()));
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
        var trace = this.GetUtility<ICommandTraceUtility>();
        var replay = this.GetUtility<ICommandReplayUtility>();
        var eventLog = this.GetUtility<IDebugEventLogUtility>();

        var builder = new StringBuilder(2048);
        builder.AppendLine($"Seed: {runModel.Seed.Value}  Layer/Node: {runModel.Layer.Value}/{runModel.NodeInLayer.Value}");
        builder.AppendLine($"Phase: {flowModel.Phase.Value}  Locks: {FormatLocks(flowModel)}");
        builder.AppendLine($"Command#: {trace.Records.Count}  ReplayEntries: {replay.Entries.Count}");
        builder.AppendLine($"BattleDeck: {deckModel.BattleDrawPile.Count}  Next: {deckModel.NextBattleCardPreview.Value.DisplayName}");
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
            builder.AppendLine($"  [{slot}] {card.DefinitionId} hp={card.CurrentHp}/{card.MaxHp} atk={card.BaseAttack} def={card.BaseDefense}");
        }

        builder.AppendLine($"HelpDeck: {deckModel.OwnedHelpCards.Count} active={deckModel.CountActiveHelpCards()}");
        builder.AppendLine("Recent Events:");
        var events = eventLog.RecentEvents;
        for (var i = Mathf.Max(0, events.Count - 8); i < events.Count; i++)
        {
            builder.AppendLine(events[i]);
        }

        mInfoText.text = builder.ToString();
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

