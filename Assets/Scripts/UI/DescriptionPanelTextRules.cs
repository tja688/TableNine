using System;
using System.Collections.Generic;

/// <summary>
/// DescriptionPanel 文案长度规则。换行符与空格均计入字数。
/// </summary>
public static class DescriptionPanelTextRules
{
    public const int MaxLength = 25;

    public static int CountLength(string text)
    {
        return string.IsNullOrEmpty(text) ? 0 : text.Length;
    }

    public static bool IsWithinLimit(string text)
    {
        return CountLength(text) <= MaxLength;
    }

    public static bool ContainsLineBreak(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0;
    }

    public static string RemoveLineBreaks(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("\r\n", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\r", string.Empty);
    }

    public static string Clamp(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= MaxLength)
        {
            return text ?? string.Empty;
        }

        return text.Substring(0, MaxLength);
    }

    public static string FormatDisplayLines(IReadOnlyList<string> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = Clamp(lines[i]?.Trim() ?? string.Empty);
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(line);
        }

        return builder.ToString();
    }

    public static bool TrySanitize(string text, out string sanitized, out bool wasTruncated, out bool hadLineBreak)
    {
        hadLineBreak = ContainsLineBreak(text);
        sanitized = Clamp(RemoveLineBreaks(text));
        wasTruncated = CountLength(RemoveLineBreaks(text)) > MaxLength;
        return !hadLineBreak && !wasTruncated;
    }

    public static string SanitizeForStorage(string text, out bool wasModified)
    {
        var original = text ?? string.Empty;
        var normalized = RemoveLineBreaks(original);
        var sanitized = Clamp(normalized);
        wasModified = ContainsLineBreak(original) || !string.Equals(sanitized, original, StringComparison.Ordinal);
        return sanitized;
    }

    public static string Format(string format, params object[] args)
    {
        if (string.IsNullOrEmpty(format))
        {
            return string.Empty;
        }

        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return format;
        }
    }
}
