using System.Text.Json;
using LouisAgent.Web.Components.Site.Meta;
using LouisAgent.Web.Components.Site.Meta.Schema;
using Microsoft.AspNetCore.Components.Web;

namespace LouisAgent.Web.Tests.Components.Site.Meta;

/// <summary>
/// A page says what it is in a search engine's own words (F123, Louis's pick: a component in the page's head): its
/// structured data lands in the head as JSON-LD, where the framework's prerenderer reads it for a crawler.
/// </summary>
public class PageSchemaTests
{
    /// <summary>The value lands in the head as a JSON-LD script, in schema.org's names.</summary>
    [Test]
    public void TheValue_LandsInTheHead_AsJsonLd()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var organisation = new SchemaOrganization("louis-agent", "https://louis-agent.dev/",
            "https://louis-agent.dev/icon-192.png", ["https://www.linkedin.com/in/example"]);

        var cut = Render(ctx, organisation);

        cut.WaitForAssertion(() =>
            Assert.That(cut.FindAll("script[type='application/ld+json']"), Has.Count.EqualTo(1)));
        using var json = JsonDocument.Parse(cut.Find("script[type='application/ld+json']").TextContent);
        var root = json.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("@context").GetString(), Is.EqualTo("https://schema.org"));
            Assert.That(root.GetProperty("@type").GetString(), Is.EqualTo("Organization"));
            Assert.That(root.GetProperty("name").GetString(), Is.EqualTo("louis-agent"));
            Assert.That(root.GetProperty("sameAs")[0].GetString(), Is.EqualTo("https://www.linkedin.com/in/example"));
        });
    }

    /// <summary>Words that would close the script can't: the JSON escapes the angle brackets.</summary>
    [Test]
    public void AClosingTagInTheWords_StaysInsideTheScript()
    {
        string json = SchemaJson.Write(new SchemaWebSite("</script><b>x</b>", "https://example.test/"));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Not.Contain("</script>"));
            Assert.That(JsonDocument.Parse(json).RootElement.GetProperty("name").GetString(),
                Is.EqualTo("</script><b>x</b>"));
        });
    }

    /// <summary>The head's outlet and the component, as a page holds them.</summary>
    private static IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> Render(
        BunitContext ctx, object value) =>
        ctx.Render(builder =>
        {
            builder.OpenComponent<HeadOutlet>(0);
            builder.CloseComponent();
            builder.OpenComponent<PageSchema>(1);
            builder.AddComponentParameter(2, nameof(PageSchema.Value), value);
            builder.CloseComponent();
        });
}
