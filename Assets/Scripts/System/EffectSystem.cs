using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public interface IEffectSystem : ISystem
{
    void ResolveEffectGraph(string effectGraphId, EffectContext context, ICanSendCommand commandSender);
    void ResolveAtom(EffectAtomDefinition atom, EffectContext context, ICanSendCommand commandSender);
}

public sealed class EffectSystem : AbstractSystem, IEffectSystem
{
    protected override void OnInit()
    {
    }

    public void ResolveEffectGraph(string effectGraphId, EffectContext context, ICanSendCommand commandSender)
    {
        if (!EffectGraphRegistry.TryGetGraph(effectGraphId, out var graph))
        {
            Debug.LogError($"[EffectSystem] Missing effect graph: {effectGraphId}");
            return;
        }

        for (var i = 0; i < graph.Atoms.Count; i++)
        {
            ResolveAtom(graph.Atoms[i], context, commandSender);
        }
    }

    public void ResolveAtom(EffectAtomDefinition atom, EffectContext context, ICanSendCommand commandSender)
    {
        if (atom == null || string.IsNullOrEmpty(atom.AtomType) || commandSender == null)
        {
            return;
        }

        switch (atom.AtomType)
        {
            case EffectAtomTypes.Heal:
                DispatchHeal(atom, context, commandSender);
                break;
            case EffectAtomTypes.Damage:
                commandSender.SendCommand(new ApplyEffectDamageAtomCommand(atom, context));
                break;
            case EffectAtomTypes.AddGold:
                commandSender.SendCommand(new ApplyEffectGoldCommand(EffectAtomParams.GetInt(atom, "amount", 0)));
                break;
            case EffectAtomTypes.ModifyStat:
                DispatchModifyStat(atom, context, commandSender);
                break;
            case EffectAtomTypes.ConsumeHelpCard:
                DispatchConsumeHelpCard(context, commandSender);
                break;
            case EffectAtomTypes.OpenChoiceOverlay:
                DispatchOpenChoiceOverlay(atom, context, commandSender);
                break;
            case EffectAtomTypes.OpenTargeting:
                DispatchOpenTargeting(atom, context, commandSender);
                break;
            case EffectAtomTypes.AddCardToHelpDeck:
                commandSender.SendCommand(new AddHelpCardCommand(
                    EffectAtomParams.Get(atom, "cardId"),
                    HelpCardAddPolicy.BypassDeckCapacity));
                break;
            case EffectAtomTypes.InjectCardToBattleDeck:
                commandSender.SendCommand(new InjectHelpCardsToBattleDeckCommand(EffectAtomParams.Get(atom, "cardId")));
                break;
            case EffectAtomTypes.MoveBoard:
                commandSender.SendCommand(new RotateBoardRingCommand(EffectAtomParams.Get(atom, "mode", "rotate_clockwise")));
                break;
            case EffectAtomTypes.SwapCards:
                DispatchSwapCards(context, commandSender);
                break;
            case EffectAtomTypes.RemoveCard:
                commandSender.SendCommand(new RemoveBoardCardsCommand(CollectRemoveTargets(atom, context)));
                break;
            case EffectAtomTypes.ApplyStatus:
                DispatchApplyStatus(atom, context, commandSender);
                break;
            case EffectAtomTypes.BloodConvertReward:
                commandSender.SendCommand(new ApplyBloodConvertRewardCommand());
                break;
            default:
                Debug.LogWarning($"[EffectSystem] Unknown atom type: {atom.AtomType}");
                break;
        }
    }

    private void DispatchHeal(EffectAtomDefinition atom, EffectContext context, ICanSendCommand commandSender)
    {
        var fullHeal = EffectAtomParams.GetBool(atom, "full");
        var amount = EffectAtomParams.GetInt(atom, "amount", 0);
        if (!fullHeal && amount <= 0)
        {
            return;
        }

        var target = EffectAtomParams.Get(atom, "target", "player");
        CardUid targetUid;
        if (target == "player")
        {
            targetUid = this.GetModel<IPlayerModel>().PlayerCardUid;
        }
        else if (context.Targets.Count > 0)
        {
            targetUid = context.Targets[0];
        }
        else if (context.Caster.HasValue)
        {
            targetUid = context.Caster.Value;
        }
        else
        {
            return;
        }

        var messageKey = context.Source == EffectSource.HelpCard && !fullHeal
            ? DescriptionPanelTextKeys.MsgPotionHeal
            : null;
        commandSender.SendCommand(new ApplyEffectHealCommand(targetUid, amount, fullHeal, messageKey));
    }

    private void DispatchModifyStat(EffectAtomDefinition atom, EffectContext context, ICanSendCommand commandSender)
    {
        if (!Enum.TryParse(EffectAtomParams.Get(atom, "stat", nameof(StatType.Attack)), out StatType statType))
        {
            return;
        }

        var delta = EffectAtomParams.GetInt(atom, "delta", 0);
        var mode = EffectAtomParams.Get(atom, "mode", "add");
        var target = EffectAtomParams.Get(atom, "target", "player");
        CardUid targetUid;
        if (target == "player")
        {
            targetUid = this.GetModel<IPlayerModel>().PlayerCardUid;
        }
        else if (context.Targets.Count > 0)
        {
            targetUid = context.Targets[0];
        }
        else
        {
            return;
        }

        if (mode == "double_current" && statType == StatType.Attack)
        {
            var runtime = this.GetModel<ICollectionModel>().GetCard(targetUid);
            delta = runtime.BaseAttack;
        }

        if (delta == 0)
        {
            return;
        }

        var causeId = context.Caster.HasValue
            ? this.GetModel<ICollectionModel>().GetCard(context.Caster.Value).DefinitionId
            : "effect_modify_stat";
        commandSender.SendCommand(new ApplyStatChangeCommand(targetUid, statType, delta, causeId));
    }

