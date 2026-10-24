namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// A check the portal can run (F27): its script under the workspace and its own arguments (<see cref="BenchmarkCheckKind.Script"/>, the
/// Python checks until step 7), or a C# eval the runner lists under its name (<see cref="BenchmarkCheckKind.Eval"/>, F18 step 4.3).
/// </summary>
/// <param name="Id">The name in URLs; for an eval, the eval's name.</param>
/// <param name="Title">What the page calls it.</param>
/// <param name="Script">The script, relative to the workspace root; run with an argument list, never a shell. Empty for an eval.</param>
/// <param name="Arguments">The script's own arguments after the repeat, so one script can be two checks.</param>
/// <param name="Kind">What it runs.</param>
public sealed record BenchmarkCheck(string Id, string Title, string Script, IReadOnlyList<string> Arguments, BenchmarkCheckKind Kind = BenchmarkCheckKind.Script)
{
    public BenchmarkCheck(string id, string title, string script) : this(id, title, script, []) { }

    /// <summary>An eval the runner lists (F18 step 4.3): its name is its id, and it has no script.</summary>
    public static BenchmarkCheck ForEval(string name, string title) => new(name, title, "", [], BenchmarkCheckKind.Eval);

    /// <summary>
    /// Its manual (F31): <c>HANDLEIDING.md</c> beside its script, so the two change together. An eval's manual is <c>benchmarks/{id}/</c>
    /// until step 7 moves the manuals beside the evals.
    /// </summary>
    public string ManualPath => Kind == BenchmarkCheckKind.Eval
        ? $"benchmarks/{Id}/HANDLEIDING.md"
        : (Path.GetDirectoryName(Script) is { Length: > 0 } folder ? folder + "/" : "").Replace('\\', '/') + "HANDLEIDING.md";
}
