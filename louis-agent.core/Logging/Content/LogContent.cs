namespace LouisAgent.Core.Logging.Content;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging.Messages;
using LouisAgent.Core.Messages;

/// <summary>
/// What a log line shows of a value it was given (F18 step 4f; GOAL skill 17, with skill 15's practice). A user's words reach a log
/// through any argument, because the model copies them into a search query or a sub-session's task, so no list of "personal" fields
/// would be complete: by default every value becomes <c>#</c>, the first 12 hex digits of its HMAC-SHA256, and its length in brackets
/// (<c>#3f9c0a1b2d4e (10)</c>). The key is random, made when the process starts and never written down, so equal values in one process
/// still match ("the same town twice") while nobody can reverse a hash by hashing a list of towns, as they could a plain SHA-256.
/// <see cref="LogContentMode.Full"/> (<c>LOG_CONTENT=full</c>) writes the values, for a developer's own machine. A null is written
/// <c>null</c> in both. A static class of functions with one process-wide key and mode (ADR 0040); the mode is set by
/// <see cref="AgentLogging.Configure"/>, and the key survives every call to it, so lines before and after a second configure hash alike.
/// </summary>
public static class LogContent
{
    private const int HashDigits = 12;
    private static readonly byte[] Key = RandomNumberGenerator.GetBytes(32);
    private static volatile LogContentMode _mode = LogContentMode.Hashed;

    internal static void Use(LogContentMode mode) => _mode = mode;

    /// <summary>LOG_CONTENT, parsed once where it enters: unset or blank is <see cref="LogContentMode.Hashed"/>; anything unknown throws.</summary>
    public static LogContentMode ModeOf(string? setting)
    {
        if (!SettingChoice.TryParse(setting, out LogContentMode? mode))
            throw new ArgumentException(MessageText.Format(LoggingErrorMessage.UnknownLogContent,
                new { Value = setting?.Trim(), Supported = string.Join(" or ", SettingChoice.Names<LogContentMode>()) }));
        return mode ?? LogContentMode.Hashed;
    }

    /// <summary>One value as a log line shows it; <paramref name="cut"/> shortens it with <c>...</c> where the mode is <c>Full</c>, as the lines always did.</summary>
    public static string Of(string? value, int? cut = null) => value switch
    {
        null => "null",
        _ when _mode == LogContentMode.Hashed => Hash(value, Key),
        _ => cut is { } limit && value.Length > limit ? value[..limit] + "..." : value,
    };

    /// <summary>
    /// A tool call's arguments: <c>Full</c> is their JSON, byte for byte as before; <c>Hashed</c> is <c>name=#… (n)</c> for each, in order,
    /// a string hashed as its text and anything else as its compact JSON. The names stay: they say what was asked, not what about.
    /// </summary>
    public static string OfArguments(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || _mode == LogContentMode.Full) return JsonSerializer.Serialize(arguments);
        if (arguments.Count == 0) return "{}";

        return string.Join(", ", arguments.Select(a => $"{a.Key}={(a.Value is null ? "null" : Hash(a.Value as string ?? JsonSerializer.Serialize(a.Value), Key))}"));
    }

    /// <summary>The pure part: the keyed hash and the length of <paramref name="value"/>, so a test can show that two keys hash one value differently.</summary>
    internal static string Hash(string value, byte[] key) =>
        $"#{Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value)))[..HashDigits]} ({value.Length})";
}
