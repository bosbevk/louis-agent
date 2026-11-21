namespace LouisAgent.Core.Agent.Catalog;

/// <summary>
/// The identities and packs a host was given don't fit together (CL15): a name or prompt path in two places, a pack that
/// no one registered, a <c>Requires</c> cycle, a tool name twice or the engine's own, or prompts added after the first
/// was read. Thrown while the host builds, before it listens, naming what to fix.
/// </summary>
public sealed class AgentCatalogException(string message) : Exception(message);
