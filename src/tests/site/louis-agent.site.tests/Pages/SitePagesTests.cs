using System.Reflection;
using AngleSharp.Dom;
using LouisAgent.Site.Pages;
using LouisAgent.Site.Pages.Framework;
using LouisAgent.Web.Components.Site.Bands;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// The site's pages, now pages of this app (F109.1): each answers at its address with its heading, and says the
/// framework's structure as it stands, not the one from before the split.
/// </summary>
public class SitePagesTests
{
    /// <summary>Each page of the menu is at its address and opens with its heading.</summary>
    [TestCase(typeof(HomePage), "/", "Ask the website.")]
    [TestCase(typeof(HowItWorksPage), "/framework/how-it-works", "One turn, through every layer.")]
    [TestCase(typeof(ArchitecturePage), "/framework/architecture",
        "The whole framework, as the repository is laid out.")]
    [TestCase(typeof(IdentitiesPage), "/framework/identities", "Who an agent is, as files.")]
    [TestCase(typeof(PacksPage), "/framework/packs", "What an agent can do is a package.")]
    [TestCase(typeof(BuildYourOwnPage), "/framework/build-your-own", "A few lines of your own.")]
    [TestCase(typeof(CsharpAgentFrameworkPage), "/csharp-ai-agent-framework",
        "An AI agent framework in C#, on your own hardware.")]
    [TestCase(typeof(ProofPage), "/framework/measured", "Every number here comes from the ledger or an eval run.")]
    [TestCase(typeof(BuiltOnItPage), "/framework/built-on-it", "Three agents run on the engine today.")]
    [TestCase(typeof(ContactPage), "/contact", "An agent for your business, built for you.")]
    [TestCase(typeof(WhatsComingPage), "/framework/whats-coming", "Planned, and said so.")]
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
            Assert.That(cut.Find("h1.chrome").TextContent, Is.EqualTo(heading));
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
    [TestCase(typeof(HomePage), "South Africa")]
    [TestCase(typeof(ArchitecturePage), "href=\"/framework/identities\"")]
    [TestCase(typeof(HomePage), "id=\"custom\"")]
    [TestCase(typeof(ProofPage), "id=\"rule\"")]
    [TestCase(typeof(ProofPage), "First principles.")]
    [TestCase(typeof(ProofPage), "This startup runs on one rule.")]
    [TestCase(typeof(ProofPage), "meaning and purpose")]
    [TestCase(typeof(ProofPage), "Kierkegaard")]
    [TestCase(typeof(ProofPage), "stoic")]
    [TestCase(typeof(ProofPage), "Competition")]
    [TestCase(typeof(HomePage), "Custom agents")]
    [TestCase(typeof(IdentitiesPage), "Never lie.")]
    [TestCase(typeof(HomePage), "That is the end of the tour.")]
    [TestCase(typeof(BuiltOnItPage), "The guide")]
    [TestCase(typeof(HomePage), "closed Docker container")]
    [TestCase(typeof(HowItWorksPage), "closed Docker container")]
    [TestCase(typeof(ContactPage), "Custom agents")]
    [TestCase(typeof(ContactPage), "A website with an agent")]
    [TestCase(typeof(ContactPage), "Every quote is made for your business, on request.")]
    [TestCase(typeof(ContactPage), "mailto:louis.marais@live.com")]
    [TestCase(typeof(ContactPage), "href=\"https://wa.me/27663912470")]
    [TestCase(typeof(ContactPage), "066 391 2470")]
    [TestCase(typeof(ContactPage), "Chat on WhatsApp")]
    [TestCase(typeof(ContactPage), "class=\"btn btn-whatsapp\"")]
    [TestCase(typeof(ContactPage), "aria-label=\"WhatsApp\"")]
    [TestCase(typeof(ContactPage), "href=\"https://www.linkedin.com/in/louismarais86\"")]
    [TestCase(typeof(ContactPage), "class=\"btn btn-linkedin\"")]
    [TestCase(typeof(ContactPage), "aria-label=\"LinkedIn\"")]
    [TestCase(typeof(ContactPage), "On LinkedIn")]
    [TestCase(typeof(ContactPage), "class=\"tile-brand\"")]
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
    /// The custom agents' section opens with a paragraph under its heading (FX119, Louis: "otherwise we must just put
    /// a paragraph there"): its cards are too tall to sit under the heading at 1080p, so the first page says what
    /// they hold instead of showing a heading alone.
    /// </summary>
    [Test]
    public void TheCustomAgents_OpenWithAParagraph()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<HomePage>();

