using LouisAgent.Composition.Agents;

namespace LouisAgent.Core.Delegation.Addresses;

/// <summary>
/// An address this instance can answer: what kind of agent it reaches, and the role a session for it opens with (the
/// identity's voice, <c>acme/voice</c>, or a role file's name, <c>acme-cook</c>).
/// </summary>
public sealed record AddressedAgent(AgentAddress Address, AddressKind Kind, string Role);
