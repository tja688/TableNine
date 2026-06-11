using System.Collections.Generic;

public static class Layer1MonsterDeckRules
{
    public static void Populate(GameConfigSet config)
    {
        config.MonsterDeckRules.Clear();

        AddRule(config, 1, Quota(MonsterLevel.Level1, 6, 8, DefaultGameConfigFactory.Layer1Level1MonsterIds),
            QuotaRemainder(MonsterLevel.Level2, 2, 4, DefaultGameConfigFactory.Layer1Level2MonsterIds));

        AddRule(config, 2, Quota(MonsterLevel.Level1, 5, 7, DefaultGameConfigFactory.Layer1Level1MonsterIds),
            QuotaRemainder(MonsterLevel.Level2, 3, 5, DefaultGameConfigFactory.Layer1Level2MonsterIds));

        AddRule(config, 3,
            Quota(MonsterLevel.Level1, 2, 4, DefaultGameConfigFactory.Layer1Level1MonsterIds),
            Quota(MonsterLevel.Level2, 3, 5, DefaultGameConfigFactory.Layer1Level2MonsterIds),
            QuotaRemainder(MonsterLevel.Level3, 2, 4, DefaultGameConfigFactory.Layer1Level3MonsterIds));

        AddRule(config, 4,
            Quota(MonsterLevel.Level1, 1, 3, DefaultGameConfigFactory.Layer1Level1MonsterIds),
            Quota(MonsterLevel.Level2, 3, 7, DefaultGameConfigFactory.Layer1Level2MonsterIds),
            QuotaRemainder(MonsterLevel.Level3, 3, 6, DefaultGameConfigFactory.Layer1Level3MonsterIds));

        AddRule(config, 5,
            Quota(MonsterLevel.Level1, 1, 1, DefaultGameConfigFactory.Layer1Level1MonsterIds),
            Quota(MonsterLevel.Level2, 3, 5, DefaultGameConfigFactory.Layer1Level2MonsterIds),
            Quota(MonsterLevel.Level3, 3, 5, DefaultGameConfigFactory.Layer1Level3MonsterIds),
            Quota(MonsterLevel.Level4, 2, 2, DefaultGameConfigFactory.Layer1Level4MonsterIds),
            mandatory: new[] { DefaultGameConfigFactory.MonsterSpadeEliteId });

        AddRule(config, 6,
            Quota(MonsterLevel.Level1, 0, 1, DefaultGameConfigFactory.Layer1Level1MonsterIds),
            Quota(MonsterLevel.Level2, 2, 5, DefaultGameConfigFactory.Layer1Level2MonsterIds),
            Quota(MonsterLevel.Level3, 3, 6, DefaultGameConfigFactory.Layer1Level3MonsterIds),
            QuotaRemainder(MonsterLevel.Level4, 1, 5, DefaultGameConfigFactory.Layer1Level4MonsterIds));

        AddRule(config, 7,
            Quota(MonsterLevel.Level2, 1, 4, DefaultGameConfigFactory.Layer1Level2MonsterIds),
            Quota(MonsterLevel.Level3, 3, 8, DefaultGameConfigFactory.Layer1Level3MonsterIds),
            QuotaRemainder(MonsterLevel.Level4, 3, 6, DefaultGameConfigFactory.Layer1Level4MonsterIds));

        AddRule(config, 8,
            Quota(MonsterLevel.Level2, 1, 4, DefaultGameConfigFactory.Layer1Level2MonsterIds),
            Quota(MonsterLevel.Level3, 3, 10, DefaultGameConfigFactory.Layer1Level3MonsterIds),
            QuotaRemainder(MonsterLevel.Level4, 3, 8, DefaultGameConfigFactory.Layer1Level4MonsterIds));

        AddRule(config, 9,
            Quota(MonsterLevel.Level2, 1, 2, DefaultGameConfigFactory.Layer1Level2MonsterIds),
            Quota(MonsterLevel.Level3, 3, 10, DefaultGameConfigFactory.Layer1Level3MonsterIds),
            QuotaRemainder(MonsterLevel.Level4, 3, 10, DefaultGameConfigFactory.Layer1Level4MonsterIds),
            mandatory: new[] { DefaultGameConfigFactory.MonsterSpadeBossId });
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
        int nodeInLayer,
        MonsterLevelQuotaDefinition quotaA,
        MonsterLevelQuotaDefinition quotaB = null,
        MonsterLevelQuotaDefinition quotaC = null,
        MonsterLevelQuotaDefinition quotaD = null,
        string[] mandatory = null)
    {
        var rule = new MonsterDeckRuleDefinition
        {
            Layer = 1,
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
