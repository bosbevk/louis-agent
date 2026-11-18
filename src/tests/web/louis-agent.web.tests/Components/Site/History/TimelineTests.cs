using LouisAgent.Testing;
using LouisAgent.Web.Components.Site.History;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.History;

/// <summary>
/// A timeline is the framework's: each entry its dates in a column beside its title, where it was and what a site
/// says of it, a word under the dates while it still runs, and the column kept on a printed page.
/// </summary>
public class TimelineTests
{
    /// <summary>The framework's web project, where the timeline's styles are.</summary>
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web");

    /// <summary>An entry that still runs.</summary>
    private static IRenderedComponent<TimelineEntry> Entry(BunitContext ctx, string? current) =>
        ctx.Render<TimelineEntry>(parameters => parameters
            .Add(entry => entry.When, "2021 to now")
            .Add(entry => entry.Current, current)
            .Add(entry => entry.Title, "Senior engineer")
            .Add(entry => entry.Place, "Acme · Cape Town")
            .AddChildContent("<ul><li>Shipped it.</li></ul>"));

    /// <summary>Its dates in their column, then its words: the title, where, and what it did.</summary>
    [Test]
    public void AnEntry_HasItsDatesBesideItsWords()
    {
        using var ctx = new BunitContext();

        var entry = Entry(ctx, "current");

        Assert.Multiple(() =>
        {
            Assert.That(entry.Find("article.timeline-entry > .when").TextContent, Does.Contain("2021 to now"));
            Assert.That(entry.Find(".when em").TextContent, Is.EqualTo("current"));
            Assert.That(entry.Find(".timeline-words h3").TextContent, Is.EqualTo("Senior engineer"));
            Assert.That(entry.Find(".timeline-words .place").TextContent, Is.EqualTo("Acme · Cape Town"));
            Assert.That(entry.Find(".timeline-words ul li").TextContent, Is.EqualTo("Shipped it."));
        });
    }

    /// <summary>An entry that is over says nothing under its dates.</summary>
    [Test]
    public void AnEntryThatIsOver_HasNoWordUnderItsDates()
    {
        using var ctx = new BunitContext();

        Assert.That(Entry(ctx, null).FindAll(".when em"), Is.Empty);
    }

    /// <summary>A ruled timeline lines its entries; one of close rows doesn't.</summary>
    [TestCase(true, "timeline is-ruled")]
    [TestCase(false, "timeline")]
    public void ATimeline_IsRuledOrClose(bool ruled, string classes)
    {
        using var ctx = new BunitContext();
        RenderFragment entries = builder => builder.AddMarkupContent(0, "<p>entries</p>");

        var cut = ctx.Render<Timeline>(parameters => parameters.Add(t => t.Ruled, ruled).AddChildContent(entries));

        Assert.That(cut.Find("div").ClassName, Is.EqualTo(classes));
    }

    /// <summary>
    /// The dates keep their column on paper whatever the sheet's width, and an entry stays on one page.
    /// </summary>
    [TestCase("grid-template-columns: 32mm minmax(0, 1fr);")]
    [TestCase("break-inside: avoid;")]
    public void OnPaper_TheColumnStays(string rule) =>
        Assert.That(File.ReadAllText(Path.Combine(Web, "Components/Site/History/TimelineEntry.razor.scss")),
            Does.Contain("@media print").And.Contain(rule));
}
