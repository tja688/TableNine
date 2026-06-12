using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QFramework;
using UnityEngine;
using Object = UnityEngine.Object;
using Stopwatch = System.Diagnostics.Stopwatch;

public interface IRandomUtility : IUtility
{
    int Range(int minInclusive, int maxExclusive);
    float Value();
    void SetSeed(int seed);
    void Shuffle<T>(IList<T> list);
    int ExportOperationIndex();
    void ImportSeedAndOperationIndex(int seed, int operationIndex);
}

public sealed class UnityRandomUtility : IRandomUtility
{
    private System.Random mRandom = new System.Random();
    private int mSeed = 1;
    private int mOperationIndex;

    public int Range(int minInclusive, int maxExclusive)
    {
        ConsumeRandom();
        return mRandom.Next(minInclusive, maxExclusive);
    }

    public float Value()
    {
        ConsumeRandom();
        return (float)mRandom.NextDouble();
    }

    public void SetSeed(int seed)
    {
        mSeed = seed;
        mRandom = new System.Random(seed);
        mOperationIndex = 0;
    }

    public void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            ConsumeRandom();
            var swapIndex = mRandom.Next(0, i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    public int ExportOperationIndex()
    {
        return mOperationIndex;
    }

    public void ImportSeedAndOperationIndex(int seed, int operationIndex)
    {
        SetSeed(seed);
        for (var i = 0; i < operationIndex; i++)
        {
            mRandom.Next();
        }

        mOperationIndex = operationIndex;
    }

    private void ConsumeRandom()
    {
        mOperationIndex++;
    }
}

public interface ISaveUtility : IUtility
{
    void SaveString(string key, string value);
    bool TryLoadString(string key, out string value);
    void DeleteKey(string key);
}

public sealed class MemorySaveUtility : ISaveUtility
{
    private readonly Dictionary<string, string> mData = new Dictionary<string, string>();

    public void SaveString(string key, string value)
    {
        mData[key] = value;
    }

    public bool TryLoadString(string key, out string value)
    {
        return mData.TryGetValue(key, out value);
    }

    public void DeleteKey(string key)
    {
        mData.Remove(key);
    }
}

public interface IConfigUtility : IUtility
{
    T LoadResource<T>(string resourcePath) where T : Object;
}

public sealed class ScriptableConfigUtility : IConfigUtility
{
    public T LoadResource<T>(string resourcePath) where T : Object
    {
        return Resources.Load<T>(resourcePath);
    }
}

public interface IResourceUtility : IUtility
{
    Sprite LoadCardSprite(string cardId);
    GameObject LoadCardPrefab(string cardId);
    GameObject LoadEffectPrefab(string effectId);
    AudioClip LoadAudioClip(string audioId);
    GameObject LoadOverlayPrefab(string overlayId);
}

public sealed class RuntimeResourceUtility : IResourceUtility
{
    public Sprite LoadCardSprite(string cardId)
    {
        return LoadFirst<Sprite>(
            $"TableNine/Cards/{cardId}",
            $"Cards/{cardId}",
            "TableNine/Cards/placeholder_card",
            "Cards/placeholder_card");
    }

    public GameObject LoadCardPrefab(string cardId)
    {
        return LoadFirst<GameObject>(
            $"TableNine/CardPrefabs/{cardId}",
            $"CardPrefabs/{cardId}",
            "TableNine/CardPrefabs/placeholder_card",
            "CardPrefabs/placeholder_card");
    }

    public GameObject LoadEffectPrefab(string effectId)
    {
        return LoadFirst<GameObject>(
            $"TableNine/Effects/{effectId}",
            $"Effects/{effectId}");
    }

    public AudioClip LoadAudioClip(string audioId)
    {
        return LoadFirst<AudioClip>(
            $"TableNine/Audio/{audioId}",
            $"Audio/{audioId}");
    }

    public GameObject LoadOverlayPrefab(string overlayId)
    {
        return LoadFirst<GameObject>(
            $"TableNine/Overlays/{overlayId}",
            $"Overlays/{overlayId}");
    }

    private static T LoadFirst<T>(params string[] resourcePaths) where T : Object
    {
        for (var i = 0; i < resourcePaths.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(resourcePaths[i]))
            {
                continue;
            }

            var loaded = Resources.Load<T>(resourcePaths[i]);
            if (loaded != null)
            {
                return loaded;
            }
        }

