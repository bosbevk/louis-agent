using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock.Website;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Showroom;

/// <summary>
/// F127.1: every showroom conversation plays to its end in the recorded chat, read from its files as the site serves
/// them, so a demo page never hangs on a recording (a guesthouse page froze in Chrome on 2026-11-10).
/// </summary>
public partial class ShowroomRecordingsTests
{
    /// <summary>Where the site serves the showroom's recordings from.</summary>
    private static readonly string Recordings =
        Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site/wwwroot/recordings/showroom");

    /// <summary>A recording's page list is read as the site's script writes it, in camelCase.</summary>
    private static readonly JsonSerializerOptions PageJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// A ShowPage call in a recorded stream and its address: it matches
    /// <c>"name":"ShowPage","input":{"path":"/stay"</c> and takes <c>/stay</c>, and doesn't match
    /// <c>"name":"SendLead","input":{"name":"Anna"</c>.
    /// </summary>
    [GeneratedRegex("\"name\":\"ShowPage\",\"input\":\\{\"path\":\"(?<path>[^\"]+)\"")]
    private static partial Regex ShowPageCall();

    /// <summary>What a recording's page lists: its places, each by its address and heading.</summary>
    private sealed record RecordedPage(List<RecordedArea> Areas);

    /// <summary>One place on a recorded page; a page of the website says "(page)" in its heading.</summary>
    private sealed record RecordedArea(string Id, string Heading);

    /// <summary>
    /// A business's agent moves between pages and sections (F127.2, Louis: "agents must demonstrate navigation between
    /// pages and sections"): every place its recording shows is one its website has, on at least two of its pages,
    /// and at least one is a section or a card, not a whole page. The law practice's goes to Wills and estates (Louis:
    /// "navigate to the will and estates page").
    /// </summary>
    [TestCase("wheatfield-kitchen", "/menu")]
    [TestCase("koringland-attorneys", "/practice/wills")]
    [TestCase("seemeeu-guesthouse", "/rooms")]
    public void ABusinesssRecording_MovesBetweenPagesAndSections(string business, string onePage)
    {
        var website = JsonSerializer.Deserialize<RecordedPage>(File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            $"src/site/recordings/pages/{business}.json")), PageJson)!;
        var pages = website.Areas.Where(area => area.Heading.EndsWith("(page)", StringComparison.Ordinal))
            .Select(area => area.Id).Prepend(MockPaths.Home).ToList();
        var places = website.Areas.Select(area => area.Id).ToHashSet();

        var shown = Enumerable.Range(1, 4)
            .Select(n => File.ReadAllText(Path.Combine(Recordings, $"{business}-{n}.sse")))
            .SelectMany(stream => ShowPageCall().Matches(stream).Select(call => call.Groups["path"].Value))
            .ToList();
        var onPages = shown.Select(path => MockPaths.PageOf(path, pages)).Distinct().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(shown, Is.Not.Empty, "its agent shows its website");
            Assert.That(shown.Where(path => !places.Contains(path)), Is.Empty, "only places its website has");
            Assert.That(onPages, Has.Count.GreaterThanOrEqualTo(2), "on at least two of its pages");
            Assert.That(shown.Where(path => !pages.Contains(path)), Is.Not.Empty,
                "a section or a card, not only pages");
            Assert.That(onPages, Does.Contain(onePage));
        });
    }

    /// <summary>A business's conversation, played with no waits, reaches its ending with every turn answered.</summary>
    [TestCase("wheatfield-kitchen")]
    [TestCase("koringland-attorneys")]
    [TestCase("seemeeu-guesthouse")]
    public void ABusinesssConversation_PlaysToItsEnd(string business)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(new HttpClient(new StubHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(File.ReadAllText(Path.Combine(Recordings,
                Path.GetFileName(request.RequestUri!.AbsolutePath))))
        }))
        { BaseAddress = new Uri("http://site.test/") });
        var turns = Enumerable.Range(1, 4)
            .Select(n => new RecordedTurn($"Question {n}", $"recordings/showroom/{business}-{n}.sse")).ToList();

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, turns)
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.PlayAgain, "Again"));

        cut.WaitForElement(".recorded-end", TimeSpan.FromSeconds(10));
        Assert.That(cut.FindAll(".turn"), Has.Count.EqualTo(4));
    }
}
