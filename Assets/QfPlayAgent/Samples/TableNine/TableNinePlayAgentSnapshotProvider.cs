using QFramework;

namespace QfPlayAgent.Samples.TableNine
{
    public sealed class TableNinePlayAgentSnapshotProvider : IPlayAgentSnapshotProvider
    {
        public object Capture(IArchitecture architecture)
        {
            var flowModel = architecture.GetModel<IFlowModel>();
            var runModel = architecture.GetModel<IRunModel>();
            var playerModel = architecture.GetModel<IPlayerModel>();

            return new
            {
                flow_phase = flowModel.Phase.Value.ToString(),
                input_locked = flowModel.IsInputLocked,
                layer = runModel.Layer.Value,
                node_in_layer = runModel.NodeInLayer.Value,
                player_gold = playerModel.Gold.Value,
                character_id = runModel.CharacterId
            };
        }
    }
}
