using LouisAgent.Api.Evals.Store;

namespace LouisAgent.Api.Tests.Evals.Store;

/// <summary>Runs the store's shared tests against the in-memory store, on every machine and on CI.</summary>
public class InMemoryEvalStoreTests : EvalStoreContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override IEvalStore CreateStore() => new InMemoryEvalStore();
}

/// <summary>Runs the shared tests for a run's own fields against the in-memory store.</summary>
public class InMemoryRunFieldsTests : EvalRunFieldsContract
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
