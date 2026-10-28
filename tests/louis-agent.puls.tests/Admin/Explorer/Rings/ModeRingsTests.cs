using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Charts;
using LouisAgent.Puls.Admin.Explorer.Legends;
using LouisAgent.Puls.Admin.Explorer.Rings;
using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Tests.Admin.Explorer.Rings;

/// <summary>The donut each "per mode" chart draws for one mode.</summary>
public class ModeRingsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A mode with two conversations: a cook routed right and priced (Puls's request and a specialist's, two turns),
    /// and a chat routed wrong whose only request has no price.
    /// </summary>
    private static ModeNode Mode()
    {
        var cook = new ConversationNode
        {
            Task = "cook-a",
            Scores = Json("""{"routed_right":true,"ingredients":4,"priced":4,"priced_ratio":1.0}""")
        };
        var first = new TurnNode
        {
            N = 1,
            Role = SampleRole.Alpha,
            Seconds = 6,
            ToolCalls = 4,
            ToolsFailed = 1,
            ToolReasons = [new ToolReasonRow("Fetch", "timed out", 1)]
        };
        first.Rows.Add(new RequestNode(T0, "turn", "s1", null, "m", 100, 20, 300, 50, 0.01m, 1500));
        first.Rows.Add(new RequestNode(T0, "turn", "s2", "s1", "m", 80, 0, 100, 20, 0.03m, 2500));
        cook.Turns.Add(first);
        cook.Turns.Add(new TurnNode
        {
            N = 2,
            Seconds = 2
        });

        var chat = new ConversationNode
        {
            Task = "chat",
            Scores = Json("""{"routed_right":false}""")
        };
        var only = new TurnNode
        {
            N = 1,
            Seconds = 1
        };
        only.Rows.Add(new RequestNode(T0, "turn", "s3", null, "m", 10, null, null, 5, null, 500));
        chat.Turns.Add(only);

        var mode = new ModeNode { Name = SampleMode.Alpha };
        mode.Conversations.AddRange([cook, chat]);
        return mode;
    }

    /// <summary>Puls's cost against the specialist's, the known total in the middle.</summary>
    [Test]
    public void TheCost_SplitsPulsFromTheSpecialist() =>
        AssertRing(ModeRings.Cost(Mode(), null), [0.01, 0.03], [ExplorerColours.Own, ExplorerColours.Spec],
            ["Puls $0.01", $"{SampleRole.Alpha} $0.03"], "$0.04", "3 requests");

    /// <summary>The four kinds of token, their total in the middle.</summary>
    [Test]
    public void TheTokens_AreByKind() =>
        AssertRing(ModeRings.Tokens(Mode()), [190, 20, 400, 75], TokenKinds.Colours,
            ["input 190", "cache written 20", "cache read 400", "output 75"], "685", "tokens");

    /// <summary>The model's seconds on each side, a turn's average wall time in the middle.</summary>
    [Test]
    public void TheTime_IsTheModelsSeconds_AndATurnsAverage()
    {
        AssertRing(ModeRings.Time(Mode()), [2, 2.5], [ExplorerColours.Own, ExplorerColours.Spec],
            ["Puls 2.0 s", $"{SampleRole.Alpha} 2.5 s"], "3.0 s", "per turn");
        Assert.That(ModeRings.ModelSeconds(Mode()), Is.EqualTo(4.5));
    }

    /// <summary>Routed right against wrong, the share in the middle; with no conversations, a dash.</summary>
    [Test]
    public void TheRouting_IsTheShareRoutedRight()
    {
        AssertRing(ModeRings.Routing(Mode()), [1, 1], [ExplorerColours.Right, ExplorerColours.Wrong],
            ["routed right 1", "wrong 1"], "50%", "1 / 2");
        Assert.That(ModeRings.Routing(new ModeNode()).Centre, Is.EqualTo("—"));
    }

    /// <summary>The ingredients priced right; a mode whose check listed none gets the empty ring.</summary>
    [Test]
    public void TheAccuracy_IsTheIngredientsPriced()
    {
        AssertRing(ModeRings.Accuracy(Mode()), [4, 0, 0, 0],
            [ExplorerColours.Right, ExplorerColours.Estimate, ExplorerColours.Guess, ExplorerColours.Wrong],
            ["right 4", "estimated 0", "guessed 0", "no price 0"], "100%", "4 / 4");
        AssertRing(ModeRings.Accuracy(new ModeNode()), [0], [ExplorerColours.Wrong], ["no list"], "—", "no list");
    }

    /// <summary>Input from the cache against input sent fresh, the hit share in the middle.</summary>
    [Test]
    public void TheCache_IsTheShareRead() =>
        AssertRing(ModeRings.Cache(Mode()), [400, 210], [ChartPalette.Series(2), ChartPalette.Series(0)],
            ["from the cache 400", "fresh 210"], "66%", "from the cache");

    /// <summary>Tool calls that worked against those that failed, the failures in the middle.</summary>
    [Test]
    public void TheTools_AreTheFailures()
    {
        AssertRing(ModeRings.Tools(Mode()), [3, 1], [ExplorerColours.Right, ExplorerColours.Wrong],
            ["worked 3", "refused or failed 1"], "1", "of 4");
        Assert.That(ModeRings.Tools(new ModeNode()).Centre, Is.EqualTo("—"));
    }

    /// <summary>The sites by kind, how many in the middle.</summary>
    [Test]
    public void TheSources_AreTheSitesByKind() =>
        AssertRing(ModeRings.Sources(new SiteCounts(["a.co.za"], 0, 1, 0, 0)), [0, 1, 0, 0],
            [ExplorerColours.Right, ChartPalette.Series(0), ChartPalette.Series(1), ExplorerColours.Other],
            ["SA shops 0", "other SA 1", "maps 0", "elsewhere 0"], "1", "site");

    /// <summary>What answered conversations cost against the rest, one answer's cost in the middle.</summary>
    [Test]
    public void TheValue_IsWhatAnAnswerCost()
    {
        AssertRing(ModeRings.Value(Mode(), null), [0.04, 0], [ExplorerColours.Right, ExplorerColours.Wrong],
            ["with an answer $0.04", "without $0"], "$0.04", "per answer");
        Assert.That(ModeRings.Value(new ModeNode(), null).Unit, Is.EqualTo("no answer"));
    }

    /// <summary>Checks every part of a ring.</summary>
    private static void AssertRing(RingFigure ring, double[] parts, IReadOnlyList<string> colours, string[] titles,
        string centre, string unit)
    {
        Assert.Multiple(() =>
        {
            Assert.That(ring.Parts, Is.EqualTo(parts).Within(1e-9));
            Assert.That(ring.Colours, Is.EqualTo(colours));
            Assert.That(ring.Titles, Is.EqualTo(titles));
            Assert.That(ring.Centre, Is.EqualTo(centre));
            Assert.That(ring.Unit, Is.EqualTo(unit));
        });
    }

    /// <summary>Scores from their JSON.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();
}