    private static void DispatchConsumeHelpCard(EffectContext context, ICanSendCommand commandSender)
    {
        if (!context.Caster.HasValue)
        {
            return;
        }

        commandSender.SendCommand(new ConsumeHelpCardCommand(context.Caster.Value, HelpCardConsumeReason.Used));
    }

    private void DispatchOpenChoiceOverlay(EffectAtomDefinition atom, EffectContext context, ICanSendCommand commandSender)
    {
        if (!context.Caster.HasValue)
        {
            return;
        }

        var overlayKind = EffectAtomParams.Get(atom, "overlayKind");
        switch (overlayKind)
        {
            case "attribute":
                commandSender.SendCommand(new OpenAttributeChoiceOverlayCommand(context.Caster.Value));
                break;
            case "chest":
                var tierRaw = EffectAtomParams.Get(atom, "chestTier", nameof(ChestTier.Normal));
                Enum.TryParse(tierRaw, out ChestTier chestTier);
                commandSender.SendCommand(new OpenChestRewardOverlayCommand(context.Caster.Value, chestTier));
                break;
        }
    }

    private static void DispatchOpenTargeting(EffectAtomDefinition atom, EffectContext context, ICanSendCommand commandSender)
    {
        if (!context.Caster.HasValue)
        {
            return;
        }

        commandSender.SendCommand(new OpenHelpTargetingCommand(
            context.Caster.Value,
            EffectAtomParams.GetInt(atom, "damage", 6),
            EffectAtomParams.Get(atom, "causeId", "help_targeting"),
            EffectAtomParams.Get(atom, "mode", "damage"),
            EffectAtomParams.Get(atom, "messageKey", DescriptionPanelTextKeys.MsgThrowingKnifeSelect)));
    }

    private static void DispatchSwapCards(EffectContext context, ICanSendCommand commandSender)
    {
        if (context.Targets.Count < 2)
        {
            return;
        }

        commandSender.SendCommand(new SwapBoardCardsCommand(context.Targets[0], context.Targets[1]));
    }

    private void DispatchApplyStatus(EffectAtomDefinition atom, EffectContext context, ICanSendCommand commandSender)
    {
        var status = EffectAtomParams.Get(atom, "status");
        switch (status)
        {
            case "blessing_shield":
                if (context.Caster.HasValue)
                {
                    commandSender.SendCommand(new ApplyBlessingShieldCommand(context.Caster.Value));
                }
                break;
            case "violence_attack":
                if (!context.Caster.HasValue)
                {
                    return;
                }

                var playerUid = this.GetModel<IPlayerModel>().PlayerCardUid;
                var runtime = this.GetModel<ICollectionModel>().GetCard(playerUid);
                commandSender.SendCommand(new ApplyStatChangeCommand(
                    playerUid,
                    StatType.Attack,
                    runtime.BaseAttack,
                    DefaultGameConfigFactory.HelpViolenceId));
                this.SendEvent(new GameplayMessageEvent("暴力：本节点攻击翻倍。"));
                break;
            case "tower_watch":
                this.SendEvent(new GameplayMessageEvent("瞭望塔已放置（持续效果待 R5 技能化）。"));
                break;
            case "tower_multiplier":
                this.SendEvent(new GameplayMessageEvent("倍增塔已放置（持续效果待 R5 技能化）。"));
                break;
        }
    }

    private List<CardUid> CollectRemoveTargets(EffectAtomDefinition atom, EffectContext context)
    {
        var scope = EffectAtomParams.Get(atom, "scope", "targets");
        var targets = new List<CardUid>();
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();

        if (scope == "targets")
        {
            targets.AddRange(context.Targets);
            return targets;
        }

        if (scope == "board_slot_monster")
        {
            var slotValue = EffectAtomParams.GetInt(atom, "slot", 0);
            if (slotValue <= 0)
            {
                return targets;
            }

            var boardSlot = new BoardSlotNo(slotValue);
            var uid = boardModel.GetCardAt(boardSlot);
            if (!uid.HasValue ||
                !collectionModel.TryGetCard(uid.Value, out var runtime) ||
                runtime.CardType != CardType.Monster ||
                runtime.MonsterLevel == MonsterLevel.Elite ||
                runtime.MonsterLevel == MonsterLevel.Boss)
            {
                return targets;
            }

            targets.Add(uid.Value);
            return targets;
        }

        for (var slot = 1; slot <= 9; slot++)
        {
            var boardSlot = new BoardSlotNo(slot);
            var uid = boardModel.GetCardAt(boardSlot);
            if (!uid.HasValue || !collectionModel.TryGetCard(uid.Value, out var runtime) || runtime.CardType != CardType.Monster)
            {
                continue;
            }

            if (scope == "all_monsters")
            {
                targets.Add(uid.Value);
            }
        }

        return targets;
    }
}
