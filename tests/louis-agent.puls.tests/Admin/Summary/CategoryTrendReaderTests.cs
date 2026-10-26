using System.Text.Json;
using LouisAgent.Puls.Admin.Summary;

namespace LouisAgent.Puls.Tests.Admin.Summary;

// The trend-by-category rework (Louis, 2026-10-26: "I need to know what it tells me"): each category is a card that says, per mode, the latest run's pass@k and pass^k as
// numbers, how many runs stand behind it, and whether there is a trend to draw yet (two runs or more on some line). Written first: the reader did not exist.
public class CategoryTrendReaderTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private const string Category = """
        {"category":"budget","modes":[
          {"name":"puls-cook (claude-haiku-5-5)","model":"claude-haiku-5-5","points":[
            {"run":"r1","at":"2026-10-24T09:00:00+00:00","pass_rate":0.5,"pass_at_k":0.5,"pass_hat_k":0.25,"n":6},
            {"run":"r3","at":"2026-10-26T09:00:00+00:00","pass_rate":0.67,"pass_at_k":1.0,"pass_hat_k":0.5,"n":6}]},
          {"name":"puls-tools","model":null,"points":[
            {"run":"r3","at":"2026-10-26T09:00:00+00:00","pass_rate":0,"pass_at_k":0,"pass_hat_k":0,"n":6}]}]}
        """;

    [Test]
    public void EachLine_HasItsLatestPoint_AndItsRunCount()
    {
        var card = CategoryTrendReader.Read(Json(Category));

        Assert.That(card.Name, Is.EqualTo("budget"));
        Assert.That(card.Lines.Select(l => (l.Name, l.Runs, l.PassAtK, l.PassHatK, l.N)), Is.EqualTo(new[]
        {
            ("puls-cook (claude-haiku-5-5)", 2, (double?)1.0, (double?)0.5, 6),
            ("puls-tools", 1, (double?)0.0, (double?)0.0, 6),
        }));
    }

    [Test]
    public void TheLatestIsByTime_NotByOrder()
    {
        var card = CategoryTrendReader.Read(Json("""
            {"category":"x","modes":[{"name":"m","model":null,"points":[
              {"run":"new","at":"2026-10-26T09:00:00+00:00","pass_rate":1,"pass_at_k":1,"pass_hat_k":1,"n":6},
              {"run":"old","at":"2026-10-20T09:00:00+00:00","pass_rate":0,"pass_at_k":0,"pass_hat_k":0,"n":6}]}]}
            """));

        Assert.That(card.Lines.Single().LatestRun, Is.EqualTo("new"));
    }

    [Test]
    public void ThereIsATrendToDraw_OnlyWithTwoRunsOrMoreOnSomeLine()
    {
        Assert.That(CategoryTrendReader.Read(Json(Category)).HasTrend, Is.True);
        const string one = """{"category":"x","modes":[{"name":"m","model":null,"points":[{"run":"r","at":"2026-10-26T09:00:00+00:00","pass_rate":1,"pass_at_k":1,"pass_hat_k":1,"n":6}]}]}""";
        Assert.That(CategoryTrendReader.Read(Json(one)).HasTrend, Is.False);
    }

    [Test]
    public void TheRunCountOfTheCard_IsTheDistinctRunsAcrossItsLines() =>
        Assert.That(CategoryTrendReader.Read(Json(Category)).Runs, Is.EqualTo(2));
}
