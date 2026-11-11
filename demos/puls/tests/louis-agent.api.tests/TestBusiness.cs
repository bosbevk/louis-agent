using LouisAgent.Choices;
using LouisAgent.Packs.Calculator;

namespace LouisAgent.Api.Tests;

/// <summary>
/// A business another agent asks in its own voice (F127.3.1 step 6), as the site's guide asks a showroom business:
/// a voice of its own (<c>agent/acme/voice.md</c>) and one pack. Its prompts are registered with Puls's before the
/// first read (<see cref="PulsPrompts"/>), since the prompt source is frozen after that.
/// </summary>
internal static class TestBusiness
{
    /// <summary>The business: acme, in its voice, with the calculator.</summary>
    public static readonly FakeIdentity Identity =
        new("acme", packs: [CalculatorToolset.Calculator.Wire()], voice: "acme/voice");
}
