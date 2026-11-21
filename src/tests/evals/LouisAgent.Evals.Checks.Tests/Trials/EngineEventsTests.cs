using System.Text.Json;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Checks.Tests.Trials;

/// <summary>
/// The engine's events, read by every check: here the one reader a pack's check needs of a tool's input, a string
/// field by name.
/// </summary>
public class EngineEventsTests
{
    /// <summary>A string field is read by name; a missing field, a field of another kind or a non-object is null.</summary>
    [TestCase("""{"url": "https://pnp.co.za/x"}""", "url", "https://pnp.co.za/x")]
    [TestCase("""{"url": 7}""", "url", null)]
    [TestCase("""{"shop": "pnp"}""", "url", null)]
    [TestCase("\"https://pnp.co.za/x\"", "url", null)]
    public void TextOf_ReadsAStringField_OrNull(string json, string name, string? expected)
    {
        var input = JsonDocument.Parse(json).RootElement.Clone();

        Assert.That(EngineEvents.TextOf(input, name), Is.EqualTo(expected));
    }
}
