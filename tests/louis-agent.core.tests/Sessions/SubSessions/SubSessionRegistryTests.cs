using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Tools.Agents;

namespace LouisAgent.Core.Tests.Sessions.SubSessions;

// F26 step 1: a chat's open sub-sessions, by handle. The registry is a store; closing is the entry's own delegate, so
// the HTTP side stays in SpecialistTools.
public class SubSessionRegistryTests
{
    private static OpenSubSession Entry(string sessionId, List<string> closed) =>
        new(new Uri("http://coder.test/"), sessionId, "coder", [CoreToolset.Web.Name()], "puls-cook", () => { closed.Add(sessionId); return Task.CompletedTask; });

    [Test]
    public async Task Handles_CountUpPerChat_AndFindTheirEntry()
    {
        var registry = new SubSessionRegistry();
        var closed = new List<string>();

        string first = await registry.AddAsync(Entry("sess_a", closed));
        string second = await registry.AddAsync(Entry("sess_b", closed));

        Assert.That((first, second), Is.EqualTo(("sub-1", "sub-2")));
        Assert.That(registry.Find("sub-2")!.SessionId, Is.EqualTo("sess_b"));
        Assert.That(registry.Find("sub-9"), Is.Null);
        Assert.That(closed, Is.Empty);
    }

    [Test]
    public async Task PastTheCap_TheOldestIsClosed()
    {
        // F26-S3: a chat never keeps more than the cap open on the peer.
        var registry = new SubSessionRegistry(cap: 2);
        var closed = new List<string>();

        await registry.AddAsync(Entry("sess_a", closed));
        await registry.AddAsync(Entry("sess_b", closed));
        string third = await registry.AddAsync(Entry("sess_c", closed));

        Assert.That(closed, Is.EqualTo(new[] { "sess_a" }));
        Assert.That(registry.Open.Select(o => o.Handle), Is.EqualTo(new[] { "sub-2", "sub-3" }));
        Assert.That(third, Is.EqualTo("sub-3"), "handles never reuse a closed one's number");
    }

    [Test]
    public async Task CloseAll_ClosesEveryOpenOne_AndEmptiesTheRegistry()
    {
        // The chat is deleted or dropped as idle: nothing stays open on the peer.
        var registry = new SubSessionRegistry();
        var closed = new List<string>();
        await registry.AddAsync(Entry("sess_a", closed));
        await registry.AddAsync(Entry("sess_b", closed));

        await registry.CloseAllAsync();

        Assert.That(closed, Is.EquivalentTo(new[] { "sess_a", "sess_b" }));
        Assert.That(registry.Open, Is.Empty);
    }

    [Test]
    public async Task Remove_ForgetsWithoutClosing()
    {
        // A peer that answered 404 (its idle drop) has nothing left to close.
        var registry = new SubSessionRegistry();
        var closed = new List<string>();
        await registry.AddAsync(Entry("sess_a", closed));

        Assert.That(registry.Remove("sub-1"), Is.True);
        Assert.That(registry.Remove("sub-1"), Is.False);
        Assert.That((registry.Open.Count, closed.Count), Is.EqualTo((0, 0)));
    }

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
