using System.Collections.Generic;
using QFramework;
using UnityEngine;

public sealed class CardViewData
{
    public CardUid Uid;
    public string DefinitionId;
    public string DisplayName;
    public CardType Type;
    public CardQuality Quality;
    public int CurrentHp;
    public int MaxHp;
    public int CurrentArmor;
    public int Attack;
    public int DamageReduction;
    public bool HasFirstStrike;
    public string Description;
    public string SystemTagText;
    public string SpriteId;
    public List<string> StatusIconIds = new List<string>();
    public Color Tint;
}

public static class CardViewDataFactory
{
    public static CardViewData Create(IController controller, CardUid uid)
    {
        var collectionModel = controller.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(uid, out var runtime))
        {
            return null;
        }

        switch (runtime.CardType)
        {
            case CardType.Player:
                return CreatePlayer(controller, runtime);
            case CardType.Monster:
                return CreateMonster(controller, runtime);
            case CardType.Help:
                return CreateHelp(controller, runtime);
            default:
                return CreateFallback(runtime);
        }
    }

    public static string FormatWorldCard(CardViewData data, bool itemSlot)
    {
        return FormatWorldCard(data, itemSlot, false);
    }

    public static string FormatWorldCard(CardViewData data, bool itemSlot, bool pending)
    {
        if (data == null)
        {
            return string.Empty;
        }

        var pendingText = pending ? "\n等待操作" : string.Empty;
        switch (data.Type)
        {
            case CardType.Player:
                return $"玩家\nHP {data.CurrentHp}/{data.MaxHp}\nATK {data.Attack} ARM {data.CurrentArmor}{pendingText}";
            case CardType.Monster:
                var firstStrike = data.HasFirstStrike ? "\n先攻" : string.Empty;
                var damageReduction = data.DamageReduction > 0 ? $" DR {data.DamageReduction}" : string.Empty;
                return $"{data.DisplayName}\nHP {data.CurrentHp}/{data.MaxHp}\nATK {data.Attack} ARM {data.CurrentArmor}{damageReduction}{firstStrike}{pendingText}";
            case CardType.Help:
                return itemSlot
                    ? $"{data.DisplayName}\n道具槽\n点击使用{pendingText}"
                    : $"{data.DisplayName}\n帮助卡\n点击拾取{pendingText}";
            default:
                return $"{data.DisplayName}{pendingText}";
        }
    }

    private static CardViewData CreatePlayer(IController controller, CardRuntime runtime)
    {
        var stats = controller.SendQuery(new GetEffectivePlayerStatsQuery());
        return new CardViewData
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId,
            DisplayName = runtime.DisplayName,
            Type = runtime.CardType,
            Quality = CardQuality.Initial,
            CurrentHp = stats.CurrentHp,
            MaxHp = stats.MaxHp,
            CurrentArmor = stats.CurrentArmor,
            Attack = stats.Attack,
            DamageReduction = stats.DamageReduction,
            HasFirstStrike = stats.HasFirstStrike,
            SpriteId = runtime.DefinitionId,
            StatusIconIds = CollectPlayerStatusIcons(controller, runtime),
            Tint = new Color(0.55f, 0.85f, 0.55f)
        };
    }

    private static CardViewData CreateMonster(IController controller, CardRuntime runtime)
    {
        var stats = controller.SendQuery(new GetEffectiveMonsterStatsQuery(runtime.Uid));
        return new CardViewData
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId,
            DisplayName = runtime.DisplayName,
            Type = runtime.CardType,
            Quality = CardQuality.Initial,
            CurrentHp = stats.CurrentHp,
            MaxHp = stats.MaxHp,
            CurrentArmor = stats.CurrentArmor,
            Attack = stats.Attack,
            DamageReduction = stats.DamageReduction,
            HasFirstStrike = stats.HasFirstStrike,
            SpriteId = runtime.DefinitionId,
            StatusIconIds = new List<string>(runtime.SkillIds),
            Tint = new Color(0.92f, 0.62f, 0.62f)
        };
    }

    private static CardViewData CreateHelp(IController controller, CardRuntime runtime)
    {
        var definition = controller.GetModel<IConfigModel>().GetCardDefinition(runtime.DefinitionId);
        return new CardViewData
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId,
            DisplayName = runtime.DisplayName,
            Type = runtime.CardType,
            Quality = definition != null ? definition.Quality : CardQuality.White,
            Description = definition != null ? definition.Description : string.Empty,
            SystemTagText = definition != null ? HelpCardSystemTagUtility.ToDisplayName(definition.SystemTag) : string.Empty,
            SpriteId = runtime.DefinitionId,
            StatusIconIds = definition != null ? new List<string>(definition.SkillIds) : new List<string>(),
            Tint = ResolveHelpColor(definition != null ? definition.Quality : CardQuality.White)
        };
    }

    private static CardViewData CreateFallback(CardRuntime runtime)
    {
        return new CardViewData
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId,
            DisplayName = runtime.DisplayName,
            Type = runtime.CardType,
            SpriteId = runtime.DefinitionId,
            StatusIconIds = new List<string>(runtime.SkillIds),
            Tint = new Color(0.9f, 0.9f, 0.9f)
        };
    }

    private static List<string> CollectPlayerStatusIcons(IController controller, CardRuntime runtime)
    {
        var icons = new List<string>(runtime.SkillIds);
        var playerModel = controller.GetModel<IPlayerModel>();
        for (var i = 0; i < playerModel.SkillIds.Count; i++)
        {
            if (!icons.Contains(playerModel.SkillIds[i]))
            {
                icons.Add(playerModel.SkillIds[i]);
            }
        }

        for (var i = 0; i < playerModel.Relics.Count; i++)
        {
            if (!playerModel.Relics[i].IsConsumed)
            {
                icons.Add(playerModel.Relics[i].RelicId);
            }
        }

        return icons;
    }

    private static Color ResolveHelpColor(CardQuality quality)
    {
        switch (quality)
        {
            case CardQuality.White:
                return new Color(0.96f, 0.96f, 0.92f);
            case CardQuality.Blue:
                return new Color(0.62f, 0.79f, 0.96f);
            case CardQuality.Gold:
                return new Color(0.95f, 0.84f, 0.42f);
            case CardQuality.Red:
                return new Color(0.92f, 0.46f, 0.46f);
            default:
                return new Color(0.9f, 0.9f, 0.9f);
        }
    }
}
