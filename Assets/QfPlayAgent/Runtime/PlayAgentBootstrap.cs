using System.Collections.Generic;

namespace QfPlayAgent
{
    public static class PlayAgentBootstrap
    {
        private static IPlayAgentArchitectureProvider sArchitectureProvider;
        private static IPlayAgentSnapshotProvider sSnapshotProvider;
        private static PlayAgentCatalogConfig sCatalog;

        public static void RegisterArchitectureProvider(IPlayAgentArchitectureProvider provider)
        {
            sArchitectureProvider = provider;
        }

        public static void RegisterSnapshotProvider(IPlayAgentSnapshotProvider provider)
        {
            sSnapshotProvider = provider;
        }

        public static void RegisterCatalog(PlayAgentCatalogConfig catalog)
        {
            sCatalog = catalog;
        }

        public static IPlayAgentArchitectureProvider ArchitectureProvider => sArchitectureProvider;
        public static IPlayAgentSnapshotProvider SnapshotProvider => sSnapshotProvider;
        public static PlayAgentCatalogConfig Catalog => sCatalog;

        public static bool TryGetReadyArchitecture(out QFramework.IArchitecture architecture, out string error)
        {
            architecture = null;
            error = null;

            if (sArchitectureProvider == null)
            {
                error = "No IPlayAgentArchitectureProvider registered. Call PlayAgentBootstrap.RegisterArchitectureProvider in your project.";
                return false;
            }

            if (!sArchitectureProvider.IsReady)
            {
                error = $"Architecture '{sArchitectureProvider.ArchitectureId}' is not ready.";
                return false;
            }

            architecture = sArchitectureProvider.GetArchitecture();
            if (architecture == null)
            {
                error = $"Architecture '{sArchitectureProvider.ArchitectureId}' returned null.";
                return false;
            }

            return true;
        }
    }
}
