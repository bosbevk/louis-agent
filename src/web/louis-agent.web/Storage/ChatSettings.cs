namespace LouisAgent.Web.Storage;

/// <summary>
/// How a site's chat talks to the API and draws itself: the keys it carries (kept in this browser only) and whether a
/// card's answer is a bubble too.
/// </summary>
public sealed class ChatSettings
{
    /// <summary>The API key for louis-agent.api, if it requires one.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The admin portal's key, kept apart so saving it never replaces the chat's (F31).</summary>
    public string? AdminKey { get; set; }

    /// <summary>
    /// Whether a message that answers a card or a form the turn before drew is a bubble too (FX142). Off by default,
    /// since the folded card already shows the answer; a site that wants both turns it on.
    /// </summary>
    public bool ShowCardAnswers { get; set; }

    /// <summary>The key a request to this address carries: the admin key on admin routes, else the chat's.</summary>
    public string? KeyFor(Uri? uri)
    {
        string path = PathOf(uri).TrimStart('/');
        bool admin = path == "v1/admin" || path.StartsWith("v1/admin/", StringComparison.Ordinal) ||
                     path.StartsWith("v1/admin?", StringComparison.Ordinal);
        return admin && !string.IsNullOrWhiteSpace(AdminKey) ? AdminKey : ApiKey;
    }

    /// <summary>The address's path, whether it came whole or relative to the API.</summary>
    /// <remarks>A switch on the address's shape, one arm per case, instead of a ternary inside a ternary (ADR 0051).</remarks>
    private static string PathOf(Uri? uri) => uri switch
    {
        null => "",
        { IsAbsoluteUri: true } => uri.AbsolutePath,
        _ => uri.OriginalString
    };
}
