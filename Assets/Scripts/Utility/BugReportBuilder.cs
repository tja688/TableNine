using System.Text;
using QFramework;

public static class BugReportBuilder
{
    public static string Build(IArchitecture architecture)
    {
        var runModel = architecture.GetModel<IRunModel>();
        var flowModel = architecture.GetModel<IFlowModel>();
        var playerModel = architecture.GetModel<IPlayerModel>();
        var collectionModel = architecture.GetModel<ICollectionModel>();
        var deckModel = architecture.GetModel<IDeckModel>();
        var replayUtility = architecture.GetUtility<ICommandReplayUtility>();
        var saveSystem = architecture.GetSystem<ISaveSystem>();
        var stats = architecture.GetSystem<IStatSystem>().GetEffectivePlayerStats();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);

        var builder = new StringBuilder(4096);
        builder.AppendLine("=== TableNine Bug Report ===");
        builder.AppendLine($"Seed: {runModel.Seed.Value}");
        builder.AppendLine($"Layer/Node: {runModel.Layer.Value}/{runModel.NodeInLayer.Value}");
        builder.AppendLine($"Phase: {flowModel.Phase.Value}");
        builder.AppendLine($"BoardHash: {RunSnapshotHashUtility.ComputeBoardHash(architecture)}");
        builder.AppendLine($"RunHash: {RunSnapshotHashUtility.ComputeRunHash(architecture)}");
        builder.AppendLine($"Player: hp={player.CurrentHp}/{player.MaxHp} armor={player.CurrentArmor} gold={playerModel.Gold.Value} dr={stats.DamageReduction}");
        builder.AppendLine($"BattleDeckCount: {deckModel.BattleDrawPile.Count}");
        builder.AppendLine($"HelpDeckActive: {deckModel.CountActiveHelpCards()}/{deckModel.OwnedHelpCards.Count}");
        builder.AppendLine();

        var save = saveSystem.CaptureCurrentRun(SaveRunReason.Manual);
        builder.AppendLine($"SaveSummary: schema={save.SchemaVersion} cards={save.Cards.Count} help={save.HelpStates.Count} battle={save.BattleDeckUids.Count}");
        builder.AppendLine();

        builder.AppendLine("--- Commands ---");
        var entries = replayUtility.Entries;
        var start = entries.Count > 50 ? entries.Count - 50 : 0;
        for (var i = start; i < entries.Count; i++)
        {
            var entry = entries[i];
            builder.AppendLine($"{entry.CommandType} | {entry.PayloadJson}");
        }

        return builder.ToString();
    }
}
