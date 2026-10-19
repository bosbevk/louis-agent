namespace LouisAgent.Identities.Puls;

using LouisAgent.Core.Agent;
using Microsoft.Extensions.FileProviders;

/// <summary>
/// Puls, the South African general assistant (CL15, ADR 0027): its modes (<c>agent/puls/</c>), its specialists
/// (<c>specialists/puls-*</c>) and its minion roles (<c>minions/puls-*</c>), and the packs it uses by name. Loaded by a host
/// with <c>IDENTITIES=puls</c> (the Puls compose sets it in both agent containers); the code-review stack never loads it.
/// </summary>
public sealed class PulsIdentity : IAgentIdentity
{
    public string Name => "puls";

    /// <summary>Puls's prompts: none of its own until step 3 moves them here from core's <c>Prompts/</c>.</summary>
    public IFileProvider Prompts { get; } = new NullFileProvider();

    /// <summary>Step 4 names <c>shops</c>; F35 adds <c>recipes</c>, CL16 <c>cooking</c>, later <c>places</c>.</summary>
    public IReadOnlyList<string> Packs => [];
}
