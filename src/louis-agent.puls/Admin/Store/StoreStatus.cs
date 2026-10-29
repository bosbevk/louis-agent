using LouisAgent.Api.Contracts.Admin.Checks;

namespace LouisAgent.Puls.Admin.Store;

/// <summary>
/// What the pill at the top of the Benchmarks page says about the store, and whether a run may start: only when the
/// store is there and answers, since a paid run whose results are lost is the worst outcome.
/// </summary>
/// <remarks>Each text is a switch on the store's shape, one arm per case, instead of nested ternaries.</remarks>
public static class StoreStatus
{
    /// <summary>The store is there and answers, so a run may start.</summary>
    public static bool Up(StoreRow store) => store is { Configured: true, Error: null };

    /// <summary>The pill's word: the store, the store down, or no store.</summary>
    public static string Label(StoreRow store) => store switch
    {
        { Configured: false } => "no store",
        { Error: null } => "store",
        _ => "store down"
    };

    /// <summary>What the pill says on hover: why the store doesn't answer, or that it does.</summary>
    public static string Title(StoreRow store) => store switch
    {
        { Error: { } error } => error,
        { Configured: true } => "puls-mongo answers",
        _ => "No store"
    };
}
