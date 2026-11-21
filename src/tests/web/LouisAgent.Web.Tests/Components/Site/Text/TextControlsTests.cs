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
            .Add(intro => intro.Summary, "The engine holds the loop, in `LouisAgent.Core`."));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h3").TextContent, Is.EqualTo("Core, and what sits on it."));
            Assert.That(cut.Find("p.summary").TextContent, Is.EqualTo("The engine holds the loop, in LouisAgent.Core."));
            Assert.That(cut.Find("p.summary code").TextContent, Is.EqualTo("LouisAgent.Core"));
        });
    }

    /// <summary>A term list draws each entry's names as code and its sentence, with its own code as code.</summary>
    [Test]
    public void ATermList_DrawsEachNameAndItsSentence()
    {
        using var ctx = new BunitContext();
        Term[] terms =
        [
            new(["LouisAgent.Core"], "The engine: the tool loop."),
            new(["LouisAgent.Core.Ollama", "LouisAgent.Core.OpenAi"], "In `src/engine`, one model each.")
        ];

        var cut = ctx.Render<TermList>(parameters => parameters.Add(list => list.Terms, terms));
        var items = cut.FindAll("ul.terms > li");

        Assert.Multiple(() =>
        {
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[1].QuerySelectorAll(".term-names code").Select(code => code.TextContent),
                Is.EqualTo(new[] { "LouisAgent.Core.Ollama", "LouisAgent.Core.OpenAi" }));
            Assert.That(items[1].QuerySelector(".term-text code")!.TextContent, Is.EqualTo("src/engine"));
            Assert.That(items[0].QuerySelector(".term-text")!.TextContent, Is.EqualTo("The engine: the tool loop."));
        });
    }

    /// <summary>
    /// A step's number stands centred beside its words, however many lines they run to (Louis: "not centered"), laid
    /// out by Bootstrap's own classes (ADR 0085), and the number never shrinks.
    /// </summary>
    [Test]
    public void AStepsNumber_IsCentredBesideItsWords()
    {
        using var ctx = new BunitContext();
        Step[] steps = [new("Copy the settings.", "Copy `.env.example` to `.env`."), new("Run it.", "Type `dotnet run`.")];

        var cut = ctx.Render<Steps>(parameters => parameters.Add(list => list.Items, steps));
        var items = cut.FindAll("ol.steps > li");

        Assert.Multiple(() =>
        {
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items.Select(item => item.ClassList), Has.All.Contains("d-flex").And.All.Contains("align-items-center"));
            Assert.That(items[1].QuerySelector(".step-number")!.ClassList, Does.Contain("flex-shrink-0"));
            Assert.That(items[1].QuerySelector(".step-number")!.TextContent, Is.EqualTo("2"));
        });
    }
}
