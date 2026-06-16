using System;
using System.Collections.Generic;

namespace QfPlayAgent
{
    public sealed class PlayAgentCommandLogEntry
    {
        public int Step { get; set; }
        public DateTime UtcTime { get; set; }
        public string CommandName { get; set; }
        public string ArgsJson { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public string Result { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string ObservePathBefore { get; set; }
        public string ObservePathAfter { get; set; }
    }

    public static class PlayAgentSession
    {
        private static readonly List<PlayAgentCommandLogEntry> sLog = new List<PlayAgentCommandLogEntry>();
        private static int sStep;

        public static string SessionId { get; private set; } = Guid.NewGuid().ToString("N");
        public static IReadOnlyList<PlayAgentCommandLogEntry> Log => sLog;

        public static void ResetSession()
        {
            SessionId = Guid.NewGuid().ToString("N");
            sLog.Clear();
            sStep = 0;
        }

        public static PlayAgentCommandLogEntry BeginStep(string commandName, string argsJson, string observeBefore = null)
        {
            sStep++;
            var entry = new PlayAgentCommandLogEntry
            {
                Step = sStep,
                UtcTime = DateTime.UtcNow,
                CommandName = commandName,
                ArgsJson = argsJson ?? string.Empty,
                ObservePathBefore = observeBefore
            };
            sLog.Add(entry);
            return entry;
        }

        public static void CompleteStep(PlayAgentCommandLogEntry entry, bool success, string error, string result, long elapsedMs, string observeAfter = null)
        {
            entry.Success = success;
            entry.Error = error;
            entry.Result = result;
            entry.ElapsedMilliseconds = elapsedMs;
            entry.ObservePathAfter = observeAfter;
        }
    }
}
