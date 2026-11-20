using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Workspace;
using LouisAgent.Api.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>The workspace handlers declare what they answer (ADR 0045).</summary>
public class WorkspaceHandlersTests
{
    /// <summary>What each handler says it answers.</summary>
    private static IEnumerable<TestCaseData> Declared()
    {
        yield return new TestCaseData("GetEntries",
            typeof(Results<JsonHttpResult<EntriesReply>, JsonHttpResult<ApiError>>));
        yield return new TestCaseData("GetFile", typeof(Results<JsonHttpResult<FileReply>, JsonHttpResult<ApiError>>));
    }

    /// <summary>A handler's signature names every answer it can give.</summary>
    [TestCaseSource(nameof(Declared))]
    public void AHandler_DeclaresItsAnswers(string handler, Type answers)
    {
        var type = typeof(ApiErrors).Assembly.GetTypes().SingleOrDefault(t => t.Name == "WorkspaceHandlers");

        Assert.That(type?.GetMethod(handler)?.ReturnType, Is.EqualTo(answers), handler);
    }
}
