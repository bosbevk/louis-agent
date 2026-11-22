using System.Buffers.Binary;
using LouisAgent.Api.Site;
using LouisAgent.Api.Site.Cards;
using LouisAgent.Api.Site.Rendering;
using LouisAgent.SiteFrameworkApi;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.SiteFramework.Tests.Meta;

/// <summary>
/// The framework's site wears a share card on every page (F169.6): its own on the Overview, and on every other page a
/// card of the page's own, drawn by the card tool and kept beside the site's files. A card is a picture of the page's
/// words, so this holds the words beside each card to the page as the host draws it today, and a heading that changes
/// fails here until the cards are drawn again. It asks the host's own <see cref="PageCards"/>, so the tool and the
/// host are held to one rule for where a card is.
/// </summary>
public class SiteCardsTests
{
    /// <summary>The site's address on the web.</summary>
    private const string Site = "https://framework.louis-agent.dev/";

    /// <summary>The site's name, as its pages' titles end.</summary>
    private const string SiteName = "louis-agent";

    /// <summary>The size every card is drawn at, in pixels, as the site's index.html says.</summary>
    private const int Width = 2400, Height = 1260;

    /// <summary>What WhatsApp asks a link preview's picture to stay under, in bytes.</summary>
    private const int MostBytes = 600 * 1024;

    private static readonly string WebRoot =
        Path.Combine(TestPaths.RepoRoot, "src/site/LouisAgent.SiteFramework/wwwroot");

    private const string Redraw =
        "draw the cards again: dotnet run --project src/tools/LouisAgent.FilmExport -- framework-cards";

    /// <summary>
    /// Every page of the site's contents but the Overview, by its own address, with the page the host serves there.
    /// </summary>
    private static IEnumerable<TestCaseData> Pages()
    {
        var services = new ServiceCollection();
        services.AddSiteAreas(FrameworkAreas.Name);
        using var provider = services.BuildServiceProvider();
        var rule = provider.GetRequiredService<SiteAddresses>();
        var routes = SiteRoutes.Of(typeof(App).Assembly, FrameworkPageServices.Register, FrameworkAreas.Home);

        return FrameworkContents.All.Pages.Select(entry => rule.Address(entry.Address, null)).Distinct()
            .Where(route => route != "/")
            .Select(route => new TestCaseData(route, routes.PageFor(route)!.Value.Page).SetArgDisplayNames(route))
            .ToList();
    }

    /// <summary>
    /// The page has a card where the host looks, and its words are the page's name and its first heading, as the host
    /// draws the page today.
    /// </summary>
    [TestCaseSource(nameof(Pages))]
    public async Task APagesCard_SaysThePagesWords(string route, Type page)
    {
        var card = new PageCards(new PhysicalFileProvider(WebRoot)).For(route);
        var rendered = await new PagePrerenderer(FrameworkPageServices.Register).RenderAsync(page, new Uri(Site));

        Assert.That(card, Is.Not.Null, $"{route} has no card: {Redraw}");
        Assert.That(rendered, Is.Not.Null, "the page renders with the host's services");
        string name = rendered!.Title.Replace($" · {SiteName}", "", StringComparison.Ordinal).Trim();
        Assert.That(card!.Description, Is.EqualTo($"{SiteName}: {name}. {FirstHeading(rendered.Body)}"),
            $"{route}'s card says other words than the page: {Redraw}");
    }

    /// <summary>Each picture, the site's own too, is the size the head says, and under WhatsApp's 600 KB.</summary>
    [Test]
    public void EveryPicture_IsTheSizeTheHeadSays_AndSmallEnough()
    {
        string folder = Path.Combine(WebRoot, PageCards.Folder);
        var pictures = (Directory.Exists(folder) ? Directory.GetFiles(folder, "*" + PageCards.PictureEnding) : [])
            .Prepend(Path.Combine(WebRoot, "social-card.png")).Where(File.Exists).ToList();
        string index = File.ReadAllText(Path.Combine(WebRoot, "index.html"));

        Assert.Multiple(() =>
        {
            Assert.That(pictures, Has.Count.EqualTo(9), "the site's own card and eight pages' cards");
            Assert.That(index, Does.Contain($"<meta property=\"og:image:width\" content=\"{Width}\""));
            Assert.That(index, Does.Contain($"<meta property=\"og:image:height\" content=\"{Height}\""));
            foreach (string picture in pictures)
            {
                byte[] bytes = File.ReadAllBytes(picture);
                Assert.That(SizeOf(bytes), Is.EqualTo((Width, Height)), picture);
                Assert.That(bytes, Has.Length.LessThan(MostBytes), picture);
            }
        });
    }

    /// <summary>The site's head names its own picture, on its own address, and says what it shows.</summary>
    [Test]
    public void TheSitesHead_NamesItsOwnPicture()
    {
        string index = File.ReadAllText(Path.Combine(WebRoot, "index.html"));

        Assert.Multiple(() =>
        {
            Assert.That(index, Does.Contain($"<meta property=\"og:image\" content=\"{Site}social-card.png\""));
            Assert.That(index, Does.Contain("<meta property=\"og:image:alt\" content=\"")
                .And.Contain("The C# AI agent framework"));
            Assert.That(index, Does.Not.Contain("https://louis-agent.dev/social-card.png"));
        });
    }

    /// <summary>A PNG's width and height, from its header: eight bytes of signature, then the first chunk's.</summary>
    private static (int Width, int Height) SizeOf(byte[] png) =>
        (BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));

    /// <summary>The words of the first heading in a page's HTML, its tags taken out.</summary>
    private static string FirstHeading(string html)
    {
        int open = html.IndexOf("<h1", StringComparison.Ordinal);
        int start = html.IndexOf('>', open) + 1;
        string inside = html[start..html.IndexOf("</h1>", start, StringComparison.Ordinal)];

        var words = new System.Text.StringBuilder();
        bool inTag = false;
        foreach (char letter in inside)
        {
            if (letter == '<') inTag = true;
            if (!inTag) words.Append(letter);
            if (letter == '>') inTag = false;
        }

        return System.Net.WebUtility.HtmlDecode(words.ToString()).Trim();
    }
}
