namespace LouisAgent.Core.Agent;

using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Tools;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;

/// <summary>
/// The guards around each tool call in the loop: every call is logged; a call the model was still writing when it hit
/// the output limit is never run; an oversized result is summarised (with a separate, tool-less model call) before it
/// reaches the model; and the loop's final request keeps the tool list. <see cref="AgentEngine"/> puts them into its
/// client pipeline. The pure safety checks (paths, secret files, SSRF) are in <c>Guardrails/</c>.
/// </summary>
/// <param name="registry">The engine's tools, kept on the loop's final request.</param>
/// <param name="summaryClient">The client for summaries: the provider's, outside the tool loop, with its own usage record.</param>
internal sealed class ToolCallGuards(ToolRegistry registry, IChatClient summaryClient)
{
    // Tool results above this are summarised before reaching the model; normal source files stay verbatim.
    internal const int MaxToolResultChars = 50_000;
    // Keeps the summarisation request itself well inside a 200k-token context, even for token-dense diffs.
    private const int MaxSummaryInputChars = 200_000;

    // Set on tool calls from a response that hit the output limit: their arguments are incomplete.
    private const string TruncatedCallKey = "louis.truncated";

    internal const string TruncatedCallMessage =
        "Error: this tool call was cut off by the output limit before its arguments were complete, so it was NOT run. " +
        "Retrying the same call will fail the same way. Split large content into chunks of at most ~300 lines: " +
        "WriteWorkspaceFile with the first chunk, then AppendToFile for each following chunk, one call at a time, until done.";

    public async ValueTask<object?> InvokeAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        string arguments = JsonSerializer.Serialize(context.Arguments);
        Console.Error.WriteLine($"[TOOL] -> {context.Function.Name}({arguments})");

        if (context.CallContent.AdditionalProperties?.ContainsKey(TruncatedCallKey) == true)
        {
            // Running it would act on partial input, e.g. half a script or a file missing its content.
            Console.Error.WriteLine($"[TOOL] <- {context.Function.Name}: skipped, call was cut off by the output limit ({LlmClientFactory.MaxOutputTokens} tokens)");
            AgentLog.RecordTruncatedToolCall(context.Function.Name, arguments, LlmClientFactory.MaxOutputTokens);
            return TruncatedCallMessage;
        }

        object? result;
        try
        {
            result = await context.Function.InvokeAsync(context.Arguments, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"[TOOL] <- {context.Function.Name} failed: {ex.GetType().Name}: {ex.Message}");
            throw;
        }

        string text = result?.ToString() ?? string.Empty;
        Console.Error.WriteLine($"[TOOL] <- {context.Function.Name}: {(text.Length > 300 ? text[..300] + "..." : text)}");

        if (text.Length <= MaxToolResultChars) return result;

