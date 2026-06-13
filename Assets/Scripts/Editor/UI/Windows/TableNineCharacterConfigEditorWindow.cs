#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TableNineCharacterConfigEditorWindow : WarmConsoleConfigEditorWindowBase
{
    private const string GroupCharacters = "character";
    private const string ListName = "Characters";

    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Character;
    protected override string WindowTitle => "角色配置";
    protected override string WindowSubtitle => "编辑可选角色、基础属性与初始卡组。";
    protected override string EditorPrefsKey => "Character";
    protected override string DefaultAssetPath => $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineCharacterConfig.asset";

    [MenuItem("TableNine/Game Config/Edit Character Config")]
    public static void OpenFromMenu()
    {
        var window = GetWindow<TableNineCharacterConfigEditorWindow>();
        window.titleContent = new GUIContent("角色配置");
        var asset = AssetDatabase.LoadAssetAtPath<TableNineCharacterConfig>(window.DefaultAssetPath);
        if (asset != null)
        {
            window.SetTarget(asset);
        }

        window.Show();
    }

    public static void Open(TableNineCharacterConfig asset)
    {
        var window = GetWindow<TableNineCharacterConfigEditorWindow>();
        window.titleContent = new GUIContent("角色配置");
        window.SetTarget(asset);
        window.Show();
    }

    protected override Type GetExpectedAssetType() => typeof(TableNineCharacterConfig);

    protected override int GetTotalItemCount()
    {
        var list = GetListProperty(ListName);
        return list?.arraySize ?? 0;
    }

    protected override void BuildNavigation(VisualElement navList)
    {
        var list = GetListProperty(ListName);
        if (list == null)
        {
            return;
        }

        AddNavSection("角色");
        for (var i = 0; i < list.arraySize; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            var displayName = GetStringProp(element, "DisplayName", "未命名角色");
            var id = GetStringProp(element, "CharacterId", $"character_{i}");
            AddNavButton(displayName, id, MakeKey(GroupCharacters, i));
        }
    }

    protected override void BuildDetail(VisualElement contentRoot)
    {
        if (!TryParseKey(SelectedKey, out var group, out var index) || group != GroupCharacters)
        {
            contentRoot.Add(Skin.CreateStatusHelpBox("请选择左侧角色条目。", HelpBoxMessageType.Info));
            return;
        }

        var element = GetListElement(ListName, index);
        if (element == null)
        {
            return;
        }

        var displayName = GetStringProp(element, "DisplayName", "未命名角色");
        var id = GetStringProp(element, "CharacterId", string.Empty);
        contentRoot.Add(Skin.CreatePageHeader(displayName, id));
        contentRoot.Add(Skin.CreateStatsGrid(
            ("生命", element.FindPropertyRelative("BaseHp").intValue.ToString(), "基础最大生命"),
            ("攻击", element.FindPropertyRelative("BaseAttack").intValue.ToString(), "基础攻击力"),
            ("防御", element.FindPropertyRelative("BaseDefense").intValue.ToString(), "基础防御力")));

        var form = new VisualElement();
        var spritePanel = new SpritePreviewPanel(Skin);
        spritePanel.Bind(TargetSo, element.FindPropertyRelative("Image"));

        form.Add(Skin.CreateSectionCard("基础信息", "角色标识与展示文案。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "CharacterId", "DisplayName", "Description");
        }));

        form.Add(Skin.CreateSectionCard("初始构筑", "开局技能与援助卡引用。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "InitialSkillIds", "InitialHelpCardIds");
        }));

        form.Add(Skin.CreateSectionCard("基础属性", "战斗初始数值。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "BaseHp", "BaseAttack", "BaseDefense");
        }));

        contentRoot.Add(Skin.CreateFormWithSpritePreview(form, spritePanel));
    }

    protected override void OnAddItem()
    {
        var list = GetListProperty(ListName);
        if (list == null)
        {
            return;
        }

        list.arraySize++;
        var element = list.GetArrayElementAtIndex(list.arraySize - 1);
        element.FindPropertyRelative("CharacterId").stringValue = $"character_new_{list.arraySize}";
        element.FindPropertyRelative("DisplayName").stringValue = "新角色";
        SelectedKey = MakeKey(GroupCharacters, list.arraySize - 1);
        EditorPrefs.SetString($"TableNine.ConfigEditor.{EditorPrefsKey}.Selection", SelectedKey);
    }

    protected override void OnDeleteItem()
    {
        if (!TryParseKey(SelectedKey, out var group, out var index) || group != GroupCharacters)
        {
            return;
        }

        var list = GetListProperty(ListName);
        if (list == null || index < 0 || index >= list.arraySize)
        {
            return;
        }

        list.DeleteArrayElementAtIndex(index);
    }
}
#endif
