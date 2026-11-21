namespace LouisAgent.Core.Web;

/// <summary>
/// What the web access is, in words a host can write down (CL57): the search provider's and the page renderer's wire
/// names, and whether each is set up. The eval studio records it with a run, so two runs can be told apart by what
/// they searched with; a host reads it from the seam, never from the web library.
/// </summary>
/// <remarks>
/// A Value Object on the seam (ADR 0092): the API used to ask the web library's factories, which tied it to the
/// browser binary for two lines. A name is <c>null</c> when the settings pick nothing, and a readiness is only
/// meaningful beside a name.
/// </remarks>
/// <param name="SearchProvider">The search provider's wire name (<c>tavily</c>, <c>duckduckgo</c>…), or none.</param>
/// <param name="SearchReady">Whether that provider has what it needs (its key, its address).</param>
/// <param name="PageRenderer">The page renderer's wire name (<c>chrome</c>, <c>tavily</c>), or none.</param>
/// <param name="RendererReady">Whether that renderer has what it needs.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0092-a-project-holds-what-is-reused-together.md">ADR 0092: a project holds what is reused together</seealso>
public sealed record WebSetup(string? SearchProvider, bool SearchReady, string? PageRenderer, bool RendererReady)
{
    /// <summary>No web access: nothing named, nothing ready.</summary>
    public static readonly WebSetup None = new(null, false, null, false);
}
