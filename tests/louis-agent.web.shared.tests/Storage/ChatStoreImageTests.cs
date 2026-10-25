using LouisAgent.SessionStream;
using System.Text.Json;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using Microsoft.JSInterop;
using static LouisAgent.Web.Shared.Tests.StreamEvents;

namespace LouisAgent.Web.Shared.Tests.Storage;

// FX22: the saved chat cuts a long tool result to 4 000 characters, in memory too; the image link beyond that is gone from the text, but the turn still
// renders the image, because the list was taken when the result arrived, and it round-trips through the store's JSON.
public class ChatStoreImageTests
{
    private sealed class CapturingJs : IJSRuntime
    {
        public string? Saved;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "localStorage.setItem" && args is [string key, string value] && key == "louis-agent.chats") Saved = value;
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
    }

    [Test]
    public async Task AnImageBeyondTheCut_IsStillInTheTurnsSet_AfterSave_AndAfterTheJsonRoundTrip()
    {
        const string Url = "https://example.co.za/wp-content/bobotie.jpg";
        string longPage = new string('x', 4_500) + " ![Bobotie](" + Url + ")";
        var turn = new Turn();
        TurnStream.Apply(turn, ToolUse("c1", "FetchUrl"));
        TurnStream.Apply(turn, ToolResult("c1", longPage));
        var js = new CapturingJs();
        var store = new ChatStore(js, new ChatSettings());
        store.Chats.Add(new Chat { SessionId = "s1", Turns = { turn } });

        await store.SaveAsync();

        Assert.That(turn.Blocks[0].ToolResult, Does.Contain("more chars not saved").And.Not.Contain(Url), "the text is cut");
        Assert.That(ImageSources.Of(turn), Does.Contain(Url), "in memory, after the cut");
        var loaded = JsonSerializer.Deserialize<List<Chat>>(js.Saved!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.That(ImageSources.Of(loaded[0].Turns[0]), Does.Contain(Url), "after the store's JSON");
    }
}
