using LouisAgent.Core.Delegation;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Sessions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Agent.Messages.Holes;
using LouisAgent.Core.Llm.Providers;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Agent.Turns;

/// <summary>
/// One chat turn (S, CL15, from AgentEngine): the history seeded and kept current, the prompt sent on the pipeline,
/// the reply read whole or streamed, and a reply cut off by the output limit continued. Each turn is one span,
/// named for the agent (F52).
/// </summary>
/// <param name="systemPrompt">
/// The system prompt as it is now: skills may have been reloaded since a conversation started.
/// </param>
/// <param name="thinking">Reasoning effort for each turn, as the engine has it; null disables thinking.</param>
/// <param name="agentName">The agent a trace names the turn for, as the engine has it.</param>
internal sealed class TurnRunner(
    IChatClient chatClient,
    ToolRegistry registry,
    Func<string> systemPrompt,
    Func<ReasoningEffort?> thinking,
    bool supportsTools,
    Func<string> agentName)
{
    /// <summary>The turn's own lines: a reply cut off by the output limit.</summary>
    private static readonly ILogger Log = AgentLogging.For<TurnRunner>();

    /// <summary>
    /// Room for a sizeable file in one tool call; larger files are written in chunks (see default.md).
    /// </summary>
    internal const int MaxOutputTokens = LlmLimits.MaxOutputTokens;

    /// <summary>How many times a cut-off reply is continued before the turn gives up.</summary>
    private const int MaxCutOffContinuations = 3;

    /// <summary>What a model without tools is told, as the history's second system message.</summary>
    internal static string NoToolsNotice => MessageText.Format(AgentToolMessage.NoToolsNotice);

    /// <summary>What the model is asked after its reply was cut off.</summary>
    internal static string ContinueAfterCutOffMessage => MessageText.Format(AgentToolMessage.ContinueAfterCutOff);

    /// <summary>A history seeded with the system prompt (and the no-tools notice when applicable).</summary>
    public List<ChatMessage> NewHistory()
    {
        var history = new List<ChatMessage> { new(ChatRole.System, systemPrompt()) };
        if (!supportsTools) history.Add(new ChatMessage(ChatRole.System, NoToolsNotice));
        return history;
    }

    /// <summary>
    /// Processes one prompt without writing to the console, allowing protocol adapters to control how the response is
    /// transported. Tool calls made by the model are executed and recorded in <paramref name="history"/> along with the
    /// final answer.
    /// </summary>
    public async Task<string> ProcessAsync(List<ChatMessage> history, string userInput,
        CancellationToken cancellationToken = default)
    {
        // The turn's span holds the tool loop's; a failure is recorded on it and still reaches the host.
        using var span = TurnSpans.Start(agentName());
        try
        {
            ChatOptions chatOptions = Begin(history, userInput);
            ChatResponse response = await chatClient.GetResponseAsync(history, chatOptions, cancellationToken);

            // Includes assistant tool-call and tool-result messages so follow-up turns keep their context.
            history.AddRange(response.Messages);
            if (ShownReport.In([.. response.Messages.SelectMany(message => message.Contents)]) is not { } report)
                return response.Text;

            history.Add(new ChatMessage(ChatRole.Assistant, report));
            return response.Text + report;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            TurnSpans.Fail(span, ex);
            throw;
        }
    }

    /// <summary>
    /// Streaming form of <see cref="ProcessAsync"/>: yields text as the model writes it, each tool call as it is made
    /// (<see cref="FunctionCallContent"/>) and its outcome (<see cref="FunctionResultContent"/>). The completed turn is
    /// recorded in <paramref name="history"/> once the stream ends.
    /// </summary>
    public async IAsyncEnumerable<ChatResponseUpdate> StreamAsync(List<ChatMessage> history, string userInput,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var span = TurnSpans.Start(agentName());
        ChatOptions chatOptions = Begin(history, userInput);
        bool owedAsked = false;
        for (int continuation = 0; ; continuation++)
        {
            var updates = new List<ChatResponseUpdate>();
            // Read by hand: C# allows no yield inside a try with a catch, and the catch records a failure on the span.
            await using var stream = chatClient.GetStreamingResponseAsync(history, chatOptions, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
            while (await NextAsync(stream, span))
            {
                updates.Add(stream.Current);
                yield return stream.Current;
            }

            // The turn ended on a specialist's report: it's shown as it is, and no request is made to retype it.
            if (ShownReport.In([.. updates.SelectMany(update => update.Contents)]) is { } report)
            {
                var shown = new ChatResponseUpdate(ChatRole.Assistant, report)
                {
                    MessageId = Guid.NewGuid().ToString()
                };
                updates.Add(shown);
                yield return shown;
            }

            history.AddMessages(updates);

            // When streaming, a tool call cut off by the output limit is dropped entirely, so the turn would just stop.
            bool cutOff = updates.LastOrDefault(u => u.FinishReason is not null)?.FinishReason ==
                          ChatFinishReason.Length;
            if (!cutOff)
            {
                // An identity whose answers end with a tool (the guide's choices, FX139) gets it once, if missing.
                if (owedAsked || OwedTool(updates, chatOptions, userInput) is not { } owed) yield break;
                owedAsked = true;
                // A request must end with the user's message (no prefill), so a notice asks for it, as after a cut-off.
                var owedTool = new EndWithToolHoles(owed);
                history.Add(new ChatMessage(ChatRole.User, MessageText.Format(AgentToolMessage.EndWithTool, owedTool)));
                chatOptions = Requiring(chatOptions, owed);
                continue;
            }

            if (continuation >= MaxCutOffContinuations) yield break;

            var outputLimit = new ReplyCutOffHoles(MaxOutputTokens);
            Log.Log(LogLevel.Warning, AgentLogMessage.ReplyCutOff, outputLimit);
            ToolCallRecords.TruncatedToolCall("(streamed reply)", string.Empty, MaxOutputTokens);
            history.Add(new ChatMessage(ChatRole.User, ContinueAfterCutOffMessage));
            // The rest of this turn's requests exist only because of the cut-off; the host's scope says so.
            if (UsageScope.Current is { } scope) scope.Purpose = UsagePurpose.Continue;
        }
    }

    /// <summary>
    /// The tool the running identity ends each answer with, when this answer didn't call it and the turn has it
    /// (FX139); none otherwise.
    /// </summary>
    private static string? OwedTool(List<ChatResponseUpdate> updates, ChatOptions options, string userInput)
    {
        if (TurnIdentity.Current?.EndsEachAnswerWith is not { } tool) return null;
        // The person said stop: the answer says goodbye and owes nothing.
        if (TurnEnding.Stopped(userInput, TurnIdentity.Current.StopsOn)) return null;
        var answer = updates.SelectMany(u => u.Contents).ToList();
        var calls = answer.OfType<FunctionCallContent>().ToList();
        if (calls.Any(call => call.Name == tool)) return null;
        // An answer that ended the turn with another tool, such as a form, is closed: nothing beside it (F127.4.3).
        if (TurnEnding.Ended(answer, options.Tools)) return null;
        // An answer that asked another agent owes nothing: that agent may be waiting for the person (FX141).
        if (calls.Any(call => call.Name == nameof(AskAgentTools.AskAgent))) return null;
        return options.Tools?.Any(t => t.Name == tool) == true ? tool : null;
    }

    /// <summary>
    /// The turn's options with the owed tool required and thinking off, since a model can't think and be made to call
    /// one tool in the same request; the tool ends the turn (FX138).
    /// </summary>
    private static ChatOptions Requiring(ChatOptions options, string tool)
    {
        var required = options.Clone();
        required.ToolMode = ChatToolMode.RequireSpecific(tool);
        required.Reasoning = null;
        return required;
    }

    /// <summary>
    /// Moves the stream on with the turn's span current, and records a failure on the span before it reaches the host.
    /// </summary>
    private static async ValueTask<bool> NextAsync(IAsyncEnumerator<ChatResponseUpdate> stream, Activity? span)
    {
        Resume(span);
        try
        {
            return await stream.MoveNextAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            TurnSpans.Fail(span, ex);
            throw;
        }
    }

    /// <summary>
    /// Makes the turn's span current again: an async iterator gets its caller's context back after every
    /// <c>yield</c>, so without this the tool loop's next span would open outside the turn.
    /// </summary>
    private static void Resume(Activity? span)
    {
        if (span is not null && Activity.Current != span && !span.IsStopped) Activity.Current = span;
    }

    /// <summary>
    /// Starts a turn: the system prompt brought up to date, the user's words added, and the request's options (the
    /// output limit, thinking, the tools).
    /// </summary>
    private ChatOptions Begin(List<ChatMessage> history, string userInput)
    {
        // Skills may have been reloaded since this conversation started; keep its system prompt current.
        string prompt = systemPrompt();
        if (history.Count > 0 && history[0].Role == ChatRole.System && history[0].Text != prompt)
            history[0] = new ChatMessage(ChatRole.System, prompt);

        history.Add(new ChatMessage(ChatRole.User, userInput));

        var chatOptions = new ChatOptions { MaxOutputTokens = MaxOutputTokens };
        if (thinking() is { } effort)
            chatOptions.Reasoning = new ReasoningOptions
            {
                Effort = effort,
                Output = ReasoningOutput.Full
            };
        if (supportsTools)
        {
            chatOptions.Tools = registry.Live;
            chatOptions.ToolMode = ChatToolMode.Auto;
        }

        return chatOptions;
    }
}
