#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TableNineConfigEditorRefresh
{
    public static TableNineGameConfig ReloadMasterAsset()
    {
        var path = TableNineGameConfigSync.MasterAssetPath;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<TableNineGameConfig>(path);
    }

    public static TableNineGameConfig RefreshAllOpenEditors()
    {
        var master = ReloadMasterAsset();
        RefreshMasterWindow(master);
        RefreshSubEditorWindows(master);
        return master;
    }

    private static void RefreshMasterWindow(TableNineGameConfig master)
    {
        var windows = Resources.FindObjectsOfTypeAll<TableNineGameConfigEditorWindow>();
        for (var i = 0; i < windows.Length; i++)
        {
            windows[i].ReloadMaster(master);
        }
    }

    private static void RefreshSubEditorWindows(TableNineGameConfig master)
    {
        RefreshSubEditor<TableNineCharacterConfig>(
            master?.CharacterConfig,
            $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineCharacterConfig.asset");

        RefreshSubEditor<TableNineCardConfig>(
            master?.CardConfig,
            $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineCardConfig.asset");

        RefreshSubEditor<TableNineSkillConfig>(
            master?.SkillConfig,
            $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineSkillConfig.asset");

        RefreshSubEditor<TableNineRelicConfig>(
            master?.RelicConfig,
            $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineRelicConfig.asset");

        RefreshSubEditor<TableNineEffectConfig>(
            master?.EffectConfig,
            $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineEffectConfig.asset");
    }

    private static void RefreshSubEditor<TAsset>(TAsset assigned, string defaultPath)
        where TAsset : ScriptableObject
    {
        var resolved = assigned != null
            ? assigned
            : AssetDatabase.LoadAssetAtPath<TAsset>(defaultPath);

        if (resolved == null)
        {
            return;
        }

        var windows = Resources.FindObjectsOfTypeAll<WarmConsoleConfigEditorWindowBase>();
        for (var i = 0; i < windows.Length; i++)
        {
            var window = windows[i];
            if (window.CurrentTargetAsset != null && window.CurrentTargetAsset.GetType() == typeof(TAsset))
            {
                window.SetTarget(resolved);
            }
        }
    }
}
#endif
