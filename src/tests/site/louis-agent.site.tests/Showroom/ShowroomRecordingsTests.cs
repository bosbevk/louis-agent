using System.Net;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Showroom;

/// <summary>
/// F127.1: every showroom conversation plays to its end in the recorded chat, read from its files as the site serves
/// them, so a demo page never hangs on a recording (a guesthouse page froze in Chrome on 2026-11-10).
/// </summary>
public class ShowroomRecordingsTests
{
    private static readonly string Recordings =
        Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site/wwwroot/recordings/showroom");

    /// <summary>A business's conversation, played with no waits, reaches its ending with every turn answered.</summary>
    [TestCase("wheatfield-kitchen")]
    [TestCase("koringland-attorneys")]
    [TestCase("seemeeu-guesthouse")]
    public void ABusinesssConversation_PlaysToItsEnd(string business)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(new HttpClient(new StubHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(File.ReadAllText(Path.Combine(Recordings,
                Path.GetFileName(request.RequestUri!.AbsolutePath)))),
        }))
        { BaseAddress = new Uri("http://site.test/") });
        var turns = Enumerable.Range(1, 4)
            .Select(n => new RecordedTurn($"Question {n}", $"recordings/showroom/{business}-{n}.sse")).ToList();

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, turns)
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.PlayAgain, "Again"));

        cut.WaitForElement(".recorded-end", TimeSpan.FromSeconds(10));
        Assert.That(cut.FindAll(".turn"), Has.Count.EqualTo(4));
    }
}
