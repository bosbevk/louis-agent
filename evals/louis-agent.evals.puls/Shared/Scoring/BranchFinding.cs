namespace LouisAgent.Evals.Puls.Shared.Scoring;

/// <summary>What a reply says about the user's shop under Where to buy (FX24): the case, so a failed check says which.</summary>
public enum BranchFinding
{
    /// <summary>The shop's branch is named, with a place.</summary>
    Named,

    /// <summary>The reply has no Where to buy section.</summary>
    NoSection,

    /// <summary>The section names no branch of that shop with a place (another shop's, a shop with no place, "any supermarket").</summary>
    NoBranchOfShop,

    /// <summary>The section gives only a "nearest" branch, with no place: the cook's shape for a branch it couldn't confirm.</summary>
    OnlyNearest,
}
