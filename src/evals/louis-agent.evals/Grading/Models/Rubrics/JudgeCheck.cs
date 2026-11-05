using LouisAgent.Composition.Messages;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Models.Rubrics.Sources;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Models.Rubrics;

/// <summary>
/// A model judges the last answer against a rubric file, told only the scenario keys the eval lists (and, when the
/// file names tools, what those tools gave back in the trial), and answers yes or no with a reason. A scenario
/// without those keys has nothing to judge, so it passes without a request. It is never required: a judge counts
/// once a person's labels have calibrated it.
/// </summary>
/// <remarks>
/// A Decorator over <see cref="EvaluatorCheck"/> (Gang of Four): the evaluator check does the asking and the mapping,
/// this adds the rubric and the ask. Its <see cref="Entry"/> builds it from a file's settings (Factory Method), reading
/// the rubric from the eval's folder. The grade carries the rubric's hash, so a grade made with another rubric is
/// another measure.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0029-simulations-in-csharp.md">ADR 0029: a judge counts once calibrated</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class JudgeCheck : IRubricCheck
{
    /// <summary>The keys the judge is told, in the order the file lists them.</summary>
    private readonly IReadOnlyList<string> _ask;

    /// <summary>The check that asks the judge and maps its verdict to a grade.</summary>
    private readonly EvaluatorCheck _inner;

    /// <summary>
    /// A judge named <paramref name="name"/> over the rubric's text, telling it the keys in <paramref name="ask"/> and
    /// what the tools in <paramref name="sources"/> gave back in the trial (none when it is left out).
    /// </summary>
    public JudgeCheck(string name, int version, string rubric, IReadOnlyList<string> ask, Func<ConfiguredJudge?> judge,
        IReadOnlyList<string>? sources = null)
    {
        (_ask, Rubric, Sources) = (ask, rubric, sources ?? []);
        var tools = Sources;
        _inner = new EvaluatorCheck(new RubricJudge(name, rubric), judge, name, version, RubricHash.Of(rubric),
            new HashSet<string>(ask), trial => [JudgeAsk.Of(trial.Scenario, ask), JudgeSources.Of(trial, tools)]);
    }

    /// <inheritdoc />
    public string Rubric { get; }

    /// <summary>The tools whose results the judge is told, in the order the file lists them.</summary>
    public IReadOnlyList<string> Sources { get; }

    /// <summary>The name its grades carry: the file's, or <c>judge</c>.</summary>
    public string Name => _inner.Name;

    /// <summary>The version its grades carry: the file's, bumped when what the judge is sent changes.</summary>
    public int Version => _inner.Version;

    /// <summary>A model judges it.</summary>
    public CheckKind Kind => CheckKind.Model;

    /// <summary>Never required: a judge counts once a person's labels have calibrated it.</summary>
    public bool Required => false;

    /// <summary>The scenario keys it is told, which the loader checks every scenario sets.</summary>
    public IReadOnlySet<string> ExpectedKeys => _inner.ExpectedKeys;

    /// <summary>Asks the judge, unless the scenario sets none of what it would be told.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        Asked(trial)
            ? _inner.GradeAsync(trial, cancellationToken)
            : ValueTask.FromResult(Verdict.Pass(Name, Version,
                MessageText.Format(CommonGradeMessage.JudgeNothingAsked, new { Keys = string.Join(", ", _ask) })));

    /// <summary>True when the scenario sets every key the judge is told.</summary>
    private bool Asked(Trial trial) =>
        _ask.Count == 0 || (trial.Scenario.Expected.ValueKind == JsonValueKind.Object &&
                            _ask.All(key => trial.Scenario.Expected.TryGetProperty(key, out _)));

    /// <summary>
    /// What a file can set: the rubric's path in the eval's folder, the scenario keys the judge is told, the judge's
    /// version, and <c>sources</c>, the names of tools whose results from the trial the judge is told too.
    /// </summary>
    /// <remarks>
    /// The version is the file's to bump: the rubric's hash covers the rubric only, so a file that adds <c>sources</c>
    /// (or changes them) bumps its judge's version, and a grade made without them is not counted with one made with.
    /// Each result is cut at <see cref="JudgeSources.ResultLimit"/> characters and the request says where it was cut.
    /// A tool that was never called is told in one line. A specialist's calls count, as for <c>tool-used</c>.
    /// </remarks>
    public sealed record Settings(
        string Rubric,
        IReadOnlyList<string>? Ask = null,
        int Version = 1,
        IReadOnlyList<string>? Sources = null);

    /// <summary>The check as a file names it, <c>judge</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("judge", CheckKind.Model)
    {
        /// <summary>
        /// Reads the rubric from the eval's folder, refusing one that isn't there, and builds the judge over its body
        /// (its header, CL27, is for the portal: neither sent nor hashed, so no calibration moves) with the judge the
        /// environment names, built when it first grades.
        /// </summary>
        protected override ICheck Create(Settings settings, CheckContext context)
        {
            var file = Path.Combine(context.Folder, settings.Rubric);
            if (!File.Exists(file))
                throw new InvalidDataException(MessageText.Format(EvalErrorMessage.RubricMissing, new { File = file }));
            string rubric = PromptHeaders.Split(settings.Rubric, File.ReadAllText(file)).Body;
            return new JudgeCheck(Type, settings.Version, rubric, settings.Ask ?? [],
                () => JudgeClients.FromEnvironment(), settings.Sources);
        }
    }
}
