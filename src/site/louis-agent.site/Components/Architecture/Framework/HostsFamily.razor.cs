using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Site.Components.Architecture.Framework;

/// <summary>
/// The hosts' family on the Architecture page: the libraries a program starts, and the accounts beside them.
/// </summary>
public partial class HostsFamily
{
    /// <summary>Each host library, and what it is.</summary>
    private static readonly IReadOnlyList<Term> Terms =
    [
        new(["louis-agent.cli"], "A chat in the terminal, interactive or one prompt and out."),
        new(["louis-agent.acp"], "The agent inside JetBrains Rider's AI chat, over the Agent Client Protocol on stdio."),
        new(["louis-agent.api"], "The HTTP API: sessions, answers streamed as server-sent events, the workspace and git routes, the admin screens. It serves a site's web app from the same origin."),
        new(["louis-agent.api.contracts"], "The API's replies and bodies as records, so a client reads what the server writes."),
        new(["louis-agent.api.evals"], "The eval studio on the API: the runner started as a child process, the stores, the recorders, the reports and their admin routes. A program opts in."),
        new(["louis-agent.mcp"], "Hands the agent's tools to any MCP client, which brings its own model, over stdio or HTTP."),
        new(["louis-agent.accounts"], "Sign-in, registration and profiles on ASP.NET Core Identity, a tenant on every account from the host name, EF Core on SQL Server, with social logins, passkeys and a second factor."),
        new(["louis-agent.accounts.contracts"], "The records the account routes send and read.")
    ];
}
