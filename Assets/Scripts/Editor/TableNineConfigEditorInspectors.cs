#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TableNineCharacterConfig))]
public sealed class TableNineCharacterConfigInspector : TableNineConfigEditorInspectorBase
{
    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Character;
    protected override string ConfigTitle => "角色配置库";
    protected override void OpenEditorWindow(ScriptableObject target) =>
        TableNineCharacterConfigEditorWindow.Open((TableNineCharacterConfig)target);
    protected override int GetItemCount(ScriptableObject target) =>
        ((TableNineCharacterConfig)target).Characters?.Count ?? 0;
}

[CustomEditor(typeof(TableNineCardConfig))]
public sealed class TableNineCardConfigInspector : TableNineConfigEditorInspectorBase
{
    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Card;
    protected override string ConfigTitle => "卡牌配置库";
    protected override void OpenEditorWindow(ScriptableObject target) =>
        TableNineCardConfigEditorWindow.Open((TableNineCardConfig)target);
    protected override int GetItemCount(ScriptableObject target)
    {
        var config = (TableNineCardConfig)target;
        return (config.Cards?.Count ?? 0) + (config.MonsterDeckRules?.Count ?? 0);
    }
}

[CustomEditor(typeof(TableNineSkillConfig))]
public sealed class TableNineSkillConfigInspector : TableNineConfigEditorInspectorBase
{
    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Skill;
    protected override string ConfigTitle => "技能配置库";
    protected override void OpenEditorWindow(ScriptableObject target) =>
        TableNineSkillConfigEditorWindow.Open((TableNineSkillConfig)target);
    protected override int GetItemCount(ScriptableObject target)
    {
        var config = (TableNineSkillConfig)target;
        return (config.Skills?.Count ?? 0) + (config.SkillBindings?.Count ?? 0) + (config.SkillBehaviorRules?.Count ?? 0);
    }
}

[CustomEditor(typeof(TableNineRelicConfig))]
public sealed class TableNineRelicConfigInspector : TableNineConfigEditorInspectorBase
{
    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Relic;
    protected override string ConfigTitle => "遗物与房间配置库";
    protected override void OpenEditorWindow(ScriptableObject target) =>
        TableNineRelicConfigEditorWindow.Open((TableNineRelicConfig)target);
    protected override int GetItemCount(ScriptableObject target)
    {
        var config = (TableNineRelicConfig)target;
        return (config.Relics?.Count ?? 0) + (config.Rooms?.Count ?? 0);
    }
}

[CustomEditor(typeof(TableNineEffectConfig))]
public sealed class TableNineEffectConfigInspector : TableNineConfigEditorInspectorBase
{
    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Effect;
    protected override string ConfigTitle => "效果配置库";
    protected override void OpenEditorWindow(ScriptableObject target) =>
        TableNineEffectConfigEditorWindow.Open((TableNineEffectConfig)target);
    protected override int GetItemCount(ScriptableObject target)
    {
        var config = (TableNineEffectConfig)target;
        return (config.EffectGraphs?.Count ?? 0) + (config.HelpCardEffectMappings?.Count ?? 0);
    }
}

public abstract class TableNineConfigEditorInspectorBase : Editor
{
    protected abstract WarmConsoleThemePalette ThemePalette { get; }
    protected abstract string ConfigTitle { get; }
    protected abstract void OpenEditorWindow(ScriptableObject target);
    protected abstract int GetItemCount(ScriptableObject target);

    public override void OnInspectorGUI()
    {
        var palette = ThemePalette;
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = palette.AccentStrong;

        if (GUILayout.Button("打开配置窗口", GUILayout.Height(28)))
        {
            OpenEditorWindow((ScriptableObject)target);
        }

        GUI.backgroundColor = prevBg;

        var pathStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = palette.TextPath }
        };
        EditorGUILayout.LabelField(ConfigTitle, EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{GetItemCount((ScriptableObject)target)} 条目", pathStyle);
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(target), pathStyle);
        EditorGUILayout.Space(8);
        DrawDefaultInspector();
    }
}
#endif
