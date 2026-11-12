using System.Text.Json;
using LouisAgent.Composition.Tools;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Packs.Site.Tools;
using LouisAgent.Packs.Site.Tools.Forms;
using Microsoft.Extensions.AI;

namespace LouisAgent.Packs.Site.Tests.Tools;

/// <summary>
/// <c>AskFor</c> (F127.4.3, Louis: "can the agent offer a card where I can put my name in?"): the agent asks for what
/// it needs with a form of one to four fields, the widget draws it, and the filled form comes back as the visitor's
/// own next message. The tool checks the form and refuses what wouldn't make a good one, saying why.
/// </summary>
public class AskForTests
{
    private readonly CardTools _tools = new();

    /// <summary>The guesthouse's form before it sends a booking: a name and a way to reach the guest.</summary>
    private static readonly FormField[] Contact = [new("Name", FieldKind.Name), new("Phone or email", FieldKind.Text)];

    /// <summary>A good form is asked; the model hears the answer comes back as the visitor's message.</summary>
    [Test]
    public void AGoodForm_IsAsked()
    {
        string answer = _tools.AskFor("Who shall I put the request under?", Contact);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Asked:"));
            Assert.That(answer, Does.Contain("2 to fill in").And.Contain("next message"));
        });
    }

    /// <summary>Asking ends the turn, as offering does (FX138), so the agent waits for the visitor's answer.</summary>
    [Test]
    public void Asking_EndsTheTurn() =>
        Assert.That(typeof(CardTools).GetMethod(nameof(CardTools.AskFor))!.IsDefined(typeof(EndsTurnAttribute), false),
            Is.True);

    /// <summary>
    /// The model writes a field's kind by name, the long box too (Louis's pick: both kinds of input), and the engine's
    /// own function hands it to the tool.
    /// </summary>
    [Test]
    public async Task AKindByName_ReachesTheTool()
    {
        var function = AgentCatalog.FunctionOf(typeof(CardTools).GetMethod(nameof(CardTools.AskFor))!,
            _ => new CardTools());
        using var input = JsonDocument.Parse(
            """{"question":"What is the job?","fields":[{"label":"The job","kind":"LongText"}]}""");
        var arguments = new AIFunctionArguments(
            input.RootElement.EnumerateObject().ToDictionary(p => p.Name, object? (p) => p.Value.Clone()));

        object? answer = await function.InvokeAsync(arguments);

        Assert.That(answer?.ToString(), Does.StartWith("Asked:").And.Contain("1 to fill in"));
    }

    /// <summary>
    /// The kinds the web draws are these, by name: the web's tests hold the same list, since neither may reference
    /// the other (ADR 0027).
    /// </summary>
    [Test]
    public void TheKinds_AreTheWebs() =>
        Assert.That(Enum.GetNames<FieldKind>(), Is.EqualTo(new[] { "Name", "Phone", "Email", "Text", "LongText" }));

    /// <summary>No field asks nothing, and five crowd the chat: both are refused, saying how many may be.</summary>
    [TestCase(0)]
    [TestCase(5)]
    public void TooFewOrTooMany_AreRefused(int count)
    {
        var fields = Enumerable.Range(1, count).Select(n => new FormField($"Field {n}", FieldKind.Text)).ToArray();

        Assert.That(_tools.AskFor("What?", fields), Does.StartWith("Error: not asked.").And.Contain("one to four"));
    }

    /// <summary>A question is needed: the form says what it asks.</summary>
    [Test]
    public void AnEmptyQuestion_IsRefused() =>
        Assert.That(_tools.AskFor(" ", Contact), Does.StartWith("Error: not asked.").And.Contain("question"));

    /// <summary>A label sits over its box: an empty one or one over 30 characters is refused, naming it.</summary>
    [TestCase("")]
    [TestCase("Your full name as on your ID document")]
    public void ABadLabel_IsRefused(string label)
    {
        FormField[] fields = [new(label, FieldKind.Name), new("Phone", FieldKind.Phone)];

        Assert.That(_tools.AskFor("Who?", fields),
            Does.StartWith("Error: not asked.").And.Contain("Field 1").And.Contain("30"));
    }

    /// <summary>Two fields with the same label would send an answer no one can tell apart: refused.</summary>
    [Test]
    public void TheSameLabelTwice_IsRefused()
    {
        FormField[] fields = [new("Name", FieldKind.Name), new("name", FieldKind.Text)];

        Assert.That(_tools.AskFor("Who?", fields), Does.StartWith("Error: not asked.").And.Contain("same"));
    }

    /// <summary>A kind the form can't draw (a number the model made up) is refused, naming the kinds there are.</summary>
    [Test]
    public void AnUnknownKind_IsRefused()
    {
        FormField[] fields = [new("Name", (FieldKind)42)];

        Assert.That(_tools.AskFor("Who?", fields),
            Does.StartWith("Error: not asked.").And.Contain("LongText"));
    }
}
