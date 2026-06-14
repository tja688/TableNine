using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TableNinePixelIconFill
{
    private const string IconFolder = "Assets/Arts/External/StoreAssets/像素图标";
    private const string ConfigFolder = TableNineGameConfigSync.ConfigFolderPath;

    [MenuItem("TableNine/Game Config/Fill Main Icons From Pixel Store")]
    public static void FillMainIconsFromPixelStore()
    {
        var icons = LoadPixelIcons();
        if (icons.Count == 0)
        {
            EditorUtility.DisplayDialog("TableNine", $"未在 {IconFolder} 找到可用 Sprite。", "OK");
            return;
        }

        Shuffle(icons);

        var index = 0;
        var filledCards = FillCardIcons(ref index, icons);
        var filledSkills = FillSkillIcons(ref index, icons);
        var filledRelics = FillRelicIcons(ref index, icons);
        var filledEffects = FillEffectIcons(ref index, icons);

        AssetDatabase.SaveAssets();

        var message =
            $"已填充主 Icon：卡牌 {filledCards}、技能 {filledSkills}、遗物 {filledRelics}、效果 {filledEffects}。\n" +
            $"共使用 {index} 个像素图标（库内 {icons.Count} 个）。";
        Debug.Log($"[TableNinePixelIconFill] {message.Replace("\n", " ")}");
        EditorUtility.DisplayDialog("TableNine", message, "OK");
    }

    private static int FillCardIcons(ref int index, IReadOnlyList<Sprite> icons)
    {
        var config = AssetDatabase.LoadAssetAtPath<TableNineCardConfig>($"{ConfigFolder}/TableNineCardConfig.asset");
        if (config?.Cards == null)
        {
            return 0;
        }

        var filled = 0;
        for (var i = 0; i < config.Cards.Count; i++)
        {
            if (config.Cards[i].Image != null)
            {
                continue;
            }

            config.Cards[i].Image = NextIcon(ref index, icons);
            filled++;
        }

        if (filled > 0)
        {
            EditorUtility.SetDirty(config);
        }

        return filled;
    }

    private static int FillSkillIcons(ref int index, IReadOnlyList<Sprite> icons)
    {
        var config = AssetDatabase.LoadAssetAtPath<TableNineSkillConfig>($"{ConfigFolder}/TableNineSkillConfig.asset");
        if (config?.Skills == null)
        {
            return 0;
        }

        var filled = 0;
        for (var i = 0; i < config.Skills.Count; i++)
        {
            if (config.Skills[i].Image != null)
            {
                continue;
            }

            config.Skills[i].Image = NextIcon(ref index, icons);
            filled++;
        }

        if (filled > 0)
        {
            EditorUtility.SetDirty(config);
        }

        return filled;
    }

    private static int FillRelicIcons(ref int index, IReadOnlyList<Sprite> icons)
    {
        var config = AssetDatabase.LoadAssetAtPath<TableNineRelicConfig>($"{ConfigFolder}/TableNineRelicConfig.asset");
        if (config?.Relics == null)
        {
            return 0;
        }

        var filled = 0;
        for (var i = 0; i < config.Relics.Count; i++)
        {
            if (config.Relics[i].Image != null)
            {
                continue;
            }

            config.Relics[i].Image = NextIcon(ref index, icons);
            filled++;
        }

        if (filled > 0)
        {
            EditorUtility.SetDirty(config);
        }

        return filled;
    }

    private static int FillEffectIcons(ref int index, IReadOnlyList<Sprite> icons)
    {
        var config = AssetDatabase.LoadAssetAtPath<TableNineEffectConfig>($"{ConfigFolder}/TableNineEffectConfig.asset");
        if (config?.EffectGraphs == null)
        {
            return 0;
        }

        var filled = 0;
        for (var i = 0; i < config.EffectGraphs.Count; i++)
        {
            if (config.EffectGraphs[i].Image != null)
            {
                continue;
            }

            config.EffectGraphs[i].Image = NextIcon(ref index, icons);
            filled++;
        }

        if (filled > 0)
        {
            EditorUtility.SetDirty(config);
        }

        return filled;
    }

    private static List<Sprite> LoadPixelIcons()
    {
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { IconFolder });
        var sprites = new List<Sprite>(guids.Length);
        for (var i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites;
    }

    private static Sprite NextIcon(ref int index, IReadOnlyList<Sprite> icons)
    {
        if (icons.Count == 0)
        {
            return null;
        }

        var sprite = icons[index % icons.Count];
        index++;
        return sprite;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
