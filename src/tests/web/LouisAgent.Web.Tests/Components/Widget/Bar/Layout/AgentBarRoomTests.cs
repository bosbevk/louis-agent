using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Layout;

/// <summary>
/// F134.3, F134's rule 5: the page keeps clear of the bar. The bar is fixed over the page, so it tells the page how
/// tall it stands, its chips or the answer over it and all, and the frame that draws it keeps that room free.
/// </summary>
public class AgentBarRoomTests
{
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web/Components");

    /// <summary>The bar over a runner with no talk yet.</summary>
    private static IRenderedComponent<AgentBar> Bar(BunitContext ctx)
    {
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(new ChatText());
        var runner = new ChatRunner(api, new ChatStore(ctx.JSInterop.JSRuntime, settings), new ChatText(),
            () => Task.FromResult("sess_fixture"));
        return ctx.Render<AgentBar>(parameters => parameters.Add(bar => bar.Runner, runner));
    }

    /// <summary>Once drawn the bar asks its module to keep its room, and when it goes, to let the room go.</summary>
    [Test]
    public async Task TheBar_KeepsItsRoomOnceDrawn_AndLetsItGoWhenItGoes()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Bar);
        Bar(ctx);

        await ctx.DisposeComponentsAsync();

        Assert.That(module.Invocations.Select(call => call.Identifier), Is.EqualTo(new[] { "keepRoom", "letGo" }));
    }

    /// <summary>
    /// The module writes the room on the page by the name the showroom's frame reads, and the start's deck is as
    /// tall as the screen leaves it once that room is kept, so the whole stage stands clear of the bar.
    /// </summary>
    [Test]
    public void TheModule_TheFrame_AndTheStart_UseTheSameNames()
    {
        string module = File.ReadAllText(Path.Combine(Web, "Widget/Bar/AgentBar.razor.js"));
        string frame = File.ReadAllText(Path.Combine(Web, "Site/Showroom/Shell/ShowroomShell.razor.scss"));
        string start = File.ReadAllText(Path.Combine(Web, "Site/Showroom/Start/ShowroomStart.razor.scss"));

        Assert.Multiple(() =>
        {
            Assert.That(module, Does.Contain("\"--bar-room\""));
            Assert.That(frame, Does.Contain("--corner-room: var(--bar-room, 150px);"));
            Assert.That(start, Does.Contain("100dvh - var(--start-rest) - var(--corner-room, 0px)"));
        });
    }

    /// <summary>
    /// The room never drops under the resting bar's, its chips and all. The chips step away each time the guide
    /// answers, and a page that took their room back would shift under the visitor every time.
    /// </summary>
    [Test]
    public void TheRoom_KeepsTheChipsRoom_WhileTheyAreAway()
    {
        string module = File.ReadAllText(Path.Combine(Web, "Widget/Bar/AgentBar.razor.js"));

        Assert.Multiple(() =>
        {
            Assert.That(module, Does.Contain("querySelector(\".agent-chips\")"));
            Assert.That(module, Does.Contain("scrollHeight"), "the chips' own height, though folded away");
            Assert.That(module, Does.Contain("Math.max(standing, restingRoomOf(bar))"));
        });
    }

    /// <summary>
    /// FX192: while the bar is closed its chips stand round the orb, not over its row, so their room is from their
    /// own top. Counted over the row they claimed 384 pixels of a 1080 screen where they stand in 234, and the
    /// showroom's deck shrank to its least.
    /// </summary>
    [Test]
    public void ChipsRoundTheOrb_AreCountedFromTheirOwnTop()
    {
        string module = File.ReadAllText(Path.Combine(Web, "Widget/Bar/AgentBar.razor.js"));

        Assert.Multiple(() =>
        {
            Assert.That(module, Does.Contain("getComputedStyle(chips).position === \"absolute\""),
                "the chips stand round the orb when they are placed apart from the bar's flow");
            Assert.That(module, Does.Contain("chips.getBoundingClientRect().top"));
        });
    }

    /// <summary>
    /// On a screen less tall the start gives up its summary, and on a short one some of its size too, so the deck
    /// keeps the height its windows need and the stage still stands on one screen above the bar.
    /// </summary>
    [Test]
    public void OnAShorterScreen_TheStartGivesUpItsLede_ToKeepItsDeck()
    {
        string start = File.ReadAllText(Path.Combine(Web, "Site/Showroom/Start/ShowroomStart.razor.scss"))
            .ReplaceLineEndings("\n");
        int at = start.IndexOf("@media (min-width: 901px) and (max-height: 1000px) {", StringComparison.Ordinal);
        string shorter = at < 0 ? "" : start[at..start.IndexOf("\n}", at, StringComparison.Ordinal)];

        Assert.Multiple(() =>
        {
            Assert.That(shorter, Does.Contain(".start-summary { display: none; }").And.Contain("--start-rest:"));
            Assert.That(start, Does.Contain("@media (min-width: 901px) and (max-height: 820px) {"));
            Assert.That(start, Does.Contain("--deck-least: 290px;").And.Not.Contain("--deck-least: 200px;"),
                "never so low that a window can't keep its website and chat side by side");
        });
    }
}
