using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using QFramework;

namespace QfPlayAgent
{
    public static class PlayAgentCommandCatalog
    {
        public static IReadOnlyList<PlayAgentCommandDescriptor> ListCommands(
            PlayAgentCatalogConfig catalog = null,
            QfAiRisk? riskFilter = null)
        {
            catalog ??= PlayAgentBootstrap.Catalog;
            var allowlist = BuildAllowlist(catalog);
            var assemblyFilter = catalog?.AssemblyNames;
            var descriptors = new List<PlayAgentCommandDescriptor>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!ShouldScanAssembly(assembly, assemblyFilter))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                for (var i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    if (!IsCommandType(type))
                    {
                        continue;
                    }

                    if (!ShouldExpose(type, catalog, allowlist, out var entry, out var aiAction))
                    {
                        continue;
                    }

                    var risk = entry?.Risk ?? aiAction?.Risk ?? QfAiRisk.PlayerInput;
                    if (riskFilter.HasValue && risk != riskFilter.Value)
                    {
                        continue;
                    }

                    descriptors.Add(BuildDescriptor(type, entry, aiAction, risk));
                }
            }

            descriptors.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return descriptors;
        }

        public static bool TryResolveCommandType(string commandName, out Type commandType)
        {
            commandType = null;
            if (string.IsNullOrWhiteSpace(commandName))
            {
                return false;
            }

            var catalog = PlayAgentBootstrap.Catalog;
            var descriptors = ListCommands(catalog);
            for (var i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                if (string.Equals(descriptor.Name, commandName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(descriptor.FullTypeName, commandName, StringComparison.Ordinal))
                {
                    commandType = Type.GetType(descriptor.FullTypeName);
                    if (commandType == null)
                    {
                        commandType = FindTypeByName(descriptor.FullTypeName) ?? FindTypeByName(descriptor.Name);
                    }

                    return commandType != null;
                }
            }

            commandType = FindTypeByName(commandName);
            return commandType != null && IsCommandType(commandType);
        }

        private static Dictionary<string, PlayAgentCommandEntry> BuildAllowlist(PlayAgentCatalogConfig catalog)
        {
            var map = new Dictionary<string, PlayAgentCommandEntry>(StringComparer.OrdinalIgnoreCase);
            if (catalog?.Commands == null)
            {
                return map;
            }

            for (var i = 0; i < catalog.Commands.Count; i++)
            {
                var entry = catalog.Commands[i];
                if (entry == null || !entry.Enabled || string.IsNullOrWhiteSpace(entry.CommandTypeName))
                {
                    continue;
                }

                map[entry.CommandTypeName] = entry;
            }

            return map;
        }

        private static bool ShouldScanAssembly(Assembly assembly, List<string> assemblyFilter)
        {
            if (assemblyFilter == null || assemblyFilter.Count == 0)
            {
                return true;
            }

            var assemblyName = assembly.GetName().Name;
            for (var i = 0; i < assemblyFilter.Count; i++)
            {
                if (string.Equals(assemblyFilter[i], assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCommandType(Type type)
        {
            if (type == null || type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
            {
                return false;
            }

            return typeof(ICommand).IsAssignableFrom(type) ||
                   ImplementsGenericCommand(type);
        }

        private static bool ImplementsGenericCommand(Type type)
        {
            var interfaces = type.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].IsGenericType &&
                    interfaces[i].GetGenericTypeDefinition() == typeof(ICommand<>))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldExpose(
            Type type,
            PlayAgentCatalogConfig catalog,
            Dictionary<string, PlayAgentCommandEntry> allowlist,
            out PlayAgentCommandEntry entry,
            out QfAiActionAttribute aiAction)
        {
            entry = null;
            aiAction = type.GetCustomAttribute<QfAiActionAttribute>();
            if (!allowlist.TryGetValue(type.FullName, out entry))
            {
                allowlist.TryGetValue(type.Name, out entry);
            }

            if (aiAction != null && !aiAction.ExposeToAgent)
            {
                return false;
            }

            if (aiAction != null && aiAction.ExposeToAgent)
            {
                return true;
            }

            if (entry != null && entry.Enabled)
            {
                return true;
            }

            if (catalog != null && catalog.RequireExplicitAllowlist)
            {
                return false;
            }

            return aiAction != null;
        }

        private static PlayAgentCommandDescriptor BuildDescriptor(
            Type type,
            PlayAgentCommandEntry entry,
            QfAiActionAttribute aiAction,
            QfAiRisk risk)
        {
            var descriptor = new PlayAgentCommandDescriptor
            {
                Name = entry?.DisplayName
                       ?? aiAction?.Name
                       ?? type.Name,
                FullTypeName = type.FullName,
                Description = entry?.Description ?? aiAction?.Description ?? string.Empty,
                Risk = risk,
                DefaultWaitFrames = entry?.DefaultWaitFrames
                                    ?? aiAction?.DefaultWaitFrames
                                    ?? 1
            };

            AppendConstructorArgs(type, descriptor);
            AppendMemberArgs(type, descriptor);
            return descriptor;
        }

        private static void AppendConstructorArgs(Type type, PlayAgentCommandDescriptor descriptor)
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (ctors.Length == 0)
            {
                return;
            }

            var ctor = ctors
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            var parameters = ctor.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                AddArgIfMissing(descriptor, parameters[i].Name, parameters[i].ParameterType, parameters[i].GetCustomAttribute<QfArgAttribute>());
            }
        }

        private static void AppendMemberArgs(Type type, PlayAgentCommandDescriptor descriptor)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            var properties = type.GetProperties(flags);
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (!property.CanWrite)
                {
                    continue;
                }

                AddArgIfMissing(descriptor, property.Name, property.PropertyType, property.GetCustomAttribute<QfArgAttribute>());
            }

            var fields = type.GetFields(flags);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.IsInitOnly)
                {
                    continue;
                }

                AddArgIfMissing(descriptor, field.Name, field.FieldType, field.GetCustomAttribute<QfArgAttribute>());
            }
        }

        private static void AddArgIfMissing(
            PlayAgentCommandDescriptor descriptor,
            string name,
            Type argType,
            QfArgAttribute argAttribute)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            for (var i = 0; i < descriptor.Args.Count; i++)
            {
                if (string.Equals(descriptor.Args[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            descriptor.Args.Add(new PlayAgentArgDescriptor
            {
                Name = name,
                TypeName = DescribeType(argType),
                Description = argAttribute?.Description ?? string.Empty,
                Required = argAttribute?.Required ?? !IsNullableOrOptional(argType),
                DefaultValue = HasDefault(argType) ? GetDefaultValueString(argType) : string.Empty
            });
        }

        private static bool IsNullableOrOptional(Type type)
        {
            if (!type.IsValueType)
            {
                return true;
            }

            return Nullable.GetUnderlyingType(type) != null;
        }

        private static bool HasDefault(Type type)
        {
            return type.IsValueType && Nullable.GetUnderlyingType(type) == null;
        }

        private static string GetDefaultValueString(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying == typeof(bool))
            {
                return "false";
            }

            if (underlying.IsEnum)
            {
                return Enum.GetNames(underlying).FirstOrDefault() ?? string.Empty;
            }

            return "0";
        }

        private static string DescribeType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying.IsEnum)
            {
                return string.Join("|", Enum.GetNames(underlying));
            }

            return underlying.Name + (Nullable.GetUnderlyingType(type) != null ? "?" : string.Empty);
        }

        private static Type FindTypeByName(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                for (var i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    if (string.Equals(type.Name, name, StringComparison.Ordinal) ||
                        string.Equals(type.FullName, name, StringComparison.Ordinal))
                    {
                        return type;
                    }
                }
            }

            return null;
        }
    }
}
