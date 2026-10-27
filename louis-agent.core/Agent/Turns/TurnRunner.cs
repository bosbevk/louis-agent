
using System.Runtime.CompilerServices;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Agent.Turns;
/// <summary>
/// One chat turn (S, CL15, from AgentEngine): the history seeded and kept current, the prompt sent on the pipeline,
/// the reply read whole or streamed, and a reply cut off by the output limit continued.
/// </summary>
/// <param name="systemPrompt">The system prompt as it is now: skills may have been reloaded since a conversation started.</param>
/// <param name="thinking">Reasoning effort for each turn, as the engine has it; null disables thinking.</param>
internal sealed class TurnRunner(
    IChatClient chatClient,
    ToolRegistry registry,
    Func<string> systemPrompt,
    Func<ReasoningEffort?> thinking,
    bool supportsTools)
{
    private static readonly ILogger Log = AgentLogging.For<TurnRunner>();

    // Room for a sizeable file in one tool call; larger files are written in chunks (see default.md).
    internal const int MaxOutputTokens = LlmClientFactory.MaxOutputTokens;

    private const int MaxCutOffContinuations = 3;

    internal static string NoToolsNotice => MessageText.Format(AgentToolMessage.NoToolsNotice);

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
        ChatOptions chatOptions = Begin(history, userInput);
        ChatResponse response = await chatClient.GetResponseAsync(history, chatOptions, cancellationToken);

        // Includes assistant tool-call and tool-result messages so follow-up turns keep their context.
        history.AddRange(response.Messages);
        return response.Text;
    }

    /// <summary>
    /// Streaming form of <see cref="ProcessAsync"/>: yields text as the model writes it, each tool call as it is made
    /// (<see cref="FunctionCallContent"/>) and its outcome (<see cref="FunctionResultContent"/>). The completed turn is
    /// recorded in <paramref name="history"/> once the stream ends.
    /// </summary>
    public async IAsyncEnumerable<ChatResponseUpdate> StreamAsync(List<ChatMessage> history, string userInput,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatOptions chatOptions = Begin(history, userInput);
        for (int continuation = 0; ; continuation++)
        {
            var updates = new List<ChatResponseUpdate>();
            await foreach (var update in chatClient.GetStreamingResponseAsync(history, chatOptions, cancellationToken))
            {
                updates.Add(update);
                yield return update;
            }

            history.AddMessages(updates);

            // When streaming, a tool call cut off by the output limit is dropped entirely, so the turn would just stop.
            bool cutOff = updates.LastOrDefault(u => u.FinishReason is not null)?.FinishReason ==
                          ChatFinishReason.Length;
            if (!cutOff || continuation >= MaxCutOffContinuations) yield break;

            Log.Log(LogLevel.Warning, AgentLogMessage.ReplyCutOff, new { MaxOutputTokens });
            ToolCallRecords.TruncatedToolCall("(streamed reply)", string.Empty, MaxOutputTokens);
            history.Add(new ChatMessage(ChatRole.User, ContinueAfterCutOffMessage));
            // The rest of this turn's requests exist only because of the cut-off; the host's scope says so.
            if (UsageScope.Current is { } scope) scope.Purpose = UsagePurpose.Continue;
        }
    }

    private ChatOptions Begin(List<ChatMessage> history, string userInput)
    {
        // Skills may have been reloaded since this conversation started; keep its system prompt current.
        string prompt = systemPrompt();
        if (history.Count > 0 && history[0].Role == ChatRole.System && history[0].Text != prompt)
            history[0] = new ChatMessage(ChatRole.System, prompt);

        history.Add(new ChatMessage(ChatRole.User, userInput));

        var chatOptions = new ChatOptions { MaxOutputTokens = MaxOutputTokens };
        if (thinking() is { } effort)
            chatOptions.Reasoning = new ReasoningOptions { Effort = effort, Output = ReasoningOutput.Full };
        if (supportsTools)
        {
            chatOptions.Tools = registry.Live;
            chatOptions.ToolMode = ChatToolMode.Auto;
        }

        return chatOptions;
    }
}
