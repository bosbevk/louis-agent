using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Testing;
using LouisAgent.Choices;
using LouisAgent.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// F156, Louis's pick A of three: the API says with each turn whether the engine paced its words, as the header
/// <c>Stream-Paced: on</c>, before the first word, so a page knows to leave its own pacer off and the words are paced
/// once. With <c>STREAM_PACE</c> off there is no such header, and a page paces as it always has.
/// </summary>
public class StreamPacedTests
{
    /// <summary>A model that answers in a few words.</summary>
    private static FakeChatClient Answers() =>
        new(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "A few words.")));

    /// <summary>The header a turn's answer carries, opened with the engine pacing or not.</summary>
    private static async Task<string?> ReadPacedHeaderAsync(bool streamPace)
    {
        await using var app = ApiTestHost.Build(model: Answers(), streamPace: streamPace);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var opened =
            await client.PostAsJsonAsync("/v1/sessions", new { toolsets = new[] { TestToolset.Files.Wire() } });
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        string id = body.RootElement.GetProperty("session_id").GetString()!;
        using var answer = await client.PostAsJsonAsync($"/v1/sessions/{id}/messages", new { message = "Hi" });
        await answer.Content.ReadAsStringAsync();

        return answer.Headers.TryGetValues(ApiHeaders.StreamPaced, out var values) ? values.Single() : null;
    }

    /// <summary>With STREAM_PACE on, each turn's answer says it was paced.</summary>
    [Test]
    public async Task On_TheAnswerSaysItWasPaced() =>
        Assert.That(await ReadPacedHeaderAsync(streamPace: true), Is.EqualTo(ApiHeaders.StreamPacedOn));

    /// <summary>With it off, the answer says nothing of pacing.</summary>
    [Test]
    public async Task Off_TheAnswerSaysNothing() =>
        Assert.That(await ReadPacedHeaderAsync(streamPace: false), Is.Null);
}
