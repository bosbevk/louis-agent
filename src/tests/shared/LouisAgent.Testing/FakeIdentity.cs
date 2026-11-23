using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Sequences;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Testing;

/// <summary>
/// An identity for the catalogue's tests: a name, in-memory prompts, the packs it names, and the voice a session
/// that names it opens with (F109.3), none unless the test says. A test may hand it a provider another identity also
/// holds, as the showroom's three businesses share one (CL68.1).
/// </summary>
public sealed class FakeIdentity(
    string name = "fakeid",
    IReadOnlyList<string>? packs = null,
    IReadOnlyDictionary<string, string>? prompts = null,
    string? voice = null,
    bool demonstration = false,
    IReadOnlyList<string>? calls = null,
    bool relaysAnswersWhole = false,
    IFileProvider? provider = null,
    IReadOnlyList<AgentSequence>? sequences = null) : IAgentIdentity
{
    public string Name => name;

    public IFileProvider Prompts { get; } = provider ?? new FakePrompts(prompts ?? new Dictionary<string, string>
    {
        [$"agents/{name}/voice.md"] = $"# {name}\n\nA voice.\n"
    });

    public IReadOnlyList<string> Packs => packs ?? [];

    public string? DefaultVoice => voice;

    /// <summary>Whether this test identity plays a demonstration (F127.2).</summary>
    public bool IsDemonstration => demonstration;

    /// <summary>The agents this test identity may ask (F127.3.1), none unless the test says.</summary>
    public IReadOnlyList<string> Calls => calls ?? [];

    /// <summary>
    /// Whether an agent this test identity asks has its answer shown whole (F127.3.1), off unless said.
    /// </summary>
    public bool RelaysAnswersWhole => relaysAnswersWhole;

    /// <summary>The fixed orders of agents this test identity runs, none unless the test says.</summary>
    public IReadOnlyList<AgentSequence> Sequences => sequences ?? [];
}
