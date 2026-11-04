using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Recipes;

/// <summary>
/// Checks that a recipe is read from the data a page publishes for machines (schema.org's <c>Recipe</c>), on real
/// pages from sites that allow us, and on small made-up blocks for the shapes no page shows.
/// </summary>
/// <remarks>
/// Each real page is a recorded file, so what a test expects is what the file says, not what we would guess.
/// </remarks>
public class SchemaOrgRecipeTests
{
    private const string OneLine = "\"recipeIngredient\":[\"1 egg\"]";
    private const string OneStep = "\"recipeInstructions\":\"Cook it.\"";

    private static readonly Uri Page = new("https://example.co.za/recipes/a-recipe/");

    /// <summary>Reads one recorded page the way the tool will: its HTML and the address it came from.</summary>
    private static Recipe? ReadFixture(string name) => SchemaOrgRecipe.Read(
        File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Recipes", "Fixtures", name + ".html")),
        Page);

    /// <summary>Wraps a piece of JSON in the script tag a page would carry it in, and reads it.</summary>
    private static Recipe? ReadJson(string json) => SchemaOrgRecipe.Read(
        $"<html><head><script type=\"application/ld+json\">{json}</script></head><body></body></html>", Page);

    /// <summary>
    /// A whole recipe with the given extra properties, for the shapes a field can take. It brings its own line and
    /// step, unless the test is about those.
    /// </summary>
    private static Recipe ReadRecipe(string properties)
    {
        var fields = new List<string>
        {
            "\"@type\":\"Recipe\"",
            "\"name\":\"Test\""
        };
        if (!properties.Contains("recipeIngredient")) fields.Add(OneLine);
        if (!properties.Contains("recipeInstructions")) fields.Add(OneStep);
        if (properties.Length > 0) fields.Add(properties);

        return ReadJson("{" + string.Join(",", fields) + "}")!;
    }

