using LouisAgent.Choices;

namespace LouisAgent.Identities.Puls;

/// <summary>
/// The packs Puls names (ADR 0043: each project keeps its own enum of the names it uses; the identity never references a pack's enum). The composition root's <c>PackKind</c> is the host's copy, and
/// its tests check that every pack an identity names is one the catalogue builds.
/// </summary>
public enum PulsPack
{
    [WireName("shops")] Shops,
}
