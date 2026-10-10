using louis_agent.core.usage;

namespace louis_agent.core.tests;

// The tracked config/prices.json must load and price the models the profiles use.
public class ShippedPricesTests
{
    private static PriceTable Shipped() => PriceTable.Load(Path.Combine(TestPaths.RepoRoot, "config", "prices.json"));

    [Test]
    public void ShippedTable_Loads()
    {
        PriceTable table = Shipped();

        Assert.That(table.IsEmpty, Is.False);
        Assert.That(table.AsOf, Is.Not.Null.And.Not.Empty);
    }

    [TestCase("claude-haiku-5-5")]         // config/.env.anthropic
    [TestCase("claude-haiku-4-5-20251001")]
    [TestCase("llama3.1")]                 // config/.env.ollama
    [TestCase("qwen2.5:0.5b")]
    public void ShippedTable_PricesTheProfilesModels(string model) =>
        Assert.That(Shipped().Find(model), Is.Not.Null);

    [Test]
    public void ShippedTable_HaikuFiveFive_HasItsLongPromptTier() =>
        Assert.That(Shipped().Find("claude-haiku-5-5")!.LongPrompt!.AboveTokens, Is.EqualTo(100_000));
}