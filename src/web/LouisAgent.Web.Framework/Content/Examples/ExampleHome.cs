namespace LouisAgent.Web.Framework.Content.Examples;

/// <summary>
/// Where an example lives: a folder in this repository, or a path in another repository of ours on GitHub, such as
/// Puls in its own (F166.4). It says which, what it is called and where it links, so the page and its test ask it.
/// </summary>
/// <remarks>
/// A value type for a small idea, ADR 0123's cure for a bare path that would have to be read as a folder or an
/// address wherever it is used (Louis's pick A of F166.4's finding 3, over a string that may be a URL and a second
/// list for products elsewhere).
/// </remarks>
/// <param name="RepositoryName">The other repository, as <c>owner/name</c>; none for a folder here.</param>
/// <param name="Path">The folder's path in its repository.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record ExampleHome(string? RepositoryName, string Path)
{
    /// <summary>This repository, which a folder here is linked in.</summary>
    private const string ThisRepository = "bosbevk/louis-agent";

    /// <summary>Where GitHub serves a repository.</summary>
    private const string GitHub = "https://github.com/";

    /// <summary>A folder in this repository.</summary>
    public static ExampleHome Here(string folder) => new(null, folder);

    /// <summary>A path in another repository of ours on GitHub.</summary>
    public static ExampleHome Elsewhere(string repository, string path) => new(repository, path);

    /// <summary>Whether it is in this repository, where a test can find it on the disk.</summary>
    public bool IsHere => RepositoryName is null;

    /// <summary>What it is called: its folder's own name.</summary>
    public string Name => System.IO.Path.GetFileName(Path);

    /// <summary>Its folder on GitHub, on its repository's main branch.</summary>
    public string Link => $"{Repository}/tree/main/{Path}";

    /// <summary>Its repository's own page on GitHub.</summary>
    public string Repository => GitHub + (RepositoryName ?? ThisRepository);
}
