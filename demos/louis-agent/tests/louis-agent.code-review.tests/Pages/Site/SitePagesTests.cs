using System.Reflection;
using AngleSharp.Dom;
using LouisAgent.CodeReview.Pages.Site;
using LouisAgent.CodeReview.Pages.Site.Framework;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Tests.Pages.Site;

/// <summary>
/// The site's pages, now pages of this app (F109.1): each answers at its address with its heading, and says the
/// framework's structure as it stands, not the one from before the split.
/// </summary>
public class SitePagesTests
{
    /// <summary>Each page of the menu is at its address and opens with its heading.</summary>
    [TestCase(typeof(HomePage), "/", "First principles.")]
    [TestCase(typeof(HowItWorksPage), "/how-it-works", "One turn, through every layer.")]
    [TestCase(typeof(ArchitecturePage), "/architecture", "The whole framework, as the repository is laid out.")]
    [TestCase(typeof(IdentitiesPage), "/identities", "Who an agent is, as files.")]
    [TestCase(typeof(PacksPage), "/packs", "What an agent can do is a package.")]
    [TestCase(typeof(BuildYourOwnPage), "/build-your-own", "A few lines of your own.")]
    [TestCase(typeof(ProofPage), "/proof", "Every number here comes from the ledger or an eval run.")]
    [TestCase(typeof(BuiltOnItPage), "/built-on-it", "Three agents run on the engine today.")]
    [TestCase(typeof(ContactPage), "/contact", "An agent for your business, built for you.")]
    [TestCase(typeof(WhatsComingPage), "/whats-coming", "Planned, and said so.")]
    public void APage_IsAtItsAddress_WithItsHeading(Type page, string address, string heading)
    {
        using var ctx = new BunitContext();

        var routes = page.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>()
            .Select(route => route.Template);
        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.Multiple(() =>
        {
            Assert.That(routes, Is.EqualTo(new[] { address }));
            Assert.That(cut.Find("h2.chrome").TextContent, Is.EqualTo(heading));
        });
    }

