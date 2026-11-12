using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Components.Blocks;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Blocks;

/// <summary>
/// FX133's live check on Louis's machine: told in the prompt to offer the demo live at *What to watch*, the guide kept
/// offering the walkthrough's three choices (Louis: "still have not guided me"). On a page where a business's window
/// shows, the cards carry Ask it live in code; elsewhere, and where the guide offered it already, they don't.
/// </summary>
public class OfferLiveCardTests
{
    /// <summary>A walkthrough stop's choices, as the guide sends them.</summary>
    private static readonly Offered Stop = new("Shall we carry on?",
        [new OfferChoice("On to Make it yours", Suggested: true), new("Explain another section"), new("Stop here")]);

    /// <summary>
    /// The cards on a page with or without a business's window, a guide's or a window's own, and their labels.
    /// </summary>
    private static IReadOnlyList<string> Labels(Offered offer, bool window, bool guide = true)
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        var windows = new AgentWindows();
        if (window) windows.Show("seemeeu-guesthouse");
        ctx.Services.AddSingleton(windows);

        var cut = ctx.Render<OfferCards>(p => p.Add(c => c.Offer, offer).Add(c => c.Chosen, (string _) => { })
            .Add(c => c.OffersLive, guide));
        return cut.FindAll(".offer-label").Select(label => label.TextContent).ToList();
    }

    /// <summary>With a business's window on the page the cards end with Ask it live; without one they don't.</summary>
    [Test]
    public void WithABusinessWindowShown_TheCardsOfferItLive()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Labels(Stop, window: true), Does.Contain("Ask it live"));
            Assert.That(Labels(Stop, window: false), Does.Not.Contain("Ask it live"));
            Assert.That(Labels(Stop, window: true, guide: false), Does.Not.Contain("Ask it live"),
                "a business's own window's cards book and send; they never offer it live");
            Assert.That(
                Labels(new Offered("Live?", [new OfferChoice("Ask it live"), new("Just the recording")]), window: true)
                    .Count(label => label == "Ask it live"), Is.EqualTo(1), "offered already: not twice");
        });
    }
}
