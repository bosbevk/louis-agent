using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Catalog;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Testing;

/// <summary>
/// An identity for the catalogue's tests: a name, in-memory prompts, the packs it names, and the voice a session
/// that names it opens with (F109.3), none unless the test says.
/// </summary>
public sealed class FakeIdentity(
    string name = "fakeid",
    IReadOnlyList<string>? packs = null,
    IReadOnlyDictionary<string, string>? prompts = null,
    string? voice = null) : IAgentIdentity
{
    public string Name => name;

    public IFileProvider Prompts { get; } = new FakePrompts(prompts ?? new Dictionary<string, string>
    {
        [$"agent/{name}/voice.md"] = $"# {name}\n\nA voice.\n"
    });

    public IReadOnlyList<string> Packs => packs ?? [];

    public string? DefaultVoice => voice;
}
