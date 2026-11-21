using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Door;

namespace LouisAgent.Web.Tests.Components.Widget.Door;

/// <summary>
/// FX134: each agent's window on the page says the page it sits in, so the guide's message carries it and the guide's
/// ask sends it on; a window that reads none is left out, and one let go is gone.
/// </summary>
public class AgentWindowsPagesTests
{
    /// <summary>The windows' pages are read when asked, each under the agent it shows.</summary>
    [Test]
    public async Task EachWindow_SaysThePageItSitsIn()
    {
        var windows = new AgentWindows();
        var page = new Page("https://acme.example/", "Acme", "Four rooms.", []);

        var acme = windows.Show("acme", () => Task.FromResult<Page?>(page));
        using var _ = windows.Show("blank");
        var pages = await windows.PagesAsync();
        acme.Dispose();

        Assert.Multiple(async () =>
        {
            Assert.That(pages.Select(p => (p.Identity, p.Page)), Is.EqualTo(new[] { ("acme", page) }));
            Assert.That(await windows.PagesAsync(), Is.Empty, "a window let go is gone");
        });
    }
}