        var summary = cut.Find("#custom h2 + p.summary");
        Assert.That(summary.TextContent, Does.Contain("your own"));
        Assert.That(summary.TextContent, Does.Not.Contain(";"));
    }

    /// <summary>The hero's words under its headline, as the design has them.</summary>
    private const string Summary = "We build websites that answer for your business. An agent on the page shows the " +
                                "room, takes the booking and sends it to you, while you're busy with the guests you " +
                                "already have.";

    /// <summary>
    /// The hero sells (F128.1, Louis's pick 1A): the website with an agent, in the design's words (FX160, Louis:
    /// "replace the red with the green", and of the line above the headline, "take this out"), then "Ask for yours"
    /// and "See what it does", with nothing under them. No tour button: the guide's bar is at the foot of every page (F151.6, Louis: "these button must
    /// go").
    /// </summary>
    [Test]
    public void TheHero_SellsTheWebsiteWithAnAgent()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<HomePage>();

        var actions = cut.Find(".hero .actions").Children.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".hero .topic"), Is.Empty, "no line above the headline");
            Assert.That(cut.Find(".hero h1").TextContent, Is.EqualTo("Ask the website."));
            Assert.That(cut.Find(".hero").ClassList, Does.Contain("is-centred"));
            Assert.That(cut.FindAll(".hero .hero-art .mark-states .orb"), Has.Count.EqualTo(1),
                "the mark, above the words");
            Assert.That(actions, Has.Count.EqualTo(2));
            Assert.That(actions[0].TextContent.Trim(), Is.EqualTo("Ask for yours"));
            Assert.That(actions[1].TextContent.Trim(), Is.EqualTo("See what it does"));
            Assert.That(actions[0].GetAttribute("href"), Is.EqualTo("/contact"));
            Assert.That(actions[1].GetAttribute("href"), Is.EqualTo("/home/does"));
            Assert.That(actions[1].GetAttribute("data-section"), Is.EqualTo("does"),
                "a move within the page, which the frame makes, as the index's links do");
            Assert.That(cut.Find(".hero .summary").TextContent, Is.EqualTo(Summary));
            Assert.That(cut.FindAll(".hero p.muted"), Is.Empty, "no note under the words");
            Assert.That(cut.FindAll(".hero .hero-aside"), Is.Empty, "nothing under the buttons");
            Assert.That(cut.FindAll(".hero .guide-button"), Is.Empty);
        });
    }

    /// <summary>
    /// The landing is one page the visitor scrolls (F110), and since F118 its sections put themselves on the frame's
    /// list as they draw: every section is listed, each by an id that is on the page once, the hero among them as
    /// "Top". The order is the page's, read in the browser, so it isn't asserted here.
    /// </summary>
    [Test]
    public void TheLanding_ListsItsSections_OnTheFramesIndex()
    {
        using var ctx = new BunitContext();
        var areas = new PageAreas();

        RenderFragment page = builder =>
        {
            builder.OpenComponent<HomePage>(0);
            builder.CloseComponent();
        };
        var cut = ctx.Render<CascadingValue<PageAreas>>(parameters =>
            parameters.Add(c => c.Value, areas).AddChildContent(page));
        var targets = areas.Items.Select(entry => entry.Id).ToList();

        Assert.That(targets, Has.Count.GreaterThanOrEqualTo(5), "the sections list themselves");
        Assert.That(areas.Items, Does.Contain(new IndexEntry("top", "Top")));
        Assert.That(targets.Where(target => cut.FindAll($"[id='{target}']").Count != 1), Is.Empty);
    }

    /// <summary>
    /// No page joins two clauses with a semicolon (Louis, 2026-11-08: "we should never do this"): a visitor reads short
    /// sentences. Code listings are code and may keep theirs, and the principles are the engine's own file, shown as it
    /// is (FX74), not the site's words.
    /// </summary>
    [Test]
    public void NoPage_JoinsClausesWithASemicolon()
    {
        using var ctx = SitePageContext.Create();
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

            return (page.Name,
                Text: cut.Nodes.OfType<IElement>().Select(node => node.TextContent).Aggregate("", string.Concat));
        }).Where(page => page.Text.Contains(';')).Select(page => page.Name).ToList();

        Assert.That(pages, Has.Count.GreaterThanOrEqualTo(10), "the pages were found");
        Assert.That(offenders, Is.Empty);
    }

    /// <summary>
    /// Every id on a page names one place (F118): a card takes its id from its title, and the guide opens it by that
    /// id, so a card named like a band beside it would send the visitor to the wrong one.
    /// </summary>
    [Test]
    public void EveryIdOnAPage_NamesOnePlace()
    {
        using var ctx = SitePageContext.Create();
        var pages = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any()).ToList();

        var twice = pages.SelectMany(page =>
        {
            var cut = ctx.Render(builder =>
            {
                builder.OpenComponent(0, page);
                builder.CloseComponent();
            });
            return cut.FindAll("[id]").GroupBy(element => element.Id).Where(ids => ids.Count() > 1)
                .Select(ids => $"{page.Name}: #{ids.Key}");
        }).ToList();

        Assert.That(pages, Has.Count.GreaterThanOrEqualTo(10), "the pages were found");
        Assert.That(twice, Is.Empty);
    }

    /// <summary>
    /// Every page that explains a part of the framework shows a diagram of it (F111, Louis: "add a nice graph on each
    /// page"), at least one, a slide that explains its own part having its own (F119, the web's on How it works); the
    /// landing has its film, Contact and What's coming explain nothing.
    /// </summary>
    [TestCase(typeof(HowItWorksPage))]
    [TestCase(typeof(ArchitecturePage))]
    [TestCase(typeof(IdentitiesPage))]
    [TestCase(typeof(PacksPage))]
    [TestCase(typeof(BuildYourOwnPage))]
    [TestCase(typeof(ProofPage))]
    [TestCase(typeof(BuiltOnItPage))]
    public void AnExplainingPage_ShowsADiagram(Type page)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.That(cut.FindAll("figure.graph"), Has.Count.GreaterThanOrEqualTo(1));
    }

    /// <summary>
    /// How it works splits the loop from the web (F119, Louis: "split the Loop sections from the web sections"): four
    /// slides, the film, the loop with the turn's diagram, the web with its own, and the rules; the web's opens with
    /// Louis's line and names how the site works with the backend.
    /// </summary>
    [Test]
    public void HowItWorks_SplitsTheLoopFromTheWeb()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<HowItWorksPage>();

        var web = cut.Find("section#web");
        Assert.Multiple(() =>
        {
            // The web's id is read from its section: the bare word is a toolset's wire name (ADR 0043).
            Assert.That(cut.FindAll(".band").Select(band => band.Id),
                Is.EqualTo(new[] { "top", "loop", web.Id, "rules" }));
            Assert.That(cut.Find("section#loop").QuerySelectorAll("figure.graph"), Has.Length.EqualTo(1));
            Assert.That(web.QuerySelectorAll("figure.graph"), Has.Length.EqualTo(1));
            Assert.That(web.TextContent,
                Does.Contain("runs without the web").And.Contain("turns an agent into a website"));
            Assert.That(web.TextContent,
                Does.Contain("WebAssembly").And.Contain("same address").And.Contain("server-sent events"));
            Assert.That(web.TextContent,
                Does.Contain("guide in the corner").And.Contain("its own address").And.Contain("signs in"));
            Assert.That(cut.Find("section#loop").TextContent, Does.Not.Contain("WebAssembly"),
                "the web's parts left the loop");
        });
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
}
