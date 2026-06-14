#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class WarmConsoleConfigEditorWindowBase : EditorWindow
{
    protected WarmConsoleUiSkin Skin { get; private set; }
    protected SerializedObject TargetSo { get; private set; }
    protected ScriptableObject TargetAsset { get; private set; }
    public ScriptableObject CurrentTargetAsset => TargetAsset;

    protected readonly List<WarmConsoleUiSkin.NavEntry> NavEntries = new List<WarmConsoleUiSkin.NavEntry>();

    protected VisualElement NavListRoot;
    protected VisualElement ContentRoot;
    protected Label SidebarFooterLabel;
    protected TextField SearchField;

    protected string SelectedKey = string.Empty;
    protected string SearchFilter = string.Empty;

    private TwoPaneSplitView _splitView;
    private ToolbarToggle _autoSaveToggle;

    protected abstract WarmConsoleThemePalette ThemePalette { get; }
    protected abstract string WindowTitle { get; }
    protected abstract string WindowSubtitle { get; }
    protected abstract string EditorPrefsKey { get; }
    protected abstract string DefaultAssetPath { get; }

    protected virtual bool CanAdd => true;
    protected virtual bool CanDelete => true;

    protected abstract void BuildNavigation(VisualElement navList);
    protected abstract void BuildDetail(VisualElement contentRoot);
    protected abstract void OnAddItem();
    protected abstract void OnDeleteItem();
    protected abstract int GetTotalItemCount();

    public void SetTarget(ScriptableObject asset)
    {
        FlushAutoSaveBeforeContextChange();

        if (asset == null)
        {
            return;
        }

        TargetAsset = asset;
        TargetSo = new SerializedObject(asset);
        SelectedKey = EditorPrefs.GetString(GetSelectionPrefsKey(), SelectedKey);
        SearchFilter = EditorPrefs.GetString(GetSearchPrefsKey(), SearchFilter);
        RefreshAll();
    }

    protected void CreateGUI()
    {
        Skin = new WarmConsoleUiSkin(ThemePalette);
        minSize = new Vector2(1180f, 760f);

        var root = new VisualElement { style = { flexGrow = 1, backgroundColor = ThemePalette.RootBg } };
        root.Add(Skin.BuildHeader(WindowTitle, WindowSubtitle));

        SearchField = new TextField { value = SearchFilter };
        SearchField.tooltip = "按名称或 ID 过滤侧栏列表";
        SearchField.RegisterValueChangedCallback(evt =>
        {
            SearchFilter = evt.newValue ?? string.Empty;
            EditorPrefs.SetString(GetSearchPrefsKey(), SearchFilter);
            RebuildNavigation();
        });

        root.Add(Skin.BuildActionBar(SearchField,
            ("保存", SaveAssets, "保存当前配置资产（自动保存关闭时必用）"),
            ("校验", ValidateConfig, "运行全局配置校验"),
            ("新增", AddItem, "新增当前分组条目"),
            ("删除", DeleteItem, "删除当前选中条目")));

        var actionBar = root[root.childCount - 1] as Toolbar;
        if (actionBar != null)
        {
            _autoSaveToggle = TableNineConfigEditorAutoSave.CreateToolbarToggle(_ => UpdateFooter());
            actionBar.Insert(0, _autoSaveToggle);
        }

        _splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        _splitView.style.flexGrow = 1;

        var sidebar = new VisualElement();
        sidebar.style.flexGrow = 0;
        sidebar.style.backgroundColor = ThemePalette.SidebarBg;
        sidebar.style.flexDirection = FlexDirection.Column;

        var navScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
        navScroll.contentContainer.style.paddingLeft = 10;
        navScroll.contentContainer.style.paddingRight = 10;
        navScroll.contentContainer.style.paddingTop = 8;
        navScroll.contentContainer.style.paddingBottom = 8;
        NavListRoot = navScroll.contentContainer;
        sidebar.Add(navScroll);

        var footer = new VisualElement();
        footer.style.paddingLeft = 12;
        footer.style.paddingRight = 12;
        footer.style.paddingTop = 8;
        footer.style.paddingBottom = 10;
        footer.style.borderTopWidth = 1;
        footer.style.borderTopColor = ThemePalette.Divider;
        SidebarFooterLabel = Skin.CreateTinyPathLabel("0 条目");
        footer.Add(SidebarFooterLabel);
        sidebar.Add(footer);

        var contentScroll = Skin.CreateContentScroll(out ContentRoot);
        _splitView.Add(sidebar);
        _splitView.Add(contentScroll);
        root.Add(_splitView);
        rootVisualElement.Add(root);

        if (TargetAsset == null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(DefaultAssetPath);
            if (asset != null)
            {
                SetTarget(asset);
            }
        }
        else
        {
            RefreshAll();
        }
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is ScriptableObject so && so.GetType() == GetExpectedAssetType())
        {
            SetTarget(so);
        }
    }

    protected abstract Type GetExpectedAssetType();

    protected void RefreshAll()
    {
        RebuildNavigation();
        RefreshDetail();
        UpdateFooter();
    }

    protected void RebuildNavigation()
    {
        NavEntries.Clear();
        NavListRoot.Clear();
        BuildNavigation(NavListRoot);
        EnsureValidSelection();
        Skin.UpdateNavigationStyles(NavEntries, SelectedKey);
        UpdateFooter();
    }

    protected void RefreshDetail()
    {
        ContentRoot.Clear();
        if (TargetSo == null)
        {
            ContentRoot.Add(Skin.CreateStatusHelpBox("未找到配置资产，请从 Project 选中对应 SO 或通过菜单打开。", HelpBoxMessageType.Warning));
            return;
        }

        TargetSo.Update();
        if (string.IsNullOrEmpty(SelectedKey))
        {
            ContentRoot.Add(Skin.CreateStatusHelpBox("从左侧选择一条配置进行编辑。", HelpBoxMessageType.Info));
            return;
        }

        BuildDetail(ContentRoot);
        TableNineConfigEditorAutoSave.BindContentRoot(ContentRoot, TargetSo, TargetAsset);
    }

    protected void SelectNav(string key)
    {
        FlushAutoSaveBeforeContextChange();
        SelectedKey = key;
        EditorPrefs.SetString(GetSelectionPrefsKey(), SelectedKey);
        Skin.UpdateNavigationStyles(NavEntries, SelectedKey);
        RefreshDetail();
    }

    protected bool MatchesSearch(string title, string subtitle)
    {
        if (string.IsNullOrWhiteSpace(SearchFilter))
        {
            return true;
        }

        var filter = SearchFilter.Trim();
        return (title != null && title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
               || (subtitle != null && subtitle.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    protected void AddNavButton(string title, string subtitle, string key)
    {
        if (!MatchesSearch(title, subtitle))
        {
            return;
        }

        NavListRoot.Add(Skin.CreateNavButton(title, subtitle, key, () => SelectNav(key), NavEntries));
    }

    protected void AddNavSection(string sectionTitle)
    {
        NavListRoot.Add(Skin.CreateSectionLabel(sectionTitle));
    }

    protected string MakeKey(string group, int index) => $"{group}:{index}";

    protected bool TryParseKey(string key, out string group, out int index)
    {
        group = string.Empty;
        index = -1;
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        var parts = key.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out index))
        {
            return false;
        }

        group = parts[0];
        return true;
    }

    protected SerializedProperty GetListProperty(string propertyName)
    {
        return TargetSo?.FindProperty(propertyName);
    }

    protected SerializedProperty GetListElement(string listPropertyName, int index)
    {
        var list = GetListProperty(listPropertyName);
        if (list == null || index < 0 || index >= list.arraySize)
        {
            return null;
        }

        return list.GetArrayElementAtIndex(index);
    }

    protected string GetStringProp(SerializedProperty parent, string name, string fallback = "")
    {
        var prop = parent?.FindPropertyRelative(name);
        return prop != null ? prop.stringValue ?? fallback : fallback;
    }

    protected void SaveAssets()
    {
        if (TargetSo == null || TargetAsset == null)
        {
            return;
        }

        TableNineConfigEditorAutoSave.Persist(TargetSo, TargetAsset);
    }

    protected void ValidateConfig()
    {
        TableNineGameConfigSync.ValidateMenu();
    }

    private void AddItem()
    {
        if (!CanAdd)
        {
            return;
        }

        OnAddItem();
        TableNineConfigEditorAutoSave.PersistIfEnabled(TargetSo, TargetAsset, immediateDisk: true);
        RebuildNavigation();
        RefreshDetail();
    }

    private void DeleteItem()
    {
        if (!CanDelete || string.IsNullOrEmpty(SelectedKey))
        {
            return;
        }

        if (!EditorUtility.DisplayDialog(WindowTitle, "确定删除当前选中条目？", "删除", "取消"))
        {
            return;
        }

        OnDeleteItem();
        SelectedKey = string.Empty;
        EditorPrefs.SetString(GetSelectionPrefsKey(), SelectedKey);
        TableNineConfigEditorAutoSave.PersistIfEnabled(TargetSo, TargetAsset, immediateDisk: true);
        RebuildNavigation();
        RefreshDetail();
    }

    private void OnDisable()
    {
        FlushAutoSaveBeforeContextChange();
    }

    private void FlushAutoSaveBeforeContextChange()
    {
        if (TargetSo == null || TargetAsset == null)
        {
            return;
        }

        TableNineConfigEditorAutoSave.PersistIfEnabled(TargetSo, TargetAsset, immediateDisk: true);
        TableNineConfigEditorAutoSave.FlushPending();
    }

    private void EnsureValidSelection()
    {
        if (NavEntries.Count == 0)
        {
            SelectedKey = string.Empty;
            return;
        }

        for (var i = 0; i < NavEntries.Count; i++)
        {
            if (NavEntries[i].Key == SelectedKey)
            {
                return;
            }
        }

        SelectedKey = NavEntries[0].Key;
        EditorPrefs.SetString(GetSelectionPrefsKey(), SelectedKey);
    }

    private void UpdateFooter()
    {
        if (SidebarFooterLabel != null)
        {
            SidebarFooterLabel.text =
                $"{GetTotalItemCount()} 条目 · {NavEntries.Count} 可见 · {TableNineConfigEditorAutoSave.GetFooterStatusLabel()}";
        }
    }

    private string GetSelectionPrefsKey() => $"TableNine.ConfigEditor.{EditorPrefsKey}.Selection";
    private string GetSearchPrefsKey() => $"TableNine.ConfigEditor.{EditorPrefsKey}.Search";
}
#endif
