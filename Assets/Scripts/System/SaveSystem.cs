using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public interface ISaveSystem : ISystem
{
    bool HasSave(string slot);
    RunSaveData CaptureCurrentRun(SaveRunReason reason);
    void SaveCurrentRun(string slot, SaveRunReason reason);
    bool TryLoadRun(string slot);
    void ApplySaveData(RunSaveData save);
    void DeleteSave(string slot);
}

public sealed class SaveSystem : AbstractSystem, ISaveSystem
{
    public const string DefaultSlot = "table_nine_run";

    protected override void OnInit()
    {
    }

    public bool HasSave(string slot)
    {
        return this.GetUtility<ISaveUtility>().TryLoadString(BuildKey(slot), out _);
    }

    public RunSaveData CaptureCurrentRun(SaveRunReason reason)
    {
        var runModel = this.GetModel<IRunModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var flowModel = this.GetModel<IFlowModel>();
        var boardModel = this.GetModel<IBoardModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var configModel = this.GetModel<IConfigModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        var save = new RunSaveData
        {
            SchemaVersion = RunSaveData.CurrentSchemaVersion,
            Seed = runModel.Seed.Value,
            RandomOperationIndex = randomUtility.ExportOperationIndex(),
            Layer = runModel.Layer.Value,
            NodeInLayer = runModel.NodeInLayer.Value,
            CharacterId = runModel.CharacterId,
            Phase = flowModel.Phase.Value,
            NextCardUid = collectionModel.GetNextUid(),
            Gold = playerModel.Gold.Value,
            PlayerSlot = boardModel.PlayerSlot.Value,
            PendingTutorSkillChoice = deckModel.PendingTutorSkillChoice,
            RefillRunning = deckModel.RefillRunning,
            RefillPending = deckModel.RefillPending,
            SaveReason = reason.ToString(),
            SavedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        save.SkillIds.AddRange(playerModel.SkillIds);
        for (var i = 0; i < playerModel.Relics.Count; i++)
        {
            save.Relics.Add(CloneRelic(playerModel.Relics[i]));
        }

        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
        save.PlayerBaseAttack = playerRuntime.BaseAttack;
        save.PlayerBaseDefense = playerRuntime.BaseDefense;
        save.PlayerCurrentHp = playerRuntime.CurrentHp;
        save.PlayerMaxHp = playerRuntime.MaxHp;

        foreach (var pair in collectionModel.Cards)
        {
            save.Cards.Add(ToCardSave(pair.Value));
        }

        foreach (var pair in deckModel.HelpCardStates)
        {
            var state = pair.Value;
            var restoreAfterNode = false;
            if (configModel.TryGetCardDefinition(state.DefinitionId, out var definition))
            {
                restoreAfterNode = definition.RestoreAfterNode;
            }

            save.HelpStates.Add(new HelpCardStateSaveData
            {
                Uid = state.Uid.Value,
                DefinitionId = state.DefinitionId,
                RestoreAfterNode = restoreAfterNode,
                IsTemporarilyRemoved = state.IsTemporarilyRemoved,
                IsPermanentlyRemoved = state.IsPermanentlyRemoved,
                IsOnBoard = state.IsOnBoard,
                IsInItemSlot = state.IsInItemSlot
            });
        }

        for (var i = 0; i < deckModel.NodeStartSnapshot.Cards.Count; i++)
        {
            var snapshotState = deckModel.NodeStartSnapshot.Cards[i];
            save.NodeStartSnapshotCards.Add(ToHelpStateSave(snapshotState, configModel));
        }

        var pending = deckModel.PendingHelpCardAction;
        save.PendingHelpCardAction = new PendingHelpCardActionSaveData
        {
            HelpCardUid = pending.HelpCardUid.Value,
            Kind = pending.Kind,
            TargetingDamage = pending.TargetingDamage,
            TargetingCauseId = pending.TargetingCauseId,
            TargetingMode = pending.TargetingMode,
            SwapFirstTargetSelected = pending.SwapFirstTargetSelected,
            SwapFirstTargetUid = pending.SwapFirstTargetUid?.Value ?? -1
        };

        save.RewardContext.CurrentRewardSource = rewardModel.CurrentRewardSource;
        if (rewardModel.RewardResumePhase.HasValue)
        {
            save.RewardContext.HasRewardResumePhase = true;
            save.RewardContext.RewardResumePhase = rewardModel.RewardResumePhase.Value;
        }

        save.RewardContext.HelpRewardCardIds.AddRange(rewardModel.HelpRewardCardIds);
        save.RewardContext.ChestRewardRelicIds.AddRange(rewardModel.ChestRewardRelicIds);
        save.RewardContext.TutorSkillIds.AddRange(rewardModel.TutorSkillIds);
        save.RewardContext.ShopCardIds.AddRange(rewardModel.ShopCardIds);
        save.RewardContext.RoomCandidateIds.AddRange(rewardModel.RoomCandidateIds);

        foreach (var lockReason in flowModel.ActiveLocks)
        {
            save.ActiveInputLocks.Add(lockReason.ToString());
        }

        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            save.OwnedHelpCardUids.Add(deckModel.OwnedHelpCards[i].Value);
        }

        for (var slot = 1; slot <= 9; slot++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(slot));
            save.BoardSlotUids.Add(uid.HasValue ? uid.Value.Value : -1);
        }

