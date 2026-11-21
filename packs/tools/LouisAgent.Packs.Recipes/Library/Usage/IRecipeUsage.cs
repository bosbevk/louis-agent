namespace LouisAgent.Packs.Recipes.Library.Usage;

/// <summary>
/// The library's counter: one small record per day, added to as things happen, so anyone can see whether the library is
/// working without running an eval. It counts what the pack sees; whether an answer used a recipe stays with the eval.
/// </summary>
/// <remarks>
/// The Computed Pattern (MongoDB): a little work on every write, so reading is one short list, in place of one row per
/// event, which grows forever and has to be added up on every read. Repository (Fowler, PoEAA) with two strategies
/// (Gang of Four), <see cref="MongoRecipeUsage"/> and <see cref="NoRecipeUsage"/>, so nothing else knows where it's
/// kept.
/// </remarks>
/// <seealso href="https://www.mongodb.com/blog/post/building-with-patterns-the-computed-pattern">The Computed Pattern</seealso>
/// <seealso href="https://www.martinfowler.com/eaaCatalog/repository.html">Repository</seealso>
public interface IRecipeUsage
{
    /// <summary>Adds to one number of one day, starting the day's record if it's the first.</summary>
    Task AddAsync(RecipeUsageKind kind, long by, DateOnly day, CancellationToken cancellationToken);

    /// <summary>Every day there's a record for, newest first.</summary>
    Task<IReadOnlyList<RecipeUsageDay>> DaysAsync(CancellationToken cancellationToken);
}
