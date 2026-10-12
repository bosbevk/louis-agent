namespace LouisAgent.Core.Logging;

using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Records of tool calls the guards had to step in on, one JSON line each under the log directory
/// (<see cref="AgentLogging.RecordFiles"/>); without a log directory they go nowhere. Never on the console.
/// </summary>
public static class ToolCallRecords
{
    private static readonly ILogger Records = AgentLogging.LoggerFactory.CreateLogger("Records");

    /// <summary>One record in oversized-tool-results.jsonl, to spot tools that need narrower output.</summary>
    public static void OversizedToolResult(
        string tool, string arguments, int chars, string? userRequest, string outcome, int returnedChars)
    {
        using (Records.BeginScope(new Dictionary<string, object> { [AgentLogging.RecordProperty] = "oversized-tool-result" }))
            Records.Log(LogLevel.Information, LogMessage.OversizedToolResult, new
            {
                Tool = tool, Chars = chars, Outcome = outcome, ReturnedChars = returnedChars, Arguments = arguments,
                UserRequest = userRequest is { Length: > 500 } ? userRequest[..500] + "..." : userRequest,
            });
    }

    /// <summary>One record in truncated-tool-calls.jsonl: calls the model could not finish within the output limit.</summary>
    public static void TruncatedToolCall(string tool, string arguments, int maxOutputTokens)
    {
        using (Records.BeginScope(new Dictionary<string, object> { [AgentLogging.RecordProperty] = "truncated-tool-call" }))
            Records.Log(LogLevel.Information, LogMessage.TruncatedToolCall,
                new { Tool = tool, MaxOutputTokens = maxOutputTokens, Arguments = arguments });
    }
}