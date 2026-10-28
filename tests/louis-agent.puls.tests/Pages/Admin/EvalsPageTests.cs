using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using LouisAgent.Puls.Pages.Admin;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using LouisAgent.Web.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Pages.Admin;

/// <summary>
/// Pins what the Evals page shows: every eval the API lists, what it is, and its runs; an eval that never ran is
/// listed as never run (goldens: <c>Evals.empty.html</c>, <c>Evals.two.html</c>).
/// </summary>
public partial class EvalsPageTests
{
    /// <summary>No evals, and a store that answers.</summary>
    private const string Empty = """{"evals":[],"store":{"configured":true,"error":null}}""";

    /// <summary>
    /// Two evals: the first of an identity with a dataset, two graders and one run; the second the stub, never run.
    /// The JSON is the file beside this test, with the sample names filled in.
    /// </summary>
    private static readonly string Two = Fill(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "tests",
        "louis-agent.puls.tests", "Pages", "Admin", "Evals.two.json")));

    /// <summary>With no evals the page says there are none, rather than showing nothing.</summary>
    [Test]
    public void WithNoEvals_ThePageSaysSo()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, Empty);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".evals-empty"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Snapshots.Match("louis-agent.puls.tests", "Pages/Admin/Evals.empty.html", Stable(cut.Markup));
    }

    /// <summary>Two evals: one with its run, one never run, each with what it is.</summary>
    [Test]
    public void TwoEvals_OneNeverRun_AreBothListed()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, Two);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".eval-card"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(10));
        Assert.That(cut.FindAll(".eval-card")[1].TextContent, Does.Contain("never run"));
        Snapshots.Match("louis-agent.puls.tests", "Pages/Admin/Evals.two.html", Stable(cut.Markup));
    }

    /// <summary>The page over an API that serves this list; it waits for nothing else.</summary>
    private static IRenderedComponent<Evals> Render(BunitContext ctx, string list)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/v1/admin/evals")
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(list, Encoding.UTF8, "application/json")
            }
            : new HttpResponseMessage(HttpStatusCode.NotFound));

        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));

        return ctx.Render<Evals>();
    }

    /// <summary>The sample names filled into a test's JSON.</summary>
    private static string Fill(string text) => SampleIdentity.Fill(SampleEval.Fill(SampleMode.Fill(text)));

    /// <summary>
    /// The markup made the same on any machine: the times, which the page writes in the reader's own time zone (CI
    /// runs in UTC).
    /// </summary>
    private static string Stable(string markup) => LocalTime().Replace(markup, "$1 HH:mm");

    /// <summary>A date and a time of day, "2026-10-24 11:00".</summary>
    [GeneratedRegex(@"(\d{4}-\d{2}-\d{2}) \d{2}:\d{2}")]
    private static partial Regex LocalTime();
}
