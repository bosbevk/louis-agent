namespace LouisAgent.Packs.Shops.Messages;

using System.ComponentModel;

/// <summary>The shop pack's log lines (CL15: the pack's own, word for word as they were in core's LogMessage).</summary>
public enum ShopLogMessage
{
    [Description("{Shop} couldn't find the store for {Place} ({Reason}); the default store answers.")]
    ShopAreaUnavailable,

    [Description("{Shop} serves {Place} (its centre {Latitude}, {Longitude}) from stores {Stores}; searches from there use them.")]
    ShopStoreForPlace,

    [Description("{Shop} names no store for {Place} (its centre {Latitude}, {Longitude}); searches from there use its default store.")]
    ShopNoStoreForPlace,

    [Description("The shops' store cache {Path} can't be read ({Reason}); towns are looked up again.")]
    ShopStoreCacheUnreadable,

    [Description("{Shop} didn't answer the store lookup for {Place} with its store answer (refused, or its site changed); the default store answers, and the town is looked up again next time.")]
    ShopStoreAnswerUnreadable,

    [Description("A shop refused a search; its next searches wait {Seconds} s.")]
    ShopRefusedSearch,

    [Description("The shops' store cache {Path} can't be written ({Reason}); towns are kept in memory only.")]
    ShopStoreCacheUnwritable,

    [Description("{Shop} answered a search from {Place} from store {Store}, not one that serves {Place}; it's said so in the answer.")]
    ShopStoreNotUsed,
}
