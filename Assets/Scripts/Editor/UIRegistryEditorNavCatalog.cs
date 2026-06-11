using System.Linq;

namespace TableNineUI.Editor
{
    /// <summary>
    /// UI 面板管理窗口 —— 非面板类配置项的侧栏导航目录。
    /// 所有条目均进入可滚动侧栏，不再使用固定底栏。
    ///
    /// 【后续 AI 开发须知】
    /// 新增配置页时：在本文件 Entries 追加 NavEntry，并在
    /// TableNineUIRegistryEditorWindow.BuildSpecialNavDetail 中接入详情构建。
    /// </summary>
    public static class UIRegistryEditorNavCatalog
    {
        public const string GlobalFallbackKey = "__global_fallback__";
        public const string ConfigSectionLabel = "系统配置";

        public sealed class NavEntry
        {
            public string Key;
            public string Title;
            public string Description;
        }

        public static readonly NavEntry[] Entries =
        {
            new NavEntry
            {
                Key = DescriptionPanelConfigEditorSection.NavKey,
                Title = "DescriptionPanel 描述文案",
                Description = "单行描述 · 40 字上限"
            },
            new NavEntry
            {
                Key = GlobalFallbackKey,
                Title = "全局 Fallback 预制件",
                Description = "组件化预制件模板引用"
            }
        };

        public static bool IsSpecialNavKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return Entries.Any(e => e.Key == key);
        }

        public static bool TryGetEntry(string key, out NavEntry entry)
        {
            foreach (var candidate in Entries)
            {
                if (candidate.Key == key)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public static bool MatchesFilter(NavEntry entry, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;

            return (entry.Key ?? "").ToLowerInvariant().Contains(filter)
                   || (entry.Title ?? "").ToLowerInvariant().Contains(filter)
                   || (entry.Description ?? "").ToLowerInvariant().Contains(filter)
                   || ConfigSectionLabel.ToLowerInvariant().Contains(filter);
        }
    }
}
