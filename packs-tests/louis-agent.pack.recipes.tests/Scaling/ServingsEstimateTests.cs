using LouisAgent.Packs.Recipes.Recipes;
using LouisAgent.Packs.Recipes.Scaling;

namespace LouisAgent.Packs.Recipes.Tests.Scaling;

/// <summary>
/// A page that doesn't say how many it feeds still has to be scaled to someone. Its main ingredient says roughly: about
/// 175 g of meat, poultry or fish a person. With no such ingredient there's nothing to go on, and it says so.
/// </summary>
public class ServingsEstimateTests
{
    /// <summary>A recipe with these ingredient lines and no yield.</summary>
    private static Recipe With(params string[] lines) => new("Pie", null, null, null, null,
        [.. lines.Select(line => new RecipeIngredient(null, line))], [new RecipeStep(null, "Cook it.")], null, null,
        new Uri("https://example.com/pie"));

    /// <summary>The meat's weight says how many, adding up every line of it.</summary>
    [TestCase(6d, "1 kg mince", "2 onions")]
    [TestCase(6d, "500 g beef", "500 g pork", "1 onion")]
    [TestCase(11d, "2 kg beef goulash")]
    [TestCase(4d, "700g oxtail")]
    [TestCase(3d, "500 g maalvleis")]
    [TestCase(6d, "1 kg hoender")]
    [TestCase(6d, "about 1 kg lamb knuckle")]
    public void TheMeatsWeight_SaysHowMany(double people, params string[] lines) =>
        Assert.That(ServingsEstimate.Of(With(lines))?.People, Is.EqualTo(people));

    /// <summary>A whole chicken feeds four, and two feed eight.</summary>
    [TestCase(4d, "1 whole chicken")]
    [TestCase(8d, "2 whole chickens")]
    [TestCase(4d, "1 hele hoender")]
    public void AWholeChicken_FeedsFour(double people, string line) =>
        Assert.That(ServingsEstimate.Of(With(line))?.People, Is.EqualTo(people));

    /// <summary>It names the line it went by, so the answer can say what the estimate rests on.</summary>
    [Test]
    public void ItNames_TheLineItWentBy() =>
        Assert.That(ServingsEstimate.Of(With("2 onions", "1 kg mince"))?.Line, Is.EqualTo("1 kg mince"));

    /// <summary>What only tastes of meat, is too little to build on, or isn't weighed gives no estimate.</summary>
    [TestCase("2 cups flour", "1 cup milk")]
    [TestCase("500 ml chicken stock", "2 onions")]
    [TestCase("120 g chicken stock powder", "2 onions")]
    [TestCase("100 g bacon", "2 cups flour")]
    [TestCase("4 chicken breasts", "2 onions")]
    [TestCase("salt and pepper", "1 onion")]
    public void WithNoMainIngredientToGoBy_ThereIsNoEstimate(params string[] lines) =>
        Assert.That(ServingsEstimate.Of(With(lines)), Is.Null);
}
