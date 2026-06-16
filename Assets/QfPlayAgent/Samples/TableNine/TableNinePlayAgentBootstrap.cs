using UnityEditor;

namespace QfPlayAgent.Samples.TableNine
{
    [InitializeOnLoad]
    public static class TableNinePlayAgentBootstrap
    {
        static TableNinePlayAgentBootstrap()
        {
            PlayAgentBootstrap.RegisterArchitectureProvider(new TableNinePlayAgentArchitectureProvider());
            PlayAgentBootstrap.RegisterSnapshotProvider(new TableNinePlayAgentSnapshotProvider());
            PlayAgentBootstrap.RegisterCatalog(TableNinePlayAgentCatalogFactory.CreateDefault());
        }
    }
}
