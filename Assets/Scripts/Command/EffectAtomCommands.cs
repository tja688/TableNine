using System.Collections.Generic;
using QFramework;

public sealed class ApplyEffectHealCommand : AbstractCommand
{
    public ApplyEffectHealCommand(CardUid targetUid, int amount, bool fullHeal, string messageKey = null)
    {
        TargetUid = targetUid;
        Amount = amount;
        FullHeal = fullHeal;
        MessageKey = messageKey;
    }

    public CardUid TargetUid { get; }
    public int Amount { get; }
    public bool FullHeal { get; }
    public string MessageKey { get; }

    protected override void OnExecute()
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(TargetUid, out var runtime))
        {
            return;
        }

        var oldHp = runtime.CurrentHp;
        if (FullHeal)
        {
            runtime.CurrentHp = runtime.MaxHp;
        }
        else if (Amount > 0)
        {
            runtime.CurrentHp += Amount;
            if (runtime.CurrentHp > runtime.MaxHp)
            {
                runtime.CurrentHp = runtime.MaxHp;
            }
        }
        else
        {
            return;
        }

        if (runtime.CurrentHp != oldHp)
        {
            this.SendEvent(new HealAppliedEvent(
                TargetUid,
                oldHp,
                runtime.CurrentHp,
                runtime.CurrentHp - oldHp,
                string.IsNullOrEmpty(MessageKey) ? "effect_heal" : MessageKey));
            this.SendEvent(new StatsDirtyEvent(TargetUid));
        }

        if (!string.IsNullOrEmpty(MessageKey))
        {
            this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(MessageKey)));
        }
    }
}

public sealed class ApplyEffectGoldCommand : AbstractCommand
{
    public ApplyEffectGoldCommand(int amount)
    {
        Amount = amount;
    }

    public int Amount { get; }

    protected override void OnExecute()
    {
        if (Amount == 0)
        {
            return;
        }

        this.ChangeGold(this.GetModel<IPlayerModel>(), Amount);
        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Format(
            DescriptionPanelTextKeys.MsgRoomGold,
            Amount)));
    }
}

public sealed class ApplyBloodConvertRewardCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var randomUtility = this.GetUtility<IRandomUtility>();
        var playerModel = this.GetModel<IPlayerModel>();
        var playerUid = playerModel.PlayerCardUid;
        var causeId = DefaultGameConfigFactory.HelpBloodConvertId;

        switch (randomUtility.Range(0, 4))
        {
            case 0:
                this.SendCommand(new ApplyStatChangeCommand(playerUid, StatType.Attack, 1, causeId));
                break;
            case 1:
                this.SendCommand(new ApplyStatChangeCommand(playerUid, StatType.Defense, 1, causeId));
                break;
            case 2:
                this.SendCommand(new ApplyEffectGoldCommand(50));
                break;
            default:
                GrantRandomRelic();
                break;
        }
    }

    private void GrantRandomRelic()
    {
        var configModel = this.GetModel<IConfigModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var relicSystem = this.GetSystem<IRelicSystem>();
        if (playerModel.Relics.Count >= playerModel.MaxRelicCount)
        {
            this.SendCommand(new ApplyEffectGoldCommand(50));
            return;
        }

        var randomUtility = this.GetUtility<IRandomUtility>();
        var candidates = new List<string>();
        foreach (var relic in configModel.GetAllRelicDefinitions())
        {
            if (relic.ExcludeFromPool || playerModel.HasRelic(relic.RelicId))
            {
                continue;
            }

            candidates.Add(relic.RelicId);
        }

        if (candidates.Count == 0)
        {
            this.SendCommand(new ApplyEffectGoldCommand(50));
            return;
        }

        relicSystem.AddRelic(candidates[randomUtility.Range(0, candidates.Count)]);
    }
}

public sealed class OpenAttributeChoiceOverlayCommand : AbstractCommand
{
    public OpenAttributeChoiceOverlayCommand(CardUid helpCardUid)
    {
        HelpCardUid = helpCardUid;
    }

    public CardUid HelpCardUid { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();

        deckModel.PendingHelpCardAction.HelpCardUid = HelpCardUid;
        deckModel.PendingHelpCardAction.Kind = PendingHelpCardActionKind.AttributeChoice;
        inputLockSystem.Lock(InputLockReason.OverlayVisible);
        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(DescriptionPanelTextKeys.MsgAttributeSelect)));
        this.SendEvent(new AttributeChoiceRequestedEvent(HelpCardUid));
    }
}

public sealed class OpenChestRewardOverlayCommand : AbstractCommand
{
    public OpenChestRewardOverlayCommand(CardUid helpCardUid, ChestTier chestTier)
    {
        HelpCardUid = helpCardUid;
        ChestTier = chestTier;
    }

