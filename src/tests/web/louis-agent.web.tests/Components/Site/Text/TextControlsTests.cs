using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Tests.Components.Site.Text;

/// <summary>
/// The first two controls a site configures instead of writing HTML (ADR 0125, Louis: "all of H3 and p in a control
/// and all of ul and kids in a control"): a heading with its summary, and a list of named entries.
/// </summary>
public class TextControlsTests
{
    /// <summary>An intro is its heading and its summary, and code in the summary is code.</summary>
    [Test]
    public void AnIntro_IsItsHeadingAndItsLede()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Intro>(parameters => parameters
            .Add(intro => intro.Title, "Core, and what sits on it.")
            .Add(intro => intro.Summary, "The engine holds the loop, in `louis-agent.core`."));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h3").TextContent, Is.EqualTo("Core, and what sits on it."));
            Assert.That(cut.Find("p.summary").TextContent, Is.EqualTo("The engine holds the loop, in louis-agent.core."));
            Assert.That(cut.Find("p.summary code").TextContent, Is.EqualTo("louis-agent.core"));
        });
    }

    /// <summary>A term list draws each entry's names as code and its sentence, with its own code as code.</summary>
    [Test]
    public void ATermList_DrawsEachNameAndItsSentence()
    {
        using var ctx = new BunitContext();
        Term[] terms =
        [
            new(["louis-agent.core"], "The engine: the tool loop."),
            new(["louis-agent.core.ollama", "louis-agent.core.openai"], "In `src/engine`, one model each.")
        ];

        var cut = ctx.Render<TermList>(parameters => parameters.Add(list => list.Terms, terms));
        var items = cut.FindAll("ul.terms > li");

        Assert.Multiple(() =>
        {
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[1].QuerySelectorAll(".term-names code").Select(code => code.TextContent),
                Is.EqualTo(new[] { "louis-agent.core.ollama", "louis-agent.core.openai" }));
            Assert.That(items[1].QuerySelector(".term-text code")!.TextContent, Is.EqualTo("src/engine"));
            Assert.That(items[0].QuerySelector(".term-text")!.TextContent, Is.EqualTo("The engine: the tool loop."));
        });
    }
}
