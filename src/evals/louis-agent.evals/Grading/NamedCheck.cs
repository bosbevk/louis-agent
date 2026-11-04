using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading;

/// <summary>
/// A check under the name an eval file gives it (<c>cook-no-error</c> for the general <c>no-error</c>), so a stored
/// run that carries the old name reads the same. Each grade it returns carries that name; everything else is the
/// check's own.
/// </summary>
/// <remarks>
/// A Decorator (Gang of Four): no entry learns about names; the eval wraps the one check its file renames.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class NamedCheck(string name, ICheck inner) : ICheck
{
    /// <summary>
    /// The check under the name, for a reader that needs to know what it is (a person's review of a judge).
    /// </summary>
    public ICheck Inner => inner;

    public string Name => name;
    public int Version => inner.Version;
    public CheckKind Kind => inner.Kind;
    public bool Required => inner.Required;
    public IReadOnlySet<string> ExpectedKeys => inner.ExpectedKeys;

    /// <summary>The check's grade, under the name.</summary>
    public async ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        await inner.GradeAsync(trial, cancellationToken) with { Check = name };
}
