using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QFramework;
using UnityEditor;
using UnityEngine;

namespace QfPlayAgent.Editor
{
    [McpForUnityTool(
        "qf_play_agent",
        Description = "QFramework Play Agent: list/invoke player-intent Commands, observe screenshots, wait, logs, snapshot.",
        Group = "testing")]
    public static class QfPlayAgentMcpTool
    {
        public class Parameters
        {
            [ToolParameter("Action: list_commands | invoke_command | observe | wait | get_command_log | reset | get_runtime_snapshot | read_logs")]
            public string action { get; set; }

            [ToolParameter("Command type name for invoke_command", Required = false)]
            public string command { get; set; }

            [ToolParameter("Command arguments object", Required = false)]
            public JObject args { get; set; }

            [ToolParameter("Filter by risk: player_input | system | debug", Required = false)]
            public string risk { get; set; }

            [ToolParameter("Frames to wait after invoke_command or for wait action", Required = false)]
            public int? wait_frames { get; set; }

            [ToolParameter("Wait timeout in milliseconds", Required = false)]
            public int? timeout_ms { get; set; }

            [ToolParameter("Screenshot filename without extension", Required = false)]
            public string filename { get; set; }

            [ToolParameter("Screenshot width", Required = false)]
            public int? width { get; set; }

            [ToolParameter("Screenshot height", Required = false)]
            public int? height { get; set; }

            [ToolParameter("Console log count for read_logs", Required = false)]
            public int? log_count { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            var parameters = @params?.ToObject<Parameters>() ?? new Parameters();
            if (string.IsNullOrWhiteSpace(parameters.action))
            {
                return new ErrorResponse("action is required");
            }

            switch (parameters.action.Trim().ToLowerInvariant())
            {
                case "list_commands":
                    return HandleListCommands(parameters);
                case "invoke_command":
                    return HandleInvokeCommand(parameters);
                case "observe":
                    return HandleObserve(parameters);
                case "wait":
                    return HandleWait(parameters);
                case "get_command_log":
                    return HandleGetCommandLog();
                case "reset":
                    return HandleReset();
                case "get_runtime_snapshot":
                    return HandleGetRuntimeSnapshot();
                case "read_logs":
                    return HandleReadLogs(parameters);
                default:
                    return new ErrorResponse($"Unknown action '{parameters.action}'");
            }
        }

        private static object HandleListCommands(Parameters parameters)
        {
            QfAiRisk? riskFilter = null;
            if (!string.IsNullOrWhiteSpace(parameters.risk) &&
                Enum.TryParse<QfAiRisk>(parameters.risk, true, out var parsedRisk))
            {
                riskFilter = parsedRisk;
            }

            var descriptors = PlayAgentCommandCatalog.ListCommands(PlayAgentBootstrap.Catalog, riskFilter);
            var payload = descriptors.Select(d => new
            {
                name = d.Name,
                type = d.FullTypeName,
                description = d.Description,
                risk = d.Risk.ToString(),
                default_wait_frames = d.DefaultWaitFrames,
                args = d.Args.Select(a => new
                {
                    name = a.Name,
                    type = a.TypeName,
                    description = a.Description,
                    required = a.Required,
                    default_value = a.DefaultValue
                })
            });

            return new SuccessResponse("Listed exposed commands.", new
            {
                session_id = PlayAgentSession.SessionId,
                architecture_ready = PlayAgentBootstrap.ArchitectureProvider?.IsReady ?? false,
                commands = payload
            });
        }

        private static object HandleInvokeCommand(Parameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.command))
            {
                return new ErrorResponse("command is required for invoke_command");
            }

            if (!PlayAgentBootstrap.TryGetReadyArchitecture(out var architecture, out var archError))
            {
                return new ErrorResponse(archError);
            }

            var args = ConvertArgs(parameters.args);
            var argsJson = parameters.args?.ToString(Formatting.None) ?? "{}";
            var logEntry = PlayAgentSession.BeginStep(parameters.command, argsJson);

            var invokeResult = PlayAgentInvoker.Invoke(architecture, parameters.command, args);
            var waitFrames = parameters.wait_frames ?? ResolveDefaultWaitFrames(parameters.command);
            var timeoutMs = parameters.timeout_ms ?? Math.Max(500, waitFrames * 100);
            PlayAgentEditorWait.WaitFrames(waitFrames, timeoutMs);

            PlayAgentSession.CompleteStep(
                logEntry,
                invokeResult.Success,
                invokeResult.Error,
                invokeResult.Result,
                invokeResult.ElapsedMilliseconds);

            if (!invokeResult.Success)
            {
                return new ErrorResponse(invokeResult.Error, new
                {
                    session_id = PlayAgentSession.SessionId,
                    command = parameters.command,
                    args = args,
                    log_step = logEntry.Step
                });
            }

            return new SuccessResponse($"Invoked {parameters.command}", new
            {
                session_id = PlayAgentSession.SessionId,
                command = parameters.command,
                args,
                result = invokeResult.Result,
                elapsed_ms = invokeResult.ElapsedMilliseconds,
                waited_frames = waitFrames,
                log_step = logEntry.Step
            });
        }

        private static object HandleObserve(Parameters parameters)
        {
            var filename = string.IsNullOrWhiteSpace(parameters.filename)
                ? $"step_{PlayAgentSession.Log.Count + 1:000}"
                : parameters.filename;

