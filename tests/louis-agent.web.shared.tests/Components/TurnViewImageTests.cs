using LouisAgent.Testing;
using LouisAgent.SessionStream;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Components;
using LouisAgent.Web.Shared.Text;
using Microsoft.Extensions.DependencyInjection;
using static LouisAgent.Web.Shared.Tests.StreamEvents;

namespace LouisAgent.Web.Shared.Tests.Components;

// FX22 (Louis, 2026-10-25: "we need at least an image for the recipe, which we get from the article"): a Puls turn whose cook read a recipe page with an
// image, and whose answer shows it (the parent's text and the delegation card's answer), renders the image in both places; the same answer with the image
// in no result renders a link. Written first: the image was a link whatever page it came from.
public class TurnViewImageTests
{
    private const string Url = "https://example.co.za/wp-content/bobotie.jpg";
    private const string Answer = "Bobotie.\n\n![Bobotie](" + Url + ")\n\nSmaaklik.";

    private static Turn Cooked(bool pageHasImage)
    {
        string page = pageHasImage ? "# Bobotie ![Bobotie](" + Url + ")" : "# Bobotie";
        var turn = new Turn();
        foreach (var streamEvent in new[]
                 {
                     ToolUse("d1", "StartSubSession", new { toolsets = new[] { SampleToolset.Alpha }, message = "Kook", role = "puls-cook" }),
                     InSubSession("d1", ToolUse("c1", "FetchUrl", new { url = "https://example.co.za/bobotie" })),
                     InSubSession("d1", ToolResult("c1", page)),
                     InSubSession("d1", TextStart()),
                     InSubSession("d1", TextDelta(Answer)),
                     ToolResult("d1", Answer),
                     TextStart(),
                     TextDelta(Answer),
                 })
            TurnStream.Apply(turn, streamEvent);
        return turn;
    }

    private static int ImageCount(Turn turn)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var cut = ctx.Render<TurnView>(p => p.Add(x => x.Turn, turn).Add(x => x.Streaming, false));
        return cut.FindAll("img").Count(image => image.GetAttribute("src") == Url);
    }

    [Test]
    public void TheRecipesImage_RendersInTheAnswer_AndInTheDelegationCard() =>
        Assert.That(ImageCount(Cooked(pageHasImage: true)), Is.EqualTo(2), "the parent's text and the card's answer");

    [Test]
    public void TheSameAnswer_WhenNoResultGaveTheImage_RendersALink() =>
        Assert.That(ImageCount(Cooked(pageHasImage: false)), Is.Zero);
}
