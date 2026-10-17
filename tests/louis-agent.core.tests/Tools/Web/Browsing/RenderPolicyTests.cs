using System.Net;
using LouisAgent.Core.Tools.Web.Browsing;
using PuppeteerSharp;

namespace LouisAgent.Core.Tests.Tools.Web.Browsing;

// F32 step 2: a browser follows redirects and loads scripts, data and frames on its own, so checking the first URL (as
// ReadRenderedPage does before it hands a page on) isn't enough. Every request the page makes goes through RenderPolicy:
// a public http(s) address is allowed, unless it's an image, a font or media, which the text doesn't need; anything
// internal (the Puls stack, the database, the cloud metadata address), any other scheme, or a host that doesn't
// resolve is refused. The same SsrfGuard as FetchUrl decides what's public.
public class RenderPolicyTests
{
    private static Task<IPAddress[]> Resolve(string host, CancellationToken _) => Task.FromResult(host switch
    {
        "www.pnp.co.za" => new[] { IPAddress.Parse("196.22.142.10") },
        "intranet.example" => [IPAddress.Parse("10.0.0.5")],
        "puls" => [IPAddress.Parse("172.23.0.2")],
        "puls-mongo" => [IPAddress.Parse("172.23.0.3")],
        "split.example" => [IPAddress.Parse("196.22.142.10"), IPAddress.Parse("192.168.1.1")],
        _ => [],
    });

    [TestCase("https://www.pnp.co.za/p/beef-mince", ResourceType.Document, RenderVerdict.Allow)]
    [TestCase("https://www.pnp.co.za/app.js", ResourceType.Script, RenderVerdict.Allow)]
    [TestCase("https://www.pnp.co.za/api/price?sku=1", ResourceType.Xhr, RenderVerdict.Allow)]
    [TestCase("https://www.pnp.co.za/api/price?sku=1", ResourceType.Fetch, RenderVerdict.Allow)]
    [TestCase("data:text/plain,hallo", ResourceType.Other, RenderVerdict.Allow)]
    public async Task APublicRequest_TheTextNeeds_IsAllowed(string url, ResourceType type, RenderVerdict expected) =>
        Assert.That(await RenderPolicy.DecideAsync(new Uri(url), type, Resolve), Is.EqualTo(expected));

    [TestCase("https://www.pnp.co.za/hero.jpg", ResourceType.Image)]
    [TestCase("https://www.pnp.co.za/montserrat.woff2", ResourceType.Font)]
    [TestCase("https://www.pnp.co.za/advert.mp4", ResourceType.Media)]
    public async Task ImagesFontsAndMedia_AreSkipped(string url, ResourceType type) =>
        Assert.That(await RenderPolicy.DecideAsync(new Uri(url), type, Resolve), Is.EqualTo(RenderVerdict.Skip));

    [TestCase("http://intranet.example/admin", ResourceType.Document)]
    [TestCase("http://puls:8080/sessions", ResourceType.Xhr)]
    [TestCase("mongodb://puls-mongo:27017/", ResourceType.Other)]
    [TestCase("http://puls-mongo:27017/", ResourceType.Fetch)]
    [TestCase("http://127.0.0.1:5070/admin/benchmarks", ResourceType.Xhr)]
    [TestCase("http://localhost/", ResourceType.Document)]
    [TestCase("http://[::1]/", ResourceType.Document)]
    [TestCase("http://169.254.169.254/latest/meta-data/", ResourceType.Xhr)]
    [TestCase("http://split.example/", ResourceType.Document)]
    [TestCase("http://10.0.0.5/logo.png", ResourceType.Image)]
    public async Task AnInternalAddress_IsRefused_WhateverItIs(string url, ResourceType type) =>
        Assert.That(await RenderPolicy.DecideAsync(new Uri(url), type, Resolve), Is.EqualTo(RenderVerdict.Refuse));

    [Test]
    public async Task AResolverThatThrows_IsARefusal_NotACrash()
    {
        // Review of PR #56: the real lookup (Dns.GetHostAddressesAsync) throws for a host that doesn't exist (a dead
        // tracker domain on a shop's page); it never returns []. Escaping the browser's async void request handler, the
        // exception would end the process, every open chat with it.
        static Task<IPAddress[]> NoSuchHost(string host, CancellationToken _) =>
            Task.FromException<IPAddress[]>(new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.HostNotFound));

        Assert.That(await RenderPolicy.DecideAsync(new Uri("https://dead-tracker.example/pixel.js"), ResourceType.Script, NoSuchHost),
            Is.EqualTo(RenderVerdict.Refuse));
    }

    [Test]
    public async Task AResolverThatThrowsAnything_IsARefusal() =>
        Assert.That(await RenderPolicy.DecideAsync(new Uri("https://www.pnp.co.za/"), ResourceType.Document,
            (_, _) => Task.FromException<IPAddress[]>(new InvalidOperationException("resolver broke"))), Is.EqualTo(RenderVerdict.Refuse));

    [TestCase("file:///etc/passwd")]
    [TestCase("ftp://www.pnp.co.za/prices.csv")]
    [TestCase("chrome://settings/")]
    [TestCase("https://nowhere.example/")]
    public async Task AnotherScheme_OrAHostThatDoesntResolve_IsRefused(string url) =>
        Assert.That(await RenderPolicy.DecideAsync(new Uri(url), ResourceType.Document, Resolve), Is.EqualTo(RenderVerdict.Refuse));
}
