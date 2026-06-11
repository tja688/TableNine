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
    /// TableNine UI 面板注册表 —— 暖棕复古控制台风格编辑器窗口。
    /// </summary>
    public class TableNineUIRegistryEditorWindow : EditorWindow
    {
        private const string WindowTitle = "UI 面板管理";

        private const string PrefKeyPrefix = "TableNineUI.RegistryEditor.";
        private const string PrefSelectedKey = PrefKeyPrefix + "selectedKey";
        private const string PrefSearchKey = PrefKeyPrefix + "search";
        private const string GlobalFallbackKey = "__global_fallback__";

        private TableNineUIPanelRegistry mRegistry;
        private SerializedObject mSerializedObj;

        private VisualElement mRoot;
        private VisualElement mSidebarList;
        private ScrollView mDetailScroll;
        private TextField mSearchField;

        private readonly List<NavButtonEntry> mNavButtons = new List<NavButtonEntry>();

        private string mSelectedKey;
        private string mSearchText = "";
        private List<PanelEntryInfo> mAllEntries = new List<PanelEntryInfo>();

        private static readonly Category[] CategoryOrder =
        {
            Category.Hud, Category.Popup, Category.Choice,
            Category.Reward, Category.Shop, Category.Prompt,
            Category.Confirm, Category.Detail
        };

        // ═══════════════════════════════════════════
        //  Theme — RGB 0~1
        // ═══════════════════════════════════════════

        private static class Theme
        {
            public static readonly Color RootBg = new Color(0.10f, 0.085f, 0.07f);
            public static readonly Color ContentBg = new Color(0.09f, 0.075f, 0.06f);
            public static readonly Color SidebarBg = new Color(0.12f, 0.095f, 0.08f);
            public static readonly Color HeaderBg = new Color(0.13f, 0.10f, 0.08f);
            public static readonly Color StatCardBg = new Color(0.16f, 0.12f, 0.09f);
            public static readonly Color SectionCardBg = new Color(0.15f, 0.12f, 0.095f);

            public static readonly Color AccentStrong = new Color(0.86f, 0.64f, 0.28f);
            public static readonly Color AccentGoldValue = new Color(0.94f, 0.75f, 0.40f);
            public static readonly Color AccentMid = new Color(0.83f, 0.62f, 0.26f);
            public static readonly Color AccentWeak = new Color(0.56f, 0.40f, 0.18f);

            public static readonly Color TextPrimary = new Color(0.95f, 0.89f, 0.79f);
            public static readonly Color TextSecondary = new Color(0.79f, 0.73f, 0.67f);
            public static readonly Color TextTertiary = new Color(0.78f, 0.72f, 0.68f);
            public static readonly Color TextPath = new Color(0.73f, 0.70f, 0.66f);
            public static readonly Color TextChecklist = new Color(0.83f, 0.78f, 0.72f);

            public static readonly Color Divider = new Color(0.22f, 0.18f, 0.14f);

            public static readonly Color NavNormalBg = new Color(0.18f, 0.14f, 0.11f);
            public static readonly Color NavSelectedBg = new Color(0.31f, 0.22f, 0.12f);
            public static readonly Color NavStripeNormal = new Color(0.20f, 0.16f, 0.13f);
        }

        private struct NavButtonEntry
        {
            public VisualElement Button;
            public VisualElement Stripe;
            public string Key;
        }

        [MenuItem("TableNine/UI 面板管理 %&u")]
        public static void OpenWindow()
        {
            var window = GetWindow<TableNineUIRegistryEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle, EditorGUIUtility.IconContent("d_RectTransform Icon").image);
            window.minSize = new Vector2(1180, 760);
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
            mRoot.style.flexGrow = 1;
            mRoot.style.backgroundColor = Theme.RootBg;

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
                RebuildDetailPane();
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
        //  Style Factories
        // ═══════════════════════════════════════════

        private static Label CreateTitleLabel(string text, int fontSize, bool bold, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            if (bold)
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private static Label CreateDescriptionLabel(string text)
        {
            return CreateTitleLabel(text, 11, false, Theme.TextSecondary);
        }

        private static Label CreateChecklistLabel(string text)
        {
            var label = CreateTitleLabel("• " + text, 11, false, Theme.TextChecklist);
            label.style.marginBottom = 6;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Label CreateTinyPathLabel(string text)
        {
            var label = CreateTitleLabel(text, 11, false, Theme.TextPath);
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static VisualElement WrapControl(string label, string description, VisualElement field)
        {
            var row = new VisualElement();
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            row.style.marginBottom = 4;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = Theme.Divider;

            row.Add(CreateTitleLabel(label, 13, true, Theme.TextPrimary));

            if (!string.IsNullOrEmpty(description))
            {
                var desc = CreateDescriptionLabel(description);
                desc.style.marginTop = 3;
                desc.style.marginBottom = 6;
                row.Add(desc);
            }

            field.style.flexGrow = 1;
            row.Add(field);
            return row;
        }

        private VisualElement WrapProperty(string label, string description, SerializedProperty prop, bool readOnly = false)
        {
            var field = new PropertyField(prop, "");
            field.Bind(mSerializedObj);
            if (readOnly)
                field.SetEnabled(false);
            return WrapControl(label, description, field);
        }

        private VisualElement CreateStatCard(string title, string value, string description)
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.width = 250;
            card.style.marginRight = 10;
            card.style.marginBottom = 10;
            card.style.backgroundColor = Theme.StatCardBg;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
            card.style.overflow = Overflow.Hidden;

            var stripe = new VisualElement();
            stripe.style.width = 3;
            stripe.style.backgroundColor = Theme.AccentMid;
            card.Add(stripe);

            var body = new VisualElement();
            body.style.flexGrow = 1;
            body.style.paddingTop = 12;
            body.style.paddingBottom = 10;
            body.style.paddingLeft = 12;
            body.style.paddingRight = 10;

            body.Add(CreateTitleLabel(title, 12, true, Theme.TextPrimary));
            var val = CreateTitleLabel(value, 20, true, Theme.AccentGoldValue);
            val.style.marginTop = 4;
            body.Add(val);
            var desc = CreateDescriptionLabel(description);
            desc.style.marginTop = 4;
            body.Add(desc);

            card.Add(body);
            return card;
        }

        private VisualElement CreateStatsGrid(int total, int formal, int fallback, int missing)
        {
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginBottom = 16;

            grid.Add(CreateStatCard("面板总计", total.ToString(), "注册表中的 UI 面板条目数"));
            grid.Add(CreateStatCard("已接入", formal.ToString(), "已绑定正式 Prefab 的面板"));
            grid.Add(CreateStatCard("Fallback", fallback.ToString(), "未绑 Prefab，使用原子回退"));
            if (missing > 0)
                grid.Add(CreateStatCard("未配置", missing.ToString(), "无 Prefab 且无有效回退策略"));

            return grid;
        }

        private VisualElement CreateSectionCard(string title, string description, Action<VisualElement> buildContent)
        {
            var outer = new VisualElement();
            outer.style.flexDirection = FlexDirection.Row;
            outer.style.marginBottom = 12;
            outer.style.backgroundColor = Theme.SectionCardBg;
            outer.style.borderTopLeftRadius = outer.style.borderTopRightRadius = 8;
            outer.style.borderBottomLeftRadius = outer.style.borderBottomRightRadius = 8;
            outer.style.overflow = Overflow.Hidden;

            var stripe = new VisualElement();
            stripe.style.width = 3;
            stripe.style.backgroundColor = Theme.AccentWeak;
            outer.Add(stripe);

            var inner = new VisualElement();
            inner.style.flexGrow = 1;
            inner.style.paddingTop = 8;
            inner.style.paddingBottom = 10;
            inner.style.paddingLeft = 8;
            inner.style.paddingRight = 10;

            var foldout = new Foldout { text = title, value = true };
            foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
            foldout.style.fontSize = 13;
            foldout.style.color = Theme.TextPrimary;
            inner.Add(foldout);

            if (!string.IsNullOrEmpty(description))
            {
                var desc = CreateDescriptionLabel(description);
                desc.style.marginLeft = 4;
                desc.style.marginTop = 6;
                desc.style.marginBottom = 8;
                foldout.contentContainer.Add(desc);
            }

            var column = new VisualElement();
            column.style.flexDirection = FlexDirection.Column;
            buildContent?.Invoke(column);
            foldout.contentContainer.Add(column);

            inner.Add(foldout);
            outer.Add(inner);
            return outer;
        }

        private VisualElement CreateNavButton(string title, string description, string key, Action onClick)
        {
            var btn = new VisualElement();
            btn.style.flexDirection = FlexDirection.Row;
            btn.style.backgroundColor = Theme.NavNormalBg;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 6;
            btn.style.marginBottom = 8;
            btn.style.overflow = Overflow.Hidden;
            btn.userData = key;

            var stripe = new VisualElement();
            stripe.style.width = 4;
            stripe.style.backgroundColor = Theme.NavStripeNormal;
            btn.Add(stripe);

            var content = new VisualElement();
            content.style.flexGrow = 1;
            content.style.paddingTop = 10;
            content.style.paddingBottom = 10;
            content.style.paddingLeft = 10;
            content.style.paddingRight = 10;
            content.style.justifyContent = Justify.Center;

            var titleLabel = CreateTitleLabel(title, 14, true, Theme.TextPrimary);
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            content.Add(titleLabel);

            if (!string.IsNullOrEmpty(description))
            {
                var descLabel = CreateDescriptionLabel(description);
                descLabel.style.marginTop = 4;
                descLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                content.Add(descLabel);
            }

            btn.Add(content);
            btn.RegisterCallback<ClickEvent>(_ => onClick?.Invoke());

            mNavButtons.Add(new NavButtonEntry { Button = btn, Stripe = stripe, Key = key });
            return btn;
        }

        private void UpdateNavigationStyles()
        {
            foreach (var entry in mNavButtons)
            {
                bool selected = entry.Key == mSelectedKey;
                entry.Button.style.backgroundColor = selected ? Theme.NavSelectedBg : Theme.NavNormalBg;
                entry.Stripe.style.backgroundColor = selected ? Theme.AccentStrong : Theme.NavStripeNormal;
            }
        }

        private VisualElement CreateButtonRow(params Button[] buttons)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginBottom = 12;

            foreach (var btn in buttons)
            {
                btn.style.height = 28;
                btn.style.marginRight = 8;
                btn.style.marginBottom = 8;
                row.Add(btn);
            }

            return row;
        }

        private VisualElement CreatePageHeader(string title, string description)
        {
            var block = new VisualElement();
            block.style.marginBottom = 14;

            var titleLabel = CreateTitleLabel(title, 24, true, new Color(0.95f, 0.90f, 0.80f));
            block.Add(titleLabel);

            if (!string.IsNullOrEmpty(description))
            {
                var desc = CreateTitleLabel(description, 12, false, new Color(0.80f, 0.74f, 0.67f));
                desc.style.marginTop = 6;
                desc.style.marginBottom = 14;
                desc.style.whiteSpace = WhiteSpace.Normal;
                block.Add(desc);
            }

            return block;
        }

        private HelpBox CreateStatusHelpBox(string status)
        {
            HelpBoxMessageType type;
            switch (status)
            {
                case "formal":
                    type = HelpBoxMessageType.Info;
                    break;
                case "fallback":
                    type = HelpBoxMessageType.Warning;
                    break;
                default:
                    type = HelpBoxMessageType.Error;
                    break;
            }

            return new HelpBox(GetStatusBadgeText(status) + " — " + GetStatusTooltip(status), type);
        }

        // ═══════════════════════════════════════════
        //  Shell UI
        // ═══════════════════════════════════════════

        private void BuildShellUI()
        {
            mRoot.Clear();
            mNavButtons.Clear();

            // 1) Header
            var header = new VisualElement();
            header.style.backgroundColor = Theme.HeaderBg;
            header.style.paddingLeft = 16;
            header.style.paddingRight = 16;
            header.style.paddingTop = 14;
            header.style.paddingBottom = 10;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Theme.Divider;

            var mainTitle = CreateTitleLabel("TableNine UI 面板注册表", 22, true, Theme.TextPrimary);
            header.Add(mainTitle);

            var subtitle = CreateTitleLabel(
                "Gameplay 通过事件描述 UI 需求，本注册表将稳定的 UI Key 映射到正式 Prefab 和受控的 Fallback 策略。",
                12, false, Theme.TextSecondary);
            subtitle.style.marginTop = 6;
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            header.Add(subtitle);
            mRoot.Add(header);

            // 2) Toolbar
            var toolbar = new Toolbar();
            toolbar.style.height = 34;
            toolbar.style.paddingLeft = 8;
            toolbar.style.paddingRight = 8;
            toolbar.style.backgroundColor = Theme.HeaderBg;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = Theme.Divider;

            var syncBtn = new ToolbarButton(OnSyncDefaults) { text = "同步推荐默认值" };
            syncBtn.tooltip = "将推荐的面板默认配置合并到注册表中（不会覆盖已有 Prefab 引用）";
            toolbar.Add(syncBtn);

            var refreshBtn = new ToolbarButton(RefreshAll) { text = "刷新" };
            refreshBtn.tooltip = "刷新编辑器显示";
            toolbar.Add(refreshBtn);

            var pingBtn = new ToolbarButton(OnPingAsset) { text = "定位资产" };
            pingBtn.tooltip = "在项目面板中高亮定位此注册表资产";
            toolbar.Add(pingBtn);

            mRoot.Add(toolbar);

            // 3) Body — TwoPaneSplitView
            var split = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;

            // Left sidebar
            var sidebarColumn = new VisualElement();
            sidebarColumn.style.flexGrow = 1;
            sidebarColumn.style.backgroundColor = Theme.SidebarBg;
            sidebarColumn.style.borderRightWidth = 1;
            sidebarColumn.style.borderRightColor = Theme.Divider;

            var searchWrap = new VisualElement();
            searchWrap.style.paddingLeft = 10;
            searchWrap.style.paddingRight = 10;
            searchWrap.style.paddingTop = 10;
            searchWrap.style.paddingBottom = 6;
            searchWrap.style.borderBottomWidth = 1;
            searchWrap.style.borderBottomColor = Theme.Divider;

            mSearchField = new TextField("搜索") { value = mSearchText };
            mSearchField.style.flexGrow = 1;
            StyleSearchField(mSearchField);
            mSearchField.RegisterValueChangedCallback(evt =>
            {
                mSearchText = evt.newValue ?? "";
                EditorPrefs.SetString(PrefSearchKey, mSearchText);
                RebuildSidebarList();
            });
            searchWrap.Add(mSearchField);
            sidebarColumn.Add(searchWrap);

            var sidebarScroll = new ScrollView(ScrollViewMode.Vertical);
            sidebarScroll.style.flexGrow = 1;
            mSidebarList = new VisualElement();
            mSidebarList.style.paddingLeft = 10;
            mSidebarList.style.paddingRight = 10;
            mSidebarList.style.paddingTop = 8;
            mSidebarList.style.paddingBottom = 8;
            sidebarScroll.Add(mSidebarList);
            sidebarColumn.Add(sidebarScroll);

            var footer = new VisualElement();
            footer.style.paddingLeft = 10;
            footer.style.paddingRight = 10;
            footer.style.paddingTop = 6;
            footer.style.paddingBottom = 10;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = Theme.Divider;

            footer.Add(CreateNavButton(
                "全局 Fallback 预制件",
                "组件化预制件模板引用",
                GlobalFallbackKey,
                SelectGlobalFallback));
            sidebarColumn.Add(footer);

            split.Add(sidebarColumn);

            // Right content
            mDetailScroll = new ScrollView(ScrollViewMode.Vertical);
            mDetailScroll.style.flexGrow = 1;
            mDetailScroll.style.backgroundColor = Theme.ContentBg;
            mDetailScroll.contentContainer.style.paddingLeft = 18;
            mDetailScroll.contentContainer.style.paddingRight = 18;
            mDetailScroll.contentContainer.style.paddingTop = 14;
            mDetailScroll.contentContainer.style.paddingBottom = 20;
            split.Add(mDetailScroll);

            mRoot.Add(split);
        }

        private static void StyleSearchField(TextField field)
        {
            field.labelElement.style.color = Theme.TextTertiary;
            field.labelElement.style.fontSize = 11;
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
                    RebuildSidebarList();
                    RebuildDetailPane();
                    return;
                }
                mSerializedObj = new SerializedObject(mRegistry);
            }

            mSerializedObj.Update();
            CollectEntries();
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

        private (int total, int formal, int fallback, int missing) ComputeStats()
        {
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
            return (mAllEntries.Count, formal, fallback, missing);
        }

        // ═══════════════════════════════════════════
        //  Sidebar
        // ═══════════════════════════════════════════

        private void RebuildSidebarList()
        {
            if (mSidebarList == null) return;

            mNavButtons.RemoveAll(e => e.Key != GlobalFallbackKey);
            mSidebarList.Clear();

            if (mRegistry == null || mAllEntries.Count == 0)
            {
                UpdateNavigationStyles();
                return;
            }

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
                var empty = CreateDescriptionLabel(string.IsNullOrEmpty(filter) ? "暂无面板条目" : "无匹配结果");
                empty.style.paddingTop = 12;
                mSidebarList.Add(empty);
            }

            UpdateNavigationStyles();
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

            var header = CreateTitleLabel(
                $"{catMeta.Icon}  {catMeta.ChineseName.ToUpperInvariant()}  ({entries.Count})",
                10, true, Theme.TextTertiary);
            header.style.marginTop = 4;
            header.style.marginBottom = 6;
            header.style.marginLeft = 2;
            mSidebarList.Add(header);

            foreach (var info in entries)
            {
                var status = GetPanelStatus(info.Property);
                var statusText = status switch
                {
                    "formal" => "已接入",
                    "fallback" => "Fallback",
                    _ => "未配置"
                };

                var key = info.UIKey;
                mSidebarList.Add(CreateNavButton(
                    info.Meta.ChineseName,
                    $"{info.UIKey}  ·  {statusText}",
                    key,
                    () => SelectPanel(key)));
            }
        }

        private void SelectPanel(string uiKey)
        {
            mSelectedKey = uiKey;
            EditorPrefs.SetString(PrefSelectedKey, uiKey);
            UpdateNavigationStyles();
            RebuildDetailPane();
        }

        private void SelectGlobalFallback()
        {
            mSelectedKey = GlobalFallbackKey;
            EditorPrefs.SetString(PrefSelectedKey, GlobalFallbackKey);
            UpdateNavigationStyles();
            RebuildDetailPane();
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
            if (mDetailScroll == null) return;
            mDetailScroll.contentContainer.Clear();

            if (mRegistry == null || mSerializedObj == null)
            {
                ShowNotFoundInDetailPane();
                return;
            }

            EnsureValidSelection();

            var stats = ComputeStats();
            mDetailScroll.Add(CreateStatsGrid(stats.total, stats.formal, stats.fallback, stats.missing));

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
            mDetailScroll.Add(CreatePageHeader(
                "选择面板",
                "从左侧导航选择一个 UI 面板，查看中文说明、设计备注与接入配置。"));

            mDetailScroll.Add(new HelpBox(
                "侧栏按分类收纳全部面板。新增面板只需在 UIPanelMetadataCatalog 字典追加一条元数据。",
                HelpBoxMessageType.Info));
        }

        private void BuildPanelDetail(PanelEntryInfo info)
        {
            var status = GetPanelStatus(info.Property);
            Categories.TryGetValue(info.Meta.Category, out var catMeta);
            var categoryLine = catMeta != null ? $"分类：{catMeta.ChineseName}" : "";

            mDetailScroll.Add(CreatePageHeader(
                info.Meta.ChineseName,
                info.Meta.ChineseDescription));

            mDetailScroll.Add(CreateTinyPathLabel(info.UIKey));
            if (!string.IsNullOrEmpty(categoryLine))
            {
                var catLabel = CreateDescriptionLabel(categoryLine);
                catLabel.style.marginTop = 4;
                catLabel.style.marginBottom = 12;
                mDetailScroll.Add(catLabel);
            }

            mDetailScroll.Add(CreateStatusHelpBox(status));

            var spacer = new VisualElement();
            spacer.style.height = 12;
            mDetailScroll.Add(spacer);

            mDetailScroll.Add(BuildInfoSection(info));
            mDetailScroll.Add(BuildConfigSection(info));
            mDetailScroll.Add(BuildFallbackSection(info));

            var notesProp = info.Property.FindPropertyRelative("Notes");
            if (notesProp != null)
                mDetailScroll.Add(BuildNotesSection(notesProp));

            mDetailScroll.Bind(mSerializedObj);
        }

        private VisualElement BuildInfoSection(PanelEntryInfo info)
        {
            return CreateSectionCard(
                "面板信息",
                "来自 UIPanelMetadataCatalog 的设计说明与触发上下文。",
                body =>
                {
                    if (!string.IsNullOrEmpty(info.Meta.TriggerEvent))
                    {
                        body.Add(WrapControl(
                            "触发事件",
                            "Gameplay 侧触发此面板的领域事件",
                            CreateDescriptionLabel(info.Meta.TriggerEvent)));
                    }

                    if (!string.IsNullOrEmpty(info.Meta.ExpectedElements))
                    {
                        body.Add(WrapControl(
                            "期望 UI 元素",
                            "正式 Prefab 应包含的关键控件",
                            CreateDescriptionLabel(info.Meta.ExpectedElements)));
                    }

                    if (!string.IsNullOrEmpty(info.Meta.DesignNote))
                    {
                        body.Add(WrapControl(
                            "设计备注",
                            "来自 UI 设计文档的补充说明",
                            CreateDescriptionLabel(info.Meta.DesignNote)));
                    }
                });
        }

        private VisualElement BuildConfigSection(PanelEntryInfo info)
        {
            return CreateSectionCard(
                "接入配置",
                "将 UI Key 映射到 UIKit 面板名与正式 Prefab。",
                body =>
                {
                    var entryProp = info.Property;

                    var uiKeyProp = entryProp.FindPropertyRelative("UIKey");
                    if (uiKeyProp != null)
                        body.Add(WrapProperty("UI Key", "稳定接入键，运行时按此查找", uiKeyProp, readOnly: true));

                    var uiTypeProp = entryProp.FindPropertyRelative("UIType");
                    if (uiTypeProp != null)
                    {
                        var typeEnum = (TableNineUIType)uiTypeProp.intValue;
                        body.Add(WrapProperty("UI 类型", GetUITypeLabel(typeEnum), uiTypeProp));
                    }

                    var panelNameProp = entryProp.FindPropertyRelative("PanelName");
                    if (panelNameProp != null)
                        body.Add(WrapProperty("面板名", "UIKit Panel 类名", panelNameProp));

                    var prefabProp = entryProp.FindPropertyRelative("Prefab");
                    if (prefabProp != null)
                        body.Add(WrapProperty("正式 Prefab", "绑定后优先使用正式 UI", prefabProp));

                    var levelProp = entryProp.FindPropertyRelative("Level");
                    if (levelProp != null)
                    {
                        var levelEnum = (UILevel)levelProp.intValue;
                        body.Add(WrapProperty("层级", GetUILevelLabel(levelEnum), levelProp));
                    }

                    var openTypeProp = entryProp.FindPropertyRelative("OpenType");
                    if (openTypeProp != null)
                    {
                        var openEnum = (PanelOpenType)openTypeProp.intValue;
                        body.Add(WrapProperty("打开方式", GetOpenTypeLabel(openEnum), openTypeProp));
                    }
                });
        }

        private VisualElement BuildFallbackSection(PanelEntryInfo info)
        {
            return CreateSectionCard(
                "Fallback 配置",
                "未绑定正式 Prefab 时的回退策略与交互阻断设置。",
                body =>
                {
                    var entryProp = info.Property;

                    var fallbackStrategyProp = entryProp.FindPropertyRelative("FallbackStrategy");
                    if (fallbackStrategyProp != null)
                    {
                        var stratEnum = (TableNineUIFallbackStrategy)fallbackStrategyProp.intValue;
                        body.Add(WrapProperty(
                            "回退策略",
                            GetFallbackStrategyDescription(stratEnum),
                            fallbackStrategyProp));
                    }

                    var blocksProp = entryProp.FindPropertyRelative("BlocksGameplayInput");
                    if (blocksProp != null)
                    {
                        body.Add(WrapProperty(
                            "阻止操作",
                            blocksProp.boolValue
                                ? "阻止底层交互（全屏遮罩）"
                                : "不阻止底层交互",
                            blocksProp));
                    }
                });
        }

        private VisualElement BuildNotesSection(SerializedProperty notesProp)
        {
            return CreateSectionCard(
                "备注",
                "策划或程序补充说明，写入注册表资产。",
                body =>
                {
                    body.Add(WrapProperty("备注内容", "", notesProp));
                });
        }

        private void BuildGlobalFallbackDetail()
        {
            mDetailScroll.Add(CreatePageHeader(
                "全局 Fallback 预制件",
                "组件化预制件，供 Fallback 面板运行时实例化。为空时回退到纯代码生成。"));

            mDetailScroll.Add(new HelpBox(
                "这些预制件被所有 Fallback 面板共享。建议保持风格统一，便于快速迭代。",
                HelpBoxMessageType.Info));

            var spacer = new VisualElement();
            spacer.style.height = 12;
            mDetailScroll.Add(spacer);

            string[] fields = { "mFallbackButtonPrefab", "mFallbackTextPrefab", "mFallbackIconPrefab", "mFallbackPanelPrefab", "mFallbackScrollViewPrefab" };
            string[] labels = { "按钮预制件", "文本预制件", "图标预制件", "面板预制件", "滚动视图预制件" };
            string[] hints = {
                "Fallback 面板中所有按钮的模板",
                "Fallback 面板中所有文本的模板",
                "Fallback 面板中图标的模板",
                "整体面板模板（可选）",
                "滚动列表模板（可选）"
            };

            mDetailScroll.Add(CreateSectionCard(
                "预制件引用",
                "拖入对应 Prefab，运行时由 Fallback 系统实例化。",
                body =>
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var prop = mSerializedObj.FindProperty(fields[i]);
                        if (prop == null) continue;
                        body.Add(WrapProperty(labels[i], hints[i], prop));
                    }
                }));

            mDetailScroll.Bind(mSerializedObj);
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
                case "formal": return "已接入正式 Prefab";
                case "fallback": return "使用 Fallback";
                default: return "未配置";
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
            mDetailScroll.Add(CreatePageHeader(
                "未找到注册表资产",
                "请确认 Assets/ScriptableObjects/TableNineUIPanelRegistry.asset 存在。"));

            mDetailScroll.Add(new HelpBox(
                "尚未创建 UI 面板注册表。点击下方按钮创建默认资产后继续配置。",
                HelpBoxMessageType.Warning));

            var createBtn = new Button(() =>
            {
                CreateRegistryAsset();
                RefreshAll();
            }) { text = "创建注册表资产" };

            mDetailScroll.Add(CreateButtonRow(createBtn));
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

        private struct PanelEntryInfo
        {
            public int Index;
            public SerializedProperty Property;
            public string UIKey;
            public PanelMeta Meta;
        }
    }

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
            title.style.color = new Color(0.95f, 0.89f, 0.79f);
            title.style.marginBottom = 4;
            root.Add(title);

            var desc = new Label("打开可视化面板管理窗口：左侧选择面板，右侧查看说明与接入配置。");
            desc.style.fontSize = 11;
            desc.style.color = new Color(0.79f, 0.73f, 0.67f);
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
            openBtn.style.height = 28;
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
                    statsLabel.style.color = new Color(0.73f, 0.70f, 0.66f);
                    root.Add(statsLabel);
                }
            }

            return root;
        }
    }
}
