using System.Net;
using System.Text;
using LouisAgent.Testing;
using LouisAgent.Puls.Components.Admin.Studio;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Studio;

/// <summary>
/// An eval's drafts on its card: folded under their count until opened, then read from the API (each sample with its
/// scenario file, each transcript with its file name), and each one deleted under its eval, after which the list is
/// read again and the page told, so the count it shows is the API's.
/// </summary>
public class DraftListTests
{
    /// <summary>The cook eval's drafts as the API lists them: one sample, one transcript.</summary>
    private const string Drafts =
        """
        {"samples":[{"id":"s1","eval":"cook","identity":"puls","run":"run1","conversation":{"mode":"beta","repeat":1,"task":"cook-af"},"sample_id":"cook-af-2","question":"Wat kan ek kook?","json":"{\"id\":\"cook-af-2\"}","author":"Louis","at":"2026-11-03T10:00:00Z"}],
         "transcripts":[{"id":"t1","eval":"cook","identity":"puls","run":"run1","conversation":{"mode":"beta","repeat":1,"task":"cook-af"},"kind":"bad","reason":"no-shop","file_name":"cook-af.bad.no-shop.txt","lines":7,"text":"","author":"Louis","at":"2026-11-03T10:00:00Z"}]}
        """;

    /// <summary>The list as rendered, the stub API behind it, and how often the page was told of a change.</summary>
    private sealed record Rendered(IRenderedComponent<DraftList> Cut, StubHandler Handler, Func<int> Changes);

    /// <summary>Renders the cook eval's two drafts, folded.</summary>
    private static Rendered Render()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(request => request.Method == HttpMethod.Delete
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Drafts, Encoding.UTF8, "application/json")
            });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings()));

        int changes = 0;
        var cut = ctx.Render<DraftList>(p => p.Add(x => x.Eval, "cook").Add(x => x.Count, 2)
            .Add(x => x.OnChanged, () => changes++));
        return new Rendered(cut, handler, () => changes);
    }

    /// <summary>Folded, it shows the count and reads nothing.</summary>
    [Test]
    public void Folded_ItShowsTheCount_AndReadsNothing()
    {
        var (cut, handler, _) = Render();

        Assert.That(cut.Find(".drafts-open").TextContent, Does.Contain("2 drafts"));
        Assert.That(handler.Requests, Is.Empty);
    }

    /// <summary>Opened, it reads the eval's drafts and shows each sample and transcript.</summary>
    [Test]
    public void Opened_ItShowsEachDraft()
    {
        var (cut, handler, _) = Render();

        cut.Find(".drafts-open").Click();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".draft-row"), Has.Count.EqualTo(2)));
        Assert.That(handler.Requests[0].Url, Is.EqualTo("http://localhost/v1/admin/evals/cook/drafts"));
        Assert.That(cut.Find(".draft-sample").TextContent, Does.Contain("cook-af-2").And.Contain("Wat kan ek kook?"));
        Assert.That(cut.Find(".draft-sample pre").TextContent, Does.Contain("\"id\":\"cook-af-2\""));
        Assert.That(cut.Find(".draft-transcript").TextContent, Does.Contain("cook-af.bad.no-shop.txt"));
    }

    /// <summary>Deleting a draft deletes it under its eval, reads the list again and tells the page.</summary>
    [Test]
    public void Deleting_DeletesUnderTheEval_ReadsAgain_AndTellsThePage()
    {
        var (cut, handler, changes) = Render();
        cut.Find(".drafts-open").Click();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".draft-row"), Has.Count.EqualTo(2)));

        cut.Find(".draft-transcript .draft-delete").Click();

        cut.WaitForAssertion(() => Assert.That(handler.Requests, Has.Count.EqualTo(3)));
        Assert.That((handler.Requests[1].Method, handler.Requests[1].Url),
            Is.EqualTo((HttpMethod.Delete, "http://localhost/v1/admin/evals/cook/drafts/t1")));
        Assert.That(handler.Requests[2].Method, Is.EqualTo(HttpMethod.Get), "the list is read again");
        Assert.That(changes(), Is.EqualTo(1));
    }
}
