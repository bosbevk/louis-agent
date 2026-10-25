namespace LouisAgent.Core.Llm.UsageMapping;

using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

/// <summary>
/// Writes one <see cref="UsageRecord"/> per model request. Sits inside <c>UseFunctionInvocation</c> in
/// <c>AgentEngine</c>, so it sees every round of the tool loop as its own request, and the prompt's hash, taken when the
/// request starts (F18 step 4c).
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
        string promptHash = PromptHash.Of(messages, options);
        var stopwatch = Stopwatch.StartNew();
        ChatResponse response;
        try
        {
            response = await base.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A timeout or a stopped caller (FX9): recorded as the streaming path records a cancelled stream. A cancelled
            // call returns no usage, so its tokens stay unknown (null), never 0. Provider errors stay unrecorded.
            Store(scope, CreateRecord(scope, round, usage: null, model: null, Cancelled, stopwatch.Elapsed, promptHash: promptHash));
            throw;
        }

        // Non-streaming responses carry their usage on ChatResponse.Usage, not as UsageContent.
        object[] raw = response.RawRepresentation is { } r && _mapper.Keeps(r) ? [r] : [];
        bool hit = response.AdditionalProperties?.ContainsKey(ModelReplayChatClient.HitTag) == true;
        Store(scope, CreateRecord(scope, round, response.Usage, response.ModelId, response.FinishReason?.Value, stopwatch.Elapsed, promptHash, raw, hit));
        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Taken when the request starts: the stream may end where the scope isn't current (disposed by the host).
        UsageScope? scope = UsageScope.Current;
        int round = NextRound(scope);
        string promptHash = PromptHash.Of(messages, options);
        var stopwatch = Stopwatch.StartNew();
        UsageDetails? usage = null;
        List<object> raw = [];
        string? model = null, stop = null;
        bool completed = false, failed = false, hit = false;

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
                // Only what the mapper asks for (on Anthropic, one event per request), not the whole stream.
                if (update.RawRepresentation is { } r && _mapper.Keeps(r)) raw.Add(r);
                hit |= update.AdditionalProperties?.ContainsKey(ModelReplayChatClient.HitTag) == true;
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
                Store(scope, CreateRecord(scope, round, usage, model, completed ? stop : Cancelled, stopwatch.Elapsed, promptHash, raw, hit));
        }
    }

    /// <summary>
    /// Writes the record, then adds it as stored (priced by <see cref="PricingUsageSink"/>) to the scope the request started
    /// in, so the turn's totals include its cost.
    /// </summary>
    private void Store(UsageScope? scope, UsageRecord record)
    {
        // Not scope?.Add(sink.Record(record)): ?. skips evaluating the argument too, so outside a scope nothing would be written.
        UsageRecord stored = sink.Record(record);
        scope?.Add(stored);
    }

    /// <summary>Stop reason for a request that ended before the provider finished it.</summary>
    internal const string Cancelled = "cancelled";

    /// <summary>
    /// Numbers a request when it starts, so overlapping requests are numbered in the order they were made. Outside any
    /// scope every request is round 1.
    /// </summary>
    private static int NextRound(UsageScope? scope) => scope?.NextRound() ?? 1;

    /// <summary>Builds the record for a finished request from the scope it started in.</summary>
    internal UsageRecord CreateRecord(
        UsageScope? scope, int round, UsageDetails? usage, string? model, string? stop, TimeSpan duration, string? promptHash,
        IReadOnlyList<object>? raw = null, bool cacheHit = false) =>
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
            Parent: scope?.Tags.Parent,
            Model: model ?? defaultModel,
            // A replayed answer cost nothing: known zeros, never the recorded tokens and never unknown (ADR 0007).
            Tokens: cacheHit ? new UsageTokens(0, 0, 0, 0, 0) : _mapper.Map(usage, raw ?? []),
            DurationMs: (long)duration.TotalMilliseconds,
            Stop: stop,
            PromptHash: promptHash,
            CacheHit: cacheHit ? true : null);
}