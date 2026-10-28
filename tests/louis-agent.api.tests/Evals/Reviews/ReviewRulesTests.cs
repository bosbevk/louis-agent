using System.Text.Json;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Runs.Protocol;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Reviews;

/// <summary>
/// The rules for reading people's grades, without any store. Each reviewer's latest grade counts (an unsure takes
/// back their earlier pass or fail, which stays in the history), and across reviewers the latest decided one wins. How
/// far a judge agrees with the people is only counted, never rated: the person's pass or fail against the judge's
/// pass, fail or can't tell on the same conversation.
/// </summary>
public class ReviewRulesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    /// <summary>One conversation of the sample run, by its task.</summary>
    private static EvalProtocol.ConversationKey Key(string task) => new(SampleMode.Beta, 1, task);

    /// <summary>A person's grade of one conversation, some seconds after the start, so grades have an order.</summary>
    private static HumanGrade Grade(string task, int seconds, HumanVerdict verdict, string reviewer = "Louis",
        string grader = "meal-for-people") =>
        new("run1", Key(task), grader, verdict, "", reviewer, T0.AddSeconds(seconds));

    /// <summary>Each reviewer counts once, with the grade they gave last.</summary>
    [Test]
    public void EachReviewersLatestGrade_IsTheOneThatCounts()
    {
        var latest = HumanGrades.Latest([
            Grade("a", 1, HumanVerdict.Fail), Grade("a", 5, HumanVerdict.Pass),
            Grade("a", 2, HumanVerdict.Fail, reviewer: "Anna")
        ]);

        Assert.That(latest.Select(g => (g.Reviewer, g.Verdict)),
            Is.EquivalentTo(new[] { ("Louis", HumanVerdict.Pass), ("Anna", HumanVerdict.Fail) }));
    }

    /// <summary>
    /// "Louis" and "louis" are the same person, so the later grade is their latest and keeps the name as typed then.
    /// </summary>
    [Test]
    public void AReviewersName_IsTheSameNameWhateverItsCase_AndTheLaterGradeIsTheirLatest()
    {
        var latest = HumanGrades.Latest([
            Grade("a", 1, HumanVerdict.Fail, reviewer: "Louis"), Grade("a", 5, HumanVerdict.Pass, reviewer: "louis")
        ]);

        Assert.That(latest.Select(g => (g.Reviewer, g.Verdict)), Is.EqualTo(new[] { ("louis", HumanVerdict.Pass) }),
            "one reviewer, and the name is kept as the later grade typed it");
    }

    /// <summary>An unsure typed in another case still takes back that same person's pass.</summary>
    [Test]
    public void AnUnsureInAnotherCase_StillTakesBackTheSameReviewersPass()
    {
        var decided = HumanGrades.Decided([
            Grade("a", 1, HumanVerdict.Pass, reviewer: "Louis"), Grade("a", 2, HumanVerdict.Unsure, reviewer: "LOUIS")
        ]);

        Assert.That(decided, Is.Empty);
    }

    /// <summary>A conversation's decided grade is the most recent pass or fail, whoever gave it.</summary>
    [Test]
    public void ADecidedGrade_IsTheLatestPassOrFail_AcrossReviewers()
    {
        var decided = HumanGrades.Decided([
            Grade("a", 1, HumanVerdict.Fail), Grade("a", 5, HumanVerdict.Pass, reviewer: "Anna"),
            Grade("b", 2, HumanVerdict.Pass)
        ]);

        Assert.That(decided.Select(g => (g.Conversation.Task, g.Verdict)),
            Is.EquivalentTo(new[] { ("a", HumanVerdict.Pass), ("b", HumanVerdict.Pass) }));
    }

    /// <summary>An unsure takes back only your own earlier verdict; someone else's still stands.</summary>
    [Test]
    public void AnUnsure_TakesBackTheSameReviewersEarlierPassOrFail_ButNotAnotherReviewers()
    {
        var decided = HumanGrades.Decided(
        [
            Grade("a", 1, HumanVerdict.Pass), Grade("a", 2, HumanVerdict.Unsure),
            Grade("b", 1, HumanVerdict.Fail), Grade("b", 2, HumanVerdict.Unsure, reviewer: "Anna"),
        ]);

        Assert.That(decided.Select(g => g.Conversation.Task), Is.EqualTo(new[] { "b" }),
            "a is unsure now; b still has Louis's fail");
    }

    /// <summary>A conversation with only an unsure has no decided grade.</summary>
    [Test]
    public void AnOnlyUnsure_IsNeverDecided()
    {
        Assert.That(HumanGrades.Decided([Grade("a", 1, HumanVerdict.Unsure)]), Is.Empty);
    }

    /// <summary>Grades for two different judges are decided apart, each on its own.</summary>
    [Test]
    public void ADecidedGrade_IsPerGrader_SoTwoJudgesDoNotShareOne()
    {
        var decided = HumanGrades.Decided([
            Grade("a", 1, HumanVerdict.Pass), Grade("a", 2, HumanVerdict.Fail, grader: "another-judge")
        ]);

        Assert.That(decided.Select(g => (g.Grader, g.Verdict)),
            Is.EquivalentTo(new[] { ("meal-for-people", HumanVerdict.Pass), ("another-judge", HumanVerdict.Fail) }));
    }

    /// <summary>A stored conversation with the judges' grades we give it.</summary>
    private static Conversation Conversation(string task, params (string Grader, EvalOutcome Outcome)[] grades) =>
        new(Key(task), 1, null, JsonDocument.Parse("{}").RootElement.Clone(), 0, 1.0,
        [
            .. grades.Select(g => new Grade(g.Grader, 1, g.Outcome == EvalOutcome.Pass ? 1 : 0, g.Outcome, "r"))
        ]);

    /// <summary>
    /// The agreement counts each pairing of the person's verdict with the judge's, for one judge only; a conversation
    /// the judge didn't grade counts as can't tell.
    /// </summary>
    [Test]
    public void TheAgreement_IsTheCountsOfThePersonAgainstTheJudge_ForOneGrader()
    {
        // What the judge said on each conversation (f it never graded).
        var conversations = new[]
        {
            Conversation("a", ("meal-for-people", EvalOutcome.Pass)),
            Conversation("b", ("meal-for-people", EvalOutcome.Pass)),
            Conversation("c", ("meal-for-people", EvalOutcome.Fail)),
            Conversation("d", ("meal-for-people", EvalOutcome.Unknown)),
            Conversation("e", ("meal-for-people", EvalOutcome.Pass)), Conversation("f"),
        };
        // What the people decided (e for another judge).
        var decided = new[]
        {
            Grade("a", 1, HumanVerdict.Pass), Grade("b", 1, HumanVerdict.Fail), Grade("c", 1, HumanVerdict.Fail),
            Grade("d", 1, HumanVerdict.Pass), Grade("e", 1, HumanVerdict.Pass, grader: "another-judge"),
            Grade("f", 1, HumanVerdict.Fail),
        };

        var tally = ReviewAgreement.Of(conversations, decided, "meal-for-people");

        Assert.That(tally, Is.EqualTo(new AgreementTally(
                PersonPassJudgePass: 1, PersonPassJudgeFail: 0, PersonPassJudgeUnknown: 1,
                PersonFailJudgePass: 1, PersonFailJudgeFail: 1, PersonFailJudgeUnknown: 1)),
            "a: pass/pass; d: pass/unknown; b: fail/pass; c: fail/fail; f: fail, no grade from the judge is unknown; e is another judge's");
    }

    /// <summary>With nobody's grades yet, the agreement is all zeros rather than missing.</summary>
    [Test]
    public void WithNoPeopleGrades_TheAgreementIsAllZeros_NotMissing()
    {
        var tally = ReviewAgreement.Of([Conversation("a", ("meal-for-people", EvalOutcome.Pass))], [],
            "meal-for-people");

        Assert.That(tally, Is.EqualTo(new AgreementTally(0, 0, 0, 0, 0, 0)));
    }

    /// <summary>A person's grade of a conversation the run doesn't have isn't counted.</summary>
    [Test]
    public void AGradeOfAConversationTheRunDoesNotHave_IsLeftOut()
    {
        var tally = ReviewAgreement.Of([Conversation("a", ("meal-for-people", EvalOutcome.Pass))],
            [Grade("zzz", 1, HumanVerdict.Pass)], "meal-for-people");

        Assert.That(tally, Is.EqualTo(new AgreementTally(0, 0, 0, 0, 0, 0)));
    }
}