    /// <summary>Food &amp; Home keeps a plain Recipe block of its own, with steps as bare strings.</summary>
    [Test]
    public void FoodAndHome_IsReadWithEveryLineAsWritten()
    {
        var recipe = ReadFixture("foodandhome-pork-bobotie")!;

        Assert.Multiple(() =>
        {
            Assert.That(recipe.Name, Is.EqualTo("Pork bobotie"));
            Assert.That(recipe.Yield, Is.EqualTo("4 to 6"));
            Assert.That(recipe.PrepTime, Is.EqualTo(TimeSpan.FromMinutes(20)));
            Assert.That(recipe.CookTime, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(recipe.Ingredients, Has.Count.EqualTo(21));
            Assert.That(recipe.Ingredients[0], Is.EqualTo("***Filling"));
            Assert.That(recipe.Ingredients[1],
                Is.EqualTo("500 g Pork mince (you can ask your butcher to prepare this for you)"));
            Assert.That(recipe.Steps, Has.Count.EqualTo(11));
            Assert.That(recipe.Steps[0].Section, Is.Null);
            Assert.That(recipe.Steps[0].Text, Does.StartWith("Preheat oven to 170"));
            Assert.That(recipe.Image?.AbsoluteUri, Is.EqualTo(
                "https://www.foodandhome.co.za/wp-content/uploads/2026/09/Pork-Mince-Bobotie-4-scaled.jpeg"));
            Assert.That(recipe.Author, Is.EqualTo("SA Pork"));
            Assert.That(recipe.Source, Is.EqualTo(Page));
        });
    }

    /// <summary>The page's total time is just "P", which says nothing, so the total is unknown.</summary>
    [Test]
    public void FoodAndHome_AnEmptyTotalTime_IsUnknown() =>
        Assert.That(ReadFixture("foodandhome-pork-bobotie")!.TotalTime, Is.Null);

    /// <summary>
    /// Bibby's Kitchen carries four blocks, and the recipe is in the theme's own. The page's blank last line is left
    /// out, and an apostrophe it wrote as <c>&amp;#039;</c> reads as an apostrophe.
    /// </summary>
    [Test]
    public void Bibbys_IsReadFromTheThemesOwnBlock_AmongFour()
    {
        var recipe = ReadFixture("bibbyskitchenat36-bobotie-filo-parcels")!;

        Assert.Multiple(() =>
        {
            Assert.That(recipe.Name, Is.EqualTo("Bobotie Filo Parcels"));
            Assert.That(recipe.Yield, Is.EqualTo("6"));
            Assert.That(recipe.PrepTime, Is.EqualTo(TimeSpan.FromMinutes(20)));
            Assert.That(recipe.CookTime, Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(recipe.TotalTime, Is.Null);
            Assert.That(recipe.Ingredients, Has.Count.EqualTo(23));
            Assert.That(recipe.Ingredients[0],
                Is.EqualTo("2 slices white bread, crusts removed and soaked in 200ml milk"));
            Assert.That(recipe.Ingredients[16], Is.EqualTo("1 tablespoon Mrs Ball's chutney"));
            Assert.That(recipe.Steps, Has.Count.EqualTo(6));
            Assert.That(recipe.Steps[0].Text,
                Is.EqualTo("Place the sultanas in a small bowl, cover with the orange juice and set aside."));
            Assert.That(recipe.Image?.AbsoluteUri,
                Is.EqualTo("https://www.bibbyskitchenat36.com/wp-content/uploads/2024/09/DSC_9521.jpg"));
            Assert.That(recipe.Author, Is.EqualTo("Dianne Bibby"));
            Assert.That(recipe.Source, Is.EqualTo(Page));
        });
    }

    /// <summary>WP Recipe Maker inside Rank Math's graph: the recipe is found inside it, steps by section.</summary>
    [Test]
    public void Tantalise_TheRecipeInsideTheGraph_IsFound()
    {
        var recipe = ReadFixture("tantalisemytastebuds-pickled-fish")!;

        Assert.Multiple(() =>
        {
            Assert.That(recipe.Name, Is.EqualTo("Cape Malay Pickled Fish"));
            Assert.That(recipe.Yield, Is.EqualTo("6"));
            Assert.That(recipe.PrepTime, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(recipe.CookTime, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(recipe.TotalTime, Is.EqualTo(TimeSpan.FromMinutes(60)));
            Assert.That(recipe.Ingredients, Has.Count.EqualTo(22));
            Assert.That(recipe.Steps, Has.Count.EqualTo(18));
            Assert.That(recipe.Author, Is.EqualTo("Razena Schroeder"));
            Assert.That(recipe.Image?.AbsoluteUri, Is.EqualTo(
                "https://www.tantalisemytastebuds.com/wp-content/uploads/2018/03/Cape-Malay-Pickled-Fish-4355.jpg"));
        });
    }

    /// <summary>Tantalise groups its steps in four named sections, and each step knows its section.</summary>
    [Test]
    public void Tantalise_EachSectionsName_BecomesItsStepsSection()
    {
        var steps = ReadFixture("tantalisemytastebuds-pickled-fish")!.Steps;

        Assert.That(steps.GroupBy(s => s.Section).Select(g => (g.Key, g.Count())),
            Is.EqualTo(new[]
            {
                ("Masala fish", 6), ("Blanching the onions", 4), ("Pickling marinade", 2),
                ("Layering the Cape Malay pickled fish", 6)
            }));
    }

    /// <summary>WP Recipe Maker inside Yoast's graph: found inside it, in two sections.</summary>
    [Test]
    public void WanderCapeTown_TheRecipeInsideTheGraph_IsFound()
    {
        var recipe = ReadFixture("wandercapetown-malva-pudding")!;

        Assert.Multiple(() =>
        {
            Assert.That(recipe.Name, Is.EqualTo("Malva Pudding"));
            Assert.That(recipe.Yield, Is.EqualTo("8"));
            Assert.That(recipe.PrepTime, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(recipe.CookTime, Is.EqualTo(TimeSpan.FromMinutes(45)));
            Assert.That(recipe.TotalTime, Is.EqualTo(TimeSpan.FromMinutes(75)));
            Assert.That(recipe.Ingredients, Has.Count.EqualTo(14));
            Assert.That(recipe.Steps.GroupBy(s => s.Section).Select(g => (g.Key, g.Count())),
                Is.EqualTo(new[] { ("Pudding", 7), ("Sauce", 3) }));
            Assert.That(recipe.Image?.AbsoluteUri,
                Is.EqualTo("https://wandercapetown.com/wp-content/uploads/2021/01/malva-pudding-recipe.jpg"));
        });
    }

    /// <summary>The author there is only a link to another entity (<c>@id</c>), with no name: unknown.</summary>
    [Test]
    public void WanderCapeTown_AnAuthorThatIsOnlyALink_IsUnknown() =>
        Assert.That(ReadFixture("wandercapetown-malva-pudding")!.Author, Is.Null);

    /// <summary>
    /// Rooi Rose's page has a Recipe block with its ingredients and steps left empty, so the cook reads its text
    /// instead of an empty recipe.
    /// </summary>
    [Test]
    public void Rooirose_ABlockWithEmptyLinesAndSteps_GivesNull() =>
        Assert.That(ReadFixture("rooirose-hoenderpasteitjies"), Is.Null);

    /// <summary>An Afrikaans recipe is read like any other, and its words stay as the page wrote them.</summary>
    [Test]
    public void AnAfrikaansRecipe_KeepsItsWords()
    {
        var recipe = ReadJson("""
                              {"@type":"Recipe","name":"Melktert",
                               "recipeIngredient":["1 liter melk"],
                               "recipeInstructions":"Bak tot gaar."}
                              """)!;

        Assert.Multiple(() =>
        {
            Assert.That(recipe.Name, Is.EqualTo("Melktert"));
            Assert.That(recipe.Ingredients, Is.EqualTo(new[] { "1 liter melk" }));
            Assert.That(recipe.Steps, Is.EqualTo(new[] { new RecipeStep(null, "Bak tot gaar.") }));
        });
    }

    /// <summary>
    /// A block with no name, no line or no step can't be cooked from, so it gives no recipe. The first case is
    /// Paarman's shape, and the last is the empty step on Bibby's list pages.
    /// </summary>
    [TestCase("""{"@type":"Recipe","name":"Only a title"}""")]
    [TestCase($$"""{"@type":"Recipe",{{OneLine}},{{OneStep}}}""")]
    [TestCase($$"""{"@type":"Recipe","name":"  ",{{OneLine}},{{OneStep}}}""")]
    [TestCase($$"""{"@type":"Recipe","name":"No lines",{{OneStep}}}""")]
    [TestCase($$"""{"@type":"Recipe","name":"Blank lines","recipeIngredient":["",""],{{OneStep}}}""")]
    [TestCase($$"""{"@type":"Recipe","name":"No steps",{{OneLine}}}""")]
    [TestCase($$"""{"@type":"Recipe","name":"Empty",{{OneLine}},"recipeInstructions":[{"@type":"HowToStep"}]}""")]
    public void ARecipeThatIsNotWhole_GivesNull(string json) => Assert.That(ReadJson(json), Is.Null);

    /// <summary>A yield written as text, as a list with one text, as a number, or not at all.</summary>
    [TestCase("\"recipeYield\":\"4 to 6\"", "4 to 6")]
    [TestCase("\"recipeYield\":[\"6\"]", "6")]
    [TestCase("\"recipeYield\":[\"6\",\"6 servings\"]", "6")]
    [TestCase("\"recipeYield\":6", "6")]
    [TestCase("\"recipeYield\":\"\"", null)]
    [TestCase("\"recipeYield\":\"   \"", null)]
    [TestCase("", null)]
    public void TheYield_KeepsTheFirstText(string properties, string? expected) =>
        Assert.That(ReadRecipe(properties).Yield, Is.EqualTo(expected));

    /// <summary>Steps as plain <c>HowToStep</c>s, each with its <c>text</c>, and no section.</summary>
    [Test]
    public void Instructions_AsHowToSteps_AreOneStepEach()
    {
        var steps = ReadRecipe("""
                               "recipeInstructions":[
                                 {"@type":"HowToStep","text":"Mix."},
                                 {"@type":"HowToStep","text":"Bake."}]
                               """).Steps;

        Assert.That(steps, Is.EqualTo(new[] { new RecipeStep(null, "Mix."), new RecipeStep(null, "Bake.") }));
    }

    /// <summary>Steps in <c>HowToSection</c>s: the section's name goes on each step inside it.</summary>
    [Test]
    public void Instructions_AsSections_GiveEachStepItsSection()
    {
        var steps = ReadRecipe("""
                               "recipeInstructions":[
                                 {"@type":"HowToSection","name":"Cake",
                                  "itemListElement":[{"@type":"HowToStep","text":"Mix."}]},
                                 {"@type":"HowToSection","name":"Icing",
                                  "itemListElement":[{"@type":"HowToStep","text":"Whisk."}]}]
                               """).Steps;

        Assert.That(steps, Is.EqualTo(new[] { new RecipeStep("Cake", "Mix."), new RecipeStep("Icing", "Whisk.") }));
    }

    /// <summary>Instructions written as one plain string are one step, with no section.</summary>
    [Test]
    public void Instructions_AsOnePlainString_AreOneStep() =>
        Assert.That(ReadRecipe("\"recipeInstructions\":\"Mix it all and bake.\"").Steps,
            Is.EqualTo(new[] { new RecipeStep(null, "Mix it all and bake.") }));

    /// <summary>Times are ISO 8601 durations; a missing or unreadable one is unknown, never zero.</summary>
    [TestCase("PT1H15M", 75)]
    [TestCase("PT20M", 20)]
    [TestCase("PT90M", 90)]
    [TestCase("PT", null)]
    [TestCase("P", null)]
    [TestCase("a while", null)]
    public void ATime_IsReadFromItsDuration(string duration, int? minutes) =>
        Assert.That(ReadRecipe($"\"cookTime\":\"{duration}\"").CookTime,
            Is.EqualTo(minutes is { } m ? TimeSpan.FromMinutes(m) : null));

    /// <summary>A time the page leaves out is unknown, not zero.</summary>
    [Test]
    public void AMissingTime_IsUnknown()
    {
        var recipe = ReadRecipe("");

        Assert.Multiple(() =>
        {
            Assert.That(recipe.PrepTime, Is.Null);
            Assert.That(recipe.CookTime, Is.Null);
            Assert.That(recipe.TotalTime, Is.Null);
        });
    }

    /// <summary>An image as a string, as an object with a <c>url</c>, or as a list (the first one counts).</summary>
    [TestCase("\"image\":\"https://img.example/a.jpg\"")]
    [TestCase("\"image\":{\"@type\":\"ImageObject\",\"url\":\"https://img.example/a.jpg\"}")]
    [TestCase("\"image\":[\"https://img.example/a.jpg\",\"https://img.example/b.jpg\"]")]
    [TestCase("\"image\":[{\"url\":\"https://img.example/a.jpg\"},{\"url\":\"https://img.example/b.jpg\"}]")]
    public void TheImage_IsTheFirstOneWhateverItsShape(string properties) =>
        Assert.That(ReadRecipe(properties).Image?.AbsoluteUri, Is.EqualTo("https://img.example/a.jpg"));

    /// <summary>A relative image address is resolved against the page it came from.</summary>
    [Test]
    public void ARelativeImage_IsResolvedAgainstThePage() =>
        Assert.That(ReadRecipe("\"image\":\"/img/a.jpg\"").Image?.AbsoluteUri,
            Is.EqualTo("https://example.co.za/img/a.jpg"));

    /// <summary>A page with no image has none.</summary>
    [Test]
    public void ANoImage_IsUnknown() => Assert.That(ReadRecipe("").Image, Is.Null);

    /// <summary>An author as an object with a name, as a plain string, or missing.</summary>
    [TestCase("\"author\":{\"@type\":\"Person\",\"name\":\"Ouma\"}", "Ouma")]
    [TestCase("\"author\":\"Ouma\"", "Ouma")]
    [TestCase("\"author\":[{\"name\":\"Ouma\"},{\"name\":\"Oupa\"}]", "Ouma")]
    [TestCase("", null)]
    public void TheAuthor_IsItsName(string properties, string? expected) =>
        Assert.That(ReadRecipe(properties).Author, Is.EqualTo(expected));

    /// <summary>A page with no data at all for machines (the text sources) gives no recipe.</summary>
    [Test]
    public void APageWithoutJsonLd_GivesNull() =>
        Assert.That(ReadFixture("thelazymakoti-chakalaka"), Is.Null);

    /// <summary>Data for machines with no <c>Recipe</c> in it (an article, say) gives no recipe either.</summary>
    [Test]
    public void JsonLdWithoutARecipe_GivesNull() =>
        Assert.That(ReadJson("""{"@type":"Article","name":"Not a recipe"}"""), Is.Null);

    /// <summary>An entity that is a Recipe among other types (<c>["Recipe","NewsArticle"]</c>) still counts.</summary>
    [Test]
    public void ATypeThatIsAList_StillCountsAsARecipe() =>
        Assert.That(ReadJson($$"""{"@type":["Recipe","NewsArticle"],"name":"Both",{{OneLine}},{{OneStep}}}""")?.Name,
            Is.EqualTo("Both"));

    /// <summary>The recipe is found in a top-level list of entities as well as in a <c>@graph</c>.</summary>
    [Test]
    public void ARecipeInATopLevelList_IsFound() =>
        Assert.That(
            ReadJson($$"""[{"@type":"WebSite"},{"@type":"Recipe","name":"Listed",{{OneLine}},{{OneStep}}}]""")?.Name,
            Is.EqualTo("Listed"));
}
