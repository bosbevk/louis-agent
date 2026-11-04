using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.ShopPrices.Messages;

namespace LouisAgent.Composition.Tests.Copies;

/// <summary>
/// The mark for a line with no price lives twice: the shop-prices pack writes it in its table, and the eval files name
/// it for <c>tool-rows-marked</c>, since a pack references no evals. This is the one project that sees both, so it
/// holds them equal: a tool that changed its mark would otherwise leave every run saying nothing was missing.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0073-packs-reference-no-evals.md">ADR 0073: packs reference no evals</seealso>
public class UnpricedMarkTests
{
    /// <summary>The check's settings in an eval file, by the name that file gives it.</summary>
    private static JsonElement UnpricedItems(string eval)
    {
        string path = Path.Combine(TestPaths.RepoRoot, "identities", "louis-agent.identity.puls.evals", "Evals", eval,
            "eval.json");
        using var file = JsonDocument.Parse(File.ReadAllText(path));
        return file.RootElement.GetProperty("checks").EnumerateArray()
            .Single(check => check.TryGetProperty("name", out var name) && name.GetString() == "unpriced-items")
            .GetProperty("parameters").Clone();
    }

    /// <summary>Both rows the tool can't price end in the mark the evals look for, in the tool they name.</summary>
    [TestCase("Cook")]
    [TestCase("Recipes")]
    public void TheEvalsMark_IsTheOneTheToolWrites(string eval)
    {
        var settings = UnpricedItems(eval);
        string ending = $"| {settings.GetProperty("mark").GetString()} |";

        Assert.That(settings.GetProperty("tool").GetString(), Is.EqualTo("EstimatePrices"));
        Assert.That(MessageText.Template(ShopPriceToolMessage.LineNotFound), Does.EndWith(ending));
        Assert.That(MessageText.Template(ShopPriceToolMessage.LineOtherUnit), Does.EndWith(ending));
    }
}
