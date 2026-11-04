using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Replay.Answers;
using LouisAgent.Core.Sessions.Replay.Store;

namespace LouisAgent.Core.Tests.Sessions.Replay.Store;

// F18 step 4a.2 (Louis's fold-in), generic since 4b.1: a recording is an answer or a failure, never both. OrThrow gives the value, or throws
// the exception the caller's function builds from the recorded text. Written first: ReplayAnswer<T> didn't exist on the red run.
public class ReplayAnswerTests
{
    [Test]
    public void AnAnsweredRecording_GivesItsValue()
    {
        var page = new ReplayPage("https://example.com/after", "<html>é</html>", ["{}"], "r",
            [new BrowserCookie("a", "b", ".example.com")]);

        var value = new ReplayAnswer<ReplayPage>.Answered(page).OrThrow(error => new InvalidOperationException(error));

        Assert.That(value, Is.SameAs(page));
    }

    [Test]
    public void AFailedRecording_ThrowsExactlyTheExceptionTheFunctionBuilds_WithTheRecordedText()
    {
        string? seen = null;
        var built = new WebBrowserException("built");

        var thrown = Assert.Throws<WebBrowserException>(() =>
            new ReplayAnswer<ReplayPage>.Failed("The page at https://x refused it.").OrThrow(error =>
            {
                seen = error;
                return built;
            }))!;

        Assert.That(thrown, Is.SameAs(built));
        Assert.That(seen, Is.EqualTo("The page at https://x refused it."));
    }
}
