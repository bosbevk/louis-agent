using LouisAgent.Choices;

namespace LouisAgent.Api.Tests;

/// <summary>
/// A business another agent asks in its own voice (F127.3.1 step 6), as the site's guide asks a showroom business:
/// a voice of its own (<c>agent/acme/voice.md</c>) and one pack. Its prompts are registered with the test agent's
/// before the first read (<see cref="ApiTestPrompts"/>), since the prompt source is frozen after that.
/// </summary>
internal static class TestBusiness
{
    /// <summary>The business: acme, in its voice, with the fake calculator.</summary>
    public static readonly FakeIdentity Identity =
        new("acme", packs: [TestToolset.Calculator.Wire()], voice: "acme/voice");
}
