using System.Collections.Generic;

namespace TableNineUI.Editor
{
    /// <summary>
    /// UI 面板元数据目录 —— 填表式架构的核心数据源。
    /// 编辑器窗口的所有中文标注、分类、描述均从此处读取。
    /// 
    /// 【后续 AI 开发须知】
    /// 新增面板时，只需在本文件的 s_EntryMeta 字典中追加一条 PanelMeta，
    /// 编辑器界面会自动展示新面板条目，无需修改编辑器代码。
    /// </summary>
    public static class UIPanelMetadataCatalog
    {
        /// <summary>面板分类标识</summary>
        public enum Category
        {
            Hud,
            Popup,
            Choice,
            Reward,
            Shop,
            Prompt,
            Confirm,
            Detail
        }

        /// <summary>单条面板的元数据</summary>
        public sealed class PanelMeta
        {
            /// <summary>中文显示名</summary>
            public string ChineseName;

            /// <summary>中文功能描述（在编辑器中显示）</summary>
            public string ChineseDescription;

            /// <summary>所属分类</summary>
            public Category Category;

            /// <summary>触发该面板的领域事件</summary>
            public string TriggerEvent;

            /// <summary>面板期望包含的关键 UI 元素说明</summary>
            public string ExpectedElements;

            /// <summary>设计备注（来自 UI 设计文档）</summary>
            public string DesignNote;
        }

