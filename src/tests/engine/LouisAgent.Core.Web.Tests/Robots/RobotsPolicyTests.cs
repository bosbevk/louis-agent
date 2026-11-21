using LouisAgent.Composition.Web.Messages;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Web.Messages;
using LouisAgent.Core.Web.Robots;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Core.Web.Tests.Robots;

/// <summary>
/// The policy that remembers each site's robots.txt: one fetch per site per day, an hour for a file we couldn't read,
/// and the verdict for a path. Written first: <c>RobotsPolicy</c> threw NotImplementedException on the red run.
/// </summary>
public class RobotsPolicyTests
{
    private const string Blocked = "User-agent: *\nDisallow: /private/\nDisallow: /*?s=\n";

    /// <summary>A stand-in for the network: answers with a file, remembers which addresses it was asked for.</summary>
    private sealed class FakeRobots(Func<RobotsFile> answer)
    {
        public List<Uri> Calls { get; } = [];

        /// <summary>Counts the call and hands back the file.</summary>
        public Task<RobotsFile> FetchAsync(Uri url, CancellationToken cancellationToken)
        {
            Calls.Add(url);
            return Task.FromResult(answer());
        }
    }

    /// <summary>A policy on a fake network and a clock that only moves when the test says so.</summary>
    private static (RobotsPolicy Policy, FakeRobots Network, FakeTimeProvider Clock) Build(RobotsFile file)
    {
        var network = new FakeRobots(() => file);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 8, 0, 0, TimeSpan.Zero));
        return (new RobotsPolicy(network.FetchAsync, clock), network, clock);
    }

    /// <summary>Two checks of one site fetch once; a day later it is read again; two sites are two fetches.</summary>
    [Test]
    public async Task ASitesFileIsFetchedOnce_UntilADayHasPassed()
    {
        var (policy, network, clock) = Build(new RobotsFile(200, Blocked));

        await policy.CheckAsync(new Uri("https://a.com/one"));
        await policy.CheckAsync(new Uri("https://a.com/two"));
        Assert.That(network.Calls, Has.Count.EqualTo(1));
        Assert.That(network.Calls[0], Is.EqualTo(new Uri("https://a.com/robots.txt")));

        clock.Advance(TimeSpan.FromHours(23));
        await policy.CheckAsync(new Uri("https://a.com/three"));
        Assert.That(network.Calls, Has.Count.EqualTo(1), "23 hours later it is still the same reading");

        clock.Advance(TimeSpan.FromHours(1));
        await policy.CheckAsync(new Uri("https://a.com/four"));
        Assert.That(network.Calls, Has.Count.EqualTo(2), "a day later it is read again");
    }

    /// <summary>
    /// The site is the scheme, host and port, so http and https, or two hosts, are each their own fetch.
    /// </summary>
    [Test]
    public async Task EachSchemeHostAndPort_IsItsOwnSite()
    {
        var (policy, network, _) = Build(new RobotsFile(404, null));

        await policy.CheckAsync(new Uri("https://a.com/x"));
        await policy.CheckAsync(new Uri("http://a.com/x"));
        await policy.CheckAsync(new Uri("https://b.com/x"));
        await policy.CheckAsync(new Uri("https://a.com:8443/x"));

        Assert.That(network.Calls.Select(u => u.AbsoluteUri),
            Is.EqualTo(new[]
            {
                "https://a.com/robots.txt", "http://a.com/robots.txt", "https://b.com/robots.txt",
                "https://a.com:8443/robots.txt",
            }));
    }

    /// <summary>Checks that arrive at the same moment share one fetch, not one each.</summary>
    [Test]
    public async Task ChecksAtTheSameTime_ShareOneFetch()
    {
        var gate = new TaskCompletionSource<RobotsFile>();
        int calls = 0;
        var policy = new RobotsPolicy((_, _) =>
        {
            Interlocked.Increment(ref calls);
            return gate.Task;
        }, new FakeTimeProvider());

        var checks = Enumerable.Range(0, 5).Select(i => policy.CheckAsync(new Uri($"https://a.com/p{i}"))).ToList();
        gate.SetResult(new RobotsFile(200, Blocked));
        await Task.WhenAll(checks);

        Assert.That(calls, Is.EqualTo(1));
    }

    /// <summary>A site with no robots.txt (404) lets us read everything.</summary>
    [Test]
    public async Task ANotFoundFile_AllowsEveryPath()
    {
        var (policy, _, _) = Build(new RobotsFile(404, null));

        Assert.That(await policy.CheckAsync(new Uri("https://a.com/private/x")), Is.EqualTo(RobotsVerdict.Allowed));
        Assert.That(await policy.CheckAsync(new Uri("https://a.com/")), Is.EqualTo(RobotsVerdict.Allowed));
    }

    /// <summary>
    /// A file that blocks a path gives Disallowed for it and Allowed for another; the query counts too.
    /// </summary>
    [Test]
    public async Task AGoodFile_DisallowsABlockedPath_AndAllowsAnother_ByPathAndQuery()
    {
        var (policy, _, _) = Build(new RobotsFile(200, Blocked));

        Assert.That(await policy.CheckAsync(new Uri("https://a.com/private/x")), Is.EqualTo(RobotsVerdict.Disallowed));
        Assert.That(await policy.CheckAsync(new Uri("https://a.com/recipes/")), Is.EqualTo(RobotsVerdict.Allowed));
        Assert.That(await policy.CheckAsync(new Uri("https://a.com/page?s=bobotie")),
            Is.EqualTo(RobotsVerdict.Disallowed));
        Assert.That(await policy.CheckAsync(new Uri("https://a.com/page")), Is.EqualTo(RobotsVerdict.Allowed));
    }

    /// <summary>A server error, or no answer at all, means we can't tell, and we say so rather than guess.</summary>
    [TestCase(503)]
    [TestCase(null)]
    public async Task AFileWeCouldntRead_IsUnreachable(int? status)
    {
        var (policy, _, _) = Build(new RobotsFile(status, null));

        Assert.That(await policy.CheckAsync(new Uri("https://a.com/x")), Is.EqualTo(RobotsVerdict.Unreachable));
    }

    /// <summary>An unreachable file is remembered for an hour, not a day, and then tried again (finding G1).</summary>
    [Test]
    public async Task AnUnreachableFile_IsKeptAnHour_ThenFetchedAgain()
    {
        var answers = new Queue<RobotsFile>([new RobotsFile(503, null), new RobotsFile(200, Blocked)]);
        var network = new FakeRobots(answers.Dequeue);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 8, 0, 0, TimeSpan.Zero));
        var policy = new RobotsPolicy(network.FetchAsync, clock);
        var page = new Uri("https://a.com/recipes/");

        Assert.That(await policy.CheckAsync(page), Is.EqualTo(RobotsVerdict.Unreachable));
        clock.Advance(TimeSpan.FromMinutes(59));
        Assert.That(await policy.CheckAsync(page), Is.EqualTo(RobotsVerdict.Unreachable));
        Assert.That(network.Calls, Has.Count.EqualTo(1), "59 minutes later it is not asked again");

        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.That(await policy.CheckAsync(page), Is.EqualTo(RobotsVerdict.Allowed));
        Assert.That(network.Calls, Has.Count.EqualTo(2), "an hour later the file is read again");
    }

    /// <summary>The robots.txt itself is always allowed, and checking it fetches nothing (no loop).</summary>
    [Test]
    public async Task TheRobotsFileItself_IsAllowedWithoutAFetch()
    {
        var (policy, network, _) = Build(new RobotsFile(503, null));

        var verdict = await policy.CheckAsync(new Uri("https://a.com/robots.txt"));

        Assert.That(verdict, Is.EqualTo(RobotsVerdict.Allowed));
        Assert.That(network.Calls, Is.Empty);
    }

    /// <summary>The refusal for a blocked path names the host and the path, and an allowed path has none.</summary>
    [Test]
    public async Task TheRefusalText_NamesTheHostAndThePath()
    {
        var (policy, _, _) = Build(new RobotsFile(200, Blocked));

        string? blocked = await policy.RefusalAsync(new Uri("https://a.com/private/x?y=1"));
        string? allowed = await policy.RefusalAsync(new Uri("https://a.com/recipes/"));

        Assert.That(blocked, Is.EqualTo(MessageText.Format(WebToolMessage.RobotsDisallows,
            new
            {
                Host = "a.com",
                Path = "/private/x?y=1"
            })));
        Assert.That(allowed, Is.Null);
    }

    /// <summary>The refusal for an unreachable file says why the file couldn't be read.</summary>
    [Test]
    public async Task TheUnreachableText_NamesTheHostAndTheReason()
    {
        var (policy, _, _) = Build(new RobotsFile(503, null));

        string? refusal = await policy.RefusalAsync(new Uri("https://a.com/x"));

        Assert.That(refusal, Is.EqualTo(MessageText.Format(WebToolMessage.RobotsUnreachable,
            new
            {
                Host = "a.com",
                Reason = "503"
            })));
    }

    /// <summary>A page's own signal is refused with the host and the signal's name.</summary>
    [Test]
    public void APageSignal_IsRefusedWithTheHostAndTheSignal()
    {
        var (policy, _, _) = Build(new RobotsFile(404, null));

        string? refusal = policy.PageRefusal(new Uri("https://a.com/x"), null,
            "<meta name=\"robots\" content=\"noai\">");

        Assert.That(refusal, Is.EqualTo(MessageText.Format(WebToolMessage.PageRefusesAiReaders,
            new
            {
                Host = "a.com",
                Signal = "meta robots noai"
            })));
        Assert.That(policy.PageRefusal(new Uri("https://a.com/x"), null, "<p>plain</p>"), Is.Null);
    }
}
