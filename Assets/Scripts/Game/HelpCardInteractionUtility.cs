using QFramework;

public enum HelpCardPlayIntentKind
{
    Immediate,
    Targeting,
    SwapTarget
}

public readonly struct HelpCardPlayIntent
{
    public HelpCardPlayIntent(HelpCardPlayIntentKind kind, string targetingMode)
    {
        Kind = kind;
        TargetingMode = string.IsNullOrEmpty(targetingMode) ? "damage" : targetingMode;
    }

    public HelpCardPlayIntentKind Kind { get; }
    public string TargetingMode { get; }
    public bool RequiresBoardTarget => Kind == HelpCardPlayIntentKind.Targeting || Kind == HelpCardPlayIntentKind.SwapTarget;
}

public static class HelpCardInteractionUtility
{
    public static HelpCardPlayIntent ResolveIntent(CardDefinition definition)
    {
        if (definition == null ||
            string.IsNullOrWhiteSpace(definition.EffectGraphId) ||
            !EffectGraphRegistry.TryGetGraph(definition.EffectGraphId, out var graph) ||
            graph?.Atoms == null)
        {
            return new HelpCardPlayIntent(HelpCardPlayIntentKind.Immediate, string.Empty);
        }

        for (var i = 0; i < graph.Atoms.Count; i++)
        {
            var atom = graph.Atoms[i];
            if (atom == null || atom.AtomType != EffectAtomTypes.OpenTargeting)
            {
                continue;
            }

            var mode = EffectAtomSerializationUtility.GetParameter(atom, "mode", "damage");
            return mode == "swap"
                ? new HelpCardPlayIntent(HelpCardPlayIntentKind.SwapTarget, mode)
                : new HelpCardPlayIntent(HelpCardPlayIntentKind.Targeting, mode);
        }

        return new HelpCardPlayIntent(HelpCardPlayIntentKind.Immediate, string.Empty);
    }

    public static bool IsValidBoardTarget(IController controller, HelpCardPlayIntent intent, BoardSlotNo slot)
    {
        if (controller == null || !intent.RequiresBoardTarget || !TableNine.IsInitialized)
        {
            return false;
        }

        var boardModel = controller.GetModel<IBoardModel>();
        var collectionModel = controller.GetModel<ICollectionModel>();
        var playerModel = controller.GetModel<IPlayerModel>();
        var uid = boardModel.GetCardAt(slot);
        if (!uid.HasValue ||
            uid.Value.Equals(playerModel.PlayerCardUid) ||
            !collectionModel.TryGetCard(uid.Value, out var runtime))
        {
            return false;
        }

        if (intent.Kind == HelpCardPlayIntentKind.SwapTarget)
        {
            return true;
        }

        switch (intent.TargetingMode)
        {
            case "kidnap":
                return runtime.CardType == CardType.Monster &&
                       runtime.MonsterLevel != MonsterLevel.Elite &&
                       runtime.MonsterLevel != MonsterLevel.Boss;
            case "damage":
            case "player_attack":
            case "player_current_hp":
            case "player_current_armor":
                return runtime.CardType == CardType.Monster;
            case "reduce_armor":
            case "teleport_to_deck":
                return runtime.CardType != CardType.Player;
            default:
                return runtime.CardType == CardType.Monster;
        }
    }
}
