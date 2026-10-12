namespace LouisAgent.Core.Config;

using System.Text.RegularExpressions;
using LouisAgent.Core.Messages;

/// <summary>
/// Writes one setting into <c>config/.env</c>, the file every host and container reads (F14: <c>HOST_CULTURE</c>, written
/// once from the PC by <c>usage-probe --host-culture</c>). Only that setting's line changes: the rest of the file, its
/// comments and its line endings stay as they were.
/// </summary>
public static partial class EnvFile
{
    /// <summary><c>config/.env</c>, searched for upward from <paramref name="startDirectory"/> like the hosts do; null if none.</summary>
    public static string? Find(string startDirectory) =>
        FindConfigDirectory(startDirectory) is { } dir && File.Exists(Path.Combine(dir, ".env"))
            ? Path.Combine(dir, ".env")
            : null;

    /// <summary>The nearest <c>config/</c> folder with a <c>.env</c> or <c>.env.secrets</c>, searched upward; null if none.</summary>
    internal static string? FindConfigDirectory(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "config");
            if (File.Exists(Path.Combine(candidate, ".env")) || File.Exists(Path.Combine(candidate, ".env.secrets")))
                return candidate;
        }

        return null;
    }

    /// <summary>Sets <paramref name="key"/> to <paramref name="value"/> in the file at <paramref name="path"/>.</summary>
    /// <returns>True when an existing line was replaced, false when the setting was added.</returns>
    public static bool Write(string path, string key, string value, string comment)
    {
        string content = File.ReadAllText(path);
        string updated = SetValue(content, key, value, comment, out bool replaced);
        File.WriteAllText(path, updated);
        return replaced;
    }

    /// <summary>
    /// <paramref name="content"/> with <c>KEY=value</c>: the first uncommented <c>KEY=</c> line is replaced; otherwise the
    /// setting is appended after a blank line and <paramref name="comment"/> (one <c>#</c> line per line of it). A
    /// commented-out <c># KEY=…</c> example is left alone. The file's own line ending (CRLF or LF) is kept.
    /// </summary>
    public static string SetValue(string content, string key, string value, string comment, out bool replaced)
    {
        if (!SettingName().IsMatch(key)) throw new ArgumentException(MessageText.Format(ErrorMessage.NotASettingName, new { Key = key }), nameof(key));
        if (value.Contains('\n') || value.Contains('\r')) throw new ArgumentException(MessageText.Format(ErrorMessage.SettingValueNotOneLine), nameof(value));

        string newline = content.Contains("\r\n") ? "\r\n" : "\n";
        string setting = $"{key}={value}";
        var isSetting = new Regex($@"^[ \t]*{Regex.Escape(key)}[ \t]*=");

        // Line by line, each keeping its own ending, so only that line changes (not a comment that repeats its text).
        string[] lines = Regex.Split(content, "(?<=\n)");
        for (int i = 0; i < lines.Length; i++)
        {
            string text = lines[i].TrimEnd('\r', '\n');
            if (!isSetting.IsMatch(text)) continue;

            lines[i] = setting + lines[i][text.Length..];
            replaced = true;
            return string.Concat(lines);
        }

        replaced = false;
        string start = content.Length == 0 || content.EndsWith('\n') ? content : content + newline;
        string comments = string.Concat(comment.Split('\n').Select(c => $"# {c.TrimEnd('\r')}{newline}"));
        return $"{start}{newline}{comments}{setting}{newline}";
    }

    [GeneratedRegex("^[A-Z][A-Z0-9_]*$")]
    private static partial Regex SettingName();
}