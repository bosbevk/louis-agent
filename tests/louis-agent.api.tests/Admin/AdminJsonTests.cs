using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;

namespace LouisAgent.Api.Tests.Admin;

/// <summary>
/// Pins the names the admin replies are written with: snake_case from the C# names, nothing else changed.
/// </summary>
public class AdminJsonTests
{
    /// <summary>
    /// A record's names come out as the portal reads them, including the ones with a single capital at the end.
    /// </summary>
    [Test]
    public void TheNames_AreWrittenInSnakeCase()
    {
        var row = new Sample(1, 2, 3, 4);

        string json = JsonSerializer.Serialize(row, AdminJson.SnakeCase);

        Assert.That(json, Is.EqualTo("""{"pass_at_k":1,"seconds_per_turn":2,"cost_per_answer":3,"k":4}"""));
    }

    /// <summary>A nothing-known value is written as null, as the replies have always sent it.</summary>
    [Test]
    public void ANullValue_IsWrittenAsNull()
    {
        string json = JsonSerializer.Serialize(new Nothing(null), AdminJson.SnakeCase);

        Assert.That(json, Is.EqualTo("""{"error":null}"""));
    }

    /// <summary>A stand-in reply with the awkward names.</summary>
    private sealed record Sample(int PassAtK, int SecondsPerTurn, int CostPerAnswer, int K);

    /// <summary>A stand-in reply with a missing value.</summary>
    private sealed record Nothing(string? Error);
}
