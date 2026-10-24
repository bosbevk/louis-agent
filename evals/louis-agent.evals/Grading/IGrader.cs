using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading;

/// <summary>
/// Scores one trial (F18 step 2). A grader names what it checks and whether a trial passes only when it passes (Required).
/// Code graders come first; a model grader only where code can't judge (step 6).
/// </summary>
public interface IGrader
{
    /// <summary>Its name, as the scores document names the key it fills ("priced-with-source").</summary>
    string Name { get; }

    /// <summary>Recorded with each grade, so a re-score can say which version gave it.</summary>
    int Version { get; }

    GraderKind Kind { get; }

    /// <summary>A trial passes only when every required grader passes.</summary>
    bool Required { get; }

    ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken);
}
