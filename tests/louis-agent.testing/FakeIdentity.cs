using LouisAgent.Core.Agent;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Testing;

/// <summary>An identity for the catalogue's tests: a name, in-memory prompts and the packs it names.</summary>
public sealed class FakeIdentity(string name = "fakeid", IReadOnlyList<string>? packs = null, IReadOnlyDictionary<string, string>? prompts = null) : IAgentIdentity
{
    public string Name => name;
    public IFileProvider Prompts { get; } = new FakePrompts(prompts ?? new Dictionary<string, string> { [$"agent/{name}/voice.md"] = $"# {name}\n\nA voice.\n" });
    public IReadOnlyList<string> Packs => packs ?? [];
}
