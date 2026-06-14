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

        characterConfig.Characters = PreserveCharacterVisuals(CloneList(config.Characters), characterConfig.Characters);
        cardConfig.CardDecks = PreserveDeckVisuals(CloneList(config.CardDecks), cardConfig.CardDecks);
        cardConfig.Cards = PreserveCardVisuals(CloneList(config.Cards), cardConfig.Cards);
        ApplyTutorCardIconsFromSkills(cardConfig.Cards, skillConfig.Skills);
        cardConfig.MonsterDeckRules = CloneList(config.MonsterDeckRules);
        skillConfig.Skills = PreserveSkillVisuals(CloneList(config.Skills), skillConfig.Skills);
        skillConfig.SkillBindings = new List<SkillEffectBinding>(SkillEffectRegistry.ExportAllBindings());
        skillConfig.SkillBehaviorRules = SkillBehaviorRuleDefaults.Build();

        relicConfig.Relics = PreserveRelicVisuals(CloneList(config.Relics), relicConfig.Relics);
        relicConfig.Rooms = CloneList(config.Rooms);

        effectConfig.EffectGraphs = PreserveEffectVisuals(MergeEffectGraphs(), effectConfig.EffectGraphs);
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

    private static List<CharacterDefinition> PreserveCharacterVisuals(
        List<CharacterDefinition> next,
        IReadOnlyList<CharacterDefinition> previous)
    {
        var visuals = BuildLookup(previous, character => character?.CharacterId, PreserveCharacterSprites);
        for (var i = 0; i < next.Count; i++)
        {
            if (visuals.TryGetValue(next[i].CharacterId, out var apply))
            {
                apply(next[i]);
            }
        }

        return next;
    }

    private static List<CardDeckDefinition> PreserveDeckVisuals(
        List<CardDeckDefinition> next,
        IReadOnlyList<CardDeckDefinition> previous)
    {
        var visuals = BuildLookup(previous, deck => deck?.DeckId, PreserveDeckSprites);
        for (var i = 0; i < next.Count; i++)
        {
            if (visuals.TryGetValue(next[i].DeckId, out var apply))
            {
                apply(next[i]);
            }
        }

        return next;
    }

    private static List<CardDefinition> PreserveCardVisuals(
        List<CardDefinition> next,
        IReadOnlyList<CardDefinition> previous)
    {
        var visuals = BuildLookup(previous, card => card?.CardId, PreserveCardSprites);
        for (var i = 0; i < next.Count; i++)
        {
            if (visuals.TryGetValue(next[i].CardId, out var apply))
            {
                apply(next[i]);
            }
        }

        return next;
    }

    private static List<SkillDefinition> PreserveSkillVisuals(
        List<SkillDefinition> next,
        IReadOnlyList<SkillDefinition> previous)
    {
        var visuals = BuildLookup(previous, skill => skill?.SkillId, PreserveSkillSprite);
        for (var i = 0; i < next.Count; i++)
        {
            if (visuals.TryGetValue(next[i].SkillId, out var apply))
            {
                apply(next[i]);
            }
        }

        return next;
    }

    private static List<RelicDefinition> PreserveRelicVisuals(
        List<RelicDefinition> next,
        IReadOnlyList<RelicDefinition> previous)
    {
        var visuals = BuildLookup(previous, relic => relic?.RelicId, PreserveRelicSprite);
        for (var i = 0; i < next.Count; i++)
        {
            if (visuals.TryGetValue(next[i].RelicId, out var apply))
            {
                apply(next[i]);
            }
        }

        return next;
    }

    private static List<EffectGraphDefinition> PreserveEffectVisuals(
        List<EffectGraphDefinition> next,
        IReadOnlyList<EffectGraphDefinition> previous)
    {
        var visuals = BuildLookup(previous, graph => graph?.EffectGraphId, PreserveEffectSprite);
        for (var i = 0; i < next.Count; i++)
        {
            if (visuals.TryGetValue(next[i].EffectGraphId, out var apply))
            {
                apply(next[i]);
            }
        }

        return next;
    }

    private static Dictionary<string, Action<TTarget>> BuildLookup<TSource, TTarget>(
        IReadOnlyList<TSource> previous,
        Func<TSource, string> getKey,
        Func<TSource, Action<TTarget>> createApply)
        where TTarget : class
    {
        var lookup = new Dictionary<string, Action<TTarget>>();
        if (previous == null)
        {
            return lookup;
        }

        for (var i = 0; i < previous.Count; i++)
        {
            var source = previous[i];
            var key = getKey(source);
            if (string.IsNullOrWhiteSpace(key) || lookup.ContainsKey(key))
            {
                continue;
            }

            lookup[key] = createApply(source);
        }

        return lookup;
    }

    private static Action<CharacterDefinition> PreserveCharacterSprites(CharacterDefinition previous)
    {
        return next =>
        {
            next.Image = previous.Image ?? next.Image;
        };
    }

    private static Action<CardDeckDefinition> PreserveDeckSprites(CardDeckDefinition previous)
    {
        return next =>
        {
            next.DefaultFaceImage = previous.DefaultFaceImage ?? next.DefaultFaceImage;
            next.DefaultBackImage = previous.DefaultBackImage ?? next.DefaultBackImage;
        };
    }

    private static Action<CardDefinition> PreserveCardSprites(CardDefinition previous)
    {
        return next =>
        {
            next.Image = previous.Image ?? next.Image;
            next.FaceImageOverride = previous.FaceImageOverride ?? next.FaceImageOverride;
            next.BackImageOverride = previous.BackImageOverride ?? next.BackImageOverride;
        };
    }

    private static Action<SkillDefinition> PreserveSkillSprite(SkillDefinition previous)
    {
        return next => next.Image = previous.Image ?? next.Image;
    }

    private static Action<RelicDefinition> PreserveRelicSprite(RelicDefinition previous)
    {
        return next => next.Image = previous.Image ?? next.Image;
    }

    private static void ApplyTutorCardIconsFromSkills(
        IReadOnlyList<CardDefinition> cards,
        IReadOnlyList<SkillDefinition> skills)
    {
        if (cards == null || skills == null)
        {
            return;
        }

        var skillImages = new Dictionary<string, Sprite>();
        for (var i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId) || skill.Image == null)
            {
                continue;
            }

            skillImages[skill.SkillId] = skill.Image;
        }

        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null || card.CardType != CardType.Tutor || card.Image != null)
            {
                continue;
            }

            if (card.SkillIds == null || card.SkillIds.Count == 0)
            {
                continue;
            }

            if (skillImages.TryGetValue(card.SkillIds[0], out var image))
            {
                card.Image = image;
            }
        }
    }

    private static Action<EffectGraphDefinition> PreserveEffectSprite(EffectGraphDefinition previous)
    {
        return next => next.Image = previous.Image ?? next.Image;
    }
}

