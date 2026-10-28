using System.Net.Http.Json;
using LouisAgent.Puls.Admin.Starting;

namespace LouisAgent.Puls.Tests.Admin.Starting;

/// <summary>What the page posts to start a run is what the API reads: the repeats, under "repeat".</summary>
public class StartRunBodyTests
{
    /// <summary>A card's request becomes the body the API's start route reads.</summary>
    [Test]
    public async Task ARequest_IsPostedAsTheRepeats()
    {
        var body = StartRunBody.Of(new StartRequest(SampleEval.Beta, 3));

        string json = await JsonContent.Create(body).ReadAsStringAsync();

        Assert.That(json, Is.EqualTo("""{"repeat":3}"""));
    }
}
