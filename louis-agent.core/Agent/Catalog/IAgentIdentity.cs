namespace LouisAgent.Core.Agent.Catalog;

using Microsoft.Extensions.FileProviders;

/// <summary>
/// Who an agent is (CL15, ADR 0027): its name, its modes, its own roles, and the packs it uses, by name. It has no tools
/// of its own: everything it can do comes from packs. It may hold workflow code that uses its own packs' public types
/// (the voorman identity's triage workflow, Phase 6); a pack never references another pack or an identity.
/// </summary>
public interface IAgentIdentity
{
    /// <summary>The name <c>IDENTITIES</c> loads it by ("acme"); its modes are <c>agent/{name}/…</c>. Lowercase letters, digits and dashes.</summary>
    string Name { get; }

    /// <summary>Its modes, its own specialists and its own minion roles: content, never converted to MCP.</summary>
    IFileProvider Prompts { get; }

    /// <summary>
    /// The packs it uses, by name: whatever the catalogue holds as "shops" serves, our compiled pack today or an MCP
    /// server named "shops" (F36); the identity can't tell.
    /// </summary>
    IReadOnlyList<string> Packs { get; }
}
