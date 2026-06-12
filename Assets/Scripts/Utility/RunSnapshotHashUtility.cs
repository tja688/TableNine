using System.Text;
using QFramework;

public static class RunSnapshotHashUtility
{
    public static string ComputeBoardHash(IArchitecture architecture)
    {
        var builder = new StringBuilder(512);
        AppendBoard(builder, architecture);
        return Fnv1a64(builder.ToString());
    }

    public static string ComputeRunHash(IArchitecture architecture)
    {
        var builder = new StringBuilder(2048);
        AppendBoard(builder, architecture);
        AppendPlayer(builder, architecture);
        AppendBattleDeck(builder, architecture);
        AppendHelpDeck(builder, architecture);
        AppendFlow(builder, architecture);
        return Fnv1a64(builder.ToString());
    }

    private static void AppendBoard(StringBuilder builder, IArchitecture architecture)
    {
        var boardModel = architecture.GetModel<IBoardModel>();
        var collectionModel = architecture.GetModel<ICollectionModel>();

        for (var slot = 1; slot <= 9; slot++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(slot));
            if (!uid.HasValue)
            {
                builder.Append(slot).Append(":empty|");
                continue;
            }

            var card = collectionModel.GetCard(uid.Value);
            builder.Append(slot)
                .Append(':').Append(card.Uid.Value)
                .Append('/').Append(card.DefinitionId)
                .Append('/').Append(card.CurrentHp)
                .Append('/').Append(card.CurrentArmor)
                .Append('|');
        }
    }

    private static void AppendPlayer(StringBuilder builder, IArchitecture architecture)
    {
        var playerModel = architecture.GetModel<IPlayerModel>();
        var collectionModel = architecture.GetModel<ICollectionModel>();
        var stats = architecture.GetSystem<IStatSystem>().GetEffectivePlayerStats();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        builder.Append("player:")
            .Append(player.CurrentHp).Append('/')
            .Append(player.CurrentArmor).Append('/')
            .Append(playerModel.Gold.Value).Append('/')
            .Append(stats.DamageReduction)
            .Append('|');
    }

    private static void AppendBattleDeck(StringBuilder builder, IArchitecture architecture)
    {
        var deckModel = architecture.GetModel<IDeckModel>();
        var collectionModel = architecture.GetModel<ICollectionModel>();
        builder.Append("battle:");
        foreach (var uid in deckModel.BattleDrawPile)
        {
            var card = collectionModel.GetCard(uid);
            builder.Append(uid.Value).Append('/').Append(card.DefinitionId).Append(',');
        }

        builder.Append('|');
    }

    private static void AppendHelpDeck(StringBuilder builder, IArchitecture architecture)
    {
        var deckModel = architecture.GetModel<IDeckModel>();
        var collectionModel = architecture.GetModel<ICollectionModel>();
        var configModel = architecture.GetModel<IConfigModel>();
        builder.Append("help:");
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

            var itemSlotIndex = -1;
            if (collectionModel.TryGetCard(uid, out var runtime) && runtime.ItemSlotIndex.HasValue)
            {
                itemSlotIndex = runtime.ItemSlotIndex.Value;
            }

            builder.Append(uid.Value)
                .Append('/').Append(state.DefinitionId)
                .Append('/').Append(restoreAfterNode ? 'T' : 'F')
                .Append('/').Append(state.IsTemporarilyRemoved ? 'T' : 'F')
                .Append('/').Append(state.IsPermanentlyRemoved ? 'T' : 'F')
                .Append('/').Append(state.IsOnBoard ? 'T' : 'F')
                .Append('/').Append(state.IsInItemSlot ? 'T' : 'F')
                .Append('/').Append(itemSlotIndex)
                .Append(',');
        }

        builder.Append('|');
    }

    private static void AppendFlow(StringBuilder builder, IArchitecture architecture)
    {
        var flowModel = architecture.GetModel<IFlowModel>();
        builder.Append("flow:").Append(flowModel.Phase.Value).Append('/');
        foreach (var reason in flowModel.ActiveLocks)
        {
            builder.Append(reason).Append(',');
        }

        builder.Append('|');
    }

    private static string Fnv1a64(string canonical)
    {
        unchecked
        {
            ulong hash = 14695981039346656037UL;
            for (var i = 0; i < canonical.Length; i++)
            {
                hash ^= canonical[i];
                hash *= 1099511628211UL;
            }

            return hash.ToString("x16");
        }
    }
}
