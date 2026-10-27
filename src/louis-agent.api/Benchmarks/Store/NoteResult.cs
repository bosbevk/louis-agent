using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// What happened when someone tried to add a note: it was kept, there's no such run, or the text was turned down.
/// </summary>
/// <remarks>
/// Result object. Whoever adds a note gets one of three answers and has to handle each, so a refusal is part of the normal return and not an exception to catch. A closed set of cases means the compiler tells you when one is missed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public abstract record NoteResult
{
    private NoteResult()
    {
    }

    /// <summary>The note was kept; this is it as stored.</summary>
    public sealed record Added(RunNote Note) : NoteResult;

    /// <summary>There's no such run, so nothing was kept.</summary>
    public sealed record NoRun : NoteResult;

    /// <summary>The text was turned down, and why. The length lets the message say how long it was.</summary>
    public sealed record Refused(NoteRefusal Reason, int Length) : NoteResult;
}
