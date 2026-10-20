namespace LouisAgent.Core.Agent.Turns;

using LouisAgent.Core.Usage.Records;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Tools;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Messages;
using System.Globalization;
using Microsoft.Extensions.Logging;

/// <summary>
/// The guards around each tool call in the loop: every call is logged; a call the model was still writing when it hit
/// the output limit is never run; an oversized result is summarised (with a separate, tool-less model call) before it
/// reaches the model; and the loop's final request keeps the tool list. <see cref="AgentEngine"/> puts them into its
/// client pipeline. The pure safety checks (paths, secret files, SSRF) are in <c>Guardrails/</c>.
/// </summary>
/// <param name="registry">The engine's tools, kept on the loop's final request.</param>
/// <param name="summaryClient">The client for summaries: the provider's, outside the tool loop, with its own usage record.</param>
/// <param name="subSessionPromptBudget">The most tokens a sub-session's prompt should reach (FX14); 0: no limit.</param>
internal sealed class ToolCallGuards(ToolRegistry registry, IChatClient summaryClient, int subSessionPromptBudget = 0)
{
    private static readonly ILogger Log = AgentLogging.For<ToolCallGuards>();
    private const string Tag = "TOOL";

    // Tool results above this are summarised before reaching the model; normal source files stay verbatim.
    internal const int MaxToolResultChars = 50_000;
    // Keeps the summarisation request itself well inside a 200k-token context, even for token-dense diffs.
    private const int MaxSummaryInputChars = 200_000;

    // Set on tool calls from a response that hit the output limit: their arguments are incomplete.
    private const string TruncatedCallKey = "louis.truncated";

    internal static string TruncatedCallMessage => MessageText.Format(ToolMessage.TruncatedCall);

    public async ValueTask<object?> InvokeAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        string arguments = JsonSerializer.Serialize(context.Arguments);
        Log.Log(LogLevel.Information, LogMessage.ToolCall, new { Tool = context.Function.Name, Arguments = arguments }, tag: Tag);

        if (context.CallContent.AdditionalProperties?.ContainsKey(TruncatedCallKey) == true)
        {
            // Running it would act on partial input, e.g. half a script or a file missing its content.
            Log.Log(LogLevel.Information, LogMessage.ToolCallSkipped, new { Tool = context.Function.Name, LlmClientFactory.MaxOutputTokens }, tag: Tag);
            ToolCallRecords.TruncatedToolCall(context.Function.Name, arguments, LlmClientFactory.MaxOutputTokens);
            return TruncatedCallMessage;
        }

