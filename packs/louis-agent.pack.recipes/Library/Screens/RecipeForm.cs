using System.Globalization;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Screens.Editing;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Library.Screens;

/// <summary>
/// A kept recipe as a form, and back. It holds a recipe's rules: a name, an ingredient and a step are required, and the
/// page and the site can't change, since they're where the recipe came from and the credit.
/// </summary>
/// <remarks>
/// A plain class apart from the screen, so the rules test without a store. What's read back always starts from the kept
/// recipe, so a value someone sent for a read-only field has nothing to land in.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
internal static class RecipeForm
{
    private const string Name = "name";
    private const string Yield = "yield";
    private const string Prep = "prep_minutes";
    private const string Cook = "cook_minutes";
    private const string Total = "total_minutes";
    private const string Image = "image";
    private const string Author = "author";
    private const string Ingredients = "ingredients";
    private const string Steps = "steps";

    /// <summary>The recipe as its form's fields, in the order they read.</summary>
    public static ScreenRecord Of(StoredRecipe stored)
    {
        var recipe = stored.Recipe;
        string[] lineColumns = [Say(RecipeScreenMessage.Section), Say(RecipeScreenMessage.Line)];
        return new ScreenRecord(recipe.Name,
        [
            new ScreenField(Name, Say(RecipeScreenMessage.Name), ScreenFieldKind.Text, recipe.Name, Required: true),
            new ScreenField(Yield, Say(RecipeScreenMessage.Yield), ScreenFieldKind.Text, recipe.Yield),
            new ScreenField(Prep, Say(RecipeScreenMessage.PrepMinutes), ScreenFieldKind.Number, Minutes(recipe.PrepTime)),
            new ScreenField(Cook, Say(RecipeScreenMessage.CookMinutes), ScreenFieldKind.Number, Minutes(recipe.CookTime)),
            new ScreenField(Total, Say(RecipeScreenMessage.TotalMinutes), ScreenFieldKind.Number, Minutes(recipe.TotalTime)),
            new ScreenField(Image, Say(RecipeScreenMessage.Image), ScreenFieldKind.Link, recipe.Image?.AbsoluteUri),
            new ScreenField(Author, Say(RecipeScreenMessage.Author), ScreenFieldKind.Text, recipe.Author),
            new ScreenField(Ingredients, Say(RecipeScreenMessage.Ingredients), ScreenFieldKind.Rows, Required: true)
            {
                RowColumns = lineColumns,
                Rows = [.. recipe.Ingredients.Select(line => (IReadOnlyList<string>)[line.Section ?? "", line.Text])]
            },
            new ScreenField(Steps, Say(RecipeScreenMessage.Steps), ScreenFieldKind.Rows, Required: true)
            {
                RowColumns = lineColumns,
                Rows = [.. recipe.Steps.Select(line => (IReadOnlyList<string>)[line.Section ?? "", line.Text])]
            },
            new ScreenField("page", Say(RecipeScreenMessage.Page), ScreenFieldKind.Link, stored.Page.AbsoluteUri, ReadOnly: true),
            new ScreenField("site", Say(RecipeScreenMessage.Site), ScreenFieldKind.Text, stored.SiteSlug, ReadOnly: true),
            new ScreenField("read_at", Say(RecipeScreenMessage.ReadAt), ScreenFieldKind.Text,
                stored.ReadAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), ReadOnly: true)
        ]);
    }

    /// <summary>The recipe with what was typed, or why a field was refused. The page and the site stay as they were.</summary>
    public static (Recipe? Recipe, IReadOnlyDictionary<string, string> Errors) Read(StoredRecipe stored, ScreenValues values)
    {
        var errors = new Dictionary<string, string>();
        string? name = Text(values, Name);
        if (name is null) errors[Name] = Say(RecipeScreenMessage.NameRequired);

        var ingredients = Lines(values, Ingredients);
        if (ingredients.Count == 0) errors[Ingredients] = Say(RecipeScreenMessage.IngredientRequired);
        var steps = Lines(values, Steps);
        if (steps.Count == 0) errors[Steps] = Say(RecipeScreenMessage.StepRequired);

        var prep = Time(values, Prep, errors);
        var cook = Time(values, Cook, errors);
        var total = Time(values, Total, errors);
        var image = Address(values, Image, errors);
        if (errors.Count > 0)
            return (null, errors);

        return (stored.Recipe with
        {
            Name = name!,
            Yield = Text(values, Yield),
            PrepTime = prep,
            CookTime = cook,
            TotalTime = total,
            Image = image,
            Author = Text(values, Author),
            Ingredients = [.. ingredients.Select(line => new RecipeIngredient(line.Section, line.Text))],
            Steps = [.. steps.Select(line => new RecipeStep(line.Section, line.Text))]
        }, errors);
    }

    /// <summary>A plain field's words; empty is null, as what a page doesn't say always is.</summary>
    private static string? Text(ScreenValues values, string field) =>
        values.Values.GetValueOrDefault(field) is { } typed && !string.IsNullOrWhiteSpace(typed) ? typed.Trim() : null;

    /// <summary>A list's lines, in order; a line with no words is a row someone left empty, so it's dropped.</summary>
    private static List<(string? Section, string Text)> Lines(ScreenValues values, string field)
    {
        var rows = values.Rows.GetValueOrDefault(field) ?? [];
        return
        [
            .. rows.Where(row => row.Count == 2 && !string.IsNullOrWhiteSpace(row[1]))
                .Select(row => (string.IsNullOrWhiteSpace(row[0]) ? null : row[0].Trim(), row[1].Trim()))
        ];
    }

    /// <summary>A time in minutes; empty is unknown, and anything but a number of minutes is refused.</summary>
    private static TimeSpan? Time(ScreenValues values, string field, Dictionary<string, string> errors)
    {
        if (Text(values, field) is not { } typed)
            return null;
        if (double.TryParse(typed, NumberStyles.Float, CultureInfo.InvariantCulture, out double minutes) && minutes >= 0)
            return TimeSpan.FromMinutes(minutes);

        errors[field] = Say(RecipeScreenMessage.MinutesRefused);
        return null;
    }

    /// <summary>A web address; empty is none, and anything else is refused.</summary>
    private static Uri? Address(ScreenValues values, string field, Dictionary<string, string> errors)
    {
        if (Text(values, field) is not { } typed)
            return null;
        if (Uri.TryCreate(typed, UriKind.Absolute, out var address) && address.Scheme is "http" or "https")
            return address;

        errors[field] = Say(RecipeScreenMessage.AddressRefused);
        return null;
    }

    /// <summary>A time as the form shows it, in minutes.</summary>
    private static string? Minutes(TimeSpan? time) => time?.TotalMinutes.ToString(CultureInfo.InvariantCulture);

    /// <summary>One of the form's words.</summary>
    private static string Say(RecipeScreenMessage message) => MessageText.Format(message);
}
