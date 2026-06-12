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

public interface ISequenceUtility : IUtility
{
    void Run(Action action);
}

public sealed class ImmediateSequenceUtility : ISequenceUtility
{
    public void Run(Action action)
    {
        action?.Invoke();
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
