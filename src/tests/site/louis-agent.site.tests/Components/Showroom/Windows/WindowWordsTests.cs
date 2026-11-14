using System.Text;
using LouisAgent.SessionStream;
using LouisAgent.Site.Components.Showroom;
using LouisAgent.Site.Components.Showroom.Windows;
using LouisAgent.Site.Pages.Agents.Showroom;
using LouisAgent.Site.Tests.Pages;
using LouisAgent.Web.Components.Recorded;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Components.Showroom.Windows;

/// <summary>
/// F134.1: each business's window on the start is a small still picture of its demo, and every word in it is the
/// demo's own. The talk is its recording's: the question the demo's page plays first, one whole sentence of the
/// agent's recorded answer, and a place the agent showed in that turn (Louis's pick of three: one whole sentence,
/// over the whole answer cut off or a shorter recording). The website's words are written again in the window and
/// held here to the demo's page (his pick of three: written again and tested, over one record both read or the
/// mockup's own words).
/// </summary>
public class WindowWordsTests
{
    /// <summary>Where the site's files are served from.</summary>
    private static readonly string Served = Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site/wwwroot");

    /// <summary>What an agent said in one recorded turn, as one text, and the places it showed.</summary>
    private sealed record Heard(string Said, IReadOnlyList<string> Shown);

    /// <summary>The talk a window shows.</summary>
    private static WindowTalk TalkOf(Type window) => window.Name switch
    {
        nameof(SeemeeuWindow) => SiteWindowTalks.Seemeeu,
        nameof(WheatfieldWindow) => SiteWindowTalks.Wheatfield,
        _ => SiteWindowTalks.Koringland,
    };

    /// <summary>A component of the site, drawn by its type.</summary>
    private static IRenderedComponent<IComponent> Draw(BunitContext ctx, Type component) =>
        ctx.Render(builder =>
        {
            builder.OpenComponent(0, component);
            builder.CloseComponent();
        });

