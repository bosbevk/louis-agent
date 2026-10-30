using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Answers;

/// <summary>
/// The first turn's answer is shorter than the limit, counted in characters: an answer that must stay brief. The
/// reason says the length and the limit. A turn the infrastructure broke is Unknown; a turn that streamed an error
/// fails.
/// </summary>
/// <remarks>
/// A Specification over the answer (Evans and Fowler): one plain rule, a string's length. Its <see cref="Entry"/>
/// builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class LengthUnderCheck(int characters, bool required = true) : ICheck
{
    /// <inheritdoc />
    public string Name => "length-under";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Measures the first turn's answer, after making sure that turn can be judged.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GradeFirst(trial.Transcript.Turns.FirstOrDefault()));

    /// <summary>
    /// The grade of the first turn: Unknown with no turn or a broken one, a fail on a streamed error, then a pass or
    /// a fail on the answer's length.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade GradeFirst(TurnTranscript? first)
    {
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;
        if (EngineEvents.StreamedError(first))
            return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed));

        var length = EngineEvents.AnswerOf(first).Length;
        var measure = new
        {
            Length = length,
            Limit = characters
        };
        return length < characters
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.LengthUnderPass, measure))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.LengthOver, measure));
    }

    /// <summary>What a file can set: the limit in characters, and whether the check must pass.</summary>
    public sealed record Settings(int Characters, bool Required = true);

    /// <summary>The check as a file names it, <c>length-under</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("length-under", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) =>
            new LengthUnderCheck(settings.Characters, settings.Required);
    }
}
