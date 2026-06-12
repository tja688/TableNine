using QFramework;

public sealed class SaveRunCommand : AbstractCommand
{
    public SaveRunCommand(SaveRunReason reason, string slot = SaveSystem.DefaultSlot)
    {
        Reason = reason;
        Slot = slot;
    }

    public SaveRunReason Reason { get; }
    public string Slot { get; }

    protected override void OnExecute()
    {
        this.GetSystem<ISaveSystem>().SaveCurrentRun(Slot, Reason);
    }
}

public sealed class LoadRunCommand : AbstractCommand
{
    public LoadRunCommand(string slot = SaveSystem.DefaultSlot)
    {
        Slot = slot;
    }

    public string Slot { get; }

    protected override void OnExecute()
    {
        if (!this.GetSystem<ISaveSystem>().TryLoadRun(Slot))
        {
            this.SendEvent(new PopupRequestedEvent("没有找到可读取的存档。"));
        }
    }
}

public sealed class ClearSaveCommand : AbstractCommand
{
    public ClearSaveCommand(string slot = SaveSystem.DefaultSlot)
    {
        Slot = slot;
    }

    public string Slot { get; }

    protected override void OnExecute()
    {
        this.GetSystem<ISaveSystem>().DeleteSave(Slot);
    }
}

public sealed class ReplayRunCommand : AbstractCommand
{
    public ReplayRunCommand(RunReplayData replayData, string expectedRunHash = null)
    {
        ReplayData = replayData;
        ExpectedRunHash = expectedRunHash;
    }

    public RunReplayData ReplayData { get; }
    public string ExpectedRunHash { get; }

    protected override void OnExecute()
    {
        if (ReplayData == null)
        {
            return;
        }

        var replayUtility = this.GetUtility<ICommandReplayUtility>();
        replayUtility.Clear();
        this.SendCommand(new StartNewRunCommand(ReplayData.CharacterId, ReplayData.Seed));

        for (var i = 0; i < ReplayData.Entries.Count; i++)
        {
            var entry = ReplayData.Entries[i];
            if (entry.CommandType == nameof(StartNewRunCommand))
            {
                continue;
            }

            var command = CommandReplayFactory.Create(entry);
            if (command != null)
            {
                this.SendCommand(command);
            }
        }

        var actualHash = this.SendQuery(new GetRunSnapshotHashQuery());
        var hashMatched = string.IsNullOrEmpty(ExpectedRunHash) || ExpectedRunHash == actualHash;
        this.SendEvent(new RunReplayCompletedEvent(ReplayData.Entries.Count, actualHash, hashMatched));
    }
}

public sealed class CopyBugReportCommand : AbstractCommand<string>
{
    protected override string OnExecute()
    {
        return BugReportBuilder.Build(TableNine.Interface);
    }
}

public sealed class DebugKillAllMonstersCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var monsters = new System.Collections.Generic.List<CardUid>();

        for (var slot = 1; slot <= 9; slot++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(slot));
            if (!uid.HasValue)
            {
                continue;
            }

            if (collectionModel.TryGetCard(uid.Value, out var runtime) && runtime.CardType == CardType.Monster)
            {
                monsters.Add(uid.Value);
            }
        }

        for (var i = 0; i < monsters.Count; i++)
        {
            this.SendCommand(new KillMonsterCommand(monsters[i]));
        }

        this.SendCommand(new CheckClearConditionCommand());
    }
}

public sealed class DebugSpawnHelpCardCommand : AbstractCommand
{
    public DebugSpawnHelpCardCommand(string cardId)
    {
        CardId = cardId;
    }

    public string CardId { get; }

    protected override void OnExecute()
    {
        var configModel = this.GetModel<IConfigModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var rewardSystem = this.GetSystem<IRewardSystem>();

        if (!rewardSystem.CanAddHelpCard(CardId))
        {
            this.SendEvent(new PopupRequestedEvent("无法添加帮助卡（容量或同名上限）。"));
            return;
        }

        var definition = configModel.GetCardDefinition(CardId);
        var runtime = collectionModel.CreateCard(definition);
        deckModel.OwnedHelpCards.Add(runtime.Uid);
        deckModel.HelpCardStates[runtime.Uid.Value] = new HelpCardState
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId
        };
        this.SendEvent(new HelpCardPurchasedEvent(CardId, 0));
    }
}

public sealed class DebugAddRelicCommand : AbstractCommand
{
    public DebugAddRelicCommand(string relicId)
    {
        RelicId = relicId;
    }

    public string RelicId { get; }

    protected override void OnExecute()
    {
        this.GetSystem<IRelicSystem>().AddRelic(RelicId);
    }
}
