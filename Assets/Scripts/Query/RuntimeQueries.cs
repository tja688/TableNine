using QFramework;

public sealed class GetEffectivePlayerStatsQuery : AbstractQuery<EffectiveStats>
{
    protected override EffectiveStats OnDo()
    {
        return this.GetSystem<IStatSystem>().GetEffectivePlayerStats();
    }
}

public sealed class GetEffectiveMonsterStatsQuery : AbstractQuery<EffectiveStats>
{
    public GetEffectiveMonsterStatsQuery(CardUid monsterUid)
    {
        MonsterUid = monsterUid;
    }

    public CardUid MonsterUid { get; }

    protected override EffectiveStats OnDo()
    {
        return this.GetSystem<IStatSystem>().GetEffectiveMonsterStats(MonsterUid);
    }
}

public sealed class CanInteractBoardSlotQuery : AbstractQuery<InteractionResult>
{
    public CanInteractBoardSlotQuery(BoardSlotNo slot)
    {
        Slot = slot;
    }

    public BoardSlotNo Slot { get; }

    protected override InteractionResult OnDo()
    {
        var flowModel = this.GetModel<IFlowModel>();
        if (flowModel.IsInputLocked)
        {
            return new InteractionResult(false, InteractionKind.None, default, "Input locked");
        }

        var boardSystem = this.GetSystem<IBoardSystem>();
        if (!boardSystem.IsOrthogonalAdjacentToPlayer(Slot))
        {
            return new InteractionResult(false, InteractionKind.None, default, "Slot is not orthogonally adjacent");
        }

        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var uid = boardModel.GetCardAt(Slot);
        if (!uid.HasValue)
        {
            return new InteractionResult(true, InteractionKind.EmptySlot, default, string.Empty);
        }

        var runtime = collectionModel.GetCard(uid.Value);
        if (runtime.CardType == CardType.Help)
        {
            return new InteractionResult(true, InteractionKind.HelpCard, uid.Value, string.Empty);
        }

        if (runtime.CardType == CardType.Monster)
        {
            return new InteractionResult(true, InteractionKind.MonsterCard, uid.Value, string.Empty);
        }

        return new InteractionResult(false, InteractionKind.None, uid.Value, "Card is not interactable");
    }
}

public sealed class GetBoardSnapshotHashQuery : AbstractQuery<string>
{
    protected override string OnDo()
    {
        return RunSnapshotHashUtility.ComputeBoardHash(this.GetArchitecture());
    }
}

public sealed class GetRunSnapshotHashQuery : AbstractQuery<string>
{
    protected override string OnDo()
    {
        return RunSnapshotHashUtility.ComputeRunHash(this.GetArchitecture());
    }
}

public sealed class GetMonsterRemainingDetailQuery : AbstractQuery<string>
{
    protected override string OnDo()
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var deckSystem = this.GetSystem<IDeckSystem>();

        var builder = new System.Text.StringBuilder(256);
        builder.Append("board:");
        for (var slot = 1; slot <= 9; slot++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(slot));
            if (!uid.HasValue)
            {
                continue;
            }

            if (collectionModel.TryGetCard(uid.Value, out var runtime) && runtime.CardType == CardType.Monster)
            {
                builder.Append(slot).Append('=').Append(runtime.DefinitionId).Append(',');
            }
        }

        builder.Append(" pile:").Append(deckModel.BattleDrawPile.Count);
        builder.Append(" remaining=").Append(deckSystem.HasMonsterRemaining());
        return builder.ToString();
    }
}
