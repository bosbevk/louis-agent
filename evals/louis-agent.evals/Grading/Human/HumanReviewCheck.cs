using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading.Models.Rubrics;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Human;

/// <summary>
/// A person's review of a judge's question. It is made <b>for</b> one model check, so the person's grade and the
/// judge's are about the same thing, and it is named <c>{judge}.human</c>. It grades every trial
/// <see cref="GradeOutcome.Pending"/>, a grade nobody has given yet: the person's grade lives in the API's store (who
/// and when), not in the transcript, so a run and a re-score write the same Pending line.
/// </summary>
/// <remarks>
/// It is never required: a pending review leaves a trial's outcome as the fixed rules made it, and a run never waits
/// on a person. A scenario its judge has nothing to judge (it lacks the judge's expected keys) gets no grade from it
/// at all, so nothing waits and no panel appears (<see cref="AppliesTo"/>). <see cref="Question"/> is what the person
/// is asked, the judge prompt's own title (<see cref="ReviewQuestion"/>), so the two are not typed twice. Its
/// <see cref="Entry"/> builds it from a file's settings (Factory Method), after the judge it reviews.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class HumanReviewCheck : ICheck
{
    /// <summary>A review of the named model check, asking this question, about scenarios with these keys.</summary>
    private HumanReviewCheck(string reviews, string question, IReadOnlySet<string> expectedKeys) =>
        (Reviews, Question, ExpectedKeys) = (reviews, question, expectedKeys);

    /// <summary>The model grader this reviews.</summary>
    public string Reviews { get; }

    /// <summary>What the person is asked ("Is this a meal for the people asked for?").</summary>
    public string Question { get; }

    public string Name => $"{Reviews}.human";
    public int Version => 2;
    public CheckKind Kind => CheckKind.Human;
    public bool Required => false;

    /// <summary>
    /// The keys its judge reads: a scenario without them gives the judge nothing to judge, so a person has nothing to
    /// review (<see cref="AppliesTo"/>).
    /// </summary>
    public IReadOnlySet<string> ExpectedKeys { get; }

    /// <summary>A review of a model grader; a fixed rule needs no person to vouch for it, so it is refused.</summary>
    public static HumanReviewCheck For(ICheck modelGrader, string question) =>
        modelGrader.Kind == CheckKind.Model
            ? new HumanReviewCheck(modelGrader.Name, question, modelGrader.ExpectedKeys)
            : throw new ArgumentException(MessageText.Format(EvalErrorMessage.ReviewNeedsAModelGrader,
                new { Grader = modelGrader.Name }));

    /// <summary>A review of a model check known only by its name, with no check to read its kind from.</summary>
    public static HumanReviewCheck
        For(string modelGrader, string question, IReadOnlySet<string>? expectedKeys = null) =>
        new(modelGrader, question, expectedKeys ?? new HashSet<string>());

    /// <summary>
    /// Whether this review has a question to ask about the trial: its judge's expected keys are all in the scenario.
    /// </summary>
    public bool AppliesTo(Trial trial) =>
        ExpectedKeys.Count == 0 || (trial.Scenario.Expected.ValueKind == JsonValueKind.Object &&
                                    ExpectedKeys.All(key => trial.Scenario.Expected.TryGetProperty(key, out _)));

    /// <summary>Pending, with the question the person is asked: the person's answer is stored by the API.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Verdict.Pending(Name, Version,
            MessageText.Format(CommonGradeMessage.HumanWaits, new { Question })));

    /// <summary>What a file can set: the name of the judge it reviews.</summary>
    public sealed record Settings(string Reviews);

    /// <summary>The check as a file names it, <c>human</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("human", CheckKind.Human)
    {
        /// <summary>
        /// The review of the judge the eval built before it, under the name the eval gave it, asking its rubric's
        /// title as the question; a name that is no judge built before it is refused, naming it.
        /// </summary>
        protected override ICheck Create(Settings settings, CheckContext context)
        {
            var reviewed = context.Before.LastOrDefault(check => check.Name == settings.Reviews);
            return ((reviewed as NamedCheck)?.Inner ?? reviewed) is JudgeCheck judge
                ? For(reviewed!, ReviewQuestion.From(judge.Rubric))
                : throw new InvalidDataException(MessageText.Format(EvalErrorMessage.ReviewsNoJudge,
                    new { settings.Reviews }));
        }
    }
}
