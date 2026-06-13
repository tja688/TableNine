#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TableNineCardConfigEditorWindow : WarmConsoleConfigEditorWindowBase
{
    private const string GroupCards = "card";
    private const string GroupRules = "rule";
    private const string CardsList = "Cards";
    private const string RulesList = "MonsterDeckRules";

    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Card;
    protected override string WindowTitle => "卡牌配置";
    protected override string WindowSubtitle => "编辑卡牌定义与怪物牌组生成规则。";
    protected override string EditorPrefsKey => "Card";
    protected override string DefaultAssetPath => $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineCardConfig.asset";

    [MenuItem("TableNine/Game Config/Edit Card Config")]
    public static void OpenFromMenu()
    {
        var window = GetWindow<TableNineCardConfigEditorWindow>();
        window.titleContent = new GUIContent("卡牌配置");
        var asset = AssetDatabase.LoadAssetAtPath<TableNineCardConfig>(window.DefaultAssetPath);
        if (asset != null) window.SetTarget(asset);
        window.Show();
    }

    public static void Open(TableNineCardConfig asset)
    {
        var window = GetWindow<TableNineCardConfigEditorWindow>();
        window.titleContent = new GUIContent("卡牌配置");
        window.SetTarget(asset);
        window.Show();
    }

    protected override Type GetExpectedAssetType() => typeof(TableNineCardConfig);

    protected override int GetTotalItemCount()
    {
        var cards = GetListProperty(CardsList);
        var rules = GetListProperty(RulesList);
        return (cards?.arraySize ?? 0) + (rules?.arraySize ?? 0);
    }

    protected override void BuildNavigation(VisualElement navList)
    {
        var cards = GetListProperty(CardsList);
        if (cards != null && cards.arraySize > 0)
        {
            AddNavSection("卡牌");
            for (var i = 0; i < cards.arraySize; i++)
            {
                var element = cards.GetArrayElementAtIndex(i);
                AddNavButton(
                    GetStringProp(element, "DisplayName", "未命名卡牌"),
                    GetStringProp(element, "CardId", $"card_{i}"),
                    MakeKey(GroupCards, i));
            }
        }

        var rules = GetListProperty(RulesList);
        if (rules != null && rules.arraySize > 0)
        {
            AddNavSection("怪物牌组规则");
            for (var i = 0; i < rules.arraySize; i++)
            {
                var element = rules.GetArrayElementAtIndex(i);
                var layer = element.FindPropertyRelative("Layer").intValue;
                var node = element.FindPropertyRelative("NodeInLayer").intValue;
                AddNavButton(
                    $"层 {layer} · 节点 {node}",
                    $"共 {element.FindPropertyRelative("TotalCardCount").intValue} 张",
                    MakeKey(GroupRules, i));
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

        if (group == GroupCards)
        {
            BuildCardDetail(contentRoot, index);
            return;
        }

        if (group == GroupRules)
        {
            BuildRuleDetail(contentRoot, index);
            return;
        }

        contentRoot.Add(Skin.CreateStatusHelpBox("未知条目类型。", HelpBoxMessageType.Warning));
    }

    private void BuildCardDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(CardsList, index);
        if (element == null) return;

        var displayName = GetStringProp(element, "DisplayName", "未命名卡牌");
        var id = GetStringProp(element, "CardId", string.Empty);
        var cardType = (CardType)element.FindPropertyRelative("CardType").enumValueIndex;

        contentRoot.Add(Skin.CreatePageHeader(displayName, $"{id} · {cardType}"));
        contentRoot.Add(Skin.CreateStatsGrid(
            ("生命", element.FindPropertyRelative("BaseHp").intValue.ToString(), "怪物基础生命"),
            ("攻击", element.FindPropertyRelative("BaseAttack").intValue.ToString(), "怪物基础攻击"),
            ("防御", element.FindPropertyRelative("BaseDefense").intValue.ToString(), "怪物基础防御"),
            ("价格", element.FindPropertyRelative("Price").intValue.ToString(), "商店/奖励价格")));

        var form = new VisualElement();
        var spritePanel = new SpritePreviewPanel(Skin);
        spritePanel.Bind(TargetSo, element.FindPropertyRelative("Image"));

        form.Add(Skin.CreateSectionCard("基础信息", "卡牌标识、类型与描述。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "CardId", "DisplayName", "Description", "CardType", "Quality", "Price");
        }));

        if (cardType == CardType.Monster)
        {
            form.Add(Skin.CreateSectionCard("怪物属性", "花色、点数与战斗数值。", section =>
            {
                ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                    "MonsterLevel", "Suit", "Rank", "BaseHp", "BaseAttack", "BaseDefense", "SkillIds");
            }));
        }
        else if (cardType == CardType.Help)
        {
            form.Add(Skin.CreateSectionCard("援助卡逻辑", "效果绑定与恢复规则。", section =>
            {
                ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                    "EffectGraphId", "SystemTag", "RestoreAfterNode", "RestoreAfterNodeAuthoritative", "SkillIds");
            }));
        }
        else
        {
            form.Add(Skin.CreateSectionCard("扩展属性", "其他卡牌字段。", section =>
            {
                ConfigEditorPropertyBuilder.AddAllChildrenExcept(section, Skin, TargetSo, element,
                    "CardId", "DisplayName", "Description", "Image", "CardType", "Quality", "Price");
            }));
        }

        contentRoot.Add(Skin.CreateFormWithSpritePreview(form, spritePanel));
    }

    private void BuildRuleDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(RulesList, index);
        if (element == null) return;

        var layer = element.FindPropertyRelative("Layer").intValue;
        var node = element.FindPropertyRelative("NodeInLayer").intValue;
        contentRoot.Add(Skin.CreatePageHeader($"层 {layer} · 节点 {node}", "怪物牌组生成规则"));
        contentRoot.Add(Skin.CreateStatsGrid(
            ("层", layer.ToString(), "地图层编号"),
            ("节点", node.ToString(), "层内节点序号"),
            ("总卡数", element.FindPropertyRelative("TotalCardCount").intValue.ToString(), "本节点牌组总数")));

        contentRoot.Add(Skin.CreateSectionCard("节点参数", "层级与数量约束。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "Layer", "NodeInLayer", "TotalCardCount");
        }));

        contentRoot.Add(Skin.CreateSectionCard("卡池与配额", "允许卡、强制卡与等级配额。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "AllowedMonsterCardIds", "MandatoryMonsterCardIds", "LevelQuotas");
        }));
    }

    protected override void OnAddItem()
    {
        if (!TryParseKey(SelectedKey, out var group, out _) || string.IsNullOrEmpty(group))
        {
            group = GroupCards;
        }

        if (group == GroupRules)
        {
            AddToList(RulesList, GroupRules, element =>
            {
                element.FindPropertyRelative("Layer").intValue = 1;
                element.FindPropertyRelative("NodeInLayer").intValue = 1;
                element.FindPropertyRelative("TotalCardCount").intValue = 3;
            });
            return;
        }

        AddToList(CardsList, GroupCards, element =>
        {
            element.FindPropertyRelative("CardId").stringValue = $"card_new_{DateTime.Now.Ticks % 10000}";
            element.FindPropertyRelative("DisplayName").stringValue = "新卡牌";
            element.FindPropertyRelative("CardType").enumValueIndex = (int)CardType.Help;
        });
    }

    private void AddToList(string listName, string group, Action<SerializedProperty> init)
    {
        var list = GetListProperty(listName);
        if (list == null) return;
        list.arraySize++;
        var element = list.GetArrayElementAtIndex(list.arraySize - 1);
        init?.Invoke(element);
        SelectedKey = MakeKey(group, list.arraySize - 1);
        EditorPrefs.SetString($"TableNine.ConfigEditor.{EditorPrefsKey}.Selection", SelectedKey);
    }

    protected override void OnDeleteItem()
    {
        if (!TryParseKey(SelectedKey, out var group, out var index)) return;

        var listName = group == GroupRules ? RulesList : CardsList;
        var list = GetListProperty(listName);
        if (list == null || index < 0 || index >= list.arraySize) return;
        list.DeleteArrayElementAtIndex(index);
    }
}
#endif
