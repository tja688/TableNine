using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace QfPlayAgent
{
    public static class PlayAgentTypeCoercion
    {
        public static bool TryConvert(object rawValue, Type targetType, out object converted, out string error)
        {
            converted = null;
            error = null;

            if (targetType == null)
            {
                error = "Target type is null.";
                return false;
            }

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (rawValue == null)
            {
                if (underlying.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    error = $"Cannot assign null to {targetType.Name}.";
                    return false;
                }

                converted = null;
                return true;
            }

            if (underlying.IsInstanceOfType(rawValue))
            {
                converted = rawValue;
                return true;
            }

            if (rawValue is string text)
            {
                if (underlying == typeof(string))
                {
                    converted = text;
                    return true;
                }

                if (underlying.IsEnum)
                {
                    try
                    {
                        converted = Enum.Parse(underlying, text, true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                        return false;
                    }
                }

                if (underlying == typeof(bool) && bool.TryParse(text, out var boolValue))
                {
                    converted = boolValue;
                    return true;
                }

                if (underlying == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                {
                    converted = intValue;
                    return true;
                }

                if (underlying == typeof(float) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                {
                    converted = floatValue;
                    return true;
                }

                if (underlying == typeof(double) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    converted = doubleValue;
                    return true;
                }
            }

            try
            {
                converted = Convert.ChangeType(rawValue, underlying, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }

    public static class PlayAgentCommandFactory
    {
        public static bool TryCreate(
            Type commandType,
            IReadOnlyDictionary<string, object> args,
            out object command,
            out string error)
        {
            command = null;
            error = null;
            args ??= new Dictionary<string, object>();

            var ctor = SelectConstructor(commandType);
            if (ctor == null)
            {
                error = $"No public constructor found for {commandType.Name}.";
                return false;
            }

            var ctorParams = ctor.GetParameters();
            var ctorArgs = new object[ctorParams.Length];
            for (var i = 0; i < ctorParams.Length; i++)
            {
                var parameter = ctorParams[i];
                if (!TryResolveArg(args, parameter.Name, parameter.ParameterType, out ctorArgs[i], out error))
                {
                    return false;
                }
            }

            try
            {
                command = ctor.Invoke(ctorArgs);
            }
            catch (Exception ex)
            {
                error = ex.InnerException?.Message ?? ex.Message;
                return false;
            }

            if (!ApplyMembers(command, commandType, args, ctorParams.Select(p => p.Name).ToArray(), out error))
            {
                return false;
            }

            return true;
        }

        private static ConstructorInfo SelectConstructor(Type commandType)
        {
            var ctors = commandType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (ctors.Length == 0)
            {
                return null;
            }

            return ctors.OrderByDescending(c => c.GetParameters().Length).First();
        }

        private static bool ApplyMembers(
            object command,
            Type commandType,
            IReadOnlyDictionary<string, object> args,
            string[] ctorParamNames,
            out string error)
        {
            error = null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            var properties = commandType.GetProperties(flags);
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (!property.CanWrite || ctorParamNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryResolveArg(args, property.Name, property.PropertyType, out var value, out error))
                {
                    return false;
                }

                if (!args.ContainsKey(property.Name) &&
                    !args.Keys.Any(k => string.Equals(k, property.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                property.SetValue(command, value);
            }

            var fields = commandType.GetFields(flags);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.IsInitOnly || ctorParamNames.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryResolveArg(args, field.Name, field.FieldType, out var value, out error))
                {
                    return false;
                }

                if (!args.ContainsKey(field.Name) &&
                    !args.Keys.Any(k => string.Equals(k, field.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                field.SetValue(command, value);
            }

            return true;
        }

        private static bool TryResolveArg(
            IReadOnlyDictionary<string, object> args,
            string name,
            Type targetType,
            out object value,
            out string error)
        {
            value = null;
            error = null;

            object raw = null;
            var found = false;
            foreach (var pair in args)
            {
                if (!string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                raw = pair.Value;
                found = true;
                break;
            }

            if (!found)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    value = Activator.CreateInstance(targetType);
                }

                return true;
            }

            return PlayAgentTypeCoercion.TryConvert(raw, targetType, out value, out error);
        }
    }
}
