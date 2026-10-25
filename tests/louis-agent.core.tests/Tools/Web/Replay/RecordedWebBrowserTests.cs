using System.Security.Cryptography;
using System.Text.Json;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Replay;
using LouisAgent.Core.Tools.Web.Replay.Store;

namespace LouisAgent.Core.Tests.Tools.Web.Replay;

// F18 step 4a.1: the browser, recorded and replayed by the session's SessionReplay. Written first: VisitAsync threw
// NotImplementedException on the red run.
public class RecordedWebBrowserTests
{
    private static readonly Uri Page = new("https://www.checkers.co.za/search?q=eggs");

    private static readonly BrowserVisit Visit = new("https://www.checkers.co.za/search?q=eggs&r=1", "<html>eggs R54.99</html>",
        ["{\"price\":54.99}", "{\"price\":49.99}"], "{\"store\":\"Malmesbury\"}", [new BrowserCookie("store", "6001", ".checkers.co.za")]);

    private static IDisposable Using(WebReplayMode mode, string? of) => SessionReplay.Use(new SessionReplay(mode, of, Model: false));

    [Test]
    public async Task RecordingThenReplaying_WithABrowserThatThrows_GivesBackTheSameVisit()
    {
        using var folder = new ReplayFolder();
        var live = new FakeBrowser(Visit);
        var dead = new FakeBrowser(throws: new InvalidOperationException("Chrome must not be opened in a replay"));

        using (Using(WebReplayMode.Record, "run_1:cook:1/bobotie"))
            await new RecordedWebBrowser(live, folder.Store).VisitAsync(Page, script: "return 1", readySelector: ".price");
        BrowserVisit replayed;
        using (Using(WebReplayMode.Replay, "run_1:cook:1/bobotie"))
            replayed = await new RecordedWebBrowser(dead, folder.Store).VisitAsync(Page, script: "return 1", readySelector: ".price");

        Assert.That(dead.Calls, Is.Zero);
        Assert.That(replayed.Url, Is.EqualTo(Visit.Url));
        Assert.That(replayed.Html, Is.EqualTo(Visit.Html));
        Assert.That(replayed.Captured, Is.EqualTo(Visit.Captured));
        Assert.That(replayed.ScriptResult, Is.EqualTo(Visit.ScriptResult));
        Assert.That(replayed.Cookies, Is.EqualTo(Visit.Cookies));
    }

    [Test]
    public async Task ARecordingReturnsTheVisitUnchanged()
    {
        using var folder = new ReplayFolder();

        BrowserVisit result;
        using (Using(WebReplayMode.Record, "run_1:cook:1/bobotie"))
            result = await new RecordedWebBrowser(new FakeBrowser(Visit), folder.Store).VisitAsync(Page);

        Assert.That(result, Is.SameAs(Visit));
    }

