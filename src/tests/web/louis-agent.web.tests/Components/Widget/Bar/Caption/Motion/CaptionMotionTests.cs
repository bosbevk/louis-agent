using LouisAgent.Web.Components.Widget.Bar.Caption;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Caption.Motion;

/// <summary>
/// F154: the guide's answer grows in from where it comes from, and its words, while they still stream, come at a
/// steady pace (Louis's pick A of three); an answer already done shows whole, so a reload never types it out again.
/// </summary>
public class CaptionMotionTests
{
    /// <summary>A long answer, more than a frame's worth of letters.</summary>
    private const string Long =
        "This is a made-up law practice. Its agent knows what it may not answer, and says so, instead of guessing.";

    /// <summary>A context with the caption's words and the browser's stand-in.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>The caption with these words, still streaming or done.</summary>
    private static IRenderedComponent<AgentCaption> Caption(BunitContext ctx, bool streaming) =>
        ctx.Render<AgentCaption>(parameters => parameters
            .Add(c => c.Title, "Louis · AI")
            .Add(c => c.Words, Long)
            .Add(c => c.Work, new AnswerWork([], 0, null))
            .Add(c => c.Streaming, streaming));

    /// <summary>The words the caption shows just now.</summary>
    private static string Shown(IRenderedComponent<AgentCaption> cut) =>
        cut.FindAll(".caption-says").Select(says => says.TextContent.Trim()).FirstOrDefault() ?? "";

    /// <summary>Words still streaming show fewer than have arrived at first, and all of them once the answer is done.</summary>
    [Test]
    public void StreamingWords_ComeAtAPace_AndAllShowOnceDone()
    {
        using var ctx = Context();
        var cut = Caption(ctx, streaming: true);

        string first = Shown(cut);
        cut.Render(parameters => parameters.Add(c => c.Streaming, false));

        cut.WaitForState(() => Shown(cut) == Long, TimeSpan.FromSeconds(5));

        Assert.That(first.Length, Is.LessThan(Long.Length));
    }

    /// <summary>
    /// F156: an answer still arriving that the server already paced shows its words as they come, whole, since a
    /// second pacing would only add lag (Louis: "web and engine no API").
    /// </summary>
    [Test]
    public void AServerPacedAnswer_ShowsItsWordsAsTheyCome()
    {
        using var ctx = Context();

        var cut = ctx.Render<AgentCaption>(parameters => parameters
            .Add(c => c.Title, "Louis · AI")
            .Add(c => c.Words, Long)
            .Add(c => c.Work, new AnswerWork([], 0, null))
            .Add(c => c.Streaming, true)
            .Add(c => c.ServerPaced, true));

        Assert.That(Shown(cut), Is.EqualTo(Long));
    }

    /// <summary>An answer already done when its caption draws shows whole at once.</summary>
    [Test]
    public void ADoneAnswer_ShowsWholeAtOnce()
    {
        using var ctx = Context();

        var cut = Caption(ctx, streaming: false);

        Assert.That(Shown(cut), Is.EqualTo(Long));
    }

    /// <summary>
    /// When the caption goes it always lets go of the page, the place it lit and the page's dimming, even if its motion
    /// module failed (Louis: "click closed and stayed like this").
    /// </summary>
    [Test]
    public async Task GoingAway_ItLetsGoOfThePage_EvenIfItsMotionFailed()
    {
        using var ctx = Context();
        // Its motion module fails, as one the library does not serve does; bUnit cannot fail the load itself, so the
        // module's first call fails instead.
        var motion = ctx.JSInterop.SetupModule(FrameworkScripts.CaptionMotion);
        motion.Setup<bool>("lessMotion").SetException(new Microsoft.JSInterop.JSException("not served"));
        var standing = ctx.JSInterop.SetupModule(FrameworkScripts.Caption);
        standing.Mode = JSRuntimeMode.Loose;
        var cut = Caption(ctx, streaming: false);

        await cut.Instance.DisposeAsync();

        Assert.That(standing.Invocations.Count(call => call.Identifier == "letGo"), Is.EqualTo(1));
    }

    /// <summary>
    /// Standing in a card's place, the caption keeps the card's width but fits its words, with no empty stretch under
    /// them (Louis's pick C).
    /// </summary>
    [Test]
    public void InACardsPlace_TheCaptionFitsItsWords()
    {
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Widget/Bar/Caption/AgentCaption.razor.scss"));

        Assert.That(styles, Does.Not.Contain("min-height: var(--caption-h);"));
    }

    /// <summary>The caption asks its motion module to grow it in when it first draws, and not again.</summary>
    [Test]
    public void TheCaption_GrowsInOnce()
    {
        using var ctx = Context();
        var motion = ctx.JSInterop.SetupModule(FrameworkScripts.CaptionMotion);
        motion.Mode = JSRuntimeMode.Loose;

        var cut = Caption(ctx, streaming: false);
        cut.Render(parameters => parameters.Add(c => c.Words, Long + " More."));

        Assert.That(motion.Invocations.Count(call => call.Identifier == "growIn"), Is.EqualTo(1));
    }
}
