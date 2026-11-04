namespace LouisAgent.Core.Guardrails.Workspace;

/// <summary>
/// Secret files that neither the agent's file tools nor the web app read or show: <c>.env*</c>, keys and certificates,
/// and credential files. A <c>.env</c> template (<c>.env.example</c>, <c>.env.secrets.example</c>, <c>.sample</c>,
/// <c>.template</c>) is not one: it documents the settings and is committed, so it holds no secrets (FX12).
/// </summary>
internal static class SensitiveFiles
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "credentials.json", "secrets.json", "appsettings.development.json",
        "id_rsa", "id_ed25519"
    };

    private static readonly string[] TemplateEndings = [".example", ".sample", ".template"];

    internal static bool IsSensitive(string path)
    {
        string name = Path.GetFileName(path);
        return (name.StartsWith(".env", StringComparison.OrdinalIgnoreCase) &&
                !TemplateEndings.Any(e => name.EndsWith(e, StringComparison.OrdinalIgnoreCase))) ||
               Names.Contains(name) ||
               name.EndsWith(".pem", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".key", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase);
    }
}