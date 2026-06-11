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

        var save = new RunSaveData
        {
            SchemaVersion = RunSaveData.CurrentSchemaVersion,
            Seed = runModel.Seed.Value,
            Layer = runModel.Layer.Value,
            NodeInLayer = runModel.NodeInLayer.Value,
            CharacterId = runModel.CharacterId,
            Phase = flowModel.Phase.Value,
            NextCardUid = collectionModel.GetNextUid(),
            Gold = playerModel.Gold.Value,
            PlayerSlot = boardModel.PlayerSlot.Value,
            PendingTutorSkillChoice = deckModel.PendingTutorSkillChoice,
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
            save.HelpStates.Add(new HelpCardStateSaveData
            {
                Uid = state.Uid.Value,
                DefinitionId = state.DefinitionId,
                IsTemporarilyRemoved = state.IsTemporarilyRemoved,
                IsPermanentlyRemoved = state.IsPermanentlyRemoved,
                IsOnBoard = state.IsOnBoard,
                IsInItemSlot = state.IsInItemSlot
            });
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
        if (save == null || save.SchemaVersion != RunSaveData.CurrentSchemaVersion)
        {
            return false;
        }

        ApplySaveData(save);
        this.SendEvent(new RunLoadedEvent(slot));
        return true;
    }

    public void ApplySaveData(RunSaveData save)
    {
        if (save == null || save.SchemaVersion != RunSaveData.CurrentSchemaVersion)
        {
            return;
        }

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
        var randomUtility = this.GetUtility<IRandomUtility>();

        collectionModel.Clear();
        collectionModel.SetNextUid(save.NextCardUid);
        deckModel.ResetForNewRun();
        boardModel.Clear();
        flowModel.Reset();

        randomUtility.SetSeed(save.Seed);
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
        this.GetSystem<IDeckSystem>().UpdateNextBattlePreview();
        flowModel.SetPhase(save.Phase);
    }

    private static string BuildKey(string slot)
    {
        return $"run_save::{slot}";
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