[CustomEditor(typeof(TableNineGameConfig))]
public sealed class TableNineGameConfigInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var palette = WarmConsoleThemePalette.Master;
        var master = (TableNineGameConfig)target;

        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = palette.AccentStrong;
        if (GUILayout.Button("打开总控窗口", GUILayout.Height(28)))
        {
            TableNineGameConfigEditorWindow.Open(master);
        }

        GUI.backgroundColor = prevBg;

        var pathStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = palette.TextPath }
        };
        EditorGUILayout.LabelField("游戏配置 Master", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{CountLinkedSubConfigs(master)}/5 子库已链接", pathStyle);
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(master), pathStyle);
        EditorGUILayout.Space(8);

        if (GUILayout.Button("刷新全部配置窗口", GUILayout.Height(24)))
        {
            TableNineConfigEditorRefresh.RefreshAllOpenEditors();
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Sync From Code Defaults"))
        {
            TableNineGameConfigSync.SyncFromCodeDefaults();
        }

        if (GUILayout.Button("Validate"))
        {
            TableNineGameConfigSync.ValidateMenu();
        }

        EditorGUILayout.Space(8);
        DrawDefaultInspector();
    }

    private static int CountLinkedSubConfigs(TableNineGameConfig master)
    {
        if (master == null)
        {
            return 0;
        }

        var count = 0;
        if (master.CharacterConfig != null) count++;
        if (master.CardConfig != null) count++;
        if (master.SkillConfig != null) count++;
        if (master.RelicConfig != null) count++;
        if (master.EffectConfig != null) count++;
        return count;
    }
}
