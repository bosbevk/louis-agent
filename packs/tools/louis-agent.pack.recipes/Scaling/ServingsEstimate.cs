using LouisAgent.Packs.Recipes.Recipes;

namespace LouisAgent.Packs.Recipes.Scaling;

/// <summary>
/// About how many a recipe feeds when its page doesn't say, from its main ingredient: 175 g of meat, poultry or fish a
/// person, and four to a whole chicken. It names the line it went by, so the answer can say what the guess rests on.
/// </summary>
/// <remarks>
/// Half of a run's answers used a page with no yield, and the cook handled them each its own way. A number in code is
/// the same every time, and the print says it's an estimate. A dish with no such ingredient gets none, since a guess
/// there would be a number from nowhere (ADR 0080).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
internal sealed record ServingsEstimate(double People, string Line)
{
    private const double GramsAPerson = 175;
    private const double FewestGrams = 300;
    private const double AWholeChickenFeeds = 4;

    private static readonly HashSet<string> MainWords = new(
        ("mince beef lamb pork mutton chicken boerewors sausage sausages oxtail steak goulash brisket ribs fish hake snoek " +
         "maalvleis hoender lam skaapvleis varkvleis beesvleis beesstert vis wors").Split(' '),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Words that say a line only tastes of its meat.</summary>
    private static readonly HashSet<string> OnlyATaste = new(
        "stock powder sauce soup seasoning paste spice cube cubes gravy bouillon aftreksel".Split(' '),
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> WholeWords = new("whole hele".Split(' '), StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ChickenWords = new(
        "chicken chickens hoender hoenders".Split(' '), StringComparer.OrdinalIgnoreCase);

    /// <summary>The estimate, or nothing when the recipe has no main ingredient to go by.</summary>
    /// <remarks>Guard clauses, one per way a recipe can say it (ADR 0051).</remarks>
    public static ServingsEstimate? Of(Recipe recipe)
    {
        string[] lines = [.. recipe.Ingredients.Select(line => line.Text)];

        string? whole = lines.FirstOrDefault(IsAWholeChicken);
        if (whole is not null && RecipeAmount.CountIn(whole) is { } chickens)
            return new ServingsEstimate(chickens * AWholeChickenFeeds, whole);

        var meat = lines.Where(IsMeat).Select(line => (Line: line, Grams: RecipeAmount.GramsIn(line) ?? 0))
            .Where(found => found.Grams > 0).ToList();
        double grams = meat.Sum(found => found.Grams);
        if (grams < FewestGrams) return null;

        return new ServingsEstimate(Math.Round(grams / GramsAPerson), meat.MaxBy(found => found.Grams).Line);
    }

    /// <summary>Whether this line is the dish's meat, poultry or fish, not what only tastes of it.</summary>
    private static bool IsMeat(string line)
    {
        string[] words = RecipeAmount.Words(line);
        return words.Any(MainWords.Contains) && !words.Any(OnlyATaste.Contains);
    }

    /// <summary>Whether this line is a whole chicken, counted.</summary>
    private static bool IsAWholeChicken(string line)
    {
        string[] words = RecipeAmount.Words(line);
        return words.Any(WholeWords.Contains) && words.Any(ChickenWords.Contains);
    }
}
