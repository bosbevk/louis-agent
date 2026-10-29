using System.Text.Json;
using LouisAgent.Core.Tools.Agents.SubSessions;

namespace LouisAgent.Core.Tests.Tools.Agents.SubSessions;

/// <summary>
/// What a sub-session says it used, read from the API's usage object. It goes to the ledger and the turn, never into
/// the text the model reads (FX27).
/// </summary>
public class SubSessionUsageTests
{
    /// <summary>Reads a usage object written as JSON.</summary>
    private static SubSessionUsage From(string json) => SubSessionUsage.From(JsonDocument.Parse(json).RootElement);

    /// <summary>
    /// The four kinds of tokens, the requests and the charged cost come through; the converted cost doesn't.
    /// </summary>
    [Test]
    public void TheFourTokenKinds_TheRequests_AndTheChargedCost()
    {
        var usage = From(
            """{"input_tokens":1000,"cache_creation_input_tokens":10,"cache_read_input_tokens":9000,"output_tokens":200,"requests":3,"cost":0.0123,"currency":"USD","display_cost":0.23}""");

        Assert.That(usage, Is.EqualTo(new SubSessionUsage(1000, 10, 9000, 200, 3, 0.0123m, "USD")));
    }

    /// <summary>A peer that leaves out the count and the cost gives none of either, not zero.</summary>
    [Test]
    public void APeerThatReportsNoCount_AndNoCost()
    {
        var usage = From("""{"input_tokens":100,"output_tokens":4}""");

        Assert.That(usage, Is.EqualTo(new SubSessionUsage(100, 0, 0, 4, null, null, null)));
    }
}