    public CardUid HelpCardUid { get; }
    public ChestTier ChestTier { get; }

    protected override void OnExecute()
    {
        var flowModel = this.GetModel<IFlowModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();

        rewardModel.RewardResumePhase = flowModel.Phase.Value;
        rewardModel.CurrentRewardSource = RewardSource.ChestCard;
        flowModel.SetPhase(FlowPhase.ChestRewardChoosing);
        inputLockSystem.Lock(InputLockReason.OverlayVisible);
        this.GetSystem<IRelicSystem>().GenerateChestRewardCandidates(ChestTier);
        this.SendEvent(new ChestRewardGeneratedEvent(rewardModel.ChestRewardRelicIds));
    }
}

public sealed class OpenHelpTargetingCommand : AbstractCommand
{
    public OpenHelpTargetingCommand(
        CardUid helpCardUid,
        int damage,
        string causeId,
        string mode,
        string messageKey)
    {
        HelpCardUid = helpCardUid;
        Damage = damage;
        CauseId = causeId;
        Mode = mode;
        MessageKey = messageKey;
    }

    public CardUid HelpCardUid { get; }
    public int Damage { get; }
    public string CauseId { get; }
    public string Mode { get; }
    public string MessageKey { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        var flowModel = this.GetModel<IFlowModel>();

        deckModel.PendingHelpCardAction.HelpCardUid = HelpCardUid;
        deckModel.PendingHelpCardAction.TargetingDamage = Damage;
        deckModel.PendingHelpCardAction.TargetingCauseId = causeIdOrDefault(CauseId);
        deckModel.PendingHelpCardAction.TargetingMode = Mode;
        deckModel.PendingHelpCardAction.SwapFirstTargetUid = null;
        deckModel.PendingHelpCardAction.SwapFirstTargetSelected = false;

        if (Mode == "swap")
        {
            deckModel.PendingHelpCardAction.Kind = PendingHelpCardActionKind.SwapTarget;
        }
        else
        {
            deckModel.PendingHelpCardAction.Kind = PendingHelpCardActionKind.ThrowingKnifeTarget;
            if (flowModel.Phase.Value != FlowPhase.RoomChoosing)
            {
                flowModel.SetPhase(FlowPhase.PlayerControl);
            }
        }

        var messageKey = string.IsNullOrEmpty(MessageKey)
            ? DescriptionPanelTextKeys.MsgThrowingKnifeSelect
            : MessageKey;
        this.SendEvent(new GameplayMessageEvent(DescriptionPanelTexts.Get(messageKey)));
    }

    private static string causeIdOrDefault(string causeId)
    {
        return string.IsNullOrEmpty(causeId) ? "help_targeting" : causeId;
    }
}

public sealed class RotateBoardRingCommand : AbstractCommand
{
    public RotateBoardRingCommand(string mode)
    {
        Mode = mode;
    }

    public string Mode { get; }

    protected override void OnExecute()
    {
        var flowModel = this.GetModel<IFlowModel>();
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        inputLockSystem.Lock(InputLockReason.BoardMoving);
        flowModel.SetPhase(FlowPhase.BoardMoving);

        if (Mode == "rotate_counterclockwise")
        {
            boardSystem.RotateCounterclockwise(BoardMoveReason.HelpCard);
        }
        else if (Mode == "rotate_clockwise")
        {
            boardSystem.RotateClockwise(BoardMoveReason.HelpCard);
        }

        this.SendCommand(new PlayPresentationSequenceCommand(
            PresentationSequenceType.BoardRotation,
            SequenceCompletionAction.ResumeAfterBoardRotation));
    }
}

public sealed class SwapBoardCardsCommand : AbstractCommand
{
    public SwapBoardCardsCommand(CardUid first, CardUid second)
    {
        First = first;
        Second = second;
    }

    public CardUid First { get; }
    public CardUid Second { get; }

    protected override void OnExecute()
    {
        var boardSystem = this.GetSystem<IBoardSystem>();
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(First, out var firstRuntime) ||
            !collectionModel.TryGetCard(Second, out var secondRuntime) ||
            !firstRuntime.BoardSlot.HasValue ||
            !secondRuntime.BoardSlot.HasValue)
        {
            return;
        }

        var firstSlot = firstRuntime.BoardSlot.Value;
        var secondSlot = secondRuntime.BoardSlot.Value;
        boardSystem.RemoveCardAt(firstSlot, RemoveReason.Effect);
        boardSystem.RemoveCardAt(secondSlot, RemoveReason.Effect);
        boardSystem.PlaceCard(First, secondSlot, CardPlacementSource.Refill);
        boardSystem.PlaceCard(Second, firstSlot, CardPlacementSource.Refill);
        this.SendEvent(new BoardSlotChangedEvent(firstSlot, boardModel.GetCardAt(firstSlot)));
        this.SendEvent(new BoardSlotChangedEvent(secondSlot, boardModel.GetCardAt(secondSlot)));
    }
}

