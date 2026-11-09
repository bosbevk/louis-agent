using System.Text.Json;
using LouisAgent.Web.Components.Site.Meta;
using LouisAgent.Web.Components.Site.Meta.Schema;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LouisAgent.Web.Tests.Components.Site.Meta;

/// <summary>
/// A page says what it is about in its head (F121), and what it is in a search engine's own words (F123): its
/// description as a meta tag and its structured data as JSON-LD, both where the browser keeps them and the framework's
/// prerenderer reads them for a search engine.
/// </summary>
public class PageHeadTests
{
    /// <summary>The description lands in the head as a meta tag, its words escaped.</summary>
    [Test]
    public void TheDescription_LandsInTheHead()
    {
        using var ctx = new BunitContext();
        // The head's outlet asks the page's script for the old title first; here nothing answers.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render(ctx, "Agents in C# & \"measured\".", null);

        cut.WaitForAssertion(() => Assert.That(cut.Find("meta[name=description]").GetAttribute("content"),
            Is.EqualTo("Agents in C# & \"measured\".")));
    }

    /// <summary>
    /// The description and every schema share the one head, since Blazor keeps only the last head a page writes: each
    /// schema a JSON-LD script in schema.org's names.
    /// </summary>
    [Test]
    public void TheDescriptionAndTheSchemas_ShareTheHead()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var organisation = new SchemaOrganization("louis-agent", "https://louis-agent.dev/",
            "https://louis-agent.dev/apple-touch-icon.png", ["https://www.linkedin.com/in/example"]);
        var site = new SchemaWebSite("louis-agent", "https://louis-agent.dev/");

        var cut = Render(ctx, "Agents in C#, measured.", [organisation, site]);

        cut.WaitForAssertion(() =>
            Assert.That(cut.FindAll("script[type='application/ld+json']"), Has.Count.EqualTo(2)));
        using var json = JsonDocument.Parse(cut.FindAll("script[type='application/ld+json']")[0].TextContent);
        var root = json.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("meta[name=description]").GetAttribute("content"),
                Is.EqualTo("Agents in C#, measured."));
            Assert.That(root.GetProperty("@context").GetString(), Is.EqualTo("https://schema.org"));
            Assert.That(root.GetProperty("@type").GetString(), Is.EqualTo("Organization"));
            Assert.That(root.GetProperty("name").GetString(), Is.EqualTo("louis-agent"));
            Assert.That(root.GetProperty("sameAs")[0].GetString(), Is.EqualTo("https://www.linkedin.com/in/example"));
        });
    }

    /// <summary>The head's outlet and the component, as a page holds them.</summary>
    private static IRenderedComponent<IComponent> Render(BunitContext ctx, string description,
        IReadOnlyList<object>? schemas) =>
        ctx.Render(builder =>
        {
            builder.OpenComponent<HeadOutlet>(0);
            builder.CloseComponent();
            builder.OpenComponent<PageHead>(1);
            builder.AddComponentParameter(2, nameof(PageHead.Description), description);
            builder.AddComponentParameter(3, nameof(PageHead.Schemas), schemas);
            builder.CloseComponent();
        });
}
