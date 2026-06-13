using System.Collections.Generic;
using NUnit.Framework;
using QFramework;
using UnityEngine;

[Category(TableNineTestCategories.NewRuleTests)]
public sealed class TableNineR8PresentationEditModeTests
{
    [SetUp]
    public void SetUp()
    {
        TableNine.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        TableNineUIRuntimeTestRecorder.Reset();
        TableNine.ResetForTests();
    }

    [Test]
    public void CardViewData_Includes_Armor_DamageReduction_And_Status_Ids()
    {
        StartRun(42);
        var controller = new TestController();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 7;
        playerModel.AddSkill(GameConfigIds.SkillHardSkinId);

        var playerData = CardViewDataFactory.Create(controller, playerModel.PlayerCardUid);

        Assert.That(playerData.CurrentArmor, Is.EqualTo(7));
        Assert.That(playerData.SpriteId, Is.EqualTo(player.DefinitionId));
        Assert.That(playerData.StatusIconIds, Does.Contain(GameConfigIds.SkillHardSkinId));
        Assert.That(CardViewDataFactory.FormatWorldCard(playerData, false), Does.Contain("ARM 7"));

        var monsterUid = FindMonsterOnBoard();
        collectionModel.GetCard(monsterUid).SkillIds.Add(GameConfigIds.SkillHardSkinId);

        var monsterData = CardViewDataFactory.Create(controller, monsterUid);

        Assert.That(monsterData.DamageReduction, Is.EqualTo(1));
        Assert.That(monsterData.StatusIconIds, Does.Contain(GameConfigIds.SkillHardSkinId));
        Assert.That(CardViewDataFactory.FormatWorldCard(monsterData, false), Does.Contain("DR 1"));
    }

    [Test]
    public void Presentation_Fact_Events_Fire_For_Board_Heal_And_Effect()
    {
        StartRun(43);
        var rotated = new List<BoardRotatedEvent>();
        var removed = new List<CardRemovedEvent>();
        var healed = new List<HealAppliedEvent>();
        var effects = new List<EffectResolvedEvent>();
        var rotateRegister = TableNine.Interface.RegisterEvent<BoardRotatedEvent>(rotated.Add);
        var removeRegister = TableNine.Interface.RegisterEvent<CardRemovedEvent>(removed.Add);
        var healRegister = TableNine.Interface.RegisterEvent<HealAppliedEvent>(healed.Add);
        var effectRegister = TableNine.Interface.RegisterEvent<EffectResolvedEvent>(effects.Add);

        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        boardSystem.RotateClockwise(BoardMoveReason.Debug);
        var monsterUid = FindMonsterOnBoard();
        var monsterSlot = TableNine.Interface.GetModel<ICollectionModel>().GetCard(monsterUid).BoardSlot.Value;
        boardSystem.RemoveCardAt(monsterSlot, RemoveReason.Debug);

        var playerUid = TableNine.Interface.GetModel<IPlayerModel>().PlayerCardUid;
        var player = TableNine.Interface.GetModel<ICollectionModel>().GetCard(playerUid);
        player.CurrentHp -= 1;
        TableNine.Interface.SendCommand(new ApplyEffectHealCommand(playerUid, 1, false));
        TableNine.Interface.SendCommand(new ResolveEffectGraphCommand(
            "eg_help_gold_card",
            new EffectContext { Source = EffectSource.HelpCard }));

        Assert.That(rotated.Count, Is.EqualTo(1));
        Assert.That(rotated[0].Reason, Is.EqualTo(BoardMoveReason.Debug));
        Assert.That(removed.Count, Is.EqualTo(1));
        Assert.That(removed[0].Reason, Is.EqualTo(RemoveReason.Debug));
        Assert.That(healed.Count, Is.EqualTo(1));
        Assert.That(healed[0].Amount, Is.EqualTo(1));
        Assert.That(effects.Count, Is.EqualTo(1));
        Assert.That(effects[0].EffectGraphId, Is.EqualTo("eg_help_gold_card"));

        rotateRegister.UnRegister();
        removeRegister.UnRegister();
        healRegister.UnRegister();
        effectRegister.UnRegister();
    }

