using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Reviews;

// F18 step 6a.2: the rules that read people's grades, pure and free of the stores. Which grade counts: a reviewer's latest (an unsure takes back their earlier pass or fail, which is
// kept in the history), and across reviewers the latest decided one. The agreement of a judge with the people is counts only, as 5a says (the API reads, it doesn't rate): the person's
// pass or fail against the judge's pass, fail or can't tell on the same conversation. Written first: HumanGrades and ReviewAgreement did not exist.
public class ReviewRulesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    private static BenchmarkProtocol.ConversationKey Key(string task) => new("puls-cook", 1, task);

    private static HumanGrade Grade(string task, int seconds, HumanVerdict verdict, string reviewer = "Louis", string grader = "meal-for-people") =>
        new("run1", Key(task), grader, verdict, "", reviewer, T0.AddSeconds(seconds));

    [Test]
    public void EachReviewersLatestGrade_IsTheOneThatCounts()
    {
        var latest = HumanGrades.Latest([Grade("a", 1, HumanVerdict.Fail), Grade("a", 5, HumanVerdict.Pass), Grade("a", 2, HumanVerdict.Fail, reviewer: "Anna")]);

        Assert.That(latest.Select(g => (g.Reviewer, g.Verdict)), Is.EquivalentTo(new[] { ("Louis", HumanVerdict.Pass), ("Anna", HumanVerdict.Fail) }));
    }

    // F18 step 6a.2b (the review's nit): a name typed as "Louis" once and "louis" the next time is one person, so the later grade is their latest and an unsure takes back their pass.
    [Test]
    public void AReviewersName_IsTheSameNameWhateverItsCase_AndTheLaterGradeIsTheirLatest()
    {
        var latest = HumanGrades.Latest([Grade("a", 1, HumanVerdict.Fail, reviewer: "Louis"), Grade("a", 5, HumanVerdict.Pass, reviewer: "louis")]);

        Assert.That(latest.Select(g => (g.Reviewer, g.Verdict)), Is.EqualTo(new[] { ("louis", HumanVerdict.Pass) }), "one reviewer, and the name is kept as the later grade typed it");
    }

    [Test]
    public void AnUnsureInAnotherCase_StillTakesBackTheSameReviewersPass()
    {
        var decided = HumanGrades.Decided([Grade("a", 1, HumanVerdict.Pass, reviewer: "Louis"), Grade("a", 2, HumanVerdict.Unsure, reviewer: "LOUIS")]);

        Assert.That(decided, Is.Empty);
    }

    [Test]
    public void ADecidedGrade_IsTheLatestPassOrFail_AcrossReviewers()
    {
        var decided = HumanGrades.Decided([Grade("a", 1, HumanVerdict.Fail), Grade("a", 5, HumanVerdict.Pass, reviewer: "Anna"), Grade("b", 2, HumanVerdict.Pass)]);

        Assert.That(decided.Select(g => (g.Conversation.Task, g.Verdict)), Is.EquivalentTo(new[] { ("a", HumanVerdict.Pass), ("b", HumanVerdict.Pass) }));
    }

    [Test]
    public void AnUnsure_TakesBackTheSameReviewersEarlierPassOrFail_ButNotAnotherReviewers()
    {
        var decided = HumanGrades.Decided(
        [
            Grade("a", 1, HumanVerdict.Pass), Grade("a", 2, HumanVerdict.Unsure),
            Grade("b", 1, HumanVerdict.Fail), Grade("b", 2, HumanVerdict.Unsure, reviewer: "Anna"),
        ]);

        Assert.That(decided.Select(g => g.Conversation.Task), Is.EqualTo(new[] { "b" }), "a is unsure now; b still has Louis's fail");
    }

    [Test]
    public void AnOnlyUnsure_IsNeverDecided()
    {
        Assert.That(HumanGrades.Decided([Grade("a", 1, HumanVerdict.Unsure)]), Is.Empty);
    }

    [Test]
    public void ADecidedGrade_IsPerGrader_SoTwoJudgesDoNotShareOne()
    {
        var decided = HumanGrades.Decided([Grade("a", 1, HumanVerdict.Pass), Grade("a", 2, HumanVerdict.Fail, grader: "another-judge")]);

        Assert.That(decided.Select(g => (g.Grader, g.Verdict)), Is.EquivalentTo(new[] { ("meal-for-people", HumanVerdict.Pass), ("another-judge", HumanVerdict.Fail) }));
    }

    private static Conversation Conversation(string task, params (string Grader, BenchmarkOutcome Outcome)[] grades) =>
        new(Key(task), 1, null, JsonDocument.Parse("{}").RootElement.Clone(), 0, 1.0, [.. grades.Select(g => new Grade(g.Grader, 1, g.Outcome == BenchmarkOutcome.Pass ? 1 : 0, g.Outcome, "r"))]);

    [Test]
    public void TheAgreement_IsTheCountsOfThePersonAgainstTheJudge_ForOneGrader()
    {
        var conversations = new[]
        {
            Conversation("a", ("meal-for-people", BenchmarkOutcome.Pass)), Conversation("b", ("meal-for-people", BenchmarkOutcome.Pass)),
            Conversation("c", ("meal-for-people", BenchmarkOutcome.Fail)), Conversation("d", ("meal-for-people", BenchmarkOutcome.Unknown)),
            Conversation("e", ("meal-for-people", BenchmarkOutcome.Pass)), Conversation("f"),
        };
        var decided = new[]
        {
            Grade("a", 1, HumanVerdict.Pass), Grade("b", 1, HumanVerdict.Fail), Grade("c", 1, HumanVerdict.Fail), Grade("d", 1, HumanVerdict.Pass),
            Grade("e", 1, HumanVerdict.Pass, grader: "another-judge"), Grade("f", 1, HumanVerdict.Fail),
        };

        var tally = ReviewAgreement.Of(conversations, decided, "meal-for-people");

        Assert.That(tally, Is.EqualTo(new AgreementTally(
            PersonPassJudgePass: 1, PersonPassJudgeFail: 0, PersonPassJudgeUnknown: 1,
            PersonFailJudgePass: 1, PersonFailJudgeFail: 1, PersonFailJudgeUnknown: 1)),
            "a: pass/pass; d: pass/unknown; b: fail/pass; c: fail/fail; f: fail, no grade from the judge is unknown; e is another judge's");
    }

    [Test]
    public void WithNoPeopleGrades_TheAgreementIsAllZeros_NotMissing()
    {
        var tally = ReviewAgreement.Of([Conversation("a", ("meal-for-people", BenchmarkOutcome.Pass))], [], "meal-for-people");

        Assert.That(tally, Is.EqualTo(new AgreementTally(0, 0, 0, 0, 0, 0)));
    }

    [Test]
    public void AGradeOfAConversationTheRunDoesNotHave_IsLeftOut()
    {
        var tally = ReviewAgreement.Of([Conversation("a", ("meal-for-people", BenchmarkOutcome.Pass))], [Grade("zzz", 1, HumanVerdict.Pass)], "meal-for-people");

        Assert.That(tally, Is.EqualTo(new AgreementTally(0, 0, 0, 0, 0, 0)));
    }
}