        foreach (var uid in deckModel.DemonDeckQueue)
        {
            save.DemonDeckUids.Add(uid.Value);
        }

        foreach (var uid in deckModel.BattleDrawPile)
        {
            save.BattleDeckUids.Add(uid.Value);
        }

        for (var i = 0; i < deckModel.ItemSlots.Length; i++)
        {
            save.ItemSlotUids.Add(deckModel.ItemSlots[i].HasValue ? deckModel.ItemSlots[i].Value.Value : -1);
        }

        return save;
    }

    public void SaveCurrentRun(string slot, SaveRunReason reason)
    {
        var save = CaptureCurrentRun(reason);
        var json = JsonUtility.ToJson(save);
        this.GetUtility<ISaveUtility>().SaveString(BuildKey(slot), json);
        this.SendEvent(new RunSavedEvent(slot, reason));
    }

    public bool TryLoadRun(string slot)
    {
        var saveUtility = this.GetUtility<ISaveUtility>();
        if (!saveUtility.TryLoadString(BuildKey(slot), out var json) || string.IsNullOrEmpty(json))
        {
            return false;
        }

        var save = JsonUtility.FromJson<RunSaveData>(json);
        if (save == null || !IsSupportedSchema(save.SchemaVersion))
        {
            return false;
        }

        NormalizeLegacySave(save);
        ApplySaveData(save);
        this.SendEvent(new RunLoadedEvent(slot));
        return true;
    }

    public void ApplySaveData(RunSaveData save)
    {
        if (save == null || !IsSupportedSchema(save.SchemaVersion))
        {
            return;
        }

        NormalizeLegacySave(save);
        RestoreRun(save);
    }

    public void DeleteSave(string slot)
    {
        this.GetUtility<ISaveUtility>().DeleteKey(BuildKey(slot));
        this.SendEvent(new RunSaveDeletedEvent(slot));
    }

    private void RestoreRun(RunSaveData save)
    {
        var configModel = this.GetModel<IConfigModel>();
        var runModel = this.GetModel<IRunModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var boardModel = this.GetModel<IBoardModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var flowModel = this.GetModel<IFlowModel>();
        var rewardModel = this.GetModel<IRewardModel>();
        var randomUtility = this.GetUtility<IRandomUtility>();

        collectionModel.Clear();
        collectionModel.SetNextUid(save.NextCardUid);
        deckModel.ResetForNewRun();
        boardModel.Clear();
        flowModel.Reset();
        rewardModel.Clear();

        randomUtility.ImportSeedAndOperationIndex(save.Seed, save.RandomOperationIndex);
        runModel.StartRun(save.CharacterId, save.Seed);
        runModel.SetNode(save.Layer, save.NodeInLayer);

        var characterDefinition = configModel.GetCharacterDefinition(save.CharacterId);
        playerModel.ResetFromCharacter(characterDefinition, default);
        playerModel.Gold.Value = save.Gold;
        for (var i = 0; i < save.SkillIds.Count; i++)
        {
            playerModel.AddSkill(save.SkillIds[i]);
        }

        for (var i = 0; i < save.Relics.Count; i++)
        {
            playerModel.AddRelic(CloneRelic(save.Relics[i]));
        }

        CardUid playerUid = default;
        for (var i = 0; i < save.Cards.Count; i++)
        {
            var cardSave = save.Cards[i];
            var runtime = FromCardSave(cardSave);
            collectionModel.RestoreCard(runtime);
            if (runtime.CardType == CardType.Player)
            {
                playerUid = runtime.Uid;
            }
        }

        playerModel.PlayerCardUid = playerUid;
        var playerRuntime = collectionModel.GetCard(playerUid);
        playerRuntime.BaseAttack = save.PlayerBaseAttack;
        playerRuntime.BaseDefense = save.PlayerBaseDefense;
        playerRuntime.CurrentHp = save.PlayerCurrentHp;
        playerRuntime.MaxHp = save.PlayerMaxHp;

        for (var i = 0; i < save.OwnedHelpCardUids.Count; i++)
        {
            deckModel.OwnedHelpCards.Add(new CardUid(save.OwnedHelpCardUids[i]));
        }

        for (var i = 0; i < save.HelpStates.Count; i++)
        {
            var stateSave = save.HelpStates[i];
            deckModel.HelpCardStates[stateSave.Uid] = new HelpCardState
            {
                Uid = new CardUid(stateSave.Uid),
                DefinitionId = stateSave.DefinitionId,
                IsTemporarilyRemoved = stateSave.IsTemporarilyRemoved,
                IsPermanentlyRemoved = stateSave.IsPermanentlyRemoved,
                IsOnBoard = stateSave.IsOnBoard,
                IsInItemSlot = stateSave.IsInItemSlot
            };
        }

        deckModel.NodeStartSnapshot = new HelpDeckSnapshot();
        for (var i = 0; i < save.NodeStartSnapshotCards.Count; i++)
        {
            deckModel.NodeStartSnapshot.Cards.Add(FromHelpStateSave(save.NodeStartSnapshotCards[i]));
        }

        var pendingSave = save.PendingHelpCardAction;
        if (pendingSave != null && pendingSave.Kind != PendingHelpCardActionKind.None)
        {
            var pending = deckModel.PendingHelpCardAction;
            pending.HelpCardUid = new CardUid(pendingSave.HelpCardUid);
            pending.Kind = pendingSave.Kind;
            pending.TargetingDamage = pendingSave.TargetingDamage;
            pending.TargetingCauseId = pendingSave.TargetingCauseId;
            pending.TargetingMode = pendingSave.TargetingMode;
            pending.SwapFirstTargetSelected = pendingSave.SwapFirstTargetSelected;
            pending.SwapFirstTargetUid = pendingSave.SwapFirstTargetUid >= 0
                ? new CardUid(pendingSave.SwapFirstTargetUid)
                : (CardUid?)null;
        }

        var rewardSave = save.RewardContext;
        if (rewardSave != null)
        {
            rewardModel.CurrentRewardSource = rewardSave.CurrentRewardSource;
            rewardModel.RewardResumePhase = rewardSave.HasRewardResumePhase ? rewardSave.RewardResumePhase : (FlowPhase?)null;
            for (var i = 0; i < rewardSave.HelpRewardCardIds.Count; i++)
            {
                rewardModel.AddHelpRewardCardId(rewardSave.HelpRewardCardIds[i]);
            }

            for (var i = 0; i < rewardSave.ChestRewardRelicIds.Count; i++)
            {
                rewardModel.AddChestRewardRelicId(rewardSave.ChestRewardRelicIds[i]);
            }

            for (var i = 0; i < rewardSave.TutorSkillIds.Count; i++)
            {
                rewardModel.AddTutorSkillId(rewardSave.TutorSkillIds[i]);
            }

            for (var i = 0; i < rewardSave.ShopCardIds.Count; i++)
            {
                rewardModel.AddShopCardId(rewardSave.ShopCardIds[i]);
            }

            for (var i = 0; i < rewardSave.RoomCandidateIds.Count; i++)
            {
                rewardModel.AddRoomCandidateId(rewardSave.RoomCandidateIds[i]);
            }
        }

        boardModel.PlayerSlot = new BoardSlotNo(save.PlayerSlot);
        for (var slot = 1; slot <= save.BoardSlotUids.Count; slot++)
        {
            var uidValue = save.BoardSlotUids[slot - 1];
            boardModel.SetCardAt(new BoardSlotNo(slot), uidValue >= 0 ? new CardUid(uidValue) : (CardUid?)null);
        }

        for (var i = 0; i < save.DemonDeckUids.Count; i++)
        {
            deckModel.DemonDeckQueue.Enqueue(new CardUid(save.DemonDeckUids[i]));
        }

        for (var i = 0; i < save.BattleDeckUids.Count; i++)
        {
            deckModel.BattleDrawPile.Enqueue(new CardUid(save.BattleDeckUids[i]));
        }

        for (var i = 0; i < save.ItemSlotUids.Count; i++)
        {
            var uidValue = save.ItemSlotUids[i];
            deckModel.ItemSlots[i] = uidValue >= 0 ? new CardUid(uidValue) : (CardUid?)null;
        }

        deckModel.PendingTutorSkillChoice = save.PendingTutorSkillChoice;
        deckModel.RefillRunning = save.RefillRunning;
        deckModel.RefillPending = save.RefillPending;
        this.GetSystem<IDeckSystem>().UpdateNextBattlePreview();

        var restoredLocks = new List<InputLockReason>();
        for (var i = 0; i < save.ActiveInputLocks.Count; i++)
        {
            if (Enum.TryParse(save.ActiveInputLocks[i], out InputLockReason lockReason))
            {
                restoredLocks.Add(lockReason);
            }
        }

        flowModel.RestoreActiveLocks(restoredLocks);
        flowModel.SetPhase(save.Phase);
        DispatchOverlayRestoreEvents(save.Phase, rewardModel, deckModel);
    }

    private void DispatchOverlayRestoreEvents(FlowPhase phase, IRewardModel rewardModel, IDeckModel deckModel)
    {
        switch (phase)
        {
            case FlowPhase.HelpRewardChoosing:
                if (rewardModel.HelpRewardCardIds.Count > 0)
                {
                    this.SendEvent(new HelpRewardGeneratedEvent(rewardModel.HelpRewardCardIds));
                }

                break;
            case FlowPhase.RoomChoosing:
                if (rewardModel.RoomCandidateIds.Count > 0)
                {
                    this.SendEvent(new RoomChoiceRequestedEvent(rewardModel.RoomCandidateIds));
                }

                break;
            case FlowPhase.ChestRewardChoosing:
                if (rewardModel.ChestRewardRelicIds.Count > 0)
                {
                    this.SendEvent(new ChestRewardGeneratedEvent(rewardModel.ChestRewardRelicIds));
                }

                break;
            case FlowPhase.Shop:
                if (rewardModel.ShopCardIds.Count > 0)
                {
                    this.SendEvent(new ShopOpenedEvent(rewardModel.ShopCardIds));
                }

                break;
            case FlowPhase.TutorSkillChoosing:
                if (rewardModel.TutorSkillIds.Count > 0)
                {
                    this.SendEvent(new TutorSkillChoiceRequestedEvent(rewardModel.TutorSkillIds));
                }

                break;
        }

        var pending = deckModel.PendingHelpCardAction;
        if (pending.Kind == PendingHelpCardActionKind.AttributeChoice)
        {
            this.SendEvent(new AttributeChoiceRequestedEvent(pending.HelpCardUid));
        }
    }

    private static bool IsSupportedSchema(int schemaVersion)
    {
        return schemaVersion == 1 || schemaVersion == RunSaveData.CurrentSchemaVersion;
    }

    private static void NormalizeLegacySave(RunSaveData save)
    {
        if (save.SchemaVersion >= RunSaveData.CurrentSchemaVersion)
        {
            return;
        }

        save.SchemaVersion = RunSaveData.CurrentSchemaVersion;
        save.PendingHelpCardAction ??= new PendingHelpCardActionSaveData();
        save.RewardContext ??= new RewardContextSaveData();
        save.NodeStartSnapshotCards ??= new List<HelpCardStateSaveData>();
        save.ActiveInputLocks ??= new List<string>();
    }

    private static string BuildKey(string slot)
    {
        return $"run_save::{slot}";
    }

    private static HelpCardStateSaveData ToHelpStateSave(HelpCardState state, IConfigModel configModel)
    {
        var restoreAfterNode = false;
        if (configModel.TryGetCardDefinition(state.DefinitionId, out var definition))
        {
            restoreAfterNode = definition.RestoreAfterNode;
        }

        return new HelpCardStateSaveData
        {
            Uid = state.Uid.Value,
            DefinitionId = state.DefinitionId,
            RestoreAfterNode = restoreAfterNode,
            IsTemporarilyRemoved = state.IsTemporarilyRemoved,
            IsPermanentlyRemoved = state.IsPermanentlyRemoved,
            IsOnBoard = state.IsOnBoard,
            IsInItemSlot = state.IsInItemSlot
        };
    }

    private static HelpCardState FromHelpStateSave(HelpCardStateSaveData save)
    {
        return new HelpCardState
        {
            Uid = new CardUid(save.Uid),
            DefinitionId = save.DefinitionId,
            IsTemporarilyRemoved = save.IsTemporarilyRemoved,
            IsPermanentlyRemoved = save.IsPermanentlyRemoved,
            IsOnBoard = save.IsOnBoard,
            IsInItemSlot = save.IsInItemSlot
        };
    }

    private static CardRuntimeSaveData ToCardSave(CardRuntime runtime)
    {
        return new CardRuntimeSaveData
        {
            Uid = runtime.Uid.Value,
            DefinitionId = runtime.DefinitionId,
            DisplayName = runtime.DisplayName,
            CardType = runtime.CardType,
            MonsterLevel = runtime.MonsterLevel,
            Suit = runtime.Suit,
            BoardSlot = runtime.BoardSlot?.Value,
            ItemSlotIndex = runtime.ItemSlotIndex,
            CurrentHp = runtime.CurrentHp,
            MaxHp = runtime.MaxHp,
            CurrentArmor = runtime.CurrentArmor,
            BaseAttack = runtime.BaseAttack,
            BaseDefense = runtime.BaseDefense,
            SkillIds = new List<string>(runtime.SkillIds)
        };
    }

    private static CardRuntime FromCardSave(CardRuntimeSaveData save)
    {
        return new CardRuntime
        {
            Uid = new CardUid(save.Uid),
            DefinitionId = save.DefinitionId,
            DisplayName = save.DisplayName,
            CardType = save.CardType,
            MonsterLevel = save.MonsterLevel,
            Suit = save.Suit,
            BoardSlot = save.BoardSlot.HasValue ? new BoardSlotNo(save.BoardSlot.Value) : (BoardSlotNo?)null,
            ItemSlotIndex = save.ItemSlotIndex,
            CurrentHp = save.CurrentHp,
            MaxHp = save.MaxHp,
            CurrentArmor = save.CurrentArmor,
            BaseAttack = save.BaseAttack,
            BaseDefense = save.BaseDefense,
            SkillIds = new List<string>(save.SkillIds)
        };
    }

    private static RelicInstance CloneRelic(RelicInstance relic)
    {
        return new RelicInstance
        {
            RelicId = relic.RelicId,
            DisplayName = relic.DisplayName,
            Quality = relic.Quality,
            StatAttackBonus = relic.StatAttackBonus,
            StatDefenseBonus = relic.StatDefenseBonus,
            StatMaxHpBonus = relic.StatMaxHpBonus,
            IsOneShot = relic.IsOneShot,
            HasTriggered = relic.HasTriggered,
            IsConsumed = relic.IsConsumed
        };
    }
}