    [Test]
    public void UIRuntime_Emits_Overlay_Events_With_Blocking_Metadata()
    {
        StartRun(44);
        TableNineUIRuntimeTestRecorder.SkipActualPanelOpen = true;
        var registry = ScriptableObject.CreateInstance<TableNineUIPanelRegistry>();
        var router = new TableNineUIRouter(registry);
        var opened = new List<OverlayOpenedEvent>();
        var closed = new List<OverlayClosedEvent>();
        var openRegister = TableNine.Interface.RegisterEvent<OverlayOpenedEvent>(opened.Add);
        var closeRegister = TableNine.Interface.RegisterEvent<OverlayClosedEvent>(closed.Add);
        router.Start();

        RemoveAllMonstersAndBattlePile();
        TableNine.Interface.SendCommand(new CheckClearConditionCommand());
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        Assert.That(opened.Count, Is.EqualTo(2));
        Assert.That(opened[0].UiKey, Is.EqualTo(TableNineUIKeys.HelpReward));
        Assert.That(opened[0].BlocksGameplayInput, Is.True);
        Assert.That(opened[1].UiKey, Is.EqualTo(TableNineUIKeys.RoomChoice));
        Assert.That(opened[1].BlocksGameplayInput, Is.False);
        Assert.That(closed.Exists(e => e.UiKey == TableNineUIKeys.HelpReward), Is.True);

        router.Dispose();
        openRegister.UnRegister();
        closeRegister.UnRegister();
        Object.DestroyImmediate(registry);
    }

    [Test]
    public void Resource_And_Audio_Utilities_Degrade_When_Assets_Are_Missing()
    {
        TableNine.InitArchitecture();
        var resourceUtility = TableNine.Interface.GetUtility<IResourceUtility>();
        var audioUtility = TableNine.Interface.GetUtility<IAudioUtility>();

        Assert.That(resourceUtility.LoadCardSprite("missing_card_sprite"), Is.Null);
        Assert.That(resourceUtility.LoadAudioClip("missing_audio_clip"), Is.Null);

        audioUtility.Play(TableNineAudioIds.Click);
        Assert.That(audioUtility.LastAudioId, Is.EqualTo(TableNineAudioIds.Click));

        audioUtility.Muted = true;
        audioUtility.Play(TableNineAudioIds.Hit);
        Assert.That(audioUtility.LastAudioId, Is.EqualTo(TableNineAudioIds.Click));
    }

    [Test]
    public void Combat_Sequence_Waits_For_Finish_Commands_Before_Rotating_And_Refilling()
    {
        StartRun(45);
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var manualSequence = new ManualSequenceUtility();
        var requested = new List<PresentationSequenceRequestedEvent>();
        var completed = new List<PresentationSequenceCompletedEvent>();
        TableNine.Interface.RegisterUtility<ISequenceUtility>(manualSequence);
        var requestRegister = TableNine.Interface.RegisterEvent<PresentationSequenceRequestedEvent>(requested.Add);
        var completeRegister = TableNine.Interface.RegisterEvent<PresentationSequenceCompletedEvent>(completed.Add);

        var monsterUid = FindOrthogonallyAdjacentMonster();
        var monster = TableNine.Interface.GetModel<ICollectionModel>().GetCard(monsterUid);
        monster.CurrentHp = 1;
        monster.CurrentArmor = 0;

        TableNine.Interface.SendCommand(new StartCombatCommand(monsterUid));

        Assert.That(flowModel.HasLock(InputLockReason.CombatResolving), Is.True);
        Assert.That(flowModel.HasLock(InputLockReason.SequenceRunning), Is.True);
        Assert.That(requested.Count, Is.EqualTo(1));
        Assert.That(requested[0].SequenceType, Is.EqualTo(PresentationSequenceType.CombatResolution));

        manualSequence.CompleteNext();

        Assert.That(flowModel.HasLock(InputLockReason.CombatResolving), Is.False);
        Assert.That(flowModel.HasLock(InputLockReason.BoardMoving), Is.True);
        Assert.That(flowModel.HasLock(InputLockReason.SequenceRunning), Is.True);
        Assert.That(requested.Count, Is.EqualTo(2));
        Assert.That(requested[1].SequenceType, Is.EqualTo(PresentationSequenceType.BoardRotation));

        manualSequence.CompleteNext();

        Assert.That(flowModel.HasLock(InputLockReason.BoardMoving), Is.False);
        Assert.That(flowModel.HasLock(InputLockReason.BoardRefillRunning), Is.True);
        Assert.That(flowModel.HasLock(InputLockReason.SequenceRunning), Is.True);
        Assert.That(requested.Count, Is.EqualTo(3));
        Assert.That(requested[2].SequenceType, Is.EqualTo(PresentationSequenceType.BoardRefill));

        manualSequence.CompleteNext();

        Assert.That(flowModel.HasLock(InputLockReason.SequenceRunning), Is.False);
        Assert.That(flowModel.HasLock(InputLockReason.BoardRefillRunning), Is.False);
        Assert.That(completed.Count, Is.EqualTo(3));
        Assert.That(completed[2].SequenceType, Is.EqualTo(PresentationSequenceType.BoardRefill));

        requestRegister.UnRegister();
        completeRegister.UnRegister();
    }

