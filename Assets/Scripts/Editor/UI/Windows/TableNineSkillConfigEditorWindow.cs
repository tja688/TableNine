#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TableNineSkillConfigEditorWindow : WarmConsoleConfigEditorWindowBase
{
    private const string GroupSkills = "skill";
    private const string GroupBindings = "binding";
    private const string GroupRules = "behavior";
    private const string SkillsList = "Skills";
    private const string BindingsList = "SkillBindings";
    private const string RulesList = "SkillBehaviorRules";

    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Skill;
    protected override string WindowTitle => "技能配置";
    protected override string WindowSubtitle => "编辑技能定义、效果绑定与行为规则。";
    protected override string EditorPrefsKey => "Skill";
    protected override string DefaultAssetPath => $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineSkillConfig.asset";

    [MenuItem("TableNine/Game Config/Edit Skill Config")]
    public static void OpenFromMenu()
    {
        var window = GetWindow<TableNineSkillConfigEditorWindow>();
        window.titleContent = new GUIContent("技能配置");
        var asset = AssetDatabase.LoadAssetAtPath<TableNineSkillConfig>(window.DefaultAssetPath);
        if (asset != null) window.SetTarget(asset);
        window.Show();
    }

    public static void Open(TableNineSkillConfig asset)
    {
        var window = GetWindow<TableNineSkillConfigEditorWindow>();
        window.titleContent = new GUIContent("技能配置");
        window.SetTarget(asset);
        window.Show();
    }

    protected override Type GetExpectedAssetType() => typeof(TableNineSkillConfig);

    protected override int GetTotalItemCount()
    {
        return (GetListProperty(SkillsList)?.arraySize ?? 0)
               + (GetListProperty(BindingsList)?.arraySize ?? 0)
               + (GetListProperty(RulesList)?.arraySize ?? 0);
    }

    protected override void BuildNavigation(VisualElement navList)
    {
        BuildListNav(SkillsList, GroupSkills, "技能", "SkillId", "DisplayName");
        BuildListNav(BindingsList, GroupBindings, "效果绑定", "BindingId", "OwnerDefinitionId");
        BuildListNav(RulesList, GroupRules, "行为规则", "RuleId", "SkillId");
    }

    private void BuildListNav(string listName, string group, string section, string idField, string titleField)
    {
        var list = GetListProperty(listName);
        if (list == null || list.arraySize == 0) return;

        AddNavSection(section);
        for (var i = 0; i < list.arraySize; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            AddNavButton(
                GetStringProp(element, titleField, "未命名"),
                GetStringProp(element, idField, $"{group}_{i}"),
                MakeKey(group, i));
        }
    }

    protected override void BuildDetail(VisualElement contentRoot)
    {
        if (!TryParseKey(SelectedKey, out var group, out var index))
        {
            contentRoot.Add(Skin.CreateStatusHelpBox("请选择左侧条目。", HelpBoxMessageType.Info));
            return;
        }

        switch (group)
        {
            case GroupSkills:
                BuildSkillDetail(contentRoot, index);
                break;
            case GroupBindings:
                BuildBindingDetail(contentRoot, index);
                break;
            case GroupRules:
                BuildBehaviorDetail(contentRoot, index);
                break;
            default:
                contentRoot.Add(Skin.CreateStatusHelpBox("未知条目类型。", HelpBoxMessageType.Warning));
                break;
        }
    }

    private void BuildSkillDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(SkillsList, index);
        if (element == null) return;

        contentRoot.Add(Skin.CreatePageHeader(
            GetStringProp(element, "DisplayName", "未命名技能"),
            GetStringProp(element, "SkillId", string.Empty)));

        contentRoot.Add(Skin.CreateStatsGrid(
            ("先攻", element.FindPropertyRelative("GrantsFirstStrike").boolValue ? "是" : "否", "是否赋予先攻"),
            ("绑定", element.FindPropertyRelative("HasRuntimeBinding").boolValue ? "有" : "无", "运行时效果绑定"),
            ("获得生命", element.FindPropertyRelative("MaxHpOnAcquire").intValue.ToString(), "获得时增加生命")));

        var form = new VisualElement();
        var spritePanel = new SpritePreviewPanel(Skin);
        spritePanel.Bind(TargetSo, element.FindPropertyRelative("Image"));

        form.Add(Skin.CreateSectionCard("基础信息", "技能标识与描述。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "SkillId", "DisplayName", "Description");
        }));

        form.Add(Skin.CreateSectionCard("触发与效果", "触发时机、条件与效果图表。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "Trigger", "ConditionKey", "EffectGraphId", "GrantsFirstStrike", "HasRuntimeBinding", "MaxHpOnAcquire");
        }));

        contentRoot.Add(Skin.CreateFormWithSpritePreview(form, spritePanel));
    }

    private void BuildBindingDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(BindingsList, index);
        if (element == null) return;

        contentRoot.Add(Skin.CreatePageHeader(
            GetStringProp(element, "BindingId", "未命名绑定"),
            GetStringProp(element, "OwnerDefinitionId", string.Empty)));

        contentRoot.Add(Skin.CreateSectionCard("绑定关系", "拥有者与触发配置。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "BindingId", "OwnerKind", "OwnerDefinitionId", "Trigger", "ConditionKey", "EffectGraphId");
        }));
    }

    private void BuildBehaviorDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(RulesList, index);
        if (element == null) return;

        contentRoot.Add(Skin.CreatePageHeader(
            GetStringProp(element, "RuleId", "未命名规则"),
            GetStringProp(element, "SkillId", string.Empty)));

        contentRoot.Add(Skin.CreateSectionCard("触发条件", "规则触发时机与条件键。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "RuleId", "SkillId", "Trigger", "ConditionKey");
        }));

        contentRoot.Add(Skin.CreateSectionCard("行为参数", "行为类型与数值参数。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "BehaviorKind", "IntValue", "IntValue2", "StatType", "CauseId", "IgnoreArmor", "AuraSourceSkillId");
        }));
    }

    protected override void OnAddItem()
    {
        if (!TryParseKey(SelectedKey, out var group, out _) || string.IsNullOrEmpty(group))
        {
            group = GroupSkills;
        }

        switch (group)
        {
            case GroupBindings:
                AddToList(BindingsList, GroupBindings, e =>
                {
                    e.FindPropertyRelative("BindingId").stringValue = $"binding_{DateTime.Now.Ticks % 10000}";
                });
                break;
            case GroupRules:
                AddToList(RulesList, GroupRules, e =>
                {
                    e.FindPropertyRelative("RuleId").stringValue = $"rule_{DateTime.Now.Ticks % 10000}";
                });
                break;
            default:
                AddToList(SkillsList, GroupSkills, e =>
                {
                    e.FindPropertyRelative("SkillId").stringValue = $"skill_{DateTime.Now.Ticks % 10000}";
                    e.FindPropertyRelative("DisplayName").stringValue = "新技能";
                });
                break;
        }
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

        var listName = group switch
        {
            GroupBindings => BindingsList,
            GroupRules => RulesList,
            _ => SkillsList
        };

        var list = GetListProperty(listName);
        if (list == null || index < 0 || index >= list.arraySize) return;
        list.DeleteArrayElementAtIndex(index);
    }
}
#endif
