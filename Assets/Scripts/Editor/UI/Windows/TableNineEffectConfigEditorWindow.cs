#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TableNineEffectConfigEditorWindow : WarmConsoleConfigEditorWindowBase
{
    private const string GroupGraphs = "graph";
    private const string GroupMappings = "mapping";
    private const string GraphsList = "EffectGraphs";
    private const string MappingsList = "HelpCardEffectMappings";

    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Effect;
    protected override string WindowTitle => "效果配置";
    protected override string WindowSubtitle => "编辑效果图表、效果步骤链与援助卡映射。";
    protected override string EditorPrefsKey => "Effect";
    protected override string DefaultAssetPath => $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineEffectConfig.asset";

    [MenuItem("TableNine/Game Config/Edit Effect Config")]
    public static void OpenFromMenu()
    {
        var window = GetWindow<TableNineEffectConfigEditorWindow>();
        window.titleContent = new GUIContent("效果配置");
        var asset = AssetDatabase.LoadAssetAtPath<TableNineEffectConfig>(window.DefaultAssetPath);
        if (asset != null) window.SetTarget(asset);
        window.Show();
    }

    public static void Open(TableNineEffectConfig asset)
    {
        var window = GetWindow<TableNineEffectConfigEditorWindow>();
        window.titleContent = new GUIContent("效果配置");
        window.SetTarget(asset);
        window.Show();
    }

    protected override Type GetExpectedAssetType() => typeof(TableNineEffectConfig);

    protected override int GetTotalItemCount()
    {
        return (GetListProperty(GraphsList)?.arraySize ?? 0) + (GetListProperty(MappingsList)?.arraySize ?? 0);
    }

    protected override void BuildNavigation(VisualElement navList)
    {
        BuildGraphNavigation();
        BuildMappingNavigation();
    }

    private void BuildGraphNavigation()
    {
        var graphs = GetListProperty(GraphsList);
        if (graphs == null || graphs.arraySize == 0)
        {
            return;
        }

        var buckets = new Dictionary<string, List<(int index, string id)>>();
        for (var i = 0; i < graphs.arraySize; i++)
        {
            var element = graphs.GetArrayElementAtIndex(i);
            var id = GetStringProp(element, "EffectGraphId", $"graph_{i}");
            var category = EffectConfigDisplayCatalog.GetGraphCategory(id);
            if (!buckets.TryGetValue(category, out var list))
            {
                list = new List<(int, string)>();
                buckets[category] = list;
            }

            list.Add((i, id));
        }

        for (var c = 0; c < EffectConfigDisplayCatalog.CategoryOrder.Length; c++)
        {
            var category = EffectConfigDisplayCatalog.CategoryOrder[c];
            if (!buckets.TryGetValue(category, out var items) || items.Count == 0)
            {
                continue;
            }

            AddNavSection(category);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var info = EffectConfigDisplayCatalog.GetGraphInfo(item.id);
                var atomCount = graphs.GetArrayElementAtIndex(item.index).FindPropertyRelative("Atoms").arraySize;
                AddNavButton(info.DisplayName, $"{atomCount} 步 · {item.id}", MakeKey(GroupGraphs, item.index));
            }
        }
    }

    private void BuildMappingNavigation()
    {
        var mappings = GetListProperty(MappingsList);
        if (mappings == null || mappings.arraySize == 0)
        {
            return;
        }

        AddNavSection("援助卡映射");
        for (var i = 0; i < mappings.arraySize; i++)
        {
            var element = mappings.GetArrayElementAtIndex(i);
            var cardId = GetStringProp(element, "CardId", string.Empty);
            var graphId = GetStringProp(element, "EffectGraphId", string.Empty);
            var cardName = EffectConfigDisplayCatalog.GetCardDisplayName(cardId);
            var graphName = EffectConfigDisplayCatalog.GetGraphDisplayName(graphId);
            AddNavButton(cardName, $"→ {graphName}", MakeKey(GroupMappings, i));
        }
    }

    protected override void BuildDetail(VisualElement contentRoot)
    {
        if (!TryParseKey(SelectedKey, out var group, out var index))
        {
            contentRoot.Add(Skin.CreateStatusHelpBox("请从左侧选择一条效果配置。", HelpBoxMessageType.Info));
            return;
        }

        if (group == GroupGraphs)
        {
            BuildGraphDetail(contentRoot, index);
            return;
        }

        if (group == GroupMappings)
        {
            BuildMappingDetail(contentRoot, index);
            return;
        }

        contentRoot.Add(Skin.CreateStatusHelpBox("未知条目类型。", HelpBoxMessageType.Warning));
    }

    private void BuildGraphDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(GraphsList, index);
        if (element == null) return;

        var graphId = GetStringProp(element, "EffectGraphId", "未命名效果");
        var info = EffectConfigDisplayCatalog.GetGraphInfo(graphId);
        var atoms = element.FindPropertyRelative("Atoms");

        contentRoot.Add(Skin.CreatePageHeader(info.DisplayName, $"{info.Category} · {graphId}"));
        contentRoot.Add(Skin.CreateStatsGrid(
            ("效果步骤", atoms.arraySize.ToString(), "按顺序执行的原子步骤数"),
            ("效果分类", info.Category, "对照设计文档的效果归属"),
            ("运行时 ID", graphId, "代码与绑定引用的键名")));

        if (!string.IsNullOrEmpty(info.Summary))
        {
            contentRoot.Add(Skin.CreateStatusHelpBox(info.Summary, HelpBoxMessageType.Info));
        }

        var form = new VisualElement();
        var spritePanel = new SpritePreviewPanel(Skin);
        spritePanel.Bind(TargetSo, element.FindPropertyRelative("Image"));

        form.Add(Skin.CreateSectionCard("图表标识", "效果图表的运行时 ID 与展示图。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperty(section, Skin, TargetSo, element.FindPropertyRelative("EffectGraphId"));
        }));

        form.Add(Skin.CreateSectionCard("效果步骤链", "每一步对应一个效果原子，按从上到下顺序执行。", section =>
        {
            for (var i = 0; i < atoms.arraySize; i++)
            {
                EffectConfigEditorUi.AddAtomBlock(section, Skin, TargetSo, atoms.GetArrayElementAtIndex(i), i);
            }

            if (atoms.arraySize == 0)
            {
                section.Add(Skin.CreateStatusHelpBox("暂无效果步骤。点击下方按钮添加，或对照设计文档从代码同步。", HelpBoxMessageType.Info));
            }

            var addAtomBtn = new Button(() =>
            {
                atoms.arraySize++;
                var newAtom = atoms.GetArrayElementAtIndex(atoms.arraySize - 1);
                newAtom.FindPropertyRelative("AtomType").stringValue = EffectAtomTypes.Damage;
                TargetSo.ApplyModifiedProperties();
                RefreshDetail();
            }) { text = "添加效果步骤" };
            section.Add(Skin.CreateButtonRow(addAtomBtn));
        }));

        contentRoot.Add(Skin.CreateFormWithSpritePreview(form, spritePanel));
    }

    private void BuildMappingDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(MappingsList, index);
        if (element == null) return;

        var cardId = GetStringProp(element, "CardId", string.Empty);
        var graphId = GetStringProp(element, "EffectGraphId", string.Empty);
        var cardName = EffectConfigDisplayCatalog.GetCardDisplayName(cardId);
        var graphInfo = EffectConfigDisplayCatalog.GetGraphInfo(graphId);

        contentRoot.Add(Skin.CreatePageHeader(cardName, $"映射到「{graphInfo.DisplayName}」"));
        contentRoot.Add(Skin.CreateStatsGrid(
            ("援助卡", cardName, cardId),
            ("主动效果", graphInfo.DisplayName, graphId)));

        contentRoot.Add(Skin.CreateSectionCard("映射关系", "援助卡使用后执行的效果图表。被动效果在技能配置的绑定中另行配置。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element, "CardId", "EffectGraphId");
        }));
    }

    protected override void OnAddItem()
    {
        if (!TryParseKey(SelectedKey, out var group, out _) || string.IsNullOrEmpty(group))
        {
            group = GroupGraphs;
        }

        if (group == GroupMappings)
        {
            AddToList(MappingsList, GroupMappings, e =>
            {
                e.FindPropertyRelative("CardId").stringValue = string.Empty;
                e.FindPropertyRelative("EffectGraphId").stringValue = string.Empty;
            });
            return;
        }

        AddToList(GraphsList, GroupGraphs, e =>
        {
            e.FindPropertyRelative("EffectGraphId").stringValue = $"effect_{DateTime.Now.Ticks % 10000}";
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
        var listName = group == GroupMappings ? MappingsList : GraphsList;
        var list = GetListProperty(listName);
        if (list == null || index < 0 || index >= list.arraySize) return;
        list.DeleteArrayElementAtIndex(index);
    }
}
#endif
