using LouisAgent.Composition.Packs.Screens.Editing;
using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Screens;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tests.Library.Screens;

/// <summary>
/// A kept recipe as a form and back: nothing is lost on the way, a recipe's rules hold, and where it came from can't
/// change.
/// </summary>
public class RecipeFormTests
{
    private static readonly StoredRecipe Melktert = new(
        new Recipe("Melktert", "8 stukke", TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(45), null,
            [new RecipeIngredient("Kors", "1 pak beskuitjies"), new RecipeIngredient(null, "1 l melk")],
            [new RecipeStep(null, "Meng."), new RecipeStep("Om klaar te maak", "Bak.")],
            new Uri("https://example.com/melktert.jpg"), "'n Kok", new Uri("https://example.com/melktert/")),
        "taste", new DateTimeOffset(2026, 11, 5, 9, 30, 0, TimeSpan.Zero));

    /// <summary>What a form sends if nobody changes anything.</summary>
    private static ScreenValues Untouched(StoredRecipe stored)
    {
        var fields = RecipeForm.Of(stored).Fields;
        return new ScreenValues(
            fields.Where(field => !field.ReadOnly && field.Kind is not ScreenFieldKind.Rows).ToDictionary(field => field.Name, field => field.Value),
            fields.Where(field => field.Kind is ScreenFieldKind.Rows).ToDictionary(field => field.Name, field => field.Rows));
    }

    /// <summary>The same form with one plain field changed.</summary>
    private static ScreenValues With(string field, string? value)
    {
        var values = Untouched(Melktert);
        return values with { Values = new Dictionary<string, string?>(values.Values) { [field] = value } };
    }

    /// <summary>A recipe with sections goes out and back unchanged.</summary>
    [Test]
    public void ARecipe_GoesOutAndBack_Unchanged()
    {
        var (recipe, errors) = RecipeForm.Read(Melktert, Untouched(Melktert));

        Assert.That(errors, Is.Empty);
        Assert.That(recipe!.Name, Is.EqualTo(Melktert.Recipe.Name));
        Assert.That(recipe.Yield, Is.EqualTo(Melktert.Recipe.Yield));
        Assert.That(recipe.PrepTime, Is.EqualTo(Melktert.Recipe.PrepTime));
        Assert.That(recipe.CookTime, Is.EqualTo(Melktert.Recipe.CookTime));
        Assert.That(recipe.TotalTime, Is.Null);
        Assert.That(recipe.Ingredients, Is.EqualTo(Melktert.Recipe.Ingredients));
        Assert.That(recipe.Steps, Is.EqualTo(Melktert.Recipe.Steps));
        Assert.That(recipe.Image, Is.EqualTo(Melktert.Recipe.Image));
        Assert.That(recipe.Author, Is.EqualTo(Melktert.Recipe.Author));
    }

    /// <summary>The fields read in order, a list has its two columns, and where it came from is locked.</summary>
    [Test]
    public void TheFields_ReadInOrder_AndTheSourceIsLocked()
    {
        var fields = RecipeForm.Of(Melktert).Fields;

        Assert.That(fields.Select(field => field.Name), Is.EqualTo(new[]
        {
            "name", "yield", "prep_minutes", "cook_minutes", "total_minutes", "image", "author", "ingredients", "steps",
            "page", "site", "read_at"
        }));
        Assert.That(fields.Where(field => field.ReadOnly).Select(field => field.Name), Is.EqualTo(new[] { "page", "site", "read_at" }));
        Assert.That(fields.Single(field => field.Name == "steps").RowColumns, Has.Count.EqualTo(2));
    }

    /// <summary>The page can't change, even if someone sends a new one: it's the link and the credit.</summary>
    [Test]
    public void TheSource_CannotChange()
    {
        var (recipe, _) = RecipeForm.Read(Melktert, With("page", "https://evil.example/"));

        Assert.That(recipe!.Source, Is.EqualTo(Melktert.Recipe.Source));
    }

    /// <summary>A name is required.</summary>
    [TestCase(null)]
    [TestCase("   ")]
    public void AName_IsRequired(string? name)
    {
        var (recipe, errors) = RecipeForm.Read(Melktert, With("name", name));

        Assert.That(recipe, Is.Null);
        Assert.That(errors.Keys, Is.EqualTo(new[] { "name" }));
    }

    /// <summary>An ingredient and a step are required, and a row left empty doesn't count as one.</summary>
    [TestCase("ingredients")]
    [TestCase("steps")]
    public void AListWithOnlyEmptyRows_IsRefused(string list)
    {
        var values = Untouched(Melktert);
        var rows = new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>(values.Rows) { [list] = [["Kors", "  "]] };

        var (recipe, errors) = RecipeForm.Read(Melktert, values with { Rows = rows });

        Assert.That(recipe, Is.Null);
        Assert.That(errors.Keys, Is.EqualTo(new[] { list }));
    }

    /// <summary>A row left empty is dropped, and an empty section is no section.</summary>
    [Test]
    public void AnEmptyRow_IsDropped()
    {
        var values = Untouched(Melktert);
        var rows = new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>(values.Rows)
        {
            ["steps"] = [["", " Meng. "], ["", ""]]
        };

        var (recipe, _) = RecipeForm.Read(Melktert, values with { Rows = rows });

        Assert.That(recipe!.Steps, Is.EqualTo(new[] { new RecipeStep(null, "Meng.") }));
    }

    /// <summary>What isn't a number of minutes is refused, beside its field.</summary>
    [TestCase("soon")]
    [TestCase("-5")]
    public void ABadTime_IsRefused(string typed)
    {
        var (recipe, errors) = RecipeForm.Read(Melktert, With("prep_minutes", typed));

        Assert.That(recipe, Is.Null);
        Assert.That(errors.Keys, Is.EqualTo(new[] { "prep_minutes" }));
    }

    /// <summary>A photo's address has to be a web address, as it reaches other people's screens.</summary>
    [TestCase("javascript:alert(1)")]
    [TestCase("not an address")]
    public void ABadAddress_IsRefused(string typed)
    {
        var (recipe, errors) = RecipeForm.Read(Melktert, With("image", typed));

        Assert.That(recipe, Is.Null);
        Assert.That(errors.Keys, Is.EqualTo(new[] { "image" }));
    }

    /// <summary>Emptying an optional field makes it unknown, never a made-up value.</summary>
    [Test]
    public void AnEmptyOptionalField_IsUnknown()
    {
        var (recipe, errors) = RecipeForm.Read(Melktert, With("yield", ""));

        Assert.That(errors, Is.Empty);
        Assert.That(recipe!.Yield, Is.Null);
    }
}
