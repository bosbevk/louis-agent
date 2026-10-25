namespace LouisAgent.Api.Endpoints.Routing;

/// <summary>Who may call a route under <c>/v1</c> (CL20, ADR 0039, rule 6). A route marks nothing and is <see cref="Key"/>.</summary>
internal enum RouteAccess
{
    /// <summary>The agent's key, <c>AGENT_API_KEY</c>: what a route is until the code says otherwise.</summary>
    Key,

    /// <summary>Anyone: the probe, the prices and the rates, which hold nothing secret and which other hosts fetch without a key.</summary>
    Open,

    /// <summary>The admin portal's own key, checked by <c>AdminGate</c>, never by the agent's key gate.</summary>
    AdminKey,
}
