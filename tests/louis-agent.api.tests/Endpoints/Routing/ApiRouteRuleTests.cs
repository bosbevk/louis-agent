using LouisAgent.Api.Endpoints.Routing;
using Microsoft.AspNetCore.Routing;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

// CL20 steps 2 and 5 (ADR 0039, rules 1, 2, 4, 5 and 6): the rules of the API's route table, checked on the real table, not on a
// list kept beside it.
public class ApiRouteRuleTests
{
    // Rules 2 and 4: a path names a resource, and the method says what is done to it, so no segment is a verb for it.
    private static readonly string[] Verbs = ["delete", "remove", "create", "add", "new", "update", "edit", "set"];

    // Rule 5: an action that is no create, read, replace or delete is the last segment of a POST.
    private static readonly string[] Actions = ["cancel", "stage", "unstage", "discard", "merge"];

    /// <summary>A route's literal segments: its pattern split on slashes, without the parameters (<c>{id}</c>, <c>{**name}</c>).</summary>
    private static string[] Words(RouteEndpoint endpoint) =>
        ApiTestHost.Pattern(endpoint).Split('/', StringSplitOptions.RemoveEmptyEntries).Where(s => !s.StartsWith('{')).ToArray();

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

    [Test]
    public void NoSegment_IsAVerb()
    {
        using var app = ApiTestHost.Build();

        var wrong = ApiTestHost.Routes(app).Where(e => Words(e).Intersect(Verbs).Any()).Select(ApiTestHost.Line).Order(StringComparer.Ordinal).ToList();

        Assert.That(wrong, Is.Empty, "A path names a resource, and the method is the verb (ADR 0039, rules 2 and 4):\n  " + string.Join("\n  ", wrong));
    }

    [Test]
    public void AnAction_IsOnlyTheLastSegmentOfAPost()
    {
        using var app = ApiTestHost.Build();

        var wrong = new List<string>();
        foreach (var endpoint in ApiTestHost.Routes(app))
        {
            var segments = ApiTestHost.Pattern(endpoint).Split('/', StringSplitOptions.RemoveEmptyEntries);
            bool isPost = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.SequenceEqual(new[] { "POST" }) == true;
            for (int i = 0; i < segments.Length; i++)
                if (Actions.Contains(segments[i]) && !(i == segments.Length - 1 && isPost)) wrong.Add(ApiTestHost.Line(endpoint));
        }

        Assert.That(wrong, Is.Empty, "An action is the last segment of a POST (ADR 0039, rule 5):\n  " + string.Join("\n  ", wrong));
    }

    [Test]
    public void NoResource_IsNamedBothSingularAndPlural()
    {
        using var app = ApiTestHost.Build();
        var words = ApiTestHost.Routes(app).SelectMany(Words).ToHashSet();

        var pairs = words
            .SelectMany(w => new[] { w + "s", w + "es", w.EndsWith('y') ? w[..^1] + "ies" : null }.OfType<string>().Where(words.Contains).Select(plural => $"{w} and {plural}"))
            .Order(StringComparer.Ordinal).ToList();

        Assert.That(pairs, Is.Empty, "A resource has one name, plural, for the collection and its items (ADR 0039, rule 2): " + string.Join(", ", pairs));
    }
}
