using LouisAgent.Composition.Packs;
using LouisAgent.Packs.Calculator;
using LouisAgent.Packs.Web;

namespace LouisAgent.Api.Testing;

/// <summary>
/// The agent the API's tests run on (F166.1): a made-up identity with the shape a real one has, two voices, a
/// specialist and two minions, on the framework's own web and calculator packs. It replaces Puls, so the API is proved
/// with no product in it.
/// </summary>
/// <remarks>
/// Louis's pick A of three: a test agent of the framework's own fakes, over running the tests on a site's identity or
/// leaving them with Puls. It is a <see cref="FakeIdentity"/> with its prompts in memory, each role file carrying the
/// header the engine reads (its level and, for the specialist, its toolsets), so a session can open it as it opens a
/// real identity's.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0076-core-ships-no-agent.md">ADR 0076: core ships no agent</seealso>
public static class TestAgent
{
    /// <summary>The identity's name, as a session names it.</summary>
    public const string Name = "testagent";

    /// <summary>The voice a session opens with.</summary>
    public const string Voice = Name + "/voice";

    /// <summary>A second voice, as an identity offers one per language.</summary>
    public const string OtherVoice = Name + "/voice-other";

    /// <summary>Its specialist, on the web and the calculator.</summary>
    public const string Specialist = Name + "-specialist";

    /// <summary>A minion its specialist hands one small task to.</summary>
    public const string Minion = Name + "-minion";

    /// <summary>A second minion, for a test that names two.</summary>
    public const string OtherMinion = Name + "-minion-other";

    /// <summary>The packs it names: the web and the calculator, which both stay the framework's.</summary>
    public static IReadOnlyList<IAgentPack> Packs { get; } = [new WebPack(), new CalculatorPack()];

    /// <summary>The identity, its prompts in memory, asking its specialist and passing its answers on whole.</summary>
    public static FakeIdentity Identity { get; } = new(Name,
        packs: [.. Packs.Select(pack => pack.Name)],
        prompts: new Dictionary<string, string>
        {
            [$"agent/{Name}/voice.md"] = Prompt("voice", "agent", "The test agent's voice."),
            [$"agent/{Name}/voice-other.md"] = Prompt("voice-other", "agent", "The test agent's second voice."),
            [$"specialists/{Specialist}.md"] = Prompt(Specialist, "specialist", "The test agent's specialist.",
                "web, calculator"),
            [$"minions/{Minion}.md"] = Prompt(Minion, "minion", "A minion with one small task."),
            [$"minions/{OtherMinion}.md"] = Prompt(OtherMinion, "minion", "A second minion with one small task.")
        },
        voice: Voice,
        calls: [Specialist],
        relaysAnswersWhole: true);

    /// <summary>
    /// A prompt file with the header every prompt opens with: its name, what it is, its level and toolsets.
    /// </summary>
    private static string Prompt(string name, string level, string description, string? toolsets = null)
    {
        string tools = toolsets is null ? "" : $"\n  toolsets: {toolsets}";
        string header = $"---\nname: {name}\ndescription: {description}\nmetadata:\n  level: {level}{tools}\n---\n";
        return $"{header}# {name}\n\n{description}\n";
    }
}
