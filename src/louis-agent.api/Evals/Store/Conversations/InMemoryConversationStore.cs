using static LouisAgent.Api.Evals.Store.EvalDocuments;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Store.Conversations;

/// <summary>
/// The conversations in memory: what the tests run on, and the reference the MongoDB one is held to. One lock is
/// plenty, because a handful of writes a second is all it ever sees.
/// </summary>
/// <remarks>
/// Repository, the in-memory side: the same conversations as MongoDB behind the same interface, held to the same
/// contract test.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class InMemoryConversationStore : IConversationStore
{
    /// <summary>The one lock over the conversations.</summary>
    private readonly Lock _lock = new();

    /// <summary>Conversations by run and key.</summary>
    private readonly Dictionary<(string Run, ConversationKey Key), Conversation> _conversations = [];

    /// <summary>Saves a conversation, replacing the one under its key.</summary>
    public Task SaveConversationAsync(string run, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        lock (_lock) _conversations[(run, conversation.Key)] = conversation;
        return Task.CompletedTask;
    }

    /// <summary>The run's conversations in the order they started.</summary>
    public Task<IReadOnlyList<Conversation>> ConversationsAsync(string run,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Conversation>>(_conversations.Where(c => c.Key.Run == run)
                .Select(c => c.Value)
                .OrderBy(c => c.Order).ToArray());
    }

    /// <summary>Forgets the run's conversations, and says whether there were any.</summary>
    public Task<bool> DeleteConversationsAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var keys = _conversations.Keys.Where(k => k.Run == run).ToArray();
            foreach (var key in keys) _conversations.Remove(key);
            return Task.FromResult(keys.Length > 0);
        }
    }
}
