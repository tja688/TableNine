using System.Collections.Generic;
using QFramework;
using UnityEngine;

public sealed class GameplayAudioPresenter : MonoBehaviour, IController
{
    private readonly List<IUnRegister> mRegisters = new List<IUnRegister>();
    private bool mRegistered;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void OnEnable()
    {
        TryRegister();
    }

    private void Start()
    {
        TryRegister();
    }

    private void TryRegister()
    {
        if (mRegistered || !TableNine.IsInitialized)
        {
            return;
        }

        mRegistered = true;
        Register<CardPlacedEvent>(_ => Play(TableNineAudioIds.Deal));
        Register<CardMovedEvent>(e => Play(e.IsBoardMovement ? TableNineAudioIds.Rotate : TableNineAudioIds.Move));
        Register<BoardRotatedEvent>(_ => Play(TableNineAudioIds.Rotate));
        Register<DamageAppliedEvent>(OnDamageApplied);
        Register<HealAppliedEvent>(_ => Play(TableNineAudioIds.Heal));
        Register<MonsterKilledEvent>(_ => Play(TableNineAudioIds.MonsterKilled));
        Register<HelpRewardPickedEvent>(_ => Play(TableNineAudioIds.Reward));
        Register<HelpRewardSkippedEvent>(_ => Play(TableNineAudioIds.Reward));
        Register<RelicRewardPickedEvent>(_ => Play(TableNineAudioIds.Reward));
        Register<ChestRewardSkippedEvent>(_ => Play(TableNineAudioIds.Reward));
        Register<HelpCardPurchasedEvent>(_ => Play(TableNineAudioIds.ShopBuy));
        Register<VictoryEvent>(_ => Play(TableNineAudioIds.Victory));
        Register<GameOverEvent>(_ => Play(TableNineAudioIds.GameOver));
    }

    private void OnDisable()
    {
        for (var i = 0; i < mRegisters.Count; i++)
        {
            mRegisters[i].UnRegister();
        }

        mRegisters.Clear();
        mRegistered = false;
    }

    private void Register<TEvent>(System.Action<TEvent> handler)
    {
        mRegisters.Add(this.RegisterEvent(handler));
    }

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (evt.Context != null && evt.Context.ArmorAbsorbed > 0 && evt.Context.HpDamage <= 0)
        {
            Play(TableNineAudioIds.ArmorAbsorb);
            return;
        }

        Play(TableNineAudioIds.Hit);
    }

    private void Play(string audioId)
    {
        this.GetUtility<IAudioUtility>().Play(audioId);
    }
}

public sealed class GameplayAnimationPresenter : MonoBehaviour, IController
{
    private readonly List<IUnRegister> mRegisters = new List<IUnRegister>();
    private readonly Queue<string> mRecentSignals = new Queue<string>();
    private bool mRegistered;

    [SerializeField] private int mMaxRecentSignals = 32;

    public IReadOnlyCollection<string> RecentSignals => mRecentSignals;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void OnEnable()
    {
        TryRegister();
    }

    private void Start()
    {
        TryRegister();
    }

    private void TryRegister()
    {
        if (mRegistered || !TableNine.IsInitialized)
        {
            return;
        }

        mRegistered = true;
        Register<CardPlacedEvent>(e => Record($"card_placed:{e.Uid.Value}:{e.Slot.Value}:{e.Source}"));
        Register<CardMovedEvent>(e => Record($"card_moved:{e.Uid.Value}:{e.PreviousSlot?.Value ?? 0}->{e.NewSlot.Value}:{e.Source}"));
        Register<BoardRotatedEvent>(e => Record($"board_rotated:{e.Clockwise}:{e.Reason}:{e.MovedCards.Count}"));
        Register<DamageAppliedEvent>(e => Record($"damage:{e.TargetUid.Value}:{e.Damage}"));
        Register<ArmorChangedEvent>(e => Record($"armor:{e.TargetUid.Value}:{e.OldArmor}->{e.NewArmor}"));
        Register<HealAppliedEvent>(e => Record($"heal:{e.TargetUid.Value}:{e.OldHp}->{e.NewHp}"));
        Register<MonsterKilledEvent>(e => Record($"monster_killed:{e.MonsterUid.Value}:{e.DefinitionId}"));
        Register<DialogueRequestedEvent>(e => Record($"dialogue_start:{e.Message}"));
        Register<DialogueCompletedEvent>(e => Record($"dialogue_end:{e.Message}"));
        Register<PresentationSequenceRequestedEvent>(e => Record($"sequence_start:{e.SequenceType}:{e.CompletionAction}"));
        Register<PresentationSequenceCompletedEvent>(e => Record($"sequence_end:{e.SequenceType}:{e.CompletionAction}"));
        Register<EffectResolvedEvent>(e => Record($"effect:{e.EffectGraphId}:{e.Source}"));
        Register<OverlayOpenedEvent>(e => Record($"overlay_open:{e.UiKey}:{e.BlocksGameplayInput}"));
        Register<OverlayClosedEvent>(e => Record($"overlay_close:{e.UiKey}"));
    }

    private void OnDisable()
    {
        for (var i = 0; i < mRegisters.Count; i++)
        {
            mRegisters[i].UnRegister();
        }

        mRegisters.Clear();
        mRecentSignals.Clear();
        mRegistered = false;
    }

    private void Register<TEvent>(System.Action<TEvent> handler)
    {
        mRegisters.Add(this.RegisterEvent(handler));
    }

    private void Record(string signal)
    {
        while (mRecentSignals.Count >= mMaxRecentSignals)
        {
            mRecentSignals.Dequeue();
        }

        mRecentSignals.Enqueue(signal);
    }
}
