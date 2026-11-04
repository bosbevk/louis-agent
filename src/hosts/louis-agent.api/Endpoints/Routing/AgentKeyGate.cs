using LouisAgent.Api.Messages;
using LouisAgent.Core.Guardrails.Auth;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints.Routing;

/// <summary>
/// The key every API call must present (FX18, CL20): the agent can run shell commands and write files, so a call to
/// any route under <c>/v1</c> must carry <c>AGENT_API_KEY</c>, in <c>x-api-key</c> or as a bearer token, unless the
/// route is marked <see cref="RouteAccess.Open"/> or <see cref="RouteAccess.AdminKey"/> (ADR 0039, rule 6): a new
/// route is closed until the code opens it. Whether the API starts without a key is <c>HostAuth</c>'s decision, made
/// in <c>Program.cs</c> (core's one rule for this and the MCP server over HTTP), and this gate is mapped only when a
/// key is set. <c>/health</c> and the web app's files are outside <c>/v1</c> and stay public: they hold no data, and
/// the app sends the key the user enters in its settings.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class AgentKeyGate
{
    /// <summary>
    /// Turns away a call to a route that needs the key and doesn't bring it, with a 401 in the API's error shape.
    /// </summary>
    public static WebApplication UseAgentKey(this WebApplication app, string apiKey)
    {
        app.Use(async (context, next) =>
        {
            // An open route, or the right key: let it through.
            string? presented = context.Request.Headers["x-api-key"].FirstOrDefault();
            string? bearer = context.Request.Headers.Authorization.FirstOrDefault();
            if (!NeedsKey(context.Request.Path, context.GetEndpoint()) || HostAuth.HasKey(presented, bearer, apiKey))
            {
                await next(context);
                return;
            }

            // The gate isn't an endpoint, so it writes the error's body itself.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiErrors.Body(ApiErrorKind.Authentication,
                MessageText.Format(ApiUserText.InvalidApiKey)));
        });
        return app;
    }

    /// <summary>
    /// Who may call the route a request matched: outside <c>/v1</c> anyone (<see cref="RouteAccess.Open"/>), under it
    /// the route's mark, which is <see cref="RouteAccess.Key"/> when it has none. A path under <c>/v1</c> that matches
    /// no route is the API's own 404.
    /// </summary>
    internal static RouteAccess AccessFor(PathString path, Endpoint? endpoint) =>
        path.StartsWithSegments(ApiMap.Version) ? RouteAccessExtensions.AccessOf(endpoint) : RouteAccess.Open;

    /// <summary>Whether the call must carry the agent's key.</summary>
    internal static bool NeedsKey(PathString path, Endpoint? endpoint) => AccessFor(path, endpoint) == RouteAccess.Key;
}
