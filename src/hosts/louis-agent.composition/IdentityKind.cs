using LouisAgent.Choices;

namespace LouisAgent.Composition;

/// <summary>The identities a host may load with <c>IDENTITIES</c> (ADR 0024: an enum, parsed once). F40 adds <c>Demo</c>.</summary>
public enum IdentityKind
{
    [WireName("puls")] Puls,
}
