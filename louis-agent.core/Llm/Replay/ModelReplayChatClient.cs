namespace LouisAgent.Core.Llm.Replay;

using System.Text.Json;
using LouisAgent.Core.Llm.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.Replay.Store;
using Microsoft.Extensions.AI;

/// <summary>
/// The model's answers, recorded and replayed (F18 step 4e, skill 13: a response cache for exactly repeated requests). It is
/// Microsoft.Extensions.AI's <see cref="CachingChatClient"/> with streaming updates coalesced, so a streamed request is stored and read as
/// one <see cref="ChatResponse"/>, over our own key (<see cref="ModelAsk"/>) and the replay store. The mode comes from the flow on each
/// request, as <c>ReplayClock</c>'s does, because engines are cached across sessions:
/// <list type="table">
/// <item><term>no replay, or web <c>Off</c></term><description>passes through.</description></item>
/// <item><term>web <c>Record</c></term><description>calls the model and saves the answer; never reads.</description></item>
/// <item><term>web <c>Replay</c>, <c>Model</c> false</term><description>passes through: the recorded web with a live model.</description></item>
/// <item><term>web <c>Replay</c>, <c>Model</c> true</term><description>answers from the recording; a miss throws and the model is never called.</description></item>
/// </list>
/// A hit carries <see cref="HitTag"/> on the response (not on the messages, so the history and the next request's key never see it) and no
/// <see cref="ChatResponse.Usage"/>, so the usage recorder writes it as a known 0. Sits innermost, below the marking of cut-off tool
/// calls and the tracing, so a hit is still a span and a replayed cut-off answer is marked again. A repeated identical request in one
/// trial is answered by its first recording (the store keeps no counters).
/// </summary>
public sealed class ModelReplayChatClient(IChatClient inner, ReplayStore store) : CachingChatClient(inner)
{
    /// <summary>The <see cref="ChatResponse.AdditionalProperties"/> key of a replayed answer; the usage recorder reads it.</summary>
    public const string HitTag = "replay_hit";

    private readonly string? _defaultModel = inner.GetService<ChatClientMetadata>()?.DefaultModelId;

    /// <summary>The trial this request records into or replays from, or null when the flow leaves the model alone.</summary>
    private static ReplayTrial? Active() => ReplayTrial.Current switch
    {
        { Mode: WebReplayMode.Record } trial => trial,
        { Replaying: true } trial when SessionReplay.Current is { Model: true } => trial,
        _ => null,
    };

    protected override bool EnableCaching(IEnumerable<ChatMessage> messages, ChatOptions? options) =>
        base.EnableCaching(messages, options) && Active() is not null;

    protected override string GetCacheKey(IEnumerable<ChatMessage> messages, ChatOptions? options, params ReadOnlySpan<object?> additionalValues) =>
        ModelAsk.Of(messages, options, _defaultModel).ToAsk().Key;

    protected override async Task<ChatResponse?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        if (Active() is not { Replaying: true } trial) return null;

        var ask = new ReplayAsk(ReplayKind.Model, key);
        if (await store.FindAsync<JsonElement>(trial.Of, ask, cancellationToken) is not { } found)
            throw new InvalidOperationException(await MissAsync(trial.Of, ask, cancellationToken));

        JsonElement recorded = found.OrThrow(error => new InvalidOperationException(error));
        ChatResponse response = recorded.Deserialize<ChatResponse>(AIJsonUtilities.DefaultOptions)!;
        response.Usage = null;
        (response.AdditionalProperties ??= [])[HitTag] = true;
        return response;
    }

    protected override async Task WriteCacheAsync(string key, ChatResponse value, CancellationToken cancellationToken)
    {
        if (Active() is not { Mode: WebReplayMode.Record } trial) return;

        JsonElement answer = JsonSerializer.SerializeToElement(value, AIJsonUtilities.DefaultOptions);
        await store.SaveAnswerAsync(trial.Of, new ReplayAsk(ReplayKind.Model, key), answer, cancellationToken);
    }

    private async Task<string> MissAsync(string trial, ReplayAsk ask, CancellationToken cancellationToken)
    {
        var recorded = (await store.AsksAsync(trial, ReplayKind.Model, cancellationToken)).Select(ModelAsk.From).OfType<ModelAsk>().ToList();
        return ReplayMiss.Describe(trial, ModelAsk.From(ask)!, recorded);
    }

    // Only reached with CoalesceStreamingUpdates off; the key and the store are made for the coalesced response.
    protected override Task<IReadOnlyList<ChatResponseUpdate>?> ReadCacheStreamingAsync(string key, CancellationToken cancellationToken) =>
        throw new NotSupportedException(MessageText.Format(LlmErrorMessage.ModelReplayNeedsCoalescing, new { }));

    protected override Task WriteCacheStreamingAsync(string key, IReadOnlyList<ChatResponseUpdate> value, CancellationToken cancellationToken) =>
        throw new NotSupportedException(MessageText.Format(LlmErrorMessage.ModelReplayNeedsCoalescing, new { }));
}
