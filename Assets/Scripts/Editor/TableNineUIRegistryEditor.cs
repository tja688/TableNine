using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using QFramework;
using static TableNineUI.Editor.UIPanelMetadataCatalog;

namespace TableNineUI.Editor
{
    /// <summary>
    /// TableNine UI 面板注册表 —— UI Toolkit 自定义编辑器窗口
    /// 侧边栏选择 + 右侧详情/配置的主从布局。
    /// </summary>
    public class TableNineUIRegistryEditorWindow : EditorWindow
    {
        private const string UssPath = "Assets/Scripts/Editor/Styles/TableNineUIRegistryEditorStyles.uss";
        private const string WindowTitle = "UI 面板管理";

        private const string PrefKeyPrefix = "TableNineUI.RegistryEditor.";
        private const string PrefSelectedKey = PrefKeyPrefix + "selectedKey";
        private const string PrefSearchKey = PrefKeyPrefix + "search";
        private const string GlobalFallbackKey = "__global_fallback__";

        private TableNineUIPanelRegistry mRegistry;
        private SerializedObject mSerializedObj;

        private VisualElement mRoot;
        private VisualElement mMainSplit;
        private VisualElement mSidebarList;
        private VisualElement mDetailPane;
        private VisualElement mStatsBar;
        private TextField mSearchField;

        private string mSelectedKey;
        private string mSearchText = "";
        private List<PanelEntryInfo> mAllEntries = new List<PanelEntryInfo>();

        private static readonly Category[] CategoryOrder =
        {
            Category.Hud, Category.Popup, Category.Choice,
            Category.Reward, Category.Shop, Category.Prompt,
            Category.Confirm, Category.Detail
        };

        [MenuItem("TableNine/UI 面板管理 %&u")]
        public static void OpenWindow()
        {
            var window = GetWindow<TableNineUIRegistryEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle, EditorGUIUtility.IconContent("d_RectTransform Icon").image);
            window.minSize = new Vector2(720, 480);
            window.Show();
            window.Focus();
        }

        public static void OpenWithRegistry(TableNineUIPanelRegistry registry)
        {
            OpenWindow();
            var window = GetWindow<TableNineUIRegistryEditorWindow>();
            window.SetRegistry(registry);
        }

        private void CreateGUI()
        {
            mRoot = rootVisualElement;
            mRoot.AddToClassList("registry-root");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (styleSheet != null)
                mRoot.styleSheets.Add(styleSheet);
            else
                ApplyInlineStyles(mRoot);

            mSelectedKey = EditorPrefs.GetString(PrefSelectedKey, "");
            mSearchText = EditorPrefs.GetString(PrefSearchKey, "");

            if (mRegistry == null)
                mRegistry = FindRegistryAsset();

            BuildShellUI();

            if (mRegistry != null)
            {
                mSerializedObj = new SerializedObject(mRegistry);
                RefreshAll();
            }
            else
            {
                ShowNotFoundInDetailPane();
            }
        }

        private void OnFocus()
        {
            if (mSerializedObj != null && mRegistry != null)
            {
                mSerializedObj.Update();
                RefreshAll();
            }
        }

        private void OnDisable()
        {
            if (mSerializedObj != null)
            {
                mSerializedObj.Dispose();
                mSerializedObj = null;
            }
        }

        private void SetRegistry(TableNineUIPanelRegistry registry)
        {
            if (mRegistry == registry && mSerializedObj != null) return;
            mRegistry = registry;
            mSerializedObj = registry != null ? new SerializedObject(registry) : null;
            RefreshAll();
        }

        // ═══════════════════════════════════════════
        //  Shell UI
        // ═══════════════════════════════════════════

