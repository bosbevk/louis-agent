using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using LouisAgent.Web.Pages.Admin.Running;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Testing;
using LouisAgent.Web.Usage;
using EvalsPage = LouisAgent.Web.Pages.Admin.Running.Evals;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages.Admin.Running;

/// <summary>
/// Pins what the Evals page shows (F53.2: list and details): every eval the API lists in the list pane with its last
/// result, and the chosen one's details beside it, the eval that ran last when the address names none; an eval that
/// never ran is listed as never run (goldens: <c>Evals.empty.html</c>, <c>Evals.two.html</c>).
/// </summary>
public partial class EvalsPageTests
{
    /// <summary>No evals, and a store that answers.</summary>
    private const string Empty = """{"evals":[],"store":{"configured":true,"error":null}}""";

    /// <summary>
    /// Two evals: the first of an identity with a dataset, two graders and one run; the second the stub, never run.
    /// The JSON is the file beside this test, with the sample names filled in.
    /// </summary>
    private static readonly string Two = Fill(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/tests/web/louis-agent.web.tests", "Pages", "Admin", "Running", "Evals.two.json")));

    /// <summary>With no evals the page says there are none, rather than showing nothing.</summary>
    [Test]
    public void WithNoEvals_ThePageSaysSo()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, Empty);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".evals-empty"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Snapshots.Match("Pages/Admin/Running/Evals.empty.html", Stable(cut.Markup));
    }

    /// <summary>
    /// Two evals in the list, one never run; with no eval in the address the one that ran last is open in the details.
    /// </summary>
    [Test]
    public void TwoEvals_AreListed_AndTheOneThatRanLastIsOpen()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, Two);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".eval-list-item"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(10));
        Assert.That(cut.FindAll(".eval-list-item")[1].TextContent, Does.Contain("never run"));
        Assert.That(cut.Find(".eval-list-item.selected").TextContent, Does.Contain(SampleEval.Beta));
        Assert.That(cut.Find(".eval-details .eval-title code").TextContent, Is.EqualTo(SampleEval.Beta));
        Snapshots.Match("Pages/Admin/Running/Evals.two.html", Stable(cut.Markup));
    }

    /// <summary>The eval the address names is the open one, and the list marks it.</summary>
    [Test]
    public void TheEvalInTheAddress_IsTheOpenOne()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, Two, "admin?eval=stub");

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".eval-details"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.That(cut.Find(".eval-details .eval-title code").TextContent, Is.EqualTo("stub"));
        Assert.That(cut.Find(".eval-list-item.selected").TextContent, Does.Contain("stub"));
    }

    /// <summary>The page at this address over an API that serves this list; it waits for nothing else.</summary>
    private static IRenderedComponent<EvalsPage> Render(BunitContext ctx, string list, string address = "admin")
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

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(address);
        return ctx.Render<EvalsPage>();
    }

    /// <summary>The sample names filled into a test's JSON.</summary>
    private static string Fill(string text) => SampleIdentity.Fill(SampleEval.Fill(SampleMode.Fill(text)));

    /// <summary>
    /// The markup made the same on any machine: the times, which the page writes in the reader's own time zone (CI
    /// runs in UTC).
    /// </summary>
    private static string Stable(string markup) =>
        RowTime().Replace(LocalTime().Replace(markup, "$1, HH:mm"), "$1 HH:mm");

    /// <summary>A day and a time, as the eval card writes them: "24 Oct 2026, 11:00".</summary>
    [GeneratedRegex(@"(\d{1,2} [A-Z][a-z]{2} \d{4}), \d{2}:\d{2}")]
    private static partial Regex LocalTime();

    /// <summary>A day and a time, as a run's row writes them: "2026-10-24 11:00".</summary>
    [GeneratedRegex(@"(\d{4}-\d{2}-\d{2}) \d{2}:\d{2}")]
    private static partial Regex RowTime();
}
