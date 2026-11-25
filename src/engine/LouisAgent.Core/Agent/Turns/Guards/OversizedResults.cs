using System.Globalization;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Logging.Content;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Tools;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Agent.Messages.Holes.Oversized;
using LouisAgent.Core.Agent.Messages.Holes.Summaries;
using LouisAgent.Core.Llm.Providers;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Agent.Turns.Guards;

/// <summary>
/// A tool result too big to send: condensed by a separate, tool-less model call so the chat's context isn't blown,
/// or cut short when that call fails. Both are recorded, so an oversized result can be found again.
/// </summary>
/// <remarks>
/// Extract Class (Fowler), out of <see cref="ToolCallGuards"/>: the summary is its own job, with its own client, limits
/// and records, and the guards only decide when to ask for one.
/// </remarks>
/// <param name="summaryClient">
/// The client for summaries: the provider's, outside the tool loop, with its own usage record and its own trace span.
/// </param>
internal sealed class OversizedResults(IChatClient summaryClient)
{
    /// <summary>The summary's own lines: a summary starting, and one that failed.</summary>
    private static readonly ILogger Log = AgentLogging.For<OversizedResults>();

    /// <summary>The label the summary's log lines carry, as the tool calls' do.</summary>
    private const string Tag = "TOOL";

    /// <summary>
    /// Tool results above this are summarised before reaching the model; normal source files stay verbatim.
    /// </summary>
    internal const int MaxToolResultChars = 50_000;

    /// <summary>
    /// Keeps the summary request itself well inside a 200k-token context, even for token-dense diffs.
    /// </summary>
    private const int MaxSummaryInputChars = 200_000;

    /// <summary>
    /// Condenses an oversized tool result with a separate, tool-less model call; falls back to cutting it short if that
    /// fails.
    /// </summary>
    internal async Task<string> SummariseAsync(
        string toolName, string arguments, string output, string? userRequest,
        CancellationToken cancellationToken = default)
    {
        string chars = output.Length.ToString("N0", CultureInfo.InvariantCulture);
        var size = new OversizedResultNoteHoles(toolName, chars);
        string note = MessageText.Format(AgentToolMessage.OversizedResultNote, size);
        string input = output.Length <= MaxSummaryInputChars ? output : Cut(output);

        try
        {
            var summarising = new ToolResultSummarisingHoles(toolName, chars);
            Log.Log(LogLevel.Information, AgentLogMessage.ToolResultSummarising, summarising, tag: Tag);
            var asked = new SummaryRequestHoles(userRequest ?? "(unknown)", toolName, arguments, input);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, MessageText.Format(AgentToolMessage.SummarySystemPrompt)),
                new(ChatRole.User, MessageText.Format(AgentToolMessage.SummaryRequest, asked)),
            };
            ChatResponse response;
            // Its own scope: recorded as "summary", numbered apart from the turn's rounds.
            using (UsageScope.Begin(UsagePurpose.Summary))
                response = await summaryClient.GetResponseAsync(
                    messages, new ChatOptions { MaxOutputTokens = LlmLimits.MaxOutputTokens }, cancellationToken);
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                var summarised = new SummarisedResultHoles(note, response.Text);
                string summary = MessageText.Format(AgentToolMessage.SummarisedResult, summarised);
                ToolCallRecords.OversizedToolResult(toolName, arguments, output.Length, userRequest, "summarised",
                    summary.Length);
                return summary;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var failed = new SummaryFailedHoles(toolName, LogContent.Of(ex.Message));
            Log.Log(LogLevel.Warning, AgentLogMessage.SummaryFailed, failed);
        }

        string truncated = $"{note}\n{ProcessRunner.Truncate(output, MaxToolResultChars)}";
        ToolCallRecords.OversizedToolResult(toolName, arguments, output.Length, userRequest, "truncated",
            truncated.Length);
        return truncated;
    }

    /// <summary>The start of an output too big even to summarise, and how much of it the summary won't see.</summary>
    private static string Cut(string output)
    {
        string unseen = (output.Length - MaxSummaryInputChars).ToString("N0", CultureInfo.InvariantCulture);
        var cut = new SummaryInputCutHoles(unseen);
        return output[..MaxSummaryInputChars] + MessageText.Format(AgentToolMessage.SummaryInputCut, cut);
    }
}
