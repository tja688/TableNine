using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using QFramework;

namespace QfPlayAgent
{
    public sealed class PlayAgentInvokeResult
    {
        public bool Success { get; set; }
        public string CommandName { get; set; }
        public string Error { get; set; }
        public string Result { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }

    public static class PlayAgentInvoker
    {
        public static PlayAgentInvokeResult Invoke(
            IArchitecture architecture,
            string commandName,
            IReadOnlyDictionary<string, object> args)
        {
            var result = new PlayAgentInvokeResult
            {
                CommandName = commandName
            };

            if (architecture == null)
            {
                result.Error = "Architecture is null.";
                return result;
            }

            if (!PlayAgentCommandCatalog.TryResolveCommandType(commandName, out var commandType))
            {
                result.Error = $"Command '{commandName}' was not found or is not exposed.";
                return result;
            }

            if (!PlayAgentCommandFactory.TryCreate(commandType, args, out var command, out var createError))
            {
                result.Error = createError;
                return result;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (TryInvokeWithResult(architecture, command, commandType, out var returnValue, out var invokeError))
                {
                    result.Success = true;
                    result.Result = returnValue;
                }
                else
                {
                    architecture.SendCommand((ICommand)command);
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        private static bool TryInvokeWithResult(
            IArchitecture architecture,
            object command,
            Type commandType,
            out string returnValue,
            out string error)
        {
            returnValue = null;
            error = null;

            var resultInterface = FindGenericCommandInterface(commandType);
            if (resultInterface == null)
            {
                return false;
            }

            var sendMethod = typeof(IArchitecture).GetMethod(nameof(IArchitecture.SendCommand), new[] { resultInterface });
            if (sendMethod == null)
            {
                return false;
            }

            var rawResult = sendMethod.Invoke(architecture, new[] { command });
            returnValue = rawResult?.ToString() ?? string.Empty;
            return true;
        }

        private static Type FindGenericCommandInterface(Type commandType)
        {
            var interfaces = commandType.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].IsGenericType &&
                    interfaces[i].GetGenericTypeDefinition() == typeof(ICommand<>))
                {
                    return interfaces[i];
                }
            }

            return null;
        }
    }
}