        // ════════════════════════════════════════════════════════════════
        //  面板条目元数据
        //  ★ 新增面板只需在此字典追加一条 ★
        // ════════════════════════════════════════════════════════════════
        public static readonly Dictionary<string, PanelMeta> Entries = new Dictionary<string, PanelMeta>
        {
            ["gameplay.hud"] = new PanelMeta
            {
                ChineseName = "主游戏界面",
                ChineseDescription = "战斗场景的常驻 HUD，显示玩家属性、卡组状态、遗物栏、技能栏和战斗日志。",
                Category = Category.Hud,
                TriggerEvent = "BattleDeckChangedEvent / DamageAppliedEvent / GoldChangedEvent 等",
                ExpectedElements = "角色名、头像、血量/血条、攻击、防御、金币、遗物栏(12格)、卡组信息、下一张预览、技能文本、战斗日志、通关按钮",
                DesignNote = "必须使用正式 Prefab。DescriptionPanel 单行描述上限 40 字，文案在 UI 面板管理 → DescriptionPanel 描述文案 中维护。"
            },

            ["popup.message"] = new PanelMeta
            {
                ChineseName = "通用提示弹窗",
                ChineseDescription = "全屏半透明遮罩 + 居中提示框，用于显示各类操作反馈。点击遮罩/文字均可关闭。",
                Category = Category.Popup,
                TriggerEvent = "PopupRequestedEvent",
                ExpectedElements = "遮罩层(Mask)、消息文本(MessageText)、关闭按钮(CloseButton)",
                DesignNote = "支持错误提示：帮助卡组已满、同名卡上限、金币不足、遗物格满、无可瞄准怪物、道具牌格已满等。"
            },

            ["choice.attribute"] = new PanelMeta
            {
                ChineseName = "属性提升选择",
                ChineseDescription = "使用属性提升卡时弹出，三个竖向按钮：攻击+1、防御+1、血量+2。显示当前属性值。",
                Category = Category.Choice,
                TriggerEvent = "AttributeChoiceRequestedEvent",
                ExpectedElements = "标题文本、攻击按钮(AttackButton)、防御按钮(DefenseButton)、血量按钮(MaxHpButton)，各显示当前值",
                DesignNote = "当前使用 UIChoiceOverlayPanel 复用，正式 UI 建议做独立竖向三按钮布局。"
            },

            ["choice.room"] = new PanelMeta
            {
                ChineseName = "房间选择",
                ChineseDescription = "节点通关后的房间二选一。随机给出 2 个房间类型（商店/金币房/宝箱房/属性房），选择后进入对应房间。",
                Category = Category.Choice,
                TriggerEvent = "RoomChoiceRequestedEvent",
                ExpectedElements = "标题文本(选择下一个房间类型)、2个房间卡按钮(Icon/Name/Desc/Select)",
                DesignNote = "非阻塞 UI，不阻止底层点击。无跳过按钮。"
            },

            ["choice.tutor_skill"] = new PanelMeta
            {
                ChineseName = "导师技能选择",
                ChineseDescription = "击败精英怪后出现导师卡，随机 3 个技能供玩家永久习得。没有跳过按钮。",
                Category = Category.Choice,
                TriggerEvent = "TutorSkillChoiceRequestedEvent",
                ExpectedElements = "标题文本(导师卡—选择一项技能永久习得)、3个技能卡(SkillName/SkillDesc/Select)",
                DesignNote = "可复用三选一窗口，但需隐藏跳过按钮。"
            },

            ["reward.help"] = new PanelMeta
            {
                ChineseName = "帮助卡奖励",
                ChineseDescription = "帮助卡三选一奖励。横排展示 3 张卡，每张显示名称/品质/效果，可选或跳过获得 +10 金币。",
                Category = Category.Reward,
                TriggerEvent = "HelpRewardGeneratedEvent",
                ExpectedElements = "标题文本(选择一张帮助卡加入卡组)、3个卡牌按钮(NameText/QualityText/DescText/ActionText)、跳过按钮(+10金币)",
                DesignNote = "当前有 3or2for1ChoiseWindow 可复用，确认补跳过按钮和 +10 金币。"
            },

            ["reward.chest"] = new PanelMeta
            {
                ChineseName = "宝箱遗物奖励",
                ChineseDescription = "使用宝箱卡后弹出遗物三选一。展示 3 个遗物卡，可选或跳过获得 +20 金币。",
                Category = Category.Reward,
                TriggerEvent = "ChestRewardGeneratedEvent",
                ExpectedElements = "标题文本(宝箱—三选一遗物)、3个遗物卡(NameText/QualityText/EffectText/SelectText)、跳过按钮(+20金币)",
                DesignNote = "需新增 ChestRelicChoiceWindow。宝箱卡有普通/蓝色/金色三种概率。"
            },

            ["shop.main"] = new PanelMeta
            {
                ChineseName = "商店",
                ChineseDescription = "商店界面，一次展示 6 个商品（可显示已售出），底部有删除帮助卡(+10金币)和前往下一节点按钮。",
                Category = Category.Shop,
                TriggerEvent = "ShopOpenedEvent",
                ExpectedElements = "标题(商店—金币:X)、6个商品卡(Name/Quality/Effect/Price/SoldOut)、删除帮助卡按钮、下一节点按钮",
                DesignNote = "当前 ShopChoiseWindow 需核对 6 商品格。Web 不是翻页商店。"
            },

            ["prompt.next_node"] = new PanelMeta
            {
                ChineseName = "下一节点提示",
                ChineseDescription = "奖励链结算完成后显示的小型状态提示，引导玩家进入下一个节点。",
                Category = Category.Prompt,
                TriggerEvent = "HelpRewardPickedEvent / HelpRewardSkippedEvent",
                ExpectedElements = "消息文本、关闭按钮",
                DesignNote = "轻量级提示，不阻塞交互。"
            },

            ["confirm.delete_help_card"] = new PanelMeta
            {
                ChineseName = "删除帮助卡确认",
                ChineseDescription = "商店中删除帮助卡的确认界面。展示帮助卡组（4列），每张可删除获得 +10 金币。空卡组时显示提示。",
                Category = Category.Confirm,
                TriggerEvent = "由商店 DeleteHelpCardButton 触发",
                ExpectedElements = "标题(删除帮助卡—金币:X)、帮助卡网格(Name/Quality/Effect/DeleteButton)、空状态文本、返回商店按钮",
                DesignNote = "需确认当前 DeleteCardChoiceWindow 的空状态和返回按钮。"
            },

            ["card.detail"] = new PanelMeta
            {
                ChineseName = "卡牌详情",
                ChineseDescription = "卡牌悬停或点击时显示详细信息：名称、品质、效果描述、数值等。",
                Category = Category.Detail,
                TriggerEvent = "由卡牌交互触发",
                ExpectedElements = "卡牌名称、品质标识、效果描述、数值信息",
                DesignNote = "可做 tooltip 或独立详情面板。"
            }
        };

        // ════════════════════════════════════════════════════════════════
        //  分类元数据
        // ════════════════════════════════════════════════════════════════
        public static readonly Dictionary<Category, CategoryMeta> Categories = new Dictionary<Category, CategoryMeta>
        {
            [Category.Hud] = new CategoryMeta
            {
                ChineseName = "HUD 面板",
                ChineseDescription = "常驻显示的主界面信息",
                Icon = "\u25a3"
            },
            [Category.Popup] = new CategoryMeta
            {
                ChineseName = "弹窗面板",
                ChineseDescription = "模态提示和信息弹窗",
                Icon = "\u25cb"
            },
            [Category.Choice] = new CategoryMeta
            {
                ChineseName = "选择覆盖层",
                ChineseDescription = "各类多选一界面",
                Icon = "\u25c6"
            },
            [Category.Reward] = new CategoryMeta
            {
                ChineseName = "奖励面板",
                ChineseDescription = "卡牌/遗物奖励选择",
                Icon = "\u2606"
            },
            [Category.Shop] = new CategoryMeta
            {
                ChineseName = "商店面板",
                ChineseDescription = "购买和删卡界面",
                Icon = "\u25a0"
            },
            [Category.Prompt] = new CategoryMeta
            {
                ChineseName = "状态提示",
                ChineseDescription = "轻量级状态引导",
                Icon = "\u25b3"
            },
            [Category.Confirm] = new CategoryMeta
            {
                ChineseName = "确认面板",
                ChineseDescription = "操作确认对话框",
                Icon = "\u26a0"
            },
            [Category.Detail] = new CategoryMeta
            {
                ChineseName = "详情面板",
                ChineseDescription = "物品/卡牌详情展示",
                Icon = "\u2139"
            }
        };

