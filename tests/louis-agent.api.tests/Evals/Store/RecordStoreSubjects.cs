using LouisAgent.Api.Evals.Store.Conversations;
using LouisAgent.Api.Evals.Store.Runs;

namespace LouisAgent.Api.Tests.Evals.Store;

/// <summary>Runs the run store's shared tests against the runs in memory, on every machine and on CI.</summary>
public class InMemoryRunStoreTests : RunStoreContract
{
    /// <summary>A fresh, empty store of runs for each test.</summary>
    protected override IRunStore CreateStore() => new InMemoryRunStore();
}

/// <summary>Runs the shared tests for a run's own fields against the runs in memory.</summary>
public class InMemoryRunFieldsTests : RunFieldsContract
{
    /// <summary>A fresh, empty store of runs for each test.</summary>
    protected override IRunStore CreateStore() => new InMemoryRunStore();
}

/// <summary>Runs the conversation store's shared tests against the conversations in memory.</summary>
public class InMemoryConversationStoreTests : ConversationStoreContract
{
    /// <summary>A fresh, empty store of conversations for each test.</summary>
    protected override IConversationStore CreateStore() => new InMemoryConversationStore();
}
