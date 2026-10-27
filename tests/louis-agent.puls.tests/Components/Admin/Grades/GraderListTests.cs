using System.Text.Json;
using LouisAgent.Puls.Components.Admin.Grades;

namespace LouisAgent.Puls.Tests.Components.Admin.Grades;

// F18 step 4.5c: the run's eval and graders, pinned to GraderList.graders.html and, for a run from before step 2a,
// GraderList.none.html (one line: the run recorded no graders). Written first: the goldens weren't there on the red run.
public class GraderListTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    [Test]
    public void WithGraders_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();
        var run = Json("""
                       {"run":{"id":"run_1","eval":"stub","eval_version":1,"commit":"abc1234",
                               "graders":[{"name":"no-error","version":1,"kind":"FixedRule"},{"name":"within-time","version":1,"kind":"FixedRule"}]}}
                       """);

        var cut = ctx.Render<GraderList>(p => p.Add(c => c.Run, run));

        Assert.That(cut.FindAll(".chip").Select(c => c.TextContent),
            Is.EqualTo(new[] { "no-error v1 · FixedRule", "within-time v1 · FixedRule" }));
        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Grades/GraderList.graders.html", cut.Markup);
    }

    [Test]
    public void FromBeforeStep2a_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<GraderList>(p => p.Add(c => c.Run, Json("""{"run":{"id":"run_0","graders":null}}""")));

        Assert.That(cut.FindAll(".chip"), Is.Empty);
        Assert.That(cut.Find("p").TextContent, Is.EqualTo("This run recorded no graders."));
        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Grades/GraderList.none.html", cut.Markup);
    }
}
