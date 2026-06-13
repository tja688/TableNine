using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TableNineGameConfigSync
{
    public const string MasterAssetPath = "Assets/ScriptableObjects/TableNineGameConfig.asset";
    public const string ConfigFolderPath = "Assets/ScriptableObjects/GameConfig";

    [MenuItem("TableNine/Game Config/Sync From Code Defaults")]
    public static void SyncFromCodeDefaultsMenu()
    {
        SyncFromCodeDefaults();
        EditorUtility.DisplayDialog("TableNine", "Game config assets synced to Assets/ScriptableObjects.", "OK");
    }

    [MenuItem("TableNine/Game Config/Validate")]
    public static void ValidateMenu()
    {
        var master = AssetDatabase.LoadAssetAtPath<TableNineGameConfig>(MasterAssetPath);
        if (master == null)
        {
            EditorUtility.DisplayDialog("TableNine", "Master config asset is missing. Run Sync From Code Defaults first.", "OK");
            return;
        }

        var bundle = master.ToRuntimeBundle();
        var errors = ConfigValidator.Validate(bundle);
        var message = errors.Count == 0
            ? "Config validation passed."
            : string.Join("\n", errors);
        EditorUtility.DisplayDialog("TableNine Config Validate", message, "OK");
    }

    public static TableNineGameConfig SyncFromCodeDefaults()
    {
        EnsureFolders();

        var config = DefaultGameConfigFactory.Create();
        var characterConfig = LoadOrCreate<TableNineCharacterConfig>("TableNineCharacterConfig.asset");
        var cardConfig = LoadOrCreate<TableNineCardConfig>("TableNineCardConfig.asset");
        var skillConfig = LoadOrCreate<TableNineSkillConfig>("TableNineSkillConfig.asset");
        var relicConfig = LoadOrCreate<TableNineRelicConfig>("TableNineRelicConfig.asset");
        var effectConfig = LoadOrCreate<TableNineEffectConfig>("TableNineEffectConfig.asset");
        var master = LoadOrCreateMaster();

        characterConfig.Characters = CloneList(config.Characters);
        cardConfig.Cards = CloneList(config.Cards);
        cardConfig.MonsterDeckRules = CloneList(config.MonsterDeckRules);
        skillConfig.Skills = CloneList(config.Skills);
        skillConfig.SkillBindings = new List<SkillEffectBinding>(SkillEffectRegistry.ExportAllBindings());
        skillConfig.SkillBehaviorRules = SkillBehaviorRuleDefaults.Build();

        relicConfig.Relics = CloneList(config.Relics);
        relicConfig.Rooms = CloneList(config.Rooms);

        effectConfig.EffectGraphs = MergeEffectGraphs();
        effectConfig.HelpCardEffectMappings = new List<HelpCardEffectMapping>(EffectGraphRegistry.ExportHelpCardMappings());

        EditorUtility.SetDirty(characterConfig);
        EditorUtility.SetDirty(cardConfig);
        EditorUtility.SetDirty(skillConfig);
        EditorUtility.SetDirty(relicConfig);
        EditorUtility.SetDirty(effectConfig);

        master.CharacterConfig = characterConfig;
        master.CardConfig = cardConfig;
        master.SkillConfig = skillConfig;
        master.RelicConfig = relicConfig;
        master.EffectConfig = effectConfig;
        EditorUtility.SetDirty(master);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return master;
    }

    private static List<EffectGraphDefinition> MergeEffectGraphs()
    {
        var merged = new Dictionary<string, EffectGraphDefinition>();
        AddGraphs(merged, EffectGraphRegistry.ExportAllGraphs());
        AddGraphs(merged, SkillEffectRegistry.ExportPassiveGraphs());
        var result = new List<EffectGraphDefinition>(merged.Count);
        foreach (var pair in merged)
        {
            result.Add(pair.Value);
        }

        return result;
    }

    private static void AddGraphs(Dictionary<string, EffectGraphDefinition> merged, IReadOnlyList<EffectGraphDefinition> graphs)
    {
        for (var i = 0; i < graphs.Count; i++)
        {
            merged[graphs[i].EffectGraphId] = EffectAtomSerializationUtility.CloneGraph(graphs[i]);
        }
    }

    private static TableNineGameConfig LoadOrCreateMaster()
    {
        var master = AssetDatabase.LoadAssetAtPath<TableNineGameConfig>(MasterAssetPath);
        if (master == null && AssetDatabase.LoadMainAssetAtPath(MasterAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(MasterAssetPath);
            master = null;
        }

        if (master != null)
        {
            return master;
        }

        master = ScriptableObject.CreateInstance<TableNineGameConfig>();
        if (master == null)
        {
            throw new InvalidOperationException(
                "Failed to create TableNineGameConfig. Fix compile errors, wait for domain reload, then run Sync again.");
        }

        AssetDatabase.CreateAsset(master, MasterAssetPath);
        return master;
    }

    private static T LoadOrCreate<T>(string fileName) where T : ScriptableObject
    {
        var path = $"{ConfigFolderPath}/{fileName}";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null && AssetDatabase.LoadMainAssetAtPath(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
            asset = null;
        }

        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        if (asset == null)
        {
            throw new InvalidOperationException(
                $"Failed to create {typeof(T).Name}. Fix compile errors, wait for domain reload, then run Sync again.");
        }

        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }

        if (!AssetDatabase.IsValidFolder(ConfigFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "GameConfig");
        }
    }

    private static List<T> CloneList<T>(IReadOnlyList<T> source)
    {
        return new List<T>(source);
    }
}

[CustomEditor(typeof(TableNineGameConfig))]
public sealed class TableNineGameConfigInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Sync From Code Defaults"))
        {
            TableNineGameConfigSync.SyncFromCodeDefaults();
        }

        if (GUILayout.Button("Validate"))
        {
            TableNineGameConfigSync.ValidateMenu();
        }
    }
}
