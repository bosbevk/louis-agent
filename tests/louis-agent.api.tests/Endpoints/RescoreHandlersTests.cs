using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Endpoints.Answers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// The re-score handler declares what it answers (ADR 0045). Starting one runs the eval runner, which the integration
/// tests do.
/// </summary>
public class RescoreHandlersTests
{
    /// <summary>A re-score answers a 201 with the new run, or an error.</summary>
    [Test]
    public void ARescore_DeclaresItsAnswers()
    {
        var type = typeof(ApiErrors).Assembly.GetTypes().SingleOrDefault(t => t.Name == "RescoreHandlers");

        Assert.That(type?.GetMethod("Rescore")?.ReturnType,
            Is.EqualTo(typeof(Task<Results<CreatedJson<LiveRunRow>, JsonHttpResult<ApiError>>>)));
    }
}
