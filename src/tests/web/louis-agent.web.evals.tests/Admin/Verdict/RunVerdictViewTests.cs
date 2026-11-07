using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Web.Evals.Admin.Verdict;

namespace LouisAgent.Web.Evals.Tests.Admin.Verdict;

/// <summary>
/// The run's verdict, worked out before any markup (F53.1 step 1): per mode how many conversations passed of those
/// graded, and whether that is more or fewer than the eval's run before; the run's cost and requests; and how many
/// answers still wait for a person's grade.
/// </summary>
public class RunVerdictViewTests
{
    /// <summary>When the run started; the runs before it sit earlier on the trend.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The graders of a cook's eval: a rule, a judge, and the judge's blind review by a person.</summary>
    private static readonly RunGraderRow[] Graders =
    [
        new("no-error", 1, "FixedRule", null),
        new("meal-for-people", 1, "Model", null),
        new("meal-for-people.human", 1, "Human", "Is this a meal?")
    ];

    /// <summary>A conversation with this outcome; one whose review is pending still waits for a person.</summary>
    private static ConversationRow Conversation(string mode, string task, string? outcome, bool reviewPending = false)
    {
        var review = new GradeRow("meal-for-people.human", 1, 0, reviewPending ? "Pending" : "Pass", "", null, null);
        return new ConversationRow(mode, 1, task, 0, null, 0, 1, [review], outcome, []);
    }

    /// <summary>A finished run of these conversations, with its total.</summary>
    private static StoredRunReply Run(IReadOnlyList<ConversationRow> conversations, RunTotalRow? total = null) =>
        new(new StoredRunRow("run3", SampleEval.Beta, 1, "done", 0, T0, T0.AddMinutes(5), total, null, null, null,
                null, null, null, Graders), null, [], null, [], null, conversations, [], [], []);

    /// <summary>A trend point: a run of this mode at this time, and the share of its conversations passed.</summary>
    private static TrendPointRow Point(string run, DateTimeOffset at, double? score, int conversations) =>
        new(run, at, null, score, conversations, null, null, null, null, null, null, null, null, null, null);

    /// <summary>The trend with one line per mode.</summary>
    private static TrendReply Trend(params TrendLineRow[] lines) => new(SampleEval.Beta, [], lines);

    /// <summary>Each mode says how many of its graded conversations passed; one not graded yet doesn't count.</summary>
    [Test]
    public void EachMode_SaysHowManyOfItsGradedConversationsPassed()
    {
        var run = Run(
        [
            Conversation(SampleMode.Alpha, "bobotie", "Pass"),
            Conversation(SampleMode.Alpha, "curry", "Fail"),
            Conversation(SampleMode.Alpha, "chat", null),
            Conversation(SampleMode.Beta, "bobotie", "Pass"),
            Conversation(SampleMode.Beta, "curry", "infrastructure")
        ]);

        var view = RunVerdictView.Of(run, null);

        Assert.That(view.Modes.Select(m => (m.Mode, m.Passed, m.Total)),
            Is.EqualTo(new[] { (SampleMode.Alpha, 1, 2), (SampleMode.Beta, 1, 1) }));
    }

    /// <summary>
    /// The change is against the mode's run before this one on the trend: the passed count that run's score and
    /// size give, so two more passed says up two.
    /// </summary>
    [Test]
    public void TheChange_IsAgainstTheModesRunBefore_OnTheTrend()
    {
        var run = Run(
        [
            Conversation(SampleMode.Alpha, "bobotie", "Pass"),
            Conversation(SampleMode.Alpha, "curry", "Pass"),
            Conversation(SampleMode.Alpha, "chat", "Pass"),
            Conversation(SampleMode.Beta, "bobotie", "Fail")
        ]);
        var trend = Trend(
            new TrendLineRow(SampleMode.Alpha,
            [
                Point("run1", T0.AddDays(-2), 0.5, 2), Point("run2", T0.AddDays(-1), 1.0 / 3, 3),
                Point("run3", T0, 1.0, 3)
            ]),
            new TrendLineRow(SampleMode.Beta, [Point("run2", T0.AddDays(-1), 1.0, 1), Point("run3", T0, 0, 1)]));

        var view = RunVerdictView.Of(run, trend);

        Assert.That(view.Modes.Select(m => (m.Change, m.First)),
            Is.EqualTo(new[] { ((int?)2, false), ((int?)-1, false) }));
    }

    /// <summary>A mode's line on a model of its own still counts as the mode's runs before.</summary>
    [Test]
    public void AModesLineOnAModel_StillCountsAsTheModesRunsBefore()
    {
        var run = Run([Conversation(SampleMode.Alpha, "bobotie", "Pass")]);
        var trend = Trend(new TrendLineRow($"{SampleMode.Alpha} (claude-haiku)",
            [Point("run1", T0.AddDays(-1), 0, 1)]));

        Assert.That(RunVerdictView.Of(run, trend).Modes.Single().Change, Is.EqualTo(1));
    }

    /// <summary>With no run before on the trend, a mode is on its first run; with no trend at all too.</summary>
    [Test]
    public void WithNoRunBefore_AModeIsOnItsFirstRun()
    {
        var run = Run([Conversation(SampleMode.Alpha, "bobotie", "Pass")]);
        var trend = Trend(new TrendLineRow(SampleMode.Alpha, [Point("run3", T0, 1.0, 1)]));

        Assert.That((RunVerdictView.Of(run, trend).Modes.Single().Change,
                RunVerdictView.Of(run, trend).Modes.Single().First), Is.EqualTo(((int?)null, true)));
        Assert.That(RunVerdictView.Of(run, null).Modes.Single().First, Is.True);
    }

    /// <summary>A run before with no score (nothing graded yet) gives no change, and it isn't a first run.</summary>
    [Test]
    public void ARunBeforeWithNoScore_GivesNoChange_AndIsNotAFirstRun()
    {
        var run = Run([Conversation(SampleMode.Alpha, "bobotie", "Pass")]);
        var trend = Trend(new TrendLineRow(SampleMode.Alpha, [Point("run1", T0.AddDays(-1), null, 0)]));

        var mode = RunVerdictView.Of(run, trend).Modes.Single();

        Assert.That((mode.Change, mode.First), Is.EqualTo(((int?)null, false)));
    }

    /// <summary>The run's cost and requests come from its total; a run without one says neither.</summary>
    [Test]
    public void TheCostAndRequests_ComeFromTheRunsTotal()
    {
        var with = RunVerdictView.Of(Run([], new RunTotalRow(1.25m, "USD", 1200, 4)), null);
        var without = RunVerdictView.Of(Run([]), null);

        Assert.That((with.Cost, with.Requests), Is.EqualTo(((decimal?)1.25m, (int?)4)));
        Assert.That((without.Cost, without.Requests), Is.EqualTo(((decimal?)null, (int?)null)));
    }

    /// <summary>The answers to grade are the conversations whose blind review still waits for a person.</summary>
    [Test]
    public void TheAnswersToGrade_AreTheConversationsWhoseReviewStillWaits()
    {
        var run = Run(
        [
            Conversation(SampleMode.Alpha, "bobotie", "Pass", reviewPending: true),
            Conversation(SampleMode.Alpha, "curry", "Fail"),
            Conversation(SampleMode.Beta, "bobotie", "Pass", reviewPending: true)
        ]);

        Assert.That(RunVerdictView.Of(run, null).ToGrade, Is.EqualTo(2));
    }
}
