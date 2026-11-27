using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Runs;
using LouisAgent.Evals.Runs.Stops;
using LouisAgent.Evals.Sessions;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Evals.Tests.Sessions;

/// <summary>
/// F126.3.3: a run asks the site's API for the page a conversation names, as the widget would send it. A live play
/// asks the route and keeps the page under its trial; a replay reads the kept page and never asks.
/// </summary>
public class PageClientTests
{
    private const string Api = "http://api.test/";

    private static readonly RunTrial Live = new("20261123-061900-guide", "guide-what-is", 1);

    private static readonly RunTrial Replay = new("20261124-090000-guide", "guide-what-is", 1,
        ReplayOf: "20261123-061900-guide");

    private static readonly PageRequest Landing = new("/", "louis-agent", "The website that answers.",
        [new PageAreaRequest("hero", "The website that answers.")], Focus: "selling");

    private string _folder = "";

    /// <summary>A folder of its own for each test's replay store.</summary>
    [SetUp]
    public void MakeFolder() => _folder = Directory.CreateTempSubdirectory("page-client-").FullName;

    /// <summary>The test's folder goes with it.</summary>
    [TearDown]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    /// <summary>A live play asks the page route for the address, and gets the page.</summary>
    [Test]
    public async Task ALivePlay_AsksTheRoute_AndGetsThePage()
    {
        var handler = new StubHandler(_ => Json(Landing));

        var page = await Client(handler, Store()).FindAsync("/", Live);

        Assert.That(Uri.UnescapeDataString(handler.Requests.Single().Url),
            Is.EqualTo($"{Api}v1/site/page?address=/"));
        Assert.That(JsonOf(page), Is.EqualTo(JsonOf(Landing)));
    }

    /// <summary>After a live play the page is kept under its trial, so its replay reads it and never asks.</summary>
    [Test]
    public async Task AfterALivePlay_ItsReplayReadsTheKeptPage_AndNeverAsks()
    {
        var store = Store();
        await Client(new StubHandler(_ => Json(Landing)), store).FindAsync("/", Live);
        var never = new StubHandler(_ => throw new AssertionException("a replay asked the route"));

        var page = await Client(never, store).FindAsync("/", Replay);

        Assert.That(JsonOf(page), Is.EqualTo(JsonOf(Landing)));
        Assert.That(never.Requests, Is.Empty);
    }

    /// <summary>A replay with no kept page is out of date, as a model's miss is.</summary>
    [Test]
    public void AReplayWithNoKeptPage_IsOutOfDate() =>
        Assert.ThrowsAsync<RecordingOutOfDateException>(() =>
            Client(new StubHandler(_ => Json(Landing)), Store()).FindAsync("/", Replay));

    /// <summary>A page the site doesn't have is a missing page that names its address.</summary>
    [Test]
    public void APageTheSiteDoesntHave_IsMissing_WithItsAddress()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var missing = Assert.ThrowsAsync<PageMissingException>(() => Client(handler, Store()).FindAsync("/gone", Live));

        Assert.That(missing!.Message, Does.Contain("/gone"));
    }

    /// <summary>A live run that keeps nothing still asks the route and gets the page.</summary>
    [Test]
    public async Task WithNoStore_ThePageIsAskedAndReturned()
    {
        var handler = new StubHandler(_ => Json(Landing));

        var page = await Client(handler, null).FindAsync("/", Live);

        Assert.That(JsonOf(page), Is.EqualTo(JsonOf(Landing)));
        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    /// <summary>A replay store in this test's own folder.</summary>
    private ReplayStore Store() => new(_folder);

    /// <summary>The client over a stub of the site's API.</summary>
    private static PageClient Client(StubHandler handler, ReplayStore? store) =>
        new(new HttpClient(handler) { BaseAddress = new Uri(Api) }, store);

    /// <summary>The page as the route answers it.</summary>
    private static HttpResponseMessage Json(PageRequest page) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(page, options: JsonSerializerOptions.Web) };

    /// <summary>A page compared as its JSON, since its lists compare by reference.</summary>
    private static string JsonOf(PageRequest page) => JsonSerializer.Serialize(page, JsonSerializerOptions.Web);
}
