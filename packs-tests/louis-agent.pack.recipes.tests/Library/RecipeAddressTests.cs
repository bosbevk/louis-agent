using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Feed;
using LouisAgent.Packs.Recipes.Library.Store;

namespace LouisAgent.Packs.Recipes.Tests.Library;

/// <summary>
/// One page has one address in the library, however a link spells it, or we'd keep it twice and miss it when we look. And
/// with nothing set up, the library holds nothing.
/// </summary>
public class RecipeAddressTests
{
    /// <summary>The ways a site or a search spells the same page.</summary>
    [TestCase("https://www.a.co.za/recipes/bobotie/")]
    [TestCase("https://a.co.za/recipes/bobotie")]
    [TestCase("http://www.a.co.za/recipes/bobotie")]
    [TestCase("https://WWW.A.CO.ZA/recipes/bobotie/#method")]
    public void OnePage_HasOneAddress(string url) =>
        Assert.That(RecipeAddress.Of(new Uri(url)), Is.EqualTo("a.co.za/recipes/bobotie"));

    /// <summary>A different page stays different: a path's case and a query can name another recipe.</summary>
    [TestCase("https://a.co.za/recipes/Bobotie", "a.co.za/recipes/Bobotie")]
    [TestCase("https://a.co.za/?p=12", "a.co.za/?p=12")]
    public void ADifferentPage_StaysDifferent(string url, string key) =>
        Assert.That(RecipeAddress.Of(new Uri(url)), Is.EqualTo(key));

    /// <summary>With no database there's nothing to read and nowhere to keep, and nothing fails.</summary>
    [Test]
    public async Task WithNoLibrary_NothingIsThere_AndNothingFails()
    {
        var page = new Uri("https://a.co.za/x");
        await new NoRecipeStore().KeepAsync(new StoredRecipe(page, "a", "X", "print", DateTimeOffset.UnixEpoch), default);

        Assert.That(await new NoRecipeStore().AllAsync(default), Is.Empty);
        Assert.That(await new NoRecipeLibrary().FindAsync(page, default), Is.Null);
        Assert.That(await new NoRecipeLibrary().SearchAsync("x", 8, default), Is.Empty);
    }
}
