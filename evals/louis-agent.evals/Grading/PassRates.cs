namespace LouisAgent.Evals.Grading;

/// <summary>
/// A trial's outcome from its grades (F18 step 4.5a; step 5a adds pass@k and pass^k over trials): only the required graders count. Fail
/// when any required grader failed, even beside an Unknown; otherwise Unknown when any required grader is Unknown or has no grade;
/// otherwise Pass. With no required grader it is Unknown, so no trial passes without a check (the catalogue refuses such an eval anyway).
/// </summary>
public static class PassRates
{
    public static TrialOutcome Of(IReadOnlyList<Grade> grades, IReadOnlyList<IGrader> graders)
    {
        var required = graders.Where(grader => grader.Required).ToList();
        if (required.Count == 0) return TrialOutcome.Unknown;

        var outcomes = required.Select(grader => grades.FirstOrDefault(grade => grade.Grader == grader.Name)?.Outcome).ToList();
        if (outcomes.Any(outcome => outcome == GradeOutcome.Fail)) return TrialOutcome.Fail;
        if (outcomes.Any(outcome => outcome is null or GradeOutcome.Unknown)) return TrialOutcome.Unknown;
        return TrialOutcome.Pass;
    }
}