        private void BuildShellUI()
        {
            mRoot.Clear();

            // 顶部栏
            var topBar = new VisualElement();
            topBar.AddToClassList("top-bar");

            var topLeft = new VisualElement();
            topLeft.AddToClassList("top-bar-left");
            var title = new Label("TableNine UI 面板注册表");
            title.AddToClassList("top-bar-title");
            topLeft.Add(title);
            var subtitle = new Label("Gameplay 事件 → UI Key → Prefab / Fallback");
            subtitle.AddToClassList("top-bar-subtitle");
            topLeft.Add(subtitle);
            topBar.Add(topLeft);

            mStatsBar = new VisualElement();
            mStatsBar.AddToClassList("top-bar-stats");
            topBar.Add(mStatsBar);

            var actions = new VisualElement();
            actions.AddToClassList("top-bar-actions");

            var syncBtn = new Button(OnSyncDefaults) { text = "同步默认值" };
            syncBtn.AddToClassList("toolbar-btn");
            syncBtn.AddToClassList("toolbar-btn--primary");
            syncBtn.tooltip = "将推荐的面板默认配置合并到注册表（不会覆盖已有 Prefab 引用）";
            actions.Add(syncBtn);

            var refreshBtn = new Button(RefreshAll) { text = "刷新" };
            refreshBtn.AddToClassList("toolbar-btn");
            refreshBtn.tooltip = "刷新编辑器显示";
            actions.Add(refreshBtn);

            var pingBtn = new Button(OnPingAsset) { text = "定位资产" };
            pingBtn.AddToClassList("toolbar-btn");
            pingBtn.tooltip = "在项目面板中高亮定位此注册表资产";
            actions.Add(pingBtn);

            topBar.Add(actions);
            mRoot.Add(topBar);

            // 主从布局
            mMainSplit = new VisualElement();
            mMainSplit.AddToClassList("main-split");
            mRoot.Add(mMainSplit);

            BuildSidebar();
            BuildDetailPaneShell();
        }

        private void BuildSidebar()
        {
            var sidebar = new VisualElement();
            sidebar.AddToClassList("sidebar");

            var searchWrap = new VisualElement();
            searchWrap.AddToClassList("sidebar-search-wrap");
            mSearchField = new TextField { value = mSearchText };
            mSearchField.AddToClassList("sidebar-search");
            mSearchField.RegisterValueChangedCallback(evt =>
            {
                mSearchText = evt.newValue ?? "";
                EditorPrefs.SetString(PrefSearchKey, mSearchText);
                RebuildSidebarList();
            });
            searchWrap.Add(mSearchField);
            sidebar.Add(searchWrap);

            var sidebarScroll = new ScrollView(ScrollViewMode.Vertical);
            sidebarScroll.AddToClassList("sidebar-scroll");
            mSidebarList = sidebarScroll;
            sidebar.Add(sidebarScroll);

            var footer = new VisualElement();
            footer.AddToClassList("sidebar-footer");

            var globalBtn = new VisualElement();
            globalBtn.AddToClassList("sidebar-global-btn");
            globalBtn.name = "global-fallback-btn";
            globalBtn.Add(new Label("\u2699") { name = "icon" });
            globalBtn.Q<Label>("icon").AddToClassList("sidebar-global-btn-icon");
            globalBtn.Add(new Label("全局 Fallback 预制件") { name = "label" });
            globalBtn.Q<Label>("label").AddToClassList("sidebar-global-btn-label");
            globalBtn.RegisterCallback<ClickEvent>(_ => SelectGlobalFallback());
            footer.Add(globalBtn);

            sidebar.Add(footer);
            mMainSplit.Add(sidebar);
        }

        private void BuildDetailPaneShell()
        {
            mDetailPane = new VisualElement();
            mDetailPane.AddToClassList("detail-pane");
            mMainSplit.Add(mDetailPane);
        }

        // ═══════════════════════════════════════════
        //  Refresh
        // ═══════════════════════════════════════════

        private void RefreshAll()
        {
            if (mRegistry == null)
            {
                mRegistry = FindRegistryAsset();
                if (mRegistry == null)
                {
                    ShowNotFoundInDetailPane();
                    return;
                }
                mSerializedObj = new SerializedObject(mRegistry);
            }

            mSerializedObj.Update();
            CollectEntries();
            UpdateStats();
            RebuildSidebarList();
            RebuildDetailPane();
        }

