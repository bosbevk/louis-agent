using System.Runtime.CompilerServices;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Llm.UsageMapping;

/// <summary>
/// Opens a usage scope of one purpose around each request (F171.1), so the ledger tells a tool's call apart from a
/// turn, a summary or a grading, while the scope keeps the turn's host, session and turn from the one it opens inside.
/// </summary>
/// <remarks>
/// Decorator: one more layer over a client that already records its usage, so the recording reads this purpose from
/// the current scope and nothing below it changes. A streamed request keeps the scope current around each step of
/// the stream, since an async iterator can't keep it across a <c>yield</c> (<see cref="UsageScope.Activate" />).
/// </remarks>
/// <param name="inner">The client that records its usage.</param>
/// <param name="purpose">What every request through it is for, as <see cref="UsagePurpose" /> names it.</param>
internal sealed class UsagePurposeChatClient(IChatClient inner, string purpose) : DelegatingChatClient(inner)
{
    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var scope = UsageScope.Begin(purpose);
        return await base.GetResponseAsync(messages, options, cancellationToken);
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var scope = UsageScope.Begin(purpose);
        var updates = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                // The scope made current for each step, where the inner client reads it and records the request.
                bool moved;
                using (scope.Activate()) moved = await updates.MoveNextAsync();
                if (!moved) yield break;
                yield return updates.Current;
            }
        }
        finally
        {
            await updates.DisposeAsync();
        }
    }
}
