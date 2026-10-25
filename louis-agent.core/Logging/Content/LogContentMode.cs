namespace LouisAgent.Core.Logging.Content;

/// <summary>What a log line shows of the values it was given (F18 step 4f, <c>LOG_CONTENT</c>): parsed once with <c>SettingChoice</c> (ADR 0024).</summary>
public enum LogContentMode
{
    /// <summary>A keyed hash and the length of each value: what ran, in what order, how long, how it ended; never what it was given.</summary>
    Hashed,

    /// <summary>The values themselves, for a developer's own machine and never a hosted instance.</summary>
    Full,
}