        private void CollectEntries()
        {
            mAllEntries.Clear();
            var panelsProp = mSerializedObj.FindProperty("mPanels");
            if (panelsProp == null) return;

            for (int i = 0; i < panelsProp.arraySize; i++)
            {
                var entry = panelsProp.GetArrayElementAtIndex(i);
                var uiKeyProp = entry.FindPropertyRelative("UIKey");
                var uiKey = uiKeyProp != null ? uiKeyProp.stringValue : "";

                mAllEntries.Add(new PanelEntryInfo
                {
                    Index = i,
                    Property = entry,
                    UIKey = uiKey,
                    Meta = GetEntryMeta(uiKey)
                });
            }
        }

        private void UpdateStats()
        {
            if (mStatsBar == null) return;
            mStatsBar.Clear();

            int formal = 0, fallback = 0, missing = 0;
            foreach (var info in mAllEntries)
            {
                switch (GetPanelStatus(info.Property))
                {
                    case "formal": formal++; break;
                    case "fallback": fallback++; break;
                    default: missing++; break;
                }
            }

            AddStatChip(mStatsBar, "总计", mAllEntries.Count.ToString(), "total");
            AddStatChip(mStatsBar, "已接入", formal.ToString(), "formal");
            AddStatChip(mStatsBar, "Fallback", fallback.ToString(), "fallback");
            if (missing > 0)
                AddStatChip(mStatsBar, "未配置", missing.ToString(), "missing");
        }

        private void AddStatChip(VisualElement parent, string label, string value, string modifier)
        {
            var chip = new VisualElement();
            chip.AddToClassList("stat-chip");

            var dot = new VisualElement();
            dot.AddToClassList("stat-dot");
            dot.AddToClassList($"stat-dot--{modifier}");
            chip.Add(dot);

            chip.Add(new Label(label) { name = "lbl" });
            chip.Q<Label>("lbl").AddToClassList("stat-chip-label");
            chip.Add(new Label(value) { name = "val" });
            chip.Q<Label>("val").AddToClassList("stat-chip-value");

            parent.Add(chip);
        }

        // ═══════════════════════════════════════════
        //  Sidebar
        // ═══════════════════════════════════════════

        private void RebuildSidebarList()
        {
            if (mSidebarList == null) return;
            mSidebarList.Clear();

            var filter = (mSearchText ?? "").Trim().ToLowerInvariant();
            var grouped = new Dictionary<Category, List<PanelEntryInfo>>();

            foreach (var info in mAllEntries)
            {
                if (!MatchesFilter(info, filter)) continue;

                var cat = info.Meta.Category;
                if (!grouped.ContainsKey(cat))
                    grouped[cat] = new List<PanelEntryInfo>();
                grouped[cat].Add(info);
            }

            bool anyVisible = false;
            foreach (var cat in CategoryOrder)
            {
                if (!grouped.TryGetValue(cat, out var entries) || entries.Count == 0)
                    continue;

                anyVisible = true;
                BuildSidebarCategory(cat, entries);
            }

            if (!anyVisible)
            {
                var empty = new Label(string.IsNullOrEmpty(filter) ? "暂无面板条目" : "无匹配结果");
                empty.style.paddingLeft = empty.style.paddingRight = 12;
                empty.style.paddingTop = empty.style.paddingBottom = 16;
                empty.style.color = new Color(0.5f, 0.5f, 0.55f);
                empty.style.fontSize = 11;
                mSidebarList.Add(empty);
            }

            UpdateSidebarSelectionHighlight();
        }

        private bool MatchesFilter(PanelEntryInfo info, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            return (info.UIKey ?? "").ToLowerInvariant().Contains(filter)
                   || (info.Meta.ChineseName ?? "").ToLowerInvariant().Contains(filter)
                   || (info.Meta.ChineseDescription ?? "").ToLowerInvariant().Contains(filter);
        }

