using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public interface ISkillSystem : ISystem
{
    void Trigger(SkillTrigger trigger, TriggerContext context, ICanSendCommand commandSender);
    void CollectParallelDamage(TriggerContext context, List<DamageContext> output);
    IReadOnlyList<SkillTriggerLog> RecentLogs { get; }
    void ResetTriggerDepth();
    void BeginRootCommand();
}

internal sealed class EffectTriggerQueue
{
    public const int MaxDepth = 8;

    private int mDepth;
    private readonly HashSet<string> mFiredKeys = new HashSet<string>();

    public void BeginRootCommand()
    {
        mDepth = 0;
        mFiredKeys.Clear();
    }

    public bool TryEnter(string key, out int depth)
    {
        depth = mDepth + 1;
        if (mDepth >= MaxDepth)
        {
            return false;
        }

        if (mFiredKeys.Contains(key))
        {
            return false;
        }

        mDepth++;
        mFiredKeys.Add(key);
        return true;
    }

    public void Exit()
    {
        mDepth = Math.Max(0, mDepth - 1);
    }
}

public sealed class SkillSystem : AbstractSystem, ISkillSystem
{
    private readonly EffectTriggerQueue mQueue = new EffectTriggerQueue(); 
    private readonly List<SkillTriggerLog> mRecentLogs = new List<SkillTriggerLog>();
    private const int MaxRecentLogs = 64;

    public IReadOnlyList<SkillTriggerLog> RecentLogs => mRecentLogs;

    protected override void OnInit()
    {
        this.RegisterEvent<CardMovedEvent>(OnCardMoved);
    }

    public void ResetTriggerDepth()
    {
        mQueue.BeginRootCommand();
    }

    public void BeginRootCommand()
    {
        mQueue.BeginRootCommand();
    }

