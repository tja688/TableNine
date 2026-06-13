using System.Collections.Generic;
using NUnit.Framework;
using QFramework;

[Category(TableNineTestCategories.NewRuleTests)]
public sealed class TableNineR1DataMigrationEditModeTests
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
    public void NewCard_Instance_Has_Zero_CurrentArmor()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 99));

        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        var stats = TableNine.Interface.GetSystem<IStatSystem>().GetEffectivePlayerStats();

        Assert.That(player.CurrentArmor, Is.EqualTo(stats.Defense),
            "R2：节点开始后护甲按有效防御填充，而非保持 0");
    }

    [Test]
    public void RestoreAfterNode_Migrates_From_IsPermanentRemoveOnUse()
    {
        var permanent = CardDefinitionMigration.CreateLegacyHelpCardDefinition("help_permanent", isPermanentRemoveOnUse: true);
        var legacyRestore = CardDefinitionMigration.CreateLegacyHelpCardDefinition("help_legacy_restore", isPermanentRemoveOnUse: false);
        var newPermanent = new CardDefinition
        {
            CardId = "help_new_permanent",
            CardType = CardType.Help,
            RestoreAfterNode = false,
            RestoreAfterNodeAuthoritative = true
        };
        var restore = new CardDefinition
        {
            CardId = "help_restore",
            CardType = CardType.Help,
            RestoreAfterNode = true,
            RestoreAfterNodeAuthoritative = true
        };

        CardDefinitionMigration.MigrateHelpCardSemantics(permanent);
        CardDefinitionMigration.MigrateHelpCardSemantics(legacyRestore);
        CardDefinitionMigration.MigrateHelpCardSemantics(newPermanent);
        CardDefinitionMigration.MigrateHelpCardSemantics(restore);

        Assert.That(permanent.RestoreAfterNode, Is.False);
        Assert.That(legacyRestore.RestoreAfterNode, Is.True);
        Assert.That(newPermanent.RestoreAfterNode, Is.False);
        Assert.That(restore.RestoreAfterNode, Is.True);
    }

    [Test]
    public void Default_Config_Help_Cards_Have_RestoreAfterNode_False()
    {
        var config = DefaultGameConfigFactory.Create();

        for (var i = 0; i < config.Cards.Count; i++)
        {
            var card = config.Cards[i];
            if (card.CardType != CardType.Help)
            {
                continue;
            }

            Assert.That(card.RestoreAfterNode, Is.False, $"Help card {card.CardId} should default to permanent remove");
        }
    }

    [Test]
    public void Default_Config_Has_No_Deprecated_Help_Card_Warnings()
    {
        var config = DefaultGameConfigFactory.Create();
        var warnings = ConfigValidator.CollectWarnings(config);

        Assert.That(warnings.Exists(w => w.Contains("legacy IsPermanentRemoveOnUse")), Is.False,
            "Default config should not rely on deprecated legacy IsPermanentRemoveOnUse");
    }

    [Test]
    public void ConfigValidator_Warns_On_Deprecated_IsPermanentRemoveOnUse()
    {
        var config = new GameConfigSet();
        config.Cards.Add(CardDefinitionMigration.CreateLegacyHelpCardDefinition("help_legacy", isPermanentRemoveOnUse: true));

        var warnings = ConfigValidator.CollectWarnings(config);
        Assert.That(warnings.Exists(w => w.Contains("legacy IsPermanentRemoveOnUse")), Is.True);
    }

    [Test]
    public void DamageContext_Can_Be_Constructed_And_Applied()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 7));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 4;
        var hpBefore = player.CurrentHp;

        var context = new DamageContext
        {
            Target = playerModel.PlayerCardUid,
            CauseId = "test_help",
            Type = DamageType.HelpCard,
            RawAttack = 6,
            DamageReduction = 1,
            DamageBeforeArmor = 5,
            ArmorAbsorbed = 4,
            HpDamage = 1
        };

        TableNine.Interface.SendCommand(new ApplyDamageCommand(context));

        Assert.That(player.CurrentArmor, Is.EqualTo(0));
        Assert.That(player.CurrentHp, Is.EqualTo(hpBefore - 1));
    }

    [Test]
    public void ChangeArmorCommand_Updates_Runtime_And_Emits_Events()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 11));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        ArmorChangedEvent? armorEvent = null;
        StatsDirtyEvent? dirtyEvent = null;
        var unRegisterArmor = TableNine.Interface.RegisterEvent<ArmorChangedEvent>(e => armorEvent = e);
        var unRegisterDirty = TableNine.Interface.RegisterEvent<StatsDirtyEvent>(e => dirtyEvent = e);

        TableNine.Interface.SendCommand(new ChangeArmorCommand(playerModel.PlayerCardUid, 3, "test"));

        unRegisterArmor.UnRegister();
        unRegisterDirty.UnRegister();

        var player = TableNine.Interface.GetModel<ICollectionModel>().GetCard(playerModel.PlayerCardUid);
        var stats = TableNine.Interface.GetSystem<IStatSystem>().GetEffectivePlayerStats();
        Assert.That(player.CurrentArmor, Is.EqualTo(stats.Defense + 3));
        Assert.That(armorEvent.HasValue, Is.True);
        Assert.That(armorEvent.Value.NewArmor, Is.EqualTo(stats.Defense + 3));
        Assert.That(dirtyEvent.HasValue, Is.True);
    }

    [Test]
    public void EffectiveStats_Includes_CurrentArmor_And_DamageReduction()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 13));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        collectionModel.GetCard(playerModel.PlayerCardUid).CurrentArmor = 2;

        var stats = TableNine.Interface.GetSystem<IStatSystem>().GetEffectivePlayerStats();
        Assert.That(stats.CurrentArmor, Is.EqualTo(2));
        Assert.That(stats.DamageReduction, Is.EqualTo(0));
        Assert.That(stats.Attack, Is.GreaterThan(0));
    }

    [Test]
    public void Legacy_Save_Data_Without_Armor_Defaults_To_Zero()
    {
        var save = new CardRuntimeSaveData
        {
            Uid = 1,
            DefinitionId = "test",
            DisplayName = "Test",
            CardType = CardType.Monster,
            CurrentHp = 5,
            MaxHp = 5
        };

        TableNine.InitArchitecture();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        collectionModel.RestoreCard(new CardRuntime
        {
            Uid = new CardUid(save.Uid),
            DefinitionId = save.DefinitionId,
            DisplayName = save.DisplayName,
            CardType = save.CardType,
            CurrentHp = save.CurrentHp,
            MaxHp = save.MaxHp,
            CurrentArmor = save.CurrentArmor
        });

        Assert.That(collectionModel.GetCard(new CardUid(1)).CurrentArmor, Is.EqualTo(0));
    }

    [Test]
    public void FlowModel_Emits_Phase_And_Lock_Events()
    {
        TableNine.InitArchitecture();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        FlowPhaseChangedEvent? phaseEvent = null;
        InputLockChangedEvent? lockEvent = null;
        var unRegisterPhase = TableNine.Interface.RegisterEvent<FlowPhaseChangedEvent>(e => phaseEvent = e);
        var unRegisterLock = TableNine.Interface.RegisterEvent<InputLockChangedEvent>(e => lockEvent = e);

        flowModel.SetPhase(FlowPhase.RoomResolving);
        flowModel.AddLock(InputLockReason.CombatResolving);

        unRegisterPhase.UnRegister();
        unRegisterLock.UnRegister();

        Assert.That(phaseEvent.HasValue, Is.True);
        Assert.That(phaseEvent.Value.NewPhase, Is.EqualTo(FlowPhase.RoomResolving));
        Assert.That(lockEvent.HasValue, Is.True);
        Assert.That(lockEvent.Value.IsLocked, Is.True);
    }
}