        object? result;
        try
        {
            result = await context.Function.InvokeAsync(context.Arguments, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Log(LogLevel.Information, LogMessage.ToolCallFailed, new { Tool = context.Function.Name, ErrorType = ex.GetType().Name, Reason = ex.Message }, tag: Tag);
            throw;
        }

        string text = result?.ToString() ?? string.Empty;
        Log.Log(LogLevel.Information, LogMessage.ToolCallResult, new { Tool = context.Function.Name, Result = text.Length > 300 ? text[..300] + "..." : text }, tag: Tag);

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

    /// <summary>
    /// A sub-session's prompt budget (FX14). Every tool result stays in the prompt, and every later request pays for it
    /// again; past Haiku 5.5's long-prompt threshold every request costs five times as much. When the next request of a
    /// sub-session's turn is expected to pass the budget (the last prompt plus its last growth), it keeps its tools with
    /// <see cref="ChatToolMode.None"/>, as the loop's final request does: the specialist answers from what it has.
    /// </summary>
    /// <remarks>
    /// Only a sub-session's turns (their usage tags name a parent turn): the user's own sessions may need a big context,
    /// and nothing above a sub-session would notice the cost. Summary requests run outside the loop and aren't counted.
    /// </remarks>
    internal ChatOptions? KeepWithinPromptBudget(ChatOptions? options, UsageScope? scope)
    {
        if (subSessionPromptBudget <= 0 || scope?.Tags.Parent is null) return options;
        if (options?.Tools is not { Count: > 0 } || options.ToolMode == ChatToolMode.None) return options;

        // Summaries run outside the loop; a sub-session's roll-up carries the specialist's tokens, not this prompt's size.
        var prompts = scope.Records
            .Where(r => r.Purpose is not (UsagePurpose.Summary or UsagePurpose.SubSession))
            .Select(r => Prompt(r.Tokens)).ToList();
        if (prompts.Count == 0) return options;
        long last = prompts[^1];
        long next = last + (prompts.Count > 1 ? Math.Max(0, last - prompts[^2]) : 0);
        if (next < subSessionPromptBudget) return options;

        Log.Log(LogLevel.Information, LogMessage.SubSessionPromptBudgetReached, new
        {
            Last = last.ToString("N0", CultureInfo.InvariantCulture), Next = next.ToString("N0", CultureInfo.InvariantCulture),
            Budget = subSessionPromptBudget.ToString("N0", CultureInfo.InvariantCulture),
        }, tag: Tag);
        ChatOptions limited = options.Clone();
        limited.ToolMode = ChatToolMode.None;
        return limited;
    }

    private static long Prompt(UsageTokens tokens) => (tokens.Input ?? 0) + (tokens.CacheRead ?? 0) + (tokens.CacheWrite ?? 0);

    private ChatOptions? Shape(ChatOptions? options) => KeepWithinPromptBudget(KeepToolsOnFinalRequest(options), UsageScope.Current);

    public Task<ChatResponse> KeepToolsOnFinalRequestAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next, CancellationToken cancellationToken) =>
        next.GetResponseAsync(messages, Shape(options), cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> KeepToolsOnFinalRequestStreamingAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next, CancellationToken cancellationToken) =>
        next.GetStreamingResponseAsync(messages, Shape(options), cancellationToken);

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
        string note = MessageText.Format(ToolMessage.OversizedResultNote, new { Tool = toolName, Chars = output.Length.ToString("N0", CultureInfo.InvariantCulture) });
        string input = output.Length <= MaxSummaryInputChars
            ? output
            : output[..MaxSummaryInputChars] + MessageText.Format(ToolMessage.SummaryInputCut, new { Chars = (output.Length - MaxSummaryInputChars).ToString("N0", CultureInfo.InvariantCulture) });

        try
        {
            Log.Log(LogLevel.Information, LogMessage.ToolResultSummarising, new { Tool = toolName, Chars = output.Length.ToString("N0", CultureInfo.InvariantCulture) }, tag: Tag);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                    MessageText.Format(ToolMessage.SummarySystemPrompt)),
                new(ChatRole.User,
                    MessageText.Format(ToolMessage.SummaryRequest, new { Request = userRequest ?? "(unknown)", Tool = toolName, Arguments = arguments, Output = input })),
            };
            ChatResponse response;
            // Its own scope: recorded as "summary", numbered apart from the turn's rounds.
            using (UsageScope.Begin(UsagePurpose.Summary))
                response = await summaryClient.GetResponseAsync(
                    messages, new ChatOptions { MaxOutputTokens = LlmClientFactory.MaxOutputTokens }, cancellationToken);
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                string summary = MessageText.Format(ToolMessage.SummarisedResult, new { Note = note, Summary = response.Text });
                ToolCallRecords.OversizedToolResult(toolName, arguments, output.Length, userRequest, "summarised", summary.Length);
                return summary;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Log(LogLevel.Warning, LogMessage.SummaryFailed, new { Tool = toolName, Reason = ex.Message });
        }

        string truncated = $"{note}\n{ProcessRunner.Truncate(output, MaxToolResultChars)}";
        ToolCallRecords.OversizedToolResult(toolName, arguments, output.Length, userRequest, "truncated", truncated.Length);
        return truncated;
    }
}
