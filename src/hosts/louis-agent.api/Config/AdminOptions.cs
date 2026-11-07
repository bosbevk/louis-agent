using LouisAgent.Api.Admin;

namespace LouisAgent.Api.Config;

/// <summary>
/// The admin portal's one setting: its key, ADMIN_API_KEY, else AGENT_API_KEY; neither, and the admin routes are
/// closed. The studio's settings are its own (<c>EvalOptions</c> in louis-agent.api.evals, CL57).
/// </summary>
/// <param name="AdminKey">The key the admin routes take; none closes them.</param>
public sealed record AdminOptions(string? AdminKey)
{
    /// <summary>Reads the key from the environment.</summary>
    public static AdminOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;

        // A blank setting is the same as a missing one; anything else comes back trimmed.
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        return new AdminOptions(AdminGate.Key(Get("ADMIN_API_KEY"), Get("AGENT_API_KEY")));
    }
}