    public void Trigger(SkillTrigger trigger, TriggerContext context, ICanSendCommand commandSender)
    {
        if (commandSender == null)
        {
            return;
        }

        var bindings = SkillEffectRegistry.AllBindings;
        for (var i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];
            if (binding.Trigger != trigger)
            {
                continue;
            }

            foreach (var owner in EnumerateOwners(binding, context))
            {
                if (!EvaluateCondition(binding.ConditionKey, owner, context))
                {
                    continue;
                }

                var triggerKey = BuildTriggerKey(owner.Uid, binding.OwnerDefinitionId, trigger);
                if (!mQueue.TryEnter(triggerKey, out var depth))
                {
                    Debug.LogWarning($"[SkillSystem] Trigger blocked by recursion guard: {triggerKey}");
                    continue;
                }

                try
                {
                    ExecuteBinding(binding, owner, context, commandSender, depth);
                }
                finally
                {
                    mQueue.Exit();
                }
            }
        }
    }

    public void CollectParallelDamage(TriggerContext context, List<DamageContext> output)
    {
        output.Clear();
        if (context == null)
        {
            return;
        }

        context.ParallelDamage.Clear();
        SendSkillCommand(new TriggerSkillSystemCommand(SkillTrigger.OnModifyDamage, context));

        for (var i = 0; i < context.ParallelDamage.Count; i++)
        {
            output.Add(context.ParallelDamage[i]);
        }
    }

    private void OnCardMoved(CardMovedEvent moved)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(moved.Uid, out var runtime) || runtime.CardType != CardType.Help)
        {
            return;
        }

        var context = new TriggerContext
        {
            OwnerUid = moved.Uid,
            OwnerDefinitionId = runtime.DefinitionId,
            CardSlot = moved.NewSlot,
            PreviousSlot = moved.PreviousSlot
        };
        SendSkillCommand(new TriggerSkillSystemCommand(SkillTrigger.OnCardMoved, context));
    }

    private void SendSkillCommand(ICommand command)
    {
        ((IBelongToArchitecture)this).GetArchitecture().SendCommand(command);
    }

    private IEnumerable<SkillOwnerInstance> EnumerateOwners(SkillEffectBinding binding, TriggerContext context)
    {
        switch (binding.OwnerKind)
        {
            case SkillOwnerKind.Relic:
                return EnumerateRelicOwners(binding.OwnerDefinitionId);
            case SkillOwnerKind.PlayerSkill:
                return EnumeratePlayerSkillOwners(binding.OwnerDefinitionId);
            case SkillOwnerKind.MonsterSkill:
                return EnumerateMonsterSkillOwners(binding, context);
            case SkillOwnerKind.HelpCardPassive:
                return EnumerateHelpCardOwners(binding.OwnerDefinitionId, context);
            default:
                return Array.Empty<SkillOwnerInstance>();
        }
    }

    private IEnumerable<SkillOwnerInstance> EnumerateRelicOwners(string relicId)
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var relicSystem = this.GetSystem<IRelicSystem>();
        if (!relicSystem.HasRelic(relicId))
        {
            yield break;
        }

        for (var i = 0; i < playerModel.Relics.Count; i++)
        {
            var relic = playerModel.Relics[i];
            if (relic.IsConsumed || relic.RelicId != relicId)
            {
                continue;
            }

            yield return new SkillOwnerInstance(relicId, null);
            yield break;
        }
    }

    private IEnumerable<SkillOwnerInstance> EnumeratePlayerSkillOwners(string skillId)
    {
        var playerModel = this.GetModel<IPlayerModel>();
        for (var i = 0; i < playerModel.SkillIds.Count; i++)
        {
            if (playerModel.SkillIds[i] != skillId)
            {
                continue;
            }

            yield return new SkillOwnerInstance(skillId, null);
            yield break;
        }
    }

    private IEnumerable<SkillOwnerInstance> EnumerateMonsterSkillOwners(SkillEffectBinding binding, TriggerContext context)
    {
        if (context.Combat == null || context.Combat.MonsterUid.Value == 0)
        {
            yield break;
        }

        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(context.Combat.MonsterUid, out var runtime))
        {
            yield break;
        }

        if (!runtime.HasSkill(binding.OwnerDefinitionId))
        {
            yield break;
        }

        yield return new SkillOwnerInstance(binding.OwnerDefinitionId, context.Combat.MonsterUid);
    }

    private IEnumerable<SkillOwnerInstance> EnumerateHelpCardOwners(string helpCardId, TriggerContext context)
    {
        if (context.OwnerUid.HasValue)
        {
            var collectionModel = this.GetModel<ICollectionModel>();
            if (collectionModel.TryGetCard(context.OwnerUid.Value, out var runtime) &&
                runtime.DefinitionId == helpCardId)
            {
                yield return new SkillOwnerInstance(helpCardId, context.OwnerUid);
            }

            yield break;
        }

        var deckModel = this.GetModel<IDeckModel>();
        var collection = this.GetModel<ICollectionModel>();
        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (!deckModel.HelpCardStates.TryGetValue(uid.Value, out var state) || state.IsPermanentlyRemoved)
            {
                continue;
            }

            if (!collection.TryGetCard(uid, out var runtime) || runtime.DefinitionId != helpCardId)
            {
                continue;
            }

            yield return new SkillOwnerInstance(helpCardId, uid);
        }
    }

    private bool EvaluateCondition(string conditionKey, SkillOwnerInstance owner, TriggerContext context)
    {
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var boardSystem = this.GetSystem<IBoardSystem>();
        var playerModel = this.GetModel<IPlayerModel>();

        switch (conditionKey)
        {
            case "always":
                return true;
            case "first_hit_player_attacks_monster":
                return context.CombatStep == CombatStep.FirstHit &&
                       context.PrimaryAttacker.HasValue &&
                       context.PrimaryDefender.HasValue &&
                       context.PrimaryAttacker.Value.Equals(context.Combat.PlayerUid) &&
                       context.PrimaryDefender.Value.Equals(context.Combat.MonsterUid);
            case "player_defender_monster_attacks":
                return context.PrimaryAttacker.HasValue &&
                       context.PrimaryDefender.HasValue &&
                       context.PrimaryDefender.Value.Equals(context.Combat.PlayerUid) &&
                       context.PrimaryAttacker.Value.Equals(context.Combat.MonsterUid);
            case "moved_to_adjacent_player":
                if (!context.CardSlot.HasValue || !owner.Uid.HasValue)
                {
                    return false;
                }

                if (!boardSystem.IsOrthogonalAdjacentToPlayer(context.CardSlot.Value))
                {
                    return false;
                }

                if (context.PreviousSlot.HasValue &&
                    BoardSlotUtility.IsOrthogonalAdjacent(boardModel.PlayerSlot, context.PreviousSlot.Value))
                {
                    return false;
                }

                return true;
            case "moved_to_slot_3_killable":
                if (!context.CardSlot.HasValue || context.CardSlot.Value.Value != 3)
                {
                    return false;
                }

                if (context.PreviousSlot.HasValue && context.PreviousSlot.Value.Value == 3)
                {
                    return false;
                }

                return TryGetKillableMonsterAtSlot(new BoardSlotNo(6), out _);
            case "in_item_slot":
                if (!owner.Uid.HasValue)
                {
                    return false;
                }

                return collectionModel.TryGetCard(owner.Uid.Value, out var runtime) && runtime.ItemSlotIndex.HasValue;
            default:
                return false;
        }
    }

    private void ExecuteBinding(
        SkillEffectBinding binding,
        SkillOwnerInstance owner,
        TriggerContext context,
        ICanSendCommand commandSender,
        int depth)
    {
        if (binding.EffectGraphId == "eg_relic_thorn_armor")
        {
            AppendThornArmorDamage(context);
        }
        else if (binding.EffectGraphId == "eg_skill_thorn_skin_reflect")
        {
            AppendThornSkinDamage(context);
        }
        else if (PassiveGraphs.TryGetGraph(binding.EffectGraphId, out var graph))
        {
            var effectContext = BuildEffectContext(binding, owner);
            if (binding.EffectGraphId == "eg_passive_boulder_kill" &&
                TryGetKillableMonsterAtSlot(new BoardSlotNo(6), out var monsterUid))
            {
                effectContext.Targets.Add(monsterUid);
            }

            var effectSystem = this.GetSystem<IEffectSystem>();
            for (var i = 0; i < graph.Atoms.Count; i++)
            {
                effectSystem.ResolveAtom(graph.Atoms[i], effectContext, commandSender);
            }
        }
        else
        {
            Debug.LogWarning($"[SkillSystem] Missing passive effect graph: {binding.EffectGraphId}");
            return;
        }

        AppendLog(binding, owner, depth);
        this.SendEvent(new SkillTriggeredEvent(binding.OwnerDefinitionId, owner.Uid, binding.Trigger, binding.EffectGraphId));
    }

    private static EffectContext BuildEffectContext(SkillEffectBinding binding, SkillOwnerInstance owner)
    {
        return new EffectContext
        {
            Source = binding.OwnerKind == SkillOwnerKind.Relic
                ? EffectSource.Relic
                : binding.OwnerKind == SkillOwnerKind.HelpCardPassive
                    ? EffectSource.HelpCard
                    : EffectSource.Skill,
            Caster = owner.Uid
        };
    }

    private void AppendThornArmorDamage(TriggerContext context)
    {
        context.ParallelDamage.Add(new DamageContext
        {
            Source = context.Combat.PlayerUid,
            Target = context.Combat.MonsterUid,
            CauseId = CombatConstants.CauseThornArmor,
            Type = DamageType.Relic,
            RawAttack = CombatConstants.ThornArmorDamage,
            DamageBeforeArmor = CombatConstants.ThornArmorDamage,
            IgnoreArmor = true,
            Preventable = false
        });
    }

    private void AppendThornSkinDamage(TriggerContext context)
    {
        var reflectAttack = Math.Max(0, context.Combat.MonsterStats.Attack);
        if (reflectAttack <= 0)
        {
            return;
        }

        var combatSystem = this.GetSystem<ICombatSystem>();
        var reflectAttackerStats = new EffectiveStats { Attack = reflectAttack };
        context.ParallelDamage.Add(combatSystem.BuildCombatDamage(
            context.Combat.PlayerUid,
            context.Combat.MonsterUid,
            reflectAttackerStats,
            context.Combat.MonsterStats,
            CombatConstants.CauseThornSkin));
    }

    private void AppendLog(SkillEffectBinding binding, SkillOwnerInstance owner, int depth)
    {
        mRecentLogs.Add(new SkillTriggerLog
        {
            OwnerDefinitionId = binding.OwnerDefinitionId,
            OwnerUid = owner.Uid,
            Trigger = binding.Trigger,
            EffectGraphId = binding.EffectGraphId,
            Depth = depth
        });

        if (mRecentLogs.Count > MaxRecentLogs)
        {
            mRecentLogs.RemoveAt(0);
        }
    }

    private static string BuildTriggerKey(CardUid? ownerUid, string ownerDefinitionId, SkillTrigger trigger)
    {
        return $"{ownerUid?.Value ?? 0}:{ownerDefinitionId}:{trigger}";
    }

    private bool TryGetKillableMonsterAtSlot(BoardSlotNo slot, out CardUid monsterUid)
    {
        monsterUid = default;
        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var uid = boardModel.GetCardAt(slot);
        if (!uid.HasValue)
        {
            return false;
        }

        if (!collectionModel.TryGetCard(uid.Value, out var runtime) || runtime.CardType != CardType.Monster)
        {
            return false;
        }

        if (runtime.MonsterLevel == MonsterLevel.Elite || runtime.MonsterLevel == MonsterLevel.Boss)
        {
            return false;
        }

        monsterUid = uid.Value;
        return true;
    }

    private readonly struct SkillOwnerInstance
    {
        public SkillOwnerInstance(string definitionId, CardUid? uid)
        {
            DefinitionId = definitionId;
            Uid = uid;
        }

        public string DefinitionId { get; }
        public CardUid? Uid { get; }
    }
}
