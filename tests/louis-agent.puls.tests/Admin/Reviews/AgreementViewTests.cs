using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

/// <summary>
/// Per judge, how often it agreed with the people (F18 step 6a.3): the API sends counts on the run reply's agreement
/// rows (it reads, it doesn't rate) and the portal makes the shares: each class of the person's grades as agreed of
/// total and a percentage, "few labels" under ten, and "no labels" for a class nobody has graded.
/// </summary>
public class AgreementViewTests
{
    /// <summary>A judge's row: what it said to the answers the person passed, and to those they failed.</summary>
    private static AgreementRow Judge(string grader, (int Pass, int Fail, int Unknown) passed,
        (int Pass, int Fail, int Unknown) failed) =>
        new(grader, new CalibrationClassRow(passed.Pass, passed.Fail, passed.Unknown),
            new CalibrationClassRow(failed.Pass, failed.Fail, failed.Unknown));

    /// <summary>The cook's meal judge: 13 of 14 passes agreed, all 4 fails.</summary>
    private static readonly AgreementRow Cook = Judge("meal-for-people", (13, 1, 0), (0, 4, 0));

    /// <summary>A judge's agreement is the counts of each class.</summary>
    [Test]
    public void AJudgesAgreement_IsTheCountsOfEachClass()
    {
        var judge = AgreementView.Of([Cook]).Single();

        Assert.That(judge.Grader, Is.EqualTo("meal-for-people"));
        Assert.That((judge.PersonPass.JudgePass, judge.PersonPass.JudgeFail, judge.PersonPass.JudgeUnknown),
            Is.EqualTo((13, 1, 0)));
        Assert.That((judge.PersonFail.JudgePass, judge.PersonFail.JudgeFail, judge.PersonFail.JudgeUnknown),
            Is.EqualTo((0, 4, 0)));
    }

    /// <summary>The shares are the agreed of the total in each class, few under ten.</summary>
    [Test]
    public void TheSharesAreTheAgreedOfTheTotalInEachClass_AndFewUnderTen()
    {
        var judge = AgreementView.Of([Cook]).Single();

        Assert.That((judge.PersonPass.Agreed, judge.PersonPass.Total, judge.PersonPass.Few),
            Is.EqualTo((13, 14, false)));
        Assert.That((judge.PersonFail.Agreed, judge.PersonFail.Total, judge.PersonFail.Few), Is.EqualTo((4, 4, true)));
        Assert.That(judge.PersonPass.Text, Is.EqualTo("13 of 14 (93%)"));
        Assert.That(judge.PersonFail.Text, Is.EqualTo("4 of 4 (100%), few labels"));
    }

    /// <summary>A "can't tell" is not agreement: it counts against its class.</summary>
    [Test]
    public void ACantTell_IsNotAgreement_ItCountsAgainstItsClass() =>
        Assert.That(AgreementView.Of([Judge("g", (9, 0, 1), (0, 0, 0))]).Single().PersonPass.Text,
            Is.EqualTo("9 of 10 (90%)"), "ten labels are not few");

    /// <summary>A class nobody has graded says no labels, never 0%.</summary>
    [Test]
    public void AClassNobodyHasGraded_SaysNoLabels_NeverZeroPercent()
    {
        var judge = AgreementView.Of([Judge("g", (2, 0, 0), (0, 0, 0))]).Single();

        Assert.That(judge.PersonFail.Text, Is.EqualTo("no labels"));
        Assert.That(judge.AnyLabels, Is.True);
    }

    /// <summary>A run nobody has graded has nothing to export.</summary>
    [Test]
    public void ARunNobodyHasGraded_HasNothingToExport() =>
        Assert.That(AgreementView.Of([Judge("g", (0, 0, 0), (0, 0, 0))]).Single().AnyLabels, Is.False);

    /// <summary>A run with no agreement rows has no judges.</summary>
    [Test]
    public void ARunWithNoAgreement_HasNoJudges() =>
        Assert.That(AgreementView.Of([]), Is.Empty);

    /// <summary>A class's counts from the contract become the same agreement the checks read.</summary>
    [Test]
    public void AClassFromTheContract_IsTheSameAgreement() =>
        Assert.That(ClassAgreement.Of(new CalibrationClassRow(27, 0, 1), personPassed: true),
            Is.EqualTo(new ClassAgreement(27, 0, 1, PersonPassed: true)));
}
