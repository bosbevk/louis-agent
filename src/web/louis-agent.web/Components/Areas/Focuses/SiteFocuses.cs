namespace LouisAgent.Web.Components.Areas.Focuses;

/// <summary>
/// The focus of each part of a site (F127.7): what its agent concentrates on, by where the visitor is. A page takes the
/// focus of the longest part that holds it, else the default; a site that names no default leaves other pages without.
/// </summary>
/// <remarks>
/// The site names its parts through the builder (ADR 0120); the guide's widget looks up the page's focus here and sends
/// it with the page, and the agent reads that part's rules. The longest part wins, so a deeper page can have its own
/// focus inside a wider part.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is wired by hand from the framework</seealso>
public sealed record SiteFocuses(string? Default, IReadOnlyList<FocusPart> Parts)
{
    /// <summary>A page's focus, by its address: the longest part that holds it, else the default.</summary>
    public string? Of(string path)
    {
        string page = PathOf(path);
        var holding = Parts.Where(part => Holds(part.Under, page)).MaxBy(part => part.Under.TrimEnd('/').Length);
        return holding?.Focus ?? Default;
    }

    /// <summary>Whether a part's address holds the page: the same address, or one under it, by whole segments.</summary>
    private static bool Holds(string under, string page)
    {
        string part = "/" + under.Trim('/');
        return page.Equals(part, StringComparison.OrdinalIgnoreCase) ||
               page.StartsWith(part + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The page's path alone: no host, no query, no place in it, one leading slash.</summary>
    private static string PathOf(string address)
    {
        string path = Uri.TryCreate(address, UriKind.Absolute, out var whole) ? whole.AbsolutePath : address;
        int cut = path.IndexOfAny(['?', '#']);
        return "/" + (cut < 0 ? path : path[..cut]).Trim('/');
    }
}
