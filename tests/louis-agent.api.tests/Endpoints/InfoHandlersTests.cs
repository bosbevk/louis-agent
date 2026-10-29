using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Info;
using LouisAgent.Api.Endpoints;
using LouisAgent.Core.Pricing.Prices;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>Each info handler declares what it can answer (ADR 0045).</summary>
public class InfoHandlersTests
{
    /// <summary>Each info handler and what it says it answers.</summary>
    private static IEnumerable<TestCaseData> Declared()
    {
        yield return new TestCaseData("GetHealth", typeof(JsonHttpResult<HealthReply>));
        yield return new TestCaseData("GetPrices",
            typeof(Results<JsonHttpResult<PriceTable>, JsonHttpResult<ApiError>>));
        yield return new TestCaseData("GetRates", typeof(Results<ContentHttpResult, JsonHttpResult<ApiError>>));
        yield return new TestCaseData("GetDisplay", typeof(JsonHttpResult<DisplayReply>));
        yield return new TestCaseData("GetToolsets", typeof(JsonHttpResult<ToolsetsReply>));
        yield return new TestCaseData("GetModel", typeof(JsonHttpResult<ModelReply>));
        yield return new TestCaseData("GetRoles", typeof(JsonHttpResult<RolesReply>));
        yield return new TestCaseData("GetLlmProfiles", typeof(JsonHttpResult<LlmProfilesReply>));
    }

    /// <summary>A handler's signature names every answer it can give.</summary>
    [TestCaseSource(nameof(Declared))]
    public void AHandler_DeclaresItsAnswers(string handler, Type answers)
    {
        var type = typeof(ApiErrors).Assembly.GetTypes().SingleOrDefault(t => t.Name == "InfoHandlers");

        Assert.That(type?.GetMethod(handler)?.ReturnType, Is.EqualTo(answers), handler);
    }
}
