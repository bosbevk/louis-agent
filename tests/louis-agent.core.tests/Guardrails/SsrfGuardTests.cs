using System.Net;
using LouisAgent.Core.Guardrails;

namespace LouisAgent.Core.Tests.Guardrails;

// Which addresses FetchUrl may reach (the guard's end-to-end behaviour, redirects included, is in WebToolsTests).
public class SsrfGuardTests
{
    [TestCase("8.8.8.8", true)]
    [TestCase("2606:4700:4700::1111", true)]
    [TestCase("10.1.2.3", false)]
    [TestCase("172.20.0.1", false)]
    [TestCase("192.168.0.1", false)]
    [TestCase("100.64.0.1", false)]
    [TestCase("169.254.169.254", false)]
    [TestCase("0.0.0.0", false)]
    [TestCase("224.0.0.1", false)]
    [TestCase("::1", false)]
    [TestCase("fe80::1", false)]
    [TestCase("fd00::1", false)]
    [TestCase("::ffff:127.0.0.1", false)]
    public void IsPublicAddress_ClassifiesRanges(string address, bool expected) =>
        Assert.That(SsrfGuard.IsPublicAddress(IPAddress.Parse(address)), Is.EqualTo(expected));

    [TestCase("http://localhost/x")]
    [TestCase("http://127.0.0.1/x")]
    [TestCase("http://[::1]/x")]
    [TestCase("http://169.254.169.254/latest/meta-data")]
    public void EnsurePublicHost_RefusesLocalAndPrivateHosts(string url)
    {
        Assert.That(() => SsrfGuard.EnsurePublicHostAsync(new Uri(url), (_, _) => Task.FromResult(Array.Empty<IPAddress>())),
            Throws.InstanceOf<BlockedAddressException>());
    }

    [Test]
    public void EnsurePublicHost_RefusesANameThatResolvesToAPrivateAddress()
    {
        Assert.That(() => SsrfGuard.EnsurePublicHostAsync(new Uri("https://internal.example/"),
                (_, _) => Task.FromResult(new[] { IPAddress.Parse("10.0.0.5") })),
            Throws.InstanceOf<BlockedAddressException>().With.Message.Contain("10.0.0.5"));
    }

    [Test]
    public void EnsurePublicHost_AllowsAPublicName()
    {
        Assert.That(() => SsrfGuard.EnsurePublicHostAsync(new Uri("https://example.com/"),
            (_, _) => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") })), Throws.Nothing);
    }
}
