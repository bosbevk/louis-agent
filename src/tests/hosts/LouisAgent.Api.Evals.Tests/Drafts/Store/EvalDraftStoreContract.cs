using LouisAgent.Api.Evals.Drafts.Store;

namespace LouisAgent.Api.Evals.Tests.Evals.Drafts.Store;

/// <summary>Runs the draft store's shared tests against the in-memory store, on every machine and on CI.</summary>
public class InMemoryEvalDraftStoreTests : EvalDraftStoreContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override IEvalDraftStore CreateStore() => new InMemoryEvalDraftStore();
}
