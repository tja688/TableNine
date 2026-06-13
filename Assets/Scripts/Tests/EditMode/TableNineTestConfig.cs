#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class TableNineTestConfig
{
    static TableNineTestConfig()
    {
        TableNine.TestGameConfigResolver = ResolveProductionConfig;
    }

    public static void EnsureProductionConfigLoaded()
    {
        TableNine.ConfigureGameConfig(ResolveProductionConfig());
    }

    public static TableNineGameConfig LoadProductionConfig()
    {
        return ResolveProductionConfig();
    }

    public static GameConfigRuntimeBundle LoadProductionRuntimeBundle()
    {
        return ResolveProductionConfig().ToRuntimeBundle();
    }

    public static GameConfigSet LoadProductionCoreConfig()
    {
        return LoadProductionRuntimeBundle().Core;
    }

    private static TableNineGameConfig ResolveProductionConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<TableNineGameConfig>(TableNineGameConfigSync.MasterAssetPath);
        if (config == null)
        {
            config = TableNineGameConfigSync.SyncFromCodeDefaults();
        }

        return config;
    }
}
#endif
