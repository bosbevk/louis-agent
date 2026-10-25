using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tests.Tools.Web.Replay;
using LouisAgent.Core.Tools.Web.Replay.Answers;
using LouisAgent.Core.Sessions.Replay.Store;

namespace LouisAgent.Core.Tests.Sessions.Replay.Store;

// F18 step 4a.1: a trial's key reaches the disk only through the safe-name map (ADR 0004), and a missing trial is a miss, not an
// exception (ADR 0007). Written first: the store and the map threw NotImplementedException on the red run.
public class ReplayStoreTests
{
    private static readonly ReplayAsk Ask = ReplayAsk.Of(ReplayKind.Visit, new { Url = "https://example.com/" });
    private static readonly ReplayAsk Other = ReplayAsk.Of(ReplayKind.Visit, new { Url = "https://example.com/other" });
    private static readonly ReplayAsk Bad = ReplayAsk.Of(ReplayKind.Visit, new { Url = "https://example.com/bad" });
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

        await folder.Store.SaveAnswerAsync(key, Ask, Page);

        string trials = Path.Combine(folder.Path, "trials");
        Assert.That(Path.GetDirectoryName(folder.Trials.Single()), Is.EqualTo(trials));
        Assert.That(Directory.GetDirectories(folder.Path).Select(Path.GetFileName), Is.EquivalentTo(new[] { "answers", "trials" }));
        Assert.That(Directory.GetFiles(folder.Path), Is.Empty, "nothing beside the two folders");
    }

    [Test]
    public async Task ALongKey_IsCutToTheNameLimit_AndStillLandsUnderTrials()
    {
        using var folder = new ReplayFolder();

        await folder.Store.SaveAnswerAsync(new string('x', 500), Ask, Page);

        string name = Path.GetFileNameWithoutExtension(folder.Trials.Single());
        Assert.That(name.Length, Is.EqualTo(TrialFileName.MaxCleanLength + 1 + 12));
    }

    [Test]
    public async Task TwoKeysThatCleanToTheSameName_GetTwoFiles()
    {
        using var folder = new ReplayFolder();

        await folder.Store.SaveAnswerAsync("run:1/a", Ask, Page);
        await folder.Store.SaveAnswerAsync("run_1_a", Ask, Page);

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

        Assert.That(await folder.Store.FindAsync<ReplayPage>("never", Ask), Is.Null);

        await folder.Store.SaveAnswerAsync("other", Ask, Page);
        Assert.That(await folder.Store.FindAsync<ReplayPage>("never", Ask), Is.Null);
        Assert.That(await folder.Store.FindAsync<ReplayPage>("other", Other), Is.Null);
    }

    [Test]
    public async Task AFoundPage_ComesBackWithItsFields_AndAFoundFailureWithItsText()
    {
        using var folder = new ReplayFolder();
        var page = new ReplayPage("https://example.com/after", "<html>é</html>", ["{}"], "r", [new BrowserCookie("a", "b", ".example.com")]);
        await folder.Store.SaveAnswerAsync("t", Ask, page);
        await folder.Store.SaveFailureAsync("t", Bad, "it refused: ‘quoted’");

        var found = await folder.Store.FindAsync<ReplayPage>("t", Ask);
        var failed = await folder.Store.FindAsync<ReplayPage>("t", Bad);

        Assert.That(found, Is.InstanceOf<ReplayAnswer<ReplayPage>.Answered>());
        Assert.That(((ReplayAnswer<ReplayPage>.Answered)found!).Value, Is.EqualTo(page).Using<ReplayPage>(SamePage));
        Assert.That(failed, Is.EqualTo(new ReplayAnswer<ReplayPage>.Failed("it refused: ‘quoted’")));
    }


    [Test]
    public async Task AnAnswerSavedAsASearch_IsNotFoundAsAVisitWithTheSameKey()
    {
        using var folder = new ReplayFolder();
        var search = ReplayAsk.Of(ReplayKind.Search, new { Url = "https://example.com/" });

        await folder.Store.SaveAnswerAsync("t", search, Page);

        Assert.That(await folder.Store.FindAsync<ReplayPage>("t", search), Is.Not.Null);
        Assert.That(await folder.Store.FindAsync<ReplayPage>("t", Ask), Is.Null, "same key, another kind: a miss");
    }

    [Test]
    public async Task TheIndexLineHoldsTheKindAsAName_NeverANumber()
    {
        using var folder = new ReplayFolder();

        await folder.Store.SaveAnswerAsync("t", Ask, Page);

        string line = File.ReadAllLines(folder.Trials.Single()).Single();
        Assert.That(line, Does.Contain("\"kind\":\"visit\""));
        Assert.That(line, Does.Not.Contain("\"kind\":0"));
    }

    [Test]
    public async Task TheAnswerFile_SitsUnderAnswers()
    {
        using var folder = new ReplayFolder();

        await folder.Store.SaveAnswerAsync("t", Ask, Page);

        Assert.That(Path.GetFileName(Path.GetDirectoryName(folder.Answers.Single())), Is.EqualTo("answers"));
        Assert.That(Directory.Exists(Path.Combine(folder.Path, "pages")), Is.False);
    }

    [Test]
    public void ANumberWhereAnEnumNameBelongs_IsRefused_ADR0024()
    {
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<ReplayKind>("0", ReplayStore.Json));
        Assert.That(System.Text.Json.JsonSerializer.Serialize(ReplayKind.Visit, ReplayStore.Json), Is.EqualTo("\"visit\""));
    }

    [Test]
    public async Task AStructAnswer_RoundTrips()
    {
        using var folder = new ReplayFolder();
        var clock = ReplayAsk.Of(ReplayKind.Clock, new { });
        var time = new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero);

        await folder.Store.SaveAnswerAsync("t", clock, time);

        var found = await folder.Store.FindAsync<DateTimeOffset>("t", clock);
        Assert.That(found, Is.EqualTo(new ReplayAnswer<DateTimeOffset>.Answered(time)));
    }

    [Test]
    public async Task AnAnswerFileThatIsGone_IsAMiss_EvenForAStruct_NotTheStructsDefault()
    {
        // Review of 4b.1: ReadAnswerAsync<T> returned T?, which for a struct is plain T, so a missing file answered default (1 January 0001).
        using var folder = new ReplayFolder();
        var clock = ReplayAsk.Of(ReplayKind.Clock, new { });
        await folder.Store.SaveAnswerAsync("t", clock, new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero));
        File.Delete(folder.Answers.Single());

        Assert.That(await folder.Store.FindAsync<DateTimeOffset>("t", clock), Is.Null);
    }

    [Test]
    public async Task AHashThatIsNotSixtyFourHexDigits_IsAMiss_EvenForAStruct()
    {
        using var folder = new ReplayFolder();
        var clock = ReplayAsk.Of(ReplayKind.Clock, new { });
        await folder.Store.SaveAnswerAsync("t", clock, new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero));
        string index = folder.Trials.Single();
        string hash = System.Text.Json.JsonDocument.Parse(File.ReadAllLines(index).Single()).RootElement.GetProperty("hash").GetString()!;
        File.WriteAllText(index, File.ReadAllText(index).Replace(hash, "../../x"));

        Assert.That(await folder.Store.FindAsync<DateTimeOffset>("t", clock), Is.Null);
    }

    private static bool SamePage(ReplayPage? a, ReplayPage? b) =>
        a is not null && b is not null && a.Url == b.Url && a.Html == b.Html && a.Captured.SequenceEqual(b.Captured)
        && a.ScriptResult == b.ScriptResult && (a.Cookies ?? []).SequenceEqual(b.Cookies ?? []) && (a.Cookies is null) == (b.Cookies is null);
}
