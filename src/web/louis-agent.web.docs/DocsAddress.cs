namespace LouisAgent.Web.Docs;

/// <summary>
/// Where a doc's path in the repo leads: a site names it once, and every doc link on its pages asks it for a doc's
/// whole web address. GitHub today; the docs site, when it is up, by changing only this.
/// </summary>
/// <remarks>
/// A value the site names once (ADR 0040), asked rather than worked out where a link stands (ADR 0123): the pages
/// say which doc, never where docs live, so moving the docs is one setting and no page changes.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record DocsAddress(string Base)
{
    /// <summary>The docs as GitHub shows them: each file on the branch.</summary>
    public static DocsAddress GitHub(string repository, string branch) =>
        new($"https://github.com/{repository}/blob/{branch}");

    /// <summary>The whole web address of the doc at this path in the repo.</summary>
    public string For(string path) => $"{Base.TrimEnd('/')}/{path.TrimStart('/')}";
}