public sealed class RemoveBoardCardsCommand : AbstractCommand
{
    public RemoveBoardCardsCommand(IReadOnlyList<CardUid> targets)
    {
        Targets = targets;
    }

    public IReadOnlyList<CardUid> Targets { get; }

    protected override void OnExecute()
    {
        var boardSystem = this.GetSystem<IBoardSystem>();
        var collectionModel = this.GetModel<ICollectionModel>();
        for (var i = 0; i < Targets.Count; i++)
        {
            if (!collectionModel.TryGetCard(Targets[i], out var runtime) || !runtime.BoardSlot.HasValue)
            {
                continue;
            }

            boardSystem.RemoveCardAt(runtime.BoardSlot.Value, RemoveReason.Effect);
            collectionModel.RemoveCard(Targets[i]);
        }
    }
}

public sealed class ApplyBlessingShieldCommand : AbstractCommand
{
    public ApplyBlessingShieldCommand(CardUid helpCardUid)
    {
        HelpCardUid = helpCardUid;
    }

    public CardUid HelpCardUid { get; }

    protected override void OnExecute()
    {
        var deckModel = this.GetModel<IDeckModel>();
        deckModel.PendingHelpCardAction.HelpCardUid = HelpCardUid;
        deckModel.PendingHelpCardAction.Kind = PendingHelpCardActionKind.BlessingShield;
        this.SendEvent(new GameplayMessageEvent("庇佑已生效：下一次受到伤害为0。"));
    }
}

public sealed class InjectHelpCardsToBattleDeckCommand : AbstractCommand
{
    public InjectHelpCardsToBattleDeckCommand(string cardId)
    {
        CardId = cardId;
    }

    public string CardId { get; }

    protected override void OnExecute()
    {
        if (string.IsNullOrEmpty(CardId))
        {
            return;
        }

        this.GetSystem<IDeckSystem>().InjectHelpCardsToBattleDeck(new[] { CardId });
    }
}

public sealed class ApplyEffectDamageAtomCommand : AbstractCommand
{
    public ApplyEffectDamageAtomCommand(EffectAtomDefinition atom, EffectContext context)
    {
        Atom = atom;
        Context = context;
    }

    public EffectAtomDefinition Atom { get; }
    public EffectContext Context { get; }

    protected override void OnExecute()
    {
        var amount = ResolveDamageAmount(Atom);
        if (amount <= 0)
        {
            return;
        }

        var scope = EffectAtomParams.Get(Atom, "scope", "targets");
        var causeId = EffectAtomParams.Get(Atom, "causeId", "effect_damage");
        var targets = CollectDamageTargets(scope, Context);
        if (targets.Count == 0)
        {
            return;
        }

        var anyKilled = false;
        var collectionModel = this.GetModel<ICollectionModel>();
        for (var i = 0; i < targets.Count; i++)
        {
            var damage = new DamageContext
            {
                Target = targets[i],
                CauseId = causeId,
                Type = DamageType.HelpCard,
                RawAttack = amount,
                DamageBeforeArmor = amount
            };
            this.SendCommand(new ApplyDamageCommand(damage));
            if (!collectionModel.TryGetCard(targets[i], out var runtime) || runtime.CurrentHp > 0)
            {
                continue;
            }

            anyKilled = true;
            this.SendCommand(new KillMonsterCommand(targets[i]));
        }

        if (anyKilled)
        {
            this.SendCommand(new RequestRefillBoardCommand());
        }
        else
        {
            this.SendCommand(new CheckClearConditionCommand());
        }
    }

    private int ResolveDamageAmount(EffectAtomDefinition atom)
    {
        var amountSource = EffectAtomParams.Get(atom, "amountSource");
        if (amountSource == "player_attack")
        {
            var playerUid = this.GetModel<IPlayerModel>().PlayerCardUid;
            return this.GetModel<ICollectionModel>().GetCard(playerUid).BaseAttack;
        }

        if (amountSource == "player_current_hp")
        {
            var playerUid = this.GetModel<IPlayerModel>().PlayerCardUid;
            return this.GetModel<ICollectionModel>().GetCard(playerUid).CurrentHp;
        }

        return EffectAtomParams.GetInt(atom, "amount", 0);
    }

    private List<CardUid> CollectDamageTargets(string scope, EffectContext context)
    {
        var targets = new List<CardUid>();
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();

        if (scope == "targets")
        {
            targets.AddRange(context.Targets);
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
                continue;
            }

            if (scope == "top_row_monsters" && BoardSlotUtility.IsTopRow(boardSlot))
            {
                targets.Add(uid.Value);
            }
        }

        return targets;
    }
}
