using System;
using System.Collections.Generic;

public enum SaveRunReason
{
    Manual,
    NewRun,
    NodeStart,
    NodeEnd,
    LayerComplete,
    Victory,
    GameOver
}

[Serializable]
public sealed class RunSaveData
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion = CurrentSchemaVersion;
    public int Seed;
    public int Layer;
    public int NodeInLayer;
    public string CharacterId;
    public FlowPhase Phase;
    public int NextCardUid;
    public int Gold;
    public int PlayerBaseAttack;
    public int PlayerBaseDefense;
    public int PlayerCurrentHp;
    public int PlayerMaxHp;
    public List<string> SkillIds = new List<string>();
    public List<RelicInstance> Relics = new List<RelicInstance>();
    public List<CardRuntimeSaveData> Cards = new List<CardRuntimeSaveData>();
    public List<HelpCardStateSaveData> HelpStates = new List<HelpCardStateSaveData>();
    public List<int> OwnedHelpCardUids = new List<int>();
    public List<int> BoardSlotUids = new List<int>();
    public int PlayerSlot = 5;
    public List<int> DemonDeckUids = new List<int>();
    public List<int> BattleDeckUids = new List<int>();
    public List<int> ItemSlotUids = new List<int>();
    public bool PendingTutorSkillChoice;
    public string SaveReason;
    public long SavedAtUnixSeconds;
}

[Serializable]
public sealed class CardRuntimeSaveData
{
    public int Uid;
    public string DefinitionId;
    public string DisplayName;
    public CardType CardType;
    public MonsterLevel MonsterLevel;
    public Suit Suit;
    public int? BoardSlot;
    public int? ItemSlotIndex;
    public int CurrentHp;
    public int MaxHp;
    public int CurrentArmor;
    public int BaseAttack;
    public int BaseDefense;
    public List<string> SkillIds = new List<string>();
}

[Serializable]
public sealed class HelpCardStateSaveData
{
    public int Uid;
    public string DefinitionId;
    public bool IsTemporarilyRemoved;
    public bool IsPermanentlyRemoved;
    public bool IsOnBoard;
    public bool IsInItemSlot;
}

[Serializable]
public sealed class CommandLogEntry
{
    public string CommandType;
    public string PayloadJson;
}

[Serializable]
public sealed class RunReplayData
{
    public int Seed;
    public string CharacterId = DefaultGameConfigFactory.CharacterImpId;
    public List<CommandLogEntry> Entries = new List<CommandLogEntry>();
}
