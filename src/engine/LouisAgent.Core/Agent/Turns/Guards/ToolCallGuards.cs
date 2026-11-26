using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Logging.Content;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Agent.Messages.Holes.Calls;
using LouisAgent.Core.Agent.Messages.Holes.Summaries;
using LouisAgent.Core.Agent.Tools;
using LouisAgent.Core.Llm.Providers;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Agent.Turns.Guards;

/// <summary>
/// The guards around each tool call in the loop: every call is logged; a call the model was still writing when it hit
/// the output limit is never run; an oversized result is summarised by <see cref="OversizedResults"/> before it reaches
/// the model; and the loop's final request keeps the tool list. <see cref="AgentEngine"/> puts them into its client
/// pipeline. The pure safety checks (paths, secret files, SSRF) are in <c>Guardrails/</c>.
/// </summary>
/// <param name="registry">The engine's tools, kept on the loop's final request.</param>
/// <param name="summaryClient">
/// The client for summaries: the provider's, outside the tool loop, with its own usage record and its own trace span.
/// </param>
/// <param name="subSessionPromptBudget">
/// The most tokens a sub-session's prompt should reach (FX14); 0: no limit.
/// </param>
internal sealed class ToolCallGuards(ToolRegistry registry, IChatClient summaryClient, int subSessionPromptBudget = 0)
{
    /// <summary>The guards' own lines: each call, its result, a skipped or failed call, a budget reached.</summary>
    private static readonly ILogger Log = AgentLogging.For<ToolCallGuards>();

    /// <summary>The label the tool lines carry, which people watch in Rider and the logs.</summary>
    private const string Tag = "TOOL";

    /// <summary>How much of a tool's result its log line shows.</summary>
    private const int LoggedResultChars = 300;

    /// <summary>Set on tool calls from a response that hit the output limit: their arguments are incomplete.</summary>
    private const string TruncatedCallKey = "louis.truncated";

    /// <summary>What the model is told of a call cut off by the output limit.</summary>
    internal static string TruncatedCallMessage => MessageText.Format(AgentToolMessage.TruncatedCall);

    /// <summary>What condenses a result too big to send.</summary>
    internal OversizedResults Oversized { get; } = new(summaryClient);

    /// <summary>
    /// Runs one tool call the loop asks for: logs it, refuses a cut-off call or one missing an argument, ends the turn
    /// when the tool says so, and hands an oversized result to the summary.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        string tool = context.Function.Name;
        string arguments = JsonSerializer.Serialize(context.Arguments);
        var call = new ToolCallHoles(tool, LogContent.OfArguments(context.Arguments));
        Log.Log(LogLevel.Information, AgentLogMessage.ToolCall, call, tag: Tag);

        if (context.CallContent.AdditionalProperties?.ContainsKey(TruncatedCallKey) == true)
        {
            // Running it would act on partial input, e.g. half a script or a file missing its content.
            var skipped = new ToolCallSkippedHoles(tool, LlmLimits.MaxOutputTokens);
            Log.Log(LogLevel.Information, AgentLogMessage.ToolCallSkipped, skipped, tag: Tag);
            ToolCallRecords.TruncatedToolCall(tool, arguments, LlmLimits.MaxOutputTokens);
            return TruncatedCallMessage;
        }

        // A call that left out an argument the tool needs isn't run: the model hears what it missed, in the tool's
        // own words with its choices, rather than the framework's "missing a value for the required parameter" (FX108).
        if (MissingArguments.Message(context.Function, context.Arguments) is { } missing)
        {
            var refused = new ToolCallFailedHoles(tool, nameof(MissingArguments), missing);
            Log.Log(LogLevel.Information, AgentLogMessage.ToolCallFailed, refused, tag: Tag);
            return missing;
        }

        object? result;
        try
        {
            result = await context.Function.InvokeAsync(context.Arguments, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var failed = new ToolCallFailedHoles(tool, ex.GetType().Name, LogContent.Of(ex.Message));
            Log.Log(LogLevel.Information, AgentLogMessage.ToolCallFailed, failed, tag: Tag);
            throw;
        }

        string text = result?.ToString() ?? string.Empty;
        var returned = new ToolCallResultHoles(tool, LogContent.Of(text, cut: LoggedResultChars));
        Log.Log(LogLevel.Information, AgentLogMessage.ToolCallResult, returned, tag: Tag);
        // A tool that asks the person ends the turn once it succeeds, so the model waits for the answer (FX138).
        if (TurnEnding.Ends(context.Function, text)) context.Terminate = true;
        if (text.Length <= OversizedResults.MaxToolResultChars) return result;

        // An oversized result (e.g. a huge diff) would blow the model's context window on the next request.
        string? request = context.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        return await Oversized.SummariseAsync(tool, arguments, text, request, cancellationToken);
    }

