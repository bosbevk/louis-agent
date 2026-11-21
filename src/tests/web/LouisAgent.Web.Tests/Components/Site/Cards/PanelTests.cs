using LouisAgent.Web.Components.Site.Cards;

namespace LouisAgent.Web.Tests.Components.Site.Cards;

/// <summary>A panel is the framework's: small label words over its heading, then what a site puts in it.</summary>
public class PanelTests
{
    /// <summary>Its label words, its heading and its content, in that order.</summary>
    [Test]
    public void APanel_DrawsItsSlots_InOrder()
    {
        using var ctx = new BunitContext();

        var panel = ctx.Render<Panel>(parameters => parameters
            .Add(p => p.Topic, "Engine · C#")
            .Add(p => p.Title, "The loop")
            .AddChildContent("<p>What it does.</p>"));

        Assert.That(panel.FindAll("div.panel > *").Select(element => element.TextContent),
            Is.EqualTo(new[] { "Engine · C#", "The loop", "What it does." }));
    }

    /// <summary>A panel given no label words draws none.</summary>
    [Test]
    public void APanelWithNoTopic_DrawsNone()
    {
        using var ctx = new BunitContext();

        var panel = ctx.Render<Panel>(parameters => parameters.Add(p => p.Title, "The loop"));

        Assert.That(panel.FindAll(".panel-topic"), Is.Empty);
    }
}
