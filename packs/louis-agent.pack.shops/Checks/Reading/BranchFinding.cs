namespace LouisAgent.Packs.Shops.Checks.Reading;

/// <summary>
/// What a reply says about the user's shop under Where to buy, case by case, so a failed check can say which one it
/// was.
/// </summary>
public enum BranchFinding
{
    /// <summary>The shop's branch is named, with a place.</summary>
    Named,

    /// <summary>The reply has no Where to buy section.</summary>
    NoSection,

    /// <summary>
    /// The section names no branch of that shop with a place: another shop's, a shop with no place, or "any
    /// supermarket".
    /// </summary>
    NoBranchOfShop,

    /// <summary>
    /// The section gives only a "nearest" branch, with no place: what the cook writes when it couldn't confirm one.
    /// </summary>
    OnlyNearest,
}