    /// <summary>
    /// Every page is made of named areas (F117, Louis: "all pages must have areas"): each section carries an id in
    /// words, unique on its page, so the guide can send a visitor to it and the section index can name it.
    /// </summary>
    [TestCase(typeof(HomePage))]
    [TestCase(typeof(HowItWorksPage))]
    [TestCase(typeof(ArchitecturePage))]
    [TestCase(typeof(IdentitiesPage))]
    [TestCase(typeof(PacksPage))]
    [TestCase(typeof(BuildYourOwnPage))]
    [TestCase(typeof(ProofPage))]
    [TestCase(typeof(BuiltOnItPage))]
    [TestCase(typeof(ContactPage))]
    [TestCase(typeof(WhatsComingPage))]
    public void EverySection_IsANamedArea(Type page)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        var ids = cut.FindAll("section").Select(section => section.GetAttribute("id") ?? "").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(ids, Is.Not.Empty);
            Assert.That(ids, Is.All.Match("^[a-z][a-z-]*$"), "an id in words on every section");
            Assert.That(ids, Is.Unique);
        });
    }

    /// <summary>The words from before the split are gone: a shops pack, tools in the engine.</summary>
    [TestCase(typeof(BuiltOnItPage), "A shops pack")]
    [TestCase(typeof(HowItWorksPage), "tools sorted by name")]
    [TestCase(typeof(HowItWorksPage), "The tool loop, streaming, the providers, the usage ledger and the guards.")]
    [TestCase(typeof(HomePage), "60+ ADRs")]
    public void APage_DropsTheOldStructure(Type page, string old)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.That(cut.Markup, Does.Not.Contain(old));
    }

    /// <summary>What's new is on the page: a demo is built on the framework, a pack never needs the engine.</summary>
    [TestCase(typeof(HomePage), "An AI agent framework")]
    [TestCase(typeof(HomePage), "100% C#")]
    [TestCase(typeof(HomePage), "South Africa")]
    [TestCase(typeof(HomePage), "one person")]
    [TestCase(typeof(HomePage), "href=\"/identities\"")]
    [TestCase(typeof(HomePage), "href=\"/architecture\"")]
    [TestCase(typeof(HomePage), "id=\"custom\"")]
    [TestCase(typeof(HomePage), "id=\"rule\"")]
    [TestCase(typeof(HomePage), "First principles.")]
    [TestCase(typeof(HomePage), "This startup runs on one rule.")]
    [TestCase(typeof(HomePage), "meaning and purpose")]
    [TestCase(typeof(HomePage), "Kierkegaard")]
    [TestCase(typeof(HomePage), "stoic")]
    [TestCase(typeof(HomePage), "Competition")]
    [TestCase(typeof(HomePage), "Custom agents")]
    [TestCase(typeof(IdentitiesPage), "Never lie.")]
    [TestCase(typeof(HomePage), "class=\"band band-strip reveal\"")]
    [TestCase(typeof(HomePage), "The guide")]
    [TestCase(typeof(BuiltOnItPage), "The guide")]
    [TestCase(typeof(HomePage), "closed Docker container")]
    [TestCase(typeof(HowItWorksPage), "closed Docker container")]
    [TestCase(typeof(HomePage), "What we sell today")]
    [TestCase(typeof(HomePage), "R500 to R1,000 a month")]
    [TestCase(typeof(ContactPage), "Custom agents")]
    [TestCase(typeof(ContactPage), "What we sell today")]
    [TestCase(typeof(ContactPage), "R500 to R1,000 a month")]
    [TestCase(typeof(ContactPage), "mailto:louis.marais@live.com")]
    [TestCase(typeof(ContactPage), "href=\"https://wa.me/27663912470")]
    [TestCase(typeof(ContactPage), "066 391 2470")]
    [TestCase(typeof(ContactPage), "Chat on WhatsApp")]
    [TestCase(typeof(ContactPage), "class=\"btn btn-whatsapp\"")]
    [TestCase(typeof(ContactPage), "aria-label=\"WhatsApp\"")]
    [TestCase(typeof(ContactPage), "href=\"https://www.linkedin.com/in/louismarais86\"")]
    [TestCase(typeof(ContactPage), "class=\"btn btn-linkedin\"")]
    [TestCase(typeof(ContactPage), "aria-label=\"LinkedIn\"")]
    [TestCase(typeof(ContactPage), "Louis on LinkedIn")]
    [TestCase(typeof(HowItWorksPage), "holds no agent and no tools")]
    [TestCase(typeof(HowItWorksPage), "never the engine")]
    [TestCase(typeof(HowItWorksPage), "as libraries")]
    [TestCase(typeof(HowItWorksPage), "framework's web")]
    [TestCase(typeof(HowItWorksPage), "accounts")]
    [TestCase(typeof(HowItWorksPage), "harness")]
    [TestCase(typeof(HowItWorksPage), "Azure")]
    [TestCase(typeof(BuiltOnItPage), "Each is a demo")]
    [TestCase(typeof(PacksPage), "never the engine")]
    public void APage_TellsTheNewStructure(Type page, string words)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.That(cut.Markup, Does.Contain(words));
    }

    /// <summary>
    /// The landing is one page the visitor scrolls (F110): its index names every section by id, each a section on
    /// the page, and a progress line sits at the top for the frame's module to fill.
    /// </summary>
    [Test]
    public void TheLanding_IndexesItsSections()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<HomePage>();
        var targets = cut.FindAll(".site-index a").Select(a => a.GetAttribute("href")!).ToList();

        Assert.That(targets, Has.Count.GreaterThanOrEqualTo(5), "the index names the sections");
        Assert.That(targets, Has.All.StartWith("#"));
        Assert.That(targets.Where(target => cut.FindAll($"[id='{target[1..]}']").Count != 1), Is.Empty);
        Assert.That(cut.FindAll("#site-progress"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// No page joins two clauses with a semicolon (Louis, 2026-11-08: "we should never do this"): a visitor reads short
    /// sentences. Code listings are code and may keep theirs, and the principles are the engine's own file, shown as it
    /// is (FX74), not the site's words.
    /// </summary>
    [Test]
    public void NoPage_JoinsClausesWithASemicolon()
    {
        using var ctx = new BunitContext();
        var pages = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any()).ToList();

        var offenders = pages.Select(page =>
        {
            var cut = ctx.Render(builder =>
            {
                builder.OpenComponent(0, page);
                builder.CloseComponent();
            });
            foreach (var code in cut.FindAll("code, pre, style, script, .principles"))
            {
                code.Remove();
            }
            return (page.Name, Text: cut.Nodes.OfType<IElement>().Select(node => node.TextContent).Aggregate("", string.Concat));
        }).Where(page => page.Text.Contains(';')).Select(page => page.Name).ToList();

        Assert.That(pages, Has.Count.GreaterThanOrEqualTo(10), "the pages were found");
        Assert.That(offenders, Is.Empty);
    }

    /// <summary>
    /// Every page that explains a part of the framework shows one diagram of it (F111, Louis: "add a nice graph on each
    /// page"); the landing has its film, Contact and What's coming explain nothing.
    /// </summary>
    [TestCase(typeof(HowItWorksPage))]
    [TestCase(typeof(ArchitecturePage))]
    [TestCase(typeof(IdentitiesPage))]
    [TestCase(typeof(PacksPage))]
    [TestCase(typeof(BuildYourOwnPage))]
    [TestCase(typeof(ProofPage))]
    [TestCase(typeof(BuiltOnItPage))]
    public void AnExplainingPage_ShowsOneDiagram(Type page)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.That(cut.FindAll("figure.graph"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Where a grid carried a summary and a story each, the page now shows a deck of short cards (F113, Louis: "make
    /// the card a short summary, and clickable with the detail sliding in at the bottom"): a click on the first card
    /// opens its story under the row, with words the row itself does not show.
    /// </summary>
    [TestCase(typeof(BuiltOnItPage), "Five specialists today")]
    [TestCase(typeof(IdentitiesPage), "five specialists as roles")]
    [TestCase(typeof(PacksPage), "read PDF, Word and Excel as text")]
    [TestCase(typeof(ProofPage), "it keeps dollars, because that is what was charged")]
    public void ACard_OpensItsStory(Type page, string storyWords)
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        bool shownBefore = cut.Markup.Contains(storyWords);
        cut.FindAll(".card-deck button.card-summary")[0].Click();

        Assert.Multiple(() =>
        {
            Assert.That(shownBefore, Is.False, "the story waits for the click");
            Assert.That(cut.Find(".card-detail-body").TextContent, Does.Contain(storyWords));
        });
    }

    /// <summary>The landing's blocks reveal as they enter (F110): the headings and the panels carry the mark.</summary>
    [Test]
    public void TheLanding_RevealsItsBlocks()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<HomePage>();

        Assert.That(cut.FindAll("h2.reveal"), Has.Count.GreaterThanOrEqualTo(3));
        Assert.That(cut.FindAll(".panel.reveal"), Has.Count.GreaterThanOrEqualTo(6));
    }

    /// <summary>The landing shows the short film; How it works the detailed one; neither the other's.</summary>
    [TestCase(typeof(HomePage), "film-short", "film-detail")]
    [TestCase(typeof(HowItWorksPage), "film-detail", "film-short")]
    public void APage_ShowsItsFilm(Type page, string film, string notThisOne)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.That(cut.FindAll($".stage.{film}"), Has.Count.EqualTo(1));
        Assert.That(cut.Markup, Does.Not.Contain(notThisOne));
    }
}
