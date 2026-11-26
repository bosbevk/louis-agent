using LouisAgent.Api.Sessions.Opening;
using LouisAgent.Api.Sessions.Store;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Testing.Sessions;

/// <summary>
/// What every session store keeps (F43.1): an opening loads back as it was saved, its system prompt too, turns append
/// and load in order with the mark of their tools, an unknown chat loads as none, and a delete forgets a chat and its
/// turns. One contract, run on each store by its own
/// fixture: the in-memory one in the unit tests, MongoDB in the integration tests.
/// </summary>
/// <remarks>
/// A static class of checks, not an abstract fixture: the store's types are internal to the API, and a fixture NUnit
/// runs must be public, so each store's public fixture calls these with its own store.
/// </remarks>
internal static class SessionStoreContract
{
    /// <summary>
    /// When the chats in the checks open: a fixed time, so a read-back can be compared to the second.
    /// </summary>
    private static readonly DateTimeOffset Opened = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The system prompt the chats in the checks opened with.</summary>
    private const string Prompt = "You plan dinners.";

    /// <summary>The mark of the tools the turns in the checks were sent with.</summary>
    private static readonly ToolsFingerprint Tools = new("3f9c0a1b");

    /// <summary>
    /// A chat that asked for a toolset, an identity, a depth and a place, as a real opening does, with its prompt.
    /// </summary>
    private static StoredSession Opening(string id) =>
        new(id, new SessionRequest(["files"], Depth: 1, Identity: "testagent",
            Place: new UserPlace("Cape Town", "Africa/Johannesburg")), Opened, Prompt);

    /// <summary>
    /// A turn that added a question and its answer, sent one text attachment, and was sent with the checks' tools.
    /// </summary>
    private static StoredTurn Turn(int number, string question) =>
        new(number, Opened.AddMinutes(number),
            [new ChatMessage(ChatRole.User, question), new ChatMessage(ChatRole.Assistant, $"answer {number}")],
            new Dictionary<string, string> { [$"note{number}.txt"] = $"note {number}" }, Tools);

    /// <summary>A saved opening loads back as it was saved, with no turns yet.</summary>
    public static async Task AnOpening_LoadsBackAsItWasSaved(ISessionStore store)
    {
        await store.SaveOpeningAsync(Opening("sess_a"));

        var saved = await store.LoadAsync("sess_a");

        Assert.That(saved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.Session.Id, Is.EqualTo("sess_a"));
            Assert.That(saved.Session.Opening.Toolsets, Is.EqualTo(new[] { "files" }));
            Assert.That(saved.Session.Opening.Identity, Is.EqualTo("testagent"));
            Assert.That(saved.Session.Opening.Depth, Is.EqualTo(1));
            Assert.That(saved.Session.Opening.Place, Is.EqualTo(new UserPlace("Cape Town", "Africa/Johannesburg")));
            Assert.That(saved.Session.OpenedAt, Is.EqualTo(Opened));
            Assert.That(saved.Session.SystemPrompt, Is.EqualTo(Prompt));
            Assert.That(saved.Turns, Is.Empty);
            Assert.That(saved.LastActivity, Is.EqualTo(Opened));
        });
    }

    /// <summary>
    /// Turns append and load in order, each with its messages and attachments; the last is the activity.
    /// </summary>
    public static async Task Turns_AppendAndLoadInOrder(ISessionStore store)
    {
        await store.SaveOpeningAsync(Opening("sess_b"));
        await store.AppendTurnAsync("sess_b", Turn(1, "first"));
        await store.AppendTurnAsync("sess_b", Turn(2, "second"));

        var saved = (await store.LoadAsync("sess_b"))!;

        Assert.Multiple(() =>
        {
            Assert.That(saved.Turns.Select(turn => turn.Number), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(saved.Turns[1].Messages.Select(message => message.Text),
                Is.EqualTo(new[] { "second", "answer 2" }));
            Assert.That(saved.Turns[1].Messages[1].Role, Is.EqualTo(ChatRole.Assistant));
            Assert.That(saved.Turns[0].Attachments["note1.txt"], Is.EqualTo("note 1"));
            Assert.That(saved.Turns[0].Tools, Is.EqualTo(Tools));
            Assert.That(saved.LastActivity, Is.EqualTo(Opened.AddMinutes(2)));
        });
    }

    /// <summary>A chat the store never saw loads as none.</summary>
    public static async Task AnUnknownChat_LoadsAsNone(ISessionStore store) =>
        Assert.That(await store.LoadAsync("sess_unknown"), Is.Null);

    /// <summary>
    /// A turn is written once: a second turn under a number the chat already keeps is refused, and the first stays
    /// (FX256: MongoDB refuses it by its id, so every store does).
    /// </summary>
    public static async Task ATurnNumberAlreadyKept_IsRefused(ISessionStore store)
    {
        await store.SaveOpeningAsync(Opening("sess_d"));
        await store.AppendTurnAsync("sess_d", Turn(1, "first"));

        Assert.That(async () => await store.AppendTurnAsync("sess_d", Turn(1, "again")), Throws.Exception);
        var saved = (await store.LoadAsync("sess_d"))!;
        Assert.That(saved.Turns.Select(turn => turn.Messages[0].Text), Is.EqualTo(new[] { "first" }));
    }

    /// <summary>A delete forgets the chat and its turns, and deleting one the store never had is no error.</summary>
    public static async Task ADelete_ForgetsTheChatAndItsTurns(ISessionStore store)
    {
        await store.SaveOpeningAsync(Opening("sess_c"));
        await store.AppendTurnAsync("sess_c", Turn(1, "first"));

        await store.DeleteAsync("sess_c");

        Assert.Multiple(async () =>
        {
            Assert.That(await store.LoadAsync("sess_c"), Is.Null);
            Assert.That(async () => await store.DeleteAsync("sess_never"), Throws.Nothing);
        });
    }
}
