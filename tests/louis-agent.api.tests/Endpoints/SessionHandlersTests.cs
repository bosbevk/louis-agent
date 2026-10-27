using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Sessions;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Api.Endpoints.Sessions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>The session handlers declare what they answer (ADR 0045).</summary>
public class SessionHandlersTests
{
    /// <summary>What each handler says it answers.</summary>
    private static IEnumerable<TestCaseData> Declared()
    {
        yield return new TestCaseData("CreateSession",
            typeof(Task<Results<CreatedJson<SessionCreatedReply>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GetSession",
            typeof(Results<JsonHttpResult<SessionReply>, JsonHttpResult<ApiError>>));
        yield return new TestCaseData("DeleteSession",
            typeof(Results<JsonHttpResult<SessionDeletedReply>, JsonHttpResult<ApiError>>));
        yield return new TestCaseData("CancelSession",
            typeof(Results<JsonHttpResult<SessionCancelledReply>, JsonHttpResult<ApiError>>));
        yield return new TestCaseData("SendMessage",
            typeof(Results<ServerSentEventsResult<string>, JsonHttpResult<ApiError>>));
    }

    /// <summary>A handler's signature names every answer it can give.</summary>
    [TestCaseSource(nameof(Declared))]
    public void AHandler_DeclaresItsAnswers(string handler, Type answers)
    {
        Assert.That(typeof(SessionHandlers).GetMethod(handler)?.ReturnType, Is.EqualTo(answers), handler);
    }
}
