using System.Collections.Generic;

/// <summary>
/// 3 层 × 9 节点恶魔卡组规则（每层节点配额相同，精英/层主按层切换）。
/// </summary>
public static class AllLayersMonsterDeckRules
{
    public static void Populate(GameConfigSet config)
    {
        config.MonsterDeckRules.Clear();

        for (var layer = 1; layer <= 3; layer++)
        {
            PopulateLayer(config, layer);
        }
    }

    private static void PopulateLayer(GameConfigSet config, int layer)
    {
        var level1 = DefaultGameConfigFactory.GetLevelMonsterIds(layer, MonsterLevel.Level1);
        var level2 = DefaultGameConfigFactory.GetLevelMonsterIds(layer, MonsterLevel.Level2);
        var level3 = DefaultGameConfigFactory.GetLevelMonsterIds(layer, MonsterLevel.Level3);
        var level4 = DefaultGameConfigFactory.GetLevelMonsterIds(layer, MonsterLevel.Level4);
        var eliteId = GameConfigIds.GetEliteMonsterId(layer);
        var bossId = GameConfigIds.GetBossMonsterId(layer);

        AddRule(config, layer, 1, Quota(MonsterLevel.Level1, 6, 8, level1),
            QuotaRemainder(MonsterLevel.Level2, 2, 4, level2));

        AddRule(config, layer, 2, Quota(MonsterLevel.Level1, 5, 7, level1),
            QuotaRemainder(MonsterLevel.Level2, 3, 5, level2));

        AddRule(config, layer, 3,
            Quota(MonsterLevel.Level1, 2, 4, level1),
            Quota(MonsterLevel.Level2, 3, 5, level2),
            QuotaRemainder(MonsterLevel.Level3, 2, 4, level3));

        AddRule(config, layer, 4,
            Quota(MonsterLevel.Level1, 1, 3, level1),
            Quota(MonsterLevel.Level2, 3, 7, level2),
            QuotaRemainder(MonsterLevel.Level3, 3, 6, level3));

        AddRule(config, layer, 5,
            Quota(MonsterLevel.Level1, 1, 1, level1),
            Quota(MonsterLevel.Level2, 3, 5, level2),
            Quota(MonsterLevel.Level3, 3, 5, level3),
            Quota(MonsterLevel.Level4, 2, 2, level4),
            mandatory: new[] { eliteId });

        AddRule(config, layer, 6,
            Quota(MonsterLevel.Level1, 0, 1, level1),
            Quota(MonsterLevel.Level2, 2, 5, level2),
            Quota(MonsterLevel.Level3, 3, 6, level3),
            QuotaRemainder(MonsterLevel.Level4, 1, 5, level4));

        AddRule(config, layer, 7,
            Quota(MonsterLevel.Level2, 1, 4, level2),
            Quota(MonsterLevel.Level3, 3, 8, level3),
            QuotaRemainder(MonsterLevel.Level4, 3, 6, level4));

        AddRule(config, layer, 8,
            Quota(MonsterLevel.Level2, 1, 4, level2),
            Quota(MonsterLevel.Level3, 3, 10, level3),
            QuotaRemainder(MonsterLevel.Level4, 3, 8, level4));

        AddRule(config, layer, 9,
            Quota(MonsterLevel.Level2, 1, 2, level2),
            Quota(MonsterLevel.Level3, 3, 10, level3),
            QuotaRemainder(MonsterLevel.Level4, 3, 10, level4),
            mandatory: new[] { bossId });
    }

    private static MonsterLevelQuotaDefinition Quota(MonsterLevel level, int min, int max, IReadOnlyList<string> pool)
    {
        return new MonsterLevelQuotaDefinition
        {
            Level = level,
            MinCount = min,
            MaxCount = max,
            PoolCardIds = new List<string>(pool)
        };
    }

    private static MonsterLevelQuotaDefinition QuotaRemainder(MonsterLevel level, int min, int max, IReadOnlyList<string> pool)
    {
        return Quota(level, min, max, pool);
    }

    private static void AddRule(
        GameConfigSet config,
        int layer,
        int nodeInLayer,
        MonsterLevelQuotaDefinition quotaA,
        MonsterLevelQuotaDefinition quotaB = null,
        MonsterLevelQuotaDefinition quotaC = null,
        MonsterLevelQuotaDefinition quotaD = null,
        string[] mandatory = null)
    {
        var rule = new MonsterDeckRuleDefinition
        {
            Layer = layer,
            NodeInLayer = nodeInLayer,
            TotalCardCount = 9 + nodeInLayer
        };

        AppendQuota(rule, quotaA);
        AppendQuota(rule, quotaB);
        AppendQuota(rule, quotaC);
        AppendQuota(rule, quotaD);

        if (mandatory != null)
        {
            rule.MandatoryMonsterCardIds.AddRange(mandatory);
        }

        BuildAllowedMonsterPool(rule);
        config.MonsterDeckRules.Add(rule);
    }

    private static void AppendQuota(MonsterDeckRuleDefinition rule, MonsterLevelQuotaDefinition quota)
    {
        if (quota == null)
        {
            return;
        }

        rule.LevelQuotas.Add(quota);
    }

    private static void BuildAllowedMonsterPool(MonsterDeckRuleDefinition rule)
    {
        var seen = new HashSet<string>();
        for (var i = 0; i < rule.LevelQuotas.Count; i++)
        {
            var pool = rule.LevelQuotas[i].PoolCardIds;
            for (var j = 0; j < pool.Count; j++)
            {
                if (seen.Add(pool[j]))
                {
                    rule.AllowedMonsterCardIds.Add(pool[j]);
                }
            }
        }

        for (var i = 0; i < rule.MandatoryMonsterCardIds.Count; i++)
        {
            var id = rule.MandatoryMonsterCardIds[i];
            if (seen.Add(id))
            {
                rule.AllowedMonsterCardIds.Add(id);
            }
        }
    }
}
