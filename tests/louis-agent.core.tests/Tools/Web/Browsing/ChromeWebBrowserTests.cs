using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Core.Tests.Tools.Web.Browsing;

// F33 step 1: the guarded headless browser, out of F32's renderer so a shop's catalogue can use it too (and its guards
// stay in one place). Without a browser, a visit says the browser didn't answer, never hangs. With one (BROWSER_URL, the
// Puls stack's puls-browser on 127.0.0.1:9222), the live tests check the guards F32 built, and that a visit keeps the
// responses a caller asks for: the page's own JSON, which is how a shop's catalogue is read.
public class ChromeWebBrowserTests
{
    [Test]
    public void ABrowserThatDoesntAnswer_IsAWebBrowserException_SayingSo() =>
        Assert.That(() => new ChromeWebBrowser(new Uri("http://127.0.0.1:1")).VisitAsync(new Uri("https://example.com/")),
            Throws.TypeOf<WebBrowserException>().With.Message.Contains("127.0.0.1:1"));

    private static ChromeWebBrowser Live()
    {
        string? url = Environment.GetEnvironmentVariable("BROWSER_URL");
        Assume.That(url, Is.Not.Null.And.Not.Empty, "BROWSER_URL isn't set: no browser to visit with (the Puls stack's puls-browser).");
        return new ChromeWebBrowser(new Uri(url!));
    }

    private static Uri Page(string html) => new("data:text/html," + Uri.EscapeDataString(html));

    [Test, Category("Live")]
    public async Task AVisit_KeepsTheResponsesAskedFor_AndOnlyThose()
    {
        // The page fetches two JSON documents; the caller asks for one of them, as a catalogue asks for its search answer.
        var visit = await Live().VisitAsync(
            Page("<body>page</body><script>fetch('https://httpbin.org/json'); fetch('https://httpbin.org/uuid');</script>"),
            capture: url => url.AbsolutePath == "/json");

        Assert.That(visit.Captured, Has.Count.EqualTo(1));
        Assert.That(visit.Captured[0], Does.Contain("slideshow"));
        Assert.That(visit.Html, Does.Contain("page"));
    }

    [Test, Category("Live")]
    public async Task APagesWebSocket_IsBlocked()
    {
        // F32 step 4: request interception doesn't see WebSockets, and the browser shares a network with puls, so a page
        // could open one to an internal address. A public echo server stands in for any address, so the test tells
        // "blocked" from "couldn't connect anyway"; a slow request keeps the page open until the socket's outcome lands.
        var visit = await Live().VisitAsync(Page("<body>waiting</body><script>try { const w = new WebSocket('wss://echo.websocket.org');" +
            "w.onopen = () => document.body.textContent = 'socket-open'; w.onerror = () => document.body.textContent = 'socket-blocked'; }" +
            "catch { document.body.textContent = 'socket-blocked'; } fetch('https://httpbin.org/delay/3');</script>"));

        Assert.That(visit.Html, Does.Contain("socket-blocked").And.Not.Contain("socket-open"));
    }

    [Test, Category("Live")]
    public async Task AWebSocketFromAScriptMadeFrame_IsBlocked()
    {
        // Review of PR #56: a frame a script makes starts as about:blank; its window has a WebSocket of its own.
        var visit = await Live().VisitAsync(Page("<body>waiting</body><script>const f = document.createElement('iframe'); document.body.appendChild(f);" +
            "try { const w = new f.contentWindow.WebSocket('wss://echo.websocket.org');" +
            "w.onopen = () => document.body.textContent = 'socket-open'; w.onerror = () => document.body.textContent = 'socket-blocked'; }" +
            "catch { document.body.textContent = 'socket-blocked'; } fetch('https://httpbin.org/delay/3');</script>"));

        Assert.That(visit.Html, Does.Contain("socket-blocked").And.Not.Contain("socket-open"));
    }

