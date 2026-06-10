using System.Collections.Generic;
using QFramework;
using UnityEngine;

public sealed class CardViewData
{
    public CardUid Uid;
    public string DisplayName;
    public CardType Type;
    public CardQuality Quality;
    public int CurrentHp;
    public int MaxHp;
    public int Attack;
    public int Defense;
    public bool HasFirstStrike;
    public string Description;
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
        if (data == null)
        {
            return string.Empty;
        }

        switch (data.Type)
        {
            case CardType.Player:
                return $"玩家\nHP {data.CurrentHp}/{data.MaxHp}\nATK {data.Attack} DEF {data.Defense}";
            case CardType.Monster:
                var firstStrike = data.HasFirstStrike ? "\n先攻" : string.Empty;
                return $"{data.DisplayName}\nHP {data.CurrentHp}/{data.MaxHp}\nATK {data.Attack} DEF {data.Defense}{firstStrike}";
            case CardType.Help:
                return itemSlot
                    ? $"{data.DisplayName}\n道具槽\n点击使用"
                    : $"{data.DisplayName}\n帮助卡\n点击拾取";
            default:
                return data.DisplayName;
        }
    }

    private static CardViewData CreatePlayer(IController controller, CardRuntime runtime)
    {
        var stats = controller.SendQuery(new GetEffectivePlayerStatsQuery());
        return new CardViewData
        {
            Uid = runtime.Uid,
            DisplayName = runtime.DisplayName,
            Type = runtime.CardType,
            Quality = CardQuality.Initial,
            CurrentHp = stats.CurrentHp,
            MaxHp = stats.MaxHp,
            Attack = stats.Attack,
            Defense = stats.Defense,
            HasFirstStrike = stats.HasFirstStrike,
            Tint = new Color(0.55f, 0.85f, 0.55f)
        };
    }

    private static CardViewData CreateMonster(IController controller, CardRuntime runtime)
    {
        var stats = controller.SendQuery(new GetEffectiveMonsterStatsQuery(runtime.Uid));
        return new CardViewData
        {
            Uid = runtime.Uid,
            DisplayName = runtime.DisplayName,
            Type = runtime.CardType,
            Quality = CardQuality.Initial,
            CurrentHp = stats.CurrentHp,
            MaxHp = stats.MaxHp,
            Attack = stats.Attack,
            Defense = stats.Defense,
            HasFirstStrike = stats.HasFirstStrike,
            Tint = new Color(0.92f, 0.62f, 0.62f)
        };
    }

    private static CardViewData CreateHelp(IController controller, CardRuntime runtime)
    {
        var definition = controller.GetModel<IConfigModel>().GetCardDefinition(runtime.DefinitionId);
        return new CardViewData
        {
            Uid = runtime.Uid,
            DisplayName = runtime.DisplayName,
            Type = runtime.CardType,
            Quality = definition != null ? definition.Quality : CardQuality.White,
            Description = definition != null ? definition.DisplayName : runtime.DisplayName,
            Tint = ResolveHelpColor(definition != null ? definition.Quality : CardQuality.White)
        };
    }

    private static CardViewData CreateFallback(CardRuntime runtime)
    {
        return new CardViewData
        {
            Uid = runtime.Uid,
            DisplayName = runtime.DisplayName,
            Type = runtime.CardType,
            Tint = new Color(0.9f, 0.9f, 0.9f)
        };
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
