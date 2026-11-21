using LouisAgent.Web.Components.Site.Controls.Labels;

namespace LouisAgent.Web.Tests.Components.Site.Controls.Labels;

/// <summary>
/// A row of tags is the framework's: a plain tag per word, in order, and nothing when there are none.
/// </summary>
public class TagListTests
{
    /// <summary>One plain tag per word, in the order given.</summary>
    [Test]
    public void ARow_HasATagPerWord()
    {
        using var ctx = new BunitContext();

        var row = ctx.Render<TagList>(parameters => parameters.Add(t => t.Tags, ["C#", ".NET", "Azure"]));

        Assert.That(row.FindAll("div.tag-list > span.tag").Select(tag => tag.TextContent),
            Is.EqualTo(new[] { "C#", ".NET", "Azure" }));
    }

    /// <summary>No words, no row.</summary>
    [Test]
    public void NoWords_DrawNoRow()
    {
        using var ctx = new BunitContext();

        var row = ctx.Render<TagList>(parameters => parameters.Add(t => t.Tags, []));

        Assert.That(row.Markup.Trim(), Is.Empty);
    }
}
