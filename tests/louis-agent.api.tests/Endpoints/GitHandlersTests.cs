using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Git.Replies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Each git handler declares what it can answer (ADR 0045). Calling them runs git, which the integration tests do.
/// </summary>
public class GitHandlersTests
{
    private const string Api = "louis-agent.api";

    /// <summary>Each git handler and what it says it answers.</summary>
    private static IEnumerable<TestCaseData> Declared()
    {
        var branches = typeof(Task<Results<JsonHttpResult<BranchesReply>, JsonHttpResult<ApiError>>>);
        yield return new TestCaseData("GitBranchesHandlers", "GetBranches", branches);
        yield return new TestCaseData("GitBranchesHandlers", "GetBranch",
            typeof(Task<Results<JsonHttpResult<BranchDiffReply>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GitBranchesHandlers", "Merge", branches);
        yield return new TestCaseData("GitBranchesHandlers", "DeleteBranch", branches);

        var status = typeof(Task<Results<JsonHttpResult<StatusReply>, JsonHttpResult<ApiError>>>);
        yield return new TestCaseData("GitChangesHandlers", "GetStatus", status);
        yield return new TestCaseData("GitChangesHandlers", "GetDiff",
            typeof(Task<Results<JsonHttpResult<DiffReply>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GitChangesHandlers", "Stage", status);
        yield return new TestCaseData("GitChangesHandlers", "Unstage", status);
        yield return new TestCaseData("GitChangesHandlers", "Discard", status);

        yield return new TestCaseData("GitCommitsHandlers", "Commit",
            typeof(Task<Results<Created<StatusReply>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GitCommitsHandlers", "GetCommits",
            typeof(Task<Results<JsonHttpResult<CommitsReply>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GitCommitsHandlers", "GetCommit",
            typeof(Task<Results<JsonHttpResult<CommitReply>, JsonHttpResult<ApiError>>>));
    }

    /// <summary>A handler's signature names every answer it can give.</summary>
    [TestCaseSource(nameof(Declared))]
    public void AHandler_DeclaresItsAnswers(string handlers, string handler, Type answers)
    {
        var type = Type.GetType($"LouisAgent.Api.Endpoints.Git.{handlers}, {Api}");

        Assert.That(type?.GetMethod(handler)?.ReturnType, Is.EqualTo(answers), $"{handlers}.{handler}");
    }
}
