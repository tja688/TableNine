using System;
using System.Collections.Generic;
using NUnit.Framework;
using QFramework;

[Category(TableNineTestCategories.NewRuleTests)]
public sealed class TableNineR3CombatPipelineEditModeTests
{
    [SetUp]
    public void SetUp()
    {
        TableNine.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        TableNine.ResetForTests();
    }

    [Test]
    public void Player_Acts_First_When_Both_Have_FirstStrike()
    {
        TableNine.InitArchitecture();
        var combatSystem = TableNine.Interface.GetSystem<ICombatSystem>();
        var playerActsFirst = combatSystem.PlayerActsFirst(
            new EffectiveStats { HasFirstStrike = true },
            new EffectiveStats { HasFirstStrike = true });

        Assert.That(playerActsFirst, Is.True);
    }

    [Test]
    public void Monster_Acts_First_When_Only_Monster_Has_FirstStrike()
    {
        TableNine.InitArchitecture();
        var combatSystem = TableNine.Interface.GetSystem<ICombatSystem>();
        var playerActsFirst = combatSystem.PlayerActsFirst(
            new EffectiveStats { HasFirstStrike = false },
            new EffectiveStats { HasFirstStrike = true });

        Assert.That(playerActsFirst, Is.False);
    }

    [Test]
    public void Dead_Monster_Does_Not_CounterAttack()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 1;
        monster.CurrentArmor = 0;
        monster.BaseDefense = 0;

        var playerHpBefore = collectionModel.GetCard(playerModel.PlayerCardUid).CurrentHp;
        var damageEvents = new List<DamageAppliedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<DamageAppliedEvent>(damageEvents.Add);

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        unReg.UnRegister();

