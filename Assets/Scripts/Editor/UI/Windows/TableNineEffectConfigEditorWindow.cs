#if UNITY_EDITOR
using System;
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
    protected override string WindowSubtitle => "编辑效果图表、原子链与援助卡映射。";
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
        var graphs = GetListProperty(GraphsList);
        if (graphs != null && graphs.arraySize > 0)
        {
            AddNavSection("效果图");
            for (var i = 0; i < graphs.arraySize; i++)
            {
                var element = graphs.GetArrayElementAtIndex(i);
                var id = GetStringProp(element, "EffectGraphId", $"graph_{i}");
                var atomCount = element.FindPropertyRelative("Atoms").arraySize;
                AddNavButton(id, $"{atomCount} 个原子", MakeKey(GroupGraphs, i));
            }
        }

        var mappings = GetListProperty(MappingsList);
        if (mappings != null && mappings.arraySize > 0)
        {
            AddNavSection("援助卡映射");
            for (var i = 0; i < mappings.arraySize; i++)
            {
                var element = mappings.GetArrayElementAtIndex(i);
                AddNavButton(
                    GetStringProp(element, "CardId", "未指定卡牌"),
                    GetStringProp(element, "EffectGraphId", "未指定效果"),
                    MakeKey(GroupMappings, i));
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
        var atoms = element.FindPropertyRelative("Atoms");

        contentRoot.Add(Skin.CreatePageHeader(graphId, "效果原子执行链"));
        contentRoot.Add(Skin.CreateStatsGrid(
            ("原子数", atoms.arraySize.ToString(), "按顺序执行的效果原子"),
            ("图表 ID", graphId, "运行时引用键")));

        var form = new VisualElement();
        var spritePanel = new SpritePreviewPanel(Skin);
        spritePanel.Bind(TargetSo, element.FindPropertyRelative("Image"));

        form.Add(Skin.CreateSectionCard("图表标识", "效果图 ID 与展示图。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperty(section, Skin, TargetSo, element.FindPropertyRelative("EffectGraphId"));
        }));

        form.Add(Skin.CreateSectionCard("效果原子", "按顺序配置每个原子的类型与参数。", section =>
        {
            for (var i = 0; i < atoms.arraySize; i++)
            {
                var atom = atoms.GetArrayElementAtIndex(i);
                var atomType = GetStringProp(atom, "AtomType", "未指定");
                section.Add(Skin.CreateSectionCard($"原子 {i + 1}: {atomType}", "单步效果逻辑与参数。", atomSection =>
                {
                    ConfigEditorPropertyBuilder.AddProperty(atomSection, Skin, TargetSo, atom.FindPropertyRelative("AtomType"));
                    ConfigEditorPropertyBuilder.AddProperty(atomSection, Skin, TargetSo, atom.FindPropertyRelative("Parameters"));
                }));
            }

            if (atoms.arraySize == 0)
            {
                section.Add(Skin.CreateStatusHelpBox("暂无效果原子，可在下方添加或在 Inspector 中编辑 Atoms 数组。", HelpBoxMessageType.Info));
            }

            var addAtomBtn = new UnityEngine.UIElements.Button(() =>
            {
                atoms.arraySize++;
                var newAtom = atoms.GetArrayElementAtIndex(atoms.arraySize - 1);
                newAtom.FindPropertyRelative("AtomType").stringValue = EffectAtomTypes.Damage;
                TargetSo.ApplyModifiedProperties();
                RefreshDetail();
            }) { text = "添加原子" };
            section.Add(Skin.CreateButtonRow(addAtomBtn));
        }));

        contentRoot.Add(Skin.CreateFormWithSpritePreview(form, spritePanel));
    }

    private void BuildMappingDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(MappingsList, index);
        if (element == null) return;

        contentRoot.Add(Skin.CreatePageHeader(
            GetStringProp(element, "CardId", "未指定卡牌"),
            $"→ {GetStringProp(element, "EffectGraphId", "未指定效果")}"));

        contentRoot.Add(Skin.CreateSectionCard("映射关系", "援助卡 ID 与效果图表 ID 的对应。", section =>
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
