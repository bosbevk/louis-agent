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
        // TODO F1 step 3: time the call, then record ChatResponse.Usage (non-streaming responses carry no UsageContent).
        //   Round and purpose come from UsageScope.Current; with no scope, record round 1 and purpose "turn".
        var stopwatch = Stopwatch.StartNew();
        ChatResponse response = await base.GetResponseAsync(messages, options, cancellationToken);
        _ = stopwatch;
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

    /// <summary>Builds the record for a finished request from the current scope.</summary>
    internal UsageRecord CreateRecord(UsageDetails? usage, string? model, string? stop, TimeSpan duration)
    {
        // TODO F1 step 3: take Round/Purpose from UsageScope.Current, tokens from UsageTokens.From(usage),
        //   model ?? defaultModel. Leave Host/Session/Turn/Task/Run/Service null until F1-S2 and F1-S3.
        _ = sink;
        throw new NotImplementedException();
    }
}