    [Test]
    public async Task TheIndexLineHoldsTheAsk_TheTime_AndTheHashOfThePageFilesBytes()
    {
        using var folder = new ReplayFolder();

        using (Using(WebReplayMode.Record, "run_1:cook:1/bobotie"))
            await new RecordedWebBrowser(new FakeBrowser(Visit), folder.Store).VisitAsync(Page, script: "return 1", readySelector: ".price");

        var line = JsonDocument.Parse(File.ReadAllLines(folder.Trials.Single()).Single()).RootElement;
        string pageFile = folder.Answers.Single();
        Assert.That(line.GetProperty("trial").GetString(), Is.EqualTo("run_1:cook:1/bobotie"));
        Assert.That(line.GetProperty("kind").GetString(), Is.EqualTo("visit"));
        Assert.That(line.GetProperty("key").GetString(),
            Is.EqualTo($"{{\"url\":\"{Page.AbsoluteUri}\",\"script\":\"return 1\",\"ready_selector\":\".price\"}}"));
        Assert.That(line.GetProperty("at").GetDateTimeOffset(), Is.EqualTo(ReplayFolder.At));
        Assert.That(line.GetProperty("error").ValueKind, Is.EqualTo(JsonValueKind.Null));
        string hash = line.GetProperty("hash").GetString()!;
        Assert.That(hash, Is.EqualTo(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(pageFile)))));
        Assert.That(Path.GetFileName(pageFile), Is.EqualTo(hash + ".json"));
    }

    [Test]
    public async Task TwoTrialsThatRecordTheSamePage_WriteOnePageFile_AndTwoIndexLines()
    {
        using var folder = new ReplayFolder();
        var browser = new RecordedWebBrowser(new FakeBrowser(Visit), folder.Store);

        using (Using(WebReplayMode.Record, "run_1:cook:1/bobotie")) await browser.VisitAsync(Page);
        using (Using(WebReplayMode.Record, "run_1:cook:1/cottage-pie")) await browser.VisitAsync(Page);

        Assert.That(folder.Answers, Has.Length.EqualTo(1));
        Assert.That(folder.Trials, Has.Length.EqualTo(2));
        Assert.That(folder.Trials.Select(t => File.ReadAllLines(t).Length), Is.EqualTo(new[] { 1, 1 }));
    }

    [Test]
    public async Task AVisitThatWasNotRecorded_FailsWithItsUrl_AndTheInnerBrowserIsNeverCalled()
    {
        using var folder = new ReplayFolder();
        var inner = new FakeBrowser(Visit);

        using (Using(WebReplayMode.Replay, "run_1:cook:1/bobotie"))
        {
            var error = Assert.ThrowsAsync<WebBrowserException>(
                async () => await new RecordedWebBrowser(inner, folder.Store).VisitAsync(Page))!;

            Assert.That(error.Message, Does.Contain(Page.AbsoluteUri).And.Contain("not read in the recorded run"));
        }
        Assert.That(inner.Calls, Is.Zero);
        Assert.That(folder.Entries, Is.Empty, "a miss writes nothing");
    }

    [Test]
    public async Task AnAskWithAnotherScriptOrSelector_IsAMiss()
    {
        using var folder = new ReplayFolder();
        using (Using(WebReplayMode.Record, "t")) await new RecordedWebBrowser(new FakeBrowser(Visit), folder.Store).VisitAsync(Page, script: "a", readySelector: ".x");
        var replay = new RecordedWebBrowser(new FakeBrowser(), folder.Store);

        using (Using(WebReplayMode.Replay, "t"))
        {
            Assert.ThrowsAsync<WebBrowserException>(async () => await replay.VisitAsync(Page, script: "b", readySelector: ".x"));
            Assert.ThrowsAsync<WebBrowserException>(async () => await replay.VisitAsync(Page, script: "a", readySelector: ".y"));
            Assert.ThrowsAsync<WebBrowserException>(async () => await replay.VisitAsync(Page));
            Assert.That((await replay.VisitAsync(Page, script: "a", readySelector: ".x")).Html, Is.EqualTo(Visit.Html));
        }
    }

    [Test]
    public async Task ARecordedFailure_ReplaysAsTheSameText_AndHasNoHash()
    {
        using var folder = new ReplayFolder();
        var refused = new FakeBrowser(throws: new WebBrowserException("The page at https://x couldn't run the browser's script: boom."));

        using (Using(WebReplayMode.Record, "t"))
        {
            var recorded = Assert.ThrowsAsync<WebBrowserException>(async () => await new RecordedWebBrowser(refused, folder.Store).VisitAsync(Page))!;
            Assert.That(recorded.Message, Does.Contain("boom"));
        }
        var line = JsonDocument.Parse(File.ReadAllLines(folder.Trials.Single()).Single()).RootElement;
        Assert.That(line.GetProperty("hash").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(folder.Answers, Is.Empty);

        using (Using(WebReplayMode.Replay, "t"))
        {
            var inner = new FakeBrowser(Visit);
            var replayed = Assert.ThrowsAsync<WebBrowserException>(async () => await new RecordedWebBrowser(inner, folder.Store).VisitAsync(Page))!;
            Assert.That(replayed.Message, Is.EqualTo("The page at https://x couldn't run the browser's script: boom."));
            Assert.That(inner.Calls, Is.Zero);
        }
    }

    [Test]
    public async Task ACancel_IsRethrown_AndNeverRecorded()
    {
        using var folder = new ReplayFolder();
        var cancelled = new FakeBrowser(throws: new OperationCanceledException());

        using (Using(WebReplayMode.Record, "t"))
            Assert.ThrowsAsync<OperationCanceledException>(async () => await new RecordedWebBrowser(cancelled, folder.Store).VisitAsync(Page));

        Assert.That(folder.Entries, Is.Empty);
        await Task.CompletedTask;
    }

    [Test]
    public async Task OffAndNoCurrent_PassThrough_AndWriteNothing()
    {
        using var folder = new ReplayFolder();
        var inner = new FakeBrowser(Visit);
        var browser = new RecordedWebBrowser(inner, folder.Store);

        var none = await browser.VisitAsync(Page);
        BrowserVisit off;
        using (Using(WebReplayMode.Off, null)) off = await browser.VisitAsync(Page);

        Assert.That((none, off), Is.EqualTo((Visit, Visit)));
        Assert.That(inner.Calls, Is.EqualTo(2));
        Assert.That(folder.Entries, Is.Empty);
    }

    [Test]
    public async Task ASecondAskOfTheSameKey_ReplaysTheFirstRecording_AndEveryVisitWritesItsOwnIndexLine()
    {
        using var folder = new ReplayFolder();
        var first = Visit with { Html = "<html>first</html>" };
        var second = Visit with { Html = "<html>second</html>" };

        using (Using(WebReplayMode.Record, "t"))
        {
            await new RecordedWebBrowser(new FakeBrowser(first), folder.Store).VisitAsync(Page);
            await new RecordedWebBrowser(new FakeBrowser(second), folder.Store).VisitAsync(Page);
        }

        Assert.That(File.ReadAllLines(folder.Trials.Single()), Has.Length.EqualTo(2));
        Assert.That(folder.Answers, Has.Length.EqualTo(2));
        using (Using(WebReplayMode.Replay, "t"))
        {
            var replay = new RecordedWebBrowser(new FakeBrowser(), folder.Store);
            Assert.That((await replay.VisitAsync(Page)).Html, Is.EqualTo("<html>first</html>"));
            Assert.That((await replay.VisitAsync(Page)).Html, Is.EqualTo("<html>first</html>"), "no counters: the same answer again");
        }
    }

    [TestCase(WebReplayMode.Record)]
    [TestCase(WebReplayMode.Replay)]
    public async Task RecordOrReplayWithNoTrial_IsAnInvalidOperation(WebReplayMode mode)
    {
        using var folder = new ReplayFolder();
        var inner = new FakeBrowser(Visit);

        using (Using(mode, null))
            Assert.ThrowsAsync<InvalidOperationException>(async () => await new RecordedWebBrowser(inner, folder.Store).VisitAsync(Page));

        Assert.That(inner.Calls, Is.Zero);
        await Task.CompletedTask;
    }
}
