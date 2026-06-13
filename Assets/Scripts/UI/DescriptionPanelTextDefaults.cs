using System.Collections.Generic;

public sealed class DescriptionPanelTextEntry
{
    public string Key;
    public string Label;
    public string Text;
    public string UsageNote;
}

public static class DescriptionPanelTextDefaults
{
    public static string GetFallback(string key)
    {
        var defaults = CreateRecommendedEntries();
        for (var i = 0; i < defaults.Count; i++)
        {
            if (defaults[i].Key == key)
            {
                return defaults[i].Text ?? string.Empty;
            }
        }

        return string.Empty;
    }

    public static List<DescriptionPanelTextEntry> CreateRecommendedEntries()
    {
        return new List<DescriptionPanelTextEntry>
        {
            Entry(DescriptionPanelTextKeys.HudDefaultHint, "默认引导", "相邻格交互，帮助卡点道具槽", "UIGameplayPanel 初始提示"),
            Entry(DescriptionPanelTextKeys.HudPreparing, "准备节点", "正在准备首个可玩节点。", "Run 未激活时"),
            Entry(DescriptionPanelTextKeys.HudThrowingKnifeReady, "飞刀瞄准", "飞刀待命：点击怪物造成6伤", "PendingHelpCardAction.ThrowingKnifeTarget"),
            Entry(DescriptionPanelTextKeys.HudAttributeChoice, "属性选择", "属性提升：请在覆盖层选择", "PendingHelpCardAction.AttributeChoice"),
            Entry(DescriptionPanelTextKeys.HudNodeClearReady, "节点清空", "节点已清空，可拾取或用卡", "FlowPhase.ClearReady"),
            Entry(DescriptionPanelTextKeys.HudMonsterKilled, "击杀反馈", "击杀怪物，获得5金币", "MonsterKilledEvent"),
            Entry(DescriptionPanelTextKeys.HudLevelClear, "层节点清空", "第{0}层第{1}节点已清空", "LevelClearReadyEvent，模板"),
            Entry(DescriptionPanelTextKeys.MsgPotionHeal, "恢复药水", "恢复药水：恢复10生命", "UseHelpCardCommand"),
            Entry(DescriptionPanelTextKeys.MsgThrowingKnifeSelect, "飞刀选目标", "飞刀待命：请选择怪物", "UseHelpCardCommand"),
            Entry(DescriptionPanelTextKeys.MsgAttributeSelect, "属性卡选目标", "属性提升：请选择属性", "UseHelpCardCommand"),
            Entry(DescriptionPanelTextKeys.MsgChestFallback, "宝箱临时", "宝箱卡暂换20金币", "UseHelpCardCommand"),
            Entry(DescriptionPanelTextKeys.MsgNotImplemented, "未接入", "{0}暂未接入效果", "UseHelpCardCommand，模板"),
            Entry(DescriptionPanelTextKeys.MsgThrowingKnifeHit, "飞刀命中", "飞刀命中{0}，6点伤害", "ResolveThrowingKnifeTargetCommand，模板"),
            Entry(DescriptionPanelTextKeys.MsgAttrAttack, "攻击提升", "属性提升：攻击+1", "ResolveAttributeChoiceCommand"),
            Entry(DescriptionPanelTextKeys.MsgAttrDefense, "防御提升", "属性提升：防御+1", "ResolveAttributeChoiceCommand"),
            Entry(DescriptionPanelTextKeys.MsgAttrMaxHp, "生命提升", "属性提升：生命+2", "ResolveAttributeChoiceCommand"),
            Entry(DescriptionPanelTextKeys.MsgRoomGold, "金币房", "金币房：获得{0}金币", "ChooseRoomCommand，模板"),
            Entry(DescriptionPanelTextKeys.MsgRoomAttribute, "温泉房", "温泉房：生命上限+{0}并回满生命", "ChooseRoomCommand，模板"),
            Entry(DescriptionPanelTextKeys.MsgHelpCardGained, "获得帮助卡", "获得帮助卡：{0}", "PickHelpCardRewardCommand，模板"),
            Entry(DescriptionPanelTextKeys.MsgHelpRewardSkip, "跳过选卡", "跳过选卡，+10金币", "SkipHelpRewardCommand"),
            Entry(DescriptionPanelTextKeys.MsgChestSkip, "跳过宝箱", "跳过宝箱，+20金币", "SkipChestRewardCommand"),
            Entry(DescriptionPanelTextKeys.MsgVictory, "通关", "恭喜通关！", "ProceedToNextNodeCommand"),
            Entry(DescriptionPanelTextKeys.MsgLayerComplete, "层通关", "恭喜！第{0}层 9 个节点已全部通过。", "ProceedToNextNodeCommand"),
            Entry(DescriptionPanelTextKeys.MsgTutorSkill, "导师技能", "获得导师技能：{0}", "PickTutorSkillCommand，模板")
        };
    }

    private static DescriptionPanelTextEntry Entry(string key, string label, string text, string usageNote)
    {
        return new DescriptionPanelTextEntry
        {
            Key = key,
            Label = label,
            Text = text,
            UsageNote = usageNote
        };
    }
}
