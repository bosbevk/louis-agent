using LouisAgent.Packs.WhereToBuy.Branches.Map;
namespace LouisAgent.Packs.WhereToBuy.Branches;

/// <summary>
/// What a branch lookup found: the town as the map knows it, the shop asked for, its nearest branches (none when
/// the map lists none), and how far around the town was searched.
/// </summary>
public sealed record BranchSearch(MapPlace Town, string Shop, IReadOnlyList<Branch> Branches, int SearchedKm);
