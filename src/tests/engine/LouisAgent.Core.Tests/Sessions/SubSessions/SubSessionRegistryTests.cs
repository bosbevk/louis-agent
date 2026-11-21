using LouisAgent.Choices;
using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Tests.Sessions.SubSessions;

/// <summary>
/// A chat's open sub-sessions, by handle (F26): the registry only stores them, and closing one is the entry's own
/// delegate, so the HTTP side stays with the tool that opened it.
/// </summary>
public class SubSessionRegistryTests
{
    /// <summary>An entry on the test peer that notes in <paramref name="closed"/> when it is closed.</summary>
    private static OpenSubSession Entry(string sessionId, List<string> closed) =>
        new(new Uri("http://coder.test/"), sessionId, "coder", [TestToolset.Web.Wire()], SampleRole.Alpha, () =>
        {
            closed.Add(sessionId);
            return Task.CompletedTask;
        });

    /// <summary>Handles count up in a chat, each finds its own entry, and adding closes nothing.</summary>
    [Test]
    public async Task Handles_CountUpPerChat_AndFindTheirEntry()
    {
        var registry = new SubSessionRegistry();
        var closed = new List<string>();

        string first = await registry.AddAsync(Entry("sess_a", closed));
        string second = await registry.AddAsync(Entry("sess_b", closed));

        Assert.Multiple(() =>
        {
            Assert.That((first, second), Is.EqualTo(("sub-1", "sub-2")));
            Assert.That(registry.Find("sub-2")!.SessionId, Is.EqualTo("sess_b"));
            Assert.That(registry.Find("sub-9"), Is.Null);
            Assert.That(closed, Is.Empty);
        });
    }

    /// <summary>
    /// An entry still kept is swapped for its changed copy under the same handle; one no longer kept stays gone.
    /// </summary>
    [Test]
    public async Task Update_SwapsAKeptEntry_AndLeavesAGoneOneGone()
    {
        var registry = new SubSessionRegistry();
        string handle = await registry.AddAsync(Entry("sess_a", []));
        var work = new AgentWork([new SubSessionEvent(null, "coder", "message_stop", "{}")]);

        registry.Update(handle, open => open with { Work = work });
        registry.Update("sub-9", open => open with { Work = work });

        Assert.Multiple(() =>
        {
            Assert.That(registry.Find(handle)!.Work, Is.SameAs(work));
            Assert.That(registry.Find(handle)!.Handle, Is.EqualTo(handle));
            Assert.That(registry.Open, Has.Count.EqualTo(1));
        });
    }

    /// <summary>A chat never keeps more than the cap open on the peer: the oldest is closed.</summary>
    [Test]
    public async Task PastTheCap_TheOldestIsClosed()
    {
        var registry = new SubSessionRegistry(cap: 2);
        var closed = new List<string>();

        await registry.AddAsync(Entry("sess_a", closed));
        await registry.AddAsync(Entry("sess_b", closed));
        string third = await registry.AddAsync(Entry("sess_c", closed));

        Assert.Multiple(() =>
        {
            Assert.That(closed, Is.EqualTo(new[] { "sess_a" }));
            Assert.That(registry.Open.Select(o => o.Handle), Is.EqualTo(new[] { "sub-2", "sub-3" }));
            Assert.That(third, Is.EqualTo("sub-3"), "handles never reuse a closed one's number");
        });
    }

    /// <summary>When the chat is deleted or dropped as idle, nothing stays open on the peer.</summary>
    [Test]
    public async Task CloseAll_ClosesEveryOpenOne_AndEmptiesTheRegistry()
    {
        var registry = new SubSessionRegistry();
        var closed = new List<string>();
        await registry.AddAsync(Entry("sess_a", closed));
        await registry.AddAsync(Entry("sess_b", closed));

        await registry.CloseAllAsync();

        Assert.Multiple(() =>
        {
            Assert.That(closed, Is.EquivalentTo(new[] { "sess_a", "sess_b" }));
            Assert.That(registry.Open, Is.Empty);
        });
    }

    /// <summary>A peer that answered 404 (its idle drop) has nothing left to close, so it is only forgotten.</summary>
    [Test]
    public async Task Remove_ForgetsWithoutClosing()
    {
        var registry = new SubSessionRegistry();
        var closed = new List<string>();
        await registry.AddAsync(Entry("sess_a", closed));

        Assert.Multiple(() =>
        {
            Assert.That(registry.Remove("sub-1"), Is.True);
            Assert.That(registry.Remove("sub-1"), Is.False);
            Assert.That((registry.Open.Count, closed.Count), Is.EqualTo((0, 0)));
        });
    }

    /// <summary>No chat's registry is current until a host makes one so, and only while it is in use.</summary>
    [Test]
    public void NoneIsCurrent_UnlessAHostSetsOne()
    {
        Assert.That(SubSessionRegistry.Current, Is.Null);

        var registry = new SubSessionRegistry();
        using (SubSessionRegistry.Use(registry))
            Assert.That(SubSessionRegistry.Current, Is.SameAs(registry));
        Assert.That(SubSessionRegistry.Current, Is.Null);
    }
}
