using LouisAgent.Api.Site.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Tests.Site;

/// <summary>
/// The framework renders a site's page to HTML on the server (F121), so a search engine that runs no script gets the
/// page's own title, description and words: the page beside a head outlet, its scripts never run.
/// </summary>
public class PagePrerendererTests
{
    private static readonly Uri Address = new("https://site.test/proof");

    /// <summary>A page as a site writes one: its title and description in the head, its words in the body.</summary>
    private sealed class SamplePage : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<PageTitle>(0);
            builder.AddComponentParameter(1, "ChildContent",
                (RenderFragment)(b => b.AddContent(0, "Measured · site")));
            builder.CloseComponent();
            builder.OpenComponent<HeadContent>(2);
            builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(b =>
                b.AddMarkupContent(0, "<meta name=\"description\" content=\"Every number from the ledger.\" />")));
            builder.CloseComponent();
            builder.AddMarkupContent(4, $"<h2>Every number</h2><p>Served at {Navigation.Uri}</p>");
        }
    }

    /// <summary>A page that fails while it renders.</summary>
    private sealed class BrokenPage : ComponentBase
    {
        protected override void OnInitialized() => throw new InvalidOperationException("broken");
    }

    private static PagePrerenderer Prerenderer() => new(_ => { });

    /// <summary>
    /// The page comes out with its own title and description, and its words in the body, never its head.
    /// </summary>
    [Test]
    public async Task APage_RendersWithItsHeadAndWords()
    {
        var page = await Prerenderer().RenderAsync(typeof(SamplePage), Address);

        Assert.That(page, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(page!.Title, Is.EqualTo("Measured · site"));
            Assert.That(page.Description, Is.EqualTo("Every number from the ledger."));
            Assert.That(page.Body, Does.Contain("<h2>Every number</h2>"));
            Assert.That(page.Body, Does.Not.Contain("<title>").And.Not.Contain("name=\"description\""));
        });
    }

    /// <summary>A page that asks where it is gets the address it is served at.</summary>
    [Test]
    public async Task APage_KnowsItsAddress()
    {
        var page = await Prerenderer().RenderAsync(typeof(SamplePage), Address);

        Assert.That(page!.Body, Does.Contain("Served at https://site.test/proof"));
    }

    /// <summary>A page that throws gives nothing, so the host serves the plain shell instead of an error.</summary>
    [Test]
    public async Task ABrokenPage_GivesNothing()
    {
        Assert.That(await Prerenderer().RenderAsync(typeof(BrokenPage), Address), Is.Null);
    }

    /// <summary>The site's own services reach the page.</summary>
    [Test]
    public async Task TheSitesServices_ReachThePage()
    {
        var prerenderer = new PagePrerenderer(services => services.AddSingleton(new SiteWord("from the site")));

        var page = await prerenderer.RenderAsync(typeof(WordPage), Address);

        Assert.That(page!.Body, Does.Contain("from the site"));
    }

    /// <summary>A service a site registers.</summary>
    private sealed record SiteWord(string Text);

    /// <summary>A page that shows a service of the site's.</summary>
    private sealed class WordPage : ComponentBase
    {
        [Inject] private SiteWord Word { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Word.Text);
    }
}
