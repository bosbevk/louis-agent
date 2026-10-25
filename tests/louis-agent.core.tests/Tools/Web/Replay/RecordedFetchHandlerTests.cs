using System.Net;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Web.Fetching;
using LouisAgent.Core.Tools.Web.Quotes;
using LouisAgent.Core.Tools.Web.Replay;
using static LouisAgent.Core.Tests.Tools.Web.FakeWeb;

namespace LouisAgent.Core.Tests.Tools.Web.Replay;

// F18 step 4b.3: a fetch, recorded and replayed at the HttpClient's handler, outside the SSRF guard. Written first: RecordedFetchHandler
// threw NotImplementedException on the red run, and PageFetcher took no store.
public class RecordedFetchHandlerTests
{
    private const string Page = "https://example.com/guide";

    private static IDisposable Using(WebReplayMode mode, string? of) => SessionReplay.Use(new SessionReplay(mode, of, Model: false));

    private static Task<System.Net.IPAddress[]> NoDns(string host, CancellationToken _) => throw new InvalidOperationException("DNS must not be used");

    private static HttpResponseMessage NoNetwork(HttpRequestMessage _) => throw new InvalidOperationException("the network must not be used");

    private static WebTools ToolsOn(StubHandler stub, ReplayFolder folder, Func<string, CancellationToken, Task<System.Net.IPAddress[]>>? dns = null) =>
        new(new FakeSearch(), new PageFetcher(stub, dns ?? PublicDns, folder.Store), renderer: null);

    private static async Task<(string Recorded, string Replayed)> RecordThenReplayAsync(Func<HttpRequestMessage, HttpResponseMessage> respond, ReplayFolder folder,
        Func<WebTools, Task<string>> call)
    {
        string recorded, replayed;
        using (Using(WebReplayMode.Record, "t")) recorded = await call(ToolsOn(new StubHandler(respond), folder));
        var dead = new StubHandler(NoNetwork);
        using (Using(WebReplayMode.Replay, "t")) replayed = await call(ToolsOn(dead, folder, NoDns));
        Assert.That(dead.Requests, Is.Empty, "a replay must not use the network");
        return (recorded, replayed);
    }

    [Test]
    public async Task RecordingThenReplayingAnHtmlPage_WithNoNetworkAndNoDns_GivesTheSameToolText()
    {
        using var folder = new ReplayFolder();

        var (recorded, replayed) = await RecordThenReplayAsync(_ => Html(ArticleHtml), folder, t => t.FetchUrl(Page));

        Assert.That(recorded, Does.Contain("Install").And.Contain("untrusted web content"));
        Assert.That(replayed, Is.EqualTo(recorded));
    }

    [Test]
    public async Task RecordingThenReplayingAPdf_GivesTheSameToolText()
    {
        using var folder = new ReplayFolder();
        byte[] pdf = LouisAgent.Core.Tests.Tools.Documents.DocumentFiles.Pdf(["Jaarverslag 2026"]);

        var (recorded, replayed) = await RecordThenReplayAsync(
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(pdf) { Headers = { ContentType = new("application/pdf") } } },
            folder, t => t.FetchUrl("https://example.com/verslag.pdf"));

