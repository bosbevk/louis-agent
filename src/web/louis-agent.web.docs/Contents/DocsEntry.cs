namespace LouisAgent.Web.Docs.Contents;

/// <summary>One page of a docs site's contents: its name in the menu and its address.</summary>
public sealed record DocsEntry(string Title, string Address)
{
    /// <summary>The root page, whose address every other sits under.</summary>
    private const string Root = "/";

    /// <summary>Whether a path is this page or one of its sections: the root holds only itself.</summary>
    public bool Holds(string path) =>
        path == Address || (Address != Root && path.StartsWith(Address + "/", StringComparison.OrdinalIgnoreCase));
}
