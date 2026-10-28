using System.Text.Json;
using LouisAgent.Puls.Admin.Prices;
using LouisAgent.Puls.Admin.Scoring;

namespace LouisAgent.Puls.Tests.Admin.Scoring;

/// <summary>
/// A conversation's scores as the chips under its turns: who answered and whether that was right first, then what
/// the checks share, each with its colour.
/// </summary>
public class ScoreChipsTests
{
    /// <summary>Every score the chips know, in the order they are shown.</summary>
    [Test]
    public void EveryScore_IsAChip_InOrder()
    {
        var chips = ScoreChips.Of(SampleRole.Alpha, Scores("""
                                                           {"routed_right":true,"follow_up":"continued","ingredients":5,"priced":3,"estimated":1,"priced_ratio":0.8,
                                                            "images":0,"unsourced":2,"per_kg":1,"deals_named":2,"untooled_shops":1,"no_shop_rows":3,"wrong_store":1,
                                                            "no_catalogue_line":true,"store_for_area":4,"store_default":2,"shop_hosts":["a","b","c"],"errors":1}
                                                           """));

        Assert.That(chips,
            Is.EqualTo(new[]
            {
                new PricedChip($"{SampleRole.Alpha} ✓", "ok"), new PricedChip("continued", "ok"),
                new PricedChip("priced 4 / 5", "ok"), new PricedChip("estimated 1", ""),
                new PricedChip("no image", "warn"), new PricedChip("guessed 2", "warn"),
                new PricedChip("per kg 1", "warn"), new PricedChip("deals 2", "ok"),
                new PricedChip("no-tool shops 1", "bad"), new PricedChip("no shop, no price 3", "ok"),
                new PricedChip("wrong store 1", "bad"), new PricedChip("no catalogue here", ""),
                new PricedChip("town's store 4", "ok"), new PricedChip("default store 2", "warn"),
                new PricedChip("shops 3", ""), new PricedChip("foute 1", "bad")
            }));
    }

    /// <summary>A conversation Puls answered itself, routed wrong, says so; another follow-up has no colour.</summary>
    [Test]
    public void PulsItself_RoutedWrong_IsBad()
    {
        var chips = ScoreChips.Of(null, Scores("""{"routed_right":false,"follow_up":"fresh"}"""));

        Assert.That(chips, Is.EqualTo(new[] { new PricedChip("Puls itself ✗", "bad"), new PricedChip("fresh", "") }));
    }

    /// <summary>Counts of nothing, and scores with no shopping list, show no chip.</summary>
    [Test]
    public void ZeroCounts_ShowNoChip()
    {
        var chips = ScoreChips.Of(null,
            Scores("""{"unsourced":2,"deals_named":0,"errors":0,"no_catalogue_line":false}"""));

        Assert.That(chips, Is.Empty);
    }

    /// <summary>The scores as the store or the script sends them.</summary>
    private static JsonElement Scores(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