        Assert.That(recorded, Does.Contain("Jaarverslag 2026"));
        Assert.That(replayed, Is.EqualTo(recorded));
    }

    [Test]
    public async Task ARedirect_ReplaysToTheSameTextAndFinalAddress_WithOneIndexLinePerHop()
    {
        using var folder = new ReplayFolder();
        HttpResponseMessage Respond(HttpRequestMessage request) => request.RequestUri!.AbsolutePath == "/old"
            ? new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("https://example.com/new") } }
            : Html(ArticleHtml);

        var (recorded, replayed) = await RecordThenReplayAsync(Respond, folder, t => t.FetchUrl("https://example.com/old"));

        Assert.That(recorded, Does.Contain("https://example.com/new"));
        Assert.That(replayed, Is.EqualTo(recorded));
        Assert.That(File.ReadAllLines(folder.Trials.Single()), Has.Length.EqualTo(2));
    }

    [Test]
    public async Task AnHttpErrorStatus_ReplaysAsTheSameText()
    {
        using var folder = new ReplayFolder();

        var (recorded, replayed) = await RecordThenReplayAsync(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = "Not Found" }, folder, t => t.FetchUrl(Page));

        Assert.That(recorded, Does.Contain("HTTP 404"));
        Assert.That(replayed, Is.EqualTo(recorded));
    }

    [Test]
    public async Task VerifyQuote_RecordedAndReplayed_GivesTheSameText()
    {
        using var folder = new ReplayFolder();
        QuoteTools Quote(StubHandler stub, Func<string, CancellationToken, Task<System.Net.IPAddress[]>> dns) => new(new PageFetcher(stub, dns, folder.Store));

        string recorded, replayed;
        using (Using(WebReplayMode.Record, "t")) recorded = await Quote(new StubHandler(_ => Html("<p>Eggs R34.99</p>")), PublicDns).VerifyQuote(Page, "R34.99", "Eggs");
        var dead = new StubHandler(NoNetwork);
        using (Using(WebReplayMode.Replay, "t")) replayed = await Quote(dead, NoDns).VerifyQuote(Page, "R34.99", "Eggs");

        Assert.That(recorded, Does.StartWith("Found"));
        Assert.That(replayed, Is.EqualTo(recorded));
        Assert.That(dead.Requests, Is.Empty);
    }

    [Test]
    public async Task ARecordingWhoseDnsAnswersAPrivateAddress_IsRefusedAsWithoutAStore_AndSavesNothing()
    {
        using var folder = new ReplayFolder();
        Task<System.Net.IPAddress[]> Private(string host, CancellationToken _) => Task.FromResult(new[] { System.Net.IPAddress.Parse("10.0.0.5") });
        var stub = new StubHandler(_ => Html("secret"));
        string withoutStore = await new WebTools(new FakeSearch(), new PageFetcher(new StubHandler(_ => Html("secret")), Private), renderer: null).FetchUrl(Page);

        string recording;
        using (Using(WebReplayMode.Record, "t")) recording = await ToolsOn(stub, folder, Private).FetchUrl(Page);

        Assert.That(recording, Is.EqualTo(withoutStore));
        Assert.That(stub.Requests, Is.Empty);
        Assert.That(folder.Entries, Is.Empty);
    }

    [Test]
    public async Task WithNoTrial_ItPassesThrough_TheGuardStillChecks_AndTheFolderStaysEmpty()
    {
        using var folder = new ReplayFolder();
        var stub = new StubHandler(_ => Html(ArticleHtml));
        string text = await ToolsOn(stub, folder).FetchUrl(Page);
        Task<System.Net.IPAddress[]> Private(string host, CancellationToken _) => Task.FromResult(new[] { System.Net.IPAddress.Parse("10.0.0.5") });
        var refused = new StubHandler(_ => Html("secret"));

        string blocked = await ToolsOn(refused, folder, Private).FetchUrl(Page);

        Assert.That(text, Does.Contain("Install"));
        Assert.That(blocked, Does.Contain("Error"));
        Assert.That(refused.Requests, Is.Empty);
        Assert.That(folder.Entries, Is.Empty);
    }

    [Test]
    public async Task TheScrub_NoHeaderOfTheRequestOrTheResponse_ReachesTheFolder()
    {
        using var folder = new ReplayFolder();
        var response = Html("<p>hello</p>");
        response.Headers.TryAddWithoutValidation("Set-Cookie", "session=SECRET-COOKIE");
        response.Headers.TryAddWithoutValidation("X-Api-Key", "SECRET-KEY");
        using var invoker = new HttpMessageInvoker(new RecordedFetchHandler(folder.Store) { InnerHandler = new StubHandler(_ => response) });
        var request = new HttpRequestMessage(HttpMethod.Get, Page);
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer SECRET-TOKEN");

        using (Using(WebReplayMode.Record, "t")) (await invoker.SendAsync(request, CancellationToken.None)).Dispose();

        string[] files = Directory.GetFiles(folder.Path, "*", SearchOption.AllDirectories);
        Assert.That(files, Is.Not.Empty);
        Assert.That(files.Select(File.ReadAllText), Has.None.Contain("SECRET"));
    }

    [Test]
    public async Task ABodyOverTheLimit_GivesTheTooLargeText_AndSavesNothing()
    {
        using var folder = new ReplayFolder();
        var big = new byte[6 * 1024 * 1024];

        string text;
        using (Using(WebReplayMode.Record, "t"))
            text = await ToolsOn(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(big) }), folder).FetchUrl(Page);

        Assert.That(text, Does.Contain("larger than 5 MB"));
        Assert.That(folder.Entries, Is.Empty);
    }

    [Test]
    public async Task AFailure_IsNotRecorded_SoItsReplayIsAMiss()
    {
        using var folder = new ReplayFolder();
        string recorded;
        using (Using(WebReplayMode.Record, "t"))
            recorded = await ToolsOn(new StubHandler(_ => throw new HttpRequestException("connection reset")), folder).FetchUrl(Page);
        Assert.That(recorded, Does.Contain("connection reset"));
        Assert.That(folder.Entries, Is.Empty);

        string replayed;
        using (Using(WebReplayMode.Replay, "t")) replayed = await ToolsOn(new StubHandler(NoNetwork), folder, NoDns).FetchUrl(Page);
        Assert.That(replayed, Does.Contain("was not read in the recorded run"));
    }

    [Test]
    public async Task AMiss_CarriesTheReplayMissText_AndTheStubIsNeverCalled()
    {
        using var folder = new ReplayFolder();
        var stub = new StubHandler(_ => Html(ArticleHtml));

        string text;
        using (Using(WebReplayMode.Replay, "t")) text = await ToolsOn(stub, folder, NoDns).FetchUrl(Page);

        Assert.That(text, Does.Contain(Page).And.Contain("was not read in the recorded run"));
        Assert.That(stub.Requests, Is.Empty);
        Assert.That(folder.Entries, Is.Empty);
    }

    [Test]
    public async Task AReplayWithNoStore_StillChecksTheAddress()
    {
        // The pre-check is skipped only when the fetcher has a store and the flow is a replay: a fetcher without one resolves as always.
        var resolved = new List<string>();
        Task<System.Net.IPAddress[]> Dns(string host, CancellationToken _) { resolved.Add(host); return PublicDns(host, _); }
        var tools = new WebTools(new FakeSearch(), new PageFetcher(new StubHandler(_ => Html(ArticleHtml)), Dns), renderer: null);

        using (Using(WebReplayMode.Replay, "t")) await tools.FetchUrl(Page);

        Assert.That(resolved, Is.EqualTo(new[] { "example.com" }));
    }
}