        return null;
    }
}

public static class TableNineAudioIds
{
    public const string Click = "click";
    public const string Deal = "card_deal";
    public const string Move = "card_move";
    public const string Rotate = "board_rotate";
    public const string Hit = "attack_hit";
    public const string ArmorAbsorb = "armor_absorb";
    public const string Heal = "heal";
    public const string MonsterKilled = "monster_killed";
    public const string Reward = "reward_select";
    public const string ShopBuy = "shop_buy";
    public const string Victory = "victory";
    public const string GameOver = "game_over";
}

public interface IAudioUtility : IUtility
{
    bool Muted { get; set; }
    string LastAudioId { get; }
    string LastBgmId { get; }
    void Play(string audioId);
    void PlayBgm(string audioId, bool loop = true);
    void StopBgm();
}

public sealed class NullAudioUtility : IAudioUtility
{
    public bool Muted { get; set; }
    public string LastAudioId { get; private set; }
    public string LastBgmId { get; private set; }

    public void Play(string audioId)
    {
        if (!Muted)
        {
            LastAudioId = audioId;
        }
    }

    public void PlayBgm(string audioId, bool loop = true)
    {
        if (!Muted)
        {
            LastBgmId = audioId;
        }
    }

    public void StopBgm()
    {
        LastBgmId = null;
    }
}

public sealed class ResourceAudioUtility : IAudioUtility
{
    private AudioSource mSfxSource;
    private AudioSource mBgmSource;

    public bool Muted { get; set; }
    public string LastAudioId { get; private set; }
    public string LastBgmId { get; private set; }

    public void Play(string audioId)
    {
        if (Muted || string.IsNullOrWhiteSpace(audioId))
        {
            return;
        }

        LastAudioId = audioId;
        var clip = LoadAudioClip(audioId);
        if (clip == null || !Application.isPlaying)
        {
            return;
        }

        EnsureSources();
        mSfxSource.PlayOneShot(clip);
    }

    public void PlayBgm(string audioId, bool loop = true)
    {
        if (Muted || string.IsNullOrWhiteSpace(audioId))
        {
            return;
        }

        LastBgmId = audioId;
        var clip = LoadAudioClip(audioId);
        if (clip == null || !Application.isPlaying)
        {
            return;
        }

        EnsureSources();
        mBgmSource.loop = loop;
        if (mBgmSource.clip == clip && mBgmSource.isPlaying)
        {
            return;
        }

        mBgmSource.clip = clip;
        mBgmSource.Play();
    }

    public void StopBgm()
    {
        LastBgmId = null;
        if (mBgmSource != null)
        {
            mBgmSource.Stop();
            mBgmSource.clip = null;
        }
    }

    private static AudioClip LoadAudioClip(string audioId)
    {
        return Resources.Load<AudioClip>($"TableNine/Audio/{audioId}") ??
               Resources.Load<AudioClip>($"Audio/{audioId}");
    }

    private void EnsureSources()
    {
        if (mSfxSource != null && mBgmSource != null)
        {
            return;
        }

        var host = GameObject.Find("TableNineAudioRuntime");
        if (host == null)
        {
            host = new GameObject("TableNineAudioRuntime");
            Object.DontDestroyOnLoad(host);
        }

        if (mSfxSource == null)
        {
            mSfxSource = host.AddComponent<AudioSource>();
        }

        if (mBgmSource == null)
        {
            mBgmSource = host.AddComponent<AudioSource>();
        }
    }
}

public interface ITextAnimatorUtility : IUtility
{
    string LastText { get; }
    void Show(string text, Action onComplete = null);
}

public sealed class NullTextAnimatorUtility : ITextAnimatorUtility
{
    public string LastText { get; private set; }

    public void Show(string text, Action onComplete = null)
    {
        LastText = text;
        onComplete?.Invoke();
    }
}

public interface ISequenceUtility : IUtility
{
    void Play(PresentationSequenceType sequenceType, Action onComplete);
}

public sealed class ImmediateSequenceUtility : ISequenceUtility
{
    public void Play(PresentationSequenceType sequenceType, Action onComplete)
    {
        onComplete?.Invoke();
    }
}

