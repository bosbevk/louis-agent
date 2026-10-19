namespace LouisAgent.Api.Admin;

/// <summary>What the admin gate decided for a request (F27).</summary>
public enum AdminAccess
{
    /// <summary>No admin key is configured: the portal is closed to everyone, so it is never open by accident.</summary>
    Closed,

    /// <summary>A key is configured and the request didn't present it.</summary>
    Unauthorized,

    Allowed,
}
