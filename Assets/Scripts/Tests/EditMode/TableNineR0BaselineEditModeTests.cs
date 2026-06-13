using System.Collections.Generic;
using NUnit.Framework;
using QFramework;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineR0BaselineEditModeTests
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
    public void Baseline_CombatSystem_Uses_Attack_Minus_DamageReduction()
    {
        TableNine.InitArchitecture();
        var combatSystem = TableNine.Interface.GetSystem<ICombatSystem>();

        var damage = combatSystem.CalculateDamage(
            new EffectiveStats { Attack = 5, Defense = 0 },
            new EffectiveStats { Attack = 0, Defense = 3, DamageReduction = 3 });

        Assert.That(damage, Is.EqualTo(2));
    }

    [Test]
    public void Baseline_ApplyDamage_Absorbs_Armor_Before_Hp()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 42));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 5;
        var hpBefore = player.CurrentHp;

        TableNine.Interface.SendCommand(new ApplyDamageCommand(playerModel.PlayerCardUid, 3));

        Assert.That(player.CurrentArmor, Is.EqualTo(2));
        Assert.That(player.CurrentHp, Is.EqualTo(hpBefore));
    }

    [Test]
    public void Baseline_EffectSystem_And_SkillSystem_Are_Registered()
    {
        TableNine.InitArchitecture();

        var effectSystem = TableNine.Interface.GetSystem<IEffectSystem>();
        Assert.That(effectSystem, Is.Not.Null);
        Assert.That(effectSystem.GetType(), Is.EqualTo(typeof(EffectSystem)));
        Assert.That(EffectGraphRegistry.ContainsGraph("eg_help_potion"), Is.True);

        var skillSystem = TableNine.Interface.GetSystem<ISkillSystem>();
        Assert.That(skillSystem, Is.Not.Null);
        Assert.That(skillSystem.GetType(), Is.EqualTo(typeof(SkillSystem)));
    }

    [Test]
    public void Baseline_SaveUtility_Is_MemorySaveUtility()
    {
        TableNine.InitArchitecture();
        Assert.That(TableNine.Interface.GetUtility<ISaveUtility>(), Is.TypeOf<MemorySaveUtility>());
    }

    [Test]
    public void Baseline_ConfigValidator_Passes_For_Layer1_Core_Config()
    {
        var config = TableNineTestConfig.LoadProductionCoreConfig();

        var errors = ConfigValidator.Validate(config);
        var layer1Errors = new List<string>();
        for (var i = 0; i < errors.Count; i++)
        {
            if (!errors[i].Contains("rule 2-") && !errors[i].Contains("rule 3-"))
            {
                layer1Errors.Add(errors[i]);
            }
        }

        Assert.That(layer1Errors, Is.Empty, string.Join("\n", layer1Errors));
    }
}
