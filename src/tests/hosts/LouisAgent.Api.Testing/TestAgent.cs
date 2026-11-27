using LouisAgent.Composition.Packs;
using LouisAgent.Core.Prompting;
using LouisAgent.Core.Prompting.Roles;

namespace LouisAgent.Api.Testing;

/// <summary>
/// The agent the API's tests run on (F166.1): a made-up identity with the shape a real one has, its voices, a
/// subagent, a reader and its lookup agents, on fake packs under the web's and the calculator's names. It replaces
/// Puls, so the API is proved with no product and no real pack in it.
/// </summary>
/// <remarks>
/// Louis's pick A of three: a test agent of the framework's own fakes, over running the tests on a site's identity or
/// leaving them with Puls. It is a <see cref="FakeIdentity"/> with its prompts in memory, each role file carrying the
/// header the engine reads (its level and, for the subagent, its toolsets), so a session can open it as it opens a
/// real identity's.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0076-core-ships-no-agent.md">ADR 0076: core ships no agent</seealso>
public static class TestAgent
{
    /// <summary>The identity's name, as a session names it.</summary>
    public const string Name = "testagent";

    /// <summary>
    /// The voice a session opens with, a language voice (<c>{identity}/voice-{xx}</c>), which also loads the shared
    /// <c>voice.md</c> that routes, as a real identity's language voice does.
    /// </summary>
    public const string Voice = Name + "/voice-en";

    /// <summary>A second language voice.</summary>
    public const string OtherVoice = Name + "/voice-de";

    /// <summary>The heading of the shared voice, which a top-level session gets and a sub-session doesn't.</summary>
    public const string RoutingHeading = "# Test agent: routing";

    /// <summary>The heading of the voice a session opens with, so a test can tell which voice it got.</summary>
    public const string VoiceHeading = "# Test agent: in English";

    /// <summary>The heading of the second voice.</summary>
    public const string OtherVoiceHeading = "# Test agent: in German";

    /// <summary>Its subagent, named by its address, whose role adds the web and the calculator.</summary>
    public const string Researcher = Name + ".researcher";

    /// <summary>A second subagent that adds no toolset of its own, working with what it is handed.</summary>
    public const string Reader = Name + ".reader";

    /// <summary>
    /// What every lookup agent of the test agent is, loaded before its own prompt: the engine finds it from the
    /// lookup agent's identity.
    /// </summary>
    public const string LookupBase = Name + "-lookup";

    /// <summary>The heading of a lookup agent's own prompt, so a test can tell it loaded.</summary>
    public const string LookupHeading = "# Test agent lookup agent: lookup";

    /// <summary>A lookup agent its subagent hands one small task and one tool, named by its address.</summary>
    public const string Lookup = Name + ".lookup";

    /// <summary>A second lookup agent, for a test that names two.</summary>
    public const string OtherLookup = Name + ".find";

    /// <summary>
    /// The packs it names: fakes under the web's and the calculator's real names, since the framework's tests reference
    /// no real pack (ADR 0076, Louis's pick 1 of three).
    /// </summary>
    public static IReadOnlyList<IAgentPack> Packs { get; } = [TestTools.WebPack, TestTools.CalculatorPack];

    /// <summary>The identity, its prompts in memory, asking its subagent and passing its answers on whole.</summary>
    public static FakeIdentity Identity { get; } = new(Name,
        packs: [.. Packs.Select(pack => pack.Name)],
        prompts: new Dictionary<string, string>
        {
            [$"agents/{Name}/voice.md"] = Prompt("voice", "agent", RoutingHeading, "How the test agent routes."),
            [$"agents/{Name}/voice-en.md"] = Prompt("voice-en", "agent", VoiceHeading, "The test agent in English."),
            [$"agents/{Name}/voice-de.md"] =
                Prompt("voice-de", "agent", OtherVoiceHeading, "The test agent in German."),
            [RoleCatalog.FileName(Researcher, RoleLevel.Subagent)] = Prompt("voice", "subagent",
                "# Test agent researcher", "The test agent's researcher.", "web, calculator"),
            [RoleCatalog.FileName(Reader, RoleLevel.Subagent)] = Prompt("voice", "subagent", "# Test agent reader",
                "Works with the toolsets it is handed."),
            [RoleCatalog.LookupBaseOf(Lookup)!] =
                Prompt("lookup", "lookup", "# Test agent lookup agent", "One task, one tool, one answer."),
            [RoleCatalog.FileName(Lookup, RoleLevel.Lookup)] =
                Prompt("voice", "lookup", LookupHeading, "Looks one thing up."),
            [RoleCatalog.FileName(OtherLookup, RoleLevel.Lookup)] =
                Prompt("voice", "lookup", "# Test agent lookup agent: find", "Finds one thing.")
        },
        voice: Voice,
        calls: [Researcher],
        relaysAnswersWhole: true);

    /// <summary>
    /// A prompt file with the header every prompt opens with, its name, what it is, its level and toolsets, then its
    /// heading and its words.
    /// </summary>
    private static string Prompt(string name, string level, string heading, string description,
        string? toolsets = null)
    {
        string tools = toolsets is null ? "" : $"\n  toolsets: {toolsets}";
        string header = $"---\nname: {name}\ndescription: {description}\nmetadata:\n  level: {level}{tools}\n---\n";
        return $"{header}{heading}\n\n{description}\n";
    }
}