        private void BuildSidebarCategory(Category cat, List<PanelEntryInfo> entries)
        {
            if (!Categories.TryGetValue(cat, out var catMeta)) return;

            var section = new VisualElement();
            section.AddToClassList("sidebar-category");

            var header = new VisualElement();
            header.AddToClassList("sidebar-category-header");
            header.Add(new Label(catMeta.Icon) { name = "icon" });
            header.Q<Label>("icon").AddToClassList("sidebar-category-icon");
            header.Add(new Label(catMeta.ChineseName.ToUpperInvariant()) { name = "name" });
            header.Q<Label>("name").AddToClassList("sidebar-category-name");
            header.Add(new Label(entries.Count.ToString()) { name = "count" });
            header.Q<Label>("count").AddToClassList("sidebar-category-count");
            section.Add(header);

            foreach (var info in entries)
            {
                var item = new VisualElement();
                item.AddToClassList("sidebar-item");
                item.userData = info.UIKey;

                var status = GetPanelStatus(info.Property);
                var statusDot = new VisualElement();
                statusDot.AddToClassList("sidebar-item-status");
                statusDot.AddToClassList($"sidebar-item-status--{status}");
                item.Add(statusDot);

                var textWrap = new VisualElement();
                textWrap.AddToClassList("sidebar-item-text");
                textWrap.Add(new Label(info.Meta.ChineseName) { name = "name" });
                textWrap.Q<Label>("name").AddToClassList("sidebar-item-name");
                textWrap.Add(new Label(info.UIKey) { name = "key" });
                textWrap.Q<Label>("key").AddToClassList("sidebar-item-key");
                item.Add(textWrap);

                item.RegisterCallback<ClickEvent>(_ => SelectPanel(info.UIKey));
                section.Add(item);
            }

            mSidebarList.Add(section);
        }

        private void SelectPanel(string uiKey)
        {
            mSelectedKey = uiKey;
            EditorPrefs.SetString(PrefSelectedKey, uiKey);
            UpdateSidebarSelectionHighlight();
            RebuildDetailPane();
        }

        private void SelectGlobalFallback()
        {
            mSelectedKey = GlobalFallbackKey;
            EditorPrefs.SetString(PrefSelectedKey, GlobalFallbackKey);
            UpdateSidebarSelectionHighlight();
            RebuildDetailPane();
        }

        private void UpdateSidebarSelectionHighlight()
        {
            if (mSidebarList == null) return;

            mSidebarList.Query(className: "sidebar-item").ForEach(item =>
            {
                var key = item.userData as string;
                item.EnableInClassList("sidebar-item--selected", key == mSelectedKey);
            });

            var globalBtn = mMainSplit?.Q("global-fallback-btn");
            if (globalBtn != null)
                globalBtn.EnableInClassList("sidebar-global-btn--selected", mSelectedKey == GlobalFallbackKey);
        }

        private void EnsureValidSelection()
        {
            if (mSelectedKey == GlobalFallbackKey) return;

            if (!string.IsNullOrEmpty(mSelectedKey) && mAllEntries.Any(e => e.UIKey == mSelectedKey))
                return;

            mSelectedKey = mAllEntries.Count > 0 ? mAllEntries[0].UIKey : "";
            EditorPrefs.SetString(PrefSelectedKey, mSelectedKey);
        }

        // ═══════════════════════════════════════════
        //  Detail Pane
        // ═══════════════════════════════════════════

        private void RebuildDetailPane()
        {
            if (mDetailPane == null) return;
            mDetailPane.Clear();

            if (mRegistry == null || mSerializedObj == null)
            {
                ShowNotFoundInDetailPane();
                return;
            }

            EnsureValidSelection();

            if (mSelectedKey == GlobalFallbackKey)
            {
                BuildGlobalFallbackDetail();
                return;
            }

            if (string.IsNullOrEmpty(mSelectedKey))
            {
                ShowEmptySelection();
                return;
            }

            var info = mAllEntries.FirstOrDefault(e => e.UIKey == mSelectedKey);
            if (info.UIKey == null)
            {
                ShowEmptySelection();
                return;
            }

            BuildPanelDetail(info);
        }

        private void ShowEmptySelection()
        {
            var empty = new VisualElement();
            empty.AddToClassList("detail-empty");
            empty.Add(new Label("\u25a1") { name = "icon" });
            empty.Q<Label>("icon").AddToClassList("detail-empty-icon");
            empty.Add(new Label("请从左侧选择一个面板") { name = "title" });
            empty.Q<Label>("title").AddToClassList("detail-empty-title");
            empty.Add(new Label("选择后可查看中文说明、设计备注和接入配置") { name = "hint" });
            empty.Q<Label>("hint").AddToClassList("detail-empty-hint");
            mDetailPane.Add(empty);
        }

