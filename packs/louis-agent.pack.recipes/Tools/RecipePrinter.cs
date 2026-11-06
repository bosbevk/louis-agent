using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Tools;

/// <summary>
/// Prints a recipe as markdown the model can quote: the name, one line of how much it makes and how long it takes,
/// the ingredients and the method as numbered lines, the photo, the author and where the page is. It prints only what
/// the page gave, and at most 60 ingredients and 60 steps, saying so when it cuts.
/// </summary>
/// <remarks>
/// A numbered print and not JSON, because the model quotes a line by its number and reads prose and lists better
/// than braces; and a plain function of the record, so the same recipe always prints the same and a test can hold the
/// text. The lines are numbered straight on through the sections, so one number is one step.
/// </remarks>
internal static class RecipePrinter
{
    private const int MaxLines = 60;
    private const string Separator = " · ";

    /// <summary>The recipe as the model reads it. The link is the page the recipe was read from.</summary>
    public static string Print(Recipe recipe)
    {
        // The name, and the line under it when the page gave a yield or any time.
        var head = new List<string> { MessageText.Format(RecipeToolMessage.RecipeHeading, new { recipe.Name }) };
        if (HowMuchAndHowLong(recipe) is { Length: > 0 } line) head.Add(line);

        // The photo, the author and the link, each only if there is one.
        var tail = new List<string>();
        if (recipe.Image is not null)
            tail.Add(MessageText.Format(RecipeToolMessage.RecipeImage,
                new
                {
                    recipe.Name,
                    Image = recipe.Image.AbsoluteUri
                }));
        if (recipe.Author is not null)
            tail.Add(MessageText.Format(RecipeToolMessage.RecipeAuthor, new { recipe.Author }));
        tail.Add(MessageText.Format(RecipeToolMessage.RecipeLink, new { Url = recipe.Source.AbsoluteUri }));

        return string.Join("\n\n", string.Join('\n', head), Ingredients(recipe), Method(recipe),
            string.Join('\n', tail));
    }

    /// <summary>
    /// <c>Makes 10 pies · prep 20 min · cook 30 min · total 50 min</c> with only the parts the page gave; a time of
    /// nothing is no time.
    /// </summary>
    private static string HowMuchAndHowLong(Recipe recipe)
    {
        var parts = new List<string?>
        {
            recipe.Yield is null
                ? null
                : MessageText.Format(IsOnlyANumber(recipe.Yield) ? RecipeToolMessage.RecipeServes : RecipeToolMessage.RecipeMakes,
                    new { recipe.Yield }),
            Minutes(RecipeToolMessage.RecipePrep, recipe.PrepTime),
            Minutes(RecipeToolMessage.RecipeCook, recipe.CookTime),
            Minutes(RecipeToolMessage.RecipeTotal, recipe.TotalTime)
        };
        return string.Join(Separator, parts.OfType<string>());
    }

    /// <summary>
    /// A site's data giving only a number, or a range, nearly always means servings. Printed as "Makes 3" it read as
    /// not people, and the cook doubled it for four.
    /// </summary>
    private static bool IsOnlyANumber(string yield) =>
        yield.Replace(" to ", "-", StringComparison.Ordinal).All(c => char.IsAsciiDigit(c) || c is '-' or '–') &&
        yield.Any(char.IsAsciiDigit);

    /// <summary>A time as its message in whole minutes, or null when the page gave none.</summary>
    private static string? Minutes(RecipeToolMessage message, TimeSpan? time) =>
        time is { TotalMinutes: > 0 } t
            ? MessageText.Format(message, new { Minutes = (int)Math.Round(t.TotalMinutes) })
            : null;

    /// <summary>
    /// The ingredient lines as written, numbered straight on, with a sub-heading each time the section changes and the
    /// cut said when there are too many.
    /// </summary>
    private static string Ingredients(Recipe recipe)
    {
        var lines = new List<string> { MessageText.Format(RecipeToolMessage.IngredientsHeading) };
        lines.AddRange(Sectioned(recipe.Ingredients.Take(MaxLines).Select(i => (i.Section, i.Text))));
        if (recipe.Ingredients.Count > MaxLines)
            lines.Add(MessageText.Format(RecipeToolMessage.IngredientsCut,
                new
                {
                    Total = recipe.Ingredients.Count,
                    Shown = MaxLines
                }));
        return string.Join('\n', lines);
    }

    /// <summary>
    /// The steps numbered straight on, with a sub-heading each time the section changes, and the cut said when there
    /// are more than the most.
    /// </summary>
    private static string Method(Recipe recipe)
    {
        var lines = new List<string> { MessageText.Format(RecipeToolMessage.MethodHeading) };
        lines.AddRange(Sectioned(recipe.Steps.Take(MaxLines).Select(s => (s.Section, s.Text))));

        if (recipe.Steps.Count > MaxLines)
            lines.Add(MessageText.Format(RecipeToolMessage.StepsCut,
                new
                {
                    Total = recipe.Steps.Count,
                    Shown = MaxLines
                }));
        return string.Join('\n', lines);
    }

    /// <summary>
    /// The lines numbered straight on, with the section's name above the first line of each new section. Ingredients
    /// and steps both come through here, so they are laid out the same way.
    /// </summary>
    private static IEnumerable<string> Sectioned(IEnumerable<(string? Section, string Text)> items)
    {
        string? section = null;
        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            // A new section gets its name once; a line with no section adds nothing.
            if (item.Section is { } name && name != section)
                yield return MessageText.Format(RecipeToolMessage.SectionHeading, new { Section = name });
            section = item.Section ?? section;
            yield return Numbered(item.Text, index);
        }
    }

    /// <summary>A line of the list with its number, which counts from 1.</summary>
    private static string Numbered(string text, int index) =>
        MessageText.Format(RecipeToolMessage.NumberedLine, new
        {
            Number = index + 1,
            Text = text
        });
}
