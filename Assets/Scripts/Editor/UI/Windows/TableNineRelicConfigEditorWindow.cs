#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TableNineRelicConfigEditorWindow : WarmConsoleConfigEditorWindowBase
{
    private const string GroupRelics = "relic";
    private const string GroupRooms = "room";
    private const string RelicsList = "Relics";
    private const string RoomsList = "Rooms";

    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Relic;
    protected override string WindowTitle => "遗物与房间配置";
    protected override string WindowSubtitle => "编辑遗物属性加成与房间事件。";
    protected override string EditorPrefsKey => "Relic";
    protected override string DefaultAssetPath => $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineRelicConfig.asset";

    [MenuItem("TableNine/Game Config/Edit Relic Config")]
    public static void OpenFromMenu()
    {
        var window = GetWindow<TableNineRelicConfigEditorWindow>();
        window.titleContent = new GUIContent("遗物配置");
        var asset = AssetDatabase.LoadAssetAtPath<TableNineRelicConfig>(window.DefaultAssetPath);
        if (asset != null) window.SetTarget(asset);
        window.Show();
    }

    public static void Open(TableNineRelicConfig asset)
    {
        var window = GetWindow<TableNineRelicConfigEditorWindow>();
        window.titleContent = new GUIContent("遗物配置");
        window.SetTarget(asset);
        window.Show();
    }

    protected override Type GetExpectedAssetType() => typeof(TableNineRelicConfig);

    protected override int GetTotalItemCount()
    {
        return (GetListProperty(RelicsList)?.arraySize ?? 0) + (GetListProperty(RoomsList)?.arraySize ?? 0);
    }

    protected override void BuildNavigation(VisualElement navList)
    {
        var relics = GetListProperty(RelicsList);
        if (relics != null && relics.arraySize > 0)
        {
            AddNavSection("遗物");
            for (var i = 0; i < relics.arraySize; i++)
            {
                var element = relics.GetArrayElementAtIndex(i);
                AddNavButton(
                    GetStringProp(element, "DisplayName", "未命名遗物"),
                    GetStringProp(element, "RelicId", $"relic_{i}"),
                    MakeKey(GroupRelics, i));
            }
        }

        var rooms = GetListProperty(RoomsList);
        if (rooms != null && rooms.arraySize > 0)
        {
            AddNavSection("房间");
            for (var i = 0; i < rooms.arraySize; i++)
            {
                var element = rooms.GetArrayElementAtIndex(i);
                AddNavButton(
                    GetStringProp(element, "DisplayName", "未命名房间"),
                    GetStringProp(element, "RoomId", $"room_{i}"),
                    MakeKey(GroupRooms, i));
            }
        }
    }

    protected override void BuildDetail(VisualElement contentRoot)
    {
        if (!TryParseKey(SelectedKey, out var group, out var index))
        {
            contentRoot.Add(Skin.CreateStatusHelpBox("请选择左侧条目。", HelpBoxMessageType.Info));
            return;
        }

        if (group == GroupRelics)
        {
            BuildRelicDetail(contentRoot, index);
            return;
        }

        if (group == GroupRooms)
        {
            BuildRoomDetail(contentRoot, index);
            return;
        }

        contentRoot.Add(Skin.CreateStatusHelpBox("未知条目类型。", HelpBoxMessageType.Warning));
    }

    private void BuildRelicDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(RelicsList, index);
        if (element == null) return;

        contentRoot.Add(Skin.CreatePageHeader(
            GetStringProp(element, "DisplayName", "未命名遗物"),
            GetStringProp(element, "RelicId", string.Empty)));

        contentRoot.Add(Skin.CreateStatsGrid(
            ("攻击", $"+{element.FindPropertyRelative("StatAttackBonus").intValue}", "攻击加成"),
            ("防御", $"+{element.FindPropertyRelative("StatDefenseBonus").intValue}", "防御加成"),
            ("生命", $"+{element.FindPropertyRelative("StatMaxHpBonus").intValue}", "最大生命加成"),
            ("品质", element.FindPropertyRelative("Quality").enumDisplayNames[element.FindPropertyRelative("Quality").enumValueIndex], "遗物品质")));

        var form = new VisualElement();
        var spritePanel = new SpritePreviewPanel(Skin);
        spritePanel.Bind(TargetSo, element.FindPropertyRelative("Image"));

        form.Add(Skin.CreateSectionCard("基础信息", "遗物标识与描述。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "RelicId", "DisplayName", "Description", "Quality");
        }));

        form.Add(Skin.CreateSectionCard("属性加成", "遗物提供的数值加成。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "StatAttackBonus", "StatDefenseBonus", "StatMaxHpBonus");
        }));

        form.Add(Skin.CreateSectionCard("池规则", "一次性与随机池设置。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "IsOneShot", "ExcludeFromPool");
        }));

        contentRoot.Add(Skin.CreateFormWithSpritePreview(form, spritePanel));
    }

    private void BuildRoomDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(RoomsList, index);
        if (element == null) return;

        contentRoot.Add(Skin.CreatePageHeader(
            GetStringProp(element, "DisplayName", "未命名房间"),
            GetStringProp(element, "RoomId", string.Empty)));

        contentRoot.Add(Skin.CreateStatsGrid(
            ("金币", element.FindPropertyRelative("RewardGold").intValue.ToString(), "完成奖励"),
            ("生命", $"+{element.FindPropertyRelative("StatMaxHpBonus").intValue}", "最大生命加成"),
            ("类型", element.FindPropertyRelative("RoomType").enumDisplayNames[element.FindPropertyRelative("RoomType").enumValueIndex], "房间类型")));

        var form = new VisualElement();
        var spritePanel = new SpritePreviewPanel(Skin);
        spritePanel.Bind(TargetSo, element.FindPropertyRelative("Image"));

        form.Add(Skin.CreateSectionCard("基础信息", "房间标识与描述。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "RoomId", "DisplayName", "Description", "RoomType");
        }));

        form.Add(Skin.CreateSectionCard("奖励与注入", "房间奖励与卡牌注入。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "RewardGold", "StatMaxHpBonus", "InjectCardId");
        }));

        contentRoot.Add(Skin.CreateFormWithSpritePreview(form, spritePanel));
    }

    protected override void OnAddItem()
    {
        if (!TryParseKey(SelectedKey, out var group, out _) || string.IsNullOrEmpty(group))
        {
            group = GroupRelics;
        }

        if (group == GroupRooms)
        {
            AddToList(RoomsList, GroupRooms, e =>
            {
                e.FindPropertyRelative("RoomId").stringValue = $"room_{DateTime.Now.Ticks % 10000}";
                e.FindPropertyRelative("DisplayName").stringValue = "新房间";
            });
            return;
        }

        AddToList(RelicsList, GroupRelics, e =>
        {
            e.FindPropertyRelative("RelicId").stringValue = $"relic_{DateTime.Now.Ticks % 10000}";
            e.FindPropertyRelative("DisplayName").stringValue = "新遗物";
        });
    }

    private void AddToList(string listName, string group, Action<SerializedProperty> init)
    {
        var list = GetListProperty(listName);
        if (list == null) return;
        list.arraySize++;
        init?.Invoke(list.GetArrayElementAtIndex(list.arraySize - 1));
        SelectedKey = MakeKey(group, list.arraySize - 1);
        EditorPrefs.SetString($"TableNine.ConfigEditor.{EditorPrefsKey}.Selection", SelectedKey);
    }

    protected override void OnDeleteItem()
    {
        if (!TryParseKey(SelectedKey, out var group, out var index)) return;
        var listName = group == GroupRooms ? RoomsList : RelicsList;
        var list = GetListProperty(listName);
        if (list == null || index < 0 || index >= list.arraySize) return;
        list.DeleteArrayElementAtIndex(index);
    }
}
#endif
