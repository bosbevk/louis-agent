using LouisAgent.Web.Components.Site.Actions;
using LouisAgent.Web.Components.Widget.Door;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site.Actions;

/// <summary>
/// A button that asks the agent (Louis: "a request to the ai to take their details"): one of the theme's buttons,
/// whose question goes through the guide's door, as the guide button's does.
/// </summary>
public class AskButtonTests
{
    /// <summary>It is the theme's main button by default, and a press asks the guide its question.</summary>
    [Test]
    public void APress_AsksTheGuideItsQuestion()
    {
        using var ctx = new BunitContext();
        var door = new GuideDoor();
        string? asked = null;
        door.Asked += question => asked = question;
        ctx.Services.AddSingleton(door);

        var button = ctx.Render<AskButton>(p => p.Add(b => b.Text, "Take my details")
            .Add(b => b.Question, "Please take my details for a quote.")).Find("button");
        button.Click();

        Assert.Multiple(() =>
        {
            Assert.That(button.TextContent, Is.EqualTo("Take my details"));
            Assert.That(button.ClassList, Is.EquivalentTo(new[] { "btn", "btn-fill" }));
            Assert.That(asked, Is.EqualTo("Please take my details for a quote."));
        });
    }
}