        public sealed class CategoryMeta
        {
            public string ChineseName;
            public string ChineseDescription;
            public string Icon;
        }

        // ════════════════════════════════════════════════════════════════
        //  枚举中文映射
        // ════════════════════════════════════════════════════════════════
        public static string GetFallbackStrategyLabel(TableNineUIFallbackStrategy strategy)
        {
            switch (strategy)
            {
                case TableNineUIFallbackStrategy.None: return "无（必须正式 Prefab）";
                case TableNineUIFallbackStrategy.AtomicPopup: return "原子弹窗";
                case TableNineUIFallbackStrategy.AtomicChoiceList: return "原子选择列表";
                case TableNineUIFallbackStrategy.AtomicRoomChoice: return "原子房间选择";
                case TableNineUIFallbackStrategy.AtomicShopList: return "原子商店列表";
                case TableNineUIFallbackStrategy.AtomicStatusPrompt: return "原子状态提示";
                default: return strategy.ToString();
            }
        }

        public static string GetFallbackStrategyDescription(TableNineUIFallbackStrategy strategy)
        {
            switch (strategy)
            {
                case TableNineUIFallbackStrategy.None: return "不提供 fallback，缺失时面板不会打开";
                case TableNineUIFallbackStrategy.AtomicPopup: return "居中显示标题、正文和确认按钮";
                case TableNineUIFallbackStrategy.AtomicChoiceList: return "标题 + 说明 + 纵向选项列表 + 可选关闭";
                case TableNineUIFallbackStrategy.AtomicRoomChoice: return "底部水平排列的房间按钮组，仅拦截按钮区域";
                case TableNineUIFallbackStrategy.AtomicShopList: return "商店购买列表 + 删卡列表 + 离开按钮";
                case TableNineUIFallbackStrategy.AtomicStatusPrompt: return "顶部小型状态提示条";
                default: return "";
            }
        }

        public static string GetUITypeLabel(TableNineUIType type)
        {
            switch (type)
            {
                case TableNineUIType.Hud: return "HUD（常驻信息）";
                case TableNineUIType.Popup: return "弹窗（模态提示）";
                case TableNineUIType.ChoiceOverlay: return "选择覆盖层（多选一）";
                case TableNineUIType.RoomChoice: return "房间选择";
                case TableNineUIType.RewardChoice: return "奖励选择";
                case TableNineUIType.Shop: return "商店";
                case TableNineUIType.Confirmation: return "确认对话框";
                case TableNineUIType.Detail: return "详情展示";
                case TableNineUIType.StatusPrompt: return "状态提示";
                default: return type.ToString();
            }
        }

        public static string GetUILevelLabel(QFramework.UILevel level)
        {
            switch (level)
            {
                case QFramework.UILevel.Common: return "Common（底层）";
                case QFramework.UILevel.PopUI: return "PopUI（弹出层）";
                default: return level.ToString();
            }
        }

        public static string GetOpenTypeLabel(QFramework.PanelOpenType openType)
        {
            switch (openType)
            {
                case QFramework.PanelOpenType.Single: return "Single（单例）";
                case QFramework.PanelOpenType.Multiple: return "Multiple（多实例）";
                default: return openType.ToString();
            }
        }

        /// <summary>根据 UIKey 获取元数据，缺失时返回占位信息</summary>
        public static PanelMeta GetEntryMeta(string uiKey)
        {
            if (!string.IsNullOrEmpty(uiKey) && Entries.TryGetValue(uiKey, out var meta))
                return meta;

            return new PanelMeta
            {
                ChineseName = uiKey ?? "(未命名)",
                ChineseDescription = "尚未填写中文描述，请在 UIPanelMetadataCatalog 中补充。",
                Category = Category.Choice,
                TriggerEvent = "-",
                ExpectedElements = "-",
                DesignNote = "请补充元数据"
            };
        }
    }
}
