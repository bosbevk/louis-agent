using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Feed;
using LouisAgent.Packs.Recipes.Library.Store;

namespace LouisAgent.Packs.Recipes.Tests.Library;

/// <summary>
/// One page has one address in the library, however a link spells it, or we'd keep it twice and miss it when we look.
/// And with nothing set up, the library holds nothing.
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

    /// <summary>A page's id is the same however its link is spelled, and other pages get other ids.</summary>
    [Test]
    public void OnePage_HasOneId()
    {
        string id = RecipeAddress.IdOf(new Uri("https://www.a.co.za/recipes/bobotie/"));

        Assert.That(RecipeAddress.IdOf(new Uri("http://a.co.za/recipes/bobotie")), Is.EqualTo(id));
        Assert.That(RecipeAddress.IdOf(new Uri("https://a.co.za/recipes/melktert")), Is.Not.EqualTo(id));
        Assert.That(RecipeAddress.IsId(id), Is.True);
    }

    /// <summary>Only what an id looks like counts as one.</summary>
    [TestCase("")]
    [TestCase("0123456789abcde")]
    [TestCase("0123456789ABCDEF")]
    [TestCase("0123456789abcdeg")]
    [TestCase("../../../../etc/")]
    public void AnythingElse_IsNoId(string text) => Assert.That(RecipeAddress.IsId(text), Is.False);

    /// <summary>With no database there's nothing to read and nowhere to keep, and nothing fails.</summary>
    [Test]
    public async Task WithNoLibrary_NothingIsThere_AndNothingFails()
    {
        var recipe = KeptRecipes.Of("https://a.co.za/x");
        await new NoRecipeStore().KeepAsync(recipe, default);

        Assert.That(await new NoRecipeStore().AllAsync(default), Is.Empty);
        Assert.That(await new NoRecipeStore().FindAsync(recipe.Id, default), Is.Null);
        Assert.That(await new NoRecipeLibrary().ReadAsync(recipe.Id, default), Is.Null);
        Assert.That(new NoRecipeLibrary().IdIn(recipe.Page), Is.Null);
        Assert.That(await new NoRecipeLibrary().SearchAsync("x", 8, default), Is.Empty);
    }
}