public readonly struct CommandTraceRecord
{
    public CommandTraceRecord(int sequence, string commandType, string phase, long elapsedMilliseconds, string detail)
    {
        Sequence = sequence;
        CommandType = commandType;
        Phase = phase;
        ElapsedMilliseconds = elapsedMilliseconds;
        Detail = detail;
    }

    public int Sequence { get; }
    public string CommandType { get; }
    public string Phase { get; }
    public long ElapsedMilliseconds { get; }
    public string Detail { get; }
}

public interface ICommandTraceUtility : IUtility
{
    IReadOnlyList<CommandTraceRecord> Records { get; }
    void Clear();
    void Before(ICommand command);
    void Before<TResult>(ICommand<TResult> command);
    void After(ICommand command);
    void After<TResult>(ICommand<TResult> command, TResult result);
    void OnException(object command, Exception exception);
}

public sealed class CommandTraceUtility : ICommandTraceUtility
{
    private sealed class TraceState
    {
        public int Sequence;
        public Stopwatch Stopwatch = new Stopwatch();
    }

    private readonly List<CommandTraceRecord> mRecords = new List<CommandTraceRecord>();
    private readonly Dictionary<int, TraceState> mActiveStates = new Dictionary<int, TraceState>();
    private int mSequence;

    public IReadOnlyList<CommandTraceRecord> Records => mRecords;

    public void Clear()
    {
        mRecords.Clear();
        mActiveStates.Clear();
        mSequence = 0;
    }

    public void Before(ICommand command)
    {
        BeforeInternal(command);
    }

    public void Before<TResult>(ICommand<TResult> command)
    {
        BeforeInternal(command);
    }

    public void After(ICommand command)
    {
        AfterInternal(command, string.Empty);
    }

    public void After<TResult>(ICommand<TResult> command, TResult result)
    {
        AfterInternal(command, FormatResult(result));
    }

    public void OnException(object command, Exception exception)
    {
        var state = PopState(command);
        var elapsedMilliseconds = state?.Stopwatch.ElapsedMilliseconds ?? 0L;
        var sequence = state?.Sequence ?? -1;
        var commandType = GetCommandType(command);
        var detail = exception.Message;
        var record = new CommandTraceRecord(sequence, commandType, "exception", elapsedMilliseconds, detail);
        mRecords.Add(record);
        Debug.LogError(FormatMessage(record));
    }

    private void BeforeInternal(object command)
    {
        var key = GetCommandKey(command);
        var state = new TraceState
        {
            Sequence = ++mSequence
        };

        state.Stopwatch.Start();
        mActiveStates[key] = state;

        var record = new CommandTraceRecord(state.Sequence, GetCommandType(command), "before", 0L, string.Empty);
        mRecords.Add(record);
        if (!Application.isPlaying)
        {
            Debug.Log(FormatMessage(record));
        }
    }

    private void AfterInternal(object command, string detail)
    {
        var state = PopState(command);
        var elapsedMilliseconds = state?.Stopwatch.ElapsedMilliseconds ?? 0L;
        var sequence = state?.Sequence ?? -1;
        var record = new CommandTraceRecord(sequence, GetCommandType(command), "after", elapsedMilliseconds, detail);
        mRecords.Add(record);
        if (!Application.isPlaying)
        {
            Debug.Log(FormatMessage(record));
        }
    }

    private TraceState PopState(object command)
    {
        var key = GetCommandKey(command);
        if (!mActiveStates.TryGetValue(key, out var state))
        {
            return null;
        }

        state.Stopwatch.Stop();
        mActiveStates.Remove(key);
        return state;
    }

    private static int GetCommandKey(object command)
    {
        return command == null ? 0 : RuntimeHelpers.GetHashCode(command);
    }

    private static string GetCommandType(object command)
    {
        return command == null ? "<null>" : command.GetType().Name;
    }

    private static string FormatResult<T>(T result)
    {
        if (ReferenceEquals(result, null))
        {
            return string.Empty;
        }

        return $"result={result}";
    }

    private static string FormatMessage(CommandTraceRecord record)
    {
        var elapsedPart = record.Phase == "before" ? string.Empty : $" ({record.ElapsedMilliseconds} ms)";
        var detailPart = string.IsNullOrWhiteSpace(record.Detail) ? string.Empty : $" {record.Detail}";
        return $"[CommandTrace #{record.Sequence}] {record.CommandType} {record.Phase}{elapsedPart}{detailPart}";
    }
}