        private void BuildPanelDetail(PanelEntryInfo info)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("detail-scroll");

            var status = GetPanelStatus(info.Property);
            Categories.TryGetValue(info.Meta.Category, out var catMeta);

            // ── 头部 ──
            var header = new VisualElement();
            header.AddToClassList("detail-header");

            var headerTop = new VisualElement();
            headerTop.AddToClassList("detail-header-top");

            var headerMain = new VisualElement();
            headerMain.AddToClassList("detail-header-main");
            headerMain.Add(new Label(info.Meta.ChineseName) { name = "title" });
            headerMain.Q<Label>("title").AddToClassList("detail-title");
            headerMain.Add(new Label(info.UIKey) { name = "key" });
            headerMain.Q<Label>("key").AddToClassList("detail-ui-key");
            headerTop.Add(headerMain);

            var badge = new Label(GetStatusBadgeText(status));
            badge.AddToClassList("detail-badge");
            badge.AddToClassList($"detail-badge--{status}");
            badge.tooltip = GetStatusTooltip(status);
            headerTop.Add(badge);
            header.Add(headerTop);

            if (catMeta != null)
            {
                var catTag = new VisualElement();
                catTag.AddToClassList("detail-category-tag");
                catTag.Add(new Label(catMeta.Icon) { name = "icon" });
                catTag.Q<Label>("icon").AddToClassList("detail-category-tag-icon");
                catTag.Add(new Label(catMeta.ChineseName) { name = "text" });
                catTag.Q<Label>("text").AddToClassList("detail-category-tag-text");
                header.Add(catTag);
            }

            scroll.Add(header);

            // ── 面板信息卡片 ──
            scroll.Add(BuildInfoCard(info));

            // ── 接入配置卡片 ──
            scroll.Add(BuildConfigCard(info));

            // ── Fallback 配置卡片 ──
            scroll.Add(BuildFallbackConfigCard(info));

            // ── 备注卡片 ──
            var notesProp = info.Property.FindPropertyRelative("Notes");
            if (notesProp != null)
                scroll.Add(BuildNotesCard(notesProp));

