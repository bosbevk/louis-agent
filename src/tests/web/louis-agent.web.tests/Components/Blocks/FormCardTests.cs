using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Calls.Forms;
using LouisAgent.Web.Components;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Blocks;

/// <summary>
/// A form the agent asks with (F127.4.3, Louis: "can the agent offer a card where I can put my name in?"): an
/// <c>AskFor</c> call is drawn as a box per field under the answer, with the work hidden too; Send sends what was
/// filled in as the visitor's message, and once the visitor has answered, the form folds to what they sent.
/// </summary>
public class FormCardTests
{
    /// <summary>The guesthouse's form before it sends a booking, as the model sends it.</summary>
    private const string Contact =
        """
        {
          "question": "Who shall I put the request under?",
          "fields": [
            { "label": "Name", "kind": "Name" },
            { "label": "Phone", "kind": "Phone" },
            { "label": "Email", "kind": "Email" },
            { "label": "Anything else", "kind": "LongText" }
          ]
        }
        """;

    /// <summary>A finished call with its input, an AskFor unless named otherwise.</summary>
    private static Block Call(string? input, bool failed = false, string tool = AskForCall.Tool) => new()
    {
        Kind = BlockKind.Tool,
        ToolName = tool,
        ToolInput = input,
        ToolResult =
            failed ? "Error: not asked. No." : "Asked: the visitor sees the form under your words, 4 to fill in.",
        ToolFailed = failed
    };

    /// <summary>A context with the chat's words, before anything renders.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>The blocks with the work hidden, as a business's window and the guide's widget draw them.</summary>
    private static IRenderedComponent<BlocksView> Render(BunitContext ctx, Block block, string? answer = null,
        bool streaming = false, Action<string>? sent = null) =>
        ctx.Render<BlocksView>(p =>
        {
            p.Add(x => x.Blocks, [block]).Add(x => x.ShowWork, false).Add(x => x.Answer, answer)
                .Add(x => x.Streaming, streaming);
            if (sent is not null) p.Add(x => x.Chosen, EventCallback.Factory.Create(new object(), sent));
        });

    /// <summary>Fills each box in order, as the visitor types.</summary>
    private static void Fill(IRenderedComponent<BlocksView> cut, params string[] values)
    {
        for (int at = 0; at < values.Length; at++)
            cut.FindAll(".form-input")[at].Input(values[at]);
    }

    /// <summary>The question and a box per field, each the kind of box its answer needs, the long one a text area.</summary>
    [Test]
    public void AForm_IsDrawnAsBoxes_WithTheWorkHidden()
    {
        using var ctx = Context();

        var cut = Render(ctx, Call(Contact), sent: _ => { });

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".form-question").TextContent, Is.EqualTo("Who shall I put the request under?"));
            Assert.That(cut.FindAll(".form-label").Select(label => label.TextContent),
                Is.EqualTo(new[] { "Name", "Phone", "Email", "Anything else" }));
            Assert.That(cut.FindAll("input.form-input").Select(input => input.GetAttribute("type")),
                Is.EqualTo(new[] { "text", "tel", "email" }));
            Assert.That(cut.FindAll("input.form-input").Select(input => input.GetAttribute("autocomplete")),
                Is.EqualTo(new[] { "name", "tel", "email" }));
            Assert.That(cut.FindAll("textarea.form-input"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>Send waits until every box is filled, then sends them as one message, each by its label.</summary>
    [Test]
    public void Send_WaitsForEveryBox_ThenSendsThemByLabel()
    {
        using var ctx = Context();
        string? sent = null;

        var cut = Render(ctx, Call(Contact), sent: message => sent = message);
        Fill(cut, "John", "066 391 2470", "john@example.co.za");
        bool waitedForTheLast = cut.Find(".form-send").HasAttribute("disabled");
        Fill(cut, "John", "066 391 2470", "john@example.co.za", "Two dogs");
        cut.Find("form").Submit();

        Assert.Multiple(() =>
        {
            Assert.That(waitedForTheLast, Is.True);
            Assert.That(sent,
                Is.EqualTo("Name: John · Phone: 066 391 2470 · Email: john@example.co.za · Anything else: Two dogs"));
        });
    }

    /// <summary>Once the visitor sent the form, it folds to what they sent, with no box left to type in.</summary>
    [Test]
    public void Answered_TheFormFoldsToWhatWasSent()
    {
        using var ctx = Context();
        const string answer = "Name: John · Phone: 066 391 2470 · Email: john@example.co.za · Anything else: Two dogs";

        var cut = Render(ctx, Call(Contact), answer: answer, sent: _ => { });

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".form-input"), Is.Empty);
            Assert.That(cut.FindAll(".form-sent .form-value").Select(value => value.TextContent),
                Is.EqualTo(new[] { "John", "066 391 2470", "john@example.co.za", "Two dogs" }));
        });
    }

    /// <summary>A visitor who typed their own words instead folds the form away: nothing of it was sent.</summary>
    [Test]
    public void AnsweredInTheirOwnWords_NoFormIsLeft()
    {
        using var ctx = Context();

        var cut = Render(ctx, Call(Contact), answer: "Can I bring two dogs?", sent: _ => { });

        Assert.That(cut.FindAll(".form-card"), Is.Empty);
    }

    /// <summary>While the answer still streams, or where nothing can send (a recording), the form can't be filled.</summary>
    [Test]
    public void WhileStreamingOrWithNothingToSend_TheFormWaits()
    {
        using var ctx = Context();

        var streaming = Render(ctx, Call(Contact), streaming: true, sent: _ => { });
        var recorded = Render(ctx, Call(Contact));

        Assert.Multiple(() =>
        {
            Assert.That(streaming.FindAll(".form-input[disabled]"), Has.Count.EqualTo(4));
            Assert.That(recorded.FindAll(".form-input[disabled]"), Has.Count.EqualTo(4));
        });
    }

    /// <summary>
    /// A refused form is a failed call, and the model asks again: it draws nothing, so the visitor never sees a form
    /// twice; any other call, one with no input yet, or one cut short, is no form.
    /// </summary>
    [Test]
    public void OnlyAGoodAskForCall_IsAForm()
    {
        using var ctx = Context();

        var failed = Render(ctx, Call(Contact, failed: true), sent: _ => { });

        Assert.Multiple(() =>
        {
            Assert.That(failed.FindAll(".form-card"), Is.Empty);
            Assert.That(AskForCall.Of(Call(Contact))?.Fields, Has.Count.EqualTo(4));
            Assert.That(AskForCall.Of(Call(Contact, tool: "Offer")), Is.Null);
            Assert.That(AskForCall.Of(Call(null)), Is.Null);
            Assert.That(AskForCall.Of(Call("{ \"question\": ")), Is.Null);
        });
    }

    /// <summary>The form matches its golden file.</summary>
    [Test]
    public void TheForm_MatchesTheGoldenFile()
    {
        using var ctx = Context();

        var cut = Render(ctx, Call(Contact), sent: _ => { });

        Snapshots.Match("Components/Blocks/FormCard.html", cut.Markup);
    }
}
