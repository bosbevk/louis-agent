using LouisAgent.Web.Components.Widget.Pieces;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Pieces;

/// <summary>
/// The pieces of a page carry the design's tag while the guide is there (FX163, Louis: "read the headings implement
/// what it does"): the watcher hands its module the site's three words, one for each state a piece is in.
/// </summary>
public class PieceWatchTests
{
    /// <summary>
    /// The module is told to ask, to say you picked it, and to say the agent shows it, in the site's words.
    /// </summary>
    [Test]
    public void TheWatch_HandsTheModuleTheTagsWords()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var text = new ChatText();
        ctx.Services.AddSingleton(text);
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.PieceWatch);

        ctx.Render<PieceWatch>();

        var watch = module.Invocations.Single(call => call.Identifier == "watch");
        Assert.That(watch.Arguments[1], Is.EqualTo(new PieceTags(text.PieceAsk, text.PiecePicked, text.PieceShown)));
        Assert.That(new[] { text.PieceAsk, text.PiecePicked, text.PieceShown },
            Is.EqualTo(new[] { "Ask about this", "You picked this", "Agent shows this" }));
    }

    /// <summary>
    /// A click away from the pieces lets the pick go (Louis: "I clicked away and the fucker is still ticked"): the
    /// module says so, and the widget hears it.
    /// </summary>
    [Test]
    public async Task AClickAway_LetsThePickGo()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        ctx.JSInterop.SetupModule(FrameworkScripts.PieceWatch);
        bool letGo = false;
        var cut = ctx.Render<PieceWatch>(p => p.Add(w => w.LetGo, () => letGo = true));

        await cut.InvokeAsync(() => cut.Instance.LetGoAsync());

        Assert.That(letGo, Is.True);
    }
}
