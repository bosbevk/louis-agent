using LouisAgent.Api.Sessions.Store;
using LouisAgent.Api.Testing.Sessions;

namespace LouisAgent.Api.Tests.Sessions.Store;

/// <summary>
/// The in-memory session store keeps the contract every store keeps (F43.1, <see cref="SessionStoreContract" />): the
/// store a test and a host with no database use.
/// </summary>
public class InMemorySessionStoreTests
{
    /// <summary>A saved opening loads back as it was saved.</summary>
    [Test]
    public Task AnOpening_LoadsBackAsItWasSaved() =>
        SessionStoreContract.AnOpening_LoadsBackAsItWasSaved(new InMemorySessionStore());

    /// <summary>Turns append and load in order.</summary>
    [Test]
    public Task Turns_AppendAndLoadInOrder() =>
        SessionStoreContract.Turns_AppendAndLoadInOrder(new InMemorySessionStore());

    /// <summary>A chat the store never saw loads as none.</summary>
    [Test]
    public Task AnUnknownChat_LoadsAsNone() =>
        SessionStoreContract.AnUnknownChat_LoadsAsNone(new InMemorySessionStore());

    /// <summary>A delete forgets the chat and its turns.</summary>
    [Test]
    public Task ADelete_ForgetsTheChatAndItsTurns() =>
        SessionStoreContract.ADelete_ForgetsTheChatAndItsTurns(new InMemorySessionStore());
}
