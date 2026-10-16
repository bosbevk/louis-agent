namespace LouisAgent.Api.Services;

/// <summary>A check the portal can run (F27): its script under the workspace, and its own arguments.</summary>
/// <param name="Id">The name in URLs.</param>
/// <param name="Title">What the page calls it.</param>
/// <param name="Script">The script, relative to the workspace root; run with an argument list, never a shell.</param>
/// <param name="Arguments">The script's own arguments after the repeat, so one script can be two checks.</param>
public sealed record BenchmarkCheck(string Id, string Title, string Script, IReadOnlyList<string> Arguments)
{
    public BenchmarkCheck(string id, string title, string script) : this(id, title, script, []) { }
}
