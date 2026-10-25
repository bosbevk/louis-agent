using LouisAgent.Api.Messages;
using LouisAgent.Core.Guardrails.Auth;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints.Routing;

/// <summary>
/// The key every API call must present (FX18, CL20): the agent can run shell commands and write files, so every call to the paths
/// <see cref="NeedsKey"/> names must carry <c>AGENT_API_KEY</c>, in <c>x-api-key</c> or as a bearer token. Whether the API starts
/// without one is <c>HostAuth</c>'s decision, made in <c>Program.cs</c> (core's one rule for this and the MCP server over HTTP), and
/// this gate is mapped only when a key is set. The web app's files stay public: they hold no data, and the app sends the key the
/// user enters in its settings.
/// </summary>
internal static class AgentKeyGate
{
    /// <summary>Refuses, with a 401 in the API's error shape, a call to a path that needs the key and doesn't present it.</summary>
    public static WebApplication UseAgentKey(this WebApplication app, string apiKey)
    {
        app.Use(async (context, next) =>
        {
            if (!NeedsKey(context.Request.Path)
                || HostAuth.HasKey(context.Request.Headers["x-api-key"].FirstOrDefault(), context.Request.Headers.Authorization.FirstOrDefault(), apiKey))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiErrors.Body("authentication_error", MessageText.Format(ApiUserText.InvalidApiKey)));
        });
        return app;
    }

    /// <summary>Whether a request path is one of the API's that needs the key; step 2 of CL20 changes this rule, in this one place.</summary>
    internal static bool NeedsKey(PathString path) =>
        path.StartsWithSegments("/sessions") || path.StartsWithSegments("/workspace") ||
        path.StartsWithSegments("/git") || path.StartsWithSegments("/toolsets") ||
        RolesView.Routes.Any(route => path.StartsWithSegments(route)) || path.StartsWithSegments("/model");
}
