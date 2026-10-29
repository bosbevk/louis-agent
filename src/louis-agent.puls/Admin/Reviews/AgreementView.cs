using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>
/// One class of the people's grades of a judge (F18 step 6a.3): those they passed, or those they failed, and what the
/// judge said to each. The judge agreed where it said the same; a "can't tell" is not agreement. The text is the
/// share the page shows: "13 of 14 (93%)", "few labels" under ten (a share of four is four answers), "no labels" for
/// a class nobody has graded.
/// </summary>
public sealed record ClassAgreement(int JudgePass, int JudgeFail, int JudgeUnknown, bool PersonPassed)
{
    /// <summary>A class with fewer labels than this is said to be thin.</summary>
    public const int FewLabels = 10;

    /// <summary>How many answers the person put in this class.</summary>
    public int Total => JudgePass + JudgeFail + JudgeUnknown;

    /// <summary>How many of them the judge answered the same way.</summary>
    public int Agreed => PersonPassed ? JudgePass : JudgeFail;

    /// <summary>Too few labels for the share to mean much.</summary>
    public bool Few => Total < FewLabels;

    /// <summary>
    /// The share the judge agreed, in whole percent; 0 for a class with no labels. The one place the maths lives
    /// (6a.5e).
    /// </summary>
    public int Percent => Total == 0 ? 0 : (int)Math.Round(100.0 * Agreed / Total, MidpointRounding.AwayFromZero);

    /// <summary>The share as the page words it.</summary>
    public string Text => Total == 0
        ? "no labels"
        : $"{Agreed} of {Total} ({Percent}%)" + (Few ? ", few labels" : "");

    /// <summary>One class's counts as the agreement and the judge checks (6a.5c) both send them.</summary>
    /// <remarks>Fowler's <i>Replace Constructor with Factory Function</i>, the guide's <c>Record.Of</c>.</remarks>
    public static ClassAgreement Of(CalibrationClassRow counts, bool personPassed) =>
        new(counts.JudgePass, counts.JudgeFail, counts.JudgeUnknown, personPassed);
}

/// <summary>A judge and how often it agreed with the people, class by class.</summary>
public sealed record JudgeAgreement(string Grader, ClassAgreement PersonPass, ClassAgreement PersonFail)
{
    /// <summary>Anyone has graded an answer of this judge, so there are labels to export.</summary>
    public bool AnyLabels => PersonPass.Total + PersonFail.Total > 0;
}

/// <summary>
/// Reads the run reply's agreement rows (the API's counts: it reads, it doesn't rate) into what the page shows; none
/// for a run from before the reviews.
/// </summary>
public static class AgreementView
{
    /// <summary>One judge per agreement row, in the API's order.</summary>
    public static IReadOnlyList<JudgeAgreement> Of(IReadOnlyList<AgreementRow> agreement) =>
    [
        .. agreement.Select(judge => new JudgeAgreement(judge.Grader,
            ClassAgreement.Of(judge.PersonPass, personPassed: true),
            ClassAgreement.Of(judge.PersonFail, personPassed: false)))
    ];
}
