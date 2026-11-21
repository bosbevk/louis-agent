using LouisAgent.Acp.Rider;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills.Providers;

namespace LouisAgent.Acp.Tests.Rider;

/// <summary>
/// Rider's discovery (CL57, out of core): it gives up quietly when Rider isn't there, finishes rather than hangs, and
/// stays off when the setting says so.
/// </summary>
public class RiderToolDiscoveryTests
{
    private static AgentEngine Engine() =>
        new(new CompositeSkillProvider(), new FakeChatClient(),
            new AgentOptions { WorkspaceRoot = Environment.CurrentDirectory }, supportsTools: true);

    private static RiderToolDiscovery Unreachable() =>
        new(new RiderOptions("http://localhost:99999", true, null));

    [Test]
    public void WhenRiderIsUnavailable_ItDoesNotThrow() =>
        Assert.DoesNotThrowAsync(() => Unreachable().DiscoverAndRegisterAsync(Engine()));

    [Test]
    public async Task WhenRiderIsUnavailable_ItFinishes()
    {
        var task = Unreachable().DiscoverAndRegisterAsync(Engine());

        Assert.That(await Task.WhenAny(task, Task.Delay(5000)), Is.SameAs(task));
    }

    [Test]
    public void Off_StartsNothing() =>
        Assert.DoesNotThrow(() => new RiderToolDiscovery(new RiderOptions("http://localhost:99999", false, null))
            .Start(Engine()));
}
