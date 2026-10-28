using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Endpoints.Admin.Runs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// The stored-run handlers declare what they answer (ADR 0045), and answer it with no HTTP in between.
/// </summary>
public class StoredRunHandlersTests
{
    /// <summary>What each handler says it answers.</summary>
    private static IEnumerable<TestCaseData> Declared()
    {
        yield return new TestCaseData("GetRun",
            typeof(Task<Results<JsonHttpResult<StoredRunReply>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GetTurnEvents",
            typeof(Task<Results<JsonHttpResult<TurnEventsReply>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GetProtocol",
            typeof(Task<Results<ContentHttpResult, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("DeleteRun", typeof(Task<Results<NoContent, JsonHttpResult<ApiError>>>));
    }

    /// <summary>A handler's signature names every answer it can give.</summary>
    [TestCaseSource(nameof(Declared))]
    public void AHandler_DeclaresItsAnswers(string handler, Type answers)
    {
        Assert.That(typeof(StoredRunHandlers).GetMethod(handler)?.ReturnType, Is.EqualTo(answers));
    }

    /// <summary>Without a store every read is a 503; with one, a run it doesn't hold is a 404.</summary>
    [Test]
    public async Task ARunNobodyKept_IsAnError()
    {
        var none = Handlers(null);
        var empty = Handlers(new InMemoryBenchmarkStore());

        Assert.That(Status((await none.GetRun("run_1", CancellationToken.None)).Result),
            Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
        Assert.That(Status((await empty.GetRun("ghost", CancellationToken.None)).Result),
            Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(Status((await empty.DeleteRun("ghost", CancellationToken.None)).Result),
            Is.EqualTo(StatusCodes.Status404NotFound));
    }

    /// <summary>The handlers over this store, with no review or judge-check store.</summary>
    private static StoredRunHandlers Handlers(IBenchmarkStore? store) =>
        new(store, new BenchmarkRunner(new BenchmarkCatalog(), ApiTestHost.RepoRoot, "http://127.0.0.1:1/"), null,
            null);

    /// <summary>The status an error answer carries.</summary>
    private static int? Status(IResult answer) => (answer as JsonHttpResult<ApiError>)?.StatusCode;
}
