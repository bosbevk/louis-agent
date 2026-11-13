using System.Net;
using System.Reflection;
using System.Text;
using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Admin.Reviews.Queue;
using LouisAgent.Web.Evals.Components.Head;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StudioPage = LouisAgent.Web.Evals.Pages.Running.Evals;

namespace LouisAgent.Web.Evals.Tests.Components.Head;

/// <summary>
/// F144: the eval studio inside the admin's frame. Its page moves from the admin's root to <c>/admin/evals</c>, since
/// the root is the admin's home; its links follow, and its card on the home says how many evals there are, how many
/// answers wait for a grade and which run was last.
/// </summary>
public class EvalsCardTests
{
    /// <summary>Two evals, one run between them, as the evals page's own test reads them.</summary>
    private static readonly string Two = SampleIdentity.Fill(SampleEval.Fill(SampleMode.Fill(File.ReadAllText(
        Path.Combine(TestPaths.RepoRoot, "src/tests/web/louis-agent.web.evals.tests", "Pages", "Running",
            "Evals.two.json")))));

    /// <summary>A context over a stub API answering the evals and the review queue, or refusing everything.</summary>
    private static BunitContext Context(HttpStatusCode status = HttpStatusCode.OK)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var stub = new StubHandler(request => new HttpResponseMessage(status)
        {
            Content = new StringContent(request.RequestUri!.AbsolutePath.EndsWith("/queue")
                ? """{"items":[],"total":3}"""
                : Two, Encoding.UTF8, "application/json")
        });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") },
            settings));
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ReviewQueueState());
        return ctx;
    }

    /// <summary>The studio's page is at /admin/evals, not the admin's root.</summary>
    [Test]
    public void TheStudio_IsAtAdminEvals() =>
        Assert.That(typeof(StudioPage).GetCustomAttribute<RouteAttribute>()!.Template, Is.EqualTo("/admin/evals"));

    /// <summary>The sidebar's Evals link goes to the studio.</summary>
    [Test]
    public void TheSidebarsEvalsLink_GoesToTheStudio()
    {
        using var ctx = Context();

        var links = ctx.Render<EvalLinks>();

        Assert.That(links.Find("a.side-link").GetAttribute("href"), Is.EqualTo("admin/evals"));
    }

    /// <summary>
    /// How many evals, how many answers wait for a grade, the last run with the way to it, and Open to the studio.
    /// </summary>
    [Test]
    public void TheCard_CountsTheEvalsAndTheAnswers_AndNamesTheLastRun()
    {
        using var ctx = Context();

        var card = ctx.Render<EvalsCard>();

        card.WaitForAssertion(() => Assert.That(card.FindAll(".evals-last a"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.Multiple(() =>
        {
            Assert.That(card.Find(".admin-card-title").TextContent, Is.EqualTo("Evals"));
            Assert.That(card.FindAll(".figure-value").Select(value => value.TextContent),
                Is.EqualTo(new[] { "2", "3" }));
            Assert.That(card.FindAll(".figure-name").Select(name => name.TextContent),
                Is.EqualTo(new[] { "evals", "to review" }));
            Assert.That(card.Find(".evals-last").TextContent, Does.Contain(SampleEval.Beta));
            Assert.That(card.Find(".evals-last a").GetAttribute("href"), Does.StartWith("admin/runs/"));
            Assert.That(card.Find("a.admin-card-open").GetAttribute("href"), Is.EqualTo("admin/evals"));
        });
    }

    /// <summary>A refused API leaves the card quiet, the way to the studio still there.</summary>
    [Test]
    public void ARefusedApi_LeavesTheCardQuiet()
    {
        using var ctx = Context(HttpStatusCode.Unauthorized);

        var card = ctx.Render<EvalsCard>();

        card.WaitForAssertion(() => Assert.That(card.FindAll(".admin-card-body .muted"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.That(card.FindAll("a.admin-card-open"), Has.Count.EqualTo(1));
    }
}
