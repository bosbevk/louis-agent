namespace LouisAgent.Core.Logging;

using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>Logging a <see cref="LogMessage"/>: its template is the message template, so the logger keeps each hole as a field.</summary>
public static class MessageLogging
{
    /// <summary>
    /// Logs <paramref name="message"/> with the values of <paramref name="values"/>' properties named like its holes, in the
    /// template's order, so a name can't drift from its argument: <c>logger.Log(LogLevel.Warning, LogMessage.X, new { Url })</c>.
    /// A hole without a value throws, as in <see cref="MessageText.Format"/>. <paramref name="tag"/> replaces the level in
    /// the console's brackets (<c>[MCP]</c>, <c>[TOOL]</c>).
    /// </summary>
    public static void Log(this ILogger logger, LogLevel level, LogMessage message, object? values = null, Exception? exception = null,
        string? tag = null)
    {
        string template = MessageText.Template(message);
        object?[] arguments = MessageText.Holes(template)
            .Select(hole => values?.GetType().GetProperty(hole) is { } property
                ? property.GetValue(values)
                : throw new ArgumentException(MessageText.Format(ErrorMessage.MessageNeedsValue,
                    new { Message = $"{nameof(LogMessage)}.{message}", Hole = $"{{{hole}}}" }), nameof(values)))
            .ToArray();
        if (tag is null)
        {
            logger.Log(level, exception, template, arguments);
            return;
        }

        using (logger.BeginScope(new Dictionary<string, object> { [AgentLogging.TagProperty] = tag }))
            logger.Log(level, exception, template, arguments);
    }
}