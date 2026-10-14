using LouisAgent.Core.Skills;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Usage;

namespace LouisAgent.Core.Tests.Usage;

// F3 step 2: the usage line after an answer (F3-S1, S3, S4) and the /usage reply (F3-S3), one format for every host.
public class UsageTextTests
{
    private static UsageRecord Record(long input, long cacheRead, long output, decimal? cost, string purpose = UsagePurpose.Turn,
        string currency = "USD") => new(
        At: DateTimeOffset.UtcNow, Round: 1, Purpose: purpose, Host: "cli", Session: "s", Turn: 1, Task: null, Run: null,
        Service: null, Model: "claude-haiku-5-5", Tokens: new UsageTokens(input, 0, cacheRead, output, null), DurationMs: 1,
        Stop: "stop", Cost: cost is null ? null : new UsageCost(currency, cost.Value, "2026-10-10"));

    [Test]
    public void Line_TurnAndSession()
    {
        UsageRecord[] turn =
        [
            Record(600, 8_000, 200, 0.002m),
            Record(500, 9_000, 150, 0.002m),
            Record(800, 9_000, 62, 0.002m),
        ];
        UsageRecord[] session = [Record(10_000, 0, 500, 0.034m), .. turn];

        Assert.That(UsageText.Line(turn, session),
            Is.EqualTo("turn: 3 requests · 28k tokens (26k cached) · $0.006 · session $0.04"));
    }

    [Test]
    public void Line_NoSession_LeavesItOut()
    {
        Assert.That(UsageText.Line([Record(1_000, 0, 100, 0.0015m)]), Is.EqualTo("turn: 1 request · 1.1k tokens · $0.0015"));
    }

    [Test]
    public void Line_ModelNotPriced_SaysPriceUnknownNotZero()
    {
        UsageRecord[] turn = [Record(1_000, 0, 100, cost: null)];

        Assert.That(UsageText.Line(turn, turn), Is.EqualTo("turn: 1 request · 1.1k tokens · price unknown · session price unknown"));
    }

    [Test]
    public void Line_PricedAtZero_IsZero()
    {
        // A local model is free; that's different from not knowing the price.
        Assert.That(UsageText.Line([Record(2_050, 0, 3, 0m)]), Is.EqualTo("turn: 1 request · 2.1k tokens · $0"));
    }

    [Test]
    public void Line_SomeUnpriced_SaysHowMany()
    {
        // A partial sum must not read as the whole cost.
        Assert.That(UsageText.Line([Record(1_000, 0, 0, 0.002m), Record(1_000, 0, 0, null)]),
            Is.EqualTo("turn: 2 requests · 2k tokens · $0.002 + 1 unpriced"));
    }

    [Test]
    public void Line_NoRequests_SaysSo()
    {
        // A turn answered without the model (e.g. a command) has nothing to cost.
        Assert.That(UsageText.Line([]), Is.EqualTo("turn: 0 requests · 0 tokens · price unknown"));
    }

    [TestCase(0, "0")]
    [TestCase(412, "412")]
    [TestCase(999, "999")]
    [TestCase(1_000, "1k")]
    [TestCase(1_949, "1.9k")]
    [TestCase(2_050, "2.1k")]
    [TestCase(9_950, "10k")]
    [TestCase(31_400, "31k")]
    [TestCase(999_499, "999k")]
    [TestCase(999_999, "1M")]
    [TestCase(1_234_567, "1.2M")]
    [TestCase(48_000_000, "48M")]
    public void Tokens_AreShortened(long count, string expected)
    {
        Assert.That(UsageText.Tokens(count), Is.EqualTo(expected));
    }

    [TestCase("0", "$0")]
    [TestCase("0.006", "$0.006")]
    [TestCase("0.0365", "$0.037")]
    [TestCase("0.04", "$0.04")]
    [TestCase("0.6586", "$0.66")]
    [TestCase("0.000012", "$0.000012")]
    [TestCase("1", "$1.00")]
    [TestCase("12.345", "$12.35")]
    public void Money_ShowsTwoSignificantDigitsBelowADollar(string amount, string expected)
    {
        Assert.That(UsageText.Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "USD"),
            Is.EqualTo(expected));
    }

    [Test]
    public void Money_OtherCurrency_NamesIt()
    {
        Assert.That(UsageText.Money(0.04m, "EUR"), Is.EqualTo("0.04 EUR"));
    }

    [Test]
    public void Summary_TotalsByPurposeAndKind()
    {
        UsageRecord[] session =
        [
            Record(1_000, 0, 100, 0.0015m),
            Record(2_000, 0, 50, 0.00225m, UsagePurpose.Summary),
            Record(1_000, 0, 100, 0.0015m),
        ];

        Assert.That(UsageText.Summary(session), Is.EqualTo(
            "Session usage: 3 requests · 4.3k tokens · $0.0053\n" +
            "- turn: 2 requests · 2.2k tokens · $0.003\n" +
            "- summary: 1 request · 2.1k tokens · $0.0023\n" +
            "- tokens: 4k input · 0 cache write · 0 cache read · 250 output"));
    }

    [Test]
    public void Summary_ListsASubSessionsRollUp_AsItsOwnPurpose()
    {
        // F5 follow-up: /usage shows what delegation cost next to the chat's own requests, weighted by the roll-up's count.
        UsageRecord[] session =
        [
            Record(1_000, 0, 100, 0.0015m),
            Record(1_000, 9_000, 200, 0.0123m, UsagePurpose.SubSession) with { Requests = 3, Model = "sub-session:puls" },
        ];

        Assert.That(UsageText.Summary(session), Is.EqualTo(
            "Session usage: 4 requests · 11k tokens (9k cached) · $0.014\n" +
            "- turn: 1 request · 1.1k tokens · $0.0015\n" +
            "- sub_session: 3 requests · 10k tokens (9k cached) · $0.012\n" +
            "- tokens: 2k input · 0 cache write · 9k cache read · 300 output"));
    }

    [Test]
    public void Summary_Unpriced_ExplainsWhy()
    {
        Assert.That(UsageText.Summary([Record(1_000, 0, 100, null)]),
            Does.EndWith("\n- price unknown for 1 request: the model isn't in the price table"));
    }

    [Test]
    public void Summary_NoRequests_SaysNothingYet()
    {
        Assert.That(UsageText.Summary([]), Is.EqualTo("No model requests in this session yet."));
    }

    private static AgentEngine Engine(FakeChatClient client) =>
        new(new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md")), client, TestPaths.Agent());

    [Test]
    public void UsageCommand_AnswersFromTheSessionRecords_WithoutTheModel()
    {
        var client = new FakeChatClient();
        UsageRecord[] session = [Record(1_000, 0, 100, 0.0015m)];

        string? reply = Engine(client).HandleUserCommand("/usage", session);

        Assert.That(reply, Is.EqualTo(UsageText.Summary(session)));
        Assert.That(client.Requests, Is.Empty);
    }

    [Test]
    public void UsageCommand_CaseAndSpacesDontMatter()
    {
        Assert.That(Engine(new FakeChatClient()).HandleUserCommand("  /USAGE ", []), Is.EqualTo("No model requests in this session yet."));
    }

    [Test]
    public void UsageCommand_HostWithoutUsage_SaysSo()
    {
        Assert.That(Engine(new FakeChatClient()).HandleUserCommand("/usage"), Is.EqualTo("Usage isn't tracked in this session."));
    }
}