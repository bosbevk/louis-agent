using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tests.Tools.Web.Replay;
using LouisAgent.Core.Tools.Web.Replay.Store;

namespace LouisAgent.Core.Tests.Tools.Web.Replay.Store;

// F18 step 4a.1: a trial's key reaches the disk only through the safe-name map (ADR 0004), and a missing trial is a miss, not an
// exception (ADR 0007). Written first: the store and the map threw NotImplementedException on the red run.
public class WebReplayStoreTests
{
    private static readonly ReplayAsk Ask = new("https://example.com/", null, null);
    private static readonly ReplayPage Page = new("https://example.com/", "<html/>", [], null, null);

    [TestCase("run_1:cook:1/bobotie")]
    [TestCase("a/b")]
    [TestCase("..")]
    [TestCase("../../x")]
    [TestCase("..\\..\\x")]
    [TestCase("C:\\Windows\\System32")]
    [TestCase("/etc/passwd")]
    [TestCase("sub-session:cook-1")]
    public async Task EveryKey_LandsInOneFileUnderTrials(string key)
    {
        using var folder = new ReplayFolder();

        await folder.Store.SavePageAsync(key, Ask, Page);

        string trials = Path.Combine(folder.Path, "trials");
        Assert.That(Path.GetDirectoryName(folder.Trials.Single()), Is.EqualTo(trials));
        Assert.That(Directory.GetDirectories(folder.Path).Select(Path.GetFileName), Is.EquivalentTo(new[] { "pages", "trials" }));
        Assert.That(Directory.GetFiles(folder.Path), Is.Empty, "nothing beside the two folders");
    }

    [Test]
    public async Task ALongKey_IsCutToTheNameLimit_AndStillLandsUnderTrials()
    {
        using var folder = new ReplayFolder();

        await folder.Store.SavePageAsync(new string('x', 500), Ask, Page);

        string name = Path.GetFileNameWithoutExtension(folder.Trials.Single());
        Assert.That(name.Length, Is.EqualTo(TrialFileName.MaxCleanLength + 1 + 12));
    }

    [Test]
    public async Task TwoKeysThatCleanToTheSameName_GetTwoFiles()
    {
        using var folder = new ReplayFolder();

        await folder.Store.SavePageAsync("run:1/a", Ask, Page);
        await folder.Store.SavePageAsync("run_1_a", Ask, Page);

        Assert.That(folder.Trials, Has.Length.EqualTo(2));
        Assert.That(TrialFileName.Of("run:1/a"), Is.Not.EqualTo(TrialFileName.Of("run_1_a")));
        Assert.That(TrialFileName.Of("run:1/a"), Does.StartWith("run_1_a-"));
    }

    [Test]
    public void ANameThatWouldLeaveTheFolder_Throws_AndTouchesNoFile()
    {
        using var folder = new ReplayFolder();
        string trials = Path.Combine(folder.Path, "trials");

        Assert.Throws<InvalidOperationException>(() => TrialFileName.Under(trials, "../x.jsonl"));
        Assert.Throws<InvalidOperationException>(() => TrialFileName.Under(trials, "../trials-evil/x.jsonl"));
        Assert.Throws<InvalidOperationException>(() => TrialFileName.Under(trials, "/etc/passwd"));
        Assert.That(TrialFileName.Under(trials, "ok.jsonl"), Is.EqualTo(Path.Combine(trials, "ok.jsonl")));
        Assert.That(folder.Entries, Is.Empty);
    }

    [Test]
    public async Task ATrialNobodyRecorded_IsNull_NotAnException()
    {
        using var folder = new ReplayFolder();

        Assert.That(await folder.Store.FindAsync("never", Ask), Is.Null);

        await folder.Store.SavePageAsync("other", Ask, Page);
        Assert.That(await folder.Store.FindAsync("never", Ask), Is.Null);
        Assert.That(await folder.Store.FindAsync("other", Ask with { Url = "https://example.com/other" }), Is.Null);
    }

    [Test]
    public async Task AFoundPage_ComesBackWithItsFields_AndAFoundFailureWithItsText()
    {
        using var folder = new ReplayFolder();
        var page = new ReplayPage("https://example.com/after", "<html>é</html>", ["{}"], "r", [new BrowserCookie("a", "b", ".example.com")]);
        await folder.Store.SavePageAsync("t", Ask, page);
        await folder.Store.SaveFailureAsync("t", Ask with { Url = "https://example.com/bad" }, "it refused: ‘quoted’");

        var found = await folder.Store.FindAsync("t", Ask);
        var failed = await folder.Store.FindAsync("t", Ask with { Url = "https://example.com/bad" });

        Assert.That(found, Is.InstanceOf<ReplayedVisit.Answered>());
        Assert.That(((ReplayedVisit.Answered)found!).Page, Is.EqualTo(page).Using<ReplayPage>(SamePage));
        Assert.That(failed, Is.EqualTo(new ReplayedVisit.Failed("it refused: ‘quoted’")));
    }

    private static bool SamePage(ReplayPage? a, ReplayPage? b) =>
        a is not null && b is not null && a.Url == b.Url && a.Html == b.Html && a.Captured.SequenceEqual(b.Captured)
        && a.ScriptResult == b.ScriptResult && (a.Cookies ?? []).SequenceEqual(b.Cookies ?? []) && (a.Cookies is null) == (b.Cookies is null);
}
