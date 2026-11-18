using System.Reflection;
using AngleSharp.Dom;
using LouisAgent.Site.Pages;
using LouisAgent.Site.Pages.About;
using LouisAgent.Web.Components.Site.Bands;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// The site's pages, now pages of this app (F109.1): each answers at its address with its heading, and is made of
/// named areas the guide can send a visitor to.
/// </summary>
public class SitePagesTests
{
    /// <summary>Each page of the menu is at its address and opens with its heading.</summary>
    [TestCase(typeof(HomePage), "/", "Your doors close at five. Your website doesn't.")]
    [TestCase(typeof(ContactPage), "/contact", "An agent for your business, built for you.")]
    [TestCase(typeof(PrivacyPage), "/privacy", "What happens to what you type.")]
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
    [TestCase(typeof(ContactPage))]
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

    /// <summary>The words from before the split are gone: a count of ADRs on the landing.</summary>
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

    /// <summary>What's new is on the page: what the site sells, where it runs, and how to reach us.</summary>
    [TestCase(typeof(HomePage), "South Africa")]
    [TestCase(typeof(HomePage), "id=\"custom\"")]
    [TestCase(typeof(HomePage), "Custom agents")]
    [TestCase(typeof(HomePage), "Leave your details with Orbi below")]
    [TestCase(typeof(HomePage), "sealed box that sees only what you hand it")]
    [TestCase(typeof(HomePage), "href=\"/privacy\"")]
    [TestCase(typeof(ContactPage), "Custom agents")]
    [TestCase(typeof(ContactPage), "A website with an agent")]
    [TestCase(typeof(ContactPage), "Every quote is made for your business, on request.")]
    [TestCase(typeof(ContactPage), "mailto:louis.marais@live.com")]
    [TestCase(typeof(ContactPage), "href=\"https://wa.me/27663912470")]
    [TestCase(typeof(ContactPage), "066 391 2470")]
    [TestCase(typeof(ContactPage), "class=\"btn btn-whatsapp\"")]
    [TestCase(typeof(ContactPage), "aria-label=\"WhatsApp\"")]
    [TestCase(typeof(ContactPage), "href=\"https://www.linkedin.com/in/louismarais86\"")]
    [TestCase(typeof(ContactPage), "class=\"btn btn-linkedin\"")]
    [TestCase(typeof(ContactPage), "aria-label=\"LinkedIn\"")]
    [TestCase(typeof(ContactPage), "On LinkedIn")]
    [TestCase(typeof(ContactPage), "class=\"tile-brand\"")]
    [TestCase(typeof(ContactPage), ">Ask for a website</a>")]
    [TestCase(typeof(ContactPage), "</svg> 066 391 2470</a>")]
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
    /// FX188: every WhatsApp button on the contact page says the same, the number beside the mark (Louis: "all my
    /// whatsapp buttons must look the same").
    /// </summary>
    [Test]
    public void EveryWhatsAppButton_SaysTheNumber()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ContactPage>();
        var labels = cut.FindAll("a.btn-whatsapp").Select(button => button.TextContent.Trim()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(labels, Has.Count.EqualTo(3));
            Assert.That(labels.Distinct(), Is.EqualTo(new[] { "066 391 2470" }));
        });
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
    private const string Summary = "It answers the question someone asks at 11 at night, and has their details waiting " +
                                "for you in the morning.";

    /// <summary>
    /// The hero sells (F128.1, Louis's pick 1A): the website with an agent, in the design's words (FX160, Louis:
    /// "replace the red with the green", and of the line above the headline, "take this out"), then "Ask for yours"
    /// and "See what it does", with nothing under them. Since the brand research of 2026-11-18 the owner's problem is
    /// the headline, and the second button leads to the demos, "Watch one work", so they see it before they ask. No tour button: the guide's bar is at the foot of every page (F151.6, Louis: "these button must
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
            Assert.That(cut.Find(".hero h1").TextContent, Is.EqualTo("Your doors close at five. Your website doesn't."));
            Assert.That(cut.FindAll(".hero h1 .headline-line"), Has.Count.EqualTo(2), "one sentence to a line");
            Assert.That(cut.Find(".hero").ClassList, Does.Contain("is-centred"));
            Assert.That(cut.FindAll(".hero .hero-art .mark-states .orb"), Has.Count.EqualTo(1),
                "the mark, above the words");
            Assert.That(actions, Has.Count.EqualTo(2));
            Assert.That(actions[0].TextContent.Trim(), Is.EqualTo("Ask for yours"));
            Assert.That(actions[1].TextContent.Trim(), Is.EqualTo("Watch one work"));
            Assert.That(actions[0].GetAttribute("href"), Is.EqualTo("/contact"));
            Assert.That(actions[1].GetAttribute("href"), Is.EqualTo("/demos"));
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
    /// sentences. Code listings are code and may keep theirs.
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
            foreach (var code in cut.FindAll("code, pre, style, script"))
            {
                code.Remove();
            }

            return (page.Name,
                Text: cut.Nodes.OfType<IElement>().Select(node => node.TextContent).Aggregate("", string.Concat));
        }).Where(page => page.Text.Contains(';')).Select(page => page.Name).ToList();

        Assert.That(pages, Has.Count.GreaterThanOrEqualTo(9), "the pages were found");
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

        Assert.That(pages, Has.Count.GreaterThanOrEqualTo(9), "the pages were found");
        Assert.That(twice, Is.Empty);
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
