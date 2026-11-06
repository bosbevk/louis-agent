using System.Globalization;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Recipes;
using LouisAgent.Packs.Recipes.Tools;

namespace LouisAgent.Packs.Recipes.Scaling;

/// <summary>
/// A recipe with every amount worked out, for the model to copy. One answer in ten or so had an amount wrong, always
/// the same way: a line missed, an ingredient doubled twice, water with no row left as it was. Sums in code can't do
/// that.
/// </summary>
/// <remarks>
/// It says which factor it used and why, and lists the amounts in the steps without changing the steps: the cook writes
/// those in the task's language, and knows a 150 g portion stays 150 g where this code can't (ADR 0080).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
internal static class RecipeScaler
{
    /// <summary>The factor's line, the ingredients scaled and numbered as the print is, the steps' amounts.</summary>
    public static string[] Print(Recipe recipe, int people, double times)
    {
        var (factor, line) = FactorFor(recipe, people, times);
        var scaled = recipe with
        {
            Ingredients = [.. recipe.Ingredients.Select(i => i with { Text = Scaled(i.Text, factor) })]
        };
        string[] amounts =
        [
            .. recipe.Steps.SelectMany((step, index) => RecipeAmount.InStep(step.Text, factor).Select(found =>
                MessageText.Format(RecipeToolMessage.ScaleStepAmount,
                    new
                    {
                        Number = index + 1,
                        found.From,
                        found.To
                    })))
        ];

        return amounts.Length == 0
            ? [line, RecipePrinter.Ingredients(scaled)]
            : [line, RecipePrinter.Ingredients(scaled), MessageText.Format(RecipeToolMessage.ScaleStepsHeading), .. amounts];
    }

    /// <summary>A line scaled, and marked when it holds an amount this couldn't scale or a number it left.</summary>
    /// <remarks>Guard clauses (ADR 0051). With a factor of one nothing was scaled, so nothing is marked.</remarks>
    private static string Scaled(string line, double factor)
    {
        string scaled = RecipeAmount.Scale(line, factor);
        if (factor == 1) return scaled;
        if (scaled == line && RecipeAmount.HoldsAnAmount(line))
            return MessageText.Format(RecipeToolMessage.ScaleLineNotScaled, new { Line = line });

        return RecipeAmount.LeavesAnAmount(line)
            ? MessageText.Format(RecipeToolMessage.ScaleLinePartly, new { Line = scaled })
            : scaled;
    }

    /// <summary>The factor to use and the line that says why.</summary>
    /// <remarks>Guard clauses, one per case (ADR 0051).</remarks>
    private static (double Factor, string Line) FactorFor(Recipe recipe, int people, double times)
    {
        if (times > 0)
            return (times, MessageText.Format(RecipeToolMessage.ScaledByTimes, new { recipe.Name, Factor = Written(times) }));
        if (people <= 0) return (1, MessageText.Format(RecipeToolMessage.ScaleWhole, new { recipe.Name }));
        if (string.IsNullOrWhiteSpace(recipe.Yield) && ServingsEstimate.Of(recipe) is { } estimate)
            return (people / estimate.People, MessageText.Format(RecipeToolMessage.ScaleEstimated,
                new
                {
                    recipe.Name,
                    estimate.Line,
                    Feeds = estimate.People,
                    Factor = Written(people / estimate.People),
                    People = people
                }));
        if (RecipeAmount.PeopleIn(recipe.Yield) is not { } served)
            return (1, MessageText.Format(RecipeToolMessage.ScaleKept, new { recipe.Name }));

        double factor = people / served;
        return (factor, MessageText.Format(RecipeToolMessage.ScaledForPeople,
            new
            {
                recipe.Name,
                Factor = Written(factor),
                People = people,
                recipe.Yield
            }));
    }

    /// <summary>A factor as a person reads it.</summary>
    private static string Written(double factor) => factor.ToString("0.##", CultureInfo.InvariantCulture);
}
