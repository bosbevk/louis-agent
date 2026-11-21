namespace LouisAgent.Api.Messages.Holes.Sessions;

/// <summary>
/// Who opened a session, as the log may show them: the user and the tenant, hashed unless set otherwise.
/// </summary>
public sealed record SessionOpenedByHoles(string Session, string User, string Tenant);
