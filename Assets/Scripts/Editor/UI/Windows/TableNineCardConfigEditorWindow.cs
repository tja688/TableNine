#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TableNineCardConfigEditorWindow : WarmConsoleConfigEditorWindowBase
{
    private const string GroupDecks = "deck";
    private const string GroupRules = "rule";
    private const string DecksList = "CardDecks";
    private const string CardsList = "Cards";
    private const string RulesList = "MonsterDeckRules";

    protected override WarmConsoleThemePalette ThemePalette => WarmConsoleThemePalette.Card;
    protected override string WindowTitle => "卡牌配置";
    protected override string WindowSubtitle => "以牌组为中心管理卡牌定义、卡面卡背与怪物生成规则。";
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
        var decks = GetListProperty(DecksList);
        var cards = GetListProperty(CardsList);
        var rules = GetListProperty(RulesList);
        return (decks?.arraySize ?? 0) + (cards?.arraySize ?? 0) + (rules?.arraySize ?? 0);
    }

    protected override void BuildNavigation(VisualElement navList)
    {
        var decks = GetListProperty(DecksList);
        if (decks != null && decks.arraySize > 0)
        {
            AddNavSection("牌组");
            for (var i = 0; i < decks.arraySize; i++)
            {
                var element = decks.GetArrayElementAtIndex(i);
                var deckId = GetStringProp(element, "DeckId", $"deck_{i}");
                var count = CountCardsInDeck(deckId);
                AddNavButton(
                    GetStringProp(element, "DisplayName", "未命名牌组"),
                    $"{deckId} · {count} 张卡",
                    MakeKey(GroupDecks, i));
            }
        }

        var rules = GetListProperty(RulesList);
        if (rules != null && rules.arraySize > 0)
        {
            AddNavSection("怪物生成规则");
            for (var i = 0; i < rules.arraySize; i++)
            {
                var element = rules.GetArrayElementAtIndex(i);
                var layer = element.FindPropertyRelative("Layer").intValue;
                var node = element.FindPropertyRelative("NodeInLayer").intValue;
                AddNavButton(
                    $"层 {layer} · 节点 {node}",
                    $"{GetStringProp(element, "SourceDeckId", "未指定牌组")} · {element.FindPropertyRelative("TotalCardCount").intValue} 张",
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

        if (group == GroupDecks)
        {
            BuildDeckDetail(contentRoot, index);
            return;
        }

        if (group == GroupRules)
        {
            BuildRuleDetail(contentRoot, index);
            return;
        }

        contentRoot.Add(Skin.CreateStatusHelpBox("未知条目类型。", HelpBoxMessageType.Warning));
    }

    private void BuildDeckDetail(VisualElement contentRoot, int index)
    {
        var deck = GetListElement(DecksList, index);
        if (deck == null) return;

        var displayName = GetStringProp(deck, "DisplayName", "未命名牌组");
        var deckId = GetStringProp(deck, "DeckId", string.Empty);
        var cardCount = CountCardsInDeck(deckId);

        contentRoot.Add(Skin.CreatePageHeader(displayName, $"{deckId} · {ReadEnum(deck, "Faction")} · {ReadEnum(deck, "DeckKind")}"));
        contentRoot.Add(Skin.CreateStatsGrid(
            ("卡牌数", cardCount.ToString(), "归属本牌组的卡牌"),
            ("阵营", ReadEnum(deck, "Faction"), "玩家或怪物"),
            ("类型", ReadEnum(deck, "DeckKind"), "牌组机制分类")));

        var form = new VisualElement();
        var spritePanel = new SpritePreviewPanel(Skin);
        spritePanel.Bind(TargetSo, deck.FindPropertyRelative("DefaultFaceImage"));

        form.Add(Skin.CreateSectionCard("牌组基础", "牌组作为卡牌组织与默认图像的顶层入口。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, deck,
                "DeckId", "DisplayName", "Description", "Faction", "DeckKind", "DefaultFaceImage", "DefaultBackImage");
        }));

        contentRoot.Add(Skin.CreateFormWithSpritePreview(form, spritePanel));
        contentRoot.Add(Skin.CreateButtonRow(
            new Button(() => AddCardToDeck(deckId)) { text = "新增卡牌到本牌组" },
            new Button(AddDeckAndRefresh) { text = "新增牌组" }));

        var cards = GetListProperty(CardsList);
        if (cards == null || cardCount == 0)
        {
            contentRoot.Add(Skin.CreateStatusHelpBox("当前牌组还没有卡牌。", HelpBoxMessageType.Info));
            return;
        }

        contentRoot.Add(Skin.CreateSectionCard("牌组内卡牌", "每张卡保留自身主图标，并可按需覆盖牌组默认卡面与卡背。", section =>
        {
            for (var i = 0; i < cards.arraySize; i++)
            {
                var card = cards.GetArrayElementAtIndex(i);
                if (GetStringProp(card, "DeckId", string.Empty) != deckId)
                {
                    continue;
                }

                section.Add(CreateCardEditorBlock(card, i));
            }
        }));
    }

    private VisualElement CreateCardEditorBlock(SerializedProperty card, int index)
    {
        var cardType = (CardType)card.FindPropertyRelative("CardType").enumValueIndex;
        var title = $"{GetStringProp(card, "DisplayName", "未命名卡牌")} · {GetStringProp(card, "CardId", $"card_{index}")}";

        return Skin.CreateSectionCard(title, cardType.ToString(), section =>
        {
            section.Add(Skin.CreateButtonRow(new Button(() => DeleteCardAt(index)) { text = "删除这张卡" }));

            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, card,
                "CardId", "DisplayName", "DeckId", "Description", "CardType", "Quality", "Price");
            AddSpriteProperty(section, card.FindPropertyRelative("Image"), "主图标", "卡牌中心核心显示图标。");
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, card,
                "FaceImageOverride", "BackImageOverride");
            section.Add(new BakedCardPreviewPanel(Skin, TargetSo, () => ResolvePreviewData(index)));

            if (cardType == CardType.Monster)
            {
                ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, card,
                    "MonsterLevel", "Suit", "Rank", "BaseHp", "BaseAttack", "BaseArmor", "SkillIds");
            }
            else if (cardType == CardType.Help)
            {
                ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, card,
                    "EffectGraphId", "SystemTag", "RestoreAfterNode", "RestoreAfterNodeAuthoritative", "SkillIds");
            }
            else
            {
                ConfigEditorPropertyBuilder.AddAllChildrenExcept(section, Skin, TargetSo, card,
                    "CardId", "DisplayName", "DeckId", "Description", "Image", "FaceImageOverride",
                    "BackImageOverride", "CardType", "Quality", "Price");
            }
        });
    }

    private void BuildRuleDetail(VisualElement contentRoot, int index)
    {
        var element = GetListElement(RulesList, index);
        if (element == null) return;

        var layer = element.FindPropertyRelative("Layer").intValue;
        var node = element.FindPropertyRelative("NodeInLayer").intValue;
        contentRoot.Add(Skin.CreatePageHeader($"层 {layer} · 节点 {node}", "怪物牌组生成规则"));
        contentRoot.Add(Skin.CreateStatsGrid(
            ("来源牌组", GetStringProp(element, "SourceDeckId", "未指定"), "节点所属怪物牌组"),
            ("层", layer.ToString(), "地图层编号"),
            ("节点", node.ToString(), "层内节点序号"),
            ("总卡数", element.FindPropertyRelative("TotalCardCount").intValue.ToString(), "本节点牌组总数")));

        contentRoot.Add(Skin.CreateSectionCard("节点参数", "层级、来源牌组与数量约束。", section =>
        {
            ConfigEditorPropertyBuilder.AddProperties(section, Skin, TargetSo, element,
                "Layer", "NodeInLayer", "SourceDeckId", "TotalCardCount");
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
            group = GroupDecks;
        }

        if (group == GroupRules)
        {
            AddToList(RulesList, GroupRules, element =>
            {
                element.FindPropertyRelative("Layer").intValue = 1;
                element.FindPropertyRelative("NodeInLayer").intValue = 1;
                element.FindPropertyRelative("SourceDeckId").stringValue = GameConfigIds.DeckWeakEliteId;
                element.FindPropertyRelative("TotalCardCount").intValue = 3;
            });
            return;
        }

        AddDeck(refresh: false);
    }

    private void AddDeckAndRefresh()
    {
        AddDeck(refresh: true);
    }

    private void AddDeck(bool refresh)
    {
        AddToList(DecksList, GroupDecks, element =>
        {
            element.FindPropertyRelative("DeckId").stringValue = $"deck_new_{DateTime.Now.Ticks % 10000}";
            element.FindPropertyRelative("DisplayName").stringValue = "新牌组";
            element.FindPropertyRelative("Faction").enumValueIndex = (int)CardDeckFaction.Player;
            element.FindPropertyRelative("DeckKind").enumValueIndex = (int)CardDeckKind.Custom;
        });

        if (!refresh)
        {
            return;
        }

        TableNineConfigEditorAutoSave.PersistIfEnabled(TargetSo, TargetAsset, immediateDisk: true);
        RebuildNavigation();
        RefreshDetail();
    }

    private void AddCardToDeck(string deckId, bool refresh = true)
    {
        var list = GetListProperty(CardsList);
        if (list == null) return;

        list.arraySize++;
        var element = list.GetArrayElementAtIndex(list.arraySize - 1);
        element.FindPropertyRelative("CardId").stringValue = $"card_new_{DateTime.Now.Ticks % 10000}";
        element.FindPropertyRelative("DisplayName").stringValue = "新卡牌";
        element.FindPropertyRelative("DeckId").stringValue = string.IsNullOrEmpty(deckId) ? GameConfigIds.DeckCommonId : deckId;
        element.FindPropertyRelative("CardType").enumValueIndex = (int)CardType.Help;
        element.FindPropertyRelative("Quality").enumValueIndex = (int)CardQuality.White;

        if (!refresh)
        {
            return;
        }

        TableNineConfigEditorAutoSave.PersistIfEnabled(TargetSo, TargetAsset, immediateDisk: true);
        RebuildNavigation();
        RefreshDetail();
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

        var listName = group == GroupRules ? RulesList : DecksList;
        var list = GetListProperty(listName);
        if (list == null || index < 0 || index >= list.arraySize) return;
        list.DeleteArrayElementAtIndex(index);
    }

    private void DeleteCardAt(int index)
    {
        if (!EditorUtility.DisplayDialog("卡牌配置", "确定删除这张卡？", "删除", "取消"))
        {
            return;
        }

        var list = GetListProperty(CardsList);
        if (list == null || index < 0 || index >= list.arraySize) return;
        list.DeleteArrayElementAtIndex(index);
        TableNineConfigEditorAutoSave.PersistIfEnabled(TargetSo, TargetAsset, immediateDisk: true);
        RebuildNavigation();
        RefreshDetail();
    }

    private int CountCardsInDeck(string deckId)
    {
        var cards = GetListProperty(CardsList);
        if (cards == null || string.IsNullOrEmpty(deckId))
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i < cards.arraySize; i++)
        {
            if (GetStringProp(cards.GetArrayElementAtIndex(i), "DeckId", string.Empty) == deckId)
            {
                count++;
            }
        }

        return count;
    }

    private string ReadEnum(SerializedProperty parent, string propertyName)
    {
        var prop = parent.FindPropertyRelative(propertyName);
        if (prop == null || prop.propertyType != SerializedPropertyType.Enum)
        {
            return string.Empty;
        }

        return prop.enumDisplayNames[prop.enumValueIndex];
    }

    private void AddSpriteProperty(VisualElement parent, SerializedProperty property, string label, string description)
    {
        if (property == null)
        {
            return;
        }

        var field = new ObjectField("")
        {
            objectType = typeof(Sprite),
            allowSceneObjects = false
        };
        field.BindProperty(property);
        field.Bind(TargetSo);
        parent.Add(Skin.WrapControl(label, description, field));
    }

    private BakedCardFaceRenderData ResolvePreviewData(int index)
    {
        if (!(TargetAsset is TableNineCardConfig asset) || index < 0 || index >= asset.Cards.Count)
        {
            return null;
        }

        var master = AssetDatabase.LoadAssetAtPath<TableNineGameConfig>("Assets/ScriptableObjects/TableNineGameConfig.asset");
        return BakedCardRenderDataFactory.CreateEditorPreview(master, asset.Cards[index]);
    }
}
#endif
