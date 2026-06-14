using System.Collections.Generic;
using QFramework;
using UnityEngine;

public interface IRunModel : IModel
{
    BindableProperty<int> Layer { get; }
    BindableProperty<int> NodeInLayer { get; }
    BindableProperty<int> Seed { get; }
    BindableProperty<bool> IsRunActive { get; }
    string CharacterId { get; }
    void StartRun(string characterId, int seed);
    void SetNode(int layer, int nodeInLayer);
}

public sealed class RunModel : AbstractModel, IRunModel
{
    private readonly BindableProperty<int> mLayer = new BindableProperty<int>();
    private readonly BindableProperty<int> mNodeInLayer = new BindableProperty<int>();
    private readonly BindableProperty<int> mSeed = new BindableProperty<int>();
    private readonly BindableProperty<bool> mIsRunActive = new BindableProperty<bool>();

    public BindableProperty<int> Layer => mLayer;
    public BindableProperty<int> NodeInLayer => mNodeInLayer;
    public BindableProperty<int> Seed => mSeed;
    public BindableProperty<bool> IsRunActive => mIsRunActive;
    public string CharacterId { get; private set; }

    protected override void OnInit()
    {
        CharacterId = string.Empty;
    }

    public void StartRun(string characterId, int seed)
    {
        CharacterId = characterId;
        mSeed.Value = seed;
        mLayer.Value = 1;
        mNodeInLayer.Value = 1;
        mIsRunActive.Value = true;
    }

    public void SetNode(int layer, int nodeInLayer)
    {
        mLayer.Value = layer;
        mNodeInLayer.Value = nodeInLayer;
    }
}

public interface IPlayerModel : IModel
{
    CardUid PlayerCardUid { get; set; }
    BindableProperty<int> Gold { get; }
    int BaseHp { get; }
    int BaseAttack { get; }
    int BaseArmor { get; }
    IReadOnlyList<string> SkillIds { get; }
    IReadOnlyList<RelicInstance> Relics { get; }
    int MaxRelicCount { get; }
    void AddRelic(RelicInstance relic);
    bool RemoveRelic(string relicId);
    bool HasRelic(string relicId);
    void ResetFromCharacter(CharacterDefinition characterDefinition, CardUid playerCardUid);
    void AddSkill(string skillId);
}

public sealed class PlayerModel : AbstractModel, IPlayerModel
{
    private readonly BindableProperty<int> mGold = new BindableProperty<int>();
    private readonly List<string> mSkillIds = new List<string>();
    private readonly List<RelicInstance> mRelics = new List<RelicInstance>();

    public CardUid PlayerCardUid { get; set; }
    public BindableProperty<int> Gold => mGold;
    public int BaseHp { get; private set; }
    public int BaseAttack { get; private set; }
    public int BaseArmor { get; private set; }
    public IReadOnlyList<string> SkillIds => mSkillIds;
    public IReadOnlyList<RelicInstance> Relics => mRelics;
    public int MaxRelicCount => 12;

    protected override void OnInit()
    {
    }

    public void AddRelic(RelicInstance relic)
    {
        if (mRelics.Count >= MaxRelicCount) return;
        if (HasRelic(relic.RelicId)) return;
        mRelics.Add(relic);
    }

