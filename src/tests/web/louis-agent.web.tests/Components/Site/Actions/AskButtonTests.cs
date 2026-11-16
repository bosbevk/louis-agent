using LouisAgent.Web.Components.Site.Actions;

namespace LouisAgent.Web.Tests.Components.Site.Actions;

/// <summary>
/// A button that asks the agent (Louis: "a request to the ai to take their details"): one of the theme's buttons,
/// carrying the question it asks, which the guide's widget picks up.
/// </summary>
public class AskButtonTests
{
    /// <summary>It is the theme's main button by default, and carries its question for the widget.</summary>
    [Test]
    public void TheButton_CarriesItsQuestion()
    {
        using var ctx = new BunitContext();

        var button = ctx.Render<AskButton>(p => p.Add(b => b.Text, "Take my details")
            .Add(b => b.Question, "Please take my details for a quote.")).Find("button");

        Assert.Multiple(() =>
        {
            Assert.That(button.TextContent, Is.EqualTo("Take my details"));
            Assert.That(button.GetAttribute("data-ask"), Is.EqualTo("Please take my details for a quote."));
            Assert.That(button.GetAttribute("type"), Is.EqualTo("button"));
            Assert.That(button.ClassList, Is.EquivalentTo(new[] { "btn", "btn-fill" }));
        });
    }
}
