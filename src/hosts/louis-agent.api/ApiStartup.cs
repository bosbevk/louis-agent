using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Api.Config;
using LouisAgent.Api.Endpoints.Admin;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Guardrails.Auth;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api;

/// <summary>
/// The API as building blocks your program calls: HTTP for the agent, every reply JSON and a message's answer streaming
/// as Server-Sent Events shaped like Claude's. Your program adds its own web app between the key check and the routes.
/// </summary>
/// <remarks>
/// Extension Methods, the way ASP.NET itself is put together: a program reads as the app it is, and this library never
/// names a pack or a web app, which a package couldn't reference anyway. The cost is that a program could forget a
/// block, so the routes fail closed: they refuse to map until the key check has run.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0074-the-folders-say-what-ships.md">ADR 0074: framework and implementation are split</seealso>
public static class ApiStartup
{
    /// <summary>Marks that the key check has run, for the routes to see.</summary>
    private const string KeyChecked = "LouisAgent.Api.KeyChecked";

    /// <summary>The services the API's routes need, for the agent you built.</summary>
    public static WebApplicationBuilder AddLouisAgentApi(this WebApplicationBuilder builder, AgentHostContext host)
    {
        builder.Services.AddApi(host);
        return builder;
    }

    /// <summary>
    /// The agent can run shell commands and write files, so its calls must present AGENT_API_KEY; which calls is
    /// AgentKeyGate's rule. Without a key the API refuses to start, here, before it listens, unless AGENT_AUTH=off says
    /// it is open on purpose (HostAuth is core's one rule for this and for the MCP server over HTTP).
    /// </summary>
    public static WebApplication UseLouisAgentKey(this WebApplication app)
    {
        string? apiKey = HostAuth.Key(Environment.GetEnvironmentVariable("AGENT_API_KEY"));
        switch (HostAuth.AtStartup(HostAuth.Mode(Environment.GetEnvironmentVariable("AGENT_AUTH")), apiKey))
        {
            case HostAuthDecision.Refuse:
                throw new InvalidOperationException(MessageText.Format(ErrorMessage.ApiKeyRequired,
                    new { Setting = "AGENT_API_KEY" }));
            case HostAuthDecision.StartOpen:
                AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api").Log(LogLevel.Warning, ApiLogMessage.NoApiKey);
                break;
            case HostAuthDecision.Start:
                app.UseAgentKey(apiKey!); // apiKey is set whenever the decision is Start
                break;
        }

        ((IApplicationBuilder)app).Properties[KeyChecked] = true;
        return app;
    }

    /// <summary>
    /// Every route of the agent's API and the admin portal, and what the portal starts in the background. It refuses to
    /// map if your program never called <see cref="UseLouisAgentKey"/>.
    /// </summary>
    public static WebApplication MapLouisAgentApi(this WebApplication app, AgentHostContext host)
    {
        RequireKeyChecked(app);

        // Every route of the agent's API is mapped by ApiMap, so the route tests build what runs, and the API's own
        // admin routes on the gated group. The eval studio is louis-agent.api.evals's, mapped by MapLouisAgentEvals (CL57).
        app.MapApi(host).MapAdmin(AdminOptions.FromEnvironment().AdminKey);

        return app;
    }

    /// <summary>Fail closed: a program that forgot the key check must not come up with these routes open; the studio asks too (CL57).</summary>
    internal static void RequireKeyChecked(WebApplication app)
    {
        if (!((IApplicationBuilder)app).Properties.ContainsKey(KeyChecked))
            throw new InvalidOperationException(MessageText.Format(ApiLogMessage.KeyCheckMissing));
    }
}