    public bool RemoveRelic(string relicId)
    {
        for (var i = 0; i < mRelics.Count; i++)
        {
            if (mRelics[i].RelicId == relicId)
            {
                mRelics.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public bool HasRelic(string relicId)
    {
        for (var i = 0; i < mRelics.Count; i++)
        {
            if (mRelics[i].RelicId == relicId && !mRelics[i].IsConsumed)
            {
                return true;
            }
        }

        return false;
    }

    public void ResetFromCharacter(CharacterDefinition characterDefinition, CardUid playerCardUid)
    {
        PlayerCardUid = playerCardUid;
        BaseHp = characterDefinition.BaseHp;
        BaseAttack = characterDefinition.BaseAttack;
        BaseArmor = characterDefinition.BaseArmor;
        mGold.Value = 0;
        mSkillIds.Clear();
        mSkillIds.AddRange(characterDefinition.InitialSkillIds);
        mRelics.Clear();
    }

    public void AddSkill(string skillId)
    {
        if (!mSkillIds.Contains(skillId))
        {
            mSkillIds.Add(skillId);
        }
    }
}

public interface IBoardModel : IModel
{
    BoardSlotNo PlayerSlot { get; set; }
    CardUid? GetCardAt(BoardSlotNo slot);
    void SetCardAt(BoardSlotNo slot, CardUid? uid);
    void Clear();
    List<BoardSlotNo> GetEmptySlots();
}

public sealed class BoardModel : AbstractModel, IBoardModel
{
    private readonly CardUid?[] mSlots = new CardUid?[10];

    public BoardSlotNo PlayerSlot { get; set; }

    protected override void OnInit()
    {
        PlayerSlot = new BoardSlotNo(5);
        Clear();
    }

    public CardUid? GetCardAt(BoardSlotNo slot)
    {
        return mSlots[slot.Value];
    }

    public void SetCardAt(BoardSlotNo slot, CardUid? uid)
    {
        mSlots[slot.Value] = uid;
    }

    public void Clear()
    {
        for (var i = 1; i <= 9; i++)
        {
            mSlots[i] = null;
        }
    }

    public List<BoardSlotNo> GetEmptySlots()
    {
        var slots = new List<BoardSlotNo>();
        for (var i = 1; i <= 9; i++)
        {
            if (!mSlots[i].HasValue)
            {
                slots.Add(new BoardSlotNo(i));
            }
        }

        return slots;
    }
}

public interface IDeckModel : IModel
{
    List<CardUid> OwnedHelpCards { get; }
    Dictionary<int, HelpCardState> HelpCardStates { get; }
    Queue<CardUid> DemonDeckQueue { get; }
    Queue<CardUid> BattleDrawPile { get; }
    CardUid?[] ItemSlots { get; }
    HelpDeckSnapshot NodeStartSnapshot { get; set; }
    PendingHelpCardAction PendingHelpCardAction { get; }
    BindableProperty<CardPreview> NextBattleCardPreview { get; }
    bool RefillRunning { get; set; }
    bool RefillPending { get; set; }
    bool PendingTutorSkillChoice { get; set; }
    void ResetForNewRun();
    void ClearNodeState();
    int FindFirstEmptyItemSlot();
    DeckCapacity GetCapacity(int layer);
    int CountActiveHelpCards();
    int CountHelpCardsById(string definitionId);
    int GetHelpDeckCapacity(int layer);
}

public sealed class DeckModel : AbstractModel, IDeckModel
{
    private readonly List<CardUid> mOwnedHelpCards = new List<CardUid>();
    private readonly Dictionary<int, HelpCardState> mHelpCardStates = new Dictionary<int, HelpCardState>();
    private readonly Queue<CardUid> mDemonDeckQueue = new Queue<CardUid>();
    private readonly Queue<CardUid> mBattleDrawPile = new Queue<CardUid>();
    private readonly CardUid?[] mItemSlots = new CardUid?[5];
    private readonly PendingHelpCardAction mPendingHelpCardAction = new PendingHelpCardAction();
    private readonly BindableProperty<CardPreview> mNextBattleCardPreview = new BindableProperty<CardPreview>();

    public List<CardUid> OwnedHelpCards => mOwnedHelpCards;
    public Dictionary<int, HelpCardState> HelpCardStates => mHelpCardStates;
    public Queue<CardUid> DemonDeckQueue => mDemonDeckQueue;
    public Queue<CardUid> BattleDrawPile => mBattleDrawPile;
    public CardUid?[] ItemSlots => mItemSlots;
    public HelpDeckSnapshot NodeStartSnapshot { get; set; } = new HelpDeckSnapshot();
    public PendingHelpCardAction PendingHelpCardAction => mPendingHelpCardAction;
    public BindableProperty<CardPreview> NextBattleCardPreview => mNextBattleCardPreview;
    public bool RefillRunning { get; set; }
    public bool RefillPending { get; set; }
    public bool PendingTutorSkillChoice { get; set; }

    protected override void OnInit()
    {
        ResetForNewRun();
    }

    public void ResetForNewRun()
    {
        mOwnedHelpCards.Clear();
        mHelpCardStates.Clear();
        ClearNodeState();
        NodeStartSnapshot = new HelpDeckSnapshot();
    }

    public void ClearNodeState()
    {
        mDemonDeckQueue.Clear();
        mBattleDrawPile.Clear();
        for (var i = 0; i < mItemSlots.Length; i++)
        {
            mItemSlots[i] = null;
        }

        foreach (var state in mHelpCardStates.Values)
        {
            state.IsOnBoard = false;
            state.IsInItemSlot = false;
            state.IsTemporarilyRemoved = false;
        }

        mPendingHelpCardAction.Clear();
        NextBattleCardPreview.Value = CardPreview.Empty;
        RefillRunning = false;
        RefillPending = false;
        PendingTutorSkillChoice = false;
    }

    public int FindFirstEmptyItemSlot()
    {
        for (var i = 0; i < mItemSlots.Length; i++)
        {
            if (!mItemSlots[i].HasValue)
            {
                return i;
            }
        }

        return -1;
    }

    public DeckCapacity GetCapacity(int layer)
    {
        return DeckCapacity.ForLayer(layer);
    }

    public int CountActiveHelpCards()
    {
        var count = 0;
        foreach (var pair in mHelpCardStates)
        {
            if (!pair.Value.IsPermanentlyRemoved)
            {
                count++;
            }
        }

        return count;
    }

    public int CountHelpCardsById(string definitionId)
    {
        var count = 0;
        foreach (var pair in mHelpCardStates)
        {
            if (!pair.Value.IsPermanentlyRemoved && pair.Value.DefinitionId == definitionId)
            {
                count++;
            }
        }

        return count;
    }

    public int GetHelpDeckCapacity(int layer)
    {
        // Layer 1: 12, Layer 2: 18, Layer 3: 24
        return 6 * (layer + 1);
    }
}

public interface ICollectionModel : IModel
{
    IReadOnlyDictionary<int, CardRuntime> Cards { get; }
    CardRuntime CreatePlayerCard(CharacterDefinition definition);
    CardRuntime CreateCard(CardDefinition definition);
    void RestoreCard(CardRuntime runtime);
    CardRuntime GetCard(CardUid uid);
    bool TryGetCard(CardUid uid, out CardRuntime cardRuntime);
    bool RemoveCard(CardUid uid);
    void Clear();
    int GetNextUid();
    void SetNextUid(int nextUid);
}

public sealed class CollectionModel : AbstractModel, ICollectionModel
{
    private readonly Dictionary<int, CardRuntime> mCards = new Dictionary<int, CardRuntime>();
    private int mNextUid = 1;

    public IReadOnlyDictionary<int, CardRuntime> Cards => mCards;

    protected override void OnInit()
    {
        Clear();
    }

    public CardRuntime CreatePlayerCard(CharacterDefinition definition)
    {
        var runtime = new CardRuntime
        {
            Uid = CreateUid(),
            DefinitionId = definition.CharacterId,
            DisplayName = definition.DisplayName,
            CardType = CardType.Player,
            CurrentHp = definition.BaseHp,
            MaxHp = definition.BaseHp,
            CurrentArmor = 0,
            BaseAttack = definition.BaseAttack,
            BaseArmor = definition.BaseArmor,
            SkillIds = new List<string>(definition.InitialSkillIds)
        };

        mCards[runtime.Uid.Value] = runtime;
        return runtime;
    }

    public void RestoreCard(CardRuntime runtime)
    {
        mCards[runtime.Uid.Value] = runtime;
        if (runtime.Uid.Value >= mNextUid)
        {
            mNextUid = runtime.Uid.Value + 1;
        }
    }

    public int GetNextUid()
    {
        return mNextUid;
    }

    public void SetNextUid(int nextUid)
    {
        mNextUid = nextUid < 1 ? 1 : nextUid;
    }

    public CardRuntime CreateCard(CardDefinition definition)
    {
        var runtime = new CardRuntime
        {
            Uid = CreateUid(),
            DefinitionId = definition.CardId,
            DisplayName = definition.DisplayName,
            CardType = definition.CardType,
            MonsterLevel = definition.MonsterLevel,
            Suit = definition.Suit,
            CurrentHp = definition.BaseHp,
            MaxHp = definition.BaseHp,
            CurrentArmor = 0,
            BaseAttack = definition.BaseAttack,
            BaseArmor = definition.BaseArmor,
            SkillIds = new List<string>(definition.SkillIds)
        };

        mCards[runtime.Uid.Value] = runtime;
        return runtime;
    }

    public CardRuntime GetCard(CardUid uid)
    {
        return mCards[uid.Value];
    }

    public bool TryGetCard(CardUid uid, out CardRuntime cardRuntime)
    {
        return mCards.TryGetValue(uid.Value, out cardRuntime);
    }

    public bool RemoveCard(CardUid uid)
    {
        return mCards.Remove(uid.Value);
    }

    public void Clear()
    {
        mCards.Clear();
        mNextUid = 1;
    }

    private CardUid CreateUid()
    {
        return new CardUid(mNextUid++);
    }
}

public interface IConfigModel : IModel
{
    bool IsLoaded { get; }
    IReadOnlyList<string> ValidationErrors { get; }
    CardDefinition GetCardDefinition(string cardId);
    CardDeckDefinition GetCardDeckDefinition(string deckId);
    CharacterDefinition GetCharacterDefinition(string characterId);
    SkillDefinition GetSkillDefinition(string skillId);
    IReadOnlyList<SkillDefinition> GetAllSkillDefinitions();
    MonsterDeckRuleDefinition GetMonsterDeckRule(int layer, int nodeInLayer);
    IReadOnlyList<CardDefinition> GetAllHelpCardDefinitions();
    IReadOnlyList<CardDefinition> GetPlayableHelpCardDefinitions(string characterId);
    IReadOnlyList<CardDefinition> GetHelpCardsByQuality(CardQuality quality);
    bool TryGetCardDefinition(string cardId, out CardDefinition definition);
    bool TryGetCardDeckDefinition(string deckId, out CardDeckDefinition definition);
    RelicDefinition GetRelicDefinition(string relicId);
    IReadOnlyList<RelicDefinition> GetAllRelicDefinitions();
    RoomDefinition GetRoomDefinition(string roomId);
    IReadOnlyList<CardDefinition> GetCardsByType(CardType cardType);
    IReadOnlyList<CardDefinition> GetCardsByDeck(string deckId);
    Sprite GetCardMainImage(string cardId);
    Sprite GetEffectiveCardFaceImage(string cardId);
    Sprite GetEffectiveCardBackImage(string cardId);
    bool TryGetEffectGraph(string effectGraphId, out EffectGraphDefinition graph);
    IReadOnlyList<SkillEffectBinding> GetSkillBindings(SkillTrigger trigger);
    IReadOnlyList<SkillBehaviorRule> GetSkillBehaviorRules(SkillTrigger trigger);
    IReadOnlyList<SkillBehaviorRule> GetEffectiveStatBehaviorRules();
}

public sealed class ConfigModel : AbstractModel, IConfigModel
{
    private readonly Dictionary<string, CardDefinition> mCardsById = new Dictionary<string, CardDefinition>();
    private readonly Dictionary<string, CardDeckDefinition> mCardDecksById = new Dictionary<string, CardDeckDefinition>();
    private readonly Dictionary<string, CharacterDefinition> mCharactersById = new Dictionary<string, CharacterDefinition>();
    private readonly Dictionary<string, SkillDefinition> mSkillsById = new Dictionary<string, SkillDefinition>();
    private readonly Dictionary<string, MonsterDeckRuleDefinition> mMonsterRulesByKey = new Dictionary<string, MonsterDeckRuleDefinition>();
    private readonly Dictionary<string, RelicDefinition> mRelicsById = new Dictionary<string, RelicDefinition>();
    private readonly Dictionary<string, RoomDefinition> mRoomsById = new Dictionary<string, RoomDefinition>();
    private readonly Dictionary<string, EffectGraphDefinition> mEffectGraphsById = new Dictionary<string, EffectGraphDefinition>();
    private readonly List<SkillEffectBinding> mSkillBindings = new List<SkillEffectBinding>();
    private readonly List<SkillBehaviorRule> mSkillBehaviorRules = new List<SkillBehaviorRule>();
    private readonly List<string> mValidationErrors = new List<string>();

    public bool IsLoaded { get; private set; }
    public IReadOnlyList<string> ValidationErrors => mValidationErrors;

    protected override void OnInit()
    {
        var gameConfig = TableNine.GameConfig
            ?? TableNine.TestGameConfigResolver?.Invoke()
            ?? TryLoadDefaultGameConfig();
        if (gameConfig == null)
        {
            Debug.LogError("[ConfigModel] TableNineGameConfig is missing. Assign it on GameplayBootstrap or run TableNine/Game Config/Sync From Code Defaults.");
            IsLoaded = false;
            return;
        }

        var bundle = gameConfig.ToRuntimeBundle();

        mValidationErrors.Clear();
        mValidationErrors.AddRange(ConfigValidator.Validate(bundle));
        var warnings = ConfigValidator.CollectWarnings(bundle.Core);
        for (var i = 0; i < mValidationErrors.Count; i++)
        {
            Debug.LogWarning($"[ConfigValidator] {mValidationErrors[i]}");
        }

        for (var i = 0; i < warnings.Count; i++)
        {
            Debug.LogWarning($"[ConfigValidator] {warnings[i]}");
        }

        mCardsById.Clear();
        mCardDecksById.Clear();
        mCharactersById.Clear();
        mSkillsById.Clear();
        mMonsterRulesByKey.Clear();
        mRelicsById.Clear();
        mRoomsById.Clear();
        mEffectGraphsById.Clear();
        mSkillBindings.Clear();
        mSkillBehaviorRules.Clear();

        var config = bundle.Core;
        for (var i = 0; i < config.CardDecks.Count; i++)
        {
            var deck = config.CardDecks[i];
            if (deck != null && !string.IsNullOrEmpty(deck.DeckId))
            {
                mCardDecksById[deck.DeckId] = deck;
            }
        }

        for (var i = 0; i < config.Cards.Count; i++)
        {
            mCardsById[config.Cards[i].CardId] = config.Cards[i];
        }

        for (var i = 0; i < config.Characters.Count; i++)
        {
            mCharactersById[config.Characters[i].CharacterId] = config.Characters[i];
        }

        for (var i = 0; i < config.Skills.Count; i++)
        {
            mSkillsById[config.Skills[i].SkillId] = config.Skills[i];
        }

        for (var i = 0; i < config.MonsterDeckRules.Count; i++)
        {
            var rule = config.MonsterDeckRules[i];
            mMonsterRulesByKey[BuildRuleKey(rule.Layer, rule.NodeInLayer)] = rule;
        }

        for (var i = 0; i < config.Relics.Count; i++)
        {
            mRelicsById[config.Relics[i].RelicId] = config.Relics[i];
        }

        for (var i = 0; i < config.Rooms.Count; i++)
        {
            mRoomsById[config.Rooms[i].RoomId] = config.Rooms[i];
        }

        for (var i = 0; i < bundle.EffectGraphs.Count; i++)
        {
            var graph = bundle.EffectGraphs[i];
            mEffectGraphsById[graph.EffectGraphId] = graph;
        }

        mSkillBindings.AddRange(bundle.SkillBindings);
        mSkillBehaviorRules.AddRange(bundle.SkillBehaviorRules);
        IsLoaded = true;
    }

#if UNITY_EDITOR
    private static TableNineGameConfig TryLoadDefaultGameConfig()
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<TableNineGameConfig>(
            "Assets/ScriptableObjects/TableNineGameConfig.asset");
    }
#else
    private static TableNineGameConfig TryLoadDefaultGameConfig()
    {
        return null;
    }
#endif

    public CardDefinition GetCardDefinition(string cardId)
    {
        return mCardsById[cardId];
    }

    public CardDeckDefinition GetCardDeckDefinition(string deckId)
    {
        return mCardDecksById[deckId];
    }

    public CharacterDefinition GetCharacterDefinition(string characterId)
    {
        return mCharactersById[characterId];
    }

    public SkillDefinition GetSkillDefinition(string skillId)
    {
        return mSkillsById[skillId];
    }

    public IReadOnlyList<SkillDefinition> GetAllSkillDefinitions()
    {
        var result = new List<SkillDefinition>(mSkillsById.Count);
        foreach (var skill in mSkillsById.Values)
        {
            result.Add(skill);
        }

        return result;
    }

    public MonsterDeckRuleDefinition GetMonsterDeckRule(int layer, int nodeInLayer)
    {
        return mMonsterRulesByKey[BuildRuleKey(layer, nodeInLayer)];
    }

    public IReadOnlyList<CardDefinition> GetAllHelpCardDefinitions()
    {
        var result = new List<CardDefinition>();
        foreach (var card in mCardsById.Values)
        {
            if (card.CardType == CardType.Help)
            {
                result.Add(card);
            }
        }

        return result;
    }

    public IReadOnlyList<CardDefinition> GetPlayableHelpCardDefinitions(string characterId)
    {
        if (string.IsNullOrEmpty(characterId) || !mCharactersById.TryGetValue(characterId, out var character))
        {
            return GetAllHelpCardDefinitions();
        }

        var result = new List<CardDefinition>();
        foreach (var card in mCardsById.Values)
        {
            if (card.CardType != CardType.Help)
            {
                continue;
            }

            if (card.DeckId == character.CommonDeckId || card.DeckId == character.ClassDeckId)
            {
                result.Add(card);
            }
        }

        return result.Count > 0 ? result : GetAllHelpCardDefinitions();
    }

    public IReadOnlyList<CardDefinition> GetHelpCardsByQuality(CardQuality quality)
    {
        var result = new List<CardDefinition>();
        foreach (var card in mCardsById.Values)
        {
            if (card.CardType == CardType.Help && card.Quality == quality)
            {
                result.Add(card);
            }
        }

        return result;
    }

    public bool TryGetCardDefinition(string cardId, out CardDefinition definition)
    {
        return mCardsById.TryGetValue(cardId, out definition);
    }

    public bool TryGetCardDeckDefinition(string deckId, out CardDeckDefinition definition)
    {
        return mCardDecksById.TryGetValue(deckId, out definition);
    }

    public RelicDefinition GetRelicDefinition(string relicId)
    {
        return mRelicsById[relicId];
    }

    public IReadOnlyList<RelicDefinition> GetAllRelicDefinitions()
    {
        var result = new List<RelicDefinition>(mRelicsById.Count);
        foreach (var relic in mRelicsById.Values)
        {
            result.Add(relic);
        }

        return result;
    }

    public RoomDefinition GetRoomDefinition(string roomId)
    {
        return mRoomsById[roomId];
    }

    public IReadOnlyList<CardDefinition> GetCardsByType(CardType cardType)
    {
        var result = new List<CardDefinition>();
        foreach (var card in mCardsById.Values)
        {
            if (card.CardType == cardType)
            {
                result.Add(card);
            }
        }

        return result;
    }

    public IReadOnlyList<CardDefinition> GetCardsByDeck(string deckId)
    {
        var result = new List<CardDefinition>();
        if (string.IsNullOrEmpty(deckId))
        {
            return result;
        }

        foreach (var card in mCardsById.Values)
        {
            if (card.DeckId == deckId)
            {
                result.Add(card);
            }
        }

        return result;
    }

    public Sprite GetCardMainImage(string cardId)
    {
        return TryGetCardDefinition(cardId, out var card) ? card.Image : null;
    }

    public Sprite GetEffectiveCardFaceImage(string cardId)
    {
        if (!TryGetCardDefinition(cardId, out var card))
        {
            return null;
        }

        if (card.FaceImageOverride != null)
        {
            return card.FaceImageOverride;
        }

        return TryGetCardDeckDefinition(card.DeckId, out var deck) ? deck.DefaultFaceImage : null;
    }

    public Sprite GetEffectiveCardBackImage(string cardId)
    {
        if (!TryGetCardDefinition(cardId, out var card))
        {
            return null;
        }

        if (card.BackImageOverride != null)
        {
            return card.BackImageOverride;
        }

        return TryGetCardDeckDefinition(card.DeckId, out var deck) ? deck.DefaultBackImage : null;
    }

    public bool TryGetEffectGraph(string effectGraphId, out EffectGraphDefinition graph)
    {
        return mEffectGraphsById.TryGetValue(effectGraphId, out graph);
    }

    public IReadOnlyList<SkillEffectBinding> GetSkillBindings(SkillTrigger trigger)
    {
        var result = new List<SkillEffectBinding>();
        for (var i = 0; i < mSkillBindings.Count; i++)
        {
            if (mSkillBindings[i].Trigger == trigger)
            {
                result.Add(mSkillBindings[i]);
            }
        }

        var skills = GetAllSkillDefinitions();
        for (var i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (!skill.HasRuntimeBinding || skill.Trigger != trigger)
            {
                continue;
            }

            result.Add(new SkillEffectBinding
            {
                BindingId = $"player_skill_{skill.SkillId}_{skill.Trigger}",
                OwnerKind = SkillOwnerKind.PlayerSkill,
                OwnerDefinitionId = skill.SkillId,
                Trigger = skill.Trigger,
                ConditionKey = skill.ConditionKey,
                EffectGraphId = skill.EffectGraphId
            });
        }

        return result;
    }

    public IReadOnlyList<SkillBehaviorRule> GetSkillBehaviorRules(SkillTrigger trigger)
    {
        var result = new List<SkillBehaviorRule>();
        for (var i = 0; i < mSkillBehaviorRules.Count; i++)
        {
            if (mSkillBehaviorRules[i].Trigger == trigger)
            {
                result.Add(mSkillBehaviorRules[i]);
            }
        }

        return result;
    }

    public IReadOnlyList<SkillBehaviorRule> GetEffectiveStatBehaviorRules()
    {
        return GetSkillBehaviorRules(SkillTrigger.OnComputeEffectiveStats);
    }

    private static string BuildRuleKey(int layer, int nodeInLayer)
    {
        return $"{layer}:{nodeInLayer}";
    }
}

public interface IFlowModel : IModel
{
    BindableProperty<FlowPhase> Phase { get; }
    IReadOnlyCollection<InputLockReason> ActiveLocks { get; }
    bool IsInputLocked { get; }
    void SetPhase(FlowPhase phase);
    void AddLock(InputLockReason reason);
    void RemoveLock(InputLockReason reason);
    bool HasLock(InputLockReason reason);
    void Reset();
    void RestoreActiveLocks(System.Collections.Generic.IEnumerable<InputLockReason> locks);
}

public sealed class FlowModel : AbstractModel, IFlowModel
{
    private readonly BindableProperty<FlowPhase> mPhase = new BindableProperty<FlowPhase>();
    private readonly HashSet<InputLockReason> mActiveLocks = new HashSet<InputLockReason>();

    public BindableProperty<FlowPhase> Phase => mPhase;
    public IReadOnlyCollection<InputLockReason> ActiveLocks => mActiveLocks;
    public bool IsInputLocked => mActiveLocks.Count > 0;

    protected override void OnInit()
    {
        Reset();
    }

    public void SetPhase(FlowPhase phase)
    {
        if (mPhase.Value == phase)
        {
            return;
        }

        var previous = mPhase.Value;
        mPhase.Value = phase;
        this.SendEvent(new FlowPhaseChangedEvent(previous, phase));
    }

    public void AddLock(InputLockReason reason)
    {
        if (!mActiveLocks.Add(reason))
        {
            return;
        }

        this.SendEvent(new InputLockChangedEvent(reason, true, mActiveLocks.Count > 0));
    }

    public void RemoveLock(InputLockReason reason)
    {
        if (!mActiveLocks.Remove(reason))
        {
            return;
        }

        this.SendEvent(new InputLockChangedEvent(reason, false, mActiveLocks.Count > 0));
    }

    public bool HasLock(InputLockReason reason)
    {
        return mActiveLocks.Contains(reason);
    }

    public void Reset()
    {
        mPhase.Value = FlowPhase.None;
        mActiveLocks.Clear();
    }

    public void RestoreActiveLocks(System.Collections.Generic.IEnumerable<InputLockReason> locks)
    {
        mActiveLocks.Clear();
        if (locks == null)
        {
            return;
        }

        foreach (var reason in locks)
        {
            mActiveLocks.Add(reason);
        }
    }
}

public interface IRewardModel : IModel
{
    IReadOnlyList<string> HelpRewardCardIds { get; }
    IReadOnlyList<string> ChestRewardRelicIds { get; }
    IReadOnlyList<string> TutorSkillIds { get; }
    IReadOnlyList<string> ShopCardIds { get; }
    IReadOnlyList<string> RoomCandidateIds { get; }
    RewardSource CurrentRewardSource { get; set; }
    FlowPhase? RewardResumePhase { get; set; }
    void Clear();
    void ClearHelpRewardCardIds();
    void ClearChestRewardRelicIds();
    void ClearTutorSkillIds();
    void ClearShopCardIds();
    void ClearRoomCandidateIds();
    void AddHelpRewardCardId(string cardId);
    void AddChestRewardRelicId(string relicId);
    void AddTutorSkillId(string skillId);
    void AddRoomCandidateId(string roomId);
    void AddShopCardId(string cardId);
    void RemoveShopCardId(string cardId);
}

public sealed class RewardModel : AbstractModel, IRewardModel
{
    private readonly List<string> mHelpRewardCardIds = new List<string>();
    private readonly List<string> mChestRewardRelicIds = new List<string>();
    private readonly List<string> mTutorSkillIds = new List<string>();
    private readonly List<string> mShopCardIds = new List<string>();
    private readonly List<string> mRoomCandidateIds = new List<string>();

    public IReadOnlyList<string> HelpRewardCardIds => mHelpRewardCardIds;
    public IReadOnlyList<string> ChestRewardRelicIds => mChestRewardRelicIds;
    public IReadOnlyList<string> TutorSkillIds => mTutorSkillIds;
    public IReadOnlyList<string> ShopCardIds => mShopCardIds;
    public IReadOnlyList<string> RoomCandidateIds => mRoomCandidateIds;
    public RewardSource CurrentRewardSource { get; set; }
    public FlowPhase? RewardResumePhase { get; set; }

    protected override void OnInit()
    {
        Clear();
    }

    public void Clear()
    {
        mHelpRewardCardIds.Clear();
        mChestRewardRelicIds.Clear();
        mTutorSkillIds.Clear();
        mShopCardIds.Clear();
        mRoomCandidateIds.Clear();
        CurrentRewardSource = RewardSource.None;
        RewardResumePhase = null;
    }

    public void ClearHelpRewardCardIds() => mHelpRewardCardIds.Clear();
    public void ClearChestRewardRelicIds() => mChestRewardRelicIds.Clear();
    public void ClearTutorSkillIds() => mTutorSkillIds.Clear();
    public void ClearShopCardIds() => mShopCardIds.Clear();
    public void ClearRoomCandidateIds() => mRoomCandidateIds.Clear();
    public void AddHelpRewardCardId(string cardId) => mHelpRewardCardIds.Add(cardId);
    public void AddChestRewardRelicId(string relicId) => mChestRewardRelicIds.Add(relicId);
    public void AddTutorSkillId(string skillId) => mTutorSkillIds.Add(skillId);
    public void AddRoomCandidateId(string roomId) => mRoomCandidateIds.Add(roomId);
    public void AddShopCardId(string cardId) => mShopCardIds.Add(cardId);
    public void RemoveShopCardId(string cardId) => mShopCardIds.Remove(cardId);
}