    [Test]
    public void NarrativeSystem_Queues_Dialogue_And_Releases_DialogueLock_On_Finish()
    {
        TableNine.InitArchitecture();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var manualTextAnimator = new ManualTextAnimatorUtility();
        var requested = new List<DialogueRequestedEvent>();
        var completed = new List<DialogueCompletedEvent>();
        TableNine.Interface.RegisterUtility<ITextAnimatorUtility>(manualTextAnimator);
        var requestRegister = TableNine.Interface.RegisterEvent<DialogueRequestedEvent>(requested.Add);
        var completeRegister = TableNine.Interface.RegisterEvent<DialogueCompletedEvent>(completed.Add);

        TableNine.Interface.SendEvent(new GameplayMessageEvent("first"));
        TableNine.Interface.SendEvent(new GameplayMessageEvent("second"));

        Assert.That(requested.Count, Is.EqualTo(1));
        Assert.That(requested[0].Message, Is.EqualTo("first"));
        Assert.That(flowModel.HasLock(InputLockReason.DialogueRunning), Is.True);

        manualTextAnimator.CompleteCurrent();

        Assert.That(completed.Count, Is.EqualTo(1));
        Assert.That(requested.Count, Is.EqualTo(2));
        Assert.That(requested[1].Message, Is.EqualTo("second"));
        Assert.That(flowModel.HasLock(InputLockReason.DialogueRunning), Is.True);

        manualTextAnimator.CompleteCurrent();

        Assert.That(completed.Count, Is.EqualTo(2));
        Assert.That(flowModel.HasLock(InputLockReason.DialogueRunning), Is.False);

        requestRegister.UnRegister();
        completeRegister.UnRegister();
    }

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static CardUid FindMonsterOnBoard()
    {
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        for (var i = 1; i <= 9; i++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(i));
            if (uid.HasValue &&
                collectionModel.TryGetCard(uid.Value, out var runtime) &&
                runtime.CardType == CardType.Monster)
            {
                return uid.Value;
            }
        }

        Assert.Fail("No monster found on board.");
        return default;
    }

    private static CardUid FindOrthogonallyAdjacentMonster()
    {
        var candidateSlots = new[] { 2, 4, 6, 8 };
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        for (var i = 0; i < candidateSlots.Length; i++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(candidateSlots[i]));
            if (uid.HasValue &&
                collectionModel.TryGetCard(uid.Value, out var runtime) &&
                runtime.CardType == CardType.Monster)
            {
                return uid.Value;
            }
        }

        Assert.Fail("No orthogonally adjacent monster found on board.");
        return default;
    }

    private static void RemoveAllMonstersAndBattlePile()
    {
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        deckModel.BattleDrawPile.Clear();
        TableNine.Interface.GetSystem<IDeckSystem>().UpdateNextBattlePreview();

        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue || uid.Value.Equals(playerModel.PlayerCardUid))
            {
                continue;
            }

            var runtime = collectionModel.GetCard(uid.Value);
            if (runtime.CardType != CardType.Monster)
            {
                continue;
            }

            boardSystem.RemoveCardAt(slot, RemoveReason.Debug);
            collectionModel.RemoveCard(uid.Value);
        }
    }

    private sealed class TestController : IController
    {
        public IArchitecture GetArchitecture()
        {
            return TableNine.Interface;
        }
    }

    private sealed class ManualSequenceUtility : ISequenceUtility
    {
        private readonly Queue<System.Action> mPendingCallbacks = new Queue<System.Action>();

        public void Play(PresentationSequenceType sequenceType, System.Action onComplete)
        {
            mPendingCallbacks.Enqueue(onComplete);
        }

        public void CompleteNext()
        {
            Assert.That(mPendingCallbacks.Count, Is.GreaterThan(0), "No pending sequence callback to complete.");
            mPendingCallbacks.Dequeue()?.Invoke();
        }
    }

    private sealed class ManualTextAnimatorUtility : ITextAnimatorUtility
    {
        private readonly Queue<System.Action> mPendingCallbacks = new Queue<System.Action>();

        public string LastText { get; private set; }

        public void Show(string text, System.Action onComplete = null)
        {
            LastText = text;
            mPendingCallbacks.Enqueue(onComplete);
        }

        public void CompleteCurrent()
        {
            Assert.That(mPendingCallbacks.Count, Is.GreaterThan(0), "No pending dialogue callback to complete.");
            mPendingCallbacks.Dequeue()?.Invoke();
        }
    }
}