    /// <summary>
    /// Reads a recording as the site serves it: the agent's words in order, and every place it showed.
    /// </summary>
    private static async Task<Heard> HearAsync(string recording)
    {
        var said = new StringBuilder();
        var shown = new List<string>();
        if (recording.Length == 0) return new Heard("", shown);

        await using var stream = File.OpenRead(Path.Combine(Served, recording));
        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, CancellationToken.None))
        {
            // The agent's words come a few at a time.
            if (streamEvent is ContentBlockDelta delta) said.Append(WordsOf(delta));

            // A place it showed is a call of its ShowPage tool, with the place's address.
            if (streamEvent is ContentBlockStarted started && PlaceShownBy(started) is { } place) shown.Add(place);
        }

        return new Heard(said.ToString(), shown);
    }

    /// <summary>The words in one piece of the stream. None when the piece is the agent's thinking.</summary>
    private static string WordsOf(ContentBlockDelta delta)
    {
        var piece = delta.Data.GetProperty("delta");
        return piece.GetProperty("type").GetString() == "text_delta" ? piece.GetProperty("text").GetString()! : "";
    }

    /// <summary>The address a block asks the website to show. None when the block is not that call.</summary>
    private static string? PlaceShownBy(ContentBlockStarted started)
    {
        var block = started.Data.GetProperty("content_block");
        if (block.GetProperty("type").GetString() != "tool_use") return null;
        if (block.GetProperty("name").GetString() != "ShowPage") return null;

        return block.GetProperty("input").GetProperty("path").GetString();
    }

    /// <summary>
    /// Whether the sentence stands in the text as a whole one: it opens the text or follows another sentence's end,
    /// and ends with its own full stop.
    /// </summary>
    private static bool IsAWholeSentenceOf(string text, string sentence)
    {
        int at = sentence.Length == 0 ? -1 : text.IndexOf(sentence, StringComparison.Ordinal);
        if (at < 0 || !sentence.EndsWith('.')) return false;

        string before = text[..at].TrimEnd();
        return before.Length == 0 || ".!?".Contains(before[^1]);
    }

    /// <summary>
    /// A window's talk is its recording's. The question is the one the demo's page plays first, which another test
    /// holds to the script that asked it. The answer is one whole sentence of what the agent said in that recording,
    /// word for word. The place is one the agent showed in it.
    /// </summary>
    [TestCase(typeof(SeemeeuWindow), typeof(GuesthouseDemoPage))]
    [TestCase(typeof(WheatfieldWindow), typeof(RestaurantDemoPage))]
    [TestCase(typeof(KoringlandWindow), typeof(LawPracticeDemoPage))]
    public async Task AWindowsTalk_IsItsRecordings(Type window, Type page)
    {
        var talk = TalkOf(window);
        using var ctx = SitePageContext.Create();
        var first = Draw(ctx, page).FindComponent<RecordedChat>().Instance.Turns[0];

        var heard = await HearAsync(talk.Recording);

        Assert.Multiple(() =>
        {
            Assert.That(talk.Question, Is.EqualTo(first.Question), "the question its demo plays first");
            Assert.That(talk.Recording, Is.EqualTo(first.Source), "from that turn's recording");
            Assert.That(IsAWholeSentenceOf(heard.Said, talk.Answer), Is.True,
                $"one whole sentence of what the agent said, which was: {heard.Said}");
            Assert.That(heard.Shown, Does.Contain(talk.Shown), "a place the agent showed in it");
        });
    }

    /// <summary>
    /// The window draws its talk: the question, the answer, and the place the agent showed, by its name.
    /// </summary>
    [TestCase(typeof(SeemeeuWindow), "The garden room")]
    [TestCase(typeof(WheatfieldWindow), "For the children")]
    [TestCase(typeof(KoringlandWindow), "After a divorce")]
    public void AWindow_DrawsItsTalk(Type window, string shown)
    {
        var talk = TalkOf(window);
        using var ctx = SitePageContext.Create();

        var drawn = Draw(ctx, window);

        Assert.Multiple(() =>
        {
            Assert.That(drawn.FindAll(".body-question").Select(asked => asked.TextContent),
                Is.EqualTo(new[] { talk.Question }));
            Assert.That(drawn.FindAll(".body-answer").Select(answered => answered.TextContent),
                Is.EqualTo(new[] { talk.Answer }));
            Assert.That(drawn.FindAll(".body-shown b").Select(place => place.TextContent),
                Is.EqualTo(new[] { shown }));
            Assert.That(string.Concat(TextsOf(drawn, ".site-body")), Does.Not.Contain(";"),
                "no semicolons for a reader");
        });
    }

    /// <summary>
    /// A window's website is its demo's: the same name, the same line and note over the same photo, the same menu,
    /// and the place it shows named as the website names it. The site's list names the window for its demo, with the
    /// website's own address as the word in its bar.
    /// </summary>
    [TestCase(typeof(SeemeeuWindow), typeof(GuesthouseDemoPage), "/guesthouse-demo")]
    [TestCase(typeof(WheatfieldWindow), typeof(RestaurantDemoPage), "/restaurant-demo")]
    [TestCase(typeof(KoringlandWindow), typeof(LawPracticeDemoPage), "/law-practice-demo")]
    public void AWindowsWebsite_IsItsDemos(Type window, Type page, string address)
    {
        var demo = SiteDemos.All.At(address)!;
        string shown = TalkOf(window).Shown;
        string itsName = $"[data-place='{shown}'] .mock-section-heading, [data-place='{shown}'] .mock-card-title";
        using var onTheStart = SitePageContext.Create();
        using var onItsPage = SitePageContext.Create();

        var drawn = Draw(onTheStart, window);
        var website = Draw(onItsPage, page);

        Assert.Multiple(() =>
        {
            Assert.That(TextsOf(drawn, ".body-mark"), Is.EqualTo(TextsOf(website, ".mock-name")));
            Assert.That(TextsOf(drawn, ".body-line"), Is.EqualTo(TextsOf(website, ".mock-hero-line")));
            Assert.That(TextsOf(drawn, ".body-note"), Is.EqualTo(TextsOf(website, ".mock-hero-note")));
            Assert.That(TextsOf(drawn, ".body-menu span"), Is.EqualTo(TextsOf(website, ".site-mock .mock-nav button")));
            Assert.That(drawn.FindAll(".body-site img").Select(photo => photo.GetAttribute("src")),
                Is.EqualTo(website.FindAll(".mock-hero .mock-photo").Select(photo => photo.GetAttribute("src"))));
            Assert.That(TextsOf(drawn, ".body-shown b"), Is.EqualTo(TextsOf(website, itsName).Take(1)));

            Assert.That(demo.Window, Is.EqualTo(window), "the site's list names this window for the demo");
            Assert.That(TextsOf(website, ".mock-address").Single(), Does.StartWith(demo.WindowAddress ?? "none"),
                "the word in its bar is its website's address");
        });
    }

    /// <summary>What the elements a selector finds say, in the page's order.</summary>
    private static List<string> TextsOf(IRenderedComponent<IComponent> drawn, string selector) =>
        [.. drawn.FindAll(selector).Select(found => found.TextContent.Trim())];
}
