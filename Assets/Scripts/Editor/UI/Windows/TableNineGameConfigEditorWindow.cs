#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TableNineGameConfigEditorWindow : EditorWindow
{
    private const string PrefsSelectionKey = "TableNine.ConfigEditor.Master.Selection";

    private static readonly IReadOnlyList<SubConfigRoute> Routes = new[]
    {
        new SubConfigRoute("overview", "总览", "Master 引用与校验状态", null, null, null, null),
        new SubConfigRoute("character", "角色", "TableNineCharacterConfig", "CharacterConfig",
            typeof(TableNineCharacterConfig), "TableNineCharacterConfig.asset",
            asset => TableNineCharacterConfigEditorWindow.Open((TableNineCharacterConfig)asset)),
        new SubConfigRoute("card", "卡牌", "TableNineCardConfig", "CardConfig",
            typeof(TableNineCardConfig), "TableNineCardConfig.asset",
            asset => TableNineCardConfigEditorWindow.Open((TableNineCardConfig)asset)),
        new SubConfigRoute("skill", "技能", "TableNineSkillConfig", "SkillConfig",
            typeof(TableNineSkillConfig), "TableNineSkillConfig.asset",
            asset => TableNineSkillConfigEditorWindow.Open((TableNineSkillConfig)asset)),
        new SubConfigRoute("relic", "遗物", "TableNineRelicConfig", "RelicConfig",
            typeof(TableNineRelicConfig), "TableNineRelicConfig.asset",
            asset => TableNineRelicConfigEditorWindow.Open((TableNineRelicConfig)asset)),
        new SubConfigRoute("effect", "效果", "TableNineEffectConfig", "EffectConfig",
            typeof(TableNineEffectConfig), "TableNineEffectConfig.asset",
            asset => TableNineEffectConfigEditorWindow.Open((TableNineEffectConfig)asset)),
    };

    private WarmConsoleUiSkin _skin;
    private WarmConsoleThemePalette _theme;
    private SerializedObject _masterSo;
    private TableNineGameConfig _master;

    private VisualElement _navListRoot;
    private VisualElement _contentRoot;
    private Label _sidebarFooterLabel;

    private readonly List<WarmConsoleUiSkin.NavEntry> _navEntries = new List<WarmConsoleUiSkin.NavEntry>();
    private string _selectedKey = "overview";

    [MenuItem("TableNine/Game Config/Edit Master Config", false, 0)]
    public static void OpenFromMenu()
    {
        Open(LoadMasterAsset());
    }

    public static void Open(TableNineGameConfig master)
    {
        var window = GetWindow<TableNineGameConfigEditorWindow>();
        window.titleContent = new GUIContent("游戏配置总控");
        window.ReloadMaster(master);
        window.Show();
    }

    public void ReloadMaster(TableNineGameConfig master)
    {
        _master = master != null
            ? master
            : LoadMasterAsset();

        _masterSo = _master != null ? new SerializedObject(_master) : null;
        RefreshAll();
    }

    private void CreateGUI()
    {
        _theme = WarmConsoleThemePalette.Master;
        _skin = new WarmConsoleUiSkin(_theme);
        minSize = new Vector2(1180f, 760f);
        _selectedKey = EditorPrefs.GetString(PrefsSelectionKey, "overview");

        var root = new VisualElement { style = { flexGrow = 1, backgroundColor = _theme.RootBg } };
        root.Add(_skin.BuildHeader("游戏配置总控", "Master 统一入口：路由子库编辑器，支持引用回退与全局刷新。"));
        root.Add(_skin.BuildToolbar(
            ("刷新", RefreshAllEditors, "重新加载 Master 资产并刷新所有已打开的子库配置窗口"),
            ("保存", SaveMaster, "保存 Master 与子库引用变更"),
            ("校验", TableNineGameConfigSync.ValidateMenu, "运行全局配置校验"),
            ("同步默认", SyncFromDefaults, "从代码默认同步全部子库并写回 Master 引用")));

        var split = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        split.style.flexGrow = 1;

        var sidebar = new VisualElement
        {
            style =
            {
                flexGrow = 0,
                backgroundColor = _theme.SidebarBg,
                flexDirection = FlexDirection.Column
            }
        };

        var navScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
        navScroll.contentContainer.style.paddingLeft = 10;
        navScroll.contentContainer.style.paddingRight = 10;
        navScroll.contentContainer.style.paddingTop = 8;
        navScroll.contentContainer.style.paddingBottom = 8;
        _navListRoot = navScroll.contentContainer;
        sidebar.Add(navScroll);

        var footer = new VisualElement();
        footer.style.paddingLeft = 12;
        footer.style.paddingRight = 12;
        footer.style.paddingTop = 8;
        footer.style.paddingBottom = 10;
        footer.style.borderTopWidth = 1;
        footer.style.borderTopColor = _theme.Divider;
        _sidebarFooterLabel = _skin.CreateTinyPathLabel(string.Empty);
        footer.Add(_sidebarFooterLabel);
        sidebar.Add(footer);

        var contentScroll = _skin.CreateContentScroll(out _contentRoot);
        split.Add(sidebar);
        split.Add(contentScroll);
        root.Add(split);
        rootVisualElement.Add(root);

        if (_master == null)
        {
            ReloadMaster(LoadMasterAsset());
        }
        else
        {
            RefreshAll();
        }
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is TableNineGameConfig master)
        {
            ReloadMaster(master);
        }
    }

    private static TableNineGameConfig LoadMasterAsset()
    {
        return AssetDatabase.LoadAssetAtPath<TableNineGameConfig>(TableNineGameConfigSync.MasterAssetPath);
    }

    private void RefreshAll()
    {
        RebuildNavigation();
        RefreshDetail();
        UpdateFooter();
    }

    private void RebuildNavigation()
    {
        _navEntries.Clear();
        _navListRoot.Clear();

        _navListRoot.Add(_skin.CreateSectionLabel("路由"));
        for (var i = 0; i < Routes.Count; i++)
        {
            var route = Routes[i];
            var subtitle = route.Key == "overview"
                ? TableNineGameConfigSync.MasterAssetPath
                : GetRouteStatusSubtitle(route);

            _navListRoot.Add(_skin.CreateNavButton(
                route.Title,
                subtitle,
                route.Key,
                () => SelectNav(route.Key),
                _navEntries));
        }

        EnsureValidSelection();
        _skin.UpdateNavigationStyles(_navEntries, _selectedKey);
    }

    private void RefreshDetail()
    {
        _contentRoot.Clear();
        if (_masterSo == null || _master == null)
        {
            _contentRoot.Add(_skin.CreateStatusHelpBox(
                "未找到 Master 配置资产。请运行「同步默认」或通过 Project 选中 TableNineGameConfig.asset。",
                HelpBoxMessageType.Warning));
            BuildFallbackSection(_contentRoot);
            return;
        }

        _masterSo.Update();

        if (_selectedKey == "overview")
        {
            BuildOverviewPage(_contentRoot);
            return;
        }

        var route = FindRoute(_selectedKey);
        if (route == null)
        {
            _contentRoot.Add(_skin.CreateStatusHelpBox("未知路由页。", HelpBoxMessageType.Warning));
            return;
        }

        BuildSubConfigPage(_contentRoot, route);
    }

    private void BuildOverviewPage(VisualElement root)
    {
        root.Add(_skin.CreatePageHeader("配置总览", "Master 汇总五个子库引用。修改引用后请使用工具栏「刷新」同步已打开的子库窗口。"));

        var linkedCount = CountLinkedSubConfigs();
        root.Add(_skin.CreateStatsGrid(
            ("已链接子库", $"{linkedCount}/5", "Master 上已赋值的子库引用"),
            ("角色条目", CountCharacters().ToString(), "Characters 列表"),
            ("卡牌条目", CountCards().ToString(), "Cards + MonsterDeckRules"),
            ("技能条目", CountSkills().ToString(), "Skills + Bindings + BehaviorRules"),
            ("遗物条目", CountRelics().ToString(), "Relics + Rooms"),
            ("效果条目", CountEffects().ToString(), "EffectGraphs + HelpCard Mappings")));

        var validationErrors = TryValidate(out var errorCount);
        root.Add(_skin.CreateStatusHelpBox(
            errorCount == 0
                ? "当前配置校验通过。"
                : $"校验发现 {errorCount} 项问题，详见下方列表。",
            errorCount == 0 ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning));

        if (errorCount > 0)
        {
            var logField = new TextField { multiline = true, value = string.Join("\n", validationErrors) };
            logField.SetEnabled(false);
            logField.style.minHeight = 160;
            logField.style.marginBottom = 12;
            root.Add(logField);
        }

        root.Add(_skin.CreateSectionCard("子库链接状态", "点击左侧路由页可打开专项编辑器或回退编辑引用。", column =>
        {
            for (var i = 1; i < Routes.Count; i++)
            {
                var route = Routes[i];
                column.Add(_skin.CreateChecklistLabel($"{route.Title} · {GetRouteStatusSubtitle(route)}"));
            }
        }));

        root.Add(_skin.CreateSectionCard("Master 引用（回退编辑）", "可直接在此修改子库引用；保存后使用「刷新」同步子窗口。", BuildMasterReferenceFields));
    }

    private void BuildSubConfigPage(VisualElement root, SubConfigRoute route)
    {
        var assigned = GetAssignedAsset(route);
        var defaultPath = $"{TableNineGameConfigSync.ConfigFolderPath}/{route.DefaultFileName}";
        var resolved = assigned != null ? assigned : AssetDatabase.LoadAssetAtPath<ScriptableObject>(defaultPath);
        var path = resolved != null ? AssetDatabase.GetAssetPath(resolved) : defaultPath;
        var itemCount = resolved != null ? CountItems(resolved) : 0;
        var status = assigned != null ? "已链接" : resolved != null ? "未链接 · 默认资产可用" : "缺失";

        root.Add(_skin.CreatePageHeader(route.Title + "配置", $"{route.AssetTypeName} · {status}"));
        root.Add(_skin.CreateStatsGrid(
            ("条目数", itemCount.ToString(), "当前子库数据量"),
            ("引用状态", assigned != null ? "已赋值" : "未赋值", "Master 上的 Object 引用"),
            ("资产路径", resolved != null ? "存在" : "缺失", path)));

        if (assigned == null)
        {
            root.Add(_skin.CreateStatusHelpBox(
                resolved != null
                    ? "Master 尚未引用该子库，但默认路径下存在资产。可恢复引用或直接在下方回退编辑。"
                    : "子库资产缺失。请运行「同步默认」或在下方手动指定引用。",
                HelpBoxMessageType.Warning));
        }

        root.Add(_skin.CreateButtonRow(
            new Button(() => OpenSpecializedEditor(route, resolved)) { text = "打开专项编辑器" },
            new Button(() => PingAsset(resolved, defaultPath)) { text = "在 Project 中定位" },
            new Button(() => RestoreDefaultReference(route, defaultPath)) { text = "恢复默认引用" }));

        root.Add(_skin.CreateSectionCard("引用回退", "在专项窗口不可用时可在此编辑 Master 上的 Object 引用。", column =>
        {
            var prop = _masterSo.FindProperty(route.PropertyName);
            if (prop != null)
            {
                column.Add(_skin.WrapProperty(
                    route.Title + " Config",
                    $"绑定到 {route.AssetTypeName} 资产。",
                    prop,
                    _masterSo));
            }
        }));

        if (resolved != null && assigned == null)
        {
            root.Add(_skin.CreateSectionCard("默认资产预览", "尚未写入 Master 的默认路径资产信息。", column =>
            {
                column.Add(_skin.CreateTinyPathLabel(path));
                column.Add(_skin.CreateDescriptionLabel($"{itemCount} 条目 · 点击上方「恢复默认引用」可写入 Master。"));
            }));
        }
    }

    private void BuildMasterReferenceFields(VisualElement column)
    {
        for (var i = 1; i < Routes.Count; i++)
        {
            var route = Routes[i];
            var prop = _masterSo.FindProperty(route.PropertyName);
            if (prop == null)
            {
                continue;
            }

            column.Add(_skin.WrapProperty(
                route.Title + " Config",
                GetRouteStatusSubtitle(route),
                prop,
                _masterSo));
        }
    }

    private void BuildFallbackSection(VisualElement root)
    {
        root.Add(_skin.CreateSectionCard("创建 / 恢复 Master", "若资产不存在，可同步代码默认生成完整配置树。", column =>
        {
            column.Add(_skin.CreateButtonRow(
                new Button(SyncFromDefaults) { text = "同步默认并打开" }));
        }));
    }

    private void SelectNav(string key)
    {
        _selectedKey = key;
        EditorPrefs.SetString(PrefsSelectionKey, _selectedKey);
        _skin.UpdateNavigationStyles(_navEntries, _selectedKey);
        RefreshDetail();
    }

    private void EnsureValidSelection()
    {
        if (FindRoute(_selectedKey) != null)
        {
            return;
        }

        _selectedKey = Routes[0].Key;
        EditorPrefs.SetString(PrefsSelectionKey, _selectedKey);
    }

    private void UpdateFooter()
    {
        if (_sidebarFooterLabel == null)
        {
            return;
        }

        var path = _master != null
            ? AssetDatabase.GetAssetPath(_master)
            : TableNineGameConfigSync.MasterAssetPath;
        _sidebarFooterLabel.text = $"{CountLinkedSubConfigs()}/5 已链接 · {path}";
    }

    private void RefreshAllEditors()
    {
        _masterSo?.ApplyModifiedProperties();
        if (_master != null)
        {
            EditorUtility.SetDirty(_master);
        }

        AssetDatabase.SaveAssets();
        ReloadMaster(TableNineConfigEditorRefresh.RefreshAllOpenEditors());
        ShowNotification(new GUIContent("已刷新 Master 与所有已打开的子库窗口"));
    }

    private void SaveMaster()
    {
        if (_masterSo == null || _master == null)
        {
            return;
        }

        _masterSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(_master);
        AssetDatabase.SaveAssets();
        RefreshAll();
    }

    private void SyncFromDefaults()
    {
        var master = TableNineGameConfigSync.SyncFromCodeDefaults();
        ReloadMaster(master);
        ShowNotification(new GUIContent("已从代码默认同步全部子库"));
    }

    private void OpenSpecializedEditor(SubConfigRoute route, ScriptableObject resolved)
    {
        if (resolved == null)
        {
            EditorUtility.DisplayDialog("游戏配置总控", $"{route.Title} 子库资产不存在，请先同步默认或恢复引用。", "确定");
            return;
        }

        route.OpenEditor(resolved);
    }

    private void RestoreDefaultReference(SubConfigRoute route, string defaultPath)
    {
        if (_masterSo == null)
        {
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(defaultPath);
        if (asset == null)
        {
            EditorUtility.DisplayDialog("游戏配置总控", $"默认资产不存在：{defaultPath}", "确定");
            return;
        }

        var prop = _masterSo.FindProperty(route.PropertyName);
        if (prop == null)
        {
            return;
        }

        prop.objectReferenceValue = asset;
        _masterSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(_master);
        AssetDatabase.SaveAssets();
        RefreshAll();
    }

    private static void PingAsset(ScriptableObject asset, string fallbackPath)
    {
        var target = asset != null ? asset : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fallbackPath);
        if (target == null)
        {
            EditorUtility.DisplayDialog("游戏配置总控", "找不到可定位的资产。", "确定");
            return;
        }

        EditorGUIUtility.PingObject(target);
        Selection.activeObject = target;
    }

    private ScriptableObject GetAssignedAsset(SubConfigRoute route)
    {
        if (_master == null || route.PropertyName == null)
        {
            return null;
        }

        return route.PropertyName switch
        {
            "CharacterConfig" => _master.CharacterConfig,
            "CardConfig" => _master.CardConfig,
            "SkillConfig" => _master.SkillConfig,
            "RelicConfig" => _master.RelicConfig,
            "EffectConfig" => _master.EffectConfig,
            _ => null
        };
    }

    private static SubConfigRoute FindRoute(string key)
    {
        for (var i = 0; i < Routes.Count; i++)
        {
            if (Routes[i].Key == key)
            {
                return Routes[i];
            }
        }

        return null;
    }

    private string GetRouteStatusSubtitle(SubConfigRoute route)
    {
        if (route.Key == "overview")
        {
            return TableNineGameConfigSync.MasterAssetPath;
        }

        var assigned = GetAssignedAsset(route);
        if (assigned != null)
        {
            return $"{CountItems(assigned)} 条目 · 已链接";
        }

        var defaultPath = $"{TableNineGameConfigSync.ConfigFolderPath}/{route.DefaultFileName}";
        var fallback = AssetDatabase.LoadAssetAtPath<ScriptableObject>(defaultPath);
        return fallback != null
            ? $"{CountItems(fallback)} 条目 · 未链接"
            : "资产缺失";
    }

    private int CountLinkedSubConfigs()
    {
        if (_master == null)
        {
            return 0;
        }

        var count = 0;
        if (_master.CharacterConfig != null) count++;
        if (_master.CardConfig != null) count++;
        if (_master.SkillConfig != null) count++;
        if (_master.RelicConfig != null) count++;
        if (_master.EffectConfig != null) count++;
        return count;
    }

    private int CountCharacters() => _master?.CharacterConfig?.Characters?.Count ?? 0;

    private int CountCards()
    {
        var config = _master?.CardConfig;
        return (config?.Cards?.Count ?? 0) + (config?.MonsterDeckRules?.Count ?? 0);
    }

    private int CountSkills()
    {
        var config = _master?.SkillConfig;
        return (config?.Skills?.Count ?? 0)
               + (config?.SkillBindings?.Count ?? 0)
               + (config?.SkillBehaviorRules?.Count ?? 0);
    }

    private int CountRelics()
    {
        var config = _master?.RelicConfig;
        return (config?.Relics?.Count ?? 0) + (config?.Rooms?.Count ?? 0);
    }

    private int CountEffects()
    {
        var config = _master?.EffectConfig;
        return (config?.EffectGraphs?.Count ?? 0) + (config?.HelpCardEffectMappings?.Count ?? 0);
    }

    private static int CountItems(ScriptableObject asset)
    {
        switch (asset)
        {
            case TableNineCharacterConfig character:
                return character.Characters?.Count ?? 0;
            case TableNineCardConfig card:
                return (card.Cards?.Count ?? 0) + (card.MonsterDeckRules?.Count ?? 0);
            case TableNineSkillConfig skill:
                return (skill.Skills?.Count ?? 0)
                       + (skill.SkillBindings?.Count ?? 0)
                       + (skill.SkillBehaviorRules?.Count ?? 0);
            case TableNineRelicConfig relic:
                return (relic.Relics?.Count ?? 0) + (relic.Rooms?.Count ?? 0);
            case TableNineEffectConfig effect:
                return (effect.EffectGraphs?.Count ?? 0) + (effect.HelpCardEffectMappings?.Count ?? 0);
            default:
                return 0;
        }
    }

    private static List<string> TryValidate(out int errorCount)
    {
        errorCount = 0;
        var master = LoadMasterAsset();
        if (master == null)
        {
            errorCount = 1;
            return new List<string> { "Master 配置资产缺失。" };
        }

        var errors = ConfigValidator.Validate(master.ToRuntimeBundle());
        errorCount = errors.Count;
        return errors;
    }

    private sealed class SubConfigRoute
    {
        public string Key { get; }
        public string Title { get; }
        public string AssetTypeName { get; }
        public string PropertyName { get; }
        public Type AssetType { get; }
        public string DefaultFileName { get; }
        public Action<ScriptableObject> OpenEditor { get; }

        public SubConfigRoute(
            string key,
            string title,
            string assetTypeName,
            string propertyName,
            Type assetType,
            string defaultFileName,
            Action<ScriptableObject> openEditor)
        {
            Key = key;
            Title = title;
            AssetTypeName = assetTypeName;
            PropertyName = propertyName;
            AssetType = assetType;
            DefaultFileName = defaultFileName;
            OpenEditor = openEditor;
        }
    }
}
#endif