            if (!PlayAgentObserveHelper.TryCaptureScreenshot(
                    filename,
                    out var absolutePath,
                    out var error,
                    parameters.width,
                    parameters.height))
            {
                return new ErrorResponse(error);
            }

            return new SuccessResponse("Screenshot captured.", new
            {
                session_id = PlayAgentSession.SessionId,
                path = PlayAgentObserveHelper.ToProjectRelativePath(absolutePath),
                absolute_path = absolutePath,
                is_playing = EditorApplication.isPlaying
            });
        }

        private static object HandleWait(Parameters parameters)
        {
            var frames = parameters.wait_frames ?? 30;
            var timeoutMs = parameters.timeout_ms ?? Math.Max(1000, frames * 100);
            PlayAgentEditorWait.WaitFrames(frames, timeoutMs);
            return new SuccessResponse($"Waited {frames} editor frames (timeout {timeoutMs}ms).", new
            {
                waited_frames = frames,
                timeout_ms = timeoutMs,
                is_playing = EditorApplication.isPlaying
            });
        }

        private static object HandleGetCommandLog()
        {
            var entries = PlayAgentSession.Log.Select(e => new
            {
                step = e.Step,
                utc_time = e.UtcTime.ToString("o"),
                command = e.CommandName,
                args = e.ArgsJson,
                success = e.Success,
                error = e.Error,
                result = e.Result,
                elapsed_ms = e.ElapsedMilliseconds,
                observe_before = e.ObservePathBefore,
                observe_after = e.ObservePathAfter
            });

            return new SuccessResponse("Command log retrieved.", new
            {
                session_id = PlayAgentSession.SessionId,
                entries
            });
        }

        private static object HandleReset()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            PlayAgentSession.ResetSession();
            return new SuccessResponse("Play Agent session reset.", new
            {
                session_id = PlayAgentSession.SessionId,
                is_playing = EditorApplication.isPlaying
            });
        }

        private static object HandleGetRuntimeSnapshot()
        {
            if (!PlayAgentBootstrap.TryGetReadyArchitecture(out var architecture, out var archError))
            {
                return new SuccessResponse("Architecture not ready.", new
                {
                    architecture_ready = false,
                    error = archError,
                    is_playing = EditorApplication.isPlaying,
                    active_scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                });
            }

            object snapshot = null;
            var provider = PlayAgentBootstrap.SnapshotProvider;
            if (provider != null)
            {
                try
                {
                    snapshot = provider.Capture(architecture);
                }
                catch (Exception ex)
                {
                    snapshot = new { error = ex.Message };
                }
            }

            return new SuccessResponse("Runtime snapshot captured.", new
            {
                architecture_ready = true,
                architecture_id = PlayAgentBootstrap.ArchitectureProvider?.ArchitectureId,
                is_playing = EditorApplication.isPlaying,
                active_scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                snapshot
            });
        }

        private static object HandleReadLogs(Parameters parameters)
        {
            var count = Math.Max(1, parameters.log_count ?? 20);
            var logs = new List<string>(count);

            try
            {
                var logEntries = GetEditorConsoleEntries();
                for (var i = Math.Max(0, logEntries.Count - count); i < logEntries.Count; i++)
                {
                    logs.Add(logEntries[i]);
                }
            }
            catch (Exception ex)
            {
                logs.Add($"[QfPlayAgent] Failed to read editor console: {ex.Message}");
            }

            return new SuccessResponse("Recent editor logs retrieved.", new
            {
                count = logs.Count,
                logs
            });
        }

        private static int ResolveDefaultWaitFrames(string commandName)
        {
            var descriptors = PlayAgentCommandCatalog.ListCommands(PlayAgentBootstrap.Catalog);
            for (var i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                if (string.Equals(descriptor.Name, commandName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(descriptor.FullTypeName, commandName, StringComparison.Ordinal))
                {
                    return descriptor.DefaultWaitFrames;
                }
            }

            return 1;
        }

        private static Dictionary<string, object> ConvertArgs(JObject argsObject)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (argsObject == null)
            {
                return result;
            }

            foreach (var property in argsObject.Properties())
            {
                result[property.Name] = ConvertToken(property.Value);
            }

            return result;
        }

        private static object ConvertToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Integer:
                    return token.Value<long>();
                case JTokenType.Float:
                    return token.Value<double>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Null:
                    return null;
                default:
                    if (token is JValue value)
                    {
                        return value.Value;
                    }

                    return token.ToString(Formatting.None);
            }
        }

        private static List<string> GetEditorConsoleEntries()
        {
            var entries = new List<string>();
            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
            var getCountMethod = logEntriesType?.GetMethod("GetCount");
            var getEntryInternalMethod = logEntriesType?.GetMethod(
                "GetEntryInternal",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            if (getCountMethod == null || getEntryInternalMethod == null)
            {
                return entries;
            }

            var count = (int)getCountMethod.Invoke(null, null);
            var entryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
            var entry = Activator.CreateInstance(entryType);

            for (var i = 0; i < count; i++)
            {
                getEntryInternalMethod.Invoke(null, new object[] { i, entry });
                var messageField = entryType.GetField("message");
                var modeField = entryType.GetField("mode");
                var message = messageField?.GetValue(entry)?.ToString() ?? string.Empty;
                var mode = modeField != null ? (int)modeField.GetValue(entry) : 0;
                var prefix = mode switch
                {
                    1 => "[Error] ",
                    2 => "[Warning] ",
                    _ => "[Log] "
                };
                entries.Add(prefix + message);
            }

            return entries;
        }
    }
}
