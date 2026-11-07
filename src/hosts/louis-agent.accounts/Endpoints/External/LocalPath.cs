namespace LouisAgent.Accounts.Endpoints.External;

/// <summary>
/// Where the callback may send a browser after a provider's sign-in: a path on this site, else home. An address
/// another site put in the query (<c>//evil.test</c>, <c>https://…</c>) is never followed, since a callback that
/// forwards anywhere is a phishing tool. The web has the same rule for its own pages; this is the server's.
/// </summary>
public static class LocalPath
{
    /// <summary>The site's home.</summary>
    public const string Home = "/";

    /// <summary>The path to send the browser to: one of ours, with a leading slash, else home.</summary>
    public static string Of(string? returnUrl)
    {
        string url = (returnUrl ?? "").Trim();
        if (url.Length == 0) return Home;
        if (url.StartsWith("//", StringComparison.Ordinal) || url.StartsWith('\\')) return Home;
        if (url.Contains("://", StringComparison.Ordinal) || url.Contains(':', StringComparison.Ordinal)) return Home;
        return url.StartsWith('/') ? url : "/" + url;
    }
}
