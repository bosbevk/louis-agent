using LouisAgent.Api.Endpoints.Routing;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

// CL20 step 2 (ADR 0039, rules 1 and 6): the rules of the API's route table, checked on the real table, not on a list kept beside it.
public class ApiRouteRuleTests
{
    [Test]
    public void EveryRouteButHealth_IsUnderV1()
    {
        using var app = ApiTestHost.Build();

        var outside = ApiTestHost.Routes(app).Select(ApiTestHost.Pattern)
            .Where(pattern => pattern != "/health" && !pattern.StartsWith("/v1/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.That(outside, Is.Empty, "Every route but /health is under /v1 (ADR 0039, rule 1); these are not:\n  " + string.Join("\n  ", outside));
    }

    [Test]
    public void TheOpenRoutes_AreTheProbeThePricesAndTheRates()
    {
        using var app = ApiTestHost.Build();

        var open = ApiTestHost.Routes(app).Where(e => ApiTestHost.Access(e) == RouteAccess.Open).Select(ApiTestHost.Line).Order(StringComparer.Ordinal);

        Assert.That(open, Is.EqualTo(new[] { "GET /health", "GET /v1/prices", "GET /v1/rates", "GET /v1/rates/display" }),
            "A route is closed until the code opens it (ADR 0039, rule 6): only the probe, the prices and the rates are open.");
    }

    [Test]
    public void TheAdminRoutes_AreBehindTheAdminKey()
    {
        using var app = ApiTestHost.Build();

        var wrong = ApiTestHost.Routes(app)
            .Where(e => (ApiTestHost.Access(e) == RouteAccess.AdminKey) != ApiTestHost.Pattern(e).StartsWith("/v1/admin/", StringComparison.Ordinal))
            .Select(ApiTestHost.Line).ToList();

        Assert.That(wrong, Is.Empty, "A route is AdminKey if and only if it is under /v1/admin/:\n  " + string.Join("\n  ", wrong));
    }
}
