#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

public static class ConfigEditorFieldLabels
{
    private static readonly Dictionary<string, (string label, string desc)> Labels =
        new Dictionary<string, (string, string)>
        {
            ["CharacterId"] = ("角色 ID", "运行时唯一标识，与技能/卡牌引用一致。"),
            ["DisplayName"] = ("显示名称", "UI 与说明面板中展示的名称。"),
            ["Image"] = ("图像", "角色立绘或头像 Sprite。"),
            ["Description"] = ("描述", "说明面板文案，最多 25 字。"),
            ["BaseHp"] = ("基础生命", "角色初始最大生命值。"),
            ["BaseAttack"] = ("基础攻击", "角色初始攻击力。"),
            ["BaseDefense"] = ("基础防御", "角色初始防御力。"),
            ["InitialSkillIds"] = ("初始技能", "开局拥有的技能 ID 列表。"),
            ["InitialHelpCardIds"] = ("初始援助卡", "开局援助卡组中的卡牌 ID。"),

            ["CardId"] = ("卡牌 ID", "运行时唯一标识。"),
            ["CardType"] = ("卡牌类型", "怪物牌或援助牌。"),
            ["Quality"] = ("品质", "影响价格与稀有度展示。"),
            ["MonsterLevel"] = ("怪物等级", "怪物牌强度分层。"),
            ["Suit"] = ("花色", "扑克花色，影响部分技能判定。"),
            ["Rank"] = ("点数", "扑克点数 1–13。"),
            ["Price"] = ("价格", "商店或奖励中的金币价格。"),
            ["RestoreAfterNode"] = ("节点后恢复", "使用后是否在节点结束时回到卡组。"),
            ["RestoreAfterNodeAuthoritative"] = ("恢复规则权威", "是否以本字段为准覆盖旧版序列化值。"),
            ["EffectGraphId"] = ("效果图表 ID", "运行时引用的效果链标识，如 eg_help_potion。"),
            ["SystemTag"] = ("系统标签", "援助卡系统分类标签。"),
            ["SkillIds"] = ("技能列表", "怪物或援助卡拥有的技能 ID。"),

            ["SkillId"] = ("技能 ID", "运行时唯一标识。"),
            ["GrantsFirstStrike"] = ("赋予先攻", "是否赋予先攻效果。"),
            ["HasRuntimeBinding"] = ("运行时绑定", "是否存在动态效果绑定。"),
            ["Trigger"] = ("触发时机", "技能或效果触发的游戏阶段。"),
            ["ConditionKey"] = ("条件键", "触发条件的逻辑键名。"),
            ["MaxHpOnAcquire"] = ("获得时生命", "获得技能时增加的最大生命。"),

            ["BindingId"] = ("绑定 ID", "技能效果绑定的唯一标识。"),
            ["OwnerKind"] = ("拥有者类型", "绑定归属：遗物/玩家技能/怪物技能等。"),
            ["OwnerDefinitionId"] = ("拥有者 ID", "绑定目标定义的 ID。"),

            ["RuleId"] = ("规则 ID", "行为规则唯一标识。"),
            ["BehaviorKind"] = ("行为类型", "技能行为规则的逻辑种类。"),
            ["IntValue"] = ("整型参数 1", "行为规则主数值参数。"),
            ["IntValue2"] = ("整型参数 2", "行为规则副数值参数。"),
            ["StatType"] = ("属性类型", "影响的属性种类。"),
            ["CauseId"] = ("原因 ID", "触发来源标识。"),
            ["IgnoreArmor"] = ("忽略护甲", "伤害是否忽略护甲。"),
            ["AuraSourceSkillId"] = ("光环来源技能", "光环类行为的来源技能 ID。"),

            ["RelicId"] = ("遗物 ID", "运行时唯一标识。"),
            ["StatAttackBonus"] = ("攻击加成", "遗物提供的攻击加成。"),
            ["StatDefenseBonus"] = ("防御加成", "遗物提供的防御加成。"),
            ["StatMaxHpBonus"] = ("生命加成", "遗物提供的最大生命加成。"),
            ["IsOneShot"] = ("一次性", "触发后是否消耗。"),
            ["ExcludeFromPool"] = ("排除出池", "是否不出现在随机池中。"),

            ["RoomId"] = ("房间 ID", "运行时唯一标识。"),
            ["RoomType"] = ("房间类型", "房间事件分类。"),
            ["RewardGold"] = ("奖励金币", "完成房间获得的金币。"),
            ["InjectCardId"] = ("注入卡牌", "进入房间时注入的卡牌 ID。"),

            ["Layer"] = ("层数", "地图层级编号。"),
            ["NodeInLayer"] = ("层内节点", "该层中的节点序号。"),
            ["TotalCardCount"] = ("总卡数", "本节点怪物牌组总数量。"),
            ["AllowedMonsterCardIds"] = ("允许怪物卡", "可抽取的怪物卡 ID 池。"),
            ["LevelQuotas"] = ("等级配额", "各怪物等级的数量上下限。"),
            ["MandatoryMonsterCardIds"] = ("强制怪物卡", "必定出现的怪物卡 ID。"),
            ["Level"] = ("等级", "怪物等级枚举。"),
            ["MinCount"] = ("最少数量", "该等级最少出现张数。"),
            ["MaxCount"] = ("最多数量", "该等级最多出现张数。"),
            ["PoolCardIds"] = ("卡池 ID", "该等级可抽取的卡牌 ID。"),

            ["Atoms"] = ("效果步骤", "按顺序执行的效果原子链。"),
            ["AtomType"] = ("原子类型", "效果步骤的逻辑类型，如治疗、伤害、消耗援助卡。"),
            ["Parameters"] = ("步骤参数", "当前效果步骤的键值对参数。"),
            ["Key"] = ("键", "参数名。"),
            ["Value"] = ("值", "参数值字符串。"),
            ["HelpCardEffectMappings"] = ("援助卡映射", "援助卡 ID 到效果图 ID 的映射。"),
        };

    public static (string label, string desc) Get(string propertyName)
    {
        if (Labels.TryGetValue(propertyName, out var pair))
        {
            return pair;
        }

        return (propertyName, string.Empty);
    }

    public static (string label, string desc) Get(SerializedProperty property)
    {
        return property == null ? (string.Empty, string.Empty) : Get(property.name);
    }
}
#endif
