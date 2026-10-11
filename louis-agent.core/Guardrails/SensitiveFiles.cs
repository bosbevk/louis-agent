namespace LouisAgent.Core.Guardrails;

/// <summary>
/// Secret files that neither the agent's file tools nor the web app read or show: <c>.env*</c>, keys and certificates,
/// and credential files.
/// </summary>
internal static class SensitiveFiles
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "credentials.json", "secrets.json", "appsettings.development.json",
        "id_rsa", "id_ed25519"
    };

    internal static bool IsSensitive(string path)
    {
        string name = Path.GetFileName(path);
        return name.StartsWith(".env", StringComparison.OrdinalIgnoreCase) ||
               Names.Contains(name) ||
               name.EndsWith(".pem", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".key", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase);
    }
}