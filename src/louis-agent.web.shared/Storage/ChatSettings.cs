using System.Text.Json;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Shared.Storage;

/// <summary>The API key for louis-agent.api, if it requires one (kept in this browser only).</summary>
public sealed class ChatSettings
{
    public string? ApiKey { get; set; }

    /// <summary>The admin portal's key (ADMIN_API_KEY), kept apart so saving it never replaces the chat's (F31).</summary>
    public string? AdminKey { get; set; }

    /// <summary>The key a request to <paramref name="uri"/> carries: the admin key under <c>admin/</c>, else the chat's.</summary>
    public string? KeyFor(Uri? uri)
    {
        string path = uri is null ? "" : uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
        path = path.TrimStart('/');
        bool admin = path == "admin" || path.StartsWith("admin/", StringComparison.Ordinal) || path.StartsWith("admin?", StringComparison.Ordinal);
        return admin && !string.IsNullOrWhiteSpace(AdminKey) ? AdminKey : ApiKey;
    }
}
