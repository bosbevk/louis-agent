using LouisAgent.Web.Chats;
using LouisAgent.Web.Components;
using LouisAgent.Web.Text;
using static LouisAgent.Web.Tests.StreamEvents;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components;

/// <summary>
/// A turn whose cook read a recipe page with an image, and whose answer shows it (the parent's text and the delegation
/// card's answer), renders the image in both places; the same answer with the image in no result renders a link.
/// </summary>
public class TurnViewImageTests
{
    private const string Url = "https://example.co.za/wp-content/bobotie.jpg";
    private const string Answer = "Bobotie.\n\n![Bobotie](" + Url + ")\n\nSmaaklik.";

    /// <summary>A turn whose cook read a page, with or without the image on it.</summary>
    private static Turn Cooked(bool pageHasImage)
    {
        string page = pageHasImage ? "# Bobotie ![Bobotie](" + Url + ")" : "# Bobotie";
        var turn = new Turn();
        foreach (var streamEvent in new[]
                 {
                     ToolUse("d1", "AskAgent",
                         new
                         {
                             toolsets = new[] { SampleToolset.Alpha },
                             message = "Kook",
                             role = SampleRole.Alpha
                         }),
                     InSubSession("d1", ToolUse("c1", "FetchUrl", new { url = "https://example.co.za/bobotie" })),
                     InSubSession("d1", ToolResult("c1", page)), InSubSession("d1", TextStart()),
                     InSubSession("d1", TextDelta(Answer)), ToolResult("d1", Answer), TextStart(), TextDelta(Answer)
                 })
            TurnStream.Apply(turn, streamEvent);
        return turn;
    }

    /// <summary>How many images the turn draws.</summary>
    private static int ImageCount(Turn turn)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var cut = ctx.Render<TurnView>(p => p.Add(x => x.Turn, turn).Add(x => x.Streaming, false));
        return cut.FindAll("img").Count(image => image.GetAttribute("src") == Url);
    }

    /// <summary>The recipe's image renders in the answer and in the card.</summary>
    [Test]
    public void TheRecipesImage_RendersInTheAnswer_AndInTheDelegationCard() =>
        Assert.That(ImageCount(Cooked(pageHasImage: true)), Is.EqualTo(2), "the parent's text and the card's answer");

    /// <summary>The same answer, when no result gave the image, renders a link.</summary>
    [Test]
    public void TheSameAnswer_WhenNoResultGaveTheImage_RendersALink() =>
        Assert.That(ImageCount(Cooked(pageHasImage: false)), Is.Zero);
}
