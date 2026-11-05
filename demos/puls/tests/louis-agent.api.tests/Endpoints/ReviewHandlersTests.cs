using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Endpoints.Admin.Reviews;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>The review handlers declare what they answer (ADR 0045).</summary>
public class ReviewHandlersTests
{
    /// <summary>What each handler says it answers.</summary>
    private static IEnumerable<TestCaseData> Declared()
    {
        yield return new TestCaseData("PutReview",
            typeof(Task<Results<JsonHttpResult<HumanGradeRow>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GetReviews",
            typeof(Task<Results<JsonHttpResult<ReviewsReply>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GetLabels",
            typeof(Task<Results<FileContentHttpResult, JsonHttpResult<ApiError>>>));
    }

    /// <summary>A handler's signature names every answer it can give.</summary>
    [TestCaseSource(nameof(Declared))]
    public void AHandler_DeclaresItsAnswers(string handler, Type answers) =>
        AssertDeclared("ReviewHandlers", handler, answers);

    /// <summary>The queue's handler (F53.3) names its answers too.</summary>
    [Test]
    public void TheQueueHandler_DeclaresItsAnswers() =>
        AssertDeclared("ReviewQueueHandlers", "GetQueue",
            typeof(Task<Results<JsonHttpResult<ReviewQueueReply>, JsonHttpResult<ApiError>>>));

    /// <summary>The named class's handler returns exactly these answers.</summary>
    private static void AssertDeclared(string className, string handler, Type answers)
    {
        var type = typeof(ApiErrors).Assembly.GetTypes().SingleOrDefault(t => t.Name == className);

        Assert.That(type?.GetMethod(handler)?.ReturnType, Is.EqualTo(answers), handler);
    }
}