    [Test, Category("Live")]
    public void ACancelledVisit_GivesUpAtOnce_NotAfterItsTimeouts()
    {
        // Review of PR #56: a cancelled turn kept one of the four browser pages for up to 40 s.
        var browser = Live();
        using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var watch = System.Diagnostics.Stopwatch.StartNew();

        Assert.That(() => browser.VisitAsync(new Uri("https://httpbin.org/delay/10"), cancellationToken: cancel.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
    }

    // F34 step 1a: a shop serves by area (Checkers keeps the area's store in cookies), so a visit can start in a session,
    // run one script of ours, and hand back the cookies the site set. Such a visit has a browser context of its own,
    // dropped after, so one user's area never reaches another's search.
    private static readonly BrowserSession Malmesbury = new([new BrowserCookie("f34area", "malmesbury", "httpbin.org")]);

    [Test, Category("Live")]
    public async Task AVisitInASession_StartsWithItsCookies()
    {
        var visit = await Live().VisitAsync(new Uri("https://httpbin.org/cookies"), session: Malmesbury);

        Assert.That(visit.Html, Does.Contain("malmesbury"));
    }

    [Test, Category("Live")]
    public async Task ASessionsCookies_StayInItsVisit()
    {
        var browser = Live();
        await browser.VisitAsync(new Uri("https://httpbin.org/cookies"), session: Malmesbury);

        var plain = await browser.VisitAsync(new Uri("https://httpbin.org/cookies"));
        var another = await browser.VisitAsync(new Uri("https://httpbin.org/cookies"),
            session: new([new BrowserCookie("f34area", "stellenbosch", "httpbin.org")]));

        Assert.That(plain.Html, Does.Not.Contain("malmesbury"), "a plain visit never sees a session's cookies");
        Assert.That(another.Html, Does.Contain("stellenbosch").And.Not.Contain("malmesbury"));
    }

    [Test, Category("Live")]
    public async Task TheCookiesASiteSets_AreReturned_FromASessionsVisit()
    {
        var visit = await Live().VisitAsync(new Uri("https://httpbin.org/cookies/set?f34store=655b0c8f"), session: new([]));

        Assert.That(visit.Cookies, Has.Some.Matches<BrowserCookie>(c => c.Name == "f34store" && c.Value == "655b0c8f"));
    }

    [Test, Category("Live")]
    public async Task AScript_RunsAfterTheLoad_AndItsResultIsReturned()
    {
        var visit = await Live().VisitAsync(Page("<body>loaded</body>"), script: "document.body.textContent + ' and scripted'");

        Assert.That(visit.ScriptResult, Is.EqualTo("loaded and scripted"));
    }

    [Test, Category("Live")]
    public async Task AScriptsRequests_AreHeldToTheAddressRule()
    {
        var visit = await Live().VisitAsync(Page("<body>x</body>"),
            script: "fetch('http://169.254.169.254/latest/meta-data/').then(() => 'reached', () => 'refused')");

        Assert.That(visit.ScriptResult, Is.EqualTo("refused"));
    }

    [Test, Category("Live")]
    public void AScriptThatFails_IsAWebBrowserException_WithItsReason()
    {
        var browser = Live();

        Assert.That(() => browser.VisitAsync(Page("<body>x</body>"), script: "(() => { throw new Error('no store here'); })()"),
            Throws.TypeOf<WebBrowserException>().With.Message.Contains("no store here"));
    }

    // F34, Louis on a slow cook ("what causes this long delay?"): Checkers' page never goes quiet (analytics), so every
    // search waited the full 10 s for silence after its answer had come. A visit that captures is done when it has its
    // answer and, when asked, the element that shows it is drawn: the page below polls forever, as Checkers' does.
    private static Uri NeverQuiet(string thenDraw) => Page("<body>loading</body><script>setInterval(() => fetch('https://httpbin.org/uuid'), 300);" +
        "fetch('https://httpbin.org/json'); setTimeout(() => { const d = document.createElement('div'); d.className = 'card'; " +
        "d.textContent = '" + thenDraw + "'; document.body.appendChild(d); }, 1500);</script>");

    [Test, Category("Live")]
    public async Task AVisitThatCaptures_EndsWhenItHasItsAnswer_NotWhenThePageGoesQuiet()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var visit = await Live().VisitAsync(NeverQuiet("drawn"), capture: url => url.AbsolutePath == "/json", readySelector: ".card");

        Assert.That(visit.Captured, Has.Count.EqualTo(1));
        Assert.That(visit.Html, Does.Contain("drawn"), "it waited for the card it was told shows the answer");
        Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(6)), "not the 10 s a page that never goes quiet costs");
    }

    // The cook, 2026-10-21: of six renders fired at once, one never came back, and the cook waited on it for good (the
    // load and the settle have timeouts; connecting, opening the page and reading it have none). A visit has a budget
    // for the whole of it: past it, the page is closed and the visit fails with its reason, never hangs.
    [Test, Category("Live")]
    public void AVisitThatOutlivesItsBudget_IsAWebBrowserException_NotAHang()
    {
        string? url = Environment.GetEnvironmentVariable("BROWSER_URL");
        Assume.That(url, Is.Not.Null.And.Not.Empty, "BROWSER_URL isn't set: no browser to visit with (the Puls stack's puls-browser).");
        var browser = new ChromeWebBrowser(new Uri(url!), budget: TimeSpan.FromSeconds(2));
        var watch = System.Diagnostics.Stopwatch.StartNew();

        Assert.That(() => browser.VisitAsync(new Uri("https://httpbin.org/delay/10")),
            Throws.TypeOf<WebBrowserException>().With.Message.Contains("within 2 s"));
        Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(6)));
    }

    [Test, Category("Live")]
    public void ARedirectToAnInternalAddress_IsRefusedInsideTheBrowser()
    {
        var browser = Live();
        var redirect = new Uri("https://httpbin.org/redirect-to?url=http%3A%2F%2F169.254.169.254%2Flatest%2Fmeta-data%2F");

        Assert.That(() => browser.VisitAsync(redirect), Throws.TypeOf<WebBrowserException>().With.Message.Contains("169.254.169.254"));
    }
}
