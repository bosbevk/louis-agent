namespace LouisAgent.Packs.Recipes.Recipes;

/// <summary>
/// A recipe as the model will see it: what it's called, how much it makes, how long it takes, what goes in and what
/// to do. Whatever a page doesn't say stays null, never a made-up zero.
/// </summary>
/// <remarks>
/// A record because it is a value that is read once and never changes (ADR 0040). The words stay as the page wrote
/// them, in whatever language: the yield is still text such as "4 to 6" until a later step turns it into a number.
/// </remarks>
/// <param name="Name">The recipe's title (the page's <c>name</c>).</param>
/// <param name="Yield">
/// How much it makes, as written (the page's <c>recipeYield</c>); the first one if it lists several.
/// </param>
/// <param name="PrepTime">Time to get ready (the page's <c>prepTime</c>).</param>
/// <param name="CookTime">Time on the heat (the page's <c>cookTime</c>).</param>
/// <param name="TotalTime">Time from start to table (the page's <c>totalTime</c>).</param>
/// <param name="Ingredients">Every ingredient line as written (the page's <c>recipeIngredient</c>).</param>
/// <param name="Steps">What to do, in order (the page's <c>recipeInstructions</c>).</param>
/// <param name="Image">The first picture, as a full address (the page's <c>image</c>).</param>
/// <param name="Author">Who wrote it (the page's <c>author</c>, its <c>name</c> when it is an object).</param>
/// <param name="Source">The page the recipe was read from, as it was given to us.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0067-a-pages-own-data-before-its-text.md">ADR 0067: a page's own data before its text</seealso>
public sealed record Recipe(
    string Name,
    string? Yield,
    TimeSpan? PrepTime,
    TimeSpan? CookTime,
    TimeSpan? TotalTime,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<RecipeStep> Steps,
    Uri? Image,
    string? Author,
    Uri Source);
