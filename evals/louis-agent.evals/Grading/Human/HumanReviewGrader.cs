using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Human;

/// <summary>
/// A person's review of a model grader's question (F18 step 6a). It is made <b>for</b> one model grader, so the person's grade and the judge's are about the same thing, and it is named
/// <c>{grader}.human</c>. It grades every trial <see cref="GradeOutcome.Pending"/>, a grade nobody has given yet: the person's grade lives in the API's store (who and when), not in the
/// transcript, so a run and a re-score write the same Pending line. It is never required: a pending review leaves a trial's outcome as the fixed rules made it, and a run never waits on a
/// person. A scenario its judge has nothing to judge (it lacks the judge's expected keys) gets no grade from it at all, so nothing waits and no panel appears (<see cref="AppliesTo"/>, step 6a.4). <see cref="Question"/> is what the person is asked, the judge prompt's own title (<see cref="ReviewQuestion"/>), so the two are not typed twice.
/// </summary>
public sealed class HumanReviewGrader : IGrader
{
    private HumanReviewGrader(string reviews, string question, IReadOnlySet<string> expectedKeys) => (Reviews, Question, ExpectedKeys) = (reviews, question, expectedKeys);

    /// <summary>The model grader this reviews.</summary>
    public string Reviews { get; }

    /// <summary>What the person is asked ("Is this a meal for the people asked for?").</summary>
    public string Question { get; }

    public string Name => $"{Reviews}.human";
    public int Version => 2;
    public GraderKind Kind => GraderKind.Human;
    public bool Required => false;
    
    /// <summary>The keys its judge reads: a scenario without them gives the judge nothing to judge, so a person has nothing to review (<see cref="AppliesTo"/>).</summary>
    public IReadOnlySet<string> ExpectedKeys { get; }

    /// <summary>A review of a model grader; a fixed rule needs no person to vouch for it, so it is refused.</summary>
    public static HumanReviewGrader For(IGrader modelGrader, string question) =>
        modelGrader.Kind == GraderKind.Model
            ? new HumanReviewGrader(modelGrader.Name, question, modelGrader.ExpectedKeys)
            : throw new ArgumentException(MessageText.Format(EvalErrorMessage.ReviewNeedsAModelGrader, new { Grader = modelGrader.Name }));

    public static HumanReviewGrader For(string modelGrader, string question, IReadOnlySet<string>? expectedKeys = null) => new(modelGrader, question, expectedKeys ?? new HashSet<string>());

    /// <summary>Whether this review has a question to ask about the trial: its judge's expected keys are all in the scenario.</summary>
    public bool AppliesTo(Trial trial) =>
        ExpectedKeys.Count == 0 || (trial.Scenario.Expected.ValueKind == JsonValueKind.Object && ExpectedKeys.All(key => trial.Scenario.Expected.TryGetProperty(key, out _)));

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Verdict.Pending(Name, Version, MessageText.Format(CommonGradeMessage.HumanWaits, new { Question })));
}
