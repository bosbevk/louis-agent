using LouisAgent.Web.Evals.Client;

namespace LouisAgent.Web.Evals.Components.Marks;

/// <summary>A person asking to flip a mark: where it is, and their note or none. The page sends it.</summary>
/// <param name="Place">Where the mark is in the run.</param>
/// <param name="Note">Why, in one line, or none.</param>
public sealed record FlipAsk(MarkPlace Place, string? Note);
