using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;

/// <summary>
/// The cook's model grader (F18 step 6.2): "is this a meal for the people asked for", the judge of <see cref="MealForPeople"/> behind the harness's
/// <see cref="EvaluatorGrader"/>. It judges the last answer, with the ask built from the trial as the judge's context. A scenario that asks for no number of people (<c>serves</c>) has no
/// meal to judge: it passes without a request. It is shown and counted but <b>not required</b>: the outcome does not wait on it until its calibration against a person's labels
/// passes (step 6.3). It reads <c>serves</c>, which the fixed rule <see cref="ServesAsAsked"/> also reads: that one checks the number is named, this one that the meal is for them.
/// </summary>
public sealed class MealForPeopleGrader : IGrader
{
    /// <summary>Version 2 (6a.3d): what the judge is sent changed in 6a.4f (the conversation so far, no budget), which the prompt's hash doesn't cover.</summary>
    private const int JudgeVersion = 2;

    private readonly EvaluatorGrader _inner;

    private MealForPeopleGrader(EvaluatorGrader inner) => _inner = inner;

    public static MealForPeopleGrader Create(Func<ConfiguredJudge?> judge) =>
        new(new EvaluatorGrader(new MealForPeople(MealForPeoplePrompt.Text), judge, MealForPeople.MetricName,
            JudgeVersion, MealForPeoplePrompt.Hash,
            new HashSet<string> { "serves" }, trial => [CookAsk.Of(trial)]));

    public string Name => _inner.Name;
    public int Version => _inner.Version;
    public GraderKind Kind => GraderKind.Model;
    public IReadOnlySet<string> ExpectedKeys => _inner.ExpectedKeys;
    public bool Required => false;

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        CookExpectations.ServesOf(trial.Scenario).Count == 0
            ? ValueTask.FromResult(Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.MealNotAsked)))
            : _inner.GradeAsync(trial, cancellationToken);
}
