using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.SiteApi;

/// <summary>
/// The framework's pages moved under <c>/framework</c> (F128.1), so the site sells first and those pages can later
/// move whole to the framework's own site. An old address from a bookmark or a search result is sent on, for good, to
/// where its place is now, with its section and its page of a section kept.
/// </summary>
public static class MovedPages
{
    /// <summary>Each old page's address and where it lives now.</summary>
    public static IReadOnlyDictionary<string, string> Moves { get; } = new Dictionary<string, string>
    {
        ["/how-it-works"] = "/framework/how-it-works",
        ["/architecture"] = "/framework/architecture",
        ["/identities"] = "/framework/identities",
        ["/packs"] = "/framework/packs",
        ["/build-your-own"] = "/framework/build-your-own",
        ["/proof"] = "/framework/measured",
        ["/built-on-it"] = "/framework/built-on-it",
        ["/whats-coming"] = "/framework/whats-coming"
    };

    /// <summary>Where an old address lives now, or null when it never moved.</summary>
    public static string? Where(string path)
    {
        var parts = path.ToLowerInvariant().Trim('/').Split('/');
        if (!Moves.TryGetValue("/" + parts[0], out string? now)) return null;

        // What came after the old page (a section, a page of it) comes after the new one.
        return string.Join('/', parts.Skip(1).Prepend(now));
    }

    /// <summary>Sends each old page, and every place under it, on to its new address.</summary>
    public static WebApplication MapMovedPages(this WebApplication app)
    {
        foreach (string old in Moves.Keys)
            app.MapGet(old + "/{**rest}", Redirect);

        return app;
    }

    /// <summary>
    /// A permanent move, so a search engine keeps the new address, with the question the old one asked.
    /// </summary>
    private static RedirectHttpResult Redirect(HttpContext context) =>
        TypedResults.Redirect(Where(context.Request.Path)! + context.Request.QueryString, permanent: true);
}
