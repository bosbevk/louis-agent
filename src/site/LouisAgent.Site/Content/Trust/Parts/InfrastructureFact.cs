namespace LouisAgent.Site.Content.Trust.Parts;

/// <summary>The facts of where the site runs and who can reach it, in the order the trust page tells them.</summary>
public enum InfrastructureFact
{
    InSouthAfrica,
    OnlyHttps,
    Encrypted,
    NoPasswordsInTheCode,
    OneThingPerContainer,
    OutboundNarrowed,
    NotRoot
}
