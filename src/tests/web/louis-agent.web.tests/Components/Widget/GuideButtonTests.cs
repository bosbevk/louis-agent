using LouisAgent.Web.Components.Widget.Door;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget;

/// <summary>
/// A button anywhere on a site can ask the guide in the corner a question (F118, Louis: "we should advertise the agent
/// can take you through the website", "a quick entry point to the walkthrough", "one you cannot miss"): it draws the
/// site's words and, on a click, puts the site's question through the door the widget listens at.
/// </summary>
public class GuideButtonTests
{
    /// <summary>A click asks the guide the button's question, once.</summary>
    [Test]
    public void AClick_AsksTheGuide()
    {
        using var ctx = new BunitContext();
        var door = new GuideDoor();
        ctx.Services.AddSingleton(door);
        var asked = new List<string>();
        door.Asked += asked.Add;

        var cut = ctx.Render<GuideButton>(parameters => parameters
            .Add(button => button.Question, "Walk me through this site.")
            .AddChildContent("Take the tour"));
        cut.Find("button").Click();

        Assert.Multiple(() =>
        {
            Assert.That(asked, Is.EqualTo(new[] { "Walk me through this site." }));
            Assert.That(cut.Find("button.guide-button").TextContent, Does.Contain("Take the tour"));
            Assert.That(cut.Find("button").GetAttribute("type"), Is.EqualTo("button"));
        });
    }

    /// <summary>The flash is the button's own, and stands still for a visitor who asked for less motion.</summary>
    [TestCase("animation: guide-sweep")]
    [TestCase("animation: guide-glow")]
    [TestCase("@media (prefers-reduced-motion: reduce)")]
    public void TheButton_IsOneYouCannotMiss(string rule)
    {
        string rules = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Widget/Door/GuideButton.razor.scss"));

        Assert.That(rules, Does.Contain(rule));
    }
}
