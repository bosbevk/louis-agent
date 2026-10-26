using System.Text.Json;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

// F18 step 6a.3: the run's page shows, per judge, how often it agreed with the people: the API sends counts (it reads, it doesn't rate) and the portal makes the shares: each class of the
// person's grades (the ones they passed, the ones they failed) as agreed of total and a percentage, "few labels" under ten, and "no labels" for a class nobody has graded.
// Written first: AgreementView did not exist.
public class AgreementViewTests
{
    private static JsonElement Run(string agreement) => JsonDocument.Parse("{\"run\":{\"id\":\"r\"},\"agreement\":" + agreement + "}").RootElement.Clone();

    private const string Cook = """
        [{"grader":"meal-for-people","person_pass":{"judge_pass":13,"judge_fail":1,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":4,"judge_unknown":0}}]
        """;

    [Test]
    public void AJudgesAgreement_IsTheCountsOfEachClass()
    {
        var judge = AgreementView.Of(Run(Cook)).Single();

        Assert.That(judge.Grader, Is.EqualTo("meal-for-people"));
        Assert.That((judge.PersonPass.JudgePass, judge.PersonPass.JudgeFail, judge.PersonPass.JudgeUnknown), Is.EqualTo((13, 1, 0)));
        Assert.That((judge.PersonFail.JudgePass, judge.PersonFail.JudgeFail, judge.PersonFail.JudgeUnknown), Is.EqualTo((0, 4, 0)));
    }

    [Test]
    public void TheSharesAreTheAgreedOfTheTotalInEachClass_AndFewUnderTen()
    {
        var judge = AgreementView.Of(Run(Cook)).Single();

        Assert.That((judge.PersonPass.Agreed, judge.PersonPass.Total, judge.PersonPass.Few), Is.EqualTo((13, 14, false)));
        Assert.That((judge.PersonFail.Agreed, judge.PersonFail.Total, judge.PersonFail.Few), Is.EqualTo((4, 4, true)));
        Assert.That(judge.PersonPass.Text, Is.EqualTo("13 of 14 (93%)"));
        Assert.That(judge.PersonFail.Text, Is.EqualTo("4 of 4 (100%), few labels"));
    }

    [Test]
    public void ACantTell_IsNotAgreement_ItCountsAgainstItsClass()
    {
        var judge = AgreementView.Of(Run("""[{"grader":"g","person_pass":{"judge_pass":9,"judge_fail":0,"judge_unknown":1},"person_fail":{"judge_pass":0,"judge_fail":0,"judge_unknown":0}}]""")).Single();

        Assert.That(judge.PersonPass.Text, Is.EqualTo("9 of 10 (90%)"), "ten labels are not few");
    }

    [Test]
    public void AClassNobodyHasGraded_SaysNoLabels_NeverZeroPercent()
    {
        var judge = AgreementView.Of(Run("""[{"grader":"g","person_pass":{"judge_pass":2,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":0,"judge_unknown":0}}]""")).Single();

        Assert.That(judge.PersonFail.Text, Is.EqualTo("no labels"));
        Assert.That(judge.AnyLabels, Is.True);
    }

    [Test]
    public void ARunNobodyHasGraded_HasNothingToExport()
    {
        var judge = AgreementView.Of(Run("""[{"grader":"g","person_pass":{"judge_pass":0,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":0,"judge_unknown":0}}]""")).Single();

        Assert.That(judge.AnyLabels, Is.False);
    }

    [Test]
    public void ARunWithNoAgreementField_HasNoJudges_AnOlderApiOrARunBeforeTheReviews()
    {
        Assert.That(AgreementView.Of(JsonDocument.Parse("""{"run":{"id":"r"}}""").RootElement.Clone()), Is.Empty);
        Assert.That(AgreementView.Of(Run("[]")), Is.Empty);
    }
}