        // An oversized result (e.g. a huge diff) would blow the model's context window on the next request.
        string? request = context.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        return await SummariseToolResultAsync(context.Function.Name, arguments, text, request, cancellationToken);
    }

    /// <summary>
    /// Sits inside the tool-call loop. After <c>MaximumIterationsPerRequest</c> rounds, <see cref="FunctionInvokingChatClient"/>
    /// sends one last request with no tools, so the model has to answer in text. Models that bind their thinking blocks to
    /// the tool list (Claude Haiku 5.5 and later) reject that request with a 400, which loses the whole turn. This puts the
    /// tools back unchanged with <see cref="ChatToolMode.None"/> (Anthropic: <c>tool_choice: none</c>): the tool list
    /// matches every earlier request, and the model still can't call a tool.
    /// </summary>
    internal ChatOptions? KeepToolsOnFinalRequest(ChatOptions? options)
    {
        // Every request of a turn carries the tools (BeginTurn), so one without them is the loop's final request.
        if (registry.Count == 0 || options?.Tools is { Count: > 0 }) return options;

        ChatOptions final = options?.Clone() ?? new ChatOptions();
        final.Tools = registry.Live;
        final.ToolMode = ChatToolMode.None;
        return final;
    }

    public Task<ChatResponse> KeepToolsOnFinalRequestAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next, CancellationToken cancellationToken) =>
        next.GetResponseAsync(messages, KeepToolsOnFinalRequest(options), cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> KeepToolsOnFinalRequestStreamingAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next, CancellationToken cancellationToken) =>
        next.GetStreamingResponseAsync(messages, KeepToolsOnFinalRequest(options), cancellationToken);

    /// <summary>
    /// Sits inside the tool-call loop: when the model stopped at the output limit, any tool calls it was writing are
    /// incomplete, so they are flagged for <see cref="InvokeAsync"/> to reject instead of run.
    /// </summary>
    public static async Task<ChatResponse> MarkTruncatedToolCallsAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next, CancellationToken cancellationToken)
    {
        ChatResponse response = await next.GetResponseAsync(messages, options, cancellationToken);
        if (response.FinishReason != ChatFinishReason.Length) return response;

        foreach (var call in response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>())
            (call.AdditionalProperties ??= [])[TruncatedCallKey] = true;
        return response;
    }

    /// <summary>Streaming form of <see cref="MarkTruncatedToolCallsAsync"/>; the tool loop only runs calls once the stream ends.</summary>
    public static async IAsyncEnumerable<ChatResponseUpdate> MarkTruncatedToolCallsStreamingAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var calls = new List<FunctionCallContent>();
        await foreach (var update in next.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            calls.AddRange(update.Contents.OfType<FunctionCallContent>());
            if (update.FinishReason == ChatFinishReason.Length)
                foreach (var call in calls)
                    (call.AdditionalProperties ??= [])[TruncatedCallKey] = true;
            yield return update;
        }
    }

    /// <summary>
    /// Condenses an oversized tool result with a separate, tool-less model call; falls back to truncation if that fails.
    /// </summary>
    internal async Task<string> SummariseToolResultAsync(
        string toolName, string arguments, string output, string? userRequest, CancellationToken cancellationToken = default)
    {
        string note = $"[{toolName} returned {output.Length:N0} chars, too large to include verbatim. " +
                      "If exact content is needed, call it again with narrower arguments (e.g. a specific file path).]";
        string input = output.Length <= MaxSummaryInputChars
            ? output
            : output[..MaxSummaryInputChars] + $"\n... [{output.Length - MaxSummaryInputChars:N0} more chars not shown]";

        try
        {
            Console.Error.WriteLine($"[TOOL] summarising {toolName} output ({output.Length:N0} chars)");
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                    "You condense tool output for another AI agent that cannot see the original. Keep what matters for " +
                    "the user's request: file names, counts, errors, key values and the substance of changes. Be concise " +
                    "and factual. If the output looks mechanical (e.g. every line changed only by line endings or " +
                    "whitespace), say so plainly."),
                new(ChatRole.User,
                    $"User's request: {userRequest ?? "(unknown)"}\nTool: {toolName}({arguments})\n\nOutput:\n{input}"),
            };
            ChatResponse response;
            // Its own scope: recorded as "summary", numbered apart from the turn's rounds.
            using (UsageScope.Begin(UsagePurpose.Summary))
                response = await summaryClient.GetResponseAsync(
                    messages, new ChatOptions { MaxOutputTokens = LlmClientFactory.MaxOutputTokens }, cancellationToken);
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                string summary = $"{note}\nSummary:\n{response.Text}";
                AgentLog.RecordOversizedToolResult(toolName, arguments, output.Length, userRequest, "summarised", summary.Length);
                return summary;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"[WARN] Could not summarise {toolName} output: {ex.Message}");
        }

        string truncated = $"{note}\n{ProcessRunner.Truncate(output, MaxToolResultChars)}";
        AgentLog.RecordOversizedToolResult(toolName, arguments, output.Length, userRequest, "truncated", truncated.Length);
        return truncated;
    }
}
