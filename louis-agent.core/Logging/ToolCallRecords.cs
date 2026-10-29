
using LouisAgent.Core.Logging.Content;
using LouisAgent.Core.Logging.Messages;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Logging;
/// <summary>
/// Records of tool calls the guards had to step in on, one JSON line each under the log directory
/// (<see cref="AgentLogging.RecordFiles"/>); without a log directory they go nowhere. Never on the console. The tool, the sizes and the
/// outcome stay in clear (what the records are for); the arguments and the user's request are hashes and lengths unless <c>LOG_CONTENT=full</c> (F18 step 4f).
/// </summary>
public static class ToolCallRecords
{
    private static readonly ILogger Records = AgentLogging.LoggerFactory.CreateLogger("Records");

    /// <summary>One record in oversized-tool-results.jsonl, to spot tools that need narrower output.</summary>
    public static void OversizedToolResult(
        string tool, string arguments, int chars, string? userRequest, string outcome, int returnedChars)
    {
        using (Records.BeginScope(new Dictionary<string, object>
        { [AgentLogging.RecordProperty] = "oversized-tool-result" }))
            Records.Log(LogLevel.Information, LoggingLogMessage.OversizedToolResult, new
            {
                Tool = tool,
                Chars = chars,
                Outcome = outcome,
                ReturnedChars = returnedChars,
                Arguments = LogContent.Of(arguments),
                // A missing request stays a JSON null; a present one is a hash and a length, or (full) its first 500 characters.
                UserRequest = userRequest is null ? null : LogContent.Of(userRequest, cut: 500),
            });
    }

    /// <summary>One record in truncated-tool-calls.jsonl: calls the model could not finish within the output limit.</summary>
    public static void TruncatedToolCall(string tool, string arguments, int maxOutputTokens)
    {
        using (Records.BeginScope(new Dictionary<string, object>
        { [AgentLogging.RecordProperty] = "truncated-tool-call" }))
            Records.Log(LogLevel.Information, LoggingLogMessage.TruncatedToolCall,
                new { Tool = tool, MaxOutputTokens = maxOutputTokens, Arguments = LogContent.Of(arguments) });
    }
}
