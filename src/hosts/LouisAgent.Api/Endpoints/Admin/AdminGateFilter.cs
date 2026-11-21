using LouisAgent.Api.Admin;
using LouisAgent.Api.Messages;
using LouisAgent.Composition.Messages;

namespace LouisAgent.Api.Endpoints.Admin;

/// <summary>
/// Lets a call through only with the admin key. If no key is set up you get a 403 that says what to set, and with the
/// wrong key a 401. One filter on the admin group, so the check runs before any route's own work.
/// </summary>
/// <remarks>Guard clauses (Fowler), one per refusal, so the three outcomes read top to bottom.</remarks>
internal sealed class AdminGateFilter(string? adminKey)
{
    /// <summary>The endpoint filter: refuse, or hand on to the route.</summary>
    public async ValueTask<object?> Gate(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var access = AdminGate.Check(adminKey, AdminGate.Presented(context.HttpContext.Request));
        if (access == AdminAccess.Closed)
            return ApiErrors.Forbidden(MessageText.Format(ApiUserText.AdminClosed));
        if (access == AdminAccess.Unauthorized)
            return ApiErrors.Unauthorized(MessageText.Format(ApiUserText.InvalidApiKey));
        return await next(context);
    }
}