        Assert.That(collectionModel.GetCard(playerModel.PlayerCardUid).CurrentHp, Is.EqualTo(playerHpBefore));
        Assert.That(collectionModel.TryGetCard(monsterUid, out _), Is.False);
        Assert.That(damageEvents.Exists(e => e.Context != null && e.Context.CauseId == CombatConstants.CauseCombatMonsterCounter), Is.False);
    }

    [Test]
    public void Surviving_Monster_Always_Counter_Attacks()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 999;
        monster.CurrentArmor = 0;
        monster.BaseAttack = 2;

        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 0;

        var damageEvents = new List<DamageAppliedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<DamageAppliedEvent>(damageEvents.Add);

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        unReg.UnRegister();

        Assert.That(collectionModel.TryGetCard(monsterUid, out _), Is.True);
        Assert.That(damageEvents.Exists(e => e.Context != null && e.Context.CauseId == CombatConstants.CauseCombatMonsterCounter), Is.True);
        Assert.That(player.CurrentHp, Is.LessThan(player.MaxHp));
    }

    [Test]
    public void Thorn_Armor_Damage_Is_In_Same_Group_As_Player_First_Hit()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicThornArmorId);

        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 3;
        monster.CurrentArmor = 0;
        monster.BaseDefense = 0;

        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.BaseAttack = 1;
        player.CurrentArmor = 0;

        CombatResolvedEvent? resolved = null;
        var unReg = TableNine.Interface.RegisterEvent<CombatResolvedEvent>(e => resolved = e);

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        unReg.UnRegister();

        Assert.That(resolved.HasValue, Is.True);
        var firstGroup = resolved.Value.Context.FirstHitGroup;
        Assert.That(firstGroup.Exists(c => c.CauseId == CombatConstants.CauseCombatPlayerFirst), Is.True);
        Assert.That(firstGroup.Exists(c => c.CauseId == CombatConstants.CauseThornArmor), Is.True);
        Assert.That(collectionModel.TryGetCard(monsterUid, out _), Is.False, "1 + 2 parallel damage should kill 3 HP monster");
    }

    [Test]
    public void Thorn_Skin_Reflect_Is_In_Same_Group_As_Monster_Counter()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        playerModel.AddSkill(DefaultGameConfigFactory.SkillThornSkinId);

        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 999;
        monster.CurrentArmor = 0;
        monster.BaseAttack = 4;
        monster.BaseDefense = 0;

        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 0;
        player.BaseAttack = 1;

        CombatResolvedEvent? resolved = null;
        var unReg = TableNine.Interface.RegisterEvent<CombatResolvedEvent>(e => resolved = e);

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        unReg.UnRegister();

        Assert.That(resolved.HasValue, Is.True);
        var counterGroup = resolved.Value.Context.CounterHitGroup;
        Assert.That(counterGroup.Exists(c => c.CauseId == CombatConstants.CauseCombatMonsterCounter), Is.True);
        Assert.That(counterGroup.Exists(c => c.CauseId == CombatConstants.CauseThornSkin), Is.True);
        Assert.That(monster.CurrentHp, Is.LessThan(999));
    }

    [Test]
    public void Phoenix_Feather_Prevents_GameOver_After_Lethal_Damage()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicPhoenixFeatherId);

        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 999;
        monster.CurrentArmor = 0;
        monster.BaseAttack = 999;
        monster.BaseDefense = 0;

        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentHp = 5;
        player.CurrentArmor = 0;

        var maxHp = TableNine.Interface.GetSystem<IStatSystem>().GetEffectivePlayerStats().MaxHp;

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.Not.EqualTo(FlowPhase.GameOver));
        Assert.That(player.CurrentHp, Is.EqualTo(Math.Max(1, maxHp / 2)));
        Assert.That(relicSystem.HasRelic(DefaultGameConfigFactory.RelicPhoenixFeatherId), Is.False);
    }

    [Test]
    public void Player_Death_Without_Phoenix_Triggers_GameOver()
    {
        StartRun(42);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 999;
        monster.CurrentArmor = 0;
        monster.BaseAttack = 999;
        monster.BaseDefense = 0;

        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentHp = 5;
        player.CurrentArmor = 0;

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.EqualTo(FlowPhase.GameOver));
        Assert.That(player.CurrentHp, Is.EqualTo(0));
    }

    [Test]
    public void Combat_Ends_With_Single_CommitPlayerAction()
    {
        StartRun(42);
        MoveAdjacentMonsterWithoutFirstStrikeToSlot2();

        var commitCount = 0;
        var unReg = TableNine.Interface.RegisterEvent<PlayerActionCommittedEvent>(_ => commitCount++);

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        unReg.UnRegister();
        Assert.That(commitCount, Is.EqualTo(1));
    }

    [Test]
    public void CombatBeforeResolvedEvent_Fires_Before_Stats_Are_Frozen()
    {
        StartRun(42);
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var attackAtEvent = -1;
        var unReg = TableNine.Interface.RegisterEvent<CombatBeforeResolvedEvent>(
            e => attackAtEvent = e.Context.PlayerStats.Attack);

        CombatResolvedEvent? resolved = null;
        var unRegResolved = TableNine.Interface.RegisterEvent<CombatResolvedEvent>(e => resolved = e);

        TableNine.Interface.SendCommand(new StartCombatCommand(monsterUid));

        unReg.UnRegister();
        unRegResolved.UnRegister();

        Assert.That(attackAtEvent, Is.EqualTo(0), "OnBeforeCombat must fire before effective stats are calculated");
        Assert.That(resolved.HasValue, Is.True);
        Assert.That(resolved.Value.Context.PlayerStats.Attack, Is.GreaterThan(0));
    }

    [Test]
    public void Blessing_Is_Not_Consumed_By_Zero_Effective_Damage_Hit()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        deckModel.PendingHelpCardAction.Kind = PendingHelpCardActionKind.BlessingShield;

        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var monsterUid = MoveAdjacentMonsterWithFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 999;
        monster.CurrentArmor = 0;
        monster.BaseAttack = 0;
        monster.BaseDefense = 0;

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.BaseAttack = 1;
        player.CurrentArmor = 0;

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        Assert.That(deckModel.PendingHelpCardAction.IsActive, Is.True,
            "庇佑应在 0 有效伤害命中后保留，等待下一次真正受伤");
    }

    [Test]
    public void Thorn_Armor_Does_Not_Trigger_On_Player_Counter_Hit()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicThornArmorId);

        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var monsterUid = MoveAdjacentMonsterWithFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 999;
        monster.CurrentArmor = 0;
        monster.BaseAttack = 1;
        monster.BaseDefense = 0;

        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.BaseAttack = 1;
        player.CurrentArmor = 0;

        CombatResolvedEvent? resolved = null;
        var unReg = TableNine.Interface.RegisterEvent<CombatResolvedEvent>(e => resolved = e);

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        unReg.UnRegister();

        Assert.That(resolved.HasValue, Is.True);
        var firstGroup = resolved.Value.Context.FirstHitGroup;
        var counterGroup = resolved.Value.Context.CounterHitGroup;
        Assert.That(firstGroup.Exists(c => c.CauseId == CombatConstants.CauseThornArmor), Is.False);
        Assert.That(counterGroup.Exists(c => c.CauseId == CombatConstants.CauseThornArmor), Is.False,
            "荆棘甲仅与玩家先手 FirstHit 并行，不应出现在怪物先攻后的玩家反击组");
        Assert.That(counterGroup.Exists(c => c.CauseId == CombatConstants.CauseCombatPlayerCounter), Is.True);
    }

    [Test]
    public void Combat_Resolve_Populates_Damage_Groups_And_Events()
    {
        StartRun(42);
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();

        CombatStartedEvent? started = null;
        CombatResolvedEvent? resolved = null;
        var unRegStart = TableNine.Interface.RegisterEvent<CombatStartedEvent>(e => started = e);
        var unRegResolved = TableNine.Interface.RegisterEvent<CombatResolvedEvent>(e => resolved = e);

        TableNine.Interface.SendCommand(new StartCombatCommand(monsterUid));

        unRegStart.UnRegister();
        unRegResolved.UnRegister();

        Assert.That(started.HasValue, Is.True);
        Assert.That(resolved.HasValue, Is.True);
        Assert.That(resolved.Value.Context.FirstHitGroup.Count, Is.GreaterThan(0));
    }

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static CardUid MoveAdjacentMonsterWithoutFirstStrikeToSlot2()
    {
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var currentAtTwo = boardModel.GetCardAt(new BoardSlotNo(2));
        if (currentAtTwo.HasValue)
        {
            boardSystem.RemoveCardAt(new BoardSlotNo(2));
        }

        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue)
            {
                continue;
            }

            var runtime = collectionModel.GetCard(uid.Value);
            if (runtime.CardType == CardType.Monster && !runtime.HasSkill(DefaultGameConfigFactory.SkillFirstStrikeId))
            {
                boardSystem.RemoveCardAt(slot);
                boardSystem.PlaceCard(uid.Value, new BoardSlotNo(2), CardPlacementSource.Refill);
                return uid.Value;
            }
        }

        Assert.Fail("Could not find a non-first-strike monster adjacent setup target.");
        return default;
    }

    private static CardUid MoveAdjacentMonsterWithFirstStrikeToSlot2()
    {
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = TableNine.Interface.GetModel<ICollectionModel>().GetCard(monsterUid);
        if (!monster.HasSkill(DefaultGameConfigFactory.SkillFirstStrikeId))
        {
            monster.SkillIds.Add(DefaultGameConfigFactory.SkillFirstStrikeId);
        }

        return monsterUid;
    }
}
