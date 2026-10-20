using System.Text.Json;
using LouisAgent.Core.Tools.Agents.SubSessions;

namespace LouisAgent.Core.Tests.Tools.Agents.SubSessions;

// S (CL15): what a sub-session reported it used, from the API's usage object, and the line the tool result shows.
public class SubSessionUsageTests
{
    private static SubSessionUsage From(string json) => SubSessionUsage.From(JsonDocument.Parse(json).RootElement);

    [Test]
    public void TheFourTokenKinds_TheRequests_AndTheChargedCost()
    {
        var usage = From("""{"input_tokens":1000,"cache_creation_input_tokens":10,"cache_read_input_tokens":9000,"output_tokens":200,"requests":3,"cost":0.0123,"currency":"USD","display_cost":0.23}""");

        Assert.That(usage, Is.EqualTo(new SubSessionUsage(1000, 10, 9000, 200, 3, 0.0123m, "USD")));
        Assert.That(usage.Line(), Does.Contain("3 requests").And.Contain("10,210 tokens").And.Contain("0.0123 USD"));
    }

    [Test]
    public void APeerThatReportsNoCount_AndNoCost()
    {
        var usage = From("""{"input_tokens":100,"output_tokens":4}""");

        Assert.That(usage, Is.EqualTo(new SubSessionUsage(100, 0, 0, 4, null, null, null)));
        Assert.That(usage.Line(), Does.Contain("0 requests").And.Contain("104 tokens").And.Not.Contain("USD"));
    }
}
