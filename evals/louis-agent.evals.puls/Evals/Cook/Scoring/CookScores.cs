
namespace LouisAgent.Evals.Puls.Evals.Cook.Scoring;

/// <summary>
/// One cook conversation as numbers (F31, the cook check's scores document): what Puls and its cook called and wrote. Each
/// property is the Python's key, in snake case when written (routed_right, priced_ratio). Nullable where the Python gives None:
/// an answer with no shopping list has no ingredients to count.
/// </summary>
public sealed record CookScores(
    string? Role,
    bool RoutedRight,
    bool AnsweredItself,
    IReadOnlyList<string> Hosts,
    IReadOnlyList<string> LocalHosts,
    IReadOnlyList<string> ShopHosts,
    bool SourcesLocal,
    bool PricedAtShops,
    int ShopToolCalls,
    int DealToolCalls,
    int DealsNamed,
    int UntooledShops,
    bool NoCatalogueLine,
    int StoreForArea,
    int StoreDefault,
    int? Ingredients,
    int? Priced,
    int? Unsourced,
    int? PerKg,
    int? Estimated,
    int NoShopRows,
    int WrongStore,
    double? PricedRatio,
    int RandAmounts,
    int UnconfirmedMarks,
    int Errors,
    string? FollowUp,
    string? FollowUpHandle) : EvalScores;
