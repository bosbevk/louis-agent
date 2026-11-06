namespace LouisAgent.Web.Storage;

/// <summary>The API key for louis-agent.api, if it requires one (kept in this browser only).</summary>
public sealed class ChatSettings
{
    public string? ApiKey { get; set; }

    /// <summary>The admin portal's key, kept apart so saving it never replaces the chat's (F31).</summary>
    public string? AdminKey { get; set; }

    /// <summary>The key a request to this address carries: the admin key on admin routes, else the chat's.</summary>
    public string? KeyFor(Uri? uri)
    {
        string path = uri is null ? "" : uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
        path = path.TrimStart('/');
        bool admin = path == "v1/admin" || path.StartsWith("v1/admin/", StringComparison.Ordinal) || path.StartsWith("v1/admin?", StringComparison.Ordinal);
        return admin && !string.IsNullOrWhiteSpace(AdminKey) ? AdminKey : ApiKey;
    }
}
