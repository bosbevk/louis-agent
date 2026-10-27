using LouisAgent.Testing;
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
}
