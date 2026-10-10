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
/// <param name="mapper">The provider's usage mapping; <see cref="StandardUsageMapper"/> when null.</param>
public sealed class UsageRecordingChatClient(
    IChatClient innerClient, IUsageSink sink, string? defaultModel = null, IUsageMapper? mapper = null)
    : DelegatingChatClient(innerClient)
{
    private readonly IUsageMapper _mapper = mapper ?? new StandardUsageMapper();

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        UsageScope? scope = UsageScope.Current;
        int round = NextRound(scope);
        var stopwatch = Stopwatch.StartNew();
        ChatResponse response = await base.GetResponseAsync(messages, options, cancellationToken);

        // Non-streaming responses carry their usage on ChatResponse.Usage, not as UsageContent.
        sink.Record(CreateRecord(scope, round, response.Usage, response.ModelId, response.FinishReason?.Value, stopwatch.Elapsed));
        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Taken when the request starts: the stream may end where the scope isn't current (disposed by the host).
        UsageScope? scope = UsageScope.Current;
        int round = NextRound(scope);
        var stopwatch = Stopwatch.StartNew();
        UsageDetails? usage = null;
        string? model = null, stop = null;
        bool completed = false, failed = false;

        // Enumerated by hand rather than with await foreach: C# allows no catch around a yield, and the catch is what
        // tells a provider error (not recorded, like a failed non-streaming call) from a cancellation (recorded).
        await using var updates = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                try
                {
                    if (!await updates.MoveNextAsync()) break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed = true;
                    throw;
                }

                ChatResponseUpdate update = updates.Current;
                // Anthropic sends one UsageContent, in the last update; adding them up also covers providers that split it.
                foreach (var content in update.Contents.OfType<UsageContent>())
                {
                    usage ??= new UsageDetails();
                    usage.Add(content.Details);
                }
                model ??= update.ModelId;
                if (update.FinishReason is { } finishReason) stop = finishReason.Value;

                yield return update;
            }

            completed = true;
        }
        finally
        {
            // Reached on normal completion, an exception, or the caller disposing the stream early (a cancelled turn).
            if (!failed)
                sink.Record(CreateRecord(scope, round, usage, model, completed ? stop : Cancelled, stopwatch.Elapsed));
        }
    }

    /// <summary>Stop reason for a stream that ended before the provider finished it.</summary>
    internal const string Cancelled = "cancelled";

    /// <summary>
    /// Numbers a request when it starts, so overlapping requests are numbered in the order they were made. Outside any
    /// scope every request is round 1.
    /// </summary>
    private static int NextRound(UsageScope? scope) => scope?.NextRound() ?? 1;

    /// <summary>Builds the record for a finished request from the scope it started in.</summary>
    internal UsageRecord CreateRecord(UsageScope? scope, int round, UsageDetails? usage, string? model, string? stop, TimeSpan duration) =>
        new(
            At: DateTimeOffset.UtcNow,
            Round: round,
            Purpose: scope?.Purpose ?? UsagePurpose.Turn,
            Host: scope?.Host,
            Session: scope?.Session,
            Turn: scope?.Turn,
            Task: scope?.Tags.Task,
            Run: scope?.Tags.Run,
            Service: scope?.Tags.Service,
            Model: model ?? defaultModel,
            Tokens: _mapper.Map(usage),
            DurationMs: (long)duration.TotalMilliseconds,
            Stop: stop);
}