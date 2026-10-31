using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Grading.Models.Rubrics;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Expectations;
using LouisAgent.Identities.Puls.Evals.Checks.Messages;

namespace LouisAgent.Identities.Puls.Judges;

/// <summary>
/// The cook's model check: "is this a meal for the people asked for", the judge of <see cref="MealForPeople"/> behind
/// the harness's <see cref="EvaluatorCheck"/>. It judges the last answer, with the ask built from the trial as the
/// judge's context. A scenario that asks for no number of people (<c>serves</c>) has no meal to judge: it passes
/// without a request.
/// </summary>
/// <remarks>
/// It is shown and counted but <b>not required</b>: the outcome does not wait on it until its calibration against a
/// person's labels passes. It reads <c>serves</c>, which the fixed rule <c>serves-as-asked</c> also reads: that
/// one checks the number is named, this one that the meal is for them. The cook's own judge, not the generic
/// <c>judge</c>: its rubric asks for <c>meal</c>, and its ask is <see cref="CookAsk"/>'s people and town, which is
/// what its calibration measured (Louis's pick, F49.2 step 8a). Its <see cref="Entry"/> reads the rubric from the
/// eval's folder (Factory Method); the same words give the same prompt hash on every platform
/// (<see cref="RubricHash"/>).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class MealForPeopleCheck : IRubricCheck
{
    /// <summary>
    /// Version 2: what the judge is sent changed (the conversation so far, no budget), which the rubric's hash doesn't
    /// cover.
    /// </summary>
    private const int JudgeVersion = 2;

    /// <summary>The check that asks the judge and maps its verdict to a grade.</summary>
    private readonly EvaluatorCheck _inner;

    /// <summary>The cook's judge over a rubric's text.</summary>
    private MealForPeopleCheck(string rubric, EvaluatorCheck inner) => (Rubric, _inner) = (rubric, inner);

    /// <summary>
    /// The cook's judge over this rubric, asking the judge <paramref name="judge"/> gives when it first grades.
    /// </summary>
    public static MealForPeopleCheck Create(string rubric, Func<ConfiguredJudge?> judge) =>
        new(rubric, new EvaluatorCheck(new MealForPeople(rubric), judge, MealForPeople.MetricName, JudgeVersion,
            RubricHash.Of(rubric), new HashSet<string> { "serves" }, trial => [CookAsk.Of(trial)]));

    /// <inheritdoc />
    public string Rubric { get; }

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public int Version => _inner.Version;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.Model;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys => _inner.ExpectedKeys;

    /// <summary>Never required: the outcome doesn't wait on it until its calibration passes.</summary>
    public bool Required => false;

    /// <summary>Asks the judge, unless the scenario asks for no number of people.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        CookExpectations.ServesOf(trial.Scenario).Count == 0
            ? ValueTask.FromResult(Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.MealNotAsked)))
            : _inner.GradeAsync(trial, cancellationToken);

    /// <summary>What a file can set: the rubric's path in the eval's folder.</summary>
    public sealed record Settings(string Rubric);

    /// <summary>The check as a file names it, <c>meal-for-people</c>.</summary>
    public sealed class Entry() : CheckType<Settings>(MealForPeople.MetricName, CheckKind.Model)
    {
        /// <summary>
        /// Reads the rubric from the eval's folder, refusing one that isn't there, and builds the judge over it with
        /// the judge the environment names, built when it first grades.
        /// </summary>
        protected override ICheck Create(Settings settings, CheckContext context)
        {
            var file = Path.Combine(context.Folder, settings.Rubric);
            if (!File.Exists(file))
                throw new InvalidDataException(MessageText.Format(EvalErrorMessage.RubricMissing, new { File = file }));
            return MealForPeopleCheck.Create(File.ReadAllText(file), () => JudgeClients.FromEnvironment());
        }
    }
}
