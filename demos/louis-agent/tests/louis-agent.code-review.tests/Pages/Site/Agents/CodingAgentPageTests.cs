using LouisAgent.CodeReview.Pages.Site.Agents;
using LouisAgent.SessionStream;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Tests.Pages.Site.Agents;

/// <summary>
/// The coding agent's page (F120, Louis's pick "Watch it work"): a visitor watches a recorded session of the agent
/// fixing a real bug, played in the framework's chat view, then gets the offer. The recording is a file of the site's,
/// so these tests hold the page, the file and the script that made it to each other.
/// </summary>
public class CodingAgentPageTests
{
    private static string Recording => Path.Combine(TestPaths.RepoRoot,
        "demos/louis-agent/web/louis-agent.code-review/wwwroot/recordings/coding-session.sse");

    private static string Script => Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/recordings/record.sh");

    /// <summary>The page is at its address, opens with its heading and plays the recording the site serves.</summary>
    [Test]
    public void ThePage_PlaysTheRecording()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<CodingAgentPage>();

        var route = typeof(CodingAgentPage).GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>()
            .Single().Template;
        Assert.Multiple(() =>
        {
            Assert.That(route, Is.EqualTo("/coding-agent"));
            Assert.That(cut.Find("h2.chrome").TextContent, Is.EqualTo("Watch it fix a real bug."));
            Assert.That(cut.FindAll(".recorded-chat"), Has.Count.EqualTo(1));
            Assert.That(cut.Markup, Does.Not.Contain(";"), "no semicolons in the site's words");
        });
    }

    /// <summary>
    /// The page is one slide, side by side (FX105, Louis's pick of three): the words and what the visitor is watching
    /// on the left, read while the session plays in the window on the right.
    /// </summary>
    [Test]
    public void ThePage_IsOneSlide_TheWordsBesideTheWindow()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<CodingAgentPage>();

        var columns = cut.Find(".band .watch-grid").Children;
        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".band"), Has.Count.EqualTo(1), "one slide");
            Assert.That(columns, Has.Length.EqualTo(2));
            Assert.That(columns[0].QuerySelector("h2.chrome"), Is.Not.Null, "the heading on the left");
            Assert.That(columns[0].QuerySelectorAll(".rules li"), Has.Length.EqualTo(4), "what you're watching");
            Assert.That(columns[1].ClassList, Does.Contain("recorded-chat"), "the window on the right");
        });
    }

    /// <summary>
    /// The recording is a whole session: it runs the tests, writes the fix, runs them again and ends its turn, and it
    /// answers the question the page shows, which is the one the script asked.
    /// </summary>
    [Test]
    public async Task TheRecording_IsAWholeSession_OfThePagesQuestion()
    {
        var events = new List<SessionEvent>();
        await using (var stream = File.OpenRead(Recording))
            await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, CancellationToken.None))
                events.Add(streamEvent);
        var tools = events.OfType<ContentBlockStarted>()
            .Select(start => start.Data.GetProperty("content_block"))
            .Where(block => block.GetProperty("type").GetString() == "tool_use")
            .Select(block => block.GetProperty("name").GetString())
            .ToList();
        string asked = File.ReadAllLines(Script).Single(line => line.StartsWith("question=")).Split('"')[1];

        Assert.Multiple(() =>
        {
            Assert.That(tools.Count(tool => tool == "DotNetTest"), Is.EqualTo(2), "the tests, before and after");
            Assert.That(tools, Does.Contain("WriteWorkspaceFile"), "the fix");
            Assert.That(events.OfType<MessageDelta>().Last().Data.GetProperty("delta").GetProperty("stop_reason")
                .GetString(), Is.EqualTo("end_turn"));
            Assert.That(asked, Is.EqualTo(CodingAgentPage.Question), "the page shows the question the script asked");
        });
    }
}
