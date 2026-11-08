using System.Net;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Testing;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Recorded;

/// <summary>
/// A recorded session plays in the chat's own view (F120): the site hands over the recording's address and the
/// question, the answer types out as it did, and the ending (the offer) shows once the play is over, with a button to
/// play it again.
/// </summary>
public class RecordedChatTests
{
    private const string Source = "recordings/session.sse";

    /// <summary>A context whose site serves the recorded Puls turn at <see cref="Source"/>, or nothing.</summary>
    private static BunitContext Context(bool served = true)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var handler = new StubHandler(request => served && request.RequestUri!.AbsolutePath.EndsWith(Source)
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(SessionStreamFixture.StreamFile)),
            }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        ctx.Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://site.test/") });
        return ctx;
    }

    /// <summary>The recording with no waits, the question asked, and an ending that names itself.</summary>
    private static IRenderedComponent<RecordedChat> Render(BunitContext ctx) =>
        ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Source, Source)
            .Add(chat => chat.Question, SessionStreamFixture.Question)
            .Add(chat => chat.Title, "the agent at work")
            .Add(chat => chat.PlayAgain, "Play again")
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.Ending, Offer));

    /// <summary>The site's ending: an offer the tests can find.</summary>
    private static readonly RenderFragment Offer =
        builder => builder.AddMarkupContent(0, "<p class=\"offer\">Yours?</p>");

    /// <summary>The answer the recording ends on.</summary>
    private static async Task<string> AnswerAsync() =>
        (await SessionStreamFixture.ReplayAsync()).Blocks.Last(block => block.Kind == BlockKind.Text).Text;

    /// <summary>The question shows, and the recorded answer types out in the chat's own view.</summary>
    [Test]
    public async Task TheRecording_PlaysInTheChatsView()
    {
        using var ctx = Context();
        string answer = await AnswerAsync();

        var cut = Render(ctx);

        cut.WaitForAssertion(() => Assert.That(cut.Find(".recorded-chat .messages").TextContent,
            Does.Contain(answer[..40])));
        Assert.That(cut.Markup, Does.Contain("the agent at work"));
    }

    /// <summary>The ending waits for the play to finish, and play again starts it over.</summary>
    [Test]
    public void TheEnding_ShowsWhenThePlayIsOver_AndPlaysAgain()
    {
        using var ctx = Context();

        var cut = Render(ctx);
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));
        cut.Find(".recorded-again").Click();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));
        Assert.That(cut.FindAll(".recorded-chat .turn").Count, Is.LessThanOrEqualTo(1), "one turn, played again");
    }

    /// <summary>A recording the site can't serve shows the ending at once, never an empty window that waits.</summary>
    [Test]
    public void AMissingRecording_GoesStraightToTheEnding()
    {
        using var ctx = Context(served: false);

        var cut = Render(ctx);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));
    }
}