    /// <summary>
    /// Sits inside the tool-call loop. After <c>MaximumIterationsPerRequest</c> rounds,
    /// <see cref="FunctionInvokingChatClient"/> sends one last request with no tools, so the model has to answer in
    /// text. Models that bind their thinking blocks to the tool list (Claude Haiku 5.5 and later) reject that request
    /// with a 400, which loses the whole turn. This puts the tools back unchanged with <see cref="ChatToolMode.None"/>
    /// (Anthropic: <c>tool_choice: none</c>): the tool list matches every earlier request, and the model still can't
    /// call a tool.
    /// </summary>
    internal ChatOptions? KeepToolsOnFinalRequest(ChatOptions? options)
    {
        // Every request of a turn carries the tools (BeginTurn), so one without them is the loop's final request.
        if (registry.Count == 0 || options?.Tools is { Count: > 0 }) return options;

        ChatOptions final = options?.Clone() ?? new ChatOptions();
        // The turn's conversation's tools, as every earlier request sent them; outside a turn, the engine's.
        final.Tools = TurnTools.Current is { } turn ? [.. turn.Tools] : registry.Live;
        final.ToolMode = ChatToolMode.None;
        return final;
    }

    /// <summary>
    /// A sub-session's prompt budget (FX14). Every tool result stays in the prompt, and every later request pays for it
    /// again; past Haiku 5.5's long-prompt threshold every request costs five times as much. When the next request of
    /// a sub-session's turn is expected to pass the budget (the last prompt plus its last growth), it keeps its tools
    /// with <see cref="ChatToolMode.None"/>, as the loop's final request does: the subagent answers from what it has.
    /// </summary>
    /// <remarks>
    /// Only a sub-session's turns (their usage tags name a parent turn): the user's own sessions may need a big
    /// context, and nothing above a sub-session would notice the cost. Summary requests run outside the loop and
    /// aren't counted.
    /// </remarks>
    internal ChatOptions? KeepWithinPromptBudget(ChatOptions? options, UsageScope? scope)
    {
        if (subSessionPromptBudget <= 0 || scope?.Tags.Parent is null) return options;
        if (options?.Tools is not { Count: > 0 } || options.ToolMode == ChatToolMode.None) return options;

        // Summaries and a tool's model requests run outside the loop, on their own prompts (F171.1); a sub-session's
        // roll-up carries the subagent's tokens, not this prompt's size.
        var prompts = scope.Records
            .Where(r => r.Purpose is not (UsagePurpose.Summary or UsagePurpose.Tool or UsagePurpose.SubSession))
            .Select(r => Prompt(r.Tokens)).ToList();
        if (prompts.Count == 0) return options;
        long last = prompts[^1];
        long next = last + (prompts.Count > 1 ? Math.Max(0, last - prompts[^2]) : 0);
        if (next < subSessionPromptBudget) return options;

        var reached = new SubSessionPromptBudgetReachedHoles(Counted(last), Counted(next),
            Counted(subSessionPromptBudget));
        Log.Log(LogLevel.Information, AgentLogMessage.SubSessionPromptBudgetReached, reached, tag: Tag);
        ChatOptions limited = options.Clone();
        limited.ToolMode = ChatToolMode.None;
        return limited;
    }

    /// <summary>A count of tokens written for reading, the same on every machine.</summary>
    private static string Counted(long tokens) => tokens.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>How big a request's prompt was: its input, read from and written to the cache.</summary>
    private static long Prompt(UsageTokens tokens) =>
        (tokens.Input ?? 0) + (tokens.CacheRead ?? 0) + (tokens.CacheWrite ?? 0);

    /// <summary>A loop request's options with the final request's tools kept and the prompt budget held.</summary>
    private ChatOptions? Shape(ChatOptions? options) =>
        KeepWithinPromptBudget(KeepToolsOnFinalRequest(options), UsageScope.Current);

    /// <summary>The pipeline step that shapes each request of the loop, answered whole.</summary>
    public Task<ChatResponse> KeepToolsOnFinalRequestAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next,
        CancellationToken cancellationToken) =>
        next.GetResponseAsync(messages, Shape(options), cancellationToken);

    /// <summary>The pipeline step that shapes each request of the loop, streamed.</summary>
    public IAsyncEnumerable<ChatResponseUpdate> KeepToolsOnFinalRequestStreamingAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next,
        CancellationToken cancellationToken) =>
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

    /// <summary>
    /// Streaming form of <see cref="MarkTruncatedToolCallsAsync"/>; the tool loop only runs calls once the stream ends.
    /// </summary>
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
}
