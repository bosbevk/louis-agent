using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Models.Rubrics;

/// <summary>
/// A model judges the last answer against a rubric file, told only the scenario keys the eval lists, and answers
/// yes or no with a reason. A scenario without those keys has nothing to judge, so it passes without a request. It
/// is never required: a judge counts once a person's labels have calibrated it.
/// </summary>
/// <remarks>
/// A Decorator over <see cref="EvaluatorCheck"/> (Gang of Four): the evaluator check does the asking and the mapping,
/// this adds the rubric and the ask. Its <see cref="Entry"/> builds it from a file's settings (Factory Method), reading
/// the rubric from the eval's folder. The grade carries the rubric's hash, so a grade made with another rubric is
/// another measure.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0029-simulations-in-csharp.md">ADR 0029: a judge counts once calibrated</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class JudgeCheck : IRubricCheck
{
    /// <summary>The keys the judge is told, in the order the file lists them.</summary>
    private readonly IReadOnlyList<string> _ask;

    /// <summary>The check that asks the judge and maps its verdict to a grade.</summary>
    private readonly EvaluatorCheck _inner;

    /// <summary>
    /// A judge named <paramref name="name"/> over the rubric's text, telling it the keys in <paramref name="ask"/>.
    /// </summary>
    public JudgeCheck(string name, int version, string rubric, IReadOnlyList<string> ask, Func<ConfiguredJudge?> judge)
    {
        (_ask, Rubric) = (ask, rubric);
        _inner = new EvaluatorCheck(new RubricJudge(name, rubric), judge, name, version, HashOf(rubric),
            new HashSet<string>(ask), trial => [JudgeAsk.Of(trial.Scenario, ask)]);
    }

    /// <inheritdoc />
    public string Rubric { get; }

    public string Name => _inner.Name;
    public int Version => _inner.Version;
    public CheckKind Kind => CheckKind.Model;
    public bool Required => false;
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
    /// The first 12 hex digits of the SHA-256 of the rubric's text: as long as a prompt hash is anywhere else in the
    /// ledger and the portal.
    /// </summary>
    private static string HashOf(string rubric) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rubric)))[..12];

    /// <summary>
    /// What a file can set: the rubric's path in the eval's folder, the scenario keys the judge is told, and the
    /// judge's version, bumped when what it is sent changes in a way the rubric's hash doesn't show.
    /// </summary>
    public sealed record Settings(string Rubric, IReadOnlyList<string>? Ask = null, int Version = 1);

    /// <summary>The check as a file names it, <c>judge</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("judge", CheckKind.Model)
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
            return new JudgeCheck(Type, settings.Version, File.ReadAllText(file), settings.Ask ?? [],
                () => JudgeClients.FromEnvironment());
        }
    }
}
