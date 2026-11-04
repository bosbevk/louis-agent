using System.Net.Http.Json;
using LouisAgent.Puls.Admin.Starting;

namespace LouisAgent.Puls.Tests.Admin.Starting;

/// <summary>
/// What the page posts to start a run is what the API reads: the repeats, under "repeat", and for a replay the run
/// to play back, under "replay" (FX28); a plain run leaves it off the wire.
/// </summary>
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

    /// <summary>A replay's request names the recording it plays back, as the API's start route reads it.</summary>
    [Test]
    public async Task AReplay_IsPostedWithTheRunItReplays()
    {
        var body = StartRunBody.Of(new StartRequest(SampleEval.Beta, 1, Replay: "dc272b95eaeb"));

        string json = await JsonContent.Create(body).ReadAsStringAsync();

        Assert.That(json, Is.EqualTo("""{"repeat":1,"replay":"dc272b95eaeb"}"""));
    }
}
