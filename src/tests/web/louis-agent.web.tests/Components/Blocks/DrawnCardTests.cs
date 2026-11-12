using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Components;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Blocks;

/// <summary>
/// A card the agent draws (F127.4.1): a <c>ShowCard</c> call in the stream is drawn in the chat as the diagrams
/// library's ticket, in the guide's widget and a business's window alike, so with the work hidden too; a call the
/// library wouldn't draw, or one that failed, draws nothing.
/// </summary>
public class DrawnCardTests
{
    /// <summary>The guesthouse's booking request, as the model sends it.</summary>
    private const string Booking =
        """
        {
          "kicker": "BOOKING REQUEST",
          "title": "The garden room",
          "subtitle": "Seemeeu Guesthouse · Yzerfontein",
          "rows": [
            { "icon": "calendar", "label": "Dates", "value": "Fri 14 to Sun 16 Nov" },
            { "icon": "users", "label": "Guests", "value": "2 adults" }
          ],
          "status": { "label": "Not sent yet", "tone": "amber" },
          "note": "The guesthouse confirms by email."
        }
        """;

    /// <summary>A finished call with its input, a ShowCard unless named otherwise.</summary>
    private static Block Call(string? input, bool failed = false, string tool = CardCall.Tool) => new()
    {
        Kind = BlockKind.Tool,
        ToolName = tool,
        ToolInput = input,
        ToolResult = failed ? "Error: not shown. No." : "Shown: the visitor sees the card.",
        ToolFailed = failed
    };

    /// <summary>The blocks drawn with the work shown or hidden.</summary>
    private static IRenderedComponent<BlocksView> Render(BunitContext ctx, Block block, bool showWork) =>
        ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [block]).Add(x => x.ShowWork, showWork));

    /// <summary>With the work hidden, the card is still drawn: it is the answer, not the agent's work.</summary>
    [TestCase(true)]
    [TestCase(false)]
    public void AShowCardCall_IsDrawnAsTheTicket(bool showWork)
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = Render(ctx, Call(Booking), showWork);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".drawn-card svg[aria-label='BOOKING REQUEST: The garden room']"),
                Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("details.tool"), Is.Empty);
        });
    }

    /// <summary>A call the library wouldn't draw (an icon there is none of), or one that failed, draws no card.</summary>
    [Test]
    public void ABadOrFailedCall_DrawsNoCard()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var unknown = Render(ctx, Call(Booking.Replace("\"calendar\"", "\"unicorn\"")), false);
        var failed = Render(ctx, Call(Booking, failed: true), false);

        Assert.Multiple(() =>
        {
            Assert.That(unknown.FindAll(".drawn-card"), Is.Empty);
            Assert.That(failed.FindAll(".drawn-card"), Is.Empty);
        });
    }

    /// <summary>Only a ShowCard call is a card; any other call, or one with no input yet, is none.</summary>
    [Test]
    public void OnlyAShowCardCall_IsACard()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CardCall.SourceOf(Call(Booking))?.Title, Is.EqualTo("BOOKING REQUEST: The garden room"));
            Assert.That(CardCall.SourceOf(Call(Booking, tool: "ShowPage")), Is.Null);
            Assert.That(CardCall.SourceOf(Call(null)), Is.Null);
        });
    }

    /// <summary>
    /// The cards show under the words (Louis's pick on the phone check): the model called the card first, then wrote,
    /// then offered, and the chat still reads words, card, choices, so "the card below" is always true.
    /// </summary>
    [Test]
    public void TheCards_ShowUnderTheWords()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        Block[] blocks =
        [
            Call(Booking),
            new()
            {
                Kind = BlockKind.Text,
                Text = "Here is your request."
            },
            Call("""{ "question": "Send it?", "choices": [ { "label": "Book it" }, { "label": "Not now" } ] }""",
                tool: OfferCall.Tool)
        ];

        var cut = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [.. blocks]).Add(x => x.ShowWork, false));

        Assert.That(cut.FindAll(".markdown, .drawn-card, .offer").Select(element => element.ClassList[0]),
            Is.EqualTo(new[] { "markdown", "drawn-card", "offer" }));
    }

    /// <summary>The drawn card matches its golden file.</summary>
    [Test]
    public void TheDrawnCard_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = Render(ctx, Call(Booking), false);

        Snapshots.Match("Components/Blocks/DrawnCard.html", cut.Markup);
    }
}
