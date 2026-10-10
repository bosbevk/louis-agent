namespace louis_agent.core.usage;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

/// <summary>
/// Writes one <see cref="UsageRecord"/> per model request. Sits inside <c>UseFunctionInvocation</c> in
/// <c>AgentEngine</c>, so it sees every round of the tool loop as its own request.
/// </summary>
/// <param name="innerClient">The provider client (or the next middleware).</param>
/// <param name="sink">Where records go.</param>
/// <param name="defaultModel">Recorded when the response doesn't name its model.</param>
public sealed class UsageRecordingChatClient(IChatClient innerClient, IUsageSink sink, string? defaultModel = null)
    : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        int round = NextRound();
        var stopwatch = Stopwatch.StartNew();
        ChatResponse response = await base.GetResponseAsync(messages, options, cancellationToken);

        // Non-streaming responses carry their usage on ChatResponse.Usage, not as UsageContent.
        sink.Record(CreateRecord(round, response.Usage, response.ModelId, response.FinishReason?.Value, stopwatch.Elapsed));
        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO F1 step 4: pass every update through unchanged, and remember the UsageContent (Anthropic sends one, in the
        //   last update), the finish reason and the model id. Record once when the stream ends; if it is cancelled,
        //   record what was reported so far with Stop = "cancelled" (a try/finally around the loop).
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            yield return update;
    }

    /// <summary>
    /// Numbers a request when it starts, so overlapping requests are numbered in the order they were made. Outside any
    /// scope every request is round 1.
    /// </summary>
    private static int NextRound() => UsageScope.Current?.NextRound() ?? 1;

    /// <summary>Builds the record for a finished request from the current scope.</summary>
    internal UsageRecord CreateRecord(int round, UsageDetails? usage, string? model, string? stop, TimeSpan duration) =>
        // Host, Session, Turn, Task, Run and Service come with F1-S2 and F1-S3.
        new(
            At: DateTimeOffset.UtcNow,
            Round: round,
            Purpose: UsageScope.Current?.Purpose ?? UsagePurpose.Turn,
            Host: null,
            Session: null,
            Turn: null,
            Task: null,
            Run: null,
            Service: null,
            Model: model ?? defaultModel,
            Tokens: UsageTokens.From(usage),
            DurationMs: (long)duration.TotalMilliseconds,
            Stop: stop);
}