            scroll.Bind(mSerializedObj);
            mDetailPane.Add(scroll);
        }

        private VisualElement BuildInfoCard(PanelEntryInfo info)
        {
            var card = CreateCard("\u2139", "面板信息");

            var desc = new Label(info.Meta.ChineseDescription);
            desc.AddToClassList("detail-desc-block");
            desc.style.whiteSpace = WhiteSpace.Normal;
            card.body.Add(desc);

            var grid = new VisualElement();
            grid.AddToClassList("detail-meta-grid");

            if (!string.IsNullOrEmpty(info.Meta.TriggerEvent))
                grid.Add(CreateMetaRow("触发事件", info.Meta.TriggerEvent));
            if (!string.IsNullOrEmpty(info.Meta.ExpectedElements))
                grid.Add(CreateMetaRow("期望 UI 元素", info.Meta.ExpectedElements));
            if (!string.IsNullOrEmpty(info.Meta.DesignNote))
                grid.Add(CreateMetaRow("设计备注", info.Meta.DesignNote));

            card.body.Add(grid);
            return card.root;
        }

        private VisualElement BuildConfigCard(PanelEntryInfo info)
        {
            var card = CreateCard("\u2699", "接入配置");
            var entryProp = info.Property;

            var uiKeyProp = entryProp.FindPropertyRelative("UIKey");
            if (uiKeyProp != null)
            {
                var field = new PropertyField(uiKeyProp, "UI Key");
                field.SetEnabled(false);
                field.AddToClassList("detail-field-value");
                card.body.Add(WrapField(field));
            }

            var uiTypeProp = entryProp.FindPropertyRelative("UIType");
            if (uiTypeProp != null)
            {
                var typeEnum = (TableNineUIType)uiTypeProp.intValue;
                card.body.Add(WrapFieldWithHint(
                    new PropertyField(uiTypeProp, "UI 类型"),
                    GetUITypeLabel(typeEnum)));
            }

            var panelNameProp = entryProp.FindPropertyRelative("PanelName");
            if (panelNameProp != null)
                card.body.Add(WrapField(new PropertyField(panelNameProp, "面板名")));

            var prefabProp = entryProp.FindPropertyRelative("Prefab");
            if (prefabProp != null)
                card.body.Add(WrapField(new PropertyField(prefabProp, "正式 Prefab")));

            var twoCol = new VisualElement();
            twoCol.AddToClassList("detail-two-col");

            var levelProp = entryProp.FindPropertyRelative("Level");
            if (levelProp != null)
            {
                var levelEnum = (UILevel)levelProp.intValue;
                twoCol.Add(WrapFieldWithHint(
                    new PropertyField(levelProp, "层级"),
                    GetUILevelLabel(levelEnum)));
            }

            var openTypeProp = entryProp.FindPropertyRelative("OpenType");
            if (openTypeProp != null)
            {
                var openEnum = (PanelOpenType)openTypeProp.intValue;
                twoCol.Add(WrapFieldWithHint(
                    new PropertyField(openTypeProp, "打开方式"),
                    GetOpenTypeLabel(openEnum)));
            }

            card.body.Add(twoCol);
            return card.root;
        }

        private VisualElement BuildFallbackConfigCard(PanelEntryInfo info)
        {
            var card = CreateCard("\u21bb", "Fallback 配置");
            var entryProp = info.Property;

            var fallbackStrategyProp = entryProp.FindPropertyRelative("FallbackStrategy");
            if (fallbackStrategyProp != null)
            {
                var stratEnum = (TableNineUIFallbackStrategy)fallbackStrategyProp.intValue;
                card.body.Add(WrapFieldWithHint(
                    new PropertyField(fallbackStrategyProp, "回退策略"),
                    GetFallbackStrategyDescription(stratEnum)));
            }

            var blocksProp = entryProp.FindPropertyRelative("BlocksGameplayInput");
            if (blocksProp != null)
            {
                card.body.Add(WrapField(new PropertyField(blocksProp, "阻止操作")));
                var blocksDesc = new Label(blocksProp.boolValue
                    ? "阻止底层交互（全屏遮罩）"
                    : "不阻止底层交互");
                blocksDesc.AddToClassList("detail-field-hint");
                card.body.Add(blocksDesc);
            }

            return card.root;
        }

        private VisualElement BuildNotesCard(SerializedProperty notesProp)
        {
            var card = CreateCard("\u270E", "备注");
            var field = new PropertyField(notesProp, "");
            field.AddToClassList("detail-field-value");
            card.body.Add(field);
            return card.root;
        }

        private void BuildGlobalFallbackDetail()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("detail-scroll");

            var header = new VisualElement();
            header.AddToClassList("global-fallback-header");
            header.Add(new Label("全局 Fallback 预制件") { name = "title" });
            header.Q<Label>("title").AddToClassList("global-fallback-title");
            var desc = new Label("组件化预制件，供 Fallback 面板运行时实例化。为空时回退到纯代码生成。");
            desc.AddToClassList("global-fallback-desc");
            desc.style.whiteSpace = WhiteSpace.Normal;
            header.Add(desc);
            scroll.Add(header);

            var card = CreateCard("\u2699", "预制件引用");

            string[] fields = { "mFallbackButtonPrefab", "mFallbackTextPrefab", "mFallbackIconPrefab", "mFallbackPanelPrefab", "mFallbackScrollViewPrefab" };
            string[] labels = { "按钮预制件", "文本预制件", "图标预制件", "面板预制件", "滚动视图预制件" };
            string[] hints = {
                "Fallback 面板中所有按钮的模板",
                "Fallback 面板中所有文本的模板",
                "Fallback 面板中图标的模板",
                "整体面板模板（可选）",
                "滚动列表模板（可选）"
            };

            for (int i = 0; i < fields.Length; i++)
            {
                var prop = mSerializedObj.FindProperty(fields[i]);
                if (prop == null) continue;

                var block = new VisualElement();
                block.AddToClassList("fallback-field-block");

                var field = new PropertyField(prop, labels[i]);
                field.AddToClassList("detail-field-value");
                block.Add(field);

                var hint = new Label(hints[i]);
                hint.AddToClassList("detail-field-hint");
                block.Add(hint);

                card.body.Add(block);
            }

            scroll.Add(card.root);
            scroll.Bind(mSerializedObj);
            mDetailPane.Add(scroll);
        }

        // ═══════════════════════════════════════════
        //  UI Helpers
        // ═══════════════════════════════════════════

        private struct CardParts
        {
            public VisualElement root;
            public VisualElement body;
        }

        private CardParts CreateCard(string icon, string title)
        {
            var root = new VisualElement();
            root.AddToClassList("detail-card");

            var header = new VisualElement();
            header.AddToClassList("detail-card-header");
            header.Add(new Label(icon) { name = "icon" });
            header.Q<Label>("icon").AddToClassList("detail-card-icon");
            header.Add(new Label(title) { name = "title" });
            header.Q<Label>("title").AddToClassList("detail-card-title");
            root.Add(header);

            var body = new VisualElement();
            body.AddToClassList("detail-card-body");
            root.Add(body);

            return new CardParts { root = root, body = body };
        }

        private VisualElement WrapField(PropertyField field)
        {
            field.Bind(mSerializedObj);
            field.AddToClassList("detail-field-value");
            var row = new VisualElement();
            row.AddToClassList("detail-field-row");
            row.Add(field);
            return row;
        }

        private VisualElement WrapFieldWithHint(PropertyField field, string hint)
        {
            var wrap = new VisualElement();
            wrap.AddToClassList("detail-field-group");
            wrap.Add(WrapField(field));
            if (!string.IsNullOrEmpty(hint))
            {
                var hintLabel = new Label(hint);
                hintLabel.AddToClassList("detail-field-hint");
                hintLabel.style.whiteSpace = WhiteSpace.Normal;
                wrap.Add(hintLabel);
            }
            return wrap;
        }

        private VisualElement CreateMetaRow(string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("detail-meta-row");
            row.Add(new Label(label) { name = "lbl" });
            row.Q<Label>("lbl").AddToClassList("detail-meta-label");
            var val = new Label(value);
            val.AddToClassList("detail-meta-value");
            val.style.whiteSpace = WhiteSpace.Normal;
            row.Add(val);
            return row;
        }

        // ═══════════════════════════════════════════
        //  Toolbar
        // ═══════════════════════════════════════════

        private void OnSyncDefaults()
        {
            if (mRegistry == null) return;

            Undo.RecordObject(mRegistry, "Sync UI Registry Defaults");
            mRegistry.ApplyRecommendedDefaults();
            EditorUtility.SetDirty(mRegistry);
            AssetDatabase.SaveAssets();
            RefreshAll();
            Debug.Log("[UI 面板管理] 已同步推荐默认值。");
        }

        private void OnPingAsset()
        {
            if (mRegistry == null) return;
            EditorGUIUtility.PingObject(mRegistry);
            Selection.activeObject = mRegistry;
        }

        // ═══════════════════════════════════════════
        //  Status & Asset
        // ═══════════════════════════════════════════

        private TableNineUIPanelRegistry FindRegistryAsset()
        {
            var guids = AssetDatabase.FindAssets("t:TableNineUIPanelRegistry");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TableNineUIPanelRegistry>(path);
            }
            return null;
        }

        private string GetPanelStatus(SerializedProperty entryProp)
        {
            var prefab = entryProp.FindPropertyRelative("Prefab");
            var fallback = entryProp.FindPropertyRelative("FallbackStrategy");

            if (prefab != null && prefab.objectReferenceValue != null)
                return "formal";
            if (fallback != null && fallback.intValue > 0)
                return "fallback";
            return "missing";
        }

        private string GetStatusBadgeText(string status)
        {
            switch (status)
            {
                case "formal": return "\u2714 已接入正式 Prefab";
                case "fallback": return "\u25CB 使用 Fallback";
                default: return "\u2716 未配置";
            }
        }

        private string GetStatusTooltip(string status)
        {
            switch (status)
            {
                case "formal": return "已绑定正式 Prefab，使用正式 UI";
                case "fallback": return "未绑定正式 Prefab，当前使用 Fallback 原子组件";
                default: return "未配置 Prefab 且无 Fallback 策略";
            }
        }

        private void ShowNotFoundInDetailPane()
        {
            if (mDetailPane == null) return;
            mDetailPane.Clear();

            var wrap = new VisualElement();
            wrap.AddToClassList("not-found-wrap");

            wrap.Add(new Label("未找到 TableNineUIPanelRegistry 资产") { name = "title" });
            wrap.Q<Label>("title").AddToClassList("not-found-title");
            wrap.Add(new Label("请确认 Assets/ScriptableObjects/TableNineUIPanelRegistry.asset 存在。") { name = "hint" });
            wrap.Q<Label>("hint").AddToClassList("not-found-hint");

            var createBtn = new Button(() =>
            {
                CreateRegistryAsset();
                RefreshAll();
            }) { text = "创建注册表资产" };
            createBtn.AddToClassList("not-found-btn");
            wrap.Add(createBtn);

            mDetailPane.Add(wrap);
        }

        private void CreateRegistryAsset()
        {
            var registry = CreateInstance<TableNineUIPanelRegistry>();
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

            AssetDatabase.CreateAsset(registry, "Assets/ScriptableObjects/TableNineUIPanelRegistry.asset");
            AssetDatabase.SaveAssets();
            mRegistry = registry;
            mSerializedObj = new SerializedObject(registry);
        }

        private void ApplyInlineStyles(VisualElement root)
        {
            root.style.backgroundColor = new Color(0.118f, 0.118f, 0.133f);
            root.style.color = new Color(0.9f, 0.9f, 0.9f);
        }

        private struct PanelEntryInfo
        {
            public int Index;
            public SerializedProperty Property;
            public string UIKey;
            public PanelMeta Meta;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  ScriptableObject Inspector 入口
    // ═══════════════════════════════════════════════════════

#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.HideReferenceObjectPicker]
#endif
    [CustomEditor(typeof(TableNineUIPanelRegistry))]
    public class TableNineUIRegistryInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.style.paddingLeft = root.style.paddingRight = 12;
            root.style.paddingTop = root.style.paddingBottom = 12;

            var title = new Label("TableNine UI 面板注册表");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.92f, 0.92f, 0.94f);
            title.style.marginBottom = 4;
            root.Add(title);

            var desc = new Label("打开可视化面板管理窗口：左侧选择面板，右侧查看说明与接入配置。");
            desc.style.fontSize = 11;
            desc.style.color = new Color(0.65f, 0.65f, 0.7f);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = 12;
            root.Add(desc);

            var openBtn = new Button(() =>
            {
                TableNineUIRegistryEditorWindow.OpenWithRegistry(target as TableNineUIPanelRegistry);
            })
            {
                text = "打开 UI 面板管理窗口"
            };
            openBtn.style.height = 32;
            openBtn.style.fontSize = 12;
            openBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            openBtn.style.backgroundColor = new Color(0.24f, 0.36f, 0.52f);
            openBtn.style.color = Color.white;
            openBtn.style.borderTopLeftRadius = openBtn.style.borderTopRightRadius = 4;
            openBtn.style.borderBottomLeftRadius = openBtn.style.borderBottomRightRadius = 4;
            openBtn.style.marginBottom = 12;
            root.Add(openBtn);

            var registry = target as TableNineUIPanelRegistry;
            if (registry != null)
            {
                var so = new SerializedObject(registry);
                var panelsProp = so.FindProperty("mPanels");
                if (panelsProp != null)
                {
                    int total = panelsProp.arraySize;
                    int formal = 0;
                    for (int i = 0; i < total; i++)
                    {
                        var prefab = panelsProp.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab");
                        if (prefab != null && prefab.objectReferenceValue != null) formal++;
                    }

                    var statsLabel = new Label($"共 {total} 个面板  ·  {formal} 个已接入  ·  {total - formal} 个 Fallback/未配置");
                    statsLabel.style.fontSize = 11;
                    statsLabel.style.color = new Color(0.55f, 0.55f, 0.6f);
                    root.Add(statsLabel);
                }
            }

            return root;
        }
    }
}
