namespace LouisAgent.Api.Services;

/// <summary>A check the portal can run (F27): its script under the workspace, and the folder names its saved runs use.</summary>
/// <param name="Id">The name in URLs and run folders.</param>
/// <param name="Title">What the page calls it.</param>
/// <param name="Script">The script, relative to the workspace root; run with an argument list, never a shell.</param>
/// <param name="FolderPrefixes">The <c>samples/</c> folder names its runs start with (the saved samples, and the portal's own).</param>
public sealed record BenchmarkCheck(string Id, string Title, string Script, IReadOnlyList<string> FolderPrefixes);
