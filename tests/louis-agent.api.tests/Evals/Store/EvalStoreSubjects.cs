using LouisAgent.Api.Evals.Store;

namespace LouisAgent.Api.Tests.Evals.Store;

/// <summary>
/// Runs the whole store's shared tests against the in-memory one, on every machine and on CI: what reaches across the
/// records (deleting a run) and the records not yet in a store of their own.
/// </summary>
public class InMemoryEvalStoreTests : EvalStoreContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override IEvalStore CreateStore() => new InMemoryEvalStore();
}

/// <summary>Runs the shared tests for a run's notes against the in-memory store.</summary>
public class InMemoryNotesTests : EvalNotesContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override IEvalStore CreateStore() => new InMemoryEvalStore();
}
