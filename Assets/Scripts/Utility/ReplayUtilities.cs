using System;
using System.Collections.Generic;
using System.Reflection;
using QFramework;

public interface ICommandReplayUtility : IUtility
{
    IReadOnlyList<CommandLogEntry> Entries { get; }
    void Clear();
    void Record(object command);
    RunReplayData Export();
}

public sealed class CommandReplayUtility : ICommandReplayUtility
{
    private readonly List<CommandLogEntry> mEntries = new List<CommandLogEntry>();

    public IReadOnlyList<CommandLogEntry> Entries => mEntries;

    public void Clear()
    {
        mEntries.Clear();
    }

    public void Record(object command)
    {
        if (command == null)
        {
            return;
        }

        var commandType = command.GetType().Name;
        if (!CommandReplayFactory.IsReplayable(commandType))
        {
            return;
        }

        mEntries.Add(new CommandLogEntry
        {
            CommandType = commandType,
            PayloadJson = CommandPayloadSerializer.Serialize(command)
        });
    }

    public RunReplayData Export()
    {
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var replay = new RunReplayData
        {
            Seed = runModel.Seed.Value,
            CharacterId = runModel.CharacterId
        };
        replay.Entries.AddRange(mEntries);
        return replay;
    }
}

public static class CommandPayloadSerializer
{
    public static string Serialize(object command)
    {
        var type = command.GetType();
        var fields = new List<string>();
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var value = property.GetValue(command);
            fields.Add($"{property.Name}={FormatValue(value)}");
        }

        return string.Join("|", fields);
    }

    private static string FormatValue(object value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is CardUid uid)
        {
            return uid.Value.ToString();
        }

        if (value is BoardSlotNo slot)
        {
            return slot.Value.ToString();
        }

        if (value is Enum enumValue)
        {
            return enumValue.ToString();
        }

        return value.ToString();
    }
}

public static class CommandReplayFactory
{
    private static readonly HashSet<string> ReplayableCommandTypes = new HashSet<string>
    {
        nameof(StartNewRunCommand),
        nameof(ClickBoardSlotCommand),
        nameof(ClickItemSlotCommand),
        nameof(UseHelpCardCommand),
        nameof(ResolveTargetingCommand),
        nameof(ResolveAttributeChoiceCommand),
        nameof(PickHelpCardRewardCommand),
        nameof(SkipHelpRewardCommand),
        nameof(ChooseRoomCommand),
        nameof(PickRelicRewardCommand),
        nameof(SkipChestRewardCommand),
        nameof(BuyHelpCardCommand),
        nameof(DeleteHelpCardForGoldCommand),
        nameof(CloseShopCommand),
        nameof(ChooseTutorSkillCommand)
    };

    public static bool IsReplayable(string commandType)
    {
        return !string.IsNullOrEmpty(commandType) && ReplayableCommandTypes.Contains(commandType);
    }

    public static ICommand Create(CommandLogEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.CommandType))
        {
            return null;
        }

        var payload = ParsePayload(entry.PayloadJson);
        switch (entry.CommandType)
        {
            case nameof(StartNewRunCommand):
                return new StartNewRunCommand(
                    GetString(payload, nameof(StartNewRunCommand.CharacterId), GameConfigIds.CharacterImpId),
                    GetNullableInt(payload, nameof(StartNewRunCommand.SeedOverride)));
            case nameof(ClickBoardSlotCommand):
                return new ClickBoardSlotCommand(new BoardSlotNo(GetInt(payload, nameof(ClickBoardSlotCommand.Slot))));
            case nameof(ClickItemSlotCommand):
                return new ClickItemSlotCommand(GetInt(payload, nameof(ClickItemSlotCommand.ItemSlotIndex)));
            case nameof(UseHelpCardCommand):
                return new UseHelpCardCommand(new CardUid(GetInt(payload, nameof(UseHelpCardCommand.HelpCardUid))));
            case nameof(ResolveTargetingCommand):
                return new ResolveTargetingCommand(new BoardSlotNo(GetInt(payload, nameof(ResolveTargetingCommand.Slot))));
            case nameof(ResolveAttributeChoiceCommand):
                return new ResolveAttributeChoiceCommand(ParseEnum(payload, nameof(ResolveAttributeChoiceCommand.Choice), AttributeUpgradeChoice.Attack));
            case nameof(PickHelpCardRewardCommand):
                return new PickHelpCardRewardCommand(GetString(payload, nameof(PickHelpCardRewardCommand.CardId), string.Empty));
            case nameof(SkipHelpRewardCommand):
                return new SkipHelpRewardCommand();
            case nameof(ChooseRoomCommand):
                return new ChooseRoomCommand(GetString(payload, nameof(ChooseRoomCommand.RoomId), string.Empty));
            case nameof(PickRelicRewardCommand):
                return new PickRelicRewardCommand(GetString(payload, nameof(PickRelicRewardCommand.RelicId), string.Empty));
            case nameof(SkipChestRewardCommand):
                return new SkipChestRewardCommand();
            case nameof(BuyHelpCardCommand):
                return new BuyHelpCardCommand(GetString(payload, nameof(BuyHelpCardCommand.CardId), string.Empty));
            case nameof(DeleteHelpCardForGoldCommand):
                return new DeleteHelpCardForGoldCommand(new CardUid(GetInt(payload, nameof(DeleteHelpCardForGoldCommand.HelpCardUid))));
            case nameof(CloseShopCommand):
                return new CloseShopCommand();
            case nameof(ChooseTutorSkillCommand):
                return new ChooseTutorSkillCommand(GetString(payload, nameof(ChooseTutorSkillCommand.SkillId), string.Empty));
            default:
                return null;
        }
    }

    private static Dictionary<string, string> ParsePayload(string payload)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(payload))
        {
            return result;
        }

        var parts = payload.Split('|');
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            result[part.Substring(0, separator)] = part.Substring(separator + 1);
        }

        return result;
    }

    private static string GetString(Dictionary<string, string> payload, string key, string fallback)
    {
        return payload.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;
    }

    private static int GetInt(Dictionary<string, string> payload, string key)
    {
        return payload.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : 0;
    }

    private static int? GetNullableInt(Dictionary<string, string> payload, string key)
    {
        if (!payload.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return null;
        }

        return int.TryParse(value, out var parsed) ? parsed : (int?)null;
    }

    private static TEnum ParseEnum<TEnum>(Dictionary<string, string> payload, string key, TEnum fallback)
        where TEnum : struct
    {
        if (!payload.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return fallback;
        }

        return Enum.TryParse(value, out TEnum parsed) ? parsed : fallback;
    }
}

public interface IDebugEventLogUtility : IUtility
{
    IReadOnlyList<string> RecentEvents { get; }
    void Record(string message);
    void Clear();
}

public sealed class DebugEventLogUtility : IDebugEventLogUtility
{
    private const int MaxEntries = 50;
    private readonly List<string> mRecentEvents = new List<string>();

    public IReadOnlyList<string> RecentEvents => mRecentEvents;

    public void Record(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        mRecentEvents.Add(line);
        if (mRecentEvents.Count > MaxEntries)
        {
            mRecentEvents.RemoveAt(0);
        }
    }

    public void Clear()
    {
        mRecentEvents.Clear();
    }
}
